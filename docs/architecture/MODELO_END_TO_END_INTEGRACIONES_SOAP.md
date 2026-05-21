# Modelo end-to-end de integraciones SOAP

Fecha: 2026-05-21

## Matriz diagnostica

| Operacion | Naturaleza | Originador | Mueve dinero | Proposito | Usa settings | Usa mappings | Hardcoded/fallback | Evidencia | Riesgo |
|---|---|---|---|---|---|---|---|---|---|
| Proc_Contrapartidas | Debito monetario | CFA | Si | MonetaryDebitRequest | Si | Si, si hay mapping publicado | Fallback transicional si no hay mapping | DryRun en `ContrapartidaDispatchJobService` | XML puede salir sin mapping parametrizado si cae al fallback |
| Proc_Transacciones | Credito monetario | Otra entidad; CFA receptora | Si | MonetaryCreditRequest | Si | Si, mapping publicado obligatorio | Builder XML desde contrato resuelto | Ejecucion/hash en `IncomingNachaIntegrationExecution` | No tiene guardrail DryRun especifico como Contrapartidas |
| RegistrarRespuestaTransaccion | Respuesta diferencial/notificacion | Entidad/camara/proveedor | No | DifferentialResponseNotification | Si | No | Mapper/parser fisico | Tests de gateway/use case | Falta mapping trace parametrizado |

## Proc_Contrapartidas

Flujo observado:

1. `ContrapartidaDispatchJobService` selecciona items elegibles.
2. `ProcContrapartidasRequestMapper` intenta resolver mapping publicado.
3. `ProcContrapartidasFunctionalMappingResolver` consume `IntegrationMappingSet`.
4. Si no existe mapping publicado, usa fallback transicional.
5. `WscfaachSoapClient` resuelve endpoint/action desde `SoapIntegrationSettingsService`.
6. En `ProcContrapartidas:Mode=DryRun`, genera payload y no transmite.

## Proc_Transacciones

Flujo observado:

1. `IncomingNachaPostProcessingOrchestrator` toma cola de entrada.
2. `ProcTransaccionesRequestMapper` exige mapping publicado.
3. `WscfaachSoapClient` resuelve endpoint/action desde `SoapIntegrationSettingsService`.
4. Se invoca `Proc_Transacciones` y se parsea respuesta.

Riesgo: no se observo modo DryRun/Disabled especifico para este orquestador. Debe agregarse guardrail antes de UAT externo real.

## RegistrarRespuestaTransaccion

Flujo observado:

1. `NotificarRespuestaAchUseCase` valida intento pendiente.
2. `RegistrarRespuestaAchCommandMapper` arma comando de aplicacion.
3. `RespuestaTransaccionesAchGateway` usa `RegistrarRespuestaAchSoapRequestMapper`.
4. `WsAxonRespuestaTransaccionesSoapClient` resuelve endpoint/action desde settings.
5. `RegistrarRespuestaAchSoapResponseParser` interpreta respuesta.
6. Se actualiza estado de notificacion/respuesta.

No se observaron llamadas a servicios monetarios desde este gateway/use case. No debe mover dinero ni afectar saldos.

## Estado productivo

Productivo permanece **NO-GO**.
