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
menu_auth = read('src/Cfa.ACHInterbank.Persistence/Security/Seeders/ReturnsMenuAuthorizationSeeder.cs')
permission_seed = read('src/Cfa.ACHInterbank.Persistence/Security/Seeders/ReturnsPermissionSeeder.cs')
fine = read('src/Cfa.ACHInterbank.Application/Security/FineGrainedPermissions.cs')
p0 = read('src/Cfa.ACHInterbank.Application/Security/P0Policies.cs')
policy_reg = read('src/Cfa.ACHInterbank.External/DependencyInjectionService.cs')
role_perm = read('src/Cfa.ACHInterbank.Persistence/Configuration/RolePermissionConfiguration.cs')
app_routes = read('web/ach-interbank-ui/src/app/app-routing.module.ts')
tx_routes = read('web/ach-interbank-ui/src/app/features/transactions/transactions-routing.module.ts')
component = read('web/ach-interbank-ui/src/app/features/transactions/components/ach-returns-management/ach-returns-management.component.ts')
template = read('web/ach-interbank-ui/src/app/features/transactions/components/ach-returns-management/ach-returns-management.component.html')
spa_service = read('web/ach-interbank-ui/src/app/features/transactions/services/ach-returns-api.service.ts')
reason_service = read('web/ach-interbank-ui/src/app/features/transactions/services/return-reasons-api.service.ts')
cycles_service = read('web/ach-interbank-ui/src/app/core/services/ach-cycles-api.service.ts')
controller = read('src/Cfa.ACHInterbank.Api/Controllers/AchReturnsController.cs')
reason_controller = read('src/Cfa.ACHInterbank.Api/Controllers/ReturnReasonsController.cs')
cycles_controller = read('src/Cfa.ACHInterbank.Api/Controllers/AchCyclesController.cs')
returns_service = read('src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchReturnsService.cs')
artifact = read('src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchReturnArtifactGenerationComponent.cs')
transitions = read('src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchReturnStateTransitionComponent.cs')
audit = read('src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchReturnAuditMonitoringComponent.cs')
ror_controller = read('src/Cfa.ACHInterbank.Api/Controllers/AchReturnOfReturnController.cs')
create_auth = read('src/Cfa.ACHInterbank.Persistence/Security/Seeders/TransactionsCreateMenuAuthorizationSeeder.cs')
upload_auth = read('src/Cfa.ACHInterbank.Persistence/Security/Seeders/NachaUploadMenuAuthorizationSeeder.cs')
manual_review = read('web/ach-interbank-ui/src/app/features/ach-responses/pages/ach-response-manual-review-page.component.ts')
test_tools = read('src/Cfa.ACHInterbank.Persistence/Security/Seeders/TestToolsMenuAuthorizationSeeder.cs')

# Canonical navigation identity remains NAV2-owned and unchanged.
item = slice_between(menu_cfg, 'Id = TransactionsReturnsId,', 'new MenuItem')
require('Route = "/transactions/returns"' in item, 'returns route must remain unchanged')
require('ParentId = TransactionsId' in item, 'baseline returns parent identity must remain unchanged')
require('Exact = true' in item, 'returns exact flag must remain unchanged')
require('IsActive = true' in item, 'returns item must remain active')
nav_returns = slice_between(nav_ia, 'TransactionsReturnsRoute,', 'ReturnOfReturnRoute,')
require('"Devoluciones"' in nav_returns, 'canonical returns label must remain unchanged')
require('operation.Id' in nav_returns, 'canonical returns parent must remain Operación')
require('order: 60' in nav_returns, 'canonical returns order must remain 60')

# Baseline direct configurations remain untouched; focused reconciler controls only this item's read entry permission.
require('TransactionsReturnsId' not in menu_perm_cfg, 'static menu permission configuration must remain untouched for returns')
require('TransactionsReturnsId' not in menu_role_cfg, 'NAV3.4D must not add a direct role-name gate for returns')
require('AuthorizationReconciliationOrder = 104' in menu_auth, 'returns authorization must reconcile after NAV3.4C')
require('PermissionConfiguration.ReadAchPermissionId' in menu_auth, 'returns menu must require CanReadAch for the complete read surface')
require('PermissionConfiguration.ManageAchPermissionId' not in menu_auth, 'returns menu must not require management for read entry')
require('ReturnsPermissionSeeder.GenerateFilePermissionId' not in menu_auth, 'mutable generate permission must not hide the mixed read surface')
require('permissionLinks.Where(link => link.PermissionId != PermissionConfiguration.ReadAchPermissionId)' in menu_auth,
        'menu reconciliation must remove disallowed direct permissions only for this item')
require('MenuItemRoles' not in menu_auth and 'menuItem.Label' not in menu_auth and 'menuItem.Order' not in menu_auth and 'menuItem.IsActive' not in menu_auth,
        'menu authorization reconciler must not alter roles/label/order/active state')

