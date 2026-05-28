using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Cryptiklemur.RimObs.Collector.Aggregation;
using Cryptiklemur.RimObs.Collector.Hosting;
using Cryptiklemur.RimObs.Collector.Security;
using Cryptiklemur.RimObs.Wire;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Cryptiklemur.RimObs.Collector.Tests;

public sealed class BundleEndpointsTests {
    private readonly ITestOutputHelper _out;

    public BundleEndpointsTests(ITestOutputHelper output) {
        _out = output;
    }

    private static int PickFreePort() {
        TcpListener listener = new(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    [Fact]
    public async Task Export_current_session_returns_zip_with_manifest_and_report() {
        int port = PickFreePort();
        CollectorToken token = CollectorToken.FromExplicitValue("bundle-bearer-token");
        WebApplication app = Program.BuildApp([], port, token);
        await app.StartAsync();
        try {
            using HttpClient http = new() { BaseAddress = new Uri($"http://127.0.0.1:{port}") };
            await WaitFor(async () => {
                HttpResponseMessage r = await http.GetAsync("/api/v1/status");
                return r.IsSuccessStatusCode;
            }, TimeSpan.FromSeconds(3));

            SessionAggregator aggregator = app.Services.GetRequiredService<SessionAggregator>();
            SessionMeta meta = new SessionMeta {
                SessionId = "bundle-session",
                StartedUtcTicks = DateTime.UtcNow.Ticks,
                StopwatchFrequency = System.Diagnostics.Stopwatch.Frequency,
                AnchorTimestamp = System.Diagnostics.Stopwatch.GetTimestamp(),
                LibraryVersion = "0.0.0-bundle",
                GameVersion = "1.6",
            };
            aggregator.OnSessionMeta(meta);

            using HttpRequestMessage post = new(HttpMethod.Post, "/api/v1/export/bundle") {
                Content = new StringContent(
                    "{\"session_id\":\"bundle-session\",\"include\":[],\"force\":false}",
                    Encoding.UTF8,
                    "application/json"),
            };
            post.Headers.Add("Origin", $"http://127.0.0.1:{port}");
            post.Headers.Add("Authorization", $"Bearer {token.Value}");
            HttpResponseMessage resp = await http.SendAsync(post);
            _out.WriteLine($"export status: {resp.StatusCode}");
            resp.StatusCode.Should().Be(HttpStatusCode.OK);
            resp.Content.Headers.ContentType!.MediaType.Should().Be("application/zip");
            resp.Content.Headers.ContentDisposition!.FileName.Should().EndWith(".rimobs.zip");
            resp.Content.Headers.ContentDisposition!.FileName.Should().Contain("bundle-session");

            byte[] zipBytes = await resp.Content.ReadAsByteArrayAsync();
            zipBytes.Length.Should().BeGreaterThan(0);

            using MemoryStream ms = new MemoryStream(zipBytes);
            using ZipArchive archive = new ZipArchive(ms, ZipArchiveMode.Read);
            HashSet<string> names = archive.Entries.Select(e => e.FullName).ToHashSet();
            names.Should().Contain("manifest.json");
            names.Should().Contain("report.html");
        }
        finally {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }

    [Fact]
    public async Task Export_unknown_session_returns_404() {
        int port = PickFreePort();
        CollectorToken token = CollectorToken.FromExplicitValue("bundle-bearer-token");
        WebApplication app = Program.BuildApp([], port, token);
        await app.StartAsync();
        try {
            using HttpClient http = new() { BaseAddress = new Uri($"http://127.0.0.1:{port}") };
            await WaitFor(async () => {
                HttpResponseMessage r = await http.GetAsync("/api/v1/status");
                return r.IsSuccessStatusCode;
            }, TimeSpan.FromSeconds(3));

            SessionAggregator aggregator = app.Services.GetRequiredService<SessionAggregator>();
            aggregator.OnSessionMeta(new SessionMeta {
                SessionId = "real-session",
                StartedUtcTicks = DateTime.UtcNow.Ticks,
                StopwatchFrequency = System.Diagnostics.Stopwatch.Frequency,
                AnchorTimestamp = System.Diagnostics.Stopwatch.GetTimestamp(),
                LibraryVersion = "0.0.0-bundle",
                GameVersion = "1.6",
            });

            using HttpRequestMessage post = new(HttpMethod.Post, "/api/v1/export/bundle") {
                Content = new StringContent(
                    "{\"session_id\":\"nope\",\"include\":[],\"force\":false}",
                    Encoding.UTF8,
                    "application/json"),
            };
            post.Headers.Add("Origin", $"http://127.0.0.1:{port}");
            post.Headers.Add("Authorization", $"Bearer {token.Value}");
            HttpResponseMessage resp = await http.SendAsync(post);
            _out.WriteLine($"export status: {resp.StatusCode}");
            resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }

    private static async Task WaitFor(Func<Task<bool>> predicate, TimeSpan timeout) {
        DateTime deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline) {
            if (await predicate())
                return;
            await Task.Delay(50);
        }
        throw new TimeoutException($"Predicate not satisfied within {timeout}.");
    }
}
