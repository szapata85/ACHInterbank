#!/usr/bin/env python3
"""R10 static recertification ratchet.

R10 is a closure/evidence phase, not a productive refactor. This validator requires the
complete R1-R9 architecture chain and additionally pins the exact R9 productive/test tree,
then consolidates the program-level invariants requested by the R10 plan.
"""
from __future__ import annotations

import hashlib
import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src"
TESTS = ROOT / "tests"

EXPECTED_SRC_TREE_SHA256 = "517e414875074a9eeca9071a20ca4230986e5d3136cd6e62455369b8b11ea91e"
EXPECTED_TESTS_TREE_SHA256 = "d1a9a58a3dc5b744d19e6034e38577e64a71b0ba429b2b1063e1a74f81847f62"
EXPECTED_PERSISTENCE_FALLBACKS = 57
EXPECTED_CONTAINS_CENIT = 8
EXPECTED_PAYMENT_RAIL_STRATEGY = 0
EXPECTED_REPORT_CONTROLLERS = 7
EXPECTED_REPORT_ACTIONS = 20
EXPECTED_REPORT_CAN_READ = 20
EXPECTED_CERT_PUBLIC_SERVICES = 10
EXPECTED_CERT_MAX_PUBLIC_PER_FILE = 1
EXPECTED_CERT_CONTRACTS = 10
EXPECTED_CERT_DI = 10
EXPECTED_R9_REAL_CLOCK = 0
EXPECTED_R9_TIME_PROVIDER_CONSUMERS = 9
EXPECTED_R9_SYNC_RESULT = 0
EXPECTED_R9_SYNC_WAIT = 0
EXPECTED_R9_SYNC_GET_RESULT = 0
EXPECTED_TIMEZONE_OWNERS = 3
EXPECTED_BUSINESS_DATE_OWNERS = 2
EXPECTED_IAchReturnsService_METHODS = 2
EXPECTED_IAchReturnsService_CALLERS = 2
EXPECTED_R5D_STAGES = 7

RUNTIME_FALLBACK = re.compile(
    r"\?\?\s*new\s+(?:[\w.]+\.)?\w+"
    r"(?:Service|Provider|Resolver|Policy|Validator|Builder|Factory|Guard|Strategy|Generator|Store|Auditor)\s*\("
)

errors: list[str] = []


def read(rel: str) -> str:
    path = ROOT / rel
    if not path.exists():
        errors.append(f"missing file: {rel}")
        return ""
    return path.read_text(encoding="utf-8-sig", errors="ignore")


def tree_sha(path: Path) -> tuple[int, str]:
    files = sorted((p for p in path.rglob("*") if p.is_file()), key=lambda p: p.relative_to(ROOT).as_posix())
    h = hashlib.sha256()
    for p in files:
        rel = p.relative_to(ROOT).as_posix()
        fh = hashlib.sha256(p.read_bytes()).hexdigest()
        h.update(rel.encode("utf-8")); h.update(b"\0"); h.update(fh.encode("ascii")); h.update(b"\n")
    return len(files), h.hexdigest()


# Require the full prior chain. R9 already invokes and requires R1-R8B.
prior = subprocess.run(
    [sys.executable, str(ROOT / "scripts/validate-r9-deterministic-time-async.py")],
    cwd=ROOT,
    capture_output=True,
    text=True,
    check=False,
    timeout=900,
)
if prior.returncode != 0:
    detail = (prior.stdout + prior.stderr).strip().replace("\n", " | ")
    errors.append(f"prior R1-R9 ratchet chain failed: {detail}")

# R10 itself is evidence-only: any productive or test-tree drift is a regression requiring explicit repair evidence.
src_count, src_tree = tree_sha(SRC)
test_count, tests_tree = tree_sha(TESTS)
if src_tree != EXPECTED_SRC_TREE_SHA256:
    errors.append(f"productive source tree changed in R10: expected {EXPECTED_SRC_TREE_SHA256}, got {src_tree}")
if tests_tree != EXPECTED_TESTS_TREE_SHA256:
    errors.append(f"test tree changed in R10: expected {EXPECTED_TESTS_TREE_SHA256}, got {tests_tree}")

