#!/usr/bin/env python3
from __future__ import annotations

import hashlib
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
checks = 0

def require(condition: bool, message: str) -> None:
    global checks
    checks += 1
    if not condition:
        raise AssertionError(message)

def read(path: str) -> str:
    return (ROOT / path).read_text(encoding="utf-8")

def sha(path: str) -> str:
    return hashlib.sha256((ROOT / path).read_bytes()).hexdigest()

def between(source: str, start: str, end: str) -> str:
    begin = source.find(start)
    require(begin >= 0, f"missing start marker: {start}")
    finish = source.find(end, begin + len(start))
    require(finish > begin, f"missing end marker after {start}: {end}")
    return source[begin:finish]

fine = read("src/Cfa.ACHInterbank.Application/Security/FineGrainedPermissions.cs")
p1 = read("src/Cfa.ACHInterbank.Application/Security/P1Policies.cs")
external = read("src/Cfa.ACHInterbank.External/DependencyInjectionService.cs")
perm_seeder = read("src/Cfa.ACHInterbank.Persistence/Security/Seeders/IntegrationMappingPermissionSeeder.cs")
menu_seeder = read("src/Cfa.ACHInterbank.Persistence/Security/Seeders/InformationRelationshipMenuAuthorizationSeeder.cs")
ia = read("src/Cfa.ACHInterbank.Persistence/Navigation/Seeders/NavigationInformationArchitectureSeeder.cs")
routes = read("web/ach-interbank-ui/src/app/features/integrations/integrations.routes.ts")
page = read("web/ach-interbank-ui/src/app/features/integrations/pages/mapping-sets-page.component.ts")
page_template = read("web/ach-interbank-ui/src/app/features/integrations/pages/mapping-sets-page.component.html")
editor = read("web/ach-interbank-ui/src/app/features/integrations/pages/mapping-editor-page.component.ts")
editor_template = read("web/ach-interbank-ui/src/app/features/integrations/pages/mapping-editor-page.component.html")
store = read("web/ach-interbank-ui/src/app/features/integrations/state/mapping-workspace.store.ts")
controller = read("src/Cfa.ACHInterbank.Api/Controllers/IntegrationMappingSetsController.cs")
catalog = read("src/Cfa.ACHInterbank.Api/Controllers/IntegrationCatalogController.cs")
service = read("src/Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationMappingSetService.cs")
validation = read("src/Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationMappingValidationService.cs")
preview = read("src/Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationMappingPreviewService.cs")
resolver = read("src/Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationExecutionContractResolver.cs")
dtos = read("src/Cfa.ACHInterbank.Application/Integrations/Dtos/IntegrationMappingSetDtos.cs")
focal_test = read("tests/Cfa.ACHInterbank.Tests/NavigationMenuNav34PInformationRelationshipAuthorizationTests.cs")
route_spec = read("web/ach-interbank-ui/src/app/features/integrations/integrations.routes.spec.ts")

# Canonical NAV2 identity remains untouched.
relationship = between(ia, "var informationRelationship = await UpsertAsync(", "var participantConnection = await UpsertAsync(")
for token in ('InformationRelationshipRoute', '"Relación de información"', 'connections.Id', 'order: 20', 'icon: "schema"', 'exact: true'):
    require(token in relationship, f"canonical Relación de información structure changed/missing: {token}")

# Existing semantic capabilities are reused; no conceptual duplicate is created.
for token in (
    'public const string Read = "Integrations.Read";',
    'public const string ManageMappings = "Integrations.ManageMappings";',
    'public const string Validate = "Integrations.Validate";',
    'public const string Publish = "Integrations.Publish";',
    'public const string Compare = "Integrations.Compare";',
):
    require(token in fine, f"pre-existing integration capability missing: {token}")
for token in (
    'public const string IntegrationMappingsManage = "P1.IntegrationMappingsManage";',
    'public const string IntegrationMappingsValidate = "P1.IntegrationMappingsValidate";',
    'public const string IntegrationMappingsPublish = "P1.IntegrationMappingsPublish";',
):
    require(token in p1, f"P1 mapping transition policy missing: {token}")

for policy, capability in (
    ("IntegrationMappingsManage", "ManageMappings"),
    ("IntegrationMappingsValidate", "Validate"),
    ("IntegrationMappingsPublish", "Publish"),
):
    block = between(external, f"options.AddPolicy(P1Policies.{policy}", "options.AddPolicy(" if policy == "IntegrationMappingsPublish" else f"options.AddPolicy(P1Policies.{ {'IntegrationMappingsManage':'IntegrationMappingsValidate','IntegrationMappingsValidate':'IntegrationMappingsPublish'}[policy] }")
    require(f"FineGrainedPermissions.Integrations.{capability}" in block, f"{policy} fine capability missing")
    require('"CanManageAch"' in block, f"{policy} legacy fallback missing")
    require('"CanReadAch"' not in block, f"read permission must not satisfy {policy}")

