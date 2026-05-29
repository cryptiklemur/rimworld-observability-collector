<script lang="ts">
    import { routes, router } from '../router.svelte';
    import Icon from './Icon.svelte';
    import { t } from '../i18n';
</script>

<aside>
    <div class="brand">
        <div class="glyph"><Icon name="gauge" size={22} /></div>
        <div class="title">
            <strong>RimObs</strong>
            <span>{t('app.subtitle')}</span>
        </div>
    </div>

    <nav>
        {#each routes as r (r.id)}
            <a
                href="#/{r.id}"
                class="item"
                class:active={router.current === r.id}
                aria-current={router.current === r.id ? 'page' : undefined}
                aria-label={t(`nav.${r.id}`, r.title)}
                title={t(`nav.${r.id}`, r.title)}
            >
                <Icon name={r.icon} size={17} />
                <span>{t(`nav.${r.id}`, r.title)}</span>
            </a>
        {/each}
    </nav>
</aside>

<style>
    aside {
        grid-area: sidebar;
        width: var(--sb-w);
        background: linear-gradient(
            180deg,
            color-mix(in srgb, var(--bg-surface) 60%, transparent),
            color-mix(in srgb, var(--bg-base) 60%, transparent)
        );
        border-right: 1px solid var(--border-soft);
        display: flex;
        flex-direction: column;
        overflow-y: auto;
        backdrop-filter: blur(6px);
    }
    .brand {
        display: flex;
        align-items: center;
        gap: 0.7rem;
        padding: 1.1rem 1.2rem;
        height: var(--topbar-h);
        border-bottom: 1px solid var(--border-soft);
    }
    .glyph {
        display: grid;
        place-items: center;
        width: 34px;
        height: 34px;
        border-radius: var(--r-md);
        color: var(--bg-void);
        background: linear-gradient(135deg, var(--ember), var(--ember-deep));
        box-shadow: 0 4px 14px color-mix(in srgb, var(--ember) 40%, transparent);
    }
    .title {
        display: flex;
        flex-direction: column;
        line-height: 1.1;
    }
    .title strong {
        font-family: var(--font-display);
        font-size: 1.05rem;
        letter-spacing: 0.04em;
    }
    .title span {
        font-size: 0.68rem;
        text-transform: uppercase;
        letter-spacing: 0.16em;
        color: var(--text-faint);
    }
    nav {
        padding: 0.7rem 0.6rem;
        display: flex;
        flex-direction: column;
        gap: 2px;
    }
    .item {
        display: flex;
        align-items: center;
        gap: 0.7rem;
        padding: 0.55rem 0.7rem;
        border-radius: var(--r-md);
        color: var(--text-dim);
        font-size: 0.88rem;
        font-weight: 500;
        position: relative;
        transition:
            background var(--t-fast) var(--ease-out),
            color var(--t-fast) var(--ease-out);
    }
    .item:hover {
        background: var(--bg-surface);
        color: var(--text);
    }
    .item.active {
        background: color-mix(in srgb, var(--ember) 12%, var(--bg-surface));
        color: var(--text);
    }
    .item.active::before {
        content: '';
        position: absolute;
        left: -0.6rem;
        top: 18%;
        bottom: 18%;
        width: 3px;
        border-radius: 0 3px 3px 0;
        background: var(--ember);
    }
    @media (max-width: 900px) {
        .brand {
            justify-content: center;
            padding: 0;
            gap: 0;
        }
        .title {
            display: none;
        }
        nav {
            padding: 0.7rem 0;
            align-items: center;
        }
        .item {
            justify-content: center;
            gap: 0;
            padding: 0.6rem;
            width: 40px;
        }
        .item span {
            display: none;
        }
        .item.active::before {
            left: -0.4rem;
        }
    }
</style>
