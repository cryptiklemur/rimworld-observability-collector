using MessagePack;

namespace Cryptiklemur.RimObs.Wire;

[MessagePackObject]
public sealed class SectionRegistrationsBatch
{
    [Key(0)]
    public int[] SectionIds { get; set; } = [];

    [Key(1)]
    public string[] Names { get; set; } = [];
}
