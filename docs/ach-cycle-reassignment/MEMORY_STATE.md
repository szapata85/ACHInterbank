# MEMORY_STATE — ACH-CR-001 / Reasignación automática de ciclos

> **Documento canónico de continuidad entre chats para esta iniciativa.** Leer primero; actualizar dentro del ZIP en *cada incremento*. No equivale a memoria persistente del asistente ni sustituye el `MEMORY.md` global del repositorio.

## Estado estructurado

```yaml
INITIATIVE_ID: ACH-CR-001
TITLE: Reasignación automática de ciclo para transacciones pendientes no presentadas
BASE_ZIP: ACHInterbank_ACHCOL_CFA_F6_3_UNIFICADO.zip
CURRENT_ZIP: ACHInterbank_ACH_CR_I06_FIX01_COMPLETO.zip
PATCH_CURRENT: I06-FIX01
INCREMENT_CURRENT: I06
NEXT_INCREMENT: NONE
IMPLEMENTATION_STATE: I06_FIX01_FUENTE_CORREGIDA_COMPILACION_NO_VERIFICADA_QUARTZ_DISABLED
RUNTIME_VALIDATION: NOT_RUN_ENVIRONMENT
UAT_STATE: NOT_STARTED
PRODUCTION_STATE: NOT_READY
LAST_UPDATED: '2026-10-26'
OWNER_OF_STATE: docs/ach-cycle-reassignment/MEMORY_STATE.md
```

## Marcadores por incremento

| Incremento | Estado | Evidencia | Siguiente |
|---|---|---|---|
| I00 Docs/arquitectura | `DELIVERED_STATIC` | `CHANGELOG.md`, `04_VALIDACION_ESTATICA.md`; integridad ZIP/archivos | I01 |
| I01 Resolver/elegibilidad | `IMPLEMENTADO_ESTATICAMENTE` | `07_I01_IMPLEMENTACION_Y_EVIDENCIA.md`, 12 tests escritos no ejecutados, 19 controles estáticos PASS | I02 |
| I02 Persistir/auditar | `IMPLEMENTADO_ESTATICAMENTE` | `08_I02_IMPLEMENTACION_Y_EVIDENCIA.md`; 8 xUnit escritos sin ejecutar; 25 controles fuente PASS | I03 |
| I03 Automatización Quartz | `IMPLEMENTADO_ESTATICAMENTE_DISABLED` | `09_I03_IMPLEMENTACION_Y_EVIDENCIA.md`; 9 xUnit escritos sin ejecutar; validador I03 y ZIP estático | I04 |
| I04 Fecha valor/jornada | `IMPLEMENTADO_ESTATICAMENTE_SIN_ROLLOVER` | `10_I04_IMPLEMENTACION_Y_EVIDENCIA.md`; 10 casos escritos (8 nuevos + 2 añadidos); validación I04 | I05 |
| I05 Regresión exportación/CENIT | `IMPLEMENTADO_ESTATICAMENTE_RUNTIME_PENDIENTE` | `11_I05_IMPLEMENTACION_Y_EVIDENCIA.md`; 4 tests añadidos escritos; admisión NACHA serializable y guardias CENIT | I06 |
| I06 Cierre documental/estático | `ENTREGADO_ESTATICAMENTE_RUNTIME_PENDIENTE` | `12_I06_AUDITORIA_Y_CIERRE.md`, `13_PLAN_HOMOLOGACION_GO_NO_GO.md`, validador I06, reporte externo ZIP | Homologación/UAT |

## Hechos verificados en I00

- `AchTransaction` persiste `AchCycleId`, `AchBatchId`, `EffectiveEntryDate`, estado y membresía exportada.
- `RoutingStrategyService` decide preferencia de cámara y ciclo; el reasignador NO puede usarlo para cambiar la cámara de una instrucción ya originada.
- `BatchResolver` puede exigir ventana abierta a un ciclo ACH Colombia proyectado; verificar el caso sábado en I01 antes de cambiar.
- `AchTransactionStateEvent` ya soporta motivo, `PayloadJson` e `IdempotencyKey`. Reusar auditoría; evitar tabla nueva.
- `NachaDataLoader.LoadByCycleAsync` selecciona por ciclo persistido.
- `LiquidityOptimizationService` ya mueve transacciones de CENIT por causa regulada de liquidez; ese flujo queda separado.
- `CenitCycleExecutionService` consume colas CENIT; existe riesgo de diferencia entre cola y asignación cuando se usa otro flujo. Investigar sin presumir defecto probado.
- ACH Colombia V35 5C es base del proyecto; candidata 7C no activada por puntos regulatorios abiertos.
- Disponibilidad del entorno en I00: ejecutable `dotnet` ausente. No se han ejecutado builds ni test runs.

