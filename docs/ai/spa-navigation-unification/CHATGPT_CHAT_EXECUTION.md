# Guía de ejecución en ChatGPT (modo chat, sin depender de agentes)

## Instrucción lista para copiar en un nuevo chat

> Trabaja sobre el ZIP completo de ACH Interbank que adjunto. Lee `AGENTS.md`, `MEMORY.md`, `docs/ai/navigation-menu/MEMORY_STATE.md` y `docs/ai/spa-navigation-unification/MEMORY_STATE.md`. Respeta las decisiones ya registradas. Tu tarea es **continuar** `NAV-UX-MFT-20261027` en el incremento que marque `next_action`: validar y corregir la unificación del menú SPA y la visibilidad/autorización MFT, sin inventar nuevas rutas, permisos o grants. Haz primero auditoría del estado real. Ejecuta los tests que permita el entorno; cuando no puedas ejecutarlos, anota `NOT_RUN_ENVIRONMENT` sin detener otros cambios. Entrega **todo el código fuente** con documentos Markdown, `MEMORY_STATE.md` actualizado, evidencia de pruebas y SHA-256 del ZIP final. No marques CLOSED sin tests runtime reales. No ejecutes `NAV3.5C`. No pidas confirmación si puedes avanzar con estas decisiones.

## Pasos en cada conversación de ejecución

1. **Recepción:** comprobar que el ZIP corresponde a `output_artifact` del estado anterior; calcular SHA-256 y registrar diferencias.
2. **Alcance:** ejecutar únicamente el incremento próximo (por ejemplo `I05` de pruebas/ajustes), sin reescribir otros dominios.
3. **Implementación:** tocar seed canónico/backend o SPA sólo si hay un hallazgo reproducible; mantener la misma ruta MFT.
4. **Validación:** pruebas estáticas, Angular unit/build, .NET xUnit, Playwright y checks de autorización cuando estén disponibles. Guardar evidencias reales.
5. **Entrega:** actualizar `MEMORY_STATE.md` y `MEMORY.md` con resultado y pendientes; generar ZIP **full source** y manifest/sha256.

## Prompt corto — retomar exclusivamente pruebas (I05)

> Continúa `NAV-UX-MFT-20261027` desde el ZIP. Ejecuta las pruebas indicadas en `docs/testing/SPA_NAVIGATION_MFT_TEST_PLAN_2026-10-27.md`. Corrige únicamente fallos derivados de los cambios de navegación/MFT. Si faltan dependencias o credenciales, registra la razón y sigue con cualquier validación disponible. Mantén sin cambio los permisos y la integración bancaria. Actualiza memoria y devuelve proyecto completo.

## Prompt corto — certificar menú MFT con base de datos (I06)

> Continúa desde el ZIP vigente y con un entorno de pruebas autorizado. Ejecuta seed de navegación dos veces, inspecciona `GET api/navigation/menu` para perfiles de lectura MFT (`Config.Read` y `CanReadAch`) y usuarios sin permisos, prueba la URL y los endpoints protegidos. No crees grants de prueba permanentes ni modifiques controles IAM. Genera evidencia, memoria y ZIP full.

## Cómo interpretar estados

| Estado | Significado |
| --- | --- |
| `ANALYZED` | Evidencia revisada, sin afirmar código ejecutado |
| `IMPLEMENTED_RUNTIME_PENDING` | Fuente cambiada; build/pruebas de ejecución pendientes |
| `STATIC_PASS_RUNTIME_PENDING` | Invariantes de fuente pasan; no equivale a suite runtime |
| `NOT_RUN_ENVIRONMENT` | No se ejecutó; registrar herramienta/dependencia faltante |
| `BLOCKED` | Cambio prohibido por decisión previa o alto riesgo |
| `CLOSED` | Criterios y runtime verificados contra el artefacto exacto |

## Regla de entrega

Evitar un ZIP de sólo parches. Conservar rutas, dependencias, tests, migraciones, documentación existente, `src/`, `web/`, `database/`, `scripts/`, `tools/` y los demás módulos de fuentes. Se pueden excluir artefactos regenerables (`bin/`, `obj/`, `node_modules/`, `.git/` y reportes de cobertura) si se documentan; los archivos fuente y lockfiles **no** se excluyen. Nunca incluir secretos de entornos productivos en documentación adicional.

## Contrato nuevo y obligatorio desde I04A

Al cerrar **cada incremento**, no sólo el último: (1) generar **un ZIP completo**, (2) actualizar la memoria Markdown interna y entregar su copia externa, (3) elaborar el informe de avance Markdown interno y externo, (4) actualizar las secciones pertinentes de arquitectura y pruebas, (5) calcular SHA-256 externo y (6) exponer enlaces en la respuesta. Seguir `DELIVERY_PROTOCOL.md`. No declarar `CLOSED` cuando existan gates obligatorios sin ejecutar.

## Actualización de continuidad I05

Partir del ZIP completo `ACHInterbank_NAV_UX_MFT_2026-10-27_I05_PROYECTO_COMPLETO.zip` y su hash lateral. **No volver a partir de I04A ni repetir I01/I02**. I05 corrigió la inconsistencia `aria-expanded` en escritorio compacto y añadió un control SHA de procedencia. Ejecutar pruebas runtime disponibles y corregir solo hallazgos reproducibles; no cambiar autorizaciones MFT. Si el ZIP I05 falta, informarlo explícitamente antes de sustituirlo por una entrega anterior. Informes: `BASELINE_LINEAGE_I05.md` y `PROGRESS_REPORT_I05_2026-10-27.md`.
