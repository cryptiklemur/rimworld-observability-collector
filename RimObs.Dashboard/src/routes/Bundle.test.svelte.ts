import { describe, it, expect, vi, afterEach } from 'vitest';
import { render, screen, fireEvent, within } from '@testing-library/svelte';
import Bundle from './Bundle.svelte';

const MANIFEST = {
    schema_version: 1,
    session_id: 'demo-session-2026-05-27',
    created_utc: '2026-05-28T11:02:00Z',
    collector_version: '0.4.0',
};
const CONTENTS = ['manifest.json', 'report.html', 'batches.msgpack'];

function mockFetch(opts: { importOk?: boolean } = {}) {
    const importOk = opts.importOk ?? true;
    const deleteSpy = vi.fn();
    const fetchMock = vi.fn(async (url: string, init?: RequestInit) => {
        const method = init?.method ?? 'GET';
        if (url === '/api/v1/sessions/current') {
            return { ok: true, status: 200, json: async () => ({ session: null }) } as Response;
        }
        if (url === '/api/v1/import/bundle' && method === 'POST') {
            if (!importOk) {
                return {
                    ok: false,
                    status: 400,
                    statusText: 'Bad Request',
                    json: async () => ({}),
                } as Response;
            }
            return {
                ok: true,
                status: 200,
                json: async () => ({ token: 'tok-1', manifest: MANIFEST, contents: CONTENTS }),
            } as Response;
        }
        if (method === 'DELETE') {
            deleteSpy(url);
            return { ok: true, status: 200, json: async () => ({}) } as Response;
        }
        return { ok: true, status: 200, json: async () => ({}) } as Response;
    });
    vi.stubGlobal('fetch', fetchMock);
    return { deleteSpy };
}

afterEach(() => vi.unstubAllGlobals());

function dropFile(container: HTMLElement) {
    const input = container.querySelector('input[type="file"]') as HTMLInputElement;
    const file = new File(['zip'], 'bundle.rimobs.zip', { type: 'application/zip' });
    return fireEvent.change(input, { target: { files: [file] } });
}

describe('Bundle route', () => {
    it('surfaces an error when the bundle fails to import', async () => {
        mockFetch({ importOk: false });
        const { container } = render(Bundle);

        await dropFile(container);

        const alert = await screen.findByRole('alert');
        expect(alert).toHaveTextContent(/Couldn't read that bundle/i);
    });

    it('requires confirmation before discarding a loaded bundle', async () => {
        const { deleteSpy } = mockFetch({ importOk: true });
        const { container } = render(Bundle);

        await dropFile(container);
        await screen.findByText('demo-session-2026-05-27');

        await fireEvent.click(screen.getByRole('button', { name: 'Discard' }));
        expect(deleteSpy).not.toHaveBeenCalled();
        expect(screen.getByText(/Discard this bundle\?/i)).toBeInTheDocument();

        await fireEvent.click(screen.getByRole('button', { name: 'Discard' }));
        await screen.findByText(/Drop a .rimobs.zip bundle here/i);
        expect(deleteSpy).toHaveBeenCalledWith('/api/v1/import/bundle/tok-1');
    });

    it('cancelling the discard keeps the loaded bundle', async () => {
        const { deleteSpy } = mockFetch({ importOk: true });
        const { container } = render(Bundle);

        await dropFile(container);
        await screen.findByText('demo-session-2026-05-27');

        await fireEvent.click(screen.getByRole('button', { name: 'Discard' }));
        await fireEvent.click(screen.getByRole('button', { name: 'Cancel' }));

        expect(deleteSpy).not.toHaveBeenCalled();
        expect(screen.getByText('demo-session-2026-05-27')).toBeInTheDocument();
    });

    it('exposes the full manifest value via title for truncated cells', async () => {
        mockFetch({ importOk: true });
        const { container } = render(Bundle);

        await dropFile(container);
        await screen.findByText('demo-session-2026-05-27');

        expect(container.querySelector('.v[title="demo-session-2026-05-27"]')).not.toBeNull();
    });

    it('moves focus into the report modal when opened', async () => {
        mockFetch({ importOk: true });
        const { container } = render(Bundle);

        await dropFile(container);
        await screen.findByText('demo-session-2026-05-27');

        await fireEvent.click(screen.getByRole('button', { name: /Open report/i }));
        const dialog = await screen.findByRole('dialog');
        const closeBtn = within(dialog).getByRole('button', { name: /Close/i });
        expect(document.activeElement).toBe(closeBtn);
    });
});
