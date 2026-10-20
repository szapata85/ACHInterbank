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


TARGETS = {
    "TestTools.Access": ("TestToolsPermissionSeeder.cs", "820d447b-2c29-4255-9627-a47b0e8ee641", "FineGrainedPermissions.TestTools.Access"),
    "Transactions.Create": ("TransactionsCreatePermissionSeeder.cs", "5c31592a-98cf-4ea2-a5b4-47e9c0803f42", "FineGrainedPermissions.Transactions.Create"),
    "Nacha.Upload": ("NachaUploadPermissionSeeder.cs", "9d97cb63-8bbe-4253-970c-a490f3c80e10", "FineGrainedPermissions.Nacha.Upload"),
    "Returns.Read": ("ReturnsReadPermissionSeeder.cs", "fa144b62-95c2-4540-b2f8-110516eeb837", "FineGrainedPermissions.Returns.Read"),
    "Returns.GenerateFile": ("ReturnsPermissionSeeder.cs", "826efd89-3c7e-4237-92b6-ad5b8d7f53ac", "FineGrainedPermissions.Returns.GenerateFile"),
    "BulkIngestion.Read": ("BulkIngestionTrackingPermissionSeeder.cs", "4902ced3-1144-432c-afc3-4d8f945c2ca8", "FineGrainedPermissions.BulkIngestion.Read"),
    "BulkIngestion.Upload": ("BulkIngestionUploadPermissionSeeder.cs", "e8194742-09ca-4148-9567-75bb24923f94", "FineGrainedPermissions.BulkIngestion.Upload"),
    "BulkIngestion.Retry": ("BulkIngestionTrackingPermissionSeeder.cs", "a2584f2a-ab66-4d30-b7f8-c69ca057d79e", "FineGrainedPermissions.BulkIngestion.Retry"),
    "BulkIngestion.Cancel": ("BulkIngestionTrackingPermissionSeeder.cs", "fd20a8b5-739b-4384-8cce-4189bd5af106", "FineGrainedPermissions.BulkIngestion.Cancel"),
    "Certificates.Read": ("CertificateManagementPermissionSeeder.cs", "b40c402c-d283-4ba9-a9b2-b63aa583b599", "FineGrainedPermissions.Certificates.Read"),
    "Certificates.UploadPublic": ("CertificateManagementPermissionSeeder.cs", "22b2d28a-e4fb-49b1-bb1a-5fe6dc517c27", "FineGrainedPermissions.Certificates.UploadPublic"),
    "Certificates.RegisterPrivate": ("CertificateManagementPermissionSeeder.cs", "469a6f60-df2c-43ac-88ef-d5760d58121b", "FineGrainedPermissions.Certificates.RegisterPrivate"),
    "Certificates.Revoke": ("CertificateManagementPermissionSeeder.cs", "2cba2029-c98a-4cf2-9e57-6d81851b74fb", "FineGrainedPermissions.Certificates.Revoke"),
    "DigitalEnvelope.Encrypt": ("DigitalEnvelopeOperationPermissionSeeder.cs", "d951161d-06c5-4e31-a574-188e5094f59d", "FineGrainedPermissions.DigitalEnvelope.Encrypt"),
    "DigitalEnvelope.Decrypt": ("DigitalEnvelopeOperationPermissionSeeder.cs", "7ccf7a90-d66f-4e63-bcb9-92275118c7e1", "FineGrainedPermissions.DigitalEnvelope.Decrypt"),
    "AuditLogs.Read": ("SystemRecordPermissionsSeeder.cs", "bafe62e5-5984-4138-9c02-4621443b5286", "FineGrainedPermissions.AuditLogs.Read"),
    "AuthLogs.Read": ("SystemRecordPermissionsSeeder.cs", "5e97df51-c57d-4855-b670-757fa4b5b6f6", "FineGrainedPermissions.AuthLogs.Read"),
    "NavigationLogs.Read": ("SystemRecordPermissionsSeeder.cs", "5de95eb2-18d0-4d9a-86c5-265f0a717bcb", "FineGrainedPermissions.NavigationLogs.Read"),
    "NavigationMenu.Manage": ("NavigationMenuPermissionSeeder.cs", "5f847759-419c-4cf5-b2bb-2a849b8fa33c", "FineGrainedPermissions.NavigationMenu.Manage"),
    "CanViewPaymentRailCapabilityRegistry": ("PaymentRailCapabilityRegistryPermissionSeeder.cs", "9c178737-a650-4437-a68b-9b3f21555e02", "FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry"),
    "Config.Read": ("ConfigurationPermissionSeeder.cs", "15d0e235-eb36-4f7d-a922-d935dc1af7e2", "FineGrainedPermissions.Config.Read"),
    "Config.Manage": ("ConfigurationPermissionSeeder.cs", "6e9d334b-d77a-4576-814f-8e1f987a421f", "FineGrainedPermissions.Config.Manage"),
    "Integrations.Read": ("IntegrationReadPermissionSeeder.cs", "e6cfca32-17a9-45c0-a6f9-2a023317fbce", "FineGrainedPermissions.Integrations.Read"),
    "Integrations.ManageContracts": ("IntegrationContractPermissionSeeder.cs", "40776ed1-238c-4a59-a890-894a1f7d9c8d", "FineGrainedPermissions.Integrations.ManageContracts"),
    "Integrations.ManageMappings": ("IntegrationMappingPermissionSeeder.cs", "a78e21ac-846c-470d-a9a5-5d4bd019bf08", "FineGrainedPermissions.Integrations.ManageMappings"),
    "Integrations.Validate": ("IntegrationMappingPermissionSeeder.cs", "67d58589-d51e-44b2-8177-2650d93bc46d", "FineGrainedPermissions.Integrations.Validate"),
    "Integrations.Publish": ("IntegrationMappingPermissionSeeder.cs", "911b72f4-ef59-48e4-86de-7d559dbafb1d", "FineGrainedPermissions.Integrations.Publish"),
    "Integrations.ManageConnections": ("IntegrationConnectionPermissionSeeder.cs", "289e92ea-abd4-43a9-b93a-23b06fb03362", "FineGrainedPermissions.Integrations.ManageConnections"),
}