# The three pre-existing fine capabilities become Permission rows, idempotently and without grants.
for token in (
    'ManageMappingsPermissionId = Guid.Parse("a78e21ac-846c-470d-a9a5-5d4bd019bf08")',
    'ValidatePermissionId = Guid.Parse("67d58589-d51e-44b2-8177-2650d93bc46d")',
    'PublishPermissionId = Guid.Parse("911b72f4-ef59-48e4-86de-7d559dbafb1d")',
    'FineGrainedPermissions.Integrations.ManageMappings',
    'FineGrainedPermissions.Integrations.Validate',
    'FineGrainedPermissions.Integrations.Publish',
):
    require(token in perm_seeder, f"mapping permission materialization missing: {token}")
require("RolePermissions" not in perm_seeder and "RoleId" not in perm_seeder, "mapping permission seeder must not add role grants")

# Entry boundary remains CanReadAch and preserves Admin-only menu discoverability.
for token in (
    'AuthorizationReconciliationOrder = 116',
    'Route = "/integraciones/relacion-informacion"',
    'PermissionConfiguration.ReadAchPermissionId',
    '"CanReadAch"',
):
    require(token in menu_seeder, f"relationship menu reconciliation missing: {token}")
require("MenuItemRoles" not in menu_seeder, "NAV3.4P must preserve existing Admin MenuItemRole, not rewrite it")
require("RolePermissions" not in menu_seeder, "menu reconciliation must not add grants")
require("ManageUsersPermissionId" not in menu_seeder, "legacy CanManageUsers dependency must be removed")

# Both child routes explicitly express the read boundary; shared parent is outside this file.
list_route = between(routes, "path: 'relacion-informacion'", "path: 'relacion-informacion/:methodCode/:mappingSetId'")
editor_route = between(routes, "path: 'relacion-informacion/:methodCode/:mappingSetId'", "path: 'conexion'")
for name, block in (("list", list_route), ("editor", editor_route)):
    require("canActivate: [permissionGuard]" in block, f"{name} child route must have explicit permission guard")
    require("permissions: ['Integrations.Read', 'CanReadAch']" in block, f"{name} child route must require Integrations.Read or legacy CanReadAch after NAV3.4R")
    require("roles:" not in block, f"{name} child route must not add a role-name gate")
require("protege explícitamente Relación de información y su editor con Integrations.Read o CanReadAch" in route_spec, "Angular route focal expectation missing")

# SPA keeps management, validation/test and publication distinct and fail-closed.
for source, name in ((page, "primary page"), (editor, "editor")):
    require("['Integrations.ManageMappings', 'CanManageAch']" in source, f"{name} mapping management capability missing")
    require("['Integrations.Validate', 'CanManageAch']" in source, f"{name} validation capability missing")
    require("['Integrations.Publish', 'CanManageAch']" in source, f"{name} publish capability missing")
    require("hasRole(" not in source, f"{name} must not hardcode role checks")
require("if (!this.canValidate) return;" in page, "primary preview refresh must fail closed")
require("if (!this.canPublish) return;" in page, "publication path must fail closed")
require("if (!this.canManage || this.store.workspace()?.mappingSet.status !== 'Draft')" in page, "primary mapping edit must fail closed")
require("!this.canSaveParameter()" in page, "primary parameter save must fail closed")
require("if (!this.canManage || !this.mappingSet || !this.selectedParameterId || this.savingRule) return;" in editor, "editor rule save must fail closed")
require("if (!this.canValidate || !this.mappingSet || this.validating) return;" in editor, "editor validate must fail closed")
require("if (!this.canValidate || !this.mappingSet || this.previewing) return;" in editor, "editor preview must fail closed")
require("if (!this.canPublish || !this.mappingSet || this.publishing) return;" in editor, "editor publication handoff must fail closed")
require("if (!this.canManage || !this.mappingSet || this.cloning) return;" in editor, "editor clone must fail closed")
for token in ('*ngIf="canPublish && workspace.mappingSet.status === \'Draft\'"', '*ngIf="canValidate"', '*ngIf="canManage && workspace.mappingSet.status === \'Draft\'"'):
    require(token in page_template, f"primary visual capability gate missing: {token}")
for token in ('*ngIf="canValidate"', '*ngIf="canPublish"', '*ngIf="canManage"'):
    require(token in editor_template, f"editor visual capability gate missing: {token}")

# Preview is not called automatically for a read-only principal.
require("private previewAuthorized = false;" in store, "preview authorization state must default fail-closed")
require("load(requestedMethodCode?: string | null, previewAuthorized = false)" in store, "load must receive explicit preview authorization")
require("if (!this.previewAuthorized)" in store, "store preview service call must fail closed")
require("this.previewStatusSignal.set('forbidden')" in store, "store must expose denied preview state without calling API")
require("this.store.load(this.route.snapshot.queryParamMap.get('method'), this.canValidate);" in page, "component must pass validation capability into store")

