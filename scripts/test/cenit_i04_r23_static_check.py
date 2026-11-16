#!/usr/bin/env python3
"""I04 source guards; do not mistake these for executing xUnit or bank integration."""
from pathlib import Path
import json
import sys

root = Path(__file__).resolve().parents[2]
policy = (root / 'src/Cfa.ACHInterbank.Application/ACH/Services/CenitIncomingReturnPolicy.cs').read_text(encoding='utf-8-sig')
models = (root / 'src/Cfa.ACHInterbank.Application/ACH/Models/CenitIncomingReturnPolicyModels.cs').read_text(encoding='utf-8-sig')
outbound = (root / 'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchReturnEligibilityPolicyComponent.cs').read_text(encoding='utf-8-sig')
inbound = (root / 'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaPostParseProcessor.cs').read_text(encoding='utf-8-sig')
tests = (root / 'tests/Cfa.ACHInterbank.Tests/CenitI04R23CycleSeparationTests.cs').read_text(encoding='utf-8-sig')

p1 = policy.split('private static CenitIncomingReturnPolicyResult? ValidateFirstCycleReturnException(')[1].split('private static CenitIncomingReturnPolicyResult EvaluateOrdinaryMonetaryReturn(')[0]
p2 = policy.split('private static CenitIncomingReturnPolicyResult EvaluateR23(')[1].split('private static bool IsImmediateFollowingCycle(')[0]
checks = {
    'C1 requires receipt on a previous operational date, not only old value date': 'request.OriginalReceivedDate.Value.Date >= request.ReturnValueDate.Date' in p1,
    'C1 retains R23 specific guard': 'CENIT_CYCLE1_R23_PRIOR_DAY_REQUIRED' in p1,
    'Same-day classifier considers persisted original receipt': 'var receivedToday = request.OriginalReceivedDate.HasValue' in p2 and 'request.OriginalReceivedDate.Value.Date == actualReturnDay' in p2,
    'Same-day branch uses both value and receipt evidence': 'if (receivedToday || (sameValueDate && !receivedBeforeReturnDay))' in p2,
    'Same-day with earlier value date rejects': 'CENIT_R23_SAME_DAY_VALUE_DATE_MISMATCH' in p2 and 'if (!sameValueDate || (request.ReturnOperationalDate.HasValue' in p2,
    'Same-day still needs immediate original+1 cycle': 'IsImmediateFollowingCycle(request.OriginalCycleNumber!.Value, request.ReturnCycleNumber!.Value)' in p2,
    'Request and immediate-cycle evidence still required': 'CENIT_R23_REQUEST_EVIDENCE_REQUIRED' in p2 and 'CENIT_R23_NOT_IMMEDIATE' in p2,
    'Later claims require independent receipt': 'CENIT_R23_ORIGINAL_RECEIPT_REQUIRED' in p2,
    'Later claims require receiver notification': 'CENIT_R23_NOTIFICATION_RECEIPT_REQUIRED' in p2,
    'Later claims keep 15 calendar days': '(submission - receipt).Days > 15' in p2,
    'Later claims keep calculated business-day deadline': 'request.CalculatedNextBusinessDayDeadline.Value.Date' in p2 and 'request.ReceiverRejectionDeadlineDate.Value.Date' not in p2,
    'Original cycle must be within last cycle': 'request.OriginalCycleNumber > request.LastReturnCycleNumber' in policy,
    'Receipt evidence is a model property': 'DateTime? OriginalReceivedDate' in models,
    'Outbound receipt derives from persisted AchCycle': 'transaction.AchCycle?.ProcessingDate' in outbound,
    'Inbound receipt derives from persisted AchCycle': 'originalCycle?.ProcessingDate' in inbound,
    'Outgoing policy applies only to CENIT': 'if (!scope.IsCenit)' in outbound,
    'Tests cover next sessions C1-C5': all(f'[InlineData({i}, {i+1})]' in tests for i in (1,2,3,4)),
    'Tests cover same/skipped sessions': 'SameDay_R23_RejectsSameOrSkippedSessions' in tests,
    'Tests cover older value date received today C1': 'C1_PriorValueDateButReceivedToday_CannotUsePriorDayException' in tests,
    'Tests cover older value date received today C2/C4': 'C2_PriorValueDateButReceivedToday_DoesNotUseLaterClaimPath' in tests and 'C4_PriorValueDateReceivedToday_CannotUseSkippedNextCycle' in tests,
    'Tests keep true prior-day C1/C5 permitted': 'C1_GenuinePriorDayReceiptAndReceiverClaim_IsStillAllowed' in tests and 'C5_GenuinePriorDayReceiptAndReceiverClaim_IsStillAllowed' in tests,
    'Tests cover unknown evidence/manual review': 'PriorDayR23_MissingReceiptEvidence_RemainsManualReview' in tests,
    'Tests cover amount and supplied-deadline tamper': 'SameDayR23_PartialReturnIsRejectedBeforeSessionRules' in tests and 'PriorDayR23_DoesNotTrustSuppliedReceiverDeadline' in tests,
    'No ACH Colombia implementation edited by I04 guard': 'ACHCOL' not in p2,
}
for name, ok in checks.items():
    print(('PASS' if ok else 'FAIL') + ': ' + name)
print(f'I04 STATIC: {sum(checks.values())}/{len(checks)}; NO xUnit, DB, NACHA-M or operator ACK executed')
sys.exit(0 if all(checks.values()) else 1)
