# ACHCOL-NACHA-CFA-EXTCYCLE-001 — MEMORY_STATE

> **Estado durable para implementación incremental.** Este archivo refleja acuerdos, evidencia y avance registrado; **no es una prueba de compilación, operación, homologación o certificación por sí mismo**. Se debe actualizar al finalizar **cada microincremento** y confirmar cambios con Git. No reemplaza el plan funcional.

```yaml
program_id: ACHCOL-NACHA-CFA-EXTCYCLE-001
program_name: NACHA-M ACH Colombia — procesamiento CFA con ciclo externo no parametrizado
state_version: 1.20.0
last_updated: '2026-10-25'
program_status: IN_PROGRESS
current_phase: F6
current_phase_status: IMPLEMENTED_UNVERIFIED
active_microtask: null
next_microtask: null
last_completed_microtask: F6.3
next_action: "CERTIFICATION_GATE_PENDING: ejecutar en CI/ambiente autorizado .NET 10.0.402, xUnit, migraciones PostgreSQL/SQL Server, regresiones R5C/R7, XSD/WSDL, UAT CFA y aprobacion de Live. F6.3 entrega auditoria offline, NO certificacion."
execution_mode: ONE_MICROTASK_PER_DELIVERY_CONTINUOUS_PROGRESS
verification_policy: IMPLEMENT_CONTINUOUSLY_DEFER_ENVIRONMENT_AND_FINAL_INTEGRATION_TESTS_TO_F6
external_dispatch_allowed_without_authorization: false
source_mode: PROVIDED_ZIP_SNAPSHOT
source_archive: ACHInterbank_ajustado.zip
previous_unified_archive: ACHInterbank_ACHCOL_CFA_F6_2_UNIFICADO.zip
current_unified_archive: ACHInterbank_ACHCOL_CFA_F6_3_UNIFICADO.zip
current_unified_archive_validation: F6_3_PASS_CRC_8130_ENTRIES_8124_UNCHANGED_5_UPDATED_1_ADDED
source_archive_sha256: 26da6cf2dffcf939aa539f8ea9f0169fa8164fb959eaf2eb3a7ae22651b6c890
source_git_head_ref_from_zip: refs/heads/ACH-Interbank
source_git_head_sha_from_zip: 0b4ce1c809ca5acfdafaf455cae4d0eaa5292f07
source_git_worktree_clean: false
source_git_worktree_normal_status_entries: 2918
source_git_worktree_status_with_autocrlf_true: 7
source_git_missing_tracked_files: 7
source_baseline_policy: PROVIDED_ARCHIVE_IS_REFERENCE_WITH_RECORDED_GIT_DIVERGENCES
source_baseline_report: F0_1_BASELINE.md
source_executable_validation: NOT_RUN_DOTNET_10_SDK_UNAVAILABLE
last_trace_report: F0_2_FLOW_TRACE.md
architecture_decision_record: F0_3_ADR_ARCHITECTURE.md
architecture_naming_addendum: F0_3_A_NAMING_OUT_ENVELOPE.md
last_completed_documentation_gate: F6.3_OFFLINE_QA_DOSSIER_NO_DOTNET_BUILD_NO_DB_NO_CFA_CERTIFICATION
package_policy: COMPLETE_SOURCE_AND_UPDATED_DOCS_AFTER_EVERY_INCREMENT
missing_environment_is_development_blocker: false
final_e2e_qa_stage: F6
state_maintainer: Equipo de implementación ACHINTERBANK
plan_document: PLAN.md
resume_document: CHATGPT_MICRO_START.md
feature_flag_proposed: AchCol:AllowCycleIndependentInboundDispatch
```

## 1. Reglas de uso y continuidad

1. **Antes de trabajar**: abrir `PLAN.md`, este `MEMORY_STATE.md`, `CHATGPT_MICRO_START.md` y los archivos fuente citados por `next_microtask`. Verificar que el checkout y el estado real coincidan con el baseline; si hay divergencias, registrarlas y conciliar antes de sobrescribir código.
2. **Microincrementos encadenados.** Ejecutar la microtarea `next_microtask` y registrar su resultado antes de continuar. Pueden ejecutarse múltiples microtareas en una misma sesión si hay capacidad, con registro individual. Respetar el estado de otros programas (`CFA-SOAP-UX-MAP-001`, etc.).
3. **Al iniciar**, actualizar `active_microtask`, su estado `IN_PROGRESS`, y registrar Git SHA, nombre de rama y estado de archivos. No presuponer que el Git HEAD del ZIP sea el del checkout de ejecución.
4. **Al terminar**, anotar cambios exactos, evidencia de lectura/pruebas/compilación, fallos, migraciones, impacto en base de datos, nuevo commit (si existe), y qué queda sin verificar. Registrar el resultado en el historial de sesiones.
5. **Estados válidos:** `NOT_STARTED`, `IN_PROGRESS`, `IMPLEMENTED_UNVERIFIED`, `VERIFIED`, `BLOCKED`, `SUPERSEDED`. `VERIFIED` exige evidencia ejecutada para el criterio de la microtarea. Una prueba que no pudo ejecutarse **no** es una prueba aprobada.
6. **No detener el desarrollo por ausencia de ambientes.** Si faltan .NET, Docker, CFA, BD o UAT, se continuará con la siguiente microimplementación después de registrar el estado `IMPLEMENTED_UNVERIFIED` y sus verificaciones pendientes. **Las pruebas finales de integración, homologación y aceptación de todo el desarrollo se ejecutarán al terminar las implementaciones (F6)**. No declarar como aprobadas pruebas no ejecutadas.
7. **No habilitar `Live` ni enviar transacciones monetarias reales** sin autorización del entorno CFA, mapping/contrato homologados, pruebas de idempotencia y aprobación operativa. Un timeout SOAP con resultado incierto debe conciliarse **antes** de retransmitir.
8. El archivo **no se actualiza automáticamente** al abrir otra conversación. Mantenerlo dentro del proyecto y actualizarlo al cierre de cada microincremento. **Cada entrega debe incluir un ZIP nuevo con el proyecto COMPLETO y modificado (fuentes, tests, migraciones, configuración y docs), además de PLAN, MEMORY_STATE, decisiones y guía de continuidad actualizados**. No sustituir el proyecto completo por parches sueltos.
9. Solo detenerse ante un impedimento **real de implementación** que impida continuar de manera segura (p. ej. falta de definición de campos contractuales indispensables). La falta de ambiente o de UAT no constituye impedimento de desarrollo. Registrar asunciones y dependencias; no inventar datos contractuales ni ejecutar operaciones monetarias reales no autorizadas.

## 2. Contrato funcional inmutable acordado

| ID | Regla confirmada | Estado |
| --- | --- | --- |
| INV-001 | `FileIdModifier` en encabezado NACHA-M: `A` a `Z` ↔ `001` a `026`; `0` a `9` ↔ `027` a `036` (manual ACH Colombia V35). `H=008` e `I=009`. | Aprobada / sin implementación nueva |
| INV-002 | Si el nombre externo usa `RRRRTTT.ZZZ.N`, `N` puede ser `8`, `9` o superior positivo para el **flujo interno CFA solicitado**, sin tope artificial `7`. | Requisito del solicitante |
| INV-003 | `FileIdModifier`, `FileSequence`/`ZZZ`, `ExternalCycleNumber`/`N` y `AchCycleId` son **cuatro conceptos distintos**. No forzar `FileSequence == ExternalCycleNumber`. | Requisito del solicitante |
| INV-004 | La ausencia de fila `AchCycle` para ciclo externo `8`, `9`, etc., **no puede ser la única razón de rechazo o de no despacho** de transacciones elegibles ACHCOL. | Requisito del solicitante |
| INV-005 | No inventar `AchCycle` ficticios; admitir `AchCycleId=null` en la nueva ruta o usar un origen de despacho por entrada NACHA-M. | Diseño propuesto / pendiente de ADR |
| INV-006 | Mantener validación de formato, cámara/fecha, perfil NACHA, secuencia del archivo cuando corresponda, duplicidad, autorizaciones, clasificación, mapping SOAP y trazabilidad. | Obligatoria |
| INV-007 | `Proc_Transacciones` recibe solo transacciones elegibles para esa operación. No forzar devoluciones, prenotificaciones o mensajes de otro tipo por la sola presencia de `H/I` o `.8/.9`. | Obligatoria |
| INV-008 | Aislar el cambio a la ruta ACH Colombia; preservar CENIT y el camino con ciclo interno configurado. | Obligatoria |
| INV-009 | Conservar idempotencia y manejo de incertidumbre CFA; ningún reintento ciego de una operación monetaria después de timeout. | Obligatoria |
| INV-010 | El manual ACH Colombia V35 suministrado describe nomenclatura `RRRRTTT.ZZZ.1`; el soporte interno CFA con sufijos `.8/.9` es **requerimiento del usuario**, aún pendiente de homologación para intercambios directos con ACH Colombia. | Distinción documental explícita |
| INV-011 | Los archivos entrantes **físicos** ACHCOL pueden terminar en `.OUT` (en claro) o `.OUT.env` (cifrados en sobre digital); en el segundo caso, verificar/descifrar antes del parser y eliminar solo `.env` del nombre canónico. | **Confirmado por usuario / F0.3.A** |
| INV-012 | La estructura física observada en tests incluye fecha: `RRRRTTT.ZZZ.AAAAMMDD.N.OUT[.env]`; el ciclo `N` precede `.OUT`, no es la extensión; `N` puede ser 8/9+. La estructura lógica parcial `RRRRTTT.ZZZ.N` **no** define por sí sola el nombre físico inbound. | **Confirmado por tests / F0.3.A** |
| INV-013 | Managed Folder/SFTP deben descubrir también `*.OUT` plano (hoy solo `.OUT.env`/`.RET.env`); no desproteger la ruta RET ni las verificaciones de firma/certificado de `.env`. | **Brecha verificada / pendiente de código F1** |

