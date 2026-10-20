# NAV6 — Recertificación y ZIP final

## Objetivo
Cerrar el programa sobre un artefacto exacto.

## Validar
- seed limpio y repetido;
- permisos/roles;
- acceso directo por URL;
- menú desktop/móvil;
- rutas y copy;
- backend build/tests;
- SPA build/tests/E2E cuando el ambiente lo permita.

## Correcciones permitidas
Sólo defectos reproducibles de NAV-MENU-IA-001. No iniciar otro refactor.

## Cierre
Actualizar MEMORY_STATE + MEMORY, generar manifest SHA-256 y ZIP full final. `CLOSED` sólo con gates ejecutados; si faltan toolchains usar `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`.


## Ejecución final — 2026-10-15

- Estado: `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`.
- Fuente: `ACHInterbank-R34-NAV-IA-NAV5-FULL-PROJECT.zip` (`c11268bb2e5509abf6bbe39f4d2e0074714b1492674ed7d8b2b40183331ec115`).
- Cobertura: 142 rutas, MENU 70/70, DEAD 0, UNKNOWN 0, gaps bloqueantes 0.
- Residual `/nacha-security/certificates/:id/versions`: `CONTEXTUAL_INTENTIONAL`, no bloqueante; no se promovió a menú.
- NAV4 drift = 0 mediante replay de snapshots/deltas aprobados. NAV3 permanece intacto; NAV3.5C sigue `BLOCKED / NO EJECUTAR` y no se ejecutó.
- Cambios productivos NAV6: 0.
- Angular=`NOT_RUN_ENVIRONMENT` (`node_modules` ausente); .NET=`NOT_RUN_ENVIRONMENT` (`dotnet` ausente).
- **IMPLEMENTACIÓN NAV-MENU-IA-001 COMPLETA** / **RECERTIFICACIÓN RUNTIME PENDIENTE POR AMBIENTE**.
- Conclusión: `COMPLETO PENDIENTE RUNTIME`; no usar `CLOSED`/`VERIFIED` hasta ejecutar los gates runtime sobre el artefacto exacto.
- Evidencia final: `NAV6_FINAL_RECERTIFICATION.md`, `NAV6_INPUT_TREE_MANIFEST.json`, `scripts/validate-nav6-final-recertification.py` y `artifacts/nav6/`.
