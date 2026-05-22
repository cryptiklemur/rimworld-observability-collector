<script lang="ts">
    import type { Snippet } from 'svelte';
    let {
        title,
        accent,
        children,
    }: { title?: string; accent?: 'ember' | 'cyan'; children: Snippet } = $props();
</script>

<section class="card" class:ember={accent === 'ember'} class:cyan={accent === 'cyan'}>
    {#if title}
        <header>
            <h3>{title}</h3>
        </header>
    {/if}
    <div class="body">{@render children()}</div>
</section>

<style>
    .card {
        background: linear-gradient(180deg, var(--bg-surface), var(--bg-surface-2));
        border: 1px solid var(--border-soft);
        border-radius: var(--r-lg);
        box-shadow: var(--shadow-card);
        overflow: hidden;
    }
    .card.ember {
        border-color: color-mix(in srgb, var(--ember) 35%, var(--border));
    }
    .card.cyan {
        border-color: color-mix(in srgb, var(--cyan) 35%, var(--border));
    }
    header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 1rem;
        padding: 0.85rem 1.1rem;
        border-bottom: 1px solid var(--border-soft);
    }
    h3 {
        font-size: 0.8rem;
        text-transform: uppercase;
        letter-spacing: 0.08em;
        color: var(--text-dim);
    }
    .body {
        padding: 1.1rem;
    }
</style>
