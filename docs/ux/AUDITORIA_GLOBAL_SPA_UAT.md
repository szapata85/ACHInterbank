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

Evidencias:

- `docs/ux/evidencias/spa-global-audit/spa-critical-routes-audit.json`
- `docs/ux/evidencias/spa-global-audit/spa-critical-routes-audit.md`
- `docs/ux/evidencias/spa-global-audit/screenshots/`

Validaciones:

- `npm run build`: OK.
- `npm test -- --watch=false --browsers=ChromeHeadless`: OK.
- `node web/ach-interbank-ui/scripts/ux-audit-spa-critical-routes.mjs`: OK.

Productivo: NO-GO.
