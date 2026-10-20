#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
assertions = 0


def read(path: str) -> str:
    return (ROOT / path).read_text(encoding='utf-8')


def require(condition: bool, message: str) -> None:
    global assertions
    assertions += 1
    if not condition:
        raise AssertionError(message)


def between(source: str, start: str, end: str) -> str:
    a = source.find(start)
    require(a >= 0, f'start marker missing: {start}')
    b = source.find(end, a + len(start))
    require(b > a, f'end marker missing after {start}: {end}')
    return source[a:b]

fine = read('src/Cfa.ACHInterbank.Application/Security/FineGrainedPermissions.cs')
p1 = read('src/Cfa.ACHInterbank.Application/Security/P1Policies.cs')
policies = read('src/Cfa.ACHInterbank.External/DependencyInjectionService.cs')
perm_cfg = read('src/Cfa.ACHInterbank.Persistence/Configuration/PermissionConfiguration.cs')
role_perm_cfg = read('src/Cfa.ACHInterbank.Persistence/Configuration/RolePermissionConfiguration.cs')
perm_seed = read('src/Cfa.ACHInterbank.Persistence/Security/Seeders/DigitalEnvelopeOperationPermissionSeeder.cs')
menu_seed = read('src/Cfa.ACHInterbank.Persistence/Security/Seeders/DigitalEnvelopeMenuAuthorizationSeeder.cs')
nacha_seed = read('src/Cfa.ACHInterbank.Persistence/Navigation/Seeders/NachaSecurityMenuSeeder.cs')
ia = read('src/Cfa.ACHInterbank.Persistence/Navigation/Seeders/NavigationInformationArchitectureSeeder.cs')
routing = read('web/ach-interbank-ui/src/app/features/nacha-security/nacha-security-routing.module.ts')
ng_permissions = read('web/ach-interbank-ui/src/app/features/nacha-security/nacha-security-permissions.ts')
component = read('web/ach-interbank-ui/src/app/features/nacha-security/components/digital-envelope-tool.component.ts')
html = read('web/ach-interbank-ui/src/app/features/nacha-security/components/digital-envelope-tool.component.html')
component_spec = read('web/ach-interbank-ui/src/app/features/nacha-security/components/digital-envelope-tool.component.spec.ts')
ng_service = read('web/ach-interbank-ui/src/app/features/nacha-security/services/sobre-digital.service.ts')
ng_service_spec = read('web/ach-interbank-ui/src/app/features/nacha-security/services/sobre-digital.service.spec.ts')
clearing_ng = read('web/ach-interbank-ui/src/app/core/services/clearing-houses-api.service.ts')
controller = read('src/Cfa.ACHInterbank.Api/Controllers/SobreDigitalController.cs')
clearing_controller = read('src/Cfa.ACHInterbank.Api/Controllers/ClearingHousesController.cs')
managed = read('src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CertificateManagement/ManagedDigitalEnvelopeService.cs')
resolver = read('src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CertificateManagement/CertificateSecretResolverServices.cs')
handler = read('src/Cfa.ACHInterbank.Application/Navigation/Queries/GetMenuForCurrentUserHandler.cs')
cert_menu = read('src/Cfa.ACHInterbank.Persistence/Security/Seeders/CertificateManagementMenuAuthorizationSeeder.cs')

# Canonical fine capabilities already existed and remain canonical; no conceptual invention.
require('public const string Encrypt = "DigitalEnvelope.Encrypt";' in fine, 'DigitalEnvelope.Encrypt canonical capability missing')
require('public const string Decrypt = "DigitalEnvelope.Decrypt";' in fine, 'DigitalEnvelope.Decrypt canonical capability missing')
require('DigitalEnvelope.Encrypt, DigitalEnvelope.Decrypt, DigitalEnvelope.Test' in fine, 'DigitalEnvelope fine capabilities missing from AllPermissions')
require('public const string DigitalEnvelopeEncrypt = "P1.DigitalEnvelopeEncrypt";' in p1, 'encrypt policy name missing')
require('public const string DigitalEnvelopeDecrypt = "P1.DigitalEnvelopeDecrypt";' in p1, 'decrypt policy name missing')
require('DigitalEnvelope.Encrypt' not in perm_cfg and 'DigitalEnvelope.Decrypt' not in perm_cfg,
        'fine digital-envelope capabilities must be materialized by seeder, not duplicated in HasData')

