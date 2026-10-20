#!/usr/bin/env python3
"""Static R7 ratchet for the AchReturnsService responsibility split."""
from __future__ import annotations

import hashlib
import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src"
PERSISTENCE = SRC / "Cfa.ACHInterbank.Persistence"
IMPL = PERSISTENCE / "ACH/Services/Implementation"
SERVICE = IMPL / "AchReturnsService.cs"
INTERFACE = SRC / "Cfa.ACHInterbank.Application/ACH/Interfaces/IAchReturnsService.cs"
CONTROLLER = SRC / "Cfa.ACHInterbank.Api/Controllers/AchReturnsController.cs"
DI = PERSISTENCE / "DependencyInjectionService.cs"

COMPONENTS = {
    "eligibility": IMPL / "AchReturnEligibilityPolicyComponent.cs",
    "correlation": IMPL / "AchReturnOriginalCorrelationComponent.cs",
    "batch": IMPL / "AchReturnBatchComposerComponent.cs",
    "artifact": IMPL / "AchReturnArtifactGenerationComponent.cs",
    "cause": IMPL / "AchReturnCausePolicyComponent.cs",
    "state": IMPL / "AchReturnStateTransitionComponent.cs",
    "audit": IMPL / "AchReturnAuditMonitoringComponent.cs",
}
MODELS = IMPL / "AchReturnGenerationComponents.cs"

BASELINE_SERVICE_LINES = 1066
BASELINE_PUBLIC_METHODS = 2
BASELINE_PRODUCTION_CALLERS = 2
BASELINE_PERSISTENCE_FALLBACKS = 57
BASELINE_CONTAINS_CENIT = 8
BASELINE_PAYMENT_RAIL_STRATEGY_REFS = 0

PROTECTED_HASHES = {
    "src/Cfa.ACHInterbank.Application/ACH/Interfaces/IAchReturnsService.cs": "f97e5b83d24e61a9a6e31749964531f22c4f54ad5d2a6eb376d7876672e37f0a",
    "src/Cfa.ACHInterbank.Api/Controllers/AchReturnsController.cs": "a34feb30aff8c68a47f023204e0ec8988aff0bbda30bcc6c9e3730702a08fc15",
    "src/Cfa.ACHInterbank.Persistence/DependencyInjectionService.cs": "afa274217ae14e517380aa2b6260901170d8512a4d9f62ced4f9661f3ec715d2",
    "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchIncomingReturnIngestionService.cs": "0699942d3388934156bbb6daac56898e1a57e8f62a0c7c8927aa21d39f503926",
    "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchReturnOfReturnEligibilityService.cs": "cfae403dd8b0e0a03cf51b20a18cff8c5f068b9bb6cd702d8150302076e3b3f8",
    "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchReturnOfReturnFileGenerationService.cs": "e18cf9966f8731ea2515f713c2aec45fe3823f4cc23334a47d6fda93f8d56e90",
    "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ReturnOfReturnOrchestrator.cs": "441f5070f5b90922299c2be5e2c22a2812bafd58f712635605e293ee90b21d7e",
    "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitReturnOfReturnService.cs": "956cfb2052ff45ba54115b73cbcceff6542e6f49f7bcfa3cac2accb24fdd6d90",
    "src/Cfa.ACHInterbank.Api/Controllers/AchReturnOfReturnController.cs": "6dffd9a0559aa461336848b11e6d169dde28c9e6a33b6c8e6978a96797746dff",
    "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaFileBuilder.cs": "16a92720d996823cc064eeb6a2036a867bae14b976ae7b9cbd11badc82855a08",
}

# R9 is authorized to make the existing Return-of-Return SLA clock deterministic without
# reopening R7 ownership/contracts. Accept the exact R7 baseline or the exact R9 time-seam
# implementation; every other protected R7 boundary remains byte-identical.
AUTHORIZED_R9_PROTECTED_HASHES = {
    "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ReturnOfReturnOrchestrator.cs":
        "2ca1154a281ff1f1d1a944cad9677f2bb3d5fb69e8fe640ece0df2276ea399da",
}

RUNTIME_FALLBACK = re.compile(
    r"\?\?\s*new\s+(?:[\w.]+\.)?\w+"
    r"(?:Service|Provider|Resolver|Policy|Validator|Builder|Factory|Guard|Strategy|Generator|Store|Auditor)\s*\("
)

errors: list[str] = []

PREVIOUS_RATCHETS = [
    "validate-solution-projects.py",
    "validate-r2-di-configuration.py",
    "validate-r3-external-resilience.py",
    "validate-r4a-nacha-builder-facade.py",
    "validate-r4b-nacha-record-renderers.py",
    "validate-r4c-nacha-assembly-pipeline.py",
    "validate-r4d-chamber-flow-policies.py",
    "validate-r4e-chamber-resolution.py",
    "validate-r5a-inbound-architecture.py",
    "validate-r5b-inbound-validation.py",
    "validate-r5c-inbound-persistence.py",
    "validate-r5d-inbound-postprocessing.py",
    "validate-r6a-mft-interface-segregation.py",
    "validate-r6b-mft-implementation-separation.py",
    "validate-r6c-incoming-command-center.py",
]


