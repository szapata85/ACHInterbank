# Evidencia SOAP Proc_Contrapartidas - ACH Colombia

Archivo generado en modo evidencia UAT/dry-run. No fue transmitido manualmente a producción ni a cámara real.

| Control | Resultado |
|---|---|
| Operación Proc_Contrapartidas presente | OK |
| XML bien formado | OK |
| Credenciales/tokens/certificados privados | No presentes |
| Hash SHA256 | 03C81BE5F2CD5C95491950FB0C9B64322E78268F9A1097A21B0B6D6A9A591881 |
| Endpoint SOAP invocado manualmente | No |
| Transmisión productiva exitosa | No |

Observación: el body fue tomado de ContrapartidaDispatchBatches.RequestPayloadXml, generado por el sistema para la transacción sintética UAT-ACHCOL-NACHA-SOAP-001. El runtime registró intentos automáticos del job SOAP con error de resolución DNS contra endpoint externo/no resoluble; se documenta como brecha de configuración UAT/mock.
