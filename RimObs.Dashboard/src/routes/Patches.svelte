<script lang="ts">
    import { api, type PatchesResponse, type PatchConflict } from '../lib/api';
    import { Resource } from '../lib/poll.svelte';
    import DataState from '../lib/components/DataState.svelte';
    import { patchType } from '../lib/format';
    import { t } from '../lib/i18n';
    import { onMount, onDestroy } from 'svelte';

    const res = new Resource<PatchesResponse>(() => api.patches(), 5000);
    onMount(() => res.start());
    onDestroy(() => res.stop());

    interface Group {
        section: string;
        target: string;
        rows: PatchConflict[];
    }

    let conflicts = $derived(res.data?.conflicts ?? []);
    let groups = $derived.by(() => {
        const map = new Map<string, Group>();
        for (const c of conflicts) {
            let g = map.get(c.section);
            if (!g) {
                g = { section: c.section, target: c.target_method, rows: [] };
                map.set(c.section, g);
            }
            g.rows.push(c);
        }
        return [...map.values()];
    });
</script>

<DataState
    state={res.state}
    error={res.error}
    empty={conflicts.length === 0}
    emptyTitle={t('patches.empty')}
    emptyHint={t('patches.empty.hint')}
    onretry={() => res.refresh()}
>
    <p class="intro">{t('patches.intro')}</p>

    <div class="groups">
        {#each groups as g (g.section)}
            <article class="group">
                <header>
                    <span class="label">{t('patches.section')}</span>
                    <span class="section mono">{g.section}</span>
                    <span class="target mono">{g.target}</span>
                </header>
                <div class="table">
                    <div class="head">
                        <span>{t('patches.col.owner')}</span>
                        <span>{t('patches.col.type')}</span>
                        <span class="num">{t('patches.col.priority')}</span>
                        <span>{t('patches.col.method')}</span>
                    </div>
                    {#each g.rows as c, i (c.other_owner + c.patch_method + i)}
                        <div class="rowline">
                            <span class="owner mono">{c.other_owner}</span>
                            <span class="type pt{c.patch_type}">{patchType(c.patch_type)}</span>
                            <span class="num mono dim">{c.priority}</span>
                            <span class="method mono dim">{c.patch_method}</span>
                        </div>
                    {/each}
                </div>
            </article>
        {/each}
    </div>
</DataState>

<style>
    .intro {
        color: var(--text-dim);
        font-size: 0.85rem;
        margin: 0 0 1.2rem;
        max-width: 70ch;
        line-height: 1.5;
    }
    .groups {
        display: flex;
        flex-direction: column;
        gap: 1rem;
    }
    .group {
        border: 1px solid var(--border-soft);
        border-radius: var(--r-lg);
        background: linear-gradient(180deg, var(--bg-surface), var(--bg-surface-2));
        overflow: hidden;
    }
    header {
        display: flex;
        align-items: baseline;
        gap: 0.6rem;
        flex-wrap: wrap;
        padding: 0.7rem 0.9rem;
        border-bottom: 1px solid var(--border-soft);
    }
    .label {
        font-size: 0.62rem;
        text-transform: uppercase;
        letter-spacing: 0.08em;
        color: var(--text-faint);
    }
    .section {
        font-size: 0.9rem;
        color: var(--text);
        font-weight: 600;
    }
    .target {
        font-size: 0.74rem;
        color: var(--text-faint);
    }
    .head,
    .rowline {
        display: grid;
        grid-template-columns: minmax(120px, 1fr) minmax(80px, 0.5fr) minmax(64px, 0.4fr) minmax(140px, 1.6fr);
        gap: 0.75rem;
        align-items: center;
        padding: 0.5rem 0.9rem;
    }
    .head {
        font-size: 0.68rem;
        text-transform: uppercase;
        letter-spacing: 0.07em;
        color: var(--text-faint);
        border-bottom: 1px solid var(--border-soft);
    }
    .rowline {
        border-bottom: 1px solid var(--border-soft);
    }
    .rowline:last-child {
        border-bottom: none;
    }
    .rowline:hover {
        background: var(--bg-surface);
    }
    .owner {
        font-size: 0.82rem;
        color: var(--text);
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
    }
    .method {
        font-size: 0.76rem;
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
    }
    .num {
        text-align: right;
        font-size: 0.82rem;
    }
    .dim {
        color: var(--text-faint);
    }
    .type {
        font-size: 0.64rem;
        text-transform: uppercase;
        letter-spacing: 0.05em;
        padding: 0.1rem 0.5rem;
        border-radius: 99px;
        justify-self: start;
        white-space: nowrap;
    }
    .pt1 {
        background: color-mix(in srgb, var(--cyan) 16%, transparent);
        color: var(--cyan-soft);
    }
    .pt2 {
        background: color-mix(in srgb, var(--good) 16%, transparent);
        color: var(--good);
    }
    .pt3 {
        background: color-mix(in srgb, var(--ember) 16%, transparent);
        color: var(--ember-soft);
    }
    .pt4 {
        background: color-mix(in srgb, var(--warn) 16%, transparent);
        color: var(--warn);
    }
    .pt0,
    .pt5 {
        background: var(--border-soft);
        color: var(--text-dim);
    }
</style>
