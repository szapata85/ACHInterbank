# Hardening tecnico pre-UAT - Fase 6C.11

## Estado tecnico actual

Preparacion tecnica pre-UAT para estabilizar CI, evidencia automatizada y guardas criticos antes de ejecutar UAT formal ACH Colombia/CENIT. Productivo permanece NO-GO.

## Baseline conocido

| Area | Baseline funcional | Comando / fuente |
| --- | --- | --- |
| Backend | 1602 passed, 1 skipped | `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build` |
| Angular | 308 success | `npm test -- --watch=false --browsers=ChromeHeadless` |
| Playwright | 34 passed | `npm run e2e` |
| Evidencia CI | Publicada | `.github/workflows/angular-ci.yml` |

## Workflows revisados

- `.github/workflows/dotnet-ci.yml`: restore, build Release y tests Release. Endurecido con `RunConfiguration.MaxCpuCount=1` por antecedente de crash paralelo CLR/EF.
- `.github/workflows/angular-ci.yml`: `npm ci`, build, unit tests, install Chromium, Playwright E2E y artefactos `playwright-report`, `playwright-test-results`, `uat-evidence-playwright` con `if: always()`.
- `.github/workflows/postgres-integration-tests.yml`: manual `workflow_dispatch` para pruebas PostgreSQL controladas.

## Evidencias publicadas

- `playwright-report`: reporte HTML.
- `playwright-test-results`: resultados, screenshots, traces/videos en fallos.
- `uat-evidence-playwright`: paquete combinado de reporte y resultados.

## Warnings conocidos

| Warning | Estado | Mitigacion |
| --- | --- | --- |
| Browserslist/Angular unsupported browsers | Documentado | No bloqueante pre-UAT; revisar `.browserslistrc` en fase separada para evitar cambio amplio de compatibilidad. |
| Node DEP0205 `module.register()` | Documentado | No bloqueante; depende de toolchain/transitivos. No actualizar dependencias mayores en hardening pre-UAT. |
| Crash paralelo CLR/EF previo | Mitigado | CI backend usa `RunConfiguration.MaxCpuCount=1`; mantener fallback local secuencial si reaparece. |
| `@playwright/test` duplicado en `package.json` | Corregido | Se conserva una sola version declarada sin tocar `package-lock.json` masivamente. |

## Reglas NO-GO automatizadas

- No SOAP real: cubierto por tests SOAP/UAT y consolas read-only.
- No movimientos monetarios: cubierto por pruebas SOAP payload/readiness/conciliacion y banners UI.
- No mutaciones criticas: Playwright valida ausencia de POST/PUT/PATCH/DELETE en consolas read-only.
- No legacy oficial: Playwright valida no uso oficial de `nacha-layouts` / `nacha-record-definitions`.
- No `/NachaExport/{hash}`: Playwright valida export por `cycleId`.
- Productivo NO-GO visible: validado en dashboard, config profiles, SOAP/UAT y conciliacion.

## Riesgos tecnicos restantes

- Warnings de toolchain deben resolverse en una fase separada para no introducir upgrades amplios antes de UAT.
- Datos persistidos pueden ser parciales; las pantallas deben mantener warnings y fuente read-only/parcial.
- La evidencia automatizada no reemplaza certificacion oficial ACH Colombia/CENIT.
- SOAP real sigue bloqueado y requiere fase UAT controlada separada.

## Recomendacion pre-UAT

Avanzar a ejecucion UAT formal solo si el checklist automatizado queda verde o con observaciones aceptadas por QA/UAT. Mantener Productivo NO-GO hasta certificacion oficial, UAT aprobado, integracion SOAP real controlada y aprobacion explicita de comite.
