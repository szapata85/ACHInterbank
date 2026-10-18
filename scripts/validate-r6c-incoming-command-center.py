#!/usr/bin/env python3
from pathlib import Path
import hashlib
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src"
APP = SRC / "Cfa.ACHInterbank.Application"
API = SRC / "Cfa.ACHInterbank.Api"
PERSISTENCE = SRC / "Cfa.ACHInterbank.Persistence"
IMPL = PERSISTENCE / "ACH/Services/Implementation"
INTERFACES = APP / "ACH/Interfaces/IIncomingNachaCommandCenterService.cs"
CONTROLLER = API / "Controllers/IncomingNachaCommandCenterController.cs"
FACADE = IMPL / "IncomingNachaCommandCenterService.cs"
QUERY = IMPL / "IncomingNachaCommandCenterQueryService.cs"
OPERATIONS = IMPL / "IncomingNachaCommandCenterOperationsService.cs"
ORPHAN = IMPL / "IncomingNachaOrphanManualResolutionService.cs"

BASELINE_SERVICE_LINES = 1502
EXPECTED_API_SURFACE_SHA256 = "3241a7f16343a6b0aac2dc390fe3b80096745f735aeb78022e247ba41072a8ac"
EXPECTED_QUERY_METHODS = 12
EXPECTED_MANUAL_DISPATCH_METHODS = 4
EXPECTED_API_QUERY_ACTIONS = 12
EXPECTED_API_COMMAND_ACTIONS = 5  # four dispatch commands + pre-existing orphan resolution
EXPECTED_TOTAL_ACTIONS = 17

errors: list[str] = []

def read(path: Path) -> str:
    try:
        return path.read_text(encoding="utf-8")
    except FileNotFoundError:
        errors.append(f"missing file: {path.relative_to(ROOT)}")
        return ""

def interface_body(text: str, name: str) -> str:
    match = re.search(rf"public\s+interface\s+{re.escape(name)}[^{{]*\{{", text)
    if not match:
        errors.append(f"missing interface declaration: {name}")
        return ""
    start = match.end()
    depth = 1
    i = start
    while i < len(text) and depth:
        if text[i] == "{": depth += 1
        elif text[i] == "}": depth -= 1
        i += 1
    if depth:
        errors.append(f"unbalanced interface declaration: {name}")
        return ""
    return text[start:i-1]

interfaces = read(INTERFACES)
controller = read(CONTROLLER)
facade = read(FACADE)
query = read(QUERY)
operations = read(OPERATIONS)

query_body = interface_body(interfaces, "IIncomingNachaCommandCenterQueryService")
operations_body = interface_body(interfaces, "IIncomingNachaCommandCenterOperationsService")
compat_body = interface_body(interfaces, "IIncomingNachaCommandCenterService")

query_methods = len(re.findall(r"\bTask<", query_body))
manual_methods = len(re.findall(r"\bTask<", operations_body))
compat_direct_methods = len(re.findall(r"\bTask<", compat_body))
if query_methods != EXPECTED_QUERY_METHODS:
    errors.append(f"query capability method count changed: expected {EXPECTED_QUERY_METHODS}, got {query_methods}")
if manual_methods != EXPECTED_MANUAL_DISPATCH_METHODS:
    errors.append(f"manual operations method count changed: expected {EXPECTED_MANUAL_DISPATCH_METHODS}, got {manual_methods}")
if compat_direct_methods != 0:
    errors.append(f"compatibility aggregate declares direct methods: {compat_direct_methods}")
if "IIncomingNachaCommandCenterQueryService" not in interfaces or "IIncomingNachaCommandCenterOperationsService" not in interfaces:
    errors.append("query/operations capability contracts are not both present")

if "class IncomingNachaCommandCenterQueryService : IIncomingNachaCommandCenterQueryService" not in query:
    errors.append("query service does not own the query capability")
if "class IncomingNachaCommandCenterOperationsService : IIncomingNachaCommandCenterOperationsService" not in operations:
    errors.append("operations service does not own the manual operations capability")
if "IIncomingNachaCommandCenterOperationsService" in query:
    errors.append("query service depends on/implements the operations capability")
if "IIncomingNachaCommandCenterQueryService" in operations:
    errors.append("operations service depends on/implements the query capability")

# The compatibility facade may remain, but production code must consume the segregated capabilities.
consumer_hits: list[str] = []
for path in SRC.rglob("*.cs"):
    if path in {INTERFACES, FACADE}:
        continue
    text = path.read_text(encoding="utf-8", errors="ignore")
    if re.search(r"\bIIncomingNachaCommandCenterService\b", text):
        consumer_hits.append(str(path.relative_to(ROOT)))
