#!/usr/bin/env python3
from __future__ import annotations

import hashlib
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
checks = 0


def read(path: str) -> str:
    return (ROOT / path).read_text(encoding="utf-8")


def require(condition: bool, message: str) -> None:
    global checks
    checks += 1
    if not condition:
        raise AssertionError(message)


def sha256(path: str) -> str:
    return hashlib.sha256((ROOT / path).read_bytes()).hexdigest()


# Final NAV3.5B migration matrix, derived from the NAV3 transition policies that remain live.
EXPECTED_EDGES: list[tuple[str, str, str, str]] = [
    ("CanReadAch", "Returns.Read", "FineGrainedPermissions.Returns.Read", "P0Policies.ReturnsRead"),
    ("CanReadAch", "BulkIngestion.Read", "FineGrainedPermissions.BulkIngestion.Read", "P1Policies.BulkIngestionRead"),
    ("CanReadAch", "Certificates.Read", "FineGrainedPermissions.Certificates.Read", "P1Policies.CertificatesRead"),
    ("CanReadAch", "NavigationLogs.Read", "FineGrainedPermissions.NavigationLogs.Read", "P1Policies.NavigationLogsRead"),
    ("CanReadAch", "CanViewPaymentRailCapabilityRegistry", "FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry", "FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry"),
    ("CanReadAch", "Config.Read", "FineGrainedPermissions.Config.Read", "P1Policies.ConfigRead"),
    ("CanReadAch", "Integrations.Read", "FineGrainedPermissions.Integrations.Read", "P1Policies.IntegrationsRead"),

    ("CanManageAch", "Transactions.Create", "FineGrainedPermissions.Transactions.Create", "P0Policies.TransactionsCreate"),
    ("CanManageAch", "Nacha.Upload", "FineGrainedPermissions.Nacha.Upload", "P1Policies.NachaUpload"),
    ("CanManageAch", "Returns.GenerateFile", "FineGrainedPermissions.Returns.GenerateFile", "P0Policies.ReturnsGenerateFile"),
    ("CanManageAch", "BulkIngestion.Upload", "FineGrainedPermissions.BulkIngestion.Upload", "P1Policies.BulkIngestionUpload"),
    ("CanManageAch", "BulkIngestion.Retry", "FineGrainedPermissions.BulkIngestion.Retry", "P1Policies.BulkIngestionRetry"),
    ("CanManageAch", "BulkIngestion.Cancel", "FineGrainedPermissions.BulkIngestion.Cancel", "P1Policies.BulkIngestionCancel"),
    ("CanManageAch", "DigitalEnvelope.Encrypt", "FineGrainedPermissions.DigitalEnvelope.Encrypt", "P1Policies.DigitalEnvelopeEncrypt"),
    ("CanManageAch", "DigitalEnvelope.Decrypt", "FineGrainedPermissions.DigitalEnvelope.Decrypt", "P1Policies.DigitalEnvelopeDecrypt"),
    ("CanManageAch", "CanViewPaymentRailCapabilityRegistry", "FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry", "FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry"),
    ("CanManageAch", "Config.Manage", "FineGrainedPermissions.Config.Manage", "P1Policies.ConfigManage"),
    ("CanManageAch", "Integrations.ManageContracts", "FineGrainedPermissions.Integrations.ManageContracts", "P1Policies.IntegrationContractsManage"),
    ("CanManageAch", "Integrations.ManageMappings", "FineGrainedPermissions.Integrations.ManageMappings", "P1Policies.IntegrationMappingsManage"),
    ("CanManageAch", "Integrations.Validate", "FineGrainedPermissions.Integrations.Validate", "P1Policies.IntegrationMappingsValidate"),
    ("CanManageAch", "Integrations.Publish", "FineGrainedPermissions.Integrations.Publish", "P1Policies.IntegrationMappingsPublish"),
    ("CanManageAch", "Integrations.ManageConnections", "FineGrainedPermissions.Integrations.ManageConnections", "P1Policies.IntegrationConnectionsManage"),

    ("CanManageUsers", "AuditLogs.Read", "FineGrainedPermissions.AuditLogs.Read", "P1Policies.AuditLogsRead"),
    ("CanManageUsers", "AuthLogs.Read", "FineGrainedPermissions.AuthLogs.Read", "P1Policies.AuthLogsRead"),
    ("CanManageUsers", "NavigationMenu.Manage", "FineGrainedPermissions.NavigationMenu.Manage", "P1Policies.NavigationMenuManage"),

    ("CanManageCertificates", "Certificates.Read", "FineGrainedPermissions.Certificates.Read", "P1Policies.CertificatesRead"),
    ("CanManageCertificates", "Certificates.UploadPublic", "FineGrainedPermissions.Certificates.UploadPublic", "P1Policies.CertificatesUploadPublic"),
    ("CanManageCertificates", "Certificates.RegisterPrivate", "FineGrainedPermissions.Certificates.RegisterPrivate", "P1Policies.CertificatesRegisterPrivate"),
    ("CanManageCertificates", "Certificates.Revoke", "FineGrainedPermissions.Certificates.Revoke", "P1Policies.CertificatesRevoke"),
]

