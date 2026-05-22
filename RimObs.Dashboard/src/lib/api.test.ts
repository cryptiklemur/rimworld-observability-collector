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

describe('ApiError', () => {
    it('carries status and name', () => {
        const err = new ApiError(503, 'down');
        expect(err.status).toBe(503);
        expect(err.name).toBe('ApiError');
        expect(err.message).toBe('down');
        expect(err).toBeInstanceOf(Error);
    });
});
