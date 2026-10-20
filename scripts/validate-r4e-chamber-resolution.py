#!/usr/bin/env python3
from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
IMPL = ROOT / "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation"
BUILDER = IMPL / "NachaFileBuilder.cs"
COORDINATOR = IMPL / "NachaFileGenerationCoordinator.cs"
POLICY = IMPL / "NachaChamberFlowPolicy.cs"
CANONICAL_RESOLVER = IMPL / "NachaClearingHouseProfileCodeResolver.cs"
PIPELINE = IMPL / "NachaOfficialAssemblyPipeline.cs"
RENDERER = IMPL / "NachaOfficialRecordRenderer.cs"
INTERFACE = ROOT / "src/Cfa.ACHInterbank.Application/ACH/Interfaces/INachaFileBuilder.cs"
FOCUSED_TEST = ROOT / "tests/Cfa.ACHInterbank.Tests/Mapping/NachaFileBuilderFileIntegrityClosureTests.cs"

R4D_BUILDER_LINES = 3349
R4D_NAME_CONTAINS_CENIT = 2
R4D_ADDITIONAL_NAME_HEURISTICS = 1
EXPECTED_INTERFACE_METHODS = {
    "BuildReturnOutAsync",
    "BuildNachaFileAsync",
    "BuildNachaFileByCycleAsync",
    "BuildNachaFileArtifactByCycleAsync",
    "BuildNachaFilesByCycleAsync",
    "BuildRecordAsync",
}

# These are rail-identification heuristics, not harmless display/audit references to names.
NAME_HEURISTIC_PATTERNS = (
    # Any partial-match decision based on a clearing-house display name is forbidden in
    # the outbound generation scope, regardless of the literal/config value involved.
    re.compile(r"ClearingHouse\??\.Name\??\.Contains\s*\(", re.IGNORECASE),
    re.compile(r"clearingHouseName\s*\.Contains\s*\(", re.IGNORECASE),
    re.compile(r"ClearingHouse\??\.Name\s*==\s*\"(?:CENIT|ACH|ACHCOL)\"", re.IGNORECASE),
    re.compile(r"string\.Equals\s*\(\s*[^,]*ClearingHouse\??\.Name\s*,\s*\"(?:CENIT|ACH|ACHCOL)\"", re.IGNORECASE),
    re.compile(r"\?\s*\"CENIT\"\s*:\s*\"ACH\"", re.IGNORECASE),
    re.compile(r"\?\s*\"ACH\"\s*:\s*\"CENIT\"", re.IGNORECASE),
)

DB_ID_RAIL_PATTERNS = (
    re.compile(r"ClearingHouseId\s*==\s*\d+"),
    re.compile(r"ClearingHouseId\s*!=\s*\d+"),
    re.compile(r"switch\s*\([^)]*ClearingHouseId", re.IGNORECASE),
    re.compile(r"ClearingHouseId\s+is\s+\d+", re.IGNORECASE),
)

FACADE_FORBIDDEN_OWNERSHIP = (
    "ResolveOfficialRuntimeConfigAsync",
    "BuildOfficialTableDrivenFileAsync",
    "BuildFileAsync",
    "_configResolver",
    "_controlTotalsCalculator",
    "_nachaSemanticValidator",
    "PersistGenerationAuditAsync",
    "SHA256.HashData",
    "NachaOfficialAssemblyPipeline",
    "NachaChamberFlowPolicyResolver.Resolve",
)

COORDINATOR_REQUIRED_OWNERSHIP = (
    "ResolveOfficialRuntimeConfigAsync",
    "BuildOfficialTableDrivenFileAsync",
    "BuildFileAsync",
    "ResolveClearingHouseCodeAsync",
    "NachaClearingHouseProfileCodeResolver.ResolveAsync",
    "_controlTotalsCalculator.Calculate",
    "new NachaOfficialAssemblyPipeline(",
    "_nachaSemanticValidator.Validate",
    "SHA256.HashData",
    "PersistGenerationAuditAsync",
    "NachaChamberFlowPolicyResolver.Resolve",
)


def fail(message: str) -> None:
    print(f"R4E CHAMBER RESOLUTION STATIC RATCHET: FAIL - {message}")
    sys.exit(1)


def read(path: Path) -> str:
    if not path.exists():
        fail(f"missing required file: {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8")


def count_patterns(text: str, patterns: tuple[re.Pattern[str], ...]) -> int:
    return sum(len(pattern.findall(text)) for pattern in patterns)


