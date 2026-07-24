# JOB 6 — Registro de ejecución

Fecha: 2026-07-24

## Baseline Git

- Rama: `ACH-Interbank-Postgresql`.
- Commit base canónico: `ef43de2ae680ea8a8ae2b164113b84be9f05a908`.
- Baseline de la corrección final del Gate 4: `2b499e8c3a1ccb0ef8e8ba99eef422f74d1ee8fe`; commit correctivo publicado: `df46bfce863457c36ceab1b18ee26eceb4d7469d`.
- El commit base es ancestro de HEAD (`git merge-base --is-ancestor`, exit 0).
- Working tree inicial limpio; no había cambios locales preexistentes.
- No se cambió de rama. Se realizó un push normal del commit correctivo a `ACH-Interbank-Postgresql`; no hubo force push ni merge.
- JOB 5 permaneció cerrado: no se tocaron reglas normativas, golden files, lógica monetaria ni migraciones históricas.

## Hallazgos iniciales que afectaban el JOB 6

- Angular 21.2.16, TypeScript 5.9.3, RxJS 7.8.2 y Playwright 1.60.0.
- Herramientas locales: Node v26.1.0 y npm 12.0.1.
- NACHA Config Admin ya tenía UI y endpoints, pero el detalle omitía contexto de cámara/flujo/dirección/servicio; coexistían permisos finos y legacy sin una aplicación uniforme, defaults silenciosos y mensajes de solo lectura.
- Certificados infería ACHCOL/CENIT por texto y usaba ACHCOL para cámaras desconocidas. El simulador inbound también contenía fallback ACHCOL.
- La ruta SPA `/clearing-houses` colisionaba con el proxy API y un deep link autenticado devolvía 401 en lugar de `index.html`.
- El layout compartido no contenía sistemáticamente tablas, modales y navegación móvil.
- El workflow E2E era opcional y solo ejecutaba el escenario PostgreSQL de ciclos.
- Baseline npm total: 1 critical, 19 high, 7 moderate, 6 low (33). Producción: 0 critical, 7 high, 1 moderate, 0 low (8).

## Cambios por fase

| Fase | Estado | Resultado |
|---|---|---|
| NACHA Config Admin | PASS | DTO de detalle completo; validación de alta/edición/clonado; errores útiles; prevención de duplicados; permisos finos y legacy; carga/vacío/error/éxito; confirmación y bloqueo durante envío. |
| Generalización | PASS | Eliminados fallbacks ACHCOL de certificados, simulador y alta de perfil. Cámara identificada por `clearingHouseId`, código y nombre de catálogo. Fixture `REDTEST` y escenario runtime `JOB6TEST`, ambos solo de prueba. |
| Responsive | PASS | Reglas compartidas para overflow, grids, tablas, formularios, modal y menú móvil; matriz automatizada de seis viewports sobre ocho rutas críticas. |
| Playwright runtime/CI | PASS confirmado | Dos pruebas contra SPA/API/PostgreSQL reales; login, menú, deep link, validación, guardado único, tercera cámara, modal y matriz responsive. Run GitHub Actions `30106942194`: ambos jobs verdes, artefacto y cleanup confirmados. |
| npm | PASS | Angular 21.2.18, CLI 21.2.19, Playwright 1.61.1 y actualizaciones conservadoras. Final: 0 critical/high, 3 moderate dev, 0 producción. |
| Runtime | PASS | API/SPA/PostgreSQL/SQL Server healthy. Se aplicó una migración canónica ya existente que faltaba en el volumen local; luego no reapareció el error `LeaseExpiresAtUtc`. |

### Clasificación de hardcodes

- Especificidad legítima: adaptadores, parsers, nombres de integración y pruebas propias de ACH Colombia/CENIT se conservaron.
- Reglas configurables: selección de cámara y perfil se toma de catálogos/contratos.
- Bloqueantes corregidos: defaults a `ACH`/`ACHCOL`, inferencia de certificado por nombre y proxy de deep link.
- Datos de prueba: `REDTEST` y `JOB6TEST`; no se agregaron a seeds.
- Textos informativos: menciones descriptivas de ACH Colombia/CENIT se conservaron.

## Dependencias y overrides

No se ejecutó `npm audit fix --force`. El lockfile se regeneró mediante instalación normal.
`npm outdated` final solo reporta majors fuera del alcance conservador (Angular/CLI 22, ag-grid 36, Jasmine 6, Puppeteer 25, TypeScript 7 y zone.js 0.16); las versiones `wanted` instaladas están satisfechas.

| Grupo | Motivo y riesgo | Validación / retiro |
|---|---|---|
| `body-parser`, `express`, `qs`, `engine.io`, `socket.io-adapter`, `ws` | Corrigen advisories en servidores/transporte de tooling transitivo. Riesgo bajo de incompatibilidad. | `npm ci`, 466 unitarias, build y E2E. Retirar al incorporarse upstream. |
| `webpack-dev-server`, `postcss`, `ajv`, `tar`, `tmp`, `undici` | Corrigen parsers, archivos temporales, red y build tooling. No forman parte del bundle desplegado como runtime servidor. | Build producción y auditorías. Retirar cuando Angular CLI los resuelva directamente. |
| `@modelcontextprotocol/sdk`, `hono`, `uuid` | Cadena transitiva del Angular CLI/MCP. Quedan tres moderadas en `@hono/node-server`, solo desarrollo. | 0 vulnerabilidades de producción. Actualizar/retirar override cuando Angular publique una cadena corregida. |

