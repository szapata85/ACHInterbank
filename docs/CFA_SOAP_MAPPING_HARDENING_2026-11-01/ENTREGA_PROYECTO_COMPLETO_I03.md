# Entrega integrada — SOAP-MAP-SAFE-001 / I03

**Archivo:** `ACHInterbank_PROYECTO_COMPLETO_SOAP_MAP_SAFE_001_I03_INTEGRADO_2026-11-01.zip`  
**Base:** `ACHInterbank_PROYECTO_COMPLETO_SOAP_MAP_SAFE_001_I02_INTEGRADO_2026-11-01.zip`  
**Contenido:** código completo + web + scripts + configuración + pruebas + documentación Markdown. No es un parche.

## Código I03 integrado

1. `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ProcTransaccionesFunctionalSourceResolver.cs` — corrección de coherencia NACHA/DB para crédito monetario (monto, cuenta receptora, código y trace).
2. `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaPostProcessingPipeline.cs` — veto Live al campo `NCTAORIG` no homologado, antes de transmitir SOAP.
3. `tests/Cfa.ACHInterbank.Tests/NachaDesagregadoIntegrationMappingTests.cs` — cuatro pruebas negativas, sin cambiar los casos preexistentes.
4. `tests/Cfa.ACHInterbank.Tests/ProcTransaccionesOriginAccountSafetyTests.cs` — cuatro pruebas sobre la protección del XML Live.

## Markdown agregado o actualizado

- `DECISIONES_I03_PROC_TRANSACCIONES.md` (matriz campo a campo y pendientes).
- `MEMORY_STATE.md`, `PLAN_INCREMENTAL.md` y este documento.
- `README.md`, `MEMORY.md` en raíz.

## Alcance y restricciones

- I01 e I02 permanecen integrados; no se eliminó ningún archivo del ZIP I02.
- No se cambió seed, base de datos, migraciones, contratos WSDL, parser NACHA-M, frontend ni flags Live.
- Pruebas .NET y compilación: `NOT_RUN_ENVIRONMENT` (sin SDK). Se validó estructura de 7 proyectos y la integridad ZIP. **No certificar comportamiento ejecutable**, UAT ni Live.
- `NCTAORIG` continúa sin origen independiente homologado. El modo efectivo Live lo rechaza cuando es no vacío; DryRun sigue permitiendo inspeccionar el payload para diagnóstico.

## Tabla de estado de incrementos

| Incremento | Estado al entregar I03 | Siguiente actividad |
|---|---|---|
| I01 | `IMPLEMENTED / NOT_RUN_ENVIRONMENT` | Ejecutar tests focales con .NET en I06 |
| I02 | `IMPLEMENTED / NOT_RUN_ENVIRONMENT / BLOCKED_FUNCTIONAL` | Homologar 5 campos con banco en I07 |
| I03 | `IMPLEMENTED / NOT_RUN_ENVIRONMENT / BLOCKED_FUNCTIONAL` | Certificar tests y cuenta origen en I06–I07 |
| I04 | `TODO` | **Siguiente incremento: Respuesta Axon** |
| I05 | `TODO` | Paridad legacy/contract-driven sin cutover |
| I06 | `TODO` | Compilación y pruebas técnicas diferidas |
| I07 | `TODO` | UAT y homologación final |
