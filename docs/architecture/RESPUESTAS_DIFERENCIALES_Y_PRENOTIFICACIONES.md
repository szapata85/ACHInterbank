# Respuestas diferenciales y prenotificaciones CFA

Fecha: 2026-05-23  
Productivo: **NO-GO**

## Objetivo

Definir el comportamiento end-to-end para respuestas diferenciales recibidas por `RegistrarRespuestaTransaccion` cuando la respuesta corresponde a una prenotificacion originada por CFA que se encuentra pendiente.

## Clasificacion funcional

- IntegrationKey: `WSAXON`
- OperationKey: `RegistrarRespuestaTransaccion`
- MappingPurpose: `DifferentialResponseNotification`
- MappingDirection: `InboundResponse`
- MovesMoney: `false`

`RegistrarRespuestaTransaccion` no ejecuta debitos, no ejecuta creditos, no invoca `IWscfaachSoapClient`, no llama `Proc_Contrapartidas` y no llama `Proc_Transacciones`.

## Flujo aprobado

1. Llega una respuesta diferencial `TipoRespuesta=Prenota`.
2. El sistema valida `IntegrationMappingReadiness`.
3. Se consume el `IntegrationMappingSet` publicado para `WSAXON.RegistrarRespuestaTransaccion`.
4. Se cruza el payload con NACHA-M desagregado:
   - `NachaHeaders`
   - `BatchHeaders`
   - `EntryDetails`
   - `AddendaRecords`
   - `BatchControls`
   - `FileControls`
5. Se identifica la `AchTransaction` interna con `IsPrenotification=true`, originada por CFA y en estado `Pending`.
6. Una respuesta exitosa marca la prenotificacion como `Certified`.
7. Una respuesta rechazada marca la prenotificacion como `ReturnedByEpr` cuando la causal inicia en `R`; en caso contrario usa `ReturnedByOperator`.
8. Se crea `AchTransactionStateEvent`.
9. Se persisten `IntegrationMappingTrace` y `IntegrationMappingTraceEntries`.
10. Se confirma `monetaryMovementCreated=false` y `balancesAffected=false`.

## Casos controlados

- Prenotificacion no encontrada: error funcional `DIFFERENTIAL_RESPONSE_PRENOTIFICATION_NOT_FOUND`, sin cambio de estado.
- Prenotificacion ya procesada: error funcional `DIFFERENTIAL_RESPONSE_ALREADY_PROCESSED`, sin duplicar transicion.
- Mapping requerido faltante: error funcional `INTEGRATION_MAPPING_REQUIRED`, sin cambio de estado.
- Respuesta sin cruce NACHA-M suficiente: error funcional `DIFFERENTIAL_RESPONSE_UNMATCHED`.

## Homologacion UAT/local

El seed UAT/local publica homologaciones activas para `TipoRespuesta=Prenota`:

- `ACH / 00` -> `Aprobada`.
- `ACH / RJ / R03` -> `Rechazada`, causal normalizada `R03`.

Estas homologaciones permiten que `ProcesarRespuestaAchUseCase` dispare el procesador diferencial sin abrir gateway monetario.

## Evidencias

- Aprobada: `docs/uat/evidencias/soap-integrations/prenotification-responses/approved/`
- Rechazada: `docs/uat/evidencias/soap-integrations/prenotification-responses/rejected/`

Productivo permanece **NO-GO**.
