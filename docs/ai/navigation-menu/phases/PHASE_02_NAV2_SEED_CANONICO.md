# NAV2 — Seed canónico

## Objetivo
Hacer que el seed produzca el árbol objetivo de forma idempotente.

## Implementar
- reconciliador final de IA de navegación;
- padres/labels/iconos/órdenes según matriz;
- órdenes 10/20/30;
- nuevos ítems funcionales no técnicos que ya tengan ruta;
- desactivación segura de duplicados.

## No hacer
No resolver aún roles técnicos ni filtro Production. No humanizar todos los títulos internos del SPA.

## Pruebas
Seed 2 veces; árbol idéntico. Admin/Operator sin regresión.

## Gate
`NAV2=CLOSED` o `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`, con next_allowed_phase NAV3 sólo si no hay defecto conocido.
