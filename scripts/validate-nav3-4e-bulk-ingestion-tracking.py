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
policy_reg = read('src/Cfa.ACHInterbank.External/DependencyInjectionService.cs')
controller = read('src/Cfa.ACHInterbank.Api/Controllers/BulkIngestionController.cs')
permission_seed = read('src/Cfa.ACHInterbank.Persistence/Security/Seeders/BulkIngestionTrackingPermissionSeeder.cs')
menu_seed = read('src/Cfa.ACHInterbank.Persistence/Security/Seeders/BulkIngestionTrackingMenuAuthorizationSeeder.cs')
role_perm = read('src/Cfa.ACHInterbank.Persistence/Configuration/RolePermissionConfiguration.cs')
nav_seed = read('src/Cfa.ACHInterbank.Persistence/Navigation/Seeders/NavigationInformationArchitectureSeeder.cs')
tx_routes = read('web/ach-interbank-ui/src/app/features/transactions/transactions-routing.module.ts')
app_routes = read('web/ach-interbank-ui/src/app/app-routing.module.ts')
tracking = read('web/ach-interbank-ui/src/app/features/transactions/components/bulk-ingestion-tracking/bulk-ingestion-tracking.component.ts')
tracking_spec = read('web/ach-interbank-ui/src/app/features/transactions/components/bulk-ingestion-tracking/bulk-ingestion-tracking.component.spec.ts')
detail = read('web/ach-interbank-ui/src/app/features/transactions/components/bulk-ingestion-detail/bulk-ingestion-detail.component.ts')
detail_html = read('web/ach-interbank-ui/src/app/features/transactions/components/bulk-ingestion-detail/bulk-ingestion-detail.component.html')
detail_spec = read('web/ach-interbank-ui/src/app/features/transactions/components/bulk-ingestion-detail/bulk-ingestion-detail.component.spec.ts')
spa_service = read('web/ach-interbank-ui/src/app/features/transactions/services/bulk-ingestion-tracking-api.service.ts')
retry_service = read('src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchBulkBatchRetryService.cs')
lifecycle = read('src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/BulkIngestionLifecycleService.cs')
dispatcher = read('src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/BulkIngestionWorkDispatcher.cs')
processor = read('src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchBulkBatchProcessingService.cs')
bulk_tx = read('src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchBulkTransactionService.cs')
create_auth = read('src/Cfa.ACHInterbank.Persistence/Security/Seeders/TransactionsCreateMenuAuthorizationSeeder.cs')
nacha_auth = read('src/Cfa.ACHInterbank.Persistence/Security/Seeders/NachaUploadMenuAuthorizationSeeder.cs')
returns_auth = read('src/Cfa.ACHInterbank.Persistence/Security/Seeders/ReturnsMenuAuthorizationSeeder.cs')
manual_review = read('web/ach-interbank-ui/src/app/features/ach-responses/pages/ach-response-manual-review-page.component.ts')
test_tools = read('src/Cfa.ACHInterbank.Persistence/Security/Seeders/TestToolsMenuAuthorizationSeeder.cs')

# Existing backend fine-grained contract.
for token in (
    'public const string Read = "BulkIngestion.Read"',
    'public const string Upload = "BulkIngestion.Upload"',
    'public const string Retry = "BulkIngestion.Retry"',
    'public const string Cancel = "BulkIngestion.Cancel"'):
    require(token in fine, f'missing fine-grained permission: {token}')
require('BulkIngestion.Upload, BulkIngestion.Read, BulkIngestion.Retry, BulkIngestion.Cancel' in fine,
        'bulk-ingestion permissions must remain in AllPermissions')
for token in (
    'public const string BulkIngestionRead = "P1.BulkIngestionRead"',
    'public const string BulkIngestionUpload = "P1.BulkIngestionUpload"',
    'public const string BulkIngestionRetry = "P1.BulkIngestionRetry"',
    'public const string BulkIngestionCancel = "P1.BulkIngestionCancel"'):
    require(token in p1, f'missing P1 policy constant: {token}')