def main() -> None:
    builder = read(BUILDER)
    coordinator = read(COORDINATOR)
    policy = read(POLICY)
    canonical = read(CANONICAL_RESOLVER)
    pipeline = read(PIPELINE)
    renderer = read(RENDERER)
    interface = read(INTERFACE)
    focused_test = read(FOCUSED_TEST)

    interface_methods = set(re.findall(r"\b(Build[A-Za-z0-9_]+)\s*(?:<[^>]+>)?\s*\(", interface))
    if interface_methods != EXPECTED_INTERFACE_METHODS:
        fail(f"INachaFileBuilder method set changed: {sorted(interface_methods)}")

    if "[Scoped]" not in builder or "public class NachaFileBuilder : INachaFileBuilder" not in builder:
        fail("public scoped NachaFileBuilder facade contract is missing")
    if "internal sealed class NachaFileGenerationCoordinator" not in coordinator:
        fail("R4E internal generation coordinator is missing")
    if "_coordinator." not in builder:
        fail("NachaFileBuilder no longer delegates to the R4E generation coordinator")

    missing_coordinator = [token for token in COORDINATOR_REQUIRED_OWNERSHIP if token not in coordinator]
    if missing_coordinator:
        fail(f"generation coordinator lost established generation ownership: {missing_coordinator}")

    retained_facade_ownership = [token for token in FACADE_FORBIDDEN_OWNERSHIP if token in builder]
    if retained_facade_ownership:
        fail(f"facade still owns post-R4E generation internals: {retained_facade_ownership}")

    outbound_scope = builder + "\n" + coordinator + "\n" + policy
    name_heuristics = count_patterns(outbound_scope, NAME_HEURISTIC_PATTERNS)
    if name_heuristics:
        fail(f"free-text/equivalent chamber-name heuristics remain in outbound generation scope: {name_heuristics}")

    db_id_inference = count_patterns(outbound_scope, DB_ID_RAIL_PATTERNS)
    if db_id_inference:
        fail(f"DB-id rail inference detected in outbound generation scope: {db_id_inference}")

    if 'string.Equals(clearingHouseCode, "CENIT"' not in policy or 'string.Equals(clearingHouseCode, "ACH"' not in policy:
        fail("canonical code-based policy selection is missing")
    if "NachaClearingHouseProfileCodeResolver.ResolveAsync" not in coordinator:
        fail("coordinator does not use the established canonical/configured chamber resolver")
    if "item.Code == operationalCode" not in canonical:
        fail("canonical resolver no longer prefers exact operational code")
    if "item.Name == operationalName" not in canonical:
        fail("configured exact-name-to-canonical-code catalog mapping was unexpectedly removed")
    if ".Contains(\"CENIT\"" in canonical or '? "CENIT" : "ACH"' in canonical:
        fail("canonical resolver contains a partial/free-text rail heuristic")

    if "PaymentRailOperationalStrategy" in outbound_scope + pipeline + renderer + canonical:
        fail("R4E introduced PaymentRailOperationalStrategy")

    # The ordinary assembly and renderer must remain rail-agnostic/deterministic.
    for label, content in (("assembly", pipeline), ("renderer", renderer)):
        for token in ("ClearingHouse", "CENIT", "ACHCOL", "INachaChamberFlowPolicy"):
            if token in content:
                fail(f"R4E leaked rail resolution into {label} via {token!r}")

    builder_lines = len(builder.splitlines())
    if builder_lines >= R4D_BUILDER_LINES:
        fail(f"NachaFileBuilder did not thin below R4D baseline {R4D_BUILDER_LINES} (current={builder_lines})")
    if builder_lines > 500:
        fail(f"NachaFileBuilder remains above the R4E indicative facade target (current={builder_lines})")

    # Characterization proves an ACH canonical code wins even when the display label contains CENIT.
    if '[InlineData("ACH CENIT Legacy Label", "ACH")]' not in focused_test:
        fail("focused characterization for misleading chamber display-name text is missing")
    if 'chamberCode == "ACH" ? 1 : 15' not in focused_test:
        fail("focused characterization still derives expected behavior from chamber display name")

    renderer_files = sorted(path.name for path in IMPL.glob("*Renderer*.cs"))
    if renderer_files != ["NachaFixedWidthRecordRenderer.cs", "NachaOfficialRecordRenderer.cs", "NachaType7LegacyRenderer.cs"]:
        fail(f"renderer set changed unexpectedly in R4E: {renderer_files}")
    assembly_files = sorted(path.name for path in IMPL.glob("*Assembly*.cs"))
    if assembly_files != ["NachaOfficialAssemblyPipeline.cs"]:
        fail(f"assembly component set changed unexpectedly in R4E: {assembly_files}")

    print("R4E CHAMBER RESOLUTION STATIC RATCHET")
    print(f"  public facade methods: {len(interface_methods)}/{len(EXPECTED_INTERFACE_METHODS)} unchanged")
    print(f"  NachaFileBuilder lines: {R4D_BUILDER_LINES} -> {builder_lines}")
    print(f"  ClearingHouse.Name.Contains(\"CENIT\") outbound fallbacks: {R4D_NAME_CONTAINS_CENIT} -> 0")
    print(f"  additional equivalent outbound name heuristics: {R4D_ADDITIONAL_NAME_HEURISTICS} -> 0")
    print("  DB-id rail inference: 0 -> 0")
    print("  canonical/configured resolution owner: NachaClearingHouseProfileCodeResolver + INachaChamberFlowPolicy")
    print("  facade generation implementation: moved to internal NachaFileGenerationCoordinator")
    print("  renderer files: 3 -> 3")
    print("  assembly components: 1 -> 1")
    print("  PaymentRailOperationalStrategy references in R4E scope: 0")
    print("  new chamber-resolution debt/exceptions: 0")
    print("R4E CHAMBER RESOLUTION STATIC RATCHET: OK")


if __name__ == "__main__":
    main()