def read(path: Path) -> str:
    try:
        return path.read_text(encoding="utf-8-sig")
    except FileNotFoundError:
        errors.append(f"missing file: {path.relative_to(ROOT)}")
        return ""


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


for validator_name in PREVIOUS_RATCHETS:
    validator_path = ROOT / "scripts" / validator_name
    result = subprocess.run(
        [sys.executable, str(validator_path)],
        cwd=ROOT,
        capture_output=True,
        text=True,
        check=False)
    if result.returncode != 0:
        detail = (result.stdout + result.stderr).strip().replace("\n", " | ")
        errors.append(f"prior architecture ratchet failed ({validator_name}): {detail}")

service = read(SERVICE)
interface = read(INTERFACE)
controller = read(CONTROLLER)
component_text = {name: read(path) for name, path in COMPONENTS.items()}
r7_text = "\n".join([service, read(MODELS), *component_text.values()])

application_root = SRC / "Cfa.ACHInterbank.Application"
application_text = "\n".join(
    path.read_text(encoding="utf-8-sig", errors="ignore")
    for path in application_root.rglob("*")
    if path.is_file() and path.suffix in {".cs", ".csproj"}
)
for token in ["Microsoft.AspNetCore", "System.ServiceModel", "NLog", "X509Store", "HttpContext", "IHttpContextAccessor"]:
    if token in application_text:
        errors.append(f"R1 Application boundary regressed: found {token}")

for rel, expected in PROTECTED_HASHES.items():
    path = ROOT / rel
    if not path.exists():
        errors.append(f"protected R7 boundary missing: {rel}")
    else:
        actual = sha256(path)
        allowed = {expected}
        authorized_r9 = AUTHORIZED_R9_PROTECTED_HASHES.get(rel)
        if authorized_r9 is not None:
            allowed.add(authorized_r9)
        if actual not in allowed:
            errors.append(f"protected R7 boundary changed without authorization: {rel}")

public_methods = len(re.findall(r"\bTask(?:<[^;]+?>)?\s+\w+Async\s*\(", interface))
if public_methods != BASELINE_PUBLIC_METHODS:
    errors.append(f"IAchReturnsService public method count changed: {BASELINE_PUBLIC_METHODS} -> {public_methods}")

production_callers: list[str] = []
for path in SRC.rglob("*.cs"):
    if path in {INTERFACE, SERVICE}:
        continue
    text = path.read_text(encoding="utf-8-sig", errors="ignore")
    if re.search(r"\bIAchReturnsService\b", text):
        production_callers.append(path.relative_to(ROOT).as_posix())
if len(production_callers) != BASELINE_PRODUCTION_CALLERS:
    errors.append(
        f"IAchReturnsService production caller count changed: {BASELINE_PRODUCTION_CALLERS} -> {len(production_callers)} "
        f"({', '.join(production_callers)})"
    )

service_lines = len(service.splitlines())
if service_lines >= 260:
    errors.append(f"AchReturnsService remains oversized after R7 split: {service_lines} lines")

for name, path in COMPONENTS.items():
    expected_class = {
        "eligibility": "AchReturnEligibilityPolicyComponent",
        "correlation": "AchReturnOriginalCorrelationComponent",
        "batch": "AchReturnBatchComposerComponent",
        "artifact": "AchReturnArtifactGenerationComponent",
        "cause": "AchReturnCausePolicyComponent",
        "state": "AchReturnStateTransitionComponent",
        "audit": "AchReturnAuditMonitoringComponent",
    }[name]
    if f"class {expected_class}" not in component_text[name]:
        errors.append(f"R7 {name} owner missing expected class {expected_class}")

required_delegations = [
    "_correlation.ResolveScopeAsync",
    "_correlation.LoadAsync",
    "_eligibilityPolicy.ValidatePreEligibilityAsync",
    "_eligibilityPolicy.ResolveNormalizedReasonAsync",
    "_causePolicy.ValidateAsync",
    "_batchAssembly.Prepare",
    "_batchAssembly.AllocateAsync",
    "_batchAssembly.BuildAssembly",
    "_artifactGeneration.GenerateAsync",
    "_auditMonitoring.CompareReturnShadow",
    "_stateTransitions.ApplyAsync",
]
for token in required_delegations:
    if token not in service:
        errors.append(f"facade missing R7 delegation: {token}")

