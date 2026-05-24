# Regresion final SPA Angular UAT

Fecha: 2026-05-24T05:01:08.639Z
Base URL: http://localhost:743

## Runtime

- /health/live: 200
- /health/ready: 200
- Login demo: OK
- Roles sanitizados: Admin, ACH.Operator
- Productivo: NO-GO

## Resumen

- Rutas auditadas: 31
- Rutas OK: 31
- Rutas no aplica: 0
- P0: 0
- P1: 0
- P2: 0

## Resultado por fase

- Fase 1 botones reportes: OK
- Fase 2 PDFs reportes: OK
- Fase 3 catalogos/AG Grid: OK
- Fase 4 NACHA layouts/definitions: OK
- Integraciones/mappings: OK

## Rutas auditadas

| Ruta | Estado | Hallazgos | Screenshot |
|---|---:|---|---|
| `/ach-cycles/nacha/export` | OK | - | `docs/ux/evidencias/spa-regression-final/screenshots/ach_cycles_nacha_export.png` |
| `/catalogs/financial-institutions` | OK | - | `docs/ux/evidencias/spa-regression-final/screenshots/catalogs_financial_institutions.png` |
| `/catalogs/bank-holidays` | OK | - | `docs/ux/evidencias/spa-regression-final/screenshots/catalogs_bank_holidays.png` |
| `/catalogs/document-types` | OK | - | `docs/ux/evidencias/spa-regression-final/screenshots/catalogs_document_types.png` |
| `/catalogs/person-types` | OK | - | `docs/ux/evidencias/spa-regression-final/screenshots/catalogs_person_types.png` |
| `/catalogs/phone-types` | OK | - | `docs/ux/evidencias/spa-regression-final/screenshots/catalogs_phone_types.png` |
| `/catalogs/email-types` | OK | - | `docs/ux/evidencias/spa-regression-final/screenshots/catalogs_email_types.png` |
| `/catalogs/address-types` | OK | - | `docs/ux/evidencias/spa-regression-final/screenshots/catalogs_address_types.png` |
| `/catalogs/transaction-codes` | OK | - | `docs/ux/evidencias/spa-regression-final/screenshots/catalogs_transaction_codes.png` |
| `/customer-third-parties` | OK | - | `docs/ux/evidencias/spa-regression-final/screenshots/customer_third_parties.png` |
| `/ach-cycles/nacha/layouts` | OK | - | `docs/ux/evidencias/spa-regression-final/screenshots/ach_cycles_nacha_layouts.png` |
| `/ach-cycles/nacha/definitions` | OK | - | `docs/ux/evidencias/spa-regression-final/screenshots/ach_cycles_nacha_definitions.png` |
| `/reports` | OK | - | `docs/ux/evidencias/spa-regression-final/screenshots/reports.png` |
| `/reports/sent` | OK | - | `docs/ux/evidencias/spa-regression-final/screenshots/reports_sent.png` |
| `/reports/received` | OK | - | `docs/ux/evidencias/spa-regression-final/screenshots/reports_received.png` |
| `/reports/returns` | OK | - | `docs/ux/evidencias/spa-regression-final/screenshots/reports_returns.png` |
| `/reports/rejections` | OK | - | `docs/ux/evidencias/spa-regression-final/screenshots/reports_rejections.png` |
| `/reports/files` | OK | - | `docs/ux/evidencias/spa-regression-final/screenshots/reports_files.png` |
| `/reports/cycles` | OK | - | `docs/ux/evidencias/spa-regression-final/screenshots/reports_cycles.png` |
| `/reports/reconciliation` | OK | - | `docs/ux/evidencias/spa-regression-final/screenshots/reports_reconciliation.png` |
| `/reports/audit` | OK | - | `docs/ux/evidencias/spa-regression-final/screenshots/reports_audit.png` |
| `/reports/history` | OK | - | `docs/ux/evidencias/spa-regression-final/screenshots/reports_history.png` |
| `/reports/traceability` | OK | - | `docs/ux/evidencias/spa-regression-final/screenshots/reports_traceability.png` |
| `/integraciones/soap-settings` | OK | - | `docs/ux/evidencias/spa-regression-final/screenshots/integraciones_soap_settings.png` |
| `/integraciones/mappings` | OK | - | `docs/ux/evidencias/spa-regression-final/screenshots/integraciones_mappings.png` |
| `/uat/nacha-inbound-simulator` | OK | - | `docs/ux/evidencias/spa-regression-final/screenshots/uat_nacha_inbound_simulator.png` |
| `/transactions/returns` | OK | - | `docs/ux/evidencias/spa-regression-final/screenshots/transactions_returns.png` |
| `/transactions/clearing-house-rules` | OK | - | `docs/ux/evidencias/spa-regression-final/screenshots/transactions_clearing_house_rules.png` |
| `/integraciones/mappings/WSCFAACH.Proc_Contrapartidas/1d6f8bce-d768-4ebd-80d0-c5331971e130` | OK | - | `docs/ux/evidencias/spa-regression-final/screenshots/integraciones_mappings_WSCFAACH_Proc_Contrapartidas_1d6f8bce_d768_4ebd_80d0_c5331971e130.png` |
| `/integraciones/mappings/WSCFAACH.Proc_Transacciones/dc1b034b-4de3-4043-93cc-79072bf8a5e9` | OK | - | `docs/ux/evidencias/spa-regression-final/screenshots/integraciones_mappings_WSCFAACH_Proc_Transacciones_dc1b034b_4de3_4043_93cc_79072bf8a5e9.png` |
| `/integraciones/mappings/WSAXON.RegistrarRespuestaTransaccion/66830bda-7d12-43b2-a2d5-efbfc69e2144` | OK | - | `docs/ux/evidencias/spa-regression-final/screenshots/integraciones_mappings_WSAXON_RegistrarRespuestaTransaccion_66830bda_7d12_43b2_a2d5_efbfc69e2144.png` |

## Conclusion

SPA Angular queda OK tecnico UAT para las rutas auditadas.

Continuar UAT controlado. Productivo NO-GO.
