> **Entrega vigente F6.3:** `ACHInterbank_ACHCOL_CFA_F6_3_UNIFICADO.zip`, proyecto completo desde F6.2; expediente de QA offline `F6_3_VERIFICATION_REPORT.md`, auditor F6.2 desacoplado de la tarea siguiente, `MEMORY_STATE.md` 1.20.0. **SIN certificación**: .NET/BD/XSD/UAT y autorización CFA siguen pendientes. `Live` permanece prohibido.

> **Entrega vigente F6.2:** `ACHInterbank_ACHCOL_CFA_F6_2_UNIFICADO.zip`, proyecto completo basado en F6.1. Ampliación aditiva del resumen de observabilidad con métricas `AchColFileBased`, pruebas xUnit escritas y runbook F6.2 de monitoreo/rollback. **No habilitar Live**; build .NET/BD/XSD/UAT pendientes. Próximo F6.3. Consultar `F6_2_IMPLEMENTATION.md` y `MEMORY_STATE.md` 1.19.0.

> **Entrega más reciente F6.1:** `ACHInterbank_ACHCOL_CFA_F6_1_UNIFICADO.zip`, raíz `ACHInterbank_ajustado/`. Incorpora revisión offline 12/12, grafo 7/7, ratchet R5D, control de sobre digital y mejora puntual XML DryRun con test escrito. **No** se ejecutaron tests .NET 10, PostgreSQL, SQL Server o CFA; está documentado el fallo del ratchet R5C. Siguiente F6.2; deuda de ejecución F6.1 abierta. Ver `F6_1_IMPLEMENTATION.md`.

# Entrega F5.2 — 2026-10-25

ZIP completo de continuidad: `ACHInterbank_ACHCOL_CFA_F5_2_UNIFICADO.zip`. Basado en F5.1 sin reestructurar el repositorio ni alterar su `.git`. Se añade proyección no transmisible desde EntryDetail, pruebas C# y `F5_2_IMPLEMENTATION.md`. `MEMORY_STATE.md` 1.16.0, siguiente **F5.3**. No se realizaron envíos CFA, .NET ni migraciones; pendientes F6.

## Entrega vigente F5.1 — 2026-10-25

Fuente completa: `ACHInterbank_ACHCOL_CFA_F5_1_UNIFICADO.zip`. Contiene todos los archivos heredados F4.3, el guard del mapper tradicional, un inventario de campos CFA por origen NACHA-M, pruebas y `F5_1_IMPLEMENTATION.md` junto con `MEMORY_STATE.md` 1.15.0. Siguiente microtarea F5.2. Sin transmisión Live ni homologación XSD; .NET no ejecutado.

# ACHINTERBANK — Paquete unificado incremental · SES-002 / F0.2

Este paquete **único** contiene los archivos del proyecto `ACHInterbank_ajustado` tal como venían en `ACHInterbank_ajustado.zip` y la carpeta `docs/ACHCOL_CFA_FILE_BASED_2026-10-24/` con la documentación incremental integrada al repositorio. **No es un ZIP con otros ZIP anidados**: puede extraerse y abrirse como proyecto directamente.

## Continuidad

- `PLAN.md`: implementación prevista, fases y aceptación.
- `MEMORY_STATE.md`: única fuente del estado de este requerimiento, microtareas terminadas `F0.1`, `F0.2` y próxima `F0.3`.
- `CHATGPT_MICRO_START.md`: instrucciones de reanudación.
- `F0_1_BASELINE.md`: evidencia verificable del snapshot, Git no limpio y SHA-256 de componentes críticos.
- `F0_2_FLOW_TRACE.md`: mapa estático de bloqueos desde nombre/encabezado hasta SOAP CFA y política de continuidad.

## Fuente

- ZIP original: `ACHInterbank_ajustado.zip`.
- SHA-256: `26da6cf2dffcf939aa539f8ea9f0169fa8164fb959eaf2eb3a7ae22651b6c890`.
- Rama y commit contenidos: `ACH-Interbank` / `0b4ce1c809ca5acfdafaf455cae4d0eaa5292f07`.
- Estado técnico: F0.1 baseline y F0.2 trazado documentados; **sin cambios funcionales ni tests .NET**; los siete archivos rastreados ausentes y las diferencias CRLF/LF originales quedan preservados y explicados.

## Siguiente paso

Ejecutar F0.3 (ADR), después continuar los incrementos de código F1–F5 **sin detener la implementación por falta de pruebas de ambiente**. Las pruebas finales son F6. Cada entrega debe ser un ZIP unificado con el proyecto completo modificado y la documentación actualizada.

## Entrega F0.3 (2026-10-24)

Este ZIP **completo** incorpora el ADR `F0_3_ADR_ARCHITECTURE.md` y actualiza `PLAN.md`, `MEMORY_STATE.md` y `CHATGPT_MICRO_START.md`. F0.3 documenta decisiones, **no modifica C#/TS/SQL** y no declara validación CFA. La siguiente microtarea es **F1.1 — parser entrante ACHCOL**. Las siete ausencias Git del snapshot original permanecen documentadas; no se alteró el código fuente de F0.2.

