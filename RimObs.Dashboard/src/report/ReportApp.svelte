<script lang="ts">
    let { raw }: { raw: unknown } = $props();
</script>

<main class="report-root">
    <h1>RimObs Diagnostic Report</h1>
    {#if raw === null}
        <p>No bundle data present.</p>
    {:else}
        <pre>{JSON.stringify(raw, null, 2)}</pre>
    {/if}
</main>

<style>
    .report-root {
        font-family: 'Hanken Grotesk', sans-serif;
        max-width: 1280px;
        margin: 0 auto;
        padding: 2rem;
    }
</style>
