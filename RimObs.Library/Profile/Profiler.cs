using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Cryptiklemur.RimObs.Profile;

public static class Profiler {
    public const long DisabledToken = -1L;

    public static volatile bool Enabled = true;

    internal static ISampleSink? Sink;

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
        return Stopwatch.GetTimestamp();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void StopById(int sectionId, long token) {
        if (token == DisabledToken)
            return;
        long elapsed = Stopwatch.GetTimestamp() - token;
        ISampleSink? sink = Sink;
        if (sink != null)
            sink.RecordSection(sectionId, token, elapsed);
    }
}
