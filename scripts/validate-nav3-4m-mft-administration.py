#!/usr/bin/env python3
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
permission_seed = read('src/Cfa.ACHInterbank.Persistence/Security/Seeders/ConfigurationPermissionSeeder.cs')
menu_seed = read('src/Cfa.ACHInterbank.Persistence/Security/Seeders/MftAdministrationMenuAuthorizationSeeder.cs')
role_grants = read('src/Cfa.ACHInterbank.Persistence/Configuration/RolePermissionConfiguration.cs')
legacy_menu_seed = read('src/Cfa.ACHInterbank.Persistence/Navigation/Seeders/AchColombiaMftAdministrationMenuSeeder.cs')
ia = read('src/Cfa.ACHInterbank.Persistence/Navigation/Seeders/NavigationInformationArchitectureSeeder.cs')
handler = read('src/Cfa.ACHInterbank.Application/Navigation/Queries/GetMenuForCurrentUserHandler.cs')
routing = read('web/ach-interbank-ui/src/app/app-routing.module.ts')
integrations_routes = read('web/ach-interbank-ui/src/app/features/integrations/integrations.routes.ts')
navigation_service = read('web/ach-interbank-ui/src/app/core/services/navigation.service.ts')
navigation_spec = read('web/ach-interbank-ui/src/app/core/services/navigation.service.spec.ts')
component = read('web/ach-interbank-ui/src/app/features/ach-colombia-mft-administration/ach-colombia-mft-administration.component.ts')
template = read('web/ach-interbank-ui/src/app/features/ach-colombia-mft-administration/ach-colombia-mft-administration.component.html')
component_spec = read('web/ach-interbank-ui/src/app/features/ach-colombia-mft-administration/ach-colombia-mft-administration.component.spec.ts')
file_service = read('web/ach-interbank-ui/src/app/core/services/ach-colombia-file-exchange.service.ts')
schedule_service = read('web/ach-interbank-ui/src/app/core/services/ach-colombia-operational-schedules.service.ts')
file_controller = read('src/Cfa.ACHInterbank.Api/Controllers/AchColombiaFileExchangeController.cs')
schedule_controller = read('src/Cfa.ACHInterbank.Api/Controllers/AchColombiaCycleOperationalSchedulesController.cs')
admin_component = read('src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchColombiaManagedMftAdministrationComponent.cs')
credential_component = read('src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchColombiaManagedMftCredentialComponent.cs')
config_provider = read('src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchColombiaManagedMftConfigurationProvider.cs')
connection_tester = read('src/Cfa.ACHInterbank.External/Connections/AchColombiaManagedMftConnectionTester.cs')
schedule_admin = read('src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchColombiaCycleOperationalScheduleAdminService.cs')
scheduler_sync = read('src/Cfa.ACHInterbank.Persistence/ACH/Quartz/SchedulerSyncService.cs')
domain = read('src/Cfa.ACHInterbank.Domain/Models/ACH/AchManagedFileTransfer.cs')
ef_config = read('src/Cfa.ACHInterbank.Persistence/Configuration/AchManagedFileTransferConfiguration.cs')
models = read('src/Cfa.ACHInterbank.Application/ACH/Models/AchManagedFileTransferModels.cs')
test_tools = read('src/Cfa.ACHInterbank.Persistence/Security/Seeders/TestToolsMenuAuthorizationSeeder.cs')
bulk_upload = read('src/Cfa.ACHInterbank.Persistence/Security/Seeders/BulkIngestionUploadMenuAuthorizationSeeder.cs')

