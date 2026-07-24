# JOB 6 — Reevaluación GO/NO-GO

Fecha: 2026-07-24

## Decisión

**GO CON CONDICIONES** para el alcance técnico del JOB 6.

Los controles críticos pasan localmente con runtime real, ambos proveedores de persistencia, SPA completa y auditoría de producción limpia. La única condición es externa y verificable: ejecutar el workflow modificado en GitHub Actions y obtener ambos jobs verdes con sus artefactos. Esta decisión no cambia el estado Productivo NO-GO general del sistema ni reabre JOB 5.

## Gates

| Gate | Estado | Evidencia | Riesgo residual |
|---|---|---|---|
| 1 — NACHA Config Admin | PASS | Flujo real de lectura/alta/detalle/filtro; validación backend/UI; un solo POST; modal; permisos; 29 focalizadas, 466 SPA y E2E 2/2. | Compatibilidad de permisos legacy conservada intencionalmente. |
| 2 — Generalización por cámara | PASS | `REDTEST` unitario y `JOB6TEST` runtime; selector/filtro/detalle propios; sin fallback ACHCOL/CENIT ni seed productivo. | Especificidad legítima permanece en adaptadores de cada red. |
| 3 — Responsive | PASS | Ocho rutas críticas en seis viewports; sin overflow global; menú móvil y modal operables; tablas contenidas. | Nuevas rutas futuras deben reutilizar el mismo smoke. |
| 4 — Playwright runtime CI | PASS local | Workflow obligatorio, runtime Docker reproducible, readiness finito, fixture idempotente, 2/2 local, diagnóstico y cleanup. | GitHub Actions real no confirmado. |
| 5 — npm | PASS | `npm ci` limpio; 0 critical/high; producción 0; unitarias/build/E2E pasan. | Tres moderadas en tooling dev Angular CLI/MCP, no explotables en el bundle desplegado. |
| 6 — Regresión general | PASS | .NET build 0/0; backend 1951 pass/5 skips; Angular 466/466; E2E 2/2; API/SPA/DB healthy; deep link correcto; ambos DB probados. | Cinco skips históricos y ejecución GitHub pendiente. |

## Condición de cierre

| Acción | Responsable/dependencia | Criterio objetivo | ¿Bloquea el GO técnico local? |
|---|---|---|---|
| Ejecutar `angular-ci` en GitHub Actions sobre la rama/PR. | Plataforma GitHub/runner externo. | `build-and-test` y `runtime-backed-e2e` verdes; artefacto `job6-playwright-runtime` publicado. | No; impide elevar el veredicto a GO incondicional. |

## Límites de la reevaluación

- No se ejecutó ni requirió SOAP.
- No se modificaron reglas NACHA-M normativas, lógica monetaria, golden files ni migraciones.
- No se añadieron datos productivos ni cámara sintética a seeds.
- No se declara confirmación de GitHub Actions sin ejecución en el runner.
