#!/usr/bin/env python3
from __future__ import annotations

import hashlib
import re
import subprocess
import textwrap
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ASSERTIONS = 0


def require(condition: bool, message: str) -> None:
    global ASSERTIONS
    ASSERTIONS += 1
    if not condition:
        raise AssertionError(message)


def read(path: str) -> str:
    return (ROOT / path).read_text(encoding="utf-8")


def file_sha256(path: str) -> str:
    return hashlib.sha256((ROOT / path).read_bytes()).hexdigest()


def aggregate_sha256(prefix: str) -> str:
    base = ROOT / prefix
    files = sorted(path for path in base.rglob("*") if path.is_file())
    digest = hashlib.sha256()
    for path in files:
        rel = path.relative_to(ROOT).as_posix()
        digest.update(rel.encode("utf-8"))
        digest.update(b"\0")
        digest.update(hashlib.sha256(path.read_bytes()).hexdigest().encode("ascii"))
        digest.update(b"\n")
    return digest.hexdigest()


naming_util = read("web/ach-interbank-ui/src/app/core/services/navigation-naming.util.ts")
naming_service = read("web/ach-interbank-ui/src/app/core/services/navigation-naming.service.ts")
layout_ts = read("web/ach-interbank-ui/src/app/layout/main-layout.component.ts")
layout_html = read("web/ach-interbank-ui/src/app/layout/main-layout.component.html")
layout_spec = read("web/ach-interbank-ui/src/app/layout/main-layout.component.spec.ts")
naming_spec = read("web/ach-interbank-ui/src/app/core/services/navigation-naming.service.spec.ts")
migas = read("web/ach-interbank-ui/src/app/shared/components/ui/ui-migas-pan.component.ts")
phase_doc = read("docs/ai/navigation-menu/phases/PHASE_04_NAV4_SPA_COPY.md")
state_doc = read("docs/ai/navigation-menu/MEMORY_STATE.md")
root_memory = read("MEMORY.md")

# Source-of-truth implementation is generic and recursive.
for marker in [
    "resolveNavigationNaming",
    "canonicalLabelForRoute",
    "indexMenu(item.children",
    "nearestAncestor.ancestors",
    "canonicalTrail",
    "currentPath.startsWith(`${candidate}/`)",
    ".split('#', 1)",
    ".split('?', 1)",
]:
    require(marker in naming_util, f"missing naming resolver marker: {marker}")

for forbidden in [
    "/reports",
    "/integraciones",
    "/administracion/mft-ach",
    "Centro de integración CFA",
    "Conexión entidad participante",
    "Reportes",
    "Conexiones",
]:
    require(forbidden not in naming_util, f"production route/label taxonomy leaked into naming util: {forbidden}")
    require(forbidden not in naming_service, f"production route/label taxonomy leaked into naming service: {forbidden}")

for marker in [
    "this.navigationService.getMenu()",
    "shareReplay({ bufferSize: 1, refCount: false })",
    "ActivatedRouteSnapshot.url",
]:
    # ActivatedRouteSnapshot.url is represented by current.url traversal in source.
    if marker == "ActivatedRouteSnapshot.url":
        require("for (const segment of current.url)" in naming_service, "parameterized routes must use resolved URL segments")
    else:
        require(marker in naming_service, f"missing shared-tree marker: {marker}")

# MainLayout consumes the shared naming model for visible and document titles.
for marker in [
    "NavigationNamingService",
    ".menu$",
    "this.navigationNaming.resolve(",
    "this.pageTitle = naming.pageTitle",
    "this.breadcrumbs = naming.breadcrumbs",
    "this.documentTitle.setTitle(`${this.productName} - ${this.pageTitle}`)",
]:
    require(marker in layout_ts, f"missing MainLayout naming marker: {marker}")

require("buildBreadcrumbs" not in layout_ts, "legacy route-data-only breadcrumb builder must no longer be effective")
require("!last && crumb.url" in layout_html, "canonical conceptual ancestors without active URLs must not create invalid links")
require("last || !crumb.url" in layout_html, "non-link canonical ancestors must remain visible")

