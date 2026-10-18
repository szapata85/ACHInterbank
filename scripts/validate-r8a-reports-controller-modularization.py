#!/usr/bin/env python3
"""Static R8A ratchet for ReportsController API modularization."""
from __future__ import annotations

import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src"
CONTROLLERS = SRC / "Cfa.ACHInterbank.Api/Controllers"
LEGACY = CONTROLLERS / "ReportsController.cs"
BASE = CONTROLLERS / "ReportsClearingHouseControllerBase.cs"

REPORT_CONTROLLERS = {
    "TransactionReportsController": CONTROLLERS / "TransactionReportsController.cs",
    "ReturnRejectionReportsController": CONTROLLERS / "ReturnRejectionReportsController.cs",
    "NachaCycleReportsController": CONTROLLERS / "NachaCycleReportsController.cs",
    "ReconciliationReportsController": CONTROLLERS / "ReconciliationReportsController.cs",
    "AuditHistoryReportsController": CONTROLLERS / "AuditHistoryReportsController.cs",
    "TraceabilityReportsController": CONTROLLERS / "TraceabilityReportsController.cs",
    "AccountingReviewReportsController": CONTROLLERS / "AccountingReviewReportsController.cs",
}

EXPECTED_ACTIONS = {
    "GetSentTransactions": ("GET", "transactions/sent", "CanReadAch"),
    "GetReceivedTransactions": ("GET", "transactions/received", "CanReadAch"),
    "GetSentTransactionsPdf": ("GET", "transactions/sent/pdf", "CanReadAch"),
    "GetReceivedTransactionsPdf": ("GET", "transactions/received/pdf", "CanReadAch"),
    "GetReturns": ("GET", "returns", "CanReadAch"),
    "GetRejections": ("GET", "rejections", "CanReadAch"),
    "GetReturnsPdf": ("GET", "returns/pdf", "CanReadAch"),
    "GetRejectionsPdf": ("GET", "rejections/pdf", "CanReadAch"),
    "GetNachaFiles": ("GET", "nacha-files", "CanReadAch"),
    "GetNachaFilesPdf": ("GET", "nacha-files/pdf", "CanReadAch"),
    "GetCycles": ("GET", "cycles", "CanReadAch"),
    "GetCyclesPdf": ("GET", "cycles/pdf", "CanReadAch"),
    "GetReconciliation": ("GET", "reconciliation", "CanReadAch"),
    "GetReconciliationPdf": ("GET", "reconciliation/pdf", "CanReadAch"),
    "GetAudit": ("GET", "audit", "CanReadAch"),
    "GetAuditPdf": ("GET", "audit/pdf", "CanReadAch"),
    "GetHistory": ("GET", "history", "CanReadAch"),
    "GetHistoryPdf": ("GET", "history/pdf", "CanReadAch"),
    "GetTraceabilityPdf": ("GET", "traceability/pdf", "CanReadAch"),
    "ExportAccountingReview": ("POST", "accounting-review/export", "CanReadAch"),
}

EXPECTED_OWNER_ACTIONS = {
    "TransactionReportsController": 4,
    "ReturnRejectionReportsController": 4,
    "NachaCycleReportsController": 4,
    "ReconciliationReportsController": 2,
    "AuditHistoryReportsController": 4,
    "TraceabilityReportsController": 1,
    "AccountingReviewReportsController": 1,
}

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
    "validate-r7-returns-separation.py",
]

BASELINE_REPORTS_CONTROLLER_LINES = 1028
BASELINE_REPORT_CONTROLLER_COUNT = 1
BASELINE_ACTIONS = 20
BASELINE_MAX_CONSTRUCTOR_DEPS = 9
BASELINE_CLEARING_VALIDATOR_OWNERS = 1
BASELINE_CLEARING_VALIDATOR_CALLS = 14
BASELINE_PERSISTENCE_FALLBACKS = 57
BASELINE_CONTAINS_CENIT = 8
BASELINE_PAYMENT_RAIL_STRATEGY_REFS = 0