# Each target responsibility has one owner and is absent from the facade implementation body.
ownership = {
    "EvaluateOutgoingReturnAsync": "eligibility",
    "ValidateAchColombiaR10Async": "eligibility",
    "CenitIncomingReturnPolicyRequest": "eligibility",
    "IncomingNachaTransactionLinks": "correlation",
    "ResolveCenitCycleEvidenceAsync": "correlation",
    "ReserveRangeAsync": "batch",
    "new NachaReturnOutEntry": "batch",
    "BuildReturnOutAsync": "artifact",
    "GenerateExternalNameAsync": "artifact",
    "AchCauseCodePolicyRequest": "cause",
    "TransitionAsync": "state",
    'eventType = "ReturnFileGenerated"': "audit",
    "PAYMENT_RAIL_SHADOW_COMPARE_RETURN": "audit",
}
for token, owner in ownership.items():
    holders = [name for name, text in component_text.items() if token in text]
    if holders != [owner]:
        errors.append(f"R7 responsibility '{token}' owners changed: expected [{owner}], got {holders}")
    if token in service:
        errors.append(f"AchReturnsService still owns extracted R7 responsibility: {token}")

for token, expected in {
    "ReserveRangeAsync(": 1,
    "BuildReturnOutAsync(": 2,
    "EvaluateOutgoingReturnAsync(": 1,
    "GenerateExternalNameAsync(": 1,
    "TransitionAsync(": 1,
    "ACHCOL_DEV14_CLAIMS_ONLY": 1,
    "CENIT_ROR_NOT_ORDINARY_RETURN": 2,
}.items():
    actual = r7_text.count(token)
    if actual != expected:
        errors.append(f"R7 semantic/side-effect site count changed for {token}: expected {expected}, got {actual}")

if service.count("ResolveReturnExternalFileNameWithoutPolicyAsync") != 0:
    errors.append("legacy no-policy filename fallback became reachable from AchReturnsService")

# The facade remains the transaction/persistence coordinator so write ordering stays visible and unchanged.
if service.count("BeginTransactionAsync(") != 1:
    errors.append(f"ReturnOut transaction owner changed: expected 1 BeginTransactionAsync in facade, got {service.count('BeginTransactionAsync(')}")
if service.count("SaveChangesAsync(") != 2:
    errors.append(f"ReturnOut facade SaveChanges sites changed: expected 2, got {service.count('SaveChangesAsync(')}")
if service.count("AddRange(allocated.GeneratedRows)") != 1:
    errors.append("ReturnOut generated-row persistence owner moved or duplicated")
if "RelationalDatabaseExceptionClassifier.IsUniqueViolation" not in service:
    errors.append("ReturnOut unique-conflict recovery semantics moved/removed")
if service.count("_returnGenerationLockService.AcquireAsync") != 1:
    errors.append("ReturnOut generation-lock ownership moved/duplicated")

for name, text in component_text.items():
    if "SaveChangesAsync(" in text or "BeginTransactionAsync(" in text:
        errors.append(f"R7 component {name} introduced transaction/persistence commit ownership")

# No external transport/retry/schema/service-locator ownership may appear in the R7 extraction.
for token in [
    "HttpClient", "SftpClient", "SshNet", "IServiceProvider", "GetRequiredService<", "GetService<",
    "WaitAndRetry", "Polly", "Task.Delay(", "Thread.Sleep(", "MigrationBuilder", "CreateTable(",
    "EntityTypeBuilder", "IAchColombiaManagedFileExchangeService", "ICenitGatewayTransportAdapter",
]:
    if token in r7_text:
        errors.append(f"out-of-scope R7 side effect/coupling introduced: {token}")

# Preserve legacy constructor compatibility/fallback count in Persistence, without adding hidden infrastructure debt.
constructor_tokens = [
    "AchDbContext context",
    "IAchRegulatoryCatalogService? regulatoryCatalogService = null",
    "IAchReturnEligibilityService? returnEligibilityService = null",
    "IAchReturnGenerationLockService? returnGenerationLockService = null",
    "IExternalFileNamePolicy? externalFileNamePolicy = null",
    "IAchCauseCodePolicy? causeCodePolicy = null",
    "IAchStateTransitionService? stateTransitionService = null",
    "INachaFileBuilder? nachaFileBuilder = null",
    "IAchReturnTraceSequenceService? returnTraceSequenceService = null",
    "ICenitIncomingReturnPolicy? cenitReturnPolicy = null",
]
for token in constructor_tokens:
    if token not in service:
        errors.append(f"AchReturnsService compatibility constructor lost dependency: {token}")

persistence_fallbacks = 0
for path in PERSISTENCE.rglob("*.cs"):
    persistence_fallbacks += len(RUNTIME_FALLBACK.findall(path.read_text(encoding="utf-8-sig", errors="ignore")))
