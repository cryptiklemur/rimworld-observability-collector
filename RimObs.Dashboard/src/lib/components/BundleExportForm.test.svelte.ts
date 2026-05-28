import { render, fireEvent } from '@testing-library/svelte';
import { describe, expect, it, vi } from 'vitest';
import BundleExportForm from './BundleExportForm.svelte';

describe('BundleExportForm', () => {
    it('renders include toggles for each optional content key', () => {
        const { getByLabelText } = render(BundleExportForm, {
            sessionId: 'sess-1',
            onExport: vi.fn(),
        });
        expect(getByLabelText(/Allocations/i)).toBeTruthy();
        expect(getByLabelText(/GC events/i)).toBeTruthy();
        expect(getByLabelText(/Call hierarchy/i)).toBeTruthy();
        expect(getByLabelText(/Patches/i)).toBeTruthy();
        expect(getByLabelText(/Metrics SQLite/i)).toBeTruthy();
    });

    it('calls onExport with selected includes', async () => {
        const handler = vi.fn();
        const { getByLabelText, getByRole } = render(BundleExportForm, {
            sessionId: 'sess-1',
            onExport: handler,
        });

        await fireEvent.click(getByLabelText(/Allocations/i));
        await fireEvent.click(getByLabelText(/GC events/i));
        await fireEvent.click(getByRole('button', { name: /Download bundle/i }));

        expect(handler).toHaveBeenCalledOnce();
        const call = handler.mock.calls[0][0];
        expect(call.includes).toEqual(expect.arrayContaining(['allocations', 'gc-events']));
        expect(call.sessionId).toBe('sess-1');
        expect(call.force).toBe(false);
    });

    it('toggles force flag when over-cap override is checked', async () => {
        const handler = vi.fn();
        const { getByLabelText, getByRole } = render(BundleExportForm, {
            sessionId: 'sess-1',
            onExport: handler,
        });
        await fireEvent.click(getByLabelText(/Export anyway/i));
        await fireEvent.click(getByRole('button', { name: /Download bundle/i }));
        expect(handler.mock.calls[0][0].force).toBe(true);
    });

    it('disables the button and shows a busy label while the export is in flight', async () => {
        let resolveExport!: () => void;
        const pending = new Promise<void>((resolve) => {
            resolveExport = resolve;
        });
        const handler = vi.fn(() => pending);
        const { getByRole } = render(BundleExportForm, {
            sessionId: 'sess-1',
            onExport: handler,
        });

        await fireEvent.click(getByRole('button', { name: /Download bundle/i }));

        const busy = getByRole('button', { name: /Preparing/i }) as HTMLButtonElement;
        expect(busy.disabled).toBe(true);

        resolveExport();
        await pending;
    });
});
