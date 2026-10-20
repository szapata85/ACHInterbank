# START HERE — NAV-MENU-IA-001

Programa incremental para reorganizar y humanizar la navegación del SPA de ACHInterbank.

## Objetivo

Entregar un menú comprensible para usuarios no técnicos, en español claro, sin spanglish innecesario, multi-cámara y gobernado por seed.

## Reglas no negociables

1. El **seed/backend es la fuente de verdad** de `label`, jerarquía, orden, permisos y visibilidad.
2. No usar Git. Cada fase parte del ZIP full anterior y entrega un ZIP full nuevo.
3. Ejecutar **una sola fase por chat**.
4. Usar prompts cortos. No pedir a ChatGPT que implemente varias fases a la vez.
5. Al terminar cada fase, actualizar `docs/ai/navigation-menu/MEMORY_STATE.md` y una entrada durable breve en `MEMORY.md`.
6. No borrar rutas funcionales en las primeras fases; ocultar/reubicar antes de retirar.
7. CENIT no se presenta como silo de navegación. Neteo y optimización quedan fuera del menú por ahora.
8. `Conexión entidad participante` es el copy aprobado para la conexión con la entidad.
9. `Herramientas de prueba` se siembra siempre; su visibilidad depende de autorización, no del ambiente.
10. No cambiar reglas ACH/CENIT/NACHA-M por este programa salvo que una fase lo requiera explícitamente.

## Orden de lectura por chat

1. `AGENTS.md`
2. `MEMORY.md` — buscar `NAV-MENU-IA-001`
3. `docs/ai/navigation-menu/MEMORY_STATE.md`
4. `docs/architecture/NAVIGATION_MENU_INFORMATION_ARCHITECTURE.md`
5. Archivo de la fase activa en `docs/ai/navigation-menu/phases/`

## Fases

| Prioridad | Fase | Resultado |
|---:|---|---|
| 1 | NAV1 — Baseline y guardas | Inventario y pruebas de caracterización sin cambio funcional |
| 2 | NAV2 — Seed canónico | Árbol, padres y órdenes controlados por seed |
| 3 | NAV3 — Seguridad técnica | Herramientas de prueba visibles sólo por autorización dedicada |
| 4 | NAV4 — SPA y textos | El SPA deja de sobrescribir el seed y se humanizan títulos/breadcrumbs |
| 5 | NAV5 — Cobertura de pantallas | Se publican rutas faltantes y se clasifican rutas contextuales/técnicas |
| 6 | NAV6 — Recertificación | Validación integral, idempotencia del seed y ZIP full final |

## Primer prompt

Usar `docs/ai/navigation-menu/MASTER_PROMPT_CHATGPT.md`. No avanzar automáticamente a NAV2.


## NAV6 — Recertificación final

Estado tras NAV6 (2026-10-15):

- `NAV6 = IMPLEMENTED_UNVERIFIED_ENVIRONMENT`.
- `NAV-MENU-IA-001 = IMPLEMENTED_UNVERIFIED_ENVIRONMENT`.
- Implementación completa; recertificación runtime pendiente por ambiente.
- 142 rutas; cobertura MENU 70/70; 0 gaps bloqueantes.
- Residual `/nacha-security/certificates/:id/versions` = `CONTEXTUAL_INTENTIONAL`, no bloqueante.
- Angular y .NET = `NOT_RUN_ENVIRONMENT` por ausencia de dependencias/toolchain, sin instalar herramientas.
- Conclusión: **COMPLETO PENDIENTE RUNTIME**.
- `NAV3.5C` no fue ejecutado; no ejecutar fases posteriores automáticamente. Esperar decisión humana.
