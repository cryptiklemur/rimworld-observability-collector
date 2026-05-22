using System;
using System.Threading;
using System.Threading.Tasks;
using Cryptiklemur.RimObs.Collector.Api;
using Cryptiklemur.RimObs.Collector.Runtime;
using Cryptiklemur.RimObs.Wire;
using Cryptiklemur.RimObs.Collector.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Cryptiklemur.RimObs.Collector.Hosting;

public static class Program {
    public static int Main(string[] args) {
        if (args.Length > 0 && args[0] != "serve") {
            return Cli.CliRouter.Run(args);
        }

        const int port = 17654;
        Logging.RingBufferLogSink logSink = new();
        string? logDir = null;
        try {
            CollectorToken token = CollectorToken.CreateFromEnvOrGenerate();
            string configDir = ConfigDirResolver.Resolve();
            logDir = Path.Combine(configDir, "logs");
            ConfigureLogger(logSink, logDir);

            try {
                RuntimeFiles.WriteAll(configDir, token, port);
                Log.Information("Wrote discovery files to {ConfigDir} (token source: {Source})", configDir, token.FromEnv ? "env" : "generated");
            }
            catch (System.Exception ex) {
                Log.Warning(ex, "Failed to write discovery files to {ConfigDir}", configDir);
            }

            string sessionsDir = Path.Combine(configDir, "sessions");
            WebApplication app = BuildApp(args, port, token, sessionsDir, logSink);
            StartUpdateCheck(app.Services);
            app.Run();
            return 0;
        }
        catch (System.Exception ex) {
            if (Log.Logger is null || ReferenceEquals(Log.Logger, Serilog.Core.Logger.None)) {
                ConfigureLogger(logSink, logDir);
            }
            Log.Fatal(ex, "Collector terminated unexpectedly");
            return 1;
        }
        finally {
            Log.CloseAndFlush();
        }
    }

    internal static void ConfigureLogger(Logging.RingBufferLogSink ringSink, string? logDir) {
        LoggerConfiguration config = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .WriteTo.Sink(ringSink);

        if (!string.IsNullOrWhiteSpace(logDir)) {
            try {
                Directory.CreateDirectory(logDir);
                string path = Path.Combine(logDir, "collector-.log");
                config = config.WriteTo.File(
                    path: path,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7);
            }
            catch (System.Exception) {
                // Best-effort: if we cannot create the log directory, continue with console + ring sinks only.
            }
        }

        Log.Logger = config.CreateLogger();
    }

    public static WebApplication BuildApp(string[] args, int port) {
        return BuildApp(args, port, CollectorToken.CreateFromEnvOrGenerate(), sessionsDir: null);
    }

    public static WebApplication BuildApp(string[] args, int port, CollectorToken token) {
        return BuildApp(args, port, token, sessionsDir: null);
    }

    public static WebApplication BuildApp(string[] args, int port, CollectorToken token, string? sessionsDir) {
        return BuildApp(args, port, token, sessionsDir, logSink: null);
    }

    public static WebApplication BuildApp(string[] args, int port, CollectorToken token, string? sessionsDir, Logging.RingBufferLogSink? logSink) {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        builder.Host.UseSerilog();
        builder.WebHost.UseUrls($"http://127.0.0.1:{port}");

        Logging.RingBufferLogSink sink = logSink ?? new Logging.RingBufferLogSink();
        builder.Services.AddSingleton(sink);

        builder.Services.AddSingleton(token);
        builder.Services.AddSingleton<Update.UpdateState>();
        bool hasPersister = !string.IsNullOrWhiteSpace(sessionsDir);
        if (hasPersister) {
            builder.Services.AddSingleton<Storage.ISessionPersister>(_ => new Storage.SqliteSessionPersister(sessionsDir!));
        }
        builder.Services.AddSingleton<Aggregation.SessionAggregator>();
        builder.Services.AddSingleton<Receive.UdpReceiver>(sp =>
            new Receive.UdpReceiver(
                sp.GetRequiredService<Aggregation.SessionAggregator>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Receive.UdpReceiver>>(),
                port
            ));
        builder.Services.AddHostedService(sp => sp.GetRequiredService<Receive.UdpReceiver>());
        if (hasPersister) {
            builder.Services.AddHostedService<Storage.PersistenceFlusher>();
        }

        WebApplication app = builder.Build();
        app.UseOriginCheck(port);
        app.UseBearerAuth(token);
        MapApiEndpoints(app);
        return app;
    }

    public static void MapApiEndpoints(WebApplication app) {
        app.MapStatusEndpoints();
        app.MapSessionsEndpoints();
        app.MapVersionEndpoints();
        app.MapLogsEndpoints();
        app.MapSpaEndpoints();
    }

    private const string UpdateOwner = "cryptiklemur";
    private const string UpdateRepo = "rimworld-observability-collector";

    internal static void StartUpdateCheck(IServiceProvider services) {
        Update.UpdateState state = services.GetRequiredService<Update.UpdateState>();
        _ = Task.Run(async () => {
            try {
                using System.Net.Http.HttpClient client = new System.Net.Http.HttpClient {
                    Timeout = TimeSpan.FromSeconds(10),
                };
                using CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                Update.ReleaseInfo? latest = await Update.UpdateChecker
                    .CheckAsync(client, BuildInfo.Revision, UpdateOwner, UpdateRepo, cts.Token)
                    .ConfigureAwait(false);
                state.Set(latest);
                if (latest is not null) {
                    Log.Information("Update available: {Tag} ({Url})", latest.TagName, latest.HtmlUrl);
                }
            }
            catch (System.Exception ex) {
                Log.Warning(ex, "Update check failed");
            }
        });
    }
}
