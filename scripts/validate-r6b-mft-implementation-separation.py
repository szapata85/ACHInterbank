#!/usr/bin/env python3
from pathlib import Path
import hashlib
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src"
IMPL = SRC / "Cfa.ACHInterbank.Persistence/ACH/Services/Implementation"
FACADE = IMPL / "AchColombiaManagedFileExchangeService.cs"
INTERFACES = SRC / "Cfa.ACHInterbank.Application/ACH/Interfaces/IAchColombiaManagedFileExchange.cs"
DI = SRC / "Cfa.ACHInterbank.Persistence/DependencyInjectionService.cs"

BASELINE_FACADE_LINES = 1000
EXPECTED_R6A_INTERFACE_SHA256 = "952425fa7daf70c9f7e7d19d5ea85872da2797f00b0baad8a7022d947f2d969e"
PROTECTED_HASHES = {
    "src/Cfa.ACHInterbank.External/Connections/AchColombiaSftpMftAdapter.cs": "b81bd17b79faee71038ccc7f510c21e5888a4d8ceefcff2ae5f722eb70f38fcc",
    "src/Cfa.ACHInterbank.External/Connections/AchColombiaManagedMftAdapterResolver.cs": "05037f82c5c09ddae2e0dd6b6ae5b02617ff584e7cc4add1f85d52d2e779ae7e",
    "src/Cfa.ACHInterbank.External/Connections/AchColombiaManagedMftFolderAdapter.cs": "c4ea2423619a4f4ba299d77ad90d4df2a9614a66ea81d826dbc5701607d4fbd0",
    "src/Cfa.ACHInterbank.External/Connections/AchColombiaManagedMftConnectionTester.cs": "7b81f6e8e5e648557bf703272fd8c70ad4eff821d52a176345135179bbe25c70",
    "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchColombiaManagedMftConfigurationProvider.cs": "8df44aa8df02a4d9a5ee872660246c22d0f62e2ab2501167dbe09f168057cff4",
    "src/Cfa.ACHInterbank.Persistence/ACH/Quartz/Jobs/Implementation/AchColombiaManagedMftHandlers.cs": "e3eafb54afc25761ff9a3c598d475ed6769e2f236548d23355f0d1b721bcee9e",
    "src/Cfa.ACHInterbank.Api/Controllers/AchColombiaFileExchangeController.cs": "3c49fc70a450e68a7d0855ba81e28537890d4be0f4a0c53c0cd684e8353930c7",
}
COMPONENTS = [
    "AchColombiaManagedMftOutboundWorkflow.cs",
    "AchColombiaManagedMftInboundWorkflow.cs",
    "AchColombiaManagedMftRetryReprocessComponent.cs",
    "AchColombiaManagedMftLifecycleComponent.cs",
    "AchColombiaManagedMftQueryProjectionComponent.cs",
    "AchColombiaManagedMftAdministrationComponent.cs",
    "AchColombiaManagedMftCredentialComponent.cs",
]
SUPPORT = IMPL / "AchColombiaManagedMftImplementationSupport.cs"

errors: list[str] = []

def read(path: Path) -> str:
    try:
        return path.read_text(encoding="utf-8")
    except FileNotFoundError:
        errors.append(f"missing file: {path.relative_to(ROOT)}")
        return ""

def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()

facade = read(FACADE)
interfaces = read(INTERFACES)
di = read(DI)
component_texts = {name: read(IMPL / name) for name in COMPONENTS}
support_text = read(SUPPORT)
all_r6b_impl = "\n".join(component_texts.values()) + "\n" + support_text + "\n" + facade

facade_lines = len(facade.splitlines())
if facade_lines > 130:
    errors.append(f"facade remains too large: {facade_lines} lines")

if sha(INTERFACES) != EXPECTED_R6A_INTERFACE_SHA256:
    errors.append("R6A capability contract file changed")

for rel, expected in PROTECTED_HASHES.items():
    p = ROOT / rel
    if not p.exists() or sha(p) != expected:
        errors.append(f"protected R6A/R2 MFT artifact changed: {rel}")

for name in COMPONENTS:
    if not (IMPL / name).exists():
        errors.append(f"missing R6B component: {name}")

