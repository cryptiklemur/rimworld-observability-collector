import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen } from '@testing-library/svelte';
import App from './App.svelte';

beforeEach(() => {
    vi.stubGlobal(
        'fetch',
        vi.fn(async () => ({ ok: true, status: 200, json: async () => ({}) })),
    );
});

afterEach(() => {
    vi.unstubAllGlobals();
    globalThis.location.hash = '';
});

describe('App route dispatch', () => {
    it('renders the Logs route for #/logs', async () => {
        globalThis.location.hash = '#/logs';
        render(App);
        expect(await screen.findByText('Warning')).toBeInTheDocument();
    });
});
