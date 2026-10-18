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
permission_seed = read('src/Cfa.ACHInterbank.Persistence/Security/Seeders/BulkIngestionUploadPermissionSeeder.cs')
menu_seed = read('src/Cfa.ACHInterbank.Persistence/Security/Seeders/BulkIngestionUploadMenuAuthorizationSeeder.cs')
role_grants = read('src/Cfa.ACHInterbank.Persistence/Configuration/RolePermissionConfiguration.cs')
nav_seed = read('src/Cfa.ACHInterbank.Persistence/Navigation/Seeders/NavigationInformationArchitectureSeeder.cs')
routing = read('web/ach-interbank-ui/src/app/features/transactions/transactions-routing.module.ts')
app_routing = read('web/ach-interbank-ui/src/app/app-routing.module.ts')
component = read('web/ach-interbank-ui/src/app/features/transactions/components/bulk-ingestion-upload/bulk-ingestion-upload.component.ts')
template = read('web/ach-interbank-ui/src/app/features/transactions/components/bulk-ingestion-upload/bulk-ingestion-upload.component.html')
component_spec = read('web/ach-interbank-ui/src/app/features/transactions/components/bulk-ingestion-upload/bulk-ingestion-upload.component.spec.ts')
spa_service = read('web/ach-interbank-ui/src/app/features/transactions/services/bulk-ingestion-tracking-api.service.ts')
controller = read('src/Cfa.ACHInterbank.Api/Controllers/BulkIngestionController.cs')
ingestion = read('src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchBulkFileIngestionService.cs')
dispatcher = read('src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/BulkIngestionWorkDispatcher.cs')
scheduler = read('src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchBulkJobScheduler.cs')
job = read('src/Cfa.ACHInterbank.Persistence/ACH/Quartz/Jobs/Implementation/ProcessBulkIngestionBatchJob.cs')
processor = read('src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchBulkBatchProcessingService.cs')
bulk_tx = read('src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchBulkTransactionService.cs')
tracking_seed = read('src/Cfa.ACHInterbank.Persistence/Security/Seeders/BulkIngestionTrackingMenuAuthorizationSeeder.cs')
nacha_seed = read('src/Cfa.ACHInterbank.Persistence/Security/Seeders/NachaUploadMenuAuthorizationSeeder.cs')
ror_seed = read('src/Cfa.ACHInterbank.Persistence/Security/Seeders/ReturnOfReturnMenuAuthorizationSeeder.cs')
test_tools = read('src/Cfa.ACHInterbank.Persistence/Security/Seeders/TestToolsMenuAuthorizationSeeder.cs')
handler = read('src/Cfa.ACHInterbank.Application/Navigation/Queries/GetMenuForCurrentUserHandler.cs')

# Canonical capability existed before NAV3.4L and is materialized without grants.
require('public const string Upload = "BulkIngestion.Upload";' in fine, 'BulkIngestion.Upload capability must exist')
require('BulkIngestion.Upload, BulkIngestion.Read, BulkIngestion.Retry, BulkIngestion.Cancel' in fine, 'BulkIngestion.Upload must remain in AllPermissions')
require('public const string BulkIngestionUpload = "P1.BulkIngestionUpload";' in p1, 'P1.BulkIngestionUpload must exist')
upload_policy = between(policies, 'options.AddPolicy(P1Policies.BulkIngestionUpload,', 'options.AddPolicy(P1Policies.BulkIngestionRetry,')
require('FineGrainedPermissions.BulkIngestion.Upload' in upload_policy, 'upload policy must accept fine upload capability')
require('"CanManageAch"' in upload_policy, 'upload policy must retain legacy CanManageAch fallback')
require('"CanReadAch"' not in upload_policy, 'read permission must not authorize upload')
require('FineGrainedPermissions.BulkIngestion.Read' not in upload_policy, 'BulkIngestion.Read must not authorize upload')
require('BulkIngestionUploadPermissionSeeder' in permission_seed, 'upload permission seeder must exist')
require('FineGrainedPermissions.BulkIngestion.Upload' in permission_seed, 'seeder must materialize existing upload capability')
require('Cargar transacciones desde archivo' in permission_seed, 'permission description must be functional Spanish')
require('e8194742-09ca-4148-9567-75bb24923f94' in permission_seed, 'upload permission id must be stable')
require('RolePermission' not in permission_seed, 'permission seeder must not create role grants')
require('UserPermission' not in permission_seed, 'permission seeder must not create direct user grants')
require('FineGrainedPermissions.BulkIngestion.Upload' not in role_grants, 'NAV3.4L must not add upload role grants')