**Ejemplos de aceptación (ficticios):** `1234567.008.8` + encabezado `H` y `1234567.009.9` + encabezado `I`. **Caso indispensable de independencia:** `1234567.008.10` + encabezado `H`: `FileSequence=8`, `ExternalCycleNumber=10` y `AchCycleId=null` pueden coexistir, conforme a perfil autorizado.

## 3. Línea base y evidencia disponible

| ID | Fuente / hallazgo | Alcance de evidencia |
| --- | --- | --- |
| EVID-BAS-001 | ZIP recibido: `ACHInterbank_ajustado.zip`, SHA-256 `26da6cf2dffcf939aa539f8ea9f0169fa8164fb959eaf2eb3a7ae22651b6c890`. | Huella binaria del paquete recibido, no certificación. |
| EVID-BAS-002 | `.git/HEAD` dentro del ZIP apunta a `refs/heads/ACH-Interbank`; ref del ZIP `0b4ce1c809ca5acfdafaf455cae4d0eaa5292f07`. El `MEMORY.md` del proyecto menciona como baseline funcional `ACH-Interbank-Postgresql`. | **Confirmado en F0.1:** checkout extraído en `ACH-Interbank` / SHA citado. No es árbol limpio: 2.918 entradas de estado Git, 7 eliminaciones seguidas (ver `F0_1_BASELINE.md`). |
| EVID-BAS-008 | `F0_1_BASELINE.md`: 8.073 entradas ZIP, integridad `unzip -tq` PASS, SHA-256 de 12 archivos críticos, Git `ACH-Interbank`/`0b4ce1c8`, `git status` con 2.918 entradas / 7 tras normalizar EOL. | F0.1 `VERIFIED` solamente para inventario y trazabilidad del snapshot. No valida build ni código funcional. |
| EVID-BAS-003 | `ExternalFileNameSupport.cs` tiene patrón ACH con sufijo de entero positivo y `BuildAchName`; `IncomingNachaCycleResolver.cs`/`IncomingNachaAdmissionValidator.cs` usan parser de nombre CENIT en el flujo actual. | Lectura estática; no prueba end-to-end. |
| EVID-BAS-004 | `IncomingNachaAdmissionValidator.cs` requiere `AchCycleId`; `IncomingNachaDispatchEligibilityPolicy.cs` comprueba `ResolvedAchCycleId`; existen dependencias de ciclo en cola, pipeline y mapper `Proc_Transacciones`. | Revisión estática; rutas y métodos trazados en `F0_2_FLOW_TRACE.md` (SES-002). |
| EVID-BAS-005 | Existe `docs/CFA_SOAP_UX_MAPPING_IMPLEMENTATION_PACKAGE_2026-10-12/MEMORY_STATE.md` de otro programa en el ZIP. | No sobrescribirlo ni declarar terminadas sus tareas desde esta iniciativa. |
| EVID-BAS-006 | Herramientas locales disponibles en la sesión que redactó este estado: `git` y `node`; `dotnet` y `docker` no aparecieron en `PATH`. | Limitación de este entorno, no necesariamente del entorno de desarrollo. |
| EVID-BAS-007 | Plan `Plan_Implementacion_NACHA_ACHCOL_Sin_Ciclo_CFA.md` con fases F0–F6 y pruebas AT-01 a AT-20. | Diseño propuesto; implementación, pruebas y homologación **no ejecutadas**. |

| EVID-IMP-011 | Parser ACHCOL físico y pruebas añadidos en F1.1, ruta separada de CENIT; 31 casos en xUnit. | Código inspeccionado; **sin ejecución .NET**. Ver `F1_1_IMPLEMENTATION.md`. |
| EVID-BAS-009 | F0.3 `F0_3_ADR_ARCHITECTURE.md`: cola única discriminada, esquema dual expand-first, origen NACHA y policy por cámara/flag definidos a partir del código F0.2. | **VERIFIED solo como ADR/documentación.** Sin .NET, DB ni SOAP; ningún cambio C# en F0.3. |

**Datos no verificados:** limpieza de working tree, compatibilidad exacta XSD importado por WSDL CFA, shape de parámetros `Proc_Transacciones` para ciclo externo, estado de migraciones desplegadas y comportamiento del endpoint CFA real. No inventar conclusiones.

## 4. Decisiones técnicas

| ID | Decisión / propuesta | Estado |
| --- | --- | --- |
| DEC-001 | Crear ruta aislada `AchColFileBased` para ACHCOL, sin modificar la semántica de `CycleBound`. | **ADOPTADA F0.3**; pendiente código |
| DEC-002 | Persistir `FileIdModifier`, `FileSequence`, `ExternalCycleNumber` y nombre original; `AchCycleId` independiente y opcional para ACHCOL. | **ADOPTADA F0.3**; pendiente código |
| DEC-003 | Parser entrante ACHCOL estricto y separado de `CenitOfficialFileNameParser`. | **IMPLEMENTADA F1.1** (parser aislado; cableado F1.2 pendiente) |
| DEC-004 | Feature flag `AchCol:AllowCycleIndependentInboundDispatch` apagada por defecto hasta UAT y autorización. | **ADOPTADA F0.3**; pendiente código |
| DEC-005 | **Extender la cola existente** con `SourceKind=CycleBound/AchColFileBased`, `EntryDetailId` FK, `AchTransactionId?` y `AchCycleId?`; preservar rutas legacy. No crear cola paralela ni ciclos ficticios. | **RESUELTA F0.3**; implementar F3 |
| DEC-006 | Reusar `WscfaachSoapClient.ProcTransaccionesAsync`, cola y auditoría; construir contexto NACHA tipado para mapper sin `cycle.id` inventado. | **ADOPTADA F0.3**; homologación contractual F5/F6 |
| DEC-007 | `Live` solo después de certificación externa; `DryRun` previo. | **OBLIGATORIA / RATIFICADA F0.3** |

| DEC-008 | **No bloquear las implementaciones por no disponer de ambiente de pruebas; ejecutar pruebas finales después de F1–F5 en F6.** | **APROBADA (solicitante)** |
| DEC-009 | **Entregar al cierre de cada microimplementación el proyecto completo modificado + PLAN, MEMORY_STATE y decisiones en un ZIP unificado.** | **APROBADA (solicitante)** |
| DEC-010 | No inferir equivalencia entre `FileSequence=ZZZ`, `ExternalCycleNumber=N` y `AchCycleId`; aceptar `H/008/.10`. | **ADOPTADA F0.3** |
| DEC-011 | Procesamiento por cámara + feature flag y resolución de contexto distinta de resolución de ciclo; no indicar ciclo "resuelto" si no existe. | **ADOPTADA F0.3** |
| DEC-012 | Migraciones expand-first con paridad PostgreSQL / SQL Server; referencias opcionales en nueva ruta e historial intacto. | **ADOPTADA F0.3** |
| DEC-013 | Idempotencia por huella de archivo + huella de negocio por entrada, sin basarse solo en ciclo o ingesta; timeout incierto exige conciliación. | **ADOPTADA F0.3** |
| DEC-014 | No mapear `ExternalCycleNumber` automáticamente a `cycle.id`/SOAP: adaptar fuente NACHA tipada y homologar semántica en F5/F6. | **ADOPTADA F0.3** |
| DEC-015 | Rollback por flag y retención controlada de cola NACHA; nunca recircular automáticamente por legacy lo ya despachado. | **ADOPTADA F0.3** |

**Decisiones aún sujetas a homologación externa o evidencia:** si `N` corresponde a un parámetro de CFA; calendario de servicio, alcance `.N` en el canal directo ACH Colombia y cambios futuros del motor de mapping CFAUX. El diseño de cola y migraciones está definido en F0.3, su ejecución y restricciones se validarán al implementar F3.

| DEC-017 | Parser inbound ACHCOL autónomo y estricto; forma física controlada, sin side effects ni dependencia de ciclo. Cableado a canales y header en F1.2. | **IMPLEMENTADA F1.1 como parser**; pruebas C# no ejecutadas |
| DEC-018 | Resolución ACHCOL `ResueltoSinCiclo=7`, `IsResolved=true`, `AchCycleId=null`; estado explícito, no ciclo ficticio. | **CÓDIGO F2.1**, sin pruebas .NET |
| DEC-019 | Activación `AchCol:AllowCycleIndependentInboundDispatch` deshabilitada por defecto, solo nombres `.OUT` canónicos válidos y código ACHCOL; no cambiar RET/CENIT. | **CÓDIGO F2.1**, sin habilitar Live |
| DEC-020 | Fecha y hora del header válidas obligatorias en ruta ACHCOL por archivo; evidencia `fileSequence`/`externalCycleNumber` sin inferir ciclo. | **CÓDIGO F2.1** |
| DEC-021 | Resolver no omite admisión ni habilita cola/SOAP; esas etapas mantienen su propia política y se implementan F2.2–F5. | **ALCANCE PROTEGIDO F2.1** |
| DEC-022 | Admisión sin AchCycle solo con `ResueltoSinCiclo`, `AchColFileBased`, `AchCycleId=null`, estado no ambiguo y flag opt-in. | **CÓDIGO F2.2**, pendiente .NET |
| DEC-023 | Defensa en profundidad: cámara activa ACHCOL y correlación `.OUT` canónico ↔ header tras descifrado, sin bypass en RET/CENIT. | **CÓDIGO F2.2** |
| DEC-024 | Fecha efectiva ACHCOL `yyyyMMdd` validada explícitamente en la nueva ruta; lectura legacy no alterada. | **CÓDIGO F2.2** |
| DEC-025 | `CycleNumber` en admisión ACHCOL expresa `N` externo, sin equivalencia `ZZZ` o `AchCycleId`; persistencia tipada F2.3. | **CÓDIGO F2.2, PERSISTENCIA PENDIENTE** |

| DEC-026 | `FileName` es nombre físico original; `CanonicalPlainFileName` derivado, sin duplicar origen. | **CÓDIGO F2.3** |
| DEC-027 | `FileIdModifier`, `FileSequence`, `ExternalCycleNumber` persistidos por separado; `AchCycleId` no se infiere. | **CÓDIGO F2.3** |
| DEC-028 | Guardar metadatos solo tras correlación `.OUT[.env]` con encabezado en claro; conservar ante fallo posterior. | **CÓDIGO F2.3** |
| DEC-029 | Migraciones aditivas PostgreSQL/SQL Server, nullable histórico, `FileNameDate` UTC ACHCOL. | **CÓDIGO F2.3, DB NO EJECUTADA** |
| DEC-030 | Exponer en DTOs opcionales; no habilitar SOAP ni modificar cola durante F2.3. | **CÓDIGO F2.3** |

