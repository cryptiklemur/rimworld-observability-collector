import { describe, it, expect, afterEach } from 'vitest';
import { t, getLang, LANGUAGES } from './i18n';
import { userPrefs } from './userPrefs.svelte';

function setSearch(search: string) {
    window.history.replaceState({}, '', `/${search}`);
}

afterEach(() => {
    setSearch('');
    userPrefs.setLang('');
});

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

    it('returns the translated value for the active language', () => {
        userPrefs.setLang('fr');
        expect(t('common.retry')).toBe('Réessayer');
    });

    it('falls back to English when a key is missing in the active language', () => {
        userPrefs.setLang('fr');
        expect(t('totally.missing', 'fallback')).toBe('fallback');
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

    it('honours a registered ?lang= for an added language', () => {
        setSearch('?lang=fr');
        expect(getLang()).toBe('fr');
    });

    it('prefers a persisted userPrefs language over the query param', () => {
        setSearch('?lang=fr');
        userPrefs.setLang('de');
        expect(getLang()).toBe('de');
    });

    it('ignores an unknown persisted language and falls back', () => {
        userPrefs.setLang('zz');
        expect(getLang()).toBe('en');
    });

    it('registers all four added languages plus English', () => {
        expect(LANGUAGES.map((l) => l.code)).toEqual(['en', 'zh', 'fr', 'es', 'de']);
    });
});
