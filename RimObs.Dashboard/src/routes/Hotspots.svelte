<script lang="ts">
    import { api, type HotspotsResponse } from '../lib/api';
    import { Resource } from '../lib/poll.svelte';
    import DataState from '../lib/components/DataState.svelte';
    import { ns, count, gradeFromShare } from '../lib/format';
    import { t } from '../lib/i18n';
    import { onMount, onDestroy } from 'svelte';

    const res = new Resource<HotspotsResponse>(() => api.hotspots(100), 3000);
    onMount(() => res.start());
    onDestroy(() => res.stop());

    let rows = $derived(res.data?.hotspots ?? []);
    let max = $derived(rows.reduce((m, h) => Math.max(m, h.total_ns), 1));
</script>

<DataState
    state={res.state}
    error={res.error}
    empty={rows.length === 0}
    onretry={() => res.refresh()}
>
    <div class="table">
        <div class="head">
            <span>{t('hotspots.col.section')}</span>
            <span class="num">{t('hotspots.col.total')}</span>
            <span class="num">{t('hotspots.col.mean')}</span>
            <span class="num">{t('hotspots.col.samples')}</span>
            <span class="num">{t('hotspots.col.min')}</span>
            <span class="num">{t('hotspots.col.max')}</span>
        </div>
        {#each rows as h (h.id)}
            {@const share = h.total_ns / max}
            <div class="rowline">
                <div class="section">
                    <span class="name mono">{h.name}</span>
                    <span class="bar"
                        ><span class="fill g{gradeFromShare(share)}" style="width: {share * 100}%"
                        ></span></span
                    >
                </div>
                <span class="num strong mono">{ns(h.total_ns)}</span>
                <span class="num mono">{ns(h.mean_ns)}</span>
                <span class="num mono">{count(h.sample_count)}</span>
                <span class="num dim mono">{ns(h.min_ns)}</span>
                <span class="num dim mono">{ns(h.max_ns)}</span>
            </div>
        {/each}
    </div>
</DataState>

<style>
    .table {
        display: flex;
        flex-direction: column;
    }
    .head,
    .rowline {
        display: grid;
        grid-template-columns: minmax(0, 2.4fr) repeat(5, minmax(72px, 0.7fr));
        gap: 0.75rem;
        align-items: center;
        padding: 0.55rem 0.9rem;
    }
    .head {
        font-size: 0.72rem;
        text-transform: uppercase;
        letter-spacing: 0.07em;
        color: var(--text-faint);
        border-bottom: 1px solid var(--border);
        position: sticky;
        top: 0;
        background: var(--bg-base);
    }
    .rowline {
        border-bottom: 1px solid var(--border-soft);
    }
    .rowline:hover {
        background: var(--bg-surface);
    }
    .num {
        text-align: right;
        font-size: 0.85rem;
    }
    .strong {
        color: var(--text);
        font-weight: 600;
    }
    .dim {
        color: var(--text-faint);
    }
    .section {
        min-width: 0;
        display: flex;
        flex-direction: column;
        gap: 0.3rem;
    }
    .name {
        font-size: 0.84rem;
        color: var(--text);
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
    }
    .bar {
        height: 4px;
        border-radius: 99px;
        background: var(--border-soft);
        overflow: hidden;
    }
    .fill {
        display: block;
        height: 100%;
        border-radius: 99px;
    }
    .g0 {
        background: var(--grade-0);
    }
    .g1 {
        background: var(--grade-1);
    }
    .g2 {
        background: var(--grade-2);
    }
    .g3 {
        background: var(--grade-3);
    }
    .g4 {
        background: var(--grade-4);
    }
</style>