require(len(EXPECTED_EDGES) == 29, "NAV3.5B must contain exactly 29 legacy->fine policy edges")
require(len({fine for _, fine, _, _ in EXPECTED_EDGES}) == 27, "NAV3.5B must cover exactly 27 unique fine capabilities")
require({legacy for legacy, _, _, _ in EXPECTED_EDGES} == {"CanReadAch", "CanManageAch", "CanManageUsers", "CanManageCertificates"},
        "NAV3.5B legacy source set drifted")
require(all(fine != "TestTools.Access" for _, fine, _, _ in EXPECTED_EDGES), "TestTools.Access must be excluded")

expected_counts = {
    "CanReadAch": 7,
    "CanManageAch": 15,
    "CanManageUsers": 3,
    "CanManageCertificates": 4,
}
for legacy, expected_count in expected_counts.items():
    require(sum(1 for edge in EXPECTED_EDGES if edge[0] == legacy) == expected_count,
            f"{legacy} mapping count drifted")

# Executable model-level migration scenarios. These do not replace the .NET/EF tests, but they
# prove the data-driven relation itself preserves the transitional policy audience while runtime
# execution is unavailable in this environment.
by_legacy_names: dict[str, set[str]] = {}
for legacy, fine, _, _ in EXPECTED_EDGES:
    by_legacy_names.setdefault(legacy, set()).add(fine)

def migrate(existing_legacy: set[str], existing_fine: set[str] | None = None) -> set[str]:
    result = set(existing_fine or set())
    for legacy in existing_legacy:
        result.update(by_legacy_names.get(legacy, set()))
    return result

synthetic_roles = {
    "CustomReadRole": {"CanReadAch"},
    "CustomManageRole": {"CanManageAch"},
    "CustomUserAdminRole": {"CanManageUsers"},
    "CustomCertificateRole": {"CanManageCertificates"},
    "CustomCombinedRole": set(expected_counts),
    "CustomNoLegacyRole": set(),
}
expected_unique_counts = {
    "CustomReadRole": 7,
    "CustomManageRole": 15,
    "CustomUserAdminRole": 3,
    "CustomCertificateRole": 4,
    "CustomCombinedRole": 27,
    "CustomNoLegacyRole": 0,
}
for role_name, legacy_grants in synthetic_roles.items():
    first = migrate(legacy_grants)
    second = migrate(legacy_grants, first)
    require(len(first) == expected_unique_counts[role_name], f"{role_name} derived fine grant count drifted")
    require(first == second, f"{role_name} migration is not idempotent")
    require("TestTools.Access" not in first, f"{role_name} must never derive TestTools.Access")
    fallbacks_by_fine: dict[str, set[str]] = {}
    for legacy, fine, _, _ in EXPECTED_EDGES:
        fallbacks_by_fine.setdefault(fine, set()).add(legacy)
    for fine, policy_fallbacks in fallbacks_by_fine.items():
        before = bool(policy_fallbacks & legacy_grants)
        after = fine in first or bool(policy_fallbacks & legacy_grants)
        require(before == after, f"{role_name} policy audience changed for {fine}")

