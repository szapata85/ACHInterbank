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
CONFIG_VALIDATION = IMPL / "NachaConfigValidationService.cs"
SIMULATION = IMPL / "NachaInboundSimulationService.cs"
INGESTION = IMPL / "IncomingNachaIngestionAppService.cs"
FOCUSED_TEST = ROOT / "tests/Cfa.ACHInterbank.Tests/NachaInboundValidationSeparationTests.cs"

R5A_PARSER_LINES = 2180
R5A_PHYSICAL_SHA256 = "028c0bfc1c5836a28356cf2ba40598bd986b8a47d3209388a103f9bd6e12c1c2"
R5A_VALIDATION_METHOD_DEFS = 20
R5A_CONTAINS = {
    "NachaConfigValidationService": 2,
    "NachaInboundSimulationService": 7,
    "IncomingNachaIngestionAppService": 1,
}

CONTAINS_CENIT = re.compile(r'\.Contains\s*\(\s*"CENIT"', re.IGNORECASE)
VALIDATION_METHOD_DEF = re.compile(
    r'^\s*(?:private|internal)\s+(?:static\s+)?(?:async\s+)?[^\n(]+\s+'
    r'(Validate[A-Za-z0-9_]*|ResolvePublishedInboundAuthority|EnforceAddendaRequirements|'
    r'GetConfiguredTransactionCodesAsync|ValidateType6CheckDigitAsync|ValidateType6ReceiverName|'
    r'IsPseCcdBatch|ShouldValidateCreditIdentity|HasReturnReason|IsAddendaForEntry)\s*\(',
    re.MULTILINE,
)
DB_ID_RAIL_PATTERNS = (
    re.compile(r"ClearingHouseId\s*==\s*\d+"),
    re.compile(r"ClearingHouseId\s*!=\s*\d+"),
    re.compile(r"switch\s*\([^)]*ClearingHouseId", re.IGNORECASE),
    re.compile(r"ClearingHouseId\s+is\s+\d+", re.IGNORECASE),
)
PERSISTENCE_SIDE_EFFECT_TOKENS = (
    "SaveChanges",
    "AddRange(",
    "_context.Add(",
    "_context.Update(",
    "_context.Remove(",
    "ExecuteUpdate",
    "ExecuteDelete",
    "Database.BeginTransaction",
)
FORBIDDEN_VALIDATOR_IO = (
    "HttpClient",
    "HttpRequestMessage",
    "Sftp",
    "MFT",
    "File.Read",
    "File.Write",
    "Directory.",
    "IServiceProvider",
)


def fail(message: str) -> None:
    print(f"R5B INBOUND VALIDATION STATIC RATCHET: FAIL - {message}")
    sys.exit(1)


def read(path: Path) -> str:
    if not path.exists():
        fail(f"missing required file: {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8-sig")


def count_db_id_inference(text: str) -> int:
    return sum(len(pattern.findall(text)) for pattern in DB_ID_RAIL_PATTERNS)


def class_slice(text: str, class_name: str, next_class_name: str) -> str:
    start = text.find(f"internal sealed class {class_name}")
    if start < 0:
        fail(f"missing validation component {class_name}")
    end = text.find(f"internal sealed class {next_class_name}", start)
    if end < 0:
        fail(f"cannot isolate validation component {class_name}")
    return text[start:end]


