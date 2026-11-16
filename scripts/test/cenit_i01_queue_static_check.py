#!/usr/bin/env python3
"""I01 source-contract guard. Static-only: cannot certify xUnit, DB, Quartz or NACHA output."""
from pathlib import Path
import re

root = Path(__file__).resolve().parents[2]
src = (root / 'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitCycleQueueService.cs').read_text()
coord = (root / 'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaFileGenerationCoordinator.cs').read_text()
model = (root / 'src/Cfa.ACHInterbank.Domain/Models/ACH/CenitCycleQueue.cs').read_text()
checks_cs = (root / 'tests/Cfa.ACHInterbank.Tests/CenitCycleQueueIncorporationF03Tests.cs').read_text()

# Inspeccionar el cuerpo relevante, no los comentarios o la ruta Enqueue.
core = src.split('private async Task<int> IncorporateCoreAsync(', 1)[1]
guard = core.index('if (targetSession == 5)')
change = core.index('tx.AchCycleId = targetAchCycleId;')
consume = core.index('item.Status = "Consumed";')
event = core.index('AchTransactionStateEvents.Add(')
checks = {
    'alcance CENIT en coordinador': 'string.Equals(clearingHouseCode, "CENIT", StringComparison.OrdinalIgnoreCase)' in coord,
    'incorpora antes de materializar transacciones NACHA': coord.index('await _cenitCycleQueueService.IncorporateForCycleAsync') < coord.index('var transactions = context.Transactions;', coord.index('BuildNachaFilesByCycleAsync(string cycleId')),
    'ciclo físico resuelto desde CycleName': 'new CycleNumberResolver().Resolve(target.CycleName)' in core,
    'falla cerrado con sesión ambigua': 'CENIT_QUEUE_TARGET_SESSION_UNRESOLVED' in core,
    'C5 sujeto a política reutilizada': '_cycleTransactionPolicy.EvaluateAsync(' in core and 'target.ProcessingDate' in core,
    'C5 clase explícita Return/ReturnOfReturn': 'decision.FunctionalClass is not ("Return" or "ReturnOfReturn")' in core,
    'C5 política denegada bloquea': '!decision.IsAllowed' in core,
    'control pre-commit antes de mover/consumir/evento': guard < change < consume < event,
    'commit transaccional serializable': 'BeginTransactionAsync(IsolationLevel.Serializable' in src and 'CommitAsync(ct)' in src,
    'captura y limpia contexto ante fallo': '_context.ChangeTracker.Clear();' in src,
    'la fecha valor no se reescribe': not re.search(r'\btx\.EffectiveEntryDate\s*=(?!=)', core),
    'estado financiero no se cambia': not re.search(r'\btx\.State\s*=(?!=)', core),
    'cola conserva status Queued o Consumed': 'public string Status { get; set; } = "Queued";' in model and 'item.Status = "Consumed";' in core,
    'tests C4-C5, replay, mixto, y sesión inválida': all(name in checks_cs for name in (
        'FifthSession_RejectsOrdinaryMonetaryBeforeConsumingQueue',
        'FifthSession_EligibleReturnFromFourthSession_IsMovedExactlyOnce',
        'FifthSession_DeniedReturnPolicy_RemainsQueued',
        'FifthSession_MixedValidReturnAndMonetary_RollsBackAllQueueMovements',
        'UnparseableCenitSession_FailsClosedWithoutMovingQueue')),
    'regresión previa DB/rollback conservada': all(name in checks_cs for name in (
        'QueuedOperation_ChangesCycleAndBatchBeforeExport',
        'InvalidSecondQueue_RollsBackFirstQueueAndItsNewBatch',
        'SqlFailureOnAuditEvent_RollsBackBatchMovementAndQueueStatus')),
}
for name, ok in checks.items():
    print(f"{'PASS' if ok else 'FAIL'} I01 {name}")
print(f"I01 STATIC RESULT {sum(checks.values())}/{len(checks)}. NO se ejecutaron xUnit, DB, Quartz ni CENIT real.")
raise SystemExit(0 if all(checks.values()) else 1)
