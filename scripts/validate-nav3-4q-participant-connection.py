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
perm_seeder = read("src/Cfa.ACHInterbank.Persistence/Security/Seeders/IntegrationConnectionPermissionSeeder.cs")
menu_seeder = read("src/Cfa.ACHInterbank.Persistence/Security/Seeders/ParticipantConnectionMenuAuthorizationSeeder.cs")
ia = read("src/Cfa.ACHInterbank.Persistence/Navigation/Seeders/NavigationInformationArchitectureSeeder.cs")
routes = read("web/ach-interbank-ui/src/app/features/integrations/integrations.routes.ts")
route_spec = read("web/ach-interbank-ui/src/app/features/integrations/integrations.routes.spec.ts")
page = read("web/ach-interbank-ui/src/app/features/integrations/pages/soap-integration-settings.component.ts")
template = read("web/ach-interbank-ui/src/app/features/integrations/pages/soap-integration-settings.component.html")
page_spec = read("web/ach-interbank-ui/src/app/features/integrations/pages/soap-integration-settings.component.spec.ts")
api_adapter = read("web/ach-interbank-ui/src/app/features/integrations/data-access/integration-connection.api.ts")
controller = read("src/Cfa.ACHInterbank.Api/Controllers/SoapIntegrationSettingsController.cs")
settings_service = read("src/Cfa.ACHInterbank.Persistence/Security/Services/SoapIntegrationSettingsService.cs")
settings_dto = read("src/Cfa.ACHInterbank.Application/Security/Dtos/SoapIntegrationSettingsDto.cs")
mapping_dto = read("src/Cfa.ACHInterbank.Application/Security/Dtos/SoapEndpointMethodMappingDto.cs")
input_dto = read("src/Cfa.ACHInterbank.Application/Security/Dtos/SoapInputParameterMappingDto.cs")
entity = read("src/Cfa.ACHInterbank.Domain/Entities/User/SoapIntegrationSetting.cs")
wsc_client = read("src/Cfa.ACHInterbank.External/Connections/WscfaachSoapClient.cs")
wsaxon_client = read("src/Cfa.ACHInterbank.External/Connections/WsAxonRespuestaTransaccionesSoapClient.cs")
endpoint_options = read("src/Cfa.ACHInterbank.Application/Security/Dtos/WsAxonEndpointSecurityOptions.cs")
transport = read("src/Cfa.ACHInterbank.External/Connections/ControlledLocalSoapTransportResolver.cs")
appsettings = read("src/Cfa.ACHInterbank.Api/appsettings.json")
devsettings = read("src/Cfa.ACHInterbank.Api/appsettings.Development.json")
focal_test = read("tests/Cfa.ACHInterbank.Tests/NavigationMenuNav34QParticipantConnectionAuthorizationTests.cs")

# Canonical NAV2 structure.
connection = between(ia, "var participantConnection = await UpsertAsync(", "// Preserve the existing MFT route")
for token in ('ParticipantConnectionRoute', '"Conexión entidad participante"', 'connections.Id', 'order: 30', 'icon: "link"', 'exact: true'):
    require(token in connection, f"canonical connection structure changed/missing: {token}")
require('private const string ParticipantConnectionRoute = "/integraciones/conexion";' in ia, "canonical route changed")

# Dedicated management capability and P1 fallback, no grants.
for token in (
    'public const string Read = "Integrations.Read";',
    'public const string ManageConnections = "Integrations.ManageConnections";',
    'public const string ManageContracts = "Integrations.ManageContracts";',
    'public const string ManageMappings = "Integrations.ManageMappings";',
    'public const string Validate = "Integrations.Validate";',
    'public const string Publish = "Integrations.Publish";',
):
    require(token in fine, f"integration capability missing: {token}")
require('Integrations.ManageConnections' in between(fine, 'public static IReadOnlyList<string> AllPermissions', '];'), "ManageConnections missing from AllPermissions")
require('public const string IntegrationConnectionsManage = "P1.IntegrationConnectionsManage";' in p1, "connection P1 constant missing")
policy_block = between(external, 'options.AddPolicy(P1Policies.IntegrationConnectionsManage', 'options.AddPolicy(P1Policies.IntegrationMappingsManage')
require('FineGrainedPermissions.Integrations.ManageConnections' in policy_block, "fine connection capability missing from P1")
require('"CanManageAch"' in policy_block, "CanManageAch fallback missing from P1")
for forbidden in ('"CanReadAch"', 'FineGrainedPermissions.Config.Manage', 'FineGrainedPermissions.Integrations.Validate', 'FineGrainedPermissions.Integrations.Publish'):
    require(forbidden not in policy_block, f"unrelated capability satisfies connection management: {forbidden}")
