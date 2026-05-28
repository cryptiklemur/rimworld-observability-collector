<script lang="ts">
    import { t } from '../i18n';
    import { api, type EstimateBundleResult } from '../api';
    import { bytes } from '../format';
    import Icon from './Icon.svelte';

    interface ExportPayload {
        sessionId: string;
        includes: string[];
        force: boolean;
    }
    interface Props {
        sessionId: string;
        onExport: (p: ExportPayload) => void | Promise<void>;
        defaultIncludes?: string[];
    }
    let { sessionId, onExport, defaultIncludes = [] }: Props = $props();

    const allKeys: { key: string; labelKey: string }[] = [
        { key: 'allocations', labelKey: 'bundle.export.include.allocations' },
        { key: 'gc-events', labelKey: 'bundle.export.include.gcEvents' },
        { key: 'call-hierarchy', labelKey: 'bundle.export.include.callHierarchy' },
        { key: 'patches', labelKey: 'bundle.export.include.patches' },
        { key: 'metrics-sqlite', labelKey: 'bundle.export.include.metricsSqlite' },
    ];

    let selected = $state<Set<string>>(new Set(defaultIncludes));
    let force = $state(false);
    let submitting = $state(false);
    let estimate = $state<EstimateBundleResult | null>(null);

    let overCap = $derived(estimate?.kind === 'ok' && estimate.exceedsSoftCap);

    function toggle(key: string) {
        if (selected.has(key)) selected.delete(key);
        else selected.add(key);
        selected = new Set(selected);
    }

    $effect(() => {
        const sid = sessionId;
        const includes = Array.from(selected);
        const handle = setTimeout(async () => {
            const result = await api.estimateBundle(sid, includes);
            estimate = result;
            if (!(result.kind === 'ok' && result.exceedsSoftCap)) force = false;
        }, 300);
        return () => clearTimeout(handle);
    });

    async function submit() {
        if (submitting) return;
        submitting = true;
        try {
            await onExport({ sessionId, includes: Array.from(selected), force });
        } finally {
            submitting = false;
        }
    }
</script>

<form
    onsubmit={(e) => {
        e.preventDefault();
        submit();
    }}
