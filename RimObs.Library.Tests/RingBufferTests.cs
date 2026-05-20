using Cryptiklemur.RimObs.Transport;
using FluentAssertions;
using Xunit;

namespace Cryptiklemur.RimObs.Tests;

public sealed class RingBufferTests
{
    [Fact]
    public void Write_then_drain_returns_same_values_in_order()
    {
        SampleRingBuffer ring = new(16);
        for (int i = 0; i < 10; i++)
        {
            ring.TryWrite(i, i * 100L, i * 1000L).Should().BeTrue();
        }

        int[] ids = new int[16];
        long[] starts = new long[16];
        long[] elapsed = new long[16];
        int n = ring.Drain(ids, starts, elapsed, 16);

        n.Should().Be(10);
        for (int i = 0; i < 10; i++)
        {
            ids[i].Should().Be(i);
            starts[i].Should().Be(i * 100L);
            elapsed[i].Should().Be(i * 1000L);
        }
    }

    [Fact]
    public void Drops_when_full_and_increments_dropped_counter()
    {
        SampleRingBuffer ring = new(4);
        for (int i = 0; i < 4; i++)
            ring.TryWrite(i, 0, 0).Should().BeTrue();

        ring.TryWrite(99, 0, 0).Should().BeFalse();
        ring.Dropped.Should().Be(1);
    }

    [Fact]
    public void Multiple_drain_cycles_progress_read_pointer()
    {
        SampleRingBuffer ring = new(8);
        int[] ids = new int[8];
        long[] starts = new long[8];
        long[] elapsed = new long[8];

        ring.TryWrite(1, 0, 0);
        ring.TryWrite(2, 0, 0);
        ring.Drain(ids, starts, elapsed, 8).Should().Be(2);

        ring.TryWrite(3, 0, 0);
        ring.TryWrite(4, 0, 0);
        ring.Drain(ids, starts, elapsed, 8).Should().Be(2);
        ids[0].Should().Be(3);
        ids[1].Should().Be(4);
    }
}
