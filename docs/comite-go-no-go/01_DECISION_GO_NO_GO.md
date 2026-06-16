# Decision GO/NO-GO - ACH Interbank

Fecha de actualizacion: 2026-06-12
Decision recomendada: NO-GO productivo
Decision alternativa permitida: Continuar UAT controlado

## Decision Recomendada

La recomendacion para el comite es mantener NO-GO productivo. El proyecto puede continuar UAT tecnico/funcional controlado con datos sinteticos, sin exponer integraciones reales ni procesos bancarios productivos.

## Razones del NO-GO Productivo

- UAT funcional formal con actas no esta completo.
- NACHA-M requiere validacion campo-a-campo y homologacion o waiver.
- CENIT/CUD esta pendiente.
- Sobre digital, firma y certificados estan pendientes.
- Backup/restore/rollback no esta validado.
- Evidencia visual/operativa y aprobaciones formales siguen pendientes.

## Matriz de Decision

| Criterio | Estado | Evidencia | Bloquea productivo | Observacion |
| --- | --- | --- | --- | --- |
| CI | OK | dotnet-ci y angular-ci reportados OK | No | Validacion tecnica base disponible. |
| Runtime | OK | Docker, PostgreSQL, API y SPA OK | No | Ambiente local/controlado estable. |
| UAT tecnico | OK con observaciones | docs/uat/EJECUCION_UAT_TECNICO_BASICO.md | No | No equivale a aprobacion productiva. |
| UAT tecnico G3.6 | GO tecnico | Commit `e5721150`; G3.6A/G3.6B 4/4 | No por si solo | PostgreSQL/Quartz reales y SOAP dry-run. |
| UAT funcional/formal | PARCIALMENTE OK | docs/uat/UAT_FUNCIONAL_SINTETICO.md | Si | Falta UAT formal y actas. |
| Seguridad | PARCIAL | docs/security/REVISION_SEGURIDAD_PRE_GO_LIVE.md | Si | Pendientes de custodia de secretos y certificados según alcance. |
| Backup/restore | PENDIENTE | docs/go-live-readiness/BRECHAS_CRITICAS_GO_LIVE.md | Si | Bloqueante operacional. |
| NACHA-M | OK TECNICO / PARCIAL NORMATIVO | Commits `7c3cbb21`, `e5721150` | Si | Naming/generacion/parseo cerrados; requiere homologacion externa. |
| CENIT/CUD | PENDIENTE | docs/go-live-readiness/BRECHAS_CRITICAS_GO_LIVE.md | Si | Interoperabilidad externa no cerrada. |
| Actas | PENDIENTE | docs/uat/ACTA_TECNICA_PRELIMINAR.md | Si | Faltan aprobaciones formales. |

## Condiciones Minimas Para Reconsiderar GO

- Cerrar brechas criticas y altas.
- Completar UAT funcional formal con actas.
- Completar NACHA-M campo-a-campo y homologacion o waiver.
- Validar CENIT/CUD.
- Validar sobre digital, firma, certificados y secretos.
- Ejecutar backup/restore/rollback.
- Obtener aprobaciones de negocio, seguridad, operaciones, auditoria y direccion.

G3.6B no prueba causalidad NachaExport -> Proc_Contrapartidas. No hubo SOAP real ni movimiento monetario. La decision permanece **NO-GO productivo**.
