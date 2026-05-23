namespace Cryptiklemur.RimObs.Wire.Control;

public sealed class ControlPatchEntry {
    public int PatchId { get; set; }
    public string Signature { get; set; } = string.Empty;
    public int SectionId { get; set; }
    public string Status { get; set; } = string.Empty;
}