if consumer_hits:
    errors.append("production compatibility-facade consumers remain: " + ", ".join(consumer_hits))

if "IIncomingNachaCommandCenterQueryService queryService" not in controller:
    errors.append("controller does not depend on query capability")
if "IIncomingNachaCommandCenterOperationsService operationsService" not in controller:
    errors.append("controller does not depend on operations capability")
if re.search(r"\bIIncomingNachaCommandCenterService\b", controller):
    errors.append("controller still depends on compatibility aggregate")

# Public route, action and authorization surface must be byte-stable modulo internal dependency names.
surface = "\n".join(
    line.strip()
    for line in controller.splitlines()
    if any(token in line for token in [
        '[Route(', '[HttpGet(', '[HttpPost(', '[Authorize', '[ProducesResponseType', 'public async Task<IActionResult>'
    ])
) + "\n"
surface_sha = hashlib.sha256(surface.encode("utf-8")).hexdigest()
if surface_sha != EXPECTED_API_SURFACE_SHA256:
    errors.append(f"public command-center API/authorization surface changed: {surface_sha}")

api_gets = controller.count("[HttpGet(")
api_posts = controller.count("[HttpPost(")
actions = len(re.findall(r"public\s+async\s+Task<IActionResult>", controller))
if api_gets != EXPECTED_API_QUERY_ACTIONS:
    errors.append(f"API query action count changed: expected {EXPECTED_API_QUERY_ACTIONS}, got {api_gets}")
if api_posts != EXPECTED_API_COMMAND_ACTIONS:
    errors.append(f"API command action count changed: expected {EXPECTED_API_COMMAND_ACTIONS}, got {api_posts}")
if actions != EXPECTED_TOTAL_ACTIONS:
    errors.append(f"controller action count changed: expected {EXPECTED_TOTAL_ACTIONS}, got {actions}")

required_policies = [
    "P1Policies.CommandCenterRead",
    "P1Policies.CommandCenterRetry",
    "P1Policies.CommandCenterUnblock",
    "P1Policies.CommandCenterRequeue",
    "P1Policies.CommandCenterMarkFailedFinal",
    'Policy = "CanManageAch"',
]
for policy in required_policies:
    if policy not in controller:
        errors.append(f"authorization policy missing from preserved API: {policy}")
if controller.count("P1Policies.CommandCenterRead") != EXPECTED_API_QUERY_ACTIONS:
    errors.append("not every command-center GET action retains CommandCenterRead")

# Query/read model is read-only: no EF write or external execution side effects.
for token in [
    "SaveChangesAsync(", "ExecuteUpdate", "ExecuteDelete", "ExecuteOutboundAsync(",
    "ExecuteInboundAsync(", "RetryAsync(", "ReprocessAsync(", "ArchiveAsync(", "RetireAsync(",
    "HandoffOutboundAsync", "PickupInboundAsync", "ArchiveInboundAsync"
]:
    if token in query:
        errors.append(f"query service contains side-effect/transport token: {token}")
for pattern in [
    r"_context\.[A-Za-z0-9_]+\.Add\(", r"_context\.[A-Za-z0-9_]+\.AddRange\(",
    r"_context\.[A-Za-z0-9_]+\.Remove\(", r"_context\.[A-Za-z0-9_]+\.Update\("
]:
    if re.search(pattern, query):
        errors.append(f"query service contains EF write pattern: {pattern}")
if query.count(".AsNoTracking()") != 38:
    errors.append(f"query AsNoTracking sites changed: expected 38 read-only sites after moving the one command replay lookup, got {query.count('.AsNoTracking()')}")

# Manual operations keep the existing single persistence/audit site and state-machine owner.
for method in ["RetryManualAsync", "UnblockManualAsync", "RequeueManualAsync", "MarkFailedFinalManualAsync"]:
    if not re.search(rf"public\s+Task<IncomingNachaManualActionResultDto>\s+{method}\s*\(", operations):
        errors.append(f"manual operation owner missing: {method}")
if operations.count("SaveChangesAsync(") != 1:
    errors.append(f"manual operations SaveChangesAsync sites changed: expected 1, got {operations.count('SaveChangesAsync(')}")
if operations.count('EventType = eventType') != 1 or operations.count('EvidenceJson = JsonSerializer.Serialize') != 1:
    errors.append("manual transition audit/event write was duplicated or removed")
if "EvaluateDispatchTransition" not in operations:
    errors.append("manual operations bypass the existing state machine")