read_policy = between(policy_reg, 'options.AddPolicy(P1Policies.BulkIngestionRead,', 'options.AddPolicy(P1Policies.BulkIngestionUpload,')
require('FineGrainedPermissions.BulkIngestion.Read' in read_policy, 'read policy must accept BulkIngestion.Read')
require('"CanReadAch"' in read_policy, 'read policy must retain CanReadAch compatibility')
require('CanManageAch' not in read_policy, 'CanManageAch must not substitute for bulk read')
retry_policy = between(policy_reg, 'options.AddPolicy(P1Policies.BulkIngestionRetry,', 'options.AddPolicy(P1Policies.BulkIngestionCancel,')
require('FineGrainedPermissions.BulkIngestion.Retry' in retry_policy, 'retry policy must accept fine retry')
require('"CanManageAch"' in retry_policy, 'retry policy must retain CanManageAch')
require('CanReadAch' not in retry_policy, 'read must not authorize retry')
cancel_policy = between(policy_reg, 'options.AddPolicy(P1Policies.BulkIngestionCancel,', 'options.AddPolicy(P1Policies.CommandCenterRead,')
require('FineGrainedPermissions.BulkIngestion.Cancel' in cancel_policy, 'cancel policy must accept fine cancel')
require('"CanManageAch"' in cancel_policy, 'cancel policy must retain CanManageAch')
require('CanReadAch' not in cancel_policy, 'read must not authorize cancel')

# Controller endpoints remain separated by operation type.
for marker in ('[HttpGet("{batchId:guid}")]', '[HttpGet("{batchId:guid}/items")]', '[HttpGet("{batchId:guid}/summary")]'):
    i = controller.find(marker)
    require(i >= 0, f'missing read endpoint {marker}')
    require('[Authorize(Policy = P1Policies.BulkIngestionRead)]' in controller[i:i+900], f'{marker} must use BulkIngestionRead')
retry_action = controller[controller.find('[HttpPost("{batchId:guid}/retry")]'):controller.find('/// <summary>\n    /// Cancela un lote')]
require('[Authorize(Policy = P1Policies.BulkIngestionRetry)]' in retry_action, 'retry POST must keep BulkIngestionRetry')
cancel_action = controller[controller.find('[HttpPost("{batchId:guid}/cancel")]'):]
require('[Authorize(Policy = P1Policies.BulkIngestionCancel)]' in cancel_action, 'cancel POST must keep BulkIngestionCancel')
upload_action = between(controller, '[HttpPost("upload")]', '/// <summary>\n    /// Consulta el estado consolidado')
require('[Authorize(Policy = P1Policies.BulkIngestionUpload)]' in upload_action, 'upload POST must remain separate and unchanged')

# Catalog materializes only existing tracking-related fine permissions and creates no grants.
for token in ('FineGrainedPermissions.BulkIngestion.Read', 'FineGrainedPermissions.BulkIngestion.Retry', 'FineGrainedPermissions.BulkIngestion.Cancel'):
    require(token in permission_seed, f'permission catalog seeder must materialize {token}')
require('FineGrainedPermissions.BulkIngestion.Upload' not in permission_seed, 'tracking increment must not materialize upload permission')
require('RolePermission' not in permission_seed, 'tracking permission seeder must not grant roles')
require('UserPermission' not in permission_seed, 'tracking permission seeder must not grant users')
for name in ('BulkIngestion.Read', 'BulkIngestion.Retry', 'BulkIngestion.Cancel'):
    require(name not in role_perm, f'NAV3.4E must not add role grant for {name}')

# Menu is a read entry point matching P1.BulkIngestionRead direct alternatives.
require('AuthorizationReconciliationOrder = 105' in menu_seed, 'tracking menu reconciler order must follow NAV3.4D')
require('item.Route == "/transactions/bulk-ingestion/tracking"' in menu_seed, 'menu reconciler must target tracking route only')
require('BulkIngestionTrackingPermissionSeeder.ReadPermissionId' in menu_seed, 'menu must accept fine read')
require('PermissionConfiguration.ReadAchPermissionId' in menu_seed, 'menu must accept legacy read')
require('PermissionConfiguration.ManageAchPermissionId' not in menu_seed, 'menu must not treat manage as read alternative')
require('RetryPermissionId' not in menu_seed and 'CancelPermissionId' not in menu_seed, 'mutable capabilities must not gate menu entry')
for token in ('MenuItemRoles', 'menuItem.Label =', 'menuItem.Route =', 'menuItem.Order =', 'menuItem.IsActive =', 'menuItem.ParentId ='):
    require(token not in menu_seed, f'menu reconciler must not mutate structural/role state: {token}')

