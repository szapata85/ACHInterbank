## 1. Resumen ejecutivo.
Se consolida la evidencia final de Scalar-SEC-PERM-5 para la migración P1 grupo 2 en certificados, sobre digital y operaciones NACHA Security, con validación de pruebas, build y OpenAPI.

## 2. Contexto.
PERM-5-2C quedó CERRADO. Commit de migración previo: 80003e10b965867299fe203f7795730cd587fa1c. Commit de pruebas/rescate: 5c5a92541ebf8746941c7c15a5242302b428a226.

## 3. Alcance.
Se documenta únicamente la evidencia de migración P1 grupo 2 ya implementada y validada.

## 4. Controladores migrados.
- CertificateManagementController.
- DigitalEnvelopeCertificatesController.
- SobreDigitalController.
- NachaSecurityOperationsController.

## 5. Policies P1 usadas.
- P1.Certificates*
- P1.DigitalEnvelope*
- P1.NachaSecurity*

## 6. Patrón controller/action aplicado.
[Authorize] a nivel de clase sin Policy y [Authorize(Policy = P1Policies.X)] en acciones migradas para evitar composición AND implícita no deseada.

## 7. Compatibilidad con CanReadAch y CanManageAch.
Se conserva compatibilidad OR por policy en DI: permiso fino correspondiente o legacy compatible según lectura/operación.

## 8. Pruebas agregadas/actualizadas.
- P1Group2FineGrainedPolicyMigrationTests.
- Ajustes en NachaSecurityOperationsControllerTests.
- Ajustes en AuthorizationUniformityP1P2ControllersTests.

## 9. Resultado de pruebas específicas.
Pruebas específicas: 37/37 OK.

## 10. Resultado de suite completa.
Suite completa: 429/429 OK.

## 11. Resultado de build final.
Build final: OK, 0 errores.

## 12. Resultado OpenAPI.
TOTAL_P1_GRUPO2_OPENAPI=23.
P1_GRUPO2_SIN_SECURITY=0.

## 13. Riesgos restantes.
Persisten superficies no migradas fuera de P1 grupo 2; la adopción total de permisos finos requiere fases posteriores.

## 14. Qué NO se implementó.
## Qué NO se implementó en Scalar-SEC-PERM-5
- No se migraron controladores fuera del grupo P1 certificados/sobre digital/NACHA Security.
- No se migraron P2/P3.
- No se eliminaron CanReadAch ni CanManageAch.
- No se exigieron permisos finos de forma exclusiva.
- No se cambiaron rutas.
- No se cambiaron contratos.
- No se cambiaron DTOs.
- No se cambiaron roles reales.
- No se crearon migraciones.
- No se declara producción lista.

## 15. Veredicto.
- Scalar-SEC-PERM-5: CERRADO.
- P1 grupo 2 migrado: SÍ.
- P2/P3 migrados: NO.
- Producción lista: NO.

## 16. Siguiente paso recomendado.
Scalar-SEC-PERM-6 — Migración controlada P1 grupo 3: usuarios, roles, permisos y configuración operativa.
