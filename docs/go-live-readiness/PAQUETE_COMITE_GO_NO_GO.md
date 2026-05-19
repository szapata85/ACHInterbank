# Paquete de Comite GO/NO-GO - ACH Interbank

Fecha: 2026-05-19
Version: 1.0 preliminar
Estado recomendado: Continuar UAT controlado / NO-GO productivo
Scorecard vigente: 67.6 / 100

## Paquete Formal

El paquete formal de comite se consolida en:

- docs/comite-go-no-go/

Indice principal:

- docs/comite-go-no-go/README.md
- docs/comite-go-no-go/00_RESUMEN_EJECUTIVO_COMITE.md
- docs/comite-go-no-go/01_DECISION_GO_NO_GO.md
- docs/comite-go-no-go/02_SCORECARD_READINESS.md
- docs/comite-go-no-go/03_EVIDENCIAS_TECNICAS.md
- docs/comite-go-no-go/04_EVIDENCIAS_UAT.md
- docs/comite-go-no-go/05_BRECHAS_CRITICAS.md
- docs/comite-go-no-go/06_PLAN_CIERRE_BRECHAS.md
- docs/comite-go-no-go/07_RIESGOS_Y_ACEPTACIONES.md
- docs/comite-go-no-go/08_RECOMENDACION_FINAL.md
- docs/comite-go-no-go/09_ANEXOS_TECNICOS.md

## Decision Recomendada

No se recomienda salida productiva.

Decision alternativa permitida:

- Continuar UAT controlado con datos sinteticos/anonimizados.
- Mantener bloqueo productivo hasta cerrar brechas criticas.
- Ejecutar UAT formal, validaciones de interoperabilidad, seguridad, operacion y aprobaciones.

## Estado Consolidado

| Area | Estado | Evidencia |
| --- | --- | --- |
| dotnet-ci | OK | docs/comite-go-no-go/03_EVIDENCIAS_TECNICAS.md |
| angular-ci | OK | docs/comite-go-no-go/03_EVIDENCIAS_TECNICAS.md |
| Backend local | OK | docs/comite-go-no-go/03_EVIDENCIAS_TECNICAS.md |
| Angular local | OK | docs/comite-go-no-go/03_EVIDENCIAS_TECNICAS.md |
| Docker runtime | OK | docs/go-live-readiness/DOCKER_RUNTIME_READINESS.md |
| PostgreSQL | OK | docs/uat/EVIDENCIA_TECNICA_UAT_RUNTIME.md |
| SPA Docker | OK | docs/comite-go-no-go/03_EVIDENCIAS_TECNICAS.md |
| Proxy SPA -> API | OK | docs/uat/EVIDENCIAS_UAT_TECNICO_BASICO.md |
| Auth/login | OK | docs/uat/EJECUCION_UAT_TECNICO_BASICO.md |
| Navigation/menu | OK | docs/uat/EVIDENCIAS_UAT_TECNICO_BASICO.md |
| UAT tecnico autenticado | OK con observaciones | docs/comite-go-no-go/04_EVIDENCIAS_UAT.md |
| UAT funcional sintetico | PARCIALMENTE OK | docs/uat/UAT_FUNCIONAL_SINTETICO.md |
| NU1903 | Corregido tecnicamente | docs/security/REVISION_SEGURIDAD_PRE_GO_LIVE.md |
| Scorecard | 67.6 / 100 | docs/go-live-readiness/SCORECARD_GO_LIVE_READINESS.md |

## Defectos y Brechas Clave

Defectos cerrados segun estado actual:

- DEF-UAT-011.
- DEF-UAT-012.
- DEF-UAT-016.
- DEF-UAT-017.
- DEF-UAT-018 documentalmente.
- DEF-UAT-019 tecnicamente por endpoint/proxy.

Brechas abiertas relevantes:

- DEF-UAT-015: ACH.Operator no asignado/no visible para usuario demo.
- DEF-UAT-020: NACHA-M 1/5/6/7/8/9 requiere validacion campo-a-campo y homologacion/waiver.
- UAT funcional formal con actas pendiente.
- Evidencia visual/operativa pendiente.
- CENIT/CUD pendiente.
- Sobre digital/firma/certificados pendiente.
- OpenBao/secrets pendiente segun alcance.
- Backup/restore/rollback pendiente.
- UAT bancario formal pendiente.

Detalle consolidado:

- docs/comite-go-no-go/05_BRECHAS_CRITICAS.md
- docs/comite-go-no-go/06_PLAN_CIERRE_BRECHAS.md

## Recomendacion al Comite

Decision propuesta:

- Productivo: NO-GO.
- UAT controlado: continuar con observaciones.
- Proximo hito: cerrar brechas bloqueantes y convocar nuevo comite GO/NO-GO.

## Advertencia

Este documento no contiene passwords, tokens completos, datos reales ni certificados privados. Tampoco constituye aprobacion productiva.
