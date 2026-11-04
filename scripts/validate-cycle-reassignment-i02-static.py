#!/usr/bin/env python3
"""I02: controles estáticos focales. NO compila C# ni simula concurrencia SQL."""
from pathlib import Path
import sys

base = Path(__file__).resolve().parent.parent
new = {
    'contract': 'src/Cfa.ACHInterbank.Application/ACH/Interfaces/IAchPendingCycleReassignmentService.cs',
    'result': 'src/Cfa.ACHInterbank.Application/ACH/Models/AchPendingCycleReassignmentResult.cs',
    'service': 'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchPendingCycleReassignmentService.cs',
    'tests': 'tests/Cfa.ACHInterbank.Tests/AchPendingCycleReassignmentServiceTests.cs',
    'evidence': 'docs/ach-cycle-reassignment/08_I02_IMPLEMENTACION_Y_EVIDENCIA.md',
}
checks = []
def check(desc, cond):
    checks.append((desc, bool(cond)))
    print(('PASS' if cond else 'FAIL')+' '+desc)
for key, value in new.items():
    check(f'archivo {key}', (base/value).is_file())
if not all((base/f).is_file() for f in new.values()):
    sys.exit(1)
c = (base/new['contract']).read_text(encoding='utf-8')
r = (base/new['result']).read_text(encoding='utf-8')
s = (base/new['service']).read_text(encoding='utf-8')
t = (base/new['tests']).read_text(encoding='utf-8')
a = (base/new['evidence']).read_text(encoding='utf-8')
check('interfaz Application libre de EF', 'ReassignAsync(int transactionId' in c and 'EntityFramework' not in c)
check('resultado independiente de Infrastructure', 'record AchPendingCycleReassignmentResult' in r and 'EntityFramework' not in r)
check('implementación DI convention', '[Scoped]' in s and ' : IAchPendingCycleReassignmentService' in s)
check('reutiliza elegibilidad I01', 'IAchPendingCycleEligibilityService' in s and '_eligibility.EvaluateAsync' in s)
check('estrategia relacional y Serializable', 'CreateExecutionStrategy()' in s and 'IsolationLevel.Serializable' in s and 'BeginTransactionAsync' in s)
check('commit después de mutación/auditoría', s.index('await dbTransaction.CommitAsync(ct)') > s.index('ReassignCoreAsync(transactionId, ct)'))
check('predicado de estado/origen coherente', 'transaction.State != AchTransferStateEnum.Pending' in s and 'transaction.AchCycleId != decision.FromCycleId' in s)
check('protección evento duplicado', 'AchTransactionStateEvents.AsNoTracking().AnyAsync' in s and 'IdempotencyKey == key' in s)
check('destino misma cámara y fecha valor', 'targetCycle.ClearingHouseId != decision.ClearingHouseId' in s and 'targetCycle.ProcessingDate.Date != transaction.EffectiveEntryDate.Date' in s)
check('lotes reutilizados de repositorio', '_batches.FindForTransactionAsync' in s and '_batches.AddAsync' in s)
check('lote incompatible fail closed', 'TARGET_BATCH_INCOMPATIBLE' in s)
check('guarda ambos FK', 'transaction.AchCycleId = targetCycle.Id' in s and 'transaction.AchBatchId = targetBatch.Id' in s)
check('auditoría from-to same state sin cambiar timestamp financiero', 'FromState = AchTransferStateEnum.Pending' in s and 'ToState = AchTransferStateEnum.Pending' in s and 'transaction.StateChangedAtUtc =' not in s)
check('auditoría en el mismo DbContext', '_context.AchTransactionStateEvents.Add(' in s and s.count('await _context.SaveChangesAsync(ct)') >= 2)
check('reusa totales clase servicio', '_persister.UpdateBatchTotalsAsync(sourceBatch' in s and '_persister.UpdateBatchServiceClassCodeAsync(targetBatch' in s)
check('no nuevas infra de bloqueo o endpoints', all(x not in s for x in ['SemaphoreSlim', 'lock (', 'Quartz', 'HttpClient', 'ExecuteSqlRaw']))
check('tests unitarios >=8', t.count('[Fact]') >= 8 and 'UseInMemoryDatabase' in t)
check('documenta carrera exportador y no runtime', 'exportador que haya leído' in a and 'NO EJECUTADAS' in a)
check('sin modificación Domain', 'using Cfa.ACHInterbank.Persistence' not in c+r)
check('no se declaran dependencias de paquetes en C# agregado', 'PackageReference' not in c+r+s+t)
print(f'I02 ESTATICO {sum(ok for _,ok in checks)}/{len(checks)}')
sys.exit(0 if all(ok for _,ok in checks) else 1)
