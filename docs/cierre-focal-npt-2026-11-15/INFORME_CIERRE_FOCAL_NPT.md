# Informe de implementación — cierre focal NPT-03/04/06/08/09/10/11

**Fecha del incremento:** 2026-11-15. **Decisión:** NO-GO para SOAP Live monetario. **Base SHA-256:** `3f99534d05603ba054faf90f782f6950bbd2a461121a1611c09ffbf24161a448`.

**Alcance:** corrección real de fuente en siete hallazgos de la solicitud focal. No se recibió en el material accesible un archivo nuevo de reauditoría independiente posterior al cierre anterior; se aplicaron las brechas concretas especificadas por el usuario y se conservó la auditoría original.

## Matriz de cierre por hallazgo

| ID | Estado | Evidencia principal | Ejecución | Riesgo residual |
|---|---|---|---|---|
| NPT-03 | Corregido en fuente pendiente de prueba | UPDATE condicional por token+estado+lease+sin conciliación, transacción breve sobre cola/resultado/auditoría. Evidencia tardía separada. | Fuente/modelo SQLite; C# escrito sin ejecutar | Validación SQL real de traducciones EF y funcionamiento multiinstancia pendiente. |
| NPT-04 | Corregido en fuente pendiente de prueba | Leases vencidos pasan a Blocked+RequiresReconciliation y se auditan; nunca regresan a cola; cae antes del transporte con código separado. | Fuente/modelo SQLite; C# escrito sin ejecutar | Recovery está vinculado a ejecución del pipeline Quartz; confirmar frecuencia operacional y crash tests con proveedor real. |
| NPT-06 | Corregido en fuente pendiente de prueba | Errores posteriores a intención SOAP se clasifican como inciertos sin reenvío, conservando timeout y evidencia. | Fuente/modelo SQLite; C# escrito sin ejecutar | Hace falta transporte simulado con fallos reales, no basta el modelo SQLite. |
| NPT-08 | Corregido en fuente pendiente de prueba | CAS Axon para pendientes/preflight y finalización con token, lease, estado padre, versión optimista y auditoría en commit transaccional. | Fuente/modelo SQLite; C# escrito sin ejecutar | Ejecución xUnit y transacción real en SQL Server y PostgreSQL pendiente. |
| NPT-09 | Corregido en fuente pendiente de prueba | Watchdog independiente, cuarentena de InFlight y salida de cancelación tras transporte con token de persistencia independiente; evento tardío por hash. | Fuente/modelo SQLite; C# escrito sin ejecutar | Simulación de cancelación real y reinicio del host aún pendiente. |
| NPT-10 | Corregido en fuente pendiente de prueba | Envelope SOAP 1.1/1.2, body único, operación y namespace, Fault, RTAACH único directo, RTALOC único, campos inesperados bloqueados, DTD prohibido. | Fuente/modelo SQLite; C# escrito sin ejecutar | Importaciones XSD xsd0/xsd1 no disponibles; no se afirma cumplimiento XSD. |
| NPT-11 | Corregido en fuente pendiente de prueba | Cámara + referencia, única entrada física, vínculo final NACHA y archivo de origen; ambigüedad y referencia duplicada a revisión manual. | Fuente/modelo SQLite; C# escrito sin ejecutar | Fecha/archivo de respuesta diferencial no homologados; no se utilizan como desempate artificial. Contrato oficial pendiente para ampliar correlación. |

## Archivos y métodos intervenidos

### NPT-03
**Fuente:** `IncomingNachaPostProcessingPipeline.cs / `TryClaimAsync`, `FinalizeClaimAsync`, `PersistPreparedRequestFencedAsync``.
**Cambio:** UPDATE condicional por token+estado+lease+sin conciliación, transacción breve sobre cola/resultado/auditoría. Evidencia tardía separada.
**Prueba:** CFA carreras y fencing en pruebas C# y modelo SQL SQLite; ejecución C# pendiente
**Riesgo/pendiente:** Validación SQL real de traducciones EF y funcionamiento multiinstancia pendiente.

### NPT-04
**Fuente:** `IncomingNachaPostProcessingPipeline.cs / `RecoverExpiredClaimsAsync`, `PreserveLateEvidenceAsync``.
**Cambio:** Leases vencidos pasan a Blocked+RequiresReconciliation y se auditan; nunca regresan a cola; cae antes del transporte con código separado.
**Prueba:** CFA lease + late result en prueba C# añadida; modelo SQLite ejecutado
**Riesgo/pendiente:** Recovery está vinculado a ejecución del pipeline Quartz; confirmar frecuencia operacional y crash tests con proveedor real.

### NPT-06
**Fuente:** `IncomingNachaPostProcessingPipeline.cs / `MarkTransportIntentAsync`, `catch InvalidOperationException`, `catch Exception``.
**Cambio:** Errores posteriores a intención SOAP se clasifican como inciertos sin reenvío, conservando timeout y evidencia.
**Prueba:** Revisión estática focal + matriz de escenarios para SOAP instrumentado
**Riesgo/pendiente:** Hace falta transporte simulado con fallos reales, no basta el modelo SQLite.

### NPT-08
**Fuente:** `AxonNotificationClaimStore.cs / `TryClaimAsync`, `TryFinishPreflightAsync`, `TryFinalizeAsync`; NotificarRespuestaAchUseCase.cs`.
**Cambio:** CAS Axon para pendientes/preflight y finalización con token, lease, estado padre, versión optimista y auditoría en commit transaccional.
**Prueba:** Prueba C# dos workers y preflight; modelo SQLite de dos conexiones ejecutado
**Riesgo/pendiente:** Ejecución xUnit y transacción real en SQL Server y PostgreSQL pendiente.

