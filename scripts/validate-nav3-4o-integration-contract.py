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
permission_seeder = read("src/Cfa.ACHInterbank.Persistence/Security/Seeders/IntegrationContractPermissionSeeder.cs")
menu_seeder = read("src/Cfa.ACHInterbank.Persistence/Security/Seeders/IntegrationContractMenuAuthorizationSeeder.cs")
ia = read("src/Cfa.ACHInterbank.Persistence/Navigation/Seeders/NavigationInformationArchitectureSeeder.cs")
routes = read("web/ach-interbank-ui/src/app/features/integrations/integrations.routes.ts")
store = read("web/ach-interbank-ui/src/app/features/integrations/state/contract-workspace.store.ts")
page = read("web/ach-interbank-ui/src/app/features/integrations/pages/contract-versions-page.component.ts")
page_template = read("web/ach-interbank-ui/src/app/features/integrations/pages/contract-versions-page.component.html")
stepper = read("web/ach-interbank-ui/src/app/features/integrations/pages/contract-import-stepper.component.ts")
stepper_template = read("web/ach-interbank-ui/src/app/features/integrations/pages/contract-import-stepper.component.html")
contract_api = read("web/ach-interbank-ui/src/app/features/integrations/data-access/integration-contract.api.ts")
workspace_api = read("web/ach-interbank-ui/src/app/features/integrations/data-access/integration-workspace.api.ts")
mapping_api = read("web/ach-interbank-ui/src/app/features/integrations/data-access/integration-mapping.api.ts")
controller = read("src/Cfa.ACHInterbank.Api/Controllers/IntegrationContractsController.cs")
catalog_controller = read("src/Cfa.ACHInterbank.Api/Controllers/IntegrationCatalogController.cs")
mapping_controller = read("src/Cfa.ACHInterbank.Api/Controllers/IntegrationMappingSetsController.cs")
analyzer = read("src/Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationContractArtifactAnalyzer.cs")
importer = read("src/Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationContractArtifactImportService.cs")
version_service = read("src/Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationContractVersionService.cs")
contract_dtos = read("src/Cfa.ACHInterbank.Application/Integrations/Dtos/IntegrationContractVersionAdministrationDtos.cs")
artifact_dtos = read("src/Cfa.ACHInterbank.Application/Integrations/Dtos/IntegrationContractArtifactDtos.cs")
catalog_dtos = read("src/Cfa.ACHInterbank.Application/Integrations/Dtos/IntegrationCatalogDtos.cs")
app_routing = read("web/ach-interbank-ui/src/app/app-routing.module.ts")
focal_test = read("tests/Cfa.ACHInterbank.Tests/NavigationMenuNav34OIntegrationContractAuthorizationTests.cs")
route_spec = read("web/ach-interbank-ui/src/app/features/integrations/integrations.routes.spec.ts")
store_spec = read("web/ach-interbank-ui/src/app/features/integrations/state/contract-workspace.store.spec.ts")
stepper_spec = read("web/ach-interbank-ui/src/app/features/integrations/pages/contract-import-stepper.component.spec.ts")

# Canonical NAV2 identity remains unchanged.
for token in (
    'private const string ContractRoute = "/integraciones/contrato";',
    '"Contrato del servicio"',
    'connections.Id',
    'order: 10',
    'icon: "description"',
    'exact: true',
):
    require(token in ia, f"canonical contract menu structure changed/missing: {token}")
contract_block = between(ia, "var contract = await UpsertAsync(", "var informationRelationship = await UpsertAsync(")
require("ContractRoute" in contract_block, "contract route must still be reconciled by NAV2 IA")
require('"Contrato del servicio"' in contract_block, "contract label must remain canonical")
require("connections.Id" in contract_block, "contract parent must remain Conexiones")
require("order: 10" in contract_block, "contract order must remain 10")

# New management capability is one focused semantic capability, persisted without grants.
require('public const string ManageContracts = "Integrations.ManageContracts";' in fine, "dedicated ManageContracts capability missing")
require("Integrations.Read, Integrations.ManageContracts, Integrations.ManageConnections, Integrations.ManageMappings" in fine, "ManageContracts must be present in AllPermissions")
require('public const string IntegrationContractsManage = "P1.IntegrationContractsManage";' in p1, "P1 contract management policy constant missing")
for token in (
    "FineGrainedPermissions.Integrations.ManageContracts",
    'ctx.User.HasClaim("permission", "CanManageAch")',
):
    require(token in external, f"P1 IntegrationContractsManage compatibility policy missing: {token}")
