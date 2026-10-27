# I01 — Elegibilidad y selección read-only (2026-10-25)

## Estado de esta entrega

`IMPLEMENTADO_ESTATICAMENTE / NOT_RUN_ENVIRONMENT`. **No existe todavía traslado automático**: I01 únicamente calcula si la operación es potencialmente elegible y cuál sería el próximo ciclo **del mismo día valor y cámara**. Ninguna llamada operativa actual consume aún este servicio para cambiar registros. Las pruebas unitarias quedaron **escritas, no ejecutadas**.

## Cambios de código

- `Application/ACH/Interfaces/IAchPendingCycleEligibilityService.cs`: interfaz con `EvaluateAsync(transactionId)`; Application no conoce EF.
- `Application/ACH/Models/AchPendingCycleEligibilityResult.cs`: DTO inmutable `IsEligible`, `ReasonCode`, origen, destino, cámara y jornada.
- `Persistence/ACH/Services/Implementation/AchPendingCycleEligibilityService.cs`: implementación `[Scoped]`, descubierta por la convención Scrutor existente; **no tiene SaveChanges** ni mutación.
- `tests/Cfa.ACHInterbank.Tests/AchPendingCycleEligibilityServiceTests.cs`: 12 pruebas xUnit preparadas, con EF InMemory, mocks y reloj fijo; no ejecutadas en este entorno.
- `scripts/validate-cycle-reassignment-i01-static.py`: controles explícitos de contratos, dependencias, fail-closed, no escritura, no bloqueo y presencia de tests.

## Reglas implementadas

1. El registro debe existir, pertenecer a un ciclo y cámara activa, conservar coherencia con su lote y tener fecha valor igual a la fecha operativa de su ciclo.
2. Solo dirección `Outgoing`, origen `Cfa`, clasificación `Determined`, estado `Pending`, sin Return/Reversal ni referencias de devolución.
3. El corte original debe haber vencido realmente según `IOperationalCycleWindowResolver` + `TimeProvider`; programar lunes mientras es sábado **no provoca traslado**.
4. El calendario de la misma cámara debe indicar día hábil.
5. Fail-closed si existe membresía `AchFileExportTransaction` o evidencia `ContrapartidaDispatchItem`, `CenitCycleQueue`, `LiquidityOptimizationDecision`, `CenitNettingDetail`.
6. Fail-closed para archivos de salida `NACHA` (el nombre realmente registrado en `AchColombiaManagedMftOutboundWorkflow`) y alias legado `PUBLISH` cuyo conteo de miembros no demuestre trazabilidad suficiente.
7. Entre ciclos materializados de igual fecha y cámara, ordenados por corte, descartar los cerrados/cancelados/pasados y consultar `ICycleTransactionPolicy` para clase funcional y código real; devolver **primer elegible**. No adivinar el destino del siguiente día (I04).
8. Si ninguna ruta es verificablemente válida, devolver `IsEligible=false` con motivo interno (nunca causal Rxx/Dxx).

## Nota sobre lunes proyectado desde sábado

En `BatchResolver.ResolveAsync`, después de seleccionar un ciclo futuro, la rama `if (isOutsideWindow && !isCenit) EnsureCycleIsOpenForTransactions(...)` rechaza un ciclo `Before`. Eso contradice la intención funcional de asignar en sábado al lunes. **No se cambió `BatchResolver` en I01** para mantener la frontera del incremento y no afectar la originación existente sin build. La prueba I01 representa un registro ya proyectado; **no prueba que la creación de sábado funcione hoy**. Se mantiene riesgo `RISK-CR-002`; abordar con alcance focal apropiado en I04 o paso correctivo previo si impide pruebas end-to-end.

## Tabla de salida (códigos internos)

