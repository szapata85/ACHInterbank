#!/usr/bin/env python3
"""NAV5 screen/route coverage validator.

Static validator for NAV-MENU-IA-001 / NAV5. It intentionally does not require
Angular node_modules or dotnet. The route graph is expanded with the globally
available TypeScript parser when Node is present; no dependency installation is
performed.
"""
from __future__ import annotations

import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import re
import shutil
import subprocess
import sys
import tempfile
import zipfile

ROOT = Path(__file__).resolve().parents[1]
APP = ROOT / "web/ach-interbank-ui/src/app"
INVENTORY = ROOT / "docs/ai/navigation-menu/phases/NAV5_SCREEN_ROUTE_COVERAGE_INVENTORY.md"
DELTA = ROOT / "docs/ai/navigation-menu/phases/NAV5_NAVIGATION_DELTA.json"
SEEDER = ROOT / "src/Cfa.ACHInterbank.Persistence/Navigation/Seeders/NavigationInformationArchitectureSeeder.cs"
IA_DOC = ROOT / "docs/architecture/NAVIGATION_MENU_INFORMATION_ARCHITECTURE.md"
STATE = ROOT / "docs/ai/navigation-menu/MEMORY_STATE.md"

BASELINE_ZIP_NAME = "ACHInterbank-R34-NAV-IA-NAV4.2J-FULL-PROJECT.zip"
BASELINE_ZIP_SHA = "3f3c297f2df4ee11e085ca32172a3f7b6667b8fc00ebf7a0b0dbfae5a1e61515"
BASELINE_SEEDER_SHA = "28371549f4b9150fdf738bee11454c98117ba420d1abae0e9971e2c49aee9f4c"
NAV5_SEEDER_SHA = "296ffd77544e8017d0fae4dce31a5267b2799a643f8e4dd888d96f79df3dac1c"
APP_BASELINE_AGG_SHA = "fb8cfcfe749d4ec6df0a1a499dfe8c8bf01fdd1b07fb29e37648beb3daf2f447"
SECURITY_BASELINE_AGG_SHA = "409ee0112b309cdca65927d0778678084186273133c93ff26c878c6e4da02fbb"
NAV_SERVICES_BASELINE_AGG_SHA = "b05ae31e671e0c239a0281ec5caac987aa66f45993a3a2458e6589271d471429"
SOLUTION_BASELINE_SHA = "e51e3ea40f0b95bb7ab995436ffe58a9cadbb5636f0fd36708fc82f59c3ab9c2"

EXPECTED_COUNTS = {
    "MENU": 70,
    "CONTEXTUAL": 36,
    "TECHNICAL": 12,
    "REDIRECT": 24,
    "UNREACHABLE_OR_DEAD": 0,
    "UNKNOWN": 0,
}

# Final primary destinations. Structural nodes and redirects are deliberately absent.
EXPECTED_MENU_ROUTES = {
    "/dashboard", "/users", "/users/list", "/users/branding", "/users/password-rules", "/users/login-lockout",
    "/customers", "/ach-cycles", "/ach-cycles/nacha/export", "/nacha-security/certificates", "/nacha-security/sobre-digital",
    "/ach-claims", "/transactions/outgoing-monitoring", "/transactions/list", "/transactions/create",
    "/transactions/bulk-ingestion/upload", "/transactions/bulk-ingestion/tracking", "/transactions/nacha-upload",
    "/transactions/returns", "/transactions/returns-ror", "/integraciones", "/integraciones/contrato",
    "/integraciones/relacion-informacion", "/integraciones/conexion", "/navigation/menu-items", "/catalogs",
    "/catalogs/financial-institutions", "/catalogs/clearing-house-preferences", "/catalogs/bank-holidays",
    "/catalogs/company-entry-descriptions", "/catalogs/document-types", "/catalogs/person-types", "/catalogs/gender-types",
    "/catalogs/phone-types", "/catalogs/email-types", "/catalogs/address-types", "/catalogs/transaction-codes",
    "/operational-restrictions", "/clearing-houses", "/scheduler/tasks", "/audit-logs", "/auth-logs", "/navigation-logs",
    "/reports", "/reports/traceability", "/reports/sent", "/reports/received", "/reports/returns", "/reports/rejections",
    "/reports/files", "/reports/cycles", "/reports/audit", "/reports/history", "/reports/reconciliation",
    "/ach-colombia/file-exchange", "/administracion/mft-ach", "/nacha-config-admin/perfiles", "/uat/nacha-inbound-simulator",
    "/incoming-nacha-command-center", "/incoming-nacha-command-center/observability",
    "/incoming-nacha-command-center/orphan-resolution", "/incoming-nacha-command-center/queue", "/ach-responses",
    "/ach-responses/manual-review", "/ach-responses/status-mappings", "/ach-responses/dashboard", "/ach-responses/audit",
    "/ach/nacha/soap-uat-console", "/ach/reconciliation", "/payment-rail-capability-registry",
}