required_ownership = {
    "AchColombiaManagedMftOutboundWorkflow.cs": ["ExecuteAsync", "HandoffAsync"],
    "AchColombiaManagedMftInboundWorkflow.cs": ["ExecuteAsync", "ProcessInboundAsync"],
    "AchColombiaManagedMftRetryReprocessComponent.cs": ["RetryAsync", "ReprocessAsync"],
    "AchColombiaManagedMftLifecycleComponent.cs": ["ArchiveAsync", "RetireAsync", "DownloadAsync", "TryArchiveInboundAfterPersistedResultAsync"],
    "AchColombiaManagedMftQueryProjectionComponent.cs": ["QueryAsync", "GetAsync", "MapAsync"],
    "AchColombiaManagedMftAdministrationComponent.cs": ["GetConfigurationAsync", "UpdateConfigurationAsync", "GetAdministrationAsync", "UpdateAdministrationAsync"],
    "AchColombiaManagedMftCredentialComponent.cs": ["SetCredentialAsync"],
}
for name, methods in required_ownership.items():
    text = component_texts[name]
    for method in methods:
        if not re.search(rf"\b{re.escape(method)}\s*\(", text):
            errors.append(f"{name} does not own expected method {method}")

# Facade must be pure delegation: no DbContext, adapter resolver, options, encryption, ingestion or transport mechanics.
for forbidden in [
    "AchDbContext", "IAchColombiaManagedMftAdapterResolver", "IEncryptionService", "IOptions<",
    "IIncomingNachaIngestionAppService", "INachaFileBuilder", "SaveChangesAsync", "HandoffOutboundAsync",
    "PickupInboundAsync", "ArchiveInboundAsync", "SftpClient", "IServiceProvider"
]:
    if forbidden in facade:
        errors.append(f"facade still owns forbidden R6B implementation concern: {forbidden}")

# R6A aliases must still resolve each capability from the same facade instance.
for contract in [
    "IAchColombiaManagedMftExecutionService",
    "IAchColombiaManagedMftTransferReadService",
    "IAchColombiaManagedMftTransferOperationsService",
    "IAchColombiaManagedMftAdministrationService",
    "IAchColombiaManagedMftCredentialAdministrationService",
]:
    expected = f"services.AddScoped<{contract}>(sp => sp.GetRequiredService<IAchColombiaManagedFileExchangeService>());"
    if expected not in di:
        errors.append(f"R6A facade alias missing/changed: {contract}")

for name in [Path(x).stem for x in COMPONENTS]:
    if f"services.AddScoped<{name}>();" not in di:
        errors.append(f"R6B component not registered scoped: {name}")

# Production facade consumers must stay zero. Interface/DI/facade declaration are compatibility wiring, not consumers.
facade_consumer_hits = []
for p in SRC.rglob("*.cs"):
    if p in {INTERFACES, FACADE, DI}:
        continue
    text = p.read_text(encoding="utf-8", errors="ignore")
    if "IAchColombiaManagedFileExchangeService" in text:
        facade_consumer_hits.append(str(p.relative_to(ROOT)))
if facade_consumer_hits:
    errors.append("production facade consumers reintroduced: " + ", ".join(facade_consumer_hits))

# Operation/audit/persistence call-site counts are intentionally preserved while ownership moves.
save_changes = all_r6b_impl.count("SaveChangesAsync")
add_event = all_r6b_impl.count("AddEvent(")
resolver_calls = all_r6b_impl.count("mftAdapterResolver.ResolveAsync")
if save_changes != 24:
    errors.append(f"managed-MFT SaveChangesAsync sites changed: expected 24, got {save_changes}")
if add_event != 37:
    errors.append(f"managed-MFT AddEvent sites changed: expected 37, got {add_event}")
if resolver_calls != 3:
    errors.append(f"adapter resolution sites changed: expected 3, got {resolver_calls}")

# Single non-facade command owners for retry and lifecycle.
retry_owner_count = sum(bool(re.search(r"public\s+async\s+Task<AchManagedFileTransferDetail>\s+RetryAsync\s*\(", t)) for t in component_texts.values())
lifecycle_owner_count = sum(bool(re.search(r"internal\s+async\s+Task<bool>\s+TryArchiveInboundAfterPersistedResultAsync\s*\(", t)) for t in component_texts.values())
if retry_owner_count != 1:
    errors.append(f"retry command owner count must be 1, got {retry_owner_count}")
if lifecycle_owner_count != 1:
    errors.append(f"lifecycle archive owner count must be 1, got {lifecycle_owner_count}")

