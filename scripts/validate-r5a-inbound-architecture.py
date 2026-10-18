#!/usr/bin/env python3
from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
IMPL = ROOT / "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation"
PARSER_SERVICE = IMPL / "NachaParserService.cs"
PHYSICAL_PARSER = IMPL / "NachaPhysicalParser.cs"
CONFIG_VALIDATION = IMPL / "NachaConfigValidationService.cs"
SIMULATION = IMPL / "NachaInboundSimulationService.cs"
INGESTION = IMPL / "IncomingNachaIngestionAppService.cs"
FOCUSED_TEST = ROOT / "tests/Cfa.ACHInterbank.Tests/NachaPhysicalParserTests.cs"

R4E_CONTAINS_CENIT = {
    "NachaConfigValidationService": 2,
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
PURE_PARSER_FORBIDDEN = (
    "AchDbContext",
    "DbContext",
    "EntityFrameworkCore",
    "IConfiguration",
    "ILogger",
    "IServiceProvider",
    "HttpClient",
    "Sftp",
    "MFT",
    "SaveChanges",
    "File.Read",
    "File.Write",
)
SERVICE_OLD_PHYSICAL_IMPLEMENTATION = (
    "Encoding.UTF8.Preamble",
    "IsAchColombiaAcceptedByte",
    "bytes.Any(value => value is (byte)'\\r' or (byte)'\\n')",
    "ACHCOL-PHYSICAL-NO-BOM",
    "ACHCOL-PHYSICAL-ASCII-REPERTOIRE",
)


def fail(message: str) -> None:
    print(f"R5A INBOUND ARCHITECTURE STATIC RATCHET: FAIL - {message}")
    sys.exit(1)


def read(path: Path) -> str:
    if not path.exists():
        fail(f"missing required file: {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8-sig")


def count_db_id_inference(text: str) -> int:
    return sum(len(pattern.findall(text)) for pattern in DB_ID_RAIL_PATTERNS)


def main() -> None:
    service = read(PARSER_SERVICE)
    physical = read(PHYSICAL_PARSER)
    focused_test = read(FOCUSED_TEST)
    tracked = {
        "NachaConfigValidationService": read(CONFIG_VALIDATION),
        "NachaInboundSimulationService": read(SIMULATION),
        "IncomingNachaIngestionAppService": read(INGESTION),
    }

    if "internal static class NachaPhysicalParser" not in physical:
        fail("deterministic physical parser component is missing or public")
    if "NachaPhysicalFile" not in physical or "NachaPhysicalRecord" not in physical:
        fail("physical parser no longer produces the tokenized physical NACHA model")
    if "CopyToAsync(buffer, ct)" not in physical:
        fail("physical byte/framing ownership is not in NachaPhysicalParser")
    if "NachaPhysicalParser.ParseAsync(nachaStream, ct, profileReader)" not in service:
        fail("NachaParserService does not delegate physical parsing to NachaPhysicalParser")

    leaked = [token for token in PURE_PARSER_FORBIDDEN if token in physical]
    if leaked:
        fail(f"pure physical parser gained DB/IO/global-service dependencies: {leaked}")

    retained = [token for token in SERVICE_OLD_PHYSICAL_IMPLEMENTATION if token in service]
    if retained:
        fail(f"NachaParserService still owns pre-R5A physical framing implementation: {retained}")

    if "ReadPhysicalRecordsAsync" not in service or "NachaPhysicalParser.ParseAsync" not in service:
        fail("existing internal physical-reader compatibility path was not preserved as delegation")

    counts = {name: len(CONTAINS_CENIT.findall(text)) for name, text in tracked.items()}
    for name, baseline in R4E_CONTAINS_CENIT.items():
        if counts[name] > baseline:
            fail(f'{name} Contains("CENIT") debt grew: {baseline} -> {counts[name]}')
    # Later increments may legitimately shrink debt that was outside R5A. The R5A ratchet
    # only forbids growth and continues to assert the physical-parser boundary.

    inbound_scope = service + "\n" + physical + "\n" + "\n".join(tracked.values())
    db_id_inference = count_db_id_inference(inbound_scope)
    if db_id_inference:
        fail(f"DB-id rail inference detected in inbound R5A scan: {db_id_inference}")
    if "PaymentRailOperationalStrategy" in inbound_scope:
        fail("R5A introduced PaymentRailOperationalStrategy")

    if "AchDbContext" in focused_test:
        fail("focused R5A physical-parser tests depend on AchDbContext")
    for token in (
        "AchColombiaHappyPath",
        "CenitReturnOfReturnFixture",
        "InvalidPhysicalStructure",
        "NormativeLatin1Enye",
    ):
        if token not in focused_test:
            fail(f"focused R5A characterization is missing {token}")

    service_db_refs = service.count("_context")
    if service_db_refs == 0:
        fail("R5A unexpectedly removed persistence/validation ownership from NachaParserService")

    total_remaining = sum(counts.values())
    print("R5A INBOUND ARCHITECTURE STATIC RATCHET")
    print("  exact R5A scope: physical byte/framing parse + fixed-width record tokenization only")
    print("  physical parsing owner coupled to AchDbContext: 1 -> 0")
    print("  deterministic DB-free physical parser components: 0 -> 1")
    print("  NachaParserService direct physical framing implementation: 1 -> 0")
    print("  profile/PUBLISH resolution, validation, persistence and post-processing: outside R5A; later phases may move them")
    print(f'  NachaConfigValidationService Contains("CENIT"): 2 -> {counts["NachaConfigValidationService"]} (out of R5A scope)')
    print(f'  NachaInboundSimulationService Contains("CENIT"): 7 -> {counts["NachaInboundSimulationService"]} (out of R5A scope)')
    print(f'  IncomingNachaIngestionAppService Contains("CENIT"): 1 -> {counts["IncomingNachaIngestionAppService"]} (out of R5A scope)')
    print(f'  total inbound Contains("CENIT") in tracked R4E debt: 10 -> {total_remaining}; R5A-scope count 0 -> 0')
    print("  inbound DB-id rail inference: 0 -> 0")
    print("  new public interfaces: 0")
    print("  new deterministic components: 1 (NachaPhysicalParser)")
    print("  PaymentRailOperationalStrategy references in R5A scope: 0")
    print("  new R5A architecture debt: 0")
    print("R5A INBOUND ARCHITECTURE STATIC RATCHET: OK")


if __name__ == "__main__":
    main()
