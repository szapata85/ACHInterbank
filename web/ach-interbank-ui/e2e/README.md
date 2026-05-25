# E2E Playwright - NACHA-M Operational Dashboard

Estas pruebas generan evidencia funcional y visual del dashboard read-only NACHA-M/readiness SOAP.

## Comandos

```bash
npm run e2e
npm run e2e:headed
npm run e2e:report
```

## Evidencia generada

- Reporte HTML en `playwright-report`.
- Artefactos por prueba en `test-results`.
- Screenshot funcional `dashboard-full-page.png` desde el test principal.
- Trace/video/screenshot automáticos en fallos, según `playwright.config.ts`.

## Alcance

- Valida `/ach/nacha/operational-dashboard`.
- Valida banner Productivo NO-GO y SOAP real deshabilitado.
- Valida resumen, archivos NACHA-M, decisiones, readiness SOAP/UAT y auditoría Phase 6B.5.
- Valida ausencia de acciones peligrosas como ejecución SOAP real, movimiento monetario, edición de perfiles o carga productiva.

## Restricciones

- Productivo permanece NO-GO.
- No se ejecuta SOAP real.
- No se prueban acciones monetarias.
- No se usan credenciales reales ni endpoints productivos.
- Los screenshots son evidencia UAT automatizada, no certificación oficial ACH Colombia/CENIT.
- Comparación visual pixel-perfect queda para una fase posterior si CI queda estabilizado.