# Existing local breadcrumb component stays in place and canonicalizes only resolvable routes/current exact route.
for marker in [
    "selector: 'ui-migas-pan'",
    "NavigationNamingService",
    "canonicalLabelForRoute",
    "item.ruta ?? (index === items.length - 1 ? currentUrl : undefined)",
    "NavigationEnd",
]:
    require(marker in migas, f"missing ui-migas-pan canonicalization marker: {marker}")

# Mandatory focal test coverage.
for marker in [
    "synthetic backend label change",
    "future backend child automatically",
    "/reports?x=1#section",
    "Transacciones enviadas",
    "/administracion/mft-ach",
    "/users/42/edit",
    "never exposes a parameter token",
    "contextual route metadata",
    "canonicalizes route-linked local breadcrumbs",
]:
    require(marker in naming_spec, f"missing NAV4.2A focal spec marker: {marker}")

for marker in [
    "canonical backend label for the visible and document title on Reportes",
    "ACH Interbank - Reportes",
]:
    require(marker in layout_spec, f"missing MainLayout document-title regression marker: {marker}")

# NAV4.1A / NAV4.1B regression: navigation tree and parent classifier remain exact.
protected_files = {
    "web/ach-interbank-ui/src/app/core/services/navigation.service.ts": "4df488ac45465a5283a02afe795fc64c4e5995a3415556d324275cc5e1d7ea93",
    "web/ach-interbank-ui/src/app/core/services/menu-route-surface.service.ts": "3a32dc7921e3c805e32fa9921e371109813aae9ba3a3173bda6a5b6b7030df72",
    "web/ach-interbank-ui/src/app/app-routing.module.ts": "14304a5b4db288552a401d71f62d77f0e5c1837bae5e0a4cb0d322a5a8ec75af",
    "web/ach-interbank-ui/src/app/features/integrations/integrations.routes.ts": "7a0106cf22e807826c97151e0e3af540ec30eb85ceeec6408b1e37b117a7b822",
    "web/ach-interbank-ui/src/app/features/reports/reports-routing.module.ts": "1508422e744ee986c65574f7a831777a849fe5cd5ee8c0ff136a3216d21f09b4",
    "web/ach-interbank-ui/src/app/features/admin/admin-routing.module.ts": "b5253d6b1538c61f7e648d310dbe62e5637aa0c9e4cf96900730c8fffae9382a",
    "web/ach-interbank-ui/src/app/features/customers/customers-routing.module.ts": "dda2a0183c9de56a06975dabb3233598921937c6f4cb48eca8b07d9714d3484c",
    "web/ach-interbank-ui/src/app/features/transactions/transactions-routing.module.ts": "f087b8574870aec7e2f577213405b2e014403debcc7f8c7e08f61f8eebc5f7ad",
    "web/ach-interbank-ui/src/app/features/ach-responses/ach-responses-routing.module.ts": "3e48e56c50c79ff12791ae78b5231565144ef68ea876eeb4cc3bfaa7e58662ec",
    "web/ach-interbank-ui/src/index.html": "ec7359c07152ad8e885c9fc15d23808a2af915002920183654651aa7ab5841fb",
    "src/Cfa.ACHInterbank.Application/Navigation/Queries/GetMenuForCurrentUserHandler.cs": "f111e416c181fc1912c297e37012cf7462f49d0434c1ef6ec9416c89b5ed1a61",
    "src/Cfa.ACHInterbank.Persistence/Navigation/Seeders/NavigationInformationArchitectureSeeder.cs": "28371549f4b9150fdf738bee11454c98117ba420d1abae0e9971e2c49aee9f4c",
    "src/Cfa.ACHInterbank.Persistence/Security/Seeders/FineGrainedGrantBackfillSeeder.cs": "e4f2b80b0b9e9c3aff636fb03e96b819019c07eeabc549bcfe4f789756775437",
    "src/Cfa.ACHInterbank.Persistence/Security/Seeders/LegacyFineGrainedRoleMigrationSeeder.cs": "e2366e75faab4ee31498d0cb84a93899a2a928698b01e4401a38ab1cc9c18105",
}
for path, expected in protected_files.items():
    require(file_sha256(path) == expected, f"protected NAV3/NAV4 file changed: {path}")

