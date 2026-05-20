using MessagePack;

namespace Cryptiklemur.RimObs.Wire;

[MessagePackObject]
public sealed class PingMessage
{
    [Key(0)]
    public string OwnerId { get; set; } = string.Empty;

    [Key(1)]
    public long SentAtUtcTicks { get; set; }
}
