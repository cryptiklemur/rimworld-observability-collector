# Prometheus export

Feed RimObs metrics into an existing Prometheus and Grafana stack.

**Status: planned -- not yet implemented.**

The configuration schema already reserves two keys for this feature:

- `Exporters.PrometheusEnabled` (default `false`)
- `Exporters.PrometheusPort` (default `7879`)

When implemented, the exporter will expose a `/metrics` endpoint on a dedicated port serving the standard Prometheus text exposition format. Counters, gauges, and histograms collected during a game session will be mapped to their Prometheus equivalents, with RimObs labels passed through as Prometheus labels. Cardinality limits that apply in [Metrics API](Metrics-API) will still be enforced.

The authoritative design is in PRD section 17. Implementation is deferred to a future release.

## Related

- [Metrics API](Metrics-API)
- [Configuration](Configuration)
