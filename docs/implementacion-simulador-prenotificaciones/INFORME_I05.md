# Informe técnico I05 — Simulación de aceptación tácita

**Corte:** 2026-11-02. **Base:** I04. **Resultado:** código incorporado, verificación estática superada; compilación y runtime pendientes.

## Cambios aplicados

- Contrato de solicitud y respuesta de preview virtual y aplicación real; interfaz del servicio extendida.
- Dos rutas API autorizadas para `preview` (solo lectura) y `apply-due` (solo vencimiento real, UAT/local).
- Servicio parcial `NachaInboundSimulationService.TacitAcceptance.cs` con reglas conservadoras de origen, cámara, banco, ciclo, fecha, transmisión, intento activo y correlación; aplica política de aceptación tácita solo cuando la regla efectiva la habilita, bloquea devoluciones generadas y responde motivos trazables.
- `PrenotificationTacitAcceptanceDecision.IsDue` como política pura de frontera temporal.
- La hora del job `AchTacitAcceptanceJobHandler` proviene de `TimeProvider` inyectable; se conservan su búsqueda canónica y fallback legacy y no se cambia la consulta del SLA.
- Los cambios de estado se delegan a `AchStateTransitionService`; una transacción `SERIALIZABLE` rodea el lote, con idempotencia derivada de ID original/ID de intento.
- No se genera un archivo NACHA-M de aceptación, no hay transporte externo y no se altera reloj global.
- Pruebas C#: política de borde temporal, ausencia de plazo, política deshabilitada, devolución generada, forma de contrato, previsualización con reloj virtual y control fail-closed para la aplicación.

## Evidencia obtenida

- `python scripts/validate-solution-projects.py` → `SOLUTION GRAPH: OK` (7/7).
- I01 20/20, I02 26/26, I03 31/31, I04 31/31, I05 36/36 `PASS_STATIC` (144/144 acumuladas).
- `dotnet --version` → `command not found`: `NO_EJECUTADA_POR_AMBIENTE` para build, Xunit, aplicación de BD, Quartz y E2E.
- La validación estática **no** acredita compilación, ejecución de migraciones, sincronización real ni manejo de carreras.

## Pendientes específicos y riesgos conocidos

1. Ejecutar `dotnet build ACHInterbank.sln -c Release` y `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter 'FullyQualifiedName~PrenotificationSimulatorTacitAcceptanceI05'`.
2. Certificar en SQL Server/PostgreSQL concurrencia entre Quartz y el endpoint, rollback de lote y estrategia de reintentos EF ante transacciones manuales.
3. Verificar que la sincronización de intento y tercero canónico queda coherente tras transición; prueba before/after, feriados y ventanas de ambas cámaras.
4. En I06 conectar la SPA sin producir mutaciones al cambiar la hora virtual; incluir confirmación explícita antes de `apply-due`.
5. I07: pruebas E2E y no regresión, homologación por operador; el código sigue **no certificado**.

## Archivos nuevos/modificados

Se incluye `MANIFIESTO_I05.json` y `I05_CAMBIOS_CODIGO.patch`, además de `MEMORY_STATE_I05.md`, evidencia y estado actualizado.