def main() -> None:
    parser = read(PARSER)
    physical = read(PHYSICAL)
    validation = read(VALIDATION)
    config = read(CONFIG_VALIDATION)
    simulation = read(SIMULATION)
    ingestion = read(INGESTION)
    focused = read(FOCUSED_TEST)

    parser_lines = len(parser.splitlines())
    if parser_lines >= R5A_PARSER_LINES:
        fail(f"NachaParserService did not shrink: {R5A_PARSER_LINES} -> {parser_lines}")

    validation_defs = VALIDATION_METHOD_DEF.findall(parser)
    if len(validation_defs) > 1 or validation_defs not in ([], ["ValidateCenitCtxAddendaCardinality"]):
        fail(f"validation method ownership still remains in NachaParserService: {validation_defs}")
    if "NachaInboundCenitValidationRules.ValidateCtxAddendaCardinality(count)" not in parser:
        fail("legacy cardinality compatibility method is not a thin delegation")

    if hashlib.sha256(PHYSICAL.read_bytes()).hexdigest() != R5A_PHYSICAL_SHA256:
        fail("R5A NachaPhysicalParser changed during R5B")
    if "internal static class NachaPhysicalParser" not in physical or "AchDbContext" in physical:
        fail("R5A DB-free physical-parser boundary regressed")

    if "internal sealed class NachaInboundValidator" not in validation:
        fail("pure/inter-record validation component is missing")
    if "internal static class NachaInboundCenitValidationRules" not in validation:
        fail("CENIT-specific validation component is missing")
    if "internal static class NachaInboundAchColombiaValidationRules" not in validation:
        fail("ACH Colombia-specific validation component is missing")
    if "internal sealed class NachaInboundDatabaseValidator" not in validation:
        fail("explicit read-only DB validation boundary is missing")
    pure = class_slice(validation, "NachaInboundValidator", "NachaInboundDatabaseValidator")
    if "AchDbContext" in pure or "_context" in pure or "EntityFrameworkCore" in pure:
        fail("DB-free validation component gained direct persistence dependency")

    side_effects = [token for token in PERSISTENCE_SIDE_EFFECT_TOKENS if token in validation]
    if side_effects:
        fail(f"validator contains persistence side effects: {side_effects}")
    forbidden_io = [token for token in FORBIDDEN_VALIDATOR_IO if token in validation]
    if forbidden_io:
        fail(f"validator contains MFT/HTTP/filesystem/service-provider dependencies: {forbidden_io}")

    if "AsNoTracking()" not in validation:
        fail("DB-backed validation boundary no longer expresses read-only EF access")
    if "NachaInboundDatabaseValidator" not in parser or "NachaInboundValidator" not in parser:
        fail("NachaParserService is not delegating inbound validation")

    counts = {
        "NachaConfigValidationService": len(CONTAINS_CENIT.findall(config)),
        "NachaInboundSimulationService": len(CONTAINS_CENIT.findall(simulation)),
        "IncomingNachaIngestionAppService": len(CONTAINS_CENIT.findall(ingestion)),
    }
    if counts["NachaConfigValidationService"] != 0:
        fail(f'NachaConfigValidationService Contains("CENIT") must be 2 -> 0, got {counts["NachaConfigValidationService"]}')
    if counts["NachaInboundSimulationService"] != R5A_CONTAINS["NachaInboundSimulationService"]:
        fail("R5B changed out-of-scope inbound simulation substring debt")
    if counts["IncomingNachaIngestionAppService"] != R5A_CONTAINS["IncomingNachaIngestionAppService"]:
        fail("R5B changed out-of-scope ingestion substring debt")
    if 'string.Equals(canonicalCode, "CENIT"' not in config:
        fail("config validation is not using canonical exact clearing-house code equality")
    if 'clearingHouseName.Contains("CENIT"' in config:
        fail("display-name substring inference remains in config validation")

    inbound_scope = parser + "\n" + validation + "\n" + config + "\n" + simulation + "\n" + ingestion
    db_id_inference = count_db_id_inference(inbound_scope)
    if db_id_inference:
        fail(f"DB-id rail inference detected: {db_id_inference}")
    if "PaymentRailOperationalStrategy" in inbound_scope:
        fail("R5B introduced PaymentRailOperationalStrategy")

    for marker in (
        "SequenceValidation_ShouldPreserveInFileDuplicateFailureSemantics",
        "CenitAddendaValidation_ShouldPreserveOwnerAssociationFailure",
        "CenitBatchValidation_ShouldKeepPpdCcdSeparateFromCtx",
        "ReturnValidation_ShouldPreserveType99ReasonAndOriginalTraceRequirements",
    ):
        if marker not in focused:
            fail(f"focused R5B characterization is missing {marker}")

    total = sum(counts.values())
    print("R5B INBOUND VALIDATION STATIC RATCHET")
    print("  exact R5B scope: inbound structural/inter-record + selected PUBLISH constraints + regulatory/semantic validation; no persistence/post-processing ownership move")
    print(f"  NachaParserService lines: {R5A_PARSER_LINES} -> {parser_lines}")
    print(f"  validation method definitions in NachaParserService: {R5A_VALIDATION_METHOD_DEFS} -> {len(validation_defs)} (compatibility delegation only)")
    print("  physical parser components: 1 -> 1; R5A source hash unchanged")
    print("  validation components: 0 -> 4 (generic/PUBLISH + CENIT + ACH Colombia + explicit read-only DB validator)")
    print("  DB-free validation components: 0 -> 3 (generic/PUBLISH + CENIT + ACH Colombia)")
    print("  persistence side effects inside validators: 0")
    print("  MFT/HTTP/filesystem/service-provider dependencies inside validators: 0")
    print(f'  NachaConfigValidationService Contains("CENIT"): 2 -> {counts["NachaConfigValidationService"]}')
    print(f'  NachaInboundSimulationService Contains("CENIT"): 7 -> {counts["NachaInboundSimulationService"]} (out of R5B scope)')
    print(f'  IncomingNachaIngestionAppService Contains("CENIT"): 1 -> {counts["IncomingNachaIngestionAppService"]} (out of R5B scope)')
    print(f'  total tracked inbound Contains("CENIT"): 10 -> {total}; remaining out-of-scope={total}')
    print("  inbound DB-id rail inference: 0 -> 0")
    print("  PaymentRailOperationalStrategy: 0 -> 0")
    print("  new R5B architecture debt: 0")
    print("R5B INBOUND VALIDATION STATIC RATCHET: OK")


if __name__ == "__main__":
    main()
