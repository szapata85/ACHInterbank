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
controller = read('src/Cfa.ACHInterbank.Api/Controllers/CertificateManagementController.cs')
load_service = read('src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CertificateManagement/CertificateLoadService.cs')
activation = read('src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CertificateManagement/CertificateActivationService.cs')
deletion = read('src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CertificateManagement/CertificateDeletionService.cs')
permission_seed = read('src/Cfa.ACHInterbank.Persistence/Security/Seeders/CertificateManagementPermissionSeeder.cs')
menu_seed = read('src/Cfa.ACHInterbank.Persistence/Security/Seeders/CertificateManagementMenuAuthorizationSeeder.cs')
legacy_cert_seed = read('src/Cfa.ACHInterbank.Persistence/Navigation/Seeders/NachaSecurityMenuSeeder.cs')
role_perm = read('src/Cfa.ACHInterbank.Persistence/Configuration/RolePermissionConfiguration.cs')
nav_seed = read('src/Cfa.ACHInterbank.Persistence/Navigation/Seeders/NavigationInformationArchitectureSeeder.cs')
routing = read('web/ach-interbank-ui/src/app/features/nacha-security/nacha-security-routing.module.ts')
routing_spec = read('web/ach-interbank-ui/src/app/features/nacha-security/nacha-security-routing.module.spec.ts')
spa_permissions = read('web/ach-interbank-ui/src/app/features/nacha-security/nacha-security-permissions.ts')
component = read('web/ach-interbank-ui/src/app/features/nacha-security/components/nacha-certificate-manager.component.ts')
template = read('web/ach-interbank-ui/src/app/features/nacha-security/components/nacha-certificate-manager.component.html')
component_spec = read('web/ach-interbank-ui/src/app/features/nacha-security/components/nacha-certificate-manager.component.spec.ts')
versions = read('web/ach-interbank-ui/src/app/features/nacha-security/components/certificate-versions.component.ts')
api_service = read('web/ach-interbank-ui/src/app/features/nacha-security/services/certificate-management-api.service.ts')
clearing_controller = read('src/Cfa.ACHInterbank.Api/Controllers/ClearingHousesController.cs')
fi_controller = read('src/Cfa.ACHInterbank.Api/Controllers/FinancialInstitutionsController.cs')
digital_component = read('web/ach-interbank-ui/src/app/features/nacha-security/components/digital-envelope-tool.component.ts')
manual_encrypt = read('web/ach-interbank-ui/src/app/features/nacha-security/components/manual-encrypt-operation.component.ts')
manual_decrypt = read('web/ach-interbank-ui/src/app/features/nacha-security/components/manual-decrypt-operation.component.ts')
generate = read('web/ach-interbank-ui/src/app/features/nacha-security/components/nacha-generate-operation.component.ts')
generate_encrypted = read('web/ach-interbank-ui/src/app/features/nacha-security/components/nacha-generate-encrypted-operation.component.ts')
test_tools = read('src/Cfa.ACHInterbank.Persistence/Security/Seeders/TestToolsMenuAuthorizationSeeder.cs')

# Existing certificate permission vocabulary and P1 policies remain the source of truth.
for token in (
    'public const string Read = "Certificates.Read"',
    'public const string UploadPublic = "Certificates.UploadPublic"',
    'public const string RegisterPrivate = "Certificates.RegisterPrivate"',
    'public const string Activate = "Certificates.Activate"',
    'public const string Revoke = "Certificates.Revoke"',
    'public const string Validate = "Certificates.Validate"',
    'public const string Audit = "Certificates.Audit"'):
    require(token in fine, f'missing existing certificate fine permission: {token}')
for token in (
    'Certificates.Read, Certificates.UploadPublic, Certificates.RegisterPrivate,',
    'Certificates.Activate, Certificates.Revoke, Certificates.Validate, Certificates.Audit'):
    require(token in fine, 'certificate permissions must remain in AllPermissions')
for token in (
    'public const string CertificatesRead = "P1.CertificatesRead"',
    'public const string CertificatesUploadPublic = "P1.CertificatesUploadPublic"',
    'public const string CertificatesRegisterPrivate = "P1.CertificatesRegisterPrivate"',
    'public const string CertificatesActivate = "P1.CertificatesActivate"',
    'public const string CertificatesRevoke = "P1.CertificatesRevoke"',
    'public const string CertificatesValidate = "P1.CertificatesValidate"',
    'public const string CertificatesAudit = "P1.CertificatesAudit"'):
    require(token in p1, f'missing P1 certificate policy: {token}')

