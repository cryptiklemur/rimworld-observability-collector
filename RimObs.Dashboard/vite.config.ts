import { defineConfig } from 'vite';
import { svelte } from '@sveltejs/vite-plugin-svelte';

export default defineConfig({
    plugins: [svelte()],
    base: '/',
    build: {
        outDir: 'dist',
        target: 'es2022',
        sourcemap: true,
        emptyOutDir: true,
    },
    server: {
        port: 5173,
        host: '127.0.0.1',
    },
});
