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

    [Fact]
    public async Task Gc_endpoint_returns_recent_events_newest_first_and_honors_limit()
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
                Ticks = [111, 222, 333],
                AllocationRateBytesPerMinute = [1000, 2000, 3000],
            }));

            await WaitFor(async () =>
            {
                string body = await http.GetStringAsync("/api/v1/sessions/current/gc");
                using JsonDocument doc = JsonDocument.Parse(body);
                return doc.RootElement.GetProperty("events").GetArrayLength() >= 3;
            }, TimeSpan.FromSeconds(3));

            string fullBody = await http.GetStringAsync("/api/v1/sessions/current/gc");
            _out.WriteLine($"gc full: {fullBody}");
            using JsonDocument full = JsonDocument.Parse(fullBody);
            full.RootElement.GetProperty("total_events").GetInt64().Should().Be(3);
            JsonElement events = full.RootElement.GetProperty("events");
            events.GetArrayLength().Should().Be(3);
            events[0].GetProperty("ticks").GetInt64().Should().Be(333);
            events[0].GetProperty("generation").GetInt32().Should().Be(2);
            events[1].GetProperty("ticks").GetInt64().Should().Be(222);
            events[2].GetProperty("ticks").GetInt64().Should().Be(111);

            string limited = await http.GetStringAsync("/api/v1/sessions/current/gc?limit=2");
            _out.WriteLine($"gc limited: {limited}");
            using JsonDocument lim = JsonDocument.Parse(limited);
            JsonElement limEvents = lim.RootElement.GetProperty("events");
            limEvents.GetArrayLength().Should().Be(2);
            limEvents[0].GetProperty("ticks").GetInt64().Should().Be(333);
            limEvents[1].GetProperty("ticks").GetInt64().Should().Be(222);
        }
        finally
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }

    [Fact]
    public async Task Hotspots_endpoint_returns_sections_sorted_by_total_descending_and_honors_limit()
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
                SessionId = "hot-session",
                StartedUtcTicks = DateTime.UtcNow.Ticks,
                StopwatchFrequency = System.Diagnostics.Stopwatch.Frequency,
                AnchorTimestamp = System.Diagnostics.Stopwatch.GetTimestamp(),
                LibraryVersion = "0.0.0-smoke",
                GameVersion = "1.6",
            }));

            SendBatch(port, BatchType.SectionRegistrations, MessagePackSerializer.Serialize(new SectionRegistrationsBatch
            {
                SectionIds = [10, 20, 30],
                Names = ["hot.cold", "hot.warm", "hot.peak"],
            }));

            SendBatch(port, BatchType.Sections, MessagePackSerializer.Serialize(new SectionBatch
            {
                SectionIds = [10, 20, 20, 30, 30, 30],
                StartTimestamps = [1, 2, 3, 4, 5, 6],
                ElapsedTicks = [100, 500, 500, 1000, 1000, 1000],
            }));

            await WaitFor(async () =>
            {
                string body = await http.GetStringAsync("/api/v1/status");
                using JsonDocument doc = JsonDocument.Parse(body);
                int sectionCount = doc.RootElement.GetProperty("receive").GetProperty("section_count").GetInt32();
                return sectionCount >= 3;
            }, TimeSpan.FromSeconds(3));

            string hotspotsBody = await http.GetStringAsync("/api/v1/sessions/current/hotspots?limit=2");
            _out.WriteLine($"hotspots: {hotspotsBody}");
            using JsonDocument doc = JsonDocument.Parse(hotspotsBody);
            JsonElement hotspots = doc.RootElement.GetProperty("hotspots");

            hotspots.GetArrayLength().Should().Be(2);
            hotspots[0].GetProperty("name").GetString().Should().Be("hot.peak");
            hotspots[1].GetProperty("name").GetString().Should().Be("hot.warm");
            hotspots[0].GetProperty("total_ns").GetInt64().Should().BeGreaterThan(hotspots[1].GetProperty("total_ns").GetInt64());
            hotspots[0].GetProperty("mean_ns").GetInt64().Should().BeGreaterThan(0);
            hotspots[0].GetProperty("sample_count").GetInt32().Should().Be(3);
            hotspots[1].GetProperty("sample_count").GetInt32().Should().Be(2);
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