## Validación ejecutada

| Comando | Resultado |
|---|---|
| `dotnet restore ACHInterbank.sln` | PASS. |
| `dotnet build ACHInterbank.sln -c Release --no-restore` | PASS, 0 warnings, 0 errors. |
| `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build` con PostgreSQL y SQL Server en `127.0.0.1` | PASS: 1951 passed, 5 skips preexistentes, 0 failed. |
| Pruebas .NET focalizadas de Config Admin/generalización | PASS: 29/29. |
| `npm ci` | PASS desde instalación limpia; 1078 paquetes. |
| `npm test -- --watch=false --browsers=ChromeHeadless` | PASS: 474/474 en 21.801 s. |
| Pruebas Angular focalizadas de resiliencia 429 y estado del formulario | PASS: 28/28 en 51.6 s. |
| `npm run build` | PASS, build producción Angular. |
| `npm audit` | 0 critical, 0 high, 3 moderate, 0 low. |
| `npm audit --omit=dev` | 0 en todas las severidades. |
| `npm run test:e2e:job6` contra Docker real aislado | PASS: 2/2 en 21.1 s, 0 skips, 0 retries finales; residuos `JOB6TEST`: cámara=0, perfil=0. |
| GitHub Actions `angular-ci` run `30106942194` | PASS: `build-and-test=success`, `runtime-backed-e2e=success`; Playwright 2/2 en 11.1 s; 0 skips/retries; cleanup PASS. |
| Parse YAML con `yaml.safe_load` | PASS. |
| `docker compose ... config --quiet` con variables aisladas | PASS. |
| Health y deep link | API live 200, ready 200, SPA 200, `/clearing-houses` 200 `text/html`. |
| `git diff --check` | PASS. |

El repositorio no define script `lint`; no se inventó uno. El workflow fue ejecutado en un runner real de GitHub sobre el head SHA `df46bfce863457c36ceab1b18ee26eceb4d7469d` (checkout del merge de PR `81a8ad14c697f3a27ee63d310cc2dd49c026b11b`).

## Incidencias corregidas durante la ejecución

- Chromium de Playwright ausente: se instaló la versión del lockfile.
- Deep link `/clearing-houses` interceptado por nginx: se separó el contrato API bajo `/api/clearing-houses`.
- Los runs fallidos `30101450168` y `30101455845` confirmaron HTTP 429 en `GET /nacha-config/catalogos-filtro`. El `catchError(() => of(null))` del `forkJoin` dejaba visible un formulario sin opciones y Playwright agotaba 120 s esperando `JOB6TEST`.
- Las lecturas idempotentes de NACHA Config Admin ahora permiten como máximo dos reintentos adicionales, respetan `Retry-After` (segundos o fecha HTTP), usan fallback explícito de 1 s y propagan el error final. Las mutaciones no se reintentan.
- Catálogos se cargan como dependencia obligatoria e independiente: el formulario queda bloqueado durante carga/fallo, muestra causa y acción de reintento, limpia selecciones inválidas y se habilita al recuperarse. Dashboard y perfiles se degradan por separado.
- Playwright espera la respuesta funcional final de catálogos, exige HTTP 200 y `JOB6TEST`, emite diagnóstico con status/URL/`Retry-After`/códigos y garantiza cleanup en `finally`.
- El runtime E2E de CI mantiene el rate limiter activo con configuración aislada `30/1 s`, cola `10`; los defaults permanecen `10/1 s`, cola `2`. Los logs locales comprobaron que el proxy concentra las lecturas en una IP.
- En una base nueva, el E2E asumía un perfil publicado previo para revisar el modal. El escenario ahora publica su propio perfil sintético y lo elimina, sin seed productivo.
- Un test de rutas exigía solo el permiso legacy: se actualizó para comprobar lectura fina y legacy sin relajar el guard.
- SQL Server en `localhost` resolvía por una ruta no alcanzable: se validó con `127.0.0.1`; la suite final completa pasó.
- El volumen local no tenía aplicada una migración existente: se inició la API con `Database__ApplyMigrations=true`; readiness 200 y cero errores posteriores de esquema.

## Bloqueos y riesgos residuales

- Sin bloqueo funcional local.
- Riesgo no crítico: tres moderadas exclusivamente en tooling Angular CLI/MCP, ausentes en producción.
- Riesgo residual bajo: una ráfaga superior a la capacidad aislada todavía produce 429, pero las lecturas elegibles fallan de forma acotada y la UI queda cerrada a mutaciones.
- Evidencia remota: run `30106942194`, artefacto `job6-playwright-runtime` ID `8602165599`, 608946 bytes, digest `sha256:e8d4091cf2c8ba5e389a39b3bf04f498126ce50d233be5ee6465243b8d2be689`.

## Estado de aceptación

| Criterio | Estado |
|---|---|
| Config Admin funcional y autorizado | PASS |
| Generalización y tercera cámara | PASS |
| Responsive y menú móvil | PASS |
| Playwright con runtime real local | PASS |
| Integración CI implementada y ejecutada | PASS confirmado en GitHub Actions |
| Gate npm | PASS |
| Regresión backend/SPA | PASS |
| Trazabilidad y reevaluación | PASS |
