export type LoadState = 'loading' | 'ok' | 'error';

export class Resource<T> {
    data = $state<T | null>(null);
    state = $state<LoadState>('loading');
    error = $state<string>('');
    consecutiveFailures = $state<number>(0);
    private timer: ReturnType<typeof setInterval> | null = null;

    constructor(
        private readonly loader: () => Promise<T>,
        private readonly intervalMs = 3000,
    ) {}

    async refresh() {
        try {
            const next = await this.loader();
            this.data = next;
            this.state = 'ok';
            this.error = '';
            this.consecutiveFailures = 0;
        } catch (err) {
            this.state = this.data ? 'ok' : 'error';
            this.error = (err as Error).message;
            this.consecutiveFailures += 1;
        }
    }

    start() {
        void this.refresh();
        if (this.intervalMs > 0) {
            this.timer = setInterval(() => void this.refresh(), this.intervalMs);
        }
    }

    stop() {
        if (this.timer) {
            clearInterval(this.timer);
            this.timer = null;
        }
    }
}
