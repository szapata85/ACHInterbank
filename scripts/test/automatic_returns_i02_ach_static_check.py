#!/usr/bin/env python3
"""Verificaciones estáticas I02-ACH. NO reemplazan compilación ni pruebas xUnit."""
from pathlib import Path
import sys

root = Path(__file__).resolve().parents[2]
p = root / 'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation'
detector = (p / 'AchColombiaApplicationFailureDetector.cs').read_text(encoding='utf-8')
pipeline = (p / 'IncomingNachaPostProcessingPipeline.cs').read_text(encoding='utf-8')
tests = (root / 'tests/Cfa.ACHInterbank.Tests/AchColombiaApplicationFailureDetectorI02Tests.cs').read_text(encoding='utf-8')
checks = {
    'Detección restringida a ACHCOL': '"ACHCOL"' in detector and 'cycle.ClearingHouse?.Code' in detector,
    'Identidad de cámara y ciclo consistente': 'cycle.ClearingHouseId == queue.ClearingHouseId' in detector,
    'Origen limitado a cola CycleBound': 'queue.SourceKind == IncomingNachaDispatchSourceKinds.CycleBound' in detector,
    'Modo Live en ambas evidencias': 'IsLive(operatingMode)' in detector and 'IsLive(execution.ExecutionMode)' in detector,
    'Respuesta de transporte confirmada': 'execution.TransportStatus == IntegrationTransportStatus.Succeeded' in detector,
    'Catálogo funcional confirmado': 'execution.ResponseCatalogId.HasValue' in detector and 'execution.BusinessStatus == IntegrationResponseBusinessStatus.Rejected' in detector,
    'Errores técnicos excluidos': '!execution.IsTechnicalFailure' in detector,
    'Sin reintentos inciertos': '!execution.RetryAllowed' in detector and '!execution.RequiresManualReview' in detector,
    'Tipos crédito/débito/prenotificación': all(x in detector for x in ['TransactionTypeEnum.Credit','TransactionTypeEnum.Debit','TransactionTypeEnum.Prenotification']),
    'Causal condicionada a catálogo': 'r.IsForReturn' in detector and 'execution.AchReturnCodeId.HasValue' in detector,
    'No inferir causal a partir del mensaje': 'suggestedReason = reasonKnown ? responseCode : string.Empty' in detector,
    'Deduplicación en seguimiento': 'IncomingNachaProcessingEvents.Local.Any' in detector and 'IncomingNachaProcessingEvents.AsNoTracking().AnyAsync' in detector,
    'Evidencia sin envío ni generación': 'returnGenerated = false' in detector and 'returnSent = false' in detector,
    'Detector conectado tras respuesta confirmada': pipeline.find('RecordResponseAsync(') < pipeline.find('RecordIfConfirmedAsync('),
    'Fallo de observación aislado': 'AchColApplicationFailureDetectionUnavailable' in pipeline,
    'Sin llamadas a generador/transporte en detector': not any(x in detector for x in ['GenerateReturnsFileAsync','AchOutboundReturnArtifactService','NachaReturnOut','SendAsync(']),
    'Escenarios de cámara, modo e incidencias': all(x in tests for x in ['ConfirmedFunctionalRejectionCreatesOnlyPassiveEvent', 'CenitAndFileBasedNeverCreateApplicationFailure', 'TechnicalRetryUnknownResponseOrMissingCatalogNeverBecomesApplicationFailure', 'SameTransactionDoesNotEmitRepeatedEventsAcrossRepeatedCalls']),
    'Pruebas sin efectos monetarios': 'Assert.False(await db.Set<AchReturnGenerated>().AnyAsync())' in tests,
}
for name, valid in checks.items():
    print(('PASS_STATIC' if valid else 'FAIL_STATIC') + ': ' + name)
print(f'RESULTADO: {sum(checks.values())}/{len(checks)} comprobaciones estáticas')
sys.exit(0 if all(checks.values()) else 1)
