# Contrato I04 — Devolución de prenotificación recibida

**Origen:** `NachaInboundSimulationService.GeneratePrenotificationReturnAsync` (I03), salida para descarga manual. **Entrada:** `NachaUploadController` y `IncomingNachaIngestionAppService` sin endpoint nuevo.

**Correlación normativa:** `AddendaRecord.CodeTypeAddendumRecord=99` y `OriginalTraceNumber` coincide con `AchTransaction.TraceNumber` de una prenotificación saliente. `EntryDetail.SequenceNumber` es diferente del original; `Amount=0` y operación original con importe 0. El `IncomingNachaTransactionLinker` debe devolver vínculo determinista (trace original exacto) o relación manual final verificable.

**Contexto:** `ResolvedClearingHouseId` coincide con el `AchCycle.ClearingHouseId` del original. Si el archivo coincide por SHA-256 con I03, se exige coincidencia exacta de cámara, instituciones invertidas, causal y pertenencia de la operación a `NachaInboundSimulation.Entries`. La huella SHA mayúscula/minúscula se normaliza.

**Estado:** intento de validación vigente pendiente (`Sent/AwaitingResponse`) o reupload idéntico de rechazo ya aplicado. Si el intento está terminal con resultado distinto, bloquear para revisión manual. Los registros no asociados a prenotificación continúan por el pipeline previo sin pasar por reglas nuevas de prenotificación.

**Salida:** estado y auditabilidad determinados por el servicio oficial, nunca por el simulador. Los eventos `PrenotificacionReturnCorrelacionada`, `PrenotificacionReturnBloqueada` y los eventos oficiales de transición documentan cada etapa. No se alteran estados si falla la guarda.

**Limitaciones:** la semántica byte a byte, origen del archivo transformado por cifrado, plazos operativos, concurrencia y resultado real de NachaUpload necesitan fixture y ejecución .NET/BD. El contrato no declara nada homologado en UAT.
