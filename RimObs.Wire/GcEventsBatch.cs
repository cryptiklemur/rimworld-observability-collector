using MessagePack;

namespace Cryptiklemur.RimObs.Wire;

[MessagePackObject]
public sealed class GcEventsBatch
{
    [Key(0)]
    public byte[] Generations { get; set; } = [];

    [Key(1)]
    public byte[] PauseTypes { get; set; } = [];

    [Key(2)]
    public long[] HeapBefore { get; set; } = [];

    [Key(3)]
    public long[] HeapAfter { get; set; } = [];

    [Key(4)]
    public long[] DurationMicros { get; set; } = [];

    [Key(5)]
    public long[] Ticks { get; set; } = [];

    [Key(6)]
    public long[] AllocationRateBytesPerMinute { get; set; } = [];
}
