using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Cryptiklemur.RimObs.Profile;

public static class Profiler {
    public const long DisabledToken = -1L;

    internal const int MaxStackDepth = 64;
    public const int NoParent = -1;

    public static volatile bool Enabled = true;

    internal static ISampleSink? Sink;

    [ThreadStatic]
    private static int[]? s_stack;

    [ThreadStatic]
    private static int s_depth;

    internal static void SetSink(ISampleSink? sink) => Sink = sink;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long Start(SectionHandle handle) => StartById(handle.Id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Stop(SectionHandle handle, long token) => StopById(handle.Id, token);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long StartById(int sectionId) {
        if (!Enabled)
            return DisabledToken;
        if ((uint)sectionId >= (uint)SectionRegistry.MaxSections)
            return DisabledToken;
        if (!SectionRegistry.s_Active[sectionId])
            return DisabledToken;

        int[] stack = s_stack ??= new int[MaxStackDepth];
        int depth = s_depth;
        if (depth < MaxStackDepth)
            stack[depth] = sectionId;
        s_depth = depth + 1;

        return Stopwatch.GetTimestamp();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void StopById(int sectionId, long token) {
        if (token == DisabledToken)
            return;

        long elapsed = Stopwatch.GetTimestamp() - token;

        int depth = s_depth;
        int parentId = NoParent;
        if (depth > 0) {
            depth--;
            s_depth = depth;
            int[]? stack = s_stack;
            if (stack != null && depth > 0 && depth - 1 < MaxStackDepth)
                parentId = stack[depth - 1];
        }

        ISampleSink? sink = Sink;
        if (sink != null)
            sink.RecordSection(sectionId, parentId, token, elapsed);
    }
}
