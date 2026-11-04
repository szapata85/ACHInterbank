#!/usr/bin/env python3
"""I02 dependency-free STATIC guardrails. Does not compile/run C#; do not report as Xunit."""
from pathlib import Path
import json
root=Path(__file__).resolve().parents[2]
def src(p): return (root/p).read_text(encoding='utf-8-sig')
paths={
 'model':'src/Cfa.ACHInterbank.Application/ACH/Models/NachaInboundSimulationModels.cs',
 'contract':'src/Cfa.ACHInterbank.Application/ACH/Services/PrenotificationSimulationContract.cs',
 'policy':'src/Cfa.ACHInterbank.Application/ACH/Services/PrenotificationSimulationEligibilityPolicy.cs',
 'query':'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaInboundSimulationService.Prenotifications.cs',
 'service':'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaInboundSimulationService.cs',
 'controller':'src/Cfa.ACHInterbank.Api/Controllers/NachaInboundSimulatorController.cs',
 'api':'src/Cfa.ACHInterbank.Application/ACH/Interfaces/INachaInboundSimulationService.cs',
 'tests':'tests/Cfa.ACHInterbank.Tests/PrenotificationSimulatorEligibilityI02Tests.cs'}
d={k:src(v) for k,v in paths.items()}
cases=[
 ('contracts-paged', all(x in d['model'] for x in ['PrenotificationCandidatePage','PageSize','OriginalPrenotificationId','DestinationAccountMasked'])),
 ('contract-max-100', 'PRENOTIFICATION_IDS_LIMIT_EXCEEDED' in d['contract']),
 ('policy-outcome-separation', all(x in d['policy'] for x in ['ReturnDeadlineExpired'.upper() if False else 'RETURN_DEADLINE_EXPIRED','PRENOTIFICATION_DEADLINE_NOT_REACHED'])),
 ('policy-not-prenotification', 'NOT_A_PRENOTIFICATION' in d['policy']),
 ('policy-source', 'NOT_ORIGINATED_HERE' in d['policy']),
 ('policy-incomplete-correlation', 'PRENOTIFICATION_CORRELATION_INCOMPLETE' in d['policy']),
 ('policy-not-transmitted', 'PRENOTIFICATION_NOT_TRANSMITTED' in d['policy']),
 ('policy-not-active', 'PRENOTIFICATION_ATTEMPT_NOT_ACTIVE' in d['policy']),
 ('policy-duplicate', 'PRENOTIFICATION_ALREADY_RESPONDED' in d['policy']),
 ('policy-deadline-unknown', 'PRENOTIFICATION_DEADLINE_UNKNOWN' in d['policy']),
 ('query-house', 'x.AchCycle.ClearingHouseId == clearingHouse.Id' in d['query']),
 ('query-src-dest', 'x.SourceInstitution.IsDefaultSource' in d['query'] and '!x.DestinationInstitution.IsDefaultSource' in d['query']),
 ('query-readonly', d['query'].count('AsNoTracking()')>=5 and 'SaveChangesAsync' not in d['query'] and 'File.Write' not in d['query']),
 ('query-transmission', all(x in d['query'] for x in ['AchFileExportLifecycleStatus.Transmitted','AchFileExportLifecycleStatus.Acknowledged','x.TraceNumber == x.AchTransaction.TraceNumber'])),
 ('query-attempt-correlated', 'x.AchTransactionId.HasValue && ids.Contains(x.AchTransactionId.Value)' in d['query']),
 ('query-previous-response', 'AchResponses.AsNoTracking()' in d['query']),
 ('preview-cycle-check', 'RESPONSE_CYCLE_BEFORE_ORIGINAL' in d['query'] and 'RESPONSE_CYCLE_EQUALS_ORIGINAL' in d['query']),
 ('preview-bank-check', 'PRENOTIFICATION_WRONG_RESPONDER' in d['query']),
 ('preview-chamber-check', 'PRENOTIFICATION_WRONG_CLEARING_HOUSE' in d['query']),
 ('preview-cenit-value-date', 'CENIT_RETURN_OUTSIDE_VALUE_DATE' in d['query']),
 ('preview-readonly-execution-off', 'violations.Count == 0, false' in d['query']),
 ('query-local-uat-only', 'IsUatLike()' in d['query']),
 ('api-policy-both-endpoints', d['controller'].count('[Authorize(Policy = P1Policies.NachaSimulatorRead)]')>=2 and 'prenotifications/originated' in d['controller'] and 'prenotifications/eligibility-preview' in d['controller']),
 ('api-interface', 'ListOriginatedPrenotificationsAsync' in d['api'] and 'PreviewPrenotificationAsync' in d['api']),
 ('legacy-scenario-preserved', 'PRENOTIFICATION_RETURN_NOT_IMPLEMENTED' in d['service'] and 'IncomingPrenotificationReturn' in d['service']),
 ('tests-adversarial', all(x in d['tests'] for x in ['BlocksRepliedClosedOrIncomingAndNonCurrentAttempt','DoesNotInferSendingFromCreation','I02_ListAndPreviewAreReadOnly','I02_RejectsOtherClearingHouseAndTooManyIds'])),
]
for desc,ok in cases: print(f"{'PASS_STATIC' if ok else 'FAIL_STATIC'} {desc}")
result={'checks_total':len(cases),'passed':sum(ok for _,ok in cases),'failed':[desc for desc,ok in cases if not ok],'classification':'STATIC_ONLY_NOT_DOTNET_BUILD_OR_TEST'}
print(json.dumps(result,ensure_ascii=False));raise SystemExit(bool(result['failed']))
