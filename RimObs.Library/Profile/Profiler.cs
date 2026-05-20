using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Cryptiklemur.RimObs.Profile;

public static class Profiler
{
    public const long DisabledToken = -1L;

    public static volatile bool Enabled = true;

    internal static ISampleSink? Sink;

    public static void SetSink(ISampleSink? sink) => Sink = sink;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long Start(SectionHandle handle)
    {
        if (!Enabled)
            return DisabledToken;

        int id = handle.Id;
        if ((uint)id >= (uint)SectionRegistry.MaxSections)
            return DisabledToken;
        if (!SectionRegistry.s_Active[id])
            return DisabledToken;

        return Stopwatch.GetTimestamp();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Stop(SectionHandle handle, long token)
    {
        if (token == DisabledToken)
            return;

        long elapsed = Stopwatch.GetTimestamp() - token;
        ISampleSink? sink = Sink;
        if (sink != null)
            sink.RecordSection(handle.Id, token, elapsed);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long StartById(int sectionId)
    {
        if (!Enabled)
            return DisabledToken;
        if ((uint)sectionId >= (uint)SectionRegistry.MaxSections)
            return DisabledToken;
        if (!SectionRegistry.s_Active[sectionId])
            return DisabledToken;
        return Stopwatch.GetTimestamp();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void StopById(int sectionId, long token)
    {
        if (token == DisabledToken)
            return;
        long elapsed = Stopwatch.GetTimestamp() - token;
        ISampleSink? sink = Sink;
        if (sink != null)
            sink.RecordSection(sectionId, token, elapsed);
    }
}
