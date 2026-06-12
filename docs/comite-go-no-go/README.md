# Paquete Comite GO/NO-GO - ACH Interbank

Fecha de actualizacion: 2026-06-12
Version: 1.1 cierre tecnico G3.5-G3.6
Estado general: Continuar UAT controlado / NO-GO productivo
Scorecard vigente: 68.1 / 100

## Proposito

Este paquete consolida el estado tecnico, funcional, operativo y de readiness del proyecto ACH Interbank para soporte del comite GO/NO-GO.

## Audiencia

- Comite tecnico.
- Negocio.
- Seguridad.
- Operaciones.
- Auditoria.
- Direccion.

## Estado General

El proyecto cerro tecnicamente G3.5-G3.6, incluyendo naming dinamico, cleanup, PostgreSQL/Quartz real y dry-run inbound/outbound. Mantiene brechas normativas, operativas, de seguridad, interoperabilidad externa y aprobacion humana que bloquean una salida productiva.

Decision recomendada:

- Continuar UAT controlado.
- Mantener NO-GO productivo.

## Indice

| Documento | Proposito |
| --- | --- |
| 00_RESUMEN_EJECUTIVO_COMITE.md | Resumen para direccion y comite. |
| 01_DECISION_GO_NO_GO.md | Decision recomendada y condiciones minimas. |
| 02_SCORECARD_READINESS.md | Scorecard consolidado de readiness. |
| 03_EVIDENCIAS_TECNICAS.md | Evidencias tecnicas y de runtime. |
| 04_EVIDENCIAS_UAT.md | Evidencias UAT tecnico y funcional sintetico. |
| 05_BRECHAS_CRITICAS.md | Brechas abiertas que bloquean productivo. |
| 06_PLAN_CIERRE_BRECHAS.md | Plan ejecutivo de cierre. |
| 07_RIESGOS_Y_ACEPTACIONES.md | Riesgos y aceptaciones requeridas. |
| 08_RECOMENDACION_FINAL.md | Recomendacion formal del paquete. |
| 09_ANEXOS_TECNICOS.md | Referencias a anexos y documentos base. |

## Advertencias

- No contiene passwords.
- No contiene tokens completos.
- No contiene datos reales.
- No contiene certificados privados.
- No constituye aprobacion productiva.
- G3.6B no demuestra causalidad NachaExport -> Proc_Contrapartidas.
- Los exports binarios existentes son anteriores a este cierre hasta que se regeneren mediante proceso aprobado.
