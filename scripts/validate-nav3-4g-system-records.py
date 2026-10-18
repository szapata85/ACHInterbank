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

ia = read('src/Cfa.ACHInterbank.Persistence/Navigation/Seeders/NavigationInformationArchitectureSeeder.cs')
base_menu = read('src/Cfa.ACHInterbank.Persistence/Configuration/MenuItemConfiguration.cs')
base_links = read('src/Cfa.ACHInterbank.Persistence/Configuration/MenuItemPermissionConfiguration.cs')
perm_config = read('src/Cfa.ACHInterbank.Persistence/Configuration/PermissionConfiguration.cs')
fine = read('src/Cfa.ACHInterbank.Application/Security/FineGrainedPermissions.cs')
handler = read('src/Cfa.ACHInterbank.Application/Navigation/Queries/GetMenuForCurrentUserHandler.cs')
parent_seed = read('src/Cfa.ACHInterbank.Persistence/Security/Seeders/SystemRecordsMenuAuthorizationSeeder.cs')
app_routing = read('web/ach-interbank-ui/src/app/app-routing.module.ts')
route_spec = read('web/ach-interbank-ui/src/app/app-routing-access.spec.ts')
audit_routing = read('web/ach-interbank-ui/src/app/features/audit/audit-routing.module.ts')
auth_routing = read('web/ach-interbank-ui/src/app/features/auth-logs/auth-logs-routing.module.ts')
nav_routing = read('web/ach-interbank-ui/src/app/features/navigation-logs/navigation-logs-routing.module.ts')
audit_component = read('web/ach-interbank-ui/src/app/features/audit/components/audit-log.component.ts')
auth_component = read('web/ach-interbank-ui/src/app/features/auth-logs/components/auth-log.component.ts')
nav_component = read('web/ach-interbank-ui/src/app/features/navigation-logs/components/navigation-log.component.ts')
audit_service = read('web/ach-interbank-ui/src/app/features/audit/services/audit-log.service.ts')
auth_service = read('web/ach-interbank-ui/src/app/features/auth-logs/services/auth-log.service.ts')
nav_list_service = read('web/ach-interbank-ui/src/app/features/navigation-logs/services/navigation-log-list.service.ts')
nav_telemetry = read('web/ach-interbank-ui/src/app/core/services/navigation-log.service.ts')
audit_controller = read('src/Cfa.ACHInterbank.Api/Controllers/AuditLogsController.cs')
auth_controller = read('src/Cfa.ACHInterbank.Api/Controllers/AuthLogsController.cs')
nav_controller = read('src/Cfa.ACHInterbank.Api/Controllers/NavigationLogsController.cs')
audit_dto = read('src/Cfa.ACHInterbank.Application/Audit/Dtos/AuditLogDtos.cs')
auth_dto = read('src/Cfa.ACHInterbank.Application/AuthLogs/Dtos/AuthLogDtos.cs')
nav_dto = read('src/Cfa.ACHInterbank.Application/NavigationLogs/Dtos/NavigationLogDtos.cs')
audit_persistence = read('src/Cfa.ACHInterbank.Persistence/Audit/Services/AuditLogsService.cs')
auth_persistence = read('src/Cfa.ACHInterbank.Persistence/AuthLogs/Services/AuthLogsService.cs')
nav_persistence = read('src/Cfa.ACHInterbank.Persistence/NavigationLogs/Services/NavigationLogsService.cs')
reports_routing = read('web/ach-interbank-ui/src/app/features/reports/reports-routing.module.ts')
reports_api = read('web/ach-interbank-ui/src/app/core/services/reports-api.service.ts')
nav34g_test = read('tests/Cfa.ACHInterbank.Tests/NavigationMenuNav34GSystemRecordsAuthorizationTests.cs')

# Canonical structure remains exactly under Administración with approved orders.
for token in (
    'public const string SystemRecordsRoute = "/logs"',
    'public const string SystemRecordsLabel = "Registros del sistema"',
    'public const string AuditLogRoute = "/audit-logs"',
    'public const string AuditLogLabel = "Registro de auditoría"',
    'public const string AuthenticationLogRoute = "/auth-logs"',
    'public const string AuthenticationLogLabel = "Registro de accesos"',
    'public const string NavigationLogRoute = "/navigation-logs"',
    'public const string NavigationLogLabel = "Registro de navegación"'):
    require(token in ia, f'missing canonical system-record token: {token}')
