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

Observación: el body fue tomado de ContrapartidaDispatchBatches.RequestPayloadXml, generado por el sistema para la transacción sintética UAT-ACHCOL-NACHA-SOAP-001. Antes del guardrail se registraron intentos automáticos con error de DNS. Tras DEF-UAT-022, el runtime validó `PROC_DRY_RUN` con la referencia sintética `UAT-SOAP-DRYRUN-001`, sin `SOAP request` ni transmisión externa.