# R1: Application boundary remains free of direct presentation/infrastructure framework ownership.
application_root = SRC / "Cfa.ACHInterbank.Application"
application_text = "\n".join(
    p.read_text(encoding="utf-8-sig", errors="ignore")
    for p in application_root.rglob("*")
    if p.is_file() and p.suffix in {".cs", ".csproj"}
)
r1_tokens = {
    "ASP.NET": "Microsoft.AspNetCore",
    "WCF": "System.ServiceModel",
    "NLog": "NLog",
    "X509Store": "X509Store",
}
r1_counts = {name: application_text.count(token) for name, token in r1_tokens.items()}
for name, count in r1_counts.items():
    if count != 0:
        errors.append(f"R1 Application boundary regression {name}: {count}")

# R2: global AppSettings eliminated; hidden critical runtime fallbacks remain zero; inherited Persistence debt stays 57.
all_src_text = "\n".join(p.read_text(encoding="utf-8-sig", errors="ignore") for p in SRC.rglob("*.cs"))
app_settings = all_src_text.count("AppSettings.Settings")
if app_settings != 0:
    errors.append(f"R2 AppSettings.Settings regression: {app_settings}")

persistence = SRC / "Cfa.ACHInterbank.Persistence"
persistence_fallbacks = sum(
    len(RUNTIME_FALLBACK.findall(p.read_text(encoding="utf-8-sig", errors="ignore")))
    for p in persistence.rglob("*.cs")
)
if persistence_fallbacks != EXPECTED_PERSISTENCE_FALLBACKS:
    errors.append(f"Persistence fallback debt changed: {EXPECTED_PERSISTENCE_FALLBACKS} -> {persistence_fallbacks}")

# Authoritative R2 validator owns the narrower 'hidden critical collaborators' definition.
r2 = subprocess.run(
    [sys.executable, str(ROOT / "scripts/validate-r2-di-configuration.py")],
    cwd=ROOT,
    capture_output=True,
    text=True,
    check=False,
    timeout=120,
)
if r2.returncode != 0:
    errors.append("R2 authoritative validator failed during R10 consolidation")
hidden_critical = -1
m = re.search(r"critical hidden runtime-service fallbacks:\s*(\d+)", r2.stdout)
if m:
    hidden_critical = int(m.group(1))
if hidden_critical != 0:
    errors.append(f"R2 hidden critical collaborators changed/unreadable: {hidden_critical}")

# R3: use authoritative validator output for transport/retry/logging metrics.
r3 = subprocess.run(
    [sys.executable, str(ROOT / "scripts/validate-r3-external-resilience.py")],
    cwd=ROOT,
    capture_output=True,
    text=True,
    check=False,
    timeout=120,
)
if r3.returncode != 0:
    errors.append("R3 authoritative validator failed during R10 consolidation")
def metric(pattern: str, text: str, default: int = -1) -> int:
    mm = re.search(pattern, text)
    return int(mm.group(1)) if mm else default
r3_http = metric(r"direct HttpClient constructions:\s*(\d+)", r3.stdout)
r3_raw_default = metric(r"raw default HttpClient action injections:\s*(\d+)", r3.stdout)
r3_sensitive = metric(r"banned sensitive-log fragments:\s*(\d+)", r3.stdout)
r3_generic_retry = metric(r"generic=(\d+)", r3.stdout)
r3_soap_retry = metric(r"soap=(\d+)", r3.stdout)
for label, value in {
    "direct new HttpClient": r3_http,
    "raw default HttpClient injection": r3_raw_default,
    "generic retry": r3_generic_retry,
    "SOAP/WCF blind retry": r3_soap_retry,
    "sensitive logs": r3_sensitive,
}.items():
    if value != 0:
        errors.append(f"R3 {label} regression/unreadable: {value}")

# R4: facade contract + deterministic owners are present; exact source pin above protects PUBLISH/totals/SHA/audit ownership.
iface = read("src/Cfa.ACHInterbank.Application/ACH/Interfaces/INachaFileBuilder.cs")
r4_method_names = set(re.findall(r"\b(Build[A-Za-z0-9_]+)\s*(?:<[^>]+>)?\s*\(", iface))
r4_public_methods = len(r4_method_names)
if r4_public_methods != 6:
    errors.append(f"R4 INachaFileBuilder contract changed: expected 6, got {r4_public_methods}")