require('Reconcile(systemRecords, SystemRecordsLabel, administration.Id, order: 30)' in ia, 'system-records parent must remain order 30')
require('Reconcile(auditLog, AuditLogLabel, systemRecords.Id, order: 10)' in ia, 'audit child must remain order 10')
require('Reconcile(authenticationLog, AuthenticationLogLabel, systemRecords.Id, order: 20)' in ia, 'access child must remain order 20')
nav_seed_block = between(ia, 'var (navigationLog, navigationLogCreated)', 'var menuOrganization')
require('NavigationLogRoute' in nav_seed_block and 'order: 30' in nav_seed_block, 'navigation child must remain order 30')

# Historical child permissions remain unchanged; only the structural parent is reconciled.
for item in ('AuditLogId', 'AuthLogId', 'LogsId'):
    require(f'MenuItemConfiguration.{item}' in base_links, f'missing historical link for {item}')
require(base_links.count('PermissionConfiguration.ManageUsersPermissionId') >= 3, 'historical logs family must retain CanManageUsers links in base seed')
require('PermissionConfiguration.ReadAchPermissionId' in nav_seed_block, 'navigation child must retain current CanReadAch menu link')

require('AuthorizationReconciliationOrder = 107' in parent_seed, 'system-records parent reconciliation order must follow NAV3.4F')
require('item.Id == MenuItemConfiguration.LogsId' in parent_seed, 'reconciler must target canonical /logs parent id')
require('NavigationInformationArchitectureSeeder.SystemRecordsRoute' in parent_seed, 'reconciler must verify /logs route')
require('PermissionConfiguration.ManageUsersPermissionId' in parent_seed, 'parent must remain compatible with audit/access children')
require('PermissionConfiguration.ReadAchPermissionId' in parent_seed, 'parent must remain compatible with navigation child')
for forbidden in ('MenuItemRoles', 'RolePermission', 'UserPermission', 'menuItem.Label =', 'menuItem.Route =', 'menuItem.ParentId =', 'menuItem.Order =', 'menuItem.IsActive ='):
    require(forbidden not in parent_seed, f'parent reconciler must not broaden scope via {forbidden}')

# Existing menu reconstruction algorithm is intentionally unchanged in behavior; current structural union prevents the known orphan for these children.
require('if (item.ParentId.HasValue && dtoLookup.TryGetValue(item.ParentId.Value, out var parentDto))' in handler, 'menu nesting algorithm changed unexpectedly')
require('else\n            {\n                roots.Add(dto);' in handler, 'known root fallback must remain explicit and unrefactored')
require('ShouldInclude(mi, userRoles, userPermissions)' in handler, 'child authorization filtering must remain active')

# All three administration screens are list/query surfaces in the current UI.
for service, endpoint in ((audit_service, 'api/audit-logs'), (auth_service, 'api/auth-logs'), (nav_list_service, 'api/navigation-logs')):
    require(endpoint in service, f'missing list API {endpoint}')
    require('.get<' in service, f'{endpoint} UI service must keep GET query')
    for verb in ('.post<', '.put<', '.patch<', '.delete<'):
        require(verb not in service, f'{endpoint} list UI must not add mutation {verb}')
for component in (audit_component, auth_component, nav_component):
    require('.delete(' not in component, 'log list UI must not delete logs')
    require('.post(' not in component, 'log list UI must not post mutations')
    require('.put(' not in component, 'log list UI must not update logs')

# API boundaries: audit/access are currently only authenticated GET; navigation GET has CanReadAch and POST telemetry remains separately authenticated.
for source, route in ((audit_controller, 'api/audit-logs'), (auth_controller, 'api/auth-logs')):
    require(f'[Route("{route}")]' in source, f'missing controller route {route}')
    require('[Authorize]' in source, f'{route} must remain authenticated')
    require('[HttpGet]' in source, f'{route} must expose GET')
    require('Authorize(Policy' not in source, f'{route} must remain unresolved rather than receiving speculative policy')
    require('[HttpPost]' not in source and '[HttpDelete]' not in source and '[HttpPut]' not in source and '[HttpPatch]' not in source, f'{route} controller must remain read-only')
require('[Route("api/navigation-logs")]' in nav_controller, 'missing navigation logs API route')
require('[HttpGet]' in nav_controller and '[Authorize(Policy = "CanReadAch")]' in nav_controller, 'navigation GET must retain CanReadAch boundary')
post_block = between(nav_controller, '[HttpPost]', 'private string? GetCurrentUserId')
require('Authorize(Policy' not in post_block, 'automatic telemetry POST must not inherit the administrative GET permission')
require('_service.AddAsync' in post_block, 'navigation POST must continue to persist telemetry')

