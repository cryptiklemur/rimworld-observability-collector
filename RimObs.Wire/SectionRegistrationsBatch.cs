namespace Cryptiklemur.RimObs.Wire;

public sealed class SectionRegistrationsBatch {
    public int[] SectionIds { get; set; } = [];

    public string[] Names { get; set; } = [];

    public string?[] Subsystems { get; set; } = [];
}
