# Catalogo controlado de campos NACHA-M desagregado para mappings SOAP

Fecha: 2026-05-23  
Alcance: `Proc_Transacciones` y `RegistrarRespuestaTransaccion` en UAT/local.  
Productivo: NO-GO.

## Diagnostico

El modelo fisico y EF Code First ya contiene las fuentes desagregadas:

- `NachaHeaders`
- `BatchHeaders`
- `EntryDetails`
- `AddendaRecords`
- `BatchControls`
- `FileControls`

La brecha detectada era que el catalogo de fuentes mapeables de integraciones solo publicaba fuentes de negocio generales (`AchTransaction`, lote, ciclo, camara, contexto de ejecucion y constantes). Por tanto, aunque el parser de NACHA-M persistia el modelo desagregado, `Proc_Transacciones` no podia demostrar por catalogo que un campo SOAP venia de `EntryDetails`, `BatchHeaders`, `NachaHeaders` u otras tablas del archivo cargado.

## Cambio aplicado

Se agregaron fuentes controladas al catalogo de mapping para todas las operaciones SOAP catalogadas. No se habilito SQL libre ni seleccion arbitraria de tablas.

Fuentes publicadas:

| Fuente funcional | Modelo EF | Ejemplos de fieldPath |
|---|---|---|
| Encabezado archivo | `NachaHeader` | `nachaHeaders.immediateOrigin`, `nachaHeaders.immediateDestination`, `nachaHeaders.fileIdModifier` |
| Encabezado lote | `BatchHeader` | `batchHeaders.companyId`, `batchHeaders.companyName`, `batchHeaders.effectiveEntryDate` |
| Detalle entrada | `EntryDetail` | `entryDetails.transactionCode`, `entryDetails.amount`, `entryDetails.sequenceNumber` |
| Addenda | `AddendaRecord` | `addendaRecords.infofromOriginator`, `addendaRecords.returnReasonCode`, `addendaRecords.originalTraceNumber` |
| Control lote | `BatchControl` | `batchControls.entryAddendaCount`, `batchControls.entryHash`, `batchControls.totalCreditAmount` |
| Control archivo | `FileControl` | `fileControls.batchCount`, `fileControls.blockCount`, `fileControls.entryHash` |
| Prenotificacion interna | `AchTransaction` | `prenotification.reference`, `prenotification.state` |
| Respuesta diferencial | `AchResponse` | `differentialResponse.idTransaccion`, `differentialResponse.codigoEstadoExterno` |

## Garantia tecnica

`Proc_Transacciones` ahora puede resolver mappings desde el contexto NACHA-M desagregado cargado por `NachaUpload`:

1. `IncomingNachaEntryClassification.EntryDetailId` identifica `EntryDetails`.
2. `EntryDetails.NachaID` identifica `NachaHeaders`.
3. `NachaID` cruza `BatchHeaders`, `BatchControls` y `FileControls`.
4. `IncomingNachaEntryClassification.AddendaRecordId` identifica `AddendaRecords` cuando existe; si no, se cruza por trace.
5. El mapper conserva `SourceValues` para que `IntegrationMappingTraceWriter` persista valor fuente sanitizado por `fieldPath`.

## Actualizacion 2026-05-23 - respuestas de prenotificaciones

`RegistrarRespuestaTransaccion` ahora usa el catalogo y los `fieldPath` controlados para cruzar respuestas diferenciales de prenotificaciones CFA pendientes:

- `batchHeaders.batchNumber` -> `ANSIDLOTE`
- `entryDetails.sequenceNumber` -> `ANSIDTX`
- `differentialResponse.codigoEstadoExterno` -> `ANSST`
- `differentialResponse.codigoCausalExterna` -> `ANCLC`
- `addendaRecords.originalTraceNumber` -> `ANSIDREVER`

El cruce usa NACHA-M desagregado y `AchTransaction.IsPrenotification=true`. Si no existe prenotificacion pendiente o falta mapping requerido, el flujo falla controladamente y no cambia estado.

Defecto `DEF-UAT-SOAP-MAP-004`: **cerrado tecnico UAT**.
