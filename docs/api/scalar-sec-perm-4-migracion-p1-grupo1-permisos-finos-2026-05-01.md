# Scalar-SEC-PERM-4 — Migración P1 grupo 1 a permisos finos compatibles (2026-05-01)

## 1. Resumen ejecutivo
Se migró P1 grupo 1 a policies compuestas compatibles (permiso fino OR legacy) para BulkIngestion, IncomingNachaCommandCenter, NachaUpload y ruta operativa de NachaController.

## 2. Contexto PERM-3/PERM-3A
Se reutilizó el patrón validado en P0: policy compuesta + `permission` + fallback `CanReadAch/CanManageAch`.

## 3. Controladores P1 migrados
- BulkIngestionController
- IncomingNachaCommandCenterController
- NachaUploadController
- NachaController (POST header)

## 4. Controladores P1 no encontrados
Ninguno del grupo permitido.

## 5. Policies compuestas P1 creadas
`P1Policies`: BulkIngestionRead/Upload/Retry/Cancel, CommandCenterRead/Retry/Unblock/Requeue/MarkFailedFinal, NachaRead/Upload/Generate/Export.

## 6. Mapeo endpoint → policy anterior → policy compuesta
Se migraron las acciones del grupo permitido desde `CanReadAch`/`CanManageAch` a su policy P1 correspondiente con fallback legacy.

## 7. Compatibilidad temporal con CanReadAch/CanManageAch
Todas las policies P1 usan `RequireAssertion` y autorizan por permiso fino o legacy equivalente.

## 8. Validación de no tocar P0/P2/P3
`git diff -- src/Cfa.ACHInterbank.Api/Controllers` muestra cambios solo en controladores permitidos del grupo P1.

## 9. Resultado pruebas específicas
Filtro de autorización/seguridad incluyendo P1: `33/33` exitosas.

## 10. Resultado suite completa
Suite completa backend: `425/425` exitosas.

## 11. Resultado build final
`dotnet build ACHInterbank.sln -c Release`: exitoso.

## 12. Resultado OpenAPI P1 grupo 1
`TOTAL_P1_GRUPO1_OPENAPI=68`, `P1_GRUPO1_SIN_SECURITY=0`.

## 13. Riesgos restantes
- El filtro de validación OpenAPI por keyword `nacha` incluye endpoints adicionales fuera del subgrupo estricto; aun así todos quedaron con `security`.
- Pendiente migración de P1 grupo 2 y posteriores.

## 14. Qué NO se implementó
## Qué NO se implementó en Scalar-SEC-PERM-4
- No se migraron controladores P2/P3.
- No se eliminaron CanReadAch ni CanManageAch.
- No se exigieron permisos finos de forma exclusiva.
- No se cambiaron rutas.
- No se cambiaron contratos.
- No se cambiaron DTOs.
- No se cambiaron roles reales.
- No se crearon migraciones.
- No se declara producción lista.

## 15. Veredicto
Scalar-SEC-PERM-4 CERRADO para P1 grupo 1.