ADMIN_TOKENS = {
    token for name, (_, _, token) in TARGETS.items() if name != "TestTools.Access"
}
OPERATOR_EXCLUDED = {
    "TestTools.Access",
    "Certificates.UploadPublic", "Certificates.RegisterPrivate", "Certificates.Revoke",
    "AuditLogs.Read", "AuthLogs.Read", "NavigationMenu.Manage",
}
OPERATOR_TOKENS = {
    token for name, (_, _, token) in TARGETS.items() if name not in OPERATOR_EXCLUDED
}

require(len(TARGETS) == 28, "NAV3.5A must evaluate exactly 28 fine-grained capabilities")
require(len(ADMIN_TOKENS) == 27, "Admin backfill must contain 27 new grants")
require(len(OPERATOR_TOKENS) == 21, "ACH.Operator backfill must contain 21 new grants")

fine = read("src/Cfa.ACHInterbank.Application/Security/FineGrainedPermissions.cs")
for name, (_, _, token) in TARGETS.items():
    require(name in fine or token in fine, f"FineGrainedPermissions is missing {name}")
    all_token = token.removeprefix("FineGrainedPermissions.")
    require(all_token in fine[fine.index("AllPermissions"):], f"AllPermissions is missing {name}")

# NAV3.5A deliberately excludes existing/non-materialized NAV3-adjacent permissions from the migration inventory.
for token in [
    "FineGrainedPermissions.Nacha.Generate",
    "FineGrainedPermissions.Certificates.Activate",
    "FineGrainedPermissions.Certificates.Validate",
    "FineGrainedPermissions.Certificates.Audit",
]:
    require(token not in ADMIN_TOKENS and token not in OPERATOR_TOKENS, f"Unexpected NAV3.5A target: {token}")

seeder_root = "src/Cfa.ACHInterbank.Persistence/Security/Seeders"
for name, (filename, stable_id, token) in TARGETS.items():
    source = read(f"{seeder_root}/{filename}")
    require(stable_id in source.lower(), f"Stable permission ID drift for {name}")
    require(token in source, f"Materializer {filename} no longer persists {name}")

roles = re.sub(r"\s+", "", read("src/Cfa.ACHInterbank.Persistence/Configuration/RoleConfiguration.cs"))
require('AdminRoleId=Guid.Parse("1f8602da-6415-43f8-b61d-cb396f8577f1")' in roles, "Admin stable ID drift")
require('OperatorRoleId=Guid.Parse("a51746c2-0710-4d79-97b1-5b4368326f56")' in roles, "ACH.Operator stable ID drift")
require(roles.count("newRole{") == 2, "Canonical seed must still define exactly two roles")
require('Name="Admin"' in roles and 'Name="ACH.Operator"' in roles, "Canonical seeded role names drifted")

