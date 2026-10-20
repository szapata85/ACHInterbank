#!/usr/bin/env python3
from __future__ import annotations

import hashlib
import re
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


navigation = read("web/ach-interbank-ui/src/app/core/services/navigation.service.ts")
menu_model = read("web/ach-interbank-ui/src/app/core/models/menu.model.ts")
route_surface = read("web/ach-interbank-ui/src/app/core/services/menu-route-surface.service.ts")
route_surface_spec = read("web/ach-interbank-ui/src/app/core/services/menu-route-surface.service.spec.ts")
layout_ts = read("web/ach-interbank-ui/src/app/layout/main-layout.component.ts")
layout_html = read("web/ach-interbank-ui/src/app/layout/main-layout.component.html")
layout_scss = read("web/ach-interbank-ui/src/app/layout/main-layout.component.scss")
layout_spec = read("web/ach-interbank-ui/src/app/layout/main-layout.component.spec.ts")
app_routes = read("web/ach-interbank-ui/src/app/app-routing.module.ts")
transactions_routes = read("web/ach-interbank-ui/src/app/features/transactions/transactions-routing.module.ts")
reports_routes = read("web/ach-interbank-ui/src/app/features/reports/reports-routing.module.ts")
integrations_routes = read("web/ach-interbank-ui/src/app/features/integrations/integrations.routes.ts")
catalogs_routes = read("web/ach-interbank-ui/src/app/features/catalogs/catalogs-routing.module.ts")
admin_routes = read("web/ach-interbank-ui/src/app/features/admin/admin-routing.module.ts")
security_routes = read("web/ach-interbank-ui/src/app/features/nacha-security/nacha-security-routing.module.ts")
config_routes = read("web/ach-interbank-ui/src/app/features/nacha-config-admin/nacha-config-admin-routing.module.ts")
uat_routes = read("web/ach-interbank-ui/src/app/features/uat/uat-routing.module.ts")
phase_doc = read("docs/ai/navigation-menu/phases/PHASE_04_NAV4_SPA_COPY.md")
state_doc = read("docs/ai/navigation-menu/MEMORY_STATE.md")
root_memory = read("MEMORY.md")

# NAV4.1A remains byte-for-byte intact where it owns the navigation-tree transformation.
require(
    file_sha256("web/ach-interbank-ui/src/app/core/services/navigation.service.ts")
    == "4df488ac45465a5283a02afe795fc64c4e5995a3415556d324275cc5e1d7ea93",
    "NAV4.1A NavigationService changed",
)
require("removeRetiredMenuItems" not in navigation, "route-based menu filtering must not return")
require("normalizeRoute" not in navigation, "route normalization used for filtering must not return")
require(".filter(" not in navigation, "NavigationService must not filter backend menu items")
require("item.label" not in navigation, "NavigationService must not rewrite labels")
require("item.route" not in navigation, "NavigationService must not rewrite/compare routes")
require("children: []" not in navigation, "NavigationService must not manufacture children")
require(".sort((a, b) => (a.order ?? 0) - (b.order ?? 0))" in navigation, "backend order sorting must remain")
require("children: this.sortMenu(item.children)" in navigation, "recursive backend ordering must remain")

# No schema/model metadata was invented for NAV4.1B.
require("isNavigable" not in menu_model and "IsNavigable" not in menu_model, "do not add navigation metadata to MenuItem in NAV4.1B")
require("children?: MenuItem[]" in menu_model, "MenuItem tree model must remain intact")

# Generic router-backed classification: no production list of routes or labels.
for marker in [
    "inject(Router)",
    "this.router.config",
    "route.component || route.loadComponent",
    "route.redirectTo !== undefined",
    "route.loadChildren",
    "createNgModule",
    "ROUTES",
    "{ self: true }",
    "runInInjectionContext",
    "WeakMap<Route, Promise<Routes>>",
]:
    require(marker in route_surface, f"missing generic router-classification marker: {marker}")

for forbidden in [
    "/reports",
    "/integraciones",
    "/catalogs",
    "/users",
    "/transactions",
    "navigableParents",
    "FUNCTIONAL_PARENT",
    "STRUCTURAL_PARENT",
    "Centro de integración CFA",
]:
    require(forbidden not in route_surface, f"hardcoded production navigation classification leaked into service: {forbidden}")

