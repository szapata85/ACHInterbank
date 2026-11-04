#!/usr/bin/env python3
"""I04-CENIT: controles estáticos exhaustivos de fuente, nunca sustituyen build/xUnit/BD/homologación."""
from pathlib import Path
import re
r=Path(__file__).resolve().parents[2]
p={
'entity':'src/Cfa.ACHInterbank.Domain/Models/ACH/CenitReturnPreparation.cs',
'dto':'src/Cfa.ACHInterbank.Application/ACH/Models/CenitReturnReviewModels.cs',
'contract':'src/Cfa.ACHInterbank.Application/ACH/Interfaces/ICenitReturnReviewService.cs',
'service':'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitReturnReviewService.cs',
'controller':'src/Cfa.ACHInterbank.Api/Controllers/CenitReturnReviewController.cs',
'config':'src/Cfa.ACHInterbank.Persistence/Configuration/CenitReturnPreparationConfiguration.cs',
'db':'src/Cfa.ACHInterbank.Persistence/DataBase/AchDbContext.cs',
'di':'src/Cfa.ACHInterbank.Persistence/DependencyInjectionService.cs',
'msql':'src/Cfa.ACHInterbank.Persistence.Migrations.SqlServer/DataBase/Migrations/SqlServer/20261103143000_CenitReturnReviewI04.cs',
'mpg':'src/Cfa.ACHInterbank.Persistence/DataBase/Migrations/Postgres/20261103143000_CenitReturnReviewI04.cs',
'ssql':'src/Cfa.ACHInterbank.Persistence.Migrations.SqlServer/DataBase/Migrations/SqlServer/AchDbContextModelSnapshot.cs',
'spg':'src/Cfa.ACHInterbank.Persistence/DataBase/Migrations/Postgres/AchDbContextModelSnapshot.cs',
'prev':'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitReturnPreparationService.cs',
'test':'tests/Cfa.ACHInterbank.Tests/CenitReturnReviewI04Tests.cs',
}
t={k:(r/v).read_text(encoding='utf-8-sig') for k,v in p.items()}; c=t['controller']; s=t['service']; tests=t['test']; ck=[]
def assertcheck(name,ok): ck.append((name,bool(ok)))
assertcheck('modelo sigue siendo independiente CENIT', 'class CenitReturnPreparation' in t['entity'])
assertcheck('estado preexistente conserva bloqueo concurrencia', 'IsConcurrencyToken()' in t['config'])
assertcheck('campo auditor revisor', 'ReviewedByUserId' in t['entity'])
assertcheck('marca temporal de decisión', 'ReviewedAtUtc' in t['entity'])
assertcheck('justificación persistente', 'DecisionJustification' in t['entity'])
assertcheck('referencia de evidencia persistente', 'DecisionEvidenceReference' in t['entity'])
assertcheck('config actor longitud', '.HasMaxLength(128)' in t['config'])
assertcheck('config justificación longitud', '.HasMaxLength(500)' in t['config'])
assertcheck('config referencia longitud', '.HasMaxLength(120)' in t['config'])
assertcheck('nuevo contrato CENIT', 'ICenitReturnReviewService' in t['contract'])
assertcheck('DTO requiere estado esperado', 'ExpectedStatus' in t['dto'])
assertcheck('DTO incluye justificación', 'Justification' in t['dto'])
assertcheck('DTO incluye evidencia', 'EvidenceReference' in t['dto'])
assertcheck('no DTO de números de cuentas', 'AccountNumber' not in t['dto'])
assertcheck('actor NO proviene del payload', 'authenticatedActorId' in t['contract'] and 'ActorId' not in t['dto'])
assertcheck('DI servicio independiente', 'AddScoped<ICenitReturnReviewService, CenitReturnReviewService>()' in t['di'])
assertcheck('API CENIT', '[Route("api/cenit/devoluciones-automaticas/revisiones")]' in c)
assertcheck('API autenticada', '[Authorize]' in c)
assertcheck('permiso lectura', '[Authorize(Policy = P1Policies.ConfigRead)]' in c)
assertcheck('permiso decisión autorizado', '[Authorize(Policy = P1Policies.ConfigManage)]' in c)
assertcheck('actor desde claims autenticados', 'FindFirst(ClaimTypes.NameIdentifier)' in c and 'IsAuthenticated' in c)
assertcheck('sin identidad HTTP la decisión es denegada', 'return Forbid()' in c)
assertcheck('HTTP 400 para inválidos', 'BadRequest(' in c)
assertcheck('HTTP 409 para conflictos', 'Conflict(' in c)
assertcheck('GET revisión y excepciones sólo consulta', '[HttpGet("excepciones")]' in c and 'REVISION_SOLICITUD' in c)
assertcheck('excepciones no usa POST de autorización', c.count('[HttpPost(')==1)
assertcheck('lista sólo CENIT', 'RegulatoryCycleScheduleCatalog.CenitCode' in s)
assertcheck('únicos estados terminales', 'APROBADA_SIN_ENVIO' in s and 'DESCARTADA' in s)
assertcheck('requiere estado esperado', 'decision.ExpectedStatus != CenitReturnPreparationService.Prepared' in s)
assertcheck('única transición', 'candidate.Status != CenitReturnPreparationService.Prepared' in s)
assertcheck('auditoría de primera persona del INSERT', 'EntityName == nameof(CenitReturnPreparation)' in s and 'x.BeforeJson == null' in s)
assertcheck('identidad de preparador no system', 'audit.ChangedBy.Equals("system"' in s)
assertcheck('diferente preparador y revisor', 'actorId, audit.ChangedBy' in s)
assertcheck('actor inválido bloqueado', 'authenticatedActorId.Equals("system"' in s)
assertcheck('justificación validada', 'Justification.Trim().Length is < 15 or > 500' in s)
assertcheck('soporte obligatorio para aprobar', 'decision.Action == "APROBAR" && string.IsNullOrWhiteSpace(decision.EvidenceReference)' in s)
assertcheck('estado modo controlado', 'settings.Mode is not' in s)
assertcheck('descarte aún con modo desactivado', 'if (decision.Action == "APROBAR")' in s and 'ValidateApprovalAsync' in s)
assertcheck('transacción serializable BD relacional', 'IsolationLevel.Serializable' in s and 'BeginTransactionAsync' in s)
assertcheck('auditoría en SaveChanges', 'context.SaveChangesAsync(ct)' in s)
assertcheck('conflicto concurrencia', 'DbUpdateConcurrencyException' in s)
assertcheck('verifica última ejecución', 'OrderByDescending(x => x.StartedAtUtc)' in s and 'execution.Id != p.SourceExecutionId' in s)
assertcheck('compara ID de cola', 'execution.DispatchQueueId != p.SourceQueueId' in s)
assertcheck('recalcula causal CENIT', 'CenitPassiveReturnDetector.Evaluate' in s)
assertcheck('solo indicio ordinario', 'finding.Classification != "INDICIO_CAUSAL_CENIT"' in s)
assertcheck('comparación de causal', 'finding.VerifiedReasonCode != p.ReasonCode' in s)
assertcheck('bloqueo explícito R06 R23 R34', 'p.ReasonCode is "R06" or "R23" or "R34"' in s)
assertcheck('bloqueo prenotificaciones', 'original.IsPrenotification' in s)
assertcheck('bloqueo devoluciones ya generadas', 'context.AchReturnsGenerated' in s)
assertcheck('bloqueo festivos', 'context.BankHolidays' in s)
assertcheck('revalida siguiente ciclo CENIT', 'CenitReturnPreparationWindow.Resolve' in s)
assertcheck('valida Fecha Valor', 'OriginalValueDate.Date' in s and 'SuggestedValueDate.Date' in s)
assertcheck('no invoca reglas ACH Colombia', 'AchColombiaReturnPreparationWindow' not in s)
assertcheck('no motor ni transmisión', not any(word in s for word in ('GenerateReturnsFileAsync(', 'TransmitAsync(', 'AchReturnsGenerated.Add(', 'BackgroundService', 'IHostedService')))
assertcheck('sin efecto procesador conectado', 'ProcessingConnected: false' in s)
assertcheck('migración SQL reversible', 'DropColumn' in t['msql'] and t['msql'].count('AddColumn')==4)
assertcheck('migración PG reversible', 'DropColumn' in t['mpg'] and t['mpg'].count('AddColumn')==4)
assertcheck('migraciones solo CENIT', all('table: "CenitReturnPreparations"' in t[k] and 'AchColombiaReturnPreparations' not in t[k] for k in ('msql','mpg')))
assertcheck('snapshots sin pérdida CENIT', all('CenitReturnPreparation' in t[k] and 'DecisionEvidenceReference' in t[k] and 'ReviewedByUserId' in t[k] for k in ('ssql','spg')))
assertcheck('prueba de doble operador', 'DifferentAuthenticatedPersonApprovesOrdinaryValidPreparationWithoutSending' in tests)
assertcheck('prueba separación personas', 'SamePreparerCannotApproveOrDiscard' in tests)
assertcheck('prueba preparación sin identidad', 'NoReliablePreparerIdentityCannotBeApproved' in tests)
assertcheck('prueba descarte terminal', 'DiscardRemainsAvailableWhenModeDisabledAndIsTerminal' in tests)
assertcheck('prueba cambio de configuración y ventana', 'ApprovalFailsIfModeDisabledOrTargetWindowChanged' in tests)
assertcheck('prueba evidencia posterior y R23', 'ApprovalFailsOnNewerSuccessfulEvidenceAndSpecialCausals' in tests)
assertcheck('prueba petición malformada', 'RejectInvalidDecisionsWithoutAnyWrite' in tests)
assertcheck('prueba sin claims', 'UnauthenticatedOrSpoofedIdentityNeverDecides' in tests)
assertcheck('prueba excepciones sin escritura', 'ReadOnlyExceptionRouteDoesNotCreateReservations' in tests)
assertcheck('prueba token concurrency', 'ModelConcurrencyAndBothDatabaseMigrationsAreDeclared' in tests)
assertcheck('sin cambios ACH Colombia en persistencia I04', 'AchColombiaReturnPreparation' not in t['entity'] and 'AchColombiaReturnPreparation' not in s)
assertcheck('sin escritura de políticas ACH Colombia', 'AutomaticReturnMode = ' not in s)
fail=[name for name,ok in ck if not ok]
for name,ok in ck: print(('OK' if ok else 'FAIL')+' - '+name)
print(f'I04-CENIT: {len(ck)-len(fail)}/{len(ck)} controles estáticos')
if fail: raise SystemExit(1)
