#!/usr/bin/env python3
"""I06-CENIT: invariantes de fuente (NO equivalen a .NET/xUnit/SQL/homologación)."""
from pathlib import Path
r=Path(__file__).resolve().parents[2]
files={
 'dto':'src/Cfa.ACHInterbank.Application/ACH/Models/CenitReturnIntegrationModels.cs',
 'interface':'src/Cfa.ACHInterbank.Application/ACH/Interfaces/ICenitReturnIntegrationReadService.cs',
 'service':'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitReturnIntegrationReadService.cs',
 'controller':'src/Cfa.ACHInterbank.Api/Controllers/CenitReturnIntegrationController.cs',
 'di':'src/Cfa.ACHInterbank.Persistence/DependencyInjectionService.cs',
 'tests':'tests/Cfa.ACHInterbank.Tests/CenitReturnIntegrationI06Tests.cs',
 'i05':'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitReturnArtifactService.cs',
 'i04':'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitReturnReviewService.cs',
 'preparation':'src/Cfa.ACHInterbank.Domain/Models/ACH/CenitReturnPreparation.cs',
 'snapshot':'src/Cfa.ACHInterbank.Persistence/DataBase/Migrations/Postgres/AchDbContextModelSnapshot.cs',
}
t={k:(r/v).read_text(encoding='utf-8-sig') for k,v in files.items()}
s=t['service'];c=t['controller'];d=t['dto'];tests=t['tests'];checks=[]
def ck(name,ok): checks.append((name,bool(ok)))
for key in ('dto','interface','service','controller','tests'):
 ck(f'Archivo I06 {key} creado', (r/files[key]).is_file())
ck('Contrato solo lectura', 'GetAsync(' in t['interface'] and 'ListAsync(' in t['interface']
   and not any(x in t['interface'] for x in ('TransmitAsync', 'SendAsync', 'ReconcileWriteAsync')))