# Existing canonical capabilities: use Config.Read / Config.Manage rather than inventing an MFT capability.
require('public static class Config' in fine, 'Config capability group must exist')
require('public const string Read = "Config.Read";' in fine, 'Config.Read must exist')
require('public const string Manage = "Config.Manage";' in fine, 'Config.Manage must exist')
require('Config.Read' in fine and 'Config.Manage' in fine, 'Config capabilities must remain in AllPermissions')
require('public const string ConfigRead = "P1.ConfigRead";' in p1, 'P1.ConfigRead must exist')
require('public const string ConfigManage = "P1.ConfigManage";' in p1, 'P1.ConfigManage must exist')
read_policy = between(policies, 'options.AddPolicy(P1Policies.ConfigRead,', 'options.AddPolicy(P1Policies.ConfigManage,')
manage_policy = between(policies, 'options.AddPolicy(P1Policies.ConfigManage,', 'options.AddPolicy(P1Policies.IntegrationsRead,')
require('FineGrainedPermissions.Config.Read' in read_policy, 'ConfigRead policy must accept Config.Read')
require('"CanReadAch"' in read_policy, 'ConfigRead must retain CanReadAch fallback')
require('FineGrainedPermissions.Config.Manage' not in read_policy, 'ConfigManage must not imply read policy')
require('"CanManageAch"' not in read_policy, 'CanManageAch must not imply read policy')
require('FineGrainedPermissions.Config.Manage' in manage_policy, 'ConfigManage policy must accept Config.Manage')
require('"CanManageAch"' in manage_policy, 'ConfigManage must retain CanManageAch fallback')
require('FineGrainedPermissions.Config.Read' not in manage_policy, 'Config.Read must not authorize management')
require('"CanReadAch"' not in manage_policy, 'CanReadAch must not authorize management')

# Fine capabilities are materialized idempotently, without grants.
for token in ('ConfigurationPermissionSeeder', 'FineGrainedPermissions.Config.Read', 'FineGrainedPermissions.Config.Manage',
              'Consultar configuración de la plataforma', 'Administrar configuración de la plataforma',
              '15d0e235-eb36-4f7d-a922-d935dc1af7e2', '6e9d334b-d77a-4576-814f-8e1f987a421f',
              'FirstOrDefaultAsync(item => item.Name == name)', 'context.Permissions.FindAsync(id)'):
    require(token in permission_seed, f'configuration permission materialization missing: {token}')
require('RolePermission' not in permission_seed, 'config permission seeder must not grant roles')
require('UserPermission' not in permission_seed, 'config permission seeder must not grant users')
require('FineGrainedPermissions.Config.Read' not in role_grants, 'Config.Read must not receive a role grant')
require('FineGrainedPermissions.Config.Manage' not in role_grants, 'Config.Manage must not receive a role grant')

# Menu identity and previous authorization are characterized; structure remains NAV2 canonical.
require('public const string Route = "/administracion/mft-ach";' in legacy_menu_seed, 'legacy MFT menu route missing')
require('PermissionConfiguration.ReadAchPermissionId' in legacy_menu_seed, 'previous MFT menu permission must be CanReadAch')
require('RoleConfiguration.AdminRoleId' in legacy_menu_seed, 'previous MFT menu role must be Admin')
mft_ia = between(ia, 'private async Task ReconcileConnectionsAsync', 'private async Task ReconcileStatusMappingsAsync')
for token in ('"Configuración del intercambio de archivos"', 'FileExchangeConfigurationRoute', 'connections.Id', 'order: 40'):
    require(token in mft_ia, f'canonical MFT menu structure changed: {token}')
require('AuthorizationReconciliationOrder = 113' in menu_seed, 'MFT auth reconciliation order must follow NAV3.4L')
require('AchColombiaMftAdministrationMenuSeeder.Route' in menu_seed, 'MFT authorization seeder must target only canonical MFT route')
require('ConfigurationPermissionSeeder.ReadPermissionId' in menu_seed, 'MFT menu must accept Config.Read')
require('PermissionConfiguration.ReadAchPermissionId' in menu_seed, 'MFT menu must retain CanReadAch fallback')
require('ConfigurationPermissionSeeder.ManagePermissionId' not in menu_seed, 'management capability must not be required merely to enter MFT surface')
require('PermissionConfiguration.ManageAchPermissionId' not in menu_seed, 'CanManageAch must not be an entry requirement')
require('MenuItemRoles.RemoveRange' not in menu_seed, 'direct MFT role must be preserved until parent/navigation is normalized')
for token in ('menuItem.Label =', 'menuItem.Route =', 'menuItem.Order =', 'menuItem.ParentId =', 'menuItem.IsActive ='):
    require(token not in menu_seed, f'MFT auth seeder must not mutate menu structure: {token}')
