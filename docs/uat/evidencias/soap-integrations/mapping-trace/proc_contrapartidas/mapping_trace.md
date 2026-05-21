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
| OFNIT | `AchTransaction.CompanyIdentification` o regla publicada | Si, cuando hay mapping publicado | Si, fallback transicional | Debe eliminarse fallback para cierre normativo completo. |
| OFEMP | `ClearingHouse.Code` o regla publicada | Si, cuando hay mapping publicado | Si, fallback transicional | Validar contra norma/camara. |
| OFCTA | `AchTransaction.SourceAccountNumber` o regla publicada | Si, cuando hay mapping publicado | Si, fallback transicional | Sanitizar en evidencia. |
| OFDD | Naturaleza transaccion | Si, cuando hay mapping publicado | Si, fallback transicional | Debe representar debito CFA. |
| OFMONDEB | Monto debito | Si, cuando hay mapping publicado | Si, fallback transicional | Monetario. |
| OFMONCRE | 0 para debito | Si, cuando hay mapping publicado | Si, fallback transicional | No usar para credito en este flujo. |
| OFIDLOT | Lote/ciclo | Si, requerido | Si, fallback transicional | El resolver falla si mapping publicado no lo resuelve. |
| OFIDTX | Identificador transaccion | Si, cuando hay mapping publicado | Si, fallback transicional | Debe ser trazable. |

## Resultado

Trace parcial. La operacion puede consumir mappings publicados, pero mantiene fallback transicional. Se registra defecto abierto para cierre posterior.
