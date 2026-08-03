# Evidencia PostgreSQL

- Proveedor real: PostgreSQL en contenedor aislado, imagen fijada por digest del job CI.
- Esquema: migraciones EF Core aplicadas en base temporal exclusiva.
- Resultado focal: 1 aprobada, 0 fallidas, 0 omitidas.
- Se ejecutó la misma matriz que en SQL Server: ciclos, resultados, devoluciones, errores, reintentos, filtros, paginación y archivo exacto.
- No se observaron diferencias funcionales de fechas, montos, nulos, cadenas u orden.
- Runtime Docker aislado: PostgreSQL, API y SPA saludables; el fixture se ejecutó repetidamente y después de reiniciar sin duplicados.
- Navegación: el seed convergió a `Panel principal` y conservó `Transacciones de salida`, ruta y permisos.
- Playwright Docker: 5/5 escenarios Fase 4 y 4/4 escenarios de permisos aprobados.
- Limpieza: se eliminaron exclusivamente los contenedores, red y volumen creados para el proyecto aislado de Fase 4.

SQLite y EF InMemory no se utilizaron como sustitutos.