# MainLayout separates a functional parent's link from its expansion button.
for marker in [
    "MenuRouteSurfaceService",
    "syncParentNavigability",
    "isNavigableParent(item: MenuItem)",
    "navigableParentIds",
]:
    require(marker in layout_ts, f"missing MainLayout parent-classification integration: {marker}")

for marker in [
    '<a',
    'class="nav-item nav-parent-link"',
    '[routerLink]="item.route"',
    '[routerLinkActiveOptions]="exactRouteMatch"',
    'class="nav-expand-toggle"',
    '[attr.data-menu-toggle-id]="item.id"',
    '[attr.aria-expanded]="isItemExpanded(item)"',
    "[attr.aria-controls]=\"'submenu-' + item.id\"",
    '(click)="toggleSubmenu(item)"',
    '<ng-template #structuralParent>',
    'type="button"',
]:
    require(marker in layout_html, f"missing functional/structural parent template marker: {marker}")

require("(click)=\"onNavItemSelected()\"" in layout_html, "functional parent link must preserve navigation/mobile-close behavior")
require("nav-expand-toggle:focus-visible" in layout_scss, "expansion toggle needs visible keyboard focus")
require("primary-sidenav.compact .nav-expand-toggle" in layout_scss, "compact sidebar must preserve expansion control")
require("nav-parent-actions" in layout_scss, "functional parent controls need stable side-by-side layout")

# Synthetic and regression tests requested by NAV4.1B.
for marker in [
    "functional future parent independent navigation and expansion controls",
    "missing structural parent as an accessible expander",
    "Reportes landing navigable",
    "ten backend children",
    "same functional-parent model to Conexiones",
    "desktop compact mode",
    "children reachable on mobile",
    "button[data-menu-toggle-id=\"40\"]",
    "aria-controls",
    "aria-current",
    "Resumen de reportes",
    "Centro de integración CFA",
    "Configuración del intercambio de archivos",
]:
    require(marker in layout_spec, f"missing MainLayout regression marker: {marker}")

for marker in [
    "classifies direct components and pathless default components as functional",
    "classifies redirects and missing routes as structural",
    "lazy route arrays",
    "lazy NgModule route configuration generically",
    "future registered parent",
]:
    require(marker in route_surface_spec, f"missing route-classification test marker: {marker}")

# Current routing evidence that drives the 13-parent inventory (4 functional, 9 structural).
require("{ path: '', pathMatch: 'full', redirectTo: 'list' }" in transactions_routes, "Operación evidence changed")
require("ReportsHomeComponent" in reports_routes and "{ path: '', component: ReportsHomeComponent" in reports_routes, "Reportes functional landing evidence missing")
require("path: ''" in integrations_routes and "IntegrationWorkspaceComponent" in integrations_routes and "redirectTo" not in integrations_routes, "Conexiones functional landing evidence missing")
require("path: ''" in catalogs_routes and "component: CatalogsListComponent" in catalogs_routes, "Catálogos functional landing evidence missing")
require("component: UsersListComponent" in admin_routes and re.search(r"path:\s*'',\s*\n\s*component:\s*UsersListComponent", admin_routes) is not None, "Usuarios y acceso functional landing evidence missing")
require("{ path: '', pathMatch: 'full', redirectTo: 'certificates' }" in security_routes, "Seguridad de archivos structural redirect evidence missing")
require("{ path: '', pathMatch: 'full', redirectTo: 'perfiles' }" in config_routes, "Configuración NACHA-M structural redirect evidence missing")
require("{ path: '', pathMatch: 'full', redirectTo: 'nacha-inbound-simulator' }" in uat_routes, "Herramientas de prueba structural redirect evidence missing")
require(re.search(r"path:\s*'logs',\s*\n\s*pathMatch:\s*'full',\s*\n\s*redirectTo:\s*'audit-logs'", app_routes) is not None, "Registros del sistema redirect evidence missing")
for absent_structural_path in ["seguimiento-control", "seguimiento-control/respuestas-transacciones", "administracion-parametros"]:
    require(f"path: '{absent_structural_path}'" not in app_routes, f"unexpected SPA route now exists for structural parent {absent_structural_path}")
require(re.search(r"path:\s*'administracion'\s*[,}]", app_routes) is None, "Administración unexpectedly became a functional route")

