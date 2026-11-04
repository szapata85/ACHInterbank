# I02-ACH — Detección pasiva de fallas de aplicación (ACH Colombia)

**Corte:** 2026-11-02. **Fuente:** ZIP íntegro I01-ACH. **Estado:** incorporado al código, NO verificado en ejecución. **Cámara:** ACH Colombia exclusivamente. **Producción:** NO-GO.

## Resultado funcional acotado

Se añadió `AchColombiaApplicationFailureDetector`, llamado por `IncomingNachaPostProcessingPipeline` inmediatamente después de obtener/registrar la respuesta de integración y de mantener la transición original del procesamiento. La detección usa la respuesta **funcional confirmada por el sistema central**; no genera devoluciones, archivos NACHA-M, transmisión, movimientos financieros ni alteraciones de estado adicionales.

La incidencia se guarda en `IncomingNachaProcessingEvents` con los tipos:

- `AchColApplicationFailureDetected` / `Detectada`: respuesta funcional confirmada con código literal Rxx también identificado por el resolvedor de respuestas de la misma cámara/tipo y catálogo de devoluciones existente. **La causal es sugerida**, no una devolución autorizada.
- `AchColApplicationFailureReview` / `RevisionCausal`: respuesta funcional confirmada sin causal inequívoca. No se inventa causal a partir de descripciones libres del SOAP.
- `AchColApplicationFailureDetectionUnavailable`: el detector pasivo falló y requiere revisión; el error de observación no reemplaza el resultado funcional del core.

Cada evento referencia `IncomingNachaFileIngestionId`, `AchTransactionId`, cuando corresponde `EntryDetailId`, y deja evidencia mínima (`queueId`, `executionId`, tipo, código de respuesta, causal sugerida y banderas de generación/envío siempre falsas). No persiste el mensaje libre del SOAP, para minimizar exposición de datos.

## Condiciones obligatorias de activación del detector

1. Modo REAL `Live` tanto en configuración usada en el despacho como en ejecución registrada (no `DryRun` ni `Disabled`).
2. Cola de origen `CycleBound`, vínculo a transacción original y consistencia entre IDs de cámara del ciclo, cola y respuesta; el código de cámara debe ser `ACHCOL`.
3. Tipo original crédito/débito/prenotificación, nunca devolución ni reversión.
4. Transporte exitoso, catálogo de respuesta presente con `Rejected`, rechazo funcional marcado, procesamiento completo, sin éxito, sin error técnico, sin política de reintento y sin revisión manual previa.
5. Deducción de causal **no permitida** a partir de textos. Un código `Rxx` solo se sugiere cuando la respuesta fue resuelta para cámara/tipo y consta como causal de devolución; en otro caso queda en revisión.

**Alcance observable real:** el procesador `CycleBound` actualmente limita parte de las prenotificaciones y la entrada `AchColFileBased` permanece sin transporte externo homologado. Las pruebas unitarias de los tres tipos validan la regla pasiva, **no prueban que hoy se obtengan respuestas LIVE de todos los tipos**. Las fallas de prevalidación de archivos, de formato, de enlace, de ventana o de transporte no se clasifican como imposibilidad real de aplicación de una cuenta.

## Idempotencia y límites

Se comprueba en los eventos **rastreado + persistido** que no exista ya incidencia para la misma transacción. Este control evita duplicados bajo procesamiento normal, pero no garantiza unicidad bajo procesos concurrentes independientes: la reserva única de candidatas/preparación transaccional queda reservada a **I03-ACH**. No crear nueva tabla/migración en I02.

La detección pasiva funciona incluso si el parámetro de devoluciones automáticas de I01 sigue `DESACTIVADO`, porque registra errores operativos; eso **no activa** generación, aprobación ni envío, y `ProcessingConnected` sigue siendo `false`.

## Archivos de código cambiados

- Nuevo `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchColombiaApplicationFailureDetector.cs`.
- Modificado `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaPostProcessingPipeline.cs` (instanciar detector y registrar después de la respuesta existente, con aislamiento de fallas del observador).
- Nuevo `tests/Cfa.ACHInterbank.Tests/AchColombiaApplicationFailureDetectorI02Tests.cs`.
- Nuevo `scripts/test/automatic_returns_i02_ach_static_check.py`.

**No se cambió** código de CENIT, servicios manuales, controlador de configuración, base de datos/migraciones, SPA ni infraestructura de envío de devoluciones.

## Pruebas y parada

- `python scripts/validate-solution-projects.py`: **PASS**, siete proyectos, referencias existentes.
- `python scripts/test/automatic_returns_i02_ach_static_check.py`: **PASS_STATIC 18/18**, examen de invariantes del código, sin sustituir el compilador.
- `dotnet build`, `dotnet test`, EF SQL Server / PostgreSQL, tests de SOAP real, pruebas de concurrencia y UAT: **NO EJECUTADAS POR AMBIENTE**. Test agregado no equivale a test superado.

**Parada:** I02-ACH, solo detección pasiva incorporada. Siguiente I03-ACH: preparación de devolución/idempotencia DB-first tras verificar las pruebas de I01/I02, sin autoenvío. CENIT independiente, NO INICIADO. Producción NO-GO.
