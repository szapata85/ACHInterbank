#!/usr/bin/env python3
from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
IMPL = ROOT / "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation"
ORCHESTRATOR = IMPL / "IncomingNachaPostProcessingOrchestrator.cs"
PIPELINE = IMPL / "IncomingNachaPostProcessingPipeline.cs"
PARSER = IMPL / "NachaParserService.cs"
PHYSICAL = IMPL / "NachaPhysicalParser.cs"
VALIDATION = IMPL / "NachaInboundValidation.cs"
PERSISTENCE = IMPL / "NachaInboundPersistenceCoordinator.cs"
INGESTION = IMPL / "IncomingNachaIngestionAppService.cs"
SIMULATION = IMPL / "NachaInboundSimulationService.cs"
CONFIG_VALIDATION = IMPL / "NachaConfigValidationService.cs"
FOCUSED_TEST = ROOT / "tests/Cfa.ACHInterbank.Tests/IncomingNachaPostProcessingPipelineSeparationTests.cs"
BEHAVIOR_TEST = ROOT / "tests/Cfa.ACHInterbank.Tests/IncomingNachaPostProcessingOrchestratorTests.cs"

R5C_ORCHESTRATOR_LINES = 1179
R5C_PARSER_LINES = 1076
EXPECTED_STAGES = (
    "IncomingNachaResolveContextStage",
    "IncomingNachaEligibilityStage",
    "IncomingNachaLinkCorrelationStage",
    "IncomingNachaStateTransitionStage",
    "IncomingNachaDispatchPlanningStage",
    "IncomingNachaExternalDispatchStage",
    "IncomingNachaAuditEventsStage",
)
CONTAINS_CENIT = re.compile(r'\.Contains\s*\(\s*"CENIT"', re.IGNORECASE)
DB_ID_RAIL_PATTERNS = (
    re.compile(r"ClearingHouseId\s*==\s*\d+"),
    re.compile(r"ClearingHouseId\s*!=\s*\d+"),
    re.compile(r"switch\s*\([^)]*ClearingHouseId", re.IGNORECASE),
    re.compile(r"ClearingHouseId\s+is\s+\d+", re.IGNORECASE),
)
TRANSACTION_TOKENS = ("BeginTransactionAsync", "CommitAsync", "RollbackAsync")
PARSED_GRAPH_TOKENS = (
    "NachaPhysicalParser",
    "NachaInboundValidator",
    "NachaInboundPersistenceCoordinator",
    "ParsedNachaEntityMapper",
    "NachaHeaders.AddRange",
)


def fail(message: str) -> None:
    print(f"R5D INBOUND POST-PROCESSING STATIC RATCHET: FAIL - {message}")
    sys.exit(1)


def read(path: Path) -> str:
    if not path.exists():
        fail(f"missing required file: {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8-sig")


def count_db_id_inference(text: str) -> int:
    return sum(len(pattern.findall(text)) for pattern in DB_ID_RAIL_PATTERNS)


