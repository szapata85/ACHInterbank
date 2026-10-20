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
PIPELINE = IMPL / "NachaOfficialAssemblyPipeline.cs"
INTERFACE = ROOT / "src/Cfa.ACHInterbank.Application/ACH/Interfaces/INachaFileBuilder.cs"
FOCUSED_TEST = ROOT / "tests/Cfa.ACHInterbank.Tests/Mapping/NachaChamberFlowPolicyTests.cs"

R4C_BUILDER_LINES = 3693
EXPECTED_INTERFACE_METHODS = {
    "BuildReturnOutAsync",
    "BuildNachaFileAsync",
    "BuildNachaFileByCycleAsync",
    "BuildNachaFileArtifactByCycleAsync",
    "BuildNachaFilesByCycleAsync",
    "BuildRecordAsync",
}

FORBIDDEN_POLICY_TOKENS = (
    "AchDbContext",
    "DbContext",
    "HttpClient",
    "IConfiguration",
    "IServiceProvider",
    "AppSettings.Settings",
    "PaymentRailOperationalStrategy",
    "ClearingHouseId",
    "_context",
    "SaveChanges",
    "PersistGenerationAuditAsync",
    "SHA256",
    "MFT",
    "SFTP",
    "File.",
    "Directory.",
    "INachaControlTotalsCalculator",
    "NachaOfficialAssemblyPipeline",
)

POLICY_REQUIRED = (
    "internal interface INachaChamberFlowPolicy",
    "internal static class NachaChamberFlowPolicyResolver",
    "internal sealed class AchColombiaNachaChamberFlowPolicy",
    "internal sealed class CenitNachaChamberFlowPolicy",
    "internal sealed class CompatibilityNachaChamberFlowPolicy",
    "RequiresServicePartitioning",
    "ResolveReturnOutFlow",
    "ResolveOrdinaryFlow",
    "CreateOfficialRecordRenderContext",
    "EnforcePreResolutionGenerationGate",
    "EnforceResolvedProfileGate",
    "ValidateOfficialLayoutSnapshot",
    'string.Equals(clearingHouseCode, "CENIT"',
    'string.Equals(clearingHouseCode, "ACH"',
    "CenitCtxOutbound2026Layout.IsProfile",
    "CenitReturnOfReturn2026Layout.FlowTypeCode",
    "AchColReturnOutV35Layout.RecordLength",
    "CenitReturnOut2026Layout.RecordLength",
)

BUILDER_RETAINED_OWNERS = (
    "ResolveOfficialRuntimeConfigAsync",
    "_configResolver.ResolvePublishedOrdinaryAsync",
    "NACHA_PUBLISH_IDENTITY_MISMATCH",
    "BuildConfigResolutionRequest",
    "_controlTotalsCalculator.Calculate",
    "ValidateCtxAddendaSequences",
    "ValidateType7Association",
    "ResolveType7Layout",
    "new NachaOfficialAssemblyPipeline(",
    "_nachaSemanticValidator.Validate",
    "SHA256.HashData",
    "PersistGenerationAuditAsync",
    "BuildReturnOutAsync",
    "AppendReturnOutOfficialRecords",
)


def fail(message: str) -> None:
    print(f"R4D CHAMBER/FLOW POLICY STATIC RATCHET: FAIL - {message}")
    sys.exit(1)


def read(path: Path) -> str:
    if not path.exists():
        fail(f"missing required file: {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8")


def extract_method(text: str, signature: str, next_signature: str) -> str:
    start = text.find(signature)
    if start < 0:
        fail(f"method signature missing: {signature.strip()}")
    end = text.find(next_signature, start)
    if end < 0:
        fail(f"method boundary missing after: {signature.strip()}")
    return text[start:end]


