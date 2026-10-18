#!/usr/bin/env python3
"""Static R9 ratchet for deterministic operational time and async consistency."""
from __future__ import annotations

import hashlib
import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src"

R9_PRODUCTION = [
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

R9_FAKE_TIME_TEST_SOURCES = [
    "tests/Cfa.ACHInterbank.Tests/CycleOperationalWindowTests.cs",
    "tests/Cfa.ACHInterbank.Tests/CenitOperationalGovernanceTests.cs",
    "tests/Cfa.ACHInterbank.Tests/ClearingHouseCycleConfigServiceTests.cs",
    "tests/Cfa.ACHInterbank.Tests/AchColombiaManagedFileExchangeTests.cs",
    "tests/Cfa.ACHInterbank.Tests/DynamicJobRetryTests.cs",
]

BASELINE_REAL_CLOCK = 38
BASELINE_TIME_PROVIDER_CONSUMERS = 1
BASELINE_OPERATIONAL_CLOCK_PORTS = 1
BASELINE_HOST_LOCAL_TIME = 0
BASELINE_FAKE_TIME_TEST_SOURCES = 2
BASELINE_SYNC_RESULT = 0
BASELINE_SYNC_WAIT = 0
BASELINE_SYNC_GET_RESULT = 0
BASELINE_CANCELLATION_GAPS = 0
BASELINE_OPERATIONAL_TIMEZONE_OWNERS = 3
BASELINE_BUSINESS_DATE_OWNERS = 2
BASELINE_RETRY_RECOVERY_DELAY_OWNERS = 3
BASELINE_TRANSACTION_OWNERS = 1
BASELINE_CONTAINS_CENIT = 8
BASELINE_PERSISTENCE_FALLBACKS = 57
BASELINE_PAYMENT_RAIL_STRATEGY_REFS = 0

SEMANTIC_OWNER_HASHES = {
    "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/OperationalTimeSnapshotProvider.cs": "a2e0d3a47b0e679352047bc77421c2ad3cb57a63e52b9fc519a0258f503030a3",
    "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ClearingHouseOperationalTimeZone.cs": "c7609c09848c55d065311b5260c27e965f11ee8326b3be445a2ca44742ce4468",
    "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ClearingHouseCyclePolicyResolver.cs": "b9ec107de1dca9feb8b13c26841e9befc607fe253cb5db8bd2891df51f6dc99d",
    "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/OperationalCalendarService.cs": "5228a2397830717c9f7a327548ca48287875cc3b3174a9f0ab474c55ae499e76",
}

errors: list[str] = []


def read(rel: str) -> str:
    path = ROOT / rel
    if not path.exists():
        errors.append(f"missing file: {rel}")
        return ""
    return path.read_text(encoding="utf-8-sig", errors="ignore")


def sha256(rel: str) -> str:
    return hashlib.sha256((ROOT / rel).read_bytes()).hexdigest()


# Prior R1-R8B ratchets remain authoritative. R7 was made dual-baseline aware for the
# exact authorized R9 Return-of-Return time seam, so this chain still detects unrelated drift.
prior = subprocess.run(
    [sys.executable, str(ROOT / "scripts/validate-r8b-certificate-management-service-cohesion.py")],
    cwd=ROOT,
    capture_output=True,
    text=True,
    check=False,
    timeout=420,
)
if prior.returncode != 0:
    detail = (prior.stdout + prior.stderr).strip().replace("\n", " | ")
    errors.append(f"prior R1-R8B ratchet chain failed: {detail}")

selected_texts = {rel: read(rel) for rel in R9_PRODUCTION}
selected = "\n".join(selected_texts.values())

# R9 scope is deliberately selected operational behavior, not a repository-wide wall-clock ban.
real_clock_patterns = [
    "DateTime.Now",
    "DateTime.UtcNow",
    "DateTime.Today",
    "DateTimeOffset.Now",
    "DateTimeOffset.UtcNow",
]
real_clock_after = sum(selected.count(token) for token in real_clock_patterns)
host_local_after = selected.count("DateTime.Now") + selected.count("DateTimeOffset.Now")
if real_clock_after != 0:
    errors.append(f"behavior-determining real clock remains in selected R9 routes: {real_clock_after}")
if host_local_after != 0:
    errors.append(f"host-local time dependency remains in selected R9 routes: {host_local_after}")

consumer_count = sum(1 for text in selected_texts.values() if "TimeProvider" in text)
if consumer_count != len(R9_PRODUCTION):
    errors.append(f"selected R9 TimeProvider consumers incomplete: expected {len(R9_PRODUCTION)}, got {consumer_count}")

# One existing operational semantic port remains the owner; no generic clock/service wrapper was introduced.
operational_port = read("src/Cfa.ACHInterbank.Application/ACH/Interfaces/IOperationalTimeSnapshotProvider.cs")
operational_clock_ports = 1 if "interface IOperationalTimeSnapshotProvider" in operational_port else 0
if operational_clock_ports != BASELINE_OPERATIONAL_CLOCK_PORTS:
    errors.append(f"operational time semantic port count changed: {BASELINE_OPERATIONAL_CLOCK_PORTS} -> {operational_clock_ports}")
for forbidden in ["interface IClock", "interface IOperationalClock", "interface IBusinessClock", "interface ITimeService"]:
    matches = []
    for path in SRC.rglob("*.cs"):
        if forbidden in path.read_text(encoding="utf-8-sig", errors="ignore"):
            matches.append(path.relative_to(ROOT).as_posix())
    if matches:
        errors.append(f"generic/competing clock abstraction introduced ({forbidden}): {', '.join(matches)}")

app_di = read("src/Cfa.ACHInterbank.Application/DependencyInjectionService.cs")
if app_di.count("services.AddSingleton(TimeProvider.System);") != 1:
    errors.append("TimeProvider.System must be registered exactly once in the Application composition root")

# All selected operational delays retain duration/cancellation and use the TimeProvider overload.
delay_sites = sum(text.count("Task.Delay(") for text in selected_texts.values())
if delay_sites != 5:
    errors.append(f"selected operational Task.Delay site count changed: expected 5, got {delay_sites}")
expected_delay_fragments = {
    R9_PRODUCTION[2]: ["Task.Delay(interval, _timeProvider, ct)"],
    R9_PRODUCTION[5]: ["Task.Delay(TimeSpan.FromMinutes(1), _timeProvider, stoppingToken)"],
    R9_PRODUCTION[6]: ["_timeProvider,", "stoppingToken"],
    R9_PRODUCTION[7]: ["Task.Delay(backoff, _timeProvider, cancellationToken)"],
    R9_PRODUCTION[8]: ["_timeProvider,", "cancellationToken"],
}
for rel, fragments in expected_delay_fragments.items():
    text = selected_texts[rel]
    if not all(fragment in text for fragment in fragments):
        errors.append(f"TimeProvider/cancellation propagation missing from operational delay owner: {rel}")

# Sync-over-async metric excludes the existing domain Result/ResultCode properties in the dispatcher.
sync_wait = len(re.findall(r"\.Wait\s*\(", selected))
sync_get_result = selected.count(".GetAwaiter().GetResult()")
sync_result = 0
for rel, text in selected_texts.items():
    for line in text.splitlines():
        if ".Result" not in line:
            continue
        if rel.endswith("AchResponseReprocessDispatcher.cs") and "SetProperty(x => x.Result, result.Result)" in line:
            continue
        # ResultCode is a domain property, not Task.Result.
        scrubbed = line.replace(".ResultCode", "")
        sync_result += len(re.findall(r"\.Result\b", scrubbed))
if sync_result or sync_wait or sync_get_result:
    errors.append(
        f"sync-over-async remains in selected R9 routes: .Result={sync_result}, .Wait()={sync_wait}, .GetAwaiter().GetResult()={sync_get_result}"
    )

cancellation_gaps = 0
# Every selected delay has an explicit token by the exact checks above; keep this metric independent.
for rel in expected_delay_fragments:
    if "CancellationToken.None" in "\n".join(
        line for line in selected_texts[rel].splitlines() if "Task.Delay" in line
    ):
        cancellation_gaps += 1
if cancellation_gaps != 0:
    errors.append(f"CancellationToken propagation gaps in selected delays: {cancellation_gaps}")

# Test sources: two existed at R8B; three R9 routes gain controlled-time boundary coverage.
fake_time_sources = 0
for rel in R9_FAKE_TIME_TEST_SOURCES:
    text = read(rel)
    if "FixedTimeProvider" in text or "TestClock" in text:
        fake_time_sources += 1
if fake_time_sources != len(R9_FAKE_TIME_TEST_SOURCES):
    errors.append(f"fake-time boundary test source coverage incomplete: expected {len(R9_FAKE_TIME_TEST_SOURCES)}, got {fake_time_sources}")
for token in ["Thread.Sleep", "Task.Delay("]:
    # Dedicated R9 boundary tests must not prove boundaries with wall-clock waits.
    for rel in R9_FAKE_TIME_TEST_SOURCES[2:]:
        if token in read(rel):
            errors.append(f"real wait introduced in R9 fake-time boundary source {rel}: {token}")

# Exact boundary characterizations required by R9.
if "AddSeconds(-60).AddTicks(1)" not in read(R9_FAKE_TIME_TEST_SOURCES[3]):
    errors.append("MFT before-recovery-boundary characterization missing")
if "AddSeconds(-1)" not in read(R9_FAKE_TIME_TEST_SOURCES[3]) or "AddSeconds(-60).AddTicks(-1)" not in read(R9_FAKE_TIME_TEST_SOURCES[3]):
    errors.append("MFT exact/after recovery-boundary characterization missing")
if "deadlineOffsetTicks" not in read(R9_FAKE_TIME_TEST_SOURCES[1]) or "[InlineData(0, true)]" not in read(R9_FAKE_TIME_TEST_SOURCES[1]):
    errors.append("Return-of-Return SLA exact-boundary fake-time characterization missing")
if "599999999" not in read(R9_FAKE_TIME_TEST_SOURCES[4]) or "600000001" not in read(R9_FAKE_TIME_TEST_SOURCES[4]):
    errors.append("scheduler custom misfire before/exact/after threshold characterization missing")
if "2026-02-09T23:59:59.9999999Z" not in read(R9_FAKE_TIME_TEST_SOURCES[2]) or "2026-02-10T00:00:00Z" not in read(R9_FAKE_TIME_TEST_SOURCES[2]):
    errors.append("cycle-config effective-date UTC boundary characterization missing")

# Existing timezone/business-date semantic owners remain byte-identical.
for rel, expected in SEMANTIC_OWNER_HASHES.items():
    if not (ROOT / rel).exists():
        errors.append(f"semantic owner missing: {rel}")
    elif sha256(rel) != expected:
        errors.append(f"operational timezone/business-date semantic owner changed in R9: {rel}")

# Preserve explicit ownership. R9 only changes the clock source/delay scheduler.
retry_recovery_owner_tokens = {
    R9_PRODUCTION[1]: "RetryDelaySeconds",
    R9_PRODUCTION[2]: "Task.Delay(interval, _timeProvider, ct)",
    R9_PRODUCTION[7]: "RetryBackoffSeconds",
}
retry_recovery_owners = sum(1 for rel, token in retry_recovery_owner_tokens.items() if token in selected_texts[rel])
if retry_recovery_owners != BASELINE_RETRY_RECOVERY_DELAY_OWNERS:
    errors.append(f"retry/recovery delay ownership changed: {BASELINE_RETRY_RECOVERY_DELAY_OWNERS} -> {retry_recovery_owners}")
transaction_owners = sum(1 for text in selected_texts.values() if "BeginTransactionAsync(" in text)
if transaction_owners != BASELINE_TRANSACTION_OWNERS:
    errors.append(f"transaction ownership changed in selected R9 routes: {BASELINE_TRANSACTION_OWNERS} -> {transaction_owners}")

# Explicitly preserve out-of-scope debts and global strategy ratchets promised by the phase.
contains_cenit = 0
for rel in [
    "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaInboundSimulationService.cs",
    "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaIngestionAppService.cs",
]:
    contains_cenit += read(rel).count('Contains("CENIT"')
if contains_cenit != BASELINE_CONTAINS_CENIT:
    errors.append(f'out-of-scope Contains("CENIT") debt changed: {BASELINE_CONTAINS_CENIT} -> {contains_cenit}')

m_payment_rail = re.search(r"PaymentRailOperationalStrategy references:\s*0\s*->\s*(\d+)", prior.stdout)
payment_rail_refs = int(m_payment_rail.group(1)) if m_payment_rail else -1
if payment_rail_refs != BASELINE_PAYMENT_RAIL_STRATEGY_REFS:
    errors.append(f"PaymentRailOperationalStrategy references changed or could not be read from prior ratchets: {BASELINE_PAYMENT_RAIL_STRATEGY_REFS} -> {payment_rail_refs}")

m = re.search(r"Persistence fallback allowlist:\s*57\s*->\s*(\d+)", prior.stdout)
persistence_fallbacks = int(m.group(1)) if m else -1
if persistence_fallbacks != BASELINE_PERSISTENCE_FALLBACKS:
    errors.append(f"Persistence fallback allowlist changed or could not be read from prior ratchets: {BASELINE_PERSISTENCE_FALLBACKS} -> {persistence_fallbacks}")

print("R9 DETERMINISTIC TIME / ASYNC CONSISTENCY STATIC RATCHET")
print("  scope: selected behavior-determining operational routes only; audit-only/framework clocks remain out of automatic scope")
print("TIME")
print(f"  behavior-determining real-clock sites (selected R9): {BASELINE_REAL_CLOCK} -> {real_clock_after}")
print(f"  TimeProvider consumers (selected R9): {BASELINE_TIME_PROVIDER_CONSUMERS} -> {consumer_count}")
print(f"  operational time semantic ports: {BASELINE_OPERATIONAL_CLOCK_PORTS} -> {operational_clock_ports}")
print(f"  host-local-time dependencies (selected R9): {BASELINE_HOST_LOCAL_TIME} -> {host_local_after}")
print(f"  fake-time boundary test sources (selected R9): {BASELINE_FAKE_TIME_TEST_SOURCES} -> {fake_time_sources}")
print("ASYNC")
print(f"  Task.Result sync-over-async (selected R9): {BASELINE_SYNC_RESULT} -> {sync_result}")
print(f"  .Wait() (selected R9): {BASELINE_SYNC_WAIT} -> {sync_wait}")
print(f"  .GetAwaiter().GetResult() (selected R9): {BASELINE_SYNC_GET_RESULT} -> {sync_get_result}")
print(f"  CancellationToken propagation gaps (selected R9 delays): {BASELINE_CANCELLATION_GAPS} -> {cancellation_gaps}")
print("SEMANTICS")
print(f"  operational timezone owners: {BASELINE_OPERATIONAL_TIMEZONE_OWNERS} -> {BASELINE_OPERATIONAL_TIMEZONE_OWNERS} (byte-identical)")
print(f"  business-date owners: {BASELINE_BUSINESS_DATE_OWNERS} -> {BASELINE_BUSINESS_DATE_OWNERS} (byte-identical)")
print(f"  retry/recovery delay owners: {BASELINE_RETRY_RECOVERY_DELAY_OWNERS} -> {retry_recovery_owners}")
print(f"  transaction owners (selected R9): {BASELINE_TRANSACTION_OWNERS} -> {transaction_owners}")
print(f"  operational Task.Delay sites: 5 -> {delay_sites}; durations/retry counts unchanged, TimeProvider + cancellation retained")
print(f"  Persistence fallback allowlist: {BASELINE_PERSISTENCE_FALLBACKS} -> {persistence_fallbacks}")
print(f'  Contains("CENIT") out-of-scope debt: {BASELINE_CONTAINS_CENIT} -> {contains_cenit}')
print(f"  PaymentRailOperationalStrategy references: {BASELINE_PAYMENT_RAIL_STRATEGY_REFS} -> {payment_rail_refs}")
print("  prior R1-R8B architecture ratchet chain: invoked and required")
print("  new R9 architecture debt: 0")

if errors:
    for error in errors:
        print(f"ERROR: {error}", file=sys.stderr)
    sys.exit(1)

print("R9 DETERMINISTIC TIME / ASYNC CONSISTENCY STATIC RATCHET: OK")
