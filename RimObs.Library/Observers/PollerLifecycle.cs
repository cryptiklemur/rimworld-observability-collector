using System;

namespace Cryptiklemur.RimObs.Observers;

internal sealed class PollerLifecycle {
    private readonly object _lock = new();
    private readonly string _name;
    private readonly Action _tick;
    private readonly int _intervalMs;
    private PollerThread? _poller;

    public PollerLifecycle(string name, Action tick, int intervalMs) {
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

    public bool IsRunning {
        get {
            lock (_lock) {
                return _poller?.IsRunning == true;
            }
        }
    }

    public bool TryStart() {
        lock (_lock) {
            if (_poller?.IsRunning == true)
                return false;
            _poller = new PollerThread(_name, _tick, _intervalMs);
            _poller.Start();
            return true;
        }
    }

    public void Stop() {
        PollerThread? poller;
        lock (_lock) {
            poller = _poller;
            _poller = null;
        }
        poller?.Stop();
    }
}
