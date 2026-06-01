# Dataset sintetico UAT - Fase 6D.1

## Proposito

Consolidar datos sinteticos/anonimizados para ejecutar la primera ronda UAT controlada ACH Colombia/CENIT sin usar datos reales, sin SOAP real, sin movimientos monetarios y sin mutaciones criticas. Productivo permanece NO-GO.

## Alcance

- Camaras: ACH Colombia y CENIT.
- Tipos: salida NACHA-M, entrada NACHA-M, `.RET`, rechazo/devolucion, prenotificacion aprobada/rechazada, ROR, conciliacion y SOAP/UAT read-only.
- Evidencias base: golden files semirreales, reportes UAT sinteticos existentes, Playwright/CI y matriz trazable.

## Reglas

- No usar datos reales de clientes.
- Cuentas, documentos, trazas y correlation ids deben estar enmascarados o ser sinteticos.
- Golden files son semirreales, anonimizados y no sustituyen certificacion oficial.
- No se ejecuta SOAP real ni se transmiten archivos productivos.
- No se modifica `tests/Cfa.ACHInterbank.Tests/TestData/Nacha/GoldenFiles`.

## Tabla dataset

| ID dataset | Camara | Escenario asociado | Archivo/fixture/evidencia | Tipo de dato | Sensibilidad | Estado |
| --- | --- | --- | --- | --- | --- | --- |
| DS-ACH-OUT-001 | ACH Colombia | UAT-ACH-001, UAT-ACH-007, UAT-ACH-008, UAT-ACH-009 | `tests/Cfa.ACHInterbank.Tests/TestData/Nacha/GoldenFiles/ACHColombia/Outgoing/ACH_COL_OUT_001.ach` | Salida NACHA-M semirreal | Anonimizado | Disponible |
| DS-ACH-IN-001 | ACH Colombia | UAT-ACH-002 | `tests/Cfa.ACHInterbank.Tests/TestData/Nacha/GoldenFiles/ACHColombia/Incoming/ACH_COL_IN_001.ach` | Entrada NACHA-M semirreal | Anonimizado | Disponible |
| DS-ACH-RET-001 | ACH Colombia | UAT-ACH-003, UAT-ACH-004 | `tests/Cfa.ACHInterbank.Tests/TestData/Nacha/GoldenFiles/ACHColombia/Returns/ACH_COL_RET_001.RET` | Devolucion/rechazo `.RET` semirreal | Anonimizado | Disponible |
| DS-CEN-OUT-001 | CENIT | UAT-CEN-001 | `tests/Cfa.ACHInterbank.Tests/TestData/Nacha/GoldenFiles/CENIT/Outgoing/CENIT_OUT_001.ach` | Salida NACHA-M semirreal | Anonimizado | Disponible |
| DS-CEN-IN-001 | CENIT | UAT-CEN-002 | `tests/Cfa.ACHInterbank.Tests/TestData/Nacha/GoldenFiles/CENIT/Incoming/CENIT_IN_001.ach` | Entrada NACHA-M semirreal | Anonimizado | Disponible |
| DS-CEN-RET-001 | CENIT | UAT-CEN-003, UAT-CEN-004 | `tests/Cfa.ACHInterbank.Tests/TestData/Nacha/GoldenFiles/CENIT/Returns/CENIT_RET_001.RET` | `.RET` CENIT semirreal | Anonimizado | Disponible |
| DS-PRENOTE-ACH-APP | ACH Colombia | UAT-ACH-005 | `docs/uat/evidencias/soap-integrations/prenotification-responses/approved/validation_report.md` | Prenotificacion aprobada sintetica | Sanitizado | Disponible |
| DS-PRENOTE-ACH-REJ | ACH Colombia | UAT-ACH-006 | `docs/uat/evidencias/soap-integrations/prenotification-responses/rejected/validation_report.md` | Prenotificacion rechazada sintetica | Sanitizado | Disponible |
| DS-PRENOTE-CENIT | CENIT | UAT-CEN-004 | `docs/uat/evidencias/nacha-m-inbound-simulator/prenotification-responses/cenit/validation_report.md` | Prenotificacion/respuesta CENIT sintetica | Sanitizado | Disponible |
| DS-ROR-001 | Ambas | UAT-CEN-006, UAT-TRV-004 | `docs/uat/REQUIREMENT_TRACEABILITY_MATRIX.md` RTM-019 + `tests/Cfa.ACHInterbank.Tests/AchReturnOfReturn*Tests.cs` | ROR sintetico automatizado | No sensible | Disponible automatizado |
| DS-CONC-001 | Ambas | UAT-TRV-004 | `web/ach-interbank-ui/e2e/ach-reconciliation.spec.ts` | Conciliacion ACH mock/sintetica | Sanitizado | Disponible automatizado |
| DS-SOAP-UAT-001 | Ambas | UAT-TRV-003 | `web/ach-interbank-ui/e2e/nacha-soap-uat-console.spec.ts` | SOAP/UAT read-only mock | Sanitizado | Disponible automatizado |
| DS-DASH-001 | Ambas | UAT-TRV-001, UAT-TRV-002 | `web/ach-interbank-ui/e2e/nacha-operational-dashboard.spec.ts` | Dashboard/detalle read-only mock | Sanitizado | Disponible automatizado |
| DS-CONFIG-001 | Ambas | UAT-TRV-005 | `web/ach-interbank-ui/e2e/nacha-config-profiles.spec.ts` | Config profiles read-only mock | No sensible | Disponible automatizado |
| DS-LEGACY-001 | Ambas | UAT-TRV-006 | `web/ach-interbank-ui/e2e/nacha-legacy-routes.spec.ts` | Guard legacy deprecated | No sensible | Disponible automatizado |
| DS-EXPORT-001 | Ambas | UAT-TRV-007 | `web/ach-interbank-ui/e2e/nacha-export-flow.spec.ts` | Export `cycleId`/no hash mock | No sensible | Disponible automatizado |

## Limitacion

Este dataset permite ronda 1 controlada y no oficial. Falta dataset UAT cargado en ambiente formal con validacion de operadores ACH Colombia/CENIT. Productivo permanece NO-GO.
