using MessagePack;

namespace Cryptiklemur.RimObs.Wire;

[MessagePackObject]
public sealed class MetricRegistrationsBatch {
    [Key(0)]
    public int[] MetricIds { get; set; } = [];

    [Key(1)]
    public string[] Names { get; set; } = [];

    [Key(2)]
    public byte[] Kinds { get; set; } = [];

    [Key(3)]
    public string[] Units { get; set; } = [];
}