## Entrega F0.3.A — Aclaración `.OUT` / `.OUT.env` (2026-10-24)

Se añadió `F0_3_A_NAMING_OUT_ENVELOPE.md`, se precisó la gramática física `RRRRTTT.ZZZ.AAAAMMDD.N.OUT[.env]` y las dos rutas (en claro/cifrado). Se anotó la brecha de descubrimiento automático `.OUT` en Managed Folder y SFTP. Actualizados ADR, PLAN, MEMORY_STATE y CHATGPT_MICRO_START. **Ningún archivo de código cambió**; siguiente implementación F1.1. Este ZIP reemplaza F0.3 como fuente de continuidad.

## Entrega F1.1 — parser físico ACH Colombia (2026-10-24)

Se agregaron archivos C# de parser y pruebas: `AchColInboundFileNameParser.cs` y `AchColInboundFileNameParserTests.cs`. No hay conexión nueva a SOAP, cifrado, scheduler o BD. Se añadió `F1_1_IMPLEMENTATION.md`, se actualizaron PLAN, MEMORY_STATE y CHATGPT_MICRO_START. F1.1 se marca `IMPLEMENTED_UNVERIFIED`: no hay SDK .NET 10 disponible para compilar/ejecutar xUnit. F1.2 es la próxima tarea. Este ZIP completo reemplaza F0.3.A para continuidad.

## Entrega F1.2 — admisión .OUT y correlación ACHCOL (2026-10-24)

Se integró en ingesta la validación del nombre ACH Colombia contra encabezado NACHA-M post-descifrado, y se ampliaron Managed Folder y SFTP para descubrir `.OUT` plano además de `.OUT.env`/`.RET.env`. Se añaden pruebas unitarias y reportes de F1.2; sin cambios SQL/EF/SOAP. La implementación está `IMPLEMENTED_UNVERIFIED`: no hay SDK .NET 10 en este entorno. El siguiente ZIP debe partir de F1.2, con `next_microtask=F2.1`.

## Entrega F2.1 — 2026-10-24

Partiendo del ZIP completo F1.2, se implementó `IncomingNachaCycleResolver` con modo `AchColFileBased` bajo flag OFF por defecto para nombres `.OUT` canónicos de ACHCOL, retorno `ResueltoSinCiclo=7` y `AchCycleId=null` sin consultar `AchCycles`. Sin alterar la admisión, cola, BD o SOAP y sin activar el flag en producción. Pruebas xUnit agregadas no ejecutadas por ausencia de .NET 10. El estado y decisiones figuran en `F2_1_IMPLEMENTATION.md` y `MEMORY_STATE.md`; siguiente F2.2. Este ZIP preserva el proyecto fuente completo y reemplaza F1.2 como línea base.


## Entrega F2.2 — 2026-10-24

Desde el ZIP F2.1 se implementó admisión ACHCOL `AchColFileBased` con estado `ResueltoSinCiclo`, `AchCycleId=null` y flag opt-in, condicionada a cámara ACHCOL activa, archivo `.OUT` post-descifrado canónico, correlación header/ZZZ, calendario y fecha efectiva `yyyyMMdd`. Se mantiene flujo CycleBound CENIT/RET. Nueva suite unitaria `IncomingNachaAdmissionAchColFileBasedTests.cs`; código C# **sin compilar ni ejecutar** por falta de SDK .NET 10; no se realizó llamada CFA.

Fuente de continuidad **vigente**: `ACHInterbank_ACHCOL_CFA_F2_2_UNIFICADO.zip` + `MEMORY_STATE.md`, `F2_2_IMPLEMENTATION.md`, `PLAN.md` y `CHATGPT_MICRO_START.md`. **Siguiente `F2.3`** persistencia de contexto sin ciclo; pruebas finales en F6. Las secciones de entregas anteriores se conservan como historial, no representan el estado actual.

## Entrega F2.3 — 2026-10-24

ZIP completo derivado del paquete F2.2. Persistencia tipada del contexto de archivo ACHCOL tras correlación validada; propiedades independientes `FileIdModifier`/`ZZZ`/`ExternalCycleNumber`, con `AchCycleId` nullable. Migraciones aditivas y snapshots en PostgreSQL y SQL Server; DTOs y Command Center exponen metadatos sin afectar contratos anteriores. Pruebas SQLite de roundtrip escritas pero **no ejecutadas** por ausencia de .NET 10. No se habilita Live, ni se modificó la cola/mapper/SOAP. Nuevo estado [`F2_3_IMPLEMENTATION.md`](F2_3_IMPLEMENTATION.md) y `MEMORY_STATE.md` con **siguiente F3.1**. Las pruebas finales se mantienen F6.

**Paquete de continuidad vigente:** `ACHInterbank_ACHCOL_CFA_F2_3_UNIFICADO.zip`.

## Actualización F3.1