for token in (
    '[Scoped]',
    'IntegrationConnectionPermissionSeeder',
    '289e92ea-abd4-43a9-b93a-23b06fb03362',
    'FineGrainedPermissions.Integrations.ManageConnections',
    'Administrar conexiones de integración',
):
    require(token in perm_seeder, f"connection permission seeder missing: {token}")
for forbidden in ('RolePermission', 'RoleId', 'UserPermission', 'UserId'):
    require(forbidden not in perm_seeder, f"connection capability seeder must not grant: {forbidden}")

# Menu alignment: preserve Admin audience and change only permission to CanReadAch.
for token in (
    'AuthorizationReconciliationOrder = 117',
    'Route = "/integraciones/conexion"',
    'PermissionConfiguration.ReadAchPermissionId',
    '"CanReadAch"',
):
    require(token in menu_seeder, f"connection menu seeder missing: {token}")
require('MenuItemRoles' not in menu_seeder, "NAV3.4Q must preserve historical Admin role link rather than reconcile roles")
require('ManageUsersPermissionId' not in menu_seeder, "legacy CanManageUsers must not remain as target menu permission")
require('InformationRelationshipMenuAuthorizationSeeder.AuthorizationReconciliationOrder < seeder.Order' in focal_test, "menu reconciler order test missing")

# SPA child has explicit read boundary. Parent route is intentionally not touched by this validator.
connection_route = between(routes, "path: 'conexion'", "loadComponent:")
require('canActivate: [permissionGuard]' in connection_route, "connection child guard missing")
require("permissions: ['Integrations.Read', 'CanReadAch']" in connection_route, "connection child must use Integrations.Read or legacy CanReadAch after NAV3.4R")
require("readonly canManage = this.auth.hasPermission(['Integrations.ManageConnections', 'CanManageAch']);" in page, "SPA management capability/fallback incorrect")
require("readonly canRead = this.auth.hasPermission(['Integrations.Read', 'CanReadAch']);" in page, "SPA read must use Integrations.Read or legacy CanReadAch and remain independent of management")
require('if (!this.canManage || !this.isEditingSelected' in page, "save path must fail closed")
require('if (!method || !revision || !stateKnown || !this.canManage)' in page, "save must re-check management before service invocation")
require('return this.canManage' in page and 'get canEditSelected' in page, "edit state must be management-gated")
require('*ngIf="!isEditingSelected && canManage"' in template, "edit visual gate missing")
require('[disabled]="!canSave"' in template, "save visual/fail-closed gate missing")
require("permissions.includes('CanReadAch')" in page_spec and "Integrations.Read" in page_spec, "read alternatives SPA tests missing")
require("permissions.includes('Integrations.ManageConnections')" in page_spec, "fine management SPA test missing")
require("no trata Integrations.ManageConnections por sí sola como permiso de lectura" in page_spec, "manage-only does not imply read test missing")
require("protege explícitamente Conexión entidad participante con Integrations.Read o CanReadAch" in route_spec, "connection route guard test missing")

# UI consumes only GET / preview-change / PUT; there is no connection/auth/SOAP test endpoint.
for token in (
    "api.get<IntegrationConnectionSettingsDto>('api/users/soap-integrations')",
    "api.post<IntegrationConnectionChangePreviewDto>('api/users/soap-integrations/preview-change'",
    "api.put<IntegrationConnectionSettingsDto>('api/users/soap-integrations'",
):
    require(token in api_adapter, f"expected consumed endpoint missing: {token}")
for forbidden in ('test-connection', 'test-auth', 'invoke-soap', 'execute-soap'):
    require(forbidden not in api_adapter.lower(), f"unexpected direct external operation added to UI adapter: {forbidden}")

