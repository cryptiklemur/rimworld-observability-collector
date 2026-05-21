using MessagePack;

namespace Cryptiklemur.RimObs.Wire;

[MessagePackObject]
public sealed class MetricsBatch
{
    [Key(0)]
    public int[] MetricIds { get; set; } = [];

    [Key(1)]
    public string[] LabelCanonicals { get; set; } = [];

    [Key(2)]
    public byte[] Kinds { get; set; } = [];

    [Key(3)]
    public long[] Values { get; set; } = [];

    [Key(4)]
    public long[] SampleCounts { get; set; } = [];
}
