using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Cryptiklemur.RimObs.Collector.Bundle;
using Cryptiklemur.RimObs.Wire;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Cryptiklemur.RimObs.Collector.Api;

public static class BundleEndpoints {
    public static IEndpointRouteBuilder MapBundleEndpoints(this IEndpointRouteBuilder endpoints) {
        endpoints.MapPost("/api/v1/export/bundle", async (HttpContext ctx, BundleExportService service) => {
            BundleExportRequestDto? dto = await ctx.Request.ReadFromJsonAsync<BundleExportRequestDto>();
            if (dto is null || string.IsNullOrEmpty(dto.SessionId))
                return Results.BadRequest(new { schema_version = SchemaVersion.Current, reason = "missing session_id" });

            HashSet<BundleContentKey> includes = ParseIncludes(dto.Include);
            BundleExportResult result = await service.ExportAsync(new BundleExportRequest {
                SessionId = dto.SessionId,
                Includes = includes,
                Force = dto.Force,
            }, ctx.RequestAborted);

            return result.Status switch {
                BundleExportStatus.Ok => Results.File(result.Bytes!, "application/zip", $"{dto.SessionId}.rimobs.zip"),
                BundleExportStatus.UnknownSession => Results.NotFound(new { schema_version = SchemaVersion.Current, reason = "unknown session" }),
                BundleExportStatus.NoActiveSession => Results.NotFound(new { schema_version = SchemaVersion.Current, reason = "no active session" }),
                BundleExportStatus.ExceedsSoftCap => Results.Json(new {
                    schema_version = SchemaVersion.Current,
                    error = "estimate_exceeds_soft_cap",
                    estimated_bytes = result.EstimatedBytes,
                    cap_bytes = BundleSizeEstimator.SoftCapBytes,
                    hint = "retry with force=true",
                }, statusCode: StatusCodes.Status413PayloadTooLarge),
                _ => Results.StatusCode(500),
            };
        });

        return endpoints;
    }

    private static HashSet<BundleContentKey> ParseIncludes(string[]? values) {
        HashSet<BundleContentKey> set = new HashSet<BundleContentKey>();
        if (values is null) return set;
        foreach (string v in values) {
            BundleContentKey? key = v switch {
                "metrics-sqlite" or "metrics_sqlite" => BundleContentKey.MetricsSqlite,
                "call-hierarchy" or "call_hierarchy" => BundleContentKey.CallHierarchy,
                "gc-events" or "gc_events" => BundleContentKey.GcEvents,
                "allocations" => BundleContentKey.Allocations,
                "patches" => BundleContentKey.Patches,
                _ => null,
            };
            if (key is not null) set.Add(key.Value);
        }
        return set;
    }

    private sealed class BundleExportRequestDto {
        [JsonPropertyName("session_id")]
        public string SessionId { get; set; } = string.Empty;

        [JsonPropertyName("include")]
        public string[]? Include { get; set; }

        [JsonPropertyName("force")]
        public bool Force { get; set; }
    }
}
