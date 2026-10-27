#!/usr/bin/env python3
"""I03 validación estática estructural. No compila C# ni prueba aislamiento/exportación."""
from pathlib import Path
import re
import sys
b=Path(__file__).resolve().parent.parent
paths={
'handler':'src/Cfa.ACHInterbank.Persistence/ACH/Quartz/Jobs/Implementation/AchPendingCycleReassignmentHandler.cs',
'catalog':'src/Cfa.ACHInterbank.Persistence/Scheduler/SchedulerTaskCatalog.cs',
'seeder':'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/Seeders/TaskDefinitionSeeder.cs',
'tests':'tests/Cfa.ACHInterbank.Tests/AchPendingCycleReassignmentHandlerTests.cs',
'evidence':'docs/ach-cycle-reassignment/09_I03_IMPLEMENTACION_Y_EVIDENCIA.md',
'memory':'docs/ach-cycle-reassignment/MEMORY_STATE.md',
'command':'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchPendingCycleReassignmentService.cs',
}
checks=[]
def check(name,condition):
 checks.append((name,bool(condition)))
 print(('PASS ' if condition else 'FAIL ')+name)
for name,p in paths.items():check(f'Existe {name}',(b/p).is_file())
if not all((b/p).is_file() for p in paths.values()):sys.exit(1)
h,c,s,t,e,m,cmd=[(b/paths[k]).read_text(encoding='utf-8') for k in ['handler','catalog','seeder','tests','evidence','memory','command']]
check('handler del scheduler existente', ' : ITaskHandler' in h and '[Scoped]' in h and 'Code => HandlerCode' in h)
check('doble activación fail closed', 'task.Status != TaskStatusEnum.Enabled' in h and 'task.Paused' in h and 'bool.TryParse(_configuration[ExecutionFlag]' in h)
check('parámetros con límites máximos', '"BatchSize", 25, 1, 100' in h and '"MaxScanned", 250, batchSize, 1000' in h)
check('selección no rastreada acotada', '_db.AchTransactions.AsNoTracking()' in h and '.Take(pageSize)' in h and '.Select(t => t.Id)' in h and 't.Id > lastTransactionId' in h)
check('filtro de ciclos vencidos por TZ', 'ClearingHouseOperationalTimeZone.Resolve(cycle)' in h and 'OperationalCycleWindowStatus.After' in h)
check('solo salientes determinadas pendientes', 'AchTransactionDirection.Outgoing' in h and 'AchTransactionOrigin.Cfa' in h and 'AchTransactionClassificationStatus.Determined' in h and 'AchTransferStateEnum.Pending' in h)
check('exclusiones de exportación y despacho', 'AchFileExportTransactions.Any' in h and 'ContrapartidaDispatchItems.Any' in h)
check('revalida por I02, no muta DbContext directo', '_command.ReassignAsync(id, cancellationToken)' in h and 'AchCycleId =' not in h and 'SaveChangesAsync' not in h)
check('maneja cancelaciones explícitamente', 'cancellationToken.ThrowIfCancellationRequested()' in h and 'catch (OperationCanceledException)' in h)
check('conserva resultado y fallos parciales visibles', 'trasladadas={moved}' in h and 'fallidas={failed}' in h and 'throw new InvalidOperationException' in h)
check('Quartz sin custom timers', 'Task.Delay(' not in h and 'BackgroundService' not in h and 'SemaphoreSlim' not in h)
check('catálogo público y no manual', '"ACH_PENDING_CYCLE_REASSIGNMENT"' in c and '"AchPendingCycleReassignment"' in c and 'false,\n            false,\n            5)' in c)
check('seeder Status Disabled', 'Code = "AchPendingCycleReassignment"' in s and 'Status = TaskStatusEnum.Disabled' in s and 'ManualExecutionEnabled = false' in s)
check('seeder sin retry ni recovery', 'RetryOnFailure = false' in s and 'RequestsRecovery = false' in s and 'MisfirePolicy = SchedulerMisfirePolicy.DoNothing' in s)
check('test cases 9+', t.count('[Fact]') >= 9 and 'UseInMemoryDatabase' in t)
check('preserva comando I02', 'IsolationLevel.Serializable' in cmd and 'AchTransactionStateEvents.Add' in cmd)
check('documenta riesgo y no go live', 'I05' in e and 'NO EJECUTADAS' in e and 'NO HABILITAR' in e)
check('memoria histórica I03 conservada y estado I03+', bool(re.search(r'^INCREMENT_CURRENT: I0[3-6]$', m, re.M)) and 'RISK-CR-001' in m and 'EVID-CR-I03-01' in m)
print(f'I03 ESTATICO {sum(ok for _,ok in checks)}/{len(checks)}')
sys.exit(0 if all(ok for _,ok in checks) else 1)
