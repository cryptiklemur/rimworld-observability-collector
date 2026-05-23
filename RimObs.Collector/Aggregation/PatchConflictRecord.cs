namespace Cryptiklemur.RimObs.Collector.Aggregation;

public sealed record PatchConflictRecord(
    string SectionName,
    string TargetMethod,
    string OtherOwner,
    byte PatchType,
    int Priority,
    string PatchMethod
);
