from pathlib import Path
import hashlib

ROOT = Path(__file__).resolve().parents[1]
checks = 0

def read(path: str) -> str:
    return (ROOT / path).read_text(encoding='utf-8')

def sha(path: str) -> str:
    return hashlib.sha256((ROOT / path).read_bytes()).hexdigest()

def require(condition: bool, message: str):
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
external = read('src/Cfa.ACHInterbank.External/DependencyInjectionService.cs')
perm_seed = read('src/Cfa.ACHInterbank.Persistence/Security/Seeders/IntegrationReadPermissionSeeder.cs')
audience_seed = read('src/Cfa.ACHInterbank.Persistence/Security/Seeders/IntegrationReadNavigationAudienceSeeder.cs')
role_grants = read('src/Cfa.ACHInterbank.Persistence/Configuration/RolePermissionConfiguration.cs')
routing = read('web/ach-interbank-ui/src/app/app-routing.module.ts')
child_routes = read('web/ach-interbank-ui/src/app/features/integrations/integrations.routes.ts')
contract_store = read('web/ach-interbank-ui/src/app/features/integrations/state/contract-workspace.store.ts')
center_store = read('web/ach-interbank-ui/src/app/features/integrations/state/integration-center.store.ts')
connection_page = read('web/ach-interbank-ui/src/app/features/integrations/pages/soap-integration-settings.component.ts')
catalog = read('src/Cfa.ACHInterbank.Api/Controllers/IntegrationCatalogController.cs')
contracts = read('src/Cfa.ACHInterbank.Api/Controllers/IntegrationContractsController.cs')
mappings = read('src/Cfa.ACHInterbank.Api/Controllers/IntegrationMappingSetsController.cs')
connections = read('src/Cfa.ACHInterbank.Api/Controllers/SoapIntegrationSettingsController.cs')
mft_seed = read('src/Cfa.ACHInterbank.Persistence/Security/Seeders/MftAdministrationMenuAuthorizationSeeder.cs')
mft_component = read('web/ach-interbank-ui/src/app/features/ach-colombia-mft-administration/ach-colombia-mft-administration.component.ts')
focal_test = read('tests/Cfa.ACHInterbank.Tests/NavigationMenuNav34RIntegrationsReadAudienceTests.cs')

# Capability existed before NAV3.4R and remains in the canonical catalog.
require('public static class Integrations' in fine, 'Integrations capability group missing')
require('public const string Read = "Integrations.Read";' in fine, 'Integrations.Read constant missing')
all_block = between(fine, 'public static IReadOnlyList<string> AllPermissions { get; } =', '];')
require('Integrations.Read' in all_block, 'Integrations.Read must remain in AllPermissions')
for token in ('Integrations.ManageContracts', 'Integrations.ManageConnections', 'Integrations.ManageMappings', 'Integrations.Validate', 'Integrations.Publish'):
    require(token in all_block, f'existing integration mutation capability missing from AllPermissions: {token}')

# NAV3.4R materializes only the existing read capability, idempotently and without grants.
for token in (
    'IntegrationReadPermissionSeeder',
    'e6cfca32-17a9-45c0-a6f9-2a023317fbce',
    'FineGrainedPermissions.Integrations.Read',
    'Consultar configuración de integraciones',
    'FirstOrDefaultAsync(item => item.Name == FineGrainedPermissions.Integrations.Read)',
    'context.Permissions.FindAsync(ReadPermissionId)',
):
    require(token in perm_seed, f'Integrations.Read materialization missing: {token}')
for forbidden in ('RolePermission', 'UserPermission'):
    require(forbidden not in perm_seed, f'Integrations.Read seeder must not create grants: {forbidden}')
require('FineGrainedPermissions.Integrations.Read' not in role_grants, 'Integrations.Read must not receive role grants')

# Canonical transitional read policy: Integrations.Read OR CanReadAch only.
require('public const string IntegrationsRead = "P1.IntegrationsRead";' in p1, 'P1.IntegrationsRead constant missing')
read_policy = between(external, 'options.AddPolicy(P1Policies.IntegrationsRead,', 'options.AddPolicy(P1Policies.IntegrationContractsManage,')
require('FineGrainedPermissions.Integrations.Read' in read_policy, 'P1.IntegrationsRead must accept Integrations.Read')
require('"CanReadAch"' in read_policy, 'P1.IntegrationsRead must retain CanReadAch fallback')
for forbidden in ('CanManageAch', 'FineGrainedPermissions.Config.Read', 'FineGrainedPermissions.Config.Manage', 'ManageContracts', 'ManageMappings', 'ManageConnections', 'Integrations.Validate', 'Integrations.Publish'):
    require(forbidden not in read_policy, f'read policy must not imply management/config capability: {forbidden}')

