using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace Cryptiklemur.RimObs.Patching;

public static class PatchInstaller
{
    public const string HarmonyId = "cryptiklemur.rimobs.library";

    private static Harmony? s_Harmony;
    private static bool s_Installed;

    public static int InstalledCount { get; private set; }
    public static int FailedCount { get; private set; }
    public static int UnresolvedCount { get; private set; }

    public static void InstallAll()
    {
        if (s_Installed)
            return;
        s_Installed = true;

        SectionCatalog.RegisterCorePack();
        SectionCatalog.ResolveAll();

        s_Harmony ??= new Harmony(HarmonyId);

        HarmonyMethod transpiler = new(MethodTransplanter.TranspilerMethod)
        {
            priority = Priority.Low,
        };

        foreach (CatalogEntry entry in SectionCatalog.Entries)
        {
            if (entry.Resolved == null)
            {
                UnresolvedCount++;
                continue;
            }

            try
            {
                s_Harmony.Patch(entry.Resolved, transpiler: transpiler);
                entry.Installed = true;
                InstalledCount++;
            }
            catch (Exception ex)
            {
                entry.InstallError = ex;
                FailedCount++;
            }
        }

        HarmonyConflictRecorder.RecordConflicts(s_Harmony);
    }

    public static IReadOnlyList<CatalogEntry> InstalledEntries
    {
        get
        {
            List<CatalogEntry> list = new();
            foreach (CatalogEntry entry in SectionCatalog.Entries)
            {
                if (entry.Installed)
                    list.Add(entry);
            }
            return list;
        }
    }

    public static Harmony? Instance => s_Harmony;

    internal static Harmony EnsureHarmony(string id)
    {
        s_Harmony ??= new Harmony(id);
        return s_Harmony;
    }

    internal static void PatchSingleForTests(MethodBase target)
    {
        Harmony harmony = EnsureHarmony("cryptiklemur.rimobs.tests");
        HarmonyMethod transpiler = new(MethodTransplanter.TranspilerMethod);
        harmony.Patch(target, transpiler: transpiler);
    }

    internal static void ResetForTests()
    {
        if (s_Harmony != null)
        {
            try
            {
                s_Harmony.UnpatchAll(s_Harmony.Id);
            }
            catch
            {
                // Best-effort cleanup; ignore failures during test teardown.
            }
        }
        s_Harmony = null;
        s_Installed = false;
        InstalledCount = 0;
        FailedCount = 0;
        UnresolvedCount = 0;
    }
}