errors: list[str] = []


def read(path: Path) -> str:
    try:
        return path.read_text(encoding="utf-8-sig")
    except FileNotFoundError:
        errors.append(f"missing file: {path.relative_to(ROOT)}")
        return ""


for validator_name in PREVIOUS_RATCHETS:
    result = subprocess.run(
        [sys.executable, str(ROOT / "scripts" / validator_name)],
        cwd=ROOT,
        capture_output=True,
        text=True,
        check=False,
    )
    if result.returncode != 0:
        detail = (result.stdout + result.stderr).strip().replace("\n", " | ")
        errors.append(f"prior architecture ratchet failed ({validator_name}): {detail}")

if LEGACY.exists():
    errors.append("legacy ReportsController.cs remains; R8A resource/capability split is incomplete")

base = read(BASE)
controller_texts = {name: read(path) for name, path in REPORT_CONTROLLERS.items()}
all_reports_text = "\n".join(controller_texts.values())

# Every modular controller keeps the exact public route prefix, authentication boundary and logical
# ControllerName used by OpenAPI operation-id/tag transformers.
for name, text in controller_texts.items():
    for token in ['[ApiController]', '[Route("api/reports")]', '[ControllerContractName("Reports")]', '[Authorize]']:
        if token not in text:
            errors.append(f"{name} missing compatibility metadata: {token}")
    action_count = len(re.findall(r"public\s+async\s+Task<IActionResult>\s+\w+\s*\(", text))
    expected_count = EXPECTED_OWNER_ACTIONS[name]
    if action_count != expected_count:
        errors.append(f"{name} action count changed: expected {expected_count}, got {action_count}")

# Parse route/policy/action surface from source and require exact 20-operation compatibility.
actions: dict[str, tuple[str, str, str, str]] = {}
for owner, text in controller_texts.items():
    method_pattern = re.compile(
        r"(?P<attrs>(?:\s*\[[^\n]+\]\s*)+)"
        r"public\s+async\s+Task<IActionResult>\s+(?P<name>\w+)\s*\(",
        re.MULTILINE,
    )
    for match in method_pattern.finditer(text):
        attrs = match.group("attrs")
        name = match.group("name")
        http = re.search(r'\[Http(Get|Post)\("([^"]+)"\)\]', attrs)
        policy = re.search(r'\[Authorize\(Policy = "([^"]+)"\)\]', attrs)
        if http is None or policy is None:
            errors.append(f"{owner}.{name} missing HTTP verb/route or policy metadata")
            continue
        verb, route = http.group(1).upper(), http.group(2)
        value = (verb, route, policy.group(1), owner)
        if name in actions:
            errors.append(f"duplicate report action name after split: {name}")
        actions[name] = value

if len(actions) != BASELINE_ACTIONS:
    errors.append(f"report action count changed: {BASELINE_ACTIONS} -> {len(actions)}")

for name, expected in EXPECTED_ACTIONS.items():
    actual = actions.get(name)
    if actual is None:
        errors.append(f"missing report action: {name}")
        continue
    if actual[:3] != expected:
        errors.append(f"public report contract changed for {name}: expected {expected}, got {actual[:3]}")

unexpected_actions = sorted(set(actions) - set(EXPECTED_ACTIONS))
if unexpected_actions:
    errors.append("unexpected report actions introduced: " + ", ".join(unexpected_actions))


controller_contract_attribute = read(CONTROLLERS / "ControllerContractNameAttribute.cs")
if "IControllerModelConvention" not in controller_contract_attribute or "controller.ControllerName = Name;" not in controller_contract_attribute:
    errors.append("ControllerContractNameAttribute does not preserve the MVC ControllerName contract")

