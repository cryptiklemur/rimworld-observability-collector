using Cryptiklemur.RimObs.Api;
using Cryptiklemur.RimObs.Config;
using Cryptiklemur.RimObs.Observers;
using Cryptiklemur.RimObs.Patching;
using Cryptiklemur.RimObs.Profile;
using Cryptiklemur.RimObs.Session;
using Cryptiklemur.RimObs.Transport;
using Verse;

namespace Cryptiklemur.RimObs.Bootstrap;

public sealed class RimObsMod : Mod {
    private const string FrameworkOwnerId = "cryptiklemur.rimobs";
    private const string CollectorHost = "127.0.0.1";
    private static readonly System.TimeSpan s_LaunchTimeout = System.TimeSpan.FromSeconds(2);
    private static UdpTelemetrySink? s_Sink;
    private static CollectorConfigClient? s_ConfigClient;

    private static string ResolveOwnerId(ModContentPack content) =>
        string.IsNullOrEmpty(content?.PackageId) ? FrameworkOwnerId : content!.PackageId;

    private static CollectorLaunchResult EnsureCollectorRunning(string ownerId) {
        System.Collections.Generic.List<CollectorCandidate> candidates = CollectCandidates();
        return CollectorLauncher.EnsureRunning(
            candidates,
            CollectorHost,
            UdpTelemetrySink.DefaultPort,
            ownerId,
            CollectorLauncher.DefaultProbeTimeout,
            s_LaunchTimeout
        );
    }

    private static System.Collections.Generic.List<CollectorCandidate> CollectCandidates() {
        System.Collections.Generic.List<CollectorCandidate> candidates = new();
        foreach (ModContentPack pack in LoadedModManager.RunningModsListForReading) {
            string rootDir = pack.RootDir;
            if (string.IsNullOrEmpty(rootDir))
                continue;
            string collectorDir = System.IO.Path.Combine(rootDir, "Assemblies", CollectorScanner.CollectorDirName);
            CollectorCandidate? candidate = CollectorScanner.TryReadCandidate(collectorDir);
            if (candidate != null)
                candidates.Add(candidate);
        }

        return candidates;
    }

    private static void StartConfigPoll(string host, int port) {
        if (s_ConfigClient != null)
            return;
        CollectorConfigClient client = new($"http://{host}:{port}");
        client.Start();
        s_ConfigClient = client;
    }

    public RimObsMod(ModContentPack content) : base(content) {
        try {
            SessionAnchor.Initialize(System.Guid.NewGuid().ToString("N"));
            string ownerId = ResolveOwnerId(content);
            WireTelemetrySink(ownerId);
            PopulateOwnerRegistry();
            ProfilingXmlLoader.LoadResult declared = LoadDeclaredProfiling();

            CollectorLaunchResult collector = EnsureCollectorRunning(ownerId);
            if (!collector.IsRunning) {
                Log.Error(
                    "[RimObs] No collector is running and none could be launched from any installed mod's "
                        + "Assemblies/Collector directory. Telemetry instrumentation is disabled for this session "
                        + "(no patches installed). Install the collector binary to enable profiling. (PRD §35.66)"
                );
                return;
            }

            PatchInstaller.InstallAll();
            Profiler.Enabled = true;
            GcObserverHost.Start();
            // AllocationSamplerHost is opt-in and stays inert at bootstrap. Mod authors
            // call AllocationSamplerHost.Start() themselves when they want it (PRD §35.18,
            // §11.2). It is off by default because the GC.GetTotalMemory delta heuristic
            // is a soft cost on every poll.
            StartConfigPoll(CollectorHost, UdpTelemetrySink.DefaultPort);
            LogBootstrapSummary(declared);
        }
        catch (System.Exception ex) {
            Log.Error($"[RimObs] Bootstrap failed: {ex}");
        }
    }

    private static void WireTelemetrySink(string ownerId) {
        if (s_Sink != null)
            return;
        UdpTelemetrySink sink = new(ownerId);
        sink.Start();
        Profiler.SetSink(sink);
        GcObserverHost.SetSink(sink);
        AllocationSamplerHost.SetSink(sink);
        s_Sink = sink;
    }

    private static void LogBootstrapSummary(ProfilingXmlLoader.LoadResult declared) {
        int coreCount = 0;
        int declaredCount = 0;
        int coreInstalled = 0;
        int declaredInstalled = 0;
        foreach (CatalogEntry entry in SectionCatalog.Entries) {
            if (entry.Declared) {
                declaredCount++;
                if (entry.Installed)
                    declaredInstalled++;
            }
            else {
                coreCount++;
                if (entry.Installed)
                    coreInstalled++;
            }
        }

        Log.Message(
            $"[RimObs] Loaded. Core: {coreInstalled}/{coreCount} sections installed. "
                + $"Declared: {declaredInstalled}/{declaredCount} sections from {declared.FilesLoaded}/{declared.FilesScanned} profiling.xml files. "
                + $"(unresolved={PatchInstaller.UnresolvedCount}, failed={PatchInstaller.FailedCount}, conflicts={HarmonyConflictRecorder.Count}). "
                + $"Owner registry: {OwnerRegistry.Count} mods. GcObserver: maxGen={GcObserverHost.Instance.MaxGeneration}."
        );

        foreach (string warning in declared.Warnings)
            Log.Warning($"[RimObs] profiling.xml: {warning}");

        foreach (CatalogEntry entry in SectionCatalog.Entries) {
            if (!entry.Installed && entry.ResolutionError != null)
                Log.Warning($"[RimObs] Section '{entry.Name}' unresolved: {entry.ResolutionError.Message}");
            else if (entry.InstallError != null)
                Log.Error($"[RimObs] Section '{entry.Name}' install failed: {entry.InstallError.Message}");
        }
    }

    private static void PopulateOwnerRegistry() {
        foreach (ModContentPack pack in LoadedModManager.RunningModsListForReading) {
            string packageId = pack.PackageId;
            if (string.IsNullOrEmpty(packageId))
                continue;

            foreach (System.Reflection.Assembly asm in pack.assemblies.loadedAssemblies) {
                OwnerRegistry.RegisterMod(asm, packageId);
            }
        }

        OwnerRegistry.SetLateResolver(ResolvePackageIdFromLoadedMods);
    }

    private static string? ResolvePackageIdFromLoadedMods(System.Reflection.Assembly assembly) {
        if (assembly == null)
            return null;

        System.Collections.Generic.List<ModContentPack>? mods = LoadedModManager.RunningModsListForReading;
        if (mods == null)
            return null;

        for (int i = 0; i < mods.Count; i++) {
            ModContentPack pack = mods[i];
            string packageId = pack.PackageId;
            if (string.IsNullOrEmpty(packageId))
                continue;

            System.Collections.Generic.List<System.Reflection.Assembly> assemblies = pack.assemblies.loadedAssemblies;
            for (int j = 0; j < assemblies.Count; j++) {
                if (ReferenceEquals(assemblies[j], assembly))
                    return packageId;
            }
        }

        return null;
    }

    private static ProfilingXmlLoader.LoadResult LoadDeclaredProfiling() {
        System.Collections.Generic.List<(string, string)> mods = new();
        foreach (ModContentPack pack in LoadedModManager.RunningModsListForReading) {
            string packageId = pack.PackageId;
            if (string.IsNullOrEmpty(packageId))
                continue;
            string rootDir = pack.RootDir;
            if (string.IsNullOrEmpty(rootDir))
                continue;
            mods.Add((rootDir, packageId));
        }
        return ProfilingXmlLoader.LoadFromMods(mods);
    }
}
