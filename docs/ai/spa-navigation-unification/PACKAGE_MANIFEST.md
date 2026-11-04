# Package manifest — NAV-UX-MFT-20261027

- **Fuente de entrada:** `ACHInterbank_ACH_CR_I06_FIX01_SEC_I06A_TESTS_PREPARATION_COMPLETO.zip`.
- **SHA-256 entrada:** `b0760a839498f1acaaa4c3465a79087e61d6ac804660d75d3da0f58c675eb897`.
- **Entrega proyectada:** `ACHInterbank_NAV_UX_MFT_2026-10-27_PROYECTO_COMPLETO.zip`.
- **Modo de salida:** full source ZIP con `src/`, `web/`, `tests/`, `docs/`, `database/`, `scripts/`, configuración, dependencias y lockfiles; no sólo delta.
- **Archivos fuente retenidos de la entrada:** 4462; archivos fuente faltantes: 0.
- **Archivos alterados:** 15; **archivos añadidos (sin contar este manifest):** 7.
- **Excluidos del paquete por regenerables o historial:** `.git/`, `bin/`, `obj/`, `artifacts/`, `node_modules/`. No se excluyen fuentes, migraciones ni test code.
- **Estado verificación:** 16/16 static PASS; 4/4 TypeScript transpile/syntax PASS; Angular/.NET/Playwright/runtime **no ejecutadas**.
- **No autorizar producción:** se requiere QA real y política de despliegue de seed.
- **Hash del ZIP final:** se calcula **fuera** del ZIP, en el archivo lateral `ACHInterbank_NAV_UX_MFT_2026-10-27_SHA256.txt` para evitar circularidad.

## Archivos modificados (SHA-256)

| Ruta relativa | SHA-256 |
| --- | --- |
| `MEMORY.md` | `5966d26259a9419e67861d7a2df38d531a0828ef007e38f1283a2252fb456e1e` |
| `docs/ai/navigation-menu/MEMORY_STATE.md` | `e39bd9f413199f1743deeebea89927517bed6492b00e41d0fab3bcbe624a01cd` |
| `src/Cfa.ACHInterbank.Persistence/Navigation/Seeders/AchColombiaMftAdministrationMenuSeeder.cs` | `e2245ebc94f152a16128fb64cfbe08282fdeb4a8b6b62832bc7721c2a2454e22` |
| `src/Cfa.ACHInterbank.Persistence/Navigation/Seeders/NavigationInformationArchitectureSeeder.cs` | `cbba925782023fc37fc05f65bba30ebe8bb10f5df61dedd8e76e83b687325080` |
| `tests/Cfa.ACHInterbank.Tests/NavigationMenuNav22ParametersInformationArchitectureTests.cs` | `34458972f68b8d64e20f35bac84f36bc0c388913359895b4022677a43c136faa` |
| `tests/Cfa.ACHInterbank.Tests/NavigationMenuNav34MMftAdministrationAuthorizationTests.cs` | `b5a28b009c751aa8f72a3cf5ad751084167355f3ba4edf52adee048de011db77` |
| `tests/Cfa.ACHInterbank.Tests/NavigationMenuNav34NConnectionsNavigationTests.cs` | `eea3c10b5402a861cc1df085e6e6d31bd46113331fe8190699522d2adf8d1513` |
| `web/ach-interbank-ui/src/app/app-routing.module.ts` | `ee59a3d8f61d5fcb095d0331e605992b4bc2ff073dbf85153afa092225ded988` |
| `web/ach-interbank-ui/src/app/core/services/navigation-naming.service.spec.ts` | `889d7d31192e247bdab487ca3bb96fe36c9965c34aefed6c799d9a207d4244cb` |
| `web/ach-interbank-ui/src/app/core/services/navigation.service.spec.ts` | `e9bc23d5af3cf21777b6a2f0da984f1fc4d7395ff0e7b7b47de2f64d7d2201c6` |
| `web/ach-interbank-ui/src/app/features/ach-colombia-file-exchange/ach-colombia-file-exchange.component.html` | `8495da7dbaa19cf76a9940a85106cd86399b686fa74bc817a6dcb8350da9a720` |
| `web/ach-interbank-ui/src/app/features/ach-colombia-mft-administration/ach-colombia-mft-administration.component.html` | `5730466fe1ee7ddaa8535f7e0a629228fe8086b741e1ef5abfbcf5f97b280b25` |
| `web/ach-interbank-ui/src/app/layout/main-layout.component.html` | `50fd415bd38010d2ba844dae94fbf67bf4355752bae7b8d51229f3cabe304176` |
| `web/ach-interbank-ui/src/app/layout/main-layout.component.scss` | `80a12006c5b2f9ed135bad2123d0c3af6e4adc243b1393043b590ceba4b0ea94` |
| `web/ach-interbank-ui/src/app/layout/main-layout.component.spec.ts` | `e75cba47780930523f44193ee8a5136012240804f7e6b3a1235070fade060c37` |

