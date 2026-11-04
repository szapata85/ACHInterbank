#!/usr/bin/env python3
"""F07 executable static/source-wiring and fixture-integrity regression.

IMPORTANT: This script does not compile or execute C# or certify CENIT operation.
The corresponding runtime xUnit suite must run separately with a .NET 10 SDK.
"""
from pathlib import Path
import hashlib
import sys

root = Path(__file__).resolve().parents[2]
implementation = root / 'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation'
application = root / 'src/Cfa.ACHInterbank.Application/ACH/Services'
tests = root / 'tests/Cfa.ACHInterbank.Tests'
contents = {name: (implementation / name).read_text(encoding='utf-8-sig') for name in (
    'CenitCycleExecutionService.cs', 'CenitNettingService.cs',
    'CenitChamberResponseService.cs', 'CenitCycleQueueService.cs',
    'NachaFileGenerationCoordinator.cs', 'NachaPhysicalParser.cs',
    'NachaParserService.cs', 'NachaInboundValidation.cs',
    'TransactionPriorityPolicy.cs', 'LiquidityOptimizationService.cs',
    'CenitReturnOfReturnService.cs', 'AchReturnOfReturnFileGenerationService.cs')}
checks = []
def check(name, passes):
    checks.append((name, bool(passes)))
    print(f'{len(checks):02} {"PASS" if passes else "FAIL"} {name}')

a = contents
check('F01 Quartz awaits official state, not simulator',
      'AwaitingOfficialResult' in a['CenitCycleExecutionService.cs'] and
      'CalculateAsync(' not in a['CenitCycleExecutionService.cs'] and
      'OptimizeAsync(' not in a['CenitCycleExecutionService.cs'])
check('F01 priority is informational and CENIT-specific evidence required',
      'Sin SEC verificable' in a['TransactionPriorityPolicy.cs'] and
      'CompanyEntryDescriptionCatalogs.AsNoTracking()' in a['TransactionPriorityPolicy.cs'])
check('F02 NACK is transaction-specific with event',
      'CENIT_OPERATOR_REJECTION_TRACE_REQUIRED' in a['CenitChamberResponseService.cs'] and
      'AchTransactionStateEvents.Add' in a['CenitChamberResponseService.cs'] and
      'export.ChamberResponseState = targetState' in a['CenitChamberResponseService.cs'])
check('F02 ADV remains evidence only',
      'ParseAdv(content)' in a['CenitChamberResponseService.cs'] and
      'officialAccountNotPosted=true' in a['CenitChamberResponseService.cs'] and
      'sessionOutput' in a['CenitChamberResponseService.cs'])
check('F03 queue incorporated before transactions materialized',
      a['NachaFileGenerationCoordinator.cs'].index('IncorporateForCycleAsync(cycleId') <
      a['NachaFileGenerationCoordinator.cs'].index('var transactions = context.Transactions;',
        a['NachaFileGenerationCoordinator.cs'].index('BuildNachaFilesByCycleAsync(')))
check('F03 queue only CENIT with serializable DB and CUD-free changes',
      'string.Equals(clearingHouseCode, "CENIT"' in a['NachaFileGenerationCoordinator.cs'] and
      'IsolationLevel.Serializable' in a['CenitCycleQueueService.cs'] and
      'CENIT_QUEUE_VALUE_DATE_REVIEW_REQUIRED' in a['CenitCycleQueueService.cs'] and
      'tx.State =' not in a['CenitCycleQueueService.cs'])
check('F04 strict R23/R06 and date evidence', all(term in (application / 'CenitIncomingReturnPolicy.cs').read_text() for term in (
      'CENIT_R23_SAME_DAY_NOT_NEXT_CYCLE', 'CENIT_CYCLE5_R06_ORIGIN_C4_REQUIRED',
      'CENIT_R23_CALENDAR_VALIDATION_REQUIRED')))
check('F05 source ROR and R74 lineage check retained',
      'R74' in a['CenitReturnOfReturnService.cs'] and
      'R32' in a['CenitReturnOfReturnService.cs'] and
      'SourceReturn' in a['CenitReturnOfReturnService.cs'])
check('F06 known CENIT profiles and published authority',
      'publishedAuthority?.IsCenit == true' in a['NachaInboundValidation.cs'] and
      all(s in a['NachaInboundValidation.cs'] for s in ('CenitReturnIn2026Layout.IsProfile','CenitReturnOfReturn2026Layout.IsProfile')))
check('F06 NACHA parser chooses domain before physical parse',
      a['NachaParserService.cs'].index('SelectNachaErrorDomain(') <
      a['NachaParserService.cs'].index('NachaPhysicalParser.ParseAsync(nachaStream'))
check('F06 CENIT rejects enyes but ACH retains historical behavior',
      '(!isCenitNachaProfile && (value is 0xD1 or 0xF1))' in a['NachaPhysicalParser.cs'])
check('F06 STA Dxx not emitted as CENIT NACHA rejection',
      'CENIT-NACHA-FATAL' in a['NachaInboundValidation.cs'] and
      'SelectNachaErrorDomain(bool isCenitNachaProfile)' in a['NachaInboundValidation.cs'])
check('F07 runtime cross-phase test cases present',
      (tests/'CenitF07CrossPhaseRegressionTests.cs').exists() and
      'F07_OfficialAckIndividualNackAndAdv' in (tests/'CenitChamberResponseLifecycleTests.cs').read_text())
check('F07 runtime safeguards include ACH/CENIT reentrancy',
      all(term in (tests/'CenitF07CrossPhaseRegressionTests.cs').read_text() for term in (
          'PhysicalProfileIsolation_AchColombiaAcceptedBeforeAndAfterCenitRejection',
          'ValidationDomainIsolation_CenitStaCodesDoNotLeakToAchColombiaAndBack',
          'ReturnPolicy_R06FifthCycleIsGatedBeforeLiquidityEvidence')))

# These are real byte-level fixtures (not outputs of the C# parser).
# Each golden file is a concatenation of ten 106-byte records. No line breaks.
fixture_dir=tests/'TestData/Nacha/GoldenFiles'
for chamber in ('ACHColombia','CENIT'):
    files=sorted((fixture_dir/chamber).rglob('*'))
    files=[p for p in files if p.is_file()]
    check(f'{chamber} three immutable golden files present',len(files)==3)
    for fixture in files:
        blob=fixture.read_bytes()
        records=[blob[i:i+106] for i in range(0,len(blob),106)]
        check(f'{chamber} byte/record integrity {fixture.name}',
              len(blob)==1060 and len(records)==10 and
              records[0][:1]==b'1' and all(len(r)==106 for r in records) and
              all(32<=b<=126 for b in blob))

ror=tests/'TestData/Nacha/NormativeSynthetic/Cenit/Ror/CENIT_ROR_IN_001.ach'
raw=ror.read_bytes(); rows=raw.splitlines()
check('CENIT synthetic ROR has 10 106-byte lines', len(rows)==10 and all(len(row)==106 for row in rows))
check('CENIT synthetic ROR retains 99 addenda and R61',len(rows)>=4 and rows[3].startswith(b'799R61'))

ok=sum(passes for _,passes in checks)
print(f'RESULT: {ok}/{len(checks)} PASS; C# xUnit, relational DB and operator gateway NOT EXECUTED')
sys.exit(0 if ok==len(checks) else 1)