**ADR con alternativas, constraints y compatibilidad:** [`F0_3_ADR_ARCHITECTURE.md`](F0_3_ADR_ARCHITECTURE.md). Las decisiones están adoptadas **como arquitectura**, no implementadas ni probadas.

### Decisiones F6.2 — 2026-10-25

- **DEC-081:** Reutilizar resumen existente con propiedad aditiva `achColFileBased`, no otro servicio de monitoreo.
- **DEC-082:** Solo conteos de seguridad ACHCOL, sin PII ni cuerpo monetario.
- **DEC-083:** KPI por ventana no sustituye revisión all-time de base antes de go/no-go.
- **DEC-084:** Rollback primario flag OFF, sin reenvíos, `Down` automáticos ni pérdida de auditoría.
- **DEC-085:** No certificar F6.2 hasta compilar/ejecutar pruebas; continuar F6.3 y mantener homologación externa bloqueada.

## 4.1 Decisiones F3.1 (2026-10-24)

| ID | Decisión | Estado |
| --- | --- | --- |
| DEC-031 | Cola existente con `SourceKind` (`CycleBound` por defecto, `AchColFileBased` como nueva ruta) | Código F3.1; PostgreSQL no ejecutado |
| DEC-032 | `EntryDetailId` FK opcional y obligatoria para fuente NACHA por check constraint; transacción/ciclo opcionales en esa ruta | Código F3.1 |
| DEC-033 | PostgreSQL expand-first y rollback protegido frente a filas incompatibles | Migración F3.1, no aplicada en DB |
| DEC-034 | Worker legado se limita temporalmente a `CycleBound` hasta F4; sin despacho accidental de nueva ruta | Código F3.1 |
| DEC-035 | DTO real usa nulabilidad, sin IDs ficticios | Código F3.1 |
| DEC-036 | SQL Server replica el modelo lógico de cola PostgreSQL F3.1 con tipos `nvarchar`/`int`, FK Restrict, check, índice y nulabilidad | Código F3.2; no ejecutado |
| DEC-037 | Default SQL Server `CycleBound` preserva filas existentes; se conserva índice único de idempotencia sin sustituir ciclo | Código F3.2 |
| DEC-038 | `Down` SQL Server falla antes de perder filas NACHA o reducir nulabilidad si existe contenido incompatible (`THROW 51032`) | Código F3.2; requiere test de BD |
| DEC-039 | Snapshot EF SQL Server representa nuevo modelo; expresión equivalente con cita SQL Server por corchetes en migración | Código F3.2; revisar en F6 |
| DEC-040 | F3.2 no activa planner, worker ni envíos SOAP; próxima tarea F3.3 | Scope acordado |

**Paridad de fuente implementada F3.2:** PostgreSQL y SQL Server cuentan con migraciones y snapshots de cola; la compatibilidad **no está certificada** hasta ejecutar ambas migraciones y pruebas en F6. No habilitar envío ACHCOL prematuro.

### Decisiones F4.2 — 2026-10-25

| ID | Decisión | Evidencia |
| --- | --- | --- |
| DEC-052 | No repetir F4.1, continuar con `next_microtask=F4.2` registrado. | `F4_2_IMPLEMENTATION.md` |
| DEC-053 | Stage NACHA file-based desacoplado de worker monetario CycleBound. | `IncomingNachaFileBasedPreparationStage.cs` |
| DEC-054 | Revalidar contexto, código monetario, cámara e idempotencia SHA-256 de ingreso. | F4.1 policy + stage |
| DEC-055 | Solo transiciones `Blocked` a motivos F4.2; jamás `Queued`, `RetryPending` o SOAP en este incremento. | Pipeline + stage |
| DEC-056 | Evitar reprocesar el mismo bloqueo; concurrencia multinstancia pendiente F6. | Filtro de código inicial y tests |

| DEC-057 | Reusar resiliencia/estados existentes, sin duplicar motor o tablas. | `F4_3_IMPLEMENTATION.md` |
| DEC-058 | Prohibir liberación/reintento ACHCOL en servidor y ocultar opciones en Command Center. | OperationsService + QueryService |
| DEC-059 | No cerrar como fallo definitivo una operación ACHCOL con intentos CFA sin conciliación. | Guard `AttemptCount`/ejecuciones |
| DEC-060 | Simulación: preflight F4.2 local sin SOAP aun con modo `Live` legado. | Tests F4.3 |

## 5. Fases y tablero de avance

| Fase | Propósito | Estado | Microtareas |
| --- | --- | --- | --- |
| **F0** | Línea base, contrato y ADR | **`VERIFIED` documental/estático** (F0.1, F0.2 y F0.3); **sin pruebas funcionales** | F0.1–F0.3 |
| **F1** | Identificadores y parser de nombre ACHCOL | **`IMPLEMENTED_UNVERIFIED`** (F1.1/F1.2, .NET no ejecutado) | F1.1–F1.2 |
| **F2** | Ingesta, resolución y persistencia sin ciclo | **`IMPLEMENTED_UNVERIFIED`** (F2.1/F2.2/F2.3 código entregado; .NET y DB pendientes) | F2.1–F2.3 |
| **F3** | Modelo relacional, cola e idempotencia | `IMPLEMENTED_UNVERIFIED` (F3.1–F3.3 en código, pruebas de ambiente pendientes) | F3.1–F3.3 |
| **F4** | Elegibilidad, planner y orquestación | `IMPLEMENTED_UNVERIFIED` (F4.1–F4.3 en código; no se ejecutaron pruebas .NET) | F4.1–F4.3 |
| **F5** | Mapeo SOAP, DryRun y preparación CFA | `IMPLEMENTED_UNVERIFIED` (F5.1 inventario, F5.2 DryRun, F5.3 harness local; XSD/homologación/Live pendientes F6) | F5.1–F5.3 |
| **F6** | Regresión, observabilidad, rollout y cierre | `IMPLEMENTED_UNVERIFIED` (F6.3 documenta QA offline; build/xUnit/BD/CFA/UAT y homologación aún pendientes de evidencia externa) | F6.1–F6.3 |

**Política del tablero:** `IMPLEMENTED_UNVERIFIED` indica código entregado, no certificado; **no detiene el avance**. Registrar deuda de pruebas para F6. La validación CFA Live/UAT se difiere hasta que terminen los desarrollos y exista ambiente autorizado; no convierte otras microtareas en `BLOCKED`.

## 6. Backlog de microincrementos ordenados

> Para ejecutar cualquiera de estas tareas: primero registrar baseline/working tree y finalizar con bitácora + próxima tarea. **No saltar a F1 mientras F0.1–F0.3 no tengan salida documentada.**

