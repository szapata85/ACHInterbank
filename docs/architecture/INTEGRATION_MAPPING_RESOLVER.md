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
| Proc_Contrapartidas | Usa `ProcContrapartidasFunctionalMappingResolver` si existe mapping publicado; cae a fallback transicional si no existe. |
| Proc_Transacciones | Usa `ProcTransaccionesRequestMapper` y exige `IntegrationMappingSet` publicado. |
| RegistrarRespuestaTransaccion | No usa `IntegrationMappingSet`; usa mapper/parser fisico del gateway. |

## Brechas

- Falta resolver transversal unificado.
- Falta trace formal persistido como evidencia para las tres operaciones.
- `RegistrarRespuestaTransaccion` no consume mappings parametrizados.
- `Proc_Contrapartidas` puede operar con fallback transicional si no hay mapping publicado.

## Recomendacion

Implementar `IIntegrationMappingResolver` en una fase acotada, manteniendo los mappers actuales como adaptadores y sin cambiar contratos SOAP.