for token in ["Polly", "WaitAndRetry", "Task.Delay(", "Thread.Sleep(", "Proc_Transacciones", "INachaParserService", "IIncomingNachaIngestionAppService"]:
    if token in operations:
        errors.append(f"manual operations introduced retry/execution ownership: {token}")

# R6C does not add managed-MFT, SFTP/session, service-locator, schema or CQRS-framework coupling.
r6c_files = [INTERFACES, CONTROLLER, FACADE, QUERY, OPERATIONS]
r6c_text = "\n".join(read(path) for path in r6c_files)
for token in [
    "AchColombiaManagedFileExchangeService", "AchColombiaManagedMftOutboundWorkflow", "AchColombiaManagedMftInboundWorkflow",
    "AchColombiaManagedMftRetryReprocessComponent", "AchColombiaManagedMftLifecycleComponent", "SftpClient",
    "SshNetAchColombiaSftpSessionFactory", "IServiceProvider", "GetRequiredService<", "PaymentRailOperationalStrategy",
    "MediatR", "IRequestHandler<", "IRequest<", "EntityTypeBuilder", "CreateTable(", "MigrationBuilder"
]:
    if token in r6c_text:
        errors.append(f"out-of-scope R6C coupling introduced: {token}")

# Existing orphan resolution remains an independent command use case; R6C does not absorb it.
if "IIncomingNachaOrphanManualResolutionService orphanResolutionService" not in controller:
    errors.append("pre-existing orphan-resolution command boundary changed")
if ORPHAN.exists() and re.search(r"IIncomingNachaCommandCenter(?:Query|Operations)?Service", ORPHAN.read_text(encoding="utf-8", errors="ignore")):
    errors.append("orphan-resolution service was coupled into the new command-center capabilities")

# Out-of-scope CENIT debt remains exactly as inherited.
contains_cenit = 0
for rel in [
    "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaInboundSimulationService.cs",
    "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaIngestionAppService.cs",
]:
    path = ROOT / rel
    contains_cenit += read(path).count('Contains("CENIT"')
if contains_cenit != 8:
    errors.append(f'out-of-scope Contains("CENIT") debt changed: expected 8, got {contains_cenit}')

facade_lines = len(facade.splitlines())
query_lines = len(query.splitlines())
operations_lines = len(operations.splitlines())
print("R6C INCOMING COMMAND CENTER STATIC RATCHET")
print("  exact R6C scope: split existing inbound command center into query/read-model and manual operations/commands; API and permissions preserved")
print(f"  API command actions: {EXPECTED_API_COMMAND_ACTIONS} -> {api_posts} (4 dispatch operations + 1 pre-existing orphan resolution)")
print(f"  API query actions: {EXPECTED_API_QUERY_ACTIONS} -> {api_gets}")
print(f"  controller actions: {EXPECTED_TOTAL_ACTIONS} -> {actions}; public API/authorization surface hash unchanged")
print(f"  capability contracts: wide 16-method service -> query={query_methods}, operations={manual_methods}, compatibility direct methods={compat_direct_methods}")
print(f"  production compatibility-facade consumers: 1 -> {len(consumer_hits)} (controller now depends on segregated capabilities)")
print(f"  implementation owner: {BASELINE_SERVICE_LINES} lines -> query={query_lines}, operations={operations_lines}, compatibility facade={facade_lines}")
print(f"  query side effects: 0; query-owned AsNoTracking sites: 38 -> {query.count('.AsNoTracking()')} (1 command replay lookup moved with operations)")
print(f"  manual transition persistence/audit SaveChangesAsync sites: 1 -> {operations.count('SaveChangesAsync(')}")
print("  timeline/read model: existing persisted ingestions/queue/executions/processing events projected read-only; no new persistent owner")
print("  direct concrete managed-MFT dependencies from R6C API/Application: 0")
print("  direct SFTP/session construction: 0; service locator: 0; new blind retry sites: 0")
print("  duplicated dispatch retry/reprocess implementation: 0; existing state-machine/audit owner preserved")
print("  DB schema dependencies/migrations introduced by R6C: 0")
print("  authorization gaps: 0; route/action/policy surface preserved")
print(f'  Contains("CENIT") out-of-scope debt: 8 -> {contains_cenit}')
print("  PaymentRailOperationalStrategy references in R6C scope: 0")
print("  new R6C architecture debt: 0")

if errors:
    for error in errors:
        print(f"ERROR: {error}")
    raise SystemExit(1)

print("R6C INCOMING COMMAND CENTER STATIC RATCHET: OK")
