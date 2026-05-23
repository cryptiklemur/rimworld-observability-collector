<script lang="ts">
    import {
        api,
        ApiError,
        type InstrumentationPatchesResponse,
        type InstrumentationSearchResponse,
        type MethodDescriptor,
        type InstrumentationPatchEntry,
    } from '../lib/api';
    import { Resource } from '../lib/poll.svelte';
    import { t } from '../lib/i18n';
    import { onMount, onDestroy } from 'svelte';

    let searchUnavailable = $state(false);
    let query = $state('');
    let searchResults = $state<MethodDescriptor[]>([]);
    let searchLoading = $state(false);
    const patches = new Resource<InstrumentationPatchesResponse>(
        () => api.instrumentationPatches(),
        5000,
    );

    onMount(() => patches.start());
    onDestroy(() => patches.stop());

    let unavailable = $derived(searchUnavailable || (patches.state === 'error' && !patches.data));

    let debounceHandle: ReturnType<typeof setTimeout> | null = null;
    function onInput(e: Event) {
        query = (e.target as HTMLInputElement).value;
        if (debounceHandle) clearTimeout(debounceHandle);
        debounceHandle = setTimeout(() => void runSearch(), 250);
    }

    async function runSearch() {
        if (!query.trim()) {
            searchResults = [];
            return;
        }
        searchLoading = true;
        try {
            const res: InstrumentationSearchResponse = await api.instrumentationSearch(query, 30);
            searchResults = res.results;
            searchUnavailable = false;
        } catch (e: unknown) {
            if (e instanceof ApiError && e.status === 503) {
                searchUnavailable = true;
                searchResults = [];
            }
        } finally {
            searchLoading = false;
        }
    }

    async function instrument(m: MethodDescriptor) {
        await api.instrumentationPatch({
            typeFullName: m.typeFullName,
            methodName: m.methodName,
            paramTypeFullNames: m.paramTypeFullNames,
        });
        await patches.refresh();
    }

    async function remove(id: number) {
        await api.instrumentationUnpatch(id);
        await patches.refresh();
    }

    let active = $derived<InstrumentationPatchEntry[]>(patches.data?.persisted ?? []);
</script>

{#if unavailable}
    <p class="unavailable">{t('instrumentation.unavailable')}</p>
{:else}
    <div class="search">
        <input
            type="search"
            placeholder={t('instrumentation.search.placeholder')}
            value={query}
            oninput={onInput}
        />
        {#if searchLoading}
            <span class="dim">…</span>
        {/if}
    </div>

    {#if !query.trim() && searchResults.length === 0}
        <p class="empty dim">{t('instrumentation.search.empty')}</p>
    {/if}

    {#if query.trim() && searchResults.length === 0 && !searchLoading}
        <p class="empty dim">{t('instrumentation.search.noresults')}</p>
    {/if}

    {#if searchResults.length > 0}
        <ul class="results">
            {#each searchResults as m (m.signature + m.assemblyName)}
                <li>
                    <span class="mono sig">{m.signature}</span>
                    <span class="dim asm">{m.assemblyName}</span>
                    <button onclick={() => instrument(m)}>{t('instrumentation.results.button')}</button>
                </li>
            {/each}
        </ul>
    {/if}

    <h2>{t('instrumentation.active.title')}</h2>
    {#if active.length === 0}
        <p class="empty dim">{t('instrumentation.active.empty')}</p>
    {:else}
        <ul class="active">
            {#each active as p (p.id)}
                <li>
                    <span class="mono sig">{p.typeFullName}.{p.methodName}({p.paramTypesJoined})</span>
                    <span class="pill pill-{p.lastStatus}">{t(`instrumentation.status.${p.lastStatus}`)}</span>
                    {#if p.lastError}
                        <span class="dim mono">{p.lastError}</span>
                    {/if}
                    <button onclick={() => remove(p.id)}>{t('instrumentation.remove')}</button>
                </li>
            {/each}
        </ul>
    {/if}
{/if}

<style>
    .search {
        display: flex;
        gap: 0.5rem;
        margin-bottom: 0.8rem;
    }
    .search input {
        flex: 1;
        padding: 0.4rem 0.6rem;
    }
    .results,
    .active {
        list-style: none;
        padding: 0;
        margin: 0;
        display: flex;
        flex-direction: column;
    }
    .results li,
    .active li {
        display: grid;
        grid-template-columns: 1fr auto auto;
        gap: 0.6rem;
        align-items: center;
        padding: 0.4rem 0.2rem;
        border-bottom: 1px solid var(--border-soft);
    }
    .dim {
        color: var(--text-dim);
    }
    .pill {
        font-size: 0.7rem;
        padding: 0.1rem 0.4rem;
        border-radius: 99px;
        background: var(--bg-surface);
    }
    .pill-active {
        color: var(--good);
    }
    .pill-stale {
        color: var(--warn);
    }
    .pill-pending {
        color: var(--text-faint);
    }
    .empty {
        padding: 0.6rem 0;
    }
    .unavailable {
        padding: 1rem;
        color: var(--text-faint);
    }
</style>