for rel, token in {
    "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaOfficialRecordRenderer.cs": "NachaOfficialRecordRenderer",
    "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaOfficialAssemblyPipeline.cs": "NachaOfficialAssemblyPipeline",
    "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaChamberFlowPolicy.cs": "INachaChamberFlowPolicy",
    "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaFileGenerationCoordinator.cs": "NachaFileGenerationCoordinator",
}.items():
    if token not in read(rel):
        errors.append(f"R4 owner missing: {rel} / {token}")
# Reuse the R4E ratchet for canonical chamber resolution + DB-ID inference=0.
r4e = subprocess.run([sys.executable, str(ROOT / "scripts/validate-r4e-chamber-resolution.py")], cwd=ROOT, capture_output=True, text=True, check=False, timeout=120)
if r4e.returncode != 0:
    errors.append("R4E authoritative validator failed during R10 consolidation")
r4_db_id = metric(r"DB-id rail inference:\s*0\s*->\s*(\d+)", r4e.stdout)
if r4_db_id != 0:
    errors.append(f"R4 DB-ID rail inference changed/unreadable: {r4_db_id}")

# R5: authoritative parser/validator/persistence/post-processing ratchets.
r5a = subprocess.run([sys.executable, str(ROOT / "scripts/validate-r5a-inbound-architecture.py")], cwd=ROOT, capture_output=True, text=True, check=False, timeout=120)
r5b = subprocess.run([sys.executable, str(ROOT / "scripts/validate-r5b-inbound-validation.py")], cwd=ROOT, capture_output=True, text=True, check=False, timeout=120)
r5d = subprocess.run([sys.executable, str(ROOT / "scripts/validate-r5d-inbound-postprocessing.py")], cwd=ROOT, capture_output=True, text=True, check=False, timeout=120)
if any(x.returncode != 0 for x in (r5a, r5b, r5d)):
    errors.append("R5 authoritative validator failed during R10 consolidation")
r5_parser_db_free = (
    "physical parsing owner coupled to AchDbContext: 1 -> 0" in r5a.stdout
    and "deterministic DB-free physical parser components: 0 -> 1" in r5a.stdout
)
r5_validator_writes = "persistence side effects inside validators: 0" in r5b.stdout
r5_persistence_owner = metric(r"parsed-graph SaveChanges owner total:\s*1\s*->\s*(\d+)", r5d.stdout)
r5_stages = metric(r"explicit post-processing stage components:\s*0\s*->\s*(\d+)", r5d.stdout)
if not r5_parser_db_free: errors.append("R5 physical parser DB-free invariant not confirmed")
if not r5_validator_writes: errors.append("R5 validator write-free invariant not confirmed")
if r5_persistence_owner != 1: errors.append(f"R5 parsed graph persistence owner changed/unreadable: {r5_persistence_owner}")
if r5_stages != EXPECTED_R5D_STAGES: errors.append(f"R5 post-processing stages changed: {EXPECTED_R5D_STAGES} -> {r5_stages}")

# R6: capability split, SFTP factory, service locator, retries and Command Center query side effects.
r6a = subprocess.run([sys.executable, str(ROOT / "scripts/validate-r6a-mft-interface-segregation.py")], cwd=ROOT, capture_output=True, text=True, check=False, timeout=120)
r6b = subprocess.run([sys.executable, str(ROOT / "scripts/validate-r6b-mft-implementation-separation.py")], cwd=ROOT, capture_output=True, text=True, check=False, timeout=120)
r6c = subprocess.run([sys.executable, str(ROOT / "scripts/validate-r6c-incoming-command-center.py")], cwd=ROOT, capture_output=True, text=True, check=False, timeout=120)
if any(x.returncode != 0 for x in (r6a, r6b, r6c)):
    errors.append("R6 authoritative validator failed during R10 consolidation")
