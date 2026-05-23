import { describe, it, expect } from 'vitest';
import { cssVar, resolveColor } from './theme';

describe('theme', () => {
    it('reads a css custom property off the element', () => {
        const el = document.createElement('div');
        el.style.setProperty('--accent', '#ff0066');
        document.body.appendChild(el);
        expect(cssVar(el, '--accent')).toBe('#ff0066');
        el.remove();
    });

    it('returns an empty string for an unset property', () => {
        const el = document.createElement('div');
        document.body.appendChild(el);
        expect(cssVar(el, '--missing')).toBe('');
        el.remove();
    });

    it('passes literal colors through untouched', () => {
        const el = document.createElement('div');
        expect(resolveColor(el, '#123456')).toBe('#123456');
        expect(resolveColor(el, 'rebeccapurple')).toBe('rebeccapurple');
    });

    it('returns undefined when no color is given', () => {
        const el = document.createElement('div');
        expect(resolveColor(el, undefined)).toBeUndefined();
    });

    it('resolves a css-var reference to its value', () => {
        const el = document.createElement('div');
        el.style.setProperty('--stroke', 'teal');
        document.body.appendChild(el);
        expect(resolveColor(el, '--stroke')).toBe('teal');
        el.remove();
    });
});
