# MEMORY STATE — NAV-UX-MFT-20261027

> **Último incremento técnico: I06 (hover/contraste).** I04A fue solo documentación; no repetir I01/I02. La línea base verificada para I06 es el ZIP I05 (SHA-256 ce27bd3b3e062211fb8d11964fde30246a7627a77931a7d556ebaf4cc20da512); I05 deriva de I04A. La comparación de contenidos I04→I04A confirmó cero cambios de código y ninguna eliminación.

Este archivo es **la fuente de continuidad** del nuevo incremento. No sustituye los estados históricos de `NAV-MENU-IA-001`, `ACHCOL_CFA_FILE_BASED`, `SEC-I06A` u otros programas: sólo registra los cambios de navegación y etiqueta MFT del paquete actual.

```yaml
schema: ACH_INTERBANK_INCREMENTAL_MEMORY_V1
program: NAV-UX-MFT-20261027
as_of: 2026-10-27
status: I06_SOURCE_IMPLEMENTED_STATIC_PASS_RUNTIME_PENDING
status_explanation: I06 corrige colisión de estilos globales al hacer hover en menú y agrega E2E de contraste; Angular/.NET/E2E siguen pendientes.
mode: CHATGPT_CHAT_NO_GIT_FULL_SOURCE_ZIP
original_project_origin_artifact: ACHInterbank_ACH_CR_I06_FIX01_SEC_I06A_TESTS_PREPARATION_COMPLETO.zip
original_project_origin_sha256: b0760a839498f1acaaa4c3465a79087e61d6ac804660d75d3da0f58c675eb897
parent_artifact: ACHInterbank_NAV_UX_MFT_2026-10-27_I05_PROYECTO_COMPLETO.zip
parent_sha256: ce27bd3b3e062211fb8d11964fde30246a7627a77931a7d556ebaf4cc20da512
output_artifact: ACHInterbank_NAV_UX_MFT_2026-10-27_I06_PROYECTO_COMPLETO.zip
increment_id: I06_SIDEBAR_HOVER_CONTRAST_ISOLATION
mandatory_incremental_delivery: FULL_PROJECT_ZIP_PLUS_UPDATED_MD_MEMORY_PLUS_PROGRESS_PLUS_EXTERNAL_SHA256
output_sha256: SEE_PACKAGE_MANIFEST_OR_DOWNLOAD_HASH_EXTERNAL
source_of_truth: Backend NavigationInformationArchitectureSeeder and authorizations
mft_route: /administracion/mft-ach
mft_menu_path: Administración de parámetros > Conexiones > Administración de transferencias de archivos (MFT)
mft_read_capabilities_any: [Config.Read, CanReadAch]
mft_write_permissions_unchanged: true
legacy_NAV3_5C: BLOCKED_NO_EXECUTE
security_grants_modified: false
api_contracts_modified: false
business_rules_modified: false

increments:
  I00_BASELINE:
    status: ANALYZED
    findings:
      - Structural vs functional parents used different opening patterns.
      - MFT SPA route and backend menu were present in source; deployed visibility unverified.
      - Navigation has dynamic API menu; no hardcoded Angular tree.
  I01_UNIFIED_DISCLOSURE:
    status: IMPLEMENTED_RUNTIME_PENDING
    change: Whole-row branch button plus secondary accessible destination link for functional parents.
    files:
      - web/ach-interbank-ui/src/app/layout/main-layout.component.html
      - web/ach-interbank-ui/src/app/layout/main-layout.component.scss
    guarantees: [keyboard_button, aria_expanded, aria_controls, desktop_compact, mobile_drawer]
  I02_MFT_DISCOVERABILITY:
    status: IMPLEMENTED_RUNTIME_PENDING
    change: Explicit MFT name in existing canonical/functional seed, Angular route metadata and screen copy.
    files:
      - src/Cfa.ACHInterbank.Persistence/Navigation/Seeders/NavigationInformationArchitectureSeeder.cs
      - src/Cfa.ACHInterbank.Persistence/Navigation/Seeders/AchColombiaMftAdministrationMenuSeeder.cs
      - web/ach-interbank-ui/src/app/app-routing.module.ts
      - web/ach-interbank-ui/src/app/features/ach-colombia-mft-administration/ach-colombia-mft-administration.component.html
      - web/ach-interbank-ui/src/app/features/ach-colombia-file-exchange/ach-colombia-file-exchange.component.html
  I03_TEST_PREPARATION:
    status: STATIC_PASS_RUNTIME_PENDING
    static_test: python scripts/validate-spa-navigation-mft-unification.py
    static_assertions: 19
    static_result: PASS
    typescript_syntax: PASS_4_FILES_GLOBAL_COMPILER_TRANSPILE_ONLY
    npm_ci_offline: BLOCKED_ENOTCACHED_ZONE_JS_0_16_3
    automated_runtime: NOT_RUN_ENVIRONMENT
    tests_updated:
      - web/ach-interbank-ui/src/app/layout/main-layout.component.spec.ts
      - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav22ParametersInformationArchitectureTests.cs
      - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav34MMftAdministrationAuthorizationTests.cs
      - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav34NConnectionsNavigationTests.cs
      - web/ach-interbank-ui/src/app/core/services/navigation-naming.service.spec.ts
      - web/ach-interbank-ui/src/app/core/services/navigation.service.spec.ts
  I04_HANDOFF:
    status: IMPLEMENTED
    files:
      - docs/architecture/SPA_NAVIGATION_MFT_UNIFICATION_EXECUTION_PLAN_2026-10-27.md
      - docs/architecture/SPA_NAVIGATION_MFT_ARCHITECTURE_2026-10-27.md
      - docs/ai/spa-navigation-unification/MEMORY_STATE.md
      - docs/ai/spa-navigation-unification/CHATGPT_CHAT_EXECUTION.md
      - docs/testing/SPA_NAVIGATION_MFT_TEST_PLAN_2026-10-27.md
      - docs/ai/spa-navigation-unification/PACKAGE_MANIFEST.md
  I04A_DELIVERY_PROTOCOL:
    status: DOCUMENTED_AND_PACKAGED
    scope: Incremental handoff contract; no application source changes.
    files:
      - docs/ai/spa-navigation-unification/DELIVERY_PROTOCOL.md
      - docs/ai/spa-navigation-unification/PROGRESS_REPORT_I04A_2026-10-27.md
      - docs/ai/spa-navigation-unification/MEMORY_STATE.md
      - docs/ai/spa-navigation-unification/CHATGPT_CHAT_EXECUTION.md
      - docs/ai/spa-navigation-unification/PACKAGE_MANIFEST.md
      - README_NAV_UX_MFT_2026-10-27.md
    test_status: NO_NEW_RUNTIME_TESTS; PACKAGE_VERIFICATION_ONLY
  I05_COMPACT_A11Y_AND_LINEAGE:
    status: STATIC_PASS_RUNTIME_PENDING
    finding: In desktop compact mode aria-expanded could be true while the child subtree was not rendered.
    fix: isItemExpanded now reflects effective visibility; expandedItems retains prior user state for restoration.
    changed_application_files:
      - web/ach-interbank-ui/src/app/layout/main-layout.component.ts
      - web/ach-interbank-ui/src/app/layout/main-layout.component.html
    changed_test_files:
      - web/ach-interbank-ui/src/app/layout/main-layout.component.spec.ts
      - scripts/validate-spa-navigation-mft-unification.py
    new_lineage_script: scripts/verify-incremental-source-lineage.py
    new_lineage_regression_tests: scripts/test/test_incremental_source_lineage.py
    lineage_negative_tests: PASS_3_OF_3
    parent_zip_sha256: 94e6d0c03576b26a9ff34cd32a070679b6e49261f7facb2efe007aca8d2aabd0
    previous_sources_retained: 4472
    previous_sources_missing: 0
    static_assertions: 19
    static_result: PASS
    typescript_syntax: PASS_4_FILES_TRANSPILE_ONLY
    angular_build_and_tests: NOT_RUN_ENVIRONMENT
    dotnet_build_and_tests: NOT_RUN_ENVIRONMENT
    deployed_menu_auth: PENDING_DEPLOYMENT
    no_mft_permissions_or_routing_changes: true

  I06_SIDEBAR_HOVER_CONTRAST_ISOLATION:
    status: SOURCE_IMPLEMENTED_STATIC_PASS_RUNTIME_PENDING
    parent_artifact: ACHInterbank_NAV_UX_MFT_2026-10-27_I05_PROYECTO_COMPLETO.zip
    parent_sha256: ce27bd3b3e062211fb8d11964fde30246a7627a77931a7d556ebaf4cc20da512
    change: Excluir botones mat-list-item de estilos globales; reforzar colores de hover/foco en sidebar para evitar mimetización.
    changed_application_files:
      - web/ach-interbank-ui/src/styles/_components.scss
      - web/ach-interbank-ui/src/app/layout/main-layout.component.scss
    added_e2e: web/ach-interbank-ui/e2e/navigation-sidebar-hover-contrast.spec.ts
    static_validation_script: scripts/validate-spa-navigation-mft-unification.py
    static_result: PASS_23_OF_23
    typescript_syntax: PASS_4_OF_4_TRANSPILE_ONLY
    angular_build_and_tests: NOT_RUN_ENVIRONMENT
    browser_contrast_test: NOT_RUN_ENVIRONMENT
    dotnet_mft_tests: NOT_RUN_ENVIRONMENT
    permissions_or_mft_routing_changed: false

quality_gates:
  static_source: PASS_23_OF_23
  angular_unit: NOT_RUN_MISSING_NODE_MODULES
  angular_build: NOT_RUN_MISSING_NODE_MODULES
  playwright: NOT_RUN_MISSING_NODE_MODULES_BROWSER_SETUP
  dotnet_compile_tests: NOT_RUN_DOTNET_SDK_UNAVAILABLE
  deployed_backend_menu_response: PENDING_DEPLOYMENT
  deployed_mft_authorization: PENDING_DEPLOYMENT
  runtime_release_approval: NOT_GRANTED

next_action: Runtime certification of I06 including Playwright hover contrast across active/expanded/focus and customized branding; Angular, .NET, MFT permissions remain pending.
completion_rule: Do not set CLOSED until Angular build/unit, dotnet tests, E2E and permission-boundary deployment checks pass against exact artifact.
```

