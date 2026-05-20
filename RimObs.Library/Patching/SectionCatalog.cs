using System;
using System.Collections.Generic;
using System.Reflection;
using Cryptiklemur.RimObs.Profile;

namespace Cryptiklemur.RimObs.Patching;

public static class SectionCatalog
{
    private static readonly List<CatalogEntry> s_Entries = new();
    private static readonly Dictionary<MethodBase, int> s_MethodToSectionId = new();
    private static readonly object s_Lock = new();
    private static bool s_CorePackRegistered;

    public static IReadOnlyList<CatalogEntry> Entries
    {
        get
        {
            lock (s_Lock)
            {
                return s_Entries.ToArray();
            }
        }
    }

    public static void RegisterCorePack()
    {
        lock (s_Lock)
        {
            if (s_CorePackRegistered)
                return;
            s_CorePackRegistered = true;

            Register("Verse.TickManager.DoSingleTick", "Verse.TickManager", "DoSingleTick", null);
            Register("Verse.Map.MapPreTick", "Verse.Map", "MapPreTick", null);
            Register("Verse.Map.MapPostTick", "Verse.Map", "MapPostTick", null);
            Register("RimWorld.Planet.World.WorldTick", "RimWorld.Planet.World", "WorldTick", null);
            Register("Verse.GameComponentUtility.GameComponentTick", "Verse.GameComponentUtility", "GameComponentTick", null);
            Register("RimWorld.Storyteller.StorytellerTick", "RimWorld.Storyteller", "StorytellerTick", null);
            Register("RimWorld.History.HistoryTick", "RimWorld.History", "HistoryTick", null);
            Register("RimWorld.FilthMonitor.FilthMonitorTick", "RimWorld.FilthMonitor", "FilthMonitorTick", null);
            Register("RimWorld.DateNotifier.DateNotifierTick", "RimWorld.DateNotifier", "DateNotifierTick", null);
            Register("Verse.AI.Pawn_JobTracker.DetermineNextJob", "Verse.AI.Pawn_JobTracker", "DetermineNextJob", null);
            Register("Verse.AI.PathFinder.FindPathNow", "Verse.PathFinder", "FindPathNow", null);
            Register("Verse.AI.Pawn_PathFollower.PatherTick", "Verse.AI.Pawn_PathFollower", "PatherTick", null);
            Register("RimWorld.MapInterface.MapInterfaceUpdate", "RimWorld.MapInterface", "MapInterfaceUpdate", null);
            Register("Verse.UIRoot.UIRootOnGUI", "Verse.UIRoot", "UIRootOnGUI", null);
            Register("Verse.WindowStack.WindowStackOnGUI", "Verse.WindowStack", "WindowStackOnGUI", null);
            Register("RimWorld.Alert.Recalculate", "RimWorld.Alert", "Recalculate", null);
            Register("RimWorld.Pawn_NeedsTracker.NeedsTrackerTickInterval", "RimWorld.Pawn_NeedsTracker", "NeedsTrackerTickInterval", null);
            Register("RimWorld.Planet.WorldPawns.WorldPawnsTick", "RimWorld.Planet.WorldPawns", "WorldPawnsTick", null);
            Register("Verse.Root_Play.Update", "Verse.Root_Play", "Update", null);
        }
    }

    public static CatalogEntry Register(string name, string typeName, string methodName, string[]? paramTypeNames)
    {
        lock (s_Lock)
        {
            CatalogEntry entry = new(name, typeName, methodName, paramTypeNames);
            s_Entries.Add(entry);
            return entry;
        }
    }

    public static CatalogEntry RegisterDirect(string name, MethodBase method)
    {
        if (method == null)
            throw new ArgumentNullException(nameof(method));

        lock (s_Lock)
        {
            CatalogEntry entry = new(name, method.DeclaringType?.FullName ?? "?", method.Name, null);
            entry.Resolved = method;
            SectionHandle handle = SectionRegistry.Register(name);
            entry.SectionId = handle.Id;
            SectionRegistry.SetActive(handle.Id, true);
            s_Entries.Add(entry);
            s_MethodToSectionId[method] = handle.Id;
            return entry;
        }
    }

    public static void ResolveAll()
    {
        lock (s_Lock)
        {
            foreach (CatalogEntry entry in s_Entries)
            {
                if (entry.Resolved != null)
                    continue;

                try
                {
                    Type? type = FindType(entry.TypeName);
                    if (type == null)
                    {
                        entry.ResolutionError = "type not found";
                        continue;
                    }

                    MethodInfo? method = ResolveMethod(type, entry.MethodName, entry.ParamTypeNames);
                    if (method == null)
                    {
                        entry.ResolutionError = "method not found";
                        continue;
                    }

                    SectionHandle handle = SectionRegistry.Register(entry.Name);
                    entry.SectionId = handle.Id;
                    entry.Resolved = method;
                    SectionRegistry.SetActive(handle.Id, true);
                    s_MethodToSectionId[method] = handle.Id;
                }
                catch (Exception ex)
                {
                    entry.ResolutionError = ex.Message;
                }
            }
        }
    }

    public static bool TryGetSectionId(MethodBase method, out int sectionId)
    {
        lock (s_Lock)
        {
            return s_MethodToSectionId.TryGetValue(method, out sectionId);
        }
    }

    private static Type? FindType(string fullName)
    {
        foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type? t = asm.GetType(fullName, throwOnError: false);
            if (t != null)
                return t;
        }
        return null;
    }

    private static MethodInfo? ResolveMethod(Type type, string methodName, string[]? paramTypeNames)
    {
        const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        MethodInfo[] candidates = type.GetMethods(Flags);
        List<MethodInfo> byName = new();
        for (int i = 0; i < candidates.Length; i++)
        {
            if (candidates[i].Name == methodName)
                byName.Add(candidates[i]);
        }

        if (byName.Count == 0)
            return null;

        if (paramTypeNames == null || paramTypeNames.Length == 0)
        {
            if (byName.Count == 1)
                return byName[0];

            MethodInfo best = byName[0];
            int bestArity = best.GetParameters().Length;
            for (int i = 1; i < byName.Count; i++)
            {
                int arity = byName[i].GetParameters().Length;
                if (arity > bestArity)
                {
                    best = byName[i];
                    bestArity = arity;
                }
            }
            return best;
        }

        foreach (MethodInfo m in byName)
        {
            ParameterInfo[] ps = m.GetParameters();
            if (ps.Length != paramTypeNames.Length)
                continue;

            bool match = true;
            for (int i = 0; i < ps.Length; i++)
            {
                if (!NameMatches(ps[i].ParameterType, paramTypeNames[i]))
                {
                    match = false;
                    break;
                }
            }
            if (match)
                return m;
        }
        return null;
    }

    private static bool NameMatches(Type t, string name)
    {
        if (t.FullName == name)
            return true;
        if (t.Name == name)
            return true;
        if (t.IsGenericType)
        {
            string definition = t.GetGenericTypeDefinition().FullName ?? "";
            if (definition == name)
                return true;
        }
        return false;
    }
}
