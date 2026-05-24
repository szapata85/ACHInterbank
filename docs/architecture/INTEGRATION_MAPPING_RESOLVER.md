# Integration Mapping Resolver

Fecha: 2026-05-21

## Objetivo

El resolver transversal esperado debe recibir:

- `integrationKey`.
- `operationKey`.
- `direction`.
- `mappingPurpose`.
- payload fuente.

Debe cargar mappings activos, validar requeridos, aplicar transformaciones/defaults y devolver un trace campo-a-campo.

## Modelo esperado

Resultado minimo:

- `mappedFields`.
- `missingRequiredFields`.
- `defaultedFields`.
- `transformedFields`.
- `unmappedFields`.
- `trace`.
- `canBuildPayload`.
- `errors`.

Trace minimo:

| Campo SOAP/XML | Campo origen | Valor sanitizado | MappingId | Transformacion | Default aplicado | Required | Hardcoded |
|---|---|---|---|---|---|---|---|

## Estado actual

| Operacion | Estado resolver |
|---|---|
| Proc_Contrapartidas | Usa `ProcContrapartidasFunctionalMappingResolver` con mapping publicado; falla controlado si no existe. |
| Proc_Transacciones | Usa `ProcTransaccionesRequestMapper`, exige `IntegrationMappingSet` publicado y queda protegido por `ProcTransacciones:Mode` DryRun/Disabled. |
| RegistrarRespuestaTransaccion | Valida readiness y persiste trace campo-a-campo con `IntegrationMappingTraceWriter`; el gateway fisico se invoca solo si el trace no tiene requeridos faltantes. |

## Brechas

- Falta resolver transversal unificado.
- Falta trace formal persistido como evidencia para las tres operaciones.
- `RegistrarRespuestaTransaccion` consume mapping publicado para trace parametrizado y bloquea gateway si faltan requeridos.
- `Proc_Contrapartidas` ya no puede operar con fallback transicional para campos requeridos.

## Recomendacion

Implementar `IIntegrationMappingResolver` en una fase acotada, manteniendo los mappers actuales como adaptadores y sin cambiar contratos SOAP.

## Actualizacion 2026-05-21

Se agrego `IIntegrationMappingReadinessService` como garantia previa al resolver/payload:

- valida metodo activo;
- valida `IntegrationMappingSet` publicado;
- valida mappings requeridos activos;
- devuelve `Ok`, `Failed` o `Partial`;
- marca `usesFallback=true` cuando detecta que el payload dependeria de fallback transicional;
- no invoca SOAP;
- no cambia estados;
- no mueve dinero.

El resolver transaccional `ITransactionIntegrationOperationResolver` determina:

- `Proc_Contrapartidas` para debitos originados por CFA;
- `Proc_Transacciones` para creditos originados por entidad externa;
- `RegistrarRespuestaTransaccion` para respuestas diferenciales no monetarias.

El trace campo-a-campo unificado queda persistido en `IntegrationMappingTraces` / `IntegrationMappingTraceEntries` para respuestas diferenciales y queda disponible como patron comun para los flujos SOAP.

## Cierre fallback Proc_Contrapartidas

Para `WSCFAACH / Proc_Contrapartidas / MonetaryDebitRequest`:

- todos los campos requeridos deben tener `IntegrationMappingRule.Enabled=true`;
- si no existe `IntegrationMappingSet` publicado, readiness queda `Failed`;
- `FallbackFields` y `RequiredFallbackFields` se informan con los parametros requeridos afectados;
- `CanBuildPayload=false`;
- el mapper no crea contrato transicional;
- el job no llama `BuildSoapBody` si `UsedFallback=true`.

## Actualizacion 2026-05-23 - soporte NACHA-M desagregado

El catalogo de fuentes mapeables se amplio con fuentes funcionales controladas para el archivo NACHA-M de entrada:

- `nachaHeaders.*`
- `batchHeaders.*`
- `entryDetails.*`
- `addendaRecords.*`
- `batchControls.*`
- `fileControls.*`

`ProcTransaccionesRequestMapper` actua como adaptador del resolver para `Proc_Transacciones`:

- carga `EntryDetail` desde `IncomingNachaEntryClassification.EntryDetailId`;
- resuelve `NachaHeader` por `NachaID` o ingesta;
- resuelve `BatchHeader`, `BatchControl` y `FileControl` por `NachaID`;
- resuelve `AddendaRecord` por `AddendaRecordId` o por trace asociado;
- bloquea si un parametro requerido queda sin valor;
- conserva `SourceValues` para evidencia campo-a-campo.

No se habilita SQL libre ni seleccion de tablas fisicas arbitrarias desde UI/API.