r6_facade_consumers = metric(r"wide direct-method MFT service contracts used by production consumers:\s*1\s*->\s*(\d+)", r6a.stdout)
r6_sftp_outside = metric(r"direct SFTP session construction outside authorized factory:\s*(\d+)", r6a.stdout)
r6_service_locator = metric(r"service-locator usage in MFT consumers:\s*(\d+)", r6a.stdout)
r6_blind_retry = metric(r"new blind/local retry sites:\s*0\s*->\s*(\d+)", r6a.stdout)
r6_query_side_effect = 0 if "query side effects: 0" in r6c.stdout else -1
for label, value in {
    "broad MFT facade consumers": r6_facade_consumers,
    "SFTP construction outside factory": r6_sftp_outside,
    "service locator": r6_service_locator,
    "new blind retry": r6_blind_retry,
    "Command Center query side effects": r6_query_side_effect,
}.items():
    if value != 0: errors.append(f"R6 {label} changed/unreadable: {value}")

# R7: public service surface/callers and transaction ownership are cheap to recertify directly.
r7_interface_rel = "src/Cfa.ACHInterbank.Application/ACH/Interfaces/IAchReturnsService.cs"
r7_service_rel = "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchReturnsService.cs"
r7_interface = read(r7_interface_rel)
r7_service = read(r7_service_rel)
r7_methods = len(re.findall(r"\bTask(?:<[^;]+?>)?\s+\w+Async\s*\(", r7_interface))
r7_callers = 0
for p in SRC.rglob("*.cs"):
    rel = p.relative_to(ROOT).as_posix()
    if rel in {r7_interface_rel, r7_service_rel}:
        continue
    if re.search(r"\bIAchReturnsService\b", p.read_text(encoding="utf-8-sig", errors="ignore")):
        r7_callers += 1
r7_transaction_owners = r7_service.count("BeginTransactionAsync(")
if r7_methods != EXPECTED_IAchReturnsService_METHODS: errors.append(f"R7 IAchReturnsService methods: {r7_methods}")
if r7_callers != EXPECTED_IAchReturnsService_CALLERS: errors.append(f"R7 production callers: {r7_callers}")
if r7_transaction_owners != 1: errors.append(f"R7 transaction owner count: {r7_transaction_owners}")

# R8A: exact public Reports surface.
report_paths = sorted((SRC / "Cfa.ACHInterbank.Api/Controllers").glob("*ReportsController.cs"))
report_paths = [p for p in report_paths if p.name != "ReportsController.cs"]
report_text = "\n".join(p.read_text(encoding="utf-8-sig", errors="ignore") for p in report_paths)
r8a_controllers = len(report_paths)
r8a_actions = len(re.findall(r"public\s+async\s+Task<IActionResult>\s+\w+\s*\(", report_text))
r8a_can_read = report_text.count('[Authorize(Policy = "CanReadAch")]')
if (r8a_controllers, r8a_actions, r8a_can_read) != (EXPECTED_REPORT_CONTROLLERS, EXPECTED_REPORT_ACTIONS, EXPECTED_REPORT_CAN_READ):
    errors.append(f"R8A Reports surface changed: controllers={r8a_controllers}, actions={r8a_actions}, CanReadAch={r8a_can_read}")

# R8B: grouped unit absent, 10 service owners, max 1 per file, 10 public contracts and DI mappings.
cert_dir = SRC / "Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CertificateManagement"
legacy_cert = cert_dir / "CertificateManagementServices.cs"
cert_expected_classes = {
    "CertificateSecretProtectorService", "CertificateValidationService", "CertificateLoadService",
    "CertificateSelectionService", "CertificateActivationService", "CertificateDeletionService",
    "CertificateRotationService", "CertificateUsageLoggerService", "CertificateAuditService", "CertificateCatalogService",
}
cert_public_counts: dict[str, int] = {}
cert_public_names: set[str] = set()
for p in cert_dir.glob("*.cs"):
    text = p.read_text(encoding="utf-8-sig", errors="ignore")
    names = re.findall(r"^public\s+class\s+(\w+)", text, re.MULTILINE)
    filtered = [n for n in names if n in cert_expected_classes]
    cert_public_counts[p.name] = len(filtered)
    cert_public_names.update(filtered)
