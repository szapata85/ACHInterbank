# Plan final de pruebas — NAV-UX-MFT-20261027

El usuario solicitó **no suspender la implementación** si las pruebas no están disponibles: las pruebas se dejan al final, separando **PASS real** de `NOT_RUN_ENVIRONMENT`.

## 1. Alcance y comandos

| Prueba | Comando/acción | Resultado esperado | Estado inicial |
| --- | --- | --- | --- |
| Invariantes offline | `python scripts/validate-spa-navigation-mft-unification.py` | 19/19 después de I05 | **PASS**, 19/19 |
| Build Angular | `cd web/ach-interbank-ui && npm ci && npm run build` | Compila sin errores de plantilla/TS | PENDIENTE |
| Unit del layout | `npm test -- --watch=false --browsers=ChromeHeadless --include=src/app/layout/main-layout.component.spec.ts` | Ramas/hojas/compacto/móvil/teclado correctos | PENDIENTE |
| Unit de naming/navigation | Ejecutar specs de `navigation-naming.service`, `navigation.service` | Fuente de verdad y label MFT consistentes | PENDIENTE |
| xUnit canon | `dotnet test ACHInterbank.sln --filter 'FullyQualifiedName~NavigationMenuNav22ParametersInformationArchitectureTests'` | Ruta e ID MFT únicos; order=40 | PENDIENTE |
| xUnit MFT | `dotnet test ACHInterbank.sln --filter 'FullyQualifiedName~NavigationMenuNav34MMftAdministrationAuthorizationTests|FullyQualifiedName~NavigationMenuNav34NConnectionsNavigationTests'` | `Config.Read OR CanReadAch`, sin grants extra | PENDIENTE |
| E2E SPA | `npm run e2e -- --project=chromium e2e/navigation-and-login-icons-regression.spec.ts` | Visual/teclado/móvil/compacto | PENDIENTE |
| Verificación API con base | Correr seeds y consultar `GET api/navigation/menu` con credenciales de QA | MFT visible sólo para perfiles autorizados | PENDIENTE |

## 2. Matriz funcional de interacción

| Caso | Esperado |
| --- | --- |
| Grupo sin landing | **Un** botón fila completa, despliega y no cambia URL |
| Grupo con landing | Mismo botón para desplegar, más enlace separado `Abrir página de…` |
| Dos niveles y tres niveles | Se puede abrir/cerrar cada profundidad; `aria-controls` coincide con submenú |
| Grupo activo | Se marcan y expanden los ancestros según ruta |
| Escritorio compacto | Clic grupo expande sidebar y grupo; no navega inesperadamente; `aria-expanded=false` mientras su subárbol está oculto y restaura expansión al volver a desplegar |
| Móvil | Abrir grupo no cierra drawer; navegar hoja o destino sí lo cierra |
| Escape / backdrop | Cierra drawer y restituye foco conforme al patrón existente |
| Actualización de menú API | `NavigationService` conserva árbol/orden y no añade MFT si no viene en respuesta |
| MFT con `Config.Read` | Enlace aparece bajo `Conexiones`; ruta accesible para lectura |
| MFT con `CanReadAch` | Mismo resultado de lectura |
| MFT sin lectura | No aparece; acceso directo a SPA/API rechazado |
| MFT solo lectura | Opciones de escritura siguen deshabilitadas y API rechaza modificaciones |
| Re-seed | Misma ruta una vez, mismo padre, mismo orden, label actualizado, sin duplicados |

## 3. Criterios de seguridad

- No conceder o cambiar `CanReadAch`, `Config.Read`, `CanManageAch`, `Config.Manage` en este trabajo.
- No usar autorización de menú como sustituto de autorización de API.
- Revisar configuración de roles/claims de usuarios QA sin registrar tokens o secretos en evidencia.
- Confirmar que `Conexiones` no oculta el MFT de un usuario autorizado debido al filtrado ancestral (tests `NAV3.4N`).
- No probar integración real MFT/SFTP con datos productivos; usar conexiones y credenciales de pruebas.

## 4. Registro de ejecución

