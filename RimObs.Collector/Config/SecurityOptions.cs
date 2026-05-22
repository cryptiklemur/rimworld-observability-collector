namespace Cryptiklemur.RimObs.Collector.Config;

public sealed class SecurityOptions {
    public bool CsrfOriginCheckEnabled { get; set; } = true;
    public string CliBearerTokenEnvVar { get; set; } = "RIMOBS_TOKEN";
}