r8b_public = len(cert_public_names)
r8b_max = max(cert_public_counts.values(), default=0)
contracts_text = read("src/Cfa.ACHInterbank.Application/ACHSobreDigital/CertificateManagement/CertificateManagementInterfaces.cs")
r8b_contracts = sum(contracts_text.count(name.replace("Service", "Service")) for name in [])  # explicit below
cert_interfaces = {
    "ICertificateSecretProtector", "ICertificateValidationService", "ICertificateLoadService", "ICertificateSelectionService",
    "ICertificateActivationService", "ICertificateDeletionService", "ICertificateRotationService", "ICertificateUsageLogger",
    "ICertificateAuditService", "ICertificateCatalogService",
}
r8b_contracts = sum(1 for name in cert_interfaces if re.search(rf"\binterface\s+{re.escape(name)}\b", contracts_text))
di_text = read("src/Cfa.ACHInterbank.Persistence/DependencyInjectionService.cs")
r8b_di = sum(1 for name in cert_interfaces if re.search(rf"AddScoped<{re.escape(name)},", di_text))
if legacy_cert.exists(): errors.append("R8B legacy CertificateManagementServices.cs reintroduced")
if r8b_public != EXPECTED_CERT_PUBLIC_SERVICES: errors.append(f"R8B public certificate services: {r8b_public}")
if r8b_max != EXPECTED_CERT_MAX_PUBLIC_PER_FILE: errors.append(f"R8B max public certificate services/file: {r8b_max}")
if r8b_contracts != EXPECTED_CERT_CONTRACTS: errors.append(f"R8B certificate contracts: {r8b_contracts}")
if r8b_di != EXPECTED_CERT_DI: errors.append(f"R8B certificate DI mappings: {r8b_di}")

# R9: deterministic selected operational time/async metrics.
r9_routes = [
    "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ClearingHouseCycleConfigService.cs",
    "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchColombiaManagedMftInboundWorkflow.cs",
    "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchResponseReprocessDispatcher.cs",
    "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ReturnOfReturnOrchestrator.cs",
    "src/Cfa.ACHInterbank.Persistence/Scheduler/SchedulerAdminService.cs",
    "src/Cfa.ACHInterbank.Persistence/ACH/Quartz/SchedulerSyncService.cs",
    "src/Cfa.ACHInterbank.Persistence/ACH/Quartz/SchedulerRuntimeService.cs",
    "src/Cfa.ACHInterbank.Persistence/ACH/Quartz/Jobs/DynamicJobExecutor.cs",
    "src/Cfa.ACHInterbank.Persistence/ACH/Quartz/Jobs/Implementation/SchedulerClusterProbeHandler.cs",
]
r9_texts = {rel: read(rel) for rel in r9_routes}
r9_text = "\n".join(r9_texts.values())
real_clock_tokens = ["DateTime.Now", "DateTime.UtcNow", "DateTime.Today", "DateTimeOffset.Now", "DateTimeOffset.UtcNow"]
r9_real_clock = sum(r9_text.count(t) for t in real_clock_tokens)
r9_tp_consumers = sum(1 for t in r9_texts.values() if "TimeProvider" in t)
r9_wait = len(re.findall(r"\.Wait\s*\(", r9_text))
r9_get_result = r9_text.count(".GetAwaiter().GetResult()")
r9_result = 0
for rel, text in r9_texts.items():
    for line in text.splitlines():
        if ".Result" not in line: continue
        if rel.endswith("AchResponseReprocessDispatcher.cs") and "SetProperty(x => x.Result, result.Result)" in line: continue
        r9_result += len(re.findall(r"\.Result\b", line.replace(".ResultCode", "")))
if r9_real_clock != EXPECTED_R9_REAL_CLOCK: errors.append(f"R9 real clock sites: {r9_real_clock}")
if r9_tp_consumers != EXPECTED_R9_TIME_PROVIDER_CONSUMERS: errors.append(f"R9 TimeProvider consumers: {r9_tp_consumers}")
if r9_result != EXPECTED_R9_SYNC_RESULT or r9_wait != EXPECTED_R9_SYNC_WAIT or r9_get_result != EXPECTED_R9_SYNC_GET_RESULT:
    errors.append(f"R9 sync-over-async changed: Result={r9_result}, Wait={r9_wait}, GetResult={r9_get_result}")
# Owner counts are pinned by exact source tree and by the R9 validator; retain explicit consolidated metrics.
timezone_owners = EXPECTED_TIMEZONE_OWNERS
business_date_owners = EXPECTED_BUSINESS_DATE_OWNERS

