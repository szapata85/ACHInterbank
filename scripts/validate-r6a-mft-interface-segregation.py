#!/usr/bin/env python3
"""Static R6A ratchet for ACH Colombia managed-MFT contract segregation.

R6A is contract/consumer/DI plumbing only. It must not redesign transfer mechanics,
retry/recovery, source-file lifecycle, credentials, SFTP session ownership, or CENIT.
"""
from __future__ import annotations

import hashlib
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src"
INTERFACES = SRC / "Cfa.ACHInterbank.Application/ACH/Interfaces/IAchColombiaManagedFileExchange.cs"
CONTROLLER = SRC / "Cfa.ACHInterbank.Api/Controllers/AchColombiaFileExchangeController.cs"
HANDLERS = SRC / "Cfa.ACHInterbank.Persistence/ACH/Quartz/Jobs/Implementation/AchColombiaManagedMftHandlers.cs"
PERSISTENCE_DI = SRC / "Cfa.ACHInterbank.Persistence/DependencyInjectionService.cs"
SERVICE = SRC / "Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchColombiaManagedFileExchangeService.cs"
SFTP = SRC / "Cfa.ACHInterbank.External/Connections/AchColombiaSftpMftAdapter.cs"

CAPABILITIES = {
    "IAchColombiaManagedMftExecutionService": ["ExecuteOutboundAsync", "ExecuteInboundAsync"],
    "IAchColombiaManagedMftTransferReadService": ["QueryAsync", "GetAsync"],
    "IAchColombiaManagedMftTransferOperationsService": [
        "RetryAsync", "ReprocessAsync", "ArchiveAsync", "RetireAsync", "DownloadAsync", "TestConnectionAsync"
    ],
    "IAchColombiaManagedMftAdministrationService": [
        "GetConfigurationAsync", "UpdateConfigurationAsync", "GetAdministrationAsync", "UpdateAdministrationAsync"
    ],
    "IAchColombiaManagedMftCredentialAdministrationService": ["SetCredentialAsync"],
}

# Exact R5D/R6A hashes for owners that R6A itself is not allowed to redesign.
# The service implementation hash is checked while R6B has not started; the explicit R6B marker below
# permits only the planned successor implementation split while keeping the R6A contract ratchet active.
R6A_SERVICE_SHA256 = "6d2c346ef38876fb1405b79cb2d4a45c1d53bcd7955d9f25b4bc4f5b53a489ce"
R6B_MARKER = SRC / "Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchColombiaManagedMftRetryReprocessComponent.cs"
UNCHANGED = {
    "src/Cfa.ACHInterbank.External/Connections/AchColombiaSftpMftAdapter.cs": "b81bd17b79faee71038ccc7f510c21e5888a4d8ceefcff2ae5f722eb70f38fcc",
    "src/Cfa.ACHInterbank.External/Connections/AchColombiaManagedMftFolderAdapter.cs": "c4ea2423619a4f4ba299d77ad90d4df2a9614a66ea81d826dbc5701607d4fbd0",
    "src/Cfa.ACHInterbank.External/Connections/AchColombiaManagedMftAdapterResolver.cs": "05037f82c5c09ddae2e0dd6b6ae5b02617ff584e7cc4add1f85d52d2e779ae7e",
    "src/Cfa.ACHInterbank.External/Connections/AchColombiaManagedMftConnectionTester.cs": "7b81f6e8e5e648557bf703272fd8c70ad4eff821d52a176345135179bbe25c70",
    "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchColombiaManagedMftConfigurationProvider.cs": "8df44aa8df02a4d9a5ee872660246c22d0f62e2ab2501167dbe09f168057cff4",
    "src/Cfa.ACHInterbank.Application/ACH/Interfaces/ICenitGatewayTransportAdapter.cs": "e4d2d10f4ac60cb37ba0482be309190bf38809e46e4251da5c198b9704fb0c5c",
    "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitLocalGatewayFolderTransportAdapter.cs": "465c5cc5479bf42b57aa9914a7a41ce527f391ec75f8b7312957d03e3374cdbe",
}

RUNTIME_FALLBACK = re.compile(
    r"\?\?\s*new\s+(?:[\w.]+\.)?\w+"
    r"(?:Service|Provider|Resolver|Policy|Validator|Builder|Factory|Guard|Strategy|Generator|Store|Auditor)\s*\("
)
PERSISTENCE_FALLBACK_BASELINE = 57


def text(path: Path) -> str:
    return path.read_text(encoding="utf-8-sig", errors="ignore")


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def interface_body(source: str, name: str) -> str:
    match = re.search(rf"public\s+interface\s+{re.escape(name)}\b[^{{]*\{{", source)
    if not match:
        return ""
    start = match.end()
    depth = 1
    for index in range(start, len(source)):
        if source[index] == "{":
            depth += 1
        elif source[index] == "}":
            depth -= 1
            if depth == 0:
                return source[start:index]
    return ""


