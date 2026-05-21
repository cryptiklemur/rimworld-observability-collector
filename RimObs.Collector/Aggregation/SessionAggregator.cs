using System.Collections.Concurrent;
using Cryptiklemur.RimObs.Collector.Storage;
using Cryptiklemur.RimObs.Wire;

namespace Cryptiklemur.RimObs.Collector.Aggregation;

public sealed class SessionAggregator {
    private const int GcEventRingCapacity = 1024;

    private readonly ConcurrentDictionary<int, SectionStats> _sections = new();
    private readonly ConcurrentDictionary<int, MetricStats> _metrics = new();
    private readonly ConcurrentDictionary<long, CallEdgeStats> _callEdges = new();
    private readonly BoundedRecordRing<GcEventRecord> _gcEvents = new(GcEventRingCapacity);
    private readonly ISessionPersister? _persister;
    private SessionMeta? _meta;

    public SessionAggregator()
        : this(persister: null) {
    }

    public SessionAggregator(ISessionPersister? persister) {
        _persister = persister;
    }

    private long _totalSamples;
    private long _totalBatches;
    private long _totalBytes;
    private long _totalGcEvents;
    private long _totalAllocations;
    private long _totalMetricObservations;
    private DateTime _lastBatchUtc;

    public SessionMeta? Meta => _meta;
    public long TotalSamples => Interlocked.Read(ref _totalSamples);
    public long TotalBatches => Interlocked.Read(ref _totalBatches);
    public long TotalBytes => Interlocked.Read(ref _totalBytes);
    public DateTime LastBatchUtc => _lastBatchUtc;
    public int SectionCount => _sections.Count;
    public long TotalGcEvents => Interlocked.Read(ref _totalGcEvents);
    public long TotalAllocations => Interlocked.Read(ref _totalAllocations);
    public long TotalMetricObservations => Interlocked.Read(ref _totalMetricObservations);
    public int MetricCount => _metrics.Count;

    public IReadOnlyCollection<SectionStats> Sections => _sections.Values.ToArray();
    public IReadOnlyCollection<MetricStats> Metrics => _metrics.Values.ToArray();
    public IReadOnlyCollection<CallEdgeStats> CallEdges => _callEdges.Values.ToArray();

    public GcEventRecord[] SnapshotGcEvents(int limit) => _gcEvents.SnapshotNewestFirst(limit);

    public void OnBatchReceived(int byteCount) {
        Interlocked.Increment(ref _totalBatches);
        Interlocked.Add(ref _totalBytes, byteCount);
        _lastBatchUtc = DateTime.UtcNow;
    }

    public void OnSessionMeta(SessionMeta meta) {
        _meta = meta;
        _persister?.WriteSessionMeta(meta);
    }

    public void OnSectionRegistrations(SectionRegistrationsBatch batch) {
        int n = Math.Min(batch.SectionIds.Length, batch.Names.Length);
        for (int i = 0; i < n; i++) {
            int id = batch.SectionIds[i];
            string name = batch.Names[i];
            SectionStats stats = _sections.GetOrAdd(id, key => new SectionStats { SectionId = key });
            stats.Name = name;
        }
    }

    public void OnMetricRegistrations(MetricRegistrationsBatch batch) {
        int n = Math.Min(
            batch.MetricIds.Length,
            Math.Min(batch.Names.Length, Math.Min(batch.Kinds.Length, batch.Units.Length))
        );
        for (int i = 0; i < n; i++) {
            int id = batch.MetricIds[i];
            MetricStats stats = _metrics.GetOrAdd(id, key => new MetricStats(key));
            stats.Name = batch.Names[i];
            stats.Kind = batch.Kinds[i];
            stats.Unit = batch.Units[i];
        }
    }

    public void OnMetrics(MetricsBatch batch) {
        int n = Math.Min(
            batch.MetricIds.Length,
            Math.Min(batch.LabelCanonicals.Length, Math.Min(batch.Kinds.Length, Math.Min(batch.Values.Length, batch.SampleCounts.Length)))
        );
        for (int i = 0; i < n; i++) {
            int id = batch.MetricIds[i];
            string canonical = batch.LabelCanonicals[i];
            long value = batch.Values[i];
            long samples = batch.SampleCounts[i];
            MetricStats stats = _metrics.GetOrAdd(id, key => new MetricStats(key));
            stats.Kind = batch.Kinds[i];
            MetricLabelStats labelStats = stats.Labels.GetOrAdd(canonical, key => new MetricLabelStats(key));
            Interlocked.Exchange(ref labelStats.LatestValue, value);
            Interlocked.Add(ref labelStats.TotalSampleCount, samples);
        }
        Interlocked.Add(ref _totalMetricObservations, n);
    }

    public void OnGcEvents(GcEventsBatch batch) {
        int n = batch.Generations.Length;
        int pauseLen = batch.PauseTypes.Length;
        int heapBeforeLen = batch.HeapBefore.Length;
        int heapAfterLen = batch.HeapAfter.Length;
        int durLen = batch.DurationMicros.Length;
        int tickLen = batch.Ticks.Length;
        int rateLen = batch.AllocationRateBytesPerMinute.Length;
        for (int i = 0; i < n; i++) {
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

    public void OnAllocations(AllocationsBatch batch) {
        int n = batch.WindowStartTimestamps.Length;
        Interlocked.Add(ref _totalAllocations, n);
    }

    public void OnSectionBatch(SectionBatch batch) {
        int n = Math.Min(batch.SectionIds.Length, Math.Min(batch.ElapsedTicks.Length, batch.StartTimestamps.Length));
        int parentLen = batch.ParentIds.Length;
        for (int i = 0; i < n; i++) {
            int id = batch.SectionIds[i];
            long elapsed = batch.ElapsedTicks[i];
            long start = batch.StartTimestamps[i];
            SectionStats stats = _sections.GetOrAdd(id, key => new SectionStats { SectionId = key });
            Interlocked.Increment(ref stats.SampleCount);
            Interlocked.Add(ref stats.TotalElapsedTicks, elapsed);
            stats.LastStartTimestamp = start;
            UpdateMin(ref stats.MinElapsedTicks, elapsed);
            UpdateMax(ref stats.MaxElapsedTicks, elapsed);

            int parentId = i < parentLen ? batch.ParentIds[i] : CallTreeBuilder.NoParent;
            long edgeKey = ((long)(uint)parentId << 32) | (uint)id;
            CallEdgeStats edge = _callEdges.GetOrAdd(edgeKey, _ => new CallEdgeStats { ParentId = parentId, SectionId = id });
            Interlocked.Increment(ref edge.CallCount);
            Interlocked.Add(ref edge.TotalElapsedTicks, elapsed);
        }
        Interlocked.Add(ref _totalSamples, n);
    }

    private static void UpdateMin(ref long location, long value) {
        long current;
        do {
            current = Interlocked.Read(ref location);
            if (value >= current)
                return;
        } while (Interlocked.CompareExchange(ref location, value, current) != current);
    }

    private static void UpdateMax(ref long location, long value) {
        long current;
        do {
            current = Interlocked.Read(ref location);
            if (value <= current)
                return;
        } while (Interlocked.CompareExchange(ref location, value, current) != current);
    }
}