## Archivos nuevos (SHA-256; sin incluir este manifest)

| Ruta relativa | SHA-256 |
| --- | --- |
| `README_NAV_UX_MFT_2026-10-27.md` | `9d206174b2206db87796368558a0b87a41bf41d11cd2afca3509c53671e40f22` |
| `docs/ai/spa-navigation-unification/CHATGPT_CHAT_EXECUTION.md` | `f8b1a8782b12b6d1af27e1ba90f26dfe7ed34b22b712ba7cca4e4fb2f0966f26` |
| `docs/ai/spa-navigation-unification/MEMORY_STATE.md` | `a453e18567cf4646b3d9f1fd294ccc6625a9f9aea5471abdfe16caa5d277a40a` |
| `docs/architecture/SPA_NAVIGATION_MFT_ARCHITECTURE_2026-10-27.md` | `d64f49482b00a5f67381f08ceb324e0b8218f904d6305c05ed737dac1951b6e0` |
| `docs/architecture/SPA_NAVIGATION_MFT_UNIFICATION_EXECUTION_PLAN_2026-10-27.md` | `b155dd377b5d26f4ee2e9a93bd513ec3c5a17cd48b22dd2d672912849f8f952c` |
| `docs/testing/SPA_NAVIGATION_MFT_TEST_PLAN_2026-10-27.md` | `aa2698568791433cedc7bbd0bda10680e526f586b46149c6f3010b1c74150e5d` |
| `scripts/validate-spa-navigation-mft-unification.py` | `c4ad4fee62688335ffa44a6f78029350565ce3417c0d8c4232fe022f7eb17f5e` |

## Evidencia y criterios de continuidad

Consultar `MEMORY_STATE.md` del mismo directorio y `docs/testing/SPA_NAVIGATION_MFT_TEST_PLAN_2026-10-27.md`. Los hashes reflejan el contenido previo a insertar este manifest; si se modifican fuentes posteriormente, regenerar el manifest.

## Addendum I04A — contrato de entregas incrementales

El manifiesto precedente documenta el incremento de fuentes `I04` y conserva su inventario histórico. La entrega `I04A` deriva del archivo `ACHInterbank_NAV_UX_MFT_2026-10-27_PROYECTO_COMPLETO.zip` (`SHA-256 607e90b90ab5f71de61f9e66656679d9fe99b849dea1df844f38d3cf023c4cb0`).

- Nuevo artefacto: `ACHInterbank_NAV_UX_MFT_2026-10-27_I04A_PROYECTO_COMPLETO.zip`.
- Todas las rutas de la base se preservan. No hay cambios de fuente de aplicación en I04A.
- Documentos **nuevos**: `docs/ai/spa-navigation-unification/DELIVERY_PROTOCOL.md` y `docs/ai/spa-navigation-unification/PROGRESS_REPORT_I04A_2026-10-27.md`.
- Documentos **actualizados**: `docs/ai/spa-navigation-unification/MEMORY_STATE.md`, `CHATGPT_CHAT_EXECUTION.md`, `PACKAGE_MANIFEST.md` y `README_NAV_UX_MFT_2026-10-27.md`.
- La memoria y el informe son también archivos Markdown externos idénticos para revisión rápida.
- El hash de la entrega I04A debe consultarse en el archivo lateral `ACHInterbank_NAV_UX_MFT_2026-10-27_I04A_SHA256.txt`.
- No se ejecutaron nuevas pruebas de frontend/backend o E2E en I04A: continúa el estado `IMPLEMENTED_RUNTIME_PENDING`.

## Addendum I06 — menú sidebar con contraste hover/foco

- **Entrada:** `ACHInterbank_NAV_UX_MFT_2026-10-27_I05_PROYECTO_COMPLETO.zip` (SHA-256 `ce27bd3b3e062211fb8d11964fde30246a7627a77931a7d556ebaf4cc20da512`).
- **Salida:** `ACHInterbank_NAV_UX_MFT_2026-10-27_I06_PROYECTO_COMPLETO.zip`, hash externo `ACHInterbank_NAV_UX_MFT_2026-10-27_I06_SHA256.txt`.
- **Código modificado:** `web/ach-interbank-ui/src/styles/_components.scss`, `web/ach-interbank-ui/src/app/layout/main-layout.component.scss`.
- **Nuevo test:** `web/ach-interbank-ui/e2e/navigation-sidebar-hover-contrast.spec.ts`. **Sin ejecución Playwright**.
- **Script actualizado:** `scripts/validate-spa-navigation-mft-unification.py` (23/23).
- **Continuidad:** `BASELINE_LINEAGE_I06.md`, `LINEAGE_VERIFICATION_I06.json`, `PROGRESS_REPORT_I06_2026-10-27.md`, `TEST_EVIDENCE_I06_2026-10-27.md`, `MEMORY_STATE.md`.
- **Estado:** fuente implementada; compilación/angular/browser/backend no certificados.