| Código | Decisión |
|---|---|
| `ELIGIBLE` | Puede proponerse ciclo destino; I02 deberá confirmar de nuevo bajo concurrencia |
| `CYCLE_NOT_EXPIRED` | El ciclo proyectado todavía no ha vencido |
| `NOT_PENDING`, `OUT_OF_SCOPE` | Estado o naturaleza fuera del alcance |
| `EXPORT_EVIDENCE_PRESENT`, `UNCERTAIN_EXPORT_MEMBERSHIP` | No hay prueba segura de ausencia de exposición |
| `PROCESSING_EVIDENCE_PRESENT` | Hubo señales de despacho, cola, netting o liquidez |
| `SOURCE_ASSIGNMENT_INCONSISTENT`, `VALUE_DATE_MISMATCH` | Inconsistencia de lote/fecha valor |
| `NOT_BUSINESS_DAY`, `SOURCE_WINDOW_UNRESOLVED` | Calendario o zona horaria no permite decidir |
| `NO_ELIGIBLE_TARGET_SAME_VALUE_DATE` | No quedan cortes compatibles en la misma fecha valor |
| `INVALID_TRANSACTION_ID`, `TRANSACTION_NOT_FOUND`, `SOURCE_CYCLE_UNRESOLVED` | No existe origen válido para evaluar |

## Casos escritos (sin ejecución .NET)

| Caso | Cobertura escrita |
|---|---|
| CR-01/CR-04 | Sábado y ciclo todavía vigente -> `CYCLE_NOT_EXPIRED`, sin traslado |
| CR-02 | Ciclo vencido + Pending + saliente + sin presentación -> selecciona C2 |
| CR-03/CR-08/CR-09 | Ciclo siguiente vetado por política -> probar C3; cuando no queda destino -> skip |
| CR-05/CR-06/CR-07 | Membresía de exportación, entrega previa o salida `NACHA` sin prueba de miembros -> fail-closed |
| CR-11/CR-12 | Cola CENIT -> fail-closed, no invocar motor de liquidez |
| CR-13/CR-14 | Sin destino el mismo día -> skip; fecha distinta -> fuera de alcance I01 |
| CR-15 | Predicado de misma cámara; no se consulta routing inter-cámara |
| CR-21/CR-22 | Fuera de alcance por clasificación/estado |
| CR-16–CR-20/CR-23 | **Pendiente:** I02/I03/I05; sin mutación no se puede demostrar atomicidad |

## Validación concreta

```text
python scripts/validate-cycle-reassignment-static.py --directory .
PASS estructura/documentación I00

python scripts/validate-cycle-reassignment-i01-static.py
PASS 19/19 comprobaciones estáticas específicas I01

command -v dotnet
NO_DOTNET
```

Estas pruebas **no** son validación sintáctica del compilador ni certifican el comportamiento EF sobre SQL Server/PostgreSQL. Se requiere build/test ejecutable, pruebas de consulta contra ambas bases, prueba de integridad concurrente con exportador y verificación funcional de corte/jornada.

## Riesgos abiertos para I02+

- Entre `EvaluateAsync` y una futura escritura puede crearse archivo de exportación; revalidación individual por sí sola **no resuelve la carrera**. I02 debe diseñar un protocolo atómico compatible con escrituras del exportador; I05 consolida la regresión. No permitir reenvío ante duda.
- `AchFileExportAuditService.RecordGeneratedFileAsync` utiliza sincronización propia e incluye membresías; I02 debe determinar puntos de exclusión/confirmación sin bloqueos globales nuevos.
- La ausencia de archivo auditado no prueba por sí sola que un transporte externo ajeno a `AchFileExport` no haya ocurrido. Mantener el alcance dentro de los flujos instrumentados.
- La selección de jornada siguiente o reescritura de `EffectiveEntryDate` **no** está autorizada en I01; I04 resuelve.
- Verificar concordancia de `ClearingHouse.ClearingHouseConfig` y políticas vigentes sobre bases reales cuando se disponga de ambiente.

## Siguiente incremento

**I02** — comando atómico de cambio de `AchCycleId`/`AchBatchId`, revalidación de evidencia, reutilización de lotes y `AchTransactionStateEvent`. Una decisión positiva en I01 es **necesaria pero no suficiente** para ejecutar I02.
