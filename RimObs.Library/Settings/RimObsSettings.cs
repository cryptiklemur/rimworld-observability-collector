using Verse;

namespace Cryptiklemur.RimObs.Settings;

public sealed class RimObsSettings : ModSettings {
    public bool AutoOpenDashboard = true;

    public override void ExposeData() {
        base.ExposeData();
        Scribe_Values.Look(ref AutoOpenDashboard, "autoOpenDashboard", true);
    }
}
