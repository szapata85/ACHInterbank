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
menu_auth = read('src/Cfa.ACHInterbank.Persistence/Security/Seeders/TransactionsCreateMenuAuthorizationSeeder.cs')
permission_seed = read('src/Cfa.ACHInterbank.Persistence/Security/Seeders/TransactionsCreatePermissionSeeder.cs')
fine = read('src/Cfa.ACHInterbank.Application/Security/FineGrainedPermissions.cs')
policy_reg = read('src/Cfa.ACHInterbank.External/DependencyInjectionService.cs')
app_routes = read('web/ach-interbank-ui/src/app/app-routing.module.ts')
tx_routes = read('web/ach-interbank-ui/src/app/features/transactions/transactions-routing.module.ts')
component = read('web/ach-interbank-ui/src/app/features/transactions/components/transaction-create/transaction-create.component.ts')
spa_service = read('web/ach-interbank-ui/src/app/features/transactions/services/transactions-api.service.ts')
controller = read('src/Cfa.ACHInterbank.Api/Controllers/TransactionsController.cs')
customers_controller = read('src/Cfa.ACHInterbank.Api/Controllers/CustomersController.cs')
institutions_controller = read('src/Cfa.ACHInterbank.Api/Controllers/FinancialInstitutionsController.cs')
tx_service = read('src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchTransactionService.cs')
dispatch_persistence = read('src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ContrapartidaDispatchPersistenceService.cs')
role_perm = read('src/Cfa.ACHInterbank.Persistence/Configuration/RolePermissionConfiguration.cs')
manual_review = read('web/ach-interbank-ui/src/app/features/ach-responses/pages/ach-response-manual-review-page.component.ts')
test_tools = read('src/Cfa.ACHInterbank.Persistence/Security/Seeders/TestToolsMenuAuthorizationSeeder.cs')

# Canonical menu identity is preserved.
create_item = slice_between(menu_cfg, 'Id = TransactionsCreateId,', 'new MenuItem',)
require('Label = "Crear transacción"' in create_item, 'create label must remain unchanged')
require('Route = "/transactions/create"' in create_item, 'create route must remain unchanged')
require('ParentId = TransactionsId' in create_item, 'create parent must remain unchanged')
require('IsActive = true' in create_item, 'create item must remain active')
require('TransactionsCreateId' in menu_role_cfg and 'RoleConfiguration.AdminRoleId' in menu_role_cfg and 'RoleConfiguration.OperatorRoleId' in menu_role_cfg,
        'existing create menu roles must remain present')

# Baseline drift remains visible in HasData, but final ordered reconciliation removes the read fallback only for this item.
require('MenuItemId = MenuItemConfiguration.TransactionsCreateId' in menu_perm_cfg, 'baseline create permission links must remain in model configuration')
require('AuthorizationReconciliationOrder = 102' in menu_auth, 'create authorization must reconcile after NAV2/TestTools reconcilers')
require('TransactionsCreatePermissionSeeder.PermissionId' in menu_auth, 'menu must accept Transactions.Create')
require('PermissionConfiguration.ManageAchPermissionId' in menu_auth, 'menu must retain CanManageAch compatibility')
require('PermissionConfiguration.ReadAchPermissionId' not in menu_auth, 'menu reconciliation must not retain CanReadAch')
require('permissionLinks.Where(link => !allowedPermissionIds.Contains(link.PermissionId))' in menu_auth,
        'menu reconciliation must remove only disallowed create permission links')
require('MenuItemRoles' not in menu_auth and 'menuItem.Label' not in menu_auth and 'menuItem.Order' not in menu_auth,
        'menu authorization reconciliation must not alter roles/label/order')

# Transactions.Create is an existing fine-grained contract; NAV3.4B only persists it for menu linkage and grants it to nobody.
require('public const string Create = "Transactions.Create"' in fine, 'Transactions.Create must already exist')
require('Transactions.Read, Transactions.Create' in fine, 'Transactions.Create must remain in AllPermissions')
require('FineGrainedPermissions.Transactions.Create' in permission_seed, 'permission catalog seeder must reuse existing Transactions.Create')
require('RolePermission' not in permission_seed and 'User' not in permission_seed.replace('using Cfa.ACHInterbank.Domain.Entities.User;', ''),
        'Transactions.Create catalog seeder must not create role/user grants')
require('RolePermissionConfiguration' not in permission_seed, 'permission seeder must not alter role grants')

# P0 create is the canonical backend capability: specific create OR legacy manage, never read.
p0_create = slice_between(policy_reg, 'options.AddPolicy(P0Policies.TransactionsCreate,', 'options.AddPolicy(P0Policies.TransactionsBulkSubmit,')
require('FineGrainedPermissions.Transactions.Create' in p0_create, 'P0 create must accept Transactions.Create')
require('"CanManageAch"' in p0_create, 'P0 create must retain CanManageAch compatibility')
require('CanReadAch' not in p0_create, 'P0 create must reject CanReadAch')

