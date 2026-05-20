using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using Cryptiklemur.RimObs;
using Cryptiklemur.RimObs.Profile;
using Cryptiklemur.RimObs.Wire;
using MessagePack;

namespace Cryptiklemur.RimObs.Transport;

public sealed class UdpTelemetrySink : ISampleSink, IDisposable
{
    public const int DefaultPort = 17654;
    private const int RingCapacity = 16384;
    private const int BatchSize = 256;
    private const int DrainIntervalMs = 100;

    private readonly UdpClient _client;
    private readonly IPEndPoint _endpoint;
    private readonly SampleRingBuffer _ring = new(RingCapacity);
    private readonly Thread _sender;
    private readonly ManualResetEventSlim _stop = new(false);
    private readonly string _ownerId;

    private readonly int[] _sectionIds = new int[BatchSize];
    private readonly long[] _startTimestamps = new long[BatchSize];
    private readonly long[] _elapsedTicks = new long[BatchSize];
    private readonly int[] _registrationIds = new int[64];
    private readonly string[] _registrationNames = new string[64];

    private ulong _sequence;
    private bool _metaSent;
    private long _sent;
    private long _bytesSent;
    private long _sendErrors;
    private Exception? _lastSendError;

    public UdpTelemetrySink(string ownerId, int port = DefaultPort, string host = "127.0.0.1")
    {
        _ownerId = ownerId ?? throw new ArgumentNullException(nameof(ownerId));
        _client = new UdpClient(AddressFamily.InterNetwork);
        _endpoint = new IPEndPoint(IPAddress.Parse(host), port);
        _sender = new Thread(SenderLoop)
        {
            Name = "RimObs.UdpSender",
            IsBackground = true,
        };
    }

    public long SamplesSent => Interlocked.Read(ref _sent);
    public long BytesSent => Interlocked.Read(ref _bytesSent);
    public long SamplesDropped => _ring.Dropped;
    public long SendErrors => Interlocked.Read(ref _sendErrors);
    public Exception? LastSendError => Volatile.Read(ref _lastSendError);

    public void Start()
    {
        _sender.Start();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RecordSection(int sectionId, long startTimestamp, long elapsedTicks)
    {
        _ring.TryWrite(sectionId, startTimestamp, elapsedTicks);
    }

    private void SenderLoop()
    {
        while (!_stop.IsSet)
        {
            try
            {
                if (!_metaSent && SessionAnchor.IsInitialized)
                {
                    SendSessionMeta();
                    _metaSent = true;
                }

                FlushRegistrations();
                FlushSamples();
            }
            catch (Exception ex) when (ex is SocketException or ObjectDisposedException or InvalidOperationException)
            {
                // Expected when the collector is absent or the socket has been torn down.
                Interlocked.Increment(ref _sendErrors);
                Volatile.Write(ref _lastSendError, ex);
            }
            _stop.Wait(DrainIntervalMs);
        }
    }

    private void SendSessionMeta()
    {
        SessionMeta meta = new()
        {
            SessionId = SessionAnchor.SessionId,
            StartedUtcTicks = SessionAnchor.StartedUtc.Ticks,
            StopwatchFrequency = SessionAnchor.StopwatchFrequency,
            AnchorTimestamp = SessionAnchor.AnchorTimestamp,
            LibraryVersion = BuildInfo.Revision,
            GameVersion = string.Empty,
        };
        byte[] payload = MessagePackSerializer.Serialize(meta);
        SendBatch(BatchType.SessionMeta, payload);
    }

    private void FlushRegistrations()
    {
        int n = SectionRegistry.DrainPendingRegistrations(_registrationIds, _registrationNames);
        if (n == 0)
            return;
        SectionRegistrationsBatch batch = new()
        {
            SectionIds = Slice(_registrationIds, n),
            Names = Slice(_registrationNames, n),
        };
        byte[] payload = MessagePackSerializer.Serialize(batch);
        SendBatch(BatchType.SectionRegistrations, payload);
    }

    private void FlushSamples()
    {
        while (true)
        {
            int n = _ring.Drain(_sectionIds, _startTimestamps, _elapsedTicks, BatchSize);
            if (n == 0)
                return;

            SectionBatch batch = new()
            {
                SectionIds = Slice(_sectionIds, n),
                StartTimestamps = Slice(_startTimestamps, n),
                ElapsedTicks = Slice(_elapsedTicks, n),
            };
            byte[] payload = MessagePackSerializer.Serialize(batch);
            SendBatch(BatchType.Sections, payload);
            Interlocked.Add(ref _sent, n);
        }
    }

    private void SendBatch(BatchType type, byte[] payload)
    {
        TelemetryBatch envelope = new()
        {
            SchemaVersion = SchemaVersion.Current,
            Sequence = ++_sequence,
            OwnerId = _ownerId,
            BatchType = type,
            Payload = payload,
        };
        byte[] bytes = MessagePackSerializer.Serialize(envelope);
        _client.Send(bytes, bytes.Length, _endpoint);
        Interlocked.Add(ref _bytesSent, bytes.Length);
    }

    private static T[] Slice<T>(T[] src, int n)
    {
        T[] dst = new T[n];
        Array.Copy(src, 0, dst, 0, n);
        return dst;
    }

    public void Dispose()
    {
        _stop.Set();
        try
        {
            _sender.Join(1000);
        }
        catch (ThreadStateException)
        {
            // Thread was never started; nothing to join.
        }
        _client.Dispose();
        _stop.Dispose();
    }
}
