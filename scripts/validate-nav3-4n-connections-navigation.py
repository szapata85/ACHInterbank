#!/usr/bin/env python3
from __future__ import annotations

import hashlib
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
checks = 0


def require(condition: bool, message: str) -> None:
    global checks
    checks += 1
    if not condition:
        raise AssertionError(message)


def read(path: str) -> str:
    return (ROOT / path).read_text(encoding="utf-8")


def sha(path: str) -> str:
    return hashlib.sha256((ROOT / path).read_bytes()).hexdigest()


def between(source: str, start: str, end: str) -> str:
    begin = source.find(start)
    require(begin >= 0, f"missing start marker: {start}")
    finish = source.find(end, begin + len(start))
    require(finish > begin, f"missing end marker after {start}: {end}")
    return source[begin:finish]


seeder = read("src/Cfa.ACHInterbank.Persistence/Security/Seeders/ConnectionsStructuralMenuAuthorizationSeeder.cs")
nav_service = read("web/ach-interbank-ui/src/app/core/services/navigation.service.ts")
nav_spec = read("web/ach-interbank-ui/src/app/core/services/navigation.service.spec.ts")
ia = read("src/Cfa.ACHInterbank.Persistence/Navigation/Seeders/NavigationInformationArchitectureSeeder.cs")
menu_seed = read("src/Cfa.ACHInterbank.Persistence/Configuration/MenuItemConfiguration.cs")
mft_auth = read("src/Cfa.ACHInterbank.Persistence/Security/Seeders/MftAdministrationMenuAuthorizationSeeder.cs")
nav34n_test = read("tests/Cfa.ACHInterbank.Tests/NavigationMenuNav34NConnectionsNavigationTests.cs")
main_layout = read("web/ach-interbank-ui/src/app/layout/main-layout.component.html")
routing = read("web/ach-interbank-ui/src/app/app-routing.module.ts")

# Canonical parent identity and hierarchy remain NAV2-owned.
for token in (
    "public const int IntegrationsId = 29;",
    'Route = "/integraciones"',
    'Label = "Integraciones"',
    "Order = 5",
    "IsActive = true",
):
    require(token in menu_seed, f"legacy seed identity missing: {token}")

parameters_block = between(ia, "var connections = await ReconcileExistingAsync(", "await ReconcileConnectionsAsync(connections);")
for token in (
    "ConnectionsRoute",
    '"Conexiones"',
    "parametersRoot.Id",
    "order: 80",
):
    require(token in parameters_block, f"canonical Conexiones property changed: {token}")

connections_block = between(ia, "private async Task ReconcileConnectionsAsync", "private async Task ReconcileStatusMappingsAsync")
for token in (
    '"Contrato del servicio"',
    "order: 10",
    '"Relación de información"',
    "order: 20",
    '"Conexión entidad participante"',
    "order: 30",
    '"Configuración del intercambio de archivos"',
    "order: 40",
):
    require(token in connections_block, f"canonical Conexiones child changed: {token}")
for route in (
    'private const string ContractRoute = "/integraciones/contrato";',
    'private const string InformationRelationshipRoute = "/integraciones/relacion-informacion";',
    'private const string ParticipantConnectionRoute = "/integraciones/conexion";',
    'private const string FileExchangeConfigurationRoute = "/administracion/mft-ach";',
):
    require(route in ia, f"canonical child route changed: {route}")

# Current legacy child menu authorization is intentionally preserved by IA mirroring.
for token in (
    "MirrorAuthorizationIfEmptyAsync(connections.Id, contract.Id)",
    "MirrorAuthorizationIfEmptyAsync(connections.Id, informationRelationship.Id)",
    "MirrorAuthorizationIfEmptyAsync(connections.Id, participantConnection.Id)",
):
    require(token in connections_block, f"legacy child authorization mirroring changed: {token}")

# NAV3.4N structural parent authorization: no role gate, existing permission union only.
require("AuthorizationReconciliationOrder = 114" in seeder, "NAV3.4N seeder order must follow NAV3.4M")
require('ConnectionsRoute = "/integraciones"' in seeder, "NAV3.4N must target only canonical Conexiones route")
require("MenuItemConfiguration.IntegrationsId" in seeder, "NAV3.4N must preserve canonical parent ID")
for token in (
    "PermissionConfiguration.ManageUsersPermissionId",
    '"CanManageUsers"',
    "ConfigurationPermissionSeeder.ReadPermissionId",
    "FineGrainedPermissions.Config.Read",
    "PermissionConfiguration.ReadAchPermissionId",
    '"CanReadAch"',
):
    require(token in seeder, f"parent existing permission union missing: {token}")
