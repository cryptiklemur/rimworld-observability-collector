<script lang="ts">
    import { api, type GcResponse } from '../lib/api';
    import { Resource } from '../lib/poll.svelte';
    import DataState from '../lib/components/DataState.svelte';
    import { bytes, count } from '../lib/format';
    import { t } from '../lib/i18n';
    import { onMount, onDestroy } from 'svelte';

    const res = new Resource<GcResponse>(() => api.gc(200), 4000);
    onMount(() => res.start());
    onDestroy(() => res.stop());

    let events = $derived(res.data?.events ?? []);
    let maxRate = $derived(events.reduce((m, e) => Math.max(m, e.allocation_rate_bpm), 1));
</script>

<DataState
    state={res.state}
    error={res.error}
    empty={events.length === 0}
    onretry={() => res.refresh()}
>
    <div class="table">
        <div class="head">
            <span class="num">{t('memory.col.gen')}</span>
            <span>{t('memory.col.pause')}</span>
            <span class="num">{t('memory.col.before')}</span>
            <span class="num">{t('memory.col.after')}</span>
            <span class="num">{t('memory.col.duration')}</span>
            <span class="num">{t('memory.col.rate')}</span>
            <span class="num">{t('memory.col.tick')}</span>
        </div>
        <!-- gc events have no stable id and rows are display-only, so index keying is safe -->
        {#each events as e, i (i)}
            <div class="rowline">
                <span class="num"
                    ><em class="gen g{Math.min(e.generation, 2)}">{e.generation}</em></span
                >
                <span class="pause">{e.pause_type}</span>
                <span class="num mono dim">{bytes(e.heap_before)}</span>
                <span class="num mono dim">{bytes(e.heap_after)}</span>
                <span class="num mono">{(e.duration_micros / 1000).toFixed(2)} ms</span>
                <span class="num rate">
                    <span class="track"
                        ><span
                            class="fill"
                            style="width: {(e.allocation_rate_bpm / maxRate) * 100}%"
                        ></span></span
                    >
                    <span class="mono">{bytes(e.allocation_rate_bpm)}/m</span>
                </span>
                <span class="num mono dim">{count(e.ticks)}</span>
            </div>
        {/each}
    </div>
</DataState>

<style>
    .head,
    .rowline {
        display: grid;
        grid-template-columns:
            54px minmax(80px, 1fr) repeat(2, minmax(90px, 1fr)) minmax(90px, 1fr)
            minmax(150px, 1.4fr) minmax(80px, 0.8fr);
        gap: 0.75rem;
        align-items: center;
        padding: 0.5rem 0.9rem;
    }
    .head {
        font-size: 0.72rem;
        text-transform: uppercase;
        letter-spacing: 0.07em;
        color: var(--text-faint);
        border-bottom: 1px solid var(--border);
    }
    .rowline {
        border-bottom: 1px solid var(--border-soft);
    }
    .rowline:hover {
        background: var(--bg-surface);
    }
    .num {
        text-align: right;
        font-size: 0.84rem;
    }
    .dim {
        color: var(--text-faint);
    }
    .pause {
        font-size: 0.82rem;
        color: var(--text-dim);
    }
    .gen {
        font-style: normal;
        font-family: var(--font-mono);
        padding: 0.05rem 0.5rem;
        border-radius: 99px;
        font-size: 0.78rem;
    }
    .gen.g0 {
        background: color-mix(in srgb, var(--good) 18%, transparent);
        color: var(--good);
    }
    .gen.g1 {
        background: color-mix(in srgb, var(--warn) 18%, transparent);
        color: var(--warn);
    }
    .gen.g2 {
        background: color-mix(in srgb, var(--bad) 18%, transparent);
        color: var(--bad);
    }
    .rate {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        justify-content: flex-end;
    }
    .rate .mono {
        font-size: 0.78rem;
        white-space: nowrap;
    }
    .track {
        flex: 1;
        height: 4px;
        border-radius: 99px;
        background: var(--border-soft);
        overflow: hidden;
        max-width: 80px;
    }
    .fill {
        display: block;
        height: 100%;
        background: var(--ember);
        border-radius: 99px;
    }
</style>
