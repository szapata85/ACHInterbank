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

## Actualizacion 2026-05-23 - cierre DEF-UAT-SOAP-MAP-004

Se implemento el caso de uso end-to-end que aplica respuesta diferencial sobre `AchTransaction.IsPrenotification=true`:

- Respuesta aprobada: `Pending -> Certified`.
- Respuesta rechazada con causal `R03`: `Pending -> ReturnedByEpr`.
- Se crea `AchTransactionStateEvent`.
- Se persisten `IntegrationMappingTrace` y `IntegrationMappingTraceEntries`.
- Se cruza payload, NACHA-M desagregado y prenotificacion interna.
- Missing mapping falla controladamente.
- Prenotificacion no encontrada falla controladamente.
- Duplicado queda controlado.
- No se mueve dinero.
- No se afectan saldos.
- No se invoca `IWscfaachSoapClient`.
- No se invoca `Proc_Contrapartidas`.
- No se invoca `Proc_Transacciones`.

Evidencia:

- `docs/uat/evidencias/soap-integrations/prenotification-responses/approved/`
- `docs/uat/evidencias/soap-integrations/prenotification-responses/rejected/`

`DEF-UAT-SOAP-MAP-004`: **cerrado tecnico UAT**.
