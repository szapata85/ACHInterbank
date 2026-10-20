#!/usr/bin/env python3
from __future__ import annotations

import hashlib
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
IMPL = ROOT / "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation"
PARSER = IMPL / "NachaParserService.cs"
PHYSICAL = IMPL / "NachaPhysicalParser.cs"
VALIDATION = IMPL / "NachaInboundValidation.cs"
PERSISTENCE = IMPL / "NachaInboundPersistenceCoordinator.cs"
MAPPER = IMPL / "ParsedNachaEntityMapper.cs"
CONFIG_VALIDATION = IMPL / "NachaConfigValidationService.cs"
SIMULATION = IMPL / "NachaInboundSimulationService.cs"
INGESTION = IMPL / "IncomingNachaIngestionAppService.cs"
FOCUSED_TEST = ROOT / "tests/Cfa.ACHInterbank.Tests/NachaInboundPersistenceSeparationTests.cs"

R5B_PARSER_LINES = 1077
R5A_PHYSICAL_SHA256 = "028c0bfc1c5836a28356cf2ba40598bd986b8a47d3209388a103f9bd6e12c1c2"
R5B_VALIDATION_SHA256 = "8f4493197d72573692999bf057424c4aaa114b5c4547afcdc27067733e839e66"
R5B_DIRECT_PERSISTENCE_DEFS = 2
R5B_CONTAINS = {
    "NachaConfigValidationService": 0,
    "NachaInboundSimulationService": 7,
    "IncomingNachaIngestionAppService": 1,
}

CONTAINS_CENIT = re.compile(r'\.Contains\s*\(\s*"CENIT"', re.IGNORECASE)
DB_ID_RAIL_PATTERNS = (
    re.compile(r"ClearingHouseId\s*==\s*\d+"),
    re.compile(r"ClearingHouseId\s*!=\s*\d+"),
    re.compile(r"switch\s*\([^)]*ClearingHouseId", re.IGNORECASE),
    re.compile(r"ClearingHouseId\s+is\s+\d+", re.IGNORECASE),
)
WRITE_TOKENS = (
    "SaveChangesAsync(",
    "NachaHeaders.AddRange(",
    "_context.Add(",
    "_context.Update(",
    "_context.Remove(",
    "ExecuteUpdate",
    "ExecuteDelete",
)
TRANSACTION_TOKENS = (
    "BeginTransactionAsync",
    "CommitAsync",
    "RollbackAsync",
)
FORBIDDEN_PERSISTENCE_STAGE_TOKENS = (
    "NachaPhysicalParser",
    "NachaInboundValidator",
    "NachaInboundDatabaseValidator",
    "IAchStateTransitionService",
    "IIncomingNachaPostParseProcessor",
    "HttpClient",
    "HttpRequestMessage",
    "Sftp",
    "Directory.",
    "File.Read",
    "File.Write",
)


def fail(message: str) -> None:
    print(f"R5C INBOUND PERSISTENCE STATIC RATCHET: FAIL - {message}")
    sys.exit(1)


def read(path: Path) -> str:
    if not path.exists():
        fail(f"missing required file: {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8-sig")


def count_db_id_inference(text: str) -> int:
    return sum(len(pattern.findall(text)) for pattern in DB_ID_RAIL_PATTERNS)


def count_tokens(text: str, tokens: tuple[str, ...]) -> int:
    return sum(text.count(token) for token in tokens)


