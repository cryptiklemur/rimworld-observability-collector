using System.Collections.Generic;
using HarmonyLib;

namespace Cryptiklemur.RimObs.Patching;

internal static class HarmonyConflictRecorder {
    private static readonly List<HarmonyConflict> s_Conflicts = new();
    private static readonly object s_Lock = new();

    public static IReadOnlyList<HarmonyConflict> Conflicts {
        get {
            lock (s_Lock) {
                return s_Conflicts.ToArray();
            }
        }
    }

    public static int Count {
        get {
            lock (s_Lock) {
                return s_Conflicts.Count;
            }
        }
    }

    public static void Clear() {
        lock (s_Lock) {
            s_Conflicts.Clear();
        }
    }

    public static void RecordConflicts(Harmony harmony) {
        lock (s_Lock) {
            foreach (CatalogEntry entry in SectionCatalog.Entries) {
                if (entry.Resolved == null)
                    continue;

                Patches? patches = Harmony.GetPatchInfo(entry.Resolved);
                if (patches == null)
                    continue;

                RecordList(entry, patches.Prefixes, HarmonyPatchType.Prefix, harmony.Id);
                RecordList(entry, patches.Postfixes, HarmonyPatchType.Postfix, harmony.Id);
                RecordList(entry, patches.Transpilers, HarmonyPatchType.Transpiler, harmony.Id);
                RecordList(entry, patches.Finalizers, HarmonyPatchType.Finalizer, harmony.Id);
            }
        }
    }

    private static void RecordList(CatalogEntry entry, IReadOnlyCollection<Patch> patches, HarmonyPatchType kind, string ownId) {
        if (patches == null)
            return;

        foreach (Patch patch in patches) {
            if (patch.owner == ownId)
                continue;

            string target = entry.Resolved!.DeclaringType?.FullName + "." + entry.Resolved.Name;
            string patchMethodName = patch.PatchMethod.DeclaringType?.FullName + "." + patch.PatchMethod.Name;
            s_Conflicts.Add(
                new HarmonyConflict(
                    sectionName: entry.Name,
                    targetMethod: target,
                    otherOwner: patch.owner,
                    patchType: kind,
                    priority: patch.priority,
                    patchMethod: patchMethodName
                )
            );
        }
    }
}
