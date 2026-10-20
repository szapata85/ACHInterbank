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


def slice_between(source: str, start: str, end: str) -> str:
    i = source.find(start)
    require(i >= 0, f'missing start marker: {start}')
    j = source.find(end, i + len(start))
    require(j > i, f'missing end marker after {start}: {end}')
    return source[i:j]

menu_cfg = read('src/Cfa.ACHInterbank.Persistence/Configuration/MenuItemConfiguration.cs')
menu_perm_cfg = read('src/Cfa.ACHInterbank.Persistence/Configuration/MenuItemPermissionConfiguration.cs')
menu_role_cfg = read('src/Cfa.ACHInterbank.Persistence/Configuration/MenuItemRoleConfiguration.cs')
nav_ia = read('src/Cfa.ACHInterbank.Persistence/Navigation/Seeders/NavigationInformationArchitectureSeeder.cs')
menu_auth = read('src/Cfa.ACHInterbank.Persistence/Security/Seeders/NachaUploadMenuAuthorizationSeeder.cs')
permission_seed = read('src/Cfa.ACHInterbank.Persistence/Security/Seeders/NachaUploadPermissionSeeder.cs')
fine = read('src/Cfa.ACHInterbank.Application/Security/FineGrainedPermissions.cs')
policy_reg = read('src/Cfa.ACHInterbank.External/DependencyInjectionService.cs')
role_perm = read('src/Cfa.ACHInterbank.Persistence/Configuration/RolePermissionConfiguration.cs')
app_routes = read('web/ach-interbank-ui/src/app/app-routing.module.ts')
tx_routes = read('web/ach-interbank-ui/src/app/features/transactions/transactions-routing.module.ts')
component = read('web/ach-interbank-ui/src/app/features/transactions/components/nacha-upload/nacha-upload.component.ts')
spa_service = read('web/ach-interbank-ui/src/app/features/transactions/services/nacha-upload.service.ts')
controller = read('src/Cfa.ACHInterbank.Api/Controllers/NachaUploadController.cs')
ingestion = read('src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaIngestionAppService.cs')
post_parse = read('src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaPostParseProcessor.cs')
planner = read('src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaDispatchPlanner.cs')
local_live = read('src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaLocalLivePreparationService.cs')
post_processing = read('src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaPostProcessingPipeline.cs')
create_auth = read('src/Cfa.ACHInterbank.Persistence/Security/Seeders/TransactionsCreateMenuAuthorizationSeeder.cs')
manual_review = read('web/ach-interbank-ui/src/app/features/ach-responses/pages/ach-response-manual-review-page.component.ts')
test_tools = read('src/Cfa.ACHInterbank.Persistence/Security/Seeders/TestToolsMenuAuthorizationSeeder.cs')

# Canonical navigation identity remains unchanged; NAV2 owns label/parent/order.
item = slice_between(menu_cfg, 'Id = NachaUploadId,', 'new MenuItem')
require('Route = "/transactions/nacha-upload"' in item, 'NACHA upload route must remain unchanged')
require('ParentId = TransactionsId' in item, 'baseline NACHA upload parent identity must remain unchanged')
require('Exact = true' in item, 'NACHA upload exact flag must remain unchanged')
require('IsActive = true' in item, 'NACHA upload item must remain active')
nav_upload = slice_between(nav_ia, 'NachaUploadRoute,', 'TransactionsReturnsRoute,')
require('"Cargar archivo NACHA-M"' in nav_upload, 'canonical upload label must remain unchanged')
require('operation.Id' in nav_upload, 'canonical upload parent must remain Operación')
require('order: 50' in nav_upload, 'canonical upload order must remain 50')

# Before NAV3.4C the upload item had no own menu auth; the focused final reconciler adds only upload/manage.
require('NachaUploadId' not in menu_perm_cfg, 'baseline menu permission configuration must remain untouched for NachaUpload')
require('NachaUploadId' not in menu_role_cfg, 'NAV3.4C must not add a direct role-name gate for NachaUpload')
require('AuthorizationReconciliationOrder = 103' in menu_auth, 'Nacha upload authorization must reconcile after NAV3.4B')
require('NachaUploadPermissionSeeder.PermissionId' in menu_auth, 'menu must accept fine-grained Nacha.Upload')
require('PermissionConfiguration.ManageAchPermissionId' in menu_auth, 'menu must retain legacy CanManageAch compatibility')
require('PermissionConfiguration.ReadAchPermissionId' not in menu_auth, 'menu upload authorization must reject CanReadAch')
require('permissionLinks.Where(link => !allowedPermissionIds.Contains(link.PermissionId))' in menu_auth,
        'menu reconciliation must remove disallowed direct permissions only for this item')
