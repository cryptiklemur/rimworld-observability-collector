<script lang="ts">
    import { api, type StatusResponse } from '../lib/api';
    import { Resource } from '../lib/poll.svelte';
    import Card from '../lib/components/Card.svelte';
    import { t, getLang } from '../lib/i18n';
    import { onMount, onDestroy } from 'svelte';

    const res = new Resource<StatusResponse>(() => api.status(), 10000);
    onMount(() => res.start());
    onDestroy(() => res.stop());

    let status = $derived(res.data);
</script>

<Card title={t('settings.title')} accent="ember">
    <div class="rows">
        <div class="kv">
            <span class="k">{t('settings.version')}</span><span class="v mono"
                >{status?.version ?? '—'}</span
            >
        </div>
        <div class="kv">
            <span class="k">{t('settings.schema')}</span><span class="v mono"
                >{status?.schema_version ?? '—'}</span
            >
        </div>
        <div class="kv">
            <span class="k">{t('settings.language')}</span><span class="v mono">{getLang()}</span>
        </div>
        {#if status?.update?.available}
            <div class="kv update">
                <span class="k">{t('settings.update')}</span>
                <a class="v mono" href={status.update.url} target="_blank" rel="noreferrer"
                    >{status.update.latest_version}</a
                >
            </div>
        {/if}
    </div>
</Card>

<style>
    .rows {
        display: flex;
        flex-direction: column;
    }
    .kv {
        display: flex;
        justify-content: space-between;
        gap: 1rem;
        padding: 0.65rem 0;
        border-bottom: 1px solid var(--border-soft);
        align-items: baseline;
    }
    .kv:last-child {
        border-bottom: none;
    }
    .k {
        font-size: 0.78rem;
        text-transform: uppercase;
        letter-spacing: 0.06em;
        color: var(--text-faint);
    }
    .v {
        font-size: 0.88rem;
    }
    .update .v {
        color: var(--ember-soft);
    }
</style>
