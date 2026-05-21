# Mapping trace - RegistrarRespuestaTransaccion

Fecha: 2026-05-21

## Clasificacion

- IntegrationKey: `WSAXON`
- OperationKey: `RegistrarRespuestaTransaccion`
- Direction: `DifferentialResponseNotification`
- Purpose: `DifferentialResponseNotification`
- Mueve dinero: no.

## Origen de campos

| Campo SOAP/XML | Origen actual | Mapping parametrizado | Hardcoded/fallback | Observacion |
|---|---|---|---|---|
| idCanal | `RegistrarRespuestaAchCommand.IdCanal` | No | Mapper fisico | No monetario. |
| nombreCanal | `RegistrarRespuestaAchCommand.NombreCanal` | No | Mapper fisico | No monetario. |
| idTransaccion | `RegistrarRespuestaAchCommand.IdTransaccion` | No | Mapper fisico | Identifica respuesta relacionada. |
| idEstado | `RegistrarRespuestaAchCommand.IdEstado` | No | Mapper fisico | Estado/causal. |
| causal | `RegistrarRespuestaAchCommand.Causal` | No | Mapper fisico | Causal/respuesta. |
| idTransaccionAxon | `RegistrarRespuestaAchCommand.IdTransaccionServicioExterno` | No | Mapper fisico | Nombre fisico aislado en External. |
| descripcionCausal | `RegistrarRespuestaAchCommand.DescripcionCausal` | No | Mapper fisico | Mensaje funcional. |

## Resultado

No hay mapping parametrizado por `IntegrationMappingSet`. Se documenta como defecto abierto `DEF-UAT-SOAP-MAP-003`. La operacion no debe mover dinero ni afectar saldos.
