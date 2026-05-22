import { describe, it, expect, vi, afterEach } from 'vitest';
import { render, screen } from '@testing-library/svelte';
import Hotspots from './Hotspots.svelte';

function mockHotspots() {
    vi.stubGlobal(
        'fetch',
        vi.fn(async () => ({
            ok: true,
            status: 200,
            json: async () => ({
                schema_version: 1,
                hotspots: [
                    { id: 1, name: 'Alpha', sample_count: 10, total_ns: 1000, mean_ns: 100, min_ns: 50, max_ns: 200 },
                    { id: 2, name: 'Beta', sample_count: 5, total_ns: 250, mean_ns: 50, min_ns: 10, max_ns: 80 },
                ],
            }),
        })),
    );
}

afterEach(() => vi.unstubAllGlobals());

describe('Hotspots route', () => {
    it('renders a row per hotspot and grades the share bar by total share', async () => {
        mockHotspots();
        const { container } = render(Hotspots);

        expect(await screen.findByText('Alpha')).toBeInTheDocument();
        expect(screen.getByText('Beta')).toBeInTheDocument();

        // Alpha holds the max total_ns -> share 1.0 -> top grade
        expect(container.querySelector('.fill.g4')).not.toBeNull();
        // Beta share 0.25 -> grade 3
        expect(container.querySelector('.fill.g3')).not.toBeNull();
    });
});
