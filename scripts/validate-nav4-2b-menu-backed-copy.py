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


def sha(path: str) -> str:
    return hashlib.sha256((ROOT / path).read_bytes()).hexdigest()


# NAV4.1A / NAV4.1B / NAV4.2A core behavior must remain byte-identical.
protected = {
    "web/ach-interbank-ui/src/app/core/services/navigation.service.ts": "4df488ac45465a5283a02afe795fc64c4e5995a3415556d324275cc5e1d7ea93",
    "web/ach-interbank-ui/src/app/core/services/menu-route-surface.service.ts": "3a32dc7921e3c805e32fa9921e371109813aae9ba3a3173bda6a5b6b7030df72",
    "web/ach-interbank-ui/src/app/core/services/navigation-naming.service.ts": "de6d4a402690956914a67f26466c7ea6c1cfac16eb717dbeb4d4b6f56e59db19",
    "web/ach-interbank-ui/src/app/core/services/navigation-naming.util.ts": "17e066e755f589ee818b6fa48fbcf3963ba14886a35cdf3187e5381e80368895",
    "src/Cfa.ACHInterbank.Application/Navigation/Queries/GetMenuForCurrentUserHandler.cs": "f111e416c181fc1912c297e37012cf7462f49d0434c1ef6ec9416c89b5ed1a61",
    "src/Cfa.ACHInterbank.Persistence/Navigation/Seeders/NavigationInformationArchitectureSeeder.cs": "28371549f4b9150fdf738bee11454c98117ba420d1abae0e9971e2c49aee9f4c",
    "src/Cfa.ACHInterbank.Persistence/Security/Seeders/FineGrainedGrantBackfillSeeder.cs": "e4f2b80b0b9e9c3aff636fb03e96b819019c07eeabc549bcfe4f789756775437",
    "src/Cfa.ACHInterbank.Persistence/Security/Seeders/LegacyFineGrainedRoleMigrationSeeder.cs": "e2366e75faab4ee31498d0cb84a93899a2a928698b01e4401a38ab1cc9c18105",
    "web/ach-interbank-ui/src/app/core/guards/permission.guard.ts": "7d9541b3899543ad40802b18cb4e734a9c14e59077b732108b31396213afa73a",
    "web/ach-interbank-ui/src/app/core/guards/role.guard.ts": "520fcea2357e5cfe2b19d3ad1d0b284ee84d21149b359025126308210d6de31e",
}
for path, expected in protected.items():
    require(sha(path) == expected, f"protected file changed: {path}")

fine = read("src/Cfa.ACHInterbank.Persistence/Security/Seeders/FineGrainedGrantBackfillSeeder.cs")
legacy = read("src/Cfa.ACHInterbank.Persistence/Security/Seeders/LegacyFineGrainedRoleMigrationSeeder.cs")
require("BackfillOrder = 119" in fine, "FineGrainedGrantBackfillSeeder must remain Order 119")
require("MigrationOrder = 120" in legacy, "LegacyFineGrainedRoleMigrationSeeder must remain Order 120")

# Focal route metadata fallback is canonical while remaining only a fallback.
route_checks = {
    "web/ach-interbank-ui/src/app/features/integrations/integrations.routes.ts": [
        "title: 'Contrato del servicio'", "breadcrumb: 'Contrato del servicio'",
        "title: 'Relación de información'", "breadcrumb: 'Relación de información'",
        "title: 'Conexión entidad participante'", "breadcrumb: 'Conexión entidad participante'",
    ],
    "web/ach-interbank-ui/src/app/features/reports/reports-routing.module.ts": [
        "title: 'Reportes'", "breadcrumb: 'Reportes'",
        "title: 'Transacciones enviadas'", "title: 'Transacciones recibidas'",
        "title: 'Archivos NACHA-M'", "title: 'Trazabilidad ACH'",
    ],
    "web/ach-interbank-ui/src/app/features/admin/admin-routing.module.ts": [
        "title: 'Usuarios y acceso'", "breadcrumb: 'Usuarios y acceso'",
        "title: 'Administrar usuarios'", "breadcrumb: 'Administrar usuarios'",
        "title: 'Crear usuario'", "breadcrumb: 'Nuevo usuario'",
        "title: 'Editar usuario'", "breadcrumb: 'Editar usuario'",
        "title: 'Administrar perfiles de acceso'", "breadcrumb: 'Perfiles de acceso'",
    ],
    "web/ach-interbank-ui/src/app/app-routing.module.ts": [
        "title: 'Configuración del intercambio de archivos'",
        "breadcrumb: 'Configuración del intercambio de archivos'",
        "title: 'Operación'", "breadcrumb: 'Operación'",
        "title: 'Conexiones'", "breadcrumb: 'Conexiones'",
    ],
}
for path, markers in route_checks.items():
    source = read(path)
    for marker in markers:
        require(marker in source, f"missing canonical/focal route metadata {marker!r} in {path}")

