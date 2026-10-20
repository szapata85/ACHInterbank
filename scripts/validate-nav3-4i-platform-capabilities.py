#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
assertions = 0

def read(path: str) -> str:
    return (ROOT / path).read_text(encoding='utf-8')

def require(condition: bool, message: str) -> None:
    global assertions
    assertions += 1
    if not condition:
        raise AssertionError(message)

def between(source: str, start: str, end: str) -> str:
    i = source.find(start)
    require(i >= 0, f'missing start marker: {start}')
    j = source.find(end, i + len(start))
    require(j > i, f'missing end marker after {start}: {end}')
    return source[i:j]

fine = read('src/Cfa.ACHInterbank.Application/Security/FineGrainedPermissions.cs')
policies = read('src/Cfa.ACHInterbank.External/DependencyInjectionService.cs')
perm_cfg = read('src/Cfa.ACHInterbank.Persistence/Configuration/PermissionConfiguration.cs')
role_cfg = read('src/Cfa.ACHInterbank.Persistence/Configuration/RoleConfiguration.cs')
role_perm_cfg = read('src/Cfa.ACHInterbank.Persistence/Configuration/RolePermissionConfiguration.cs')
permission_seed = read('src/Cfa.ACHInterbank.Persistence/Security/Seeders/PaymentRailCapabilityRegistryPermissionSeeder.cs')
menu_seed = read('src/Cfa.ACHInterbank.Persistence/Security/Seeders/PaymentRailCapabilityRegistryMenuAuthorizationSeeder.cs')
ia = read('src/Cfa.ACHInterbank.Persistence/Navigation/Seeders/NavigationInformationArchitectureSeeder.cs')
app_routing = read('web/ach-interbank-ui/src/app/app-routing.module.ts')
feature_routing = read('web/ach-interbank-ui/src/app/features/payment-rail-capability-registry/payment-rail-capability-registry-routing.module.ts')
component = read('web/ach-interbank-ui/src/app/features/payment-rail-capability-registry/pages/payment-rail-capability-registry-page.component.ts')
html = read('web/ach-interbank-ui/src/app/features/payment-rail-capability-registry/pages/payment-rail-capability-registry-page.component.html')
ng_service = read('web/ach-interbank-ui/src/app/features/payment-rail-capability-registry/services/payment-rail-capability-registry-api.service.ts')
ng_service_spec = read('web/ach-interbank-ui/src/app/features/payment-rail-capability-registry/services/payment-rail-capability-registry-api.service.spec.ts')
controller = read('src/Cfa.ACHInterbank.Api/Controllers/PaymentRailCapabilityRegistryController.cs')
contract = read('src/Cfa.ACHInterbank.Application/ACH/Interfaces/PaymentRails/IPaymentRailCapabilityRegistryService.cs')
service = read('src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/PaymentRailCapabilityRegistryService.cs')
handler = read('src/Cfa.ACHInterbank.Application/Navigation/Queries/GetMenuForCurrentUserHandler.cs')
nav_menu_seed = read('src/Cfa.ACHInterbank.Persistence/Security/Seeders/NavigationMenuAuthorizationSeeder.cs')
system_seed = read('src/Cfa.ACHInterbank.Persistence/Security/Seeders/SystemRecordsMenuAuthorizationSeeder.cs')
navlogs = read('src/Cfa.ACHInterbank.Api/Controllers/NavigationLogsController.cs')
reports_routing = read('web/ach-interbank-ui/src/app/features/reports/reports-routing.module.ts')

# Canonical capability already existed; NAV3.4I materializes it instead of inventing another name.
require('public const string CanViewPaymentRailCapabilityRegistry = "CanViewPaymentRailCapabilityRegistry";' in fine,
        'canonical capability constant missing')
require('CanViewPaymentRailCapabilityRegistry,' in fine[fine.find('AllPermissions'):],
        'canonical capability missing from AllPermissions')
require('PaymentRailCapability' not in fine.replace('CanViewPaymentRailCapabilityRegistry', ''),
        'unexpected duplicate conceptual permission name detected')
require('CanViewPaymentRailCapabilityRegistry' not in perm_cfg,
        'dedicated capability should be materialized by seeder, not duplicated in model HasData')

