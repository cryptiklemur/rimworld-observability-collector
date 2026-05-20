<script lang="ts">
    import { onMount } from 'svelte';

    let status = $state<string>('loading');
    let version = $state<string>('');

    onMount(async () => {
        try {
            const res = await fetch('/api/v1/status');
            const body = await res.json();
            status = body.status ?? 'unknown';
            version = body.version ?? '';
        } catch (err) {
            status = `error: ${(err as Error).message}`;
        }
    });
</script>

<main>
    <h1>RimWorld Observability</h1>
    <p>Collector status: <code>{status}</code></p>
    {#if version}
        <p>Version: <code>{version}</code></p>
    {/if}
</main>

<style>
    main {
        font-family: ui-sans-serif, system-ui, sans-serif;
        padding: 2rem;
        max-width: 64rem;
        margin: 0 auto;
    }
    h1 {
        font-size: 1.5rem;
        margin: 0 0 1rem 0;
    }
    code {
        background: #1f2733;
        color: #d4e5ff;
        padding: 0.1rem 0.35rem;
        border-radius: 0.25rem;
        font-family: ui-monospace, monospace;
    }
</style>