# Routing/security/seed were inspection-only in NAV4.1B.
protected_files = {
    "src/Cfa.ACHInterbank.Application/Navigation/Queries/GetMenuForCurrentUserHandler.cs": "f111e416c181fc1912c297e37012cf7462f49d0434c1ef6ec9416c89b5ed1a61",
    "src/Cfa.ACHInterbank.Persistence/Navigation/Seeders/NavigationInformationArchitectureSeeder.cs": "28371549f4b9150fdf738bee11454c98117ba420d1abae0e9971e2c49aee9f4c",
    "src/Cfa.ACHInterbank.Persistence/Security/Seeders/TestToolsMenuAuthorizationSeeder.cs": "bdd2edfef249883a4a19e2d674e354d37f9d6af535634650ef7afd9de4862798",
    "src/Cfa.ACHInterbank.Persistence/Security/Seeders/FineGrainedGrantBackfillSeeder.cs": "e4f2b80b0b9e9c3aff636fb03e96b819019c07eeabc549bcfe4f789756775437",
    "src/Cfa.ACHInterbank.Persistence/Security/Seeders/LegacyFineGrainedRoleMigrationSeeder.cs": "e2366e75faab4ee31498d0cb84a93899a2a928698b01e4401a38ab1cc9c18105",
    "web/ach-interbank-ui/src/app/app-routing.module.ts": "14304a5b4db288552a401d71f62d77f0e5c1837bae5e0a4cb0d322a5a8ec75af",
    "web/ach-interbank-ui/src/app/core/models/menu.model.ts": "9a0602ac6a2a358f7f7d8ad040df706d7fe100f98ca84419bb85a2073b371c77",
}
for path, expected in protected_files.items():
    require(file_sha256(path) == expected, f"protected NAV3/NAV4.1A file changed unexpectedly: {path}")

protected_groups = {
    "src/Cfa.ACHInterbank.Application/Security": "e6c89e8dc66aba73e2075c66919a60656f62bfc4e1e5867b0733d3d1d263e2ef",
    "src/Cfa.ACHInterbank.Persistence/Navigation/Seeders": "512f43f6a12bfde3241a6801efcefa1123b9ddd5451fb3d284c56ff19dedfef5",
    "src/Cfa.ACHInterbank.Persistence/Security/Seeders": "5c2874626c8df6ed53bc86616580b96456fa57322a2eb00e0dfe2f7df8282c01",
    "web/ach-interbank-ui/src/app/core/guards": "c3e43798fddd8b4f0fc5aca24032f605be4598b485de3fe4f2af1a2c5dfcaf57",
}
for prefix, expected in protected_groups.items():
    require(aggregate_sha256(prefix) == expected, f"protected NAV3/security group changed unexpectedly: {prefix}")

fine = read("src/Cfa.ACHInterbank.Persistence/Security/Seeders/FineGrainedGrantBackfillSeeder.cs")
legacy = read("src/Cfa.ACHInterbank.Persistence/Security/Seeders/LegacyFineGrainedRoleMigrationSeeder.cs")
require("BackfillOrder = 119" in fine, "FineGrainedGrantBackfillSeeder must remain Order 119")
require("MigrationOrder = 120" in legacy, "LegacyFineGrainedRoleMigrationSeeder must remain Order 120")

# Handoff/state documentation must state the inspected inventory and phase boundaries.
for marker in [
    "NAV4.1B",
    "13",
    "FUNCTIONAL_PARENT",
    "STRUCTURAL_PARENT",
    "Reportes",
    "Conexiones",
    "Usuarios y acceso",
    "Catálogos",
    "router",
    "aria-expanded",
    "NOT_RUN_ENVIRONMENT",
]:
    require(marker in phase_doc, f"NAV4 phase documentation missing marker: {marker}")

require("active_phase: NAV4.1B" in state_doc, "MEMORY_STATE active phase must be NAV4.1B")
require("active_phase_status: IMPLEMENTED_UNVERIFIED_ENVIRONMENT" in state_doc, "NAV4.1B state must remain unverified by runtime")
require("current_source_package: ACHInterbank-R34-NAV-IA-NAV4.1A-FULL-PROJECT.zip" in state_doc, "source handoff package must be NAV4.1A")
require("NAV4.1B" in root_memory and "router" in root_memory.lower(), "root memory must capture durable NAV4.1B decision")

print(f"PASS NAV4.1B static validation — {ASSERTIONS} assertions")
