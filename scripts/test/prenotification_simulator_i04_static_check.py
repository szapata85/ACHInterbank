#!/usr/bin/env python3
"""I04 static-only source invariants: NOT a C# build or banking E2E test."""
from pathlib import Path
import json
ROOT = Path(__file__).resolve().parents[2]
processor = (ROOT/'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaPostParseProcessor.cs').read_text(encoding='utf-8-sig')
guard = (ROOT/'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingPrenotificationReturnGuard.cs').read_text()
tests = (ROOT/'tests/Cfa.ACHInterbank.Tests/IncomingPrenotificationReturnGuardI04Tests.cs').read_text()
integration = (ROOT/'tests/Cfa.ACHInterbank.Tests/IncomingNachaPostParseProcessorTests.cs').read_text(encoding='utf-8-sig')
state = (ROOT/'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchStateTransitionService.cs').read_text()
ingestion = (ROOT/'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaIngestionAppService.cs').read_text(encoding='utf-8-sig')
checks={
 'guard-is-pure-no-sql': 'DbContext' not in guard and 'SaveChanges' not in guard,
 'original-trace-exact': 'PRENOTE_ORIGINAL_TRACE_MISMATCH' in guard,
 'addenda-type-99': '"99"' in guard,
 'zero-value': 'PRENOTE_RETURN_NONZERO_AMOUNT' in guard,
 'new-trace': 'PRENOTE_RETURN_TRACE_INVALID' in guard,
 'same-clearing-house': 'PRENOTE_CLEARING_HOUSE_MISMATCH' in guard,
 'final-exact-link': 'PRENOTE_LINK_NOT_EXACT' in guard,
 'current-attempt': 'PRENOTE_ATTEMPT_CONTEXT_MISMATCH' in guard,
 'already-resolved': 'PRENOTE_ALREADY_RESOLVED' in guard,
 'same-return-replay': 'isExactReplay' in guard,
 'simulation-hash-ambiguous': 'SIMULATION_FILE_HASH_AMBIGUOUS' in guard,
 'simulation-bank-match': 'OriginFinancialInstitutionId != original.DestinationInstitutionId' in guard,
 'simulation-source-match': 'DestinationFinancialInstitutionId != original.SourceInstitutionId' in guard,
 'simulation-cause-match': 'matchedSimulation.ReasonCode' in guard,
 'simulation-membership': 'matchedSimulation.Entries.Count' in guard,
 'simulation-attempt-required': 'SIMULATION_PRENOTE_ATTEMPT_REQUIRED' in guard,
 'non-prenote-unmodified': 'Result.NotApplicable' in guard,
 'pre-transition': processor.index('EvaluatePrenotificationReturnAsync(')<processor.index('var route = await ResolveReturnRouteAsync('),
 'blocked-manual-review': 'PrenotificacionReturnBloqueada' in processor and 'IncomingNachaEligibilityStatus.RevisionManual' in processor,
 'audit-correlated': 'PrenotificacionReturnCorrelacionada' in processor,
 'sha-upper-normalized': 'FileHashSha256?.Trim().ToUpperInvariant()' in processor,
 'sha-lower-ingested': 'b.ToString("x2")' in ingestion,
 'no-filename-trust': '.Where(x => x.Sha256 == simulationSha' in processor,
 'prenote-type-flag': 'transaction.IsPrenotification || transaction.Type == TransactionTypeEnum.Prenotification' in processor,
 'cenit-prenote-type': 'transaction.IsPrenotification ? TransactionTypeEnum.Prenotification : transaction.Type' in processor,
 'existing-state-sync': 'SynchronizeCanonicalPrenotificationValidationAsync(' in state and 'SynchronizePrenotificationThirdPartyAsync(' in state,
 'no-parallel-state-writer': 'attempt.Status =' not in processor and 'validation.Status =' not in processor,
 'original-tests-two-chambers': '[InlineData(1)]' in tests and '[InlineData(2)]' in tests,
 'duplicate-tests': 'ReplaySameRejectedReturn' in tests and 'ConflictingRejectedReturn' in tests,
 'integration-block-test': 'I04_PrenoteReturnWithWrongOriginalTrace_DoesNotApplyAnyState' in integration,
 'integration-good-trace': 'I04_ValidPrenoteLink_PassesCorrelationGuardBeforeCatalogResolution' in integration,
}
for k,good in checks.items():
    print(('PASS_STATIC' if good else 'FAIL_STATIC'),k)
print(json.dumps({'checks_total':len(checks),'passed':sum(checks.values()),'failed':[k for k,v in checks.items() if not v],'classification':'STATIC_ONLY_NOT_DOTNET_BUILD_OR_NACHAUPLOAD_E2E'},ensure_ascii=False))
if not all(checks.values()): raise SystemExit(1)
