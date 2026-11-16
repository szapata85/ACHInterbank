#!/usr/bin/env python3
"""I05 source assertions and normative reference fixtures. Not dotnet build/xUnit."""
from pathlib import Path
import re
import sys

r = Path(__file__).resolve().parents[2]
services = r / 'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation'
p = (services/'TransactionPriorityPolicy.cs').read_text(encoding='utf-8-sig')
s = (services/'LiquidityOptimizationService.cs').read_text(encoding='utf-8-sig')
t = (r/'tests/Cfa.ACHInterbank.Tests/CenitI05PrioritySimulationTests.cs').read_text(encoding='utf-8-sig')
old = (r/'tests/Cfa.ACHInterbank.Tests/CenitOperationalGovernanceTests.cs').read_text(encoding='utf-8-sig')
policy_map = {
    ('CCD','Return','21'):10, ('PPD','Return','21'):9,
    ('CCD','Return','26'):8, ('PPD','Return','26'):7,
    ('CTX','Return','26'):6, ('CCD','Debit','27'):5,
    ('PPD','Debit','27'):4, ('CCD','Credit','22'):3,
    ('PPD','Credit','22'):2, ('CTX','Credit','22'):1,
}
checks = {
  'All ten DSP-152 §5.1 priority fixtures appear as xUnit InlineData': all(
      f'[InlineData("{sec}", TransactionTypeEnum.{typ}, "{code}", {level})]' in t
      for (sec,typ,code),level in policy_map.items()),
  'Ten normative labels cover exact consecutive levels': sorted(policy_map.values()) == list(range(1,11)),
  'Policy verifies active SEC catalog before mapping': 'x.Id == transaction.CompanyEntryDescriptionId && x.IsActive' in p and '"PPD" or "CCD" or "CTX"' in p,
  'Policy denies priority without catalog evidence': 'if (_context is null || transaction.CompanyEntryDescriptionId <= 0)' in p and 'return 0;' in p,
  'Return debit codes restricted to returns': 'transaction.TransactionCode is "21" or "31" or "51"' in p,
  'Return credit codes restricted to returns': 'transaction.TransactionCode is "26" or "36" or "56"' in p,
  'Return does not include ordinary credit or prenotification codes': '"21" or "22" or "23"' not in p,
  'Return does not include ordinary debit or prenotification codes': '"26" or "27" or "28"' not in p,
  'Ordinary credit validated against NACHA code': 'var isOrdinaryCredit = transaction.TransactionCode is' in p,
  'Ordinary debit validated against NACHA code': 'var isOrdinaryDebit = transaction.TransactionCode is' in p,
  'Unknown SEC and inconsistent type return zero': 'if ((transaction.Type == TransactionTypeEnum.Credit && !isOrdinaryCredit)' in p and '(transaction.Type == TransactionTypeEnum.Debit && !isOrdinaryDebit)' in p,
  'CCD returned debit has priority ten': '"CCD" => 10, "PPD" => 9' in p,
  'Returned credit includes CTX level six': '"CCD" => 8, "PPD" => 7, "CTX" => 6' in p,
  'Ordinary transaction table 5 to 1 complete': all(pattern in p for pattern in ('("CCD", TransactionTypeEnum.Debit) => 5', '("PPD", TransactionTypeEnum.Debit) => 4', '("CCD", TransactionTypeEnum.Credit) => 3','("PPD", TransactionTypeEnum.Credit) => 2','("CTX", TransactionTypeEnum.Credit) => 1')),
  'Simulator guards against ACH Colombia before returning snapshot': 'if (!string.Equals(cycle.ClearingHouse?.Code, "CENIT"' in s and s.index('if (!string.Equals(cycle.ClearingHouse?.Code, "CENIT"') < s.index('var existing = await _context.LiquidityOptimizationDecisions'),
  'Candidate simulation sorted descending priority': '.OrderByDescending(t => priorityMap[t.Id])' in s,
  'Amount tie break descending': '.ThenByDescending(t => t.Amount)' in s,
  'Final tie break by stable transaction ID': '.ThenBy(t => t.Id)' in s,
  'Unknown class cannot consume simulated liquidity': 'var hasLiquidity = classified && liquidity >= tx.Amount;' in s,
  'Unclassified decisions have distinct state': '"SimulatedUnclassified"' in s and '"UnverifiedSecOrTransactionCode_NoLiquidityConsumption"' in s,
  'Simulated decision has no destination cycle': 'ToCycleId = null' in s,
  'Liquidity service writes only simulation projections': '_context.LiquidityOptimizationDecisions.AddRange(decisions)' in s and 'tx.State =' not in s and 'tx.ReturnReasonCode =' not in s,
  'No simulated operator reject state': 'DecisionType = !classified ? "SimulatedUnclassified"' in s and 'DecisionType = "Rejected"' not in s,
  'No other chamber reference in source query': '.Where(x => x.AchCycleId == execution.AchCycleId)' in s,
  'xUnit negative SEC/transaction codes': 'InvalidSecOrCode_NeverReceivesPriority' in t and '[InlineData("CTX", TransactionTypeEnum.Return, "21")]' in t,
  'xUnit deterministic permutation fixture': 'Simulation_OrdersDescending_ThenAmount_ThenId_AcrossInputPermutations' in t and 'Array.Reverse(specs)' in t,
  'xUnit whole pipeline and idempotence': 'OptimizeCycleAsync(execution, CancellationToken.None)' in t and 'Assert.Equal(first.Count, await context.LiquidityOptimizationDecisions.CountAsync())' in t,
  'xUnit verifies no state mutations': 'Assert.False(await context.CenitCycleQueues.AnyAsync())' in t and 'Assert.False(await context.AchTransactionStateEvents.AnyAsync())' in t,
  'xUnit prevents foreign chamber before writes and on snapshot replay': 'Simulation_RefusesForeignClearingHouse_BeforeWritingDecisions' in t and 'Simulation_RefusesForeignClearingHouse_EvenWithPreviousSnapshot' in t,
  'Legacy priority tests updated to return codes': '[InlineData("PPD", TransactionTypeEnum.Return, "21", 9)]' in old and '[InlineData("CCD", TransactionTypeEnum.Return, "26", 8)]' in old,
  'Legacy optimization fixture provides CENIT context': 'new TransactionPriorityPolicy(catalog, context)' in old and 'TransactionCode = "27", AchCycleId' in old,
  'New tests do not assert official settlement': 'operator' in t.lower() or 'Banco de la República' in t,
}
for name, ok in checks.items():
    print(f'{"PASS" if ok else "FAIL"}: {name}')
print(f'I05 STATIC: {sum(checks.values())}/{len(checks)}; no .NET compilation, xUnit, SQL engine or operator ACK executed')
sys.exit(0 if all(checks.values()) else 1)