## Evidencia nueva de I01 (2026-10-25)

- `EVID-CR-I01-01`: servicio de consulta read-only `IAchPendingCycleEligibilityService`/`AchPendingCycleEligibilityService`; resultado estructurado y mismo clearing house/día valor; `AsNoTracking` sin escrituras.
- `EVID-CR-I01-02`: se escribieron 12 tests xUnit `AchPendingCycleEligibilityServiceTests.cs`, **NO EJECUTADOS** por falta de `dotnet`.
- `EVID-CR-I01-03`: validadores estructural I00 e I01 ejecutados; I01 con 19/19 controles PASS. ZIP/CRC y manifest final externos en la entrega.
- `EVID-CR-I01-04`: `AchColombiaManagedMftOutboundWorkflow` usa `ExportKind="NACHA"`; `PUBLISH` corresponde a snapshots de configuración en `NachaConfigPublicationService`. Se protegieron las salidas transaccionales NACHA con membresía incompleta.
- `EVID-CR-I01-05`: `BatchResolver` rechaza en `Before` ciclos ACH Colombia proyectados fuera de ventana; el caso sábado->lunes sigue pendiente de un ajuste de originación. El test de sábado de I01 valida **solo** un registro ya proyectado.

**Incrementos restantes del plan tras I06:** **0**. La homologación y UAT son puertas externas al plan incremental, no incrementos declarados como hechos.

## Evidencia nueva de I02 (2026-10-25)

- `EVID-CR-I02-01`: contrato `IAchPendingCycleReassignmentService`, resultado e implementación Persistence `[Scoped]`, sin nuevo esquema ni scheduler. Servicio todavía opt-in (ningún consumidor automático).
- `EVID-CR-I02-02`: aislamiento relacional Serializable y `ExecutionStrategy`, revalidación I01 en la unidad de trabajo, lote destino con repositorio actual, evento `CYCLE_REASSIGNED` e idempotency key en el mismo commit. Se conservan identidad, fecha valor y `StateChangedAtUtc`.
- `EVID-CR-I02-03`: uso de `ITransactionPersister` para recalcular totales/clase; 8 tests xUnit escritos **NO EJECUTADOS**.
- `EVID-CR-I02-04`: validador I02 25/25 PASS, script I00/I01 y verificación ZIP/CRC externa a concluir en artefacto entregado.
- `EVID-CR-I02-05`: condición abierta de coordinación con exportación iniciada antes del commit; no homologar como seguro end-to-end (I05).

## Decisiones durables de la iniciativa

- `DEC-CR-001`: MVP = saliente Pending sin evidencia de exportación, despacho o compensación; fail-closed.
- `DEC-CR-002`: destino posterior dentro de la misma cámara, filtrado por políticas y calendario vigentes.
- `DEC-CR-003`: no rehacer creación ni tocar trazas, golden files ni motor NACHA sin defecto probado.
- `DEC-CR-004`: auditar con `AchTransactionStateEvent`; sin migración/tabla nueva por defecto.
- `DEC-CR-005`: CENIT liquidez/cola no es política transversal de pendientes.
- `DEC-CR-006`: fases pequeñas, validación estática real, ZIP completo por incremento; sin bloqueos por no disponer de .NET/Docker.
- `DEC-CR-007`: no modificar fecha valor a otro día hasta verificación contractual/normativa en I04.
- `DEC-CR-008`: no activar ACH Colombia candidata 7C al implementar ACH-CR-001.
- `DEC-CR-009`: I01 limita selección al mismo día valor/cámara, sin transición de fecha valor ni persistencia. Un resultado positivo no autoriza una escritura hasta revalidación atómica en I02.

- `DEC-CR-010`: en I02 no agregar índice/tabla; serializar el comando en DB relacional y reutilizar auditoría, lotes y recalculador de totales actual.
- `DEC-CR-011`: exponer I02 por convención DI sin invocador automático; si hay atributos de lote incompatibles, no forzar mezcla.
- `DEC-CR-012`: el riesgo de exportador concurrente persiste; I03 debe mantenerse inactivo por defecto en entorno productivo hasta I05.

