#!/usr/bin/env python3
"""I05-CENIT: inspección estática de fuente. NO sustituye build, xUnit, BD ni homologación."""
from pathlib import Path
import re
r=Path(__file__).resolve().parents[2]
paths={
 'domain':'src/Cfa.ACHInterbank.Domain/Models/ACH/CenitReturnPreparation.cs',
 'config':'src/Cfa.ACHInterbank.Persistence/Configuration/CenitReturnPreparationConfiguration.cs',
 'contract':'src/Cfa.ACHInterbank.Application/ACH/Interfaces/ICenitReturnArtifactService.cs',
 'dto':'src/Cfa.ACHInterbank.Application/ACH/Models/CenitReturnArtifactModels.cs',
 'service':'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitReturnArtifactService.cs',
 'controller':'src/Cfa.ACHInterbank.Api/Controllers/CenitReturnArtifactController.cs',
 'review':'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitReturnReviewService.cs',
 'di':'src/Cfa.ACHInterbank.Persistence/DependencyInjectionService.cs',
 'sql':'src/Cfa.ACHInterbank.Persistence.Migrations.SqlServer/DataBase/Migrations/SqlServer/20261103190000_CenitReturnArtifact2026.cs',
 'pg':'src/Cfa.ACHInterbank.Persistence/DataBase/Migrations/Postgres/20261103190000_CenitReturnArtifact2026.cs',
 'sqlsnapshot':'src/Cfa.ACHInterbank.Persistence.Migrations.SqlServer/DataBase/Migrations/SqlServer/AchDbContextModelSnapshot.cs',
 'pgsnapshot':'src/Cfa.ACHInterbank.Persistence/DataBase/Migrations/Postgres/AchDbContextModelSnapshot.cs',
 'tests':'tests/Cfa.ACHInterbank.Tests/CenitReturnArtifactI05Tests.cs',
 'cenitlayout':'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitReturnOut2026Layout.cs',
 'nacha':'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaFileGenerationCoordinator.cs',
}
t={k:(r/v).read_text(encoding='utf-8-sig') for k,v in paths.items()}
c=t['controller'];s=t['service'];tst=t['tests'];domain=t['domain'];checks=[]
def ck(label,ok):checks.append((label,bool(ok)))
ck('Entidad única CENIT conservada', 'class CenitReturnPreparation' in domain)
for field in ['ProtectedArtifactContent','ArtifactSha256','ArtifactFileName','ArtifactTraceNumber','ArtifactProfileCode','ArtifactRecordCount','GeneratedAtUtc','GeneratedByUserId']:
 ck(f'Metadato I05 {field}',field in domain and field in t['config'])
 ck(f'Migraciones ambos motores {field}', all(field in t[k] for k in ['sql','pg','sqlsnapshot','pgsnapshot']))
