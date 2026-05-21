namespace Cryptiklemur.RimObs.Collector.Aggregation;

public sealed class MetricLabelStats
{
    public MetricLabelStats(string canonical)
    {
        Canonical = canonical;
    }

    public string Canonical { get; }
    public long LatestValue;
    public long TotalSampleCount;
}
