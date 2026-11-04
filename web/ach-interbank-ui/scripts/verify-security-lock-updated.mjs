/**
 * SEC-I02: dependency/lockfile static check supporting simple AND scoped overrides.
 * This is NOT a substitute for npm ci, runtime tests, or npm audit.
 */
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import path from 'node:path';

const cwd = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const pkg = JSON.parse(readFileSync(path.join(cwd, 'package.json'), 'utf8'));
const lock = JSON.parse(readFileSync(path.join(cwd, 'package-lock.json'), 'utf8'));
const errors = [];
const warnings = [];
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

// A lockfile uses physical paths for hoisting. Resolve a child's nearest installed
// location from an actual parent path, instead of assuming that nested entries
// must physically live under parent/node_modules.
function resolveChild(parentPath, child) {
  let base = parentPath;
  while (true) {
    const candidate = base ? `${base}/node_modules/${child}` : `node_modules/${child}`;
    if (packages[candidate]) return [candidate, packages[candidate]];
    const idx = base.lastIndexOf('/node_modules/');
    if (idx !== -1) {
      base = base.slice(0, idx);
    } else if (base.startsWith('node_modules/')) {
      base = '';
    } else {
      break;
    }
  }
  return null;
}
function parseParent(selector) {
  // Supports normal package names, scoped names, and version-specific major
  // ranges used in ACH Interbank's overrides (minimatch@^3, minimatch@^10).
  const marker = selector.startsWith('@') ? selector.indexOf('@', 1) : selector.indexOf('@');
  if (marker < 0) return { name: selector, major: null };
  const name = selector.slice(0, marker);
  const version = selector.slice(marker + 1);
  const match = /^\^(\d+)(?:\.|$)/.exec(version);
  if (!match) return { name, major: NaN };
  return { name, major: Number(match[1]) };
}
let scopedValidated = 0;
for (const [selector, target] of Object.entries(pkg.overrides ?? {})) {
  if (typeof target === 'string') {
    const matches = Object.entries(packages).filter(([p]) =>
      p === `node_modules/${selector}` || p.endsWith(`/node_modules/${selector}`));
    if (matches.length === 0) errors.push(`Override ${selector}@${target} no está en el lockfile`);
    for (const [p, meta] of matches) {
      if (meta.version !== target) errors.push(`Override ${p}: ${meta.version} != ${target}`);
    }
    continue;
  }
  if (!target || typeof target !== 'object' || Array.isArray(target)) {
    errors.push(`Override inválido: ${selector}`);
    continue;
  }
  const { name, major } = parseParent(selector);
  if (Number.isNaN(major)) {
    warnings.push(`Selector de versión no validado estáticamente: ${selector}`);
    continue;
  }
  const parents = Object.entries(packages).filter(([p, meta]) =>
    (p === `node_modules/${name}` || p.endsWith(`/node_modules/${name}`)) &&
    (major === null || Number((meta.version ?? '').split('.')[0]) === major));
  if (!parents.length) {
    warnings.push(`Sin consumidores instalados para override ${selector}`);
    continue;
  }
  for (const [parentPath, parentMeta] of parents) {
    for (const [child, wantedVersion] of Object.entries(target)) {
      if (child === '.') {
        if (parentMeta.version !== wantedVersion) errors.push(`${parentPath} versión ${parentMeta.version}, esperado ${wantedVersion}`);
        continue;
      }
      if (typeof wantedVersion !== 'string') {
        warnings.push(`Override anidado no validado: ${selector} > ${child}`);
        continue;
      }
      if (parentMeta.dependencies?.[child] == null && parentMeta.optionalDependencies?.[child] == null) {
        warnings.push(`No es dependencia directa: ${selector} > ${child}; ejecutar npm ls`);
        continue;
      }
      const installed = resolveChild(parentPath, child);
      if (!installed) {
        errors.push(`No se resuelve ${selector} > ${child} en lockfile`);
      } else if (installed[1].version !== wantedVersion) {
        errors.push(`${selector} > ${child}: ${installed[1].version} != ${wantedVersion} (${installed[0]})`);
      } else {
        scopedValidated++;
      }
    }
  }
}
// Guard the legacy runner against an accidental, untested major upgrade.
if (pkg.devDependencies?.karma) {
  const karmaPath = 'node_modules/karma';
  if (!packages[karmaPath]) {
    errors.push('Karma no está instalado en el lockfile');
  } else {
    for (const [name, expectedMajor] of [['glob', 7], ['minimatch', 3]]) {
      const resolved = resolveChild(karmaPath, name);
      if (!resolved) errors.push(`Karma no puede resolver ${name}`);
      else if (Number(resolved[1].version.split('.')[0]) !== expectedMajor) {
        errors.push(`Karma > ${name}: major ${resolved[1].version} (esperado ${expectedMajor}.x)`);
      }
    }
  }
}
const missingIntegrity = Object.entries(packages)
  .filter(([, meta]) => /^https:\/\/registry\.npmjs\.org\//.test(meta.resolved ?? '') && !meta.integrity)
  .map(([name]) => name);
if (missingIntegrity.length) {
  warnings.push(`${missingIntegrity.length} paquetes del registry sin integrity`);
  if (process.argv.includes('--strict-integrity')) errors.push('Integridades faltantes');
}
for (const warn of warnings) console.warn(`SEC-I02 WARN: ${warn}`);
if (errors.length) {
  console.error(`SEC-I02 LOCK FAIL: ${errors.length} inconsistencias`);
  for (const err of errors) console.error(` - ${err}`);
  process.exitCode = 1;
} else {
  console.log(`SEC-I02 LOCK PASS_STATIC: ${Object.keys(packages).length} paquetes; ${scopedValidated} overrides anidados comprobados.`);
}
