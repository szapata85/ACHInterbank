# Informe de avance — ACH Interbank / Menú SPA y MFT

**Fecha de corte:** 2026-10-27  
**Programa:** `NAV-UX-MFT-20261027`  
**Último incremento:** `I04A` — formalización de entregas completas por incremento  
**Estado general:** `IMPLEMENTED_RUNTIME_PENDING` (implementación en fuentes; pruebas de ejecución no certificadas).

## Avance por incremento

| Incremento | Trabajo realizado | Estado verificable | Pendiente |
|---|---|---|---|
| `I00` | Auditoría del patrón del menú y localización de MFT | `ANALYZED` | Ninguno del diagnóstico |
| `I01` | Unificación de apertura de ramas y enlace separado para padres navegables | `IMPLEMENTED_RUNTIME_PENDING` | Angular unit, E2E y accesibilidad runtime |
| `I02` | Etiqueta MFT en seeder canónico/funcional, ruta y copy de pantalla | `IMPLEMENTED_RUNTIME_PENDING` | Seed de navegación y permisos de usuario en ambiente autorizado |
| `I03` | Ampliación de pruebas y validador de invariantes | `STATIC_PASS_RUNTIME_PENDING` | Unit/build Angular, .NET y Playwright |
| `I04` | Plan, arquitectura, guía chat, memoria y manifiesto | `IMPLEMENTED` | Aprobación y pruebas runtime |
| `I04A` | Regla de entrega: ZIP completo + documentación + memoria + avance por incremento | `DOCUMENTED_AND_PACKAGED` | Aplicar regla al cierre de I05 y siguientes |

## Evidencia y límites

- Validación **informada por el incremento I03**, sin re-ejecución de suites runtime en I04A: **16/16 comprobaciones estáticas PASS** y sintaxis/transpilación de cuatro archivos TypeScript PASS.
- Instalación Angular offline previamente bloqueada por dependencia `zone.js@0.16.3` no disponible en caché. SDK .NET no disponible en el entorno de ese incremento.
- **No se ha confirmado** la publicación efectiva del ítem MFT en el backend desplegado ni su visualización por usuarios con claims específicos.
- El incremento `I04A` **no modifica código de aplicación**. Sólo actualiza el protocolo de entrega, la documentación de continuidad y el empaquetado íntegro.
- Verificación del paquete I04A: se consigna en un resumen externo y en el archivo lateral SHA-256; estos controles **no sustituyen** build/tests funcionales.

## Estado por frente

| Frente | Completado en fuentes | Validación final |
|---|---|---|
| SPA — control de apertura consistente | Sí | Pendiente runtime |
| SPA — accesibilidad y navegación | Sí, según código | Pendiente runtime |
| MFT — opción y ruta explícitas | Sí | Pendiente seed/claims/despliegue |
| IAM/seguridad | Sin cambio intencional | Confirmar límites de permisos |
| Documentación de arquitectura y pruebas | Sí | Mantener según hallazgos |
| Regla de entregas incrementales | Registrada en `I04A` | Uso obligatorio a partir del próximo incremento |

**No se asigna un porcentaje global de finalización:** las pruebas/runtime y la comprobación de permisos faltantes son criterios de aceptación, no simples tareas accesorias.

## Próximo incremento

`I05 — Verificación de ejecución y corrección de fallos reproducibles.` Ejecutar el plan documentado con herramientas disponibles; no detener cambios permitidos por dependencias ausentes, pero no aprobar producción sin las verificaciones obligatorias. Al cerrar I05, entregar otro ZIP íntegro, memoria Markdown modificada, informe de avance y SHA-256 externo.

## Referencias de continuidad

- `docs/ai/spa-navigation-unification/MEMORY_STATE.md`
- `docs/ai/spa-navigation-unification/DELIVERY_PROTOCOL.md`
- `docs/ai/spa-navigation-unification/CHATGPT_CHAT_EXECUTION.md`
- `docs/testing/SPA_NAVIGATION_MFT_TEST_PLAN_2026-10-27.md`
