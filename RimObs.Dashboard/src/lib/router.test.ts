import { describe, it, expect } from 'vitest';
import { routes } from './router.svelte';

describe('routes', () => {
    it('starts with overview as the first route', () => {
        expect(routes[0].id).toBe('overview');
    });

    it('has unique ids', () => {
        const ids = routes.map((r) => r.id);
        expect(new Set(ids).size).toBe(ids.length);
    });

    it('gives every route a title and an icon', () => {
        for (const route of routes) {
            expect(route.title.length).toBeGreaterThan(0);
            expect(route.icon.length).toBeGreaterThan(0);
        }
    });
});