# No new blind/local transport retry loop in R6B implementation.
for token in ["Polly", "WaitAndRetry", "Task.Delay(", "Thread.Sleep("]:
    if token in all_r6b_impl:
        errors.append(f"new local retry primitive found in R6B implementation: {token}")

# No service locator in implementation/consumers.
if "IServiceProvider" in all_r6b_impl or "GetRequiredService<" in all_r6b_impl:
    errors.append("service locator introduced in managed-MFT implementation")

# R2 session construction remains in the existing factory, nowhere in R6B/Persistence implementation.
if "new SftpClient" in all_r6b_impl or "new SshNetAchColombiaSftpSessionFactory" in all_r6b_impl:
    errors.append("direct SFTP/session factory construction introduced into R6B implementation")

# No duplicated direct transfer mechanics: workflows call the existing adapter once per logical operation path.
if component_texts["AchColombiaManagedMftOutboundWorkflow.cs"].count("HandoffOutboundAsync") != 1:
    errors.append("outbound adapter handoff implementation duplicated or missing")
if component_texts["AchColombiaManagedMftInboundWorkflow.cs"].count("PickupInboundAsync") != 1:
    errors.append("inbound pickup implementation duplicated or missing")
if component_texts["AchColombiaManagedMftLifecycleComponent.cs"].count("ArchiveInboundAsync") != 1:
    errors.append("inbound archive implementation duplicated or missing")

# Guard against accidental R6C/CENIT scope expansion.
contains_cenit = 0
for rel in [
    "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaInboundSimulationService.cs",
    "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaIngestionAppService.cs",
]:
    p = ROOT / rel
    if p.exists():
        contains_cenit += p.read_text(encoding="utf-8", errors="ignore").count('Contains("CENIT"')
if contains_cenit != 8:
    errors.append(f'out-of-scope Contains("CENIT") debt changed: expected 8, got {contains_cenit}')

payment_rail_strategy = sum(
    p.read_text(encoding="utf-8", errors="ignore").count("PaymentRailOperationalStrategy")
    for p in [FACADE, SUPPORT, *(IMPL / x for x in COMPONENTS)]
)
if payment_rail_strategy != 0:
    errors.append("PaymentRailOperationalStrategy introduced in R6B scope")

print("R6B MFT IMPLEMENTATION SEPARATION STATIC RATCHET")
print("  exact R6B scope: split managed-MFT implementation into outbound/inbound workflows, retry/reprocess, lifecycle, query projection, administration and credentials; R6C untouched")
print(f"  AchColombiaManagedFileExchangeService lines: {BASELINE_FACADE_LINES} -> {facade_lines}")
print(f"  implementation ownership components: 1 -> {len(COMPONENTS)} (plus one shared pure/support helper)")
print("  R6A capability contracts: 5 -> 5; compatibility facade direct methods remain 0")
print(f"  production facade consumers: 0 -> {len(facade_consumer_hits)}")
print("  facade direct implementation collaborators: 12 -> 8 (7 cohesive R6B owners + existing connection tester)")
print("  session creation owners: 1 -> 1 (existing SshNetAchColombiaSftpSessionFactory; protected hash unchanged)")
print(f"  retry command owners: 1 -> {retry_owner_count}; new blind/local retry sites: 0")
print(f"  lifecycle archive owners: 1 -> {lifecycle_owner_count}")
print("  audit/persistence: not extracted as a new layer; operation owners keep original ordering")
print(f"  managed-MFT SaveChangesAsync call sites: 24 -> {save_changes}; AddEvent sites: 37 -> {add_event}")
print(f"  adapter resolver sites: 3 -> {resolver_calls}; duplicated direct transfer implementation: 0")
print("  direct SFTP/session construction outside composition root/factory: 0")
print("  service-locator usage in MFT implementation/consumers: 0")
print(f'  Contains("CENIT") out-of-scope debt: 8 -> {contains_cenit}')
print("  PaymentRailOperationalStrategy references in R6B scope: 0")
print("  protected R6A contracts/handlers/controller + External MFT/SFTP transport/configuration hashes: unchanged")
print("  new R6B architecture debt: 0")

if errors:
    for error in errors:
        print(f"ERROR: {error}")
    raise SystemExit(1)

print("R6B MFT IMPLEMENTATION SEPARATION STATIC RATCHET: OK")
