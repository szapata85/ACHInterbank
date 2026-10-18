#!/usr/bin/env python3
"""Static R8B ratchet for the selected CertificateManagementServices cohesion cleanup."""
from __future__ import annotations

import hashlib
import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src"
CERT_DIR = SRC / "Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CertificateManagement"
LEGACY = CERT_DIR / "CertificateManagementServices.cs"

EXPECTED_FILES = {
    "CertificateManagementMapper.cs": "0587764d10582c1730f901de81b958c1b39a56bbd7e9a252175ad612fb40c560",
    "CertificateSecretProtectorService.cs": "6be4e4ba627ec6a85022a4dcfaa0a915be78937b8fe6dc55c194f5381297a6b1",
    "CertificateValidationService.cs": "d5cf23445f6d967373e9f27f4e183162437e1fc603bf0fd6d5902a9282879920",
    "CertificateLoadService.cs": "10c5b58f57d90cc15167496a6549b33090e5dc352aed22d69baef9ee18ff0bff",
    "CertificateSelectionService.cs": "4abac9d2b84530cc8ee2a958d3f8ca5ecad6bb75f7d5e60939fc9026566478de",
    "CertificateActivationService.cs": "0ca81e10ce5729b4ae324fdd8bbd6756c64972db630955b19fb786a8a1a1e767",
    "CertificateDeletionService.cs": "08edf57b9cad778550e26365d4d941db3a579177052e2e9ae0e359c4daed2fbe",
    "CertificateRotationService.cs": "65055e143ebca40267015135b83e7b3a0e2651e993b2d8661f1405770f075d10",
    "CertificateUsageLoggerService.cs": "ed153dfffbbbce4272b2f7c587359a80f84d2e088c36a56b9e0cdf0692900a8f",
    "CertificateAuditService.cs": "824ec580909175e0c729375a969c935630342635b51287cc312a63fa8b9fb004",
    "CertificateCatalogService.cs": "3a22ad78074a3e21f85f710813139f5e76f1fe9e7fdc180c698845ce34a26f24",
}

