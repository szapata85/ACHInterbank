from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
checks = 0

def read(path: str) -> str:
    return (ROOT / path).read_text(encoding='utf-8')

def require(condition: bool, message: str) -> None:
    global checks
    checks += 1
    if not condition:
        raise AssertionError(message)

def between(source: str, start: str, end: str) -> str:
    i = source.find(start)
    require(i >= 0, f'missing start marker: {start}')
    j = source.find(end, i + len(start))
    require(j > i, f'missing end marker after {start}: {end}')
    return source[i:j]

fine = read('src/Cfa.ACHInterbank.Application/Security/FineGrainedPermissions.cs')
p1 = read('src/Cfa.ACHInterbank.Application/Security/P1Policies.cs')
policies = read('src/Cfa.ACHInterbank.External/DependencyInjectionService.cs')
perm_seed = read('src/Cfa.ACHInterbank.Persistence/Security/Seeders/SystemRecordPermissionsSeeder.cs')
menu_seed = read('src/Cfa.ACHInterbank.Persistence/Security/Seeders/SystemRecordsMenuAuthorizationSeeder.cs')
role_config = read('src/Cfa.ACHInterbank.Persistence/Configuration/RolePermissionConfiguration.cs')
app_routing = read('web/ach-interbank-ui/src/app/app-routing.module.ts')
route_spec = read('web/ach-interbank-ui/src/app/app-routing-access.spec.ts')
audit_controller = read('src/Cfa.ACHInterbank.Api/Controllers/AuditLogsController.cs')
auth_controller = read('src/Cfa.ACHInterbank.Api/Controllers/AuthLogsController.cs')
nav_controller = read('src/Cfa.ACHInterbank.Api/Controllers/NavigationLogsController.cs')
telemetry = read('web/ach-interbank-ui/src/app/core/services/navigation-log.service.ts')
ia = read('src/Cfa.ACHInterbank.Persistence/Navigation/Seeders/NavigationInformationArchitectureSeeder.cs')
handler = read('src/Cfa.ACHInterbank.Application/Navigation/Queries/GetMenuForCurrentUserHandler.cs')
reports_routing = read('web/ach-interbank-ui/src/app/features/reports/reports-routing.module.ts')
reports_api = read('web/ach-interbank-ui/src/app/core/services/reports-api.service.ts')
audit_api = read('web/ach-interbank-ui/src/app/features/audit/services/audit-log.service.ts')
cs_test = read('tests/Cfa.ACHInterbank.Tests/NavigationMenuNav34G2SystemRecordsAuthorizationTests.cs')

# Dedicated capabilities follow the existing Domain.Action convention and are included in the global catalog.
for cls, name in (('AuditLogs', 'AuditLogs.Read'), ('AuthLogs', 'AuthLogs.Read'), ('NavigationLogs', 'NavigationLogs.Read')):
    require(f'public static class {cls}' in fine, f'missing FineGrainedPermissions.{cls}')
    require(f'public const string Read = "{name}";' in fine, f'missing {name}')
require('AuditLogs.Read, AuthLogs.Read, NavigationLogs.Read' in fine, 'system-record capabilities missing from AllPermissions')

# Permission persistence is idempotent and creates no grants.
for token in (
    'FineGrainedPermissions.AuditLogs.Read, "Consultar registro de auditoría"',
    'FineGrainedPermissions.AuthLogs.Read, "Consultar registro de accesos"',
    'FineGrainedPermissions.NavigationLogs.Read, "Consultar registro de navegación"',
    'FirstOrDefaultAsync(item => item.Name == name)',
    'context.Permissions.FindAsync(id)',
    'context.Permissions.Add(permission)',
):
    require(token in perm_seed, f'permission seeder missing: {token}')
for forbidden in ('RolePermission', 'MenuItemRole', 'UserPermission', 'AdminRoleId', 'AchOperatorRoleId'):
    require(forbidden not in perm_seed, f'permission seeder must not create grants: {forbidden}')
for concept in ('AuditLogs.Read', 'AuthLogs.Read', 'NavigationLogs.Read'):
    require(concept not in role_config, f'RolePermissionConfiguration must not grant {concept}')

# Policies are explicit OR fallbacks, never role-name checks.
for const in (
    'AuditLogsRead = "P1.AuditLogsRead"',
    'AuthLogsRead = "P1.AuthLogsRead"',
    'NavigationLogsRead = "P1.NavigationLogsRead"'):
    require(const in p1, f'missing P1 policy constant {const}')
audit_policy = between(policies, 'options.AddPolicy(P1Policies.AuditLogsRead', 'options.AddPolicy(P1Policies.AuthLogsRead')
require('FineGrainedPermissions.AuditLogs.Read' in audit_policy and '"CanManageUsers"' in audit_policy, 'audit policy must be AuditLogs.Read OR CanManageUsers')
auth_policy = between(policies, 'options.AddPolicy(P1Policies.AuthLogsRead', 'options.AddPolicy(P1Policies.NavigationLogsRead')
require('FineGrainedPermissions.AuthLogs.Read' in auth_policy and '"CanManageUsers"' in auth_policy, 'auth policy must be AuthLogs.Read OR CanManageUsers')
nav_policy = between(policies, 'options.AddPolicy(P1Policies.NavigationLogsRead', 'options.AddPolicy(P1Policies.SchedulerView')
require('FineGrainedPermissions.NavigationLogs.Read' in nav_policy and '"CanReadAch"' in nav_policy, 'navigation policy must be NavigationLogs.Read OR CanReadAch')
for block in (audit_policy, auth_policy, nav_policy):
    require('RequireRole' not in block and 'IsInRole' not in block, 'system-record policies must not check role names')