policy_block = between(external, "options.AddPolicy(P1Policies.IntegrationContractsManage", "options.AddPolicy(P1Policies.IntegrationMappingsManage")
require("Integrations.ManageContracts" in policy_block, "contract management fine capability missing from policy")
require('"CanManageAch"' in policy_block, "legacy management fallback missing from policy")
for forbidden in ("CanReadAch", "Integrations.Read", "Integrations.ManageMappings", "Integrations.Validate", "Integrations.Publish", "Integrations.Compare"):
    require(forbidden not in policy_block, f"management policy must not be satisfied by {forbidden}")

require("ManageContractsPermissionId" in permission_seeder, "permission seeder stable id missing")
require('FineGrainedPermissions.Integrations.ManageContracts' in permission_seeder, "permission seeder must persist ManageContracts")
require('"Administrar contratos de integración"' in permission_seeder, "permission description missing")
require("RolePermissions" not in permission_seeder, "permission seeder must not create role grants")
require("RoleId" not in permission_seeder, "permission seeder must not assign roles")
require("User" not in permission_seeder.replace("Domain.Entities.User", ""), "permission seeder must not grant directly to users")

# Menu entry normalizes unrelated CanManageUsers to CanReadAch, while preserving existing Admin audience.
require('AuthorizationReconciliationOrder = 115' in menu_seeder, "contract menu reconciliation must follow NAV3.4N")
require('Route = "/integraciones/contrato"' in menu_seeder, "menu seeder must target only contract route")
require("PermissionConfiguration.ReadAchPermissionId" in menu_seeder, "contract menu must use CanReadAch entry boundary")
require('"CanReadAch"' in menu_seeder, "CanReadAch semantic check missing")
require("ManageUsersPermissionId" not in menu_seeder, "contract menu must remove legacy CanManageUsers dependency")
require("MenuItemRoles" not in menu_seeder, "contract menu seeder must preserve, not rewrite, existing Admin role audience")
require("RolePermissions" not in menu_seeder, "contract menu seeder must not add grants")
require("context.Permissions.Add" not in menu_seeder and "new Permission" not in menu_seeder, "menu seeder must not create capabilities")

# Child SPA now states read authorization explicitly; parent route remains unchanged.
contract_route = between(routes, "path: 'contrato'", "path: 'relacion-informacion'")
require("canActivate: [permissionGuard]" in contract_route, "contract child route must have explicit permission guard")
require("permissions: ['Integrations.Read', 'CanReadAch']" in contract_route, "contract child route must require Integrations.Read or legacy CanReadAch after NAV3.4R")
require("roles:" not in contract_route, "contract child route must not add a child role gate")
parent_route = between(app_routing, "path: 'integraciones'", "path: 'navigation'")
require("roles: ['Admin', 'ACH.Operator']" in parent_route, "shared /integraciones parent role guard must remain unchanged")
require("permissions: ['Integrations.Read', 'CanReadAch', 'CanManageAch']" in parent_route, "shared /integraciones parent must preserve legacy alternatives and admit Integrations.Read")

# Store keeps read prerequisite and introduces one management alternative; no role-name checks.
permissions_block = between(store, "readonly permissions = computed<IntegrationPermissionsState>", "readonly activeContract")
require("canRead: permissions.includes('Integrations.Read') || permissions.includes('CanReadAch')" in permissions_block, "screen read must use Integrations.Read or legacy CanReadAch after NAV3.4R")
require("permissions.includes('Integrations.ManageContracts')" in permissions_block, "fine contract management missing in SPA store")
require("permissions.includes('CanManageAch')" in permissions_block, "legacy CanManageAch fallback missing in SPA store")
for forbidden in ("hasRole", "Admin", "ACH.Operator"):
    require(forbidden not in permissions_block, f"SPA permission model must not hardcode role {forbidden}")

# Actual page operations are characterized: read/list/detail, metadata edit, local analyze/preview/import only.
for token in (
    "this.workspaceApi.getMethods()",
    "this.api.getByMethod(method.id)",
    "this.mappingApi.getByMethod(method.id)",
    "this.api.getById(contractVersionId)",
    "this.api.updateParameterPresentation",
    "this.api.analyze(",
    "this.api.preview(",
    "this.api.import(",
):
    require(token in store, f"expected contract workspace operation missing: {token}")
for forbidden in (
    "this.api.activate(",
    "this.api.rollback(",
    "this.api.create(",
    "this.api.clone(",
    "this.api.compare(",
):
    require(forbidden not in store, f"out-of-scope lifecycle operation must not be consumed by Contracto page: {forbidden}")