# Principal page-name headings / dynamic shared heading are aligned without a route->label map.
heading_checks = {
    "web/ach-interbank-ui/src/app/features/integrations/presentation/integration-copy.ts": ["centerTitle: 'Conexiones'"],
    "web/ach-interbank-ui/src/app/features/integrations/pages/contract-versions-page.component.html": [">Contrato del servicio<"],
    "web/ach-interbank-ui/src/app/features/integrations/pages/soap-integration-settings.component.html": [">Conexión entidad participante<"],
    "web/ach-interbank-ui/src/app/features/ach-colombia-mft-administration/ach-colombia-mft-administration.component.html": [">Configuración del intercambio de archivos<"],
    "web/ach-interbank-ui/src/app/features/reports/components/reports-home.component.html": ["title=\"Reportes\""],
    "web/ach-interbank-ui/src/app/features/transactions/components/transaction-list/transaction-list.component.html": ["title=\"Consultar transacciones\""],
    "web/ach-interbank-ui/src/app/features/transactions/components/bulk-ingestion-upload/bulk-ingestion-upload.component.html": ["title=\"Cargar transacciones desde archivo\""],
    "web/ach-interbank-ui/src/app/features/transactions/components/bulk-ingestion-tracking/bulk-ingestion-tracking.component.html": ["title=\"Seguimiento de cargas\""],
    "web/ach-interbank-ui/src/app/features/nacha-security/components/nacha-certificate-manager.component.html": ["title=\"Certificados digitales\""],
    "web/ach-interbank-ui/src/app/features/nacha-security/components/digital-envelope-tool.component.html": ["title=\"Cifrar y descifrar archivos\""],
    "web/ach-interbank-ui/src/app/features/auth-logs/components/auth-log.component.html": ["title=\"Registro de accesos\""],
    "web/ach-interbank-ui/src/app/features/navigation-logs/components/navigation-log.component.html": ["title=\"Registro de navegación\""],
    "web/ach-interbank-ui/src/app/features/navigation/components/navigation-menu.component.html": ["title=\"Organización del menú\""],
}
for path, markers in heading_checks.items():
    source = read(path)
    for marker in markers:
        require(marker in source, f"missing canonical principal heading marker {marker!r} in {path}")

users = read("web/ach-interbank-ui/src/app/features/admin/components/users-list.component.ts")
users_html = read("web/ach-interbank-ui/src/app/features/admin/components/users-list.component.html")
for marker in [
    "pageHeading = String(this.route.snapshot.data['title'] ?? '')",
    ".menu$",
    "this.navigationNaming.canonicalLabelForRoute(",
    "this.router.url",
]:
    require(marker in users, f"shared users landing missing naming marker: {marker}")
require("{{ pageHeading }}" in users_html, "users visible heading must use effective naming")
for forbidden in ["this.router.url === '/users'", "'/users': 'Usuarios y acceso'", "routeToLabel"]:
    require(forbidden not in users, f"users heading introduced a hardcoded route taxonomy: {forbidden}")

report_list = read("web/ach-interbank-ui/src/app/features/reports/components/report-list-page.component.ts")
for marker in [
    "sent: { key, title: 'Transacciones enviadas'",
    "received: { key, title: 'Transacciones recibidas'",
    "files: { key, title: 'Archivos NACHA-M'",
]:
    require(marker in report_list, f"report child heading not canonical: {marker}")

# Known legacy aliases must not remain as the principal naming mechanism for focal surfaces.
require("centerTitle: 'Centro de integración CFA'" not in read("web/ach-interbank-ui/src/app/features/integrations/presentation/integration-copy.ts"), "legacy integration screen title returned")
require("title=\"Central de Reportes ACH\"" not in read("web/ach-interbank-ui/src/app/features/reports/components/reports-home.component.html"), "legacy reports screen title returned")
require("<h2 id=\"users-title\">Administración de usuarios</h2>" not in users_html, "legacy users screen title returned")

