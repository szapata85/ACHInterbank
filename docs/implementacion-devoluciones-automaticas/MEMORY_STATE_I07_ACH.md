# MEMORY STATE — DEVOLUCIONES AUTOMÁTICAS — I07-ACH

**Corte:** 2026-11-02. **Incremento vigente:** I07-ACH, integridad de recuperación y trazabilidad operativa. **Base previa:** `ACHInterbank_COMPLETO_DEVOLUCIONES_AUTOMATICAS_I06_ACHCOLOMBIA_2026-11-02.zip`. **Base siguiente:** `ACHInterbank_COMPLETO_DEVOLUCIONES_AUTOMATICAS_I07_ACHCOLOMBIA_2026-11-02.zip`. **Estado productivo:** **NO-GO**. **CENIT:** sin cambios funcionales.

## Hechos de código incorporados y verificables

1. Se preserva el código de I01–I06: configuración por cámara, detección de rechazo funcional, preparación única, aprobación, generación explícita y generación autónoma bajo dos condiciones. `AchCol:EnableAutomaticReturnFileGeneration=false` permanece en el archivo entregado; no se activan transporte, compensación ni liquidación.
2. I07 refuerza exclusivamente `AchColombiaReturnReviewService.RecoverGeneratedFileAsync`: exige evento original `AchTransactionStateEvents` con clave `outbound-return-v1:{OriginalTransactionId}`, `ReturnedByEpr` y cámara ACH Colombia de la transacción.
3. Se verifican contra el evento original: identificador de transacción, cámara, nombre de archivo, ciclo de devolución, causal, cantidad de registros y devoluciones, y condición de **generado sin transmisión**. La huella `contentSha256` ya estaba en el evento histórico `ReturnFileGenerated`: se compara SHA-256 real del contenido reconstruido en tiempo constante. No se usa la huella declarada por el servicio de reconstrucción como sustituto de los bytes.
4. Si falta evento/huella, los metadatos difieren o los bytes reconstruidos no corresponden al original, la descarga falla de manera controlada con `AchColombiaReturnReviewConflictException`. **No** regenera archivo, no consume secuencia, no transmite, no modifica estado ni intenta reparar datos automáticamente.
5. La consulta de candidatas aclara «Archivo generado, sin envío. La descarga verificará su integridad». No se cambió la SPA, ninguna tabla, migración, contrato HTTP, lógica CENIT ni el generador NACHA-M.
6. Se añadieron pruebas xUnit I07 para contenido alterado, evento faltante, JSON inválido, archivo/cámara/causal/estado inconsistentes, verificación positiva y aislamiento CENIT. Se ajustó el escenario positivo I06 para representar el evento persistente real.

## Evidencias y estado de pruebas

- `scripts/test/automatic_returns_i07_ach_static_check.py`: **24/24 PASS_STATIC** (solo análisis de fuente).
- Comprobaciones estáticas históricas I02 18/18, I03 25/25, I05 33/33, I06 28/28. El verificador I04 ahora marca 32/35 porque contiene tres reglas prohibiendo generar/descargar archivos, sustituidas intencionalmente desde I05/I06; NO se afirma éxito completo de ese verificador.
- Grafo de solución: 7/7 proyectos, referencias presentes. **No equivale a compilación**.
- **NO EJECUTADO**: .NET SDK (no disponible), xUnit, Angular/Karma, migraciones o integración SQL Server/PostgreSQL, simulaciones de corrupción reales, concurrencia multinstancia, comparación NACHA-M original/reconstruido, homologación ACH. No atribuirles `PASS`.

## Riesgos/bloqueos abiertos

- `AchReturnStateTransitionComponent` asigna `ReturnedByEpr` durante **generación**; `AchReconciliationReportService` y `AchOperationalReconciliationService` interpretan estados ReturnedByEpr en informes. **No equiparar esos informes a devolución efectivamente transmitida, aceptada, compensada o liquidada** sin verificar eventos de transmisión/compensación. Cambio transversal NO autorizado para I07.
- El constructor de artefacto reconstruido puede producir bytes diferentes al original; I07 lo detecta y bloquea recuperación, pero NO corrige aún equivalencia ni guarda archivo binario original. Ese trabajo requiere prueba de golden file y decisión operativa antes de abrir flag.
- Los archivos históricos sin huella/evento de origen verificable quedan **sin recuperación mediante este endpoint**, para revisión manual. No inventar huellas retroactivamente.
- Confirmar calendario vigente ACH Colombia: manual V35 de cinco ciclos y anexo de nuevos ciclos de siete presentan discrepancia documental; validar reglas reales y ventanas antes de habilitar generación autónoma.
- Persisten pendientes SQL Server/PostgreSQL (migraciones I01/I03, concurrencia), integridad contable, acuses/certificación y contrato transporte de la cámara.

## Continuación

**Punto exacto de parada:** I07-ACH incorporado en fuente, con comprobación estática, pruebas xUnit escritas y NO ejecutadas, **NO-GO**. **Siguiente I08-ACH:** ejecutar batería de pruebas y homologación disponible, comparar archivo original vs reconstruido byte a byte, validar reportes/estado financiero y solo entonces decidir el acople de transporte/conciliación sin alterar CENIT. Si faltan ambientes, continuar correcciones seguras y documentar el bloqueo sin afirmar aprobación. Actualizar esta memoria sin sobrescribir `MEMORY.md`/`MEMORY_STATE.md` raíz, que pertenecen a otras iniciativas.