def main() -> None:
    parser = read(PARSER)
    physical = read(PHYSICAL)
    validation = read(VALIDATION)
    persistence = read(PERSISTENCE)
    mapper = read(MAPPER)
    config = read(CONFIG_VALIDATION)
    simulation = read(SIMULATION)
    ingestion = read(INGESTION)
    focused = read(FOCUSED_TEST)

    parser_lines = len(parser.splitlines())
    if parser_lines > R5B_PARSER_LINES:
        fail(f"NachaParserService unexpectedly grew: {R5B_PARSER_LINES} -> {parser_lines}")

    if hashlib.sha256(PHYSICAL.read_bytes()).hexdigest() != R5A_PHYSICAL_SHA256:
        fail("R5A NachaPhysicalParser changed during R5C")
    if hashlib.sha256(VALIDATION.read_bytes()).hexdigest() != R5B_VALIDATION_SHA256:
        fail("R5B inbound validation components changed during R5C")

    if "internal sealed class NachaInboundPersistenceCoordinator" not in persistence:
        fail("explicit R5C persistence coordinator is missing")
    if "ParsedNachaEntityMapper.Map(headers)" not in persistence:
        fail("persistence coordinator does not map the validated parsed graph")
    if "_context.NachaHeaders.AddRange(persistenceGraph)" not in persistence:
        fail("persistence coordinator does not own the inbound graph AddRange")
    if "await _context.SaveChangesAsync(ct)" not in persistence:
        fail("persistence coordinator does not own SaveChangesAsync")
    if "await _persistenceCoordinator.PersistAsync(headers, ct)" not in parser:
        fail("NachaParserService is not delegating validated graph persistence")
    if "ParsedNachaEntityMapper.Map(headers)" in parser or "_context.NachaHeaders.AddRange" in parser or "_context.SaveChangesAsync" in parser:
        fail("direct parsed-graph persistence still remains in NachaParserService")

    parser_write_defs = count_tokens(parser, WRITE_TOKENS)
    if parser_write_defs != 0:
        fail(f"direct write side effects remain in NachaParserService: {parser_write_defs}")
    persistence_save_sites = persistence.count("SaveChangesAsync(")
    if persistence_save_sites != 1:
        fail(f"R5C persistence coordinator must own exactly one SaveChangesAsync site, got {persistence_save_sites}")

    transaction_ops = [token for token in TRANSACTION_TOKENS if token in persistence]
    if transaction_ops:
        fail(f"R5C persistence coordinator changed transaction ownership: {transaction_ops}")
    ingestion_transaction_owners = ingestion.count("BeginTransactionAsync")
    if ingestion_transaction_owners != 1:
        fail(f"existing ingestion transaction owner changed: expected 1, got {ingestion_transaction_owners}")

    physical_writes = [token for token in WRITE_TOKENS + TRANSACTION_TOKENS if token in physical]
    if physical_writes:
        fail(f"R5A physical parser gained persistence side effects: {physical_writes}")
    validation_writes = [token for token in WRITE_TOKENS + TRANSACTION_TOKENS if token in validation]
    if validation_writes:
        fail(f"R5B validators gained persistence side effects: {validation_writes}")

    forbidden_stage = [token for token in FORBIDDEN_PERSISTENCE_STAGE_TOKENS if token in persistence]
    if forbidden_stage:
        fail(f"R5C persistence coordinator contains parsing/validation/post-processing/IO dependencies: {forbidden_stage}")
    if "BeginTransaction" in mapper or "SaveChanges" in mapper or "AchDbContext" in mapper:
        fail("pure entity mapper gained transaction/DbContext ownership")

    # Preserve the two existing idempotency owners: source-file fingerprint/reprocess admission
    # in ingestion and NACHA identity/replay guard in the parser. R5C must not invent a third owner.
    parser_idempotency_markers = (
        "_context.NachaHeaders",
        "isAuthorizedReplay",
        "BuildReplayNachaId",
    )
    ingestion_idempotency_markers = (
        "FileHashSha256",
        "candidatesByFingerprint",
        "ForceReprocess",
    )
    if any(marker not in parser for marker in parser_idempotency_markers):
        fail("parser NACHA duplicate/replay ownership changed during R5C")
    if any(marker not in ingestion for marker in ingestion_idempotency_markers):
        fail("ingestion file-fingerprint/reprocess ownership changed during R5C")
    if any(marker in persistence for marker in ("FileHashSha256", "isAuthorizedReplay", "BuildReplayNachaId", "ForceReprocess")):
        fail("persistence coordinator acquired duplicate/reprocess policy ownership")

    if "IncomingNachaFileIngestionId = source.IncomingNachaFileIngestionId" not in mapper:
        fail("exact-file membership mapping changed")
    for lineage_marker in (
        "NachaHeader = header",
        "BatchHeader = batchByNumber.GetValueOrDefault(x.BatchNumber)",
        "EntryDetail = entry",
    ):
        if lineage_marker not in mapper:
            fail(f"lineage mapping marker missing: {lineage_marker}")

    counts = {
        "NachaConfigValidationService": len(CONTAINS_CENIT.findall(config)),
        "NachaInboundSimulationService": len(CONTAINS_CENIT.findall(simulation)),
        "IncomingNachaIngestionAppService": len(CONTAINS_CENIT.findall(ingestion)),
    }
    if counts != R5B_CONTAINS:
        fail(f'R5C changed out-of-scope Contains("CENIT") debt: expected {R5B_CONTAINS}, got {counts}')

    inbound_scope = parser + "\n" + validation + "\n" + persistence + "\n" + config + "\n" + simulation + "\n" + ingestion
    db_id_inference = count_db_id_inference(inbound_scope)
    if db_id_inference:
        fail(f"DB-id rail inference detected: {db_id_inference}")
    if "PaymentRailOperationalStrategy" in inbound_scope:
        fail("R5C introduced PaymentRailOperationalStrategy")

    for marker in (
        "PersistAsync_ShouldPreserveExactFileMembershipLineageAndRorFields",
        "PersistAsync_ShouldParticipateInAmbientTransactionWithoutCommittingIt",
    ):
        if marker not in focused:
            fail(f"focused R5C characterization is missing {marker}")

    total_contains = sum(counts.values())
    print("R5C INBOUND PERSISTENCE STATIC RATCHET")
    print("  exact R5C scope: persist already parsed/validated inbound graph in one explicit write component; preserve ambient transaction, lineage and exact-file membership; no R5D move")
    print(f"  NachaParserService lines: {R5B_PARSER_LINES} -> {parser_lines}")
    print("  physical parser components: 1 -> 1; R5A source hash unchanged")
    print("  validation components: 4 -> 4; R5B validation source hash unchanged")
    print("  write-owning inbound persistence components: 0 -> 1")
    print(f"  direct parsed-graph persistence definitions in NachaParserService: {R5B_DIRECT_PERSISTENCE_DEFS} -> {parser_write_defs}")
    print("  parsed-graph SaveChanges ownership: NachaParserService 1 -> 0; NachaInboundPersistenceCoordinator 0 -> 1; total 1 -> 1")
    print(f"  transaction boundary owners: IncomingNachaIngestionAppService 1 -> {ingestion_transaction_owners}; persistence coordinator begins/commits/rolls back 0")
    print("  duplicate/idempotency policy owners: 2 -> 2 (ingestion fingerprint/reprocess + parser NACHA identity/replay); persistence coordinator 0")
    print("  write side effects in physical parser: 0")
    print("  write side effects in R5B validators: 0")
    print(f'  NachaConfigValidationService Contains("CENIT"): 0 -> {counts["NachaConfigValidationService"]}')
    print(f'  NachaInboundSimulationService Contains("CENIT"): 7 -> {counts["NachaInboundSimulationService"]} (out of R5C scope)')
    print(f'  IncomingNachaIngestionAppService Contains("CENIT"): 1 -> {counts["IncomingNachaIngestionAppService"]} (out of R5C scope)')
    print(f'  total tracked inbound Contains("CENIT"): 8 -> {total_contains}; remaining out-of-scope={total_contains}')
    print("  inbound DB-id rail inference: 0 -> 0")
    print("  PaymentRailOperationalStrategy: 0 -> 0")
    print("  new R5C architecture debt: 0")
    print("R5C INBOUND PERSISTENCE STATIC RATCHET: OK")


if __name__ == "__main__":
    main()
