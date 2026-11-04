# Entrega — Unificación de navegación SPA y administración MFT

Se incorpora el programa incremental `NAV-UX-MFT-20261027` sobre la base de ACH Interbank recibida. Este ZIP contiene el **proyecto fuente completo**, las modificaciones y la documentación de continuidad.

## Punto de entrada

- **Plan:** [`docs/architecture/SPA_NAVIGATION_MFT_UNIFICATION_EXECUTION_PLAN_2026-10-27.md`](docs/architecture/SPA_NAVIGATION_MFT_UNIFICATION_EXECUTION_PLAN_2026-10-27.md).
- **Arquitectura:** [`docs/architecture/SPA_NAVIGATION_MFT_ARCHITECTURE_2026-10-27.md`](docs/architecture/SPA_NAVIGATION_MFT_ARCHITECTURE_2026-10-27.md).
- **Memoria del incremento:** [`docs/ai/spa-navigation-unification/MEMORY_STATE.md`](docs/ai/spa-navigation-unification/MEMORY_STATE.md).
- **Continuar en ChatGPT chat:** [`docs/ai/spa-navigation-unification/CHATGPT_CHAT_EXECUTION.md`](docs/ai/spa-navigation-unification/CHATGPT_CHAT_EXECUTION.md).
- **Pruebas al final:** [`docs/testing/SPA_NAVIGATION_MFT_TEST_PLAN_2026-10-27.md`](docs/testing/SPA_NAVIGATION_MFT_TEST_PLAN_2026-10-27.md).
- **Validación estática:** `python scripts/validate-spa-navigation-mft-unification.py`.
- **Manifest:** [`docs/ai/spa-navigation-unification/PACKAGE_MANIFEST.md`](docs/ai/spa-navigation-unification/PACKAGE_MANIFEST.md).

## Resumen de implementación

1. Un **solo botón de apertura** para todas las ramas de menú, con `aria-expanded` y `aria-controls`; enlace secundario accesible cuando el padre posee pantalla funcional.
2. MFT se identifica como **«Administración de transferencias de archivos (MFT)»** bajo **Administración de parámetros → Conexiones**. La ruta se conserva: `/administracion/mft-ach`.
3. No se cambian backend de seguridad, permisos/grants, reglas bancarias, endpoints ni estructura de navegación fuera del copy MFT.
4. Pruebas de fuente: **16 PASS**. Compilación y suites runtime quedan pendientes de un entorno adecuado.

**Nota:** Si el menú desplegado no muestra MFT, corroborar ejecución de los seeders, la respuesta de `GET api/navigation/menu` y los claims `Config.Read`/`CanReadAch` antes de atribuirlo a Angular.

## Entrega incremental I04A

Se formaliza el contrato de **entrega completa después de cada implementación**. Consultar:

- `docs/ai/spa-navigation-unification/DELIVERY_PROTOCOL.md` (regla de empaquetado y pruebas).
- `docs/ai/spa-navigation-unification/PROGRESS_REPORT_I04A_2026-10-27.md` (avance detallado).
- `docs/ai/spa-navigation-unification/MEMORY_STATE.md` (continuidad de incrementos).

Este incremento I04A es documental y **no** supone nuevas correcciones de fuentes de SPA/backend ni certificación runtime. SHA-256 del ZIP actual en archivo lateral fuera del paquete.

## I05 — corrección funcional nueva y protección de la línea base

A diferencia del I04A documental, I05 corrige el estado ARIA de una rama al ocultarse en escritorio compacto, agrega una prueba de regresión, amplía la validación a **19/19** y añade **3/3 pruebas unitarias offline del verificador de linaje** y verifica con SHA-256 la procedencia del ZIP padre I04A. El código y los permisos MFT permanecen intactos desde I02. La compilación Angular/.NET y los escenarios runtime continúan pendientes; no publicar como certificado.

Continuar desde `ACHInterbank_NAV_UX_MFT_2026-10-27_I05_PROYECTO_COMPLETO.zip` (hash lateral). Ver `docs/ai/spa-navigation-unification/PROGRESS_REPORT_I05_2026-10-27.md`, `BASELINE_LINEAGE_I05.md` y `MEMORY_STATE.md`.

## I06 — hover/contraste del menú corregido en fuente

El botón `mat-list-item` del sidebar ya no hereda los colores de hover de botones globales y se refuerzan estilos accesibles de hover/foco. Se agregaron cuatro invariantes (23/23 PASS) y un E2E Playwright para verificar contraste AA ≥4.5:1 con branding claro/oscuro y color primario claro. La prueba E2E **no fue ejecutada**; no está certificada la estabilización en navegador.

**Línea base para continuar:** `ACHInterbank_NAV_UX_MFT_2026-10-27_I06_PROYECTO_COMPLETO.zip` (checksum lateral); consultar `docs/ai/spa-navigation-unification/MEMORY_STATE.md` y `PROGRESS_REPORT_I06_2026-10-27.md`.
