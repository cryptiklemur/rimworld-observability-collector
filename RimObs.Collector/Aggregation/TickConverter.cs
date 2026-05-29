using System.Diagnostics;
using Cryptiklemur.RimObs.Wire;

namespace Cryptiklemur.RimObs.Collector.Aggregation;

internal static class TickConverter {
    public static double NsPerTick(SessionMeta? meta) => NsPerTick(meta?.StopwatchFrequency ?? 0L);

    public static double NsPerTick(long stopwatchFrequency) {
        long freq = stopwatchFrequency > 0 ? stopwatchFrequency : Stopwatch.Frequency;
        return 1_000_000_000.0 / freq;
    }
}