require('MenuItemRoles' not in menu_auth and 'menuItem.Label' not in menu_auth and 'menuItem.Order' not in menu_auth and 'menuItem.IsActive' not in menu_auth,
        'menu authorization reconciliation must not alter roles/label/order/active state')

# Nacha.Upload already existed as a backend capability; NAV3.4C only persists it for menu linkage and grants it to nobody.
require('public const string Upload = "Nacha.Upload"' in fine, 'Nacha.Upload must already exist as a fine-grained permission')
require('Nacha.Read, Nacha.Upload' in fine, 'Nacha.Upload must remain in AllPermissions')
require('FineGrainedPermissions.Nacha.Upload' in permission_seed, 'catalog seeder must reuse existing Nacha.Upload')
require('RolePermission' not in permission_seed, 'permission catalog seeder must not create role grants')
require('UserPermission' not in permission_seed, 'permission catalog seeder must not create user grants')
require('Nacha.Upload' not in role_perm, 'NAV3.4C must not add a RolePermission grant for Nacha.Upload')

# P1.NachaUpload is the canonical mutable capability: fine upload OR legacy management, never read.
p1_upload = slice_between(policy_reg, 'options.AddPolicy(P1Policies.NachaUpload,', 'options.AddPolicy(P1Policies.NachaGenerate,')
require('FineGrainedPermissions.Nacha.Upload' in p1_upload, 'P1.NachaUpload must accept Nacha.Upload')
require('"CanManageAch"' in p1_upload, 'P1.NachaUpload must retain CanManageAch compatibility')
require('CanReadAch' not in p1_upload, 'P1.NachaUpload must reject CanReadAch')
p1_read = slice_between(policy_reg, 'options.AddPolicy(P1Policies.NachaRead,', 'options.AddPolicy(P1Policies.NachaUpload,')
require('FineGrainedPermissions.Nacha.Read' in p1_read and '"CanReadAch"' in p1_read,
        'NACHA read policy must remain fine-read OR CanReadAch')

# SPA child mirrors backend alternatives. Shared parent remains legacy and cannot substitute for child upload capability.
upload_route = slice_between(tx_routes, "path: 'nacha-upload'", "path: 'cycle-configs'")
require('canActivate: [permissionGuard]' in upload_route, 'NACHA upload child route must keep permissionGuard')
require("permissions: ['Nacha.Upload', 'CanManageAch']" in upload_route, 'NACHA upload child route must mirror P1.NachaUpload')
require('CanReadAch' not in upload_route, 'NACHA upload child route must reject CanReadAch')
require('roleGuard' not in upload_route and 'roles:' not in upload_route, 'NACHA upload child route must not add hardcoded roles')
parent_route = slice_between(app_routes, "path: 'transactions'", "path: 'integraciones'")
require("roles: ['Admin', 'ACH.Operator']" in parent_route, 'shared transactions parent roles must remain untouched')
require("permissions: ['CanManageAch', 'CanReadAch']" in parent_route, 'shared transactions parent legacy permission prerequisite must remain untouched')
create_route = slice_between(tx_routes, "path: 'create'", "path: 'bulk-create'")
require("permissions: ['Transactions.Create', 'CanManageAch']" in create_route, 'NAV3.4B create authorization must remain intact')

# UI performs validation, mutable upload/reprocess, and read-only record listing.
for token in (
    'classifyNachaUploadFile(file.name)',
    'this.submitUpload(false)',
    'this.submitUpload(true, this.lastUploadResult.ingestionId)',
    '.upload(file, clearingHouseId, { forceReprocess, parentIngestionId })',
    '.listRecords(filters)'):
    require(token in component, f'NACHA upload component missing expected flow operation: {token}')
