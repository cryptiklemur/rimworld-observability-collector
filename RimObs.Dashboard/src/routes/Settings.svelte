<script lang="ts">
    import { api, type StatusResponse } from '../lib/api';
    import { Resource } from '../lib/poll.svelte';
    import Card from '../lib/components/Card.svelte';
    import Tooltip from '../lib/components/Tooltip.svelte';
    import { t, getLang } from '../lib/i18n';
    import { userPrefs } from '../lib/userPrefs.svelte';
    import { onMount, onDestroy } from 'svelte';

    const res = new Resource<StatusResponse>(() => api.status(), 10000);
    onMount(() => res.start());
    onDestroy(() => res.stop());

    let status = $derived(res.data);
</script>

<Card title={t('settings.title')} accent="ember">
    <div class="rows">
        <div class="kv">
            <Tooltip text={t('tip.settings.version')}><span class="k">{t('settings.version')}</span></Tooltip>
            <span class="v mono">{status?.version ?? '—'}</span>
        </div>
        <div class="kv">
            <Tooltip text={t('tip.settings.schema')}><span class="k">{t('settings.schema')}</span></Tooltip>
            <span class="v mono">{status?.schema_version ?? '—'}</span>
        </div>
        <div class="kv">
            <Tooltip text={t('tip.settings.language')}><span class="k">{t('settings.language')}</span></Tooltip>
            <span class="v mono">{getLang()}</span>
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

<Card title={t('settings.behavior')} accent="cyan">
    <label class="toggle">
        <input
            type="checkbox"
            checked={userPrefs.closeOnDisconnect}
            onchange={(e) =>
                userPrefs.setCloseOnDisconnect((e.currentTarget as HTMLInputElement).checked)}
        />
        <span class="toggle-text">
            <span class="toggle-label">{t('settings.close_on_disconnect')}</span>
            <span class="toggle-hint">{t('settings.close_on_disconnect.hint')}</span>
        </span>
    </label>
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
    .toggle {
        display: flex;
        gap: 0.85rem;
        align-items: flex-start;
        cursor: pointer;
    }
    .toggle input {
        margin-top: 0.2rem;
        accent-color: var(--cyan);
        cursor: pointer;
    }
    .toggle-text {
        display: flex;
        flex-direction: column;
        gap: 0.25rem;
    }
    .toggle-label {
        font-size: 0.92rem;
        color: var(--text);
    }
    .toggle-hint {
        font-size: 0.78rem;
        color: var(--text-faint);
        line-height: 1.4;
    }
</style>