>
    <fieldset>
        <legend>{t('bundle.export.contents')}</legend>
        <p class="hint">{t('bundle.export.contentsHint')}</p>
        <div class="checks">
            {#each allKeys as { key, labelKey } (key)}
                <label for={`bundle-include-${key}`} class:on={selected.has(key)}>
                    <input
                        id={`bundle-include-${key}`}
                        type="checkbox"
                        checked={selected.has(key)}
                        onchange={() => toggle(key)}
                    />
                    <span>{t(labelKey)}</span>
                </label>
            {/each}
        </div>
    </fieldset>

    <div class="estimate" aria-live="polite">
        <span class="estimate-label">{t('bundle.export.estimateLabel')}</span>
        {#if estimate?.kind === 'ok'}
            <span class="estimate-value" class:over={overCap}>{bytes(estimate.estimatedBytes)}</span>
        {:else if estimate?.kind === 'error'}
            <span class="estimate-value muted">{t('bundle.export.estimateUnavailable')}</span>
        {:else}
            <span class="estimate-value muted">{t('bundle.export.estimating')}</span>
        {/if}
    </div>

    {#if overCap}
        <p class="over-warning" role="alert">{t('bundle.export.overCapWarning')}</p>
        <label class="force" for="bundle-force">
            <input id="bundle-force" type="checkbox" bind:checked={force} />
            <span class="force-text">
                <span class="force-label">{t('bundle.export.force')}</span>
                <span class="hint">{t('bundle.export.forceHint')}</span>
            </span>
        </label>
    {/if}

    <button type="submit" class="primary" disabled={submitting} aria-busy={submitting}>
        <Icon name="download" size={16} />
        {submitting ? t('bundle.export.submitting') : t('bundle.export.submit')}
    </button>
</form>

<style>
    form {
        display: flex;
        flex-direction: column;
        gap: 1.1rem;
    }
    fieldset {
        border: 1px solid var(--border-soft);
        border-radius: var(--r-md);
        padding: 0.9rem 1rem 1rem;
        margin: 0;
        background: color-mix(in srgb, var(--bg-base) 40%, transparent);
    }
    legend {
        font-size: 0.72rem;
        text-transform: uppercase;
        letter-spacing: 0.07em;
        color: var(--text-faint);
        padding: 0 0.4rem;
    }
    .hint {
        margin: 0;
        font-size: 0.76rem;
        line-height: 1.45;
        color: var(--text-faint);
    }
    .checks {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(180px, 1fr));
        gap: 0.3rem;
        margin-top: 0.75rem;
    }
    .checks label {
        display: flex;
        align-items: center;
        gap: 0.6rem;
        padding: 0.45rem 0.55rem;
        border: 1px solid transparent;
        border-radius: var(--r-sm);
        cursor: pointer;
        font-size: 0.86rem;
        color: var(--text-dim);
        transition:
            background var(--t-fast) var(--ease-out),
            border-color var(--t-fast) var(--ease-out),
            color var(--t-fast) var(--ease-out);
    }
    .checks label:hover {
        background: var(--bg-elev);
        color: var(--text);
    }
    .checks label.on {
        border-color: color-mix(in srgb, var(--cyan) 35%, transparent);
        background: color-mix(in srgb, var(--cyan) 9%, transparent);
        color: var(--text);
    }
    input[type='checkbox'] {
        accent-color: var(--cyan);
        width: 16px;
        height: 16px;
        flex: none;
        cursor: pointer;
    }
    .estimate {
        display: flex;
        align-items: baseline;
        justify-content: space-between;
        gap: 0.75rem;
        padding: 0.55rem 0.75rem;
        border-radius: var(--r-sm);
        background: color-mix(in srgb, var(--bg-base) 40%, transparent);
        font-size: 0.82rem;
    }
    .estimate-label {
        color: var(--text-faint);
        text-transform: uppercase;
        letter-spacing: 0.07em;
        font-size: 0.72rem;
    }
    .estimate-value {
        font-variant-numeric: tabular-nums;
        font-weight: 600;
        color: var(--text);
    }
    .estimate-value.muted {
        font-weight: 400;
        color: var(--text-faint);
    }
    .estimate-value.over {
        color: var(--ember-soft);
    }
    .over-warning {
        margin: 0;
        padding: 0.55rem 0.75rem;
        border-radius: var(--r-sm);
        background: color-mix(in srgb, var(--ember) 12%, transparent);
        color: var(--ember-soft);
        font-size: 0.78rem;
        line-height: 1.45;
    }
    .force {
        display: flex;
        align-items: flex-start;
        gap: 0.6rem;
        cursor: pointer;
    }
    .force input {
        accent-color: var(--ember);
        margin-top: 0.15rem;
    }
    .force-text {
        display: flex;
        flex-direction: column;
        gap: 0.15rem;
    }
    .force-label {
        font-size: 0.86rem;
        color: var(--text);
    }
    .primary {
        align-self: flex-start;
        display: inline-flex;
        align-items: center;
        gap: 0.5rem;
        padding: 0.5rem 1rem;
        font-family: var(--font-ui);
        font-size: 0.84rem;
        font-weight: 600;
        color: var(--ember-soft);
        background: color-mix(in srgb, var(--ember) 14%, var(--bg-elev));
        border: 1px solid color-mix(in srgb, var(--ember) 40%, transparent);
        border-radius: var(--r-md);
        cursor: pointer;
        transition:
            border-color var(--t-fast) var(--ease-out),
            background var(--t-fast) var(--ease-out);
    }
    .primary:hover:not(:disabled) {
        border-color: var(--ember);
        background: color-mix(in srgb, var(--ember) 20%, var(--bg-elev));
    }
    .primary:disabled {
        opacity: 0.55;
        cursor: progress;
    }
</style>