# Every edge must still be justified by the actual policy expression. We intentionally inspect only
# the target transition policies, not every permission in the system.
external = read("src/Cfa.ACHInterbank.External/DependencyInjectionService.cs")
for legacy, fine_name, fine_token, policy_token in EXPECTED_EDGES:
    start = external.find(f"AddPolicy({policy_token}")
    require(start >= 0, f"Missing policy {policy_token} for {legacy}->{fine_name}")
    block = external[start:start + 1100]
    require(fine_token in block, f"Policy {policy_token} no longer contains {fine_token}")
    require(f'"{legacy}"' in block or f"FineGrainedPermissions.{legacy}" in block,
            f"Policy {policy_token} no longer contains fallback {legacy}")

# Seeder implementation is data-driven and role-name agnostic.
seeder_path = "src/Cfa.ACHInterbank.Persistence/Security/Seeders/LegacyFineGrainedRoleMigrationSeeder.cs"
seeder = read(seeder_path)
require("public const int MigrationOrder = 120;" in seeder, "NAV3.5B seeder order must be 120")
require("public int Order => MigrationOrder;" in seeder, "NAV3.5B seeder must expose deterministic order 120")
require("MigrationRules" in seeder and "MigrationRule" in seeder, "NAV3.5B seeder must expose an explicit data-driven mapping")
require("context.RolePermissions" in seeder, "NAV3.5B must derive migration from persisted RolePermission state")
require("legacyRoleGrants" in seeder and "requestedGrants" in seeder, "NAV3.5B must derive fine grants from existing legacy role grants")
require("HashSet<(Guid RoleId, Guid PermissionId)>" in seeder, "NAV3.5B must union duplicate migration edges")
require("existing.Add(grant)" in seeder, "NAV3.5B must be idempotent on RolePermission pairs")
require("context.RolePermissions.Add(new RolePermission" in seeder, "NAV3.5B must only add RolePermission rows")
require("RoleConfiguration" not in seeder, "NAV3.5B generic migration must not depend on canonical role IDs/names")
require('"Admin"' not in seeder and '"ACH.Operator"' not in seeder, "NAV3.5B generic migration must not branch on canonical role names")
require("TestTools.Access" not in seeder, "NAV3.5B generic migration must not grant TestTools.Access")
for forbidden in ["context.Roles.Add", "context.Users.Add", "context.UserRoles.Add", "context.Permissions.Add", ".Remove(", ".RemoveRange(", "UserPermission"]:
    require(forbidden not in seeder, f"NAV3.5B seeder contains forbidden mutation: {forbidden}")

# Extract the C# mapping and require exact equality with the code-derived matrix.
actual_pairs = re.findall(
    r'new\("(Can(?:ReadAch|ManageAch|ManageUsers|ManageCertificates))",\s*(FineGrainedPermissions(?:\.[A-Za-z0-9_]+)+)\)',
    seeder,
)
expected_pairs = [(legacy, fine_token) for legacy, _, fine_token, _ in EXPECTED_EDGES]
require(len(actual_pairs) == 29, "NAV3.5B C# migration must declare exactly 29 mapping rows")
require(sorted(actual_pairs) == sorted(expected_pairs), "NAV3.5B C# migration mapping differs from live policy-derived matrix")
require(len(set(actual_pairs)) == len(actual_pairs), "NAV3.5B mapping contains duplicate edges")

# Order must be deterministic after NAV3.5A and unique among security seeders.
security_root = ROOT / "src/Cfa.ACHInterbank.Persistence/Security/Seeders"
order_120_files = []
for path in security_root.glob("*.cs"):
    text = path.read_text(encoding="utf-8")
    if re.search(r"(?:MigrationOrder\s*=\s*120|Order\s*=>\s*120)", text):
        order_120_files.append(path.name)