# Mutation policies O/P/Q remain exact capability OR CanManageAch transitions.
policy_pairs = [
    ('IntegrationContractsManage', 'FineGrainedPermissions.Integrations.ManageContracts'),
    ('IntegrationConnectionsManage', 'FineGrainedPermissions.Integrations.ManageConnections'),
    ('IntegrationMappingsManage', 'FineGrainedPermissions.Integrations.ManageMappings'),
    ('IntegrationMappingsValidate', 'FineGrainedPermissions.Integrations.Validate'),
    ('IntegrationMappingsPublish', 'FineGrainedPermissions.Integrations.Publish'),
]
for idx, (name, cap) in enumerate(policy_pairs):
    start = f'options.AddPolicy(P1Policies.{name},'
    # use next policy marker or MaintenanceSeed for the last integration policy
    next_name = policy_pairs[idx+1][0] if idx + 1 < len(policy_pairs) else 'MaintenanceSeed'
    block = between(external, start, f'options.AddPolicy(P1Policies.{next_name},')
    require(cap in block, f'{name} lost fine capability')
    require('"CanManageAch"' in block, f'{name} lost CanManageAch fallback')
    require('FineGrainedPermissions.Integrations.Read' not in block, f'Integrations.Read must not authorize {name}')
    require('"CanReadAch"' not in block, f'CanReadAch must not authorize {name}')

# Final menu reconciler: three integration children gain Admin OR ACH.Operator and transitional read.
for token in (
    'AuthorizationReconciliationOrder = 118',
    'IntegrationContractMenuAuthorizationSeeder.Route',
    'InformationRelationshipMenuAuthorizationSeeder.Route',
    'ParticipantConnectionMenuAuthorizationSeeder.Route',
    'IntegrationReadPermissionSeeder.ReadPermissionId',
    'PermissionConfiguration.ReadAchPermissionId',
    'RoleConfiguration.AdminRoleId',
    'RoleConfiguration.OperatorRoleId',
):
    require(token in audience_seed, f'NAV3.4R menu reconciler missing: {token}')
require('ConfigurationPermissionSeeder.ReadPermissionId' in audience_seed, 'Conexiones parent must retain Config.Read for MFT')
require('ManageUsersPermissionId' not in audience_seed and 'CanManageUsers' not in audience_seed, 'obsolete CanManageUsers must not remain in final parent model')
require('AchColombiaMftAdministrationMenuSeeder.Route' not in audience_seed, 'NAV3.4R audience seeder must not mutate MFT child authorization')
for forbidden in ('RolePermissions.Add', 'UserPermission', 'new RolePermission'):
    require(forbidden not in audience_seed, f'NAV3.4R must not add grants: {forbidden}')

# Child SPA routes explicitly use Integrations.Read OR CanReadAch; no child role hardcoding.
require(child_routes.count("permissions: ['Integrations.Read', 'CanReadAch']") == 4, 'all three integration routes plus mapping editor must use transitional read')
for path in ("path: 'contrato'", "path: 'relacion-informacion'", "path: 'relacion-informacion/:methodCode/:mappingSetId'", "path: 'conexion'"):
    require(path in child_routes, f'integration child route missing: {path}')
require('roles:' not in child_routes, 'child integration route file must not introduce role-name checks')

# Parent SPA stays transitional role-scoped and retains legacy management alternative.
parent_route = between(routing, "path: 'integraciones'", "path: 'navigation'")
require("roles: ['Admin', 'ACH.Operator']" in parent_route, 'parent SPA must remain Admin OR ACH.Operator')
require("permissions: ['Integrations.Read', 'CanReadAch', 'CanManageAch']" in parent_route, 'parent SPA must accept Integrations.Read + legacy read/manage alternatives')
require('roleGuard' in parent_route and 'permissionGuard' in parent_route, 'parent SPA must remain role AND permission guarded')

# Internal read state follows the same transition without changing mutation checks.
require("canRead: permissions.includes('Integrations.Read') || permissions.includes('CanReadAch')" in contract_store, 'Contract store read model not normalized')
require("canRead: permissions.includes('Integrations.Read') || permissions.includes('CanReadAch')" in center_store, 'Integration center read model not normalized')
require("readonly canRead = this.auth.hasPermission(['Integrations.Read', 'CanReadAch']);" in connection_page, 'Connection screen read model not normalized')
require("permissions.includes('Integrations.ManageContracts')" in contract_store and "permissions.includes('CanManageAch')" in contract_store, 'Contract management model regressed')
require("this.auth.hasPermission(['Integrations.ManageConnections', 'CanManageAch'])" in connection_page, 'Connection management model regressed')

# All integration-domain GETs are under P1.IntegrationsRead; no direct CanReadAch GET remains in these controllers.
controllers = [catalog, contracts, mappings, connections]
for source, expected_count, name in ((catalog, 4, 'catalog'), (contracts, 3, 'contracts'), (mappings, 5, 'mappings'), (connections, 1, 'connection')):
    require(source.count('[Authorize(Policy = P1Policies.IntegrationsRead)]') == expected_count, f'{name} GET count under P1.IntegrationsRead changed')
    require('[Authorize(Policy = "CanReadAch")]' not in source, f'{name} still contains direct CanReadAch authorization')
require(sum(s.count('[Authorize(Policy = P1Policies.IntegrationsRead)]') for s in controllers) == 13, 'expected 13 normalized integration GET endpoints')