require("context.MenuItemRoles.RemoveRange(parentRoleLinks);" in seeder, "Conexiones direct role gate must be removed")
require("context.MenuItemRoles.RemoveRange(mftRoleLinks);" in seeder, "MFT residual Admin role must be removed")
require("RolePermissions" not in seeder, "NAV3.4N must not create role grants")
require("context.Permissions.Add" not in seeder, "NAV3.4N must not create capabilities")
require("new Permission" not in seeder, "NAV3.4N must not create permission records")
for forbidden in (
    "/integraciones/contrato",
    "/integraciones/relacion-informacion",
    "/integraciones/conexion",
):
    require(forbidden not in seeder, f"NAV3.4N must not rewrite child authorization directly: {forbidden}")

# MFT permission alternatives remain NAV3.4M Config.Read OR CanReadAch; NAV3.4N removes only role residual.
require("ConfigurationPermissionSeeder.ReadPermissionId" in mft_auth, "MFT Config.Read reconciliation missing")
require("PermissionConfiguration.ReadAchPermissionId" in mft_auth, "MFT CanReadAch fallback missing")
require("Config.Manage" not in mft_auth, "MFT menu entry must not require management permission")
for token in (
    "permissions: ['Config.Read', 'CanReadAch']",
    "this.auth.hasPermission(['Config.Manage', 'CanManageAch'])",
):
    target = routing if token.startswith("permissions") else read("web/ach-interbank-ui/src/app/features/ach-colombia-mft-administration/ach-colombia-mft-administration.component.ts")
    require(token in target, f"NAV3.4M SPA contract changed: {token}")

# NavigationService now trusts backend Conexiones node and authorized children.
require("normalizeIntegrationNavigation" not in nav_service, "integration hardcoded normalization must be removed")
require("INTEGRATION_CENTER_ROUTE" not in nav_service, "integration hardcoded route alias must be removed")
require("OBSOLETE_INTEGRATION_NAV_ROUTES" not in nav_service, "integration hardcoded replacement routes must be removed")
require("Centro de integración CFA" not in nav_service, "NavigationService must not override backend Conexiones label")
require("children: []" not in nav_service, "NavigationService must not flatten backend integration children")
require("this.sortMenu(this.removeRetiredMenuItems(items ?? []))" in nav_service, "NavigationService must retain generic backend sorting/filtering only")
for token in (
    "preserva Conexiones y sus hijos exactamente desde el backend",
    "'/integraciones/contrato'",
    "'/integraciones/relacion-informacion'",
    "'/integraciones/conexion'",
    "'/administracion/mft-ach'",
    "expect(menu[0].children?.map((item) => item.order)).toEqual([10, 20, 30, 40]);",
    "expect(flattenLabels(menu)).not.toContain('Centro de integración CFA');",
    "no fabrica ni sustituye rutas de integración que el backend no devolvió",
):
    require(token in nav_spec, f"NavigationService focal regression missing: {token}")

# Existing MainLayout structural-parent behavior remains unchanged: parents with children toggle only.
require("*ngIf=\"item.children?.length; else navigableMenuItem\"" in main_layout, "parent branch behavior changed")
require('(click)="toggleSubmenu(item)"' in main_layout, "structural parent must remain an expander")
require('[routerLink]="item.route"' in main_layout, "leaf navigation behavior changed")

# Parent SPA still exists and remains explicitly out of NAV3.4N authorization normalization.
parent_route = between(routing, "path: 'integraciones'", "path: 'navigation'")
require("loadChildren" in parent_route and "integrations.routes" in parent_route, "functional /integraciones SPA route must remain")
require("roles: ['Admin', 'ACH.Operator']" in parent_route, "parent SPA role gate must remain unchanged")
require("permissions: ['Integrations.Read', 'CanReadAch', 'CanManageAch']" in parent_route, "parent SPA permission gate must preserve legacy alternatives and admit Integrations.Read after NAV3.4R")

# .NET focal tests characterize structure, filtering, no sibling leakage, no grants and MFT continuity.
for token in (
    "StructuralSeeder_PreservesCanonicalTree_UsesExistingPermissionUnion_AndRemovesRoleGates",
    "MenuFiltering_ConfigReadOnly_KeepsParametersAndConnectionsAroundOnlyMftChild",
    "MenuFiltering_CanReadAchOnly_KeepsParametersAndConnectionsAroundOnlyMftChild",
    "MenuFiltering_CurrentLegacyChildAuthorization_KeepsParentWithoutGrantingMft",
    "MenuFiltering_NoChildCapability_DoesNotExposeConnectionsOrItsChildren",
    "SourceRegression_HandlerAndMftSpaApiRemainGenericAndNav34MPoliciesStayUnchanged",
    "Assert.Equal(beforeLegacyChildren",
    "Assert.Equal(beforeGrants",
):
    require(token in nav34n_test, f"NAV3.4N focal test missing: {token}")

