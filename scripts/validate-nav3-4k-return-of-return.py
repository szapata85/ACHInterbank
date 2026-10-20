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

routing = read('web/ach-interbank-ui/src/app/features/transactions/transactions-routing.module.ts')
routing_spec = read('web/ach-interbank-ui/src/app/features/transactions/transactions-routing.module.spec.ts')
app_routing = read('web/ach-interbank-ui/src/app/app-routing.module.ts')
component = read('web/ach-interbank-ui/src/app/features/transactions/components/ach-return-of-return-management/ach-return-of-return-management.component.ts')
template = read('web/ach-interbank-ui/src/app/features/transactions/components/ach-return-of-return-management/ach-return-of-return-management.component.html')
component_spec = read('web/ach-interbank-ui/src/app/features/transactions/components/ach-return-of-return-management/ach-return-of-return-management.component.spec.ts')
spa_service = read('web/ach-interbank-ui/src/app/features/transactions/services/ach-returns-api.service.ts')
controller = read('src/Cfa.ACHInterbank.Api/Controllers/AchReturnOfReturnController.cs')
eligibility = read('src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchReturnOfReturnEligibilityService.cs')
generation = read('src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchReturnOfReturnFileGenerationService.cs')
permissions = read('src/Cfa.ACHInterbank.Application/Security/FineGrainedPermissions.cs')
policies = read('src/Cfa.ACHInterbank.External/DependencyInjectionService.cs')
read_seeder = read('src/Cfa.ACHInterbank.Persistence/Security/Seeders/ReturnsReadPermissionSeeder.cs')
menu_seeder = read('src/Cfa.ACHInterbank.Persistence/Security/Seeders/ReturnOfReturnMenuAuthorizationSeeder.cs')
role_grants = read('src/Cfa.ACHInterbank.Persistence/Configuration/RolePermissionConfiguration.cs')
nav_ia = read('src/Cfa.ACHInterbank.Persistence/Navigation/Seeders/NavigationInformationArchitectureSeeder.cs')
returns_component = read('web/ach-interbank-ui/src/app/features/transactions/components/ach-returns-management/ach-returns-management.component.ts')
returns_menu = read('src/Cfa.ACHInterbank.Persistence/Security/Seeders/ReturnsMenuAuthorizationSeeder.cs')
returns_controller = read('src/Cfa.ACHInterbank.Api/Controllers/AchReturnsController.cs')
handler = read('src/Cfa.ACHInterbank.Application/Navigation/Queries/GetMenuForCurrentUserHandler.cs')

# Canonical fine-grained contracts already existed; NAV3.4K materializes only Returns.Read.
require('public const string Read = "Returns.Read";' in permissions, 'Returns.Read fine-grained capability must already exist')
require('public const string GenerateFile = "Returns.GenerateFile";' in permissions, 'Returns.GenerateFile fine-grained capability must remain')
require('Returns.Read, Returns.GenerateFile' in permissions, 'Returns permissions must remain in AllPermissions')
require('FineGrainedPermissions.Returns.Read' in read_seeder, 'NAV3.4K must materialize existing Returns.Read')
require('Consultar devoluciones ACH' in read_seeder, 'Returns.Read catalog label must be functional and Spanish')
require('RolePermissions' not in read_seeder, 'Returns.Read seeder must not create role grants')
require('User' not in read_seeder.replace('Entities.User', ''), 'Returns.Read seeder must not create direct user grants')
require('ReturnsPermissionSeeder' not in read_seeder, 'NAV3.4K must not duplicate Returns.GenerateFile seeding')
require('FineGrainedPermissions.Returns.GenerateFile' not in read_seeder, 'NAV3.4K read seeder must not rewrite generate capability')

# Effective policy semantics remain backend source of truth.
returns_read_policy = slice_between(policies, 'options.AddPolicy(P0Policies.ReturnsRead,', 'options.AddPolicy(P0Policies.ReturnsGenerateFile,')
require('FineGrainedPermissions.Returns.Read' in returns_read_policy, 'P0.ReturnsRead must accept Returns.Read')
require('"CanReadAch"' in returns_read_policy, 'P0.ReturnsRead must retain CanReadAch fallback')
require('"CanManageAch"' not in returns_read_policy, 'P0.ReturnsRead must not use CanManageAch as read fallback')
returns_generate_policy = slice_between(policies, 'options.AddPolicy(P0Policies.ReturnsGenerateFile,', 'options.AddPolicy(P0Policies.ClaimsRead,')
require('FineGrainedPermissions.Returns.GenerateFile' in returns_generate_policy, 'P0.ReturnsGenerateFile must accept Returns.GenerateFile')
require('"CanManageAch"' in returns_generate_policy, 'P0.ReturnsGenerateFile must retain CanManageAch fallback')
require('"CanReadAch"' not in returns_generate_policy, 'CanReadAch must not generate ROR files')

