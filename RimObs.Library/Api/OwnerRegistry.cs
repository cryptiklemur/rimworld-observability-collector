using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Cryptiklemur.RimObs.Api;

public static class OwnerRegistry
{
    private static readonly Dictionary<Assembly, string> s_AssemblyToPackageId = new();
    private static readonly object s_Lock = new();

    public static void RegisterMod(Assembly assembly, string packageId)
    {
        if (assembly == null)
            throw new ArgumentNullException(nameof(assembly));
        if (string.IsNullOrEmpty(packageId))
            throw new ArgumentException("packageId must not be empty.", nameof(packageId));

        lock (s_Lock)
        {
            s_AssemblyToPackageId[assembly] = packageId;
        }
    }

    public static bool TryGetPackageId(Assembly assembly, [MaybeNullWhen(false)] out string packageId)
    {
        lock (s_Lock)
        {
            return s_AssemblyToPackageId.TryGetValue(assembly, out packageId);
        }
    }

    public static string ResolveOrThrow(Assembly assembly)
    {
        if (assembly == null)
            throw new ArgumentNullException(nameof(assembly));

        if (TryGetPackageId(assembly, out string? packageId))
            return packageId;

        throw new InvalidOperationException(
            $"Assembly '{assembly.GetName().Name}' is not registered with RimObs. "
                + "The library auto-registers loaded mods at startup via ModContentPack.PackageId. "
                + "For tests, call OwnerRegistry.RegisterMod(assembly, packageId) explicitly."
        );
    }

    public static int Count
    {
        get
        {
            lock (s_Lock)
            {
                return s_AssemblyToPackageId.Count;
            }
        }
    }

    public static void Clear()
    {
        lock (s_Lock)
        {
            s_AssemblyToPackageId.Clear();
        }
    }
}
