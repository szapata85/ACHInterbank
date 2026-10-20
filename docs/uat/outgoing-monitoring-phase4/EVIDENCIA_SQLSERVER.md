# Evidencia SQL Server

- Proveedor real: SQL Server en contenedor aislado, imagen fijada por digest del job CI.
- Esquema: migraciones EF Core aplicadas en base temporal exclusiva.
- Resultado focal: 1 aprobada, 0 fallidas, 0 omitidas.
- Matriz: 34 salidas determinadas `UAT-F4-MON-SAL-`, un histórico no determinado excluido, paginación estable y archivo exacto.
- Runtime Docker principal: API `:843`, SPA `:743`, SQL Server saludable; fixture ejecutado tres veces sin duplicados, incluida una ejecución posterior al reinicio.
- Persistencia: conservada después del reinicio controlado de API y SPA; 34 salidas visibles y una raíz de reintento con dos intentos.
- Navegación: `Panel principal` y `Transacciones de salida` persistieron en español, sin duplicar opciones.
- Playwright Docker: 5/5 escenarios Fase 4 y 4/4 escenarios de permisos aprobados.

No se imprimieron cadenas de conexión ni datos sensibles.
