import { cp, copyFile, mkdir, readdir } from 'node:fs/promises';
import { join, resolve } from 'node:path';

const projectRoot = resolve(import.meta.dirname, '..');
const webRoot = join(projectRoot, 'Web');
const outputRoot = join(projectRoot, 'dist');

await mkdir(outputRoot, { recursive: true });

const entries = await readdir(webRoot, { withFileTypes: true });
for (const entry of entries) {
  if (entry.isFile() && /\.(html|js|css)$/i.test(entry.name)) {
    await copyFile(join(webRoot, entry.name), join(outputRoot, entry.name));
  }
}

for (const directory of ['assets', 'templates']) {
  await cp(join(webRoot, directory), join(outputRoot, directory), {
    recursive: true,
    force: true
  });
}

console.log('Web assets prepared for Vercel.');
