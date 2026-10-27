#!/usr/bin/env python3
"""I04 checks de composición estática. No compila C#/ejecuta red o EF."""
from pathlib import Path
import re
import sys
root=Path(__file__).resolve().parent.parent
paths={
    'batch':'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/BatchResolver.cs',
    'routing':'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/RoutingStrategyService.cs',
    'eligibility':'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchPendingCycleEligibilityService.cs',
    'command':'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchPendingCycleReassignmentService.cs',
    'handler':'src/Cfa.ACHInterbank.Persistence/ACH/Quartz/Jobs/Implementation/AchPendingCycleReassignmentHandler.cs',
    'seeder':'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/Seeders/TaskDefinitionSeeder.cs',
    'tests':'tests/Cfa.ACHInterbank.Tests/AchCycleCalendarProjectionTests.cs',
    'old_tests':'tests/Cfa.ACHInterbank.Tests/AchPendingCycleEligibilityServiceTests.cs',
    'docs':'docs/ach-cycle-reassignment/10_I04_IMPLEMENTACION_Y_EVIDENCIA.md',
    'memory':'docs/ach-cycle-reassignment/MEMORY_STATE.md',
    'plan':'docs/ach-cycle-reassignment/02_PLAN_INCREMENTAL.md',
}
checks=[]
def check(name,ok):
    checks.append((name,bool(ok)))
    print(('PASS ' if ok else 'FAIL ')+name)
for n,p in paths.items():
    check(f'Existe {n}',(root/p).is_file())
if not all((root/p).is_file() for p in paths.values()):sys.exit(1)
d={n:(root/p).read_text(encoding='utf-8') for n,p in paths.items()}
b,r,e,c,h,s,t,o,v,m,p=(d[x] for x in ['batch','routing','eligibility','command','handler','seeder','tests','old_tests','docs','memory','plan'])
check('Calendario del contrato existente', 'IOperationalCalendarService? calendar = null' in b and '_calendar.IsBusinessDayAsync(' in b)
check('Sin día valor arbitrario en originación', 'DateTime effectiveEntryDate = cycle.ProcessingDate.Date' in b and 'batch.EffectiveEntryDate = effectiveEntryDate' not in b)
check('Permite solo fecha FUTURA Before', 'window.LocalNow.Date < cycle.ProcessingDate.Date' in b and 'window.Status == OperationalCycleWindowStatus.Before' in b)
check('Mantiene mismo día Before rechazado', 'aún no está abierto para recepción' in b)
check('Mantiene After rechazado', 'está cerrado para recepción' in b)
check('Sin habilitar día inhábil', '!await _calendar.IsBusinessDayAsync(' in b and 'día no hábil' in b)
check('Protege estado Closed Cancelled en resolver', 'if (!isCenit && (cycle.OperationalStatus is AchCycleOperationalStatus.Closed or AchCycleOperationalStatus.Cancelled))' in b)
check('Routing omite Closed y Cancelled', 'x.Cycle.OperationalStatus is not AchCycleOperationalStatus.Closed' in r and 'and not AchCycleOperationalStatus.Cancelled' in r)
check('Routing reutiliza días hábiles', '_calendar.GetNextBusinessDayAsync(' in r)
check('Preserva CENIT Queue preexistente', 'MustQueueForTargetCycle = isCenit && isOutsideWindow' in b and 'QueueReason = isCenit && isOutsideWindow' in b)
check('Conserva política de tipos del ciclo', '_cycleTransactionPolicy.EvaluateAsync(' in b and 'if (!regulatoryDecision.IsAllowed)' in b)
check('I01 conserva misma fecha valor', 'cycle.ProcessingDate.Date == original.ProcessingDate.Date' in e and 'NO_ELIGIBLE_TARGET_SAME_VALUE_DATE' in e)
check('I01 conserva evidencia de exportación', 'EXPORT_EVIDENCE_PRESENT' in e and 'PROCESSING_EVIDENCE_PRESENT' in e)
check('I02 conserva cambio de cámara/fecha prohibido', 'targetCycle.ClearingHouseId != decision.ClearingHouseId' in c and 'targetCycle.ProcessingDate.Date != transaction.EffectiveEntryDate.Date' in c)
check('I02 auditoría idempotente', 'AchTransactionStateEvents.Add' in c and 'IdempotencyKey = key' in c)
check('I03 sigue bajo doble guardia', 'task.Status != TaskStatusEnum.Enabled' in h and 'bool.TryParse(_configuration[ExecutionFlag]' in h)
check('Seeder Quartz deshabilitado', 'Code = "AchPendingCycleReassignment"' in s and 'Status = TaskStatusEnum.Disabled' in s)
check('Pruebas de alta casos weekend/holiday/future fail closed', all(x in t for x in ['CreatedSaturday_ProjectedMonday','HolidayMonday_RoutedTuesday','SameDayBeforeCycleOpening_StillDenied','FutureDateMarkedNonBusiness_DeniedWithoutCreatingBatch','FutureCycleClosedOrCancelled_Denied','CycleCutoffExpired_StillDenied','FutureProjection_DoesNotBypassTypePolicy']))
check('Ocho escenarios de test nuevo declarados', t.count('[Fact]') >= 6 and t.count('[InlineData(') >= 2 and 'UseInMemoryDatabase' in t)
check('Último ciclo y non-business en test I01+', 'LastEligibleCycleExpired_DoesNotChangeValueDateOrSelectNextDay' in o and 'SourceCycleOnNonBusinessDay_DeniesRegardlessOfUpcomingCycles' in o)
check('Cero cambios de fecha valor en I04', 'EffectiveEntryDate =' not in e and 'transaction.EffectiveEntryDate =' not in c)
check('Memoria I04->I05', bool(re.search(r'^INCREMENT_CURRENT: I0[4-6]$',m,re.M)) and 'EVID-CR-I04-01' in m and 'RISK-CR-001' in m)
check('Plan I05 siguiente, I04 documentado', 'I04 — Fin de jornada / día no hábil / fecha valor [IMPLEMENTADO ESTÁTICAMENTE]' in p and ('I05 — Guardias de regresión de exportación y CENIT [SIGUIENTE]' in p or 'I05 — Guardias de regresión de exportación y CENIT [IMPLEMENTADO ESTÁTICAMENTE]' in p))
check('Evidencia mantiene runtime pendiente', 'NO EJECUTAD' in v.upper() and 'I05' in v and 'NO se mueve a otro día'.lower() in v.lower())
check('No scheduler/DB schema nuevo', 'ALTER TABLE' not in b and 'DbSet<' not in t and 'BackgroundService' not in b)
print(f'I04 ESTATICO {sum(x for _,x in checks)}/{len(checks)}')
sys.exit(0 if all(ok for _,ok in checks) else 1)