read_policy = between(policy_reg, 'options.AddPolicy(P1Policies.CertificatesRead,', 'options.AddPolicy(P1Policies.CertificatesUploadPublic,')
require('FineGrainedPermissions.Certificates.Read' in read_policy, 'certificate read must accept Certificates.Read')
require('FineGrainedPermissions.CanManageCertificates' in read_policy, 'certificate read must accept CanManageCertificates')
require('"CanReadAch"' in read_policy, 'certificate read must retain CanReadAch compatibility')

upload_policy = between(policy_reg, 'options.AddPolicy(P1Policies.CertificatesUploadPublic,', 'options.AddPolicy(P1Policies.CertificatesRegisterPrivate,')
require('FineGrainedPermissions.Certificates.UploadPublic' in upload_policy, 'upload preview policy must accept fine upload-public')
require('FineGrainedPermissions.CanManageCertificates' in upload_policy, 'upload preview policy must retain CanManageCertificates')
require('CanReadAch' not in upload_policy, 'read must not authorize upload preview')

register_policy = between(policy_reg, 'options.AddPolicy(P1Policies.CertificatesRegisterPrivate,', 'options.AddPolicy(P1Policies.CertificatesActivate,')
require('FineGrainedPermissions.Certificates.RegisterPrivate' in register_policy, 'managed save must accept fine register-private')
require('FineGrainedPermissions.CanManageCertificates' in register_policy, 'managed save must retain CanManageCertificates')
require('CanReadAch' not in register_policy, 'read must not authorize managed save')
revoke_policy = between(policy_reg, 'options.AddPolicy(P1Policies.CertificatesRevoke,', 'options.AddPolicy(P1Policies.CertificatesValidate,')
require('FineGrainedPermissions.Certificates.Revoke' in revoke_policy, 'revoke policy must accept fine revoke')
require('FineGrainedPermissions.CanManageCertificates' in revoke_policy, 'revoke policy must retain CanManageCertificates')
require('CanReadAch' not in revoke_policy, 'read must not authorize revoke/delete')

# Actual controller operations remain separated by policy.
controller_expectations = {
    '[HttpPost("managed/preview")]': 'P1Policies.CertificatesUploadPublic',
    '[HttpPost("managed")]': 'P1Policies.CertificatesRegisterPrivate',
    '[HttpGet]': 'P1Policies.CertificatesRead',
    '[HttpGet("{id:int}/versions")]': 'P1Policies.CertificatesRead',
    '[HttpPost("versions/{id:int}/activate")]': 'P1Policies.CertificatesActivate',
    '[HttpPost("versions/{id:int}/revoke")]': 'P1Policies.CertificatesRevoke',
    '[HttpDelete("versions/{id:int}")]': 'P1Policies.CertificatesRevoke',
    '[HttpPost("versions/{id:int}/validate")]': 'P1Policies.CertificatesValidate',
    '[HttpGet("audit")]': 'P1Policies.CertificatesAudit',
}
for marker, policy in controller_expectations.items():
    i = controller.find(marker)
    require(i >= 0, f'missing controller endpoint: {marker}')
    require(f'[Authorize(Policy = {policy})]' in controller[i:i+650], f'{marker} must keep {policy}')

# Current SPA consumes list, versions, preview/save, revoke/delete. It does not expose activate/validate/audit/download.
for token in (
    'this.api.get<CertificateListItem[]>(this.basePath)',
    '`${this.basePath}/managed/preview`',
    '`${this.basePath}/managed`',
    '`${this.basePath}/${id}/versions`',
    '`${this.basePath}/versions/${versionId}/revoke`',
    '`${this.basePath}/versions/${versionId}`'):
    require(token in api_service, f'missing certificate SPA API operation: {token}')
for token in ('this.api.list()', 'this.api.preview(this.request())', 'this.api.save(this.request())', 'this.api.revoke(', 'this.api.delete('):
    require(token in component, f'missing consumed manager operation: {token}')
require('this.api.getVersions(this.certificateId)' in versions, 'versions screen must remain read-only using getVersions')
for token in ('api.activate(', 'api.validate(', 'api.audit(', 'downloadPublic('):
    require(token not in component, f'current manager must not expose unused operation {token}')
require('downloadPublic(' not in api_service, 'SPA service must not expose certificate material download')

# Catalog materializes only existing fine permissions needed by this screen and creates no grants.
for token in (
    'FineGrainedPermissions.Certificates.Read',
    'FineGrainedPermissions.Certificates.UploadPublic',
    'FineGrainedPermissions.Certificates.RegisterPrivate',
    'FineGrainedPermissions.Certificates.Revoke'):
    require(token in permission_seed, f'certificate permission seeder must materialize {token}')
