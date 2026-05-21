# Mapping trace - Proc_Transacciones

Fecha: 2026-05-21

## Clasificacion

- IntegrationKey: `WSCFAACH`
- OperationKey: `Proc_Transacciones`
- Direction: `OutboundRequest`
- Purpose: `MonetaryCreditRequest`
- Mueve dinero: si.

## Origen de campos

| Campo SOAP/XML | Origen actual | Mapping parametrizado | Hardcoded/fallback | Observacion |
|---|---|---|---|---|
| TREG | `IntegrationMappingRule` | Si | No | Requerido. |
| TIPTRAN | `IntegrationMappingRule` | Si | No | Requerido. |
| MONTO | `AchTransaction.Amount` via mapping | Si | No | Monetario. |
| IDTRAN | `AchTransaction`/queue via mapping | Si | No | Requerido. |
| IDCAMCOMPE | Ciclo/camara via mapping | Si | No | Requerido. |

## Resultado

`ProcTransaccionesRequestMapper` exige `IntegrationMappingSet` publicado. Si falta, bloquea con error controlado. Pendiente agregar guardrail DryRun/Disabled especifico para evitar transmision externa en UAT/local.
