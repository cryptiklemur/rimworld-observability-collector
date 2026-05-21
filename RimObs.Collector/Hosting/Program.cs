using Cryptiklemur.RimObs.Collector.Api;
using Cryptiklemur.RimObs.Collector.Runtime;
using Cryptiklemur.RimObs.Collector.Security;
using Microsoft.AspNetCore.Builder;
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

        const int port = 17654;
        try
        {
            CollectorToken token = CollectorToken.CreateFromEnvOrGenerate();
            string configDir = ConfigDirResolver.Resolve();
            try
            {
                RuntimeFiles.WriteAll(configDir, token, port);
                Log.Information("Wrote discovery files to {ConfigDir} (token source: {Source})", configDir, token.FromEnv ? "env" : "generated");
            }
            catch (System.Exception ex)
            {
                Log.Warning(ex, "Failed to write discovery files to {ConfigDir}", configDir);
            }

            string sessionsDir = Path.Combine(configDir, "sessions");
            WebApplication app = BuildApp(args, port, token, sessionsDir);
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
        return BuildApp(args, port, CollectorToken.CreateFromEnvOrGenerate(), sessionsDir: null);
    }

    public static WebApplication BuildApp(string[] args, int port, CollectorToken token)
    {
        return BuildApp(args, port, token, sessionsDir: null);
    }

    public static WebApplication BuildApp(string[] args, int port, CollectorToken token, string? sessionsDir)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        builder.Host.UseSerilog();
        builder.WebHost.UseUrls($"http://127.0.0.1:{port}");

        builder.Services.AddSingleton(token);
        if (!string.IsNullOrWhiteSpace(sessionsDir))
        {
            builder.Services.AddSingleton<Storage.ISessionPersister>(_ => new Storage.SqliteSessionPersister(sessionsDir));
        }
        builder.Services.AddSingleton<Aggregation.SessionAggregator>();
        builder.Services.AddSingleton<Receive.UdpReceiver>(sp =>
            new Receive.UdpReceiver(
                sp.GetRequiredService<Aggregation.SessionAggregator>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Receive.UdpReceiver>>(),
                port
            ));
        builder.Services.AddHostedService(sp => sp.GetRequiredService<Receive.UdpReceiver>());

        WebApplication app = builder.Build();
        app.UseOriginCheck(port);
        app.UseBearerAuth(token);
        MapApiEndpoints(app);
        return app;
    }

    public static void MapApiEndpoints(WebApplication app)
    {
        app.MapStatusEndpoints();
        app.MapSessionsEndpoints();
        app.MapVersionEndpoints();
    }
}
