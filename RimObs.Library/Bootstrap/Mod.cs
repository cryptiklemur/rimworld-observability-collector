using Cryptiklemur.RimObs.Api;
using Cryptiklemur.RimObs.Config;
using Cryptiklemur.RimObs.Observers;
using Cryptiklemur.RimObs.Patching;
using Cryptiklemur.RimObs.Transport;
using Verse;

namespace Cryptiklemur.RimObs.Bootstrap;

public sealed class RimObsMod : Mod
{
    public RimObsMod(ModContentPack content) : base(content)
    {
        try
        {
            SessionAnchor.Initialize(System.Guid.NewGuid().ToString("N"));
            PopulateOwnerRegistry();
            SectionCatalog.RegisterCorePack();
            ProfilingXmlLoader.LoadResult declared = LoadDeclaredProfiling();
            PatchInstaller.InstallAll();
            GcObserverHost.Start();

            int coreCount = 0;
            int declaredCount = 0;
            int coreInstalled = 0;
            int declaredInstalled = 0;
            foreach (CatalogEntry entry in SectionCatalog.Entries)
            {
                if (entry.Declared)
                {
                    declaredCount++;
                    if (entry.Installed)
                        declaredInstalled++;
                }
                else
                {
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

            foreach (CatalogEntry entry in SectionCatalog.Entries)
            {
                if (!entry.Installed && entry.ResolutionError != null)
                    Log.Warning($"[RimObs] Section '{entry.Name}' unresolved: {entry.ResolutionError}");
                else if (entry.InstallError != null)
                    Log.Error($"[RimObs] Section '{entry.Name}' install failed: {entry.InstallError.Message}");
            }
        }
        catch (System.Exception ex)
        {
            Log.Error($"[RimObs] Bootstrap failed: {ex}");
        }
    }

    private static void PopulateOwnerRegistry()
    {
        foreach (ModContentPack pack in LoadedModManager.RunningModsListForReading)
        {
            string packageId = pack.PackageId;
            if (string.IsNullOrEmpty(packageId))
                continue;

            foreach (System.Reflection.Assembly asm in pack.assemblies.loadedAssemblies)
            {
                OwnerRegistry.RegisterMod(asm, packageId);
            }
        }
    }

    private static ProfilingXmlLoader.LoadResult LoadDeclaredProfiling()
    {
        System.Collections.Generic.List<(string, string)> mods = new();
        foreach (ModContentPack pack in LoadedModManager.RunningModsListForReading)
        {
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