# Gating is both visual and fail-closed inside mutable methods.
require("this.store.permissions().canManage" in page, "page management capability check missing")
require("if (!this.store.permissions().canManage || this.store.methodId() == null) return;" in page, "beginImport must fail closed")
require("if (this.functionalMetadataForm.invalid || !this.canEditCurrentContract())" in page, "metadata save must fail closed")
require("@if (store.permissions().canManage && store.methodId() != null && !showImportFlow())" in page_template, "import UI must be visually gated")
require("@if (canEditCurrentContract() && editingParameterId() !== parameter.parameterId)" in page_template, "metadata edit UI must be visually gated")
for method in ("analyzeSelectedFile", "validateOperation", "previewDiff", "importDraft"):
    marker = f"{method}(): void {{"
    idx = stepper.find(marker)
    require(idx >= 0, f"stepper method missing: {method}")
    body = stepper[idx: stepper.find("\n  }", idx) + 4]
    require("if (!this.store.permissions().canManage) return;" in body, f"{method} must fail closed before service call")
for disabled in (
    '!store.permissions().canManage || !store.artifactFile()',
    '!store.permissions().canManage || operationForm.controls.operationName.invalid',
    '!store.permissions().canManage || !analysisValid() || !selectedBaseContractId()',
    '!store.permissions().canManage || importForm.invalid || !diffReviewed()',
):
    require(disabled in stepper_template, f"stepper visual gating missing: {disabled}")

# Exact HTTP endpoints consumed by the target screen.
for token in (
    "'api/integrations/methods'",
    '`api/integrations/methods/${methodId}/contracts`',
    '`api/integrations/contracts/${contractVersionId}`',
    '`api/integrations/contracts/${contractVersionId}/parameters/${parameterId}/presentation`',
    "'api/integrations/contracts/analyze'",
    '`api/integrations/methods/${methodId}/contracts/preview/${baseContractVersionId}`',
    '`api/integrations/methods/${methodId}/contracts/import`',
    '`api/integrations/mappingsets${query}`',
):
    source = workspace_api + contract_api + mapping_api
    require(token in source, f"target consumed endpoint missing: {token}")

# NAV3.4R normalizes integration-domain reads to P1.IntegrationsRead while retaining CanReadAch fallback in the policy.
require('[HttpGet("methods")]' in catalog_controller and '[Authorize(Policy = P1Policies.IntegrationsRead)]' in catalog_controller, "shared method catalog read must use P1.IntegrationsRead")
map_get = between(mapping_controller, "[HttpGet]", '[HttpGet("published")]')
require('[Authorize(Policy = P1Policies.IntegrationsRead)]' in map_get, "shared mapping-list read must use P1.IntegrationsRead")

# Target controller: GETs remain read; only mutations actually consumed by this page use dedicated management policy.
def action(source: str, method_marker: str, next_marker: str | None) -> str:
    start = source.find(method_marker)
    require(start >= 0, f"action marker missing: {method_marker}")
    if next_marker is None:
        return source[start:]
    end = source.find(next_marker, start + len(method_marker))
    require(end > start, f"next action marker missing after {method_marker}: {next_marker}")
    return source[start:end]

get_list = action(controller, '[HttpGet("methods/{methodId:int}/contracts")]', '[EndpointSummary("Consultar detalle de versión contractual")]')
get_detail = action(controller, '[HttpGet("contracts/{contractVersionId:guid}")]', '[EndpointSummary("Crear versión contractual manual")]')
update = action(controller, '[HttpPut("contracts/{contractVersionId:guid}/parameters/{parameterId:long}/presentation")]', '[EndpointSummary("Comparar versiones contractuales")]')
analyze = action(controller, '[HttpPost("contracts/analyze")]', '[EndpointSummary("Previsualizar diff de WSDL/XSD contra versión base")]')
preview = action(controller, '[HttpPost("methods/{methodId:int}/contracts/preview/{baseContractVersionId:guid}")]', '[EndpointSummary("Importar WSDL/XSD local a Draft")]')
import_action = action(controller, '[HttpPost("methods/{methodId:int}/contracts/import")]', None)
for name, block in (("list", get_list), ("detail", get_detail)):
    require('[Authorize(Policy = P1Policies.IntegrationsRead)]' in block, f"contract {name} GET must use P1.IntegrationsRead after NAV3.4R")
