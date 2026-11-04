# ACH-CR-001 — Historial de entregas

## I00 — 2026-10-25 — Documentación inicial y línea base

**Tipo:** docs-only + validador estático (sin cambio a lógica de negocio).

**Fuente:** `ACHInterbank_ACHCOL_CFA_F6_3_UNIFICADO.zip`.

**Salida:** `ACHInterbank_ACH_CR_I00_COMPLETO.zip`.

**Agregados:**
- `docs/ach-cycle-reassignment/00_START_HERE.md`
- `docs/ach-cycle-reassignment/01_ARQUITECTURA.md`
- `docs/ach-cycle-reassignment/02_PLAN_INCREMENTAL.md`
- `docs/ach-cycle-reassignment/03_REGLAS_Y_ESCENARIOS.md`
- `docs/ach-cycle-reassignment/04_VALIDACION_ESTATICA.md`
- `docs/ach-cycle-reassignment/05_GUIA_DE_CHAT.md`
- `docs/ach-cycle-reassignment/06_TRAZABILIDAD.md`
- `docs/ach-cycle-reassignment/MEMORY_STATE.md`
- `docs/ach-cycle-reassignment/CHANGELOG.md`
- `scripts/validate-cycle-reassignment-static.py`

**Modificados/eliminados del ZIP base:** ninguno.

**Datos de empaquetado / hashes:** evidencia detallada se comunica con el ZIP entregado. El SHA-256 del archivo completo se calcula al finalizar, fuera del propio ZIP, para evitar referencia circular. Para I01 se debe usar el SHA-256 que acompaña el artefacto entregado.

**Validación estática:** se preservaron los 8.130 registros originales por ruta+CRC+tamaños; 10 rutas nuevas; 8.140 entradas totales. La revisión CRC completa del ZIP y el validador `validate-cycle-reassignment-static.py --zip` terminaron en PASS. Resultados completos y SHA-256 se anexan a la entrega (no se incrusta el hash del ZIP en sí mismo para evitar referencia circular). Tests de código .NET: `NOT_RUN_ENVIRONMENT`. Pruebas de integración/UAT: `NOT_RUN`.

**Decisiones:** mínimo de diseño; reutilizar audit events, calendario/política/Quartz; separar CENIT liquidez; no tabla nueva sin necesidad; no cambio de fecha valor automático.

**Pendiente:** I01; carreras de exportación y rutas de fin de ciclo, comprobación sábado.

## Plantilla para I01+

```text
## Ixx — YYYY-MM-DD — <alcance>
Entrada ZIP + SHA-256: ...
Salida ZIP + SHA-256: ...
Agregados: ...
Modificados: ...
Eliminados: ...
Migraciones: ...
Verificaciones estáticas ejecutadas: ...
Tests compilación/runtime: resultado real / NOT_RUN_ENVIRONMENT
Riesgos abiertos / cerrados: ...
Decisiones nuevas o revertidas: ...
NEXT_INCREMENT: Iyy
```


## I01 — 2026-10-25 — Evaluación de elegibilidad y ciclo destino (sin persistencia)

**Entrada:** `ACHInterbank_ACH_CR_I00_COMPLETO.zip`. **Salida:** `ACHInterbank_ACH_CR_I01_COMPLETO.zip`.

**Código agregado (3 archivos):**
- `src/Cfa.ACHInterbank.Application/ACH/Interfaces/IAchPendingCycleEligibilityService.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Models/AchPendingCycleEligibilityResult.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchPendingCycleEligibilityService.cs`

**Pruebas escritas (1 archivo):** `tests/Cfa.ACHInterbank.Tests/AchPendingCycleEligibilityServiceTests.cs` (12 `[Fact]`, no ejecutadas).

**Script agregado (1 archivo):** `scripts/validate-cycle-reassignment-i01-static.py` (19/19 controles PASSED).

**Documentos:** actualizados `00_START_HERE.md`, `01_ARQUITECTURA.md`, `02_PLAN_INCREMENTAL.md`, `03_REGLAS_Y_ESCENARIOS.md`, `04_VALIDACION_ESTATICA.md`, `05_GUIA_DE_CHAT.md`, `06_TRAZABILIDAD.md`, `MEMORY_STATE.md`, `CHANGELOG.md`; agregado `07_I01_IMPLEMENTACION_Y_EVIDENCIA.md`.

