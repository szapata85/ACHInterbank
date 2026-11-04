# Entrega integrada — SOAP-MAP-SAFE-001 / I02

**Base:** ZIP completo `ACHInterbank_PROYECTO_COMPLETO_SOAP_MAP_SAFE_001_I01_INTEGRADO_2026-11-01.zip`.  
**Salida:** ZIP completo con I01 + I02, preservando el árbol de solución original. **No aplicar parches por separado.**

## Código modificado en I02

1. `src/Cfa.ACHInterbank.Persistence/Integrations/Services/ProcContrapartidasFunctionalMappingResolver.cs`: guardas de semántica monetaria y de débito antes de emitir SOAP.
2. `tests/Cfa.ACHInterbank.Tests/IntegrationMappingEndToEndTests.cs`: tests negativos y de regresión del seed, adaptando fixture de mapping dinámico a valores monetarios válidos.

## Documentos actualizados / agregados

- `docs/CFA_SOAP_MAPPING_HARDENING_2026-11-01/DECISIONES_I02_CONTRAPARTIDAS.md`: tabla de decisiones y pendientes de homologación.
- `docs/CFA_SOAP_MAPPING_HARDENING_2026-11-01/MEMORY_STATE.md`: bitácora, resultados y `next_increment=I03`.
- `docs/CFA_SOAP_MAPPING_HARDENING_2026-11-01/PLAN_INCREMENTAL.md`: encabezado de estado del programa.
- `README.md` y `MEMORY.md`: referencia breve a la continuidad I02.

## Evidencia y restricciones

- Control estático del grafo de solución: **PASS — 7 proyectos**.
- Compilación y tests .NET: **NOT_RUN_ENVIRONMENT** (no existe `dotnet` instalado). Por tanto: código **IMPLEMENTED_UNVERIFIED_RUNTIME**, no `VERIFIED_LOCAL`.
- Las cinco dudas funcionales abiertas están registradas; el seed permanece intacto. Ninguna decisión representa conformidad del banco.
- Sin cambios NACHA-M, migraciones, configuración Live ni UAT. **No autorizar movimientos monetarios reales**.
- `last_completed_increment: I02`; **siguiente: I03**. Ejecutar solo I03 en la próxima sesión.
