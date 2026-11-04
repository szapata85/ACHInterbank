#!/usr/bin/env python3
"""Static F03 regression guard; not a .NET test or a certified database exercise."""
from pathlib import Path
root=Path(__file__).resolve().parents[2]
service=(root/'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitCycleQueueService.cs').read_text()
iface=(root/'src/Cfa.ACHInterbank.Application/ACH/Interfaces/ICenitCycleQueueService.cs').read_text()
coord=(root/'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaFileGenerationCoordinator.cs').read_text()
builder=(root/'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaFileBuilder.cs').read_text()
loader=(root/'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaDataLoader.cs').read_text()
tests=(root/'tests/Cfa.ACHInterbank.Tests/CenitCycleQueueIncorporationF03Tests.cs').read_text()
checks = {
 'Service method is a public interface contract': 'Task<int> IncorporateForCycleAsync(' in iface and 'public async Task<int> IncorporateForCycleAsync(' in service,
 'Export calls incorporation before materializing NACHA details': coord.index('await _cenitCycleQueueService.IncorporateForCycleAsync') < coord.index('var transactions = context.Transactions;',coord.index('public async Task<NachaFileBuildResult> BuildNachaFilesByCycleAsync')),
 'Only CENIT invokes incorporation': 'string.Equals(clearingHouseCode, "CENIT", StringComparison.OrdinalIgnoreCase)' in coord,
 'Builder forwards dependency to coordinator': 'cenitCycleQueueService);' in builder,
 'Loader filters transactions by actual cycle id': '.Where(t => t.AchCycleId == cycleId' in loader,
 'Serializable transaction and retry strategy': 'IsolationLevel.Serializable' in service and 'CreateExecutionStrategy' in service,
 'Queue is consumed only after assignment': service.index('tx.AchCycleId = targetAchCycleId;') < service.index('item.Status = "Consumed";'),
 'Transaction, batch and event share SaveChanges': 'await _context.SaveChangesAsync(ct);' in service and 'AchTransactionStateEvents.Add' in service,
 'No CUD and no financial-state transition': 'tx.State =' not in service and 'tx.ReturnReasonCode' not in service and 'CUD' not in service,
 'Idempotence avoids repeated consumption': 'if (queued.Count == 0) return 0;' in service and 'if (existing.Status == "Consumed") return existing;' in service,
 'Exported target blocked': 'CENIT_QUEUE_TARGET_ALREADY_EXPORTED' in service,
 'Previously presented source blocked': 'CENIT_QUEUE_ALREADY_PRESENTED' in service,
 'Cross-business-day value date not silently modified': 'CENIT_QUEUE_VALUE_DATE_REVIEW_REQUIRED' in service and 'tx.EffectiveEntryDate =' not in service,
 'Old and new batch control totals updated': 'touchedBatchIds.Add(oldBatchId)' in service and 'touchedBatchIds.Add(targetBatch.Id)' in service,
 'F03 tests include success and idempotence': 'QueuedOperation_ChangesCycleAndBatchBeforeExport' in tests and 'Assert.Equal(0, await sut.IncorporateForCycleAsync' in tests,
 'F03 tests include fault and rollback': 'SqlFailureOnAuditEvent_RollsBackBatchMovementAndQueueStatus' in tests and 'InvalidSecondQueue_RollsBackFirstQueueAndItsNewBatch' in tests,
 'F03 tests include ACH and value-date boundaries': 'AnotherChamber_RejectsDirectQueueProcessing' in tests and 'DifferentBusinessDay_DoesNotSilentlyChangeValueDate' in tests,
}
for k,v in checks.items():print(('PASS' if v else 'FAIL')+' F03 '+k)
if not all(checks.values()):raise SystemExit(1)
print(f'RESULTADO {len(checks)}/{len(checks)} controles estáticos; dotnet/xUnit NO ejecutados por ausencia de SDK.')
