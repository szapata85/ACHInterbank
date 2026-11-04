# Entrega integral — SOAP-MAP-SAFE-001 / I04

**Entrega:** `ACHInterbank_PROYECTO_COMPLETO_SOAP_MAP_SAFE_001_I04_INTEGRADO_2026-11-01.zip`  
**Base:** ZIP completo I03 de 2026-11-01. **Incluye:** todo el código, SPA, scripts, pruebas, configuraciones, migraciones y Markdown más los incrementos I01–I04. **No aplicar patches.**

## Archivos de código cambiados en I04

1. `src/Cfa.ACHInterbank.Application/ACH/Responses/Notification/Services/NotificarRespuestaAchUseCase.cs` — validación del comando antes de SOAP; excepción luego de gateway ⇒ conciliación manual, no reintento ciego.
2. `tests/Cfa.ACHInterbank.Tests/NotificarRespuestaAchUseCaseTests.cs` — prueba los siete campos, acuse persistido, validación previa, resultado incierto y duplicados.

## Documentación

- `DECISIONES_I04_RESPUESTA_AXON.md` — matriz campo a campo, estados y deudas.
- `MEMORY_STATE.md` — `last_completed_increment=I04`, `next_increment=I05`.
- `PLAN_INCREMENTAL.md`, `README.md`, `MEMORY.md`, este archivo.

## Validación

- Grafo de solución: 7/7 `PASS` (estático).
- Compilación y pruebas .NET: `NOT_RUN_ENVIRONMENT` (SDK ausente).
- UAT: `NOT_STARTED_FINAL_ONLY`. Real Live: `NOT_AUTHORIZED`.
- Sin migraciones, nueva cola o modificación de WSDL, seed, NACHA, SPA, scheduler ni configuración Live.

## Estado de todos los incrementos

| ID | Estado | Restricción |
| --- | --- | --- |
| I01 | `IMPLEMENTED / NOT_RUN_ENVIRONMENT` | Runtime / pruebas pendientes |
| I02 | `IMPLEMENTED / NOT_RUN_ENVIRONMENT / BLOCKED_FUNCTIONAL` | 5 campos por homologar |
| I03 | `IMPLEMENTED / NOT_RUN_ENVIRONMENT / BLOCKED_FUNCTIONAL` | Cuenta origen no homologada |
| I04 | `IMPLEMENTED / NOT_RUN_ENVIRONMENT` | Ejecutar pruebas / concurrencia cuando exista SDK y BD |
| I05 | `TODO` **siguiente** | Paridad legacy/contract-driven |
| I06 | `TODO` | Cierre técnico; pruebas diferidas |
| I07 | `TODO` | UAT/homologación al final |
