# Playwright Evidence UAT - Fase 6C.7

## Proposito

Documentar el paquete de evidencia automatizada Playwright para revision tecnica y comite UAT de las pantallas NACHA-M/SOAP read-only.

## Ejecucion local

```bash
cd web/ach-interbank-ui
npm run build
npm test -- --watch=false --browsers=ChromeHeadless
npm run e2e
```

## Pantallas cubiertas

- Dashboard operacional NACHA-M.
- Config profiles oficiales read-only.
- Detalle operativo de archivo NACHA-M.
- Flujo de exportacion con `cycleId`, sin hash.
- Rutas legacy deprecated/read-only.
- Consola SOAP/UAT read-only.
- Consola de conciliacion ACH read-only.

## Artefactos esperados

- `playwright-report`: reporte HTML navegable.
- `test-results`: salidas por prueba, incluyendo screenshots.
- Screenshots funcionales generados por specs de evidencia.
- Traces/videos/screenshots automaticos en fallos segun `playwright.config.ts`.

## Garantias

- Productivo permanece NO-GO.
- No se ejecuta SOAP real.
- No hay movimientos monetarios.
- No hay mutaciones criticas.
- No se usa legacy como fuente oficial.
- No se solicita `/NachaExport/{hash}`.

## Limitacion

Esta evidencia automatizada no reemplaza la certificacion oficial con ACH Colombia/CENIT ni la aprobacion formal de salida productiva.