# Canonical menu identity remains unchanged.
require('BulkIngestionTrackingRoute' in nav_seed, 'canonical tracking route must remain in navigation seeder')
tracking_nav = between(nav_seed, 'var (bulkTracking, bulkTrackingCreated)', 'await ReconcileExistingAsync(\n            NachaUploadRoute')
for token in ('"Seguimiento de cargas"', 'order: 40', 'icon: "manage_search"', 'exact: true'):
    require(token in tracking_nav, f'tracking menu identity changed: {token}')

# SPA entry and contextual detail use read capability, not management-only.
tracking_route = between(tx_routes, "path: 'bulk-ingestion/tracking'", "path: 'bulk-ingestion/:batchId'")
require('canActivate: [permissionGuard]' in tracking_route, 'tracking route must keep permission guard')
require("permissions: ['BulkIngestion.Read', 'CanReadAch']" in tracking_route, 'tracking route must mirror backend read alternatives')
require('CanManageAch' not in tracking_route, 'tracking entry must not require management')
require('roles:' not in tracking_route, 'tracking child must not hardcode roles')
detail_route = between(tx_routes, "path: 'bulk-ingestion/:batchId'", "path: 'nacha-upload'")
require('canActivate: [permissionGuard]' in detail_route, 'detail route must keep permission guard')
require("permissions: ['BulkIngestion.Read', 'CanReadAch']" in detail_route, 'detail route must mirror backend read alternatives')
require('CanManageAch' not in detail_route, 'detail entry must not require management')
require('roles:' not in detail_route, 'detail child must not hardcode roles')
parent_route = between(app_routes, "path: 'transactions'", "path: 'integraciones'")
require("roles: ['Admin', 'ACH.Operator']" in parent_route, 'shared transactions parent roles must remain untouched')
require("permissions: ['CanManageAch', 'CanReadAch']" in parent_route, 'shared transactions parent permissions must remain untouched')

# Upload and bulk-create stay out of this increment.
upload_route = between(tx_routes, "path: 'bulk-ingestion/upload'", "path: 'bulk-ingestion/tracking'")
require('CanManageAch' in upload_route, 'bulk upload route must retain the legacy management fallback')
require('BulkIngestion.Read' not in upload_route, 'tracking read capability must not leak into upload')
require('CanReadAch' not in upload_route, 'tracking legacy read must not leak into upload')
bulk_create_route = between(tx_routes, "path: 'bulk-create'", "path: 'bulk-ingestion/upload'")
require("permissions: ['CanManageAch']" in bulk_create_route, 'bulk-create route must remain unchanged')
require('/transactions/bulk-create' not in nav_seed, 'bulk-create must remain outside canonical menu')

# Tracking screen is mixed read + retry. Retry is visually gated in cell renderer and fail-closed in method.
for token in ('this.api.getBatch(batchId)', 'this.api.getBatch(id)', 'this.openDetail(params.data!.batchId)', 'this.retryFailed(params.data!.batchId)'):
    require(token in tracking, f'tracking operation missing: {token}')
require("BULK_INGESTION_RETRY_PERMISSIONS = ['BulkIngestion.Retry', 'CanManageAch']" in tracking, 'tracking retry must mirror backend alternatives')
require('this.canRetryBatches && this.canRetry(' in tracking, 'tracking retry control must be visually permission-gated')
require('if (!this.ensureCanRetry())' in tracking, 'tracking retry method must fail closed')
require('No tiene permisos para reintentar lotes de carga.' in tracking, 'tracking denied retry must report authorization failure')
require('CanReadAch' not in between(tracking, 'BULK_INGESTION_RETRY_PERMISSIONS', '@Component'), 'CanReadAch must not authorize retry action')
require('Admin' not in tracking and 'ACH.Operator' not in tracking, 'tracking component must not hardcode role names')
require('fails closed when the user lacks the retry capability' in tracking_spec, 'tracking spec must cover fail-closed retry')

# Contextual detail is read-entry with separately protected retry/cancel mutations.
for token in ('this.api.getSummary(batchId)', 'this.api.getBatchItems(', 'this.api.retry(', 'this.api.cancel('):
    require(token in detail, f'detail operation missing: {token}')
