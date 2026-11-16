#!/usr/bin/env python3
"""I02 structural and source-contract checks. This is NOT compiled/executed C#."""
from pathlib import Path
root = Path(__file__).resolve().parents[2]
src = root / 'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation'
files = {name: (src / name).read_text(encoding='utf-8-sig') for name in (
    'CenitParentReturnEvidenceResolver.cs',
    'CenitReturnOfReturnService.cs',
    'AchReturnOfReturnFileGenerationService.cs',
    'IncomingNachaPostParseProcessor.cs')}
t = root / 'tests/Cfa.ACHInterbank.Tests'
service_tests = (t/'CenitReturnOfReturn2026Tests.cs').read_text(encoding='utf-8-sig')
gen_tests = (t/'AchReturnOfReturnFileGenerationServiceTests.cs').read_text(encoding='utf-8-sig')
r, s, g, p = (files[n] for n in files)
checks = {
    'Resolver exists in CENIT implementation': 'internal static class CenitParentReturnEvidenceResolver' in r,
    'Link GUID embedded in Return In state evidence': 'cenitEvidence[CenitParentReturnEvidenceResolver.LinkIdProperty] = link.Id.ToString("D")' in p and 'if (cenitPolicy is not null)' in p,
    'Pinned payload precedes state transition': p.index('stateEvidence = cenitEvidence.ToJsonString()') < p.index('classification.OriginalTraceRef,',p.index('stateEvidence = cenitEvidence.ToJsonString()')),
    'Resolver parses GUID from immutable event payload': 'ReadPinnedLinkId(parentEvent.PayloadJson)' in r and 'Guid.TryParse' in r,
    'Malformed pinned ID fails closed': 'CENIT_ROR_SOURCE_RETURN_LINK_INVALID' in r,
    'Foreign/missing pinned link fails closed': 'CENIT_ROR_SOURCE_RETURN_LINK_MISMATCH' in r,
    'Legacy ambiguity fails closed': 'candidates.Count > 1' in r and 'CENIT_ROR_SOURCE_RETURN_AMBIGUOUS' in r,
    'Only final NACHA links eligible': 'x.IsFinal' in r,
    'Raw addenda type 99 required': 'CodeTypeAddendumRecord?.Trim(), "99"' in r,
    'Original trace must match parent payment': 'OriginalTraceNumber?.Trim(), original.TraceNumber?.Trim()' in r,
    'Raw amount must match original': 'x.EntryDetail.Amount == original.Amount' in r,
    'Link FK to entry/addenda checked': 'x.AddendaRecord.EntryDetailId == x.EntryDetail.EntryDetailID' in r,
    'Creation reuses shared resolution': 'CenitParentReturnEvidenceResolver.ResolveAsync(context, parentEvent, original, ct)' in s,
    'Creation does not choose last linked raw': 'OrderByDescending(x => x.LinkedAtUtc)' not in s,
    'Creation checks persisted Return original trace if present': 'sourceReturns[0].OriginalTraceRef' in s,
    'C1 exporter prevalidates with shared resolution': 'CenitParentReturnEvidenceResolver.ResolveAsync(\n                context, parentEvent, flow.OriginalTransaction, ct)' in g,
    'Physical builder resolves with same authoritative link': g.count('CenitParentReturnEvidenceResolver.ResolveAsync(') == 2,
    'No latest-link substitution in CENIT builder': 'OrderByDescending(x => x.LinkedAtUtc)' not in g,
    'Build checks ROR amount': 'CENIT_ROR_AMOUNT_CORRELATION_INVALID' in g,
    'Legacy null SourceReturnTransaction supported': 'flow.SourceReturnTransaction is not null' in g,
    'Idempotency unique flow by parent retained': 'ParentIncomingReturnStateEventId == parentEvent.Id' in s,
    'C1 positive correlation and replay test exists': 'I02_Cycle1_PinnedParentChoosesExactReturnAmongTwoMatchingReasons' in service_tests and 'duplicate.WasDuplicate.Should().BeTrue()' in service_tests,
    'C1 ambiguous and foreign-parent negatives exist': 'I02_Cycle1_LegacyParentWithTwoRawMatchesFailsClosed' in service_tests and 'I02_Cycle1_PinnedParentBelongingToOtherOriginalFailsClosed' in service_tests,
    'Generator both legacy ambiguous and pinned positive tests exist': 'I02_Cycle1_FileValidationRejectsAmbiguousLegacyButAcceptsPinnedParent' in gen_tests,
    'Generator changed link negative exists': 'I02_Cycle1_FileValidationRejectsParentLinkChangedAfterRegistration' in gen_tests,
    'No schema migration for this increment': not list((root/'src/Cfa.ACHInterbank.Persistence/DataBase/Migrations').glob('*I02*')),
}
for i,(name,pass_) in enumerate(checks.items(),1):
    print(f'{i:02} {"PASS" if pass_ else "FAIL"} {name}')
print(f'I02 SOURCE-CHECK: {sum(checks.values())}/{len(checks)} PASS. No C# compilation, DB or official operator verification.')
raise SystemExit(0 if all(checks.values()) else 1)