## Riesgos y asuntos abiertos

- `RISK-CR-001 / MITIGATED_STATIC_RUNTIME_OPEN`: la admisión de NACHA y movimiento I02 son Serializable y las rutas de salida conocidas auditan antes de hacer disponible el artefacto; **sin pruebas relacionales concurrentes** no declarar cierre/golive. Revisar rutas nuevas y proveedores con estrategia de reintentos.
- `RISK-CR-002 / FIXED_STATIC_I04_RUNTIME_PENDING`: `BatchResolver` ahora acepta `Before` únicamente si fecha de procesamiento es posterior al día local y es día hábil, con ciclo no cerrado/cancelado; tests escritos, faltan ejecución/homologación.
- `RISK-CR-003 / OPEN_POLICY_NOT_AUTHORIZED`: fecha valor inter-día por traslado de una transacción ya creada **no autorizada**; I04 conserva bloqueada esa transición con `NO_ELIGIBLE_TARGET_SAME_VALUE_DATE`. Requiere definición contractual y prueba de cámara antes de habilitarla.
- `RISK-CR-004 / OPEN`: interacción con `CenitCycleQueue`/liquidez y sus referencias (I05).
- `RISK-CR-005 / PARTIAL_STATIC_RECALCULATOR_REUSED`: mecanismo exacto de actualización de totales de lotes / dispatch y correlación (I02).
- `RISK-CR-006 / OPEN`: 7C ACH Colombia sin fecha efectiva y ambigüedad de horario oficial C4; fuera de alcance de activación.
- `RISK-CR-007 / OPEN`: no se dispone de evidencia ejecutable de este cambio; build/tests/UAT pendientes del entorno adecuado.

## Evidencia registrada

- `EVID-CR-I00-01`: inspección de modelos, contratos, implementaciones, jobs y normativa (ver `06_TRAZABILIDAD.md`).
- `EVID-CR-I00-02`: ZIP base conservado intacto, con nuevos artefactos aislados (ver `CHANGELOG.md`).
- `EVID-CR-I00-03`: script validador estático incluido (ver `04_VALIDACION_ESTATICA.md`).

## Evidencia nueva de I03 (2026-10-25)

- `EVID-CR-I03-01`: `AchPendingCycleReassignmentHandler` implementa ITaskHandler dentro de Persistence, consulta IDs salientes Pending por ciclos vencidos (TZ), paginación keyset y `BatchSize`/`MaxScanned` acotados. Toda mutación se delega a I02, que vuelve a evaluar I01.
- `EVID-CR-I03-02`: catálogo Quartz y `TaskDefinitionSeeder` con tarea `AchPendingCycleReassignment` Disabled de fábrica, sin manual, retry ni requests recovery, con `ConcurrencyPolicy.SkipIfRunning`. Segunda barrera `AchCycleReassignment:EnableExecution` false si ausente. **NO ACTIVAR antes I05**.
- `EVID-CR-I03-03`: 9 tests xUnit escritos en `AchPendingCycleReassignmentHandlerTests.cs`, **NO EJECUTADOS** por entorno sin SDK.
- `EVID-CR-I03-04`: nuevo validador I03 y validaciones heredadas. Registrar los conteos/resultado reales en artefacto y en la entrega, nunca como evidencia runtime.
- `DEC-CR-013`: seguir usando el scheduler dinámico existente; sin servicio en segundo plano nuevo, tabla, endpoint ni migración.
- `DEC-CR-014`: doble guardia obligatoria para impedir puesta en marcha accidental.
- `RISK-CR-008 / OPEN`: prefijos inelegibles de IDs pueden ocupar `MaxScanned` repetidamente (sin cursor inter-ejecución). No introducir nuevo esquema hasta medir o demostrar necesidad en I06.

## Siguiente acción exacta

**No quedan incrementos planificados I00–I06.** Conservar `ACHInterbank_ACH_CR_I06_FIX01_COMPLETO.zip` como fuente completa de continuidad. El parche I06-FIX01 corrige tres diagnósticos de C# reportados externamente, sin afirmar build completo. Próxima acción EXTERNA: ejecutar `13_PLAN_HOMOLOGACION_GO_NO_GO.md` en ambiente autorizado con .NET/BD/Quartz y cámara simulada, levantar evidencias y solicitar aprobación de negocio. Mantener Quartz deshabilitado y no ejecutar pagos reales.

## Procedimiento invariable después de CADA incremento

