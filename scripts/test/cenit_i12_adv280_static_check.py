"""I12/CE-01 source-contract checks. These are NOT executed C# tests."""
from pathlib import Path
R = Path(__file__).resolve().parents[2]
s = (R/'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitChamberResponseService.cs').read_text()
t = (R/'tests/Cfa.ACHInterbank.Tests/CenitChamberResponseLifecycleTests.cs').read_text()
checks = {
    'physical 106-byte stride rather than requiring newlines': 'content.Length % 106 != 0' in s and 'content.Substring(index * 106, 106)' in s,
    'legacy line reader retains fields without Trim': 'content.Split(\'\\n\')' in s and "segment.EndsWith('\\r')" in s,
    'legacy Reconciliation detects packed ADV': 'if (LooksLikeAdv(content))' in s,
    'invalid batch profile cannot downgrade to opaque reconciliation': "content[secondOffset] == '5'" in s,
    'file header record type validated': "records[0][0] != '1'" in s,
    'header record length validated': 'records[0].Substring(36, 3) != "106"' in s,
    'blocking factor 10 validated': 'records[0].Substring(39, 2) != "10"' in s,
    'limits inbound message size': 'content.Length > 1_000_000' in s,
    'no non-CENIT alphabet accepted': 'NachaPhysicalParser.IsCenitAcceptedCharacter' in s,
    'profile code 280 enforced': 'header.Substring(1, 3) != "280"' in s,
    'SEC ADV required': 'header.Substring(50, 3) != "ADV"' in s,
    'valid value date required': 'DateTime.TryParseExact(header.Substring(71, 8)' in s,
    'batch sequence numeric': 'batchId.All(char.IsAsciiDigit)' in s,
    'all 8 ADV codes constrained': all(f'"{i}"' in s for i in range(81, 89)),
    'detail amount digits': 'TryUnsigned(line, 27, 18' in s,
    'detail route digits': 'TryUnsigned(line, 3, 8' in s,
    'adenda flag zero required': "line[84] != '0'" in s,
    'batch has nonempty details': 'details == 0' in s,
    'batch control follows details': "records[index][0] != '8'" in s,
    'batch detail count checked': 'expectedCount != details' in s,
    'batch routing checksum checked': 'expectedRoutes != routes % 10_000_000_000m' in s,
    'batch debit control checked': 'expectedDebits != debits' in s,
    'batch credit control checked': 'expectedCredits != credits' in s,
    'batch sequence matches': 'control.Substring(99, 7) != batchId' in s,
    'file T9 required': "records[index][0] != '9'" in s,
    'file batch count checked': 'expectedBatches != batches' in s,
    'file physical blocks checked': 'expectedBlocks != (decimal)Math.Ceiling(records.Length / 10m)' in s,
    'file detail count checked': 'expectedDetails != totalDetails' in s,
    'file route check': 'expectedRoutesAll != totalRoutes % 10_000_000_000m' in s,
    'file debit sum': 'expectedDebitsAll != totalDebits' in s,
    'file credit sum': 'expectedCreditsAll != totalCredits' in s,
    'file reserved blank': "fileControl.AsSpan(67, 39).IndexOfAnyExcept(' ')" in s,
    'file padding modulo 10': 'records.Length % 10 != 0' in s,
    'all padding must be exact 9s': "new string('9', 106)" in s,
    'only returns after file checks': s.index('return output;',s.index('private static IReadOnlyList<ParsedResponse> ParseAdv')) > s.index('CENIT_ADV_FILE_CONTROL_INVALID',s.index('private static IReadOnlyList<ParsedResponse> ParseAdv')),
    'official event may not mutate transaction on session output': 'if (correlation == CenitChamberCorrelationOutcome.Matched && sessionOutput)' in s,
    'PT23 physical positive fixture test': 'PT23_PhysicalAdvWithoutLineEndings_AllEightCodes' in t,
    'PT23 legacy Reconciliation physical test': 'PT23_PhysicalAdvUnderLegacyReconciliationEnvelope' in t,
    'PT23 malformed physical variants': 'PT23_MalformedPhysicalAdv_FailsClosedAndDoesNotPost' in t,
    'PT23 raw fixture preserves full record widths': 'PhysicalAdv(params string[] codes)' in t,
    'no posting after receipt asserted': 'Assert.Empty(await context.AchTransactionStateEvents.ToListAsync());' in t,
}
for label, ok in checks.items():
    print(('PASS' if ok else 'FAIL') + ' ' + label)
print(f'{sum(checks.values())}/{len(checks)} source-contract checks (not C# execution)')
if not all(checks.values()): raise SystemExit(1)
