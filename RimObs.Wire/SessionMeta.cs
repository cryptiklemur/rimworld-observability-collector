using MessagePack;

namespace Cryptiklemur.RimObs.Wire;

[MessagePackObject]
public sealed class SessionMeta
{
    [Key(0)]
    public string SessionId { get; set; } = string.Empty;

    [Key(1)]
    public long StartedUtcTicks { get; set; }

    [Key(2)]
    public long StopwatchFrequency { get; set; }

    [Key(3)]
    public long AnchorTimestamp { get; set; }

    [Key(4)]
    public string LibraryVersion { get; set; } = string.Empty;

    [Key(5)]
    public string GameVersion { get; set; } = string.Empty;
}