**No modificados:** lógica existente de creación, exportación, Quartz, CENIT liquidez, Domain, migraciones, solución, perfil regulatorio, golden files y UI. No se agregaron locks globales. La selección no actualiza BD.

**Resultado de validación estática:** validador heredado I00: PASS; nuevo I01: 19/19 PASS; integridad CRC/diff del ZIP registrada fuera del paquete al finalizar. `dotnet build`/`dotnet test`: **NOT_RUN_ENVIRONMENT (no SDK)**. EF en SQL Server/PostgreSQL, concurrencia exportación y UAT: **NOT_RUN**.

**Riesgos:** carrera exportación I02/I05; sábado->lunes `BatchResolver` `Before` I04/ajuste focal; fecha valor otra jornada I04. Documentación de detalle: `07_I01_IMPLEMENTACION_Y_EVIDENCIA.md`.

**Siguiente:** I02 — cambio atómico de ciclo/lote y evento de auditoría. **Quedan cinco incrementos:** I02–I06.


## I02 — 2026-10-25 — Reasignación persistente y evento auditado (sin Quartz)

**Fuente:** `ACHInterbank_ACH_CR_I01_COMPLETO.zip`. **Salida:** `ACHInterbank_ACH_CR_I02_COMPLETO.zip`.

**Nuevos códigos:** `IAchPendingCycleReassignmentService.cs`, `AchPendingCycleReassignmentResult.cs`, `AchPendingCycleReassignmentService.cs` (Application/Persistence). Se revalida I01 dentro de transacción relacional SERIALIZABLE, se reasigna ciclo y lote, se guarda evento con same-state Pending y se recalculan totales/clase con `ITransactionPersister`. Conserva fecha valor, monto y trazas. Sin cambios a Domain, Quartz ni exportador.

**Pruebas/validación:** `AchPendingCycleReassignmentServiceTests.cs` 8 tests escritos NO EJECUTADOS; validador `validate-cycle-reassignment-i02-static.py` 25/25 PASS; validaciones I00/I01 + grafo solución deben incluirse en resumen final del ZIP. Build/tests SQL Server/PostgreSQL y carrera externa: NOT_RUN_ENVIRONMENT.

**Documentos:** actualizado START_HERE, ARQUITECTURA, PLAN, REGLAS, VALIDACION, GUIA, TRAZABILIDAD, CHANGELOG, MEMORY_STATE; agregado `08_I02_IMPLEMENTACION_Y_EVIDENCIA.md`. Sin migraciones, sin paquetes nuevos, sin endpoints ni cambios de cron.

**Riesgo:** serializar el comando no coordina un exportador que obtuvo antes datos del ciclo. Mantener automatización desactivada hasta cierre I05 y prueba relacional. **NEXT_INCREMENT: I03**. **Restan 4 incrementos** I03–I06. Conteos/integridad y hash SHA-256 del ZIP: informe de entrega externo `ACH_CR_I02_VALIDATION.json`/`.sha256`.


## I03 — 2026-10-25 — Automatización Quartz opt-in, NO habilitada

**Entrada:** `ACHInterbank_ACH_CR_I02_COMPLETO.zip`. **Salida:** `ACHInterbank_ACH_CR_I03_COMPLETO.zip`.

**Código añadido:** `AchPendingCycleReassignmentHandler.cs` (reutiliza `ITaskHandler`, consulta read-only keyset y delega en comando I02). **Código modificado:** `SchedulerTaskCatalog.cs`, `TaskDefinitionSeeder.cs` (tarea Disabled y sin manual/recovery/retry).

**Pruebas:** `AchPendingCycleReassignmentHandlerTests.cs` (9 `[Fact]` escritos, **NO EJECUTADOS**). **Script:** `validate-cycle-reassignment-i03-static.py`. **Docs:** nuevo `09_I03_IMPLEMENTACION_Y_EVIDENCIA.md` y actualización de documentos maestros, incluido `MEMORY_STATE.md`. Sin migraciones, cambios Domain, modificaciones del motor NACHA, endpoints, UI, nuevos locking services o archivos eliminados.

