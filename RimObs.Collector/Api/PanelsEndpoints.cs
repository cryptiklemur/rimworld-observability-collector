using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Cryptiklemur.RimObs.Collector.Config;
using Cryptiklemur.RimObs.Collector.Panels;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Cryptiklemur.RimObs.Collector.Api;

public static class PanelsEndpoints {
    public static IEndpointRouteBuilder MapPanelsEndpoints(this IEndpointRouteBuilder endpoints) {
        endpoints.MapGet("/api/v1/panels", (PanelRegistry registry) => {
            List<object> owners = new();
            foreach (KeyValuePair<string, PanelDefinition[]> owner in registry.Snapshot()) {
                List<object> panels = new(owner.Value.Length);
                foreach (PanelDefinition panel in owner.Value) {
                    List<object> layout = new(panel.Layout.Count);
                    foreach (PanelLayoutItem item in panel.Layout) {
                        layout.Add(new { metric = item.Metric, widget = item.Widget });
                    }

                    panels.Add(new { id = panel.Id, title = panel.Title, icon = panel.Icon, layout = layout.ToArray() });
                }

                owners.Add(new { owner_id = owner.Key, panels = panels.ToArray() });
            }

            return Results.Ok(new { schema_version = PanelRegistry.SchemaVersion, owners = owners.ToArray() });
        });

        endpoints.MapPost("/api/v1/panels/register", async (HttpContext context, PanelRegistry registry) => {
            PanelRegistration? registration;
            try {
                registration = await context.Request.ReadFromJsonAsync<PanelRegistration>(ConfigJson.Options);
            }
            catch (JsonException) {
                return Results.BadRequest(new { schema_version = PanelRegistry.SchemaVersion, reason = "malformed panel registration" });
            }

            if (registration is null) {
                return Results.BadRequest(new { schema_version = PanelRegistry.SchemaVersion, reason = "empty registration body" });
            }

            if (registration.SchemaVersion != PanelRegistry.SchemaVersion) {
                return Results.BadRequest(new {
                    schema_version = PanelRegistry.SchemaVersion,
                    reason = $"unsupported schema_version {registration.SchemaVersion}",
                });
            }

            if (string.IsNullOrWhiteSpace(registration.OwnerId)) {
                return Results.BadRequest(new { schema_version = PanelRegistry.SchemaVersion, reason = "owner_id is required" });
            }

            foreach (PanelDefinition panel in registration.Panels) {
                foreach (PanelLayoutItem item in panel.Layout) {
                    if (!PanelWidgets.IsValid(item.Widget)) {
                        return Results.BadRequest(new {
                            schema_version = PanelRegistry.SchemaVersion,
                            reason = $"unknown widget '{item.Widget}'",
                        });
                    }
                }
            }

            registry.Replace(registration.OwnerId, registration.Panels.ToArray());
            return Results.Ok(new {
                schema_version = PanelRegistry.SchemaVersion,
                owner_id = registration.OwnerId,
                panel_count = registration.Panels.Count,
            });
        });

        endpoints.MapGet("/api/v1/panels/refresh_requested", (PanelRegistry registry, ConfigStore config) =>
            RefreshResult(registry, config));

        endpoints.MapPost("/api/v1/panels/refresh_requested", (PanelRegistry registry, ConfigStore config) => {
            registry.RequestRefresh();
            return RefreshResult(registry, config);
        });

        return endpoints;
    }

    private static IResult RefreshResult(PanelRegistry registry, ConfigStore config) {
        RefreshFlagState state = registry.RefreshState(config.Current.Panels.RefreshFlagTtlSeconds);
        return Results.Ok(new {
            schema_version = PanelRegistry.SchemaVersion,
            refresh_requested = state.Requested,
            ttl_seconds_remaining = state.RemainingSeconds,
        });
    }
}