## Registro incremental (append-only)

| ID | Alcance | Evidencia | Resultado | Siguiente gate |
| --- | --- | --- | --- | --- |
| `I00` | Auditoría del ZIP | Árbol SPA/backend y guardas MFT identificados | ANALYZED | Unificación |
| `I01` | Control desplegable común | Template/estilos modificados y unit ampliado | IMPLEMENTED | Build/test Angular |
| `I02` | MFT reconocible | Seed #40 bajo Conexiones; ruta y guards preservados | IMPLEMENTED | Seed desde vacío y repetido + claims |
| `I03` | Pruebas | 16 invariantes estáticos PASS; pruebas Angular/C# ajustadas | STATIC_PASS | Runtime final |
| `I04` | Entrega | Plan, arquitectura, test-plan, prompts, manifest, memoria | IMPLEMENTED | Aceptación humana |
| `I04A` | Protocolo de entregas | ZIP completo por incremento + memoria e informe de avance + SHA-256; sin código nuevo | DOCUMENTED_AND_PACKAGED | Cumplido en I05 |
| `I05` | Accesibilidad compacto + control de linaje | 19/19 validaciones estáticas; 4/4 sintaxis TS; 3/3 tests offline linaje; SHA padre y 4.472 archivos retenidos | STATIC_PASS_RUNTIME_PENDING | Angular/.NET/E2E, QA seed/permisos |
| `I06` | Corrección de hover/contraste de menú | Botones list-item excluidos de estilos genéricos; 23/23 estáticos; 4/4 TS sintáctico; nuevo Playwright contraste ≥4.5:1 sin ejecutar | SOURCE_IMPLEMENTED_RUNTIME_PENDING | Build, browser E2E, QA manual MFT |

