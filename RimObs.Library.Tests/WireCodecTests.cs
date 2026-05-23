using System.Buffers;
using Cryptiklemur.RimObs.Wire;
using FluentAssertions;
using MessagePack;
using Xunit;

namespace Cryptiklemur.RimObs.Tests;

public sealed class WireCodecTests {
    private static TelemetryBatch SampleEnvelope() => new() {
        SchemaVersion = SchemaVersion.Current,
        Sequence = 42UL,
        OwnerId = "test.owner",
        BatchType = BatchType.Sections,
        Payload = [1, 2, 3, 250, 0],
    };

    private static SectionBatch SampleSections() => new() {
        SectionIds = [1, 2, 3],
        ElapsedTicks = [100L, 200L, 300L],
        StartTimestamps = [10L, 20L, 30L],
        ParentIds = [-1, 1, 1],
    };

    private static MetricsBatch SampleMetrics() => new() {
        MetricIds = [7, 9],
        LabelCanonicals = ["a=1", "b=2"],
        Kinds = [0, 1],
        Values = [123L, 456L],
        SampleCounts = [1L, 2L],
    };

    private static GcEventsBatch SampleGcEvents() => new() {
        Generations = [0, 1, 2],
        PauseTypes = [0, 0, 1],
        HeapBefore = [1000L, 2000L, 3000L],
        HeapAfter = [900L, 1800L, 2700L],
        DurationMicros = [50L, 60L, 70L],
        Ticks = [11L, 22L, 33L],
        AllocationRateBytesPerMinute = [4096L, 8192L, 0L],
    };

