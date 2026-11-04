# MEMORY_STATE — DEVOLUCIONES AUTOMÁTICAS — I06-ACH

**Corte:** 2026-11-02. **Último incremento:** I06-ACH (conexión protegida de generación autónoma y recuperación). **Base obligatoria para continuar:** `ACHInterbank_COMPLETO_DEVOLUCIONES_AUTOMATICAS_I06_ACHCOLOMBIA_2026-11-02.zip`. **CENIT:** sin cambios. **Estado productivo:** **NO-GO**.

## Hechos verificables incorporados en I06

1. Se conservan I01–I05 y sus documentos históricos. La configuración de ACH Colombia sigue persistida por cámara: `DESACTIVADO` / `CON_APROBACION` / `AUTOMATICO`.
2. Se añadió puerta técnica `AchCol:EnableAutomaticReturnFileGeneration`, **false** en `appsettings.json`. **Para generar de forma autónoma se necesitan ambas condiciones:** puerta técnica en true en despliegue **y** modo persistido `AUTOMATICO` para ACH Colombia. En los artefactos entregados no se habilita ninguna generación autónoma.
3. El procesamiento existente `IncomingNachaPostProcessingOrchestrator` conecta un ejecutor interno solo si la puerta está abierta y el servicio de revisión existe. Después de persistir resultados funcionales, prepara candidatos (I03) y puede generar archivos en una pasada limitada a 10. No se añade tarea Quartz, servicio residente, controlador para iniciar autonomías ni transporte.
4. Solo se consideran candidatas `PREPARADA_SIN_ENVIO` o `APROBADA_SIN_ENVIO` de ACHCOL. El servicio comprueba de nuevo cámara, modo AUTOMATICO, causal, evidencia confirmada I02, ciclo, vigencia, duplicados y R10. Reutiliza `IAchReturnsService`, el bloqueo y la unicidad de `AchReturnsGenerated`. En automático pasa directamente a `ARCHIVO_GENERADO_SIN_ENVIO` tras verificar el registro persistido (no afirma aprobación humana).
5. El flujo manual I05 conserva su regla: requiere `APROBADA_SIN_ENVIO` y un clic. En modo `CON_APROBACION` la nueva ruta automática no funciona.
6. Nueva recuperación de archivo ya generado: `GET api/ach-colombia/devoluciones-automaticas/candidatas/{candidateId}/archivo` con `P0.ReturnsGenerateFile`. Solo acepta candidata de ACHCOL en `ARCHIVO_GENERADO_SIN_ENVIO`, cruza `AchReturnsGenerated` y reconstruye mediante `IAchOutboundReturnArtifactService`, comprobando cámara, ciclo y transacción única. No usa generador, no consume nueva secuencia ni transmite. Respuesta incluye el nombre oficial del archivo; la SPA muestra **Descargar archivo existente** en el mismo componente Angular Material.
7. Ante error de ejecución automática, se registra advertencia sin detalles financieros y se detiene el lote; no se reintenta ciegamente. Sigue sin existir transmisión automática, acuse, conciliación automática ni homologación.
8. `ProcessingConnected` informa solo conexión de **generación interna** cuando ambas condiciones están activas; no significa transporte ni disponibilidad productiva.

## Límites y riesgos abiertos

- El generador histórico cambia estado interno de transacción a `ReturnedByEpr` al **crear** el archivo. Eso NO implica envío, aceptación ni liquidación; validar semántica con pruebas reales antes de activar el flag.
- Aún no se ejecutaron xUnit, compilación .NET, pruebas completas Angular, integración SQL Server/PostgreSQL, concurrencia multinstancia, acuses, comparación binaria del artefacto reconstruido ni homologación ACH Colombia. La reconstrucción no garantiza equivalencia bit a bit con el archivo generado inicialmente sin esas pruebas.
- Migraciones SQL Server I01/I03 y paridad PostgreSQL continúan pendientes de verificación en ambiente real; no se agregó migración nueva en I06.
- El calendario de cinco/siete ciclos de ACH Colombia permanece sujeto a confirmación operativa y certificación. Causales R10 sin evidencia y créditos tardíos no se automatizan.
- No afirmar que todo rechazo técnico o error de formato constituye devolución. Solo eventos funcionales confirmados I02 habilitan la preparación y posterior generación.
- Las candidatas que fallan repetidamente pueden requerir descarte o resolución manual; el ejecutor detiene el lote para evitar procesamientos inciertos y preservar trazabilidad.

## Evidencias de esta entrega

- `scripts/test/automatic_returns_i06_ach_static_check.py`: **28/28 PASS_STATIC**.
- Scripts históricos I02 **18/18**, I03 **25/25**, I05 **33/33**; I04 **33/35**, con dos comprobaciones de no-generación que se volvieron obsoletas desde I05.
- TypeScript afectado: cuatro archivos parseados sin error con TypeScript `transpileModule`; no equivale a compilación Angular.
- Referencias de seis proyectos fuente sin rutas faltantes; no equivale a `dotnet build`.
- Se añadieron xUnit I06 y Jasmine, **no ejecutados**: falta SDK .NET / dependencias del entorno / BD reales.
- Integridad ZIP y comparación de archivos acumulados documentadas en `MANIFIESTO_I06_ACH.json` y `EVIDENCIA_I06_ACH.md`.

## Continuidad y parada

**I06-ACH: incorporado en código, configuración técnica apagada, prueba ejecutable pendiente, NO-GO.**
**Siguiente trabajo solo si se solicita:** I07-ACH, pruebas de ejecución y homologación, reconciliación del ciclo de vida de archivos y evaluación del transporte. No activar el flag ni declarar aptitud productiva sin pruebas UAT documentadas. **CENIT continúa en ruta separada.**

La memoria original de raíz `MEMORY.md`/`MEMORY_STATE.md` no se modifica; esta es la memoria vigente de esta iniciativa.
