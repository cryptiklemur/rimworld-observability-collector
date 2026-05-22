using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Cryptiklemur.RimObs.Collector.Aggregation;
using Cryptiklemur.RimObs.Collector.Storage;
using Cryptiklemur.RimObs.Wire;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Cryptiklemur.RimObs.Collector.Api;

public static class SessionsEndpoints {
    private const int DefaultHotspotLimit = 50;
    private const int MaxHotspotLimit = 500;
    private const int DefaultGcEventLimit = 100;
    private const int MaxGcEventLimit = 1024;
    private const int MaxCallTreeDepth = 64;
    private const int MaxCallTreeTopN = 256;

    public static IEndpointRouteBuilder MapSessionsEndpoints(this IEndpointRouteBuilder endpoints) {
        endpoints.MapGet("/api/v1/sessions", (SessionAggregator aggregator, IServiceProvider services) => {
            SessionMeta? current = aggregator.Meta;
            List<object> sessions = new List<object>();
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);

            if (current is not null) {
                sessions.Add(MapSession(current, isCurrent: true));
                seen.Add(current.SessionId);
            }

            if (services.GetService<ISessionPersister>() is SqliteSessionPersister persister) {
                foreach (SessionMeta meta in SessionCatalog.List(persister.SessionsDirectory)) {
                    if (seen.Add(meta.SessionId))
                        sessions.Add(MapSession(meta, isCurrent: meta.SessionId == current?.SessionId));
                }
            }

            return Results.Ok(new {
                schema_version = SchemaVersion.Current,
                sessions = sessions.ToArray(),
            });
        });

        endpoints.MapGet("/api/v1/sessions/current", (SessionAggregator aggregator) => {
            SessionMeta? meta = aggregator.Meta;
            if (meta is null)
                return Results.NotFound(new { schema_version = SchemaVersion.Current, reason = "no active session" });

            return Results.Ok(new {
                schema_version = SchemaVersion.Current,
                session = MapSession(meta, isCurrent: true),
                receive = new {
                    total_batches = aggregator.TotalBatches,
                    total_samples = aggregator.TotalSamples,
                    total_bytes = aggregator.TotalBytes,
                    last_batch_utc = aggregator.LastBatchUtc == default ? (DateTime?)null : aggregator.LastBatchUtc,
                    section_count = aggregator.SectionCount,
                    total_gc_events = aggregator.TotalGcEvents,
                    total_allocations = aggregator.TotalAllocations,
                },
            });
        });

        endpoints.MapGet("/api/v1/sessions/current/summary", (SessionAggregator aggregator) => {
            SessionMeta? meta = aggregator.Meta;
            if (meta is null)
                return Results.NotFound(new { schema_version = SchemaVersion.Current, reason = "no active session" });

            long freq = meta.StopwatchFrequency > 0 ? meta.StopwatchFrequency : Stopwatch.Frequency;
            double nsPerTick = 1_000_000_000.0 / freq;
            long totalSectionTicks = 0;
            foreach (SectionStats section in aggregator.Sections)
                totalSectionTicks += section.TotalElapsedTicks;

            return Results.Ok(new {
                schema_version = SchemaVersion.Current,
                session = MapSession(meta, isCurrent: true),
                section_count = aggregator.SectionCount,
                metric_count = aggregator.MetricCount,
                total_batches = aggregator.TotalBatches,
                total_samples = aggregator.TotalSamples,
                total_bytes = aggregator.TotalBytes,
                total_gc_events = aggregator.TotalGcEvents,
                total_allocations = aggregator.TotalAllocations,
                total_metric_observations = aggregator.TotalMetricObservations,
                total_section_ns = (long)(totalSectionTicks * nsPerTick),
                last_batch_utc = aggregator.LastBatchUtc == default ? (DateTime?)null : aggregator.LastBatchUtc,
            });
        });

        endpoints.MapGet("/api/v1/sessions/current/sections", (SessionAggregator aggregator) => {
            long freq = aggregator.Meta?.StopwatchFrequency ?? Stopwatch.Frequency;
            double nsPerTick = 1_000_000_000.0 / freq;
            return Results.Ok(new {
                schema_version = SchemaVersion.Current,
                sections = aggregator.Sections.Select(s => new {
                    id = s.SectionId,
                    name = s.Name,
                    sample_count = s.SampleCount,
                    total_ns = (long)(s.TotalElapsedTicks * nsPerTick),
                    min_ns = s.MinElapsedTicks == long.MaxValue ? 0 : (long)(s.MinElapsedTicks * nsPerTick),
                    max_ns = (long)(s.MaxElapsedTicks * nsPerTick),
                }).ToArray(),
            });
        });

