using System.Text.Json;
using System.Threading.Tasks;
using Cryptiklemur.RimObs.Collector.Config;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Cryptiklemur.RimObs.Collector.Api;

public static class ConfigEndpoints {
    public static IEndpointRouteBuilder MapConfigEndpoints(this IEndpointRouteBuilder endpoints) {
        endpoints.MapGet("/api/v1/config", (ConfigStore store) =>
            Results.Json(store.Current, ConfigJson.Options));

        endpoints.MapPost("/api/v1/config", async (HttpContext context, ConfigStore store) => {
            RimObsConfig? incoming;
            try {
                incoming = await context.Request.ReadFromJsonAsync<RimObsConfig>(ConfigJson.Options);
            }
            catch (JsonException) {
                return Results.BadRequest(new { schema_version = RimObsConfig.Version, reason = "malformed config body" });
            }

            if (incoming is null) {
                return Results.BadRequest(new { schema_version = RimObsConfig.Version, reason = "empty config body" });
            }

            if (incoming.SchemaVersion != RimObsConfig.Version) {
                return Results.BadRequest(new {
                    schema_version = RimObsConfig.Version,
                    reason = $"unsupported schema_version {incoming.SchemaVersion}",
                });
            }

            store.Replace(incoming);
            return Results.Json(store.Current, ConfigJson.Options);
        });

        return endpoints;
    }
}
