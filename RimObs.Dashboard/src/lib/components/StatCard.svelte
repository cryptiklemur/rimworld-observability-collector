<script lang="ts">
    import Icon, { type IconName } from './Icon.svelte';
    let {
        label,
        value,
        icon,
        tone = 'cyan',
    }: {
        label: string;
        value: string;
        icon: IconName;
        tone?: 'cyan' | 'ember' | 'good' | 'warn';
    } = $props();
</script>

<div class="stat tone-{tone}">
    <div class="ic"><Icon name={icon} size={20} /></div>
    <div class="meta">
        <span class="label">{label}</span>
        <span class="value mono">{value}</span>
    </div>
</div>

<style>
    .stat {
        display: flex;
        gap: 0.85rem;
        align-items: flex-start;
        background: linear-gradient(180deg, var(--bg-surface), var(--bg-surface-2));
        border: 1px solid var(--border-soft);
        border-radius: var(--r-lg);
        padding: 1rem 1.15rem;
        box-shadow: var(--shadow-card);
        position: relative;
        overflow: hidden;
    }
    .stat::before {
        content: '';
        position: absolute;
        inset: 0 auto 0 0;
        width: 3px;
        background: var(--accent);
    }
    .tone-cyan {
        --accent: var(--cyan);
    }
    .tone-ember {
        --accent: var(--ember);
    }
    .tone-good {
        --accent: var(--good);
    }
    .tone-warn {
        --accent: var(--warn);
    }
    .ic {
        color: var(--accent);
        display: grid;
        place-items: center;
        width: 38px;
        height: 38px;
        border-radius: var(--r-md);
        background: color-mix(in srgb, var(--accent) 14%, transparent);
        flex-shrink: 0;
    }
    .meta {
        display: flex;
        flex-direction: column;
        min-width: 0;
    }
    .label {
        font-size: 0.72rem;
        text-transform: uppercase;
        letter-spacing: 0.07em;
        color: var(--text-faint);
    }
    .value {
        font-size: 1.5rem;
        font-weight: 600;
        line-height: 1.2;
        color: var(--text);
    }
</style>
