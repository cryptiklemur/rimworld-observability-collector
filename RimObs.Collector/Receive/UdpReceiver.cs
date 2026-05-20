using System.Net;
using System.Net.Sockets;
using Cryptiklemur.RimObs.Collector.Aggregation;
using Cryptiklemur.RimObs.Wire;
using MessagePack;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cryptiklemur.RimObs.Collector.Receive;

public sealed class UdpReceiver : BackgroundService
{
    private readonly SessionAggregator _aggregator;
    private readonly ILogger<UdpReceiver> _log;
    private readonly int _port;
    private UdpClient? _client;

    public UdpReceiver(SessionAggregator aggregator, ILogger<UdpReceiver> log, int port = 17654)
    {
        _aggregator = aggregator;
        _log = log;
        _port = port;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _client = new UdpClient(new IPEndPoint(IPAddress.Loopback, _port));
        _log.LogInformation("UDP receiver listening on 127.0.0.1:{Port}", _port);

        stoppingToken.Register(() =>
        {
            try
            {
                _client?.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // Already disposed by another path.
            }
        });

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                UdpReceiveResult result = await _client.ReceiveAsync(stoppingToken).ConfigureAwait(false);
                Dispatch(result.Buffer);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "UDP receive error");
            }
        }
    }

    private void Dispatch(byte[] bytes)
    {
        TelemetryBatch envelope;
        try
        {
            envelope = MessagePackSerializer.Deserialize<TelemetryBatch>(bytes);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Failed to deserialize TelemetryBatch envelope ({Bytes} bytes)", bytes.Length);
            return;
        }

        if (envelope.SchemaVersion != SchemaVersion.Current)
        {
            _log.LogWarning("Dropping batch with schema_version={Version} (expected {Expected})", envelope.SchemaVersion, SchemaVersion.Current);
            return;
        }

        _aggregator.OnBatchReceived(bytes.Length);

        try
        {
            switch (envelope.BatchType)
            {
                case BatchType.SessionMeta:
                    _aggregator.OnSessionMeta(MessagePackSerializer.Deserialize<SessionMeta>(envelope.Payload));
                    break;
                case BatchType.SectionRegistrations:
                    _aggregator.OnSectionRegistrations(MessagePackSerializer.Deserialize<SectionRegistrationsBatch>(envelope.Payload));
                    break;
                case BatchType.Sections:
                    _aggregator.OnSectionBatch(MessagePackSerializer.Deserialize<SectionBatch>(envelope.Payload));
                    break;
                case BatchType.Ping:
                    // Ping handling deferred to Phase 2 (collector discovery handshake).
                    break;
                default:
                    _log.LogDebug("Ignoring batch_type={Type} (not implemented in M0)", envelope.BatchType);
                    break;
            }
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Failed to dispatch batch_type={Type}", envelope.BatchType);
        }
    }
}
