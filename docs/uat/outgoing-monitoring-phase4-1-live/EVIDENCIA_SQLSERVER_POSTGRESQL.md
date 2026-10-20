# Evidencia — SQL Server y PostgreSQL

| Verificación | SQL Server | PostgreSQL |
| --- | --- | --- |
| API Docker saludable | Sí, puerto 843 | Sí, puerto 844 |
| SPA Docker disponible | Sí, puerto 743 | Sí, puerto 744 |
| Catálogo de ciclos real | Aprobado | Aprobado |
| Proc_Contrapartidas LIVE | Aprobado | Aprobado |
| Error técnico persistido | Aprobado | Aprobado |
| Reintento sobre una raíz | Aprobado | Aprobado |
| API y monitor | Aprobado | Aprobado |
| Pruebas relacionales del monitor | Aprobado | Aprobado |

PostgreSQL expuso dos brechas de inicialización en base nueva: mapping seed no canónico y ausencia de la operación en settings SOAP. Se corrigieron con reparación idempotente limitada a publicaciones del seed. Las pruebas de bootstrap y las pruebas relacionales reales quedaron verdes. No se requirieron migraciones.