PROGRAMMATIC_REDIRECTS = {
    "/catalogs/clearing-house-special-dates": {"/clearing-houses", "/clearing-houses/:id/special-dates"},
    "/nacha-config-admin/records": {"/nacha-config-admin/perfiles", "/nacha-config-admin/perfiles/:id"},
    "/nacha-config-admin/variants-fields": {"/nacha-config-admin/perfiles", "/nacha-config-admin/perfiles/:id"},
}

EXPECTED_RESIDUAL = {"/nacha-security/certificates/:id/versions": "ORPHAN_CONTEXTUAL_ROUTE"}

ALLOWED_BASELINE_DIFFS = {
    "src/Cfa.ACHInterbank.Persistence/Navigation/Seeders/NavigationInformationArchitectureSeeder.cs",
    "docs/architecture/NAVIGATION_MENU_INFORMATION_ARCHITECTURE.md",
    "docs/ai/navigation-menu/phases/PHASE_05_NAV5_COBERTURA.md",
    "docs/ai/navigation-menu/MEMORY_STATE.md",
    "MEMORY.md",
}
ALLOWED_NEW_FILES = {
    "docs/ai/navigation-menu/phases/NAV5_SCREEN_ROUTE_COVERAGE_INVENTORY.md",
    "docs/ai/navigation-menu/phases/NAV5_NAVIGATION_DELTA.json",
    "scripts/validate-nav5-screen-route-coverage.py",
}

checks = 0

def require(cond: bool, message: str) -> None:
    global checks
    checks += 1
    if not cond:
        raise AssertionError(message)