role_permissions = re.sub(r"\s+", "", read("src/Cfa.ACHInterbank.Persistence/Configuration/RolePermissionConfiguration.cs"))
for role_symbol, permission_symbols in {
    "AdminRoleId": ["ManageAchPermissionId", "ReadAchPermissionId", "ManageUsersPermissionId", "ManageCertificatesPermissionId"],
    "OperatorRoleId": ["ManageAchPermissionId", "ReadAchPermissionId"],
}.items():
    for permission_symbol in permission_symbols:
        require(
            f"RoleId=RoleConfiguration.{role_symbol},PermissionId=PermissionConfiguration.{permission_symbol}" in role_permissions,
            f"Legacy fallback population drift: {role_symbol} -> {permission_symbol}",
        )
require(
    "RoleId=RoleConfiguration.OperatorRoleId,PermissionId=PermissionConfiguration.ManageUsersPermissionId" not in role_permissions,
    "ACH.Operator must not gain CanManageUsers in canonical seed",
)
require(
    "RoleId=RoleConfiguration.OperatorRoleId,PermissionId=PermissionConfiguration.ManageCertificatesPermissionId" not in role_permissions,
    "ACH.Operator must not gain CanManageCertificates in canonical seed",
)

backfill_path = f"{seeder_root}/FineGrainedGrantBackfillSeeder.cs"
backfill = read(backfill_path)
require("public const int BackfillOrder = 119;" in backfill, "NAV3.5A backfill order must be 119")
require("public int Order => BackfillOrder;" in backfill, "Backfill must expose deterministic Order 119")
require("TestTools.Access" not in backfill, "NAV3.5A must not regrant TestTools.Access; existing Admin-only seeder owns it")
require("context.RolePermissions.Add(new RolePermission" in backfill, "Backfill must add RolePermission rows")
for forbidden in ["context.Permissions.Add", "context.Roles.Add", "context.UserRoles.Add", "UserPermission", ".Remove(", ".RemoveRange("]:
    require(forbidden not in backfill, f"Backfill contains forbidden mutation: {forbidden}")
require("AnyAsync" not in backfill or "RolePermissions" in backfill, "Backfill idempotence must be RolePermission-scoped")
require("existing.Add((grant.RoleId, permissionId))" in backfill, "Backfill must de-duplicate requested RolePermission pairs")


def array_tokens(property_name: str) -> set[str]:
    match = re.search(rf"{property_name}.*?=\s*\[(.*?)\];", backfill, re.S)
    require(match is not None, f"Missing {property_name}")
    return set(re.findall(r"FineGrainedPermissions(?:\.[A-Za-z0-9_]+)+", match.group(1)))


require(array_tokens("AdminBackfillCapabilities") == ADMIN_TOKENS, "Admin backfill capability set drifted")
require(array_tokens("OperatorBackfillCapabilities") == OPERATOR_TOKENS, "ACH.Operator backfill capability set drifted")
require(sum(1 for path in (ROOT / seeder_root).glob("*.cs") if re.search(r"(?:Order\s*=>\s*119|BackfillOrder\s*=\s*119)", path.read_text(encoding="utf-8"))) == 1,
        "Order 119 must be unique among security seeders")

# Existing TestTools.Access decision remains Admin-only and idempotent.
test_tools_grant = read(f"{seeder_root}/TestToolsAdminRolePermissionSeeder.cs")
require("RoleConfiguration.AdminRoleId" in test_tools_grant, "TestTools.Access Admin grant missing")
require("RoleConfiguration.OperatorRoleId" not in test_tools_grant, "TestTools.Access must remain Admin-only")
require("RolePermissions.AnyAsync" in test_tools_grant, "TestTools.Admin grant must remain idempotent")

