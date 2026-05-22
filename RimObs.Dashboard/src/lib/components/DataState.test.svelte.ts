import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/svelte';
import { createRawSnippet } from 'svelte';
import DataState from './DataState.svelte';

const children = createRawSnippet(() => ({
    render: () => `<div data-testid="content">loaded</div>`,
}));

describe('DataState', () => {
    it('hides children while loading', () => {
        render(DataState, { state: 'loading', children });
        expect(screen.queryByTestId('content')).toBeNull();
    });

    it('renders children when ok and not empty', () => {
        render(DataState, { state: 'ok', empty: false, children });
        expect(screen.getByTestId('content')).toBeInTheDocument();
    });

    it('hides children and shows the empty branch when empty', () => {
        render(DataState, { state: 'ok', empty: true, children });
        expect(screen.queryByTestId('content')).toBeNull();
    });

    it('shows the error text and fires onretry on the error branch', async () => {
        const onretry = vi.fn();
        render(DataState, { state: 'error', error: 'boom', onretry, children });
        expect(screen.queryByTestId('content')).toBeNull();
        expect(screen.getByText('boom')).toBeInTheDocument();
        await fireEvent.click(screen.getByRole('button'));
        expect(onretry).toHaveBeenCalledOnce();
    });
});