```yaml
as_of: 2026-10-27
static_source: PASS_19_OF_19
angular_build: NOT_RUN_ENVIRONMENT_NPM_CI_OFFLINE_ENOTCACHED_ZONE_JS_0_16_3
angular_unit: NOT_RUN_ENVIRONMENT_NO_NODE_MODULES
playwright: NOT_RUN_ENVIRONMENT_NO_NODE_MODULES
dotnet_tests: NOT_RUN_ENVIRONMENT_DOTNET_SDK_NOT_INSTALLED
database_seed_runtime: NOT_RUN_ENVIRONMENT
manual_access_matrix: NOT_RUN_ENVIRONMENT
release_status: NOT_CERTIFIED
```

Actualizar estos valores **sólo con evidencia real** y registrar versión/fecha/hash del ZIP probado. Falta de runtime **no** invalida los cambios en fuentes ni autoriza decir que el software está certificado.

## 5. Ejecución registrada en la entrega

- `python scripts/validate-spa-navigation-mft-unification.py`: **PASS 16/16**.
- TypeScript `transpileModule` con compilador global: **PASS de sintaxis 4/4 archivos**; no ejecuta análisis semántico ni Angular AOT.
- `npm ci --offline --ignore-scripts --no-audit --no-fund`: **no completó**, código `ENOTCACHED` para `zone.js@0.16.3`. No se ejecutó build ni Karma/Playwright por dependencias ausentes.
- `dotnet`: **no instalado** en el entorno; xUnit queda no ejecutado.
- QA de base de datos/deployment: **no ejecutada** porque no se suministró entorno de base/credenciales de prueba.

**Conclusión:** fuente modificada y validación estática positiva. No certificación runtime.

## 6. Corte incremental I05 (sin certificación de ejecución)

- `scripts/validate-spa-navigation-mft-unification.py`: **PASS 19/19** en la presente entrega. Incluye tres nuevas comprobaciones sobre el cómputo de expansión efectiva del menú compacto, el render de submenú y la presencia de la prueba de regresión.
- `typescript.transpileModule` con TypeScript global: **PASS 4/4** archivos (`main-layout.component.ts`, `.spec.ts`, `app-routing.module.ts`, `navigation.service.ts`); sintaxis/transpilación únicamente, no template/AOT ni typecheck del proyecto.
- `scripts/verify-incremental-source-lineage.py`: **PASS** contra ZIP I04A SHA-256 `94e6d0c...aabd0` (hash completo en `BASELINE_LINEAGE_I05.md`); 4.472 archivos del padre conservados y cero eliminados.
- `npm ci`, `ng build`, Karma, Playwright y xUnit: **NOT_RUN_ENVIRONMENT**, sin afirmar nuevos fallos ni éxitos. No se ejecutó `npm ci` en I05 porque `zone.js@0.16.3` no está disponible en el caché offline y no se cuenta con instalación Angular; se verificó la ausencia del paquete en la caché. No hay SDK .NET.
- La corrección I05 se considera implementada en fuentes, **pendiente de validar en navegador real**. Ver `PROGRESS_REPORT_I05_2026-10-27.md`.

- Nueva batería offline de linaje I05: `python -m unittest scripts/test/test_incremental_source_lineage.py -v`, **PASS 3/3**. Contiene caso positivo con fuente modificada, SHA-256 padre incorrecto y archivo original ausente.

## 7. I06 — regresión hover/contraste (no certificada aún)

**Defecto identificado en fuente:** regla global de `button:not(.mat-mdc-button-base):hover` podía interceptar botones `[mat-list-item]`, cambiando el fondo del menú a `--color-primary-hover` sin armonizar el texto `--color-on-sidebar`.

**Prueba automatizada añadida:**

```bash
cd web/ach-interbank-ui
npx playwright test e2e/navigation-sidebar-hover-contrast.spec.ts
```

Criterios: contraste texto/superficie ≥4.5:1 (WCAG AA), con sidebar claro/oscuro, color primario claro, hover de grupo y hoja, expandido, foco de teclado y hoja activa. Probar también manualmente iconos y scroll del menú compacto y móvil. La prueba exige `ACH_USER`/`ACH_PASS` de QA, SPA+API locales autorizadas y paquetes instalados.

**Estado I06:** estáticos **PASS 23/23**; sintaxis TS **PASS 4/4**; Angular build/Karma/Playwright/.NET y matrix de roles MFT **NOT_RUN_ENVIRONMENT**. No concluir estabilización completa antes de las pruebas visuales.