# Canonical MenuItem identity remains unchanged and direct permissions mirror backend policy.
nav_upload = between(nav_seed, 'var (bulkUpload, bulkUploadCreated)', 'var (bulkTracking, bulkTrackingCreated)')
for token in ('BulkIngestionUploadRoute', '"Cargar transacciones desde archivo"', 'order: 30', 'icon: "upload_file"', 'exact: true'):
    require(token in nav_upload, f'upload menu identity changed: {token}')
require('AuthorizationReconciliationOrder = 112' in menu_seed, 'upload menu reconciler must run after NAV3.4K')
require('item.Route == "/transactions/bulk-ingestion/upload"' in menu_seed, 'menu reconciler must target upload only')
require('BulkIngestionUploadPermissionSeeder.UploadPermissionId' in menu_seed, 'menu must accept fine upload')
require('PermissionConfiguration.ManageAchPermissionId' in menu_seed, 'menu must retain legacy management fallback')
require('PermissionConfiguration.ReadAchPermissionId' not in menu_seed, 'menu must not accept legacy read')
require('BulkIngestionTrackingPermissionSeeder' not in menu_seed, 'tracking capabilities must not leak into upload menu')
require('MenuItemRoles.RemoveRange' not in menu_seed, 'NAV3.4L must preserve existing child role links while shared parent stays legacy-gated')
for token in ('menuItem.Label =', 'menuItem.Route =', 'menuItem.Order =', 'menuItem.IsActive =', 'menuItem.ParentId ='):
    require(token not in menu_seed, f'authorization seeder must not mutate menu structure: {token}')

# SPA: model A. Local preparation only; route and upload action require upload/manage.
upload_route = between(routing, "path: 'bulk-ingestion/upload'", "path: 'bulk-ingestion/tracking'")
require('canActivate: [permissionGuard]' in upload_route, 'upload route must keep permission guard')
require("permissions: ['BulkIngestion.Upload', 'CanManageAch']" in upload_route, 'upload route must mirror backend upload alternatives')
require('CanReadAch' not in upload_route, 'CanReadAch must not authorize upload route')
require('BulkIngestion.Read' not in upload_route, 'BulkIngestion.Read must not authorize upload route')
require('roles:' not in upload_route, 'child route must not hardcode role names')
parent_route = between(app_routing, "path: 'transactions'", "path: 'integraciones'")
require("roles: ['Admin', 'ACH.Operator']" in parent_route, 'shared transactions parent role gate must remain untouched')
require("permissions: ['CanManageAch', 'CanReadAch']" in parent_route, 'shared transactions parent permission gate must remain untouched')
require("BULK_INGESTION_UPLOAD_PERMISSIONS = ['BulkIngestion.Upload', 'CanManageAch']" in component, 'component action must use backend-equivalent upload alternatives')
require('get canUploadBatch(): boolean' in component, 'component must expose visual upload gate')
require('if (!this.ensureCanUpload())' in component, 'upload method must fail closed')
require('No tiene permisos para cargar transacciones desde archivo.' in component, 'denied upload must report authorization failure')
require('*ngIf="canUploadBatch"' in template, 'upload action must be visually gated')
require('*ngIf="!canUploadBatch"' in template, 'denied state must be visible')
require('hasRole' not in component and 'Admin' not in component and 'ACH.Operator' not in component, 'component must not use role-name checks')
require('fail closed and hide upload action without BulkIngestion.Upload or CanManageAch' in component_spec, 'component test must cover fail-closed upload')
require("hasPermission.and.returnValue(false)" in component_spec, 'component test must simulate denied upload permission')

# Functional surface: local preparation/validation then exactly one upload mutation from this component.
for token in ('selectedFile = signal<File | null>', "acceptedFormats = '.json,.csv,.xlsx,.xls'", 'const maxSizeBytes = 20 * 1024 * 1024', 'metadataForm = this.fb.group'):
    require(token in component, f'local preparation behavior missing: {token}')
require('this.api.upload(file' in component, 'component must invoke upload POST')
for forbidden in ('this.api.getBatch(', 'this.api.getBatchItems(', 'this.api.getSummary(', 'this.api.retry(', 'this.api.cancel('):
    require(forbidden not in component, f'upload component must not consume tracking mutation/read API: {forbidden}')
require("this.api.post<BulkFileUploadResponse>('transactions/bulk-ingestion/upload'" in spa_service, 'shared service must retain upload POST')

