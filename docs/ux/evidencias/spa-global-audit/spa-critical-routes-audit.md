# Auditoria SPA rutas criticas

Fecha: 2026-05-24T01:40:05.835Z
Base URL: http://localhost:743

## Runtime

- /health/live: 200
- /health/ready: 200
- Login demo: OK
- Productivo: NO-GO

## Resumen

- Rutas auditadas: 23
- OK: 15
- P0: 0
- P1: 8
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
| `/reports/sent` | P1 | P1 low-contrast-buttons: 3 botones con contraste bajo o potencialmente ilegibles. | `docs/ux/evidencias/spa-global-audit/screenshots/reports_sent.png` |
| `/reports/received` | P1 | P1 low-contrast-buttons: 3 botones con contraste bajo o potencialmente ilegibles. | `docs/ux/evidencias/spa-global-audit/screenshots/reports_received.png` |
| `/reports/returns` | P1 | P1 low-contrast-buttons: 3 botones con contraste bajo o potencialmente ilegibles. | `docs/ux/evidencias/spa-global-audit/screenshots/reports_returns.png` |
| `/reports/rejections` | P1 | P1 low-contrast-buttons: 3 botones con contraste bajo o potencialmente ilegibles. | `docs/ux/evidencias/spa-global-audit/screenshots/reports_rejections.png` |
| `/reports/files` | P1 | P1 low-contrast-buttons: 3 botones con contraste bajo o potencialmente ilegibles. | `docs/ux/evidencias/spa-global-audit/screenshots/reports_files.png` |
| `/reports/cycles` | P1 | P1 low-contrast-buttons: 3 botones con contraste bajo o potencialmente ilegibles. | `docs/ux/evidencias/spa-global-audit/screenshots/reports_cycles.png` |
| `/reports/reconciliation` | OK | Sin hallazgos automaticos | `docs/ux/evidencias/spa-global-audit/screenshots/reports_reconciliation.png` |
| `/reports/audit` | P1 | P1 low-contrast-buttons: 3 botones con contraste bajo o potencialmente ilegibles. | `docs/ux/evidencias/spa-global-audit/screenshots/reports_audit.png` |
| `/reports/history` | P1 | P1 low-contrast-buttons: 3 botones con contraste bajo o potencialmente ilegibles. | `docs/ux/evidencias/spa-global-audit/screenshots/reports_history.png` |
| `/reports/traceability` | OK | Sin hallazgos automaticos | `docs/ux/evidencias/spa-global-audit/screenshots/reports_traceability.png` |

## Orden recomendado de correccion

### P1
- `/reports/sent`: low-contrast-buttons - 3 botones con contraste bajo o potencialmente ilegibles.
- `/reports/received`: low-contrast-buttons - 3 botones con contraste bajo o potencialmente ilegibles.
- `/reports/returns`: low-contrast-buttons - 3 botones con contraste bajo o potencialmente ilegibles.
- `/reports/rejections`: low-contrast-buttons - 3 botones con contraste bajo o potencialmente ilegibles.
- `/reports/files`: low-contrast-buttons - 3 botones con contraste bajo o potencialmente ilegibles.
- `/reports/cycles`: low-contrast-buttons - 3 botones con contraste bajo o potencialmente ilegibles.
- `/reports/audit`: low-contrast-buttons - 3 botones con contraste bajo o potencialmente ilegibles.
- `/reports/history`: low-contrast-buttons - 3 botones con contraste bajo o potencialmente ilegibles.


## Nota

Esta auditoria no modifica backend, Angular, estilos ni reglas ACH/NACHA-M/CENIT/ROR. Productivo permanece NO-GO.
