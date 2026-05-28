import { copyFileSync, mkdirSync, rmSync, existsSync } from 'node:fs';
import { resolve } from 'node:path';

const src = resolve(process.cwd(), 'dist-report/src/report/index.html');
const dst = resolve(process.cwd(), 'dist/report.html');
if (!existsSync(src)) {
    console.error('Expected report build output at', src);
    process.exit(1);
}
mkdirSync(resolve(process.cwd(), 'dist'), { recursive: true });
copyFileSync(src, dst);
rmSync(resolve(process.cwd(), 'dist-report'), { recursive: true, force: true });
console.log('Wrote dist/report.html');
