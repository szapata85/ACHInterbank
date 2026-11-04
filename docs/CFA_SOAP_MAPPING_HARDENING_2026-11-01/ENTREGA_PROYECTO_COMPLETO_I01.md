# SOAP-MAP-SAFE-001 — Entrega integrada del proyecto completo (I01)

**Fecha:** 2026-11-01  
**Estado:** `IMPLEMENTED_UNVERIFIED_RUNTIME`  
**Siguiente incremento:** `I02` — semántica de campos `Proc_Contrapartidas`  
**UAT:** `NOT_STARTED_FINAL_ONLY`  
**Monetario Live / cutover:** `NOT_AUTHORIZED`

## Qué contiene el ZIP

Esta es una **distribución íntegra del repositorio ACHInterbank**, no un parche aislado. Al extraerlo, se obtiene `ACHInterbank_ajustado/` con:

- `ACHInterbank.sln` y los **7 proyectos .NET** (incluidas las migraciones SQL Server).
- Backend, frontend Angular, pruebas, scripts, configuraciones, perfiles y archivos ya presentes en el proyecto base.
- **I01 aplicado directamente** a los cinco archivos C# respectivos, sin que el receptor tenga que aplicar un diff ni copiar archivos.
- Toda la documentación Markdown existente del proyecto, junto con `PLAN_INCREMENTAL.md`, `MEMORY_STATE.md` y este documento de entrega.
- `AGENTS.md`, `MEMORY.md` y `README.md` como puntos de entrada de continuidad.

La fuente es `ACHInterbank_NAV_UX_MFT_2026-10-27_I06_PROYECTO_COMPLETO.zip` (SHA-256 `6621b649b7f00dde68e1ba410c57aea945ab24bae5caeec4818e666b39ba058d`), a la cual se integró el contenido de `ACHInterbank_SOAP_MAP_SAFE_001_I01_PATCH.zip` (SHA-256 `94aa16999ebd6906a30f90f732117fe772d0c776b74a501bd0d68dca7dd49b67`) y la documentación incremental. Ningún archivo del ZIP base fue omitido.

## Alcance exacto de I01

Código modificado e integrado:

1. `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ContrapartidaDispatchJobService.cs` — envío individual, correlación de resultado y protección contra reenvío incierto.
2. `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ProcContrapartidasRequestMapper.cs` — guarda de cardinalidad.
3. `src/Cfa.ACHInterbank.Persistence/Integrations/Services/ProcContrapartidasFunctionalMappingResolver.cs` — un único registro de entrada.
4. `tests/Cfa.ACHInterbank.Tests/ContrapartidaDispatchJobServiceTests.cs` — casos de dos solicitudes independientes, resultados diferentes y reintentos.
5. `tests/Cfa.ACHInterbank.Tests/ProcContrapartidasFallbackClosureTests.cs` — guardas en ambas rutas de mapping.

**Sin cambios** al seed SOAP, formato NACHA-M, contratos WSDL, migraciones EF, frontend, configuración DryRun/Live o `UseContractDrivenRequest`. Los incrementos I02–I07 **no están ejecutados**.

## Evidencia en la preparación del ZIP

- El ZIP base pasó su verificación CRC (`testzip()`: sin errores).
- Se conservaron los **4.484 archivos originales** del ZIP fuente.
- Los cinco archivos C# integrados son byte a byte idénticos a los cinco archivos suministrados en el parche I01.
- `python scripts/validate-solution-projects.py`: `SOLUTION GRAPH: OK`, 7 proyectos en solución/disco, todas las referencias resueltas.
- `python scripts/validate-spa-navigation-mft-unification.py`: 23 comprobaciones estáticas OK (sin demostrar Angular en runtime).
- No existe `dotnet` en el ambiente de integración: compilación y pruebas unitarias `NOT_RUN_ENVIRONMENT`. No se indica `PASS` para ellas.
- No se ejecutó UAT, ni conexión al banco, ni transacción monetaria Live.

## Cómo abrir y seguir

1. Extraer el **ZIP completo** y abrir la carpeta `ACHInterbank_ajustado/` (no copiar el parche sobre otra versión).
2. Leer `AGENTS.md`, después `MEMORY.md` y `docs/CFA_SOAP_MAPPING_HARDENING_2026-11-01/MEMORY_STATE.md`.
3. Con el SDK .NET indicado en `global.json`, desde la raíz ejecutar:

   ```cmd
   python scripts/validate-solution-projects.py
   dotnet build ACHInterbank.sln -c Release
   dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~ContrapartidaDispatchJobServiceTests|FullyQualifiedName~ProcContrapartidasFallbackClosureTests"
   ```

4. Si alguna herramienta o ambiente bloquea las pruebas, registrar `NOT_RUN_ENVIRONMENT` y continuar el desarrollo independiente sin inventar éxito técnico. Mantener el gate de Live cerrado.
5. El **siguiente desarrollo** es exclusivamente `I02` según `PLAN_INCREMENTAL.md`. UAT y homologación bancaria se reservan para I07.

## Regla de integración para futuros ZIP

Cada incremento nuevo debe publicarse como **un único ZIP con la solución completa ya integrada y la documentación Markdown actualizada**. Un diff/parche puede adjuntarse como evidencia opcional, pero nunca sustituir el proyecto completo.
