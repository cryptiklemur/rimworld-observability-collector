using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Cryptiklemur.RimObs.Collector.Hosting;

public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length > 0 && args[0] != "serve")
        {
            return Cli.CliRouter.Run(args);
        }

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .CreateLogger();

        try
        {
            WebApplication app = BuildApp(args, port: 17654);
            app.Run();
            return 0;
        }
        catch (System.Exception ex)
        {
            Log.Fatal(ex, "Collector terminated unexpectedly");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    public static WebApplication BuildApp(string[] args, int port)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        builder.Host.UseSerilog();
        builder.WebHost.UseUrls($"http://127.0.0.1:{port}");

        builder.Services.AddSingleton<Aggregation.SessionAggregator>();
        builder.Services.AddSingleton<Receive.UdpReceiver>(sp =>
            new Receive.UdpReceiver(
                sp.GetRequiredService<Aggregation.SessionAggregator>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Receive.UdpReceiver>>(),
                port
            ));
        builder.Services.AddHostedService(sp => sp.GetRequiredService<Receive.UdpReceiver>());

        WebApplication app = builder.Build();

        app.MapGet("/api/v1/status", (Aggregation.SessionAggregator agg) =>
        {
            Wire.SessionMeta? meta = agg.Meta;
            return Results.Ok(new
            {
                schema_version = Wire.SchemaVersion.Current,
                status = "running",
                version = BuildInfo.Revision,
                session = meta is null ? null : new
                {
                    id = meta.SessionId,
                    started_utc = new DateTime(meta.StartedUtcTicks, DateTimeKind.Utc),
                    library_version = meta.LibraryVersion,
                },
                receive = new
                {
                    total_batches = agg.TotalBatches,
                    total_samples = agg.TotalSamples,
                    total_bytes = agg.TotalBytes,
                    last_batch_utc = agg.LastBatchUtc == default ? (DateTime?)null : agg.LastBatchUtc,
                    section_count = agg.SectionCount,
                    total_gc_events = agg.TotalGcEvents,
                    total_allocations = agg.TotalAllocations,
                },
            });
        });

        app.MapGet("/api/v1/sessions/current/sections", (Aggregation.SessionAggregator agg) =>
        {
            long freq = agg.Meta?.StopwatchFrequency ?? Stopwatch.Frequency;
            double nsPerTick = 1_000_000_000.0 / freq;
            return Results.Ok(new
            {
                schema_version = Wire.SchemaVersion.Current,
                sections = agg.Sections.Select(s => new
                {
                    id = s.SectionId,
                    name = s.Name,
                    sample_count = s.SampleCount,
                    total_ns = (long)(s.TotalElapsedTicks * nsPerTick),
                    min_ns = s.MinElapsedTicks == long.MaxValue ? 0 : (long)(s.MinElapsedTicks * nsPerTick),
                    max_ns = (long)(s.MaxElapsedTicks * nsPerTick),
                }).ToArray(),
            });
        });

        app.MapGet("/api/v1/version", () => Results.Ok(new
        {
            schema_version = Wire.SchemaVersion.Current,
            version = BuildInfo.Revision,
            built_at = BuildInfo.BuildTime,
        }));

        app.MapGet("/", () => Results.Text("RimObs Collector is running. Dashboard SPA will be served here.", "text/plain"));

        return app;
    }
}
