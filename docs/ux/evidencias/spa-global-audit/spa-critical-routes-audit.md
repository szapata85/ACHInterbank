# Auditoria SPA rutas criticas

Fecha: 2026-05-24T05:02:46.567Z
Base URL: http://localhost:743

## Runtime

- /health/live: 200
- /health/ready: 200
- Login demo: OK
- Productivo: NO-GO

## Resumen

- Rutas auditadas: 23
- OK: 23
- P0: 0
- P1: 0
- P2: 0

## Hallazgos por ruta

| Ruta | Estado | Hallazgos | Screenshot |
|---|---|---|---|
| `/ach-cycles/nacha/export` | OK | Sin hallazgos automaticos | `docs/ux/evidencias/spa-global-audit/screenshots/ach_cycles_nacha_export.png` |
| `/catalogs/financial-institutions` | OK | Sin hallazgos automaticos | `docs/ux/evidencias/spa-global-audit/screenshots/catalogs_financial_institutions.png` |
| `/catalogs/bank-holidays` | OK | Sin hallazgos automaticos | `docs/ux/evidencias/spa-global-audit/screenshots/catalogs_bank_holidays.png` |
| `/catalogs/document-types` | OK | Sin hallazgos automaticos | `docs/ux/evidencias/spa-global-audit/screenshots/catalogs_document_types.png` |
| `/catalogs/person-types` | OK | Sin hallazgos automaticos | `docs/ux/evidencias/spa-global-audit/screenshots/catalogs_person_types.png` |
| `/catalogs/phone-types` | OK | Sin hallazgos automaticos | `docs/ux/evidencias/spa-global-audit/screenshots/catalogs_phone_types.png` |
| `/catalogs/email-types` | OK | Sin hallazgos automaticos | `docs/ux/evidencias/spa-global-audit/screenshots/catalogs_email_types.png` |
| `/catalogs/address-types` | OK | Sin hallazgos automaticos | `docs/ux/evidencias/spa-global-audit/screenshots/catalogs_address_types.png` |
| `/catalogs/transaction-codes` | OK | Sin hallazgos automaticos | `docs/ux/evidencias/spa-global-audit/screenshots/catalogs_transaction_codes.png` |
| `/customer-third-parties` | OK | Sin hallazgos automaticos | `docs/ux/evidencias/spa-global-audit/screenshots/customer_third_parties.png` |
| `/ach-cycles/nacha/layouts` | OK | Sin hallazgos automaticos | `docs/ux/evidencias/spa-global-audit/screenshots/ach_cycles_nacha_layouts.png` |
| `/ach-cycles/nacha/definitions` | OK | Sin hallazgos automaticos | `docs/ux/evidencias/spa-global-audit/screenshots/ach_cycles_nacha_definitions.png` |
| `/reports` | OK | Sin hallazgos automaticos | `docs/ux/evidencias/spa-global-audit/screenshots/reports.png` |
| `/reports/sent` | OK | Sin hallazgos automaticos | `docs/ux/evidencias/spa-global-audit/screenshots/reports_sent.png` |
| `/reports/received` | OK | Sin hallazgos automaticos | `docs/ux/evidencias/spa-global-audit/screenshots/reports_received.png` |
| `/reports/returns` | OK | Sin hallazgos automaticos | `docs/ux/evidencias/spa-global-audit/screenshots/reports_returns.png` |
| `/reports/rejections` | OK | Sin hallazgos automaticos | `docs/ux/evidencias/spa-global-audit/screenshots/reports_rejections.png` |
| `/reports/files` | OK | Sin hallazgos automaticos | `docs/ux/evidencias/spa-global-audit/screenshots/reports_files.png` |
| `/reports/cycles` | OK | Sin hallazgos automaticos | `docs/ux/evidencias/spa-global-audit/screenshots/reports_cycles.png` |
| `/reports/reconciliation` | OK | Sin hallazgos automaticos | `docs/ux/evidencias/spa-global-audit/screenshots/reports_reconciliation.png` |
| `/reports/audit` | OK | Sin hallazgos automaticos | `docs/ux/evidencias/spa-global-audit/screenshots/reports_audit.png` |
| `/reports/history` | OK | Sin hallazgos automaticos | `docs/ux/evidencias/spa-global-audit/screenshots/reports_history.png` |
| `/reports/traceability` | OK | Sin hallazgos automaticos | `docs/ux/evidencias/spa-global-audit/screenshots/reports_traceability.png` |

## Orden recomendado de correccion

No hay correcciones obligatorias detectadas por la auditoria automatizada.

## Nota

Esta auditoria no modifica backend, Angular, estilos ni reglas ACH/NACHA-M/CENIT/ROR. Productivo permanece NO-GO.