PROTECTED_HASHES = {
    "src/Cfa.ACHInterbank.Application/ACHSobreDigital/CertificateManagement/CertificateManagementInterfaces.cs": "a2263f6bff4a1f75d0ef7225c589a4930080621f749d49ab491b39488bba43bf",
    "src/Cfa.ACHInterbank.Application/ACHSobreDigital/CertificateManagement/CertificateManagementDtos.cs": "a222ba661ac9126bfab8f81c3b6e986e1d692ae6546cc96ce3e8bc7e682c4a93",
    "src/Cfa.ACHInterbank.Persistence/DependencyInjectionService.cs": "afa274217ae14e517380aa2b6260901170d8512a4d9f62ced4f9661f3ec715d2",
    "src/Cfa.ACHInterbank.Api/Controllers/CertificateManagementController.cs": "3237c2580d29a954994a64d8ecc96787b961d895672d23c455042e44ad532f6d",
    "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/Seeders/NachaConfigOfficialProfilesSeeder.cs": "849f7bf3c5a5f1a02b83cda6915056dca63cf7e8f14e43b5c8481b3f066a92e6",
    "docs/architecture/refactor-ca-solid-resilience-r8a.md": "f7f1fb6c5a5c3841da216813c12abcf723b420d269bc9e4bd945a4f98583ba4b",
    "scripts/validate-r8a-reports-controller-modularization.py": "5f6d105b73d9ff0cc1deb10d96f345ed5cae9b397ca6e5ac3febd55498038819",
    "src/Cfa.ACHInterbank.Api/Controllers/TransactionReportsController.cs": "29891f717a05ca73f6d1ba754a91ea830afbfe7d000114e91a9ec9fa03873efe",
    "src/Cfa.ACHInterbank.Api/Controllers/ReturnRejectionReportsController.cs": "4f804205b198ca22e7c177ed7f21fadabf5db3e4283e522b12a6743a230545d6",
    "src/Cfa.ACHInterbank.Api/Controllers/NachaCycleReportsController.cs": "95d243c4cb1f9167e0dde76d98fcf9c5f373458f412862a4c418af28c53b8e7e",
    "src/Cfa.ACHInterbank.Api/Controllers/ReconciliationReportsController.cs": "8b3e67e84c8980291d0c7d60075c295034982fa4a729e448dec2daaf4e2f1491",
    "src/Cfa.ACHInterbank.Api/Controllers/AuditHistoryReportsController.cs": "1b6894b1dd2d1027ad7f5f19e7fd8cf26776766e8a9c2521d09f6885106cc44c",
    "src/Cfa.ACHInterbank.Api/Controllers/TraceabilityReportsController.cs": "c81b86615ef5bc15e692bc966f78324e6b7e7ed6f2b08a7b4dc51b6bfa24f589",
    "src/Cfa.ACHInterbank.Api/Controllers/AccountingReviewReportsController.cs": "58e622acbd5da4f7d1ec8fa6e7066bf8e6ae60521edaf8b52d5464f2cb12d538",
    "src/Cfa.ACHInterbank.Api/Controllers/ReportsClearingHouseControllerBase.cs": "4a3d8d8b145865500d95640ca1b7ad6908cc59fd08041b3cee8564969053d24e",
    "src/Cfa.ACHInterbank.Api/Controllers/ControllerContractNameAttribute.cs": "57d3e4d12393223bbb6cfbd574896147f24ba59242b1f9d8ac7b1c266f5f9180",
    "src/Cfa.ACHInterbank.Api/OpenApi/OpenApiOperationIdTransformer.cs": "da4679201fd519066e6a10da3d8e93bdda0ce13781e1a9cf920b7f7c7edd3f56",
    "src/Cfa.ACHInterbank.Api/OpenApi/OpenApiIntegrationTagsTransformer.cs": "6f6f75867b0a6ea461c2dfc1174097e9f95d13d7083cef26863fa1a36cf1ea14",
}

EXPECTED_PUBLIC_CLASSES = {
    "CertificateSecretProtectorService": "ICertificateSecretProtector",
    "CertificateValidationService": "ICertificateValidationService",
    "CertificateLoadService": "ICertificateLoadService",
    "CertificateSelectionService": "ICertificateSelectionService",
    "CertificateActivationService": "ICertificateActivationService",
    "CertificateDeletionService": "ICertificateDeletionService",
    "CertificateRotationService": "ICertificateRotationService",
    "CertificateUsageLoggerService": "ICertificateUsageLogger",
    "CertificateAuditService": "ICertificateAuditService",
    "CertificateCatalogService": "ICertificateCatalogService",
}

BASELINE_LINES = 1327
BASELINE_COMPILATION_UNITS = 1
BASELINE_PUBLIC_SERVICE_CLASSES = 10
BASELINE_MAX_PUBLIC_SERVICES_PER_FILE = 10
BASELINE_SAVE_CHANGES = 12
BASELINE_ADD_SITES = 23
BASELINE_REMOVE_SITES = 2
BASELINE_AS_NO_TRACKING = 20
BASELINE_UTC_NOW = 15
BASELINE_ZERO_MEMORY = 1
BASELINE_PERSISTENCE_FALLBACKS = 57
BASELINE_CONTAINS_CENIT = 8
BASELINE_PAYMENT_RAIL_STRATEGY_REFS = 0

errors: list[str] = []


