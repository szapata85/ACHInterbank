#!/usr/bin/env python3
from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
IMPL = ROOT / "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation"
BUILDER = IMPL / "NachaFileBuilder.cs"
COORDINATOR = IMPL / "NachaFileGenerationCoordinator.cs"
PIPELINE = IMPL / "NachaOfficialAssemblyPipeline.cs"
RENDERER = IMPL / "NachaOfficialRecordRenderer.cs"
FIELD_HELPER = IMPL / "NachaDeterministicFieldFormatter.cs"
BLOCK_HELPER = IMPL / "NachaFileBlocking.cs"
INTERFACE = ROOT / "src/Cfa.ACHInterbank.Application/ACH/Interfaces/INachaFileBuilder.cs"
FOCUSED_TEST = ROOT / "tests/Cfa.ACHInterbank.Tests/Mapping/NachaOfficialAssemblyPipelineTests.cs"

R4B_BUILDER_LINES = 3796
EXPECTED_INTERFACE_METHODS = {
    "BuildReturnOutAsync",
    "BuildNachaFileAsync",
    "BuildNachaFileByCycleAsync",
    "BuildNachaFileArtifactByCycleAsync",
    "BuildNachaFilesByCycleAsync",
    "BuildRecordAsync",
}

FORBIDDEN_PIPELINE_TOKENS = (
    "AchDbContext",
    "DbContext",
    "HttpClient",
    "IConfiguration",
    "IServiceProvider",
    "AppSettings.Settings",
    "INachaConfigResolver",
    "INachaDataLoader",
    "INachaSemanticValidator",
    "PaymentRailOperationalStrategy",
    "CENIT",
    "ACHCOL",
    "AchCol",
    "ClearingHouse",
    "TransactionTypeEnum",
    "ResolveType7Layout",
    "ValidateType7Association",
    "PersistGenerationAuditAsync",
    "SaveChanges",
    "SHA256",
    "MFT",
    "SFTP",
    "File.",
    "Directory.",
    "INachaControlTotalsCalculator",
    "_controlTotalsCalculator",
)

BUILDER_OWNERSHIP_RETAINED = (
    "ResolveOfficialRuntimeConfigAsync",
    "_controlTotalsCalculator.Calculate",
    "BuildEntryDetailRecordsOfficialAsync",
    "ValidateCtxAddendaSequences",
    "ValidateType7Association",
    "ResolveType7Layout",
    "PersistGenerationAuditAsync",
    "SHA256.HashData",
    "_nachaSemanticValidator.Validate",
)

PIPELINE_OWNERSHIP_REQUIRED = (
    "internal sealed class NachaOfficialAssemblyPipeline",
    "AppendFileHeader",
    "BeginBatch",
    "BeginEntry",
    "AppendAddenda",
    "EndEntry",
    "EndBatch",
    "Complete",
    "INachaOfficialRecordRenderer",
    "NachaFileBlocking.BuildPaddingRecord",
    "NachaBatchControlTotals",
    "NachaFileControlTotals",
    "ValidateRenderedControlTotals",
    "NACHA_RECORD_COUNT_MISMATCH",
    "FileIntegrity:BatchCount=",
)