# SPA aligns navigation list with its current GET, without speculatively changing audit/access.
nav_route = between(app_routing, "path: 'navigation-logs'", "path: 'customer-third-parties'")
require('canActivate: [permissionGuard]' in nav_route, 'navigation log SPA must use permissionGuard')
require("permissions: ['CanReadAch']" in nav_route, 'navigation log SPA must mirror current GET capability')
require('roles:' not in nav_route, 'navigation log SPA must not hardcode roles')
audit_route = between(app_routing, "path: 'audit-logs'", "path: 'auth-logs'")
auth_route = between(app_routing, "path: 'auth-logs'", "path: 'navigation-logs'")
require('permissionGuard' not in audit_route, 'audit route must not receive a speculative permission in NAV3.4G')
require('permissionGuard' not in auth_route, 'access route must not receive a speculative permission in NAV3.4G')
require("protege la lectura administrativa de navegación con la misma capacidad del GET API" in route_spec, 'Angular focal route test missing')

# Feature routes remain context-only and do not add conflicting guards.
for feature in (audit_routing, auth_routing, nav_routing):
    require('permissionGuard' not in feature, 'feature route must not duplicate app-level authorization')

# Sensitive fields characterize why authenticated-only cannot be assumed sufficient.
for token in ('EntityName', 'EntityId', 'Action', 'ChangedBy', 'ChangedFields'):
    require(token in audit_dto, f'audit sensitive field missing: {token}')
for token in ('Username', 'Success', 'FailureReason', 'IpAddress', 'UserAgent'):
    require(token in auth_dto, f'access sensitive field missing: {token}')
for token in ('UserId', 'Username', 'Route', 'SessionId', 'DurationMs', 'IpAddress', 'UserAgent'):
    require(token in nav_dto, f'navigation sensitive field missing: {token}')

# Read implementations stay no-tracking; navigation purge is internal and not exposed by the administration screen.
require('.AsNoTracking()' in audit_persistence, 'audit query must remain read-only')
require('.AsNoTracking()' in auth_persistence, 'access query must remain read-only')
require('.AsNoTracking()' in nav_persistence, 'navigation query must remain read-only')
require('PurgeOlderThanAsync' in nav_persistence, 'internal navigation retention operation remains present')
require('PurgeOlderThanAsync' not in nav_controller, 'navigation retention must not become a UI/API action')
require('PurgeOlderThanAsync' not in nav_list_service, 'navigation UI must not expose retention mutation')

# Automatic navigation telemetry remains independent of administrative list read authorization.
require(".post('api/navigation-logs'" in nav_telemetry, 'automatic navigation telemetry POST missing')
require('this.auth.isAuthenticated()' in nav_telemetry, 'automatic telemetry must still require authenticated session')
require('CanReadAch' not in nav_telemetry, 'telemetry client must not require administrative log-read permission')

# No semantically suitable general system-log capability exists yet: do not invent one here.
for concept in ('SystemLogs.Read', 'AuditLogs.Read', 'AuthLogs.Read', 'NavigationLogs.Read'):
    require(concept not in fine, f'NAV3.4G must not invent fine permission {concept}')
    require(concept not in perm_config, f'NAV3.4G must not persist speculative permission {concept}')
require('RolePermission' not in parent_seed, 'NAV3.4G must not add role grants')

# /reports/audit remains a distinct Reportes surface and contract.
require("path: 'audit'" in reports_routing, 'reports audit route must remain')
require("permissions: ['CanReadAch']" in reports_routing, 'reports audit authorization must remain unchanged')
require('api/reports/audit' in reports_api, 'reports audit API contract must remain distinct')
require('api/audit-logs' in audit_service, 'administrative audit API contract must remain distinct')

# Focal C# tests document the partial closure and the no-orphan guarantee for current children.
for test_token in (
    'StructuralParent_AcceptsCurrentChildPermissionFamilies_WithoutChangingChildAuthorization',
    'MenuHandler_CurrentSystemRecordChildrenRemainNestedUnderStructuralParent',
    'Controllers_CharacterizeReadOnlyAdministrativeViews_AndKeepNavigationTelemetrySeparate',
    'NavigationLogSpaRead_RequiresCurrentGetCapability_WhileTelemetryPostRemainsAuthenticatedOnly',
    'AuditAndAccessRemainUnbroadenedBecauseNoExistingSemanticSystemLogReadCapabilityExists',
    'ReportsAuditAndSystemRecordStructureRemainDistinctAndUnchanged'):
    require(test_token in nav34g_test, f'missing focal test {test_token}')

print(f'NAV3.4G static validation passed: {checks} assertions')