for excluded in ('/integraciones/contrato', '/integraciones/relacion-informacion', '/integraciones/conexion'):
    require(excluded not in menu_seed, f'MFT auth seeder must not touch excluded surface {excluded}')

# NAV3.4N may remove the structural blocker after NAV3.4M; MFT functional/API/SPA semantics stay unchanged.
require('normalizeIntegrationNavigation' not in navigation_service, 'NAV3.4N must remove the integration-center navigation special case')
require("Centro de integración CFA" not in navigation_service, 'NavigationService must no longer override the canonical Conexiones label')
require('children: []' not in navigation_service, 'NavigationService must no longer flatten canonical Conexiones children')
require("preserva Conexiones y sus hijos exactamente desde el backend" in navigation_spec, 'navigation tests must characterize backend-owned Conexiones hierarchy')
require('var hasRole = !requiredRoles.Any() || requiredRoles.Any(role => userRoles.Contains(role!));' in handler, 'menu handler role side of AND semantics changed')
require('var hasPermission = !requiredPermissions.Any() || requiredPermissions.Any(permission => userPermissions.Contains(permission!));' in handler, 'menu handler permission side of AND semantics changed')
require('if (item.ParentId.HasValue && dtoLookup.TryGetValue(item.ParentId.Value, out var parentDto))' in handler and 'roots.Add(dto);' in handler, 'menu tree orphan-promotion behavior must remain characterized')

# SPA entry is read-capability based; no hardcoded route role gate. Mutations fail closed on Config.Manage / CanManageAch.
mft_route = between(routing, "path: 'administracion/mft-ach'", "path: 'cenit'")
require('canActivate: [permissionGuard]' in mft_route, 'MFT route must use permission guard')
require("permissions: ['Config.Read', 'CanReadAch']" in mft_route, 'MFT route must mirror ConfigRead policy alternatives')
require('roles:' not in mft_route, 'MFT route must not hardcode role names')
require('roleGuard' not in mft_route, 'MFT route must not use role guard directly')
require("this.auth.hasPermission(['Config.Manage', 'CanManageAch'])" in component, 'MFT component management check must mirror ConfigManage')
for token in ('if (!this.model || !this.canManage || this.savingConnection) return;',
              'if (!this.canManage || this.testingConnection) return;',
              'if (!this.model || !this.canManage || this.savingSchedules) return;'):
    require(token in component, f'MFT mutable method must fail closed: {token}')
require('*ngIf="canManage"' in template, 'MFT mutable UI controls must be visually gated')
require('[disabled]="!canManage' in template, 'MFT editable controls must be disabled for read-only principals')
require('hasRole(' not in component and "'ACH.Operator'" not in component and "'Admin'" not in component, 'MFT component must not hardcode roles')
require("hasPermission.and.returnValue(false)" in component_spec, 'component tests must cover denied management capability')
require("['Config.Manage', 'CanManageAch']" in component_spec, 'component tests must assert canonical management alternatives')
require('readonlyComponent.saveConnection();' in component_spec, 'component tests must exercise denied save')
require('readonlyComponent.testConnection();' in component_spec, 'component tests must exercise denied connection test')
require('readonlyComponent.saveSchedules();' in component_spec, 'component tests must exercise denied schedule mutation')

# Surface characterization: exact services/endpoints consumed by this SPA.
for token in ('this.api.administration()', 'this.schedulesApi.administration()', 'this.api.updateAdministration',
              'this.api.setCredential', 'this.api.testConnection()', 'this.schedulesApi.create', 'this.schedulesApi.update'):
    require(token in component, f'expected MFT administration operation missing: {token}')
for forbidden in ('executeOutbound(', 'executeInbound(', 'retry(', 'reprocess(', 'archive(', 'retire(', 'download('):
    require(forbidden not in component, f'MFT admin component must not perform transfer/lifecycle operation: {forbidden}')
for token in ('`${this.base}/administration`', '`${this.base}/administration/credential`', '`${this.base}/administration/test-connection`'):
    require(token in file_service, f'file exchange administration endpoint missing: {token}')