| ID | Alcance máximo de una sesión | Criterio de salida |
| --- | --- | --- |
| **F0.1** | Verificar checkout de Git, huellas, documentos y diferencias con ZIP; enumerar dependencias bloqueantes. **Sin cambios funcionales.** | Baseline SHA, rama, dirty diff, alcance exacto; marcar bloqueos. |
| **F0.2** | Trazar ruta real `nombre → encabezado → ingesta → admission → parse → cola → mapper → SOAP`; revisar mapeos vigentes. **Sin código funcional.** | Diagrama/tabla con archivos, métodos y contratos, evidencia de condiciones de bloqueo. |
| **F0.3** | ADR sobre modelo `ExternalCycleNumber`, exclusividad ACHCOL, cola, fechas, naming y estrategia de reversión; especificar tests/redes. | Decisiones aprobadas, abiertos identificados, matriz de dependencias con programa CFAUX. |
| **F1.1** | **IMPLEMENTED_UNVERIFIED.** `AchColInboundFileNameParser` separa `ZZZ`, fecha, `N`, original/canónico y `PlainNacha/DigitalEnvelope`; sin límite 7 y sin enlace a `AchCycle`. **No ejecuta cifrado, firma ni integración aún.** | 31 casos xUnit **escritos, no ejecutados**; compilación pendiente por falta de SDK .NET 10. Ver `F1_1_IMPLEMENTATION.md`. |
| **F1.2** | **IMPLEMENTED_UNVERIFIED**: correlación de encabezado y ZZZ posterior a descifrado; descubren `.OUT` en Managed Folder/SFTP; conservación `.OUT.env` y `.RET.env` y `ZZZ != N`. | Tests añadidos sin ejecutar (SDK .NET 10 ausente). Informe `F1_2_IMPLEMENTATION.md`. |
| **F2.1** | **IMPLEMENTED_UNVERIFIED:** resolver ACHCOL con `ResueltoSinCiclo`, `AchCycleId=null`, flag desactivado por defecto; CENIT/RET y legacy preservados. | Nuevos tests xUnit agregados sin ejecución; `F2_1_IMPLEMENTATION.md`. |
| **F2.2** | **IMPLEMENTED_UNVERIFIED:** admisión ACHCOL basada en archivo, sin `AchCycle`; control opt-in, cámara activa, correlación, calendario y fecha efectiva de ocho dígitos. | Casos H/I/.10 y negativos añadidos a xUnit, no ejecutados (SDK .NET 10). `F2_2_IMPLEMENTATION.md`. |
| **F2.3** | **IMPLEMENTED_UNVERIFIED:** cinco campos de contexto ACHCOL persistidos desde correlación tipo-1, modelos/DTO y migraciones en ambos proveedores; `AchCycleId` nullable sin inventar ciclos. | 6 escenarios xUnit/SQLite escritos, no ejecutados. Ver `F2_3_IMPLEMENTATION.md`. |
| **F3.1** | **IMPLEMENTED_UNVERIFIED:** cola única con SourceKind/EntryDetailId y AchTransactionId?/AchCycleId?, migración PostgreSQL y protección fail-closed legacy. | Tests de modelo/SQLite escritos, sin ejecutar (SDK .NET 10 no disponible); ver `F3_1_IMPLEMENTATION.md`. |
| **F3.2** | **IMPLEMENTED_UNVERIFIED:** migración SQL Server y snapshot alineados con PostgreSQL F3.1 sin ciclos ficticios. | 3 pruebas xUnit de operaciones EF/esquema y rollback escritas, no ejecutadas; ver `F3_2_IMPLEMENTATION.md`. |
| **F3.3** | **IMPLEMENTED_UNVERIFIED:** planner `AchColFileBased`, SHA-256 de identidad de negocio y cola segura `Blocked`; link opcional con migraciones en ambas BD. | Tests de planner/SQLite escritos, no ejecutados; índice UNIQUE preservado; pruebas multiworker y CFA reservadas F6. `F3_3_IMPLEMENTATION.md`. |
| **F4.1** | **IMPLEMENTED_UNVERIFIED:** política ACHCOL por cámara/ingesta/identidad, clase y allowlist monetaria; sin referencia a `AchCycle`. | Pruebas xUnit escritas sin ejecución; filas `Blocked` hasta F4.2/F5; ver `F4_1_IMPLEMENTATION.md`. |
| **F4.2** | **IMPLEMENTED_UNVERIFIED:** prevalidación desde worker/orquestador de filas ACHCOL sin ciclo, prioridad y revalidación de integridad; mantiene `Blocked`. | Tests xUnit escritos sin ejecución; no SOAP ni transmisión Live. `F4_2_IMPLEMENTATION.md`. |
| **F4.3** | **IMPLEMENTED_UNVERIFIED:** guard server-side contra retry/unblock/requeue ACHCOL, bloqueo de cierre tras cualquier intento CFA, UI sin acciones inseguras, auditoría de rechazos y prueba de preflight Live sin SOAP. | Pruebas xUnit escritas sin ejecutar; sin motor nuevo ni migración. `F4_3_IMPLEMENTATION.md`. |
| **F5.1** | Homologar contrato/mappings de `Proc_Transacciones` para origen NACHA + `ExternalCycleNumber` cuando corresponda. | Mapa campo→fuente completo; sin reemplazar `cycle.id` por `N` sin contrato. |
| **F5.2** | Adaptar mapper y ejecutar `DryRun` con H/I: payload SOAP, trazabilidad y resultado local. | Payload correcto; **0** transmisiones Live. |
| **F5.3** | **IMPLEMENTED_UNVERIFIED:** harness local de forma SOAP DryRun y evidencia sin valores sensibles; reglas de habilitación documentadas, no activa Live. | Pruebas xUnit escritas no ejecutadas; XSD/homologación/BD/CFA pendientes F6. `F5_3_IMPLEMENTATION.md`. |
| **F6.1** | **IMPLEMENTED_UNVERIFIED**: auditoría offline F1–F5, solución 7/7, guard XML corregido y tests escritos; sin SDK .NET 10.0.402. Compilación, xUnit, concurrencia y migraciones ambas BD **NO EJECUTADAS**. | Evidencia `F6_1_IMPLEMENTATION.md`; 12/12 checks estáticos. R5C histórico falla y R7 no concluyó. Deuda de ejecución abierta. |
| **F6.2** | Observabilidad, documentación operacional, feature flag, rollout y procedimiento de rollback. | `IMPLEMENTED_UNVERIFIED`: API existente con KPIs de seguridad ACHCOL, tests escritos, runbook sin habilitar Live. Ver F6_2_IMPLEMENTATION.md. |
| **F6.3** | **IMPLEMENTED_UNVERIFIED**: auditoría offline y expediente de certificación; prueba real/UAT/contrato CFA pendiente. | `F6_3_VERIFICATION_REPORT.md`; no hay .NET/BD/CFA ni aprobación Live; certificación queda abierta como gate externo, sin ocultar deuda R5C/R7. |

## 7. Mapa de código impactado (a verificar en F0.2)

- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaCycleResolver.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaAdmissionValidator.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaIngestionAppService.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaDispatchEligibilityPolicy.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaDispatchPlanner.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaPostProcessingPipeline.cs`
- `src/Cfa.ACHInterbank.Domain/Models/ACH/IncomingNachaIngestion.cs`
- `src/Cfa.ACHInterbank.Domain/Models/ACH/IncomingNachaDispatchModels.cs`
- `src/Cfa.ACHInterbank.Persistence/Configuration/IncomingNachaDispatchQueueConfiguration.cs`
- `src/Cfa.ACHInterbank.Persistence/.../ProcTransaccionesRequestMapper.cs` (**confirmar ruta**).
- `ExternalFileNameSupport.cs`, `ExternalFileNameBuilder.cs`, `NachaParserService.cs` (**confirmar rutas**).
- Migraciones EF Core bajo `Postgres` y `SqlServer` y pruebas `IncomingNacha*Tests.cs`.
- Integraciones y su programa de memory state CFAUX: confirmar solapamiento antes de tocar contrato y mapper SOAP.

## 8. Matriz de aceptación y certificación

| Grupo | Casos del plan | Estado | Evidencia |
| --- | --- | --- | --- |
| H/I, sin ciclos configurados | AT-01, 02, 03, 16, 18 | `NOT_STARTED` | — |
| Independencia letra / ZZZ / sufijo N | AT-05, 06, 17 | `NOT_STARTED` | — |
| Ruta tradicional y CENIT | AT-04, 15 | `NOT_STARTED` | — |
| Seguridad, formato, elegibilidad | AT-07, 08, 09, 10 | `NOT_STARTED` | — |
| DryRun, SOAP, Live | AT-11, 12, 19, 20 | `NOT_STARTED` | — |
| Reintentos inciertos / concurrencia | AT-13, 14 | `NOT_STARTED` | — |

**Quality gates necesarios:** análisis estático y tests focales → build backend .NET → migrations PostgreSQL/SQL Server → tests de integración/contención → `DryRun` → UAT CFA autorizado → autorización Live. Evidencia individual por gate en el historial; **no se ha ejecutado ninguno para esta iniciativa**.

## 9. Riesgos y bloqueos

| ID | Riesgo / bloqueo | Estado | Mitigación |
| --- | --- | --- | --- |
| BLK-001 | Acoplamiento de modelos/cola/mapper a `AchCycle` obligatorio | **DISEÑO DEFINIDO; IMPLEMENTACIÓN PENDIENTE** | `F0_3_ADR_ARCHITECTURE.md`; adaptar consumidor, relaciones y migraciones F2–F5. |
| BLK-002 | El contrato CFA puede requerir datos actualmente leídos de `cycle.*` | ABIERTO | Inventariar XSD y mappings activos; homologar campo a campo antes de Live. |
| BLK-003 | Manual V35 y aceptación de sufijo `.8/.9` en intercambio **directo ACH Colombia** no están homologados entre sí | ABIERTO | Separar alcance interno CFA de canal externo; obtener aprobación de receptor. |
| BLK-004 | En este entorno `dotnet` y `docker` no estaban disponibles | **DEUDA DE VERIFICACIÓN F6 (NO BLOQUEA DESARROLLO)** | Seguir implementando F1–F5; ejecutar gates finales en runner con SDK/bases. |
| BLK-005 | Otro programa CFA SOAP UX/Mapping está en curso, con estado independiente | ABIERTA DEPENDENCIA | Detectar conflictos de contrato/mapping/archivos antes de merge. |
| BLK-006 | El ZIP incluye rama `ACH-Interbank` y `HEAD` `0b4ce1c8`; difiere de la referencia histórica `ACH-Interbank-Postgresql`. | EVIDENCIADO EN F0.1 | Tomar el ZIP entregado como snapshot, no como checkout limpio; conciliar rama de integración antes de modificar código productivo. |
| BLK-007 | `git status` del ZIP extraído: 2.911 modificaciones por CRLF y 7 archivos rastreados ausentes; posible impacto de compilación. | ABIERTO — NO RESTAURADO | Comparar los 7 archivos con uso en código y compilar en runner; no restaurar automáticamente. |

## 10. Historial incremental / bitácora

### SES-000 — 2026-10-24 — Preparación documental (sin cambios en aplicación)

- **Microtarea:** preparación del `MEMORY_STATE` y guía de reanudación; **no cuenta** como F0.1.
- **Fuente:** ZIP `ACHInterbank_ajustado.zip` y `PLAN.md` suministrados en conversación.
- **Cambios:** documentación de implementación incremental, sin edición de archivos `.cs`, `.ts`, SQL, migraciones ni configuración runtime.
- **Comprobación:** verificación de existencia de los documentos y lectura estática puntual del ZIP; `.NET/Docker` no disponibles en esta sesión; ninguna prueba de aplicación ejecutada.
- **Resultado:** `NOT_STARTED` para la implementación.
- **Siguiente:** `F0.1`, línea base y dependencias. `active_microtask=null`.

### SES-001 — 2026-10-24 — F0.1 — Baseline del ZIP + integración documental

- **Baseline:** fuente `ACHInterbank_ajustado.zip` (SHA-256 `26da6cf2dffcf939aa539f8ea9f0169fa8164fb959eaf2eb3a7ae22651b6c890`); checkout extraído `ACH-Interbank`, `HEAD=0b4ce1c809ca5acfdafaf455cae4d0eaa5292f07`.
- **Estado de Git previo a editar docs:** 2.918 entradas (2.911 cambios CRLF/LF + 7 archivos seguidos ausentes); con `git -c core.autocrlf=true`, 7 eliminaciones; **NO** hay árbol limpio.
- **Objetivo acotado:** incorporar `PLAN.md`, `MEMORY_STATE.md`, `CHATGPT_MICRO_START.md` dentro del proyecto y preservar el snapshot fuente sin cambios de lógica.
- **Archivos agregados/modificados:** solo `docs/ACHCOL_CFA_FILE_BASED_2026-10-24/{PLAN.md,MEMORY_STATE.md,CHATGPT_MICRO_START.md,F0_1_BASELINE.md}` (y manifiesto de entrega de paquete). Sin modificaciones `.cs`, SQL, `.ts` ni migraciones.
- **Decisiones:** ZIP entregado es snapshot de referencia, aunque su Git interno no sea limpio; se documentan 7 archivos ausentes, sin reponerlos. No cambiar `MEMORY_STATE.md` del programa CFAUX concurrente.
- **Pruebas/comandos ejecutados:** `unzip -tq` PASS; `git rev-parse HEAD`, rama, `git status --porcelain`, `git -c core.autocrlf=true status`, listado de archivos clave, `sha256sum` de fuentes clave. Evidencia detallada en `F0_1_BASELINE.md`.
- **Pruebas no ejecutadas:** build/tests .NET, DB, Docker y SOAP CFA; no hay `dotnet` ni `docker` disponibles en PATH. Tampoco hay despliegue ni commit nuevo.
- **Riesgos y rollback:** riesgo de siete archivos seguidos ausentes y diferencias EOL; preservar el ZIP original sin sobrescribir código. El nuevo paquete se revierte eliminando su carpeta documental si se decide no adoptarlo.
- **Estado final:** `VERIFIED` para F0.1 inventario / caracterización estática, **no** para compilación ni para capacidad funcional.
- **Siguiente microtarea:** `F0.2`. Antes del primer cambio productivo, evaluar impacto de las siete ausencias y reconciliar rama/build.

### SES-002 — 2026-10-24 — F0.2 — Trazado de código y política de continuidad

- **Alcance:** recorrido estático completo entre nombre/encabezado e integración SOAP CFA; evidencia en `F0_2_FLOW_TRACE.md`.
- **Baseline:** rama `ACH-Interbank`, `HEAD=0b4ce1c809ca5acfdafaf455cae4d0eaa5292f07`; conservadas las siete ausencias rastreadas documentadas en F0.1.
- **Código funcional:** sin modificaciones `.cs`, `.ts`, migraciones ni SQL en esta microtarea.
- **Hallazgos:** `ExternalFileNameSupport` acepta `.8/.9` solo en el perfil outward `NachaOut`; resolución/admisión ingresante y cola/mapper siguen atados a `AchCycle`; se confirma que es cambio transversal.
- **Decisión nueva `DEC-008` (APROBADA por solicitante):** ausencia de ambientes **no detiene** desarrollo; las pruebas finales de integración se concentran tras F1–F5, en F6; conservar evidencias locales cuando sea posible.
- **Decisión nueva `DEC-009` (APROBADA):** después de **cada** microimplementación, entregar **un ZIP con el proyecto completo modificado y documentación ajustada**; no parches parciales.
- **Verificación:** revisión estática de funciones, modelos y rutas, revisión del contrato del cliente SOAP; **sin pruebas de .NET, BD ni endpoint CFA**.
- **Estado:** `F0.2=VERIFIED` exclusivamente para inventario de dependencias; no certificación de implementación funcional.
- **Siguiente:** `F0.3` ADR, continuando a F1 sin esperar pruebas de ambiente.

### SES-003 — 2026-10-24 — F0.3 — Decisiones de arquitectura para ruta ACHCOL sin ciclo

- **Baseline:** el proyecto completo proviene de `ACHInterbank_ACHCOL_CFA_F0_2_UNIFICADO.zip`; el `.git/HEAD` interno continúa en `ACH-Interbank` / `0b4ce1c809ca5acfdafaf455cae4d0eaa5292f07`. Siete ausencias históricas y desviaciones EOL se preservan sin corregir; ver F0.1.
- **Alcance ejecutado:** revisión de `IncomingNachaFileIngestion`, `IncomingNachaDispatchQueue`, configuración EF, planner, resolver, policy, mapper y pipeline; adopción de ADR `F0_3_ADR_ARCHITECTURE.md` con alternativas y plan de migraciones/rollout.
- **Decisiones:** cola **única** con `SourceKind`, relación obligatoria `EntryDetail` en nuevo origen y `AchTransactionId?/AchCycleId?`; parser entrante específico, contexto inmutable, flag de cámara, idempotencia por origen/negocio, SOAP sin campos inventados, compatibilidad con CFAUX. DEC-001–DEC-015 actualizadas.
- **Modificaciones:** solo `docs/ACHCOL_CFA_FILE_BASED_2026-10-24/{F0_3_ADR_ARCHITECTURE.md,MEMORY_STATE.md,PLAN.md,CHATGPT_MICRO_START.md,README_PAQUETE_UNIFICADO.md}`; no hay cambios `.cs`, `.ts`, SQL, migraciones ni config runtime.
- **Verificación:** revisión estática, validación del estado/nexthop del Markdown y prueba de integridad/diferencias del ZIP tras empaquetar. **No hay compilación .NET ni pruebas de BD/SOAP/UAT; no se certificó F1–F6.**
- **Resultado:** `F0.3=VERIFIED` exclusivamente para arquitectura/documentación; F0 cerrada. `F1.1=NOT_STARTED` y es la siguiente microimplementación de código. La falta de ambientes **no** detiene la secuencia.

### Plantilla obligatoria para cada nueva sesión

```markdown
### SES-XXX — YYYY-MM-DD — F#.n — [título]
- Baseline: rama, `git rev-parse HEAD`, `git status --short`, snapshot hash si procede.
- Objetivo acotado:
- Archivos examinados:
- Archivos modificados (incluye migraciones):
- Decisiones tomadas / decisiones pendientes (IDs):
- Pruebas/comandos ejecutados y resultado literal:
- Pruebas no ejecutadas y motivo:
- Evidencia (logs, capturas, paths, test names, commits):
- Riesgos y rollback:
- Estado final de esta microtarea: NOT_STARTED | IN_PROGRESS | IMPLEMENTED_UNVERIFIED | VERIFIED | BLOCKED
- Siguiente microtarea / condición de desbloqueo:
```

### SES-004 — 2026-10-24 — Addendum F0.3.A: extensión física `.OUT` / `.OUT.env`

- **Solicitud:** usuario aclaró archivos ACH Colombia `.out` plano y `.out.env` cifrados en sobre digital; referencia a Sheet con Apps Script de otras conversaciones (no accesible directamente en esta revisión; no atribuirle detalles no verificables).
- **Fuente:** `ACHInterbank_ACHCOL_CFA_F0_3_UNIFICADO.zip` (workspace conservado). Revisados `NachaUploadController`, `IncomingNachaIngestionAppService`, `ManagedDigitalEnvelopeServiceTests`, `IncomingNachaIngestionAppServiceTests`, `AchColombiaManagedMftFolderAdapter`, `AchColombiaSftpMftAdapter`, `AchColombiaManagedMftImplementationSupport`.
- **Hallazgo:** descifrado `.env` ya implementado y cubierto por tests existentes; ingreso manual admite `.OUT`; **descubrimiento automático de `.OUT` plano ausente** en MFT/SFTP; ejemplos reales de código llevan `AAAAMMDD` antes de `N.OUT`.
- **Decisión:** ADR complementado con `F0_3_A_NAMING_OUT_ENVELOPE.md` (DEC-016) y reglas INV-011 a INV-013; 8 casos nuevos AT-21–AT-28. `F1.1` seguirá siendo siguiente tarea; no se declara implementado.
- **Archivos modificados:** únicamente docs `PLAN.md`, `MEMORY_STATE.md`, `CHATGPT_MICRO_START.md`, `F0_3_ADR_ARCHITECTURE.md`, `README_PAQUETE_UNIFICADO.md`, más nuevo `F0_3_A_NAMING_OUT_ENVELOPE.md`. **Sin cambios de código .NET, tests, SQL ni despliegues**.
- **Validación:** revisión estática de código y tests; empaquetado íntegro verificado; **no se ejecutaron** builds ni tests ni conexión CFA. Las pruebas finales permanecen F6.
- **Siguiente:** `F1.1` parser físico / clasificación plain-cipher. No detener por falta de ambiente.

### SES-005 — 2026-10-24 — F1.1: parser físico ACHCOL aislado

- **Fuente exacta:** `ACHInterbank_ACHCOL_CFA_F0_3_A_UNIFICADO.zip` (snapshot preservado), no `ACHInterbank_ajustado.zip` original ni un checkout actualizado por red.
- **Código nuevo:** `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ExternalFileNames/AchColInboundFileNameParser.cs` — parser estricto del formato físico `RRRRTTT.ZZZ.yyyyMMdd.N.OUT[.env]`, con códigos de error estables y validaciones de longitud, ASCII numérico, fechas reales, secuencia 001–036, ciclo externo positivo sin límite 7, protección plain/cifrado, nombre original y canónico. **No incorpora** `AchCycleId` ni interpreta `ZZZ` como `N`.
- **Pruebas agregadas:** `tests/Cfa.ACHInterbank.Tests/AchColInboundFileNameParserTests.cs`, **29 filas InlineData + 2 Facts = 31 casos escritos** para H/008/.8, I/009/.9, H/008/.10, límites, errores, traversal y separación CENIT. **NO EJECUTADAS:** SDK .NET 10 ausente en esta sesión; pruebas finales de integración en F6.
- **Compatibilidad:** sin cambios a la ingesta actual, resolución de ciclos, `CenitOfficialFileNameParser`, RET, descifrado, MFT/SFTP, base de datos o SOAP; el parser se integra en F1.2. Esto evita activar parcialmente una nueva ruta monetaria.
- **Decisión DEC-017:** no recortar rutas, no aceptar sufijos `.env` incompletos, ni adivinar tipo/fecha/ciclo; sólo cadenas de nombre físico controladas. No imponer validación de header aquí (F1.2), ni `AchCycleId` (F2).
- **Estado:** `F1.1=IMPLEMENTED_UNVERIFIED`, `F1.2=NOT_STARTED`; el desarrollo continúa sin esperar UAT/ambientes. Detalle en `F1_1_IMPLEMENTATION.md`.

## 11. Próxima ejecución — `CERTIFICATION_GATE_PENDING`

**F6.3 entregada como expediente QA offline, NO certificada.** Al disponer de un runner .NET 10.0.402 y bases de datos aisladas, ejecutar las pruebas compiladas y los ensayos de migración/concurrencia, y adjuntar artefactos de CI; obtener XSD oficiales y homologación CFA antes de UAT y eventual autorización Live. Véase `F6_3_VERIFICATION_REPORT.md`. La deuda R5C/R7 permanece abierta.

1. Revisar mapper, contrato WSDL suministrado y mapping dinámico CFAUX, incluyendo valores que antes dependían de `AchTransaction`/`AchCycle`.
2. Elaborar matriz campo SOAP → fuente NACHA, tipo y validación; marcar sin homologar lo que no tenga evidencia.
3. Conservar `Blocked`, resultado `CanTransmit=false` y cero envíos Live; verificar contract readiness y usar harness únicamente local.
4. Entregar ZIP completo con plan/memory/decisiones y siguiente tarea.

## 12. Prompt breve para reanudar sin pérdida de contexto

> Trabajaremos en el programa `ACHCOL-NACHA-CFA-EXTCYCLE-001` de ACHINTERBANK. Lee `docs/ACHCOL_CFA_FILE_BASED_2026-10-24/PLAN.md`, `MEMORY_STATE.md` y `CHATGPT_MICRO_START.md`. Ejecuta la microtarea `next_microtask`, regístrala y continúa al siguiente incremento cuando sea viable. No detengas desarrollo por ausencia de entorno de pruebas; registra pruebas diferidas para F6. Entrega siempre el ZIP completo y modificado. Mantén separados `FileIdModifier`, `FileSequence`, `ExternalCycleNumber` y `AchCycleId`. Nunca bloquees archivos ACHCOL elegibles únicamente porque `.8/.9` no están parametrizados; conserva validaciones, idempotencia y seguridad, sin habilitar Live no autorizado. Al final actualiza `MEMORY_STATE.md` con evidencia real y el siguiente paso. No alteres el `MEMORY_STATE.md` del programa CFA-SOAP-UX-MAP-001.

### SES-006 — 2026-10-24 — F1.2: ingesta física y correlación ACHCOL

- **Baseline:** ZIP completo `ACHInterbank_ACHCOL_CFA_F1_1_UNIFICADO.zip`; rama Git y HEAD preservados; sin `git commit` nuevo.
- **Código nuevo:** `ExternalFileNames/AchColInboundHeaderCorrelation.cs` compara nombre físico `RRRRTTT.ZZZ.yyyyMMdd.N.OUT[.env]` con nombre canónico post-descifrado, fecha de registro tipo 1 y caracter identificador (posición 36); `A..Z,0..9` correlacionado con ZZZ 001–036. No fuerza `ZZZ=N` ni `N=AchCycleId`.
- **Ingesta:** `IncomingNachaIngestionAppService.cs` ejecuta correlación después de resolver cámara y descifrar, pero antes del rechazo por ausencia de ciclo. Si falla, persiste resultado/causa y no llega a parser transaccional. Si valida, persiste evento `AchColInboundHeaderCorrelated` con trazabilidad. No cambia comportamiento CENIT ni proceso RET.
- **Transportes:** Managed Folder (`AchColombiaManagedMftFolderAdapter`) y SFTP (`AchColombiaSftpMftAdapter`) ahora descubren `.OUT` plano adicionalmente a `.OUT.env` y `.RET.env`, conservando claim exclusivo, recuperación, hash y archivado. `AchColombiaManagedMftImplementationSupport` y `AchColombiaManagedMftInboundWorkflow` mantienen compuerta de extensiones. `.env` se descifra solo mediante servicio existente; `.OUT` se procesa sin invocarlo.
- **Pruebas definidas (no ejecutadas):** nuevo `AchColInboundHeaderCorrelationTests.cs`, `AchColombiaManagedMftInboundNameTests.cs` y pruebas ampliadas en `AchColombiaManagedMftFolderAdapterTests.cs` y `AchColombiaSftpMftAdapterTests.cs`, incluyendo recuperación de claim `.OUT`.
- **Sin cambios:** `AchCycle`, esquema/EF/migraciones, SOAP CFA, mapeos, CENIT, RET y feature flag. No se habilita Live.
- **Verificación realizada:** revisión estática de diferencias y gramática, comparación de integridad ZIP; **`dotnet build/test`, BD, CFA y UAT NO ejecutados** porque SDK .NET 10 y ambientes faltan. Este estado no certifica el flujo E2E.
- **Límite deliberado:** el resolver/admission aún bloquea cuando falta `AchCycleId`; se atiende en F2.1/F2.2. La aceptación de sufijos N arbitrarios en el canal directo ACH Colombia requiere homologación antes de Live.
- **Estado:** `F1.2=IMPLEMENTED_UNVERIFIED`; siguiente `F2.1`.

### SES-007 — 2026-10-24 — F2.1: resolución ACHCOL con cámara/fecha y ciclo interno opcional

- **Fuente exacta:** `ACHInterbank_ACHCOL_CFA_F1_2_UNIFICADO.zip` extraído, preservado; historial Git heredado (rama `ACH-Interbank` / HEAD `0b4ce1c809ca5acfdafaf455cae4d0eaa5292f07`); siete ausencias históricas no corregidas.
- **Código:** resolver con flag `AchCol:AllowCycleIndependentInboundDispatch=true` únicamente para `ACHCOL` y nombre canónico `*.OUT` válido; contexto `AchColFileBased`, estado `ResueltoSinCiclo=7`, `AchCycleId=null`; no se consulta `AchCycles` en nueva ruta; fecha y hora tipo-1 inválidas no se admiten. Sin alterar enum numérico histórico.
- **Tests:** se añadieron casos unitarios H/008/.8, I/009/.9, H/008/.10, sin ciclos, con un ciclo existente, flag OFF, RET, fecha y hora inválidas. **No ejecutados** por ausencia del SDK .NET 10. Ver `F2_1_IMPLEMENTATION.md`.
- **Decisiones:** DEC-018 a DEC-021; flag opt-in **OFF por defecto** para proteger despacho monetario mientras F2.2–F5 quedan incompletas. No crear ciclos ficticios ni combinar ZZZ y N.
- **No cambia:** validación/admisión (todavía bloquea `CYCLE_NOT_RESOLVED`), parser, Managed MFT/SFTP, SQL/EF, cola, dispatcher, mapeo ni SOAP. No habilitar Live.
- **Pruebas finales diferidas:** F6 después de F1–F5; pruebas estáticas e integridad de ZIP no equivalen a `dotnet test`.
- **Resultado:** `F2.1=IMPLEMENTED_UNVERIFIED`; **siguiente `F2.2`**. No bloquear el avance por falta de ambiente.


### SES-008 — 2026-10-24 — F2.2: admisión ACHCOL sin ciclo interno

- **Baseline:** ZIP completo `ACHInterbank_ACHCOL_CFA_F2_1_UNIFICADO.zip`; rama `ACH-Interbank`, HEAD `0b4ce1c809ca5acfdafaf455cae4d0eaa5292f07`; snapshot previo conservado.
- **Código modificado:** `IncomingNachaAdmissionValidator.cs`: nueva admisión opt-in solo para estado/rail ACHCOL `ResueltoSinCiclo`, `ResolutionMode=AchColFileBased`, `AchCycleId=null` y resolución inequívoca; cámara activa y válida, fecha de cabecera y efectiva, día hábil, hora y validación física `.OUT` post-descifrado. Sin consulta `AchCycles` en nueva rama; legacy intacto.
- **Pruebas agregadas:** `IncomingNachaAdmissionAchColFileBasedTests.cs` con casos H/008/.8, I/009/.9, H/008/.10, sin filas de `AchCycle`, negativas de flag, estado, fecha, feriado, header, cámara y tipo de fichero. Tests **no ejecutados** por SDK .NET 10 ausente.
- **Decisiones:** DEC-022–DEC-025; no crear ciclos ficticios ni convertir `ZZZ` en `N` o FK; `CycleNumber` de preview en ACHCOL es `N` externo.
- **No cambia:** modelos/SQL/EF, cola, dispatcher, SOAP, cifrado, MFT/SFTP, CENIT ni RET. La admisión no autoriza pagos. Flag permanece OFF por defecto.
- **Verificaciones:** revisión estática de cambios y conservación ZIP; compilación y xUnit pendientes, integración CFA y UAT reservadas F6. No se afirma resultado de ambiente ni certificación.
- **Estado:** `F2.2=IMPLEMENTED_UNVERIFIED`, próximo `F2.3` persistencia y trazabilidad de contexto sin ciclo.

### SES-009 — 2026-10-24 — F2.3: persistencia tipada de contexto ACHCOL

- **Baseline:** ZIP completo `ACHInterbank_ACHCOL_CFA_F2_2_UNIFICADO.zip`; rama Git/HEAD heredados. No se modificó el programa CFAUX paralelo.
- **Código:** metadatos persistidos en ingesta (`CanonicalPlainFileName`, `FileIdModifier`, `FileSequence`, `ExternalCycleNumber`, `FileProtectionKind`); `FileName` original y `FileNameDate` existentes. Materialización únicamente después de correlacionar nombre con tipo 1 post-descifrado, guardada aun si hay bloqueo posterior. No inferir `AchCycleId` ni crear ciclos.
- **Base de datos:** migraciones aditivas y snapshots actualizados para PostgreSQL y SQL Server. Columnas nuevas opcionales; registros históricos y otras cámaras preservados sin backfill. La migración queda por ejecutar/certificar en F6.
- **API:** respuesta de ingesta y detalle consultable incluyen campos opcionales. Respuestas por duplicado preservan contexto.
- **Tests:** 6 escenarios escritos para roundtrip SQLite/validación, **no ejecutados** por falta de .NET 10. No se ejecutaron builds, migraciones DB ni SOAP/UAT. Ver `F2_3_IMPLEMENTATION.md`.
- **Decisiones:** DEC-026 a DEC-030. Sin cambios en cifrado, interfaces de CFA, cola, idempotencia ni feature flag.
- **Estado:** `F2.3=IMPLEMENTED_UNVERIFIED`; siguiente `F3.1` ampliar cola de origen NACHA en Postgres. No repetir migración de ingesta F2.3.

## SES-010 — F3.1 (2026-10-24)

- **Base:** `ACHInterbank_ACHCOL_CFA_F2_3_UNIFICADO.zip` (ZIP completo suministrado).
- **Entrega:** `ACHInterbank_ACHCOL_CFA_F3_1_UNIFICADO.zip` con código fuente íntegro, migración PostgreSQL, pruebas y documentación.
- **Código:** `IncomingNachaDispatchQueue.SourceKind`, `EntryDetailId`, `AchTransactionId?`, `AchCycleId?`; modelo EF + check por origen + FK Restrict + índice; default CycleBound para datos históricos.
- **Compatibilidad:** worker de despacho existente procesa solo origen CycleBound; planner legacy declara origen explicitamente. DTOs admiten nulos. SQL Server pendiente F3.2.
- **Evidencia:** inspección estructural/consistencia de archivos y ZIP; tests SQLite escritos **sin ejecutar**, sin SDK .NET 10, sin conexión BD y sin SOAP CFA.
- **Decisiones:** DEC-031–DEC-035, con detalle en `F3_1_IMPLEMENTATION.md`. No habilitar Live.
- **Estado:** `F3.1=IMPLEMENTED_UNVERIFIED`, próximo `F3.2` (paridad SQL Server).

### SES-011 — 2026-10-24 — F3.2: paridad SQL Server de cola NACHA-M

- **Baseline:** `ACHInterbank_ACHCOL_CFA_F3_1_UNIFICADO.zip`, 8.100 entradas. Rama/HEAD Git heredados del ZIP, sin checkout funcional o commit nuevo; no interpretar su estado como limpio.
- **Cambio C#:** migración SQL Server `20261024224500_AchColFileBasedDispatchQueueF32.cs`, `SourceKind` con default `CycleBound`, `EntryDetailId` opcional con índice y FK Restrict, `AchTransactionId?` y `AchCycleId?`, shape check SQL Server por origen. Snapshot SQL Server sincronizado, índice único `IdempotencyDispatchKey` preservado.
- **Seguridad:** `Down` SQL Server con `THROW 51032` previo a perder columnas/reducir nulabilidad si hay filas de origen NACHA o legacy incompletas. Sin cambios a SOAP, planificador, feature flag, cifrado o migración PostgreSQL.
- **Pruebas:** 3 casos xUnit escritos para paridad lógica de operaciones EF Core PostgreSQL/SQL Server, guardia de rollback y registro; **no ejecutados** (.NET 10 no disponible). Migración SQL Server no ejecutada sobre BD.
- **Evidencia:** inspección de código, snapshot, operaciones de migración y preservación del ZIP; sin ambiente de bases/CFA. Ver `F3_2_IMPLEMENTATION.md`.
- **Decisiones:** DEC-036–DEC-040. **Estado F3.2:** `IMPLEMENTED_UNVERIFIED`. **Siguiente:** F3.3 idempotencia y planificación NACHA sin ciclo interno; pruebas finales F6.


### SES-012 — 2026-10-24 — F3.3: planner sin ciclo, identidad de negocio, cola segura

- **Base:** `ACHInterbank_ACHCOL_CFA_F3_2_UNIFICADO.zip`; rama del `.git` incluida `ACH-Interbank`, referencia `0b4ce1c809ca5acfdafaf455cae4d0eaa5292f07` (snapshot del ZIP, no commit nuevo). Divergencias heredadas documentadas F0.1. Ningún commit Git creado en esta sesión.
- **Código:** `IncomingNachaDispatchPlanner` bifurca explícitamente `AchColFileBased` tras validar cámara activa/fecha/flag; crea filas **Blocked** sin ciclo, transacción ni link; `IncomingNachaFileBasedDispatchIdentity` computa clave de negocio SHA-256 sin ciclo ni GUID ingesta; `IncomingNachaPostParseProcessor` no marca los créditos/débitos ACHCOL como linking fallido por ausencia de transacción interna.
- **DB:** `IncomingNachaTransactionLinkId` opcional, CHECK por origen, migraciones F3.3 y snapshots ambos motores, con Down protegido. Índice UNIQUE de idempotencia intacto.
- **Tests:** suites `IncomingNachaFileBasedDispatchPlannerF33Tests.cs` y `IncomingNachaFileBasedDispatchSchemaF33Tests.cs` agregadas (sin ejecutar SDK .NET). Revisión estática/ZIP verificada al empaquetar; no declarar ejecución de migraciones/Live.
- **Riesgo:** manejo de carreras entre conexiones sigue protegido por índice UNIQUE que podría lanzar excepción por colisión; se requiere validación con bases reales y reconciliación F4/F6. No se habilitó worker de ACHCOL ni `Proc_Transacciones`.
- **Decisiones:** DEC-041–DEC-046; informe `F3_3_IMPLEMENTATION.md`.
- **Estado:** `F3.3=IMPLEMENTED_UNVERIFIED`; siguiente `F4.1=NOT_STARTED`; pruebas finales acumuladas a F6 sin bloquear desarrollo.

### SES-013 — 2026-10-24 — F4.1 elegibilidad por entrada ACHCOL

- **Base:** ZIP completo F3.3; rama/commit heredados como snapshot, sin `git commit` nuevo.
- **Código:** nuevo contrato `EvaluateFileBasedAsync` en interfaz existente, política ACHCOL fail-closed por cámara, estado, nombre físico, consecutivo/ciclo independientes, encabezado/detalle, clasificación, códigos monetarios y huella de identidad. Planner aplica política antes de crear filas; solo `Blocked` y sin SOAP.
- **Tests:** `IncomingNachaFileBasedDispatchEligibilityF41Tests.cs` y regresiones añadidas a fixture F3.3; pruebas no ejecutadas por falta de .NET 10.
- **Decisiones:** DEC-047–DEC-051 en `F4_1_IMPLEMENTATION.md`. Sin migraciones ni cambios de endpoints. Pruebas finales/certificación F6.
- **Estado:** F4.1=`IMPLEMENTED_UNVERIFIED`; F4.2=`NOT_STARTED`. Fuente de continuidad `ACHInterbank_ACHCOL_CFA_F4_1_UNIFICADO.zip`.

### SES-014 — 2026-10-25 — F4.2 prevalidación desde worker sin liberar CFA

- **Base:** ZIP F4.1 completo; snapshot de rama Git `ACH-Interbank` y SHA `0b4ce1c8...` heredados; no se realizó commit Git ni se declaró árbol limpio.
- **Código:** etapa `IncomingNachaFileBasedPreparationStage`, integrada en pipeline R5D y activada solo por flag; selecciona/ordena candidatos ACHCOL F3.3, carga contexto NACHA en lote y vuelve a evaluar F4.1 e idempotencia antes de preparar una futura liberación.
- **Estados seguros:** `Blocked/ACHCOL_MAPPING_NOT_READY_F42` si válido, `Blocked/ACHCOL_PREFLIGHT_REJECTED_F42` si inválido; sin `NextAttemptAtUtc`, sin SOAP, sin `IncomingNachaIntegrationExecution`; worker tradicional permanece `CycleBound`.
- **Tests:** pruebas xUnit/InMemory añadidas H/I/.10, cifrado/claro, feature flag OFF, datos corrompidos, reejecución sin doble evento. NO ejecutadas sin SDK .NET; pruebas DB y multinstancia F6.
- **Persistencia:** sin migraciones nuevas; solamente estado de cola y eventos de auditoría. Cambios de F4.2 `IMPLEMENTED_UNVERIFIED`; próxima tarea `F4.3`.
- **Decisiones:** DEC-052–DEC-056, ver `F4_2_IMPLEMENTATION.md`.

### SES-017 — 2026-10-25 — F4.3: resiliencia mínima y barreras manuales

- **Base:** `ACHInterbank_ACHCOL_CFA_F4_2_UNIFICADO.zip`; raíz `ACHInterbank_ajustado/`, rama HEAD y metadatos `.git` preservados, sin commits nuevos. Paquete `ACHInterbank_ACHCOL_CFA_F4_3_UNIFICADO.zip`.
- **Códigos:** `IncomingNachaCommandCenterOperationsService.cs` rechaza retry/unblock/requeue para origen `AchColFileBased`, deja evento de transición rechazado y prohíbe cierre como `FailedFinal` cuando existan intentos/executions CFA; `IncomingNachaCommandCenterQueryService.cs` no ofrece esas acciones para ACHCOL. Ruta `CycleBound` inalterada.
- **Pruebas escritas sin ejecutar:** acciones manuales inseguras, presentación UI, cierre sin intento y bloqueo tras intento, preflight local con configuración SOAP `Live` y mock estricto sin invocación.
- **Sin nuevas tablas, migraciones, SOAP o feature flags.** Reusa eventos de auditoría; idempotencia multinstancia y conciliación final pendientes F6. No hubo envíos monetarios.
- **Estado:** `IMPLEMENTED_UNVERIFIED` por ausencia de .NET 10/BD/CFA. **Próxima:** `F5.1`, mapeo de contrato sin asumir campos; no habilitar envío antes de homologación F5 y pruebas F6. **Decisiones:** DEC-057–DEC-060. Informe `F4_3_IMPLEMENTATION.md`.


## 13. Incremento F5.1 — 2026-10-25

- **Estado:** `IMPLEMENTED_UNVERIFIED`: matriz de fuentes y guard compilables incluidos; ejecución .NET pendiente. `last_completed_microtask=F5.1`, `next_microtask=F5.2`.
- **Entregable:** `F5_1_IMPLEMENTATION.md`, 25 entradas técnico-contrato más dos salidas; estado DirectCandidate/Conditional/Unresolved por cada parámetro. El WSDL declara la operación pero importa esquemas XSD no incluidos: no se afirma homologación externa.
- **C# modificado:** `ProcTransaccionesRequestMapper.ResolveAsync` rechaza origen `AchColFileBased` en la firma legada dependiente de `AchTransaction`/`AchCycle` (código `PROC_TRANSACCIONES_ACHCOL_MAPPING_NOT_HOMOLOGATED_F51`). Comportamiento `CycleBound` preservado.
- **C# nuevo:** `AchColProcTransaccionesFieldSources.cs`, inventario estático simple sin tabla ni motor adicional. **Test nuevo:** `AchColProcTransaccionesFieldSourcesF51Tests.cs`.
- **Decisiones:** DEC-061–DEC-065. No mapear `ExternalCycleNumber` a `IDCAMCOMPE` o `cycle.id`, no permitir valores `SEED`/`PLACEHOLDER` como mapeos financieros; no tocar CFAUX.
- **Pendiente F5.2:** carga de NACHA directamente por clasificación/`EntryDetail`, resolución de campos homologados y DryRun; bloqueos SOAP Live/Command Center permanecen.
- **Verificación ejecutada:** comprobaciones estáticas + integridad ZIP; **no ejecutadas** compilación .NET/xUnit, XSD CFA, migraciones de BD, UAT. F6.


## 14. Incremento F5.2 — 2026-10-25

- **Estado:** `IMPLEMENTED_UNVERIFIED` (pruebas escritas; no compiladas ni ejecutadas). `last_completed_microtask=F5.2`; `next_microtask=F5.3`.
- **Fuente:** ZIP completo F5.1; salida ZIP completo `ACHInterbank_ACHCOL_CFA_F5_2_UNIFICADO.zip`. Conserva `.git` y todos los fuentes existentes sin rebase ni nuevos commits.
- **C#:** `IProcTransaccionesRequestMapper.PreviewAchColFileBasedAsync` devuelve `AchColProcTransaccionesDryRunPreview` NO transmisible, distinto de `ProcTransaccionesRequestContract`; carga archivo/ingesta/clasificación/header/lote/registro 6; exige cola bloqueada en estado F4.2, cámara ACHCOL activa, clave idempotente, clase monetaria y mapping publicado de referencia.
- **Contrato:** XML parcial solo diagnóstico, observado de `EntryDetail`; `IDTRAN`, `IDLOTE`, `IDCAMCOMPE` y otros condicionales ausentes deliberadamente; `CanTransmit=false` sin excepción. XSD externo no proporcionado; no hay homologación CFA.
- **Pruebas escritas:** casos H/008/.8, I/009/.9, H/008/.10, claro/cifrado, cola desbloqueada, identidad alterada, prenotificación, mapping faltante y ausencia de mutaciones o envíos SOAP. **No ejecutadas** por falta de `dotnet`. Sin DB/CFA.
- **Cambios en persistencia/infra:** ninguno, no se añadieron colas, migraciones ni clientes externos. Otros programas y rutas `CycleBound` intactos.
- **Decisiones:** DEC-066–DEC-070. Detalles en `F5_2_IMPLEMENTATION.md`; pruebas de entorno y contrato externo diferidas a F6.


## 15. Incremento F5.3 — 2026-10-25

- **Estado:** `IMPLEMENTED_UNVERIFIED`; `last_completed_microtask=F5.3`, `next_microtask=F6.1`; comienza la etapa de pruebas finales una vez terminadas F1–F5.
- **Base:** ZIP consolidado F5.2. Se preservó el historial `.git` del archivo ZIP, sin commit ni cambio de rama declarados. Ver F0.1 para divergencias históricas.
- **Código:** `AchColProcTransaccionesDryRunContractCheck.Inspect` verifica el XML parcial y los campos permitidos del preview F5.2; el mapper lo invoca antes de exponer la vista diagnóstica. No hay cola, migración, endpoint, servicio SOAP nuevo ni transmisión.
- **Seguridad:** XML sin DTD ni recursos externos; solo parámetros candidatos observados; la evidencia no contiene valores monetarios, cuentas ni identidades; salida `CanTransmit=false`, XSD no disponibles, mapping ACHCOL no aprobado.
- **Tests:** `AchColProcTransaccionesContractReadinessF53Tests.cs` agrega positivos/negativos, campos no homologados, XML manipulado, entidades externas y guarda de homologación. **No ejecutados** por ausencia de .NET 10; pruebas de BD/CFA quedan F6.
- **Contrato:** WSDL revisado; importa `xsd0` y `xsd1` no suministrados, sin consultas a URLs externas. Matriz de campos CFA F5.1 mantiene pendientes `IDTRAN`, `IDLOTE`, `IDCAMCOMPE`, etc. No se ha aprobado transacción Live.
- **Decisiones:** DEC-071–DEC-075. Documentadas en `F5_3_IMPLEMENTATION.md`.
- **Entrega validada:** `ACHInterbank_ACHCOL_CFA_F5_3_UNIFICADO.zip`, 8.124 entradas; 8.116 entradas de F5.2 conservadas con CRC/tamaño idénticos, 5 actualizadas y 3 agregadas; `ZipFile.testzip()` exitosa. No se ejecutó .NET.


## 17. Incremento F6.2 — 2026-10-25

- **Estado:** `IMPLEMENTED_UNVERIFIED`. Se amplió el Command Center ya existente con `achColFileBased`: 6 KPIs anónimos por origen y motivo de bloqueo, alertas para `QueueStatus != Blocked` e `AttemptCount > 0`. Los valores son de una ventana de creación de 1–168 h; control histórico completo requiere lectura BD sin escritura.
- **Fuentes:** `IncomingNachaCommandCenterDtos.cs`, `IncomingNachaCommandCenterQueryService.cs`; contrato OpenAPI esperado actualizado y tests xUnit añadidos. **No** endpoint, tabla, worker, feature flag ni cambio de mapeo SOAP nuevo.
- **Operación:** `F6_2_OPERATIONS_RUNBOOK.md` y `F6_2_IMPLEMENTATION.md` documentan GO/NO-GO, consultas PostgreSQL y SQL Server, rollback por flag OFF y conciliación de resultados monetarios inciertos sin retry ciego.
- **No verificado:** build .NET, xUnit, DB, homologación XSD y UAT. No se habilitó `Live` ni se ejecutó SOAP monetario. Deuda F6.1 R5C y R7 sigue abierta.
- **Decisiones:** DEC-081–DEC-085. **Próximo F6.3.** Se conserva historial Git del ZIP, sin commit creado aquí.

## 16. Incremento F6.1 — 2026-10-25

- **Estado:** `IMPLEMENTED_UNVERIFIED`, ver `F6_1_IMPLEMENTATION.md`. Se ejecutó `validate-solution-projects.py` OK (7/7); auditoría offline específica ACHCOL **12/12 PASS**; ratchet R5D **PASS**; sobre digital NAV3.4J **145 PASS**.
- **Hallazgos abiertos:** R5C histórico **FAIL** por SHA de validador R5B cambiada, requiere revisión, no ocultar; R7 inconcluso por tiempo. No hay .NET 10.0.402 ni acceso de descarga, no se compilaron fuentes, no se ejecutó xUnit ni BD ni CFA; no se declara F6.1 certificada.
- **Corrección focal:** guard SOAP `DryRun` rechazará atributos/textos XML inesperados; test de regresión nuevo escrito (pendiente de ejecución). Script estático `scripts/validate-achcol-f61-offline.py` agregado y ejecutado.
- **Sin cambios** en cliente SOAP, esquema DB, feature flags, mapping Live o flujo `CycleBound`. **CFA monetario Live deshabilitado**. No hubo envío real, homologación ni certificación.
- **Siguiente tarea:** F6.2 observabilidad y runbook de activación/rollback. Mantener en deuda F6.1 todas las pruebas ejecutables e inspeccionar los resultados de CI al disponer del proyecto en un ambiente .NET autorizado. **Decisiones DEC-076–DEC-080**.

## 18. Incremento F6.3 — 2026-10-25 (cierre de auditoría offline; certificación ABIERTA)

- **Estado del incremento:** `IMPLEMENTED_UNVERIFIED` (expediente y control estático completados, pruebas ejecutables y certificación no realizadas). `last_completed_microtask=F6.3`, `next_microtask=null` significa **no hay otra microimplementación planificada**, no significa sistema certificado. `program_status=IN_PROGRESS` porque la certificación externa sigue abierta.
- **Fuente exacta:** `ACHInterbank_ACHCOL_CFA_F6_2_UNIFICADO.zip`, SHA-256 `85f79f594496404ea92f3d76558e875bf72149742892a7890d7cb76c4ba90b95`. Sin acceso a DB ni endpoints CFA; no se ejecutó transporte monetario.
- **Verificaciones realmente ejecutadas:** grafo de solución 7/7 PASS; `validate-achcol-f61-offline.py` 12/12 PASS; `validate-achcol-f62-offline.py` 10/10 PASS; `validate-r5d-inbound-postprocessing.py` PASS; `validate-nav3-4j-digital-envelope.py` 145 comprobaciones PASS. Ratchet R5C FAIL por comprobación SHA de componentes R5B históricos; R7 NO CONCLUYÓ dentro de la ventana de ejecución y depende de ratchets anteriores; no adjudicarle PASS.
- **Corrección puntual:** en `scripts/validate-achcol-f62-offline.py`, sustituir una aserción dependiente del puntero mutable `next_microtask: F6.3` por identidad del programa y guardia `external_dispatch_allowed_without_authorization: false`. Evita regresiones falsas al cerrar F6.3; sin cambio en API/cola/SOAP/DB.
- **No ejecutadas ni aprobadas:** `dotnet restore/build/test`, xUnit F1–F6, SQL Server/PostgreSQL reales, carga/cifrado `.OUT.env` en entorno, concurrencia multinstancia, CI del nuevo snapshot, UAT/homologación CFA y pruebas Live. Falta SDK local .NET 10.0.402; WSDL CFA importa XSD no proporcionados.
- **Decisiones DEC-086–DEC-089:** conservar el bloqueo de transmisión, corregir ratchet de estado mutable, separar diagnóstico offline de certificación e impedir que un ZIP íntegro se use como criterio GO para producción.
- **Próximo paso fuera de las microimplementaciones:** ejecutar gates detallados en `F6_3_VERIFICATION_REPORT.md`, anexar evidencias y sólo después evaluar autorización CFA `Live`; no inferir contratos ausentes ni efectuar reintentos monetarios a ciegas.
