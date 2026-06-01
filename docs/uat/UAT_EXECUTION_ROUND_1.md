# Ejecucion UAT controlada ronda 1 - Fase 6D.1

## Datos de ejecucion

- Fecha: 2026-06-01.
- Commit base informado: `57a5f8da2631f6ead4503bb9cbe7bd8f838c303a`.
- Ambiente: repositorio local/CI documental con evidencia automatizada existente; sin ambiente UAT externo conectado.
- Datos: sinteticos, anonimizados y semirreales. Sin datos reales de clientes.
- Restricciones: Productivo NO-GO, sin SOAP real, sin movimientos monetarios, sin mutaciones criticas, sin legacy oficial, sin `/NachaExport/{hash}`.

## Alcance ejecutado

- Revision de escenarios 6C.10 contra dataset sintetico 6D.1.
- Evidencia automatizada Playwright/CI y golden files semirreales.
- Registro de resultados por escenario para habilitar UAT formal ampliado.

## Exclusiones

- Certificacion oficial ACH Colombia/CENIT.
- Transmision de archivos reales o productivos.
- SOAP real y certificados reales.
- Carga de datos reales o movimientos monetarios.

## Resultados por escenario

| ID escenario | Camara | Objetivo | Dataset | Pasos resumidos | Evidencia esperada | Resultado | Observacion | Defecto asociado |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| UAT-ACH-001 | ACH Colombia | Salida NACHA-M valida | DS-ACH-OUT-001 | Validar golden/evidencia salida | Golden file, matriz RTM | Observado | Cubierto tecnicamente con golden semirreal; falta UAT formal externo | UAT-FND-001 |
| UAT-ACH-002 | ACH Colombia | Entrada NACHA-M valida | DS-ACH-IN-001 | Validar golden/evidencia entrada | Golden file, dashboard | Observado | Sin ingesta en ambiente UAT externo | UAT-FND-002 |
| UAT-ACH-003 | ACH Colombia | `.RET` ACH | DS-ACH-RET-001 | Validar `.RET` semirreal | Golden `.RET`, conciliacion | Observado | Falta contraste oficial de causales | UAT-FND-001 |
| UAT-ACH-004 | ACH Colombia | Rechazo/devolucion | DS-ACH-RET-001 | Revisar causal y conciliacion | Conciliacion ACH | Observado | Causales requieren homologacion formal | UAT-FND-003 |
| UAT-ACH-005 | ACH Colombia | Prenotificacion aprobada | DS-PRENOTE-ACH-APP | Revisar evidencia prenote aprobada | Reporte sanitizado | OK | No monetario, evidencia sintetica disponible | No aplica |
| UAT-ACH-006 | ACH Colombia | Prenotificacion rechazada | DS-PRENOTE-ACH-REJ | Revisar evidencia prenote rechazada | Reporte sanitizado | OK | No monetario, evidencia sintetica disponible | No aplica |
| UAT-ACH-007 | ACH Colombia | Naming `RRRRTTT.ZZZ.1` | DS-ACH-OUT-001 | Validar naming en evidencia | Archivo/evidencia UAT | Observado | Requiere validacion oficial ACH Colombia | UAT-FND-001 |
| UAT-ACH-008 | ACH Colombia | FileIdModifier | DS-ACH-OUT-001 | Validar regla 001-036 | Tests/golden | OK | Cubierto por pruebas automatizadas | No aplica |
| UAT-ACH-009 | ACH Colombia | Totales/padding/fixed-width | DS-ACH-OUT-001 | Validar totals/padding/106 | Golden/tests | OK | Cubierto por pruebas/golden semirreales | No aplica |
| UAT-CEN-001 | CENIT | Salida CENIT | DS-CEN-OUT-001 | Validar salida CENIT semirreal | Golden CENIT | Observado | Falta contraste formal Banco Republica/CENIT | UAT-FND-001 |
| UAT-CEN-002 | CENIT | Entrada CENIT | DS-CEN-IN-001 | Validar entrada CENIT semirreal | Golden CENIT | Observado | Sin ingesta en ambiente UAT externo | UAT-FND-002 |
| UAT-CEN-003 | CENIT | `.RET` CENIT | DS-CEN-RET-001 | Validar `.RET` CENIT | Golden `.RET` | Observado | Falta contraste oficial de causales | UAT-FND-003 |
| UAT-CEN-004 | CENIT | Reglas diferenciales CENIT | DS-CEN-RET-001, DS-PRENOTE-CENIT | Revisar causales/respuestas | Reportes sanitizados | Observado | Requiere validacion con Anexos A/B en UAT formal | UAT-FND-003 |
| UAT-CEN-005 | CENIT | Ciclos/colas/neteo | DS-DASH-001 | Revisar visibilidad operacional | Dashboard/read-store | No ejecutado | Requiere datos/ciclos UAT CENIT representativos | UAT-FND-004 |
| UAT-CEN-006 | CENIT | Conciliacion CENIT | DS-ROR-001, DS-CONC-001 | Revisar cruce sintetico | Consola conciliacion | OK | Cubierto por read-only/Playwright y tests ROR | No aplica |
| UAT-TRV-001 | Ambas | Dashboard operativo | DS-DASH-001 | Revisar evidencia Playwright | `playwright-report` | OK | Guard NO-GO/read-only cubierto | No aplica |
| UAT-TRV-002 | Ambas | Detalle archivo | DS-DASH-001 | Revisar detalle operativo | `playwright-report` | OK | Sanitizacion cubierta | No aplica |
| UAT-TRV-003 | Ambas | Consola SOAP/UAT | DS-SOAP-UAT-001 | Revisar consola read-only | `playwright-report` | OK | SOAP real bloqueado | No aplica |
| UAT-TRV-004 | Ambas | Conciliacion ACH | DS-CONC-001 | Revisar consola conciliacion | `playwright-report` | OK | Sin mutaciones criticas | No aplica |
| UAT-TRV-005 | Ambas | Config profiles | DS-CONFIG-001 | Revisar perfiles read-only | `playwright-report` | OK | Legacy no oficial | No aplica |
| UAT-TRV-006 | Ambas | Legacy deprecated | DS-LEGACY-001 | Revisar guard legacy | `playwright-report` | OK | Deprecated/read-only | No aplica |
| UAT-TRV-007 | Ambas | Export `cycleId` / no hash | DS-EXPORT-001 | Revisar guard export | `playwright-report` | OK | No `/NachaExport/{hash}` | No aplica |
| UAT-TRV-008 | Ambas | Playwright evidence | DS-DASH-001 | Revisar artefactos CI | `playwright-report`, `test-results` | OK | CI publica evidencia | No aplica |

## Resumen

| Total escenarios | OK | Observado | Bloqueado | No ejecutado |
| --- | --- | --- | --- | --- |
| 23 | 13 | 9 | 0 | 1 |

## Cierre ronda 1

La ronda 1 es controlada y no oficial. El resultado permite continuar con UAT formal ampliado, pero no habilita productivo ni certificacion. Productivo permanece NO-GO.