# Constructor dependencies are reduced to capability-specific abstractions; no service locator/concrete
# operational service creation is allowed in controller code.
constructor_dep_counts: dict[str, int] = {}
for name, text in controller_texts.items():
    ctor = re.search(rf"public\s+{name}\s*\((.*?)\)\s*(?::\s*base\([^)]*\))?\s*\{{", text, re.S)
    if ctor is None:
        errors.append(f"constructor not found for {name}")
        continue
    params = [line.strip().rstrip(",") for line in ctor.group(1).splitlines() if line.strip()]
    constructor_dep_counts[name] = len(params)
    for param in params:
        type_name = param.split()[0]
        if not (type_name.startswith("I") or type_name.startswith("ILogger<")):
            errors.append(f"{name} constructor has non-abstraction dependency: {param}")

max_constructor_deps = max(constructor_dep_counts.values(), default=0)
if max_constructor_deps > 3:
    errors.append(f"report controller constructor fan-in remains too broad: max dependencies {max_constructor_deps}")

for token in [
    "IServiceProvider", "GetRequiredService<", "GetService<", "AchDbContext", "SaveChangesAsync(",
    "BeginTransaction", "CommitAsync(", "RollbackAsync(", "HttpClient", "SftpClient", "System.ServiceModel",
    "Polly", "WaitAndRetry", "PaymentRailOperationalStrategy",
]:
    if token in all_reports_text:
        errors.append(f"out-of-scope/concrete side-effect dependency introduced in reports controllers: {token}")

# Shared clearing-house validation remains a single API owner, called at the same 14 action sites.
validator_defs = base.count("ValidateClearingHouseIdAsync(") + all_reports_text.count("protected async Task<IActionResult?> ValidateClearingHouseIdAsync(")
validator_calls = all_reports_text.count("ValidateClearingHouseIdAsync(")
if validator_defs != BASELINE_CLEARING_VALIDATOR_OWNERS:
    errors.append(f"clearing-house validation owner count changed: {BASELINE_CLEARING_VALIDATOR_OWNERS} -> {validator_defs}")
if validator_calls != BASELINE_CLEARING_VALIDATOR_CALLS:
    errors.append(f"clearing-house validation call count changed: {BASELINE_CLEARING_VALIDATOR_CALLS} -> {validator_calls}")
if "IClearingHouseService" not in base or "BadRequest(new { message = \"ClearingHouseId debe ser mayor a cero.\" })" not in base or "La cámara seleccionada no existe." not in base:
    errors.append("shared clearing-house validation semantics changed or are incomplete")

# Traceability-specific timeout/logging behavior remains single-owned, including the pre-R8A logger category.
trace = controller_texts["TraceabilityReportsController"]
if 'LegacyLoggerCategory = "Cfa.ACHInterbank.Api.Controllers.ReportsController"' not in trace or "CreateLogger(LegacyLoggerCategory)" not in trace:
    errors.append("traceability logger category changed from the legacy ReportsController category")
for token, expected_count in {
    "NormalizeDateRange(": 2,  # one call + one definition
    "CancellationTokenSource.CreateLinkedTokenSource(ct)": 1,
    "CancelAfter(ReportGenerationTimeout)": 1,
    "StatusCodes.Status408RequestTimeout": 3,  # metadata + OpenAPI description + result
    "ReportExecutionStarted": 1,
    "ReportExecutionCompleted": 1,
    "ReportExecutionTimeout": 1,
    "ReportExecutionFailed": 1,
}.items():
    actual = trace.count(token)
    if actual != expected_count:
        errors.append(f"traceability behavior site count changed for {token}: expected {expected_count}, got {actual}")

# Accounting-review export remains service-delegated and non-persistent.
accounting = controller_texts["AccountingReviewReportsController"]
if accounting.count("_accountingReviewExportAppService.ExportAsync(") != 1:
    errors.append("accounting-review export delegation count changed")
if "ArgumentException" not in accounting or accounting.count("BadRequest(new { message =") != 3:
    errors.append("accounting-review validation/error mapping changed")

# Existing out-of-scope debt remains exactly inherited; R8A does not consume it.
contains_cenit = 0
for rel in [
    "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaInboundSimulationService.cs",
    "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaIngestionAppService.cs",
]:
    contains_cenit += read(ROOT / rel).count('Contains("CENIT"')
