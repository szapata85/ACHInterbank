# Mapping trace - RegistrarRespuestaTransaccion

Fecha: 2026-05-21

## Clasificacion

- IntegrationKey: `WSAXON`
- OperationKey: `RegistrarRespuestaTransaccion`
- Direction: `InboundResponse`
- Purpose: `DifferentialResponseNotification`
- Mueve dinero: no.

## Origen de campos

| Campo SOAP/XML | Origen actual | Mapping parametrizado | Hardcoded/fallback | Observacion |
|---|---|---|---|---|
| ANSIDLOTE / id lote | `RegistrarRespuestaAchCommand.IdTransaccionServicioExterno` | Si, trace persistido | No | No monetario. |
| ANSST / estado | `RegistrarRespuestaAchCommand.IdEstado` | Si, trace persistido | No | Estado interpretado. |
| ANCLC / causal | `RegistrarRespuestaAchCommand.Causal` | Si, trace persistido | No | Causal/respuesta. |
| ANSIDTX / id transaccion | `RegistrarRespuestaAchCommand.IdTransaccion` | Si, trace persistido | No | Identifica respuesta relacionada. |
| ANSIDREVER / reverso | `RegistrarRespuestaAchCommand.IdTransaccionServicioExterno` | Si, trace persistido | No | Opcional segun mapping. |

## Resultado

Trace parametrizado por `IntegrationMappingSet` implementado. `DEF-UAT-SOAP-MAP-003` queda cerrado tecnicamente: si falta un campo requerido en el trace, no se invoca gateway y no se mueve dinero ni se afectan saldos.
