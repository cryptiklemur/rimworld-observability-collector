using System;
using System.Diagnostics;
using Cryptiklemur.RimObs.Profile;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Cryptiklemur.RimObs.Tests;

public sealed class ProfilerOverheadTests
{
    private readonly ITestOutputHelper _out;

    public ProfilerOverheadTests(ITestOutputHelper output)
    {
        _out = output;
    }

    private sealed class CountingSink : ISampleSink
    {
        public long Count;

        public void RecordSection(int sectionId, long startTimestamp, long elapsedTicks)
        {
            Count++;
        }
    }

    [Fact]
    public void Disabled_section_short_circuits_zero_alloc()
    {
        SectionHandle handle = SectionRegistry.Register("alloc-test-disabled");
        SectionRegistry.SetActive(handle.Id, false);
        Profiler.SetSink(null);

        for (int warm = 0; warm < 50_000; warm++)
        {
            long t = Profiler.Start(handle);
            Profiler.Stop(handle, t);
        }
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100_000; i++)
        {
            long t = Profiler.Start(handle);
            Profiler.Stop(handle, t);
        }
        long after = GC.GetAllocatedBytesForCurrentThread();

        long delta = after - before;
        _out.WriteLine($"disabled 100k Start/Stop alloc delta = {delta} bytes");
        delta.Should().Be(0);
    }

    [Fact]
    public void Enabled_section_with_noop_sink_zero_alloc()
    {
        SectionHandle handle = SectionRegistry.Register("alloc-test-enabled");
        SectionRegistry.SetActive(handle.Id, true);
        CountingSink sink = new();
        Profiler.SetSink(sink);

        for (int warm = 0; warm < 50_000; warm++)
        {
            long t = Profiler.Start(handle);
            Profiler.Stop(handle, t);
        }
        sink.Count = 0;
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100_000; i++)
        {
            long t = Profiler.Start(handle);
            Profiler.Stop(handle, t);
        }
        long after = GC.GetAllocatedBytesForCurrentThread();

        long delta = after - before;
        _out.WriteLine($"enabled 100k Start/Stop alloc delta = {delta} bytes, sink count = {sink.Count}");
        delta.Should().Be(0);
        sink.Count.Should().Be(100_000);

        Profiler.SetSink(null);
    }

    [Fact]
    public void Disabled_section_overhead_under_5ns()
    {
        SectionHandle handle = SectionRegistry.Register("perf-test-disabled");
        SectionRegistry.SetActive(handle.Id, false);
        Profiler.SetSink(null);

        const int iterations = 10_000_000;

        for (int warm = 0; warm < 10_000; warm++)
        {
            long t = Profiler.Start(handle);
            Profiler.Stop(handle, t);
        }

        Stopwatch sw = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            long t = Profiler.Start(handle);
            Profiler.Stop(handle, t);
        }
        sw.Stop();

        double nsPerOp = sw.Elapsed.TotalMilliseconds * 1_000_000.0 / iterations;
        _out.WriteLine($"disabled Start/Stop = {nsPerOp:F2} ns/op ({iterations:N0} iterations in {sw.Elapsed.TotalMilliseconds:F1} ms)");
        nsPerOp.Should().BeLessThan(5.0);
    }

    [Fact]
    public void Enabled_section_overhead_under_100ns()
    {
        SectionHandle handle = SectionRegistry.Register("perf-test-enabled");
        SectionRegistry.SetActive(handle.Id, true);
        CountingSink sink = new();
        Profiler.SetSink(sink);

        const int iterations = 1_000_000;

        for (int warm = 0; warm < 10_000; warm++)
        {
            long t = Profiler.Start(handle);
            Profiler.Stop(handle, t);
        }

        Stopwatch sw = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            long t = Profiler.Start(handle);
            Profiler.Stop(handle, t);
        }
        sw.Stop();

        double nsPerOp = sw.Elapsed.TotalMilliseconds * 1_000_000.0 / iterations;
        _out.WriteLine($"enabled Start/Stop (noop sink) = {nsPerOp:F2} ns/op ({iterations:N0} iterations in {sw.Elapsed.TotalMilliseconds:F1} ms)");
        nsPerOp.Should().BeLessThan(100.0);

        Profiler.SetSink(null);
    }
}