# Fine-grained mutable permission was already declared; NAV3.4D only materializes it, without grants.
require('public const string Read = "Returns.Read"' in fine, 'Returns.Read must already exist')
require('public const string GenerateFile = "Returns.GenerateFile"' in fine, 'Returns.GenerateFile must already exist')
require('Returns.Read, Returns.GenerateFile' in fine, 'returns fine permissions must remain in AllPermissions')
require('FineGrainedPermissions.Returns.GenerateFile' in permission_seed, 'catalog seeder must reuse existing Returns.GenerateFile')
require('FineGrainedPermissions.Returns.Read' not in permission_seed, 'NAV3.4D need not materialize fine read because aggregate screen reads still require CanReadAch')
require('RolePermission' not in permission_seed, 'permission catalog seeder must not create role grants')
require('UserPermission' not in permission_seed, 'permission catalog seeder must not create user grants')
require('Returns.GenerateFile' not in role_perm, 'NAV3.4D must not add a RolePermission grant for Returns.GenerateFile')

# Backend is the source of truth: read and mutable generation are distinct capabilities.
require('public const string ReturnsRead = "P0.ReturnsRead"' in p0, 'P0.ReturnsRead must remain declared')
require('public const string ReturnsGenerateFile = "P0.ReturnsGenerateFile"' in p0, 'P0.ReturnsGenerateFile must remain declared')
p0_read = slice_between(policy_reg, 'options.AddPolicy(P0Policies.ReturnsRead,', 'options.AddPolicy(P0Policies.ReturnsGenerateFile,')
require('FineGrainedPermissions.Returns.Read' in p0_read, 'P0.ReturnsRead must accept fine Returns.Read')
require('"CanReadAch"' in p0_read, 'P0.ReturnsRead must retain CanReadAch')
require('CanManageAch' not in p0_read, 'P0.ReturnsRead must not treat management as a read substitute')
p0_generate = slice_between(policy_reg, 'options.AddPolicy(P0Policies.ReturnsGenerateFile,', 'options.AddPolicy(P0Policies.ClaimsRead,')
require('FineGrainedPermissions.Returns.GenerateFile' in p0_generate, 'P0.ReturnsGenerateFile must accept fine Returns.GenerateFile')
require('"CanManageAch"' in p0_generate, 'P0.ReturnsGenerateFile must retain CanManageAch')
require('CanReadAch' not in p0_generate, 'P0.ReturnsGenerateFile must reject CanReadAch')

# The complete screen remains a read-entry surface. It consumes two auxiliary APIs that still require CanReadAch.
returns_route = slice_between(tx_routes, "path: 'returns'", "path: 'returns-ror'")
require('canActivate: [permissionGuard]' in returns_route, 'returns child route must keep permissionGuard')
require("permissions: ['CanReadAch']" in returns_route, 'returns child route must require aggregate screen read capability CanReadAch')
require('CanManageAch' not in returns_route, 'returns child route must not require management just to read')
require('roles:' not in returns_route, 'returns child route must not add a hardcoded role-name gate')
parent_route = slice_between(app_routes, "path: 'transactions'", "path: 'integraciones'")
require("roles: ['Admin', 'ACH.Operator']" in parent_route, 'shared transactions parent roles must remain untouched')
require("permissions: ['CanManageAch', 'CanReadAch']" in parent_route, 'shared transactions parent legacy permissions must remain untouched')
require('[Authorize(Policy = "CanReadAch")]' in reason_controller, 'return-reasons read API must remain CanReadAch-protected')
require('[Authorize(Policy = "CanReadAch")]' in cycles_controller, 'cycle read API must remain CanReadAch-protected')

# SPA operations are mixed: read/preparation stays available; generation is visually and programmatically fail-closed.
for token in (
    'this.cyclesApi.search({ page: 1, pageSize: 100 })',
    'this.returnReasonsApi.getForReturns()',
    'this.returnsApi.getTransactionsByCycle(cycleId)',
    'openDetail()',
    'openReasonDialog()',
    'generateFile(reasonCode: string'):
    require(token in component, f'returns component missing expected operation: {token}')
require("RETURNS_GENERATE_PERMISSIONS = ['Returns.GenerateFile', 'CanManageAch']" in component,
        'mutable SPA action must mirror P0.ReturnsGenerateFile alternatives')
require('get canGenerateReturns(): boolean' in component, 'component must expose mutable-action capability')
require(component.count('if (!this.ensureCanGenerateReturns())') >= 2,
        'both reason-dialog and generate methods must fail closed')
require("No tiene permisos para generar devoluciones." in component, 'denied mutable action must fail closed with notification')
require('*ngIf="canGenerateReturns"' in template, 'generate control must be visually gated')
require('Ver detalle' in template, 'read/detail control must remain available independently of generation')
require('CanReadAch' not in component[component.find('RETURNS_GENERATE_PERMISSIONS'):component.find('interface ReturnReasonDialogData')],
        'CanReadAch must not be a mutable-action alternative')
