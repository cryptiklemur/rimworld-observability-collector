import { describe, expect, it } from 'vitest';
import { parseBundle } from './parsers';

describe('parseBundle', () => {
    it('returns null when raw is null', () => {
        expect(parseBundle(null)).toBeNull();
    });

    it('parses minimal manifest', () => {
        const raw = {
            manifest: {
                schema_version: 1,
                session_id: 'abc',
                created_utc: '2026-05-28T10:00:00Z',
                collector_version: '0.1.0',
                entries: ['session_summary.json'],
            },
            session_summary: { session_id: 'abc', section_count: 0 },
            metric_descriptors: { metrics: [] },
            hotspots: { hotspots: [] },
            custom_metrics: { metrics: [] },
            load_order: { mods: [] },
            collector_health: { uptime_seconds: 0 },
        };
        const parsed = parseBundle(raw);
        expect(parsed).not.toBeNull();
        expect(parsed!.manifest.sessionId).toBe('abc');
        expect(parsed!.hasAllocations).toBe(false);
        expect(parsed!.hasGcEvents).toBe(false);
    });

    it('flags optional sections when present', () => {
        const raw = {
            manifest: {
                schema_version: 1,
                session_id: 'x',
                created_utc: '2026-05-28T10:00:00Z',
                collector_version: '0.1.0',
                entries: [],
            },
            session_summary: {},
            metric_descriptors: { metrics: [] },
            hotspots: { hotspots: [] },
            custom_metrics: { metrics: [] },
            load_order: { mods: [] },
            collector_health: {},
            allocations: { allocations: [] },
            gc_events: { events: [] },
        };
        const parsed = parseBundle(raw)!;
        expect(parsed.hasAllocations).toBe(true);
        expect(parsed.hasGcEvents).toBe(true);
    });
});
