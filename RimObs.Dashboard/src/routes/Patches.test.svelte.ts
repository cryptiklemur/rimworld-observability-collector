import { describe, it, expect, vi, afterEach } from 'vitest';
import { render, screen } from '@testing-library/svelte';
import Patches from './Patches.svelte';

function mockPatches(conflicts: unknown[]) {
    vi.stubGlobal(
        'fetch',
        vi.fn(async () => ({
            ok: true,
            status: 200,
            json: async () => ({ schema_version: 1, conflicts }),
        })),
    );
}

afterEach(() => vi.unstubAllGlobals());

describe('Patches route', () => {
    it('groups conflicts by section and renders owner, type and priority', async () => {
        mockPatches([
            {
                section: 'core.tick',
                target_method: 'Verse.TickManager:DoSingleTick',
                other_owner: 'Dubs.PerformanceAnalyzer',
                patch_type: 1,
                priority: 400,
                patch_method: 'Dubs.Patch:Prefix',
            },
            {
                section: 'core.tick',
                target_method: 'Verse.TickManager:DoSingleTick',
                other_owner: 'Some.OtherMod',
                patch_type: 3,
                priority: 0,
                patch_method: 'Some.Patch:Transpiler',
            },
        ]);
        const { container } = render(Patches);

        expect(await screen.findByText('Dubs.PerformanceAnalyzer')).toBeInTheDocument();
        expect(screen.getByText('Some.OtherMod')).toBeInTheDocument();
        // One group header for the single shared section.
        expect(container.querySelectorAll('.group')).toHaveLength(1);
        // Patch-type badges resolve to readable labels.
        expect(screen.getByText('prefix')).toBeInTheDocument();
        expect(screen.getByText('transpiler')).toBeInTheDocument();
    });

    it('shows the positive empty state when there are no conflicts', async () => {
        mockPatches([]);
        render(Patches);

        expect(await screen.findByText('No conflicting patches')).toBeInTheDocument();
    });
});
