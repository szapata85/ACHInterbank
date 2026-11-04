# F4.1 — Política de elegibilidad ACH Colombia por entrada, sin ciclo interno

**Programa:** `ACHCOL-NACHA-CFA-EXTCYCLE-001`  
**Fecha:** 2026-10-24  
**Base:** `ACHInterbank_ACHCOL_CFA_F3_3_UNIFICADO.zip`  
**Entrega:** `ACHInterbank_ACHCOL_CFA_F4_1_UNIFICADO.zip`  
**Estado:** `IMPLEMENTED_UNVERIFIED`  
**Siguiente:** F4.2 — integración controlada con la orquestación/worker, sin permiso Live.

## Alcance y decisiones

F4.1 agrega `EvaluateFileBasedAsync` al contrato de `IIncomingNachaDispatchEligibilityPolicy`, implementado por `IncomingNachaDispatchEligibilityPolicy` y utilizado por `IncomingNachaDispatchPlanner.PlanFileBasedAsync` antes de generar la clave/idempotencia y agregar un registro a cola. La política heredada `EvaluateAsync` **no cambia**: sigue aplicando las ventanas de `AchCycle` y `ICycleTransactionPolicy` solo al origen `CycleBound`. La ruta NACHA nunca necesita un `AchTransaction` ni construye ciclos internos de respaldo.

Se evalúan de manera obligatoria:

1. Flag de ACHCOL habilitada en el planner; ingesta `Completado`, `ResolutionMode=AchColFileBased`, `CycleResolutionStatus=ResueltoSinCiclo`, `ResolvedAchCycleId=null`, fecha operativa y cámara resueltas.
2. Cámara activa, identificador coincidente y código exacto `ACHCOL`. No se permite caer a CENIT.
3. Nombre físico `RRRRTTT.ZZZ.yyyyMMdd.N.OUT[.env]` validado de nuevo por parser específico; `FileSequence=ZZZ`, `ExternalCycleNumber=N`, modificador A–Z/0–9, fecha, protección de sobre y nombre canónico coherentes. `.8/.9/.10` válidos como valores independientes del consecutivo.
4. Clasificación y registro detalle asociados a la **misma** ingesta/encabezado, coherencia `EntryDetailId`, `NachaID` y modificador de header.
5. Clasificación `Elegible`, sin estado manual, sin causal de devolución, sin trace original de retorno y `PrenoteStatus=NoAplica`.
6. Únicamente códigos **monetarios** autorizados por el perfil tipo-6 ACHCOL existente: crédito `22/32/52` con `CreditoEntrante` o débito `27/37/55` con `DebitoEntrante`. `23/28/33/38/53/57` (prenotificaciones), clases de devolución/retorno/ambigua, códigos desconocidos y discrepancias de dirección se rechazan en modo seguro.
7. Identidad de negocio positiva y determinística con `IncomingNachaFileBasedDispatchIdentity.TryBuild` (trace de 15 dígitos, cuenta, lote, origen/destino, monto positivo); sin exponer PII en el JSON de evidencia.

**Resultado y seguridad de despacho:** solo las entradas elegibles ingresan al planificador `AchColFileBased`; se persisten **`Blocked`**, con `ACHCOL_DISPATCH_NOT_ENABLED_F33` (código de bloqueo heredado de F3.3) y mensaje F4.1, sin `NextAttemptAtUtc` ni worker SOAP. El resultado de la política es una **clasificación de elegibilidad**, no autorización para ejecutar `Proc_Transacciones`. F4.2/F5 controlarán la habilitación futura con guardas y homologación. Las entradas rechazadas por esta política no generan filas despachables.

## Archivos fuente modificados

- `src/Cfa.ACHInterbank.Application/ACH/Interfaces/IIncomingNachaDispatchEligibilityPolicy.cs` — firma file-based sin `AchCycle`/`AchTransaction`.
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaDispatchEligibilityPolicy.cs` — política ACHCOL fail-closed; no modifica método legacy.
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaDispatchPlanner.cs` — aplica decisión previa a planificar sin habilitar transporte.
- `tests/Cfa.ACHInterbank.Tests/IncomingNachaFileBasedDispatchEligibilityF41Tests.cs` — casos unitarios positivos/negativos sin dependencias externas.
- `tests/Cfa.ACHInterbank.Tests/IncomingNachaFileBasedDispatchPlannerF33Tests.cs` — fixture de protección y regresiones de código prenotificación mal clasificado.

**Sin migraciones nuevas, alteración de snapshots ni cambios en CENIT/RET, MFT/SFTP, sobre digital o cliente SOAP.**

## Decisiones técnicas de F4.1

- **DEC-047:** Aislar elegibilidad ACHCOL file-based en un método distinto; no aplicar ventanas de un `AchCycle` inexistente ni modificar `CycleBound`.
- **DEC-048:** Allowlist monetaria ACHCOL y coincidencia obligatoria de dirección/crédito-débito; una clasificación aparentemente monetaria no excusa código de prenotificación o retorno.
- **DEC-049:** Revalidar identidad física e integridad de asociaciones ingesta/header/entry antes de planificar; rechazar cualquier desajuste incluso si F1/F2 ya validaron.
- **DEC-050:** `IsEligible` no equivale a `CanSend`; mantener `Blocked` y el flag OFF por defecto; no ejecutar SOAP/Live en F4.1.
- **DEC-051:** Evidencias de política sin PII; identidad SHA-256 e idempotencia UNIQUE existentes se conservan. No hay ensayo ni certificado de concurrencia.

## Comprobaciones y limitaciones

- **Hechas:** inspección estática de contrato/reglas NACHA tipo-6, integración del método en planner, verificaciones estructurales, `unzip -tqq` satisfactorio y comparación CRC32/tamaño de todas las entradas heredadas frente a F3.3 (sin comparación SHA-256 integral por entrada).
- **Escritas, no ejecutadas:** pruebas xUnit de H/008/.8, I/009/.9, H/008/.10; direcciones, prenotificaciones, clases/estado, cámara, nombre, fecha, envelope, vínculo header, trace/monto, feature flag. Las pruebas de regresión `CycleBound` permanecen en el proyecto.
- **Pendientes F6:** `dotnet build/test`, pruebas DB PostgreSQL/SQL Server y carreras UNIQUE, confirmación de semántica del contrato CFA, DryRun/QA, homologación y UAT. En este entorno no se dispone de SDK .NET 10; no se declara ejecución ni aprobación.

**Riesgo pendiente:** la política F4.1 no sustituye la política operativa de ventanas CFA que deberá definirse explícitamente para esta fuente; tampoco resuelve la transmisión, mapping ni reintentos inciertos. No autorizar Live hasta F5/F6 y aprobación operativa.