# Menu canonical structure and direct authorization.
require('ReturnOfReturnRoute,' in nav_ia and '"Devolución de una devolución"' in nav_ia, 'canonical ROR MenuItem must remain')
require('order: 70' in nav_ia, 'ROR MenuItem must remain Order 70')
require('AuthorizationReconciliationOrder = 111' in menu_seeder, 'ROR authorization seeder must run after prior reconcilers')
require('NavigationInformationArchitectureSeeder.OperationRoute' in menu_seeder, 'ROR menu must remain under Operación')
require('"/transactions/returns-ror"' in menu_seeder, 'ROR menu route must remain unchanged')
require('"Devolución de una devolución"' in menu_seeder, 'ROR label must remain unchanged')
require('menuItem.Order != 70' in menu_seeder, 'ROR seeder must validate Order 70')
require('ReturnsReadPermissionSeeder.ReadPermissionId' in menu_seeder, 'ROR menu must include Returns.Read')
require('PermissionConfiguration.ReadAchPermissionId' in menu_seeder, 'ROR menu must retain CanReadAch fallback')
require('PermissionConfiguration.ManageAchPermissionId' not in menu_seeder, 'ROR menu entry must not require mutable capability')
require('MenuItemRoles.RemoveRange' in menu_seeder, 'ROR menu must not retain accidental role gate')
require('RolePermissions' not in menu_seeder, 'ROR menu seeder must not create grants')

# SPA is mixed: read/evaluation entry, generation fail-closed.
ror_route = routing[routing.find("path: 'returns-ror'"):]
require("permissions: ['Returns.Read', 'CanReadAch']" in ror_route, 'ROR child route must use P0.ReturnsRead equivalents')
require("permissions: ['CanManageAch']" not in ror_route, 'ROR child route must no longer be management-only')
require('roles:' not in ror_route, 'ROR child route must not hardcode roles')
require("RETURN_OF_RETURN_GENERATE_PERMISSIONS = ['Returns.GenerateFile', 'CanManageAch']" in component, 'ROR generation action must use backend-equivalent mutable permissions')
require('get canGenerateFiles(): boolean' in component, 'ROR component must expose visual generation gate')
require(component.count('if (!this.ensureCanGenerateFiles())') == 2, 'both generation methods must fail closed before calling services')
require('No tiene permisos para generar archivos de devolución de devolución.' in component, 'fail-closed generation denial must be explicit')
require('*ngIf="canGenerateFiles; else readOnlyGenerationNotice"' in template, 'generation controls must be visually gated')
require('permite evaluar elegibilidad, pero no generar archivos' in template, 'read-only users must receive an explicit UI explanation')
require('canGenerateFiles\n      && !this.loadingNacha' in component, 'productive NACHA enablement must include generation permission')
require("AuthService" in component and 'inject(AuthService)' in component, 'component must evaluate permission claims, not roles')
require('hasRole' not in component, 'ROR component must not use role-name authorization')
require('falla cerrado al intentar generar auditoría sin capacidad mutable' in component_spec, 'SPA tests must cover audit fail-closed')
require('falla cerrado al intentar generar NACHA sin capacidad mutable' in component_spec, 'SPA tests must cover NACHA fail-closed')
require("expectRouteContainsPermission('returns-ror', 'Returns.Read')" in routing_spec, 'route tests must cover fine read capability')
require("expectRouteContainsPermission('returns-ror', 'CanReadAch')" in routing_spec, 'route tests must cover legacy read fallback')
require("expect(route?.data?.['permissions']).not.toContain('CanManageAch')" in routing_spec, 'route tests must reject management-only child entry')

# Parent /transactions is intentionally untouched and remains an additional inherited prerequisite.
parent_route = slice_between(app_routing, "path: 'transactions'", "path: 'integraciones'")
require("roles: ['Admin', 'ACH.Operator']" in parent_route, 'transactions parent role gate must remain unchanged')
require("permissions: ['CanManageAch', 'CanReadAch']" in parent_route, 'transactions parent permission gate must remain unchanged')

