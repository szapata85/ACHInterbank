#!/usr/bin/env python3
"""Post-I13 ROR C5: static source-contract checks, NOT a replacement for .NET/xUnit."""
from pathlib import Path
import re
p = Path(__file__).resolve().parents[2]
s = (p/'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitReturnOfReturnService.cs').read_text(encoding='utf-8')
t = (p/'tests/Cfa.ACHInterbank.Tests/CenitReturnOfReturn2026Tests.cs').read_text(encoding='utf-8')
policy = s.split('private async Task<CenitReturnOfReturnResult?> ValidateOperationalWindowAsync',1)[1].split('private static AchTransaction CloneAsRor',1)[0]
same = policy.split('if (target.ProcessingDate.Date == parent.ProcessingDate.Date)',1)[1].split('var nextBusinessDay',1)[0]
checks = {
 'Fixed CENIT normative last cycle C5': 'private const int CenitLastRegularCycleNumber = 5;' in s,
 'No last-cycle maximum based on rows': 'Max()' not in policy and 'lastCycleNumber' not in policy and 'DefaultIfEmpty' not in policy,
 'Require a resolved parent cycle': 'parent is null || target is null' in policy,
 'Require same date for same-day branch': 'target.ProcessingDate.Date == parent.ProcessingDate.Date' in policy,
 'Require parent from C2 or later': 'parentNumber.Value >= 2' in same,
 'Restrict parent to prior to C5': 'parentNumber.Value < CenitLastRegularCycleNumber' in same,
 'Require destination exactly C5': 'targetNumber.Value == CenitLastRegularCycleNumber' in same,
 'Keep specific same-day rejection code': 'CENIT_ROR_SAME_DAY_CYCLE_INVALID' in same,
 'No implicit operational-exception bypass': 'authorized' not in same.lower() and 'override' not in same.lower(),
 'Preserve next-business-day check': 'GetNextBusinessDayAsync' in policy and 'targetNumber.Value <= 2' in policy,
 'Outgoing path invokes shared window': 'var operationalFailure = await ValidateOperationalWindowAsync(' in s.split('public async Task<CenitReturnOfReturnResult> CreateOutgoingAsync',1)[1].split('public async Task<CenitReturnOfReturnResult> IngestIncomingAsync',1)[0],
 'Incoming path invokes shared window': 'var operationalFailure = await ValidateOperationalWindowAsync(' in s.split('public async Task<CenitReturnOfReturnResult> IngestIncomingAsync',1)[1].split('private async Task<CenitReturnOfReturnResult?> ValidatePolicyAsync',1)[0],
 'Fixture has five sessions C1 to C5': all(f'Id = "C{i}", CycleName = "Ciclo {i}"' in t.split('private static AchTransaction SeedDomain',1)[1] for i in range(1,6)),
 'Missing C5 four-cycle negative': 'RorSameDay_C4CannotBecomeLastCycleWhenC5IsMissingFromDatabase' in t,
 'Configured C5 still rejects C4': 'RorSameDay_C4IsNotPermittedEvenWithC5Configured' in t,
 'Positive and negative parent-cycle matrix': 'RorSameDay_OnlyParentC2ThroughC4MayTargetC5' in t and all(f'[InlineData("C{i}", {str(i in (2,3,4)).lower()})]' in t for i in range(1,6)),
 'Incoming path rejects incomplete day': 'RorIncoming_C4CannotBeTargetWhenDatabaseContainsOnlyFourCycles' in t,
 'Previously positive outgoing now targets C5': 'var outResult = await sut.CreateOutgoingAsync(new(1001, "R60", "C5"' in t,
 'Previously positive incoming now targets C5': '2001, original.Id, "C5", "R61"' in t,
 'Next-day C1 tests retained': 'F05_Cycle1_CorrelatesPersistedSourceReturnBeforeCreatingFlow' in t,
}
for i,(name,ok) in enumerate(checks.items(),1): print(f'{i:02d} {"PASS" if ok else "FAIL"} {name}')
print(f'TOTAL {sum(checks.values())}/{len(checks)} STATIC SOURCE-CHECK ONLY')
raise SystemExit(0 if all(checks.values()) else 1)
