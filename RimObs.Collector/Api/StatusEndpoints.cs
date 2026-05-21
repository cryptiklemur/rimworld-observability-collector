using System;
using Cryptiklemur.RimObs.Collector.Aggregation;
using Cryptiklemur.RimObs.Wire;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Cryptiklemur.RimObs.Collector.Api;

public static class StatusEndpoints {
    public static IEndpointRouteBuilder MapStatusEndpoints(this IEndpointRouteBuilder endpoints) {
        endpoints.MapGet("/api/v1/status", (SessionAggregator aggregator, Update.UpdateState updateState) => {
            SessionMeta? meta = aggregator.Meta;
            Update.ReleaseInfo? latest = updateState.Latest;
            return Results.Ok(new {
                schema_version = SchemaVersion.Current,
                status = "running",
                version = BuildInfo.Revision,
                session = meta is null ? null : new {
                    id = meta.SessionId,
                    started_utc = new DateTime(meta.StartedUtcTicks, DateTimeKind.Utc),
                    library_version = meta.LibraryVersion,
                },
                receive = new {
                    total_batches = aggregator.TotalBatches,
                    total_samples = aggregator.TotalSamples,
                    total_bytes = aggregator.TotalBytes,
                    last_batch_utc = aggregator.LastBatchUtc == default ? (DateTime?)null : aggregator.LastBatchUtc,
                    section_count = aggregator.SectionCount,
                    total_gc_events = aggregator.TotalGcEvents,
                    total_allocations = aggregator.TotalAllocations,
                },
                update = new {
                    available = latest is not null,
                    latest_version = latest?.TagName,
                    url = latest?.HtmlUrl,
                },
            });
        });

        return endpoints;
    }
}
