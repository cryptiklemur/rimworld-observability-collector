import { mount } from 'svelte';
import ReportApp from './ReportApp.svelte';

declare global {
    interface Window {
        __BUNDLE__?: unknown;
    }
}

mount(ReportApp, {
    target: document.getElementById('app')!,
    props: { raw: window.__BUNDLE__ ?? null },
});
