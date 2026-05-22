import en from '../i18n/en.json';

type Dict = Record<string, string>;

const dictionaries: Record<string, Dict> = { en };

export function getLang(): string {
    const param = new URLSearchParams(window.location.search).get('lang');
    return param && dictionaries[param] ? param : 'en';
}

export function t(key: string, fallback?: string): string {
    const lang = getLang();
    return dictionaries[lang]?.[key] ?? dictionaries.en[key] ?? fallback ?? key;
}
