namespace Cryptiklemur.RimObs.Collector.Aggregation;

public readonly record struct TimelineBucket(long EpochSeconds, long Count, long TotalTicks);
