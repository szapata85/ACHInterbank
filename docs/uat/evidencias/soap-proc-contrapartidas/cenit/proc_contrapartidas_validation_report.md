# Evidencia SOAP Proc_Contrapartidas - CENIT

Archivo generado en modo evidencia UAT/dry-run. No fue transmitido manualmente a producción ni a cámara real.

| Control | Resultado |
|---|---|
| Operación Proc_Contrapartidas presente | OK |
| XML bien formado | OK |
| Credenciales/tokens/certificados privados | No presentes |
| Hash SHA256 | F6AF7AD69B45563D6ECF41D1142EC5682986DB87C30BED7E9F5381A58F9D1C16 |
| Endpoint SOAP invocado manualmente | No |
| Transmisión productiva exitosa | No |

Observación: el body fue tomado de ContrapartidaDispatchBatches.RequestPayloadXml, generado por el sistema para la transacción sintética UAT-CENIT-NACHA-SOAP-001. Antes del guardrail se registraron intentos automáticos con error de DNS. Tras DEF-UAT-022, el runtime validó `PROC_DRY_RUN` con la referencia sintética `UAT-SOAP-DRYRUN-001`, sin `SOAP request` ni transmisión externa.