**Control de activación:** requiere **ambas** condiciones: tarea Enabled/unpaused y `AchCycleReassignment:EnableExecution=true`. Se deja deshabilitada de fábrica. Ninguna llamada financiera externa se realiza por el nuevo handler.

**Limitación crítica:** exportador concurrente NO coordinado; sigue abierta `RISK-CR-001` hasta I05. `BatchResolver` proyectado sábado y cambio de fecha valor corresponden a I04; no se tocan en I03. Barrido acotado puede sufrir starvation si primeros IDs son inelegibles (RISK-CR-008).

**Validación:** ejecutar scripts I00/I01/I02/I03, diff ZIP I02→I03 por ruta/CRC y CRC completo. Build/test SQL/Quartz/UAT: `NOT_RUN_ENVIRONMENT`. SHA-256 final fuera del propio ZIP.

**NEXT_INCREMENT:** I04. **Quedan 3: I04, I05, I06.**

## I04 — 2026-10-25 — Proyección de fecha futura, cierre jornada y fecha valor sin cambio automático

**Origen:** `ACHInterbank_ACH_CR_I03_COMPLETO.zip` → **Salida:** `ACHInterbank_ACH_CR_I04_COMPLETO.zip`.

**Modificados:** `BatchResolver.cs` (futuro de día hábil, estado, ventana; preserva cola CENIT), `RoutingStrategyService.cs` (no elegir Closed/Cancelled), `AchPendingCycleEligibilityServiceTests.cs` (+2 casos); `validate-cycle-reassignment-i03-static.py` (acepta avance de MEMORY_STATE para revalidación histórica); docs maestros de iniciativa, `CHANGELOG.md` y memoria.

**Añadidos:** `AchCycleCalendarProjectionTests.cs` (8 pruebas), `scripts/validate-cycle-reassignment-i04-static.py`, `docs/ach-cycle-reassignment/10_I04_IMPLEMENTACION_Y_EVIDENCIA.md`.

**No tocados:** modelos Domain, contratos Application, tablas, migraciones, Quartz, configuración de activación, NACHA, colas/algoritmo de liquidez CENIT, componentes de presentación, golden files.

**Validación:** scripts I00/I01/I02/I03/I04 y grafo solución, diff completo contra I03, CRC de ZIP y SHA-256 en archivo externo de entrega. Pruebas escritas **NO EJECUTADAS**, .NET/SQL/MFT/Quartz/UAT **NOT_RUN_ENVIRONMENT**.

**Riesgos:** carrera exportación I05 todavía **abierta**, cambio inter-día de fecha valor no autorizado y no implementado; tarea Quartz sigue Disabled. **NEXT_INCREMENT: I05**. **Restan 2 incrementos**.

## I05 — 2026-10-25 — Admisión NACHA serializable y regresiones CENIT

**Entrada:** `ACHInterbank_ACH_CR_I04_COMPLETO.zip`.
**Salida:** `ACHInterbank_ACH_CR_I05_COMPLETO.zip`.

**Código actualizado:** `AchPendingCycleEligibilityService.cs`, `AchFileExportAuditService.cs`, `NachaSecurityOperationService.cs`.
**Pruebas ampliadas:** `AchPendingCycleEligibilityServiceTests.cs` (2 `[Fact]`), `AchFileExportAuditServiceTests.cs` (2 `[Fact]`).
**Validadores:** `validate-cycle-reassignment-i05-static.py` agregado; ajuste al validador histórico I04 para preservar su evidencia en versiones posteriores.
**Documentación:** nueva `11_I05_IMPLEMENTACION_Y_EVIDENCIA.md`, cambios as-built en 00–06, MEMORY_STATE y CHANGELOG.
**No se modificaron:** Domain, Application, migrations, scheduler, golden files, API endpoints, CENIT ejecución/liquidez, transporte real.
**Pruebas ejecutables:** no ejecutadas; homologación de concurrencia real abierta. **Quartz:** Disabled. **Siguiente:** I06 (1 incremento pendiente).

