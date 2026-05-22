<script lang="ts">
    import Icon from '../lib/components/Icon.svelte';
    import { t } from '../lib/i18n';

    let { title }: { title: string } = $props();
</script>

<div class="soon">
    <span class="glyph"><Icon name="cog" size={28} /></span>
    <h2>{title}</h2>
    <p>{t('common.soon')}</p>
</div>

<style>
    .soon {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        gap: 0.6rem;
        min-height: 50vh;
        text-align: center;
    }
    .glyph {
        display: grid;
        place-items: center;
        width: 64px;
        height: 64px;
        border-radius: var(--r-lg);
        background: linear-gradient(
            135deg,
            color-mix(in srgb, var(--cyan) 14%, transparent),
            color-mix(in srgb, var(--ember) 14%, transparent)
        );
        color: var(--cyan-soft);
        margin-bottom: 0.4rem;
    }
    h2 {
        font-family: var(--font-display);
        font-size: 1.2rem;
        font-weight: 600;
        margin: 0;
    }
    p {
        color: var(--text-faint);
        font-size: 0.85rem;
        margin: 0;
    }
</style>
