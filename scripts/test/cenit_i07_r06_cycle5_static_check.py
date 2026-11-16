#!/usr/bin/env python3
"""I07: conservative source-contract checks (NOT .NET build/xUnit or PT-09)."""
from pathlib import Path
import sys

root = Path(__file__).resolve().parents[2]
app = root / 'src/Cfa.ACHInterbank.Application/ACH'
persist = root / 'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation'
tests = root / 'tests/Cfa.ACHInterbank.Tests'
policy = (app/'Services/CenitIncomingReturnPolicy.cs').read_text(encoding='utf-8-sig')
models = (app/'Models/CenitIncomingReturnPolicyModels.cs').read_text(encoding='utf-8-sig')
out = (persist/'AchReturnEligibilityPolicyComponent.cs').read_text(encoding='utf-8-sig')
inc = (persist/'IncomingNachaPostParseProcessor.cs').read_text(encoding='utf-8-sig')
unit = (tests/'CenitI07R06Cycle5Tests.cs').read_text(encoding='utf-8-sig')
return_service = (persist/'AchReturnsService.cs').read_text(encoding='utf-8-sig')
f04 = (tests/'CenitF04ReturnPolicyTests.cs').read_text(encoding='utf-8-sig')
f07 = (tests/'CenitF07CrossPhaseRegressionTests.cs').read_text(encoding='utf-8-sig')
branch=policy.split('if (code == R06)')[1].split('if (code == R23)')[0]
checks = {
    'C5 only: C4 source remains required': 'request.ReturnCycleNumber == 5' in branch and 'request.OriginalCycleNumber != 4' in branch,
    'C5 still enforces matching value dates': 'request.ReturnValueDate.Date != request.OriginalValueDate.Date' in branch,
    'C5 requires both persisted session dates': '!request.OriginalReceivedDate.HasValue || !request.ReturnOperationalDate.HasValue' in branch,
    'C5 dates compare actual original and return operating days': 'request.OriginalReceivedDate.Value.Date != request.ReturnOperationalDate.Value.Date' in branch,
    'C5 does not infer day from current system clock': 'DateTime.Now' not in branch and 'DateTime.UtcNow' not in branch,
    'C5 no evidence triggers manual review': 'CENIT_CYCLE5_R06_OPERATIONAL_DAY_EVIDENCE_REQUIRED' in branch and 'return Manual(' in branch,
    'C5 wrong operational day triggers rejection': 'return Reject("CENIT_CYCLE5_R06_ORIGIN_C4_REQUIRED"' in branch,
    'C5 check comes before R06 operational rules': policy.index('if (code == R06)') < policy.index('return EvaluateR06(request)') < policy.index('if (code == R23)'),
    'R06 still requires full original amount': 'CENIT_R06_PARTIAL_OR_AMOUNT_MISMATCH' in policy,
    'R06 still requires immediate return session confirmation': 'CENIT_R06_NOT_IMMEDIATE' in policy,
    'R06 still requires originator confirmation': 'CENIT_R06_CONFIRMATION_REQUIRED' in policy,
    'R06 credit still enforces funds when necessary': 'CENIT_R06_FUNDS_NOT_AVAILABLE' in policy,
    'Additional operational date is optional appended field': 'DateTime? CalculatedNextBusinessDayDeadline = null,\n    DateTime? ReturnOperationalDate = null);' in models,
    'Outbound value date remains transaction-effective': 'transaction.EffectiveEntryDate' in out,
    'Outbound operational day comes from trusted return cycle': 'scope.ReturnCycle.ProcessingDate));' in out,
    'Outbound original operational day comes from AchCycle': 'transaction.AchCycle?.ProcessingDate' in out,
    'Inbound has resolved cycle object with processing date': '.Select(x => new { x.CycleName, x.ProcessingDate })' in inc,
    'Inbound resolves origin AchCycle operational date': 'originalCycle?.ProcessingDate' in inc,
    'Inbound uses resolved return operational date': 'resolvedReturnCycle?.ProcessingDate));' in inc,
    'Inbound R06 fails closed when resolved cycle absent': '!resolvedReturnCycleNumber.HasValue' in inc,
    'Inbound R06 compares file-derived hint and resolved cycle': 'ingestion.DetectedCycleNumber.Value != resolvedReturnCycleNumber.Value' in inc,
    'Inbound R06 mismatch requires review': 'CENIT_R06_RETURN_CYCLE_EVIDENCE_CONFLICT' in inc,
    'Inbound R06 uses verified resolved cycle number': '? resolvedReturnCycleNumber' in inc and 'var returnCycleNumber = string.Equals(classification.ReturnReasonCode, "R06"' in inc,
    'Inbound non-R06 preserves original detected-cycle precedence': ': ingestion.DetectedCycleNumber ?? resolvedReturnCycleNumber;' in inc,
    'Inbound mismatch guard precedes policy evaluate': inc.index('CENIT_R06_RETURN_CYCLE_EVIDENCE_CONFLICT') < inc.index('return _cenitReturnPolicy.Evaluate(new CenitIncomingReturnPolicyRequest('),
    'Outbound policy precedes any physical ReturnOut artifact generation': return_service.index('ValidateCenitOperationalPolicyAsync(') < return_service.index('GenerateAsync(scope, request, assembly, now, ct)'),
    'Outbound CENIT isolation preserved': 'if (!scope.IsCenit)' in out,
    'Inbound CENIT isolation preserved': 'string.Equals(clearingHouseCode, "CENIT"' in inc,
    'Test C4 valid for debit and credit': 'C4SameDayIntoC5_WithCompleteEvidence_IsAllowed' in unit and all(x in unit for x in ('[InlineData(TransactionTypeEnum.Credit)]','[InlineData(TransactionTypeEnum.Debit)]')),
    'Test non-C4 source C1/C2/C3/C5': 'NonC4SourceIntoC5_IsRejected' in unit and all(f'[InlineData({n})]' in unit for n in (1,2,3,5)),
    'Test same value date but different real day': 'C4FromAnotherOperationalDay_IsRejected_EvenWithEqualValueDates' in unit,
    'Test missing evidence manual-review': 'MissingVerifiedCycleDate_NeedsManualReview' in unit,
    'Test partial and non-immediate negative': 'PartialReturnCannotPassC5OperationalGuard' in unit and 'NonImmediateRequestCannotPassC5OperationalGuard' in unit,
    'Test missing funds and confirmation': 'CreditFundsRulesRemainEnforced' in unit and 'OriginatorConfirmationStillRequired' in unit,
    'Test other sessions and R23 unaffected': 'C4ReturnBehaviorRemainsUnchangedWhenNoNewC5EvidenceExists' in unit and 'R23DoesNotInheritR06C5AdditionalEvidence' in unit,
    'Historical F04 positive R06 fixture now has dates': 'OriginalReceivedDate: Monday, ReturnOperationalDate: Monday' in f04,
    'Historical F07 positive R06 fixture now has dates': 'OriginalReceivedDate: ValueDate, ReturnOperationalDate: ValueDate' in f07,
}
for idx,(name,ok) in enumerate(checks.items(),1):
    print(f'{idx:02d} {"PASS" if ok else "FAIL"}: {name}')
print(f'I07: {sum(checks.values())}/{len(checks)} static assertions; runtime tests, DB, output file and CENIT ACK NOT EXECUTED')
sys.exit(0 if all(checks.values()) else 1)