if persistence_fallbacks != BASELINE_PERSISTENCE_FALLBACKS:
    errors.append(f"Persistence fallback debt changed: {BASELINE_PERSISTENCE_FALLBACKS} -> {persistence_fallbacks}")

contains_cenit = 0
for rel in [
    "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaInboundSimulationService.cs",
    "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaIngestionAppService.cs",
]:
    contains_cenit += read(ROOT / rel).count('Contains("CENIT"')
if contains_cenit != BASELINE_CONTAINS_CENIT:
    errors.append(f'out-of-scope Contains("CENIT") debt changed: {BASELINE_CONTAINS_CENIT} -> {contains_cenit}')

payment_rail_strategy_refs = 0
payment_rail_strategy_pattern = re.compile(r"(?<![A-Za-z0-9_])PaymentRailOperationalStrategy(?![A-Za-z0-9_])")
for path in SRC.rglob("*.cs"):
    payment_rail_strategy_refs += len(
        payment_rail_strategy_pattern.findall(path.read_text(encoding="utf-8-sig", errors="ignore"))
    )
if payment_rail_strategy_refs != BASELINE_PAYMENT_RAIL_STRATEGY_REFS:
    errors.append(
        f"PaymentRailOperationalStrategy reference count changed: "
        f"{BASELINE_PAYMENT_RAIL_STRATEGY_REFS} -> {payment_rail_strategy_refs}"
    )

# Security/API/DI contracts are protected by exact hashes; summarize their structural counts too.
api_gets = controller.count("[HttpGet(")
api_posts = controller.count("[HttpPost(")
authorize = controller.count("[Authorize")

print("R7 RETURNS / RETURN-OF-RETURN STATIC RATCHET")
print(f"  prior solution/R2-R6C ratchets executed by R7 validator: {len(PREVIOUS_RATCHETS)}; failures={sum(1 for error in errors if error.startswith('prior architecture ratchet failed'))}")
print("  R1 Application forbidden host/transport/logging tokens: ASP.NET=0, WCF=0, NLog=0, X509Store=0, HttpContext=0")
print("  exact R7 scope: reduce AchReturnsService responsibilities only; preserve accepted Return/ROR policy and dedicated ROR owners")
print(f"  AchReturnsService lines: {BASELINE_SERVICE_LINES} -> {service_lines}")
print(f"  extracted responsibility owners: 0 -> {len(COMPONENTS)} (eligibility, correlation, batch, artifact, cause, state, audit/monitoring)")
print(f"  public IAchReturnsService methods: {BASELINE_PUBLIC_METHODS} -> {public_methods}")
print(f"  production IAchReturnsService callers: {BASELINE_PRODUCTION_CALLERS} -> {len(production_callers)}")
print("  public controller/API: unchanged by hash; GET actions=%d, POST actions=%d, Authorize attributes=%d" % (api_gets, api_posts, authorize))
print(f"  transaction owner: AchReturnsService 1 -> {service.count('BeginTransactionAsync(')}")
print(f"  facade SaveChangesAsync sites: 2 -> {service.count('SaveChangesAsync(')}; extracted components=0")
print("  trace allocation owner: AchReturnsService -> AchReturnBatchComposerComponent; ReserveRangeAsync sites remain 1")
print("  NACHA artifact owner: AchReturnsService -> AchReturnArtifactGenerationComponent; BuildReturnOutAsync sites remain 2")
print("  eligibility/cause owners: generic/R10/CENIT eligibility and rail-flow cause policy separated without policy reinterpretation")
print("  state transition owner: AchReturnsService -> AchReturnStateTransitionComponent; canonical IAchStateTransitionService retained")
print("  audit/monitoring owner: AchReturnsService -> AchReturnAuditMonitoringComponent; event identity/hash/shadow compare retained")
print("  dedicated incoming Return/ROR boundaries: protected by exact hashes; ReturnOfReturnOrchestrator accepts only the R7 baseline or authorized R9 deterministic-time hash")
print("  direct HTTP/SFTP/MFT construction in R7 scope: 0; service locator: 0; new blind retry sites: 0")
print("  DB schema/migration dependencies introduced by R7: 0")
print(f"  Persistence fallback debt: {BASELINE_PERSISTENCE_FALLBACKS} -> {persistence_fallbacks}")
print(f'  Contains("CENIT") out-of-scope debt: {BASELINE_CONTAINS_CENIT} -> {contains_cenit}')
print(f"  PaymentRailOperationalStrategy references: {BASELINE_PAYMENT_RAIL_STRATEGY_REFS} -> {payment_rail_strategy_refs}")
print("  new R7 architecture debt: 0")

if errors:
    for error in errors:
        print(f"ERROR: {error}")
    raise SystemExit(1)

print("R7 RETURNS / RETURN-OF-RETURN STATIC RATCHET: OK")