    [Fact]
    public void TelemetryBatch_round_trips() {
        TelemetryBatch original = SampleEnvelope();
        TelemetryBatch decoded = WireCodec.Deserialize<TelemetryBatch>(WireCodec.Serialize(original));
        decoded.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void PingMessage_round_trips() {
        PingMessage original = new() { OwnerId = "owner", SentAtUtcTicks = 638000000000000000L };
        PingMessage decoded = WireCodec.Deserialize<PingMessage>(WireCodec.Serialize(original));
        decoded.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void PongMessage_round_trips_with_session() {
        PongMessage original = new() {
            OwnerId = "owner",
            PingSentAtUtcTicks = 123L,
            CollectorVersion = "1.2.3",
            SessionId = "abc",
        };
        PongMessage decoded = WireCodec.Deserialize<PongMessage>(WireCodec.Serialize(original));
        decoded.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void PongMessage_round_trips_with_null_session() {
        PongMessage original = new() {
            OwnerId = "owner",
            PingSentAtUtcTicks = 123L,
            CollectorVersion = "1.2.3",
            SessionId = null,
        };
        PongMessage decoded = WireCodec.Deserialize<PongMessage>(WireCodec.Serialize(original));
        decoded.SessionId.Should().BeNull();
        decoded.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void SessionMeta_round_trips() {
        SessionMeta original = new() {
            SessionId = "sess",
            StartedUtcTicks = 1L,
            StopwatchFrequency = 10_000_000L,
            AnchorTimestamp = 555L,
            LibraryVersion = "0.1.0",
            GameVersion = "1.6",
        };
        SessionMeta decoded = WireCodec.Deserialize<SessionMeta>(WireCodec.Serialize(original));
        decoded.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void SectionRegistrationsBatch_round_trips() {
        SectionRegistrationsBatch original = new() {
            SectionIds = [1, 2],
            Names = ["core.tick", "core.map"],
        };
        SectionRegistrationsBatch decoded = WireCodec.Deserialize<SectionRegistrationsBatch>(WireCodec.Serialize(original));
        decoded.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void SectionBatch_round_trips() {
        SectionBatch original = SampleSections();
        SectionBatch decoded = WireCodec.Deserialize<SectionBatch>(WireCodec.Serialize(original));
        decoded.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void MetricRegistrationsBatch_round_trips() {
        MetricRegistrationsBatch original = new() {
            MetricIds = [1, 2],
            Names = ["m.count", "m.gauge"],
            Kinds = [0, 1],
            Units = ["", "ms"],
        };
        MetricRegistrationsBatch decoded = WireCodec.Deserialize<MetricRegistrationsBatch>(WireCodec.Serialize(original));
        decoded.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void MetricsBatch_round_trips() {
        MetricsBatch original = SampleMetrics();
        MetricsBatch decoded = WireCodec.Deserialize<MetricsBatch>(WireCodec.Serialize(original));
        decoded.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void GcEventsBatch_round_trips() {
        GcEventsBatch original = SampleGcEvents();
        GcEventsBatch decoded = WireCodec.Deserialize<GcEventsBatch>(WireCodec.Serialize(original));
        decoded.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void AllocationsBatch_round_trips() {
        AllocationsBatch original = new() {
            WindowStartTimestamps = [1L, 2L],
            WindowDurationsMs = [1000L, 1000L],
            BytesAllocated = [4096L, 8192L],
            SamplesCount = [3L, 5L],
        };
        AllocationsBatch decoded = WireCodec.Deserialize<AllocationsBatch>(WireCodec.Serialize(original));
        decoded.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void PatchConflictsBatch_round_trips() {
        PatchConflictsBatch original = new() {
            SectionNames = ["core.tick", "core.map"],
            TargetMethods = ["Verse.TickManager:DoSingleTick", "Verse.Map:MapPreTick"],
            OtherOwners = ["Dubs.PerformanceAnalyzer", "Some.OtherMod"],
            PatchTypes = [1, 3],
            Priorities = [400, 0],
            PatchMethods = ["Dubs.Patch:Prefix", "Some.Patch:Transpiler"],
        };
        PatchConflictsBatch decoded = WireCodec.Deserialize<PatchConflictsBatch>(WireCodec.Serialize(original));
        decoded.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void TpsFpsBatch_round_trips() {
        TpsFpsBatch original = new() {
            Tps = 59.84231,
            Fps = 142.0007,
            Tick = 12345,
        };
        TpsFpsBatch decoded = WireCodec.Deserialize<TpsFpsBatch>(WireCodec.Serialize(original));
        decoded.Tps.Should().Be(original.Tps);
        decoded.Fps.Should().Be(original.Fps);
        decoded.Tick.Should().Be(original.Tick);
    }

    [Fact]
    public void TpsFpsBatch_round_trips_special_doubles() {
        TpsFpsBatch original = new() {
            Tps = 0.0,
            Fps = double.PositiveInfinity,
            Tick = -1,
        };
        TpsFpsBatch decoded = WireCodec.Deserialize<TpsFpsBatch>(WireCodec.Serialize(original));
        decoded.Tps.Should().Be(0.0);
        decoded.Fps.Should().Be(double.PositiveInfinity);
        decoded.Tick.Should().Be(-1);
    }

    [Fact]
    public void Empty_arrays_round_trip() {
        SectionBatch original = new();
        SectionBatch decoded = WireCodec.Deserialize<SectionBatch>(WireCodec.Serialize(original));
        decoded.SectionIds.Should().BeEmpty();
        decoded.ElapsedTicks.Should().BeEmpty();
        decoded.StartTimestamps.Should().BeEmpty();
        decoded.ParentIds.Should().BeEmpty();
    }

    [Fact]
    public void WireCodec_output_is_standard_messagepack_for_envelope() {
        TelemetryBatch original = SampleEnvelope();
        byte[] wireBytes = WireCodec.Serialize(original);

        MessagePackReader reader = new MessagePackReader(wireBytes);
        reader.ReadArrayHeader().Should().Be(5);
        reader.ReadInt32().Should().Be(original.SchemaVersion);
        reader.ReadUInt64().Should().Be(original.Sequence);
        reader.ReadString().Should().Be(original.OwnerId);
        reader.ReadByte().Should().Be((byte)original.BatchType);
        reader.ReadBytes()!.Value.ToArray().Should().Equal(original.Payload);
        reader.End.Should().BeTrue();
    }

    [Fact]
    public void WireCodec_reads_messagepack_serializer_output_for_envelope() {
        TelemetryBatch original = SampleEnvelope();

        ArrayBufferWriter<byte> buffer = new ArrayBufferWriter<byte>();
        MessagePackWriter writer = new MessagePackWriter(buffer);
        writer.WriteArrayHeader(5);
        writer.Write(original.SchemaVersion);
        writer.Write(original.Sequence);
        writer.Write(original.OwnerId);
        writer.Write((byte)original.BatchType);
        writer.Write(original.Payload);
        writer.Flush();

        TelemetryBatch viaWire = WireCodec.Deserialize<TelemetryBatch>(buffer.WrittenSpan.ToArray());
        viaWire.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void WireCodec_output_is_standard_messagepack_for_gc_events() {
        GcEventsBatch original = SampleGcEvents();
        byte[] wireBytes = WireCodec.Serialize(original);

        MessagePackReader reader = new MessagePackReader(wireBytes);
        reader.ReadArrayHeader().Should().Be(7);
        reader.ReadBytes()!.Value.ToArray().Should().Equal(original.Generations);
        reader.ReadBytes()!.Value.ToArray().Should().Equal(original.PauseTypes);
        int heapBeforeCount = reader.ReadArrayHeader();
        heapBeforeCount.Should().Be(original.HeapBefore.Length);
        for (int i = 0; i < heapBeforeCount; i++)
            reader.ReadInt64().Should().Be(original.HeapBefore[i]);
    }

    [Fact]
    public void Generic_serialize_dispatches_to_concrete_overload() {
        SectionBatch original = SampleSections();
        byte[] viaGeneric = WireCodec.Serialize<SectionBatch>(original);
        byte[] viaOverload = WireCodec.Serialize(original);
        viaGeneric.Should().Equal(viaOverload);
    }

    [Fact]
    public void SessionMeta_round_trips_control_fields() {
        SessionMeta original = new SessionMeta {
            SessionId = "abc",
            StartedUtcTicks = 100,
            StopwatchFrequency = 1_000_000,
            AnchorTimestamp = 200,
            LibraryVersion = "0.0.0",
            GameVersion = "1.6",
            ControlPort = 50321,
            ControlSecret = "deadbeef",
        };

        byte[] bytes = WireCodec.Serialize(original);
        SessionMeta decoded = WireCodec.Deserialize<SessionMeta>(bytes);

        decoded.ControlPort.Should().Be(50321);
        decoded.ControlSecret.Should().Be("deadbeef");
    }

    [Fact]
    public void SessionMeta_decodes_legacy_6_field_payload_with_default_control_fields() {
        ArrayBufferWriter<byte> buffer = new ArrayBufferWriter<byte>();
        MessagePackWriter writer = new MessagePackWriter(buffer);
        writer.WriteArrayHeader(6);
        writer.Write("legacy");
        writer.Write(1L);
        writer.Write(2L);
        writer.Write(3L);
        writer.Write("lib");
        writer.Write("game");
        writer.Flush();
        byte[] legacy = buffer.WrittenSpan.ToArray();

        SessionMeta decoded = WireCodec.Deserialize<SessionMeta>(legacy);

        decoded.SessionId.Should().Be("legacy");
        decoded.ControlPort.Should().Be(0);
        decoded.ControlSecret.Should().Be(string.Empty);
    }
}