# SPA create route mirrors P0 create alternatives; list remains read-only and parent remains untouched.
list_route = slice_between(tx_routes, "path: 'list'", "path: 'create'")
create_route = slice_between(tx_routes, "path: 'create'", "path: 'bulk-create'")
require("permissions: ['CanReadAch']" in list_route, 'transaction list must remain CanReadAch')
require("permissions: ['Transactions.Create', 'CanManageAch']" in create_route, 'create route must mirror P0 create alternatives')
require('CanReadAch' not in create_route, 'create child guard must not accept CanReadAch')
require('roleGuard' not in create_route and 'roles:' not in create_route, 'NAV3.4B must not add role-name checks to create child route')
parent_route = slice_between(app_routes, "path: 'transactions'", "path: 'integraciones'")
require("roles: ['Admin', 'ACH.Operator']" in parent_route, 'existing transactions parent roles must remain unchanged')
require("permissions: ['CanManageAch', 'CanReadAch']" in parent_route, 'existing transactions parent read/manage prerequisite must remain unchanged')

# UI actually performs read/preparation + a POST create.
for token in ('this.customersApi.getAll()', 'this.financialInstitutionsApi.getAll()', 'this.api.getCompanyEntryDescriptions()', '.getContextualThirdPartyAccounts(', 'this.api.previewPolicy(', '.createTransaction(sanitized)'):
    require(token in component, f'create component missing expected operation: {token}')
require('this.api.post<TransactionResponse>(this.basePath, sanitized)' in spa_service, 'create API service must POST api/transactions')
require('createBulkTransaction(' not in component, 'individual create screen must not submit bulk create')

# API create remains protected by P0.TransactionsCreate; auxiliary GETs keep read/preview policies.
create_action = slice_between(controller, '[EndpointSummary("Registrar transacción individual")]', '[EndpointSummary("Registrar transacciones en lote")')
require('[HttpPost]' in create_action, 'individual create must remain POST')
require('[Authorize(Policy = P0Policies.TransactionsCreate)]' in create_action, 'individual create must require P0.TransactionsCreate')
require('_transactionService.RegisterTransactionAsync' in create_action, 'individual create must call registration service')
require('[Authorize(Policy = P0Policies.TransactionsRead)]' in slice_between(controller, '[HttpGet("company-entry-descriptions")]', '[HttpGet("third-party-accounts/selector")'), 'company entry descriptions must remain read-protected')
require('[Authorize(Policy = P0Policies.TransactionsRead)]' in slice_between(controller, '[HttpGet("third-party-accounts/selector")]', '[HttpGet("policies/preview")'), 'third-party selector must remain read-protected')
require('[Authorize(Policy = P0Policies.TransactionsPolicyPreview)]' in slice_between(controller, '[HttpGet("policies/preview")]', '[EndpointSummary("Registrar transacción individual")'), 'policy preview must retain preview/read capability')
require('[Authorize(Policy = "CanReadAch")]' in customers_controller, 'customers auxiliary API must remain CanReadAch')
require('[Authorize(Policy = "CanReadAch")]' in institutions_controller, 'institutions auxiliary API must remain CanReadAch')

# Functional behavior: internal persistence/queue preparation only; no direct live dispatch/file generation added.
require('_transactionPersister.PersistAsync' in tx_service, 'create flow must persist transaction')
require('_cenitCycleQueueService.EnqueueAsync' in tx_service, 'create flow may enqueue internal cycle work')
require('_contrapartidaDispatchPersistenceService.EnsurePendingDispatchAsync' in tx_service, 'create flow may prepare pending dispatch record')
require('_unitOfWork.CommitAsync' in tx_service, 'create flow must commit internal state')
for token in ('IContrapartidaDispatchJobService', 'ProcessTransactionAsync'):
    require(token not in tx_service, f'create service must not directly dispatch externally: {token}')
require('State = ContrapartidaDispatchItemStateEnum.PendingContrapartidaReport' in dispatch_persistence,
        'contrapartida preparation must remain pending, not live execution')
require('IContrapartidaDispatchJobService' not in dispatch_persistence and 'ProcessTransactionAsync' not in dispatch_persistence,
        'pending-dispatch persistence must not run live job')

# No role grant changes and no regression in already-closed adjacent surfaces.
require('Transactions.Create' not in role_perm, 'NAV3.4B must not grant Transactions.Create through RolePermissions')
require('Nacha.Generate' in manual_review and 'CanManageAch' in manual_review, 'NAV3.4A manual-review protection must remain present')
require('FineGrainedPermissions.TestTools.Access' in test_tools, 'TestTools.Access menu protection must remain intact')
require('serviceTests.IsActive = true;' in test_tools, 'Pruebas de servicios must remain active')

print(f'NAV3.4B static validation passed: {checks} assertions')