require('Admin' not in component and 'ACH.Operator' not in component, 'component must not hardcode role names')

# Actual endpoints used by /transactions/returns: read catalogs/list plus one POST generation endpoint.
require('ach-returns/cycles/${encodeURIComponent(cycleId)}/transactions' in spa_service, 'SPA must GET return candidates by cycle')
require("postBlob('ach-returns/generate-file'" in spa_service, 'SPA must POST generate-file')
require("private readonly basePath = 'return-reasons'" in reason_service and 'return this.api.get<ReturnReason[]>(this.basePath' in reason_service, 'SPA must GET return reasons')
require("private readonly basePath = 'api/ach-cycles'" in cycles_service and 'return this.api.get<PagedAchCycleResponse | AchCycleSummary[]>(this.basePath' in cycles_service, 'SPA must GET cycle catalog')
get_action = slice_between(controller, '[HttpGet("cycles/{cycleId}/transactions")]', '/// <summary>\n    /// Genera el archivo')
require('[Authorize(Policy = P0Policies.ReturnsRead)]' in get_action, 'candidate GET must require P0.ReturnsRead')
require('GetTransactionsByCycleAsync' in get_action, 'candidate GET must remain a query')
post_action = controller[controller.find('[HttpPost("generate-file")]'):]
require('[Authorize(Policy = P0Policies.ReturnsGenerateFile)]' in post_action, 'generate POST must require P0.ReturnsGenerateFile')
require('GenerateReturnsFileAsync' in post_action, 'generate POST must delegate to existing mutable service')

# Functional behavior remains unchanged: persist generated return rows, generate NACHA-M artifact, audit and change state.
require('_context.Set<AchReturnGenerated>().AddRange' in returns_service, 'generation must persist AchReturnGenerated rows')
require('_artifactGeneration.GenerateAsync' in returns_service, 'generation must build the existing return artifact')
require('_stateTransitions.ApplyAsync' in returns_service, 'generation must apply the existing state transition')
require('BuildReturnOutAsync' in artifact, 'generation must continue producing ReturnOut NACHA-M')
require('PersistAudit = true' in artifact, 'artifact generation must preserve audit persistence')
require('AchTransferStateEnum.ReturnedByEpr' in transitions, 'generation must continue moving original transactions to ReturnedByEpr')
require('TransitionAsync' in transitions, 'state transition must remain persisted through the transition service')
require('transmissionStatus = "GeneratedNotTransmitted"' in audit, 'audit must continue documenting no transmission by generation request')
require('productiveStatus = "TechnicalGeneratedOnly"' in audit, 'audit must continue documenting generated-only productive status')
for source_name, source in (('controller', controller), ('returns service', returns_service)):
    require('IAchOutboundReturnDispatchService' not in source, f'{source_name} must not directly dispatch outbound returns')
    require('IContrapartidaDispatchJobService' not in source, f'{source_name} must not directly dispatch contrapartida jobs')
    require('IWscfaachSoapClient' not in source, f'{source_name} must not directly own SOAP client')

# ROR child-route authorization is owned by NAV3.4K. NAV3.4D retains regression
# coverage for the shared service/backend contracts without pinning the superseded guard.
require("path: 'returns-ror'" in tx_routes, 'returns-ror route must remain present')
require('evaluateReturnOfReturn' in spa_service, 'shared SPA service must retain ROR methods')
require('generateReturnOfReturnAuditFile' in spa_service, 'shared SPA service must retain ROR audit generation')
require('generateReturnOfReturnNachaFile' in spa_service, 'shared SPA service must retain ROR NACHA generation')
require('P0Policies.ReturnsGenerateFile' in ror_controller, 'ROR controller mutable policy must remain untouched')
require('P0Policies.ReturnsRead' in ror_controller, 'ROR controller read policy must remain untouched')

# Previously closed NAV3.4 surfaces and test tools remain intact.
require('TransactionsCreatePermissionSeeder.PermissionId' in create_auth and 'PermissionConfiguration.ReadAchPermissionId' not in create_auth,
        'NAV3.4B create menu authorization must remain closed')
require('NachaUploadPermissionSeeder.PermissionId' in upload_auth and 'PermissionConfiguration.ReadAchPermissionId' not in upload_auth,
        'NAV3.4C upload menu authorization must remain closed')
require('Nacha.Generate' in manual_review and 'CanManageAch' in manual_review,
        'NAV3.4A manual-review mutable-action gate must remain intact')
require('FineGrainedPermissions.TestTools.Access' in test_tools and 'serviceTests.IsActive = true;' in test_tools,
        'test-tools security must remain intact')

print(f'NAV3.4D static validation passed: {checks} assertions')
