#!/usr/bin/env python3
from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
IMPL = ROOT / "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation"
BUILDER = IMPL / "NachaFileBuilder.cs"
COORDINATOR = IMPL / "NachaFileGenerationCoordinator.cs"
RENDERER = IMPL / "NachaOfficialRecordRenderer.cs"
ASSEMBLY_PIPELINE = IMPL / "NachaOfficialAssemblyPipeline.cs"
FIELD_HELPER = IMPL / "NachaDeterministicFieldFormatter.cs"
BLOCK_HELPER = IMPL / "NachaFileBlocking.cs"
INTERFACE = ROOT / "src/Cfa.ACHInterbank.Application/ACH/Interfaces/INachaFileBuilder.cs"
FOCUSED_TEST = ROOT / "tests/Cfa.ACHInterbank.Tests/Mapping/NachaOfficialRecordRendererTests.cs"

R4A_BUILDER_LINES = 4575
EXPECTED_INTERFACE_METHODS = {
    "BuildReturnOutAsync",
    "BuildNachaFileAsync",
    "BuildNachaFileByCycleAsync",
    "BuildNachaFileArtifactByCycleAsync",
    "BuildNachaFilesByCycleAsync",
    "BuildRecordAsync",
}

FORBIDDEN_RENDERER_TOKENS = (
    "AchDbContext",
    "DbContext",
    "HttpClient",
    "IConfiguration",
    "IServiceProvider",
    "AppSettings.Settings",
    "PaymentRailOperationalStrategy",
    "Cenit",
    "CENIT",
    "AchCol",
    "ACHCOL",
    "File.",
    "Directory.",
    "_context",
    "SaveChanges",
    "StringBuilder",
    "INachaControlTotalsCalculator",
)

BUILDER_RENDERING_METHODS_REMOVED = (
    "private string RenderOfficialRecord(",
    "private static string ExecuteOfficialFieldRules(",
    "private object? ResolveOfficialRawValue(",
    "private static string FormatOfficialValue(",
    "private static Task<string> BuildRecordInternalAsync",
)

POST_RENDER_OWNERSHIP_RETAINED = (
    "private async Task<string> BuildOfficialTableDrivenFileAsync(",
    "private async Task<string> BuildFileAsync(",
    "_controlTotalsCalculator.Calculate",
    "_nachaSemanticValidator.Validate",
    "PersistGenerationAuditAsync",
    "ResolveOfficialRuntimeConfigAsync",
)


def fail(message: str) -> None:
    print(f"R4B NACHA RECORD RENDERER STATIC RATCHET: FAIL - {message}")
    sys.exit(1)


def read(path: Path) -> str:
    if not path.exists():
        fail(f"missing required file: {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8")


def main() -> None:
    builder = read(BUILDER)
    coordinator = COORDINATOR.read_text(encoding="utf-8") if COORDINATOR.exists() else ""
    renderer = read(RENDERER)
    assembly_pipeline = ASSEMBLY_PIPELINE.read_text(encoding="utf-8") if ASSEMBLY_PIPELINE.exists() else ""
    field_helper = read(FIELD_HELPER)
    block_helper = read(BLOCK_HELPER)
    interface = read(INTERFACE)
    read(FOCUSED_TEST)

    interface_methods = set(re.findall(r"\b(Build[A-Za-z0-9_]+)\s*(?:<[^>]+>)?\s*\(", interface))
    if interface_methods != EXPECTED_INTERFACE_METHODS:
        fail(f"INachaFileBuilder method set changed: {sorted(interface_methods)}")

    if "internal interface INachaOfficialRecordRenderer" not in renderer:
        fail("R4B internal official renderer contract is missing")
    if "internal sealed class NachaOfficialRecordRenderer : INachaOfficialRecordRenderer" not in renderer:
        fail("R4B official renderer implementation is missing or no longer internal/sealed")
    generation_owner = coordinator if coordinator else builder
    render_consumers = builder + "\n" + coordinator + "\n" + assembly_pipeline
    if "NachaOfficialRecordRenderer" not in generation_owner or "OfficialRecordRenderer" not in render_consumers or ".Render(" not in render_consumers:
        fail("official NACHA rendering no longer delegates to the R4B renderer")

    retained_rendering = [token for token in BUILDER_RENDERING_METHODS_REMOVED if token in builder + "\n" + coordinator]
    if retained_rendering:
        fail(f"builder/coordinator still owns R4B record-rendering responsibilities: {retained_rendering}")

    missing_post_render = [token for token in POST_RENDER_OWNERSHIP_RETAINED if token not in generation_owner]
    if missing_post_render:
        fail(f"R4B post-render generation ownership regressed: {missing_post_render}")

    for token in FORBIDDEN_RENDERER_TOKENS:
        if token in renderer:
            fail(f"deterministic R4B renderer acquired forbidden IO/DB/rail/assembly token {token!r}")

    required_renderer_primitives = (
        "NachaDeterministicFieldFormatter.TryResolveValue",
        "NachaDeterministicFieldFormatter.ResolveProperty",
        "NachaDeterministicFieldFormatter.NormalizeIdentifier",
        "CfgLayoutVariant",
        "CfgFieldRule",
        "NachaGenerationTraceEntry",
    )
    missing_primitives = [token for token in required_renderer_primitives if token not in renderer]
    if missing_primitives:
        fail(f"R4B renderer no longer uses the published-layout/R4A deterministic primitives: {missing_primitives}")

    changed_scope = builder + coordinator + renderer + assembly_pipeline + field_helper + block_helper
    if "PaymentRailOperationalStrategy" in changed_scope:
        fail("R4B scope introduced PaymentRailOperationalStrategy coupling")

    builder_lines = len(builder.splitlines())
    if builder_lines >= R4A_BUILDER_LINES:
        fail(f"NachaFileBuilder did not shrink below the R4A baseline {R4A_BUILDER_LINES} (current={builder_lines})")

    renderer_files = sorted(path.name for path in IMPL.glob("*Renderer*.cs"))
    if renderer_files != ["NachaFixedWidthRecordRenderer.cs", "NachaOfficialRecordRenderer.cs", "NachaType7LegacyRenderer.cs"]:
        fail(f"unexpected renderer component set: {renderer_files}")

    print("R4B NACHA RECORD RENDERER STATIC RATCHET")
    print(f"  public facade methods: {len(interface_methods)}/{len(EXPECTED_INTERFACE_METHODS)} unchanged")
    print(f"  NachaFileBuilder lines: {R4A_BUILDER_LINES} -> {builder_lines}")
    print("  renderer files in implementation root: 2 -> 3")
    print("  new internal renderer interfaces: 0 -> 1")
    print("  builder-owned official field rendering/rules/trace: extracted")
    print("  R4C/R4E-forward compatibility: assembly may move to its pipeline; profile/totals/semantic-validation/audit persistence remain in the generation owner")
    print("  renderer IO/DB/rail-policy/assembly forbidden tokens: 0")
    print("  PaymentRailOperationalStrategy references in R4B scope: 0")
    print("R4B NACHA RECORD RENDERER STATIC RATCHET: OK")


if __name__ == "__main__":
    main()
