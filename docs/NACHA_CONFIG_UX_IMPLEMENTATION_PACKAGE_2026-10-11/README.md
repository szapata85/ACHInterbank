# Paquete de implementación — NACHA Config UX

Este paquete contiene los documentos canónicos para implementar la experiencia operativa de parametrización NACHA-M sin desviar el alcance.

## Archivos

1. `NACHA_CONFIG_UX_START_HERE.md` — contrato de ejecución para agentes/modelos.
2. `NACHA_CONFIG_UX_STATE.md` — estado/memoria de la iniciativa.
3. `NACHA_CONFIG_UX_ARCHITECTURE.md` — arquitectura objetivo y límites.
4. `NACHA_CONFIG_UX_IMPLEMENTATION_PLAN.md` — fases UX0–UX6, entregables y pruebas.
5. `NACHA_CONFIG_UX_MEMORY_PATCH.md` — bloque sugerido para incorporar a `MEMORY.md` cuando se inicie formalmente el programa.

La regla principal del paquete es: **mejorar la experiencia de administración sin reescribir el motor NACHA-M table-driven y sin introducir catálogos funcionales fijos en Angular.**

## Estado final

`NACHA-CONFIG-UX-001` está **CLOSED** con `UX6 (CLOSED)` y `NEXT_ALLOWED_PHASE=NONE`. El cierre endurece accesibilidad, consistencia Material, branding dinámico, navegación, estados, mensajes de error, responsividad, protección de `PUBLICADO`, publicación fail-closed, aislamiento ACH/CENIT y guardrails, sin modificar el motor NACHA-M, goldens, snapshots oficiales ni migraciones.

La evidencia ejecutable que requiere `node_modules` o .NET queda registrada como `NOT_RUN_ENVIRONMENT`, no como PASS. El detalle canónico está en `NACHA_CONFIG_UX_STATE.md`.