El hash y el inventario CRC real se entregan fuera del ZIP en `ACH_CR_I05_VALIDATION.json` y `.sha256` tras empaquetar, para evitar auto-referencia circular.

## I06 — 2026-10-25 — Cierre estático y plan de homologación (NO-GO)

**Entrada:** `ACHInterbank_ACH_CR_I05_COMPLETO.zip`. **Salida:** `ACHInterbank_ACH_CR_I06_COMPLETO.zip`.

**Código funcional:** sin cambios de lógica ni estados financieros. Se corrigen tres textos obsoletos en documentación interna de I02/I03/TaskDefinitionSeeder; no se activa Quartz.

**Validador:** se desacopla el chequeo heredado I05 de `INCREMENT_CURRENT: I05`, sin retirar sus guardias; se añade `validate-cycle-reassignment-i06-static.py` para arquitectura/evidencia final. Se rerun I00–I05 y grafo de solución.

**Docs actualizados:** `00_START_HERE.md`, `01_ARQUITECTURA.md`, `02_PLAN_INCREMENTAL.md`, `03_REGLAS_Y_ESCENARIOS.md`, `04_VALIDACION_ESTATICA.md`, `05_GUIA_DE_CHAT.md`, `06_TRAZABILIDAD.md`, `MEMORY_STATE.md`, `CHANGELOG.md`. **Nuevos:** `12_I06_AUDITORIA_Y_CIERRE.md`, `13_PLAN_HOMOLOGACION_GO_NO_GO.md` y validador I06.

**Pruebas ejecutables:** .NET/SQL Server/PostgreSQL/Quartz/MFT/UAT **NOT_RUN_ENVIRONMENT**. Sin credenciales, paquetes, migraciones, endpoints, UI ni archivos normativos cambiados.

**Hash SHA-256, comparación I05→I06, pruebas CRC y resultados de scripts:** evidencia externa en `ACH_CR_I06_VALIDATION.json` y `ACHInterbank_ACH_CR_I06_COMPLETO.sha256` tras generar el ZIP, sin referencia circular.

**Estado:** plan incremental I00–I06 entregado estáticamente; **0 incrementos pendientes**. `RUNTIME_VALIDATION=NOT_RUN_ENVIRONMENT`; `UAT=NOT_STARTED`; `PRODUCTION=NOT_READY`; **GO-LIVE NO AUTORIZADO**. Cualquier corrección posterior requiere alcance y entrega versionada nuevos.


## I06-FIX01 — 2026-10-26 — Reparación focal de tres errores de compilación reportados

**Entrada:** `ACHInterbank_ACH_CR_I06_COMPLETO.zip`. **Salida:** `ACHInterbank_ACH_CR_I06_FIX01_COMPLETO.zip`. **Tipo:** parche, NO incremento funcional.

**Código modificado (tres archivos):**
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ContrapartidaDispatchJobService.cs`: `HasValue` + `Value` para filtro `int?` contra `int[]` (`CS1929`).
- `src/Cfa.ACHInterbank.Persistence/ACH/Quartz/Jobs/Implementation/AchPendingCycleReassignmentHandler.cs`: `using Cfa.ACHInterbank.Application.ACH.Models` (`CS0103`).
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaAdmissionValidator.cs`: inicialización previa de `correlationError` (`CS0165`).

**Agregados:** `14_I06_FIX01_CORRECCIONES_COMPILACION.md`, `15_I06_FIX01_VALIDACION.json`, `scripts/validate-cycle-reassignment-i06-fix01-static.py`. **Modificados:** `00_START_HERE.md`, `MEMORY_STATE.md`, `CHANGELOG.md`, `scripts/validate-cycle-reassignment-i06-static.py` (solo compatibilidad con el nombre de entrega I06-FIX01 evidenciado). **Eliminados:** ninguno. **Migraciones:** ninguna.

**Verificaciones:** validadores históricos I01–I06 y grafo solución; nuevo validador focal I06-FIX01, ZIP/CRC y comparación con I06. Ver el reporte entregado con el ZIP.

**Limitación:** SDK .NET no disponible: **no** se comprobó el build. **Estado productivo NO-GO**, Quartz deshabilitado. **Incrementos restantes:** 0.
