# Mapping trace - Proc_Contrapartidas

Fecha: 2026-05-21

## Clasificacion

- IntegrationKey: `WSCFAACH`
- OperationKey: `Proc_Contrapartidas`
- Direction: `OutboundRequest`
- Purpose: `MonetaryDebitRequest`
- Mueve dinero: si.

## Origen de campos

| Campo SOAP/XML | Origen actual | Mapping parametrizado | Hardcoded/fallback | Observacion |
|---|---|---|---|---|
| OFNIT | `IntegrationMappingSet` publicado | Si, requerido | No permitido | Sin mapping activo falla antes de XML. |
| OFEMP | `IntegrationMappingSet` publicado | Si, requerido | No permitido | Validar contra norma/camara. |
| OFCTA | `IntegrationMappingSet` publicado | Si, requerido | No permitido | Sanitizar en evidencia. |
| OFDD | `IntegrationMappingSet` publicado | Si, requerido | No permitido | Debe representar debito CFA. |
| OFMONDEB | `IntegrationMappingSet` publicado | Si, requerido | No permitido | Monetario. |
| OFMONCRE | `IntegrationMappingSet` publicado | Si, requerido | No permitido | No usar para credito en este flujo. |
| OFIDLOT | `IntegrationMappingSet` publicado | Si, requerido | No permitido | El resolver falla si mapping publicado no lo resuelve. |
| OFIDTX | `IntegrationMappingSet` publicado | Si, requerido | No permitido | Debe ser trazable. |

## Resultado

Trace actualizado. La operacion exige mappings publicados activos para campos requeridos. Si falta mapping o el mapper reporta `UsedFallback=true`, el flujo queda bloqueado antes de XML, DryRun o dispatch.
