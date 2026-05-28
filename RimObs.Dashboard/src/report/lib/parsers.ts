import type { BundleData, BundleManifest } from './types';

function readRecord(source: unknown, key: string): Record<string, unknown> | undefined {
    if (typeof source !== 'object' || source === null) return undefined;
    const value = (source as Record<string, unknown>)[key];
    if (typeof value !== 'object' || value === null) return undefined;
    return value as Record<string, unknown>;
}

function parseManifest(source: unknown): BundleManifest {
    const obj = readRecord(source, 'manifest') ?? {};
    return {
        schemaVersion: typeof obj.schema_version === 'number' ? obj.schema_version : 0,
        sessionId: typeof obj.session_id === 'string' ? obj.session_id : '',
        createdUtc: typeof obj.created_utc === 'string' ? obj.created_utc : '',
        collectorVersion: typeof obj.collector_version === 'string' ? obj.collector_version : '',
        entries: Array.isArray(obj.entries) ? (obj.entries as string[]) : [],
    };
}

export function parseBundle(raw: unknown): BundleData | null {
    if (raw === null || typeof raw !== 'object') return null;

    const manifest = parseManifest(raw);
    const allocations = readRecord(raw, 'allocations');
    const gcEvents = readRecord(raw, 'gc_events');
    const callHierarchy = readRecord(raw, 'call_hierarchy');
    const patches = readRecord(raw, 'patches');

    return {
        manifest,
        sessionSummary: readRecord(raw, 'session_summary') ?? {},
        metricDescriptors: readRecord(raw, 'metric_descriptors') ?? {},
        hotspots: readRecord(raw, 'hotspots') ?? {},
        customMetrics: readRecord(raw, 'custom_metrics') ?? {},
        loadOrder: readRecord(raw, 'load_order') ?? {},
        collectorHealth: readRecord(raw, 'collector_health') ?? {},
        allocations,
        gcEvents,
        callHierarchy,
        patches,
        hasAllocations: allocations !== undefined,
        hasGcEvents: gcEvents !== undefined,
        hasCallHierarchy: callHierarchy !== undefined,
        hasPatches: patches !== undefined,
    };
}