def sha256_bytes(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def sha256_file(path: Path) -> str:
    return sha256_bytes(path.read_bytes())


def aggregate_hash(paths: list[Path]) -> tuple[str, int]:
    files: list[Path] = []
    for p in paths:
        if p.is_dir():
            files.extend(x for x in p.rglob("*") if x.is_file())
        elif p.is_file():
            files.append(p)
    files = sorted(set(files), key=lambda x: x.relative_to(ROOT).as_posix())
    h = hashlib.sha256()
    for f in files:
        rel = f.relative_to(ROOT).as_posix()
        h.update(rel.encode("utf-8")); h.update(b"\0"); h.update(f.read_bytes()); h.update(b"\0")
    return h.hexdigest(), len(files)


def discover_typescript_module() -> Path:
    env = os.environ.get("TYPESCRIPT_LIB")
    if env and Path(env).is_file():
        return Path(env)
    tsc = shutil.which("tsc")
    if tsc:
        real = Path(tsc).resolve()
        # .../lib/node_modules/typescript/bin/tsc -> ../lib/typescript.js
        cand = real.parent.parent / "lib" / "typescript.js"
        if cand.is_file():
            return cand
    # Known NVM layouts, without installing anything.
    for cand in Path("/opt/nvm/versions/node").glob("*/lib/node_modules/typescript/lib/typescript.js"):
        if cand.is_file():
            return cand
    raise AssertionError("TypeScript global parser unavailable; NAV5 route universe cannot be proven")


ROUTE_SCANNER_JS = r'''
const fs=require('fs'), path=require('path');
const ts=require(process.argv[2]);
const PROJECT=process.argv[3];
const ROOT=path.join(PROJECT,'web/ach-interbank-ui/src/app');
function sf(file){return ts.createSourceFile(file,fs.readFileSync(file,'utf8'),ts.ScriptTarget.Latest,true,ts.ScriptKind.TS)}
function resolveImport(base,spec){if(!spec.startsWith('.'))return null;let p=path.resolve(path.dirname(base),spec);for(const c of [p+'.ts',path.join(p,'index.ts')])if(fs.existsSync(c))return c;return null;}
function text(n,s){return n.getText(s)}
function stringVal(n){return n&&(ts.isStringLiteral(n)||ts.isNoSubstitutionTemplateLiteral(n))?n.text:null}
function arrStrings(n){if(!n||!ts.isArrayLiteralExpression(n))return null;return n.elements.map(stringVal).filter(x=>x!==null)}
function objProp(o,name){if(!o||!ts.isObjectLiteralExpression(o))return null;for(const p of o.properties){if(ts.isPropertyAssignment(p)){let k=ts.isIdentifier(p.name)||ts.isStringLiteral(p.name)?p.name.text:null;if(k===name)return p.initializer;}}return null;}
function dynImportInfo(n,s){if(!n)return null;let t=text(n,s);let m=t.match(/import\(['"]([^'"]+)['"]\).*?\.then\(\s*\([^)]*\)\s*=>\s*[^.]+\.([A-Za-z0-9_]+)/s);return m?{spec:m[1],symbol:m[2],raw:t}:null;}
function parseRouteObj(o,s,file){let data=objProp(o,'data');const getId=n=>n?(ts.isIdentifier(n)?n.text:text(n,s)):null;let ch=objProp(o,'children');return{source:path.relative(PROJECT,file).replace(/\\/g,'/'),line:s.getLineAndCharacterOfPosition(o.getStart(s)).line+1,path:stringVal(objProp(o,'path'))??'',pathMatch:stringVal(objProp(o,'pathMatch')),redirectTo:stringVal(objProp(o,'redirectTo')),component:getId(objProp(o,'component')),loadComponent:dynImportInfo(objProp(o,'loadComponent'),s),loadChildren:dynImportInfo(objProp(o,'loadChildren'),s),data:data&&ts.isObjectLiteralExpression(data)?{title:stringVal(objProp(data,'title')),breadcrumb:stringVal(objProp(data,'breadcrumb')),permissions:arrStrings(objProp(data,'permissions')),roles:arrStrings(objProp(data,'roles'))}:null,canActivate:(()=>{let a=objProp(o,'canActivate');return a&&ts.isArrayLiteralExpression(a)?a.elements.map(getId):[]})(),children:ch&&ts.isArrayLiteralExpression(ch)?ch.elements.filter(ts.isObjectLiteralExpression).map(x=>parseRouteObj(x,s,file)):[]};}
function findArray(file,symbol){let s=sf(file),found=null;function v(n){if(ts.isVariableDeclaration(n)&&ts.isIdentifier(n.name)&&n.name.text===symbol&&n.initializer&&ts.isArrayLiteralExpression(n.initializer))found=n.initializer;ts.forEachChild(n,v)}v(s);if(!found)return null;return{source:s,routes:found.elements.filter(ts.isObjectLiteralExpression).map(o=>parseRouteObj(o,s,file))};}
function moduleChildRoutes(file){let s=sf(file),direct=null;function v(n){if(ts.isCallExpression(n)&&ts.isPropertyAccessExpression(n.expression)&&n.expression.name.text==='forChild'&&n.arguments.length)direct=n.arguments[0];ts.forEachChild(n,v)}v(s);if(direct){if(ts.isArrayLiteralExpression(direct))return direct.elements.filter(ts.isObjectLiteralExpression).map(o=>parseRouteObj(o,s,file));if(ts.isIdentifier(direct)){let f=findArray(file,direct.text);if(f)return f.routes;}}let imports=[];s.statements.forEach(st=>{if(ts.isImportDeclaration(st)&&ts.isStringLiteral(st.moduleSpecifier)){let rf=resolveImport(file,st.moduleSpecifier.text);if(rf&&/routing\.module\.ts$/.test(rf))imports.push(rf)}});for(const rf of imports){let rr=moduleChildRoutes(rf);if(rr.length)return rr}return[];}
function lazyRoutes(parent,info){let f=resolveImport(parent,info.spec);if(!f)return{file:null,routes:[]};if(f.endsWith('.routes.ts')){let a=findArray(f,info.symbol);return{file:f,routes:a?a.routes:[]}}if(f.endsWith('.module.ts'))return{file:f,routes:moduleChildRoutes(f)};return{file:f,routes:[]};}
function join(base,p){if(p==='**')return(base==='/'?'':base)+'/**';if(!p)return base||'/';if(p.startsWith('/'))return p;let b=base==='/'?'':base.replace(/\/$/,'');return b+'/'+p;}
let rootFile=path.join(ROOT,'app-routing.module.ts'),root=findArray(rootFile,'APP_ROUTES').routes,leaves=[];
function walk(routes,base,inh){for(const r of routes){let full=join(base,r.path);let gates={roles:[...(inh.roles||[]),...((r.data&&r.data.roles)||[])],permissions:[...(inh.permissions||[]),...((r.data&&r.data.permissions)||[])],guards:[...(inh.guards||[]),...(r.canActivate||[])]};let common={...r,basePath:base,fullPath:full,effectiveGates:gates};if(r.redirectTo!==null){leaves.push({...common,kind:'redirect'});continue}if(r.children&&r.children.length){walk(r.children,full,gates);continue}if(r.loadChildren){let l=lazyRoutes(path.resolve(PROJECT,r.source),r.loadChildren);if(!l.routes.length)leaves.push({...common,kind:'lazy-empty',lazyFile:l.file});else walk(l.routes,full,gates);continue}if(r.component||r.loadComponent)leaves.push({...common,kind:'screen'});else leaves.push({...common,kind:'other'});}}
walk(root,'/',{roles:[],permissions:[],guards:[]});
console.log(JSON.stringify(leaves));
'''


def scan_routes() -> list[dict]:
    node = shutil.which("node")
    require(bool(node), "Node is required for the TypeScript AST route scanner")
    ts_lib = discover_typescript_module()
    with tempfile.NamedTemporaryFile("w", suffix=".js", delete=False, encoding="utf-8") as f:
        f.write(ROUTE_SCANNER_JS)
        js = Path(f.name)
    try:
        proc = subprocess.run([node, str(js), str(ts_lib), str(ROOT)], text=True, capture_output=True, check=False)
        require(proc.returncode == 0, f"route scanner failed: {proc.stderr.strip()}")
        return json.loads(proc.stdout)
    finally:
        js.unlink(missing_ok=True)


def parse_inventory() -> dict[str, dict[str, str]]:
    rows: dict[str, dict[str, str]] = {}
    for line in INVENTORY.read_text(encoding="utf-8").splitlines():
        if not re.match(r"^\|\s*\d+\s*\|", line):
            continue
        cols = [c.strip().replace("\\|", "|") for c in line.strip().strip("|").split("|")]
        require(len(cols) == 13, f"inventory row has {len(cols)} columns: {line[:100]}")
        route = cols[1]
        require(route not in rows, f"duplicate inventory route {route}")
        rows[route] = {
            "category": cols[4], "menu": cols[5], "parent": cols[6], "label": cols[7],
            "gate": cols[8], "access": cols[9], "evidence": cols[10], "gap": cols[11], "decision": cols[12],
        }
    return rows


def normalize_redirect(base: str, target: str) -> str:
    if target.startswith("/"):
        return target
    base = base or "/"
    # basePath is the parent route prefix in which redirectTo is evaluated.
    parts = [x for x in (base.rstrip("/") + "/" + target).split("/") if x not in ("", ".")]
    out: list[str] = []
    for p in parts:
        if p == "..":
            if out: out.pop()
        else:
            out.append(p)
    return "/" + "/".join(out)


def baseline_zip_path() -> Path | None:
    candidates = []
    if os.environ.get("NAV5_BASELINE_ZIP"):
        candidates.append(Path(os.environ["NAV5_BASELINE_ZIP"]))
    candidates.extend([ROOT.parent / BASELINE_ZIP_NAME, Path("/mnt/data") / BASELINE_ZIP_NAME])
    for p in candidates:
        if p.is_file(): return p
    return None


def validate_baseline_diff() -> None:
    zpath = baseline_zip_path()
    if zpath is None:
        # This check is optional in a redistributed NAV5 package; fixed hashes below still enforce
        # the critical protected surfaces.
        return
    require(sha256_file(zpath) == BASELINE_ZIP_SHA, "baseline ZIP SHA-256 mismatch")
    with zipfile.ZipFile(zpath) as z:
        baseline = {n: z.read(n) for n in z.namelist() if not n.endswith("/")}
    current = {p.relative_to(ROOT).as_posix(): p.read_bytes() for p in ROOT.rglob("*") if p.is_file()}
    changed = {p for p in baseline.keys() & current.keys() if baseline[p] != current[p]}
    removed = set(baseline) - set(current)
    added = set(current) - set(baseline)
    require(not removed, f"baseline files removed: {sorted(removed)[:10]}")
    require(changed <= ALLOWED_BASELINE_DIFFS, f"unauthorized modified files: {sorted(changed-ALLOWED_BASELINE_DIFFS)}")
    require(added <= ALLOWED_NEW_FILES, f"unauthorized new files: {sorted(added-ALLOWED_NEW_FILES)}")


def main() -> int:
    routes = scan_routes()
    require(len(routes) == 142, f"expected 142 routes, found {len(routes)}")
    by_path = {r["fullPath"]: r for r in routes}
    require(len(by_path) == len(routes), "duplicate effective route paths in Angular graph")

    inv = parse_inventory()
    require(set(inv) == set(by_path), f"inventory/route universe mismatch; missing={set(by_path)-set(inv)}, extra={set(inv)-set(by_path)}")
    for category, expected in EXPECTED_COUNTS.items():
        found = sum(1 for row in inv.values() if row["category"] == category)
        require(found == expected, f"{category}: expected {expected}, found {found}")
    require(len(inv) == 142, "inventory must contain 142 unique routes")

    menu_routes = {p for p, row in inv.items() if row["category"] == "MENU"}
    require(menu_routes == EXPECTED_MENU_ROUTES, f"MENU route set mismatch: missing={EXPECTED_MENU_ROUTES-menu_routes}, extra={menu_routes-EXPECTED_MENU_ROUTES}")
    require(len(menu_routes) == 70, "MENU coverage must be exactly 70")
    for p in menu_routes:
        require(p in by_path, f"menu route does not exist: {p}")
        require(by_path[p]["kind"] == "screen", f"MENU route is not a screen surface: {p}")
        require(p not in PROGRAMMATIC_REDIRECTS, f"programmatic redirect exposed as MENU: {p}")

    # Parent/order coherence from the published inventory: siblings use unique multiples of 10.
    siblings: dict[str, set[int]] = {}
    for p in menu_routes:
        row = inv[p]
        m = re.search(r"\(Order\s+(\d+)\)", row["menu"])
        require(bool(m), f"MENU row lacks canonical Order: {p}")
        order = int(m.group(1))
        require(order > 0 and order % 10 == 0, f"non-canonical menu order {order} for {p}")
        parent = row["parent"]
        bucket = siblings.setdefault(parent, set())
        require(order not in bucket, f"duplicate sibling order {order} under {parent}")
        bucket.add(order)

    # NAV5-specific final seeder correction.
    seed = SEEDER.read_text(encoding="utf-8")
    require("NachaConfigProfilesRoute" in seed and "order: 10" in seed, "profile menu order NAV5 correction missing")
    require("DeactivateMenuRouteAsync(NachaConfigRecordsRedirectRoute);" in seed, "records redirect deactivation missing")
    require("DeactivateMenuRouteAsync(NachaConfigVariantsFieldsRedirectRoute);" in seed, "variants-fields redirect deactivation missing")
    require(sha256_file(SEEDER) == NAV5_SEEDER_SHA, "NAV5 seeder has untracked drift")
    ia = IA_DOC.read_text(encoding="utf-8")
    require("      10 Perfiles oficiales" in ia, "canonical IA profile child missing")
    require("      20 Registros oficiales" not in ia and "      30 Campos y variantes" not in ia, "redirect-only NACHA aliases still published in canonical tree")

    # Route-level redirects must resolve to an existing real route template.
    all_paths = set(by_path)
    for r in routes:
        if r["kind"] != "redirect": continue
        target = normalize_redirect(r.get("basePath") or "/", r.get("redirectTo") or "")
        require(target in all_paths, f"redirect {r['fullPath']} resolves to missing target {target}")
    for src, targets in PROGRAMMATIC_REDIRECTS.items():
        require(src in all_paths, f"programmatic redirect source missing: {src}")
        require(inv[src]["category"] == "REDIRECT", f"programmatic redirect misclassified: {src}")
        for target in targets:
            require(target in all_paths, f"programmatic redirect {src} can target missing route {target}")

    # The two NAV5 aliases were exposed before the fix and are now redirect-only; certificate versions
    # is the single documented contextual orphan. There must be no UNKNOWN/dead route.
    for p in ("/nacha-config-admin/records", "/nacha-config-admin/variants-fields"):
        require("REDIRECT_INCONSISTENCY (CORREGIDO)" == inv[p]["gap"], f"corrected gap missing for {p}")
    require(inv["/nacha-config-admin/perfiles"]["gap"] == "WRONG_ORDER (CORREGIDO)", "profile order gap not traced")
    require({p: row["gap"] for p, row in inv.items() if row["gap"] == "ORPHAN_CONTEXTUAL_ROUTE"} == EXPECTED_RESIDUAL, "unexpected residual orphan set")
    require(sum("CORREGIDO" in row["gap"] for row in inv.values()) == 3, "expected exactly 3 corrected NAV5 gaps")

    # Prove orphan evidence by source scan: only route declaration/spec may mention this literal pattern.
    refs = []
    pat = re.compile(r"certificates[^\n'\"]*versions", re.I)
    for f in APP.rglob("*"):
        if not f.is_file() or f.suffix not in {".ts", ".html"}: continue
        txt = f.read_text(encoding="utf-8", errors="ignore")
        if pat.search(txt): refs.append(f.relative_to(ROOT).as_posix())
    unexpected = [p for p in refs if not (p.endswith("nacha-security-routing.module.ts") or p.endswith("nacha-security-routing.module.spec.ts"))]
    require(not unexpected, f"certificate versions unexpectedly linked/referenced: {unexpected}")

    # All route permission/role identifiers resolve to existing backend security/configuration source text.
    cs_text = "\n".join(p.read_text(encoding="utf-8", errors="ignore") for p in (ROOT / "src").rglob("*.cs"))
    permission_refs = sorted({perm for r in routes for perm in r["effectiveGates"].get("permissions", [])})
    role_refs = sorted({role for r in routes for role in r["effectiveGates"].get("roles", [])})
    require(len(permission_refs) == 25, f"expected 25 effective permission references, found {len(permission_refs)}")
    for perm in permission_refs:
        require(perm in cs_text, f"route permission reference not found in backend source: {perm}")
    for role in role_refs:
        require(role in cs_text, f"route role reference not found in backend source: {role}")

    # Protected surfaces are byte-equivalent to NAV4.2J.
    app_hash, app_files = aggregate_hash([APP])
    require(app_files == 729 and app_hash == APP_BASELINE_AGG_SHA, f"NAV4 SPA/copy drift: files={app_files} hash={app_hash}")
    security_hash, security_files = aggregate_hash([
        ROOT / "src/Cfa.ACHInterbank.Persistence/Security",
        ROOT / "src/Cfa.ACHInterbank.Application/Security",
        ROOT / "src/Cfa.ACHInterbank.Application/Navigation/Queries/GetMenuForCurrentUserHandler.cs",
    ])
    require(security_files == 68 and security_hash == SECURITY_BASELINE_AGG_SHA, f"security drift: files={security_files} hash={security_hash}")
    nav_hash, nav_files = aggregate_hash([
        APP / "core/services/navigation.service.ts",
        APP / "core/services/menu-route-surface.service.ts",
        APP / "core/services/navigation-naming.service.ts",
        APP / "core/services/navigation-naming.util.ts",
    ])
    require(nav_files == 4 and nav_hash == NAV_SERVICES_BASELINE_AGG_SHA, f"navigation service drift: files={nav_files} hash={nav_hash}")
    require(sha256_file(ROOT / "ACHInterbank.sln") == SOLUTION_BASELINE_SHA, "solution graph file drift")
    require("NAV3.5C" in STATE.read_text(encoding="utf-8") and "BLOCKED / NO EJECUTAR" in STATE.read_text(encoding="utf-8"), "NAV3.5C blocked invariant missing")

    # Delta is exhaustive and reverses the one productive file to exact NAV4.2J baseline.
    delta = json.loads(DELTA.read_text(encoding="utf-8"))
    require(delta["productive_files"] == ["src/Cfa.ACHInterbank.Persistence/Navigation/Seeders/NavigationInformationArchitectureSeeder.cs"], "unexpected productive file list in delta")
    require(len(delta["changes"]) == 3, "NAV5 delta must contain exactly 3 productive changes")
    require(delta["security_changes"] == [] and delta["business_logic_changes"] == [], "delta declares unauthorized security/business changes")
    require(delta["baseline_file_sha256"] == BASELINE_SEEDER_SHA and delta["nav5_file_sha256"] == NAV5_SEEDER_SHA, "delta file hashes mismatch")
    lines = SEEDER.read_text(encoding="utf-8").splitlines(keepends=True)
    ops = delta["reverse_reconstruction"]["operations"]
    for op in sorted(ops, key=lambda x: x["nav5_start_line"], reverse=True):
        start, end = op["nav5_start_line"], op["nav5_end_line"]
        require(lines[start:end] == op["nav5_lines"], f"reverse delta source slice mismatch at lines {start}:{end}")
        lines[start:end] = op["baseline_lines"]
    reconstructed = "".join(lines).encode("utf-8")
    require(sha256_bytes(reconstructed) == BASELINE_SEEDER_SHA, "reverting NAV5 delta does not reconstruct NAV4.2J seeder")

    # Strong whole-package comparison when the supplied baseline ZIP is present.
    validate_baseline_diff()

    print(f"PASS NAV5 screen-route coverage: {checks} assertions")
    print("routes=142 MENU=70 CONTEXTUAL=36 TECHNICAL=12 REDIRECT=24 DEAD=0 UNKNOWN=0")
    print("menu_navigable=70 covered=70 missing=0 invalid=0 exposed_contextual_technical=0 duplicates=0")
    print("gaps_found=4 corrected=3 blocked_security=0 residual=1 orphan_contextual=1")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print(f"FAIL NAV5: {exc}", file=sys.stderr)
        raise SystemExit(1)
