<script lang="ts">
    interface ExportPayload {
        sessionId: string;
        includes: string[];
        force: boolean;
    }
    interface Props {
        sessionId: string;
        onExport: (p: ExportPayload) => void;
        defaultIncludes?: string[];
    }
    let { sessionId, onExport, defaultIncludes = [] }: Props = $props();

    const allKeys: { key: string; label: string }[] = [
        { key: 'allocations', label: 'Allocations' },
        { key: 'gc-events', label: 'GC events' },
        { key: 'call-hierarchy', label: 'Call hierarchy' },
        { key: 'patches', label: 'Patches' },
        { key: 'metrics-sqlite', label: 'Metrics SQLite' },
    ];

    let selected = $state<Set<string>>(new Set(defaultIncludes));
    let force = $state(false);

    function toggle(key: string) {
        if (selected.has(key)) selected.delete(key);
        else selected.add(key);
        selected = new Set(selected);
    }

    function submit() {
        onExport({ sessionId, includes: Array.from(selected), force });
    }
</script>

<form onsubmit={(e) => { e.preventDefault(); submit(); }}>
    <fieldset>
        <legend>Optional contents</legend>
        {#each allKeys as { key, label }}
            <label for={`bundle-include-${key}`}>
                <input
                    id={`bundle-include-${key}`}
                    type="checkbox"
                    checked={selected.has(key)}
                    onchange={() => toggle(key)}
                />
                {label}
            </label>
        {/each}
    </fieldset>
    <label class="force" for="bundle-force">
        <input id="bundle-force" type="checkbox" bind:checked={force} />
        Export anyway (override 25MB soft cap)
    </label>
    <button type="submit">Export</button>
</form>

<style>
    form { display: flex; flex-direction: column; gap: 1rem; }
    fieldset { display: flex; flex-direction: column; gap: 0.5rem; border: 1px solid var(--border, #444); padding: 1rem; }
    legend { padding: 0 0.5rem; }
    label { display: flex; align-items: center; gap: 0.5rem; cursor: pointer; }
    button { padding: 0.5rem 1rem; cursor: pointer; }
</style>
