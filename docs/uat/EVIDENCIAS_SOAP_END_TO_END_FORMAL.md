# Evidencias SOAP end-to-end formal

Fecha: 2026-05-21

## Alcance

Flujos cubiertos:

| Flujo | Operacion | MappingPurpose | Movimiento monetario | Estado UAT |
|---|---|---|---|---|
| Debito CFA | `Proc_Contrapartidas` | `MonetaryDebitRequest` | Si | Cerrado tecnico previamente |
| Credito externo | `Proc_Transacciones` | `MonetaryCreditRequest` | Si | Cerrado tecnico DryRun/Disabled |
| Respuesta diferencial | `RegistrarRespuestaTransaccion` | `DifferentialResponseNotification` | No | Cerrado tecnico trace/mapping |

## Evidencia automatizada

- `IncomingNachaPostProcessingOrchestratorTests.ProcTransacciones_DryRun_ShouldValidateReadinessBeforePayload`
- `IncomingNachaPostProcessingOrchestratorTests.ProcTransacciones_DryRun_ShouldFail_WhenRequiredFieldUsesFallback`
- `IncomingNachaPostProcessingOrchestratorTests.ProcTransacciones_DryRun_ShouldGeneratePayloadAndNotTransmitExternally`
- `IncomingNachaPostProcessingOrchestratorTests.ProcTransacciones_DisabledMode_ShouldBlockControlledAndNotInvokeSoap`
- `NotificarRespuestaAchUseCaseTests.RegistrarRespuestaTransaccion_ShouldPersistFieldByFieldTrace_BeforeGateway`
- `NotificarRespuestaAchUseCaseTests.RegistrarRespuestaTransaccion_ShouldNotInvokeGateway_WhenTraceHasMissingRequiredField`
- `IntegrationMappingTraceWriterTests.RegistrarRespuestaTransaccion_ShouldPersistFieldByFieldTrace`

## Resultado

Los tres flujos mantienen separacion funcional. En UAT/local no se transmite externamente. `RegistrarRespuestaTransaccion` no mueve dinero, no afecta saldos y no invoca WSCFAACH.

Productivo: **NO-GO**.