ck('Migraciones reversibles SQL', 'protected override void Down' in t['sql'] and t['sql'].count('DropColumn')==8)
ck('Migraciones reversibles PG', 'protected override void Down' in t['pg'] and t['pg'].count('DropColumn')==8)
ck('No crea tablas nuevas', 'CreateTable' not in t['sql'] and 'CreateTable' not in t['pg'])
ck('Estado concurrencia preexistente', 'IsConcurrencyToken()' in t['config'])
ck('Contrato CENIT propio', 'interface ICenitReturnArtifactService' in t['contract'])
ck('DTO bandera de no transmisión', 'Transmittable = false' in t['dto'])
ck('DI independiente', 'AddScoped<ICenitReturnArtifactService, CenitReturnArtifactService>()' in t['di'])
ck('Solo API cenit', '[Route("api/cenit/devoluciones-automaticas/artefactos")]' in c)
ck('Autenticación general', '[Authorize]' in c)
ck('Rol lectura metadata', '[Authorize(Policy = P1Policies.ConfigRead)]' in c)
ck('Rol fuerte generación y descarga', c.count('[Authorize(Policy = P1Policies.ConfigManage)]')==2)
ck('POST sólo manual', c.count('[HttpPost(')==1 and 'generar-local' in c)
ck('No endpoint de envío', not any(x in c for x in ('HttpPost("enviar','HttpPost("transmitir','Mft','TransportAsync','SoapClient')))
ck('Identidad solo claims', 'FindFirst(ClaimTypes.NameIdentifier)' in c and 'IsAuthenticated' in c and 'return Forbid()' in c)
ck('HTTP no caché local', 'no-store' in c and 'nosniff' in c)
ck('Archivo interno etiquetado', 'NO_TRANSMITIR_CENIT_I05_' in s)
ck('Sin transporte en servicio', not any(x in s for x in ('IOutboundReturnTransport','SubmitToCenit','SendFileAsync','IMft','ISftp')))
ck('No flujo financiero ACH', 'new AchReturnsService' not in s and 'GenerateReturnsFileAsync(' not in s and 'AchReturnsGenerated.Add' not in s)
ck('Estado de salida limitado', 'GENERADA_SIN_ENVIO' in s)
ck('Origen debe estar aprobado', 'p.Status != CenitReturnReviewService.Approved' in s)
ck('Única generación', 'p.GeneratedAtUtc.HasValue' in s and 'p.ProtectedArtifactContent is not null' in s)
ck('Estado fuente revalidado', 'ValidateApprovalAsync(p, ct)' in s)
ck('Modo CENIT revalidado en I04', 'settings.Mode is not' in t['review'])
ck('Separación preparador revisor', 'p.ReviewedByUserId' in s and 'firstAudit.ChangedBy' in s)
ck('Auditoría INSERT verdadera', 'x.BeforeJson == null' in s and 'x.EntityName == nameof(CenitReturnPreparation)' in s)
ck('Identidad generador no preparador', 'firstAudit.ChangedBy, actor' in s)
ck('Actor system bloqueado', '"system"' in s and 'IsValidActor' in s)
ck('Fuente original final única', 'links.Count != 1' in s and 'x.IsFinal' in s)
ck('Vínculo único por cámara', 'ResolvedClearingHouseId != houseId' in s and 'NachaHeader.ClearingHouseId != houseId' in s)
ck('No clasificación manual ambigua', 'ExactTrace15 or IncomingNachaLinkType.ExactCompositeBusinessKey' in s)
ck('Trazabilidad exacta 15', 'RequiredDigits(source.Entry.SequenceNumber, 15' in s)
ck('Comparación importe original', 'source.Entry.Amount != original.Amount' in s)
ck('Comparación cuenta original', 'source.Entry.AccountNumber?.Trim()' in s)
ck('Comparación código original', 'source.Entry.TransactionCode != original.TransactionCode' in s)
ck('DFIs verificados', 'RequiredDigits(original.ReceivingDFI, 8' in s and 'RequiredDigits(original.OriginatingDFI, 8' in s)
ck('PPD CCD exclusivo', 'sec is not ("PPD" or "CCD")' in s)
ck('Sin inventar CTX', 'CTX y otros servicios' in s)
ck('Adenda original y nueva secuencia', 'originalTrace, string.Empty, dfi' in s and 'string.Empty, newTrace' in s)
ck('Secuencias compartidas con reserva', 'sequences.ReserveRangeAsync' in s)
ck('Misma fecha valor', 'DateOnly.FromDateTime(p.SuggestedValueDate.Date)' in s)
ck('EF serializable', 'IsolationLevel.Serializable' in s and 'BeginTransactionAsync' in s)
ck('Concurrencia EF', 'DbUpdateConcurrencyException' in s)
ck('Perfil propio CENIT', 'CenitReturnOut2026Layout.NormativeVersion' in s and 'CenitReturnOut2026Layout.ProfileCode' in s)
ck('Motor oficial Opción C', 'builder.BuildReturnOutAsync(draft, ct)' in s and 'PersistAudit: false' in s)
ck('No fallback legado', 'result.LegacyFallbackUsed' in s)
ck('Estructura 106 y bloques10', 'result.Content.Length % 106' in s and 'records.Length != 10' in s)
ck('Adenda 99', 'records[3].Substring(1, 2) != "99"' in s)
ck('Registro 6 y 7 secuencias', 'records[2].Substring(87, 15)' in s and 'records[3].Substring(81, 15)' in s)
ck('Registros 1/5/6/7/8/9', 'records.Take(6)' in s)
ck('Suma de entradas 2', 'records[4].Substring(4, 6)' in s and 'records[5].Substring(13, 8)' in s)
ck('Importe de centavos cotejado', 'expectedAmount' in s and 'records[2].Substring(29, 18)' in s)
ck('Hash SHA-256 materializado', 'SHA256.HashData' in s)
ck('Protección cifrada por Data Protection', '_protector.Protect' in s and '_protector.Unprotect' in s)
ck('Material cifrado dentro de entidad', 'p.ProtectedArtifactContent = _protector.Protect' in s)
ck('Protector existente configurado', '.PersistKeysToDbContext<AchDbContext>()' in t['di'])
ck('Hash verificado al leer', 'Convert.ToHexString(SHA256.HashData(data))' in s)
ck('No descarga con modo inválido', s.count('ValidateApprovalAsync(p, ct)')==2)
ck('No file name externo', 'GenerateExternalNameAsync' not in s)
ck('No persistir retorno financiero', '.Add(new AchReturnGenerated' not in s)
ck('No procesamiento por job', 'HostedService' not in s and 'Quartz' not in s)
ck('Prueba requiere aprobación', 'GenerateRequiresAnActuallyApprovedReservation' in tst)
ck('Prueba origen inexistente', 'ApprovedButMissingRawOriginalFailsClosed' in tst)
ck('Prueba cifrado y sin envío', 'GenerationIsCenitOnlySingleUseProtectedAndNotExported' in tst)
ck('Prueba desactivado/cámara cruzada', 'GenerationFailsWhenModeDisabledOrOriginalSourceBelongsToAnotherHouse' in tst)
ck('Prueba permisos', 'P1Policies.ConfigManage' in tst and 'P1Policies.ConfigRead' in tst)
ck('Prueba corrupción física', 'ProfileAndPhysicalStructureHardFailIfAnyFieldIsCorrupted' in tst)
ck('Golden master de CENIT ya existe', (r/'tests/Cfa.ACHInterbank.Tests/CenitReturnOut2026Tests.cs').exists())
failed=[name for name,ok in checks if not ok]
for name,ok in checks:print(('OK' if ok else 'FALLO')+' - '+name)
print(f'RESULTADO ESTATICO I05-CENIT: {len(checks)-len(failed)}/{len(checks)}')
if failed:raise SystemExit(1)