for token in ('FineGrainedPermissions.Certificates.Activate', 'FineGrainedPermissions.Certificates.Validate', 'FineGrainedPermissions.Certificates.Audit'):
    require(token not in permission_seed, f'NAV3.4F must not materialize unused capability {token}')
require('RolePermission' not in permission_seed, 'certificate permission seeder must not grant roles')
require('UserPermission' not in permission_seed, 'certificate permission seeder must not grant users')
for name in ('Certificates.Read', 'Certificates.UploadPublic', 'Certificates.RegisterPrivate', 'Certificates.Revoke'):
    require(name not in role_perm, f'NAV3.4F must not add RolePermission grant for {name}')

# Menu entry mirrors P1.CertificatesRead and preserves structural identity.
require('AuthorizationReconciliationOrder = 106' in menu_seed, 'certificate menu reconciler order must follow NAV3.4E')
require('item.Id == MenuItemConfiguration.NachaCertificatesId' in menu_seed, 'certificate reconciler must target canonical MenuItem id')
require('"/nacha-security/certificates"' in menu_seed, 'certificate reconciler must target exact route')
require('CertificateManagementPermissionSeeder.ReadPermissionId' in menu_seed, 'menu must accept Certificates.Read')
require('PermissionConfiguration.ManageCertificatesPermissionId' in menu_seed, 'menu must accept CanManageCertificates')
require('PermissionConfiguration.ReadAchPermissionId' in menu_seed, 'menu must accept CanReadAch')
for token in ('MenuItemRoles', 'menuItem.Label =', 'menuItem.Route =', 'menuItem.Order =', 'menuItem.IsActive =', 'menuItem.ParentId ='):
    require(token not in menu_seed, f'menu reconciler must not mutate structural/role state: {token}')
require('CanManageCertificates' in legacy_cert_seed, 'legacy certificate seed remains the historical initial restriction')

cert_nav = between(nav_seed, 'await ReconcileExistingAsync(\n            DigitalCertificatesRoute,', 'await ReconcileExistingAsync(\n            DigitalEnvelopeRoute,')
for token in ('"Certificados digitales"', 'order: 10'):
    require(token in cert_nav, f'certificate menu structural identity changed: {token}')

# SPA read entry matches backend read alternatives; versions route no longer drifts to CanManageAch.
cert_route = between(routing, "path: 'certificates'", "path: 'certificates/:id/versions'")
for token in ('certificatesRead', 'canManageCertificates', 'canReadAch'):
    require(token in cert_route, f'certificate route missing read alternative: {token}')
require('canManageAch' not in cert_route, 'generic CanManageAch must not substitute certificate read')
require('roles:' not in cert_route, 'certificate route must not hardcode roles')
versions_route = between(routing, "path: 'certificates/:id/versions'", "path: 'nacha/generate'")
for token in ('certificatesRead', 'canManageCertificates', 'canReadAch'):
    require(token in versions_route, f'versions route missing read alternative: {token}')
require('canManageAch' not in versions_route, 'versions route must not use unrelated CanManageAch')
require('alinea certificados y sus versiones con la capacidad de lectura del backend' in routing_spec, 'routing spec must cover certificate read alignment')

# Fine-grained SPA constants exist and no role checks are introduced.
for token in (
    "certificatesRead: 'Certificates.Read'",
    "certificatesUploadPublic: 'Certificates.UploadPublic'",
    "certificatesRegisterPrivate: 'Certificates.RegisterPrivate'",
    "certificatesRevoke: 'Certificates.Revoke'",
    "canManageCertificates: 'CanManageCertificates'",
    "canReadAch: 'CanReadAch'"):
    require(token in spa_permissions, f'missing SPA permission constant: {token}')
require('hasRole(' not in component, 'certificate manager must not hardcode role authorization')

# Pure fine read no longer depends on auxiliary CanReadAch-only catalogs.
require('const canReadAuxiliaryContext = this.auth.hasPermission(NACHA_SECURITY_PERMISSIONS.canReadAch)' in component, 'manager must explicitly separate auxiliary CanReadAch context')
require('canReadAuxiliaryContext ? this.clearingHousesApi.list() : of([])' in component, 'pure fine read must skip clearing-house auxiliary GET')
require('canReadAuxiliaryContext ? this.financialInstitutionsApi.getAll() : of([])' in component, 'pure fine read must skip financial-institution auxiliary GET')
require('permite consulta fina sin exigir los catálogos auxiliares protegidos por CanReadAch' in component_spec, 'component spec must prove fine-read-only load')
require('[Authorize(Policy = "CanReadAch")]' in clearing_controller, 'clearing-house auxiliary API must retain CanReadAch')
require('[Authorize(Policy = "CanReadAch")]' in fi_controller, 'financial-institution auxiliary API must retain CanReadAch')

