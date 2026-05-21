using Cryptiklemur.RimObs.Collector.Security;

namespace Cryptiklemur.RimObs.Collector.Runtime;

public static class RuntimeFiles {
    public const string TokenFileName = "collector.token";
    public const string PortFileName = "collector.port";

    public static void WriteAll(string configDir, CollectorToken token, int port) {
        if (string.IsNullOrWhiteSpace(configDir))
            throw new ArgumentException("Config directory must be provided.", nameof(configDir));

        Directory.CreateDirectory(configDir);

        string tokenPath = Path.Combine(configDir, TokenFileName);
        File.WriteAllText(tokenPath, token.Value);
        TryRestrictFilePermissions(tokenPath);

        string portPath = Path.Combine(configDir, PortFileName);
        File.WriteAllText(portPath, port.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    private static void TryRestrictFilePermissions(string path) {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
            return;
        try {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
        catch {
        }
    }
}
