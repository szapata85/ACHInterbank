# Auditoria global SPA UAT

Fecha: 2026-05-23

## Alcance

Auditoria visual automatizada con Playwright para rutas criticas de la SPA ACH Interbank, incluyendo catalogos, NACHA, terceros y reportes.

## Fase 0

- Rutas auditadas: 23.
- P0: 0.
- P1: 8.
- P2: 0.
- Hallazgo comun: botones `Buscar`, `Limpiar` y `Exportar PDF` con bajo contraste en rutas `/reports/*`.

## Correccion P1 reportes

Se corrigio el componente comun `ReportListPageComponent`:

- `Buscar`: variante primaria.
- `Limpiar`: variante secundaria.
- `Exportar PDF`: variante exportar/documento.
- Sin cambios de funcionalidad, rutas, endpoints, PDFs, backend ni reglas ACH/NACHA-M/CENIT/ROR.

## Resultado posterior

- Rutas auditadas: 23.
- OK: 23.
- P0: 0.
- P1: 0.
- P2: 0.

## Fase 3 - Catalogos, terceros y AG Grid

Se valido y corrigio el alcance acotado de catalogos y terceros:

- `/catalogs/financial-institutions`: AG Grid legible, columnas con anchos minimos, acciones visibles y estado de error controlado.
- `/catalogs/bank-holidays`: carga runtime corregida mediante proxy SPA; grilla y estados de carga/error/vacio controlados.
- Catalogos tipologicos: `document-types`, `person-types`, `phone-types`, `email-types`, `address-types`, `transaction-codes` cargan por proxy SPA y muestran grilla/estado vacio sin spinner infinito.
- `/customer-third-parties`: carga inicial ejecutada y estado de error/vacio visible.

No se tocaron reportes, PDFs, SOAP, mappings, backend, seeds ni reglas ACH/NACHA-M/CENIT/ROR.

Evidencias:

- `docs/ux/evidencias/catalogs-aggrid/catalogs-aggrid-validation.json`
- `docs/ux/evidencias/catalogs-aggrid/catalogs-aggrid-validation.md`
- `docs/ux/evidencias/catalogs-aggrid/screenshots/`

Validaciones:

- `npm run build`: OK.
- `npm test -- --watch=false --browsers=ChromeHeadless`: OK.
- `node web/ach-interbank-ui/scripts/ux-validate-catalogs-aggrid.mjs`: OK.
- `node web/ach-interbank-ui/scripts/ux-audit-spa-critical-routes.mjs`: OK, P0=0/P1=0/P2=0.

## Fase 4 - NACHA-M layouts y definitions

Se valido y corrigio el alcance acotado de:

- `/ach-cycles/nacha/layouts`
- `/ach-cycles/nacha/definitions`

Diagnostico:

- Ambas rutas cargaban con endpoints `200` y sin errores criticos de consola.
- `/ach-cycles/nacha/layouts` tenia una experiencia funcional pero poco operativa: header basico, sin resumen y acciones pequenas.
- `/ach-cycles/nacha/definitions` usaba editor inline bajo la grilla; la edicion perdia contexto y saturaba la vista.
- No se detecto bug backend ni necesidad de cambiar contratos API.

Cambios aplicados:

- Header/resumen operativo para layouts y definitions.
- Estados de error/vacio explicitos.
- Botones de accion con el patron visual vigente.
- Definitions usa modal/drawer lateral para crear/editar, con contexto del registro, cancelar/cerrar, loading de guardado y refresco de lista tras guardar.

No se tocaron reportes, PDFs, catalogos, mappings, SOAP, export NACHA-M, backend ni reglas ACH/NACHA-M/CENIT/ROR.

Evidencias:

- `docs/ux/evidencias/nacha-layouts-definitions/nacha-layouts-definitions-validation.json`
- `docs/ux/evidencias/nacha-layouts-definitions/nacha-layouts-definitions-validation.md`
- `docs/ux/evidencias/nacha-layouts-definitions/screenshots/nacha-layouts.png`
- `docs/ux/evidencias/nacha-layouts-definitions/screenshots/nacha-definitions.png`
- `docs/ux/evidencias/nacha-layouts-definitions/screenshots/nacha-definitions-edit-modal.png`

Validaciones:

- `npm run build`: OK.
- `npm test -- --watch=false --browsers=ChromeHeadless`: OK.
- `node web/ach-interbank-ui/scripts/ux-validate-nacha-layouts-definitions.mjs`: OK.
- `node web/ach-interbank-ui/scripts/ux-audit-spa-critical-routes.mjs`: OK, P0=0/P1=0/P2=0.

## Validacion funcional PDF reportes

Se valido funcionalmente la exportacion PDF en las rutas priorizadas:

- `/reports/reconciliation`: el backend/runtime devolvio PDF vacio para el escenario sin datos; la SPA bloquea la descarga y muestra `No hay informacion para exportar`.
- `/reports/traceability`: el backend/runtime devolvio PDF vacio para el escenario sin datos; la SPA bloquea la descarga, muestra mensaje claro y deduplica la seleccion multiple de ciclos antes de invocar el endpoint.

No se modificaron endpoints, backend, SOAP ni reglas ACH/NACHA-M/CENIT/ROR. No se agrego exportacion Excel nueva; las rutas `/reports/*` validadas no exponen accion Excel en esta pantalla.

Evidencias:

- `docs/ux/evidencias/spa-global-audit/spa-critical-routes-audit.json`
- `docs/ux/evidencias/spa-global-audit/spa-critical-routes-audit.md`
- `docs/ux/evidencias/spa-global-audit/screenshots/`
- `docs/ux/evidencias/reports-pdf/reports-pdf-validation.json`
- `docs/ux/evidencias/reports-pdf/reconciliation-pdf-result.json`
- `docs/ux/evidencias/reports-pdf/traceability-pdf-result.json`
- `docs/ux/evidencias/reports-pdf/screenshots/`

Validaciones:

- `npm run build`: OK.
- `npm test -- --watch=false --browsers=ChromeHeadless`: OK.
- `node web/ach-interbank-ui/scripts/ux-validate-reports-pdf.mjs`: OK.
- `node web/ach-interbank-ui/scripts/ux-audit-spa-critical-routes.mjs`: OK.

Productivo: NO-GO.

## Fase 5 - Regresion final SPA Angular

Se ejecuto regresion final completa sobre las rutas criticas de fases previas y rutas adicionales de integraciones/mappings:

- Rutas auditadas en regresion final: 30.
- Rutas OK: 30.
- P0: 0.
- P1: 0.
- P2: 0.
- `npm run build`: OK.
- `npm test -- --watch=false --browsers=ChromeHeadless`: OK, 214 SUCCESS.
- `node web/ach-interbank-ui/scripts/ux-audit-spa-critical-routes.mjs`: OK, 23 rutas, P0=0/P1=0/P2=0.
- `node web/ach-interbank-ui/scripts/ux-audit-spa-final-regression.mjs`: OK, 30 rutas, P0=0/P1=0/P2=0.
- `node web/ach-interbank-ui/scripts/ux-validate-reports-pdf.mjs`: OK.

Evidencias:

- `docs/ux/REGRESION_FINAL_SPA_UAT.md`.
- `docs/ux/evidencias/spa-regression-final/precheck_runtime.md`.
- `docs/ux/evidencias/spa-regression-final/spa-final-regression.json`.
- `docs/ux/evidencias/spa-regression-final/spa-final-regression.md`.
- `docs/ux/evidencias/spa-regression-final/screenshots/`.

Resultado: SPA Angular OK tecnico UAT. Continuar UAT controlado. Productivo **NO-GO**.