# Upload is fail-closed: wizard needs selector read context plus both backend mutation capabilities.
require('get canUploadCertificates()' in component, 'manager must expose upload capability gate')
require('this.auth.hasPermission(NACHA_SECURITY_PERMISSIONS.canReadAch)' in component, 'upload UI must retain auxiliary selector prerequisite')
require('this.auth.hasPermission(CERTIFICATE_PREVIEW_PERMISSIONS)' in component, 'upload UI must require preview policy alternative')
require('this.auth.hasPermission(CERTIFICATE_SAVE_PERMISSIONS)' in component, 'upload UI must require save policy alternative')
require('if (!this.canUploadCertificates)' in component, 'openUpload must fail closed')
require('get canPreviewCertificate()' in component, 'upload dialog must gate preview')
require('get canSaveCertificate()' in component, 'upload dialog must gate save')
require('if (!this.canPreviewCertificate)' in component, 'verify method must fail closed')
require('if (!this.canSaveCertificate)' in component, 'save method must fail closed')
require('[disabled]="fileForm.invalid || verifying || !canPreviewCertificate"' in component, 'verify control must be visually gated')
require('[disabled]="!preview?.isValid || saving || !canSaveCertificate"' in component, 'save control must be visually gated')
require('@if (canUploadCertificates)' in template, 'upload/replace controls must be visually gated')

# Revoke/delete use the existing revoke capability, both visually and inside methods.
require('get canRevokeCertificates()' in component, 'manager must expose revoke/delete gate')
require('return this.auth.hasPermission(CERTIFICATE_REVOKE_PERMISSIONS)' in component, 'revoke/delete gate must mirror backend alternatives')
require(component.count('if (!this.canRevokeCertificates)') >= 4, 'request and execution methods for revoke/delete must all fail closed')
require('canRevokeCertificates && canRevoke(certificate)' in template, 'revoke control must be visually gated')
require('canRevokeCertificates && certificate.canDelete' in template, 'delete control must be visually gated')
require('bloquea fail-closed las mutaciones para un usuario de sólo lectura' in component_spec, 'component spec must cover denied mutations')
require('acepta la combinación fina de carga sin requerir CanManageCertificates' in component_spec, 'component spec must cover fine upload capabilities')
require('acepta las capacidades finas de revocación sin checks por rol' in component_spec, 'component spec must cover fine revoke capability')

# Functional cryptographic behavior remains protected and unchanged in semantics.
for token in ('_privateMaterialProtector.Protect', 'EncryptedPrivateMaterial', 'RawPublicCertificate', 'previous.Status = CertificateStatus.Replaced', 'CertificateRotationHistories'):
    require(token in load_service, f'certificate load/rotation behavior missing: {token}')
require('version.Status = CertificateStatus.Revoked' in activation, 'revoke must persist Revoked status')
require('version.RevokedAtUtc = DateTime.UtcNow' in activation, 'revoke must persist revocation timestamp')
for token in ('hasOperationalReferences', 'Revoca el certificado antes de eliminarlo', 'DigitalCertificateVersions.Remove', 'CryptographicOperations.ZeroMemory'):
    require(token in deletion, f'certificate deletion safety missing: {token}')
require('La contraseña se utilizará únicamente para abrir y validar el certificado. No se almacenará' in component, 'SPA must retain password handling disclosure')

# Scope regression: Digital envelope and manual technical operations stay represented and separate.
for path in ('nacha/generate', 'nacha/generate-encrypted', 'digital-envelope/manual-encrypt', 'digital-envelope/manual-decrypt', 'sobre-digital'):
    require(f"path: '{path}'" in routing, f'out-of-scope security route missing: {path}')
require('DigitalEnvelope' in digital_component, 'Sobre digital component must remain present')
require('NachaGenerate' in generate, 'manual NACHA generation component must remain present')
require('NachaGenerateEncrypted' in generate_encrypted, 'encrypted NACHA generation component must remain present')
require('ManualEncrypt' in manual_encrypt, 'manual encrypt component must remain present')
require('ManualDecrypt' in manual_decrypt, 'manual decrypt component must remain present')
require('FineGrainedPermissions.TestTools.Access' in test_tools and 'serviceTests.IsActive = true;' in test_tools, 'test-tools security must remain intact')

print(f'NAV3.4F static validation passed: {checks} assertions')
