"""F02 source invariants only; NOT a substitute for dotnet test or CENIT integration."""
from pathlib import Path

root = Path(__file__).resolve().parents[2]
service = (root / 'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitChamberResponseService.cs').read_text()
tests = (root / 'tests/Cfa.ACHInterbank.Tests/CenitChamberResponseLifecycleTests.cs').read_text()
checks = {
    'F02-S01 operator rejection has a branch before file transitions':
        service.index('parsed.Type == CenitChamberResponseType.OperatorRejected)') < service.index('if (CanTransition(export.ChamberResponseState, targetState))'),
    'F02-S02 NACK updates only matched transaction state':
        'x.Id == membership.AchTransactionId && x.AchCycleId == export.AchCycleId' in service
        and 'transaction.State = AchTransferStateEnum.ReturnedByOperator;' in service,
    'F02-S03 NACK creates operator event':
        'context.AchTransactionStateEvents.Add(new AchTransactionStateEvent' in service
        and 'Source = AchStateEventSourceEnum.Operator' in service,
    'F02-S04 ACK/whole file NACK retains separate file transition':
        'parsed.Type != CenitChamberResponseType.OperatorRejected' in service
        and 'export.ChamberResponseState = targetState;' in service,
    'F02-S05 item NACK cannot override official trace by caller':
        '? parsed.TransactionTrace' in service
        and 'CENIT_OPERATOR_REJECTION_TRACE_REQUIRED' in service,
    'F02-S06 duplicate preflight remains before mutations':
        service.index('if (duplicate is not null) return Map(duplicate, true);') < service.index('transaction.State = AchTransferStateEnum.ReturnedByOperator;'),
    'F02-S07 all row effects are saved in one ImportAsync SaveChanges':
        service.index('results.Add(await ImportItemAsync(') < service.index('await context.SaveChangesAsync(ct);')
        and service.index('await context.SaveChangesAsync(ct);') < service.index('private async Task<CenitChamberResponseResult> ImportItemAsync('),
    'F02-S08 ADV validates profile, control and totals':
        'header.Substring(1, 3) != "280"' in service
        and 'header.Substring(50, 3) != "ADV"' in service
        and 'rawDebits != debits * 100m' in service,
    'F02-S09 ADV codes 81-88 and cycle correlation':
        all(f'"{i}"' in service for i in range(81,89))
        and 'parsed.Type is CenitChamberResponseType.Reconciliation' in service,
    'F02-S10 ADV does not apply transaction event or file state':
        'if (correlation == CenitChamberCorrelationOutcome.Matched && sessionOutput)' in service,
    'F02-S11 multiple-item and item-only tests present':
        'IndividualNack_ShouldRejectOnlyMatchingTransaction_AndPreserveFileState' in tests
        and 'IndividualNackAfterTechnicalAck_ShouldStillRejectOnlyTheTransaction' in tests,
    'F02-S12 negative and replay tests present':
        'BatchOnlyNack_ShouldNotRejectAnUnidentifiedTransaction' in tests
        and 'AdvWrongBatchTotals_ShouldNotApply' in tests
        and 'Assert.True(repeated.IsDuplicate);' in tests,
    'F02-S13 gateway continues to invoke official response service':
        'ICenitChamberResponseService' in (root / 'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitLocalGatewayPickupHostedService.cs').read_text(),
}
for label, ok in checks.items():
    print(('PASS' if ok else 'FAIL'), label)
if not all(checks.values()):
    raise SystemExit(1)
print(f'{len(checks)}/{len(checks)} static checks PASS (no C# compilation performed).')
