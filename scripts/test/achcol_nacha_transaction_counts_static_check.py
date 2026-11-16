#!/usr/bin/env python3
"""Fuente-only assertions for ACHCOL T6 physical/accepted/rejected counters.
Not a .NET compilation, runtime test, or substitute for xUnit/DB verification.
"""
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[2]
PARSER = (ROOT / 'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaParserService.cs').read_text()
INGEST = (ROOT / 'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaIngestionAppService.cs').read_text()
MODEL = (ROOT / 'src/Cfa.ACHInterbank.Application/ACH/Models/IncomingNachaIngestionDtos.cs').read_text()
PHYSICAL_TESTS = (ROOT / 'tests/Cfa.ACHInterbank.Tests/NachaFunctionalValidationTests.cs').read_text()
APP_TESTS = (ROOT / 'tests/Cfa.ACHInterbank.Tests/IncomingNachaIngestionAppServiceTests.cs').read_text()
BOUNDARY_TESTS = (ROOT / 'tests/Cfa.ACHInterbank.Tests/AchTransactionNachaTests.cs').read_text()
CENIT_TESTS = (ROOT / 'tests/Cfa.ACHInterbank.Tests/CenitOrdinaryInbound2026ParserTests.cs').read_text()

checks = [
    ('T6 read before validation', PARSER.index('totalEntries++;') < PARSER.index('await databaseValidator.ValidateEntrySequencePolicyAsync(')),
    ('single T6 count increment', PARSER.count('totalEntries++;') == 1),
    ('no T6 count on addenda', 'case \'7\':' in PARSER and PARSER.index('totalEntries++;') < PARSER.index("case '7':")),
    ('counts accepted after addenda', 'validEntriesCount = validEntries.Count;' in PARSER),
    ('final physical total', 'TotalEntries = totalEntries,' in PARSER),
    ('final valid distinct', 'ValidEntries = validEntriesCount,' in PARSER),
    ('final rejected distinct', 'InvalidEntries = totalEntries - validEntriesCount,' in PARSER),
    ('failure count distinct', 'ErrorCount = failures.Count,' in PARSER),
    ('parser contract valid', 'public int ValidEntries { get; init; }' in MODEL),
    ('parser contract invalid', 'public int InvalidEntries { get; init; }' in MODEL),
    ('ingestion physical total', 'processingResult.TotalEntries = parseResult.TotalEntries;' in INGEST),
    ('ingestion valid direct', 'processingResult.ValidCount = parseResult.ValidEntries;' in INGEST),
    ('ingestion invalid direct', 'processingResult.InvalidCount = parseResult.InvalidEntries;' in INGEST),
    ('no error count double subtraction', 'parseResult.TotalEntries - parseResult.ErrorCount' not in INGEST),
    ('physical mixed batch test', 'ParseMixedPhysicalBatches_D01DoesNotSubtractFromAcceptedT6' in PHYSICAL_TESTS),
    ('mixed batch builder T6 twice', PHYSICAL_TESTS.split('private static string BuildIncomingMixedDateBatchesFile()', 1)[1].split('private static string BuildReturnFile()', 1)[0].count('BuildType6(') == 2),
    ('physical mixed batch accepted one', re.search(r'ParseMixedPhysicalBatches_D01DoesNotSubtractFromAcceptedT6.*?result.ValidEntries.Should\(\).Be\(1\)', PHYSICAL_TESTS, re.S) is not None),
    ('physical mixed batch rejected one', re.search(r'ParseMixedPhysicalBatches_D01DoesNotSubtractFromAcceptedT6.*?result.InvalidEntries.Should\(\).Be\(1\)', PHYSICAL_TESTS, re.S) is not None),
    ('late addenda review test', 'ParseEntryRejectedAfterAddendaReview_KeepsPhysicalAndFinalCountsSeparate' in PHYSICAL_TESTS),
    ('early rejection count updated', 'result.TotalEntries.Should().Be(1); // One physical T6 was read and rejected.' in PHYSICAL_TESTS),
    ('ingestion persistence test', 'IngestAsync_MixedValidAndRejectedEntries_UsesParserDispositionNotFailureCount' in APP_TESTS),
    ('multiple failure messages case', '[InlineData(2)]' in APP_TESTS),
    ('ingestion conservation assertion', 'Assert.Equal(processing.TotalEntries, processing.ValidCount + processing.InvalidCount);' in APP_TESTS),
    ('ingestion cardinality invariant', 'NACHA_ENTRY_COUNTS_INCONSISTENT' in INGEST),
    ('inconsistent result fails closed regression', 'IngestAsync_InconsistentParserEntryCounts_FailsClosedWithoutPostProcessing' in APP_TESTS),
    ('file-based physical D02 test corrected', 'Assert.Equal(0, result.ValidEntries);' in BOUNDARY_TESTS and 'Assert.Equal(1, result.InvalidEntries);' in BOUNDARY_TESTS),
    ('CENIT rejected PPD T6 count contract', 'result.TotalEntries.Should().Be(1); // Rejected but physically present.' in CENIT_TESTS),
    ('CENIT invalid entries final distinct', CENIT_TESTS.count('result.InvalidEntries.Should().Be(1);') == 2),
]
for number, (name, ok) in enumerate(checks, 1):
    print(f'{number:02d} {"PASS" if ok else "FAIL"} SOURCE_STATIC {name}')
if not all(ok for _, ok in checks):
    raise SystemExit(1)
print(f'TOTAL {len(checks)}/{len(checks)} SOURCE STATIC; .NET BUILD/XUNIT/DB NOT EXECUTED')