# Policy regression: all compatibility alternatives stay in place. The source is also hash-locked below.
external = read("src/Cfa.ACHInterbank.External/DependencyInjectionService.cs")
policy_expectations = {
    "P0Policies.TransactionsCreate": ["FineGrainedPermissions.Transactions.Create", '"CanManageAch"'],
    "P0Policies.ReturnsRead": ["FineGrainedPermissions.Returns.Read", '"CanReadAch"'],
    "P0Policies.ReturnsGenerateFile": ["FineGrainedPermissions.Returns.GenerateFile", '"CanManageAch"'],
    "P1Policies.NachaUpload": ["FineGrainedPermissions.Nacha.Upload", '"CanManageAch"'],
    "P1Policies.BulkIngestionRead": ["FineGrainedPermissions.BulkIngestion.Read", '"CanReadAch"'],
    "P1Policies.BulkIngestionUpload": ["FineGrainedPermissions.BulkIngestion.Upload", '"CanManageAch"'],
    "P1Policies.BulkIngestionRetry": ["FineGrainedPermissions.BulkIngestion.Retry", '"CanManageAch"'],
    "P1Policies.BulkIngestionCancel": ["FineGrainedPermissions.BulkIngestion.Cancel", '"CanManageAch"'],
    "P1Policies.CertificatesRead": ["FineGrainedPermissions.Certificates.Read", "FineGrainedPermissions.CanManageCertificates", '"CanReadAch"'],
    "P1Policies.CertificatesUploadPublic": ["FineGrainedPermissions.Certificates.UploadPublic", "FineGrainedPermissions.CanManageCertificates"],
    "P1Policies.CertificatesRegisterPrivate": ["FineGrainedPermissions.Certificates.RegisterPrivate", "FineGrainedPermissions.CanManageCertificates"],
    "P1Policies.CertificatesRevoke": ["FineGrainedPermissions.Certificates.Revoke", "FineGrainedPermissions.CanManageCertificates"],
    "P1Policies.DigitalEnvelopeEncrypt": ["FineGrainedPermissions.DigitalEnvelope.Encrypt", '"CanManageAch"'],
    "P1Policies.DigitalEnvelopeDecrypt": ["FineGrainedPermissions.DigitalEnvelope.Decrypt", '"CanManageAch"'],
    "P1Policies.AuditLogsRead": ["FineGrainedPermissions.AuditLogs.Read", '"CanManageUsers"'],
    "P1Policies.AuthLogsRead": ["FineGrainedPermissions.AuthLogs.Read", '"CanManageUsers"'],
    "P1Policies.NavigationLogsRead": ["FineGrainedPermissions.NavigationLogs.Read", '"CanReadAch"'],
    "P1Policies.NavigationMenuManage": ["FineGrainedPermissions.NavigationMenu.Manage", '"CanManageUsers"'],
    "FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry": ["FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry", '"CanManageAch"', '"CanReadAch"'],
    "P1Policies.ConfigRead": ["FineGrainedPermissions.Config.Read", '"CanReadAch"'],
    "P1Policies.ConfigManage": ["FineGrainedPermissions.Config.Manage", '"CanManageAch"'],
    "P1Policies.IntegrationsRead": ["FineGrainedPermissions.Integrations.Read", '"CanReadAch"'],
    "P1Policies.IntegrationContractsManage": ["FineGrainedPermissions.Integrations.ManageContracts", '"CanManageAch"'],
    "P1Policies.IntegrationMappingsManage": ["FineGrainedPermissions.Integrations.ManageMappings", '"CanManageAch"'],
    "P1Policies.IntegrationMappingsValidate": ["FineGrainedPermissions.Integrations.Validate", '"CanManageAch"'],
    "P1Policies.IntegrationMappingsPublish": ["FineGrainedPermissions.Integrations.Publish", '"CanManageAch"'],
    "P1Policies.IntegrationConnectionsManage": ["FineGrainedPermissions.Integrations.ManageConnections", '"CanManageAch"'],
}
for policy, terms in policy_expectations.items():
    start = external.find(f"AddPolicy({policy}")
    require(start >= 0, f"Missing policy {policy}")
    block = external[start:start + 900]
    for term in terms:
        require(term in block, f"Policy {policy} lost transition term {term}")