require("private readonly base = 'api/ach-colombia/operational-schedules';" in schedule_service, 'schedule service base changed')
require('`${this.base}/administration`' in schedule_service, 'schedule administration read endpoint missing')
require('this.api.post<AchColombiaOperationalSchedule>(this.base' in schedule_service, 'schedule create endpoint missing')
require('this.api.put<AchColombiaOperationalSchedule>(`${this.base}/${id}`' in schedule_service, 'schedule update endpoint missing')
require('.deactivate(' not in component and 'deactivate(' not in component, 'schedule deactivate endpoint must not be consumed by MFT screen')

# API endpoints actually consumed by the surface have read/manage separation. Other transfer endpoints stay unchanged.
def action_block(source: str, marker: str, next_marker: str) -> str:
    return between(source, marker, next_marker)

file_get = action_block(file_controller, '[HttpGet("administration")]', '[HttpPut("administration")]')
file_put = action_block(file_controller, '[HttpPut("administration")]', '[HttpPut("administration/credential")]')
file_credential = action_block(file_controller, '[HttpPut("administration/credential")]', '[HttpPost("administration/test-connection")]')
# test connection may be final action; slice to final brace using known summary text after body is not needed.
idx = file_controller.find('[HttpPost("administration/test-connection")]')
require(idx >= 0, 'test connection endpoint missing')
file_test = file_controller[idx:]
require('[Authorize(Policy = P1Policies.ConfigRead)]' in file_get, 'MFT administration GET must use ConfigRead')
require('[Authorize(Policy = P1Policies.ConfigManage)]' in file_put, 'MFT administration PUT must use ConfigManage')
require('[Authorize(Policy = P1Policies.ConfigManage)]' in file_credential, 'credential PUT must use ConfigManage')
require('[Authorize(Policy = P1Policies.ConfigManage)]' in file_test, 'test-connection POST must use ConfigManage')
require('[Authorize(Policy = "CanReadAch")]' in file_controller, 'non-MFT-admin transfer read endpoints must remain outside this increment')
require('[Authorize(Policy = "CanManageAch")]' in file_controller, 'non-MFT-admin transfer mutation endpoints must remain outside this increment')

schedule_get = action_block(schedule_controller, '[HttpGet("administration")]', '[HttpPost]')
schedule_post = action_block(schedule_controller, '[HttpPost]', '[HttpPut("{id:int}")]')
schedule_put = action_block(schedule_controller, '[HttpPut("{id:int}")]', '[HttpPatch("{id:int}/deactivate")]')
require('[Authorize(Policy = P1Policies.ConfigRead)]' in schedule_get, 'schedule administration GET must use ConfigRead')
require('[Authorize(Policy = P1Policies.ConfigManage)]' in schedule_post, 'schedule create must use ConfigManage')
require('[Authorize(Policy = P1Policies.ConfigManage)]' in schedule_put, 'schedule update must use ConfigManage')
deactivate_idx = schedule_controller.find('[HttpPatch("{id:int}/deactivate")]')
require(deactivate_idx >= 0, 'schedule deactivate endpoint must remain')
require('[Authorize(Policy = "CanManageAch")]' in schedule_controller[deactivate_idx:], 'unconsumed deactivate endpoint must remain unchanged/out of scope')

# Persisted configuration and sensitive data are characterized without changing storage/crypto.
for token in ('Endpoint', 'Port', 'Principal', 'ProtectedCredential', 'SftpHostKeyFingerprint',
              'OutboundLocation', 'InboundLocation', 'ArchiveLocation', 'ProfileEnabled', 'Protocol'):
    require(token in domain, f'persisted MFT field missing: {token}')
require('builder.Property(x => x.ProtectedCredential).HasMaxLength(8000);' in ef_config, 'protected credential persistence mapping changed')
admin_dto = between(models, 'public sealed record AchManagedMftAdministrationDto(', 'public sealed record UpdateAchManagedMftAdministrationRequest(')
for token in ('Endpoint', 'Port', 'Principal', 'CredentialConfigured', 'CredentialType', 'CredentialUpdatedAtUtc',
              'SftpHostKeyFingerprint', 'OutboundLocation', 'InboundLocation', 'ArchiveLocation'):
    require(token in admin_dto, f'administration DTO must characterize exposed metadata: {token}')
