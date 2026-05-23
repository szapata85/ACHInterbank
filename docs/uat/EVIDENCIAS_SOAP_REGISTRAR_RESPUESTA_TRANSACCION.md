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

## Actualizacion 2026-05-23 - cruce NACHA/prenotificaciones

Se mantiene cerrado el guardrail no monetario de `RegistrarRespuestaTransaccion`:

- `MovesMoney=false`.
- No inyecta `IWscfaachSoapClient`.
- No llama `Proc_Contrapartidas`.
- No llama `Proc_Transacciones`.
- Valida readiness de `WSAXON / RegistrarRespuestaTransaccion / DifferentialResponseNotification / InboundResponse`.
- Persiste trace campo-a-campo.

Avance aplicado:

- El catalogo controlado ahora expone fuentes `DifferentialResponse` y `Prenotification`.
- El catalogo tambien expone las seis fuentes NACHA-M desagregadas para que los mappings puedan cruzar respuesta, archivo y transaccion/prenotificacion.

Brecha abierta:

- No existe aun un caso de uso end-to-end que aplique una respuesta diferencial sobre `AchTransaction.IsPrenotification=true` para aprobar/rechazar la prenotificacion con state event usando catalogos homologados. Queda documentado como `DEF-UAT-SOAP-MAP-004`; no se simulo exito.