# Source regression locks: NAV3.5A is grants-only. Any drift here means menu, SPA, API policy, role seed,
# navigation filtering, guards, or parent-route behavior changed during the backfill.
BASELINE_HASHES = {
    "src/Cfa.ACHInterbank.External/DependencyInjectionService.cs": "69c4e32480e982b325957e51f4a130e20cd2d9d01879b546053fd56e1b2300f1",
    "src/Cfa.ACHInterbank.Application/Security/FineGrainedPermissions.cs": "3ca012750d4f45547082537002836df540ab5ee79444259cbee6e38f8fbd55b1",
    "src/Cfa.ACHInterbank.Application/Security/P0Policies.cs": "c080408f26c5a228a259f1aabd006463cc780778cba6574df48e114759fa28c1",
    "src/Cfa.ACHInterbank.Application/Security/P1Policies.cs": "a5eb51f927b62a187fe7b0c6c4405e2c13e2cab48d1c0dbfa059b83d13ae3808",
    "src/Cfa.ACHInterbank.Persistence/Configuration/RoleConfiguration.cs": "8ab02e39567d0551a80cd3d8270b17180c250827f91bbe178b7785aaa491ee11",
    "src/Cfa.ACHInterbank.Persistence/Configuration/RolePermissionConfiguration.cs": "87e8a639603858373ad110e5025d159119ff472d02db7de77786e6e9ef10f619",
    "src/Cfa.ACHInterbank.Persistence/Configuration/MenuItemRoleConfiguration.cs": "352ee244e52939c0f365fb8f2619167cb7c5a5dfe2da7c8af655413487abd00e",
    "src/Cfa.ACHInterbank.Persistence/Configuration/MenuItemPermissionConfiguration.cs": "d750e2499e548892c2055b8f46866e212d526408f97446076f00a05081b9573c",
    "src/Cfa.ACHInterbank.Application/Navigation/Queries/GetMenuForCurrentUserHandler.cs": "f111e416c181fc1912c297e37012cf7462f49d0434c1ef6ec9416c89b5ed1a61",
    "web/ach-interbank-ui/src/app/core/services/navigation.service.ts": "f8b984005ca4f0b99adb3abccd4e0c2e46da5ffc6f8e9a9f9d09ccf84bd2a3de",
    "web/ach-interbank-ui/src/app/core/guards/permission.guard.ts": "7d9541b3899543ad40802b18cb4e734a9c14e59077b732108b31396213afa73a",
    "web/ach-interbank-ui/src/app/core/guards/role.guard.ts": "520fcea2357e5cfe2b19d3ad1d0b284ee84d21149b359025126308210d6de31e",
    "web/ach-interbank-ui/src/app/layout/main-layout.component.ts": "3b9bc7ddd4aa8628d11493ae53fff2829c197cf6d38cdda521fd8c775ca4a3af",
    "web/ach-interbank-ui/src/app/app-routing.module.ts": "14304a5b4db288552a401d71f62d77f0e5c1837bae5e0a4cb0d322a5a8ec75af",
    "web/ach-interbank-ui/src/app/features/transactions/transactions-routing.module.ts": "f087b8574870aec7e2f577213405b2e014403debcc7f8c7e08f61f8eebc5f7ad",
    "web/ach-interbank-ui/src/app/features/integrations/integrations.routes.ts": "7a0106cf22e807826c97151e0e3af540ec30eb85ceeec6408b1e37b117a7b822",
}
for path, expected in BASELINE_HASHES.items():
    require(sha256(path) == expected, f"NAV3.5A grants-only regression: {path} changed from NAV3.4R")

# No direct-user permission model exists in the canonical context, and the new seeder does not mutate users/user roles.
context = read("src/Cfa.ACHInterbank.Persistence/DataBase/AchDbContext.cs")
require("DbSet<UserPermission>" not in context and "UserPermissions =>" not in context, "Unexpected direct UserPermission persistence model")
require("context.UserRoles" not in backfill and "context.Users" not in backfill, "Backfill must not mutate users or user-role assignments")

# Focal executable test contract for environments where .NET is available.
test = read("tests/Cfa.ACHInterbank.Tests/NavigationMenuNav35AFineGrainedGrantBackfillTests.cs")
for marker in [
    "Assert.Equal(28, target.Length)",
    "Assert.Equal(49, after.Length)",
    "Assert.Equal(28, admin.Length)",
    "Assert.Equal(21, operatorGrants.Length)",
    "SeededRolePolicyAudience_IsIdenticalBeforeAndAfterBackfill",
    "Assert.DoesNotContain(FineGrainedPermissions.TestTools.Access, operatorGrants)",
    "Assert.Equal(userRolesBefore",
]:
    require(marker in test, f"Focal NAV3.5A test coverage missing marker: {marker}")

print(f"PASS NAV3.5A fine-grained grant backfill static validator ({checks} assertions)")
print("Inventory: 28 capabilities | New RolePermissions: 48 | Admin: 27 new (+ existing TestTools.Access) | ACH.Operator: 21 new")
print("Capability-level SKIP: 0 | Explicit ACH.Operator role-capability SKIPs: 7")
