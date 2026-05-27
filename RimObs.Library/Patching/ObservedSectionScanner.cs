using System;
using System.Collections.Generic;
using System.Reflection;
using Cryptiklemur.RimObs.Api;

namespace Cryptiklemur.RimObs.Patching;

internal static class ObservedSectionScanner {
    public sealed class ScanResult {
        public int AssembliesScanned;
        public int AttributesFound;
        public int Registered;
        public int SkippedDuplicate = 0;
        public int SkippedUnsupported = 0;
        public int Failed = 0;
        public List<string> Warnings { get; } = new();
    }

    public static ScanResult Scan(IEnumerable<(string packageId, IReadOnlyList<Assembly> assemblies)> mods) {
        if (mods == null)
            throw new ArgumentNullException(nameof(mods));

        ScanResult result = new();
        foreach ((string packageId, IReadOnlyList<Assembly> assemblies) in mods) {
            if (string.IsNullOrEmpty(packageId) || assemblies == null)
                continue;

            foreach (Assembly assembly in assemblies) {
                if (assembly == null)
                    continue;
                if (assembly.GetName().Name == "RimObs")
                    continue;

                result.AssembliesScanned++;
                Type[] types = assembly.GetTypes();
                foreach (Type type in types) {
                    foreach (MethodInfo method in type.GetMethods(
                        BindingFlags.Public | BindingFlags.NonPublic |
                        BindingFlags.Instance | BindingFlags.Static |
                        BindingFlags.DeclaredOnly)) {
                        ObservedSectionAttribute? attr = method.GetCustomAttribute<ObservedSectionAttribute>();
                        if (attr == null)
                            continue;

                        result.AttributesFound++;

                        if (!IsPatchable(method, out string reason)) {
                            result.SkippedUnsupported++;
                            result.Warnings.Add(
                                $"[{packageId}] {type.FullName}.{method.Name}: skipped ({reason})");
                            continue;
                        }

                        if (SectionCatalog.TryGetSectionId(method, out _)) {
                            result.SkippedDuplicate++;
                            result.Warnings.Add(
                                $"[{packageId}] {type.FullName}.{method.Name}: skipped (already registered by core or XML)");
                            continue;
                        }

                        string computedName = attr.Name ?? $"{type.FullName}.{method.Name}";
                        string prefixed = $"{packageId}.{computedName}";
                        SectionCatalog.RegisterDirect(prefixed, method, subsystem: attr.Subsystem);
                        PatchInstaller.PatchAttributeMethod(method);
                        result.Registered++;
                    }
                }
            }
        }
        return result;
    }

    private static bool IsPatchable(MethodInfo method, out string reason) {
        if (method.IsAbstract) {
            reason = "method is abstract";
            return false;
        }
        if (method.ContainsGenericParameters) {
            reason = "method has unresolved generic parameters";
            return false;
        }
        if (method.GetCustomAttribute<System.Runtime.CompilerServices.AsyncStateMachineAttribute>() != null) {
            reason = "async methods are not supported in v1";
            return false;
        }
        if (method.GetCustomAttribute<System.Runtime.CompilerServices.IteratorStateMachineAttribute>() != null) {
            reason = "iterator (yield) methods are not supported in v1";
            return false;
        }
        reason = string.Empty;
        return true;
    }
}
