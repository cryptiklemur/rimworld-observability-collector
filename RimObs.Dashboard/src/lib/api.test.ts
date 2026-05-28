import { describe, it, expect, vi, afterEach } from 'vitest';
import { api, ApiError } from './api';

function mockFetch(
    body: unknown,
    init: { ok?: boolean; status?: number; statusText?: string } = {},
) {
    const ok = init.ok ?? true;
    const fetchMock = vi.fn().mockResolvedValue({
        ok,
        status: init.status ?? (ok ? 200 : 500),
        statusText: init.statusText ?? (ok ? 'OK' : 'Server Error'),
        json: async () => body,
    });
    vi.stubGlobal('fetch', fetchMock);
    return fetchMock;
}

afterEach(() => vi.unstubAllGlobals());

describe('api endpoint URLs', () => {
    it('builds the status URL', async () => {
        const f = mockFetch({ status: 'running' });
        await api.status();
        expect(f).toHaveBeenCalledWith(
            '/api/v1/status',
            expect.objectContaining({ headers: { accept: 'application/json' } }),
        );
    });

    it('passes the hotspots limit', async () => {
        const f = mockFetch({ hotspots: [] });
        await api.hotspots(25);
        expect(f.mock.calls[0][0]).toBe('/api/v1/sessions/current/hotspots?limit=25');
    });

    it('defaults the hotspots limit to 50', async () => {
        const f = mockFetch({ hotspots: [] });
        await api.hotspots();
        expect(f.mock.calls[0][0]).toBe('/api/v1/sessions/current/hotspots?limit=50');
    });

    it('builds the call tree URL with depth and top', async () => {
        const f = mockFetch({ roots: [] });
        await api.callTree(8, 12);
        expect(f.mock.calls[0][0]).toBe('/api/v1/sessions/current/call_tree?depth=8&top=12');
    });

    it('omits the level param when not provided', async () => {
        const f = mockFetch({ entries: [] });
        await api.logs();
        expect(f.mock.calls[0][0]).toBe('/api/v1/logs?limit=200');
    });

    it('encodes the level param when provided', async () => {
        const f = mockFetch({ entries: [] });
        await api.logs(10, 'Warning');
        expect(f.mock.calls[0][0]).toBe('/api/v1/logs?limit=10&level=Warning');
    });

    it('builds the sessions list URL', async () => {
        const f = mockFetch({ sessions: [] });
        await api.sessions();
        expect(f.mock.calls[0][0]).toBe('/api/v1/sessions');
    });

    it('builds the current session URL', async () => {
        const f = mockFetch({ session: {}, receive: {} });
        await api.currentSession();
        expect(f.mock.calls[0][0]).toBe('/api/v1/sessions/current');
    });

    it('builds the session summary URL', async () => {
        const f = mockFetch({ session: {} });
        await api.sessionSummary();
        expect(f.mock.calls[0][0]).toBe('/api/v1/sessions/current/summary');
    });

    it('builds the patches URL', async () => {
        const f = mockFetch({ conflicts: [] });
        await api.patches();
        expect(f.mock.calls[0][0]).toBe('/api/v1/sessions/current/patches');
    });
});

describe('error handling', () => {
    it('throws ApiError with status on non-ok response', async () => {
        mockFetch(null, { ok: false, status: 404, statusText: 'Not Found' });
        await expect(api.status()).rejects.toBeInstanceOf(ApiError);
        await expect(api.status()).rejects.toMatchObject({ status: 404 });
    });

    it('returns parsed json on success', async () => {
        mockFetch({ status: 'running', version: '1.2.3' });
        const result = await api.status();
        expect(result).toMatchObject({ status: 'running', version: '1.2.3' });
    });
});

describe('allSections', () => {
    it('calls /api/v1/sections and returns parsed registry sections', async () => {
        const f = mockFetch({
            schema_version: 1,
            sections: [
                { id: 1, name: 'pawns.work.Tick', subsystem: 'pawns.work' },
                { id: 2, name: 'foo.Bar', subsystem: null },
            ],
        });
        const result = await api.allSections();
        expect(f.mock.calls[0][0]).toBe('/api/v1/sections');
        expect(result.sections).toHaveLength(2);
        expect(result.sections[0].subsystem).toBe('pawns.work');
        expect(result.sections[1].subsystem).toBeNull();
    });
});

describe('ApiError', () => {
    it('carries status and name', () => {
        const err = new ApiError(503, 'down');
        expect(err.status).toBe(503);
        expect(err.name).toBe('ApiError');
        expect(err.message).toBe('down');
        expect(err).toBeInstanceOf(Error);
    });
});

describe('api.exportBundle', () => {
    it('POSTs to /api/v1/export/bundle and returns Blob', async () => {
        const blob = new Blob(['zipcontent'], { type: 'application/zip' });
        const fetchMock = vi.fn(
            async () => new Response(blob, { status: 200, headers: { 'Content-Type': 'application/zip' } }),
        );
        vi.stubGlobal('fetch', fetchMock);

        const result = await api.exportBundle({ sessionId: 'sess', includes: ['allocations'], force: false });

        expect(fetchMock).toHaveBeenCalledOnce();
        const [url, init] = fetchMock.mock.calls[0];
        expect(url).toBe('/api/v1/export/bundle');
        expect((init as RequestInit).method).toBe('POST');
        expect(JSON.parse((init as RequestInit).body as string)).toEqual({
            session_id: 'sess', include: ['allocations'], force: false,
        });
        expect(result.kind).toBe('ok');
        if (result.kind === 'ok') expect(result.blob.type).toBe('application/zip');
    });

    it('returns over_cap on 413', async () => {
        const body = JSON.stringify({ error: 'estimate_exceeds_soft_cap', estimated_bytes: 30_000_000, cap_bytes: 26_214_400 });
        vi.stubGlobal('fetch', vi.fn(
            async () => new Response(body, { status: 413, headers: { 'Content-Type': 'application/json' } }),
        ));

        const result = await api.exportBundle({ sessionId: 'sess', includes: [], force: false });

        expect(result.kind).toBe('over_cap');
        if (result.kind === 'over_cap') expect(result.estimatedBytes).toBe(30_000_000);
    });
});