# Idempotent persistence with functional Spanish description and no grants.
require('public sealed class PaymentRailCapabilityRegistryPermissionSeeder' in permission_seed,
        'permission seeder missing')
require('FirstOrDefaultAsync(item => item.Name == FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry)' in permission_seed,
        'permission seeder must search by canonical name')
require('?? await context.Permissions.FindAsync(PermissionId)' in permission_seed,
        'permission seeder must fall back to stable id')
require('permission.Name = FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry;' in permission_seed,
        'permission seeder canonical name mismatch')
require('permission.Description = "Consultar capacidades de la plataforma";' in permission_seed,
        'permission functional description mismatch')
for forbidden in ('RolePermission', 'AdminRoleId', 'OperatorRoleId', 'UserPermission'):
    require(forbidden not in permission_seed, f'permission seeder creates forbidden grant: {forbidden}')
require('PaymentRailCapabilityRegistryPermissionSeeder.PermissionId' not in role_perm_cfg,
        'dedicated permission must not have RolePermission seed')

# Effective backend policy remains the already-correct three-way read policy.
policy = between(policies,
                 'options.AddPolicy(FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry',
                 'options.AddPolicy(P0Policies.TransactionsRead')
require('ctx.User.HasClaim("permission", FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry)' in policy,
        'backend policy missing dedicated capability')
require('ctx.User.HasClaim("permission", "CanManageAch")' in policy,
        'backend policy missing CanManageAch fallback')
require('ctx.User.HasClaim("permission", "CanReadAch")' in policy,
        'backend policy missing CanReadAch fallback')
require('IsInRole' not in policy and '"Admin"' not in policy and '"ACH.Operator"' not in policy,
        'backend policy must not depend on role names')

# Seeded audience evidence: only source-defined Admin/Operator roles, both with both legacy fallbacks.
require('Name = "Admin"' in role_cfg and 'Name = "ACH.Operator"' in role_cfg,
        'expected seeded roles missing')
require('RoleId = RoleConfiguration.AdminRoleId,\n                PermissionId = PermissionConfiguration.ManageAchPermissionId' in role_perm_cfg,
        'Admin CanManageAch grant missing')
require('RoleId = RoleConfiguration.AdminRoleId,\n                PermissionId = PermissionConfiguration.ReadAchPermissionId' in role_perm_cfg,
        'Admin CanReadAch grant missing')
require('RoleId = RoleConfiguration.OperatorRoleId,\n                PermissionId = PermissionConfiguration.ManageAchPermissionId' in role_perm_cfg,
        'ACH.Operator CanManageAch grant missing')
require('RoleId = RoleConfiguration.OperatorRoleId,\n                PermissionId = PermissionConfiguration.ReadAchPermissionId' in role_perm_cfg,
        'ACH.Operator CanReadAch grant missing')

# Canonical MenuItem structure remains unchanged and new post-IA reconciler mirrors policy OR semantics.
require('public const string PlatformCapabilitiesRoute = "/payment-rail-capability-registry";' in ia,
        'platform capabilities route changed')
require('public const string PlatformCapabilitiesLabel = "Capacidades de la plataforma";' in ia,
        'platform capabilities label changed')
admin_block = between(ia, 'var (platformCapabilities, platformCapabilitiesCreated)', 'private async Task ReconcileTestToolsAsync')
require('administration.Id' in admin_block and 'order: 50' in admin_block and 'exact: true' in admin_block,
        'platform capabilities must remain Administration child Order 50')
require('public const int AuthorizationReconciliationOrder = 109;' in menu_seed,
        'NAV3.4I menu reconciliation order mismatch')
require('NavigationInformationArchitectureSeeder.PlatformCapabilitiesRoute' in menu_seed,
        'menu reconciler must target canonical route')
require('NavigationInformationArchitectureSeeder.AdministrationRoute' in menu_seed,
        'menu reconciler must verify Administration parent')
for marker in (
    'PaymentRailCapabilityRegistryPermissionSeeder.PermissionId',
    'PermissionConfiguration.ManageAchPermissionId',
    'PermissionConfiguration.ReadAchPermissionId'):
    require(marker in menu_seed, f'menu authorization alternative missing: {marker}')
