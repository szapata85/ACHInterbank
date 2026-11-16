# Revisión adversarial focal NPT-CFA-LATE-01

- **Doble intento:** `PreserveLateEvidenceAsync` ya no construye ni agrega `IncomingNachaIntegrationExecution`; su índice único permanece intacto.
- **Duplicado concurrente:** PK determinista `IncomingNachaProcessingEvents.Id` controla la carrera; tras colisión EF, se verifica el evento existente y su `evidenceKey`. Errores ajenos se propagan.
- **Rollback:** solo se inserta un evento; fallo del INSERT no afecta ejecución ni cola. Test con trigger SQLite incorporado, aún no ejecutado xUnit.
- **XML / código / correlación:** JSON conserva ambas tramas, hashes, respuesta, correlación y referencias de ejecución. Idempotencia basada en identidad semántica, excluyendo tiempos variables.
- **Histórico:** no hay UPDATE sobre execution, eventos previos o estado de cola. Dos respuestas distintas producen eventos distintos.
- **Fencing:** la ruta llamante hace rollback y `ChangeTracker.Clear()` antes de evidenciar; queda bloqueado el claim posterior por conciliación.
- **Sin segundo débito:** ninguna llamada SOAP en método; no modifica `RequiresReconciliation` ni `AttemptCount`.
- **Inspección de efectos secundarios:** un INSERT autónomo; no hay nueva tabla, FK, migración o dependencia.

Límite: la prueba efectiva de ejecución C# y concurrencia real de SQL Server/PostgreSQL sigue pendiente por falta del SDK/infraestructura. El comprobador NPT general previo sigue fallando 1/42 por expectativa de Axon ya sustituida; no se alteró para forzar PASS. **Estado real: corregido en fuente, pendiente de prueba.**
