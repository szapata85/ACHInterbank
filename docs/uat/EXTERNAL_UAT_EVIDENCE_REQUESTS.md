# Solicitudes de evidencia UAT externo - Fase 6D.4

Productivo permanece NO-GO. Las evidencias deben ser sanitizadas y no incluir secretos, datos reales no autorizados, payloads completos ni certificados.

| ID | Fuente | Evidencia requerida | Formato esperado | Responsable | Estado |
| --- | --- | --- | --- | --- | --- |
| EVI-CFA-001 | CFA | Paquete UAT interno completo | Enlaces docs/uat + artefactos CI | Mesa UAT | Listo |
| EVI-CFA-002 | CFA | `playwright-report`, `playwright-test-results`, `uat-evidence-playwright` | Artefactos CI | DevOps | Listo |
| EVI-CFA-003 | CFA | Archivos NACHA-M sinteticos/golden semirreales | Archivo o hash/control interno sanitizado | CFA Tecnologia | Listo |
| EVI-CFA-004 | CFA | Matriz RTM requisito-norma-codigo-prueba | Markdown/PDF interno | QA/UAT | Listo |
| EVI-ACH-001 | ACH Colombia | Acuse o resultado validacion MAN-004 V32 | Acta/correo/evidencia oficial sanitizada | ACH Colombia | Pendiente |
| EVI-ACH-002 | ACH Colombia | Validacion naming, `.RET`, causales y prenotes | Resultado por escenario | ACH Colombia | Pendiente |
| EVI-CEN-001 | CENIT/Banco Republica | Resultado validacion DSP-152/Anexos | Acta/correo/evidencia oficial sanitizada | CENIT | Pendiente |
| EVI-CEN-002 | CENIT/Banco Republica | Ciclo/cola/neteo sintetico | Evidencia de ventana y resultado | CENIT | Pendiente |
| EVI-SEC-001 | Seguridad | Validacion certificados/endpoints sin secretos | Acta o aprobacion | CFA Seguridad | Pendiente |
| EVI-SEC-002 | Seguridad | Confirmacion de segregacion UAT | Evidencia ambiente aislado | CFA Seguridad | Pendiente |
| EVI-CONC-001 | CFA/terceros | Conciliacion de respuestas/.RET/ROR | Reporte sanitizado | CFA Operaciones | Pendiente |
| EVI-COM-001 | Comite UAT | Decision UAT externo | Acta de decision | Auditoria/Compliance | Pendiente |

## Formato minimo

- Fecha, ventana, responsable y version/commit.
- Escenario asociado.
- Resultado: OK, Observado, Bloqueado o No ejecutado.
- Evidencia sanitizada.
- Defecto/hallazgo asociado si aplica.