def main() -> None:
    builder = read(BUILDER)
    coordinator = COORDINATOR.read_text(encoding="utf-8") if COORDINATOR.exists() else ""
    policy = read(POLICY)
    pipeline = read(PIPELINE)
    interface = read(INTERFACE)
    read(FOCUSED_TEST)

    interface_methods = set(re.findall(r"\b(Build[A-Za-z0-9_]+)\s*(?:<[^>]+>)?\s*\(", interface))
    if interface_methods != EXPECTED_INTERFACE_METHODS:
        fail(f"INachaFileBuilder method set changed: {sorted(interface_methods)}")

    missing_policy = [token for token in POLICY_REQUIRED if token not in policy]
    if missing_policy:
        fail(f"chamber/flow policy boundary is incomplete: {missing_policy}")

    for token in FORBIDDEN_POLICY_TOKENS:
        if token in policy:
            fail(f"chamber/flow policy acquired forbidden IO/DB/PUBLISH/totals/side-effect token {token!r}")

    generation_owner = coordinator if coordinator else builder
    missing_builder = [token for token in BUILDER_RETAINED_OWNERS if token not in generation_owner]
    if missing_builder:
        fail(f"generation coordinator/existing components lost non-R4D ownership: {missing_builder}")

    return_out_method = extract_method(
        generation_owner,
        "    public async Task<NachaReturnOutBuildResult> BuildReturnOutAsync(",
        "    private static IReadOnlyDictionary<string, object?> ReturnOutValues",
    )
    official_method = extract_method(
        generation_owner,
        "    private async Task<string> BuildOfficialTableDrivenFileAsync(",
        "    private int AppendReturnOutOfficialRecords(",
    )

    for label, method in (("ReturnOut", return_out_method), ("official ordinary", official_method)):
        forbidden_direct_branches = (
            'string.Equals(clearingHouseCode, "CENIT"',
            'string.Equals(clearingHouseCode, "ACH"',
            "CenitCtxOutbound2026Layout.IsProfile",
            "CenitReturnOfReturn2026Layout.FlowTypeCode",
        )
        retained = [token for token in forbidden_direct_branches if token in method]
        if retained:
            fail(f"{label} builder path still owns direct chamber/flow decisions: {retained}")

    required_delegations = (
        "NachaChamberFlowPolicyResolver.Resolve(clearingHouseCode)",
        "chamberPolicy.ResolveReturnOutFlow",
        "chamberPolicy.ResolveOrdinaryFlow",
        "chamberPolicy.CreateOfficialRecordRenderContext",
        "chamberPolicy.EnforceResolvedProfileGate",
        "chamberPolicy.ValidateOfficialLayoutSnapshot",
    )
    combined = builder + "\n" + coordinator + "\n" + policy
    missing_delegations = [token for token in required_delegations if token not in combined]
    if missing_delegations:
        fail(f"R4D policy delegation is incomplete: {missing_delegations}")

    if "PaymentRailOperationalStrategy" in builder + coordinator + policy + pipeline:
        fail("R4D introduced PaymentRailOperationalStrategy")

    # R4C physical assembly must remain rail-agnostic after R4D.
    for token in ("CENIT", "ACHCOL", "AchCol", "ClearingHouse", "INachaChamberFlowPolicy"):
        if token in pipeline:
            fail(f"R4D leaked chamber policy into deterministic assembly pipeline via {token!r}")

    builder_lines = len(builder.splitlines())
    if builder_lines >= R4C_BUILDER_LINES:
        fail(f"NachaFileBuilder did not shrink below the R4C baseline {R4C_BUILDER_LINES} (current={builder_lines})")

    renderer_files = sorted(path.name for path in IMPL.glob("*Renderer*.cs"))
    if renderer_files != ["NachaFixedWidthRecordRenderer.cs", "NachaOfficialRecordRenderer.cs", "NachaType7LegacyRenderer.cs"]:
        fail(f"renderer set changed unexpectedly in R4D: {renderer_files}")

    assembly_files = sorted(path.name for path in IMPL.glob("*Assembly*.cs"))
    if assembly_files != ["NachaOfficialAssemblyPipeline.cs"]:
        fail(f"assembly component set changed unexpectedly in R4D: {assembly_files}")

    policy_interface_count = policy.count("internal interface ")
    policy_impl_count = len(re.findall(r"internal sealed class .*NachaChamberFlowPolicy", policy))
    if policy_interface_count != 1:
        fail(f"expected exactly one internal chamber/flow policy interface, found {policy_interface_count}")
    if policy_impl_count != 3:
        fail(f"expected three chamber/compatibility policy implementations, found {policy_impl_count}")

    print("R4D CHAMBER/FLOW POLICY STATIC RATCHET")
    print(f"  public facade methods: {len(interface_methods)}/{len(EXPECTED_INTERFACE_METHODS)} unchanged")
    print(f"  NachaFileBuilder lines: {R4C_BUILDER_LINES} -> {builder_lines}")
    print("  deterministic formatter/blocking/renderer/assembly components: 4 -> 4")
    print("  renderer files: 3 -> 3")
    print("  assembly components: 1 -> 1")
    print("  chamber/flow policy files: 0 -> 1")
    print(f"  new R4D internal policy interfaces: {policy_interface_count}")
    print(f"  chamber/compatibility policy implementations: {policy_impl_count}")
    print("  direct official ReturnOut/ordinary chamber decisions: builder -> chamber/flow policies")
    print("  PUBLISH/profile resolution, transaction/batch preparation, totals calculation, semantic validation, SHA-256 and audit persistence: retained in generation coordinator/existing owners")
    print("  policy DB/HTTP/filesystem/PUBLISH/totals/hash/persistence/MFT/SFTP forbidden tokens: 0")
    print("  PaymentRailOperationalStrategy references in R4D scope: 0")
    print("R4D CHAMBER/FLOW POLICY STATIC RATCHET: OK")


if __name__ == "__main__":
    main()