# API read/write split. Preview is an administrative preflight, not an external test.
get_block = between(controller, '[HttpGet]', '[HttpPost("preview-change")]')
preview_block = between(controller, '[HttpPost("preview-change")]', '[HttpPut]')
put_block = controller[controller.find('[HttpPut]'):]
require('[Authorize(Policy = P1Policies.IntegrationsRead)]' in get_block, "GET must use P1.IntegrationsRead after NAV3.4R")
for block, name in ((preview_block, 'preview-change'), (put_block, 'PUT')):
    require('[Authorize(Policy = P1Policies.IntegrationConnectionsManage)]' in block, f"{name} must use dedicated connection management policy")
require('No persiste ni invoca SOAP' in controller, "preview behavior must remain explicitly non-invoking")
for forbidden in ('[HttpDelete', 'test-connection', 'test-authentication', 'InvokeSoapAsync', 'TestConnectionAsync'):
    require(forbidden.lower() not in controller.lower(), f"unexpected controller operation added: {forbidden}")

# What is persisted and what is exposed.
for token in ('WscfaachMappingsJson', 'WsAxonRespuestaTransaccionesMappingsJson'):
    require(token in entity, f"persisted connection storage missing: {token}")
for token in ('mapping.MethodName', 'mapping.Endpoint', 'mapping.SoapAction', 'mapping.OperatingMode', 'mapping.TimeoutSeconds', 'mapping.Enabled', 'mapping.InputParameterMappings'):
    require(token in settings_service, f"persisted mapping field not characterized: {token}")
for token in ('Endpoint = sanitizedEndpoint', 'SoapAction = sanitizedSoapAction', 'SanitizeEndpoint', 'SanitizeTechnicalUri', 'UserName = string.Empty', 'Password = string.Empty', 'Query = string.Empty', 'Fragment = string.Empty'):
    require(token in settings_service, f"sanitized browser response behavior missing: {token}")
require('ExpectedRevision = null' in settings_service, "expected revision write token must not be echoed")
require('PreservePersistedSensitiveValues' in settings_service, "sanitized placeholders must preserve raw persisted technical values")
require('PreservePersistedTimeouts' in settings_service, "UI must not accidentally zero backend timeout")

public_surface = settings_dto + mapping_dto + input_dto + entity
for forbidden in ('Password', 'AccessToken', 'ApiKey', 'ClientSecret', 'PrivateKey', 'SecretRef', 'EncryptedCredential', 'CertificatePem', 'BearerToken'):
    require(forbidden.lower() not in public_surface.lower(), f"credential/secret field exposed by connection DTO/entity: {forbidden}")
for token in ('Endpoint', 'SoapAction', 'OperatingMode', 'TimeoutSeconds', 'Enabled', 'InputParameterMappings'):
    require(token in mapping_dto, f"connection metadata field missing: {token}")
require('DefaultValue' in input_dto, "input mapping default metadata not characterized")
require('timeoutSeconds' not in read("web/ach-interbank-ui/src/app/features/integrations/data-access/integration-api.models.ts")[read("web/ach-interbank-ui/src/app/features/integrations/data-access/integration-api.models.ts").find('export interface IntegrationConnectionMethodDto'):read("web/ach-interbank-ui/src/app/features/integrations/data-access/integration-api.models.ts").find('export interface IntegrationConnectionSettingsDto')], "SPA unexpectedly began editing timeout")
for token in ('formControlName="endpoint"', 'formControlName="soapAction"', 'formControlName="operatingMode"', 'formControlName="enabled"'):
    require(token in template, f"editable field missing: {token}")
for token in ('certificados, hosts, puertos y resolución de red permanecen exclusivamente en backend', 'Seleccionar, editar o cancelar nunca ejecuta una operación SOAP', 'no incluye credenciales ni secretos'):
    require(token in template, f"security behavior copy missing: {token}")

# Admin requests do not call external transport. Runtime clients later consume saved configuration.
for forbidden in ('HttpClient', 'HttpRequestMessage', 'IWscfaachSoapClient', 'IWsAxonRespuestaTransaccionesSoapClient'):
    require(forbidden not in settings_service, f"settings request must not invoke external transport: {forbidden}")
