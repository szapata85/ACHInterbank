# F3.3 — Planificación `AchColFileBased` e idempotencia por entrada NACHA-M

**Programa:** `ACHCOL-NACHA-CFA-EXTCYCLE-001`  
**Fecha:** 2026-10-24  
**Base:** `ACHInterbank_ACHCOL_CFA_F3_2_UNIFICADO.zip`  
**Entrega:** `ACHInterbank_ACHCOL_CFA_F3_3_UNIFICADO.zip`  
**Estado:** `IMPLEMENTED_UNVERIFIED`  
**Siguiente:** **F4.1** — política de elegibilidad ACHCOL y guardas independientes de ciclo.

## Objetivo y decisión de implementación

Se habilita **planificación persistente, no despacho** de entradas monetarias de un NACHA-M ACH Colombia en modalidad `AchColFileBased` cuando no hay `AchCycleId`. Las filas planificadas son expresamente `Blocked` (`ACHCOL_DISPATCH_NOT_ENABLED_F33`), no `Queued` ni `RetryPending`; worker/transportes/mapper CFA no son habilitados en esta fase. Feature flag `AchCol:AllowCycleIndependentInboundDispatch` continúa **OFF por defecto**; incluso encendida no autoriza `Live`.

**Separación de orígenes:** el `IncomingNachaDispatchPlanner` sigue utilizando íntegramente el flujo `CycleBound` para los demás casos. Para `ResolutionMode=AchColFileBased`, cualquier estado no `ResueltoSinCiclo`, ciclo asignado indebidamente, flag deshabilitado o contexto incompleto **no** cae al planificador de ciclos. Solo se planifica si existe cámara activa de código `ACHCOL`, fecha/correlación persistidas y clasificación de crédito/débito elegible (no devolución/prenotificación, no revisión manual).

El procesador post-parse evita intentar correlacionar un crédito/débito ACHCOL `ResueltoSinCiclo` contra una **transacción interna inexistente**. Conserva el linker histórico para RET, CENIT, devoluciones y demás clases funcionales. Se persiste la clasificación sin fabricar un `IncomingNachaTransactionLink`.

## Esquema relacional en ambos proveedores

`IncomingNachaDispatchQueue.IncomingNachaTransactionLinkId` pasa a `Guid?` **opcional**. F3.1/F3.2 ya habían hecho opcionales `AchTransactionId`/`AchCycleId` y obligatorio `EntryDetailId` para NACHA. F3.3 ajusta el CHECK por fuente:

- `CycleBound`: transacción, ciclo y link **obligatorios**; `EntryDetailId` nulo.
- `AchColFileBased`: `EntryDetailId` obligatorio; `AchTransactionId` y `AchCycleId` **nulos**; link opcional (admite historial previo, sin obligarlo).
- Otros `SourceKind` rechazados por constraint. FK de link permanece `Restrict`; no cambia índice único `IdempotencyDispatchKey`.

Migraciones aditivas en orden EF tras F3.1/F3.2:

- PostgreSQL: `20261024235500_AchColFileBasedQueuePlannerF33.cs`.
- SQL Server: `20261024235500_AchColFileBasedQueuePlannerF33.cs`.
- Ambos snapshots del modelo EF actualizados. `Down` se niega a restaurar la nulabilidad antigua si existen filas con link nulo; exige conciliación, **sin borrar movimientos**.

## Identidad de negocio y concurrencia

`IncomingNachaFileBasedDispatchIdentity.TryBuild` calcula SHA-256 en mayúsculas, con componentes normalizados y separados por longitud: versión/operación `Proc_Transacciones`, cámara, fecha operativa, origen inmediato del encabezado, trace NACHA exacto 15 dígitos, lote, tipo de operación, entidad receptora, cuenta, identificación receptora y monto decimal invariante. La clave **no depende** de `AchCycleId`, `ExternalCycleNumber`, `FileSequence`, nombre del archivo, `IngestionId` ni contenido aleatorio del sobre `.env`. Así, el replay de la misma operación se reconoce aun con cambio de sufijo `.8/.9/.10` o de representación física.

