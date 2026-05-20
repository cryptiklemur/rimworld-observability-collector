using MessagePack;

namespace Cryptiklemur.RimObs.Wire;

[MessagePackObject]
public sealed class TelemetryBatch
{
    [Key(0)]
    public int SchemaVersion { get; set; }

    [Key(1)]
    public ulong Sequence { get; set; }

    [Key(2)]
    public string OwnerId { get; set; } = string.Empty;

    [Key(3)]
    public byte BatchType { get; set; }

    [Key(4)]
    public byte[] Payload { get; set; } = [];
}
