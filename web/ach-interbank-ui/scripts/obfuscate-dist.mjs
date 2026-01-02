import { readdir, readFile, stat, writeFile } from 'node:fs/promises';
import path from 'node:path';
import process from 'node:process';
import JavaScriptObfuscator from 'javascript-obfuscator';

const distRoot = path.resolve('dist', 'ach-interbank-ui');
const skipPatterns = [/polyfills/i];

async function collectJsFiles(dir) {
  const entries = await readdir(dir, { withFileTypes: true });
  const files = [];

  for (const entry of entries) {
    const fullPath = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      files.push(...await collectJsFiles(fullPath));
      continue;
    }

    if (entry.isFile() && fullPath.endsWith('.js')) {
      files.push(fullPath);
    }
  }

  return files;
}

async function main() {
  const exists = await stat(distRoot).catch(() => null);
  if (!exists) {
    console.error(`No se encontró la carpeta de build: ${distRoot}`);
    process.exit(1);
  }

  const files = await collectJsFiles(distRoot);
  const targets = files.filter((file) => !skipPatterns.some((pattern) => pattern.test(file)));

  for (const file of targets) {
    const content = await readFile(file, 'utf8');
    const result = JavaScriptObfuscator.obfuscate(content, {
      compact: true,
      stringArray: true,
      stringArrayThreshold: 0.75,
      renameGlobals: false
    });
    await writeFile(file, result.getObfuscatedCode(), 'utf8');
  }

  console.log(`Obfuscación completada para ${targets.length} archivos.`);
}

main().catch((error) => {
  console.error(error);
  process.exit(1);
});
