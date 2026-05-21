using System.Diagnostics;
using System.Linq;
using Cryptiklemur.RimObs.Collector.Aggregation;
using Cryptiklemur.RimObs.Wire;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Cryptiklemur.RimObs.Collector.Api;

public static class SessionsEndpoints
{
    public static IEndpointRouteBuilder MapSessionsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/sessions/current/sections", (SessionAggregator aggregator) =>
        {
            long freq = aggregator.Meta?.StopwatchFrequency ?? Stopwatch.Frequency;
            double nsPerTick = 1_000_000_000.0 / freq;
            return Results.Ok(new
            {
                schema_version = SchemaVersion.Current,
                sections = aggregator.Sections.Select(s => new
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

        return endpoints;
    }
}
