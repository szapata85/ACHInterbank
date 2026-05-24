# Regresion final SPA Angular UAT

Fecha: 2026-05-24  
Ambiente: Docker/UAT/local  
Base URL: `http://localhost:743`  
Commit auditado: `d85151e6`

## Alcance

Se ejecuto regresion final sobre las 23 rutas criticas auditadas en fases previas y rutas adicionales de integraciones, mappings, simulador NACHA-M, devoluciones transaccionales y reglas por camara.

## Resultado ejecutivo

- SPA Angular: OK tecnico UAT.
- Rutas auditadas por regresion final: 31.
- Rutas OK: 31.
- P0: 0.
- P1: 0.
- P2: 0.
- Pantallas en blanco: 0.
- Spinners infinitos: 0.
- Botones blancos criticos: 0.
- Scroll horizontal critico: 0.
- Failed requests sin manejo visual: 0.

## Validaciones ejecutadas

| Validacion | Resultado | Evidencia |
|---|---|---|
| Pre-check runtime | OK | `docs/ux/evidencias/spa-regression-final/precheck_runtime.md` |
| Auditoria global historica 23 rutas | OK, P0=0/P1=0/P2=0 | `docs/ux/evidencias/spa-global-audit/spa-critical-routes-audit.md` |
| Regresion final 31 rutas | OK, P0=0/P1=0/P2=0 | `docs/ux/evidencias/spa-regression-final/spa-final-regression.md` |
| Reportes PDF reconciliation/traceability | OK | `docs/ux/evidencias/reports-pdf/reports-pdf-validation.json` |
| Angular build | OK | `npm run build` |
| Angular tests | OK, 224 SUCCESS | `npm test -- --watch=false --browsers=ChromeHeadless` |

## Estado por fase

- Fase 1 reportes botones: sin regresion; botones criticos con contraste.
- Fase 2 reportes PDF: no descarga PDF vacio; si no hay datos muestra mensaje claro.
- Fase 3 catalogos, terceros y AG Grid: cargan o muestran estado vacio/error claro; grillas legibles.
- Fase 4 NACHA layouts/definitions: pantallas legibles; modal/drawer de edicion validado.
- Integraciones/mappings: rutas principales y editores runtime auditados; sin loading infinito.
- `/transactions/returns`: ruta incluida en la regresion final; carga con selector de ciclos, catalogo de causales y estado vacio claro cuando API retorna `[]`.

## Cierre especifico /transactions/returns

Diagnostico:

- La ruta Angular existe y usa `AchReturnsManagementComponent`.
- El API de ciclos devolvia `processingDate`, pero el componente esperaba `date`, generando `RangeError: Invalid time value` y dejando el selector sin opciones.
- La SPA Docker no proxyeaba `/return-reasons` ni `/ach-returns/`; en runtime esos endpoints podian devolver `index.html` en lugar de JSON.

Correccion:

- El componente acepta `date` o `processingDate`, valida fechas invalidas y finaliza siempre `loading`.
- Errores API se muestran en la grilla con accion de reintento.
- Si no hay devoluciones se muestra `No hay devoluciones registradas`.
- Se agregaron proxies SPA para `/return-reasons` y `/ach-returns/`.

Evidencia:

- `docs/ux/evidencias/transactions-returns/transactions-returns-validation.json`.
- `docs/ux/evidencias/transactions-returns/transactions-returns-validation.md`.
- `docs/ux/evidencias/transactions-returns/transactions-returns.png`.

## Evidencias

- JSON final: `docs/ux/evidencias/spa-regression-final/spa-final-regression.json`.
- Markdown final: `docs/ux/evidencias/spa-regression-final/spa-final-regression.md`.
- Screenshots: `docs/ux/evidencias/spa-regression-final/screenshots/`.

## Decision

El frente SPA Angular queda OK tecnico UAT para las rutas auditadas.

Continuar UAT controlado. Productivo permanece **NO-GO**.
