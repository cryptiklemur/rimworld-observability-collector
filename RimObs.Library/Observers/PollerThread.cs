using System;
using System.Threading;

namespace Cryptiklemur.RimObs.Observers;

internal sealed class PollerThread
{
    private readonly object _lock = new();
    private readonly string _name;
    private readonly Action _tick;
    private readonly int _intervalMs;
    private Thread? _thread;
    private volatile bool _stop;

    public PollerThread(string name, Action tick, int intervalMs)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Thread name must not be empty.", nameof(name));
        if (tick == null)
            throw new ArgumentNullException(nameof(tick));
        if (intervalMs <= 0)
            throw new ArgumentOutOfRangeException(nameof(intervalMs), "Poll interval must be positive.");
        _name = name;
        _tick = tick;
        _intervalMs = intervalMs;
    }

    public bool IsRunning
    {
        get
        {
            lock (_lock)
            {
                return _thread != null;
            }
        }
    }

    public void Start()
    {
        lock (_lock)
        {
            if (_thread != null)
                return;
            _stop = false;
            _thread = new Thread(Loop)
            {
                Name = _name,
                IsBackground = true,
                Priority = ThreadPriority.BelowNormal,
            };
            _thread.Start();
        }
    }

    public void Stop(int joinTimeoutMs = 2000)
    {
        Thread? thread;
        lock (_lock)
        {
            thread = _thread;
            _thread = null;
            _stop = true;
        }
        thread?.Join(joinTimeoutMs);
    }

    private void Loop()
    {
        while (!_stop)
        {
            _tick();
            Thread.Sleep(_intervalMs);
        }
    }
}
