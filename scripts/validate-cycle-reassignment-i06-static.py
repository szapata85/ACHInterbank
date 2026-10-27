#!/usr/bin/env python3
"""ACH-CR-001 I06: validacion estructural de cierre. NO compila C#/ejecuta EF/Quartz."""
from pathlib import Path
import re
import sys
r = Path(__file__).resolve().parent.parent
p = {
    'memory': 'docs/ach-cycle-reassignment/MEMORY_STATE.md',
    'changelog': 'docs/ach-cycle-reassignment/CHANGELOG.md',
    'start': 'docs/ach-cycle-reassignment/00_START_HERE.md',
    'arch': 'docs/ach-cycle-reassignment/01_ARQUITECTURA.md',
    'plan': 'docs/ach-cycle-reassignment/02_PLAN_INCREMENTAL.md',
    'rules': 'docs/ach-cycle-reassignment/03_REGLAS_Y_ESCENARIOS.md',
    'valid': 'docs/ach-cycle-reassignment/04_VALIDACION_ESTATICA.md',
    'guide': 'docs/ach-cycle-reassignment/05_GUIA_DE_CHAT.md',
    'trace': 'docs/ach-cycle-reassignment/06_TRAZABILIDAD.md',
    'auditdoc': 'docs/ach-cycle-reassignment/12_I06_AUDITORIA_Y_CIERRE.md',
    'homologdoc': 'docs/ach-cycle-reassignment/13_PLAN_HOMOLOGACION_GO_NO_GO.md',
    'app_eligible': 'src/Cfa.ACHInterbank.Application/ACH/Interfaces/IAchPendingCycleEligibilityService.cs',
    'app_assign': 'src/Cfa.ACHInterbank.Application/ACH/Interfaces/IAchPendingCycleReassignmentService.cs',
    'eligible': 'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchPendingCycleEligibilityService.cs',
    'command': 'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchPendingCycleReassignmentService.cs',
    'export': 'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchFileExportAuditService.cs',
    'security': 'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaSecurity/NachaSecurityOperationService.cs',
    'batch': 'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/BatchResolver.cs',
    'quartz': 'src/Cfa.ACHInterbank.Persistence/ACH/Quartz/Jobs/Implementation/AchPendingCycleReassignmentHandler.cs',
    'catalog': 'src/Cfa.ACHInterbank.Persistence/Scheduler/SchedulerTaskCatalog.cs',
    'seeder': 'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/Seeders/TaskDefinitionSeeder.cs',
    'i05validator': 'scripts/validate-cycle-reassignment-i05-static.py',
    'i01tests': 'tests/Cfa.ACHInterbank.Tests/AchPendingCycleEligibilityServiceTests.cs',
    'i02tests': 'tests/Cfa.ACHInterbank.Tests/AchPendingCycleReassignmentServiceTests.cs',
    'i03tests': 'tests/Cfa.ACHInterbank.Tests/AchPendingCycleReassignmentHandlerTests.cs',
    'i04tests': 'tests/Cfa.ACHInterbank.Tests/AchCycleCalendarProjectionTests.cs',
    'i05tests': 'tests/Cfa.ACHInterbank.Tests/AchFileExportAuditServiceTests.cs'
}
checks=[]
def test(title, result):
    okay=bool(result)
    checks.append((title,okay))
    print(('PASS' if okay else 'FAIL')+' '+title)
for key,path in p.items():
    test('Ruta '+key, (r/path).is_file())
if not all(x[1] for x in checks):
    print('I06: fallo archivos inexistentes'); sys.exit(1)
s={key:(r/path).read_text(encoding='utf-8-sig') for key,path in p.items()}
m=s['memory']; q=s['quartz']; se=s['seeder']; c=s['command']; e=s['eligible']; a=s['export']
test('Memoria indica ZIP I06 o parche verificable',
     'CURRENT_ZIP: ACHInterbank_ACH_CR_I06_COMPLETO.zip' in m
     or ('CURRENT_ZIP: ACHInterbank_ACH_CR_I06_FIX01_COMPLETO.zip' in m
         and 'PATCH_CURRENT: I06-FIX01' in m
         and 'EVID-CR-I06-FIX01-01' in m))