protected_groups = {
    "src/Cfa.ACHInterbank.Application/Security": "e6c89e8dc66aba73e2075c66919a60656f62bfc4e1e5867b0733d3d1d263e2ef",
    "src/Cfa.ACHInterbank.Persistence/Navigation/Seeders": "512f43f6a12bfde3241a6801efcefa1123b9ddd5451fb3d284c56ff19dedfef5",
    "src/Cfa.ACHInterbank.Persistence/Security/Seeders": "5c2874626c8df6ed53bc86616580b96456fa57322a2eb00e0dfe2f7df8282c01",
    "web/ach-interbank-ui/src/app/core/guards": "c3e43798fddd8b4f0fc5aca24032f605be4598b485de3fe4f2af1a2c5dfcaf57",
}
for prefix, expected in protected_groups.items():
    require(aggregate_sha256(prefix) == expected, f"protected security/navigation group changed: {prefix}")

fine = read("src/Cfa.ACHInterbank.Persistence/Security/Seeders/FineGrainedGrantBackfillSeeder.cs")
legacy = read("src/Cfa.ACHInterbank.Persistence/Security/Seeders/LegacyFineGrainedRoleMigrationSeeder.cs")
require("BackfillOrder = 119" in fine, "FineGrainedGrantBackfillSeeder must remain Order 119")
require("MigrationOrder = 120" in legacy, "LegacyFineGrainedRoleMigrationSeeder must remain Order 120")

# State/docs are explicit about phase boundaries and no hardcoded taxonomy.
for marker in [
    "NAV4.2A",
    "Infraestructura previa de naming visible",
    "Contextual/non-menu",
    "query params",
    "Administración de parámetros -> Conexiones -> Configuración del intercambio de archivos",
    "no se agregó ninguna lista productiva",
    "NOT_RUN_ENVIRONMENT",
]:
    require(marker.lower() in phase_doc.lower(), f"NAV4 phase documentation missing marker: {marker}")

require("active_phase: NAV4.2A" in state_doc, "MEMORY_STATE active phase must be NAV4.2A")
require("active_phase_status: IMPLEMENTED_UNVERIFIED_ENVIRONMENT" in state_doc, "NAV4.2A must remain unverified by runtime")
require("current_source_package: ACHInterbank-R34-NAV-IA-NAV4.1B-FULL-PROJECT.zip" in state_doc, "NAV4.2A input package must be NAV4.1B")
require("NAV3.5C: \"BLOCKED / NO EJECUTAR\"" in state_doc, "NAV3.5C must remain blocked")
require("NAV4.1A: IMPLEMENTED_UNVERIFIED_ENVIRONMENT" in state_doc, "NAV4.1A status changed")
require("NAV4.1B: IMPLEMENTED_UNVERIFIED_ENVIRONMENT" in state_doc, "NAV4.1B status changed")
require("NAV4.2A" in root_memory and "route→label" in root_memory, "root memory must capture durable source-of-truth decision")