# UI consumes only three ROR endpoints: read-only evaluation and two generation operations.
require("'ach-returns/return-of-return/evaluate'" in spa_service, 'SPA must keep ROR evaluate endpoint')
require("'ach-returns/return-of-return/generate-audit-file'" in spa_service, 'SPA must keep ROR audit generation endpoint')
require("'ach-returns/return-of-return/generate-nacha-file'" in spa_service, 'SPA must keep ROR NACHA generation endpoint')
require('cenit/out' not in spa_service, 'SPA must not consume separate CENIT ROR creation endpoint')
require(spa_service.count('return-of-return/') == 3, 'SPA ROR service surface must remain exactly three consumed operations')

# Backend policies are already correct and remain unchanged.
require('[HttpPost("evaluate")]' in controller and '[Authorize(Policy = P0Policies.ReturnsRead)]' in controller, 'evaluate must remain P0.ReturnsRead')
require('[HttpPost("generate-audit-file")]' in controller, 'audit generation endpoint must remain')
require('[HttpPost("generate-nacha-file")]' in controller, 'NACHA generation endpoint must remain')
require(controller.count('[Authorize(Policy = P0Policies.ReturnsGenerateFile)]') == 3, 'all ROR mutable controller actions must remain protected by ReturnsGenerateFile')
require('[HttpPost("cenit/out")]' in controller, 'separate CENIT ROR creation endpoint must remain intact')

# Functional characterization/effects.
require('AsNoTracking()' in eligibility, 'eligibility evaluation must read without tracking')
require('SaveChanges' not in eligibility, 'eligibility evaluation must not persist changes')
require('ReturnOfReturnFlows.Add' not in eligibility, 'eligibility evaluation must not create ROR flows')
require('context.AchReturnOfReturnGeneratedFileAudits.Add(audit)' in generation, 'ROR generation must persist generated-file audit evidence')
require(generation.count('context.AchReturnOfReturnGeneratedFileAudits.Add(audit)') >= 3, 'audit and productive generation paths must persist evidence')
require('GenerateNachaAsync' in generation, 'productive ROR NACHA generation must remain')
require('BuildReturnOutAsync' in generation, 'CENIT ROR generation must continue using NACHA builder')
require('ReturnOfReturnFlows.Add' not in generation, 'file generation must not create ROR flow entities')
for forbidden in ('IContrapartidaDispatchJobService', 'IWscfaachSoapClient', 'IAchOutboundReturnDispatchService', 'HttpClient'):
    require(forbidden not in generation, f'ROR generation must not directly own external transport: {forbidden}')

# No grants changed by NAV3.4K; seeded Admin/Operator legacy grants remain the same source baseline.
require('PermissionConfiguration.ManageAchPermissionId' in role_grants, 'legacy CanManageAch role grants must remain')
require('PermissionConfiguration.ReadAchPermissionId' in role_grants, 'legacy CanReadAch role grants must remain')
require('ReturnsReadPermissionSeeder' not in role_grants, 'NAV3.4K must not add Returns.Read RolePermissions')
require('ReturnsPermissionSeeder' not in role_grants, 'NAV3.4K must not add Returns.GenerateFile RolePermissions')

# NAV3.4D ordinary returns remains intact.
returns_route = slice_between(routing, "path: 'returns'", "path: 'returns-ror'")
require("permissions: ['CanReadAch']" in returns_route, 'ordinary /transactions/returns route must remain NAV3.4D read-only entry')
require("RETURNS_GENERATE_PERMISSIONS = ['Returns.GenerateFile', 'CanManageAch']" in returns_component, 'ordinary returns mutable gate must remain NAV3.4D model')
require('PermissionConfiguration.ReadAchPermissionId' in returns_menu, 'ordinary returns menu authorization must remain CanReadAch')
require('ReturnsReadPermissionSeeder' not in returns_menu, 'NAV3.4K must not alter ordinary returns menu authorization')
require('P0Policies.ReturnsRead' in returns_controller and 'P0Policies.ReturnsGenerateFile' in returns_controller, 'ordinary returns API policies must remain')
require('return hasRole && hasPermission;' in handler, 'GetMenuForCurrentUserHandler must remain unchanged')

print(f'NAV3.4K static validation passed: {checks} assertions')