# Idempotent materialization only; no grants.
require('public sealed class DigitalEnvelopeOperationPermissionSeeder' in perm_seed, 'operation permission seeder missing')
require('FineGrainedPermissions.DigitalEnvelope.Encrypt' in perm_seed, 'encrypt fine capability not persisted')
require('FineGrainedPermissions.DigitalEnvelope.Decrypt' in perm_seed, 'decrypt fine capability not persisted')
require('FirstOrDefaultAsync(item => item.Name == name)' in perm_seed, 'permission seeder must reconcile by canonical name')
require('?? await context.Permissions.FindAsync(id)' in perm_seed, 'permission seeder must fall back to stable id')
require('"Cifrar archivos mediante sobre digital"' in perm_seed, 'encrypt description mismatch')
require('"Descifrar archivos mediante sobre digital"' in perm_seed, 'decrypt description mismatch')
for forbidden in ('RolePermission', 'RoleConfiguration.', 'UserPermission', 'UserRole'):
    require(forbidden not in perm_seed, f'permission seeder creates forbidden grant: {forbidden}')
require('DigitalEnvelopeOperationPermissionSeeder.EncryptPermissionId' not in role_perm_cfg,
        'encrypt fine capability must not receive RolePermission seed')
require('DigitalEnvelopeOperationPermissionSeeder.DecryptPermissionId' not in role_perm_cfg,
        'decrypt fine capability must not receive RolePermission seed')

# Backend policies are already correct: fine OR CanManageAch, never CanReadAch for crypto.
encrypt_policy = between(policies, 'options.AddPolicy(P1Policies.DigitalEnvelopeEncrypt', 'options.AddPolicy(P1Policies.DigitalEnvelopeDecrypt')
require('FineGrainedPermissions.DigitalEnvelope.Encrypt' in encrypt_policy, 'encrypt policy missing fine capability')
require('"CanManageAch"' in encrypt_policy, 'encrypt policy missing CanManageAch fallback')
require('"CanReadAch"' not in encrypt_policy, 'read-only capability must not authorize encryption')
decrypt_policy = between(policies, 'options.AddPolicy(P1Policies.DigitalEnvelopeDecrypt', 'options.AddPolicy(P1Policies.DigitalEnvelopeTest')
require('FineGrainedPermissions.DigitalEnvelope.Decrypt' in decrypt_policy, 'decrypt policy missing fine capability')
require('"CanManageAch"' in decrypt_policy, 'decrypt policy missing CanManageAch fallback')
require('"CanReadAch"' not in decrypt_policy, 'read-only capability must not authorize decryption')

# Current SPA preparation requires CanReadAch because it loads operational clearing-house context.
require(".get<ClearingHouseOption[] | { items?: ClearingHouseOption[] }>(`${this.basePath}/operational`)" in clearing_ng,
        'sobre digital no longer loads operational clearing-house context')
operational = between(clearing_controller, '[HttpGet("operational")]', '[HttpGet("payment-rail-options")')
require('[Authorize(Policy = "CanReadAch")]' in operational, 'operational clearing-house read boundary changed')
require('[HttpGet("certificates")]' in controller, 'certificate preparation endpoint missing')
cert_action = between(controller, '[HttpGet("certificates")]', '[EndpointSummary("Cifrar archivo')
require('[Authorize(Policy = P1Policies.CertificatesRead)]' in cert_action, 'certificate preparation policy changed')

# Menu retains canonical structure and gains the real preparation entry boundary only.
require('MenuItemConfiguration.DigitalEnvelopeId' in nacha_seed, 'digital envelope MenuItem seed missing')
require('"/nacha-security/sobre-digital"' in nacha_seed, 'digital envelope route seed changed')
file_security = between(ia, 'private async Task ReconcileFileSecurityAsync()', 'private async Task ReconcileReportsAsync()')
require('"Cifrar y descifrar archivos"' in file_security, 'canonical menu label changed')
require('order: 20' in file_security, 'canonical menu order changed')
require('public const int AuthorizationReconciliationOrder = 110;' in menu_seed, 'NAV3.4J menu reconciliation order mismatch')
require('MenuItemConfiguration.NachaSecurityId' in menu_seed, 'menu parent validation missing')
require('MenuItemConfiguration.DigitalEnvelopeId' in menu_seed, 'menu child validation missing')
require('"Cifrar y descifrar archivos"' in menu_seed, 'menu reconciler label validation missing')
require('"/nacha-security/sobre-digital"' in menu_seed, 'menu reconciler route validation missing')
require('menuItem.Order != 20' in menu_seed, 'menu reconciler order validation missing')
require('PermissionConfiguration.ReadAchPermissionId' in menu_seed, 'menu entry must require CanReadAch preparation capability')
require('DigitalEnvelopeOperationPermissionSeeder.EncryptPermissionId' not in menu_seed,
        'menu must not OR encrypt capability around the preparation dependency')