# Execute the actual pure TypeScript resolver without Angular/node_modules.
node_program = textwrap.dedent(
    r"""
    const fs=require('fs'),vm=require('vm'),ts=require('typescript');
    const file=process.argv[1];
    const js=ts.transpileModule(fs.readFileSync(file,'utf8'),{compilerOptions:{target:ts.ScriptTarget.ES2022,module:ts.ModuleKind.CommonJS}}).outputText;
    const mod={exports:{}}; vm.runInNewContext(js,{module:mod,exports:mod.exports,require,console},{filename:file});
    const {resolveNavigationNaming,canonicalLabelForRoute}=mod.exports;
    const menu=[
      {id:50,label:'Administración de parámetros',route:'/administracion-parametros',children:[{id:58,label:'Conexiones',route:'/integraciones',children:[
        {id:581,label:'Contrato del servicio',route:'/integraciones/contrato'},
        {id:582,label:'Relación de información',route:'/integraciones/relacion-informacion'},
        {id:583,label:'Conexión entidad participante',route:'/integraciones/conexion'},
        {id:584,label:'Configuración del intercambio de archivos',route:'/administracion/mft-ach'}]}]},
      {id:70,label:'Reportes',route:'/reports',children:[{id:701,label:'Transacciones enviadas',route:'/reports/sent'}]},
      {id:80,label:'Administración',route:'/administracion',children:[{id:81,label:'Usuarios y acceso',route:'/users'}]}
    ];
    const labels=x=>x.breadcrumbs.map(b=>b.label);
    const eq=(a,b,n)=>{if(JSON.stringify(a)!==JSON.stringify(b)){throw new Error(n+': '+JSON.stringify(a)+' != '+JSON.stringify(b));}};
    let r=resolveNavigationNaming(menu,'/integraciones/conexion',[{url:'/integraciones',title:'Centro de integración CFA',breadcrumb:'Centro de integración CFA'},{url:'/integraciones/conexion',title:'Conexión CFA',breadcrumb:'Conexión CFA'}]);
    eq(r.pageTitle,'Conexión entidad participante','exact title');
    eq(labels(r),['Administración de parámetros','Conexiones','Conexión entidad participante'],'backend ancestors');
    const changed=JSON.parse(JSON.stringify(menu)); changed[0].children[0].children[2].label='Label sintético';
    r=resolveNavigationNaming(changed,'/integraciones/conexion',[{url:'/integraciones/conexion',title:'Alias Angular',breadcrumb:'Alias Angular'}]);
    eq(r.pageTitle,'Label sintético','synthetic backend label');
    changed[0].children[0].children.push({id:585,label:'Child futuro',route:'/integraciones/futura'});
    r=resolveNavigationNaming(changed,'/integraciones/futura',[{url:'/integraciones/futura',title:'Fallback',breadcrumb:'Fallback'}]);
    eq(r.pageTitle,'Child futuro','future child');
    r=resolveNavigationNaming(menu,'/reports?x=1#section',[{url:'/reports',title:'Reportes ACH',breadcrumb:'Reportes'}]);
    eq(r.pageTitle,'Reportes','query fragment');
    r=resolveNavigationNaming(menu,'/administracion/mft-ach',[{url:'/administracion/mft-ach',title:'Administración MFT ACH',breadcrumb:'MFT ACH'}]);
    eq(labels(r),['Administración de parámetros','Conexiones','Configuración del intercambio de archivos'],'MFT conceptual hierarchy');
    r=resolveNavigationNaming(menu,'/users/42/edit',[{url:'/users/42/edit',title:'Editar usuario',breadcrumb:'Editar usuario'}]);
    eq(r.pageTitle,'Editar usuario','context title'); if(r.pageTitle.includes(':id')) throw new Error('parameter token leaked');
    eq(canonicalLabelForRoute(menu,'/integraciones','Centro de integración CFA'),'Conexiones','local breadcrumb canonicalization');
    console.log('PASS NAV4.2A pure resolver behavior');
    """
)
util_path = str(ROOT / "web/ach-interbank-ui/src/app/core/services/navigation-naming.util.ts")
result = subprocess.run(["node", "-e", node_program, util_path], cwd=ROOT, capture_output=True, text=True)
require(result.returncode == 0, f"pure resolver Node test failed: {result.stderr.strip()}")
require("PASS NAV4.2A pure resolver behavior" in result.stdout, "pure resolver Node test did not report PASS")

print(f"PASS NAV4.2A static validation — {ASSERTIONS} assertions")
