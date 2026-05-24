# Evidencias UAT - respuestas diferenciales de prenotificaciones CFA

Fecha: 2026-05-23  
Ambiente: UAT/local  
Productivo: **NO-GO**

## Resultado

Estado: `OK TECNICO UAT`.

`DEF-UAT-SOAP-MAP-004` queda cerrado tecnicamente: `RegistrarRespuestaTransaccion` procesa respuestas diferenciales de prenotificaciones CFA pendientes, cruza contra NACHA-M desagregado y la prenotificacion interna, crea evento de estado y persiste trace campo-a-campo sin movimiento monetario.

## Clasificacion

- IntegrationKey: `WSAXON`
- OperationKey: `RegistrarRespuestaTransaccion`
- MappingPurpose: `DifferentialResponseNotification`
- MappingDirection: `InboundResponse`
- MovesMoney: `false`

## Escenario aprobado

- Entrada: respuesta diferencial `TipoRespuesta=Prenota`, estado externo `00`.
- Cruce principal: `EntryDetails.SequenceNumber = 000128300012345`.
- Estado inicial: `Pending`.
- Estado final: `Certified`.
- Evento de estado: creado.
- Trace: `IntegrationMappingTrace` + `IntegrationMappingTraceEntries`.
- Movimiento monetario: false.
- Saldos afectados: false.

Evidencia: `docs/uat/evidencias/soap-integrations/prenotification-responses/approved/`.

## Escenario rechazado

- Entrada: respuesta diferencial `TipoRespuesta=Prenota`, estado externo `RJ`, causal `R03`.
- Cruce principal: `EntryDetails.SequenceNumber = 000128300012345`.
- Estado inicial: `Pending`.
- Estado final: `ReturnedByEpr`.
- Evento de estado: creado.
- Causal: `R03`.
- Trace: `IntegrationMappingTrace` + `IntegrationMappingTraceEntries`.
- Movimiento monetario: false.
- Saldos afectados: false.

Evidencia: `docs/uat/evidencias/soap-integrations/prenotification-responses/rejected/`.

## Negativos cubiertos

- Prenotificacion pendiente no encontrada: falla controlada.
- Prenotificacion ya procesada: duplicado controlado.
- Mapping requerido faltante: falla controlada antes de cambiar estado.
- No dependencia de `IWscfaachSoapClient`.
- No dependencia de `Proc_Contrapartidas`.
- No dependencia de `Proc_Transacciones`.

## Confirmaciones

- No hubo transmision externa.
- No se movio dinero.
- No se afectaron saldos.
- No se expusieron secretos.
- Runtime UAT/local expone homologaciones activas `Prenota`: `ACH:00` y `ACH:RJ/R03`.
- Productivo permanece **NO-GO**.