# API read vs manage vs validate/test vs publish.
def action(marker: str, next_marker: str | None) -> str:
    start = controller.find(marker)
    require(start >= 0, f"controller action marker missing: {marker}")
    if next_marker is None:
        return controller[start:]
    end = controller.find(next_marker, start + len(marker))
    require(end > start, f"next controller marker missing after {marker}: {next_marker}")
    return controller[start:end]

for marker, next_marker in (
    ('[HttpGet]\n', '[HttpGet("published")]'),
    ('[HttpGet("published")]\n', '[HttpGet("{id:guid}")]'),
    ('[HttpGet("{id:guid}")]\n', '[HttpGet("{id:guid}/workspace")]'),
    ('[HttpGet("{id:guid}/workspace")]\n', '[HttpPost]\n'),
    ('[HttpGet("{id:guid}/history")]\n', '[HttpPost("compare")]'),
):
    require('[Authorize(Policy = P1Policies.IntegrationsRead)]' in action(marker, next_marker), f"mapping read must use P1.IntegrationsRead after NAV3.4R: {marker}")

for marker, next_marker in (
    ('[HttpPost]\n', '[HttpPut("{id:guid}")]'),
    ('[HttpPut("{id:guid}")]\n', '[HttpPut("{id:guid}/rules")]'),
    ('[HttpPut("{id:guid}/rules")]\n', '[HttpPut("{id:guid}/parameters/{parameterId:long}")]'),
    ('[HttpPut("{id:guid}/parameters/{parameterId:long}")]\n', '[HttpPost("{id:guid}/validate")]'),
    ('[HttpPost("{id:guid}/clone")]\n', '[HttpPost("{id:guid}/clone-to-contract-version")]'),
):
    require('[Authorize(Policy = P1Policies.IntegrationMappingsManage)]' in action(marker, next_marker), f"mapping mutation must use ManageMappings P1: {marker}")
for marker, next_marker in (
    ('[HttpPost("{id:guid}/validate")]\n', '[HttpPost("{id:guid}/preview")]'),
    ('[HttpPost("{id:guid}/preview")]\n', '[HttpPost("{id:guid}/publish")]'),
):
    require('[Authorize(Policy = P1Policies.IntegrationMappingsValidate)]' in action(marker, next_marker), f"validation/test must use Validate P1: {marker}")
require('[Authorize(Policy = P1Policies.IntegrationMappingsPublish)]' in action('[HttpPost("{id:guid}/publish")]\n', '[HttpPost("{id:guid}/clone")]'), "publish must use Publish P1")
for marker, next_marker in (
    ('[HttpPost("{id:guid}/clone-to-contract-version")]\n', '[HttpPost("{id:guid}/validate-contract-mapping")]'),
    ('[HttpPost("{id:guid}/validate-contract-mapping")]\n', '[HttpGet("{id:guid}/history")]'),
    ('[HttpPost("compare")]\n', None),
):
    require('[Authorize(Policy = "CanManageAch")]' in action(marker, next_marker), f"unconsumed/cross-surface action changed: {marker}")
require("[HttpDelete" not in controller, "Relación de información has no delete endpoint")

# NAV3.4R adopts P1.IntegrationsRead for shared integration-domain catalog reads.
for marker in ('[HttpGet("methods")]', '[HttpGet("methods/{methodId:int}/parameters")]', '[HttpGet("source-catalog")]', '[HttpGet("transformations")]'):
    pos = catalog.find(marker)
    require(pos >= 0, f"shared catalog endpoint missing: {marker}")
    block = catalog[pos:pos+900]
    require('[Authorize(Policy = P1Policies.IntegrationsRead)]' in block, f"shared catalog read must use P1.IntegrationsRead after NAV3.4R: {marker}")

# Persistence/effects: rules/defaults/transforms mutate configuration, validation persists a summary, publication affects future runtime.
for token in ("FixedValue", "DefaultValue", "TransformationCode", "FormatMask", "Priority", "RequiredOverride", "Enabled", "ConditionExpression"):
    require(token in dtos, f"mapping rule field not characterized: {token}")
require("ValidationSummaryJson = JsonSerializer.Serialize" in validation and "SaveChangesAsync" in validation, "validation persistence not characterized")
for token in ("set.Status = IntegrationMappingSetStatusEnum.Published", "published.IsActive = false", "set.IsActive = true", "set.PublishedAtUtc", "set.PublishedBy"):
    require(token in service, f"publication state change missing: {token}")
require("IntegrationMappingSetStatusEnum.Published" in resolver and "IsActive" in resolver, "future productive runtime must select active published mapping")

