<script lang="ts">
    import { api, type StatusResponse } from '../lib/api';
    import { Resource } from '../lib/poll.svelte';
    import DataState from '../lib/components/DataState.svelte';
    import Card from '../lib/components/Card.svelte';
    import { count, bytes, relativeTime } from '../lib/format';
    import { t } from '../lib/i18n';
    import { onMount, onDestroy } from 'svelte';

    const res = new Resource<StatusResponse>(() => api.status(), 4000);
    onMount(() => res.start());
    onDestroy(() => res.stop());

    let session = $derived(res.data?.session ?? null);
    let receive = $derived(res.data?.receive ?? null);
</script>

<DataState state={res.state} error={res.error} empty={!session} onretry={() => res.refresh()}>
    {#if session && receive}
        <Card title={t('sessions.title')} accent="cyan">
            <div class="grid">
                <div class="kv">
                    <span class="k">id</span><span class="v mono">{session.id}</span>
                </div>
                <div class="kv">
                    <span class="k">started</span><span class="v mono"
                        >{relativeTime(session.started_utc)}</span
                    >
                </div>
                <div class="kv">
                    <span class="k">library</span><span class="v mono"
                        >{session.library_version}</span
                    >
                </div>
                <div class="kv">
                    <span class="k">batches</span><span class="v mono"
                        >{count(receive.total_batches)}</span
                    >
                </div>
                <div class="kv">
                    <span class="k">samples</span><span class="v mono"
                        >{count(receive.total_samples)}</span
                    >
                </div>
                <div class="kv">
                    <span class="k">sections</span><span class="v mono"
                        >{count(receive.section_count)}</span
                    >
                </div>
                <div class="kv">
                    <span class="k">bytes</span><span class="v mono"
                        >{bytes(receive.total_bytes)}</span
                    >
                </div>
                <div class="kv">
                    <span class="k">last batch</span><span class="v mono"
                        >{receive.last_batch_utc ? relativeTime(receive.last_batch_utc) : '—'}</span
                    >
                </div>
            </div>
        </Card>
    {/if}
</DataState>

<style>
    .grid {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
        gap: 0.6rem 1.4rem;
    }
    .kv {
        display: flex;
        justify-content: space-between;
        gap: 1rem;
        padding: 0.5rem 0;
        border-bottom: 1px solid var(--border-soft);
        align-items: baseline;
    }
    .k {
        font-size: 0.72rem;
        text-transform: uppercase;
        letter-spacing: 0.06em;
        color: var(--text-faint);
    }
    .v {
        font-size: 0.86rem;
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
    }
</style>
