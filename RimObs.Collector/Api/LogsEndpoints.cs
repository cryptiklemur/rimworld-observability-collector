using Cryptiklemur.RimObs.Collector.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Cryptiklemur.RimObs.Collector.Api;

public static class LogsEndpoints {
    public const int DefaultLimit = 200;
    public const int MaxLimit = 1024;

    public static IEndpointRouteBuilder MapLogsEndpoints(this IEndpointRouteBuilder endpoints) {
        endpoints.MapGet("/api/v1/logs", (RingBufferLogSink sink, string? level, int? limit) => {
            int requested = limit ?? DefaultLimit;
            int effective = requested <= 0 ? DefaultLimit : Math.Min(requested, MaxLimit);
            IReadOnlyList<LogEntry> entries = sink.Snapshot(level, effective);
            return Results.Ok(new {
                count = entries.Count,
                entries = entries.Select(e => new {
                    timestamp = e.Timestamp,
                    level = e.Level,
                    message = e.Message,
                    exception = e.Exception,
                }),
            });
        });

        return endpoints;
    }
}