**Fail-closed:** trace ausente/no numérico/no 15 caracteres, origen/recibidor/cuenta/código no informados, lote inválido, monto no positivo, estado manual/bloqueado o cámara no ACHCOL no generan fila despachable. La consulta compara `NachaHeader`/`EntryDetail`/clasificación **dentro de la misma ingesta**. Se usan `HashSet` de claves del lote y búsqueda previa en BD; la **última línea de defensa bajo carreras** es el índice UNIQUE existente. En una carrera real de inserciones la segunda operación puede fallar por UNIQUE y requerir reintento de proceso **sin envío automático**; el control transaccional/distribuido del worker y la reconciliación se completarán en F4/F6. El hash evita exponer cuentas en la clave, pero no sustituye controles de privacidad del resto de datos.

## Archivos de código y pruebas

1. `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaDispatchPlanner.cs` — planificador aislado y feature flag de admisión defensiva, filas `Blocked`.
2. `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaFileBasedDispatchIdentity.cs` — clave de negocio estable.
3. `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaPostParseProcessor.cs` — evita bloquear créditos/débitos ACHCOL legítimos por falta de link a transacción interna.
4. `src/Cfa.ACHInterbank.Domain/Models/ACH/IncomingNachaDispatchModels.cs` — link opcional.
5. `src/Cfa.ACHInterbank.Persistence/Configuration/IncomingNachaDispatchQueueConfiguration.cs` — CHECK de forma por origen.
6. Snapshots PostgreSQL/SQL Server y migraciones F3.3 de ambos proveedores.
7. `tests/Cfa.ACHInterbank.Tests/IncomingNachaFileBasedDispatchPlannerF33Tests.cs` — H/008/.8, I/009/.9, H/008/.10, replay, flag OFF, clases no procesables, datos inválidos, cámara errónea.
8. `tests/Cfa.ACHInterbank.Tests/IncomingNachaFileBasedDispatchSchemaF33Tests.cs` — contrato de modelo EF, SQLite para fila sin link vs rechazo del origen legacy inconsistente.

## Decisiones registradas

- **DEC-041:** La identidad monetaria es por entrada/operación y no por ciclo, archivo ni GUID de ingesta.
- **DEC-042:** Nueva planificación se almacena `Blocked`, sin `NextAttemptAtUtc` ni despacho automático mientras F4/F5 no estén homologados.
- **DEC-043:** Link histórico opcional solo para NACHA, explícitamente obligatorio para `CycleBound`; ambas bases migradas simétricamente con rollback protegido.
- **DEC-044:** Clasificación monetaria ACHCOL puede preservarse sin resolver AchTransaction; retorna/prenota siguen su circuito anterior.
- **DEC-045:** El índice UNIQUE existente es protección contra concurrencia; posibles conflictos de guardado no se interpretan como éxito o como autorización de retry financiero.
- **DEC-046:** Rechazo fail-closed de identidades ambiguas, origen no ACHCOL, estado sin resolución o feature flag OFF. CENIT/RET excluidos.

## Verificación / pendientes no ocultos

- **Ejecutado:** inspección estática, verificación de coincidencia esquema-modelo-migración, ausencia de referencias SOAP nuevas, integridad ZIP y contraste de entradas heredadas (registrados al cierre de empaquetado).
- **No ejecutado:** `dotnet build`, `dotnet test`/xUnit, SQLite en ejecución, migraciones reales PostgreSQL/SQL Server, pruebas con dos procesos/threads concurrentes, CFA DryRun/Live. SDK .NET 10 no disponible en el entorno de esta entrega; ambientes y QA final en F6.
- **Riesgo residual:** validez final de identidad de negocio frente al contrato CFA, colisiones de trace legítimas entre operaciones, carreras UNIQUE y política final de ventana ACHCOL requerirán escenarios homologados F4/F6. No se autorizan reenvíos automáticos por respuesta incierta.

**Siguiente F4.1:** política de elegibilidad por cámara/tipo/estado para levantar filas `AchColFileBased` solo bajo condición determinística, sin reactivar por error entradas `Blocked` de origen legacy. F4.2 integra el worker y F5 el mapping. Las pruebas finales de ambiente se realizan **después** de las implementaciones, en F6.
