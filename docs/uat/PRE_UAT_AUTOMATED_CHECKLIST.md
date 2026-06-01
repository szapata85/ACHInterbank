# Checklist automatizado pre-UAT - Fase 6C.11

Estados sugeridos: `Pendiente`, `Ejecutado OK`, `Ejecutado con observacion`, `Bloqueado`, `No aplica`.

Productivo permanece NO-GO. No SOAP real, no movimientos monetarios, no mutaciones criticas, no secretos, no legacy oficial, no `/NachaExport/{hash}`.

| ID | Control automatizado | Comando/workflow | Evidencia esperada | Estado | Observacion |
| --- | --- | --- | --- | --- | --- |
| AUT-001 | Backend build Release | `dotnet build ACHInterbank.sln -c Release` / `dotnet-ci.yml` | Build OK, 0 errores | Ejecutado OK | 6C.11: build OK |
| AUT-002 | Backend tests Release | `dotnet test ... -c Release --no-build` | Tests OK | Ejecutado OK | 6C.11: 1602 passed, 1 skipped |
| AUT-003 | Backend tests secuencial | `dotnet test ... -- RunConfiguration.MaxCpuCount=1` / `dotnet-ci.yml` | Tests OK sin crash paralelo | Ejecutado OK | Mitigacion CLR/EF aplicada |
| AUT-004 | Angular build | `npm run build` / `angular-ci.yml` | Build OK | Ejecutado con observacion | Build OK; warning Browserslist conocido |
| AUT-005 | Angular unit tests | `npm test -- --watch=false --browsers=ChromeHeadless` / `angular-ci.yml` | 308 success o superior | Ejecutado con observacion | 308 success; warnings Angular existentes |
| AUT-006 | Playwright E2E | `npm run e2e` / `angular-ci.yml` | 34 passed o superior | Ejecutado con observacion | 34 passed; DEP0205/Browserslist documentados |
| AUT-007 | Publicacion `playwright-report` | `angular-ci.yml` | Artifact con `if: always()` | Ejecutado OK | Confirmado en workflow |
| AUT-008 | Publicacion `playwright-test-results` | `angular-ci.yml` | Artifact con `if: always()` | Ejecutado OK | Confirmado en workflow |
| AUT-009 | Publicacion `uat-evidence-playwright` | `angular-ci.yml` | Artifact con `if: always()` | Ejecutado OK | Confirmado en workflow |
| AUT-010 | Guard no `/NachaExport/{hash}` | `web/ach-interbank-ui/e2e/*export*.spec.ts` | Falla si se usa hash | Ejecutado OK | Cubierto por Playwright 34 passed |
| AUT-011 | Guard no legacy oficial | `nacha-legacy-routes.spec.ts`, dashboard/config specs | No llamadas oficiales legacy | Ejecutado OK | Cubierto por Playwright 34 passed |
| AUT-012 | Guard no POST/PUT/PATCH/DELETE read-only | `nacha-soap-uat-console.spec.ts`, `ach-reconciliation.spec.ts` | Sin mutaciones en consolas | Ejecutado OK | Cubierto por Playwright 34 passed |
| AUT-013 | Guard no SOAP real | Backend SOAP/UAT tests, Playwright SOAP/UAT | SOAP real bloqueado/read-only | Ejecutado OK | Cubierto por backend/Playwright |
| AUT-014 | Guard Productivo NO-GO visible | Playwright dashboard/config/SOAP/conciliacion | Banners NO-GO visibles | Ejecutado OK | Cubierto por Playwright 34 passed |
| AUT-015 | Matriz trazable creada | `docs/uat/REQUIREMENT_TRACEABILITY_MATRIX.md` | 30 filas/requisitos | Ejecutado OK | Fase 6C.9 |
| AUT-016 | Paquete UAT formal creado | `docs/uat/UAT_FORMAL_PREPARATION_ACH_CENIT.md` | Preparacion/checklist/escenarios/riesgos | Ejecutado OK | Fase 6C.10 |
| AUT-017 | Certificacion oficial pendiente | Docs UAT/contexto | Estado pendiente explicito | Ejecutado OK | No afirmar certificacion |
