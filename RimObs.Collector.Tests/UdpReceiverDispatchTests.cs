using Cryptiklemur.RimObs.Collector.Aggregation;
using Cryptiklemur.RimObs.Collector.Receive;
using Cryptiklemur.RimObs.Wire;
using FluentAssertions;
using MessagePack;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Cryptiklemur.RimObs.Collector.Tests;

public sealed class UdpReceiverDispatchTests
{
    [Fact]
    public void Dispatch_drops_payload_with_wrong_schema_version_without_aggregating()
    {
        SessionAggregator agg = new();
        UdpReceiver receiver = NewReceiver(agg);
        byte[] bytes = SerializeEnvelope(BatchType.SessionMeta, [], schemaVersion: SchemaVersion.Current + 1);

        receiver.Dispatch(bytes);

        agg.TotalBatches.Should().Be(0);
        agg.Meta.Should().BeNull();
    }

    [Fact]
    public void Dispatch_drops_malformed_envelope_bytes_without_aggregating()
    {
        SessionAggregator agg = new();
        UdpReceiver receiver = NewReceiver(agg);
        byte[] bytes = [0xFF, 0xFE, 0xFD, 0xFC];

        receiver.Dispatch(bytes);

        agg.TotalBatches.Should().Be(0);
    }

    [Fact]
    public void Dispatch_session_meta_routes_to_aggregator_meta()
    {
        SessionAggregator agg = new();
        UdpReceiver receiver = NewReceiver(agg);
        SessionMeta meta = new()
        {
            SessionId = "abc",
            StartedUtcTicks = 1,
            StopwatchFrequency = 10_000_000,
            AnchorTimestamp = 0,
            LibraryVersion = "0.0.0",
            GameVersion = "1.6",
        };
        byte[] payload = MessagePackSerializer.Serialize(meta);
        byte[] bytes = SerializeEnvelope(BatchType.SessionMeta, payload);

        receiver.Dispatch(bytes);

        agg.Meta.Should().NotBeNull();
        agg.Meta!.SessionId.Should().Be("abc");
        agg.TotalBatches.Should().Be(1);
    }

    [Fact]
    public void Dispatch_section_batch_routes_to_aggregator_section_handler()
    {
        SessionAggregator agg = new();
        UdpReceiver receiver = NewReceiver(agg);
        SectionBatch batch = new()
        {
            SectionIds = [9],
            StartTimestamps = [10],
            ElapsedTicks = [100],
        };
        byte[] bytes = SerializeEnvelope(BatchType.Sections, MessagePackSerializer.Serialize(batch));

        receiver.Dispatch(bytes);

        agg.TotalSamples.Should().Be(1);
        agg.TotalBatches.Should().Be(1);
    }

    [Fact]
    public void Dispatch_section_batch_with_corrupt_payload_does_not_crash_or_increment_samples()
    {
        SessionAggregator agg = new();
        UdpReceiver receiver = NewReceiver(agg);
        byte[] bytes = SerializeEnvelope(BatchType.Sections, [0xFF, 0xFF]);

        receiver.Dispatch(bytes);

        agg.TotalBatches.Should().Be(1);
        agg.TotalSamples.Should().Be(0);
    }

    [Fact]
    public void Dispatch_ping_batch_increments_batches_without_throwing()
    {
        SessionAggregator agg = new();
        UdpReceiver receiver = NewReceiver(agg);
        byte[] bytes = SerializeEnvelope(BatchType.Ping, []);

        receiver.Dispatch(bytes);

        agg.TotalBatches.Should().Be(1);
    }

    private static UdpReceiver NewReceiver(SessionAggregator agg)
    {
        return new UdpReceiver(agg, NullLogger<UdpReceiver>.Instance, port: 0);
    }

    private static byte[] SerializeEnvelope(BatchType batchType, byte[] payload, int? schemaVersion = null)
    {
        TelemetryBatch envelope = new()
        {
            SchemaVersion = schemaVersion ?? SchemaVersion.Current,
            BatchType = batchType,
            Payload = payload,
        };
        return MessagePackSerializer.Serialize(envelope);
    }
}
