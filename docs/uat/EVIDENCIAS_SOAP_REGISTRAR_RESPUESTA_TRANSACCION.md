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
- No consume `IntegrationMappingSet`; usa mapper/parser fisico.

## Estado

Funcionalmente separado de flujos monetarios. Pendiente parametrizar mappings y trace campo-a-campo para respuesta diferencial.

Productivo: **NO-GO**.