require(order_120_files == ["LegacyFineGrainedRoleMigrationSeeder.cs"], f"Order 120 collision: {order_120_files}")
nav35a = read("src/Cfa.ACHInterbank.Persistence/Security/Seeders/FineGrainedGrantBackfillSeeder.cs")
require("public const int BackfillOrder = 119;" in nav35a, "NAV3.5A seeder order must remain 119")
require("FineGrainedGrantBackfillSeeder" in nav35a, "NAV3.5A seeder must remain intact")

# Canonical post-NAV3.5A expectation: zero extra inserts. Derive this rather than hard-coding role names in the new seeder.
def extract_capability_tokens(source: str, property_name: str) -> set[str]:
    match = re.search(rf"{property_name}.*?=\s*\[(.*?)\];", source, re.S)
    require(match is not None, f"Missing NAV3.5A {property_name}")
    return set(re.findall(r"FineGrainedPermissions(?:\.[A-Za-z0-9_]+)+", match.group(1)))

admin_nav35a = extract_capability_tokens(nav35a, "AdminBackfillCapabilities")
operator_nav35a = extract_capability_tokens(nav35a, "OperatorBackfillCapabilities")
by_legacy: dict[str, set[str]] = {}
for legacy, _, fine_token, _ in EXPECTED_EDGES:
    by_legacy.setdefault(legacy, set()).add(fine_token)
admin_expected = set().union(*(by_legacy[x] for x in ["CanReadAch", "CanManageAch", "CanManageUsers", "CanManageCertificates"]))
operator_expected = set().union(*(by_legacy[x] for x in ["CanReadAch", "CanManageAch"]))
require(admin_nav35a == admin_expected, "NAV3.5A Admin grants no longer fully cover NAV3.5B canonical migration")
require(operator_nav35a == operator_expected, "NAV3.5A ACH.Operator grants no longer fully cover NAV3.5B canonical migration")
require(len(admin_expected) == 27 and len(operator_expected) == 21, "Canonical migration target counts drifted")

# Direct-user permission audit: persisted authorization has UserRole + RolePermission only.
context = read("src/Cfa.ACHInterbank.Persistence/DataBase/AchDbContext.cs")
require("DbSet<RolePermission> RolePermissions" in context, "RolePermission persistence missing")
require("DbSet<UserRole> UserRoles" in context, "UserRole persistence missing")
require("DbSet<UserPermission>" not in context and "UserPermissions =>" not in context, "Unexpected persisted direct UserPermission model")
for path in (ROOT / "src").rglob("*.cs"):
    if "/Migrations/" in str(path).replace("\\", "/"):
        continue
    text = path.read_text(encoding="utf-8", errors="ignore")
    require("class UserPermission" not in text, f"Unexpected UserPermission entity in {path.relative_to(ROOT)}")
    require("UserPermissionConfiguration" not in text, f"Unexpected UserPermissionConfiguration in {path.relative_to(ROOT)}")

auth_repo = read("src/Cfa.ACHInterbank.Persistence/DataBase/Repositories/Security/UserAuthRepository.cs")
for marker in ["Include(u => u.UserRoles)", "ThenInclude(r => r!.RolePermissions)", "ThenInclude(rp => rp.Permission)"]:
    require(marker in auth_repo, f"Authentication repository lost role-permission projection: {marker}")
auth_service = read("src/Cfa.ACHInterbank.Application/Services/Authentication/Implementations/AuthService.cs")
for marker in [
    "user.UserRoles",
    ".SelectMany(x => x.Role!.RolePermissions)",
    ".Select(rp => rp.Permission!.Name!)",
    'claims.AddRange(permissions.Select(permission => new Claim("permission", permission)))',
]:
    require(marker in auth_service, f"Authentication permission claim source drift: {marker}")
require("UserPermission" not in auth_service, "Authentication unexpectedly supports direct-user permission rows")

