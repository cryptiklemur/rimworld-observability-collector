using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Cryptiklemur.RimObs.Collector.Hosting;
using Cryptiklemur.RimObs.Wire;
using FluentAssertions;
using MessagePack;
using Microsoft.AspNetCore.Builder;
using Xunit;
using Xunit.Abstractions;

namespace Cryptiklemur.RimObs.Collector.Tests;

public sealed class EndToEndSmokeTests
{
    private readonly ITestOutputHelper _out;

    public EndToEndSmokeTests(ITestOutputHelper output)
    {
        _out = output;
    }

    private static int PickFreePort()
    {
        TcpListener listener = new(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    [Fact]
    public async Task Udp_batch_appears_in_status_and_sections()
    {
        int port = PickFreePort();
        WebApplication app = Program.BuildApp([], port);
        await app.StartAsync();
        try
        {
            using HttpClient http = new() { BaseAddress = new Uri($"http://127.0.0.1:{port}") };

            await WaitFor(async () =>
            {
                HttpResponseMessage r = await http.GetAsync("/api/v1/status");
                return r.IsSuccessStatusCode;
            }, TimeSpan.FromSeconds(3));

            SendBatch(port, BatchType.SessionMeta, MessagePackSerializer.Serialize(new SessionMeta
            {
                SessionId = "smoke-session",
                StartedUtcTicks = DateTime.UtcNow.Ticks,
                StopwatchFrequency = System.Diagnostics.Stopwatch.Frequency,
                AnchorTimestamp = System.Diagnostics.Stopwatch.GetTimestamp(),
                LibraryVersion = "0.0.0-smoke",
                GameVersion = "1.6",
            }));

            SendBatch(port, BatchType.SectionRegistrations, MessagePackSerializer.Serialize(new SectionRegistrationsBatch
            {
                SectionIds = [42, 43],
                Names = ["smoke.tick", "smoke.path"],
            }));

            SendBatch(port, BatchType.Sections, MessagePackSerializer.Serialize(new SectionBatch
            {
                SectionIds = [42, 42, 43],
                StartTimestamps = [1000, 2000, 3000],
                ElapsedTicks = [500, 600, 700],
            }));

            await WaitFor(async () =>
            {
                string body = await http.GetStringAsync("/api/v1/status");
                _out.WriteLine($"status: {body}");
                using JsonDocument doc = JsonDocument.Parse(body);
                int samples = doc.RootElement.GetProperty("receive").GetProperty("total_samples").GetInt32();
                return samples >= 3;
            }, TimeSpan.FromSeconds(3));

            string statusBody = await http.GetStringAsync("/api/v1/status");
            using JsonDocument status = JsonDocument.Parse(statusBody);
            status.RootElement.GetProperty("receive").GetProperty("total_samples").GetInt32().Should().BeGreaterThanOrEqualTo(3);
            status.RootElement.GetProperty("receive").GetProperty("section_count").GetInt32().Should().Be(2);
            status.RootElement.GetProperty("session").GetProperty("id").GetString().Should().Be("smoke-session");

            string sectionsBody = await http.GetStringAsync("/api/v1/sessions/current/sections");
            _out.WriteLine($"sections: {sectionsBody}");
            using JsonDocument sectionsDoc = JsonDocument.Parse(sectionsBody);
            JsonElement sections = sectionsDoc.RootElement.GetProperty("sections");
            sections.GetArrayLength().Should().Be(2);

            int totalSamples = 0;
            foreach (JsonElement s in sections.EnumerateArray())
                totalSamples += s.GetProperty("sample_count").GetInt32();
            totalSamples.Should().Be(3);
        }
        finally
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }


    [Fact]
    public async Task GcEvents_and_Allocations_batches_increment_status_counters()
    {
        int port = PickFreePort();
        WebApplication app = Program.BuildApp([], port);
        await app.StartAsync();
        try
        {
            using HttpClient http = new() { BaseAddress = new Uri($"http://127.0.0.1:{port}") };

            await WaitFor(async () =>
            {
                HttpResponseMessage r = await http.GetAsync("/api/v1/status");
                return r.IsSuccessStatusCode;
            }, TimeSpan.FromSeconds(3));

            SendBatch(port, BatchType.GcEvents, MessagePackSerializer.Serialize(new GcEventsBatch
            {
                Generations = [0, 1, 2],
                PauseTypes = [0, 0, 1],
                HeapBefore = [100, 200, 300],
                HeapAfter = [80, 180, 280],
                DurationMicros = [10, 20, 30],
                Ticks = [1, 2, 3],
                AllocationRateBytesPerMinute = [1000, 2000, 3000],
            }));

            SendBatch(port, BatchType.Allocations, MessagePackSerializer.Serialize(new AllocationsBatch
            {
                WindowStartTimestamps = [10, 20],
                WindowDurationsMs = [5, 5],
                BytesAllocated = [4096, 8192],
                SamplesCount = [1, 1],
            }));

            await WaitFor(async () =>
            {
                string body = await http.GetStringAsync("/api/v1/status");
                _out.WriteLine($"status: {body}");
                using JsonDocument doc = JsonDocument.Parse(body);
                JsonElement recv = doc.RootElement.GetProperty("receive");
                long gc = recv.GetProperty("total_gc_events").GetInt64();
                long alloc = recv.GetProperty("total_allocations").GetInt64();
                return gc >= 3 && alloc >= 2;
            }, TimeSpan.FromSeconds(3));

            string statusBody = await http.GetStringAsync("/api/v1/status");
            using JsonDocument status = JsonDocument.Parse(statusBody);
            JsonElement receive = status.RootElement.GetProperty("receive");
            receive.GetProperty("total_gc_events").GetInt64().Should().Be(3);
            receive.GetProperty("total_allocations").GetInt64().Should().Be(2);
        }
        finally
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }

    private static void SendBatch(int port, BatchType type, byte[] payload)
    {
        TelemetryBatch envelope = new()
        {
            SchemaVersion = SchemaVersion.Current,
            Sequence = 1,
            OwnerId = "smoke",
            BatchType = type,
            Payload = payload,
        };
        byte[] bytes = MessagePackSerializer.Serialize(envelope);
        using UdpClient client = new();
        client.Send(bytes, bytes.Length, "127.0.0.1", port);
    }

    private static async Task WaitFor(Func<Task<bool>> condition, TimeSpan timeout)
    {
        DateTime deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                if (await condition())
                    return;
            }
            catch
            {
                // retry
            }
            await Task.Delay(50);
        }
        throw new TimeoutException("WaitFor condition never became true");
    }
}