# Byte-level regression guard: handler, three excluded surfaces, MFT functional/API/SPA and TestTools stay NAV3.4M-identical.
expected_hashes = {
    "src/Cfa.ACHInterbank.Application/Navigation/Queries/GetMenuForCurrentUserHandler.cs": "f111e416c181fc1912c297e37012cf7462f49d0434c1ef6ec9416c89b5ed1a61",
    "web/ach-interbank-ui/src/app/app-routing.module.ts": "14304a5b4db288552a401d71f62d77f0e5c1837bae5e0a4cb0d322a5a8ec75af",
    "web/ach-interbank-ui/src/app/features/integrations/integrations.routes.ts": "7a0106cf22e807826c97151e0e3af540ec30eb85ceeec6408b1e37b117a7b822",
    "src/Cfa.ACHInterbank.Api/Controllers/IntegrationCatalogController.cs": "65abda2dcd2230222bb92c4360047633d45f34a9e0ae46ebcb719ebb71975650",
    "src/Cfa.ACHInterbank.Api/Controllers/IntegrationContractsController.cs": "5fcf0a4f281304dde87733ca6579d38c0bc65ebb5c2b7135014545ccb25fefbd",
    "src/Cfa.ACHInterbank.Api/Controllers/IntegrationMappingSetsController.cs": "32ba2302fdb6f48d15135a3e9afdd48b8dd8b182076c6e07feb085e672a2fa91",
    "src/Cfa.ACHInterbank.Api/Controllers/SoapIntegrationSettingsController.cs": "6f1b17cef092cb5d5476d30febcde65b73ee1d06963b6f5c630ebdf4236acc66",
    "web/ach-interbank-ui/src/app/features/ach-colombia-mft-administration/ach-colombia-mft-administration.component.ts": "dac1bf2b458ce23c74d49758a66f931c583cf3410a175ccd451094d9d08e0499",
    "src/Cfa.ACHInterbank.Api/Controllers/AchColombiaFileExchangeController.cs": "6ec050055e13ad74403bc9167787924c65784f83ee4a8f51d39b0203ccad9bb2",
    "src/Cfa.ACHInterbank.Api/Controllers/AchColombiaCycleOperationalSchedulesController.cs": "dab894745a44c0fc7b4ae76da8b4e9f3283e5898909c2da65b5f36b49818c276",
    "src/Cfa.ACHInterbank.Persistence/Navigation/Seeders/NavigationInformationArchitectureSeeder.cs": "28371549f4b9150fdf738bee11454c98117ba420d1abae0e9971e2c49aee9f4c",
    "src/Cfa.ACHInterbank.Persistence/Security/Seeders/TestToolsMenuAuthorizationSeeder.cs": "bdd2edfef249883a4a19e2d674e354d37f9d6af535634650ef7afd9de4862798",
}
for path, expected in expected_hashes.items():
    require(sha(path) == expected, f"protected NAV3.4M/NAV2 artifact changed unexpectedly: {path}")

# Characterize the unchanged handler semantics with the NAV3.4N parent/child model.
# Menu filtering is Role-OR-within-roles AND Permission-OR-within-permissions, then visible
# children attach to visible parents; otherwise they become roots.
items = [
    dict(route="/administracion-parametros", parent=None, roles=set(), permissions=set()),
    dict(route="/integraciones", parent="/administracion-parametros", roles=set(), permissions={"CanManageUsers", "Config.Read", "CanReadAch"}),
    dict(route="/integraciones/contrato", parent="/integraciones", roles={"Admin"}, permissions={"CanManageUsers"}),
    dict(route="/integraciones/relacion-informacion", parent="/integraciones", roles={"Admin"}, permissions={"CanManageUsers"}),
    dict(route="/integraciones/conexion", parent="/integraciones", roles={"Admin"}, permissions={"CanManageUsers"}),
    dict(route="/administracion/mft-ach", parent="/integraciones", roles=set(), permissions={"Config.Read", "CanReadAch"}),
]


def visible(roles: set[str], permissions: set[str]) -> list[str]:
    result = []
    for item in items:
        has_role = not item["roles"] or bool(item["roles"] & roles)
        has_permission = not item["permissions"] or bool(item["permissions"] & permissions)
        if has_role and has_permission:
            result.append(item["route"])
    return result

config_only = visible(set(), {"Config.Read"})
require(config_only == ["/administracion-parametros", "/integraciones", "/administracion/mft-ach"], "Config.Read-only hierarchy model regressed")
read_only = visible(set(), {"CanReadAch"})
require(read_only == ["/administracion-parametros", "/integraciones", "/administracion/mft-ach"], "CanReadAch-only hierarchy model regressed")
legacy_child = visible({"Admin"}, {"CanManageUsers"})
require(legacy_child == ["/administracion-parametros", "/integraciones", "/integraciones/contrato", "/integraciones/relacion-informacion", "/integraciones/conexion"], "legacy child hierarchy model regressed")
none = visible(set(), {"CanManageAch"})
require(none == ["/administracion-parametros"], "unrelated capability must not expose Conexiones surfaces")

print(f"NAV3.4N static validation passed: {checks} assertions")