def fail(message: str) -> None:
    print(f"R4C NACHA ASSEMBLY PIPELINE STATIC RATCHET: FAIL - {message}")
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
    pipeline = read(PIPELINE)
    renderer = read(RENDERER)
    field_helper = read(FIELD_HELPER)
    block_helper = read(BLOCK_HELPER)
    interface = read(INTERFACE)
    read(FOCUSED_TEST)

    interface_methods = set(re.findall(r"\b(Build[A-Za-z0-9_]+)\s*(?:<[^>]+>)?\s*\(", interface))
    if interface_methods != EXPECTED_INTERFACE_METHODS:
        fail(f"INachaFileBuilder method set changed: {sorted(interface_methods)}")

    missing_pipeline = [token for token in PIPELINE_OWNERSHIP_REQUIRED if token not in pipeline]
    if missing_pipeline:
        fail(f"assembly pipeline is missing required deterministic ownership: {missing_pipeline}")

    for token in FORBIDDEN_PIPELINE_TOKENS:
        if token in pipeline:
            fail(f"assembly pipeline acquired forbidden IO/policy/persistence token {token!r}")

    generation_owner = coordinator if coordinator else builder
    missing_builder = [token for token in BUILDER_OWNERSHIP_RETAINED if token not in generation_owner]
    if missing_builder:
        fail(f"generation coordinator lost policy/config/totals/audit/hash ownership: {missing_builder}")

    official_method = extract_method(
        generation_owner,
        "    private async Task<string> BuildOfficialTableDrivenFileAsync(",
        "    private int AppendReturnOutOfficialRecords(",
    )
    required_delegations = (
        "new NachaOfficialAssemblyPipeline(",
        "assembly.AppendFileHeader(",
        "assembly.BeginBatch(",
        "assembly.BeginEntry(",
        "assembly.AppendAddenda(",
        "assembly.EndEntry(",
        "assembly.EndBatch(",
        "assembly.Complete(",
    )
    missing_delegations = [token for token in required_delegations if token not in official_method]
    if missing_delegations:
        fail(f"official builder path does not delegate assembly stages: {missing_delegations}")

    forbidden_official_builder_tokens = (
        "new StringBuilder(",
        "AppendOfficialRecords(",
        "NachaFileBlocking.BuildPaddingRecord",
        "ValidateRenderedControlTotals(",
        "ValidateRenderedControlField(",
        "OfficialRecordRenderer.Render(",
    )
    retained_assembly = [token for token in forbidden_official_builder_tokens if token in official_method]
    if retained_assembly:
        fail(f"official builder path still owns R4C physical assembly details: {retained_assembly}")

    if "private int AppendOfficialRecords(" in generation_owner:
        fail("generic official AppendOfficialRecords helper remains in the facade")
    if "private static void ValidateRenderedControlTotals(" in generation_owner or "private static void ValidateRenderedControlField(" in generation_owner:
        fail("rendered control-total verification remains duplicated in the facade")

    if "internal interface" in pipeline:
        fail("R4C introduced an unnecessary internal interface for the assembly session")

    pipeline_order = [
        pipeline.find("public void AppendFileHeader"),
        pipeline.find("public void BeginBatch"),
        pipeline.find("public void BeginEntry"),
        pipeline.find("public void AppendAddenda"),
        pipeline.find("public void EndBatch"),
        pipeline.find("public string Complete"),
    ]
    if any(position < 0 for position in pipeline_order) or pipeline_order != sorted(pipeline_order):
        fail("assembly stage contract is missing or unexpectedly reordered")

    builder_lines = len(builder.splitlines())
    if builder_lines >= R4B_BUILDER_LINES:
        fail(f"NachaFileBuilder did not shrink below the R4B baseline {R4B_BUILDER_LINES} (current={builder_lines})")

    renderer_files = sorted(path.name for path in IMPL.glob("*Renderer*.cs"))
    if renderer_files != ["NachaFixedWidthRecordRenderer.cs", "NachaOfficialRecordRenderer.cs", "NachaType7LegacyRenderer.cs"]:
        fail(f"renderer set changed unexpectedly in R4C: {renderer_files}")

    assembly_files = sorted(path.name for path in IMPL.glob("*Assembly*.cs"))
    if assembly_files != ["NachaOfficialAssemblyPipeline.cs"]:
        fail(f"unexpected assembly component set: {assembly_files}")

    deterministic_scope = builder + coordinator + pipeline + renderer + field_helper + block_helper
    if "PaymentRailOperationalStrategy" in deterministic_scope:
        fail("R4C scope introduced PaymentRailOperationalStrategy coupling")

    print("R4C NACHA ASSEMBLY PIPELINE STATIC RATCHET")
    print(f"  public facade methods: {len(interface_methods)}/{len(EXPECTED_INTERFACE_METHODS)} unchanged")
    print(f"  NachaFileBuilder lines: {R4B_BUILDER_LINES} -> {builder_lines}")
    print("  deterministic components: 3 -> 4 (formatter, blocking, official renderer, official assembly pipeline)")
    print("  renderer files: 3 -> 3")
    print("  assembly components: 0 -> 1")
    print("  new R4C internal interfaces: 0")
    print("  official physical assembly/order/control verification/padding: builder -> deterministic assembly pipeline")
    print("  PUBLISH/profile selection, transaction/batch preparation, rail policy, totals calculation, semantic validation, hash and audit persistence: retained in the generation owner")
    print("  pipeline IO/DB/global-config/rail-policy/hash/persistence forbidden tokens: 0")
    print("  PaymentRailOperationalStrategy references in R4C scope: 0")
    print("R4C NACHA ASSEMBLY PIPELINE STATIC RATCHET: OK")


if __name__ == "__main__":
    main()
