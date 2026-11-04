/**
 * Incremental, scoped npm vulnerability remediation for ACH Interbank UI.
 * Run from the project UI root with `node scripts/apply-residual-security.mjs`.
 * Does not run npm or modify package-lock.json; it only adds three scoped overrides.
 */
import { existsSync, readFileSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';

const packagePath = resolve(process.cwd(), 'package.json');
if (!existsSync(packagePath)) {
  console.error('package.json not found. Run from web/ach-interbank-ui.');
  process.exit(1);
}

const original = readFileSync(packagePath, 'utf8');
const pkg = JSON.parse(original);
if (pkg.name !== 'ach-interbank-ui') {
  console.error(`Unexpected package: ${pkg.name || 'missing name'}. No changes made.`);
  process.exit(1);
}
if (!pkg.overrides || typeof pkg.overrides !== 'object' || Array.isArray(pkg.overrides)) {
  console.error('Expected existing object "overrides" in package.json. No changes made.');
  process.exit(1);
}

// Distinct major lines coexist in this dependency tree. A global override of
// brace-expansion to v5 would break older minimatch consumers such as Karma.
const scoped = {
  'minimatch@^3.0.0': { 'brace-expansion': '1.1.21' },
  'minimatch@^10.0.0': { 'brace-expansion': '5.0.12' },
  '@vercel/cli-config': { zod: '4.6.7' }
};

const nextOverrides = { ...pkg.overrides };
for (const [parent, children] of Object.entries(scoped)) {
  const existing = nextOverrides[parent];
  if (existing != null && (typeof existing !== 'object' || Array.isArray(existing))) {
    console.error(`Conflicting override for ${parent}: expected a nested object. No changes made.`);
    process.exit(1);
  }
  nextOverrides[parent] = { ...(existing ?? {}), ...children };
}
pkg.overrides = nextOverrides;
const next = `${JSON.stringify(pkg, null, 2)}\n`;
if (next === original) {
  console.log('Scoped security overrides already present. No changes needed.');
  process.exit(0);
}
writeFileSync(packagePath, next, 'utf8');
console.log('Updated package.json:');
for (const [parent, children] of Object.entries(scoped)) {
  for (const [child, version] of Object.entries(children)) {
    console.log(`  ${parent} -> ${child}@${version}`);
  }
}
console.log('Lockfile not changed. Next: npm run security:refresh-lock, npm ci, npm run build, npm audit.');
