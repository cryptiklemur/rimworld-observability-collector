using System.Collections.Generic;

namespace Cryptiklemur.RimObs.Collector.Panels;

public sealed class PanelRegistration {
    public int SchemaVersion { get; set; } = PanelRegistry.SchemaVersion;
    public string OwnerId { get; set; } = "";
    public List<PanelDefinition> Panels { get; set; } = new();
}
