using MessagePack;

namespace Cryptiklemur.RimObs.Wire;

[MessagePackObject]
public sealed class PongMessage {
    [Key(0)]
    public string OwnerId { get; set; } = string.Empty;

    [Key(1)]
    public long PingSentAtUtcTicks { get; set; }

    [Key(2)]
    public string CollectorVersion { get; set; } = string.Empty;

    [Key(3)]
    public string? SessionId { get; set; }
}