require("BULK_INGESTION_RETRY_PERMISSIONS = ['BulkIngestion.Retry', 'CanManageAch']" in detail, 'detail retry must mirror backend policy')
require("BULK_INGESTION_CANCEL_PERMISSIONS = ['BulkIngestion.Cancel', 'CanManageAch']" in detail, 'detail cancel must mirror backend policy')
require('if (!this.ensureCanRetry())' in detail, 'detail retry method must fail closed')
require('if (!this.ensureCanCancel())' in detail, 'detail cancel method must fail closed')
require('*ngIf="canRetryBatchAction && canRetry()"' in detail_html, 'detail retry controls must be visually gated')
require('*ngIf="canCancelBatchAction && canCancel()"' in detail_html, 'detail cancel control must be visually gated')
require('CanReadAch' not in between(detail, 'BULK_INGESTION_RETRY_PERMISSIONS', '@Component'), 'read must not authorize detail mutations')
require('Admin' not in detail and 'ACH.Operator' not in detail, 'detail component must not hardcode role names')
require('fails closed when the user lacks the retry capability' in detail_spec, 'detail spec must cover denied retry')
require('fails closed when the user lacks the cancel capability' in detail_spec, 'detail spec must cover denied cancel')
require('hides retry and cancel controls when mutable permissions are absent' in detail_spec, 'detail spec must cover visual gating')

# Exact SPA API surface used by tracking/detail.
for token in (
    'transactions/bulk-ingestion/${batchId}`',
    'transactions/bulk-ingestion/${batchId}/items',
    'transactions/bulk-ingestion/${batchId}/summary',
    'transactions/bulk-ingestion/${batchId}/retry',
    'transactions/bulk-ingestion/${batchId}/cancel'):
    require(token in spa_service, f'missing tracking/detail endpoint call: {token}')
require("transactions/bulk-ingestion/upload" in spa_service, 'shared service must retain upload method')

# Retry has real operational effects: persistent reset/attempt + async dispatch + later transaction creation.
for token in (
    'item.Status = BulkIngestionItemStatusEnum.Ready',
    'item.TransactionId = null',
    'TriggerType = BulkIngestionTriggerTypeEnum.Retry',
    'batch.Status = BulkIngestionBatchStatusEnum.Retrying',
    'DispatchProcessingAsync(batchId, attempt.Id, ct)'):
    require(token in retry_service, f'retry behavior changed/missing: {token}')
require('_jobScheduler.EnqueueBatchAsync(batchId, attemptId, ct)' in dispatcher, 'retry must continue dispatching asynchronous processing')
require('_bulkTransactionService.RegisterBulkAsync' in processor, 'async processing must continue registering transactions')
require('_contrapartidaDispatchPersistenceService.EnsurePendingDispatchAsync' in bulk_tx, 'bulk processing may continue preparing later contrapartida dispatch')
require('_cenitCycleQueueService.EnqueueAsync' in bulk_tx, 'bulk processing may continue queueing target-cycle work')

# Cancel remains a persistent state mutation only; it does not physically cancel a scheduler job here.
for token in (
    'batch.Status = BulkIngestionBatchStatusEnum.Cancelled',
    'batch.LastJobMessage = $"Cancelado por {requestedBy}',
    'batch.ProcessingFinishedAtUtc = DateTime.UtcNow',
    'await _context.SaveChangesAsync(ct)'):
    require(token in lifecycle, f'cancel behavior changed/missing: {token}')
for token in ('Unschedule', 'DeleteJob', 'Interrupt'):
    require(token not in lifecycle, f'cancel flow unexpectedly acquired physical scheduler operation: {token}')

# No direct external transport is added to the HTTP retry/cancel request path.
for source_name, source in (('controller', controller), ('retry service', retry_service), ('lifecycle', lifecycle)):
    require('IWscfaachSoapClient' not in source, f'{source_name} must not directly own SOAP client')
    require('IContrapartidaDispatchJobService' not in source, f'{source_name} must not directly invoke live contrapartida job')
    require('IAchOutboundReturnDispatchService' not in source, f'{source_name} must not directly dispatch outbound returns')

# Previously closed models remain represented.
require('TransactionsCreatePermissionSeeder.PermissionId' in create_auth, 'NAV3.4B create menu authorization regressed')
require('NachaUploadPermissionSeeder.PermissionId' in nacha_auth, 'NAV3.4C NACHA upload menu authorization regressed')
require('PermissionConfiguration.ReadAchPermissionId' in returns_auth, 'NAV3.4D returns read-entry authorization regressed')
require('Nacha.Generate' in manual_review and 'CanManageAch' in manual_review, 'NAV3.4A manual-review mutation gate regressed')
require('FineGrainedPermissions.TestTools.Access' in test_tools and 'serviceTests.IsActive = true;' in test_tools, 'test-tools security regressed')

print(f'NAV3.4E static validation passed: {checks} assertions')
