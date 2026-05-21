using System;

namespace Cryptiklemur.RimObs.Transport;

public sealed class CollectorCandidate
{
    public CollectorCandidate(string executablePath, Version version, string? rid = null)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
            throw new ArgumentException("executablePath must be provided", nameof(executablePath));
        if (version is null)
            throw new ArgumentNullException(nameof(version));
        ExecutablePath = executablePath;
        Version = version;
        Rid = rid;
    }

    public string ExecutablePath { get; }
    public Version Version { get; }
    public string? Rid { get; }

    public static Version ParseLooseSemver(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            throw new ArgumentException("version string must be provided", nameof(s));
        int dash = s.IndexOf('-');
        int plus = s.IndexOf('+');
        int cut = -1;
        if (dash >= 0) cut = dash;
        if (plus >= 0 && (cut < 0 || plus < cut)) cut = plus;
        string core = cut < 0 ? s : s.Substring(0, cut);
        return Version.Parse(core);
    }
}
