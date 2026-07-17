# Matriz de códigos de respuesta

| Fuente/categoría | Método | Código | Descripción | Clasificación | Reintento | Revisión manual | Estado |
| --- | --- | --- | --- | --- | --- | --- | --- |
| CORE_SOAP_RESPONSE | Proc_Contrapartidas | R96 | Débito aplicado correctamente | Success | No | No | Activo |
| CORE_SOAP_RESPONSE | Proc_Transacciones | R96 | Crédito aplicado correctamente | Success | No | No | Activo |

## Separación de categorías

| Categoría | Uso |
| --- | --- |
| CORE_SOAP_RESPONSE | Respuesta del aplicativo SOAP/core, discriminada por método |
| ACH_RETURN_CAUSE | Causales de devolución originadas por participantes, por ejemplo R01 |
| ACH_OPERATOR_RETURN | Devoluciones del operador, por ejemplo D18 |
| ACH_FATAL_FILE_ERROR | Rechazo fatal del archivo |
| ACH_CLAIM_CAUSE | REC, REV, REI y DEV |

R96 no se clasifica por prefijo. La resolución exige fuente, método y código. Un código ausente queda `PendingCatalog`, no permite reintento automático, requiere revisión manual y nunca se interpreta como éxito.