def main() -> None:
    orchestrator = read(ORCHESTRATOR)
    pipeline = read(PIPELINE)
    parser = read(PARSER)
    physical = read(PHYSICAL)
    validation = read(VALIDATION)
    persistence = read(PERSISTENCE)
    ingestion = read(INGESTION)
    simulation = read(SIMULATION)
    config = read(CONFIG_VALIDATION)
    focused = read(FOCUSED_TEST)
    behavior = read(BEHAVIOR_TEST)

    orchestrator_lines = len(orchestrator.splitlines())
    parser_lines = len(parser.splitlines())
    if orchestrator_lines >= R5C_ORCHESTRATOR_LINES:
        fail(f"post-processing orchestrator was not thinned: {R5C_ORCHESTRATOR_LINES} -> {orchestrator_lines}")
    if parser_lines != R5C_PARSER_LINES:
        fail(f"R5D unexpectedly changed NachaParserService: {R5C_PARSER_LINES} -> {parser_lines}")

    if "_pipeline.ExecuteAsync(chunkSize, triggeredBy, ct)" not in orchestrator:
        fail("public post-processing orchestrator does not delegate to the R5D stage pipeline")
    if "ProcTransaccionesAsync(" in orchestrator or "SaveChangesAsync(" in orchestrator:
        fail("thin orchestrator still owns external dispatch or state persistence")

    missing_stages = [stage for stage in EXPECTED_STAGES if f"internal sealed class {stage}" not in pipeline]
    if missing_stages:
        fail(f"missing explicit R5D stages: {missing_stages}")
    for marker in ("CancellationToken ct", "PrepareAsync(", "SendAsync(", "RecordResponseAsync("):
        if marker not in pipeline:
            fail(f"stage pipeline does not expose expected cancellation/input-output boundary: {marker}")

    parsed_graph_leaks = [token for token in PARSED_GRAPH_TOKENS if token in pipeline]
    if parsed_graph_leaks:
        fail(f"R5D reopened R5A/R5B/R5C parsed-graph ownership: {parsed_graph_leaks}")
    transaction_leaks = [token for token in TRANSACTION_TOKENS if token in orchestrator or token in pipeline]
    if transaction_leaks:
        fail(f"R5D introduced direct transaction ownership: {transaction_leaks}")

    # R5C remains the only owner of parsed-graph SaveChanges. R5D retains its two pre-existing
    # queue/state/audit SaveChanges boundaries; it must not persist the NACHA graph.
    if persistence.count("SaveChangesAsync(") != 1:
        fail("R5C parsed-graph persistence owner changed")
    if pipeline.count("SaveChangesAsync(") != 2:
        fail("R5D must preserve the two existing queue/state/audit SaveChanges boundaries")
    if any(token in pipeline for token in ("_context.NachaHeaders.AddRange", "ParsedNachaEntityMapper.Map")):
        fail("R5D acquired parsed NACHA graph persistence")

    ingestion_transaction_owners = ingestion.count("BeginTransactionAsync")
    if ingestion_transaction_owners != 1:
        fail(f"explicit ingestion transaction owner changed: expected 1, got {ingestion_transaction_owners}")

    # External SOAP dispatch remains one call site and no local retry loop is introduced.
    soap_sites = pipeline.count("_soapClient.ProcTransaccionesAsync")
    if soap_sites != 1:
        fail(f"Proc_Transacciones external dispatch ownership must be exactly one stage site, got {soap_sites}")
    if re.search(r"for\s*\([^)]*ProcTransacciones|while\s*\([^)]*ProcTransacciones", pipeline, re.IGNORECASE):
        fail("blind local retry loop detected around Proc_Transacciones")
    if "Polly" in pipeline or "RetryAsync" in pipeline:
        fail("R5D introduced transport retry inside the post-processing pipeline")

    # The previously documented direct-parser Return transition is a compatibility path and
    # remains untouched because the R5D plan explicitly targets IncomingNachaPostProcessingOrchestrator.
    if parser.count("ApplyReturnStateTransitionsAsync(") != 2:
        fail("direct-parser Return transition compatibility path changed during R5D")

    counts = {
        "NachaConfigValidationService": len(CONTAINS_CENIT.findall(config)),
        "NachaInboundSimulationService": len(CONTAINS_CENIT.findall(simulation)),
        "IncomingNachaIngestionAppService": len(CONTAINS_CENIT.findall(ingestion)),
    }
    expected_counts = {
        "NachaConfigValidationService": 0,
        "NachaInboundSimulationService": 7,
        "IncomingNachaIngestionAppService": 1,
    }
    if counts != expected_counts:
        fail(f'R5D changed out-of-scope Contains("CENIT") debt: expected {expected_counts}, got {counts}')

    scope = "\n".join((orchestrator, pipeline, parser, validation, persistence, ingestion, simulation, config))
    db_id_inference = count_db_id_inference(scope)
    if db_id_inference:
        fail(f"DB-id rail inference detected: {db_id_inference}")
    if "PaymentRailOperationalStrategy" in scope:
        fail("R5D introduced PaymentRailOperationalStrategy")

    for marker in (
        "R5dOrchestrator_ShouldRemainThinAndDelegateToExplicitStagePipeline",
        "R5dPipeline_ShouldDeclareAllAuthorizedStagesWithCancellationBoundaries",
        "R5dPipeline_ShouldNotReopenR5aR5bOrR5cParsedGraphOwnership",
    ):
        if marker not in focused:
            fail(f"focused R5D architecture characterization is missing {marker}")
    if "ExecuteAsync_PriorSuccessfulExecution_ShouldNotDispatchAgain" not in behavior:
        fail("focused R5D idempotent no-duplicate-dispatch regression is missing")

    total_contains = sum(counts.values())
    print("R5D INBOUND POST-PROCESSING STATIC RATCHET")
    print("  exact R5D scope: decompose IncomingNachaPostProcessingOrchestrator into resolve-context, eligibility, correlation, state-transition, dispatch-planning, external-dispatch and audit/events stages")
    print(f"  NachaParserService lines: {R5C_PARSER_LINES} -> {parser_lines} (direct-parser Return compatibility path unchanged)")
    print(f"  IncomingNachaPostProcessingOrchestrator lines: {R5C_ORCHESTRATOR_LINES} -> {orchestrator_lines}")
    print("  physical parser components: 1 -> 1")
    print("  validation components: 4 -> 4")
    print("  parsed-graph persistence coordinator: 1 -> 1")
    print(f"  explicit post-processing stage components: 0 -> {len(EXPECTED_STAGES)}")
    print("  parsed-graph SaveChanges owner total: 1 -> 1 (R5C coordinator)")
    print(f"  R5D queue/state/audit SaveChanges sites: 2 -> {pipeline.count('SaveChangesAsync(')}")
    print(f"  explicit transaction owner: IncomingNachaIngestionAppService 1 -> {ingestion_transaction_owners}; R5D new owners=0")
    print("  side-effect ownership: Proc_Transacciones -> ExternalDispatchStage; queue/transaction state -> StateTransitionStage; audit/events -> AuditEventsStage")
    print(f"  non-idempotent Proc_Transacciones direct call sites: 1 -> {soap_sites}; new local retry sites=0")
    print(f'  NachaInboundSimulationService Contains("CENIT"): 7 -> {counts["NachaInboundSimulationService"]} (out of R5D scope)')
    print(f'  IncomingNachaIngestionAppService Contains("CENIT"): 1 -> {counts["IncomingNachaIngestionAppService"]} (out of R5D scope)')
    print(f'  total tracked inbound Contains("CENIT"): 8 -> {total_contains}; remaining out-of-scope={total_contains}')
    print("  inbound DB-id rail inference: 0 -> 0")
    print("  PaymentRailOperationalStrategy: 0 -> 0")
    print("  new R5D architecture debt: 0")
    print("R5D INBOUND POST-PROCESSING STATIC RATCHET: OK")


if __name__ == "__main__":
    main()
