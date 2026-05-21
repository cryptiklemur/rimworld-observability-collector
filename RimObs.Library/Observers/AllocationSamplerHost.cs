using System.Collections.Generic;
using System.Threading;

namespace Cryptiklemur.RimObs.Observers;

public static class AllocationSamplerHost
{
    private static readonly object s_Lock = new();
    private static AllocationSampler? s_Instance;
    private static Thread? s_PollThread;
    private static volatile bool s_Stop;
    private static readonly List<AllocationSample> s_RecentSamples = new(capacity: 64);
    private static IAllocationSink? s_Sink;
    private const int MaxRecentSamples = 64;
    private const int PollIntervalMs = 1000;
    private const long DefaultWindowMs = 60_000L;
    private static long s_WindowDurationMs = DefaultWindowMs;

    public static void SetSink(IAllocationSink? sink)
    {
        lock (s_Lock)
        {
            s_Sink = sink;
        }
    }

    public static AllocationSampler Instance
    {
        get
        {
            lock (s_Lock)
            {
                return s_Instance ??= new AllocationSampler();
            }
        }
    }

    public static bool IsRunning
    {
        get
        {
            lock (s_Lock)
            {
                return s_PollThread != null;
            }
        }
    }

    public static long WindowDurationMs
    {
        get
        {
            lock (s_Lock)
            {
                return s_WindowDurationMs;
            }
        }
        set
        {
            lock (s_Lock)
            {
                s_WindowDurationMs = value;
            }
        }
    }

    public static IReadOnlyList<AllocationSample> RecentSamples
    {
        get
        {
            lock (s_Lock)
            {
                return s_RecentSamples.ToArray();
            }
        }
    }

    public static void Start()
    {
        lock (s_Lock)
        {
            if (s_PollThread != null)
                return;

            s_Instance ??= new AllocationSampler();
            s_Stop = false;
            s_PollThread = new Thread(PollLoop)
            {
                Name = "RimObs.AllocationSampler",
                IsBackground = true,
                Priority = ThreadPriority.BelowNormal,
            };
            s_PollThread.Start();
        }
    }

    public static void Stop()
    {
        Thread? thread;
        lock (s_Lock)
        {
            thread = s_PollThread;
            s_PollThread = null;
            s_Stop = true;
        }
        thread?.Join(2000);
    }

    public static void ClearRecentSamples()
    {
        lock (s_Lock)
        {
            s_RecentSamples.Clear();
        }
    }

    public static bool PollOnce()
    {
        AllocationSampler sampler = Instance;
        long windowMs;
        lock (s_Lock)
        {
            windowMs = s_WindowDurationMs;
        }
        if (!sampler.TryPollWindow(windowMs, out AllocationSample sample))
            return false;

        IAllocationSink? sink;
        lock (s_Lock)
        {
            if (s_RecentSamples.Count >= MaxRecentSamples)
                s_RecentSamples.RemoveAt(0);
            s_RecentSamples.Add(sample);
            sink = s_Sink;
        }
        sink?.RecordAllocation(sample);
        return true;
    }

    private static void PollLoop()
    {
        while (!s_Stop)
        {
            PollOnce();
            Thread.Sleep(PollIntervalMs);
        }
    }
}