require('ProtectedCredential' not in admin_dto, 'protected/raw credential must never be returned in administration DTO')
require('Password' not in admin_dto and 'Secret' not in admin_dto, 'raw secret field must not be returned in administration DTO')
require('!string.IsNullOrWhiteSpace(x.ProtectedCredential)' in admin_component, 'browser should receive only credential configured state')
require('entity.ProtectedCredential = encryption.Encrypt(value.Secret);' in credential_component, 'credential write must remain encrypted in backend')
require('ProtectedCredential = configured.ProtectedCredential' in config_provider, 'protected credential must remain backend-only effective configuration input')

# GET administration may bootstrap default persistence, but no external connection. Mutations persist config.
require('GetOrCreateConfigurationEntityAsync(ct)' in admin_component, 'administration read must keep default-configuration bootstrap behavior')
require('context.Add(configuration);' in admin_component, 'GET bootstrap persistence behavior must stay characterized')
require('await context.SaveChangesAsync(ct);' in admin_component, 'configuration component must persist updates/bootstrap')
require('_sessionFactory' not in admin_component and '.Connect(' not in admin_component, 'configuration read/write component must not itself connect externally')

# Connection test has immediate external I/O but no file transfer; secret is decrypted only backend-side.
require('_encryption.Decrypt(configuration.ProtectedCredential!)' in connection_tester, 'connection test must decrypt credential only in backend')
require('_sessionFactory.Connect(configuration, password)' in connection_tester, 'SFTP connection test must authenticate externally')
require('session.List(path)' in connection_tester, 'SFTP connection test must list configured remote paths')
for token in ('Directory.Exists', 'Directory.EnumerateFileSystemEntries'):
    require(token in connection_tester, f'ManagedFolder connection test behavior missing: {token}')
for forbidden in ('Upload', 'Download', 'MoveFile', 'DeleteFile'):
    require(forbidden not in connection_tester, f'connection test must not transfer/mutate files: {forbidden}')

# Schedule save persists and synchronizes Quartz future jobs; request does not directly transfer files.
require('await context.SaveChangesAsync(ct);' in schedule_admin, 'schedule admin must persist schedule changes')
require('await scheduleSynchronizer.SynchronizeOutboundAsync(ct);' in schedule_admin, 'schedule admin must resynchronize outbound jobs')
require('class SchedulerSyncService : BackgroundService, IAchColombiaOperationalScheduleSynchronizer' in scheduler_sync, 'schedule synchronizer implementation changed')
require('public async Task SynchronizeOutboundAsync' in scheduler_sync, 'outbound synchronization method missing')
require('AchColombiaOutboundHandlerCode' in scheduler_sync and 'await SynchronizeTaskAsync(taskId.Value, ct);' in scheduler_sync, 'schedule synchronization must reschedule outbound automation')
for forbidden in ('SftpClient', '_sessionFactory.Connect', '.Upload(', '.Download('):
    require(forbidden not in schedule_admin, f'schedule save request must not directly transfer externally: {forbidden}')

# Excluded Conexiones surfaces and previous NAV phases remain untouched by M-specific authorization files.
integrations_parent = between(routing, "path: 'integraciones'", "path: 'navigation'")
require("roles: ['Admin', 'ACH.Operator']" in integrations_parent, 'integration parent SPA role gate must remain untouched')
require("permissions: ['Integrations.Read', 'CanReadAch', 'CanManageAch']" in integrations_parent, 'integration parent SPA permission gate must retain legacy alternatives while adding Integrations.Read')
for token in ("path: 'contrato'", "path: 'relacion-informacion'", "path: 'conexion'"):
    require(token in integrations_routes, f'excluded integrations route missing/regressed: {token}')
for label in ('"Contrato del servicio"', '"Relación de información"', '"Conexión entidad participante"'):
    require(label in ia, f'excluded canonical Conexiones child regressed: {label}')
require('FineGrainedPermissions.TestTools.Access' in test_tools, 'TestTools authorization must remain intact')
require('AuthorizationReconciliationOrder = 112' in bulk_upload, 'NAV3.4L must remain intact')
require('Config.Read' not in handler and 'Config.Manage' not in handler, 'GetMenuForCurrentUserHandler must remain generic')

print(f'NAV3.4M static validation passed: {checks} assertions')
