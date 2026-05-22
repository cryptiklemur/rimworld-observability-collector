<script lang="ts">
    import { api, type MetricsResponse } from '../lib/api';
    import { Resource } from '../lib/poll.svelte';
    import DataState from '../lib/components/DataState.svelte';
    import { count, metricKind } from '../lib/format';
    import { t } from '../lib/i18n';
    import { onMount, onDestroy } from 'svelte';

    const res = new Resource<MetricsResponse>(() => api.metrics(), 3000);
    onMount(() => res.start());
    onDestroy(() => res.stop());

    let metrics = $derived(res.data?.metrics ?? []);
</script>

<DataState
    state={res.state}
    error={res.error}
    empty={metrics.length === 0}
    onretry={() => res.refresh()}
>
    <div class="list">
        {#each metrics as m (m.id)}
            <article class="metric">
                <header>
                    <span class="name mono">{m.name}</span>
                    <span class="kind k{m.kind}">{metricKind(m.kind)}</span>
                    {#if m.unit}<span class="unit">{m.unit}</span>{/if}
                </header>
                <div class="labels">
                    {#each m.labels as l (l.canonical)}
                        <div class="label">
                            <span class="canon mono">{l.canonical || '(default)'}</span>
                            <span class="val mono">{count(l.latest_value)}</span>
                            <span class="samples mono"
                                >{count(l.total_sample_count)} {t('metrics.col.samples')}</span
                            >
                        </div>
                    {/each}
                </div>
            </article>
        {/each}
    </div>
</DataState>

<style>
    .list {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
        gap: 1rem;
    }
    .metric {
        border: 1px solid var(--border-soft);
        border-radius: var(--r-lg);
        background: linear-gradient(180deg, var(--bg-surface), var(--bg-surface-2));
        overflow: hidden;
    }
    header {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        padding: 0.7rem 0.9rem;
        border-bottom: 1px solid var(--border-soft);
    }
    .name {
        font-size: 0.85rem;
        flex: 1;
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
    }
    .kind {
        font-size: 0.66rem;
        text-transform: uppercase;
        letter-spacing: 0.06em;
        padding: 0.1rem 0.5rem;
        border-radius: 99px;
    }
    .k0 {
        background: color-mix(in srgb, var(--cyan) 16%, transparent);
        color: var(--cyan-soft);
    }
    .k1 {
        background: color-mix(in srgb, var(--ember) 16%, transparent);
        color: var(--ember-soft);
    }
    .k2 {
        background: color-mix(in srgb, var(--warn) 16%, transparent);
        color: var(--warn);
    }
    .unit {
        font-size: 0.72rem;
        color: var(--text-faint);
    }
    .labels {
        display: flex;
        flex-direction: column;
    }
    .label {
        display: grid;
        grid-template-columns: 1fr auto;
        grid-template-areas: 'canon val' 'samples val';
        gap: 0 0.5rem;
        padding: 0.55rem 0.9rem;
        border-bottom: 1px solid var(--border-soft);
        align-items: center;
    }
    .label:last-child {
        border-bottom: none;
    }
    .canon {
        grid-area: canon;
        font-size: 0.8rem;
        color: var(--text-dim);
    }
    .val {
        grid-area: val;
        font-size: 1.15rem;
        font-weight: 600;
        text-align: right;
    }
    .samples {
        grid-area: samples;
        font-size: 0.7rem;
        color: var(--text-faint);
    }
</style>