require('SaveChangesAsync' in settings_service, "settings update must persist")
for source, name in ((wsc_client, 'WSCFAACH'), (wsaxon_client, 'WSAXON')):
    require('HttpRequestMessage(HttpMethod.Post' in source, f"{name} runtime client no longer performs SOAP HTTP POST")
    require('_soapSettingsService.GetAsync' in source, f"{name} runtime client no longer consumes persisted connection settings")
    require('not enabled for Live execution by database configuration' in source, f"{name} Live mode gate missing")
    require('mapping.Enabled' in source, f"{name} enabled gate missing")
    require('mapping.TimeoutSeconds is < 1 or > 300' in source, f"{name} timeout gate missing")
require('WsAxonEndpointSecurityMode.ControlledLocal' in wsaxon_client, "WSAXON ControlledLocal gate missing")
require('WsAxonEndpointSecurityMode.ConfiguredAllowlist' in wsaxon_client, "WSAXON allowlist gate missing")
require('WsAxonEndpointSecurityMode.Unconfigured' in endpoint_options, "WSAXON fail-closed unconfigured mode missing")
require('SoapEndpointSecurity:WsAxonRespuestaTransacciones' in endpoint_options, "WSAXON endpoint policy section changed")
require('SoapTransport:ControlledLocal' in endpoint_options, "controlled local transport section changed")
require('authorized local hosts' in transport and 'localhost' in transport and 'host.docker.internal' in transport, "controlled local alias gate changed")
require('"ProcContrapartidas"' in appsettings and '"Mode": "DryRun"' in appsettings, "base ProcContrapartidas DryRun option changed")
require('"ProcTransacciones"' in appsettings and appsettings.count('"Mode": "DryRun"') >= 2, "base ProcTransacciones DryRun option changed")
require('"SoapEndpointSecurity"' in devsettings and '"Mode": "ControlledLocal"' in devsettings, "development WSAXON ControlledLocal gate changed")
require('"TransportHost": "host.docker.internal"' in devsettings, "development controlled-local transport changed")
# WSCFAACH has no equivalent ConfiguredAllowlist in this client; its guards are DB Live/enabled/timeout/safe URI plus optional controlled-local transport.
require('Uri.TryCreate(mapping.Endpoint, UriKind.Absolute' in wsc_client, "WSCFAACH absolute URI validation missing")
require('endpoint.UserInfo' in wsc_client and 'endpoint.Fragment' in wsc_client, "WSCFAACH embedded credential/fragment guards missing")
require('ControlledLocalSoapTransportResolver.Resolve' in wsc_client, "WSCFAACH controlled-local transport resolution changed")

# Focal C# tests cover the durable boundary.
for token in (
    'ManageConnections_IsDedicatedCapability_AndSeederPersistsItIdempotentlyWithoutGrants',
    'ParticipantConnectionMenu_UsesCanReadAch_PreservesCanonicalStructureAndAdminAudience_AndAddsNoGrants',
    'ConnectionManagementPolicy_AllowsDedicatedOrLegacyManagement_ButNotReadOrOtherIntegrationCapabilities',
    'ParticipantConnectionApi_SeparatesReadFromConnectionManagement_AndHasNoTestOrInvokeAction',
    'AdministrationResponse_SanitizesDestinationAndAction_AndConnectionConfigurationContainsNoCredentialFields',
    'SettingsRequests_DoNotInvokeExternalTransport_ButSavedLiveConfigurationFeedsGuardedDownstreamSoapClients',
):
    require(token in focal_test, f"C# focal test missing: {token}")

