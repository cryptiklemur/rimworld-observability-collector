import { describe, it, expect, vi, afterEach } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/svelte';
import Hotspots from './Hotspots.svelte';

function mockApi() {
    vi.stubGlobal(
        'fetch',
        vi.fn(async (url: string) => {
            if (typeof url === 'string' && url.includes('/timeseries')) {
                // Empty points hit the no-data branch, avoiding a real uPlot/canvas
                // render which jsdom cannot back (matches the Memory chart test).
                return {
                    ok: true,
                    status: 200,
                    json: async () => ({
                        schema_version: 1,
                        id: 1,
                        name: 'Alpha',
                        bucket_seconds: 1,
                        points: [],
                    }),
                };
            }
            return {
                ok: true,
                status: 200,
                json: async () => ({
                    schema_version: 1,
                    hotspots: [
                        {
                            id: 1,
                            name: 'Alpha',
                            sample_count: 10,
                            total_ns: 1000,
                            mean_ns: 100,
                            min_ns: 50,
                            max_ns: 200,
                            p50_ns: 95,
                            p95_ns: 180,
                            p99_ns: 195,
                        },
                        {
                            id: 2,
                            name: 'Beta',
                            sample_count: 5,
                            total_ns: 250,
                            mean_ns: 50,
                            min_ns: 10,
                            max_ns: 80,
                            p50_ns: 48,
                            p95_ns: 75,
                            p99_ns: 79,
                        },
                    ],
                }),
            };
        }),
    );
}

afterEach(() => vi.unstubAllGlobals());

describe('Hotspots route', () => {
    it('renders a row per hotspot and grades the share bar by total share', async () => {
        mockApi();
        const { container } = render(Hotspots);

        expect(await screen.findByText('Alpha')).toBeInTheDocument();
        expect(screen.getByText('Beta')).toBeInTheDocument();

        // Alpha holds the max total_ns -> share 1.0 -> top grade
        expect(container.querySelector('.fill.g4')).not.toBeNull();
        // Beta share 0.25 -> grade 3
        expect(container.querySelector('.fill.g3')).not.toBeNull();
    });

    it('expands a clicked row and renders its trend panel', async () => {
        mockApi();
        const { container } = render(Hotspots);

        const firstRow = await screen.findByText('Alpha');
        const button = firstRow.closest('button');
        expect(button).not.toBeNull();

        await fireEvent.click(button!);

        // min/max move into the expanded detail panel
        expect(await screen.findByText(/min 50 ns/)).toBeInTheDocument();
        expect(button!.getAttribute('aria-expanded')).toBe('true');
        expect(container.querySelector('.trend')).not.toBeNull();
    });
});
