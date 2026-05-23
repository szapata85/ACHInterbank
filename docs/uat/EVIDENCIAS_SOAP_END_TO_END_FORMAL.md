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

## Actualizacion 2026-05-23 - DEF-UAT-SOAP-MAP-004 y envelope Proc_Transacciones

| Flujo | Estado | Evidencia |
|---|---|---|
| `RegistrarRespuestaTransaccion` aprueba prenotificacion CFA pendiente | OK tecnico UAT | `docs/uat/evidencias/soap-integrations/prenotification-responses/approved/` |
| `RegistrarRespuestaTransaccion` rechaza prenotificacion CFA pendiente | OK tecnico UAT | `docs/uat/evidencias/soap-integrations/prenotification-responses/rejected/` |
| `Proc_Transacciones` envelope DryRun sanitizado | OK tecnico UAT | `docs/uat/evidencias/soap-integrations/mapping-trace/proc_transacciones/proc_transacciones_envelope_sanitizado.xml` |

Confirmaciones:

- `RegistrarRespuestaTransaccion` persiste `IntegrationMappingTrace` y entradas campo-a-campo.
- `RegistrarRespuestaTransaccion` crea `AchTransactionStateEvent`.
- `RegistrarRespuestaTransaccion` no mueve dinero ni afecta saldos.
- `RegistrarRespuestaTransaccion` no invoca WSCFAACH ni flujos `Proc_*`.
- `Proc_Transacciones` no transmite externamente en DryRun.

Productivo: **NO-GO**.