# Legacy permissions remain persisted and visible in the permission catalog. Current first-party role/permission
# controllers are read-only, so NAV3.5B does not create a new assignment path or disable legacy entries.
permission_config = read("src/Cfa.ACHInterbank.Persistence/Configuration/PermissionConfiguration.cs")
for legacy in expected_counts:
    require(f'Name = "{legacy}"' in permission_config, f"Legacy permission {legacy} disappeared from Permission catalog")
permissions_service = read("src/Cfa.ACHInterbank.Persistence/Security/Services/PermissionsService.cs")
require("_dbContext.Permissions" in permissions_service and ".ToListAsync" in permissions_service,
        "Permissions catalog no longer enumerates persisted Permission rows")
roles_controller = read("src/Cfa.ACHInterbank.Api/Controllers/RolesController.cs")
permissions_controller = read("src/Cfa.ACHInterbank.Api/Controllers/PermissionsController.cs")
for source, name in [(roles_controller, "RolesController"), (permissions_controller, "PermissionsController")]:
    require("[HttpGet]" in source, f"{name} must retain its GET surface")
    require("[HttpPost" not in source and "[HttpPut" not in source and "[HttpDelete" not in source and "[HttpPatch" not in source,
            f"{name} unexpectedly gained role/permission mutation endpoints during NAV3.5B")

# TestTools remains the explicit Admin-only exception owned by its original seeder.
test_tools = read("src/Cfa.ACHInterbank.Persistence/Security/Seeders/TestToolsAdminRolePermissionSeeder.cs")
require("RoleConfiguration.AdminRoleId" in test_tools, "TestTools.Access Admin grant missing")
require("RoleConfiguration.OperatorRoleId" not in test_tools, "ACH.Operator must remain excluded from TestTools.Access")
require("RolePermissions.AnyAsync" in test_tools, "TestTools.Access grant must remain idempotent")

# Dependency/readiness locks. These are deliberately not normalized in NAV3.5B.
app_routes = read("web/ach-interbank-ui/src/app/app-routing.module.ts")
transactions_routes = read("web/ach-interbank-ui/src/app/features/transactions/transactions-routing.module.ts")
integration_routes = read("web/ach-interbank-ui/src/app/features/integrations/integrations.routes.ts")
nacha_security_routes = read("web/ach-interbank-ui/src/app/features/nacha-security/nacha-security-routing.module.ts")
cert_component = read("web/ach-interbank-ui/src/app/features/nacha-security/components/nacha-certificate-manager.component.ts")
require("roles: ['Admin', 'ACH.Operator']" in app_routes and "permissions: ['CanManageAch', 'CanReadAch']" in app_routes,
        "/transactions parent legacy gate changed")
require("permissions: ['Transactions.Create', 'CanManageAch']" in transactions_routes, "Transactions.Create child guard changed")
require("permissions: ['BulkIngestion.Read', 'CanReadAch']" in transactions_routes, "BulkIngestion read child guard changed")
require("roles: ['Admin', 'ACH.Operator']" in app_routes and "permissions: ['Integrations.Read', 'CanReadAch', 'CanManageAch']" in app_routes,
        "/integraciones parent transitional gate changed")
require(integration_routes.count("permissions: ['Integrations.Read', 'CanReadAch']") >= 4,
        "Integration child transitional read guards changed")
require("permissions: [NACHA_SECURITY_PERMISSIONS.canReadAch]" in nacha_security_routes,
        "Sobre digital entry must remain CanReadAch-gated in NAV3.5B")
require("const canReadAuxiliaryContext = this.auth.hasPermission(NACHA_SECURITY_PERMISSIONS.canReadAch);" in cert_component,
        "Certificate auxiliary selector dependency on CanReadAch changed")
require("return this.auth.hasPermission(NACHA_SECURITY_PERMISSIONS.canReadAch)" in cert_component,
        "Certificate upload auxiliary CanReadAch gate changed")