1. Sustituir `CURRENT_ZIP`, `INCREMENT_CURRENT`, `NEXT_INCREMENT`, `LAST_UPDATED` y `IMPLEMENTATION_STATE` por valores **reales**; no declarar cierre sin evidencia.
2. Cambiar estado de la fila correspondiente sin borrar riesgos anteriores: cerrar riesgos solo con evidencia.
3. Agregar archivos modificados/añadidos, resumen de decisiones, riesgos, resultado estático, tests no ejecutados y hash en `CHANGELOG.md`.
4. Actualizar arquitectura **as-built**, matriz de escenarios y fuentes cuando aplique.
5. Comparar contra el ZIP del incremento previo y generar nuevo ZIP **completo**, con todos los componentes y documentación histórica relevante.
6. Entregar el nuevo ZIP al usuario y pedir que sea el punto de partida del siguiente chat. No afirmar memoria automática entre conversaciones.

## Evidencia nueva de I04 (2026-10-25)

- `EVID-CR-I04-01`: ajuste `BatchResolver` con `IOperationalCalendarService`, permite proyección de fecha futura y evita aceptar mismo día antes de apertura, vencidos y calendario inhábil (solo ruta no-CENIT).
- `EVID-CR-I04-02`: `RoutingStrategyService` omite sesiones terminales Closed/Cancelled.
- `EVID-CR-I04-03`: ocho pruebas xUnit nuevas + dos añadidas a I01; **NO EJECUTADAS** por ausencia .NET.
- `EVID-CR-I04-04`: razón `NO_ELIGIBLE_TARGET_SAME_VALUE_DATE` y conservación de `EffectiveEntryDate` ante fin de jornada; no autorizar rollover por inferencia.
- `DEC-CR-015`: no introducir cambio automático de fecha valor entre días en I04; solo proyecto de día hábil durante creación conforme al ciclo ya seleccionado por routing.
- `DEC-CR-016`: no tocar cola CENIT, no activar Quartz I03 ni agregar esquema/migraciones.
- `RISK-CR-009 / OPEN`: escenario real fin de jornada sin ventana posterior queda pendiente de negocio para acordar nuevo valor fecha, caducidad o cancelación explícita.
- **Siguiente histórico tras I04:** I05. El plan I00–I06 se cerró documentalmente en I06; ninguna prueba runtime fue ejecutada.

## Evidencia y decisiones I05 (2026-10-25)

- `EVID-CR-I05-01`: inspeccionada la secuencia builder → auditoría → MFT/descarga; NACHA audit tenía validación/Save fuera de una misma transacción serializable, ajustado `AchFileExportAuditService` para relacional NACHA.
- `EVID-CR-I05-02`: ruta `NachaSecurityOperationService.GenerateNachaInternalAsync` audita archivo y membresía por partición antes de `IOperationArtifactStore.SaveAsync` y `DownloadAvailable`.
- `EVID-CR-I05-03`: `AchPendingCycleEligibilityService` deniega source cycle con cualquier `NACHA`/`PUBLISH`, además de procesos ciclo CENIT existentes. Se conservan barreras transaccionales I02.
- `EVID-CR-I05-04`: cuatro tests escritos en suites existentes; nuevos validadores estáticos I05 y reportes del paquete, **sin ejecución .NET**.
- `DEC-CR-017`: reusar `IAchFileExportAuditService` y `AchFileExportTransaction` como frontera de admisión NACHA. No ampliar Domain ni añadir schema/lock distribuido.
- `DEC-CR-018`: política fail-closed por presencia de exportación del ciclo fuente, conservadora a nivel de ciclo. Operaciones retenidas requieren revisión, no force-move.
- `DEC-CR-019`: mecanismos CENIT de ejecución/cola/liquidez no se modifican y prevalecen ante el traslado transversal.
- `RISK-CR-010 / OPEN_RUNTIME`: validar SERIALIZABLE y retries con SQL Server/PostgreSQL y dos procesos competidores; fallos de serialización/deadlock no implican éxito.
- `RISK-CR-011 / OPEN_MULTIFILE`: una exportación con particiones no es commit global atómico con filesystem/entrega; fallo tardío puede dejar evidencia parcial. Nunca reintentar arbitrariamente.
- `RISK-CR-012 / OPEN_CONSERVATIVE_DENIAL`: exportaciones previas de otras transacciones del ciclo pueden dejar pendientes sin mover; inspección operativa y política futura si aplica.
- **No go-live, Quartz disabled, no automatic retries network.**
- **Siguiente histórico tras I05:** I06. I06 cerró el alcance estático del plan; UAT/go-live permanecen pendientes.