# NAV4.2A remains backend-first after menu load; this test is explicit in the focal spec.
naming_spec = read("web/ach-interbank-ui/src/app/core/services/navigation-naming.service.spec.ts")
for marker in [
    "Fallback X",
    "Canonical Y",
    "keeps backend naming as source of truth after menu load",
    "/reports?x=1#section",
    "/users/42/edit",
    "/administracion/mft-ach",
]:
    require(marker in naming_spec, f"missing NAV4.2A/NAV4.2B regression spec marker: {marker}")

# Execute the unchanged pure resolver against fallback-vs-backend and contextual/query cases.
node_program = textwrap.dedent(r"""
const fs=require('fs'),vm=require('vm'),ts=require('typescript');
const file=process.argv[1];
const js=ts.transpileModule(fs.readFileSync(file,'utf8'),{compilerOptions:{target:ts.ScriptTarget.ES2022,module:ts.ModuleKind.CommonJS}}).outputText;
const mod={exports:{}}; vm.runInNewContext(js,{module:mod,exports:mod.exports,require,console},{filename:file});
const {resolveNavigationNaming}=mod.exports;
const menu=[
 {id:50,label:'Administración de parámetros',route:'/administracion-parametros',children:[{id:58,label:'Conexiones',route:'/integraciones',children:[{id:584,label:'Configuración del intercambio de archivos',route:'/administracion/mft-ach'}]}]},
 {id:70,label:'Canonical Y',route:'/reports'},
 {id:80,label:'Administración',route:'/administracion',children:[{id:81,label:'Usuarios y acceso',route:'/users'}]}
];
const labels=x=>x.breadcrumbs.map(b=>b.label);
const eq=(a,b,n)=>{if(JSON.stringify(a)!==JSON.stringify(b))throw new Error(n+': '+JSON.stringify(a)+' != '+JSON.stringify(b));};
let r=resolveNavigationNaming(menu,'/reports?x=1#section',[{url:'/reports',title:'Fallback X',breadcrumb:'Fallback X'}]);
eq(r.pageTitle,'Canonical Y','backend beats fallback');
r=resolveNavigationNaming(menu,'/administracion/mft-ach',[{url:'/administracion/mft-ach',title:'Configuración del intercambio de archivos',breadcrumb:'Configuración del intercambio de archivos'}]);
eq(labels(r),['Administración de parámetros','Conexiones','Configuración del intercambio de archivos'],'backend ancestors');
r=resolveNavigationNaming(menu,'/users/42/edit',[{url:'/users/42/edit',title:'Editar usuario',breadcrumb:'Editar usuario'}]);
eq(r.pageTitle,'Editar usuario','contextual metadata'); if(r.pageTitle.includes(':id')) throw new Error('parameter token leaked');
console.log('PASS NAV4.2B resolver regression');
""")
util = str(ROOT / "web/ach-interbank-ui/src/app/core/services/navigation-naming.util.ts")
result = subprocess.run(["node", "-e", node_program, util], cwd=ROOT, capture_output=True, text=True)
require(result.returncode == 0, f"pure resolver regression failed: {result.stderr.strip()}")
require("PASS NAV4.2B resolver regression" in result.stdout, "pure resolver regression did not report PASS")

# Contextual user routes retain their own metadata and are not converted to menu naming.
admin_routes = read("web/ach-interbank-ui/src/app/features/admin/admin-routing.module.ts")
for path, title in [("new", "Crear usuario"), (":id/edit", "Editar usuario"), (":id/roles", "Administrar perfiles de acceso")]:
    pattern = rf"path:\s*'{re.escape(path)}'[\s\S]*?title:\s*'{re.escape(title)}'"
    require(re.search(pattern, admin_routes) is not None, f"contextual user route {path} lost its metadata")

# MainLayout document title convention and backend shared naming are unchanged.
layout = read("web/ach-interbank-ui/src/app/layout/main-layout.component.ts")
for marker in [
    "this.navigationNaming.resolve(",
    "this.documentTitle.setTitle(`${this.productName} - ${this.pageTitle}`)",
    ".menu$",
]:
    require(marker in layout, f"MainLayout NAV4.2A behavior changed: {marker}")

print(f"PASS NAV4.2B static validation — {ASSERTIONS} assertions")
