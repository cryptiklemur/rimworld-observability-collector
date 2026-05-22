<script lang="ts">
    import type { Snippet } from 'svelte';
    import type { LoadState } from '../poll.svelte';
    import { t } from '../i18n';
    import Icon from './Icon.svelte';

    let {
        state,
        error = '',
        empty = false,
        onretry,
        children,
    }: {
        state: LoadState;
        error?: string;
        empty?: boolean;
        onretry?: () => void;
        children: Snippet;
    } = $props();
</script>

{#if state === 'loading'}
    <div class="msg">
        <Icon name="dot" size={28} />
        <span>{t('status.loading')}</span>
    </div>
{:else if state === 'error'}
    <div class="msg err">
        <Icon name="alert" size={28} />
        <span>{t('common.error')}</span>
        <code>{error}</code>
        {#if onretry}<button onclick={onretry}>{t('common.retry')}</button>{/if}
    </div>
{:else if empty}
    <div class="msg">
        <Icon name="dot" size={28} />
        <span>{t('common.empty')}</span>
        <p>{t('common.empty.hint')}</p>
    </div>
{:else}
    {@render children()}
{/if}

<style>
    .msg {
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: 0.6rem;
        padding: 3rem 1rem;
        color: var(--text-dim);
        text-align: center;
    }
    .msg p {
        margin: 0;
        max-width: 26rem;
        color: var(--text-faint);
        font-size: 0.85rem;
    }
    .msg code {
        color: var(--text-faint);
        font-size: 0.78rem;
    }
    .err {
        color: var(--bad);
    }
    button {
        margin-top: 0.4rem;
        background: var(--bg-elev);
        color: var(--text);
        border: 1px solid var(--border);
        border-radius: var(--r-md);
        padding: 0.4rem 1rem;
        cursor: pointer;
        font-family: var(--font-ui);
    }
    button:hover {
        border-color: var(--cyan);
    }
</style>