## Evidencia y estado final I06 (2026-10-25)

- `EVID-CR-I06-01`: auditoría estática transversal de rutas Application → Persistence → Quartz; conservados contratos y guardias I01–I05; ninguna migración, endpoint ni motor financiero agregado.
- `EVID-CR-I06-02`: se corrigió el validador estático heredado de I05 para revisar evidencia histórica sin obligar a que `INCREMENT_CURRENT` permanezca en I05. Se actualizaron tres comentarios/descripciones obsoletos del código sin alterar semántica ejecutable.
- `EVID-CR-I06-03`: `scripts/validate-cycle-reassignment-i06-static.py` comprueba estados finales, restricciones, arquitecturas y cobertura estática; los conteos, diff y SHA-256 finales están en `ACH_CR_I06_VALIDATION.json` / `.sha256` externos.
- `EVID-CR-I06-04`: cierre documental as-built, matriz de escenarios y plan GO/NO-GO en `12_I06_AUDITORIA_Y_CIERRE.md` y `13_PLAN_HOMOLOGACION_GO_NO_GO.md`.
- `DEC-CR-020`: fin del plan **I00–I06 = CIERRE ESTÁTICO**, diferente de compilación, UAT, certificación bancaria o aprobación productiva.
- `DEC-CR-021`: conservar tarea Quartz `Disabled`, `AchCycleReassignment:EnableExecution=false` y sin reintentos de red; cualquier habilitación requiere plan de homologación aprobado y nueva evidencia de configuración/operación.
- `RISK-CR-013 / OPEN_RUNTIME`: resolución completa de `Serializable` bajo SQL Server/PostgreSQL, admisión de NACHA y proceso de despliegue/retroceso necesita prueba real y evidencia de observabilidad. NO sustituida por validadores de presencia textual.
- `RISK-CR-014 / OPEN_POLICY`: revisar lotes pendientes retenidos por guardia conservadora de exportación por ciclo y candidatos inelegibles que pueden monopolizar `MaxScanned`. No flexibilizar sin evidencia UAT.
- `RISK-CR-015 / OPEN_RELEASE`: la falta de `dotnet` y servicios de BD/cámara impidió pruebas ejecutables; **NO-GO** hasta completar la checklist de homologación.

**Estado de cierre I06:** `IMPLEMENTADO_ESTATICAMENTE`, **0 incrementos pendientes**; `RUNTIME_VALIDATION: NOT_RUN_ENVIRONMENT`, `UAT_STATE: NOT_STARTED`, `PRODUCTION_STATE: NOT_READY`.


## Parche de compilación I06-FIX01 (2026-10-26)

- `EVID-CR-I06-FIX01-01`: el usuario reportó **CS1929, CS0103 y CS0165** al compilar I06 en Visual Studio. Por tanto, I06 **no** puede considerarse compilado aunque los validadores estáticos previos fueran satisfactorios.
- `EVID-CR-I06-FIX01-02`: corregido el LINQ de `ContrapartidaDispatchJobService` para `int? AchTransactionId` con guardia `HasValue` y `.Value`; se conserva el filtro por cámara y ciclo.
- `EVID-CR-I06-FIX01-03`: importado `Cfa.ACHInterbank.Application.ACH.Models` en `AchPendingCycleReassignmentHandler` para resolver `OperationalCycleWindowStatus`.
- `EVID-CR-I06-FIX01-04`: inicializado `correlationError` antes del cortocircuito en `IncomingNachaAdmissionValidator`, conservando el código de rechazo y fallback.
- `DEC-CR-022`: tratar I06-FIX01 como **parche** posterior a I06, sin nueva fase; solo tres archivos C# y la documentación/validador del parche.
- `RISK-CR-016 / OPEN_COMPILE`: sin `dotnet` en este contenedor. **No se afirma compilación exitosa**, ni se cierra homologación por validaciones sintácticas o textuales.
- `NEXT_INCREMENT: NONE`; **incrementos pendientes: 0**.
- Revisar `14_I06_FIX01_CORRECCIONES_COMPILACION.md` y `scripts/validate-cycle-reassignment-i06-fix01-static.py`. Ejecutar build real en Windows con el ZIP FIX01; Quartz debe permanecer deshabilitado y `PRODUCTION_STATE: NOT_READY`.