Este paquete unificado incorpora el **modelo de cola con origen tipado y migración PostgreSQL**. Se preserva íntegro el código F2.3 y se añade `F3_1_IMPLEMENTATION.md`. No incluye todavía migración SQL Server (F3.2), planificador ACHCOL sin ciclo (F3.3) ni despacho SOAP nuevo (F4–F5). No ejecutar esta versión contra SQL Server hasta F3.2. Ver `MEMORY_STATE.md`.

## Entrega F3.2 — 2026-10-24

Paquete completo **`ACHInterbank_ACHCOL_CFA_F3_2_UNIFICADO.zip`**, derivado de F3.1 (no un patch ni ZIP anidado). Paridad de cola NACHA `AchColFileBased` para SQL Server: migración con FK/índice, default `CycleBound`, nulabilidad transacción/ciclo, CHECK por origen, `Down` protegido. Snapshot SQL Server y tres tests EF de contrato nuevos; `F3_2_IMPLEMENTATION.md`, `MEMORY_STATE.md`, `PLAN.md` y `CHATGPT_MICRO_START.md` actualizados. **Estado `IMPLEMENTED_UNVERIFIED`**: SDK .NET/BD de pruebas ausentes, no se ejecutaron migraciones ni xUnit. Ningún envío CFA habilitado. Próxima tarea `F3.3`. La documentación previa permanece como historial.


## Entrega F3.3 — 2026-10-24 (vigente)

Paquete completo `ACHInterbank_ACHCOL_CFA_F3_3_UNIFICADO.zip` sobre F3.2, sin ZIP anidado: se añadió planificador `AchColFileBased` por NACHA `EntryDetail`, identidad SHA-256 estable sin ciclo/ingesta, guardas fail-closed, y columnas/constraint/snapshots/migraciones equivalentes en PostgreSQL y SQL Server para que el link interno sea opcional en el origen ACHCOL. Se añadió `F3_3_IMPLEMENTATION.md` y suites de tests. Los registros se planifican `Blocked`, sin autorización de envío a CFA. No se ejecutó .NET, xUnit, migraciones ni Live. El siguiente incremento es **F4.1**, ver `MEMORY_STATE.md` v1.11.0; toda certificación real está en F6. Las secciones previas permanecen como historial y no expresan estado vigente.

## Entrega F4.1 — 2026-10-24 (vigente)

Paquete completo `ACHInterbank_ACHCOL_CFA_F4_1_UNIFICADO.zip` sobre F3.3: política de elegibilidad específica para `AchColFileBased` y evaluación obligatoria por planner. Crédito/débito monetario validado conforme perfil ACHCOL; prenotificaciones/RET excluidas, cámara/contexto/header controlados. **No se habilita despacho**: filas siguen `Blocked` y feature flag OFF por defecto. Sin migraciones, SOAP ni cambios CENIT/RET. Suite xUnit ampliada, no ejecutada por falta de .NET 10. `MEMORY_STATE.md` v1.12.0 y `F4_1_IMPLEMENTATION.md` registran las decisiones DEC-047–DEC-051; siguiente **F4.2**. Las secciones anteriores son historial.

## Entrega F4.2 — 2026-10-25

Paquete completo `ACHInterbank_ACHCOL_CFA_F4_2_UNIFICADO.zip` construido sobre F4.1. Añade stage de prevalidación ACHCOL en orquestador R5D (solo flag explícito, cola en `Blocked`), pruebas unitarias F4.2 y reporte `F4_2_IMPLEMENTATION.md`. No cambia mapeos CFA, SOAP, migraciones ni procesamiento de otros orígenes. Compilación/.NET/BD/CFA no ejecutadas; `IMPLEMENTED_UNVERIFIED`, siguiente F4.3. Consultar `MEMORY_STATE.md`.


## Entrega F4.3 — 2026-10-25

Partiendo del ZIP completo F4.2, se añadieron salvaguardas **mínimas** en las operaciones manuales y en las acciones disponibles del Command Center: para `AchColFileBased` se rechazan `retry`, `unblock` y `requeue` con auditoría existente. El cierre manual definitivo solo se admite sin intento/ejecución CFA. La prevalidación F4.2 permanece como simulación local sin envíos, incluso con `Live` tradicional. **Sin migración ni motor nuevo de reintentos.** Tests añadidos, sin ejecutar (falta .NET 10). Detalles en `F4_3_IMPLEMENTATION.md`, estado `MEMORY_STATE.md` 1.14.0 y **próximo F5.1**. El ZIP completo F4.3 reemplaza F4.2 como base de continuidad.


## Entrega F5.3 — 2026-10-25 (vigente)

Paquete consolidado `ACHInterbank_ACHCOL_CFA_F5_3_UNIFICADO.zip`, construido sobre F5.2, con una validación local del XML SOAP parcial (`AchColProcTransaccionesDryRunContractCheck`) y pruebas negativas F5.3. No introduce infraestructura extra ni libera Live. Los XSD externos CFA y la homologación de campos permanecen pendientes. El reporte `F5_3_IMPLEMENTATION.md`, el `PLAN.md` y el `MEMORY_STATE.md` 1.17.0 registran criterios de F6. **Siguiente microtarea: F6.1**. El material previo se conserva como historial.
