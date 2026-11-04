/**
 * SEC-I02: read-only npm lockfile consistency inspection.
 * No external dependencies and no registry access are required.
 * Only a clean npm ci with the target Node/npm and registry can validate actual installation.
 */
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import path from 'node:path';

const cwd = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const pkg = JSON.parse(readFileSync(path.join(cwd, 'package.json'), 'utf8'));
const lock = JSON.parse(readFileSync(path.join(cwd, 'package-lock.json'), 'utf8'));
const errors = [];
const packages = lock.packages ?? {};
const root = packages[''] ?? {};

if (lock.lockfileVersion !== 3) errors.push('Se esperaba npm lockfileVersion=3');
for (const field of ['name', 'version']) {
  if (lock[field] !== pkg[field] || root[field] !== pkg[field]) {
    errors.push(`Campo ${field} diferente entre package.json y package-lock.json`);
  }
}
for (const section of ['dependencies', 'devDependencies']) {
  const wanted = pkg[section] ?? {};
  const found = root[section] ?? {};
  for (const name of new Set([...Object.keys(wanted), ...Object.keys(found)])) {
    if (wanted[name] !== found[name]) errors.push(`${section}: ${name} no sincronizado`);
    if (wanted[name] && !packages[`node_modules/${name}`]) {
      errors.push(`${section}: ${name} no existe en packages`);
    }
  }
}
for (const [name, version] of Object.entries(pkg.overrides ?? {})) {
  const matches = Object.entries(packages).filter(([p]) =>
    p === `node_modules/${name}` || p.endsWith(`/node_modules/${name}`));
  if (matches.length === 0) {
    errors.push(`Override ${name}@${version} no se encuentra en lockfile`);
  }
  for (const [p, metadata] of matches) {
    if (metadata.version !== version) {
      errors.push(`Override ${p}: ${metadata.version} != ${version}`);
    }
  }
}
// Missing npm integrity is NOT repaired by guessing or fabricating SHA values.
const missingIntegrity = Object.entries(packages)
  .filter(([, v]) => /^https:\/\/registry\.npmjs\.org\//.test(v.resolved ?? '') && !v.integrity)
  .map(([name]) => name);

if (errors.length) {
  console.error(`SEC-I02 LOCK FAIL: ${errors.length} inconsistencias`);
  for (const err of errors) console.error(` - ${err}`);
  process.exitCode = 1;
} else {
  console.log(`SEC-I02 LOCK PASS_STATIC: ${Object.keys(packages).length} packages; manifests sincronizados; overrides revisados.`);
}
if (missingIntegrity.length) {
  console.warn(`SEC-I02 LOCK WARN: ${missingIntegrity.length} packages del registry sin hash integrity (pendiente SEC-I03).`);
  if (process.argv.includes('--strict-integrity')) process.exitCode = 1;
}