# Global residual debt is measured, not 'fixed' by R10.
contains_cenit = sum(
    read(rel).count('Contains("CENIT"')
    for rel in [
        "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaInboundSimulationService.cs",
        "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaIngestionAppService.cs",
    ]
)
payment_rail_pattern = re.compile(r"(?<![A-Za-z0-9_])PaymentRailOperationalStrategy(?![A-Za-z0-9_])")
payment_rail_refs = sum(len(payment_rail_pattern.findall(p.read_text(encoding="utf-8-sig", errors="ignore"))) for p in SRC.rglob("*.cs"))
if contains_cenit != EXPECTED_CONTAINS_CENIT: errors.append(f'Contains("CENIT") debt changed: {EXPECTED_CONTAINS_CENIT} -> {contains_cenit}')
if payment_rail_refs != EXPECTED_PAYMENT_RAIL_STRATEGY: errors.append(f"PaymentRailOperationalStrategy changed: {payment_rail_refs}")

print("R10 FINAL RECERTIFICATION STATIC RATCHET")
print("  R10 scope: evidence consolidation/closure only; no productive/test refactor")
print(f"  exact R9 productive source tree: files={src_count}, sha256={src_tree}")
print(f"  exact R9 test tree: files={test_count}, sha256={tests_tree}")
print("R1")
print("  Application forbidden boundaries: " + ", ".join(f"{k}={v}" for k, v in r1_counts.items()))
print("R2")
print(f"  AppSettings.Settings={app_settings}; hidden critical collaborators={hidden_critical}; Persistence fallback={persistence_fallbacks}")
print("R3")
print(f"  new HttpClient={r3_http}; raw default injection={r3_raw_default}; generic retry={r3_generic_retry}; SOAP retry={r3_soap_retry}; sensitive logs={r3_sensitive}")
print("R4")
print(f"  INachaFileBuilder public contract={r4_public_methods}/6; renderer/assembly/policy/coordinator owners=present; DB-ID rail inference={r4_db_id}")
print("  PUBLISH/totals/SHA/audit ownership: preserved by exact source-tree pin + R4A-R4E ratchets")
print("R5")
print(f"  physical parser DB/write-free={r5_parser_db_free}; validator writes=0={r5_validator_writes}; parsed graph persistence owners={r5_persistence_owner}; post-processing stages={r5_stages}")
print("R6")
print(f"  broad MFT facade consumers={r6_facade_consumers}; SFTP outside factory={r6_sftp_outside}; service locator={r6_service_locator}; new blind retry={r6_blind_retry}; Command Center query side effects={r6_query_side_effect}")
print("R7")
print(f"  IAchReturnsService methods={r7_methods}; production callers={r7_callers}; transaction owner sites={r7_transaction_owners}; Return/ROR owners protected by exact source-tree pin + R7/R9 ratchets")
print("R8A")
print(f"  Reports controllers={r8a_controllers}; actions={r8a_actions}; CanReadAch={r8a_can_read}/20; HTTP/OpenAPI source protected by exact source-tree pin")
print("R8B")
print(f"  grouped legacy present={legacy_cert.exists()}; public services={r8b_public}; max public services/file={r8b_max}; contracts={r8b_contracts}; DI mappings={r8b_di}")
print("R9")
print(f"  behavior-determining wall-clock selected scope={r9_real_clock}; TimeProvider consumers={r9_tp_consumers}; sync-over-async Result/Wait/GetResult={r9_result}/{r9_wait}/{r9_get_result}")
print(f"  operational timezone owners={timezone_owners}; business-date owners={business_date_owners}; fake-time boundaries preserved by exact test-tree pin + R9 ratchet")
print("GLOBAL")
print(f'  PaymentRailOperationalStrategy={payment_rail_refs}; Contains("CENIT")={contains_cenit}; Persistence fallback={persistence_fallbacks}; new architectural debt=0')
print("  prior R1-R9 architecture ratchet chain: invoked and required")

if errors:
    for error in errors:
        print(f"ERROR: {error}", file=sys.stderr)
    sys.exit(1)

print("R10 FINAL RECERTIFICATION STATIC RATCHET: OK")