require('DigitalEnvelopeOperationPermissionSeeder.DecryptPermissionId' not in menu_seed,
        'menu must not OR decrypt capability around the preparation dependency')
require('context.MenuItemRoles.RemoveRange(roleLinks);' in menu_seed, 'menu must not add role-name boundary')
for forbidden in ('RolePermission', 'UserPermission', 'UserRole'):
    require(forbidden not in menu_seed, f'menu reconciler creates forbidden grant: {forbidden}')

# SPA route becomes read/preparation only; sensitive methods are gated separately and fail closed.
require('canActivateChild: [permissionGuard]' in routing, 'direct child navigation must be protected by permissionGuard')
require('roleGuard' not in routing, 'nacha-security routing must not add role-name checks')
route = between(routing, "path: 'sobre-digital'", "path: '', pathMatch: 'full'")
require('permissions: [NACHA_SECURITY_PERMISSIONS.canReadAch]' in route, 'sobre digital route must use CanReadAch entry boundary')
require('canManageAch' not in route, 'CanManageAch alone cannot satisfy current preparation dependencies')
require('canManualEncryptEnvelope' not in route and 'canManualDecryptEnvelope' not in route,
        'manual-route aliases must not authorize managed sobre digital entry')
require("digitalEnvelopeEncrypt: 'DigitalEnvelope.Encrypt'" in ng_permissions, 'SPA canonical encrypt capability missing')
require("digitalEnvelopeDecrypt: 'DigitalEnvelope.Decrypt'" in ng_permissions, 'SPA canonical decrypt capability missing')
require('private readonly auth = inject(AuthService);' in component, 'component action authorization service missing')
require('get canEncrypt(): boolean' in component, 'encrypt action gate missing')
require('NACHA_SECURITY_PERMISSIONS.digitalEnvelopeEncrypt' in component, 'encrypt action gate missing fine capability')
require('get canDecrypt(): boolean' in component, 'decrypt action gate missing')
require('NACHA_SECURITY_PERMISSIONS.digitalEnvelopeDecrypt' in component, 'decrypt action gate missing fine capability')
require(component.count('NACHA_SECURITY_PERMISSIONS.canManageAch') >= 2, 'CanManageAch action fallback missing')
require('if (!this.canExecute(mode))' in component, 'sensitive method must fail closed before service invocation')
require("No tienes autorización para cifrar archivos mediante sobre digital." in component, 'encrypt fail-closed feedback missing')
require("No tienes autorización para descifrar archivos mediante sobre digital." in component, 'decrypt fail-closed feedback missing')
require('|| !canEncrypt' in html, 'encrypt visual gate missing')
require('|| !canDecrypt' in html, 'decrypt visual gate missing')
require('permite consultar y preparar la operación' in html, 'mixed-screen authorization explanation missing')
require('falla cerrado antes de invocar cifrado cuando solo existe capacidad de lectura' in component_spec,
        'Angular fail-closed test missing')
require('separa las capacidades finas de cifrado y descifrado' in component_spec,
        'Angular distinct-action-capability test missing')

# Actual UI surface: one configuration GET plus encrypt/decrypt POST, plus operational clearing-house GET.
require("private readonly basePath = 'api/nacha-security/digital-envelope';" in ng_service, 'sobre digital service base path changed')
require(ng_service.count('this.http.get<') == 1, 'sobre digital service must expose exactly one direct GET')
require(ng_service.count('this.http.post(') == 2, 'sobre digital service must expose exactly two direct POST operations')
require('`${this.basePath}/certificates`' in ng_service, 'certificate GET missing')
require('`${this.basePath}/encrypt`' in ng_service, 'encrypt POST missing')
require('`${this.basePath}/decrypt`' in ng_service, 'decrypt POST missing')
for forbidden in ('put(', 'patch(', 'delete(', 'manual-encrypt', 'manual-decrypt', 'generate-encrypted', '/mft', 'soap'):
    require(forbidden.lower() not in ng_service.lower(), f'unexpected UI operation detected: {forbidden}')
require(ng_service_spec.count("expect(request.request.method).toBe('POST')") == 2, 'service tests must cover both crypto POST operations')
require(ng_service_spec.count("expect(request.request.method).toBe('GET')") == 1, 'service tests must cover certificate GET')

