using MessagePack;

namespace Cryptiklemur.RimObs.Wire;

[MessagePackObject]
public sealed class AllocationsBatch {
    [Key(0)]
    public long[] WindowStartTimestamps { get; set; } = [];

    [Key(1)]
    public long[] WindowDurationsMs { get; set; } = [];

    [Key(2)]
    public long[] BytesAllocated { get; set; } = [];

    [Key(3)]
    public long[] SamplesCount { get; set; } = [];
}
