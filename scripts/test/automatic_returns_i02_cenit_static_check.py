#!/usr/bin/env python3
"""I02-CENIT: controles de código fuente, NO sustituyen xUnit, DB ni homologación."""
from pathlib import Path
import re
root = Path(__file__).resolve().parents[2]
paths = {
    'service':'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitPassiveReturnDetector.cs',
    'interface':'src/Cfa.ACHInterbank.Application/ACH/Interfaces/ICenitPassiveReturnDetector.cs',
    'contract':'src/Cfa.ACHInterbank.Application/ACH/Models/CenitPassiveReturnDetection.cs',
    'controller':'src/Cfa.ACHInterbank.Api/Controllers/CenitPassiveReturnDetectionController.cs',
    'di':'src/Cfa.ACHInterbank.Persistence/DependencyInjectionService.cs',
    'tests':'tests/Cfa.ACHInterbank.Tests/CenitPassiveReturnDetectionI02Tests.cs',
    'policy':'src/Cfa.ACHInterbank.Application/ACH/Services/CenitIncomingReturnPolicy.cs',
    'settings':'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitAutomaticReturnSettingsService.cs',
}
t = {name: (root/path).read_text('utf-8-sig') for name,path in paths.items()}
checks=[]
def check(msg, valid): checks.append((msg,bool(valid)))
s=t['service']; c=t['controller']; tests=t['tests']; contract=t['contract']
check('servicio CENIT propio', 'class CenitPassiveReturnDetector' in s)
check('API contrato propio', 'interface ICenitPassiveReturnDetector' in t['interface'])
check('DTO no autoriza procesamiento', 'bool ProcessingConnected' in contract and 'VerifiedReasonCode' in contract)
check('API solo GET', '[HttpGet]' in c and '[HttpPost]' not in c and '[HttpPut]' not in c)
check('ruta exclusivamente CENIT', '[Route("api/cenit/devoluciones-automaticas/deteccion")]' in c)
check('autenticacion', '[Authorize]' in c)
check('permiso consulta administracion', '[Authorize(Policy = P1Policies.ConfigRead)]' in c)
check('limite endpoint', 'maxTransactions is < 1 or > 100' in c and 'BadRequest' in c)
check('registro DI independiente', 'AddScoped<ICenitPassiveReturnDetector, CenitPassiveReturnDetector>()' in t['di'])
check('valida configuración original I01', 'settings.GetAsync' in s)
check('camara por codigo canonico', 'RegulatoryCycleScheduleCatalog.CenitCode' in s)
check('filtra camara tanto ejecucion como cola', 'x.ClearingHouseId == cenitId' in s and 'x.DispatchQueue.ClearingHouseId == cenitId' in s)
check('filtra camara por ciclo transaccion', 'cycle.ClearingHouseId != cenitId' in s)
check('bloquea ACHCOL', 'cycle.ClearingHouse?.Code != RegulatoryCycleScheduleCatalog.CenitCode' in s)
check('requiere transaccion entrante y clasificada', 'AchTransactionDirection.Incoming' in s and 'AchTransactionOrigin.ExternalInstitution' in s and 'AchTransactionClassificationStatus.Determined' in s)
check('requiere clasificacion funcional entrante', 'classification.FunctionalClass' in s and 'IncomingNachaEligibilityStatus.Elegible' in s)
check('requiere cola funcional finalizada', 'IncomingNachaDispatchQueueStatus.FailedFinal' in s and 'queue.LastErrorCode != "IFUNC"' in s)
check('trazabilidad final', '!link.IsFinal' in s and 'link.AchTransactionId != tx.Id' in s)
check('protege origen ingesta', 'link.IncomingNachaFileIngestionId != queue.IncomingNachaFileIngestionId' in s)
check('excluye enlace ambiguo', 'IncomingNachaLinkType.Ambiguous' in s)
check('unicamente nucleo Funcional', '"Proc_Transacciones"' in s and 'IncomingNachaPostProcessingMode.IsLive' in s)
check('no toma errores de transporte', 'TransportStatus.Succeeded' in s and 'IsTechnicalFailure' in s)
check('requiere resultado funcional definitivo', 'BusinessStatus.Rejected' in s and 'ProcessingStatus != IncomingNachaIndividualProcessingStatus.Completed' in s)
check('exige catalogo resuelto', 'attempt.ResponseCatalogId.HasValue' in s and 'AchReturnCodeId.HasValue' in s)
check('catalogo de causal exclusivamente CENIT', 'CenitIncomingReturnPolicy.CauseDefinitions' in s and 'x.ClearingHouseId == cenitId' in s)
check('vigencia y tipo de catalogo', 'x.EffectiveFrom.Date' in s and 'x.FlowType == AchReturnFlowType.Any' in s)
check('no Dxx ni Rxx inventado', "code[0] == 'R'" in s and 'char.IsAsciiDigit' in s)
check('aplicabilidad debito credito', 'AppliesToDebitMonetary' in s and 'AppliesToCreditMonetary' in s)
check('R06 R23 especiales', '"R06" or "R23"' in s)
check('cancelacion manual especial', '"R34"' in s)
check('prenotificacion revision', 'REVISION_PRENOTIFICACION' in s)
check('no considera resultado obsoleto', 'visited.Add' in s and 'OrderByDescending(x => x.StartedAtUtc)' in s)
check('ignora devoluciones ya generadas', 'AchReturnsGenerated.AsNoTracking()' in s and 'returnedIds.Contains' in s)
check('sin SaveChanges del detector', 'SaveChanges' not in s)
check('sin creacion de filas', not re.search(r'context\.\w+\.Add(?:Async|Range)?\s*\(', s))
check('sin motor ni transporte ni scheduler', all(v not in s for v in ('ReturnFileRunner', 'GenerateAutomaticallyAsync', 'TransmitAsync','Quartz','IHostedService','NachaFileBuilder')))
check('respuesta NO conectada', 'ProcessingConnected: false' in s)
check('lectura sin seguimiento', s.count('.AsNoTracking()') >= 4)
check('ventana acotada', 'ScanWindow = 500' in s and 'Take(ScanWindow)' in s)
check('sin datos de cuentas DTO', 'AccountNumber' not in contract and 'SourceAccount' not in contract)
check('prueba de estado desactivado', 'ConfirmedCenitFailureIsOnlyAPassiveIndication_WhileDisabled_NoWrites' in tests)
check('prueba modos', 'ModeNeverEnablesExecution_AndAchIsUntouched' in tests)
check('prueba fallo tecnico simulacion', 'TechnicalOrSimulationOrUnlinkedOrSuccessfulResultNeverBecomesAFinding' in tests)
check('prueba de respuesta posterior', 'OlderRejectionIsNotReportedAfterNewerSuccessfulExecution' in tests)
check('prueba causales', 'CauseCannotBeTakenFromAchRulesOrWithoutCenitApplicability' in tests)
check('prueba prenotificaciones y duplicado', 'PrenotificationNeedsAdditionalEvidence_AndReturnAlreadyGeneratedIsIgnored' in tests)
check('prueba aislamiento de cola ACH', 'AQueueBelongingToAchColombiaCannotAppearInCenitDetection' in tests)
check('prueba aislamiento catalogo', 'ForeignAchCodeMappingIsNeverAcceptedAsCenitEvidence' in tests)
check('prueba de permisos', 'RejectsUnboundedSearch_AndHttpEndpointRequiresAuthorizedReadOnlyAccess' in tests)
check('conserva API I01', 'class CenitAutomaticReturnSettingsService' in t['settings'])
for label,ok in checks: print(('PASS' if ok else 'FAIL')+': '+label)
print(f'I02-CENIT: {sum(x[1] for x in checks)}/{len(checks)} controles estáticos')
if not all(x[1] for x in checks): raise SystemExit(1)
