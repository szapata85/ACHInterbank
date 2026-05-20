# Matriz de Reglas - Respuestas NACHA-M de Entrada

Fecha: 2026-05-20  
Estado: Inicial UAT/local  
Productivo: NO-GO

| Escenario | ResponseMode | Requiere referencia | Requiere reasonCode | Cambia estado al generar | Procesamiento esperado posterior |
|---|---|---|---|---|---|
| IncomingCredit | No aplica | No | No | No | Carga manual por NachaUpload |
| IncomingDebit | No aplica | No | No | No | Carga manual por NachaUpload |
| IncomingPrenotificationResponse | Approved/Rejected | Prenotificacion pendiente | Si es rechazo | No | Carga manual por NachaUpload |
| IncomingCreditConfirmation | Confirmed | Transaccion UAT | No | No | Carga manual por NachaUpload |
| IncomingCreditRejection | Rejected | Transaccion UAT | Si | No | Carga manual por NachaUpload |
| IncomingCreditReturn | Returned | Transaccion UAT | Si | No | Carga manual por NachaUpload |
| IncomingDebitConfirmation | Confirmed | Transaccion UAT | No | No | Carga manual por NachaUpload |
| IncomingDebitRejection | Rejected | Transaccion UAT | Si | No | Carga manual por NachaUpload |
| IncomingDebitReturn | Returned | Transaccion UAT | Si | No | Carga manual por NachaUpload |

## Codigos Funcionales

| Codigo | Uso |
|---|---|
| SIMULATOR_DISABLED | Simulador deshabilitado por configuracion |
| CLEARING_HOUSE_REQUIRED | Camara requerida |
| CLEARING_HOUSE_NOT_SUPPORTED | Camara no soportada por UAT/local |
| SCENARIO_NOT_SUPPORTED | Escenario no soportado |
| SYNTHETIC_DATA_REQUIRED | Se requieren datos sinteticos |
| INBOUND_RESPONSE_RULE_NOT_CONFIGURED | Regla de respuesta no configurada |
| INBOUND_RESPONSE_WINDOW_EXPIRED | Ventana normativa vencida |
| PRENOTIFICATION_NOT_FOUND | Prenotificacion no encontrada |
| PRENOTIFICATION_NOT_PENDING | Prenotificacion no pendiente |
| PRENOTIFICATION_CLEARING_HOUSE_MISMATCH | Camara no coincide |
| TRANSACTION_NOT_FOUND | Transaccion no encontrada |
| TRANSACTION_NATURE_MISMATCH | Naturaleza no coincide |
| TRANSACTION_NOT_PENDING_RESPONSE | Transaccion no apta para respuesta |
| TRANSACTION_ALREADY_PROCESSED | Transaccion ya procesada |
| TRANSACTION_REASON_CODE_REQUIRED | Causal requerida |
| TRANSACTION_REASON_CODE_INVALID | Causal invalida |

## Nota

La generacion del archivo no constituye procesamiento. Los estados deben cambiar solo despues de cargar manualmente el archivo por NachaUpload y ejecutar el flujo real.