test('Memoria I06 + NONE',bool(re.search(r'^INCREMENT_CURRENT: I06$',m,re.M)) and bool(re.search(r'^NEXT_INCREMENT: NONE$',m,re.M)))
test('Estado ejecutable transparente', all(x in m for x in ('RUNTIME_VALIDATION: NOT_RUN_ENVIRONMENT','UAT_STATE: NOT_STARTED','PRODUCTION_STATE: NOT_READY')))
test('Riesgos e historial conservados',all(f'EVID-CR-I0{i}-01' in m for i in range(1,7)) and 'RISK-CR-010' in m)
test('I06 referencia checklist', '13_PLAN_HOMOLOGACION_GO_NO_GO.md' in m and 'I06' in s['changelog'])
test('Cierre NO-GO', 'NO-GO' in s['start'] and 'TESTS_NOT_EXECUTED' in s['homologdoc'])
test('No nuevos incrementos', 'Incrementos adicionales planificados: 0' in s['plan'] and '**0 incrementos pendientes**' in m)
test('Plan ensayos reales', all('H%02d'%i in s['homologdoc'] for i in range(1,15)))
test('Plan no habilita Quartz', 'Disabled' in s['homologdoc'] and 'AchCycleReassignment:EnableExecution=false' in s['homologdoc'])
test('Riesgos SQL/multiarchivo', all(word in s['homologdoc'] for word in ('SQL Server','PostgreSQL','segunda partición','ACK')))
test('Documenta ejecución no probada', 'dotnet' in s['auditdoc'] and 'NO-GO' in s['auditdoc'])
test('Contratos Application estables', 'interface IAchPendingCycle' in s['app_eligible'] and 'interface IAchPendingCycle' in s['app_assign'])
test('Eligibility sin escrituras', '.AsNoTracking()' in e and 'SaveChangesAsync(' not in e and 'NO_ELIGIBLE_TARGET_SAME_VALUE_DATE' in e)
test('I02 transacción atómica indicada', 'BeginTransactionAsync(IsolationLevel.Serializable' in c and 'CommitAsync(ct)' in c and 'AchTransactionStateEvents.Add' in c)
test('I02 mantiene mismo día/cámara', 'targetCycle.ClearingHouseId != decision.ClearingHouseId' in c and 'targetCycle.ProcessingDate.Date != transaction.EffectiveEntryDate.Date' in c)
test('Exportador Serializable exacto', 'IsolationLevel.Serializable' in a and 'requestedIds.Contains(x.Id) && x.AchCycleId == cycleId' in a)
test('Seguridad audita antes de publicar', s['security'].index('foreach (var membership in exportMemberships)') < s['security'].index('_artifactStore.SaveAsync(operation.OperationId, artifactExtension'))
test('CENIT conservador', 'CENIT_CYCLE_PROCESSING_EVIDENCE' in e and 'CenitCycleExecutions' in e and 'CenitCycleQueues' in e)
test('Bloqueo ciclo fuente con NACHA', 'SOURCE_CYCLE_EXPORT_EXISTS' in e and 'file.ExportKind == "NACHA"' in e)
test('Proyección futura sin cambio valor', 'window.LocalNow.Date < cycle.ProcessingDate.Date' in s['batch'] and 'DateTime effectiveEntryDate = cycle.ProcessingDate.Date' in s['batch'])
test('Tarea Disabled de fábrica y doble guard', 'Status = TaskStatusEnum.Disabled' in se and 'task.Status != TaskStatusEnum.Enabled' in q and 'EnableExecution' in q)
test('Sin retry ni recovery automaticos', 'RetryOnFailure = false' in se and 'RequestsRecovery = false' in se)
test('Seeder texto final no falso pendiente', 'las reasigna al siguiente ciclo elegible; I05 pendiente.' not in se)
test('Handler documenta no homologación', 'concurrencia relacional aún no está homologada' in q)
test('I05 validador acepta historia I06', 'INCREMENT_CURRENT: I0[5-6]' in s['i05validator'] and 'EVID-CR-I05-01' in s['i05validator'])
test('Tests por incremento existentes', all('Fact' in s[x] or 'Theory' in s[x] for x in ('i01tests','i02tests','i03tests','i04tests','i05tests')))
test('Pruebas estáticas documentadas', 'validate-cycle-reassignment-i06-static.py' in s['valid'] and 'validate-solution-projects.py' in s['valid'])
test('Trazabilidad y guía continuidad', 'I06' in s['trace'] and 'MEMORY_STATE.md' in s['guide'] and 'ACHInterbank_ACH_CR_I06_COMPLETO.zip' in s['guide'])
count=sum(ok for _,ok in checks)
print(f'I06 ESTATICO: {count}/{len(checks)} controles. NO se ejecutaron .NET/EF/Quartz/MFT/UAT.')
sys.exit(0 if count==len(checks) else 1)
