import { describe, it, expect, vi, afterEach } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/svelte';
import Sections from './Sections.svelte';

function mockSections(sections: unknown[]) {
    vi.stubGlobal(
        'fetch',
        vi.fn(async () => ({
            ok: true,
            status: 200,
            json: async () => ({ schema_version: 1, sections }),
        })),
    );
}

afterEach(() => vi.unstubAllGlobals());

describe('Sections route', () => {
    it('renders all sections and subsystem chip filters', async () => {
        mockSections([
            { id: 1, name: 'pawns.work.Tick', subsystem: 'pawns.work' },
            { id: 2, name: 'pawns.work.Jobs', subsystem: 'pawns.work' },
            { id: 3, name: 'core.Bar', subsystem: null },
        ]);
        render(Sections);

        expect(await screen.findByText('pawns.work.Tick')).toBeInTheDocument();
        expect(screen.getByText('pawns.work.Jobs')).toBeInTheDocument();
        expect(screen.getByText('core.Bar')).toBeInTheDocument();

        expect(screen.getByRole('button', { name: 'All' })).toBeInTheDocument();
        expect(screen.getByRole('button', { name: 'pawns.work' })).toBeInTheDocument();
        expect(screen.getByRole('button', { name: '(none)' })).toBeInTheDocument();
    });

    it('filters to a subsystem when its chip is clicked', async () => {
        mockSections([
            { id: 1, name: 'pawns.work.Tick', subsystem: 'pawns.work' },
            { id: 2, name: 'core.Bar', subsystem: null },
        ]);
        render(Sections);

        await screen.findByText('pawns.work.Tick');
        await fireEvent.click(screen.getByRole('button', { name: 'pawns.work' }));

        expect(screen.getByText('pawns.work.Tick')).toBeInTheDocument();
        expect(screen.queryByText('core.Bar')).not.toBeInTheDocument();
    });

    it('filters to null-subsystem sections when (none) chip is clicked', async () => {
        mockSections([
            { id: 1, name: 'pawns.work.Tick', subsystem: 'pawns.work' },
            { id: 2, name: 'core.Bar', subsystem: null },
        ]);
        render(Sections);

        await screen.findByText('pawns.work.Tick');
        await fireEvent.click(screen.getByRole('button', { name: '(none)' }));

        expect(screen.queryByText('pawns.work.Tick')).not.toBeInTheDocument();
        expect(screen.getByText('core.Bar')).toBeInTheDocument();
    });

    it('initializes filter from ?subsystem= URL param', async () => {
        window.history.replaceState({}, '', '/?subsystem=pawns.work');
        mockSections([
            { id: 1, name: 'pawns.work.Tick', subsystem: 'pawns.work' },
            { id: 2, name: 'core.Bar', subsystem: null },
        ]);
        render(Sections);

        await screen.findByText('pawns.work.Tick');
        expect(screen.getByText('pawns.work.Tick')).toBeInTheDocument();
        expect(screen.queryByText('core.Bar')).not.toBeInTheDocument();
    });

    it('updates URL when chip is clicked', async () => {
        window.history.replaceState({}, '', '/');
        mockSections([
            { id: 1, name: 'pawns.work.Tick', subsystem: 'pawns.work' },
            { id: 2, name: 'core.Bar', subsystem: null },
        ]);
        render(Sections);

        await screen.findByText('pawns.work.Tick');
        await fireEvent.click(screen.getByRole('button', { name: 'pawns.work' }));

        expect(new URLSearchParams(window.location.search).get('subsystem')).toBe('pawns.work');
    });

    it('selects All when ?subsystem= is absent', async () => {
        window.history.replaceState({}, '', '/');
        mockSections([
            { id: 1, name: 'pawns.work.Tick', subsystem: 'pawns.work' },
            { id: 2, name: 'core.Bar', subsystem: null },
        ]);
        render(Sections);

        await screen.findByText('pawns.work.Tick');
        expect(screen.getByText('core.Bar')).toBeInTheDocument();
        expect(screen.getByRole('button', { name: 'All' })).toHaveClass('active');
    });

    it('shows empty state when no sections are registered', async () => {
        mockSections([]);
        render(Sections);

        expect(await screen.findByText('No sections registered yet.')).toBeInTheDocument();
    });
});
