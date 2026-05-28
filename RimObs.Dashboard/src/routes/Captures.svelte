<script lang="ts">
    import { api, type CapturesResponse, type CaptureSummary } from '../lib/api';
    import { Resource } from '../lib/poll.svelte';
    import DataState from '../lib/components/DataState.svelte';
    import CallTreeRow from '../lib/components/CallTreeRow.svelte';
    import Icon from '../lib/components/Icon.svelte';
    import { t } from '../lib/i18n';
    import { bytes, count, relativeTime } from '../lib/format';
    import { onMount, onDestroy } from 'svelte';

    const res = new Resource<CapturesResponse>(() => api.captures(true), 2000);
    let busy = $state(false);
    let actionError = $state('');
    let selectedId = $state<string | null>(null);

    onMount(() => {
        res.start();
        window.addEventListener('beforeunload', stopOnUnload);
    });
    onDestroy(() => {
        res.stop();
        window.removeEventListener('beforeunload', stopOnUnload);
    });

    let captures = $derived(res.data?.captures ?? []);
    let activeId = $derived(res.data?.active_capture_id ?? null);
    let active = $derived(captures.find((c) => c.id === activeId) ?? null);
    let selected = $derived(
        captures.find((c) => c.id === selectedId) ?? active ?? captures[0] ?? null,
    );
    let selectedTotalNs = $derived((selected?.roots ?? []).reduce((s, r) => s + r.total_ns, 0));

    function stopOnUnload() {
        if (activeId) navigator.sendBeacon?.('/api/v1/captures/stop');
    }

    async function start() {
        busy = true;
        actionError = '';
        try {
            await api.startCapture();
            await res.refresh();
        } catch {
            actionError = t('captures.error.start', 'Could not start capture');
        } finally {
            busy = false;
        }
    }

    async function stop() {
        busy = true;
        actionError = '';
        try {
            await api.stopCapture();
            await res.refresh();
        } catch {
            actionError = t('captures.error.stop', 'Could not stop capture');
        } finally {
            busy = false;
        }
    }

    function triggerLabel(c: CaptureSummary): string {
        return c.trigger === 'slow_tick'
            ? t('captures.trigger.slow_tick', 'Slow tick')
            : t('captures.trigger.manual', 'Manual');
    }

    function reasonLabel(c: CaptureSummary): string {
        switch (c.finalize_reason) {
            case 'user_stopped':
                return t('captures.reason.user_stopped', 'Stopped');
            case 'time_cap':
                return t('captures.reason.time_cap', 'Duration cap');
            case 'size_cap':
                return t('captures.reason.size_cap', 'Size cap');
            case 'dashboard_closed':
                return t('captures.reason.dashboard_closed', 'Dashboard closed');
            default:
                return '';
        }
    }
</script>

