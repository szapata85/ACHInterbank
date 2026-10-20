# JOB 6 — Reevaluación GO/NO-GO

Fecha: 2026-07-24

## Decisión

**GO** para el alcance técnico del JOB 6.

Los controles críticos pasan localmente y el workflow `angular-ci` fue confirmado en GitHub Actions: run `30106942194`, `build-and-test=success`, `runtime-backed-e2e=success`, Playwright 2/2 y artefacto `job6-playwright-runtime` ID `8602165599`. Esta decisión corresponde únicamente al alcance técnico del JOB 6. El estado Productivo general de ACHInterbank continúa **NO-GO** por dependencias externas y regulatorias ajenas a este cierre; JOB 5 no fue reabierto.

## Gates

| Gate | Estado | Evidencia | Riesgo residual |
|---|---|---|---|
| 1 — NACHA Config Admin | PASS | Flujo real de lectura/alta/detalle/filtro; catálogos obligatorios con retry 429 acotado; validación backend/UI; un solo POST; modal; permisos; 474 SPA y E2E 2/2. | Compatibilidad de permisos legacy conservada intencionalmente. |
| 2 — Generalización por cámara | PASS | `REDTEST` unitario y `JOB6TEST` runtime; selector/filtro/detalle propios; sin fallback ACHCOL/CENIT ni seed productivo. | Especificidad legítima permanece en adaptadores de cada red. |
| 3 — Responsive | PASS | Ocho rutas críticas en seis viewports; sin overflow global; menú móvil y modal operables; tablas contenidas. | Nuevas rutas futuras deben reutilizar el mismo smoke. |
| 4 — Playwright runtime CI | PASS confirmado | Run `30106942194` sobre head SHA `df46bfce863457c36ceab1b18ee26eceb4d7469d`: ambos jobs verdes; Playwright 2/2 en 11.1 s, 0 skips/retries; artefacto ID `8602165599`; cleanup completo. | Bajo: el rate limiter permanece activo y un agotamiento real deja el formulario bloqueado con reintento manual. |
| 5 — npm | PASS | `npm ci` limpio; 0 critical/high; producción 0; unitarias/build/E2E pasan. | Tres moderadas en tooling dev Angular CLI/MCP, no explotables en el bundle desplegado. |
| 6 — Regresión general | PASS | .NET build 0 warnings/errores; backend 1949 pass más 2/2 multi-DB con configuración obligatoria, 5 skips históricos; Angular 474/474; E2E local/remoto 2/2; API/SPA/DB healthy. | Cinco skips históricos no creados por JOB 6. |

## Cierre confirmado

- Workflow: `angular-ci`.
- Run: `30106942194` — `https://github.com/szapata85/ACHInterbank/actions/runs/30106942194`.
- Head SHA: `df46bfce863457c36ceab1b18ee26eceb4d7469d`; checkout PR merge SHA: `81a8ad14c697f3a27ee63d310cc2dd49c026b11b`.
- Jobs: `build-and-test` ID `89526556110`, `runtime-backed-e2e` ID `89527277125`, ambos `success`.
- Artefacto: `job6-playwright-runtime`, ID `8602165599`, 608946 bytes, digest `sha256:e8d4091cf2c8ba5e389a39b3bf04f498126ce50d233be5ee6465243b8d2be689`.
- Cleanup: contenedores SPA/API/PostgreSQL, volumen y red eliminados por el paso obligatorio.

## Límites de la reevaluación

- No se ejecutó ni requirió SOAP.
- No se modificaron reglas NACHA-M normativas, lógica monetaria, golden files ni migraciones.
- No se añadieron datos productivos ni cámara sintética a seeds.
- No se hizo merge del Pull Request ni force push.
- El GO no cambia el NO-GO productivo general ni resuelve dependencias externas/regulatorias.
