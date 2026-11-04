/**
 * ACH Interbank — EXPERIMENTAL, scoped remediation of the modern npm toolchain.
 * Leaves Karma's legacy glob@7/minimatch@3 untouched.
 * Usage (from web/ach-interbank-ui):
 *   node scripts/apply-moderate-security.mjs
 *   node scripts/apply-moderate-security.mjs --rollback
 *
 * Never invokes npm. All changes to package-lock.json must be made by npm.
 */
import { existsSync, readFileSync, writeFileSync, mkdirSync, copyFileSync, rmSync } from 'node:fs';
import { resolve, join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = process.cwd();
const packagePath = join(root, 'package.json');
const lockPath = join(root, 'package-lock.json');
const verifierPath = join(root, 'scripts', 'verify-security-lock.mjs');
const verifierSource = resolve(dirname(fileURLToPath(import.meta.url)), 'verify-security-lock-updated.mjs');
const backupDir = join(root, '.npm-security-moderadas-backup');
const paths = {
  'package.json': packagePath,
  'package-lock.json': lockPath,
  'verify-security-lock.mjs': verifierPath
};

function abort(msg) { console.error(`ERROR: ${msg}`); process.exit(1); }
if (!existsSync(packagePath) || !existsSync(lockPath) || !existsSync(verifierPath)) {
  abort('Execute from the frontend root, with package.json, package-lock.json, and scripts/verify-security-lock.mjs.');
}

if (process.argv.includes('--rollback')) {
  if (!existsSync(backupDir)) abort('No backup found; nothing to restore.');
  for (const [name, dest] of Object.entries(paths)) {
    const source = join(backupDir, name);
    if (!existsSync(source)) abort(`Backup missing: ${name}. Rollback stopped.`);
  }
  for (const [name, dest] of Object.entries(paths)) {
    copyFileSync(join(backupDir, name), dest);
    console.log(`Restored ${name}`);
  }
  rmSync(backupDir, { recursive: true, force: true });
  console.log('Restored the previous manifest, lockfile and static verifier.');
  process.exit(0);
}

if (!existsSync(verifierSource)) abort('Copy verify-security-lock-updated.mjs alongside this script first.');

const original = readFileSync(packagePath, 'utf8');
const pkg = JSON.parse(original);
if (pkg.name !== 'ach-interbank-ui') abort('Unexpected project name.');
if (!/^\^?21\./.test(pkg.devDependencies?.['@angular/cli'] ?? '')) abort('This patch is limited to Angular CLI 21.');
if (!/^\^?6\.4\./.test(pkg.devDependencies?.karma ?? '')) abort('Expected Karma 6.4.x; no changes made.');
if (pkg.overrides == null || typeof pkg.overrides !== 'object' || Array.isArray(pkg.overrides)) {
  abort('Expected an existing overrides object in package.json.');
}

const scoped = {
  '@npmcli/package-json': { glob: '14.0.2' },
  'cacache': { glob: '14.0.2' },
  '@tufjs/models': { minimatch: '11.0.1' },
  'ignore-walk': { minimatch: '11.0.1' }
};
const overrides = { ...pkg.overrides };
for (const [parent, children] of Object.entries(scoped)) {
  const old = overrides[parent];
  if (old != null && (typeof old !== 'object' || Array.isArray(old))) {
    abort(`Conflicting override object for ${parent}; inspect manually.`);
  }
  for (const [child, version] of Object.entries(children)) {
    if (old?.[child] && old[child] !== version) {
      abort(`Existing ${parent} > ${child} override is ${old[child]}, not ${version}; inspect manually.`);
    }
  }
  overrides[parent] = { ...(old ?? {}), ...children };
}
pkg.overrides = overrides;
const updated = JSON.stringify(pkg, null, 2) + '\n';
if (updated === original && readFileSync(verifierPath, 'utf8') === readFileSync(verifierSource, 'utf8')) {
  console.log('Already applied; no changes needed.');
  process.exit(0);
}
if (existsSync(backupDir)) {
  abort('An earlier backup exists. Do not overwrite it; inspect changes or roll back before applying again.');
}
mkdirSync(backupDir);
for (const [name, source] of Object.entries(paths)) copyFileSync(source, join(backupDir, name));
writeFileSync(packagePath, updated, 'utf8');
copyFileSync(verifierSource, verifierPath);
console.log('Updated scoped overrides and static verifier (backup saved in .npm-security-moderadas-backup).');
console.log('Karma dependency versions were NOT changed in package.json.');
console.log('Next: npm run security:refresh-lock && npm run security:verify-lock && npm ci');
console.log('If build/tests fail: node scripts/apply-moderate-security.mjs --rollback && npm ci');
