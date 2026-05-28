export interface BundleManifest {
    schemaVersion: number;
    sessionId: string;
    createdUtc: string;
    collectorVersion: string;
    entries: string[];
}

export interface BundleData {
    manifest: BundleManifest;
    sessionSummary: Record<string, unknown>;
    metricDescriptors: Record<string, unknown>;
    hotspots: Record<string, unknown>;
    customMetrics: Record<string, unknown>;
    loadOrder: Record<string, unknown>;
    collectorHealth: Record<string, unknown>;
    allocations?: Record<string, unknown>;
    gcEvents?: Record<string, unknown>;
    callHierarchy?: Record<string, unknown>;
    patches?: Record<string, unknown>;
    hasAllocations: boolean;
    hasGcEvents: boolean;
    hasCallHierarchy: boolean;
    hasPatches: boolean;
}
