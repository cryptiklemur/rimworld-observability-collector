const STORAGE_KEY = 'rimobs:userPrefs';

export interface PersistedPrefs {
    closeOnDisconnect: boolean;
}

export const DEFAULT_PREFS: PersistedPrefs = {
    closeOnDisconnect: true,
};

function load(): PersistedPrefs {
    if (typeof localStorage === 'undefined') return { ...DEFAULT_PREFS };
    try {
        const raw = localStorage.getItem(STORAGE_KEY);
        if (raw == null) return { ...DEFAULT_PREFS };
        const parsed = JSON.parse(raw) as Partial<PersistedPrefs>;
        return { ...DEFAULT_PREFS, ...parsed };
    } catch {
        return { ...DEFAULT_PREFS };
    }
}

function persist(prefs: PersistedPrefs): void {
    if (typeof localStorage === 'undefined') return;
    try {
        localStorage.setItem(STORAGE_KEY, JSON.stringify(prefs));
    } catch {
        // ignore quota / privacy-mode errors
    }
}

export class UserPrefs {
    closeOnDisconnect = $state<boolean>(DEFAULT_PREFS.closeOnDisconnect);

    constructor() {
        const loaded = load();
        this.closeOnDisconnect = loaded.closeOnDisconnect;
    }

    setCloseOnDisconnect(value: boolean): void {
        this.closeOnDisconnect = value;
        persist({ closeOnDisconnect: this.closeOnDisconnect });
    }

    reset(): void {
        this.closeOnDisconnect = DEFAULT_PREFS.closeOnDisconnect;
        if (typeof localStorage !== 'undefined') {
            try {
                localStorage.removeItem(STORAGE_KEY);
            } catch {
                // ignore
            }
        }
    }
}

export const userPrefs = new UserPrefs();
