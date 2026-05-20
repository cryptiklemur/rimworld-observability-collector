using MessagePack;

namespace Cryptiklemur.RimObs.Wire;

[MessagePackObject]
public sealed class SectionBatch
{
    [Key(0)]
    public int[] SectionIds { get; set; } = [];

    [Key(1)]
    public long[] ElapsedTicks { get; set; } = [];

    [Key(2)]
    public long[] StartTimestamps { get; set; } = [];
}
