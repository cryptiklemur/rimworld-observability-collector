<script lang="ts">
    import { api } from '../lib/api';
    import BundleExportForm from '../lib/components/BundleExportForm.svelte';
    import { onMount } from 'svelte';

    let currentSessionId = $state<string | null>(null);
    let exportError = $state<string | null>(null);
    let importing = $state(false);
    let importedToken = $state<string | null>(null);
    let importedManifest = $state<Record<string, unknown> | null>(null);
    let importedContents = $state<string[]>([]);
    let reportOpen = $state(false);

    onMount(async () => {
        try {
            const s = await api.currentSession();
            currentSessionId = s?.session?.id ?? null;
        } catch {
            currentSessionId = null;
        }
    });

    async function handleExport(p: { sessionId: string; includes: string[]; force: boolean }) {
        exportError = null;
        const result = await api.exportBundle(p);
        if (result.kind === 'over_cap') {
            exportError = `Bundle would be ${(result.estimatedBytes / 1_048_576).toFixed(1)} MB (cap ${(result.capBytes / 1_048_576).toFixed(1)} MB). Tick "Export anyway" to override.`;
            return;
        }
        if (result.kind === 'error') {
            exportError = result.message;
            return;
        }
        const url = URL.createObjectURL(result.blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `${p.sessionId}.rimobs.zip`;
        a.click();
        URL.revokeObjectURL(url);
    }

    async function handleFile(event: Event) {
        const target = event.target as HTMLInputElement;
        const file = target.files?.[0];
        if (!file) return;
        importing = true;
        try {
            const r = await api.importBundle(file);
            importedToken = r.token;
            importedManifest = r.manifest;
            importedContents = r.contents;
        } finally {
            importing = false;
        }
    }

    async function discardImport() {
        if (importedToken) await api.deleteImport(importedToken);
        importedToken = null;
        importedManifest = null;
        importedContents = [];
        reportOpen = false;
    }
</script>

<section class="bundle-page">
    <h1>Diagnostic Bundles</h1>

    <article class="export">
        <h2>Export</h2>
        {#if currentSessionId === null}
            <p>No active session.</p>
        {:else}
            <BundleExportForm sessionId={currentSessionId} onExport={handleExport} />
            {#if exportError}
                <p class="error">{exportError}</p>
            {/if}
        {/if}
    </article>

    <article class="import">
        <h2>Import preview</h2>
        {#if importedToken === null}
            <input type="file" accept=".zip" onchange={handleFile} disabled={importing} />
        {:else}
            <div class="manifest">
                <h3>Manifest</h3>
                <pre>{JSON.stringify(importedManifest, null, 2)}</pre>
                <h3>Contents</h3>
                <ul>
                    {#each importedContents as name}
                        <li>
                            <a href={api.getImportFileUrl(importedToken, name)} download={name}>{name}</a>
                        </li>
                    {/each}
                </ul>
                <button onclick={() => (reportOpen = true)} disabled={!importedContents.includes('report.html')}>Open report</button>
                <button onclick={discardImport}>Discard</button>
            </div>
            {#if reportOpen}
                <div class="report-modal" role="dialog">
                    <button class="close" onclick={() => (reportOpen = false)}>Close</button>
                    <iframe title="Diagnostic report" src={api.getImportFileUrl(importedToken, 'report.html')}></iframe>
                </div>
            {/if}
        {/if}
    </article>
</section>

<style>
    .bundle-page { padding: 1.5rem; display: grid; gap: 2rem; }
    .error { color: #d32f2f; }
    .report-modal {
        position: fixed; inset: 4rem; background: var(--bg, #111); border: 1px solid var(--border, #333);
        display: flex; flex-direction: column;
    }
    .report-modal iframe { flex: 1; border: none; background: white; }
    .close { align-self: flex-start; padding: 0.5rem 1rem; margin: 0.5rem; }
</style>