### NPT-09
**Fuente:** `AxonNotificationClaimStore.cs / `QuarantineExpiredAsync`, `RecordLateEvidenceAsync`; AxonNotificationRecoveryHostedService.cs; NotificarRespuestaAchUseCase.cs`.
**Cambio:** Watchdog independiente, cuarentena de InFlight y salida de cancelación tras transporte con token de persistencia independiente; evento tardío por hash.
**Prueba:** Prueba C# lease y late evidence añadida, modelo SQLite ejecutado
**Riesgo/pendiente:** Simulación de cancelación real y reinicio del host aún pendiente.

### NPT-10
**Fuente:** `ProcTransaccionesResponseParser.cs / `Parse``.
**Cambio:** Envelope SOAP 1.1/1.2, body único, operación y namespace, Fault, RTAACH único directo, RTALOC único, campos inesperados bloqueados, DTD prohibido.
**Prueba:** NptFocalProcTransaccionesParserTests.cs (escrito, no ejecutado), inspección estática focal
**Riesgo/pendiente:** Importaciones XSD xsd0/xsd1 no disponibles; no se afirma cumplimiento XSD.

### NPT-11
**Fuente:** `DifferentialPrenotificationResponseProcessor.cs / `ResolvePrenotificationMatchAsync`, `ResolvePrenotificationAsync``.
**Cambio:** Cámara + referencia, única entrada física, vínculo final NACHA y archivo de origen; ambigüedad y referencia duplicada a revisión manual.
**Prueba:** Dos pruebas C# añadidas a DifferentialPrenotificationResponseProcessorTests.cs; estática focal
**Riesgo/pendiente:** Fecha/archivo de respuesta diferencial no homologados; no se utilizan como desempate artificial. Contrato oficial pendiente para ampliar correlación.

## Puntos de seguridad verificados adversarialmente

- CFA: toda finalización normal y finalización por excepción requiere token/estado/lease/no conciliación y commitea estado de cola, transacción y auditoría en la misma transacción corta. Un worker antiguo conserva solo evidencia tardía de códigos y hashes en nuevo contexto.
- CFA: trazas que internamente guardan cambios ahora se ejecutan en transacción corta con comprobación del claim antes de SOAP; ninguna transacción abarca SOAP.
- CFA: el resultado incierto tras marcar intención, incluyendo InvalidOperationException, no puede originar replay; recuperación vencida no reactiva automáticamente la cola.
- Axon: dos notificaciones no pueden ganar el mismo claim, ni sobrescribir una cuarentena; actualizaciones de intento y AchResponse se sincronizan bajo SQL CAS y un nuevo valor de versión.
- Axon: incluso las validaciones previas al claim se terminan bajo CAS; un preflight tardío no pisa InFlight. El watchdog corre con independencia de nuevas notificaciones.
- Parser: éxito no se infiere desde RTAACH anidado, Fault, operación distinta, campos extra o namespace adulterado; falta validación XSD oficial.
- Prenotificaciones: no se infiere fecha/archivo original de un dato no homologado; doble candidato o archivo físico sin vínculo final resulta ambiguo.

## Límites de la evidencia
- Verificación nueva ejecutada: **26/26 fuente + modelo de concurrencia SQLite mediante Python**; dos conexiones SQL en los modelos monetario y Axon. **No ejecuta el código C#**, EF Core ni SOAP.
- Cinco suites estáticas/SQLite históricas ejecutadas: **28/28, 35/35, 39/39, 27/27, 31/31**, todos exit=0. No son pruebas xUnit.
- Verificador NPT antiguo: **41/42, exit=1**. La única aserción obsoleta exige `await _unitOfWork.CommitAsync(CancellationToken.None)` durante cancelación Axon; la nueva solución utiliza `TryFinalizeAsync(..., CancellationToken.None)` condicional con auditoría. No se alteró el verificador histórico ni se proclamó que pasó. Ver `npt_static_guard.log`.
- `dotnet --info`, `restore`, `build` y `test` fueron invocados: salida **127** (`dotnet: command not found`). No hay compilación, xUnit, SQL Server, PostgreSQL ni SOAP instrumentado ejecutados en este entorno.
- Se añadieron casos C# y se preserva la matriz histórica de 38 escenarios. Su ejecución queda pendiente.
- No se realizó UAT ni se habilitó Live productivo.

## Hallazgos fuera del alcance
NPT-01, NPT-02, NPT-05, NPT-14 siguen **bloqueados por contrato externo**, sin cambios de fuente. NPT-07, NPT-12, NPT-13 se preservan sin modificar los respectivos correctivos anteriores.

## Migraciones
**Ninguna nueva.** Se reutilizan `DispatchClaimToken`, `DispatchLeaseExpiresAtUtc`, `TransportStartedAtUtc`, `RequiresReconciliation`, `ClaimToken`, `LeaseExpiresAtUtc`, `TransportStartedAtUtc` ya presentes en migraciones NPT-03/04 SQL Server y PostgreSQL del ZIP base. La versión de concurrencia `AchResponse.Version` ya existía. Ejecutar ensayo de migraciones de la base completa en ambos proveedores antes de Live.

**Estado por hallazgo:** ningún hallazgo se presenta como cerrado en producción: los siete se consideran `Corregido en fuente pendiente de prueba` hasta compilar y aprobar las pruebas instrumentadas en los proveedores reales.
