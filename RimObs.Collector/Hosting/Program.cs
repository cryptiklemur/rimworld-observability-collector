using Cryptiklemur.RimObs.Collector.Api;
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
        return BuildApp(args, port, CollectorToken.CreateFromEnvOrGenerate());
    }

    public static WebApplication BuildApp(string[] args, int port, CollectorToken token)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        builder.Host.UseSerilog();
        builder.WebHost.UseUrls($"http://127.0.0.1:{port}");

        builder.Services.AddSingleton(token);
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
