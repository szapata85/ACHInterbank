# Evidencia — I03 ACH Colombia

**Fuente de verificación:** artefactos compilables y scripts incluidos en el ZIP acumulativo I03.

## Ejecutado en esta sesión

- `python scripts/validate-solution-projects.py`: `SOLUTION GRAPH: OK` (7 de 7).
- `python scripts/test/automatic_returns_i02_ach_static_check.py`: `18/18 PASS_STATIC` (invariantes históricos I02, sin sustituir xUnit).
- `python scripts/test/automatic_returns_i03_ach_static_check.py`: `25/25 PASS_STATIC` (modelo, contexto, migración, dos índices únicos, aislamiento, 4 ciclos, 7 ciclos, contrato con el detector y ausencia de generación/envío).
- Comprobación de migración/snapshot SQL Server por inspección de código; **no se ejecutó sobre motor real**.

## No ejecutado

- `dotnet build`, `dotnet test`, EF migrations SQL Server, integración con `Proc_Transacciones`, concurrencia multiinstancia, pruebas de rendimiento, pruebas de usuario, Angular.
- La prueba xUnit `SqliteUniqueIndexBlocksConcurrentDuplicateAtDatabaseLevel` fue **redactada, no ejecutada**. No implica homologación contra SQL Server.
- No existe evidencia de despliegue, envío real, compensación ni conciliación.

## Condiciones de cierre posterior

1. Compilar solución y ejecutar xUnit I01, I02, I03 sin regresiones.
2. Aplicar/retroceder migración SQL Server en ambiente de pruebas sin afectar otras tablas.
3. Ejecutar dos procesos simultáneos sobre el mismo `OriginalTransactionId`: máximo una fila persistida.
4. Verificar modo `DESACTIVADO` sin candidatas, `CON_APROBACION` y `AUTOMATICO` con reserva **sin envío**.
5. Contrastar cada ventana con el calendario homologado ACH Colombia (cinco ciclos históricos y siete ciclos confirmados, incluyendo cruce de medianoche).
6. Validar que no existe movimiento de recursos ni generación NACHA-M y que CENIT queda sin cambios funcionales.

**Decisión:** implementación estática completa; `GO_INCREMENT=YES`; `GO_DEPLOY=NO`; `GO_LIVE_MONETARY=NO`.