for name, block in (("update", update), ("analyze", analyze), ("preview", preview), ("import", import_action)):
    require('[Authorize(Policy = P1Policies.IntegrationContractsManage)]' in block, f"contract {name} must use dedicated management policy")
    require('CanReadAch' not in block, f"read permission must not authorize contract {name}")

# Unconsumed lifecycle endpoints remain unchanged/out of scope.
for summary in (
    "Crear versión contractual manual",
    "Clonar versión contractual",
    "Activar ContractVersion + MappingSet de forma atómica",
    "Rollback contractual no destructivo",
):
    idx = controller.find(f'[EndpointSummary("{summary}")]')
    require(idx >= 0, f"lifecycle endpoint missing: {summary}")
    block = controller[idx:controller.find("public async Task<IActionResult>", idx)]
    require('[Authorize(Policy = "CanManageAch")]' in block, f"unconsumed lifecycle policy changed: {summary}")
compare_idx = controller.find('[EndpointSummary("Comparar versiones contractuales")]')
compare_block = controller[compare_idx:controller.find("public async Task<IActionResult>", compare_idx)]
require('[Authorize(Policy = P1Policies.IntegrationsRead)]' in compare_block, "contract compare read must use P1.IntegrationsRead after NAV3.4R")

# Contract data/technical sensitivity: metadata is exposed, credentials/secrets are not part of browser DTO surface.
dto_surface = contract_dtos + artifact_dtos + catalog_dtos
for token in (
    "OperationName",
    "TargetNamespace",
    "SoapAction",
    "SourceDocumentName",
    "SourceDocumentHash",
    "SourceReference",
    "XmlElementName",
    "XsdType",
):
    require(token in dto_surface, f"contract metadata field not characterized: {token}")
for secret in (
    "Password",
    "ProtectedCredential",
    "ClientSecret",
    "AccessToken",
    "RefreshToken",
    "PrivateKey",
    "SecretReference",
):
    require(secret.lower() not in dto_surface.lower(), f"contract DTO surface must not expose secret field: {secret}")
require("Endpoint" not in contract_dtos and "Endpoint" not in artifact_dtos, "contract DTO must not expose participant connection endpoint")

# Local WSDL/XSD analysis is fail-closed and performs no external reference resolution/network I/O.
for token in (
    "DtdProcessing = DtdProcessing.Prohibit",
    "XmlResolver = null",
    "MaxCharactersFromEntities = 0",
    "ValidateNoExternalReferences",
    '"EXTERNAL_REFERENCE_NOT_ALLOWED"',
    '"wsdl:import no está permitido en análisis local fail-closed',
    'XSD no está permitido en análisis local fail-closed',
):
    require(token in analyzer, f"secure local analyzer behavior missing: {token}")
for forbidden in ("HttpClient", "SftpClient", "IWscfaachSoapClient", "WebRequest", "DownloadString", "SendAsync"):
    require(forbidden not in analyzer, f"artifact analyzer must not perform external transport: {forbidden}")

# Preview is read-only; import persists a new Draft and never activates/publishes from this screen.
require("CompareArtifactAsync" in importer, "preview must compare local analysis against persisted base")
preview_method = between(importer, "public async Task<IntegrationContractArtifactPreviewDto> PreviewDraftAsync", "public async Task<IntegrationContractArtifactImportResultDto> ImportDraftAsync")
require("SaveChangesAsync" not in preview_method, "preview must not persist")
for token in (
    "Status = IntegrationContractVersionStatusEnum.Draft",
    'SourceReference = "local-upload"',
    "_context.IntegrationContractVersions.Add(contract)",
    "await _context.SaveChangesAsync(ct)",
    "detail.Contract.Status != IntegrationContractVersionStatusEnum.Draft",
):
    require(token in importer, f"Draft-only import persistence missing: {token}")
for forbidden in ("ActivateAsync", "MarkValidatedAsync", "Publish", "HttpClient", "SftpClient", "IWscfaachSoapClient"):
    require(forbidden not in importer, f"import request must not activate/publish/invoke external transport: {forbidden}")

# Presentation edit mutates functional metadata only and advances revision; technical fields stay untouched.
presentation = between(version_service, "public async Task<IntegrationContractParameterPresentationResultDto> UpdateParameterPresentationAsync", "public async Task<IntegrationContractDiffDto> CompareAsync")
for token in ("parameter.DisplayName", "parameter.DescriptionEs", "parameter.UiHelpText", "parameter.Category", "parameter.ExampleValue", "contract.Revision = checked(contract.Revision + 1)", "SaveChangesAsync"):
    require(token in presentation, f"functional metadata mutation missing: {token}")