# Source regression locks: NAV3.5B is migration/readiness only. These hashes are from the NAV3.5A FULL ZIP.
BASELINE_HASHES = {
    "src/Cfa.ACHInterbank.External/DependencyInjectionService.cs": "69c4e32480e982b325957e51f4a130e20cd2d9d01879b546053fd56e1b2300f1",
    "src/Cfa.ACHInterbank.Application/Security/FineGrainedPermissions.cs": "3ca012750d4f45547082537002836df540ab5ee79444259cbee6e38f8fbd55b1",
    "src/Cfa.ACHInterbank.Application/Security/P0Policies.cs": "c080408f26c5a228a259f1aabd006463cc780778cba6574df48e114759fa28c1",
    "src/Cfa.ACHInterbank.Application/Security/P1Policies.cs": "a5eb51f927b62a187fe7b0c6c4405e2c13e2cab48d1c0dbfa059b83d13ae3808",
    "src/Cfa.ACHInterbank.Persistence/Configuration/RoleConfiguration.cs": "8ab02e39567d0551a80cd3d8270b17180c250827f91bbe178b7785aaa491ee11",
    "src/Cfa.ACHInterbank.Persistence/Configuration/PermissionConfiguration.cs": "83fce79433ec71bc9b2212e5745d1125c890a3f8431dc7a66ca242275f4ad25e",
    "src/Cfa.ACHInterbank.Persistence/Configuration/RolePermissionConfiguration.cs": "87e8a639603858373ad110e5025d159119ff472d02db7de77786e6e9ef10f619",
    "src/Cfa.ACHInterbank.Persistence/Configuration/UserRoleConfiguration.cs": "1fcec8f15b872c30709c35143e40072e802d7ebdb1e713e869106d03ecd19a9d",
    "src/Cfa.ACHInterbank.Persistence/Configuration/MenuItemRoleConfiguration.cs": "352ee244e52939c0f365fb8f2619167cb7c5a5dfe2da7c8af655413487abd00e",
    "src/Cfa.ACHInterbank.Persistence/Configuration/MenuItemPermissionConfiguration.cs": "d750e2499e548892c2055b8f46866e212d526408f97446076f00a05081b9573c",
    "src/Cfa.ACHInterbank.Application/Navigation/Queries/GetMenuForCurrentUserHandler.cs": "f111e416c181fc1912c297e37012cf7462f49d0434c1ef6ec9416c89b5ed1a61",
    "src/Cfa.ACHInterbank.Persistence/Navigation/Services/MenuItemsService.cs": "af41148cf337585f407dc46e23cc173e03923540582bd7a99872e2b7ad971235",
    "web/ach-interbank-ui/src/app/core/services/navigation.service.ts": "f8b984005ca4f0b99adb3abccd4e0c2e46da5ffc6f8e9a9f9d09ccf84bd2a3de",
    "web/ach-interbank-ui/src/app/core/guards/permission.guard.ts": "7d9541b3899543ad40802b18cb4e734a9c14e59077b732108b31396213afa73a",
    "web/ach-interbank-ui/src/app/core/guards/role.guard.ts": "520fcea2357e5cfe2b19d3ad1d0b284ee84d21149b359025126308210d6de31e",
    "web/ach-interbank-ui/src/app/layout/main-layout.component.ts": "3b9bc7ddd4aa8628d11493ae53fff2829c197cf6d38cdda521fd8c775ca4a3af",
    "web/ach-interbank-ui/src/app/app-routing.module.ts": "14304a5b4db288552a401d71f62d77f0e5c1837bae5e0a4cb0d322a5a8ec75af",
    "web/ach-interbank-ui/src/app/features/transactions/transactions-routing.module.ts": "f087b8574870aec7e2f577213405b2e014403debcc7f8c7e08f61f8eebc5f7ad",
    "web/ach-interbank-ui/src/app/features/integrations/integrations.routes.ts": "7a0106cf22e807826c97151e0e3af540ec30eb85ceeec6408b1e37b117a7b822",
    "web/ach-interbank-ui/src/app/features/nacha-security/nacha-security-routing.module.ts": "8c38d16a314af30192de8bb1c8fe9088f05710dd60f0a3f23a284d15fae54dde",
    "web/ach-interbank-ui/src/app/features/nacha-security/components/nacha-certificate-manager.component.ts": "c741dd6e71f4961ba4bf255f3bc357a77b54e99585e0eddc84c04efd77141eb7",
    "web/ach-interbank-ui/src/app/features/nacha-security/components/digital-envelope-tool.component.ts": "afb244cbe16145dae6b753018a487d5f67cc0cd12478986ad540ae03a9bc9ec9",
    "src/Cfa.ACHInterbank.Application/Services/Authentication/Implementations/AuthService.cs": "1ca97f7d5d0486d80d4801c87092747bff87790f7a1c74d311ca6c2c2f7406bb",
    "src/Cfa.ACHInterbank.Persistence/DataBase/Repositories/Security/UserAuthRepository.cs": "0ec67b4dcc2c6d09986483de8abc0c6f8d4817605feb6ecab3cfb7b1e77ed3e5",
    "src/Cfa.ACHInterbank.Persistence/DataBase/AchDbContext.cs": "8b3f6dfd4a36cff10ac49ad1326eb77c4302132c66ca33c3ba41c0a102faac5b",
}
for path, expected in BASELINE_HASHES.items():
    require(sha256(path) == expected, f"NAV3.5B migration-only regression: {path} changed from NAV3.5A")

