namespace Cryptiklemur.RimObs.Collector.Aggregation;

public sealed class SectionStats
{
    public int SectionId { get; init; }
    public string Name { get; set; } = string.Empty;
    public long SampleCount;
    public long TotalElapsedTicks;
    public long MinElapsedTicks = long.MaxValue;
    public long MaxElapsedTicks;
    public long LastStartTimestamp;
}
