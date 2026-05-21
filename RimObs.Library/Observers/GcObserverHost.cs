using System.Collections.Generic;
using System.Threading;

namespace Cryptiklemur.RimObs.Observers;

public static class GcObserverHost
{
    private static readonly object s_Lock = new();
    private static GcObserver? s_Instance;
    private static Thread? s_PollThread;
    private static volatile bool s_Stop;
    private static readonly List<GcEventSample> s_RecentSamples = new(capacity: 64);
    private static IGcEventSink? s_Sink;
    private const int MaxRecentSamples = 64;
    private const int PollIntervalMs = 1000;

    public static void SetSink(IGcEventSink? sink)
    {
        lock (s_Lock)
        {
            s_Sink = sink;
        }
    }

    public static GcObserver Instance
    {
        get
        {
            lock (s_Lock)
            {
                return s_Instance ??= new GcObserver();
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

    public static IReadOnlyList<GcEventSample> RecentSamples
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

            s_Instance ??= new GcObserver();
            s_Stop = false;
            s_PollThread = new Thread(PollLoop)
            {
                Name = "RimObs.GcObserver",
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

    public static bool PollOnce(long currentTick)
    {
        GcObserver observer = Instance;
        if (!observer.TryPoll(currentTick, out GcEventSample sample))
            return false;

        IGcEventSink? sink;
        lock (s_Lock)
        {
            if (s_RecentSamples.Count >= MaxRecentSamples)
                s_RecentSamples.RemoveAt(0);
            s_RecentSamples.Add(sample);
            sink = s_Sink;
        }
        sink?.RecordGcEvent(sample);
        return true;
    }

    private static void PollLoop()
    {
        long tick = 0;
        while (!s_Stop)
        {
            PollOnce(tick++);
            Thread.Sleep(PollIntervalMs);
        }
    }
}
