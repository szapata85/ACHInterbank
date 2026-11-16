# Resultado técnico focal — RES-H02-01 / RES-H02-02 (2026-11-15)

**Única línea base de fuente:** `ACHInterbank_PROYECTO_COMPLETO_HOTFIX_FOCAL_H01_H02_2026-11-15.zip` (SHA-256 `0c9662d1e44a8b68e7760ebe5c23c04b70b835acd61269cebdc2e3fcaf8de631`).
**Estado:** IMPLEMENTADO EN FUENTE, PENDIENTE COMPILACIÓN Y PRUEBAS INTEGRADAS. **GO/NO-GO: NO-GO.** UAT no ejecutada.

## Corrección RES-H02-01 (P1): ciclo cerrado antes del SOAP

`ContrapartidaDispatchJobService.cs` ejecuta dos controles frescos por cada instrucción, tanto en Quartz como en `RetryBatchAsync`: (1) después de preparar el payload y antes de persistir la intención; (2) **después de persistir `InFlight` e inmediatamente antes de `ProcContrapartidasAsync`**. La comprobación vuelve a consultar el ciclo, su estado `Open`, entidad/cámara, calendario, vigencia, ventana y política de débito. Un `ExecuteUpdateAsync` condicional sobre el ítem verifica en la base de datos el ciclo reclamado, token, fase, lease vigente y no conciliación. El payload del mapper no puede autorizar por sí solo un envío.

Si cualquiera de las validaciones devuelve falso, `ContrapartidaDispatchRecovery.AbortUnsentCycleClaimAsync` revierte **únicamente el claim propio** con comparación atómica `Id`/`AchCycleId`/token/fase, guarda evento de auditoría `NotExecuted` con ordinal negativo, conserva XML de intención cuando existía, no incrementa `AttemptCount` y deja el ítem en `RetryPending` con código `RES_H02_CYCLE_CLOSED_PRE_SEND`. La política ya existente de recuperación/reasignación puede mover solo este caso **confirmado sin envío** a un nuevo ciclo abierto. La incertidumbre tras una caída de `InFlight` sin aborto confirmado continúa en conciliación, sin reenvío. Un fallo al guardar el aborto no se interpreta como un intento SOAP.

En el dispatcher programado, si la selección quedó obsoleta o la operación fue reasignada, se cancela el batch y se abandona el bucle, evitando selección infinita. El lote manual conserva los ítems no procesados.

## Corrección RES-H02-02 (P1): claim de ciclo atómico

El `ExecuteUpdateAsync` de claim programado ahora exige `i.AchCycleId == cycle.Id`, `i.ClearingHouseId == cycle.ClearingHouseId`, estado de ciclo `Open` y fase `Idle`, además de elegibilidad, contador y conciliación existentes. La lectura de ítems reclamados exige el mismo ciclo, fase y token. El claim manual existente conserva el CAS del ciclo de origen y reasigna destino en el mismo update; ambas rutas pasan por la nueva guarda pre-SOAP. Los workers retrasados no pueden reclamar una fila reubicada mediante la selección antigua.

## Alcance, límites y cautelas

- No se cambió `AchTransaction.AchCycleId`, fecha de valor, negocio, E2E-03 externo, interfaces SOAP, WSDL ni otras rutas de pago.
- **Sin nuevas migraciones**, columnas, snapshots ni dependencias; se reutilizan estados, lease, audit, intento e índices existentes. Migraciones anteriores preservadas para SQL Server y PostgreSQL.
- No se ejecutaron transacciones monetarias, SOAP externo ni UAT. Los 13 correctivos CENIT, hotfixes ACH Colombia y E2E anteriores se conservaron por inventario; sus pruebas runtime siguen pendientes.
- **Límite de garantía:** la comprobación final es una guarda DB de último instante, no un candado distribuido mantenido durante la llamada de red. Si otro proceso cierra el ciclo **después de la última confirmación atómica pero antes del I/O**, subsiste una ventana TOCTOU; su eliminación absoluta requeriría que el proceso de cierre participe en el mismo protocolo de exclusión o que el receptor SOAP compruebe el estado/idempotency key. No se declara garantía matemática de cero envíos en esta ventana hasta una prueba integrada del protocolo de cierre. El resultado incierto sigue protegido contra replay por reconciliación.

## Evidencia realmente ejecutada

| Control | Resultado | Alcance |
|---|---|---|
| Verificador focal nuevo `scripts/test/res_h02_soap_preflight_sqlite_check.py` | **27/27, salida 0** | inspección C# + modelo CAS SQLite separado |
| Verificadores previos de H01/H02, E2E y ACH/CENIT | **12/12 scripts, salida 0** | fuente y modelos SQLite; no ejecutan C# |
| Grafo de solución `scripts/validate-solution-projects.py` | **7/7 proyectos, salida 0** | referencias existentes, no build |
| `dotnet --info`, `dotnet build`, `dotnet test` | **código 127** | SDK no instalado; no compilación ni xUnit |
| xUnit nuevos | **4 métodos / 6 escenarios escritos, 0 ejecutados** | cierre durante mapper, después de intención, ventana vencida, claim obsoleto; Quartz y manual |
| SQL Server / PostgreSQL, 2+ workers, SOAP simulado/Live | **no ejecutados** | faltan servidores/SDK/ambiente |
| UAT | **no ejecutada** | reservada para cierre general |

**Ficheros probatorios:** `evidencias/FOCAL_RES_H02.txt`, `evidencias/REGRESSION_RESULTS.json`, `evidencias/REGRESSION_00..11.txt`, `evidencias/DOTNET_*.txt`, `evidencias/XUNIT_ATTEMPT.txt`, `evidencias/SOLUTION_STRUCTURE.txt`, `evidencias/SOURCE_DIFF_MANIFEST.json`.

## Verificación por realizar antes de SOAP Live (no ejecutada)

1. `dotnet restore ACHInterbank.sln && dotnet build ACHInterbank.sln -c Release` con .NET 10 y `dotnet test ... --filter FullyQualifiedName~ContrapartidaDispatchJobServiceTests` guardando TRX.
2. Ejecutar los 6 nuevos escenarios xUnit y la suite completa; validar que mocks interceptan `ProcContrapartidasAsync` con cero envíos tras cierre y cero intentos monetarios consumidos por abortos.
3. Integración real SQL Server y PostgreSQL con 2+ procesos Quartz/manual/recovery y una modificación concurrente de `AchCycleId`; confirmar SQL generado para `ExecuteUpdate` con navegación y exactitud de `rows affected`.
4. Pruebas de cierre antes/durante/justo después de `PersistIntent`, reinicios y resultados SOAP ambiguos; verificar `RetryPending` seguro vs `RequiresReconciliation`, reubicación a ciclo habilitado y que no se repita un débito.
5. Definir/verificar coordinación atómica con el proceso que **cierra ciclos** para resolver la ventana residual; hasta entonces, **NO-GO para habilitar SOAP Live**.

**Siguiente fase:** recorrido técnico controlado hacia SOAP Live, no nueva auditoría general; UAT sigue pospuesta.
