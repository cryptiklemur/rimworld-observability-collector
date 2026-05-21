using System.Collections.Concurrent;
using Cryptiklemur.RimObs.Wire;

namespace Cryptiklemur.RimObs.Collector.Aggregation;

public sealed class SessionAggregator
{
    private const int GcEventRingCapacity = 1024;

    private readonly ConcurrentDictionary<int, SectionStats> _sections = new();
    private readonly BoundedRecordRing<GcEventRecord> _gcEvents = new(GcEventRingCapacity);
    private SessionMeta? _meta;
    private long _totalSamples;
    private long _totalBatches;
    private long _totalBytes;
    private long _totalGcEvents;
    private long _totalAllocations;
    private DateTime _lastBatchUtc;

    public SessionMeta? Meta => _meta;
    public long TotalSamples => Interlocked.Read(ref _totalSamples);
    public long TotalBatches => Interlocked.Read(ref _totalBatches);
    public long TotalBytes => Interlocked.Read(ref _totalBytes);
    public DateTime LastBatchUtc => _lastBatchUtc;
    public int SectionCount => _sections.Count;
    public long TotalGcEvents => Interlocked.Read(ref _totalGcEvents);
    public long TotalAllocations => Interlocked.Read(ref _totalAllocations);

    public IReadOnlyCollection<SectionStats> Sections => _sections.Values.ToArray();

    public GcEventRecord[] SnapshotGcEvents(int limit) => _gcEvents.SnapshotNewestFirst(limit);

    public void OnBatchReceived(int byteCount)
    {
        Interlocked.Increment(ref _totalBatches);
        Interlocked.Add(ref _totalBytes, byteCount);
        _lastBatchUtc = DateTime.UtcNow;
    }

    public void OnSessionMeta(SessionMeta meta)
    {
        _meta = meta;
    }

    public void OnSectionRegistrations(SectionRegistrationsBatch batch)
    {
        int n = Math.Min(batch.SectionIds.Length, batch.Names.Length);
        for (int i = 0; i < n; i++)
        {
            int id = batch.SectionIds[i];
            string name = batch.Names[i];
            SectionStats stats = _sections.GetOrAdd(id, key => new SectionStats { SectionId = key });
            stats.Name = name;
        }
    }

    public void OnGcEvents(GcEventsBatch batch)
    {
        int n = batch.Generations.Length;
        int pauseLen = batch.PauseTypes.Length;
        int heapBeforeLen = batch.HeapBefore.Length;
        int heapAfterLen = batch.HeapAfter.Length;
        int durLen = batch.DurationMicros.Length;
        int tickLen = batch.Ticks.Length;
        int rateLen = batch.AllocationRateBytesPerMinute.Length;
        for (int i = 0; i < n; i++)
        {
            GcEventRecord record = new(
                generation: batch.Generations[i],
                pauseType: i < pauseLen ? batch.PauseTypes[i] : (byte)0,
                heapBefore: i < heapBeforeLen ? batch.HeapBefore[i] : 0L,
                heapAfter: i < heapAfterLen ? batch.HeapAfter[i] : 0L,
                durationMicros: i < durLen ? batch.DurationMicros[i] : 0L,
                ticks: i < tickLen ? batch.Ticks[i] : 0L,
                allocationRateBytesPerMinute: i < rateLen ? batch.AllocationRateBytesPerMinute[i] : 0L
            );
            _gcEvents.Add(in record);
        }
        Interlocked.Add(ref _totalGcEvents, n);
    }

    public void OnAllocations(AllocationsBatch batch)
    {
        int n = batch.WindowStartTimestamps.Length;
        Interlocked.Add(ref _totalAllocations, n);
    }

    public void OnSectionBatch(SectionBatch batch)
    {
        int n = Math.Min(batch.SectionIds.Length, Math.Min(batch.ElapsedTicks.Length, batch.StartTimestamps.Length));
        for (int i = 0; i < n; i++)
        {
            int id = batch.SectionIds[i];
            long elapsed = batch.ElapsedTicks[i];
            long start = batch.StartTimestamps[i];
            SectionStats stats = _sections.GetOrAdd(id, key => new SectionStats { SectionId = key });
            Interlocked.Increment(ref stats.SampleCount);
            Interlocked.Add(ref stats.TotalElapsedTicks, elapsed);
            stats.LastStartTimestamp = start;
            UpdateMin(ref stats.MinElapsedTicks, elapsed);
            UpdateMax(ref stats.MaxElapsedTicks, elapsed);
        }
        Interlocked.Add(ref _totalSamples, n);
    }

    private static void UpdateMin(ref long location, long value)
    {
        long current;
        do
        {
            current = Interlocked.Read(ref location);
            if (value >= current)
                return;
        } while (Interlocked.CompareExchange(ref location, value, current) != current);
    }

    private static void UpdateMax(ref long location, long value)
    {
        long current;
        do
        {
            current = Interlocked.Read(ref location);
            if (value <= current)
                return;
        } while (Interlocked.CompareExchange(ref location, value, current) != current);
    }
}
