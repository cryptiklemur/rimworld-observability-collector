using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Cryptiklemur.RimObs.Library.Control;

internal static class MethodResolver {
    private static readonly string[] s_BlocklistedNamespaces = [
        "Cryptiklemur.RimObs.Library.Profile.",
        "Cryptiklemur.RimObs.Library.Transport.",
        "Cryptiklemur.RimObs.Library.Patching.",
        "Cryptiklemur.RimObs.Library.Api.",
        "Cryptiklemur.RimObs.Library.Metrics.",
        "Cryptiklemur.RimObs.Library.Observers.",
        "Cryptiklemur.RimObs.Library.Config.",
        "Cryptiklemur.RimObs.Library.Session.",
        "Cryptiklemur.RimObs.Library.Control.",
        "Cryptiklemur.RimObs.Wire.",
        "HarmonyLib.",
        "MonoMod.",
    ];

    public static MethodResolveResult Resolve(string typeFullName, string methodName, string[] paramTypeFullNames) {
        for (int i = 0; i < s_BlocklistedNamespaces.Length; i++) {
            if (typeFullName.StartsWith(s_BlocklistedNamespaces[i], StringComparison.Ordinal))
                return MethodResolveResult.Refuse("blocklist: cannot instrument RimObs internals");
        }

        Type? type = null;
        Assembly[] allAssemblies = AppDomain.CurrentDomain.GetAssemblies();
        for (int i = 0; i < allAssemblies.Length; i++) {
            type = allAssemblies[i].GetType(typeFullName, throwOnError: false, ignoreCase: false);
            if (type is not null)
                break;
        }
        if (type is null)
            return MethodResolveResult.Refuse($"type not found: {typeFullName}");

        MethodInfo[] candidates = type.GetMethods(
            BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.Instance | BindingFlags.Static |
            BindingFlags.DeclaredOnly);

        List<MethodInfo> byName = new List<MethodInfo>();
        for (int i = 0; i < candidates.Length; i++) {
            if (string.Equals(candidates[i].Name, methodName, StringComparison.Ordinal))
                byName.Add(candidates[i]);
        }
        if (byName.Count == 0)
            return MethodResolveResult.Refuse($"method not found: {typeFullName}.{methodName}");

        MethodInfo target;
        if (byName.Count == 1) {
            target = byName[0];
        }
        else {
            MethodInfo? narrowed = null;
            int narrowCount = 0;
            for (int i = 0; i < byName.Count; i++) {
                if (ParametersMatch(byName[i], paramTypeFullNames)) {
                    narrowed = byName[i];
                    narrowCount++;
                }
            }
            if (narrowCount != 1)
                return MethodResolveResult.Refuse("ambiguous overload; specify exact param types");
            target = narrowed!;
        }

        if (target.IsAbstract)
            return MethodResolveResult.Refuse("abstract method has no IL body");
        if ((target.GetMethodImplementationFlags() & MethodImplAttributes.InternalCall) != 0)
            return MethodResolveResult.Refuse("extern method has no IL body");
        if (target.IsGenericMethodDefinition || target.ContainsGenericParameters)
            return MethodResolveResult.Refuse("open generic methods are not supported");

        return MethodResolveResult.Accept(target, BuildSignature(target));
    }

    public static string BuildSignature(MethodInfo method) {
        StringBuilder sb = new StringBuilder();
        sb.Append(method.DeclaringType?.FullName ?? "?");
        sb.Append(':');
        sb.Append(method.Name);
        sb.Append('(');
        ParameterInfo[] ps = method.GetParameters();
        for (int i = 0; i < ps.Length; i++) {
            if (i > 0)
                sb.Append(", ");
            sb.Append(ps[i].ParameterType.Name);
        }
        sb.Append(')');
        return sb.ToString();
    }

    private static bool ParametersMatch(MethodInfo method, string[] paramTypeFullNames) {
        ParameterInfo[] ps = method.GetParameters();
        if (ps.Length != paramTypeFullNames.Length)
            return false;
        for (int i = 0; i < ps.Length; i++) {
            string? full = ps[i].ParameterType.FullName;
            if (!string.Equals(full, paramTypeFullNames[i], StringComparison.Ordinal))
                return false;
        }
        return true;
    }
}