# Closed/out-of-scope surfaces remain byte-identical to the NAV3.4P input ZIP.
expected_hashes = {
    "src/Cfa.ACHInterbank.Persistence/Security/Seeders/ConnectionsStructuralMenuAuthorizationSeeder.cs": "e47aa6009b1a3bdb8724e48c9489f93f9aed00210441422b570ff6bcaf40b2f1",
    "web/ach-interbank-ui/src/app/core/services/navigation.service.ts": "f8b984005ca4f0b99adb3abccd4e0c2e46da5ffc6f8e9a9f9d09ccf84bd2a3de",
    "src/Cfa.ACHInterbank.Application/Navigation/Queries/GetMenuForCurrentUserHandler.cs": "f111e416c181fc1912c297e37012cf7462f49d0434c1ef6ec9416c89b5ed1a61",
    "web/ach-interbank-ui/src/app/app-routing.module.ts": "14304a5b4db288552a401d71f62d77f0e5c1837bae5e0a4cb0d322a5a8ec75af",
    "src/Cfa.ACHInterbank.Persistence/Security/Seeders/IntegrationContractPermissionSeeder.cs": "a5462711598c057621f723aa8ba684cd99a635c37d041a50ccaa1c24f00e0b4a",
    "src/Cfa.ACHInterbank.Persistence/Security/Seeders/IntegrationContractMenuAuthorizationSeeder.cs": "9539afd6956992892c89551d8c1ff55f0c6b9ca874ceae720ad999e970951f6c",
    "web/ach-interbank-ui/src/app/features/integrations/pages/contract-versions-page.component.ts": "1155f4f312ae789b8ec1c700ce1ca6a722b9f989eb9df481bd4e1a8b9f9874a7",
    "src/Cfa.ACHInterbank.Api/Controllers/IntegrationContractsController.cs": "5fcf0a4f281304dde87733ca6579d38c0bc65ebb5c2b7135014545ccb25fefbd",
    "src/Cfa.ACHInterbank.Persistence/Security/Seeders/IntegrationMappingPermissionSeeder.cs": "128c77eb91f6028d98714242da664acc8d8533df977bd77732f44d7b493f825f",
    "src/Cfa.ACHInterbank.Persistence/Security/Seeders/InformationRelationshipMenuAuthorizationSeeder.cs": "b5cb8fc05a3e2bfca28714f697324d567177cf4d69cd88aa5ee354be4ea6e4a6",
    "web/ach-interbank-ui/src/app/features/integrations/pages/mapping-editor-page.component.ts": "70d81417dbe5dc288be63c9dd3a04c41ba972c1bea719369480e73bcffc567f0",
    "src/Cfa.ACHInterbank.Api/Controllers/IntegrationMappingSetsController.cs": "32ba2302fdb6f48d15135a3e9afdd48b8dd8b182076c6e07feb085e672a2fa91",
    "src/Cfa.ACHInterbank.Persistence/Security/Seeders/MftAdministrationMenuAuthorizationSeeder.cs": "d2e27ad8e2bc70c4e56d76a68d4fd638e23a2b53f5b3f7147361838a9945559a",
    "web/ach-interbank-ui/src/app/features/ach-colombia-mft-administration/ach-colombia-mft-administration.component.ts": "dac1bf2b458ce23c74d49758a66f931c583cf3410a175ccd451094d9d08e0499",
    "src/Cfa.ACHInterbank.Api/Controllers/AchColombiaFileExchangeController.cs": "6ec050055e13ad74403bc9167787924c65784f83ee4a8f51d39b0203ccad9bb2",
    "src/Cfa.ACHInterbank.Persistence/Security/Services/SoapIntegrationSettingsService.cs": "d05b265d31833ae5500275f4503634626149b8d39bdbe836b41c9cfdbfc58544",
    "src/Cfa.ACHInterbank.External/Connections/WscfaachSoapClient.cs": "c7cf6bc000fe4c5fbebfc60351aa6f25e3085ee1dd3ae364cd0536c919f98f72",
    "src/Cfa.ACHInterbank.External/Connections/WsAxonRespuestaTransaccionesSoapClient.cs": "d8f0844a3ff904683ec357d4a51f93c642eb136686dce50a7d07deccc011657d",
    "src/Cfa.ACHInterbank.External/Connections/ControlledLocalSoapTransportResolver.cs": "c99a24374bbcb7326ba67788a0598392dd5ca01ff4b761d55dc6b2689272e497",
    "src/Cfa.ACHInterbank.Application/Security/Dtos/WsAxonEndpointSecurityOptions.cs": "c92c428f3c4fdda2c17f95ff600d761d948d957045f2b5b111e2f335b28ecee0",
    "src/Cfa.ACHInterbank.Domain/Entities/User/SoapIntegrationSetting.cs": "f9e30ea9fa2987460bb6551e2517fcbc0fcb0a2c366e819e497dd643893f337f",
}
for path, expected in expected_hashes.items():
    require(sha(path) == expected, f"out-of-scope/functional runtime file changed: {path}")

print(f"NAV3.4Q participant-connection static validation PASS ({checks} assertions)")