# API endpoints consumed by this UI preserve canonical policies and return artifacts in the same response.
encrypt_action = between(controller, '[HttpPost("encrypt")]', '[EndpointSummary("Descifrar archivo')
require('[Authorize(Policy = P1Policies.DigitalEnvelopeEncrypt)]' in encrypt_action, 'encrypt endpoint policy changed')
require('[Consumes("multipart/form-data")]' in encrypt_action, 'encrypt endpoint must receive multipart file')
require('return File(result.Content, result.ContentType, result.FileName);' in encrypt_action, 'encrypt artifact response changed')
decrypt_action = between(controller, '[HttpPost("decrypt")]', '[HttpPost("testRSA")')
require('[Authorize(Policy = P1Policies.DigitalEnvelopeDecrypt)]' in decrypt_action, 'decrypt endpoint policy changed')
require('[Consumes("multipart/form-data")]' in decrypt_action, 'decrypt endpoint must receive multipart file')
require('return File(result.Content, result.ContentType, result.FileName);' in decrypt_action, 'decrypt artifact response changed')
require('[Authorize(Policy = P1Policies.DigitalEnvelopeTest)]' in controller, 'hidden technical test endpoint changed')
require('[ApiExplorerSettings(IgnoreApi = true)]' in controller, 'hidden technical test endpoint exposure changed')

# Crypto model unchanged: private material server-side, audit metadata persisted, no direct transmission/storage of artifacts.
require('ResolvePrivateCertificateAsync' in managed and '_secretResolver.ResolveAsync' in managed,
        'private certificate must be resolved on server')
require('GetRSAPrivateKey()' in managed, 'private-key operation missing from server-side service')
require('DigitalEnvelopeOperationLogs.Add' in managed, 'operation audit persistence missing')
require('HashPlainSha256' in managed and 'HashEncryptedSha256' in managed, 'audit hashes missing')
require('_context.SaveChangesAsync' in managed, 'audit persistence save missing')
for forbidden in ('File.Write', 'FileStream(', 'HttpClient', 'Soap', 'SOAP', 'Mft', 'MFT', 'Sftp', 'FTP'):
    require(forbidden not in managed, f'direct external/artifact effect unexpectedly present: {forbidden}')
require('DatabaseEncryptedCertificateSecretProvider' in resolver, 'database-encrypted secret provider missing')
require('_protector.Unprotect(version.EncryptedPrivateMaterial)' in resolver, 'protected private material no longer resolved server-side')
require('X509KeyStorageFlags.EphemeralKeySet' in resolver, 'ephemeral in-memory certificate handling changed')
require('CertificateSecretMasker.Mask' in resolver, 'secret references are no longer masked')
require('EncryptedPrivateMaterial' not in ng_service, 'private material leaked into Angular service contract')
require('SecretRef' not in ng_service, 'secret reference leaked into Angular service contract')

# Download is the immediate crypto response plus browser-side repeat; no independent download API/capability.
require('await this.downloads.save(response);' in component, 'initial artifact download handling changed')
require('async downloadAgain()' in component and 'await this.downloads.save(this.lastResponse);' in component,
        'repeat download must remain browser-side cached response')
require('CanDownloadEnvelope' not in component and 'AuthorizeDownload' not in component,
        'unrelated download capability must not be invented for this UI')

# Certificates and technical/manual routes remain present and independently authorized.
require("path: 'certificates'" in routing and 'NACHA_SECURITY_PERMISSIONS.certificatesRead' in routing,
        'certificates route authorization regressed')
require('CertificateManagementPermissionSeeder.ReadPermissionId' in cert_menu, 'certificate menu normalization regressed')
for route_name, perm in (
    ("path: 'nacha/generate'", 'canGenerateNacha'),
    ("path: 'nacha/generate-encrypted'", 'canGenerateEncryptedNacha'),
    ("path: 'digital-envelope/manual-encrypt'", 'canManualEncryptEnvelope'),
    ("path: 'digital-envelope/manual-decrypt'", 'canManualDecryptEnvelope')):
    require(route_name in routing, f'technical/manual route missing: {route_name}')
    require(perm in routing, f'technical/manual route permission changed: {perm}')
require('return hasRole && hasPermission;' in handler, 'GetMenuForCurrentUserHandler changed unexpectedly')

# Seeded audience remains unchanged: Admin and ACH.Operator retain legacy read/manage; no fine grants are added.
for role_id in ('RoleConfiguration.AdminRoleId', 'RoleConfiguration.OperatorRoleId'):
    require(f'RoleId = {role_id},\n                PermissionId = PermissionConfiguration.ManageAchPermissionId' in role_perm_cfg,
            f'{role_id} CanManageAch seed missing')
    require(f'RoleId = {role_id},\n                PermissionId = PermissionConfiguration.ReadAchPermissionId' in role_perm_cfg,
            f'{role_id} CanReadAch seed missing')

print(f'PASS: NAV3.4J static focal validation ({assertions} assertions)')
