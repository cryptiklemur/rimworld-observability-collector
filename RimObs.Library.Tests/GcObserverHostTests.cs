using System;
using Cryptiklemur.RimObs.Observers;
using FluentAssertions;
using Xunit;

namespace Cryptiklemur.RimObs.Tests;

public sealed class GcObserverHostTests : IDisposable
{
    public GcObserverHostTests()
    {
        GcObserverHost.Stop();
        GcObserverHost.ClearRecentEvents();
    }

    public void Dispose()
    {
        GcObserverHost.Stop();
        GcObserverHost.ClearRecentEvents();
    }

    [Fact]
    public void Instance_returns_singleton()
    {
        GcObserver a = GcObserverHost.Instance;
        GcObserver b = GcObserverHost.Instance;

        a.Should().BeSameAs(b);
    }

    [Fact]
    public void PollOnce_appends_to_recent_events_when_collection_occurs()
    {
        GcObserverHost.PollOnce(0);

        GC.Collect(0, GCCollectionMode.Forced, blocking: true);

        bool detected = GcObserverHost.PollOnce(42);

        detected.Should().BeTrue();
        var events = GcObserverHost.RecentEvents;
        events.Should().NotBeEmpty();
        events[events.Count - 1].Tick.Should().Be(42);
    }

    [Fact]
    public void Start_and_Stop_round_trip()
    {
        GcObserverHost.IsRunning.Should().BeFalse();

        GcObserverHost.Start();
        GcObserverHost.IsRunning.Should().BeTrue();

        GcObserverHost.Stop();
        GcObserverHost.IsRunning.Should().BeFalse();
    }

    [Fact]
    public void Start_is_idempotent()
    {
        GcObserverHost.Start();
        GcObserverHost.Start();

        GcObserverHost.IsRunning.Should().BeTrue();
    }

    [Fact]
    public void ClearRecentEvents_empties_buffer()
    {
        GcObserverHost.PollOnce(0);
        GC.Collect(0, GCCollectionMode.Forced, blocking: true);
        GcObserverHost.PollOnce(1);

        GcObserverHost.ClearRecentEvents();

        GcObserverHost.RecentEvents.Should().BeEmpty();
    }
}