## Resumen de decisiones que no deben perderse

1. La fuente de verdad es el **seed/backend**, no un array hardcodeado Angular.
2. Toda rama se abre con **botón principal**, toda hoja navega por enlace; los padres funcionales reciben un **enlace secundario**.
3. MFT **no** se clona en Administración: permanece en Conexiones, bajo Administración de parámetros.
4. Visible MFT **no** implica permiso de modificación. No inventar permisos/grants ni retirar políticas existentes.
5. La inexistencia de una prueba ejecutable no impide implementar; sí impide declarar estado `CLOSED`.
6. Mantener `NAV3.5C` histórico **BLOCKED**.

## Instrucción de reanudación

**NO iniciar desde I04/I04A/I05 para repetir cambios.** A partir de la presente entrega, usar `ACHInterbank_NAV_UX_MFT_2026-10-27_I06_PROYECTO_COMPLETO.zip` como base (su SHA-256 está en el archivo lateral I05). I05 no certifica runtime; su prioridad al reanudar es validar los gates, detectar fallos nuevos y corregirlos, no rehacer código previamente implementado. Si el ZIP I05 no está disponible, declarar la falta de la última línea base y no afirmar que un ZIP anterior es el más reciente.

Leer `AGENTS.md`, `MEMORY.md`, este documento y `docs/ai/spa-navigation-unification/CHATGPT_CHAT_EXECUTION.md` antes de cambios. Partir del ZIP completo generado, nunca de fragmentos de respuesta. Ejecutar primero los gates pendientes; corregir fallos reales en un nuevo incremento `I05` sólo cuando existan resultados. Preservar la documentación de estado y registrar hashes de cada nuevo paquete.