# Preview can use controlled or DB transaction samples, but is sanitized and does not call external SOAP/HTTP transport.
for token in ("SensitivePathFragments", "SecretValueMarkers", "SanitizePreviewValue", "useControlledSample"):
    require(token.lower() in preview.lower(), f"preview sensitivity behavior missing: {token}")
for token in ("PreviewValue", "PayloadPreviewJson", "RawPreviewJson", "IsMasked"):
    require(token in dtos, f"sanitized preview DTO field missing: {token}")
for source, name in ((service, "mapping service"), (validation, "validation service"), (preview, "preview service")):
    for forbidden in ("HttpClient", "SftpClient", "IWscfaachSoapClient"):
        require(forbidden not in source, f"{name} must not invoke external transport directly: {forbidden}")

# Focal tests exist for the important boundaries.
for token in (
    "MappingCapabilities_ArePreExistingDistinctCapabilities_AndSeederPersistsThemIdempotentlyWithoutGrants",
    "InformationRelationshipMenu_UsesCanReadAch_PreservesCanonicalStructureAndAdminAudience_AndAddsNoGrants",
    "MappingP1Policies_PreserveLegacyCanManageAchFallback_AndKeepManageValidatePublishSeparate",
    "MappingApi_SeparatesReadManageValidateAndPublish_WhileLeavingUnconsumedCrossContractActionsLegacy",
    "MappingSurface_PersistsRulesValidationAndPublication_ForFutureRuntimeButDoesNotInvokeExternalTransportDirectly",
):
    require(token in focal_test, f"C# focal test missing: {token}")

# Byte-level regression guards for NAV3.4N structure and excluded sibling surfaces.
expected_hashes = {
    "src/Cfa.ACHInterbank.Persistence/Security/Seeders/ConnectionsStructuralMenuAuthorizationSeeder.cs": "e47aa6009b1a3bdb8724e48c9489f93f9aed00210441422b570ff6bcaf40b2f1",
    "web/ach-interbank-ui/src/app/core/services/navigation.service.ts": "f8b984005ca4f0b99adb3abccd4e0c2e46da5ffc6f8e9a9f9d09ccf84bd2a3de",
    "src/Cfa.ACHInterbank.Application/Navigation/Queries/GetMenuForCurrentUserHandler.cs": "f111e416c181fc1912c297e37012cf7462f49d0434c1ef6ec9416c89b5ed1a61",
    "web/ach-interbank-ui/src/app/app-routing.module.ts": "14304a5b4db288552a401d71f62d77f0e5c1837bae5e0a4cb0d322a5a8ec75af",
    "src/Cfa.ACHInterbank.Persistence/Security/Seeders/IntegrationContractPermissionSeeder.cs": "a5462711598c057621f723aa8ba684cd99a635c37d041a50ccaa1c24f00e0b4a",
    "src/Cfa.ACHInterbank.Persistence/Security/Seeders/IntegrationContractMenuAuthorizationSeeder.cs": "9539afd6956992892c89551d8c1ff55f0c6b9ca874ceae720ad999e970951f6c",
    "web/ach-interbank-ui/src/app/features/integrations/pages/contract-versions-page.component.ts": "1155f4f312ae789b8ec1c700ce1ca6a722b9f989eb9df481bd4e1a8b9f9874a7",
    "src/Cfa.ACHInterbank.Api/Controllers/IntegrationContractsController.cs": "5fcf0a4f281304dde87733ca6579d38c0bc65ebb5c2b7135014545ccb25fefbd",
    "web/ach-interbank-ui/src/app/features/integrations/pages/soap-integration-settings.component.ts": "6797692f41f0aca7f1f8b8c3c6d6388f136544e2f62aca898c4616a5d34f3947",
    "src/Cfa.ACHInterbank.Api/Controllers/SoapIntegrationSettingsController.cs": "6f1b17cef092cb5d5476d30febcde65b73ee1d06963b6f5c630ebdf4236acc66",
    "src/Cfa.ACHInterbank.Persistence/Security/Seeders/MftAdministrationMenuAuthorizationSeeder.cs": "d2e27ad8e2bc70c4e56d76a68d4fd638e23a2b53f5b3f7147361838a9945559a",
    "web/ach-interbank-ui/src/app/features/ach-colombia-mft-administration/ach-colombia-mft-administration.component.ts": "dac1bf2b458ce23c74d49758a66f931c583cf3410a175ccd451094d9d08e0499",
    "src/Cfa.ACHInterbank.Api/Controllers/AchColombiaFileExchangeController.cs": "6ec050055e13ad74403bc9167787924c65784f83ee4a8f51d39b0203ccad9bb2",
}
for path, expected in expected_hashes.items():
    require(sha(path) == expected, f"out-of-scope/closed surface changed: {path}")

print(f"NAV3.4P information-relationship static validation PASS ({checks} assertions)")
