# Prompts cortos por fase — NAV-MENU-IA-001

Copiar sólo el bloque de la fase activa.

## NAV1
```text
Ejecuta NAV1 solamente. Caracteriza menú, rutas, seed, permisos y pruebas actuales. No cambies comportamiento. Actualiza MEMORY_STATE.md y entrega ZIP full.
```

## NAV2
```text
Ejecuta NAV2 solamente. Implementa la arquitectura canónica del menú por seed, con órdenes 10/20/30 y nombres aprobados. No toques autorización técnica todavía. Actualiza MEMORY_STATE.md y entrega ZIP full.
```

## NAV3
```text
Ejecuta NAV3 solamente. Implementa permisos/roles de Herramientas de prueba y elimina su dependencia del ambiente. No amplíes acceso a usuarios normales. Actualiza MEMORY_STATE.md y entrega ZIP full.
```

## NAV4
```text
Ejecuta NAV4 solamente. Haz que el SPA respete el menú del backend y humaniza títulos/breadcrumbs aprobados. Quita overrides CFA/MFT/SOAP/UAT visibles. Actualiza MEMORY_STATE.md y entrega ZIP full.
```

## NAV5
```text
Ejecuta NAV5 solamente. Publica las pantallas faltantes según la matriz y deja contextuales/técnicas fuera del menú. No borres rutas de compatibilidad. Actualiza MEMORY_STATE.md y entrega ZIP full.
```

## NAV6
```text
Ejecuta NAV6 solamente. Recertifica seed, roles, rutas, SPA, idempotencia y cobertura. Corrige sólo defectos del programa. Actualiza MEMORY_STATE.md y entrega ZIP full final.
```

## Prompt de reanudación
```text
Lee MEMORY_STATE.md. Reanuda sólo la fase activa desde last_completed_step. No repitas pasos cerrados ni avances de fase.
```

## Prompt de validación sin cambios
```text
Valida la fase activa contra sus criterios de aceptación. No modifiques código salvo defecto reproducible. Registra evidencia en MEMORY_STATE.md.
```
