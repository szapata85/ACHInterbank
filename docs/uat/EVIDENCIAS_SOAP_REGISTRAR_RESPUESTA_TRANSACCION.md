# Evidencias SOAP RegistrarRespuestaTransaccion

Fecha: 2026-05-21

## Clasificacion funcional

- IntegrationKey: `WSAXON`
- OperationKey: `RegistrarRespuestaTransaccion`
- Cliente tecnico: `WsAxonRespuestaTransaccionesSoapClient`
- Naturaleza: respuesta diferencial / notificacion.
- Mueve dinero: no.
- Proposito mapping: `DifferentialResponseNotification`.

## Hallazgos

- `NotificarRespuestaAchUseCase` solo actualiza estado de intento/respuesta.
- `RespuestaTransaccionesAchGateway` depende de `IWsAxonRespuestaTransaccionesSoapClient`.
- No depende de `IWscfaachSoapClient`.
- No se observaron llamadas a `Proc_Contrapartidas` ni `Proc_Transacciones`.
- Valida readiness `WSAXON / RegistrarRespuestaTransaccion / DifferentialResponseNotification / InboundResponse`.
- Persiste trace campo-a-campo mediante `IntegrationMappingTraceWriter`.
- Si el trace detecta campo requerido faltante, no invoca gateway y deja error funcional controlado.
- Mantiene `MonetaryMovementCreated=false`.

## Estado

`DEF-UAT-SOAP-MAP-003` queda **cerrado tecnicamente** en alcance UAT/local: consume mapping publicado para trace, persiste entradas campo-a-campo y conserva guardrail no monetario.

Productivo: **NO-GO**.
