# Solicitudes de evidencia Seguridad/Compliance UAT - Fase 6D.5

Productivo permanece NO-GO. Las evidencias deben ser sanitizadas y no incluir secretos, URLs reales, certificados, thumbprints, contrasenas, datos reales ni payloads completos.

| ID | Evidencia esperada | Responsable | Formato | Estado |
| --- | --- | --- | --- | --- |
| SECEVI-001 | Evidencia de ambiente aislado | CFA Tecnologia | Acta/captura sanitizada | Pendiente |
| SECEVI-002 | Evidencia de no datos reales | Mesa UAT | Declaracion dataset + muestra anonimizada | Pendiente |
| SECEVI-003 | Evidencia CI/Playwright | DevOps | Artefactos `playwright-report`, `test-results` | Listo |
| SECEVI-004 | Evidencia ausencia de secretos en repo | CFA Seguridad | Revision/acta | Pendiente |
| SECEVI-005 | Evidencia custodia aprobada | CFA Seguridad | Registro OpenBao/Vault sin valores | Pendiente |
| SECEVI-006 | Evidencia validacion certificados | CFA Seguridad | Acta sin certificados ni thumbprints reales | Pendiente |
| SECEVI-007 | Evidencia aprobacion endpoints | CFA Tecnologia + Seguridad | Matriz sin URLs reales | Pendiente |
| SECEVI-008 | Evidencia logging sanitizado | CFA Tecnologia | Extractos sanitizados | Pendiente |
| SECEVI-009 | Evidencia NO-GO productivo | Auditoria/Compliance | Acta/comite | Pendiente |
| SECEVI-010 | Evidencia aprobacion formal | Seguridad/Compliance/Tecnologia | Decision firmada | Pendiente |

## Criterio de aceptacion

La evidencia puede aprobar preparacion de UAT externo aislado, pero no certificacion oficial ni salida productiva.
