<script lang="ts">
    import { onMount, onDestroy } from 'svelte';
    import { api, type StatusResponse } from './lib/api';
    import { Resource } from './lib/poll.svelte';
    import { router } from './lib/router.svelte';
    import Sidebar from './lib/components/Sidebar.svelte';
    import TopBar from './lib/components/TopBar.svelte';
    import Overview from './routes/Overview.svelte';
    import Hotspots from './routes/Hotspots.svelte';
    import CallTree from './routes/CallTree.svelte';
    import Memory from './routes/Memory.svelte';
    import Metrics from './routes/Metrics.svelte';
    import Patches from './routes/Patches.svelte';
    import Sessions from './routes/Sessions.svelte';
    import Logs from './routes/Logs.svelte';
    import Settings from './routes/Settings.svelte';
    import Soon from './routes/Soon.svelte';

    const status = new Resource<StatusResponse>(() => api.status(), 2000);

    onMount(() => {
        router.start();
        status.start();
    });
    onDestroy(() => status.stop());

    let route = $derived(router.route);
</script>

<div class="shell">
    <Sidebar />
    <TopBar status={status.data} />
    <main class="main" id="main">
        {#key route.id}
            <div class="view">
                {#if route.id === 'overview'}
                    <Overview status={status.data} />
                {:else if route.id === 'hotspots'}
                    <Hotspots />
                {:else if route.id === 'calltree'}
                    <CallTree />
                {:else if route.id === 'memory'}
                    <Memory />
                {:else if route.id === 'metrics'}
                    <Metrics />
                {:else if route.id === 'patches'}
                    <Patches />
                {:else if route.id === 'sessions'}
                    <Sessions />
                {:else if route.id === 'logs'}
                    <Logs />
                {:else if route.id === 'settings'}
                    <Settings />
                {:else}
                    <Soon title={route.title} />
                {/if}
            </div>
        {/key}
    </main>
</div>

<style>
    .shell {
        display: grid;
        grid-template-columns: var(--sb-w) 1fr;
        grid-template-rows: var(--topbar-h) 1fr;
        grid-template-areas:
            'sidebar topbar'
            'sidebar main';
        height: 100vh;
        overflow: hidden;
    }
    .main {
        grid-area: main;
        overflow-y: auto;
        padding: 1.5rem 1.8rem 3rem;
    }
    .view {
        max-width: 1320px;
        margin: 0 auto;
    }
</style>
