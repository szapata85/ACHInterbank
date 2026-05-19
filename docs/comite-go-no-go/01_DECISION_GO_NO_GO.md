# Decision GO/NO-GO - ACH Interbank

Fecha: 2026-05-19
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
- ACH.Operator no esta asignado o visible para el usuario demo segun brecha vigente.
- Evidencia visual/operativa y aprobaciones formales siguen pendientes.

## Condiciones Minimas Para Reconsiderar GO

- Cerrar brechas criticas y altas documentadas.
- Completar UAT funcional formal con actas firmadas.
- Completar validacion NACHA-M campo-a-campo y homologacion o waiver.
- Validar CENIT/CUD y flujo de interoperabilidad externa aplicable.
- Validar sobre digital, firma, certificados y manejo de secretos.
- Ejecutar y aprobar backup/restore/rollback.
- Completar evidencia operativa, soporte, monitoreo y procedimientos.
- Obtener aprobaciones de negocio, seguridad, operaciones, auditoria y direccion.

## Aprobaciones Requeridas

- Aprobacion tecnica.
- Aprobacion funcional/negocio.
- Aprobacion de seguridad.
- Aprobacion de operaciones.
- Aprobacion de auditoria/compliance.
- Aprobacion de direccion.

## Matriz de Decision

| Criterio | Estado | Evidencia | Bloquea productivo | Observacion |
| --- | --- | --- | --- | --- |
| CI | OK | dotnet-ci y angular-ci reportados OK | No | Validacion tecnica base disponible. |
| Runtime | OK | Docker, PostgreSQL, API y SPA OK | No | Ambiente local/controlado estable. |
| UAT tecnico | OK con observaciones | docs/uat/EJECUCION_UAT_TECNICO_BASICO.md | No | No equivale a aprobacion productiva. |
| UAT funcional | PARCIALMENTE OK | docs/uat/UAT_FUNCIONAL_SINTETICO.md | Si | Falta UAT formal y actas. |
| Seguridad | PARCIAL | docs/security/REVISION_SEGURIDAD_PRE_GO_LIVE.md | Si | Pendientes secretos/certificados/OpenBao segun alcance. |
| Operacion | PARCIAL | docs/operations/RUNBOOK_UAT_Y_PREPRODUCTIVO.md | Si | Falta evidencia operativa final. |
| Backup/restore | PENDIENTE | docs/go-live-readiness/BRECHAS_CRITICAS_GO_LIVE.md | Si | Bloqueante operacional. |
| NACHA-M | PARCIAL | docs/go-live-readiness/MATRIZ_NACHA_M_LAYOUTS.md | Si | Requiere validacion campo-a-campo y homologacion/waiver. |
| CENIT/CUD | PENDIENTE | docs/go-live-readiness/BRECHAS_CRITICAS_GO_LIVE.md | Si | Interoperabilidad externa no cerrada. |
| Sobre digital | PENDIENTE | docs/go-live-readiness/BRECHAS_CRITICAS_GO_LIVE.md | Si | Pendiente firma/certificados. |
| Certificados | PENDIENTE | docs/security/REVISION_SEGURIDAD_PRE_GO_LIVE.md | Si | No incluir certificados privados en documentacion. |
| Actas | PENDIENTE | docs/uat/ACTA_TECNICA_PRELIMINAR.md | Si | Faltan aprobaciones formales. |
| Aprobaciones | PENDIENTE | Este paquete | Si | Requiere comite final posterior. |