<div class="bar">
    <div class="bar-text">
        <h2>{t('captures.title', 'Focused captures')}</h2>
        <p>
            {t('captures.subtitle', 'Record a bounded high-detail call tree around a slow window.')}
        </p>
    </div>
    <div class="controls">
        {#if active}
            <button class="btn stop" onclick={stop} disabled={busy}>
                <Icon name="dot" size={14} />
                {t('captures.stop', 'Stop capture')}
            </button>
        {:else}
            <button class="btn start" onclick={start} disabled={busy}>
                <Icon name="flame" size={14} />
                {t('captures.start', 'Start capture')}
            </button>
        {/if}
    </div>
</div>

{#if actionError}
    <p class="action-error" role="alert">{actionError}</p>
{/if}

<DataState
    state={res.state}
    error={res.error}
    empty={captures.length === 0}
    onretry={() => res.refresh()}
>
    <div class="layout">
        <ul class="list">
            {#each captures as c (c.id)}
                <li>
                    <button
                        class="row"
                        class:active={c.id === selected?.id}
                        onclick={() => (selectedId = c.id)}
                    >
                        <span class="dot {c.status}"></span>
                        <span class="meta">
                            <span class="title">{triggerLabel(c)}</span>
                            <span class="sub">{relativeTime(c.started_utc)}</span>
                        </span>
                        <span class="stats">
                            <span>{count(c.edge_count)} {t('captures.edges', 'edges')}</span>
                            <span>{bytes(c.estimated_bytes)}</span>
                        </span>
                    </button>
                    {#if c.warning}
                        <p class="warning" role="status">{c.warning}</p>
                    {/if}
                </li>
            {/each}
        </ul>

        <div class="detail">
            {#if selected}
                <div class="detail-head">
                    <span class="badge {selected.status}">
                        {selected.status === 'running'
                            ? t('captures.running', 'Running')
                            : t('captures.finalized', 'Finalized')}
                    </span>
                    {#if reasonLabel(selected)}
                        <span class="reason">{reasonLabel(selected)}</span>
                    {/if}
                    {#if selected.dropped_samples > 0}
                        <span class="dropped">
                            {count(selected.dropped_samples)}
                            {t('captures.dropped', 'dropped')}
                        </span>
                    {/if}
                </div>
                {#if selected.roots.length === 0}
                    <p class="empty-tree">
                        {t('captures.empty_tree', 'No sections recorded yet.')}
                    </p>
                {:else}
                    <div class="legend">
                        <span>{t('calltree.title', 'Call tree')}</span>
                        <span class="cols">
                            <span>{t('calltree.share', 'Share')}</span>
                            <span>{t('calltree.calls', 'Calls')}</span>
                            <span>{t('calltree.total', 'Total')}</span>
                        </span>
                    </div>
                    <div class="tree">
                        {#each selected.roots as root (root.id)}
                            <CallTreeRow node={root} parentNs={selectedTotalNs} />
                        {/each}
                    </div>
                {/if}
            {/if}
        </div>
    </div>
</DataState>

<style>
    .bar {
        display: flex;
        justify-content: space-between;
        align-items: flex-start;
        gap: 1rem;
        margin-bottom: 1rem;
    }
    .bar-text h2 {
        margin: 0;
        font-size: 1.1rem;
    }
    .bar-text p {
        margin: 0.2rem 0 0;
        color: var(--text-faint);
        font-size: 0.82rem;
    }
    .controls {
        flex-shrink: 0;
    }
    .btn {
        display: inline-flex;
        align-items: center;
        gap: 0.4rem;
        border: 1px solid var(--border);
        border-radius: 6px;
        padding: 0.5rem 0.9rem;
        font-size: 0.85rem;
        cursor: pointer;
        background: var(--surface);
        color: var(--text);
    }
    .btn:disabled {
        opacity: 0.5;
        cursor: progress;
    }
    .btn.start {
        border-color: var(--accent);
        color: var(--accent);
    }
    .btn.stop {
        border-color: var(--danger, #d9534f);
        color: var(--danger, #d9534f);
    }
    .action-error {
        color: var(--danger, #d9534f);
        font-size: 0.82rem;
        margin: 0 0 0.8rem;
    }
    .layout {
        display: grid;
        grid-template-columns: minmax(220px, 320px) 1fr;
        gap: 1.2rem;
    }
    .list {
        list-style: none;
        margin: 0;
        padding: 0;
        display: flex;
        flex-direction: column;
        gap: 0.4rem;
    }
    .row {
        width: 100%;
        display: flex;
        align-items: center;
        gap: 0.6rem;
        padding: 0.55rem 0.7rem;
        border: 1px solid var(--border);
        border-radius: 6px;
        background: var(--surface);
        cursor: pointer;
        text-align: left;
        color: var(--text);
    }
    .row.active {
        border-color: var(--accent);
    }
    .dot {
        width: 8px;
        height: 8px;
        border-radius: 50%;
        flex-shrink: 0;
        background: var(--text-faint);
    }
    .dot.running {
        background: var(--accent);
        box-shadow: 0 0 0 3px color-mix(in srgb, var(--accent) 25%, transparent);
    }
    .meta {
        display: flex;
        flex-direction: column;
        flex: 1;
        min-width: 0;
    }
    .meta .title {
        font-size: 0.85rem;
    }
    .meta .sub {
        font-size: 0.72rem;
        color: var(--text-faint);
    }
    .stats {
        display: flex;
        flex-direction: column;
        align-items: flex-end;
        font-size: 0.72rem;
        color: var(--text-faint);
    }
    .warning {
        margin: 0.3rem 0 0;
        font-size: 0.72rem;
        color: var(--warn, #c98a00);
    }
    .detail {
        min-width: 0;
    }
    .detail-head {
        display: flex;
        align-items: center;
        gap: 0.6rem;
        margin-bottom: 0.7rem;
    }
    .badge {
        font-size: 0.7rem;
        text-transform: uppercase;
        letter-spacing: 0.06em;
        padding: 0.15rem 0.5rem;
        border-radius: 999px;
        border: 1px solid var(--border);
        color: var(--text-faint);
    }
    .badge.running {
        border-color: var(--accent);
        color: var(--accent);
    }
    .reason,
    .dropped {
        font-size: 0.74rem;
        color: var(--text-faint);
    }
    .legend {
        display: flex;
        justify-content: space-between;
        align-items: center;
        font-size: 0.72rem;
        text-transform: uppercase;
        letter-spacing: 0.07em;
        color: var(--text-faint);
        padding: 0 0.5rem 0.6rem;
        border-bottom: 1px solid var(--border);
        margin-bottom: 0.4rem;
    }
    .cols {
        display: flex;
        gap: 1.6rem;
    }
    .tree {
        display: flex;
        flex-direction: column;
        overflow-x: auto;
    }
    .empty-tree {
        color: var(--text-faint);
        font-size: 0.82rem;
    }
</style>
