#!/usr/bin/env python3
"""I03-CENIT: comprobaciones estáticas de fuente, NO build, xUnit o prueba SQL."""
from pathlib import Path
import re
root = Path(__file__).resolve().parents[2]
paths = {
 'service':'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitReturnPreparationService.cs',
 'domain':'src/Cfa.ACHInterbank.Domain/Models/ACH/CenitReturnPreparation.cs',
 'config':'src/Cfa.ACHInterbank.Persistence/Configuration/CenitReturnPreparationConfiguration.cs',
 'models':'src/Cfa.ACHInterbank.Application/ACH/Models/CenitReturnPreparationModels.cs',
 'interface':'src/Cfa.ACHInterbank.Application/ACH/Interfaces/ICenitReturnPreparationService.cs',
 'controller':'src/Cfa.ACHInterbank.Api/Controllers/CenitReturnPreparationController.cs',
 'tests':'tests/Cfa.ACHInterbank.Tests/CenitReturnPreparationI03Tests.cs',
 'db':'src/Cfa.ACHInterbank.Persistence/DataBase/AchDbContext.cs',
 'di':'src/Cfa.ACHInterbank.Persistence/DependencyInjectionService.cs',
 'sql':'src/Cfa.ACHInterbank.Persistence.Migrations.SqlServer/DataBase/Migrations/SqlServer/20261103081000_CenitReturnPreparationI03.cs',
 'pg':'src/Cfa.ACHInterbank.Persistence/DataBase/Migrations/Postgres/20261103081000_CenitReturnPreparationI03.cs',
 'sqlsnap':'src/Cfa.ACHInterbank.Persistence.Migrations.SqlServer/DataBase/Migrations/SqlServer/AchDbContextModelSnapshot.cs',
 'pgsnap':'src/Cfa.ACHInterbank.Persistence/DataBase/Migrations/Postgres/AchDbContextModelSnapshot.cs',
}
t={k:(root/p).read_text(encoding='utf-8-sig') for k,p in paths.items()}
checks=[]
def check(desc, result): checks.append((desc,bool(result)))
s=t['service']; c=t['controller']; tests=t['tests']
check('servicio propio CENIT', 'class CenitReturnPreparationService' in s)
check('modelo propio CENIT', 'class CenitReturnPreparation' in t['domain'])
check('DTO demanda transacción y evidencia de ejecución', 'ExpectedExecutionId' in t['models'] and 'OriginalTransactionId' in t['models'])
check('contrato propio', 'interface ICenitReturnPreparationService' in t['interface'])
check('DI registrado', 'AddScoped<ICenitReturnPreparationService, CenitReturnPreparationService>()' in t['di'])
check('DbSet CENIT propio', 'DbSet<CenitReturnPreparation> CenitReturnPreparations' in t['db'])
check('GET y POST solamente', '[HttpGet]' in c and '[HttpPost]' in c and '[HttpPut]' not in c and '[HttpDelete]' not in c)
check('ruta CENIT única', '[Route("api/cenit/devoluciones-automaticas/preparaciones")]' in c)
check('autenticación declarativa', '[Authorize]' in c)
check('GET permiso lectura', '[Authorize(Policy = P1Policies.ConfigRead)]' in c)
check('POST permiso configuración', '[Authorize(Policy = P1Policies.ConfigManage)]' in c)
check('POST no es aprobación', 'APROBAR' not in c and 'Generate' not in c)
check('validación de entrada HTTP', 'BadRequest' in c and 'ArgumentException' in c)
check('conflicto HTTP 409', 'Conflict(' in c and 'CenitReturnPreparationConflictException' in c)
check('guardia modo desactivado', 'settings.Mode is not' in s and 'WithApproval' in s and 'Automatic' in s)
check('configuración real I01', 'CenitAutomaticReturnSettingsService(context).GetAsync' in s)
check('filtro cámara CENIT por código', 'RegulatoryCycleScheduleCatalog.CenitCode' in s)
check('sin reglas ACH Colombia', 'AchColombiaReturnPreparationWindow' not in s and 'AchColombiaReturnPreparationService' not in s)
check('reconsulta última ejecución', 'OrderByDescending(x => x.StartedAtUtc)' in s and 'ThenByDescending(x => x.Id)' in s)
check('compara ID evidencia vista por operador', 'execution.Id != request.ExpectedExecutionId' in s)
check('revalida EVALUATE I02', 'CenitPassiveReturnDetector.Evaluate' in s)
check('admite solo indicio causal ordinario', 'finding.Classification != "INDICIO_CAUSAL_CENIT"' in s)
check('cataloga por cámara', 'x.ClearingHouseId == houseId && x.IsActive' in s)
check('no devuelve ya generado', 'AchReturnsGenerated.AsNoTracking()' in s)
check('no duplicación en aplicación', 'CenitReturnPreparations.AsNoTracking()' in s and 'AnyAsync(x => x.OriginalTransactionId' in s)
check('serializable en BD real', 'IsolationLevel.Serializable' in s and 'BeginTransactionAsync' in s)
check('falla cerrada ante unique concurrente', 'IsUniqueViolation' in s and 'CenitReturnPreparationConflictException' in s)
check('firma de auditoría por SaveChanges', 'context.SaveChangesAsync(ct)' in s and 'AuditLogs' in s)
check('Fecha Valor real', 'original.EffectiveEntryDate.Date' in s and 'target.ProcessingDate.Date' in s)
check('siguiente sesión del mismo día', 'number.Value + 1' in s and 'source.ProcessingDate.Date != localNow.Date' in s)
check('límite ciclo no superado', 'localNow.TimeOfDay >= target.CutoffTime' in s)
check('comparación con agenda CENIT', 'GetRequired(RegulatoryCycleScheduleCatalog.CenitCode)' in s)
check('requiere ciclo original cerrado', 'AchCycleOperationalStatus.Closed' in s)
check('siguiente ciclo abierto/programado', 'AchCycleOperationalStatus.Scheduled or AchCycleOperationalStatus.Open' in s)
check('requiere política activa', '!policy.IsActive || !policy.AllowsReturn' in s)
check('bloquea fechas futuras y cruces de valor', 'original.EffectiveEntryDate.Date != source.ProcessingDate.Date' in s)
check('bloquea fines de semana y festivos', 'DayOfWeek.Saturday or DayOfWeek.Sunday' in s and 'context.BankHolidays' in s)
check('fuera de horario no crea candidatos tardíos', 'number.Value is < 1 or > 4' in s and 'successors.Length != 1' in s)
check('prenotificación y solicitudes no se preparan', 'original.IsPrenotification' in s and 'INDICIO_CAUSAL_CENIT' in s)
check('solo lectura no registra cambios', 'AsNoTracking()' in s and 'ListAsync' in s)
check('DTO no expone datos bancarios', 'AccountNumber' not in t['models'] and 'RecipientId' not in t['models'])
check('respuesta desconectada', 'ProcessingConnected: false' in s)
check('ni generación NACHA ni transporte', not any(x in s for x in ('TransmitAsync(', 'GenerateReturnsFileAsync(', 'AchReturnsGenerated.Add(', 'IHostedService', 'Quartz', 'NachaFileBuilder')))
check('unique original EF', 'HasIndex(x => x.OriginalTransactionId).IsUnique()' in t['config'])
check('unique origen ejecución EF', 'HasIndex(x => x.SourceExecutionId).IsUnique()' in t['config'])
check('referencias EF restrictivas', t['config'].count('DeleteBehavior.Restrict') == 2)
check('migraciones SQL y PG', 'CreateTable(name: "CenitReturnPreparations"' in t['sql'] and 'CreateTable(name: "CenitReturnPreparations"' in t['pg'])
check('indices SQL y PG', all('UX_CenitReturnPreparation_Original' in t[k] and 'UX_CenitReturnPreparation_Execution' in t[k] for k in ('sql','pg')))
check('FK a evidencia SQL y PG', all('IncomingNachaIntegrationExecution' in t[k] for k in ('sql','pg')))
check('model snapshots ambos', all('CenitReturnPreparation' in t[k] for k in ('sqlsnap','pgsnap')))
check('test estado DESACTIVADO', 'DisabledModeNeverPersistsEvenWithRealConfirmedEvidence' in tests)
check('test modos y auditoría', 'ExplicitPreparationIsAuditedAndIdempotentWithoutFinancialSideEffects' in tests)
check('test R06 y ejecución obsoleta', 'StaleExecutionAndSpecialCausesNeverBecomePreparations' in tests)
check('test resultado posterior', 'LaterSuccessfulExecutionInvalidatesOldRejection' in tests)
check('test cámara y Fecha Valor', 'WrongHouseAndWrongDateAndExpiredWindowFailClosed' in tests)
check('test festivo y retorno existente', 'MissingOrClosedTargetCycleHolidayAndGeneratedReturnAreBlocked' in tests)
check('test permisos', 'ControllerEnforcesPermissionsAndOnlyExposesPreparationAndList' in tests)
failed=[name for name,okay in checks if not okay]
for name,okay in checks:
 print(f"{'OK' if okay else 'FAIL'} - {name}")
print(f'RESULTADO ESTATICO I03-CENIT: {len(checks)-len(failed)}/{len(checks)}')
if failed: raise SystemExit(1)
