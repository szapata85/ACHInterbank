# I02 — Decisiones de semántica para `Proc_Contrapartidas`

**Programa:** `SOAP-MAP-SAFE-001`  
**Fecha:** 2026-11-01  
**Fuente comprobada:** seed de `IntegrationMappingBootstrapper`, contrato técnico de `IntegrationCatalogService`/`IntegrationCatalogBootstrapper`, entidades de dominio, resolver y pruebas del repositorio integrado I01.  
**Alcance:** exclusivamente I02. **No equivale a homologación del banco**. WSDL original sin XSD importados completos.

## Decisiones campo a campo

| SOAP | Valor actual desde el seed | Estado I02 | Decisión / evidencia pendiente |
|---|---|---|---|
| `OFEMP` | `ClearingHouses.Code`, vía `clearinghouse.code` | `BLOCKED_FUNCTIONAL` | El catálogo pide **código empresa/origen**; no hay prueba de que el código de cámara equivalga al código empresarial del banco. Se conserva para no inventar otro. Confirmar entidad y código autorizado. |
| `OFIDARCH` | `AchTransactions.AchBatchId` → `AchBatches.Id` (`batch.id`) | `BLOCKED_FUNCTIONAL` | Identificador **interno de lote**, no se puede afirmar que sea el ID del **archivo de envío**. Correlacionar con ID de archivo bancario antes de sustituir. |
| `OFIDLOT` | `AchTransactions.AchBatchId` → `AchBatches.Id` (`batch.id`) | `BLOCKED_FUNCTIONAL` | Es un PK interno, no se puede equiparar sin evidencia a `BatchSequenceNumber` ni al lote bancario. Mantener el valor anterior. |
| `OFST` | constante `"OO"` | `BLOCKED_FUNCTIONAL` | La constante era aceptada en readiness, pero falta catálogo funcional del **estado origen**. No cambiar por un texto/estado supuesto. |
| `OFDIRECCIONIP` | constante `"0.0.0.0"` | `BLOCKED_FUNCTIONAL` | IP técnica de reserva, no IP de origen rastreable; readiness ya advierte. No reemplazar por IP de servidor, proxy o usuario inventada. |
| `OFDD` | `AchTransactions.Type` → `D` / `C` | **Guardada en I02** | La ruta `Proc_Contrapartidas` del proyecto está destinada al débito CFA. El resolver ahora **rechaza** créditos, prenotificaciones, y todo mapping que resuelva un indicador distinto de `D`. |
| `OFMONDEB` | `AchTransactions.Amount` | **Guardada en I02** | Requiere `Amount > 0` y `OFMONDEB == Amount` exacto; no se permite un mapping que cambie el monto. |
| `OFMONCRE` | constante `0` | **Guardada en I02** | La ruta de débito debe resolver **exactamente 0** para monto crédito. No se permite envío de ambos importes. |

## Decisión técnica mínima

- `ProcContrapartidasFunctionalMappingResolver` verifica primero que la entidad corresponda a un débito monetario positivo, no una prenotificación.
- Después de resolver las reglas publicadas y convertir valores, valida en conjunto `OFDD`, `OFMONDEB`, `OFMONCRE` contra la transacción original; falla controladamente antes de construir el SOAP.
- Errores explícitos: `PROC_CONTRAPARTIDAS_DEBIT_ONLY` y `PROC_CONTRAPARTIDAS_MONETARY_MISMATCH`.
- No se agregaron tablas, migraciones, endpoints, capas ni reglas nuevas de seed. `UseContractDrivenRequest` conserva `false`. El flujo contract-driven, si en el futuro se habilita, pasa primero por el mismo resolver funcional.
- Se mantuvieron deliberadamente sin cambio `OFEMP`, `OFIDARCH`, `OFIDLOT`, `OFST` e `OFDIRECCIONIP`. **La operación NO está homologada para Live** por esos cinco pendientes.

## Pruebas y evidencia

- `IntegrationMappingEndToEndTests`: regresión del valor de los ocho campos del seed (solo caracterización, no aprobación bancaria); rechazo de crédito, prenotificación y monto no positivo; rechazo de reglas publicadas que cambian `OFDD`, `OFMONDEB` u `OFMONCRE`.
- `python scripts/validate-solution-projects.py`: `SOLUTION GRAPH: OK` (7 proyectos).
- Compilación y unit tests: `NOT_RUN_ENVIRONMENT` porque falta SDK `dotnet` en el contenedor. No etiquetar PASS los tests escritos.
- Comando a ejecutar cuando haya .NET: `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~IntegrationMappingEndToEndTests"`.
- Sin SQL externo, llamadas SOAP, Live, UAT ni homologación bancaria.

## Pendientes funcionales para homologación final I07

Solicitar al responsable del core bancario: definición/catálogo de empresa (`OFEMP`), origen y semántica de ID de archivo (`OFIDARCH`), identificador de lote (`OFIDLOT`), códigos válidos de estado (`OFST`), y procedencia autorizada de IP (`OFDIRECCIONIP`). No implementar fuentes alternativas hasta disponer de evidencia; continuar I03 como incremento independiente. La deuda de pruebas .NET se recoge en I06.