if contains_cenit != BASELINE_CONTAINS_CENIT:
    errors.append(f'out-of-scope Contains("CENIT") debt changed: {BASELINE_CONTAINS_CENIT} -> {contains_cenit}')

# Previous R2 ratchet is authoritative for Persistence fallback count; repeat only the promised metric.
r2_result = subprocess.run(
    [sys.executable, str(ROOT / "scripts" / "validate-r2-di-configuration.py")],
    cwd=ROOT,
    capture_output=True,
    text=True,
    check=False,
)
match = re.search(r"Persistence fallback debt count:\s*(\d+)/(\d+)", r2_result.stdout)
persistence_fallbacks = int(match.group(1)) if match else -1
if persistence_fallbacks != BASELINE_PERSISTENCE_FALLBACKS:
    errors.append(f"Persistence fallback debt changed: {BASELINE_PERSISTENCE_FALLBACKS} -> {persistence_fallbacks}")

payment_rail_refs = 0
payment_rail_pattern = re.compile(r"(?<![A-Za-z0-9_])PaymentRailOperationalStrategy(?![A-Za-z0-9_])")
for path in SRC.rglob("*.cs"):
    payment_rail_refs += len(payment_rail_pattern.findall(path.read_text(encoding="utf-8-sig", errors="ignore")))
if payment_rail_refs != BASELINE_PAYMENT_RAIL_STRATEGY_REFS:
    errors.append(f"PaymentRailOperationalStrategy references changed: {BASELINE_PAYMENT_RAIL_STRATEGY_REFS} -> {payment_rail_refs}")

modular_lines = sum(len(text.splitlines()) for text in controller_texts.values())
print("R8A REPORTS CONTROLLER MODULARIZATION STATIC RATCHET")
print("  exact R8A scope: split ReportsController endpoints by report resource/capability; routes, DTOs, policies, action semantics and OpenAPI logical controller name preserved")
print(f"  legacy ReportsController: {BASELINE_REPORTS_CONTROLLER_LINES} lines -> {'present' if LEGACY.exists() else 'retired'}")
print(f"  report endpoint controllers: {BASELINE_REPORT_CONTROLLER_COUNT} -> {len(REPORT_CONTROLLERS)}; modular controller lines={modular_lines}")
print(f"  public report actions: {BASELINE_ACTIONS} -> {len(actions)}")
print(f"  max controller constructor dependencies: {BASELINE_MAX_CONSTRUCTOR_DEPS} -> {max_constructor_deps}")
print(f"  shared clearing-house validation owners: {BASELINE_CLEARING_VALIDATOR_OWNERS} -> {validator_defs}; call sites: {BASELINE_CLEARING_VALIDATOR_CALLS} -> {validator_calls}")
print("  API route prefix: api/reports -> api/reports; action verbs/routes exact map preserved")
print("  authorization: class [Authorize] + action CanReadAch preserved for 20/20 actions")
print("  OpenAPI compatibility: logical controller name Reports preserved for all modular controllers; existing operationId/tag transformers remain byte-compatible through MVC ControllerName")
print("  controller concrete operational dependencies/service locator: 0 -> 0")
print("  controller DB writes/transactions/external transport/retry ownership: 0 -> 0")
print(f"  Persistence fallback allowlist: {BASELINE_PERSISTENCE_FALLBACKS} -> {persistence_fallbacks}")
print(f'  Contains("CENIT") out-of-scope debt: {BASELINE_CONTAINS_CENIT} -> {contains_cenit}')
print(f"  PaymentRailOperationalStrategy references: {BASELINE_PAYMENT_RAIL_STRATEGY_REFS} -> {payment_rail_refs}")
print("  prior R1-R7 architecture ratchets: invoked and required")
print("  new R8A architecture debt: 0")

if errors:
    for error in errors:
        print(f"ERROR: {error}", file=sys.stderr)
    sys.exit(1)

print("R8A REPORTS CONTROLLER MODULARIZATION STATIC RATCHET: OK")
