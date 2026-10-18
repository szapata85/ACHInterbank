/**
 * Resolve and validate the SPA lockfile using the npm registry.
 * Designed to run from `npm run security:refresh-lock` on Windows, macOS and Linux.
 * Requires registry access. On failure restores package.json and package-lock.json.
 * A nonzero result from npm audit fix alone does NOT invalidate a consistent lockfile:
 * unpatched upstream advisories may remain and must be reviewed separately.
 */
import { spawnSync } from 'node:child_process';
import { readFileSync, writeFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import path from 'node:path';

const projectRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const packagePath = path.join(projectRoot, 'package.json');
const lockPath = path.join(projectRoot, 'package-lock.json');
const originalPackage = readFileSync(packagePath, 'utf8');
const originalLock = readFileSync(lockPath, 'utf8');

// Angular CLI/Express constrain these transitive versions. npm overrides are
// needed to select patched releases without forcing a breaking Angular upgrade.
const securityOverrides = {
  '@modelcontextprotocol/sdk': '1.31.0',
  'proxy-addr': '2.0.8',
  'shell-quote': '1.11.0'
};

// Invoking npm.cmd via spawnSync({ shell: false }) is unreliable on Windows.
// npm populates npm_execpath when running this script from npm run; executing
// its JS CLI with the currently running Node executable is cross-platform.
const npmCliPath = process.env.npm_execpath;
if (!npmCliPath || !/\.(c?js|mjs)$/i.test(npmCliPath)) {
  console.error('Run this command through npm: npm run security:refresh-lock');
  process.exit(1);
}

function runNpm(args, { quiet = false } = {}) {
  const result = spawnSync(process.execPath, [npmCliPath, ...args], {
    cwd: projectRoot,
    shell: false,
    encoding: 'utf8',
    stdio: quiet ? 'pipe' : 'inherit',
    env: process.env
  });
  if (result.error) {
    console.error(`Could not start npm: ${result.error.message}`);
    return false;
  }
  if (result.status !== 0) {
    if (quiet) {
      if (result.stdout) console.error(result.stdout.trim());
      if (result.stderr) console.error(result.stderr.trim());
    }
    console.error(`npm ${args.join(' ')} failed with exit code ${result.status ?? 'unknown'}.`);
    return false;
  }
  return true;
}

function verifyLockfile() {
  const pkg = JSON.parse(readFileSync(packagePath, 'utf8'));
  const lock = JSON.parse(readFileSync(lockPath, 'utf8'));
  if (lock.lockfileVersion < 3) throw new Error('Expected a npm v3 lockfile');
  const lockRoot = lock.packages?.[''];
  if (!lockRoot) throw new Error('Missing root package in lockfile');
  for (const section of ['dependencies', 'devDependencies']) {
    for (const [name, versionRange] of Object.entries(pkg[section] ?? {})) {
      if (lockRoot[section]?.[name] !== versionRange) {
        throw new Error(`package-lock.json has an outdated ${section} entry for ${name}`);
      }
    }
    for (const name of Object.keys(lockRoot[section] ?? {})) {
      if (!(name in (pkg[section] ?? {}))) {
        throw new Error(`package-lock.json still includes removed ${section}: ${name}`);
      }
    }
  }
  for (const [name, expected] of Object.entries(securityOverrides)) {
    if (pkg.overrides?.[name] !== expected) {
      throw new Error(`package.json is missing security override ${name}@${expected}`);
    }
    const actual = lock.packages?.[`node_modules/${name}`]?.version;
    if (actual !== expected) {
      throw new Error(`Lockfile ${name} is ${actual ?? 'missing'}; expected ${expected}`);
    }
  }
  for (const removed of ['puppeteer', 'puppeteer-core', '@puppeteer/browsers', 'basic-ftp', 'extract-zip']) {
    if (Object.keys(lock.packages).some((p) => p === `node_modules/${removed}` || p.endsWith(`/node_modules/${removed}`))) {
      throw new Error(`Unexpected Puppeteer-only package remains: ${removed}`);
    }
  }
}

const pkg = JSON.parse(originalPackage);
pkg.overrides = { ...pkg.overrides, ...securityOverrides };
writeFileSync(packagePath, `${JSON.stringify(pkg, null, 2)}\n`);

try {
  console.log('1/4 Resolving complete dependency tree (including optional dependencies)...');
  // npm 11 checks optional dependencies of less (make-dir, probe-image-size,
  // needle and children) even when npm 10 dry-run can overlook missing entries.
  if (!runNpm(['install', '--package-lock-only', '--include=optional', '--ignore-scripts', '--no-audit', '--no-fund'])) {
    throw new Error('Unable to resolve missing lockfile packages from the configured npm registry.');
  }
  console.log('2/4 Applying available compatible npm security fixes...');
  const auditFixOk = runNpm(['audit', 'fix', '--package-lock-only', '--include=optional', '--ignore-scripts', '--no-fund']);
  if (!auditFixOk) {
    console.warn('Some audit findings may be unpatched; preserving the verified compatible changes.');
  }
  console.log('3/4 Reconciling npm lockfile after security fixes...');
  if (!runNpm(['install', '--package-lock-only', '--include=optional', '--ignore-scripts', '--no-audit', '--no-fund'])) {
    throw new Error('Unable to reconcile package-lock.json after npm audit fix.');
  }
  verifyLockfile();
  console.log('4/4 Checking that npm ci accepts the lockfile...');
  if (!runNpm(['ci', '--dry-run', '--ignore-scripts', '--no-audit', '--no-fund', '--silent'], { quiet: true })) {
    throw new Error('npm ci reports an inconsistent lockfile.');
  }
} catch (error) {
  writeFileSync(packagePath, originalPackage);
  writeFileSync(lockPath, originalLock);
  console.error(`Security refresh stopped and restored the previous files: ${error.message}`);
  console.error('Check npm registry connectivity (npm ping) and inspect the npm error output above.');
  process.exit(1);
}

console.log('\nLockfile resolved and npm ci dry-run succeeded.');
console.log('Next: npm ci && npm run build && npm audit');
console.log('npm audit may still report upstream advisories with no published fix.');