require("this.http.post(this.api.resolveUrl('NachaUpload/upload'), form)" in spa_service, 'SPA service must POST NACHA upload')
require("this.http.get<NachaUploadRecord[]>(this.api.resolveUrl('NachaUpload/records')" in spa_service, 'SPA service must GET upload records')
require("form.append('forceReprocess', 'true')" in spa_service, 'reprocess option must remain explicit multipart input')

# API keeps the mutable upload protected by P1.NachaUpload and the records query by P1.NachaRead.
upload_action = slice_between(controller, '[EndpointSummary("Cargar archivo NACHA-M para ingestión")]', '[EndpointSummary("Consultar archivos NACHA-M cargados")')
require('[HttpPost("upload")]' in upload_action, 'upload action must remain POST')
require('[Authorize(Policy = P1Policies.NachaUpload)]' in upload_action, 'upload action must require P1.NachaUpload')
require('_ingestionService.IngestAsync' in upload_action, 'upload action must delegate to ingestion service')
require('AllowedExtensions.Contains(extension)' in upload_action, 'controller format/extension validation must remain')
require('AllowedContentTypes.Contains(file.ContentType)' in upload_action, 'controller MIME validation must remain')
records_action = controller[controller.find('[HttpGet("records")]'):]
require('[Authorize(Policy = P1Policies.NachaRead)]' in records_action, 'records GET must remain read-protected')
require('AsNoTracking()' in records_action, 'records GET must remain read-only query')

# Functional behavior is unchanged: persist ingestion + parsed graph, apply post-parse logic, and plan durable async dispatch work.
for token in ('IncomingNachaFileIngestion', 'IncomingNachaFileProcessingResult', 'IncomingNachaProcessingEvent', 'SaveChangesAsync'):
    require(token in ingestion, f'ingestion must retain persistent audit/state behavior: {token}')
require('ParseAndSaveDetailedAsync' in ingestion, 'ingestion must parse and persist NACHA graph')
require('_postParseProcessor.ProcessAsync' in ingestion, 'ingestion must run existing post-parse processing')
require('_dispatchPlanner.PlanForIngestionAsync' in post_parse, 'post-parse must keep planning async dispatch')
require('_context.IncomingNachaDispatchQueue.Add' in planner, 'planner must keep writing durable dispatch queue rows')

# The upload HTTP path does not directly transmit externally; later pipeline retains its separate guarded live integration capability.
for source_name, source in (('controller', controller), ('ingestion', ingestion), ('planner', planner)):
    require('IWscfaachSoapClient' not in source, f'{source_name} must not directly own the SOAP client')
    require('IContrapartidaDispatchJobService' not in source, f'{source_name} must not directly own the contrapartida live job')
require('IWscfaachSoapClient' in post_processing, 'separate post-processing pipeline must retain existing SOAP integration capability')
require('_contrapartidaDispatchJobService.ProcessCycleAsync' in post_processing, 'separate post-processing pipeline must retain delegated later processing')

# Controlled local-live test preparation remains development-only behind all existing explicit switches.
require('_environment.IsDevelopment()' in local_live, 'local-live preparation must remain Development-only')
require('RUN_LOCAL_SOAP_PROC_TRANSACCIONES_E2E' in local_live, 'local-live preparation must retain RUN_LOCAL_SOAP_PROC_TRANSACCIONES_E2E')
require('ALLOW_LOCAL_MONETARY_SOAP_E2E' in local_live, 'local-live preparation must retain ALLOW_LOCAL_MONETARY_SOAP_E2E')
require('ProcTransacciones:Mode' in local_live, 'local-live preparation must retain ProcTransacciones mode guard')
require('new AchTransaction' in local_live, 'local-live preparation remains the explicitly guarded path that can create a local transaction')

# No regression in already closed security surfaces.
require('TransactionsCreatePermissionSeeder.PermissionId' in create_auth and 'PermissionConfiguration.ReadAchPermissionId' not in create_auth,
        'NAV3.4B create menu authorization must remain closed')
require('Nacha.Generate' in manual_review and 'CanManageAch' in manual_review,
        'NAV3.4A manual-review mutable-action gate must remain intact')
require('FineGrainedPermissions.TestTools.Access' in test_tools and 'serviceTests.IsActive = true;' in test_tools,
        'test-tools security and active service-tests state must remain intact')

print(f'NAV3.4C static validation passed: {checks} assertions')
