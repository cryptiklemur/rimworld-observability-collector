using Cryptiklemur.RimObs.Collector.Aggregation;
using Cryptiklemur.RimObs.Wire;
using FluentAssertions;
using Xunit;

namespace Cryptiklemur.RimObs.Collector.Tests;

public sealed class SessionAggregatorTests
{
    [Fact]
    public void OnBatchReceived_increments_counters()
    {
        SessionAggregator agg = new();

        agg.OnBatchReceived(128);
        agg.OnBatchReceived(64);

        agg.TotalBatches.Should().Be(2);
        agg.TotalBytes.Should().Be(192);
        agg.LastBatchUtc.Should().NotBe(default);
    }

    [Fact]
    public void OnSessionMeta_stores_meta()
    {
        SessionAggregator agg = new();
        SessionMeta meta = new()
        {
            SessionId = "test-session",
            StartedUtcTicks = 123456,
            StopwatchFrequency = 10_000_000,
            AnchorTimestamp = 999,
            LibraryVersion = "1.2.3",
            GameVersion = "1.6",
        };

        agg.OnSessionMeta(meta);

        agg.Meta.Should().NotBeNull();
        agg.Meta!.SessionId.Should().Be("test-session");
        agg.Meta.LibraryVersion.Should().Be("1.2.3");
    }

    [Fact]
    public void OnSectionRegistrations_records_name_and_id()
    {
        SessionAggregator agg = new();
        SectionRegistrationsBatch batch = new()
        {
            SectionIds = [1, 2, 3],
            Names = ["alpha", "bravo", "charlie"],
        };

        agg.OnSectionRegistrations(batch);

        agg.SectionCount.Should().Be(3);
        SectionStats[] sections = agg.Sections.ToArray();
        sections.Should().Contain(s => s.SectionId == 2 && s.Name == "bravo");
    }

    [Fact]
    public void OnSectionRegistrations_tolerates_length_mismatch_by_truncating()
    {
        SessionAggregator agg = new();
        SectionRegistrationsBatch batch = new()
        {
            SectionIds = [1, 2, 3],
            Names = ["alpha", "bravo"],
        };

        agg.OnSectionRegistrations(batch);

        agg.SectionCount.Should().Be(2);
    }

    [Fact]
    public void OnSectionBatch_accumulates_sample_stats()
    {
        SessionAggregator agg = new();
        SectionBatch batch = new()
        {
            SectionIds = [1, 1, 1],
            StartTimestamps = [100, 200, 300],
            ElapsedTicks = [50, 20, 100],
        };

        agg.OnSectionBatch(batch);

        agg.TotalSamples.Should().Be(3);
        SectionStats stats = agg.Sections.Single(s => s.SectionId == 1);
        stats.SampleCount.Should().Be(3);
        stats.TotalElapsedTicks.Should().Be(170);
        stats.MinElapsedTicks.Should().Be(20);
        stats.MaxElapsedTicks.Should().Be(100);
        stats.LastStartTimestamp.Should().Be(300);
    }

    [Fact]
    public void OnSectionBatch_updates_min_and_max_across_multiple_batches()
    {
        SessionAggregator agg = new();
        agg.OnSectionBatch(new()
        {
            SectionIds = [1],
            StartTimestamps = [100],
            ElapsedTicks = [500],
        });
        agg.OnSectionBatch(new()
        {
            SectionIds = [1],
            StartTimestamps = [200],
            ElapsedTicks = [10],
        });
        agg.OnSectionBatch(new()
        {
            SectionIds = [1],
            StartTimestamps = [300],
            ElapsedTicks = [1000],
        });

        SectionStats stats = agg.Sections.Single(s => s.SectionId == 1);
        stats.MinElapsedTicks.Should().Be(10);
        stats.MaxElapsedTicks.Should().Be(1000);
    }

    [Fact]
    public void OnGcEvents_increments_total_count_by_batch_size()
    {
        SessionAggregator agg = new();
        GcEventsBatch batch = new()
        {
            Generations = new byte[] { 0, 1, 2 },
            PauseTypes = new byte[3],
            HeapBefore = new long[3],
            HeapAfter = new long[3],
            DurationMicros = new long[3],
            Ticks = new long[3],
            AllocationRateBytesPerMinute = new long[3],
        };

        agg.OnGcEvents(batch);
        agg.OnGcEvents(batch);

        agg.TotalGcEvents.Should().Be(6);
    }

