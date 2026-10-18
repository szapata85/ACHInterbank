# Estrategia de seed de navegación — NAV-MENU-IA-001

## Estado actual relevante

- `DbInitializer.SeedAllAsync()` obtiene todos los `IDbSeeder` y los ejecuta por `Order`.
- Existen seeders funcionales independientes que crean/mueven entradas de navegación.
- `MenuItemConfiguration.HasData()` contiene el baseline histórico.
- Hay colisiones de `Order` y varios seeders vuelven a asignar padre/label/order.

## Estrategia objetivo

Mantener los seeders funcionales, pero añadir un **reconciliador final** de arquitectura de información que se ejecute después de ellos.

Responsabilidades del reconciliador:

1. Upsert por `Route` canónica.
2. Crear agrupadores faltantes.
3. Asignar `ParentId` por ruta del padre.
4. Aplicar label humanizado.
5. Aplicar icono y `Exact`.
6. Aplicar orden 10/20/30.
7. Desactivar duplicados de navegación conocidos.
8. Conservar permisos/roles salvo cambios explícitos de NAV3.
9. Ser idempotente.

## No hacer

- No depender de cambios manuales en `MenuItems`.
- No usar migración de esquema para un simple reordenamiento.
- No borrar rutas SPA sólo porque no estén en menú.
- No dejar que cada seeder funcional vuelva a imponer la IA final después del reconciliador.
- No usar un `if Production` para decidir si existe un ítem de menú.

## Orden

Usar un `Order` claramente posterior a los seeders actuales de navegación, por ejemplo `100`, después de verificar que no existe otro reconciliador posterior.

## Idempotencia mínima

Ejecutar dos veces debe producir exactamente el mismo conjunto de rutas activas, padres, labels, órdenes, roles y permisos.

## Padres con hijos

El `MainLayoutComponent` actual convierte un nodo con hijos en botón de expansión y no navega a su `route`. Por ello, cuando un padre también sea landing funcional, incluir un primer hijo que apunte a la landing o resolver el patrón de navegación en NAV4. No crear rutas falsas que terminen navegables si los hijos son filtrados por permisos.