# API upload is one mutation protected by P1.BulkIngestionUpload; read/retry/cancel remain separate.
require('[HttpPost("upload")]' in controller, 'upload endpoint must remain POST')
require('[Authorize(Policy = P1Policies.BulkIngestionUpload)]' in controller, 'upload endpoint must remain under P1.BulkIngestionUpload')
require('[Authorize(Policy = P1Policies.BulkIngestionRead)]' in controller, 'tracking read policy must remain')
require('[Authorize(Policy = P1Policies.BulkIngestionRetry)]' in controller, 'retry policy must remain')
require('[Authorize(Policy = P1Policies.BulkIngestionCancel)]' in controller, 'cancel policy must remain')
require('RequestSizeLimit(MaxUploadSizeBytes)' in controller, 'server upload size limit must remain')

# Same-request persistence and async follow-up are characterized, not changed.
for token in ('ResolveFileType', 'ComputeSha256Async', 'parser.ParseAsync', '_structuralValidator.Validate', 'batch.Items.Add(new BulkIngestionItem', '_context.BulkIngestionBatches.Add(batch)', 'await _context.SaveChangesAsync(ct)'):
    require(token in ingestion, f'upload parse/persistence behavior missing: {token}')
require('new BulkIngestionAttempt' in ingestion, 'upload must create initial processing attempt for queued valid items')
require('DispatchProcessingAsync(batch.Id, attempt.Id, ct)' in ingestion, 'upload must dispatch async processing after persistence')
require('_jobScheduler.EnqueueBatchAsync(batchId, attemptId, ct)' in dispatcher, 'dispatcher must delegate to scheduler')
require('JobBuilder.Create<ProcessBulkIngestionBatchJob>()' in scheduler, 'Quartz job creation must remain')
require('.StartNow()' in scheduler, 'Quartz job must remain immediate async trigger')
require('_processingService.ProcessBatchAsync' in job, 'Quartz job must invoke batch processor')
require('_bulkTransactionService.RegisterBulkAsync' in processor, 'async processor must register ACH transactions')
require('_transactionPersister.PersistAsync' in bulk_tx, 'bulk transaction processing must persist ACH transactions')
require('_contrapartidaDispatchPersistenceService.EnsurePendingDispatchAsync' in bulk_tx, 'bulk processing must continue preparing pending contrapartida dispatch')
require('_cenitCycleQueueService.EnqueueAsync' in bulk_tx, 'bulk processing may continue queueing target-cycle work')
for source_name, source in (('controller', controller), ('ingestion', ingestion), ('dispatcher', dispatcher), ('scheduler', scheduler), ('job', job), ('processor', processor), ('bulk transaction service', bulk_tx)):
    for forbidden in ('IWscfaachSoapClient', 'SftpClient', 'HttpClient'):
        require(forbidden not in source, f'{source_name} must not directly transmit externally via {forbidden}')

# NAV3.4E tracking and NAV3.4C NACHA upload stay intact.
tracking_route = between(routing, "path: 'bulk-ingestion/tracking'", "path: 'bulk-ingestion/:batchId'")
require("permissions: ['BulkIngestion.Read', 'CanReadAch']" in tracking_route, 'tracking read route regressed')
detail_route = between(routing, "path: 'bulk-ingestion/:batchId'", "path: 'nacha-upload'")
require("permissions: ['BulkIngestion.Read', 'CanReadAch']" in detail_route, 'tracking detail route regressed')
require('BulkIngestionTrackingPermissionSeeder.ReadPermissionId' in tracking_seed, 'tracking menu authorization regressed')
nacha_route = between(routing, "path: 'nacha-upload'", "path: 'cycle-configs'")
require("permissions: ['Nacha.Upload', 'CanManageAch']" in nacha_route, 'NACHA upload route regressed')
require('NachaUploadPermissionSeeder.PermissionId' in nacha_seed, 'NACHA upload menu authorization regressed')
require('ReturnsReadPermissionSeeder.ReadPermissionId' in ror_seed, 'NAV3.4K ROR authorization regressed')
require('FineGrainedPermissions.TestTools.Access' in test_tools, 'TestTools.Access authorization regressed')

# Navigation handler remains untouched in behavior; no NAV3.4L-specific branching should be introduced.
require('BulkIngestion.Upload' not in handler, 'GetMenuForCurrentUserHandler must remain generic')

print(f'NAV3.4L static validation passed: {checks} assertions')
