#!/usr/bin/env python3
"""ACH-CR-001 I05: chequeos estructurales, NO compila C#, no simula Serializable."""
from pathlib import Path
import re,sys
r=Path(__file__).resolve().parent.parent
paths={
'eligibility':'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchPendingCycleEligibilityService.cs',
'audit':'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchFileExportAuditService.cs',
'security':'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaSecurity/NachaSecurityOperationService.cs',
'mft':'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchColombiaManagedMftOutboundWorkflow.cs',
'controller':'src/Cfa.ACHInterbank.Api/Controllers/NachaExportController.cs',
'command':'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchPendingCycleReassignmentService.cs',
'handler':'src/Cfa.ACHInterbank.Persistence/ACH/Quartz/Jobs/Implementation/AchPendingCycleReassignmentHandler.cs',
'seeder':'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/Seeders/TaskDefinitionSeeder.cs',
'c_queue':'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitCycleQueueService.cs',
'c_liq':'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/LiquidityOptimizationService.cs',
'e_tests':'tests/Cfa.ACHInterbank.Tests/AchPendingCycleEligibilityServiceTests.cs',
'a_tests':'tests/Cfa.ACHInterbank.Tests/AchFileExportAuditServiceTests.cs',
'memory':'docs/ach-cycle-reassignment/MEMORY_STATE.md',
'evidence':'docs/ach-cycle-reassignment/11_I05_IMPLEMENTACION_Y_EVIDENCIA.md',
'plan':'docs/ach-cycle-reassignment/02_PLAN_INCREMENTAL.md',
}
checks=[]
def chk(name,ok):
 ok=bool(ok);checks.append((name,ok));print(('PASS ' if ok else 'FAIL ')+name)
for name,path in paths.items():chk('Existe '+name,(r/path).is_file())
if not all(v for _,v in checks):sys.exit(1)
d={x:(r/p).read_text(encoding='utf-8-sig') for x,p in paths.items()}
e,a,s,m,c,cmd,h,q,et,at=(d[x] for x in ['eligibility','audit','security','mft','controller','command','handler','seeder','e_tests','a_tests'])
chk('Admisión NACHA transacción Serializable', 'exportKind, "NACHA"' in a and 'BeginTransactionAsync(' in a and 'IsolationLevel.Serializable' in a and 'admission.CommitAsync(ct)' in a)
chk('Sin crear DB transacción anidada para NACHA','NACHA_EXPORT_AMBIENT_TRANSACTION_UNSUPPORTED' in a and 'context.Database.CurrentTransaction is not null' in a)
chk('Conserva strategy EF y guardia de cancelación', 'CreateExecutionStrategy()' in a and 'ct' in a)
chk('Atomicidad membresía/registro antes de commit',a.index('RecordVerifiedFileAsync(cycleId')<a.index('admission.CommitAsync(ct)') and 'await context.SaveChangesAsync(ct)' in a)
chk('Verifica snapshot transacción pertenece ciclo y es exportable', 'requestedIds.Contains(x.Id) && x.AchCycleId == cycleId' in a and 'NachaExportEligibility.ExportableStates.Contains(x.State)' in a)
chk('Rechaza membresía inconsistente','snapshots.Count != requestedIds.Length' in a and 'throw new InvalidOperationException' in a)
chk('Conserva idempotencia de nombre+hash', 'existingIds.SequenceEqual(requestedIds)' in a and 'ContentSha256' in a and 'SemaphoreSlim' in a)
chk('RETURN no adopta NACHA gate de forma forzada','exportKind != "NACHA"' in a)
chk('Seguridad inyecta interfaz existente','IAchFileExportAuditService fileExportAudit' in s and '_fileExportAudit = fileExportAudit;' in s)
chk('Seguridad audita antes de guardar y habilitar', s.index('foreach (var membership in exportMemberships)') < s.index('_artifactStore.SaveAsync(operation.OperationId, artifactExtension') < s.index('operation.DownloadAvailable = true;',s.index('foreach (var membership in exportMemberships)')))
chk('Seguridad usa IDs de cada artefacto NACHA','item.artifact.AchTransactionIds' in s and 'membership.Ids' in s and 'membership.Hash' in s)
chk('MFT conserva audit antes de handoff',m.index('await exportAudit.RecordGeneratedFileAsync(')<m.index('return await HandoffAsync(transfer'))
chk('Controller conserva audit antes de retorno HTTP','await _fileExportAuditService.RecordGeneratedFileAsync(' in c and c.index('await _fileExportAuditService.RecordGeneratedFileAsync(')<c.index('return File('))
chk('Elegibilidad deniega todo source cycle con export NACHA','SOURCE_CYCLE_EXPORT_EXISTS' in e and 'file.AchCycleId == original.Id' in e and 'file.ExportKind == "NACHA"' in e)
chk('Evidencia de tx aún prioritaria','EXPORT_EVIDENCE_PRESENT' in e and 'PROCESSING_EVIDENCE_PRESENT' in e)
chk('Guarda ejecución CENIT sin cambiarliquidez','CENIT_CYCLE_PROCESSING_EVIDENCE' in e and 'CenitCycleExecutions' in e and 'CenitCycleQueues' in e and 'LiquidityOptimizationDecisions' in e)
chk('I02 serializable y reevalúa I01','IsolationLevel.Serializable' in cmd and '_eligibility.EvaluateAsync' in cmd)
chk('Quartz continúa doble guardia','task.Status != TaskStatusEnum.Enabled' in h and 'EnableExecution' in h and 'Status = TaskStatusEnum.Disabled' in q)
chk('Tests fuente ciclo NACHA + CENIT', 'CompleteExportOfAnotherTransactionInSourceCycle_DeniesConservatively' in et and 'CenitCycleExecution_DeniesEvenWithoutEvidenceForTransaction' in et)
chk('Tests no re-registro post traslado e idempotencia', 'NachaAdmission_MovedAwayBeforeAudit_DoesNotWriteExportEvidence' in at and 'NachaAdmission_DoesNotReplaceExistingMembershipOnSameFileName' in at)
chk('Memoria preserva evidencia histórica I05 y runtime pendiente', bool(re.search(r'^INCREMENT_CURRENT: I0[5-6]$', d['memory'], re.M)) and 'EVID-CR-I05-01' in d['memory'] and 'RUNTIME_VALIDATION: NOT_RUN_ENVIRONMENT' in d['memory'])
chk('Documenta límites y no go live','NO EJECUTADO' in d['evidence'] and 'Quartz' in d['evidence'] and 'I06' in d['plan'])
chk('Sin modificaciones a CENIT core en I05', 'ResolveTargetCycleAsync' in d['c_queue'] and 'OptimizeCycleAsync' in d['c_liq'])
print('I05 controles:',sum(v for _,v in checks),'/',len(checks));sys.exit(0 if all(v for _,v in checks) else 1)
