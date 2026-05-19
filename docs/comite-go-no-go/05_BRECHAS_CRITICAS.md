# Brechas Criticas - Comite GO/NO-GO

Fecha: 2026-05-19
Estado general: Brechas bloqueantes para productivo

## Brechas Abiertas Consolidadas

| ID | Brecha | Severidad | Impacto | Bloquea productivo | Accion requerida | Responsable sugerido |
| --- | --- | --- | --- | --- | --- | --- |
| DEF-UAT-015 | ACH.Operator no asignado/no visible para usuario demo | Alta | Afecta validacion de autorizacion por rol operador | Si | Corregir seed/claims o aprobar usuario operador sintetico formal | Seguridad / Backend |
| DEF-UAT-020 | NACHA-M 1/5/6/7/8/9 requiere validacion campo-a-campo y homologacion/waiver | Critica | Riesgo de interoperabilidad y cumplimiento ACH/NACHA-M | Si | Ejecutar matriz campo-a-campo con archivo sintetico y obtener aprobacion | Arquitectura ACH / QA |
| UAT-FORMAL | UAT funcional formal con actas pendiente | Critica | No hay aceptacion funcional formal | Si | Ejecutar UAT formal, evidencias y firmas | Negocio / QA |
| EVI-VISUAL | Evidencia visual/operativa pendiente | Alta | Debilita trazabilidad de aprobacion | Si | Completar evidencia de pantallas, flujos y operacion | QA / Operaciones |
| CENIT-CUD | CENIT/CUD pendiente | Critica | Interoperabilidad externa no validada | Si | Definir alcance, pruebas sinteticas y homologacion | Arquitectura Integracion |
| SOBRE-DIGITAL | Sobre digital/firma/certificados pendiente | Critica | Riesgo de seguridad e interoperabilidad | Si | Validar firma, certificados y manejo seguro | Seguridad / Integracion |
| OPENBAO | OpenBao/secrets pendiente segun alcance | Alta | Riesgo de gestion de secretos | Si | Definir alcance y completar validacion de secretos | Seguridad / DevOps |
| BKP-RESTORE | Backup/restore/rollback pendiente | Critica | Riesgo operativo ante incidente | Si | Ejecutar prueba documentada de recuperacion y rollback | Operaciones / SRE |
| ENV-SECRETS | .env/secrets pendientes si aplica | Alta | Riesgo de exposicion o configuracion insegura | Si | Revisar inventario de secretos y controles | Seguridad / DevOps |
| ACTAS-APR | Actas y aprobaciones formales pendientes | Critica | No existe autorizacion formal de salida | Si | Obtener actas de negocio, seguridad, operaciones y direccion | PMO / Direccion |
| UAT-BANCARIO | UAT bancario formal pendiente | Critica | Validacion externa/formal no concluida | Si | Coordinar y ejecutar UAT bancario formal | Negocio / Interoperabilidad |

## Fuente

Documento base: docs/go-live-readiness/BRECHAS_CRITICAS_GO_LIVE.md

Esta consolidacion no cierra brechas. Solo las organiza para decision de comite y plan de cierre.
