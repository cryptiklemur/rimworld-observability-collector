using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
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
    private const string FrameworkOwnerId = "CryptikLemur.RimObs";
    private const string CollectorHost = "127.0.0.1";
    // Cold-launching a fresh self-contained collector (extract + JIT) takes longer than
    // pinging an already-running daemon did, so allow generous readiness headroom.
    private static readonly TimeSpan s_LaunchTimeout = TimeSpan.FromSeconds(10);
    private static UdpTelemetrySink? s_Sink;
    private static CollectorConfigClient? s_ConfigClient;

    private static string ResolveOwnerId(ModContentPack content) =>
        string.IsNullOrEmpty(content?.PackageId) ? FrameworkOwnerId : content!.PackageId;

    private static CollectorLaunchResult EnsureCollectorRunning(string ownerId, int port, int parentPid) {
        List<CollectorCandidate> candidates = CollectCandidates();
        return CollectorLauncher.EnsureRunning(
            candidates,
            CollectorHost,
            port,
            ownerId,
            CollectorLauncher.DefaultProbeTimeout,
            s_LaunchTimeout,
            parentPid: parentPid
        );
    }

    private static List<CollectorCandidate> CollectCandidates() {
        List<CollectorCandidate> candidates = new();
        foreach (ModContentPack pack in LoadedModManager.RunningModsListForReading) {
            string rootDir = pack.RootDir;
            if (string.IsNullOrEmpty(rootDir))
                continue;
            string collectorDir = Path.Combine(rootDir, CollectorScanner.CollectorDirName);
            CollectorScanner.ReadCandidates(collectorDir, candidates);
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
            SessionAnchor.Initialize(Guid.NewGuid().ToString("N"));
            string ownerId = ResolveOwnerId(content);

            int port = EphemeralPort.Allocate();
            int parentPid = Process.GetCurrentProcess().Id;

            WireTelemetrySink(ownerId, port);
            PopulateOwnerRegistry();
            ProfilingXmlLoader.LoadResult declared = LoadDeclaredProfiling();

            CollectorLaunchResult collector = EnsureCollectorRunning(ownerId, port, parentPid);
            if (!collector.IsRunning) {
                Log.Error(
                    "[RimObs] No collector is running and none could be launched from any installed mod's "
                        + "Collector directory. Telemetry instrumentation is disabled for this session "
                        + "(no patches installed). Install the collector binary to enable profiling. (PRD §35.66)"
                );
                return;
            }

            PatchInstaller.InstallAll();
            FrameTickPatches.InstallAll();
            s_Sink?.SetPatchConflicts(HarmonyConflictRecorder.BuildBatch());
            Profiler.Enabled = true;
            GcObserverHost.Start();
            TpsFpsObserverHost.Start();
            // AllocationSamplerHost is opt-in and stays inert at bootstrap. Mod authors
            // call AllocationSamplerHost.Start() themselves when they want it (PRD §35.18,
            // §11.2). It is off by default because the GC.GetTotalMemory delta heuristic
            // is a soft cost on every poll.
            StartConfigPoll(CollectorHost, port);
            LogBootstrapSummary(declared);
        }
        catch (Exception ex) {
            Log.Error($"[RimObs] Bootstrap failed: {ex}");
        }
    }

    private static void WireTelemetrySink(string ownerId, int port) {
        if (s_Sink != null)
            return;
        UdpTelemetrySink sink = new(ownerId, port);
        sink.Start();
        Profiler.SetSink(sink);
        GcObserverHost.SetSink(sink);
        AllocationSamplerHost.SetSink(sink);
        TpsFpsObserverHost.SetSink(sink);
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

            foreach (Assembly asm in pack.assemblies.loadedAssemblies) {
                OwnerRegistry.RegisterMod(asm, packageId);
            }
        }

        OwnerRegistry.SetLateResolver(ResolvePackageIdFromLoadedMods);
    }

    private static string? ResolvePackageIdFromLoadedMods(Assembly assembly) {
        if (assembly == null)
            return null;

        List<ModContentPack>? mods = LoadedModManager.RunningModsListForReading;
        if (mods == null)
            return null;

        for (int i = 0; i < mods.Count; i++) {
            ModContentPack pack = mods[i];
            string packageId = pack.PackageId;
            if (string.IsNullOrEmpty(packageId))
                continue;

            List<Assembly> assemblies = pack.assemblies.loadedAssemblies;
            for (int j = 0; j < assemblies.Count; j++) {
                if (ReferenceEquals(assemblies[j], assembly))
                    return packageId;
            }
        }

        return null;
    }

    private static ProfilingXmlLoader.LoadResult LoadDeclaredProfiling() {
        List<(string, string)> mods = new();
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