    [Fact]
    public void OnAllocations_increments_total_count_by_window_count()
    {
        SessionAggregator agg = new();
        AllocationsBatch batch = new()
        {
            WindowStartTimestamps = new long[] { 1, 2 },
            WindowDurationsMs = new long[2],
            BytesAllocated = new long[2],
            SamplesCount = new long[2],
        };

        agg.OnAllocations(batch);

        agg.TotalAllocations.Should().Be(2);
    }


    [Fact]
    public void OnMetricRegistrations_records_name_kind_and_unit()
    {
        SessionAggregator agg = new();
        MetricRegistrationsBatch batch = new()
        {
            MetricIds = [10, 11],
            Names = ["my.mod.frames_drawn", "my.mod.heap_used"],
            Kinds = [0, 1],
            Units = ["count", "bytes"],
        };

        agg.OnMetricRegistrations(batch);

        agg.MetricCount.Should().Be(2);
        MetricStats m10 = agg.Metrics.Single(m => m.MetricId == 10);
        m10.Name.Should().Be("my.mod.frames_drawn");
        m10.Kind.Should().Be((byte)0);
        m10.Unit.Should().Be("count");
        MetricStats m11 = agg.Metrics.Single(m => m.MetricId == 11);
        m11.Name.Should().Be("my.mod.heap_used");
        m11.Kind.Should().Be((byte)1);
        m11.Unit.Should().Be("bytes");
    }

    [Fact]
    public void OnMetrics_accumulates_latest_value_and_total_samples_per_label()
    {
        SessionAggregator agg = new();
        agg.OnMetricRegistrations(new MetricRegistrationsBatch
        {
            MetricIds = [10],
            Names = ["my.mod.frames"],
            Kinds = [0],
            Units = ["count"],
        });

        agg.OnMetrics(new MetricsBatch
        {
            MetricIds = [10, 10],
            LabelCanonicals = ["scene=map", "scene=ui"],
            Kinds = [0, 0],
            Values = [42, 17],
            SampleCounts = [1, 1],
        });
        agg.OnMetrics(new MetricsBatch
        {
            MetricIds = [10],
            LabelCanonicals = ["scene=map"],
            Kinds = [0],
            Values = [99],
            SampleCounts = [3],
        });

        agg.TotalMetricObservations.Should().Be(3);
        MetricStats stats = agg.Metrics.Single();
        MetricLabelStats mapLabel = stats.Labels["scene=map"];
        mapLabel.LatestValue.Should().Be(99);
        mapLabel.TotalSampleCount.Should().Be(4);
        MetricLabelStats uiLabel = stats.Labels["scene=ui"];
        uiLabel.LatestValue.Should().Be(17);
        uiLabel.TotalSampleCount.Should().Be(1);
    }

    [Fact]
    public void OnMetrics_creates_placeholder_metric_when_registration_missing()
    {
        SessionAggregator agg = new();
        agg.OnMetrics(new MetricsBatch
        {
            MetricIds = [99],
            LabelCanonicals = [""],
            Kinds = [2],
            Values = [123],
            SampleCounts = [1],
        });

        MetricStats stats = agg.Metrics.Single();
        stats.MetricId.Should().Be(99);
        stats.Kind.Should().Be((byte)2);
        stats.Labels[""].LatestValue.Should().Be(123);
    }

    [Fact]
    public void Section_registered_after_samples_keeps_name_and_sample_counts()
    {
        SessionAggregator agg = new();
        agg.OnSectionBatch(new()
        {
            SectionIds = [42],
            StartTimestamps = [100],
            ElapsedTicks = [200],
        });
        agg.OnSectionRegistrations(new()
        {
            SectionIds = [42],
            Names = ["late.name"],
        });

        SectionStats stats = agg.Sections.Single(s => s.SectionId == 42);
        stats.Name.Should().Be("late.name");
        stats.SampleCount.Should().Be(1);
        stats.TotalElapsedTicks.Should().Be(200);
    }
}
