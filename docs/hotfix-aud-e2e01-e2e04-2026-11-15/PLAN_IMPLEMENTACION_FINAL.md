# PLAN DE IMPLEMENTACIÓN — AUD-E2E-01..04 (2026-11-15)

**Estado:** implementación de código y scripts completa, aceptación técnica pendiente; NO-GO. No iniciar UAT.

| Puerta | Actividad | Resultado actual |
|---|---|---|
| H0 | Aplicar patch P0/P1 en fuente, migraciones PostgreSQL/MSSQL, snapshots y nuevos xUnit | Realizado; no hay compilación disponible |
| G1 | `dotnet restore`, `build`, pruebas xUnit focales y regresión completa | Pendiente; falta SDK .NET 10 |
| G2 | BD PostgreSQL/SQL Server: preflight duplicados, migración y rollback en ensayo, 2+ nodos, carreras exactas | Pendiente; sin bases |
| G3 | Mock de SOAP: caída antes de intent, después de intent, durante timeout, después de respuesta; reinicio Quartz; ciclo cerrado/día siguiente | Pendiente; sin ambiente de integración |
| G4 | Conciliación/observabilidad, retención y protección de XML sensible, aprobación técnica | Pendiente |
| UAT | Solo fase final del proyecto | No ejecutada por instrucción |

**Condición de aceptación:** 1 instrucción originadora/clave externa → máximo 1 registro; toda intención SOAP durable antes del envío; ninguna retransmisión automática de outcome desconocido; ningún retry probado pre-send queda permanentemente dependiente del ciclo cerrado si existe un ciclo futuro permitido. Probar también que no cambia la fecha/estado monetario original cuando el retry se reasigna a un ciclo posterior.
