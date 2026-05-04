# Scalar-SEC-PERM-FINAL — Cierre ejecutivo-técnico del programa de permisos finos

## 1. Resumen ejecutivo
El programa Scalar-SEC-PERM se cierra para el alcance ejecutado con autorización explícita, metadata OpenAPI, permisos finos P0/P1 seleccionados y deuda legacy controlada.

## 2. Alcance del programa Scalar-SEC-PERM
Incluye endurecimiento de autorización, registro de policies finas y migraciones priorizadas P0/P1 con compatibilidad OR hacia permisos legacy para transición segura.

## 3. Línea de tiempo de cierres
SEC-5 y PERM-1 a PERM-7 cerrados en fases incrementales con validaciones técnicas y evidencia documental.

## 4. Estado por bloque
- PERM-1: diseño de permisos finos.
- PERM-2 / PERM-2A: constantes y registro de policies.
- PERM-3 / PERM-3A: P0 migrado y compatibilidad OR validada.
- PERM-4 / PERM-4A: P1 grupo 1 migrado/corregido.
- PERM-5: certificados, sobre digital y NACHA Security.
- PERM-6: usuarios, roles, permisos y configuración operativa seleccionada.
- PERM-7: diagnóstico, limpieza técnica y deuda legacy controlada.

## 5. Controladores migrados
Se migraron los controladores priorizados de P0/P1 definidos en PERM-3 a PERM-6, manteniendo compatibilidad con claims legacy en policies de transición.

## 6. Policies finas implementadas
Se implementaron `P0Policies`, `P1Policies` y `FineGrainedPermissions` para transacciones, trazabilidad, devoluciones, bulk ingestion, command center, certificados, sobre digital, NACHA security, usuarios, roles, permisos y configuración.

## 7. Compatibilidad legacy
Se mantuvo compatibilidad OR entre permiso fino y `CanReadAch`/`CanManageAch` durante la fase de transición.

## 8. Evidencia técnica consolidada
- Build Release: OK.
- Suite completa previa PERM-6D: 440/440 OK.
- TOTAL_OPERACIONES_OPENAPI=213.
- OPERACIONES_CON_SECURITY=207.
- OPERACIONES_SIN_SECURITY=6.
- OPERACIONES_SIN_SECURITY_NO_ESPERADAS=0.
- ESCRITURAS_SIN_SECURITY_NO_ESPERADAS=0.
- TOTAL_CONTROLLERS_LEGACY=22.
- Los 6 endpoints sin security son públicos esperados:
  - POST /Auth/login.
  - POST /Auth/forgot-password.
  - POST /Auth/reset-password.
  - GET /api/users/branding.
  - POST /Oauths/GenerateToken.
  - POST /Oauths/GenerateTokenAsync.

## 9. OpenAPI y metadata security
La evidencia consolidada indica cobertura de seguridad sin huecos inesperados para el alcance ejecutado y metadata de seguridad explícita en rutas críticas evaluadas.

## 10. Deuda legacy controlada
- Existen controladores que aún usan CanReadAch/CanManageAch directo.
- La deuda fue aceptada y documentada en PERM-7.
- IntegrationMappingSetsController fue identificado como candidato principal futuro.
- AchCyclesController queda como candidato secundario.
- ReportsController no se recomienda migrar en esta fase por predominio de lectura y alto volumen.
- Esta deuda no representa hueco inesperado OpenAPI según evidencia, pero sí requiere plan posterior.

## 11. Riesgos residuales
Persisten riesgos de mantenimiento por coexistencia de policies finas y legacy hasta completar fases futuras de migración focalizada.

## 12. Criterios para avanzar a preproducción
- Build Release reproducible.
- Suite automatizada en verde.
- OpenAPI sin huecos inesperados de security.
- JWT/claims validados en ambiente controlado.
- Variables de ambiente definidas.
- Base de datos y migraciones controladas.
- Secretos no embebidos en appsettings.
- Logs básicos operativos.
- Plan de rollback.
- Aprobación técnica interna.

## 13. Criterios que bloquean producción
- Pentest pendiente.
- UAT de seguridad pendiente.
- Validación real de roles/claims del IdP pendiente.
- Gestión definitiva de secretos/OpenBao pendiente.
- Hardening de infraestructura pendiente.
- Monitoreo/SIEM pendiente.
- Backup/restore pendiente.
- Pruebas de carga pendientes.
- Aprobación formal de seguridad/cumplimiento pendiente.

## Qué NO se implementó en Scalar-SEC-PERM

- No se migraron todos los controladores del sistema.
- No se migraron todos los controladores legacy restantes.
- No se eliminó CanReadAch ni CanManageAch.
- No se exigieron permisos finos de forma exclusiva.
- No se cambiaron rutas.
- No se cambiaron contratos.
- No se cambiaron DTOs.
- No se cambiaron roles reales.
- No se crearon migraciones.
- No se ejecutó pentest.
- No se ejecutó UAT de seguridad.
- No se validó OpenBao como gestión definitiva de secretos.
- No se validó infraestructura productiva.
- No se declara producción lista.

## 15. Veredicto ejecutivo
- Scalar-SEC-PERM: CERRADO para el alcance ejecutado.
- Permisos finos P0/P1 seleccionados: implementados y validados.
- OpenAPI sin huecos inesperados de security: SÍ.
- Deuda legacy restante: aceptada y documentada.
- Puede avanzar a evaluación de preproducción: SÍ.
- Producción lista: NO.

## 16. Siguiente fase recomendada
Scalar-PROD-READINESS-1 — Evaluación técnica para paso a preproducción.
