namespace Cryptiklemur.RimObs.Observers;

public readonly struct GcEventSample
{
    public GcEventSample(byte generation, byte pauseType, long heapBefore, long heapAfter, long durationMicros, long tick, long allocationRateBytesPerMinute)
    {
        Generation = generation;
        PauseType = pauseType;
        HeapBefore = heapBefore;
        HeapAfter = heapAfter;
        DurationMicros = durationMicros;
        Tick = tick;
        AllocationRateBytesPerMinute = allocationRateBytesPerMinute;
    }

    public byte Generation { get; }
    public byte PauseType { get; }
    public long HeapBefore { get; }
    public long HeapAfter { get; }
    public long DurationMicros { get; }
    public long Tick { get; }
    public long AllocationRateBytesPerMinute { get; }
}

public static class GcPauseType
{
    public const byte Foreground = 0;
    public const byte Background = 1;
}