def sha256_bytes(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def sha256_file(path: Path) -> str:
    return sha256_bytes(path.read_bytes())


def read(path: Path) -> str:
    try:
        return path.read_text(encoding="utf-8-sig")
    except FileNotFoundError:
        errors.append(f"missing file: {path.relative_to(ROOT)}")
        return ""


def normalized_body_hash(path: Path) -> str:
    text = read(path).replace("\r\n", "\n")
    marker = "namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.CertificateManagement;\n\n"
    if marker not in text:
        errors.append(f"unexpected namespace/header in {path.name}")
        return ""
    body = text.split(marker, 1)[1]
    return sha256_bytes(body.encode("utf-8"))


# Re-run the complete prior R1-R8A ratchet chain through the existing R8A validator.
# Exact hashes and lightweight public-surface checks below add local R8B protection on top.
prior = subprocess.run(
    [sys.executable, str(ROOT / "scripts/validate-r8a-reports-controller-modularization.py")],
    cwd=ROOT,
    capture_output=True,
    text=True,
    check=False,
)
if prior.returncode != 0:
    detail = (prior.stdout + prior.stderr).strip().replace("\n", " | ")
    errors.append(f"prior R1-R8A ratchet chain failed: {detail}")

if LEGACY.exists():
    errors.append("legacy CertificateManagementServices.cs remains; R8B physical owner split is incomplete")

for name, expected_body_hash in EXPECTED_FILES.items():
    path = CERT_DIR / name
    if not path.exists():
        errors.append(f"missing R8B file: {path.relative_to(ROOT)}")
        continue
    actual_hash = normalized_body_hash(path)
    if actual_hash != expected_body_hash:
        errors.append(f"R8A service/helper body changed while splitting {name}: expected {expected_body_hash}, got {actual_hash}")

for rel, expected_hash in PROTECTED_HASHES.items():
    path = ROOT / rel
    if not path.exists():
        errors.append(f"missing protected file: {rel}")
        continue
    actual = sha256_file(path)
    if actual != expected_hash:
        errors.append(f"protected R8A/R8B compatibility file changed: {rel} expected={expected_hash} actual={actual}")

service_texts: dict[str, str] = {}
public_class_counts: dict[str, int] = {}
for file_name in EXPECTED_FILES:
    path = CERT_DIR / file_name
    text = read(path)
    service_texts[file_name] = text
    public_class_counts[file_name] = len(re.findall(r"^public\s+class\s+\w+", text, re.MULTILINE))

all_split_text = "\n".join(service_texts.values())
public_matches = re.findall(r"^public\s+class\s+(\w+)\s*:\s*(\w+)", all_split_text, re.MULTILINE)
actual_public_map = dict(public_matches)
if actual_public_map != EXPECTED_PUBLIC_CLASSES:
    errors.append(f"public certificate service ownership changed: expected {EXPECTED_PUBLIC_CLASSES}, got {actual_public_map}")

for file_name, count in public_class_counts.items():
    expected = 0 if file_name == "CertificateManagementMapper.cs" else 1
    if count != expected:
        errors.append(f"{file_name} public service class count changed: expected {expected}, got {count}")

if all_split_text.count("[Scoped]") != BASELINE_PUBLIC_SERVICE_CLASSES:
    errors.append(f"scoped service markers changed: expected {BASELINE_PUBLIC_SERVICE_CLASSES}, got {all_split_text.count('[Scoped]')}")

# Preserve the public contract and DI mapping exactly; additionally ensure every service remains registered once.
di = read(ROOT / "src/Cfa.ACHInterbank.Persistence/DependencyInjectionService.cs")
for class_name, interface_name in EXPECTED_PUBLIC_CLASSES.items():
    registration = f"AddScoped<{interface_name}, ACH.Services.Implementation.CertificateManagement.{class_name}>()"
    if di.count(registration) != 1:
        errors.append(f"DI registration changed for {interface_name}/{class_name}: count={di.count(registration)}")

# Preserve side-effect ownership/counts exactly. R8B is source-cohesion only, not behavior relocation.
counts = {
    "SaveChangesAsync": all_split_text.count("SaveChangesAsync("),
    "Add": all_split_text.count(".Add("),
    "Remove": all_split_text.count(".Remove("),
    "AsNoTracking": all_split_text.count("AsNoTracking("),
    "DateTime.UtcNow": all_split_text.count("DateTime.UtcNow"),
    "ZeroMemory": all_split_text.count("CryptographicOperations.ZeroMemory"),
}
expected_counts = {
    "SaveChangesAsync": BASELINE_SAVE_CHANGES,
    "Add": BASELINE_ADD_SITES,
    "Remove": BASELINE_REMOVE_SITES,
    "AsNoTracking": BASELINE_AS_NO_TRACKING,
    "DateTime.UtcNow": BASELINE_UTC_NOW,
    "ZeroMemory": BASELINE_ZERO_MEMORY,
}
for name, expected in expected_counts.items():
    if counts[name] != expected:
        errors.append(f"certificate side-effect/behavior site count changed for {name}: {expected} -> {counts[name]}")

for token in [
    "BeginTransaction", "CommitAsync(", "RollbackAsync(", "HttpClient", "SftpClient", "System.ServiceModel",
    "WaitAndRetry", "IServiceProvider", "GetRequiredService<", "GetService<", "PaymentRailOperationalStrategy",
]:
    if token in all_split_text:
        errors.append(f"new out-of-scope infrastructure/service-locator/retry token in R8B certificate services: {token}")

# Out-of-scope debt and global strategy ratchets promised by the phase request.
contains_cenit = 0
for rel in [
    "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaInboundSimulationService.cs",
    "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaIngestionAppService.cs",
]:
    contains_cenit += read(ROOT / rel).count('Contains("CENIT"')
if contains_cenit != BASELINE_CONTAINS_CENIT:
    errors.append(f'out-of-scope Contains("CENIT") debt changed: {BASELINE_CONTAINS_CENIT} -> {contains_cenit}')

payment_rail_pattern = re.compile(r"(?<![A-Za-z0-9_])PaymentRailOperationalStrategy(?![A-Za-z0-9_])")
payment_rail_refs = 0
for path in SRC.rglob("*.cs"):
    payment_rail_refs += len(payment_rail_pattern.findall(path.read_text(encoding="utf-8-sig", errors="ignore")))
if payment_rail_refs != BASELINE_PAYMENT_RAIL_STRATEGY_REFS:
    errors.append(f"PaymentRailOperationalStrategy references changed: {BASELINE_PAYMENT_RAIL_STRATEGY_REFS} -> {payment_rail_refs}")

# Lightweight R8A public-surface preservation (all source is additionally hash-protected).
report_files = [
    ROOT / "src/Cfa.ACHInterbank.Api/Controllers/TransactionReportsController.cs",
    ROOT / "src/Cfa.ACHInterbank.Api/Controllers/ReturnRejectionReportsController.cs",
    ROOT / "src/Cfa.ACHInterbank.Api/Controllers/NachaCycleReportsController.cs",
    ROOT / "src/Cfa.ACHInterbank.Api/Controllers/ReconciliationReportsController.cs",
    ROOT / "src/Cfa.ACHInterbank.Api/Controllers/AuditHistoryReportsController.cs",
    ROOT / "src/Cfa.ACHInterbank.Api/Controllers/TraceabilityReportsController.cs",
    ROOT / "src/Cfa.ACHInterbank.Api/Controllers/AccountingReviewReportsController.cs",
]
report_text = "\n".join(read(path) for path in report_files)
report_actions = len(re.findall(r"public\s+async\s+Task<IActionResult>\s+\w+\s*\(", report_text))
report_can_read = report_text.count('[Authorize(Policy = "CanReadAch")]')
report_contract_names = report_text.count('[ControllerContractName("Reports")]')
if report_actions != 20 or report_can_read != 20 or report_contract_names != 7:
    errors.append(f"R8A Reports surface changed: actions={report_actions}, CanReadAch={report_can_read}, contractNames={report_contract_names}")

# R8A already executes the authoritative R2 ratchet and reports the preserved metric.
# Reuse that result instead of spawning R2 a second time after the nested R1-R8A chain.
m = re.search(r"Persistence fallback allowlist:\s*57\s*->\s*(\d+)", prior.stdout)
persistence_fallbacks = int(m.group(1)) if m else -1
if persistence_fallbacks != BASELINE_PERSISTENCE_FALLBACKS:
    errors.append(f"Persistence fallback debt changed or could not be read from the R8A chain: {BASELINE_PERSISTENCE_FALLBACKS} -> {persistence_fallbacks}")

split_lines = sum(len(text.splitlines()) for text in service_texts.values())
max_public_per_file = max(public_class_counts.values(), default=0)

print("R8B CERTIFICATE MANAGEMENT SERVICE COHESION STATIC RATCHET")
print("  exact R8B scope: selected plan-named CertificateManagementServices compilation unit only; physically separate existing service owners without changing class bodies/contracts/DI/API")
print(f"  legacy grouped compilation unit: {BASELINE_LINES} lines / {BASELINE_PUBLIC_SERVICE_CLASSES} public services -> {'present' if LEGACY.exists() else 'retired'}")
print(f"  certificate management compilation units: {BASELINE_COMPILATION_UNITS} -> {len(EXPECTED_FILES)}; split lines including repeated headers={split_lines}")
print(f"  public certificate service classes: {BASELINE_PUBLIC_SERVICE_CLASSES} -> {len(actual_public_map)}; max public service classes/file: {BASELINE_MAX_PUBLIC_SERVICES_PER_FILE} -> {max_public_per_file}")
print("  public Application certificate contracts: byte-identical; DTO contracts: byte-identical")
print("  production DI registrations/lifetimes: 10 scoped mappings -> 10 scoped mappings; exact registration file byte-identical")
print("  CertificateManagementController HTTP/OpenAPI/authorization surface: byte-identical")
print("  service/helper implementation bodies: byte-identical after line-ending normalization")
print(f"  persistence side effects: SaveChangesAsync {BASELINE_SAVE_CHANGES} -> {counts['SaveChangesAsync']}; Add {BASELINE_ADD_SITES} -> {counts['Add']}; Remove {BASELINE_REMOVE_SITES} -> {counts['Remove']}")
print("  explicit transactions/external transports/retry/service-locator ownership: 0 -> 0")
print(f"  AsNoTracking sites: {BASELINE_AS_NO_TRACKING} -> {counts['AsNoTracking']}; DateTime.UtcNow sites: {BASELINE_UTC_NOW} -> {counts['DateTime.UtcNow']}; ZeroMemory sites: {BASELINE_ZERO_MEMORY} -> {counts['ZeroMemory']}")
print("  official profile seeder: byte-identical and intentionally out of selected R8B item")
print(f"  Persistence fallback allowlist: {BASELINE_PERSISTENCE_FALLBACKS} -> {persistence_fallbacks}")
print(f'  Contains("CENIT") out-of-scope debt: {BASELINE_CONTAINS_CENIT} -> {contains_cenit}')
print(f"  PaymentRailOperationalStrategy references: {BASELINE_PAYMENT_RAIL_STRATEGY_REFS} -> {payment_rail_refs}")
print(f"  R8A Reports protected surface: actions 20 -> {report_actions}; CanReadAch 20 -> {report_can_read}; logical controller name markers 7 -> {report_contract_names}")
print("  prior R1-R8A architecture ratchet chain: invoked and required; R8A additionally protected by exact hashes + public-surface ratchet")
print("  new R8B architecture debt: 0")

if errors:
    for error in errors:
        print(f"ERROR: {error}", file=sys.stderr)
    sys.exit(1)

print("R8B CERTIFICATE MANAGEMENT SERVICE COHESION STATIC RATCHET: OK")
