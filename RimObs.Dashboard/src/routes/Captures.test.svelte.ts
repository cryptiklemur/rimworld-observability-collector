import { describe, it, expect, vi, afterEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/svelte';
import Captures from './Captures.svelte';

afterEach(() => vi.unstubAllGlobals());

const RUNNING_CAPTURE = {
    id: 'cap-1',
    session_id: 'sess-1',
    trigger: 'slow_tick',
    status: 'running',
    started_utc: new Date().toISOString(),
    stopped_utc: null,
    finalize_reason: 'none',
    edge_count: 3,
    estimated_bytes: 144,
    dropped_samples: 2,
    warning: null,
    roots: [
        {
            id: 1,
            name: 'TickManager.DoSingleTick',
            call_count: 1,
            total_ns: 20_000_000,
            is_other: false,
            children: [],
        },
    ],
};

function mockApi(captures: unknown[], activeId: string | null) {
    const startCalls: string[] = [];
    const stopCalls: string[] = [];
    vi.stubGlobal(
        'fetch',
        vi.fn(async (url: string, init?: { method?: string }) => {
            if (typeof url === 'string' && url.includes('/captures/start')) {
                startCalls.push(url);
                return { ok: true, status: 200, json: async () => ({ schema_version: 2 }) };
            }
            if (typeof url === 'string' && url.includes('/captures/stop')) {
                stopCalls.push(url);
                return { ok: true, status: 200, json: async () => ({ schema_version: 2 }) };
            }
            if (typeof url === 'string' && url.includes('/captures')) {
                return {
                    ok: true,
                    status: 200,
                    json: async () => ({
                        schema_version: 2,
                        active_capture_id: activeId,
                        captures,
                    }),
                };
            }
            return { ok: true, status: 200, json: async () => ({}) };
        }),
    );
    return { startCalls, stopCalls };
}

describe('Captures route', () => {
    it('shows the start control and no captures when empty', async () => {
        mockApi([], null);
        render(Captures);
        expect(await screen.findByRole('button', { name: /start capture/i })).toBeInTheDocument();
    });

    it('renders a running capture with its tree and shows the stop control', async () => {
        mockApi([RUNNING_CAPTURE], 'cap-1');
        render(Captures);
        await waitFor(() => {
            expect(screen.getByRole('button', { name: /stop capture/i })).toBeInTheDocument();
        });
        expect(screen.getByText(/TickManager.DoSingleTick/)).toBeInTheDocument();
        expect(screen.getByText(/Slow tick/)).toBeInTheDocument();
    });

    it('surfaces dropped samples for a capped capture', async () => {
        mockApi([RUNNING_CAPTURE], 'cap-1');
        render(Captures);
        await waitFor(() => {
            expect(screen.getByText(/dropped/i)).toBeInTheDocument();
        });
    });

    it('posts to the stop endpoint when stop is clicked', async () => {
        const { stopCalls } = mockApi([RUNNING_CAPTURE], 'cap-1');
        render(Captures);
        const stopBtn = await screen.findByRole('button', { name: /stop capture/i });
        await fireEvent.click(stopBtn);
        await waitFor(() => expect(stopCalls.length).toBeGreaterThan(0));
    });
});
