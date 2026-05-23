namespace Cryptiklemur.RimObs.Wire.Control;

public sealed class ControlPatchResponse {
    public int PatchId { get; set; }
    public int SectionId { get; set; }
    public string SectionName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ErrorReason { get; set; }
}