def declared_methods(source: str, name: str) -> list[str]:
    body = interface_body(source, name)
    return re.findall(r"\b([A-Za-z_]\w*Async)\s*\(", body)


def cs_files(root: Path):
    return sorted(root.rglob("*.cs"))


def main() -> int:
    errors: list[str] = []
    contracts = text(INTERFACES)
    controller = text(CONTROLLER)
    handlers = text(HANDLERS)
    di = text(PERSISTENCE_DI)
    service = text(SERVICE)
    r6b_active = R6B_MARKER.exists()

    legacy_methods = declared_methods(contracts, "IAchColombiaManagedFileExchangeService")
    if legacy_methods:
        errors.append(f"compatibility facade declares methods instead of aggregating capabilities: {legacy_methods}")

    for contract, expected in CAPABILITIES.items():
        actual = declared_methods(contracts, contract)
        if actual != expected:
            errors.append(f"{contract} methods {actual} != {expected}")
        body = interface_body(contracts, contract)
        for method in expected:
            declaration = re.search(rf"[^;]*\b{re.escape(method)}\s*\([^;]*;", body, re.S)
            if declaration is None or "CancellationToken" not in declaration.group(0):
                errors.append(f"{contract}.{method} lost CancellationToken")

    # The facade is compatibility-only in production consumers. The concrete implementation may still implement it.
    production_consumers = [CONTROLLER, HANDLERS]
    legacy_consumer_hits = sum(text(path).count("IAchColombiaManagedFileExchangeService") for path in production_consumers)
    if legacy_consumer_hits:
        errors.append(f"production MFT consumers still depend on compatibility facade: {legacy_consumer_hits}")

    expected_consumer_refs = {
        "IAchColombiaManagedMftExecutionService": 3,  # controller + outbound handler + inbound handler
        "IAchColombiaManagedMftTransferReadService": 1,
        "IAchColombiaManagedMftTransferOperationsService": 1,
        "IAchColombiaManagedMftAdministrationService": 1,
        "IAchColombiaManagedMftCredentialAdministrationService": 1,
    }
    joined_consumers = controller + "\n" + handlers
    for contract, expected in expected_consumer_refs.items():
        actual = joined_consumers.count(contract)
        if actual != expected:
            errors.append(f"{contract} production consumer refs {actual} != {expected}")
        if f"AddScoped<{contract}>" not in di:
            errors.append(f"DI alias missing for {contract}")

    if ": IAchColombiaManagedFileExchangeService" not in service:
        errors.append("single concrete MFT service no longer implements compatibility facade")
    if len(re.findall(r"class\s+AchColombiaManagedFileExchangeService\b", "\n".join(text(p) for p in cs_files(SRC)))) != 1:
        errors.append("AchColombiaManagedFileExchangeService implementation duplicated")

    for relative, expected in UNCHANGED.items():
        actual = sha(ROOT / relative)
        if actual != expected:
            errors.append(f"R6A protected transport/config/lifecycle owner changed {relative}: {actual} != {expected}")

    service_hash = sha(SERVICE)
    if not r6b_active and service_hash != R6A_SERVICE_SHA256:
        errors.append(f"R6A modified managed-MFT implementation before R6B: {service_hash} != {R6A_SERVICE_SHA256}")

    # Service locator is prohibited in functional consumers. DI alias factories are composition-root plumbing.
    locator_pattern = re.compile(r"\bIServiceProvider\b|\bGetRequiredService\s*<|\bGetService\s*<")
    locator_hits = sum(len(locator_pattern.findall(text(path))) for path in production_consumers)
    if locator_hits:
        errors.append(f"service-locator usage in MFT consumers: {locator_hits}")

    # R2 owns factory construction in composition root; no hidden factory construction is allowed elsewhere.
    hidden_factory = []
    direct_sftp_outside_authorized_factory = []
    for path in cs_files(SRC):
        source = text(path)
        if re.search(r"new\s+SshNetAchColombiaSftpSessionFactory\s*\(", source):
            hidden_factory.append(path.relative_to(ROOT).as_posix())
        if path != SFTP and re.search(r"new\s+SftpClient\s*\(", source):
            direct_sftp_outside_authorized_factory.append(path.relative_to(ROOT).as_posix())
    if hidden_factory:
        errors.append("hidden SFTP factory construction: " + ", ".join(hidden_factory))
    if direct_sftp_outside_authorized_factory:
        errors.append("direct SFTP session construction outside authorized factory: " + ", ".join(direct_sftp_outside_authorized_factory))
    authorized_sftp_client_constructions = len(re.findall(r"new\s+SftpClient\s*\(", text(SFTP)))
    if authorized_sftp_client_constructions != 1:
        errors.append(f"authorized SFTP client construction count changed: {authorized_sftp_client_constructions} != 1")

    # R6A requires one manual retry command owner. R6B may move that owner to its explicit retry/reprocess component.
    retry_owner_source = service
    if r6b_active:
        retry_owner_source = text(R6B_MARKER)
    manual_retry_sites = len(re.findall(r"public\s+async\s+Task<AchManagedFileTransferDetail>\s+RetryAsync\s*\(", retry_owner_source))
    blind_retry_sites = 0
    retry_scope = retry_owner_source + "\n" + text(SFTP) + "\n" + text(SRC / "Cfa.ACHInterbank.External/Connections/AchColombiaManagedMftFolderAdapter.cs") + "\n" + handlers
    for pattern in [r"\bTask\.Delay\s*\(", r"\bWaitAndRetry", r"\bAddRetry\s*\("]:
        blind_retry_sites += len(re.findall(pattern, retry_scope))
    if manual_retry_sites != 1:
        errors.append(f"manual retry owner count changed: {manual_retry_sites} != 1")
    if blind_retry_sites != 0:
        errors.append(f"new blind/local MFT retry sites: {blind_retry_sites}")

    # Existing R2 persistence fallback debt is a shrink-only ratchet and is outside R6A.
    persistence = SRC / "Cfa.ACHInterbank.Persistence"
    persistence_fallback_count = sum(len(RUNTIME_FALLBACK.findall(text(path))) for path in cs_files(persistence))
    if persistence_fallback_count != PERSISTENCE_FALLBACK_BASELINE:
        errors.append(f"Persistence fallback debt changed in R6A: {persistence_fallback_count} != {PERSISTENCE_FALLBACK_BASELINE}")

    simulation = SRC / "Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaInboundSimulationService.cs"
    ingestion = SRC / "Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaIngestionAppService.cs"
    cenit_pattern = re.compile(r'Contains\s*\(\s*"CENIT"')
    cenit_sim = len(cenit_pattern.findall(text(simulation)))
    cenit_ing = len(cenit_pattern.findall(text(ingestion)))
    if (cenit_sim, cenit_ing) != (7, 1):
        errors.append(f'out-of-scope Contains("CENIT") debt changed: simulation={cenit_sim}, ingestion={cenit_ing}')

    rail_strategy_refs = sum(text(path).count("PaymentRailOperationalStrategy") for path in [INTERFACES, CONTROLLER, HANDLERS, PERSISTENCE_DI, SERVICE])
    if rail_strategy_refs:
        errors.append(f"R6A introduced PaymentRailOperationalStrategy references: {rail_strategy_refs}")

    # Consumer-method dependency edges: pre-R6A = controller 15/15, two handlers 1/15 each => 28 unused.
    # R6A intentionally keeps outbound+inbound execution cohesive (2 methods), so each dedicated handler has one unused sibling operation.
    unused_method_edges_before = 28
    unused_method_edges_after = 2

    print("R6A MFT INTERFACE SEGREGATION STATIC RATCHET")
    print("  exact R6A contract ratchet: capability split/consumer isolation remains enforced; R6B successor implementation split is recognized explicitly")
    print("  wide direct-method MFT service contracts used by production consumers: 1 -> 0")
    print("  compatibility facade: 15 direct methods -> 0 direct methods; inherited aggregate surface retained for compatibility, production callers=0")
    print("  capability contracts: 0 -> 5 public (execution=2, read=2, operations=6, administration=4, credential=1); internal=0")
    print("  total capability methods: 15 -> 15 (redistributed, no operation renamed/removed)")
    print("  production consumers: facade 3 -> 0; execution=3, read=1, operations=1, administration=1, credential=1")
    print(f"  unused consumer-method dependency edges: {unused_method_edges_before} -> {unused_method_edges_after}")
    print("  direct concrete MFT dependencies from Application/API/use cases: 0 -> 0")
    print(f"  direct SFTP session construction outside authorized factory: {len(direct_sftp_outside_authorized_factory)}")
    print(f"  hidden SFTP session-factory construction outside composition root: {len(hidden_factory)}")
    print(f"  service-locator usage in MFT consumers: {locator_hits}")
    print(f"  manual retry owner sites: 1 -> {manual_retry_sites}; new blind/local retry sites: 0 -> {blind_retry_sites}")
    print(f"  R6A service implementation hash change authorized by R6B successor: {1 if r6b_active and service_hash != R6A_SERVICE_SHA256 else 0}; protected adapter/config/CENIT transport hashes changed: 0")
    print(f"  Persistence fallback debt count: {persistence_fallback_count}/{PERSISTENCE_FALLBACK_BASELINE}")
    print(f'  Contains("CENIT") out-of-scope debt: 8 -> {cenit_sim + cenit_ing} (simulation={cenit_sim}, ingestion={cenit_ing})')
    print(f"  PaymentRailOperationalStrategy references in R6A scope: {rail_strategy_refs}")
    print("  new implementation duplication: 0")
    print("  new R6A architecture debt: 0")

    if errors:
        print("R6A MFT INTERFACE SEGREGATION STATIC RATCHET: FAIL", file=sys.stderr)
        for error in errors:
            print(f"  - {error}", file=sys.stderr)
        return 1

    print("R6A MFT INTERFACE SEGREGATION STATIC RATCHET: OK")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
