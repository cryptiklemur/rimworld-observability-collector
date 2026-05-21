using System.Collections.Concurrent;
using Cryptiklemur.RimObs.Wire;

namespace Cryptiklemur.RimObs.Collector.Aggregation;

public sealed class SessionAggregator
{
    private readonly ConcurrentDictionary<int, SectionStats> _sections = new();
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