        endpoints.MapGet("/api/v1/sessions/current/hotspots", (SessionAggregator aggregator, int? limit) => {
            long freq = aggregator.Meta?.StopwatchFrequency ?? Stopwatch.Frequency;
            double nsPerTick = 1_000_000_000.0 / freq;
            int take = limit is int l && l > 0 ? Math.Min(l, MaxHotspotLimit) : DefaultHotspotLimit;
            return Results.Ok(new {
                schema_version = SchemaVersion.Current,
                hotspots = aggregator.Sections
                    .OrderByDescending(s => s.TotalElapsedTicks)
                    .Take(take)
                    .Select(s => new {
                        id = s.SectionId,
                        name = s.Name,
                        sample_count = s.SampleCount,
                        total_ns = (long)(s.TotalElapsedTicks * nsPerTick),
                        mean_ns = s.SampleCount == 0 ? 0 : (long)(s.TotalElapsedTicks * nsPerTick / s.SampleCount),
                        min_ns = s.MinElapsedTicks == long.MaxValue ? 0 : (long)(s.MinElapsedTicks * nsPerTick),
                        max_ns = (long)(s.MaxElapsedTicks * nsPerTick),
                    })
                    .ToArray(),
            });
        });

        endpoints.MapGet("/api/v1/sessions/current/gc", (SessionAggregator aggregator, int? limit) => {
            int take = limit is int l && l > 0 ? Math.Min(l, MaxGcEventLimit) : DefaultGcEventLimit;
            GcEventRecord[] snapshot = aggregator.SnapshotGcEvents(take);
            return Results.Ok(new {
                schema_version = SchemaVersion.Current,
                total_events = aggregator.TotalGcEvents,
                events = snapshot.Select(e => new {
                    generation = e.Generation,
                    pause_type = e.PauseType,
                    heap_before = e.HeapBefore,
                    heap_after = e.HeapAfter,
                    duration_micros = e.DurationMicros,
                    ticks = e.Ticks,
                    allocation_rate_bpm = e.AllocationRateBytesPerMinute,
                }).ToArray(),
            });
        });

        endpoints.MapGet("/api/v1/sessions/current/metrics", (SessionAggregator aggregator) => {
            return Results.Ok(new {
                schema_version = SchemaVersion.Current,
                total_observations = aggregator.TotalMetricObservations,
                metrics = aggregator.Metrics.Select(m => new {
                    id = m.MetricId,
                    name = m.Name,
                    kind = m.Kind,
                    unit = m.Unit,
                    labels = m.Labels.Values.Select(l => new {
                        canonical = l.Canonical,
                        latest_value = Interlocked.Read(ref l.LatestValue),
                        total_sample_count = Interlocked.Read(ref l.TotalSampleCount),
                    }).ToArray(),
                }).ToArray(),
            });
        });

        endpoints.MapGet("/api/v1/sessions/current/call_tree", (SessionAggregator aggregator, int? depth, int? top) => {
            long freq = aggregator.Meta?.StopwatchFrequency ?? Stopwatch.Frequency;
            double nsPerTick = 1_000_000_000.0 / freq;
            int depthCap = depth is int d && d > 0 ? Math.Min(d, MaxCallTreeDepth) : CallTreeBuilder.DefaultDepthCap;
            int topN = top is int t && t > 0 ? Math.Min(t, MaxCallTreeTopN) : CallTreeBuilder.DefaultTopN;

            Dictionary<int, string> names = aggregator.Sections.ToDictionary(s => s.SectionId, s => s.Name);
            IReadOnlyList<CallTreeNode> roots = CallTreeBuilder.Build(aggregator.CallEdges, names, nsPerTick, depthCap, topN);

            return Results.Ok(new {
                schema_version = SchemaVersion.Current,
                depth_cap = depthCap,
                top_n = topN,
                roots = roots.Select(MapCallNode).ToArray(),
            });
        });

        return endpoints;
    }

    private static object MapSession(SessionMeta meta, bool isCurrent) {
        return new {
            id = meta.SessionId,
            started_utc = new DateTime(meta.StartedUtcTicks, DateTimeKind.Utc),
            library_version = meta.LibraryVersion,
            game_version = meta.GameVersion,
            is_current = isCurrent,
        };
    }

    private static object MapCallNode(CallTreeNode node) {
        return new {
            id = node.SectionId,
            name = node.Name,
            call_count = node.CallCount,
            total_ns = node.TotalNs,
            is_other = node.IsOther,
            children = node.Children.Select(MapCallNode).ToArray(),
        };
    }
}
