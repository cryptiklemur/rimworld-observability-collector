import { describe, it, expect, afterEach } from 'vitest';
import { t, getLang } from './i18n';

function setSearch(search: string) {
    window.history.replaceState({}, '', `/${search}`);
}

afterEach(() => setSearch(''));

describe('t', () => {
    it('returns the value for a known key', () => {
        expect(t('app.title')).toBe('RimWorld Observability');
    });

    it('falls back to the provided fallback for an unknown key', () => {
        expect(t('does.not.exist', 'fallback text')).toBe('fallback text');
    });

    it('falls back to the key itself when no fallback is given', () => {
        expect(t('totally.missing')).toBe('totally.missing');
    });
});

describe('getLang', () => {
    it('defaults to en with no query param', () => {
        setSearch('');
        expect(getLang()).toBe('en');
    });

    it('honours a valid ?lang= override', () => {
        setSearch('?lang=en');
        expect(getLang()).toBe('en');
    });

    it('ignores an unknown ?lang= and falls back to en', () => {
        setSearch('?lang=zz');
        expect(getLang()).toBe('en');
    });
});