# Focal executable tests cover synthetic custom roles, union/idempotence, canonical zero-insert and audience preservation.
test = read("tests/Cfa.ACHInterbank.Tests/NavigationMenuNav35BDeploymentSafeLegacyPermissionMigrationTests.cs")
for marker in [
    "Assert.Equal(29, rules.Count)",
    "Assert.Equal(27, rules.Select",
    "CanonicalPostNav35AState_Nav35BAddsZeroRows_AndIsIdempotent",
    "CustomCanReadAchRole_ReceivesExactlyItsSevenFineEquivalents",
    "CustomCanManageAchRole_ReceivesExactlyItsFifteenFineEquivalents_WithoutTestTools",
    "CustomCanManageUsersRole_ReceivesOnlyItsThreeFineEquivalents",
    "CustomCanManageCertificatesRole_ReceivesExactlyItsFourCertificateEquivalents",
    "CustomRoleWithMultipleLegacyPermissions_ReceivesUnionWithoutDuplicates_AndRerunAddsNothing",
    "CustomRoleWithoutTargetLegacyPermission_ReceivesZeroFineMigrationGrants",
    "CustomRolePolicyAudience_IsIdenticalBeforeAndAfterMigration",
    "Assert.DoesNotContain(FineGrainedPermissions.TestTools.Access",
    "Assert.Equal(120, seeder.Order)",
]:
    require(marker in test, f"Focal NAV3.5B test coverage missing marker: {marker}")

# Documentation/status guardrails.
memory_state = read("docs/ai/navigation-menu/MEMORY_STATE.md")
security_model = read("docs/security/NAVIGATION_MENU_SECURITY_MODEL.md")
project_memory = read("MEMORY.md")
for source, label in [(memory_state, "MEMORY_STATE"), (security_model, "security model"), (project_memory, "MEMORY")]:
    require("NAV3.5B" in source, f"{label} missing NAV3.5B record")
require("IMPLEMENTED_UNVERIFIED_ENVIRONMENT" in memory_state, "NAV2/runtime status guardrail missing")
require("NAV3.5C" in memory_state and "NO" in memory_state[memory_state.rfind("NAV3.5B"):].upper(), "NAV3.5B state must explicitly defer NAV3.5C")

print(f"PASS NAV3.5B deployment-safe legacy permission migration static validator ({checks} assertions)")
print("Migration edges: 29 | Unique fine capabilities: 27 | Legacy sources: 4")
print("Canonical post-NAV3.5A expected inserts: 0 | TestTools.Access: excluded")
print("Direct-user persisted permission mechanism: none detected | Role-based JWT permission claims only")