ck('DTO no habilita transmisión', 'TransmissionEnabled = false' in d)
ck('DTO no atribuye acuse', 'ChamberAcknowledgementAttributed = false' in d)
ck('DTO no declara conciliado', 'FinanciallyReconciled = false' in d)
ck('Servicio implementa contrato propio', 'ICenitReturnIntegrationReadService' in s)
ck('DI registra servicio I06', 'AddScoped<ICenitReturnIntegrationReadService, CenitReturnIntegrationReadService>()' in t['di'])
ck('Endpoint único de conciliación CENIT', '[Route("api/cenit/devoluciones-automaticas/conciliacion")]' in c)
ck('Autenticación general', '[Authorize]' in c)
ck('GET único', c.count('[HttpGet')==2)
ck('Ambos GET protegidos ConfigRead', c.count('[Authorize(Policy = P1Policies.ConfigRead)]')==2)
ck('Sin endpoints de mutación', not any(x in c for x in ('HttpPost', 'HttpPut', 'HttpDelete', 'HttpPatch')))
ck('Sin descarga de contenido desde I06', 'return File(' not in c and 'FileContentResult' not in c)
ck('Respuesta no almacenable en caché', c.count('no-store, private')==2)
ck('Límite de consulta 1..100', 'maxItems is < 1 or > 100' in c and 'maxItems is < 1 or > 100' in s)
ck('Truncamiento explícito', 'Take(maxItems + 1)' in s and 'rows.Count > maxItems' in s)
ck('Búsqueda filtrada por código CENIT', 'RegulatoryCycleScheduleCatalog.CenitCode' in s)
ck('No usa modo ACH Colombia', 'AchColombia' not in s)
ck('Lectura de modo CENIT', 'context.ClearingHouseConfigs.AsNoTracking()' in s and 'AutomaticReturnMode' in s)
ck('Modo desactivado no marca conciliación', 'MODO_CENIT_DESACTIVADO' in s)
ck('Inspect after expiry sin ValidateApproval', 'ValidateApprovalAsync' not in s)
ck('Carga sin tracking', 'CenitReturnPreparations.AsNoTracking()' in s)
ck('Verifica cifra con mismo propósito I05', 'Cfa.ACHInterbank.CENIT.Returns.I05.LocalArtifact.v1' in s and 'Cfa.ACHInterbank.CENIT.Returns.I05.LocalArtifact.v1' in t['i05'])
ck('Desprotección sólo interna', '_protector.Unprotect(' in s and 'byte[]' not in d)
ck('Verifica nombre local NO_TRANSMITIR', 'NO_TRANSMITIR_CENIT_I05_' in s)
ck('Perfil oficial CENIT exclusivo', 'CenitReturnOut2026Layout.ProfileCode' in s)
ck('Longitud 106x10', 'value.Length != 1060' in s)
ck('Hash SHA-256', 'SHA256.HashData' in s and 'hash, p.ArtifactSha256' in s)
ck('Detecta corrupción de clave', 'catch (CryptographicException)' in s)
ck('Detecta estructura 1/5/6/7/8/9', 'records.Take(6)' in s)
ck('Verifica adenda 99', 'records[3].Substring(1, 2)' in s)
ck('Verifica causal propia', 'records[3].Substring(3, 3) != p.ReasonCode' in s)
ck('Verifica original trace', 'records[3].Substring(6, 15) != p.OriginalTransaction.TraceNumber' in s)
ck('Verifica trace nuevo en 6 y 7', 'records[2].Substring(87, 15)' in s and 'records[3].Substring(81, 15)' in s)
ck('Verifica importe original', 'p.OriginalTransaction.Amount * 100m' in s)
ck('Cruce con devolución manual existente', 'context.AchReturnsGenerated.AsNoTracking()' in s)
ck('Cruce con exportaciones por traza CENIT', 'context.AchFileExportTransactions.AsNoTracking()' in s and 'x.AchFileExport.ClearingHouseId == houseId' in s)
ck('Cruce respuestas CENIT aislado', 'context.CenitChamberResponses.AsNoTracking()' in s and 'x.ClearingHouseId == houseId' in s)
ck('Acuse externo nunca atribuido a I05', 'RESPUESTA_EXTERNA_NO_ATRIBUIBLE_A_I05' in s and 'SIN_ACUSE_ATRIBUIBLE_I05' in s)
ck('Sin nombre externo homologado', 'SIN_NOMBRE_EXTERNO_HOMOLOGADO' in s)
ck('Sin canal financiero', 'SIN_TRANSPORTE_I06' in s)
ck('Sin conciliación financiera', 'SIN_CONCILIACION_FINANCIERA' in s)
ck('Sin SaveChanges en servicio', 'SaveChanges' not in s)
ck('Sin tablas nuevas', 'Migration' not in s and 'CreateTable' not in s)
ck('Sin llamadas de envío', not any(x in s for x in ('SendAsync(', 'TransmitAsync(', 'DispatchAsync(', 'PublishAsync(', 'SendFileAsync(', 'AchFileExports.Add(', 'AchReturnsGenerated.Add(')))
for method in ('InitialPreparationRemainsUnsentUnacknowledgedAndReadOnly',
 'EncryptedLocalFileCanBeVerifiedAfterWindowWithoutBeingMarkedTransmitted',
 'TamperedCiphertextHashOrPhysicalRecordsFailClosed',
 'ManualDuplicateAndExternalAckNeverConfirmI05',
 'WrongHouseAndBoundedListCannotLeakAchColombia',
 'LimitedWindowIsReportedAndApiHasOnlyAuthorizedReads',
 'LocalTraceFoundInOtherCenitExportIsAConflictNotAnI05Delivery',
 'MissingDecryptionKeysAndOtherHouseAckFailClosed'):
 ck('Caso xUnit incorporado: '+method, method in tests)
ck('xUnit comprueba ausencia de conciliación', 'Assert.False(result.FinanciallyReconciled)' in tests)
ck('xUnit comprueba respuestas no atribuibles', 'Assert.False(result.ChamberAcknowledgementAttributed)' in tests)
ck('xUnit comprueba no exportación', 'Assert.Empty(db.AchFileExports)' in tests)
ck('Se conserva I05 original', 'GENERADA_SIN_ENVIO' in t['i05'] and 'Transmittable = false' in (r/'src/Cfa.ACHInterbank.Application/ACH/Models/CenitReturnArtifactModels.cs').read_text())
ck('No modifica snapshot SQL Server', (r/'src/Cfa.ACHInterbank.Persistence.Migrations.SqlServer/DataBase/Migrations/SqlServer/AchDbContextModelSnapshot.cs').exists())
fail=[n for n,ok in checks if not ok]
for n,ok in checks:print(('OK' if ok else 'FALLO')+' - '+n)
print(f'RESULTADO ESTATICO I06-CENIT: {len(checks)-len(fail)}/{len(checks)}')
if fail: raise SystemExit(1)
