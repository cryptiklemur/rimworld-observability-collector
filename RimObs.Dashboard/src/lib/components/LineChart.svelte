<script lang="ts">
    import uPlot from 'uplot';
    import 'uplot/dist/uPlot.min.css';
    import { onMount, onDestroy } from 'svelte';
    import { cssVar as readVar, resolveColor as readColor } from '../theme';

    interface SeriesSpec {
        label: string;
        values: number[];
        stroke: string;
        fill?: string;
    }

    let {
        x,
        series,
        height = 240,
        format = (n: number) => `${n}`,
        xFormat = (n: number) => `${n}`,
    }: {
        x: number[];
        series: SeriesSpec[];
        height?: number;
        format?: (n: number) => string;
        xFormat?: (n: number) => string;
    } = $props();

    let host: HTMLDivElement;
    let chart: uPlot | null = null;
    let ro: ResizeObserver | null = null;

    function cssVar(name: string): string {
        return readVar(host, name);
    }

    function resolveColor(color: string | undefined): string | undefined {
        return readColor(host, color);
    }

    function data(): uPlot.AlignedData {
        return [x, ...series.map((s) => s.values)] as uPlot.AlignedData;
    }

    function build(): uPlot {
        const grid = cssVar('--border-soft');
        const axisColor = cssVar('--text-faint');
        const font = `11px ${cssVar('--font-mono') || 'monospace'}`;
        const axis = {
            stroke: axisColor,
            grid: { stroke: grid, width: 1 },
            ticks: { stroke: grid, width: 1 },
            font,
        };
        const opts: uPlot.Options = {
            width: host.clientWidth || 600,
            height,
            scales: { x: { time: false } },
            legend: { show: false },
            cursor: { y: false, points: { size: 6 } },
            axes: [
                { ...axis, values: (_u, vals) => vals.map((v) => xFormat(v)) },
                { ...axis, size: 64, values: (_u, vals) => vals.map((v) => format(v)) },
            ],
            series: [
                { label: 'x', value: (_u, v) => (v == null ? '' : xFormat(v)) },
                ...series.map((s) => ({
                    label: s.label,
                    stroke: resolveColor(s.stroke),
                    fill: resolveColor(s.fill),
                    width: 2,
                    points: { show: false },
                    value: (_u: uPlot, v: number | null) => (v == null ? '' : format(v)),
                })),
            ],
        };
        return new uPlot(opts, data(), host);
    }

    onMount(() => {
        chart = build();
        ro = new ResizeObserver(() => {
            if (chart) chart.setSize({ width: host.clientWidth, height });
        });
        ro.observe(host);
    });

    onDestroy(() => {
        ro?.disconnect();
        chart?.destroy();
        chart = null;
    });

    $effect(() => {
        if (chart) chart.setData(data());
    });
</script>

<div class="chart" bind:this={host}></div>

<style>
    .chart {
        width: 100%;
    }
    .chart :global(.u-axis) {
        color: var(--text-faint);
    }
</style>