for forbidden in ("XmlElementName =", "XsdType =", "DataType =", "SoapAction =", "TargetNamespace =", "OperationName ="):
    require(forbidden not in presentation, f"functional metadata edit must not mutate technical field: {forbidden}")

# Focal tests cover authorization, capabilities, local effects, no secrets, and UI fail-closed behavior.
for token in (
    "ManageContracts_IsDedicatedCapability_AndSeederPersistsItIdempotentlyWithoutGrants",
    "ContractMenu_UsesCanReadAch_PreservesCanonicalStructureAndAdminAudience_AndAddsNoGrants",
    "ContractMenu_DirectBoundary_RemainsAdminAudienceButNoLongerDependsOnCanManageUsers",
    "ContractManagementPolicy_AllowsDedicatedOrLegacyManagement_ButNotReadCapabilities",
    "ContractPageConsumedApi_SeparatesReadAndManagement_WithoutChangingOtherLifecycleActions",
    "ContractSurface_UsesLocalArtifactAnalysis_DraftPersistence_AndDoesNotExposeSecretsOrInvokeExternalTransport",
):
    require(token in focal_test, f"C# focal test missing: {token}")
require("protege explícitamente Contrato del servicio con Integrations.Read o CanReadAch" in route_spec, "Angular route focal test missing")
require("acepta Integrations.ManageContracts" in store_spec, "Angular fine management store test missing")
require("Integrations.ManageContracts por sí sola" in store_spec, "Angular read/manage separation test missing")
require("falla cerrado dentro de los métodos mutables" in stepper_spec, "Angular method-level fail-closed test missing")

# Byte-level regression guard for explicitly excluded/closed surfaces and NAV3.4N structural pieces.
expected_hashes = {
    "src/Cfa.ACHInterbank.Persistence/Security/Seeders/ConnectionsStructuralMenuAuthorizationSeeder.cs": "e47aa6009b1a3bdb8724e48c9489f93f9aed00210441422b570ff6bcaf40b2f1",
    "web/ach-interbank-ui/src/app/core/services/navigation.service.ts": "f8b984005ca4f0b99adb3abccd4e0c2e46da5ffc6f8e9a9f9d09ccf84bd2a3de",
    "src/Cfa.ACHInterbank.Application/Navigation/Queries/GetMenuForCurrentUserHandler.cs": "f111e416c181fc1912c297e37012cf7462f49d0434c1ef6ec9416c89b5ed1a61",
    "src/Cfa.ACHInterbank.Persistence/Security/Seeders/MftAdministrationMenuAuthorizationSeeder.cs": "d2e27ad8e2bc70c4e56d76a68d4fd638e23a2b53f5b3f7147361838a9945559a",
    "web/ach-interbank-ui/src/app/features/ach-colombia-mft-administration/ach-colombia-mft-administration.component.ts": "dac1bf2b458ce23c74d49758a66f931c583cf3410a175ccd451094d9d08e0499",
    "src/Cfa.ACHInterbank.Api/Controllers/AchColombiaFileExchangeController.cs": "6ec050055e13ad74403bc9167787924c65784f83ee4a8f51d39b0203ccad9bb2",
    "web/ach-interbank-ui/src/app/features/integrations/pages/mapping-sets-page.component.ts": "ca463dc88f03ba8f0160f607f8854b0939dd5765356ff271419f7d330ac5726a",
    "web/ach-interbank-ui/src/app/features/integrations/pages/soap-integration-settings.component.ts": "6797692f41f0aca7f1f8b8c3c6d6388f136544e2f62aca898c4616a5d34f3947",
    "src/Cfa.ACHInterbank.Api/Controllers/IntegrationMappingSetsController.cs": "32ba2302fdb6f48d15135a3e9afdd48b8dd8b182076c6e07feb085e672a2fa91",
    "src/Cfa.ACHInterbank.Api/Controllers/SoapIntegrationSettingsController.cs": "6f1b17cef092cb5d5476d30febcde65b73ee1d06963b6f5c630ebdf4236acc66",
    "web/ach-interbank-ui/src/app/app-routing.module.ts": "14304a5b4db288552a401d71f62d77f0e5c1837bae5e0a4cb0d322a5a8ec75af",
}
for path, expected in expected_hashes.items():
    require(sha(path) == expected, f"out-of-scope NAV3.4N/MFT/sibling surface changed: {path}")

print(f"NAV3.4O integration-contract static validation PASS ({checks} assertions)")
