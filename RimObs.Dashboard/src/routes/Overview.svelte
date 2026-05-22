<script lang="ts">
    import type { StatusResponse } from '../lib/api';
    import StatCard from '../lib/components/StatCard.svelte';
    import Card from '../lib/components/Card.svelte';
    import { count, bytes, relativeTime } from '../lib/format';
    import { t } from '../lib/i18n';

    let { status }: { status: StatusResponse | null } = $props();
    let r = $derived(status?.receive);
</script>

{#if r}
    <div class="grid">
        <StatCard
            icon="stack"
            tone="cyan"
            label={t('overview.batches')}
            value={count(r.total_batches)}
        />
        <StatCard
            icon="metric"
            tone="cyan"
            label={t('overview.samples')}
            value={count(r.total_samples)}
        />
        <StatCard
            icon="flame"
            tone="ember"
            label={t('overview.sections')}
            value={count(r.section_count)}
        />
        <StatCard
            icon="memory"
            tone="warn"
            label={t('overview.gc')}
            value={count(r.total_gc_events)}
        />
        <StatCard
            icon="metric"
            tone="warn"
            label={t('overview.allocations')}
            value={count(r.total_allocations)}
        />
        <StatCard
            icon="logs"
            tone="good"
            label={t('overview.bytes')}
            value={bytes(r.total_bytes)}
        />
    </div>

    <div class="row">
        <Card title={t('overview.session')}>
            {#if status?.session}
                <dl>
                    <dt>id</dt>
                    <dd class="mono">{status.session.id}</dd>
                    <dt>library</dt>
                    <dd class="mono">{status.session.library_version}</dd>
                    <dt>started</dt>
                    <dd>{new Date(status.session.started_utc).toLocaleString()}</dd>
                    <dt>last batch</dt>
                    <dd>{relativeTime(r.last_batch_utc)}</dd>
                </dl>
            {:else}
                <p class="none">{t('overview.noSession')}</p>
            {/if}
        </Card>
        <Card title="Collector">
            <dl>
                <dt>status</dt>
                <dd class="mono">{status?.status}</dd>
                <dt>version</dt>
                <dd class="mono">{status?.version}</dd>
                <dt>update</dt>
                <dd>{status?.update?.available ? status.update.latest_version : 'up to date'}</dd>
            </dl>
        </Card>
    </div>
{/if}

<style>
    .grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(210px, 1fr));
        gap: 1rem;
        margin-bottom: 1.2rem;
    }
    .row {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
        gap: 1rem;
    }
    dl {
        margin: 0;
        display: grid;
        grid-template-columns: auto 1fr;
        gap: 0.45rem 1rem;
    }
    dt {
        color: var(--text-faint);
        font-size: 0.78rem;
        text-transform: uppercase;
        letter-spacing: 0.05em;
    }
    dd {
        margin: 0;
        text-align: right;
        word-break: break-all;
    }
    .none {
        color: var(--text-faint);
        margin: 0;
    }
</style>