# API GETs use the new policies; POST telemetry remains class-authenticated only.
require('[Authorize(Policy = P1Policies.AuditLogsRead)]' in audit_controller, 'audit GET policy missing')
require('[Authorize(Policy = P1Policies.AuthLogsRead)]' in auth_controller, 'auth GET policy missing')
require('[Authorize(Policy = P1Policies.NavigationLogsRead)]' in nav_controller, 'navigation GET policy missing')
post = between(nav_controller, '[HttpPost]', 'private string? GetCurrentUserId')
require('Authorize(Policy' not in post, 'navigation telemetry POST must not require administrative read policy')
require('_service.AddAsync' in post, 'navigation telemetry POST must remain functional')

# SPA uses OR lists, matching permissionGuard semantics.
for start, end, expected in (
    ("path: 'audit-logs'", "path: 'auth-logs'", "permissions: ['AuditLogs.Read', 'CanManageUsers']"),
    ("path: 'auth-logs'", "path: 'navigation-logs'", "permissions: ['AuthLogs.Read', 'CanManageUsers']"),
    ("path: 'navigation-logs'", "path: 'customer-third-parties'", "permissions: ['NavigationLogs.Read', 'CanReadAch']"),
):
    block = between(app_routing, start, end)
    require('canActivate: [permissionGuard]' in block, f'{start} must use permissionGuard')
    require(expected in block, f'{start} must use exact semantic+legacy alternatives')
    require('roles:' not in block, f'{start} must not hardcode roles')
require('protege los registros del sistema con capacidades semánticas y fallbacks legacy' in route_spec, 'Angular focal system-record route test missing')

# Menu children mirror their policies and structural parent accepts union of child alternatives.
for token in (
    'SystemRecordPermissionsSeeder.AuditReadPermissionId,\n            PermissionConfiguration.ManageUsersPermissionId',
    'SystemRecordPermissionsSeeder.AuthReadPermissionId,\n            PermissionConfiguration.ManageUsersPermissionId',
    'SystemRecordPermissionsSeeder.NavigationReadPermissionId,\n            PermissionConfiguration.ReadAchPermissionId',
    'SystemRecordPermissionsSeeder.AuditReadPermissionId,\n            SystemRecordPermissionsSeeder.AuthReadPermissionId,\n            SystemRecordPermissionsSeeder.NavigationReadPermissionId,\n            PermissionConfiguration.ManageUsersPermissionId,\n            PermissionConfiguration.ReadAchPermissionId',
):
    require(token in menu_seed, f'menu authorization missing expected capability set: {token}')
for forbidden in ('RolePermission', 'UserPermission', 'MenuItemRoles', 'RequireRole'):
    require(forbidden not in menu_seed, f'menu reconciler must not create/grant roles: {forbidden}')
require('AuthorizationReconciliationOrder = 107' in menu_seed, 'system-record reconciliation order changed unexpectedly')

# Parent IA and menu reconstruction algorithm remain unchanged; no orphan/leakage refactor.
require('Reconcile(systemRecords, SystemRecordsLabel, administration.Id, order: 30)' in ia, 'Registros del sistema must remain Administration order 30')
require('Reconcile(auditLog, AuditLogLabel, systemRecords.Id, order: 10)' in ia, 'audit child order/parent changed')
require('Reconcile(authenticationLog, AuthenticationLogLabel, systemRecords.Id, order: 20)' in ia, 'auth child order/parent changed')
require('NavigationLogRoute' in ia and 'order: 30' in ia, 'navigation child order/parent changed')
require('if (item.ParentId.HasValue && dtoLookup.TryGetValue(item.ParentId.Value, out var parentDto))' in handler, 'GetMenuForCurrentUserHandler nesting algorithm changed')
require('roots.Add(dto);' in handler, 'GetMenuForCurrentUserHandler root fallback changed')

# Telemetry remains independent and reports audit remains distinct.
require(".post('api/navigation-logs'" in telemetry, 'navigation telemetry POST client missing')
require('this.auth.isAuthenticated()' in telemetry, 'navigation telemetry must remain authenticated-session based')
require('NavigationLogs.Read' not in telemetry and 'CanReadAch' not in telemetry, 'telemetry client must not require admin read permissions')
require("path: 'audit'" in reports_routing and "permissions: ['CanReadAch']" in reports_routing, '/reports/audit route authorization changed')
require('api/reports/audit' in reports_api, '/reports/audit API contract changed')
require('api/audit-logs' in audit_api, 'administrative audit API contract changed')

# Focal C# tests cover persistence, grants, policy layers, parent/child behavior and telemetry separation.
for token in (
    'DedicatedPermissions_ArePersistedIdempotently_WithoutRoleGrants',
    'MenuAuthorization_UsesDedicatedOrLegacyAlternatives_AndParentUsesUnion',
    'MenuHandler_KeepsAuthorizedChildrenNested_WithoutSiblingLeakage',
    'Controllers_RequireDedicatedReadPolicies_WhileNavigationTelemetryPostRemainsAuthenticatedOnly',
    'PoliciesAndSpaRoutes_UseOrFallbacks_AndReportsAuditRemainsSeparate'):
    require(token in cs_test, f'missing focal G2 test {token}')

print(f'NAV3.4G2 static validation passed: {checks} assertions')
