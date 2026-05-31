# E2E Playwright - NACHA-M UAT Evidence

Estas pruebas generan evidencia funcional y visual UAT de pantallas NACHA-M/SOAP read-only.

## Comandos

```bash
npm run build
npm test -- --watch=false --browsers=ChromeHeadless
npm run e2e
npm run e2e:headed
npm run e2e:report
```

En CI, `angular-ci.yml` publica `playwright-report`, `playwright-test-results` y `uat-evidence-playwright` con `if: always()`.

## Evidencia generada

- Reporte HTML en `playwright-report`.
- Artefactos por prueba en `test-results`.
- Screenshots funcionales generados por specs de evidencia.
- Trace/video/screenshot automaticos en fallos, segun `playwright.config.ts`.

## Alcance

- Valida `/ach/nacha/operational-dashboard`.
- Valida `/ach/nacha/config-profiles` como modelo oficial read-only.
- Valida detalle operativo de archivo NACHA-M.
- Valida flujo de exportacion con `cycleId` y sin `/NachaExport/{hash}`.
- Valida rutas legacy como deprecated/read-only.
- Valida `/ach/nacha/soap-uat-console`.
- Valida ausencia de acciones peligrosas como ejecucion SOAP real, movimiento monetario, edicion de perfiles o carga productiva.

## Restricciones

- Productivo permanece NO-GO.
- No se ejecuta SOAP real.
- No se prueban acciones monetarias.
- No se usan credenciales reales ni endpoints productivos.
- Los screenshots son evidencia UAT automatizada, no certificacion oficial ACH Colombia/CENIT.
