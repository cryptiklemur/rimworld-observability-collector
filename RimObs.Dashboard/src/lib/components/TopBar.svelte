<script lang="ts">
    import type { StatusResponse } from '../api';
    import { router } from '../router.svelte';
    import { t } from '../i18n';
    import { relativeTime } from '../format';
    import Icon from './Icon.svelte';

    let { status }: { status: StatusResponse | null } = $props();

    let online = $derived(status?.status === 'running');
    let connected = $derived(!!status?.session);
</script>

<header class="topbar">
    <div class="crumbs">
        <h1>{t(`nav.${router.current}`, router.route.title)}</h1>
    </div>

    <div class="right">
        {#if status?.update?.available}
            <a class="update" href={status.update.url ?? '#'} target="_blank" rel="noreferrer">
                <Icon name="external" size={14} />
                {status.update.latest_version} available
            </a>
        {/if}

        <div class="session" class:connected>
            <span class="dot"></span>
            {#if connected}
                <span class="sid mono">{status?.session?.id}</span>
                <span class="ago">{relativeTime(status?.receive?.last_batch_utc ?? null)}</span>
            {:else}
                <span class="ago">{t('overview.noSession')}</span>
            {/if}
        </div>

        <div class="health" class:up={online}>
            <span class="dot"></span>
            {online ? t('status.running') : t('status.offline')}
        </div>
    </div>
</header>

<style>
    .topbar {
        grid-area: topbar;
        height: var(--topbar-h);
        display: flex;
        align-items: center;
        justify-content: space-between;
        padding: 0 1.4rem;
        border-bottom: 1px solid var(--border-soft);
        background: color-mix(in srgb, var(--bg-base) 55%, transparent);
        backdrop-filter: blur(6px);
        position: sticky;
        top: 0;
        z-index: 5;
    }
    h1 {
        font-size: 1.15rem;
    }
    .right {
        display: flex;
        align-items: center;
        gap: 0.8rem;
    }
    .update {
        display: inline-flex;
        align-items: center;
        gap: 0.35rem;
        font-size: 0.78rem;
        color: var(--ember-soft);
        border: 1px solid color-mix(in srgb, var(--ember) 40%, transparent);
        background: color-mix(in srgb, var(--ember) 12%, transparent);
        border-radius: 99px;
        padding: 0.25rem 0.7rem;
    }
    .session,
    .health {
        display: inline-flex;
        align-items: center;
        gap: 0.45rem;
        font-size: 0.78rem;
        color: var(--text-dim);
        border: 1px solid var(--border-soft);
        border-radius: 99px;
        padding: 0.3rem 0.75rem;
        background: var(--bg-surface);
    }
    .sid {
        color: var(--text);
        max-width: 12rem;
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
    }
    .ago {
        color: var(--text-faint);
    }
    .dot {
        width: 8px;
        height: 8px;
        border-radius: 50%;
        background: var(--text-faint);
    }
    .session.connected .dot {
        background: var(--cyan);
        box-shadow: 0 0 0 3px color-mix(in srgb, var(--cyan) 25%, transparent);
    }
    .health.up .dot {
        background: var(--good);
        box-shadow: 0 0 0 3px color-mix(in srgb, var(--good) 25%, transparent);
    }
</style>