require('context.MenuItemRoles.RemoveRange(roleLinks);' in menu_seed,
        'legacy MenuItemRole gate must be removed')
require('RolePermission' not in menu_seed and 'UserPermission' not in menu_seed,
        'menu reconciler must not create grants')

# SPA direct URL is permission-only and uses exactly the backend alternatives.
route = between(app_routing, "path: 'payment-rail-capability-registry'", "path: 'unauthorized'")
require('canActivate: [permissionGuard]' in route, 'SPA route must use permissionGuard')
require("permissions: ['CanViewPaymentRailCapabilityRegistry', 'CanManageAch', 'CanReadAch']" in route,
        'SPA route permission alternatives mismatch')
require('roles:' not in route and 'roleGuard' not in route,
        'SPA route must not add role-name AND boundary')
require("path: '', component: PaymentRailCapabilityRegistryPageComponent" in feature_routing,
        'feature route changed unexpectedly')

# UI remains read-only: only three GET methods and no editing/cutover action.
for method in ('getRails()', 'getCapabilitiesByRail(', 'getCapabilityByRail('):
    require(method in ng_service, f'Angular GET operation missing: {method}')
require(ng_service.count('this.api.get<') == 3, 'Angular service must expose exactly three GET calls')
for forbidden in ('this.api.post<', 'this.api.put<', 'this.api.patch<', 'this.api.delete<'):
    require(forbidden not in ng_service, f'Angular mutation detected: {forbidden}')
require('Modo\', valor: \'Solo lectura\'' in component, 'read-only UI indicator missing')
require('Esta pantalla no permite edición' in html, 'read-only UI warning missing')
require('sin cutover' in html.lower(), 'no-cutover UI statement missing')
for marker in ("expect(req.request.method).toBe('GET')",):
    require(ng_service_spec.count(marker) == 3, 'Angular service tests must verify all three GET operations')

# API surface remains exactly three GET actions under the canonical policy.
require('[Route("api/payment-rails/capability-registry")]' in controller, 'controller route changed')
require(controller.count('[HttpGet(') == 3, 'controller must expose exactly three GET endpoints')
require(controller.count('[Authorize(Policy = FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry)]') == 3,
        'all GET endpoints must use canonical read policy')
for forbidden in ('[HttpPost', '[HttpPut', '[HttpPatch', '[HttpDelete'):
    require(forbidden not in controller, f'controller mutation endpoint detected: {forbidden}')

# The underlying service has an internal mutator, but it is not surfaced by this UI/controller.
require('UpsertCapabilityAsync' in contract, 'known internal registry mutator contract missing')
require('UpsertCapabilityAsync' in service and 'SaveChangesAsync' in service,
        'known internal registry mutator implementation missing')
require('UpsertCapabilityAsync' not in controller, 'internal mutator leaked into read-only controller')
require('UpsertCapabilityAsync' not in ng_service, 'internal mutator leaked into read-only Angular service')

# Regression: previous NAV3.4 security boundaries and handler remain intact.
require('NavigationMenuPermissionSeeder.PermissionId' in nav_menu_seed and 'PermissionConfiguration.ManageUsersPermissionId' in nav_menu_seed,
        'NAV3.4H menu authorization regressed')
require('SystemRecordPermissionsSeeder.NavigationReadPermissionId' in system_seed and 'PermissionConfiguration.ReadAchPermissionId' in system_seed,
        'NAV3.4G2 navigation log authorization regressed')
require("permissions: ['NavigationLogs.Read', 'CanReadAch']" in app_routing,
        'navigation logs SPA authorization regressed')
require('[Authorize(Policy = P1Policies.NavigationLogsRead)]' in navlogs,
        'navigation logs API authorization regressed')
require("path: 'audit'" in reports_routing and "permissions: ['CanReadAch']" in reports_routing,
        '/reports/audit regressed')
require('return hasRole && hasPermission;' in handler,
        'GetMenuForCurrentUserHandler changed unexpectedly')

print(f'PASS: NAV3.4I static focal validation ({assertions} assertions)')