# Mutation annotations remain the O/P/Q policies/fallback boundaries.
for token in (
    '[Authorize(Policy = P1Policies.IntegrationContractsManage)]',
):
    require(contracts.count(token) == 4, 'Contract focal mutation policy count changed')
require(mappings.count('[Authorize(Policy = P1Policies.IntegrationMappingsManage)]') >= 4, 'Mapping management policy missing')
require(mappings.count('[Authorize(Policy = P1Policies.IntegrationMappingsValidate)]') >= 2, 'Mapping validate/preview policy missing')
require(mappings.count('[Authorize(Policy = P1Policies.IntegrationMappingsPublish)]') >= 1, 'Mapping publish policy missing')
require(connections.count('[Authorize(Policy = P1Policies.IntegrationConnectionsManage)]') == 2, 'Connection management policy count changed')

# MFT is intentionally not part of Integrations.Read.
mft_route = between(routing, "path: 'administracion/mft-ach'", "path: 'cenit'")
require("permissions: ['Config.Read', 'CanReadAch']" in mft_route, 'MFT SPA read boundary changed')
require('Integrations.Read' not in mft_route, 'MFT must not depend on Integrations.Read')
require('ConfigurationPermissionSeeder.ReadPermissionId' in mft_seed and 'PermissionConfiguration.ReadAchPermissionId' in mft_seed, 'MFT menu read model changed')
require('IntegrationReadPermissionSeeder' not in mft_seed and 'Integrations.Read' not in mft_seed, 'MFT menu must remain independent of Integrations.Read')
require("hasPermission(['Config.Manage', 'CanManageAch'])" in mft_component, 'MFT management model changed')

# Focal C# tests cover materialization, policy, menu audience, API and MFT regression.
for token in (
    'IntegrationsRead_IsPreExistingCapability_MaterializedIdempotentlyWithoutGrants',
    'IntegrationsReadPolicy_AllowsFineCapabilityOrLegacyRead_ButNotManagement',
    'NavigationAudienceSeeder_AlignsThreeIntegrationChildren_AndRemovesObsoleteParentCanManageUsers',
    'MenuFiltering_AdminAndOperatorWithLegacyRead_SeeTheSameIntegrationChildren',
    'MenuFiltering_FineReadRequiresApprovedRoleForIntegrationChildren_AndDoesNotGrantMft',
    'MenuFiltering_RoleWithoutReadOrCanManageUsersOnly_DoesNotExposeIntegrationChildren',
    'IntegrationReadApiPolicy_CoversIntegrationGetEndpoints_AndMutationPoliciesRemainSeparated',
    'SourceRegression_ParentAndChildrenUseTransitionalRead_MftAndMutationPoliciesRemainIntact',
):
    require(token in focal_test, f'NAV3.4R focal test missing: {token}')

# Closed infrastructure and mutation capability seeders remain byte-identical to NAV3.4Q input.
expected_hashes = {
    'src/Cfa.ACHInterbank.Application/Navigation/Queries/GetMenuForCurrentUserHandler.cs': 'f111e416c181fc1912c297e37012cf7462f49d0434c1ef6ec9416c89b5ed1a61',
    'web/ach-interbank-ui/src/app/core/services/navigation.service.ts': 'f8b984005ca4f0b99adb3abccd4e0c2e46da5ffc6f8e9a9f9d09ccf84bd2a3de',
    'src/Cfa.ACHInterbank.Persistence/Security/Seeders/MftAdministrationMenuAuthorizationSeeder.cs': 'd2e27ad8e2bc70c4e56d76a68d4fd638e23a2b53f5b3f7147361838a9945559a',
    'web/ach-interbank-ui/src/app/features/ach-colombia-mft-administration/ach-colombia-mft-administration.component.ts': 'dac1bf2b458ce23c74d49758a66f931c583cf3410a175ccd451094d9d08e0499',
    'src/Cfa.ACHInterbank.Api/Controllers/AchColombiaFileExchangeController.cs': '6ec050055e13ad74403bc9167787924c65784f83ee4a8f51d39b0203ccad9bb2',
    'src/Cfa.ACHInterbank.Api/Controllers/AchColombiaCycleOperationalSchedulesController.cs': 'dab894745a44c0fc7b4ae76da8b4e9f3283e5898909c2da65b5f36b49818c276',
    'src/Cfa.ACHInterbank.Persistence/Security/Seeders/IntegrationContractPermissionSeeder.cs': 'a5462711598c057621f723aa8ba684cd99a635c37d041a50ccaa1c24f00e0b4a',
    'src/Cfa.ACHInterbank.Persistence/Security/Seeders/IntegrationMappingPermissionSeeder.cs': '128c77eb91f6028d98714242da664acc8d8533df977bd77732f44d7b493f825f',
    'src/Cfa.ACHInterbank.Persistence/Security/Seeders/IntegrationConnectionPermissionSeeder.cs': '82ed02669979534a7754c4bc208c4b6d019406fa6c3d1565d9bae8fa8691a26f',
}
for path, expected in expected_hashes.items():
    require(sha(path) == expected, f'closed/out-of-scope artifact changed unexpectedly: {path}')

print(f'NAV3.4R integrations-read/audience static validation PASS ({checks} assertions)')
