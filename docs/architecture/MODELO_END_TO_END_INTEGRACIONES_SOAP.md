# Modelo end-to-end de integraciones SOAP

Fecha: 2026-05-21

## Matriz diagnostica

| Operacion | Naturaleza | Originador | Mueve dinero | Proposito | Usa settings | Usa mappings | Hardcoded/fallback | Evidencia | Riesgo |
|---|---|---|---|---|---|---|---|---|---|
| Proc_Contrapartidas | Debito monetario | CFA | Si | MonetaryDebitRequest | Si | Si, mapping publicado obligatorio | Fallback transicional bloqueado | DryRun en `ContrapartidaDispatchJobService` | Si falta mapping requerido, falla antes de XML |
| Proc_Transacciones | Credito monetario | Otra entidad; CFA receptora | Si | MonetaryCreditRequest | Si | Si, mapping publicado obligatorio | Builder XML desde contrato resuelto | Ejecucion/hash/trace en `IncomingNachaIntegrationExecution` y `IntegrationMappingTraces` | DryRun/Disabled bloquea transmision externa |
| RegistrarRespuestaTransaccion | Respuesta diferencial/notificacion | Entidad/camara/proveedor | No | DifferentialResponseNotification | Si | Si, para trace parametrizado | Mapper/parser fisico despues del trace | `IntegrationMappingTraces` y tests de use case | No monetario; no WSCFAACH |

## Proc_Contrapartidas

Flujo observado:

1. `ContrapartidaDispatchJobService` selecciona items elegibles.
2. `ProcContrapartidasRequestMapper` intenta resolver mapping publicado.
3. `ProcContrapartidasFunctionalMappingResolver` consume `IntegrationMappingSet`.
4. Si no existe mapping publicado, falla con `INTEGRATION_MAPPING_REQUIRED`.
5. `WscfaachSoapClient` resuelve endpoint/action desde `SoapIntegrationSettingsService`.
6. En `ProcContrapartidas:Mode=DryRun`, genera payload y no transmite.

## Proc_Transacciones

Flujo observado:

1. `IncomingNachaPostProcessingOrchestrator` toma cola de entrada.
2. `ProcTransaccionesRequestMapper` exige mapping publicado.
3. `WscfaachSoapClient` resuelve endpoint/action desde `SoapIntegrationSettingsService`.
4. `ProcTransacciones:Mode=DryRun/Disabled` bloquea transmision externa en UAT/local.
5. Se invoca `Proc_Transacciones` solo en `Live` configurado formalmente y se parsea respuesta.

Riesgo: no se observo modo DryRun/Disabled especifico para este orquestador. Debe agregarse guardrail antes de UAT externo real.

## RegistrarRespuestaTransaccion

Flujo observado:

1. `NotificarRespuestaAchUseCase` valida intento pendiente.
2. `RegistrarRespuestaAchCommandMapper` arma comando de aplicacion.
3. `RespuestaTransaccionesAchGateway` usa `RegistrarRespuestaAchSoapRequestMapper`.
4. `WsAxonRespuestaTransaccionesSoapClient` resuelve endpoint/action desde settings.
5. `IntegrationMappingTraceWriter` persiste trace campo-a-campo parametrizado.
6. `RegistrarRespuestaAchSoapResponseParser` interpreta respuesta.
7. Se actualiza estado de notificacion/respuesta.

No se observaron llamadas a servicios monetarios desde este gateway/use case. No debe mover dinero ni afectar saldos.

## Estado productivo

Productivo permanece **NO-GO**.

## Actualizacion 2026-05-21 - Garantia Transaction Integration Readiness

Se implemento una garantia tecnica verificable por pruebas para la cadena:

`Transaction -> ExpectedIntegrationOperation -> IntegrationMappingReadiness -> XML/Payload/Response Trace`.

Componentes:

- `ITransactionIntegrationOperationResolver`: resuelve la operacion esperada por naturaleza/originador.
- `IIntegrationMappingReadinessService`: valida mappings publicados requeridos y marca fallback requerido como `Failed`.
- `ITransactionIntegrationReadinessService`: expone consulta read-only por transaccion.
- `GET /Transactions/{id}/integration-readiness`: endpoint sin mutacion, sin SOAP y sin movimiento monetario.

Guardrails:

- `Proc_Contrapartidas` valida readiness antes del XML; fallback transicional queda bloqueado para campos requeridos y no construye envelope.
- `Proc_Transacciones` valida readiness antes del payload/XML y en UAT/local no transmite con `ProcTransacciones:Mode=DryRun/Disabled`.
- `RegistrarRespuestaTransaccion` valida readiness de `DifferentialResponseNotification`, persiste trace campo-a-campo y no usa WSCFAACH ni logica monetaria.

Pruebas:

- `TransactionIntegrationReadinessGuaranteeTests`.
- `TransactionsControllerTests.GetTransactionIntegrationReadiness_ShouldReturnExpectedOperation`.
- `NotificarRespuestaAchUseCaseTests.RegistrarRespuestaTransaccion_ShouldFailControlled_WhenRequiredMappingMissing`.

Queda pendiente la ejecucion de acta UAT firmada con runtime representativo; el trace parametrizado queda implementado para respuesta diferencial y disponible como patron comun.

## Actualizacion 2026-05-21 - DEF-UAT-SOAP-MAP-001

El fallback transicional de `Proc_Contrapartidas` queda cerrado tecnicamente:

- sin mapping publicado, `ProcContrapartidasRequestMapper` falla antes de contrato/XML;
- si una resolucion externa marca `UsedFallback=true`, `ContrapartidaDispatchJobService` falla antes de `BuildSoapBody`;
- no se ejecuta DryRun exitoso ni dispatch con XML basado en fallback;
- readiness no retorna `Ok` si `usesFallback=true`.