## Contrato de entrega actualizado (I04A)

La solicitud del usuario establece que **cada incremento debe entregarse como un ZIP completo**, acompañado por documentación y `MEMORY_STATE.md` modificados, además de un informe Markdown de avance. Esta decisión está persistida en `docs/ai/spa-navigation-unification/DELIVERY_PROTOCOL.md` (incluido en el ZIP). Se debe conservar el historial de estados y actualizar el reporte después de cada incremento sin confundir trabajo aplicado con pruebas ejecutadas.

**Corte I04A:** documentación y reempaquetado solamente; ningún nuevo cambio de fuente de aplicación; I05 sigue pendiente. Ver `PROGRESS_REPORT_I04A_2026-10-27.md` para estado por frente. El SHA-256 del ZIP I04A es un archivo **externo**, no incrustado aquí para evitar autorreferencia.

## Cierre incremental I05 — 2026-10-27

- Se confirma que I04A no cambió fuente funcional frente a I04 (0 modificaciones de aplicación).
- I05 corrige una inconsistencia real del estado accesible al compactar el panel lateral y agrega prueba de regresión.
- Se agrega una guarda automatizada de procedencia `scripts/verify-incremental-source-lineage.py`. **Solo garantiza continuidad frente al ZIP padre suministrado; no descubre versiones posteriores no proporcionadas.**
- Validación estructural 19/19, sintaxis TypeScript 4/4, tests de linaje 3/3 y conservación del árbol fuente padre 4.472/4.472; no hubo cambios en rutas/permisos MFT.
- Compilación Angular, Karma, Playwright, .NET, seed e inspección de menú desplegado **no ejecutadas** en este entorno. No aprobado para producción.
- Reporte: `PROGRESS_REPORT_I05_2026-10-27.md`; evidencia: `TEST_EVIDENCE_I05_2026-10-27.md`; evidencia de linaje: `LINEAGE_VERIFICATION_I05.json`; plan: `docs/testing/SPA_NAVIGATION_MFT_TEST_PLAN_2026-10-27.md`.

## Cierre incremental I06 — 2026-10-27

- Incidencia nueva comunicada: en hover el texto de algunas opciones podía mimetizarse con el fondo. Se identifica en fuente una colisión entre `button:not(.mat-mdc-button-base):hover` global y `button[mat-list-item]` del sidebar. No se ejecutó navegador para reproducir antes/después.
- Se excluyen `mat-list-item` de los estilos de botón genérico, conservando diseño global fuera del menú; se refuerzan tokens Material de texto/iconos y fondo en hover/foco.
- E2E nuevo evalúa contraste visual WCAG AA (≥4.5:1) en branding claro y oscuro, rama expandida, hoja con foco, hover y selección. **NOT_RUN_ENVIRONMENT.**
- Fuente: 23/23 invariantes estáticos PASS; 4/4 archivos TS transpilados sin error sintáctico; no certificación de UI renderizada.
- Linaje desde padre I05 verificado (SHA completo en `BASELINE_LINEAGE_I06.md`). ZIP I06 y checksum externo; ver `PROGRESS_REPORT_I06_2026-10-27.md`.
- Siguiente: ejecutar Angular y E2E con entorno local autorizado, revisar contraste real también con paletas personalizadas y QA de permisos MFT.
