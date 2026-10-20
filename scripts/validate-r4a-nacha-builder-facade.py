#!/usr/bin/env python3
from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
BUILDER = ROOT / "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaFileBuilder.cs"
COORDINATOR = ROOT / "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaFileGenerationCoordinator.cs"
FIELD_HELPER = ROOT / "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaDeterministicFieldFormatter.cs"
BLOCK_HELPER = ROOT / "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaFileBlocking.cs"
INTERFACE = ROOT / "src/Cfa.ACHInterbank.Application/ACH/Interfaces/INachaFileBuilder.cs"
OFFICIAL_RENDERER = ROOT / "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaOfficialRecordRenderer.cs"

EXPECTED_INTERFACE_METHODS = {
    "BuildReturnOutAsync",
    "BuildNachaFileAsync",
    "BuildNachaFileByCycleAsync",
    "BuildNachaFileArtifactByCycleAsync",
    "BuildNachaFilesByCycleAsync",
    "BuildRecordAsync",
}

FORBIDDEN_HELPER_TOKENS = (
    "AchDbContext",
    "DbContext",
    "HttpClient",
    "IConfiguration",
    "IServiceProvider",
    "PaymentRailOperationalStrategy",
    "ACHCOL",
    "CENIT",
    "File.",
    "Directory.",
)


def fail(message: str) -> None:
    print(f"R4A NACHA BUILDER STATIC RATCHET: FAIL - {message}")
    sys.exit(1)


def read(path: Path) -> str:
    if not path.exists():
        fail(f"missing required file: {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8")


def main() -> None:
    builder = read(BUILDER)
    coordinator = COORDINATOR.read_text(encoding="utf-8") if COORDINATOR.exists() else ""
    field_helper = read(FIELD_HELPER)
    block_helper = read(BLOCK_HELPER)
    interface = read(INTERFACE)

    if "public class NachaFileBuilder : INachaFileBuilder" not in builder:
        fail("NachaFileBuilder no longer preserves the public facade contract")

    interface_methods = set(re.findall(r"\b(Build[A-Za-z0-9_]+)\s*(?:<[^>]+>)?\s*\(", interface))
    if interface_methods != EXPECTED_INTERFACE_METHODS:
        fail(f"INachaFileBuilder method set changed: {sorted(interface_methods)}")

    if "internal static class NachaDeterministicFieldFormatter" not in field_helper:
        fail("deterministic field formatter is not internal/static")
    if "internal static class NachaFileBlocking" not in block_helper:
        fail("file blocking helper is not internal/static")

    for path, content in ((FIELD_HELPER, field_helper), (BLOCK_HELPER, block_helper)):
        for token in FORBIDDEN_HELPER_TOKENS:
            if token in content:
                fail(f"deterministic helper {path.name} acquired forbidden dependency/policy token {token!r}")

    r4a_consumers = builder + "\n" + coordinator
    if OFFICIAL_RENDERER.exists():
        r4a_consumers += "\n" + read(OFFICIAL_RENDERER)

    required_field_delegations = (
        "NachaDeterministicFieldFormatter.TryResolveValue",
        "NachaDeterministicFieldFormatter.ResolveProperty",
        "NachaDeterministicFieldFormatter.NormalizeIdentifier",
    )
    missing_field = [token for token in required_field_delegations if token not in r4a_consumers]
    if missing_field:
        fail(f"R4A deterministic formatter is no longer delegated by the NACHA rendering path: {missing_field}")

    required_builder_delegations = (
        "NachaFileBlocking.Calculate",
        "NachaFileBlocking.ResolveConfiguredBlockSize",
        "NachaFileBlocking.BuildPaddingRecord",
        "_controlTotalsCalculator.Calculate",
    )
    generation_owner = coordinator if coordinator else builder
    missing_builder = [token for token in required_builder_delegations if token not in generation_owner]
    if missing_builder:
        fail(f"NACHA generation owner is missing required R4A blocking/control-total delegations: {missing_builder}")

    removed_builder_definitions = (
        "private static PropertyInfo? ResolveProperty(",
        "private static bool TryResolveValue(",
        "private static string NormalizeIdentifier(",
        "private static string NormalizeBatchSettlementDate(",
        "private static string FormatValue(",
        "private static int ResolveOfficialBlockSize(",
        "private static string BuildPaddingRecord(",
    )
    retained = [token for token in removed_builder_definitions if token in builder + "\n" + coordinator]
    if retained:
        fail(f"builder/coordinator still owns deterministic responsibilities extracted by R4A: {retained}")

    builder_lines = len(builder.splitlines())
    if builder_lines >= 4798:
        fail(f"builder did not shrink below the R3 baseline of 4798 lines (current={builder_lines})")

    changed_scope = builder + coordinator + field_helper + block_helper + (read(OFFICIAL_RENDERER) if OFFICIAL_RENDERER.exists() else "")
    if "PaymentRailOperationalStrategy" in changed_scope:
        fail("R4A builder extraction introduced PaymentRailOperationalStrategy coupling")

    print("R4A NACHA BUILDER STATIC RATCHET")
    print(f"  public facade methods: {len(interface_methods)}/{len(EXPECTED_INTERFACE_METHODS)} unchanged")
    print(f"  NachaFileBuilder lines: 4798 -> {builder_lines}")
    print("  extracted deterministic components: field-resolution/legacy-formatting, blocking/padding")
    print("  pre-existing control-totals component: retained/delegated")
    print("  forward-compatible renderer ownership: R4B may consume R4A deterministic helpers without moving policy/IO into them")
    print("  PaymentRailOperationalStrategy references in R4A scope: 0")
    print("  helper IO/DB/chamber-policy forbidden tokens: 0")
    print("R4A NACHA BUILDER STATIC RATCHET: OK")


if __name__ == "__main__":
    main()
