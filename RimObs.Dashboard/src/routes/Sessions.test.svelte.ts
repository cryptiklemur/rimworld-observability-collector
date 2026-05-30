import { describe, it, expect, vi, afterEach } from 'vitest';
import { render, screen } from '@testing-library/svelte';
import Sessions from './Sessions.svelte';

function mockSessions() {
    vi.stubGlobal(
        'fetch',
        vi.fn(async (url: string) => {
            if (url === '/api/v1/sessions/current/summary') {
                return {
                    ok: true,
                    status: 200,
                    json: async () => ({
                        schema_version: 1,
                        session: {
                            id: 'cur-1',
                            started_utc: new Date().toISOString(),
                            library_version: '1.2.3',
                            game_version: '1.6',
                            is_current: true,
                        },
                        section_count: 4,
                        metric_count: 2,
                        total_batches: 10,
                        total_samples: 99,
                        total_bytes: 4096,
                        total_gc_events: 3,
                        total_allocations: 7,
                        total_metric_observations: 12,
                        total_section_ns: 123456,
                        last_batch_utc: new Date().toISOString(),
                    }),
                };
            }
            return {
                ok: true,
                status: 200,
                json: async () => ({
                    schema_version: 1,
                    sessions: [
                        {
                            id: 'cur-1',
                            started_utc: new Date().toISOString(),
                            library_version: '1.2.3',
                            game_version: '1.6',
                            is_current: true,
                        },
                        {
                            id: 'old-9',
                            started_utc: new Date(Date.now() - 3_600_000).toISOString(),
                            library_version: '1.0.0',
                            game_version: '1.6',
                            is_current: false,
                        },
                    ],
                }),
            };
        }),
    );
}

afterEach(() => vi.unstubAllGlobals());

describe('Sessions route', () => {
    it('lists every session and flags the current one', async () => {
        mockSessions();
        const { container } = render(Sessions);

        expect(await screen.findByText('old-9')).toBeInTheDocument();
        expect(screen.getAllByText('cur-1').length).toBeGreaterThan(0);
        expect(container.querySelector('.rowline.current')).not.toBeNull();
        expect(screen.getByText('current')).toBeInTheDocument();
    });

    it('shows "Export bundle" action for the current session row', async () => {
        mockSessions();
        const { findAllByRole } = render(Sessions);
        const buttons = await findAllByRole('button', { name: /Export bundle/i });
        const enabled = (buttons as HTMLButtonElement[]).filter((b) => !b.disabled);
        expect(enabled).toHaveLength(1);
    });
});
