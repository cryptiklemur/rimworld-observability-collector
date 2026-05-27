import type { IconName } from './components/Icon.svelte';

export interface RouteDef {
    id: string;
    title: string;
    icon: IconName;
    ready: boolean;
}

export const routes: RouteDef[] = [
    { id: 'overview', title: 'Overview', icon: 'gauge', ready: true },
    { id: 'hotspots', title: 'Hotspots', icon: 'flame', ready: true },
    { id: 'instrumentation', title: 'Instrumentation', icon: 'probe', ready: true },
    { id: 'sections', title: 'Sections', icon: 'sections', ready: true },
    { id: 'calltree', title: 'Call Tree', icon: 'tree', ready: true },
    { id: 'memory', title: 'Memory', icon: 'memory', ready: true },
    { id: 'metrics', title: 'Metrics', icon: 'metric', ready: true },
    { id: 'patches', title: 'Patches', icon: 'patch', ready: true },
    { id: 'sessions', title: 'Sessions', icon: 'stack', ready: true },
    { id: 'logs', title: 'Logs', icon: 'logs', ready: true },
    { id: 'incidents', title: 'Incidents', icon: 'alert', ready: false },
    { id: 'errors', title: 'Errors', icon: 'bug', ready: false },
    { id: 'comparison', title: 'Comparison', icon: 'compare', ready: false },
    { id: 'panels', title: 'Panels', icon: 'panel', ready: false },
    { id: 'settings', title: 'Settings', icon: 'cog', ready: true },
];

const DEFAULT_ROUTE = 'overview';

function parseHash(): string {
    const raw = window.location.hash.replace(/^#\/?/, '').split('?')[0];
    return routes.some((r) => r.id === raw) ? raw : DEFAULT_ROUTE;
}

class Router {
    current = $state<string>(DEFAULT_ROUTE);

    start() {
        this.current = parseHash();
        window.addEventListener('hashchange', () => {
            this.current = parseHash();
        });
        if (!window.location.hash) {
            window.location.hash = `#/${DEFAULT_ROUTE}`;
        }
    }

    go(id: string) {
        window.location.hash = `#/${id}`;
    }

    get route(): RouteDef {
        return routes.find((r) => r.id === this.current) ?? routes[0];
    }
}

export const router = new Router();
