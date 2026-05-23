<script lang="ts">
    import { api, type GcResponse } from '../lib/api';
    import { Resource } from '../lib/poll.svelte';
    import DataState from '../lib/components/DataState.svelte';
    import StatCard from '../lib/components/StatCard.svelte';
    import Card from '../lib/components/Card.svelte';
    import LineChart from '../lib/components/LineChart.svelte';
    import { bytes, count } from '../lib/format';
    import { pauseLabel, genLabel, summarize, heapSeries } from '../lib/gc';
    import { t } from '../lib/i18n';
    import { onMount, onDestroy } from 'svelte';

    const res = new Resource<GcResponse>(() => api.gc(200), 4000);
    onMount(() => res.start());
    onDestroy(() => res.stop());

    let events = $derived(res.data?.events ?? []);
    let maxRate = $derived(events.reduce((m, e) => Math.max(m, e.allocation_rate_bpm), 1));
    let summary = $derived(summarize(events));
    let trend = $derived(heapSeries(events));
    let chartSeries = $derived([
        {
            label: t('memory.chart.heap'),
            values: trend.heap,
            stroke: '--cyan',
            fill: 'rgba(57, 196, 212, 0.12)',
        },
    ]);
</script>

<DataState
    state={res.state}
    error={res.error}
    empty={events.length === 0}
    onretry={() => res.refresh()}
>
    <div class="stats">
        <StatCard
            icon="memory"
            tone="cyan"
            label={t('memory.stat.currentHeap')}
            value={bytes(summary.currentHeap)}
        />
        <StatCard
            icon="memory"
            tone="warn"
            label={t('memory.stat.peakHeap')}
            value={bytes(summary.peakHeap)}
        />
        <StatCard
            icon="metric"
            tone="ember"
            label={t('memory.stat.peakRate')}
            value="{bytes(summary.peakAllocRate)}/m"
        />
        <StatCard
            icon="stack"
            tone="good"
            label={t('memory.stat.collections')}
            value="{count(summary.perGen[0])} / {count(summary.perGen[1])} / {count(
                summary.perGen[2],
            )}"
        />
    </div>

    <Card title={t('memory.chart.title')}>
        {#if trend.heap.length > 1}
            <LineChart
                x={trend.ticks}
                series={chartSeries}
                height={220}
                format={(n) => bytes(n)}
                xFormat={(n) => count(n)}
            />
        {:else}
            <p class="hint">{t('memory.chart.waiting')}</p>
        {/if}
    </Card>

    <Card title={t('memory.events.title')}>
        <div class="table">
            <div class="head">
                <span class="num">{t('memory.col.gen')}</span>
                <span>{t('memory.col.pause')}</span>
                <span class="num">{t('memory.col.before')}</span>
                <span class="num">{t('memory.col.after')}</span>
                <span class="num">{t('memory.col.interval')}</span>
                <span class="num">{t('memory.col.rate')}</span>
                <span class="num">{t('memory.col.tick')}</span>
            </div>
            <!-- gc events have no stable id and rows are display-only, so index keying is safe -->
            {#each events as e, i (i)}
                <div class="rowline">
                    <span class="num"
                        ><em class="gen g{Math.min(e.generation, 2)}">{genLabel(e.generation)}</em
                        ></span
                    >
                    <span class="pause">{pauseLabel(e.pause_type)}</span>
                    <span class="num mono dim">{bytes(e.heap_before)}</span>
                    <span class="num mono dim">{bytes(e.heap_after)}</span>
                    <span class="num mono">{(e.duration_micros / 1000).toFixed(0)} ms</span>
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
    </Card>

    <Card title={t('memory.guide.title')}>
        <dl class="guide">
            <dt><em class="gen g0">Gen 0</em></dt>
            <dd>{t('memory.guide.g0')}</dd>
            <dt><em class="gen g1">Gen 1</em></dt>
            <dd>{t('memory.guide.g1')}</dd>
            <dt><em class="gen g2">Gen 2</em></dt>
            <dd>{t('memory.guide.g2')}</dd>
        </dl>
        <p class="note">{t('memory.guide.interval')}</p>
    </Card>
</DataState>

<style>
    .stats {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
        gap: 1rem;
        margin-bottom: 1.2rem;
    }
    :global(.card) {
        margin-bottom: 1.2rem;
    }
    .hint,
    .note {
        color: var(--text-faint);
        font-size: 0.85rem;
        margin: 0;
    }
    .note {
        margin-top: 0.9rem;
        border-top: 1px solid var(--border-soft);
        padding-top: 0.9rem;
    }
    .head,
    .rowline {
        display: grid;
        grid-template-columns:
            72px minmax(90px, 1fr) repeat(2, minmax(90px, 1fr)) minmax(72px, 0.8fr)
            minmax(150px, 1.4fr) minmax(80px, 0.8fr);
        gap: 0.75rem;
        align-items: center;
        padding: 0.5rem 0.3rem;
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
        font-size: 0.74rem;
        white-space: nowrap;
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
    .guide {
        margin: 0;
        display: grid;
        grid-template-columns: auto 1fr;
        gap: 0.55rem 1rem;
        align-items: baseline;
    }
    .guide dd {
        margin: 0;
        font-size: 0.86rem;
        color: var(--text-dim);
    }
</style>
