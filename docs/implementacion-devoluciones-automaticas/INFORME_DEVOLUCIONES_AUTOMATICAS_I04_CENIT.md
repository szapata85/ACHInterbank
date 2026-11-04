# INFORME TÉCNICO — I04-CENIT — 2026-11-03

**Base exclusiva:** `ACHInterbank_COMPLETO_DEVOLUCIONES_AUTOMATICAS_I03_CENIT_2026-11-03.zip` (SHA-256 `31bf10972c6b7e2a5dd087e44fcde15d298ac0ec00ae0c1cfa1dac3a215224e2`). **Producción: NO-GO**.

## Alcance implementado

I04 incorpora una revisión de segundo operador exclusiva CENIT sobre las preparaciones I03 existentes. Se utilizó la misma tabla `CenitReturnPreparations` y la auditoría inmutable `AuditLogs`; NO se creó otra tabla ni un motor adicional.

- `GET /api/cenit/devoluciones-automaticas/revisiones`: lista de reservas CENIT (máximo 100) con sus estados y metadatos de revisión; consulta no valida aprobación futura.
- `POST /api/cenit/devoluciones-automaticas/revisiones/{preparationId}/decision`: `APROBAR` o `DESCARTAR` explícitos, solo desde `PREPARADA_SIN_ENVIO`; requiere `ExpectedStatus`, justificación y, si aprueba, referencia identificable de evidencia (no constituye validación documental de la evidencia externa).
- `GET /api/cenit/devoluciones-automaticas/revisiones/excepciones`: filtra hallazgos `REVISION_CAUSAL`, `REVISION_SOLICITUD`, `REVISION_PRENOTIFICACION` del detector I02, conservando la marca `LimitedWindow`. Es **solo lectura** y no crea ni aprueba reservas. La ventana no abarca necesariamente todo el histórico.
- Estado final: `APROBADA_SIN_ENVIO` o `DESCARTADA`; no reabrir en I04. No existen métodos nuevos para producir archivo NACHA-M, reservar consecutivos, enviar a CENIT, aplicar movimientos ni conciliar.

## Seguridad y restricciones

El POST requiere JWT autenticado y política existente `P1.ConfigManage`; las consultas usan `P1.ConfigRead`. El ID del revisor se obtiene del principal (`uid`, `sub` o NameIdentifier), nunca del JSON enviado. La segregación de personas se confirma frente al `AuditLogs` original de creación de la reserva I03: si no hay trazabilidad de preparador o figura `system`, se **rechaza la decisión**. Un preparador no puede aprobar ni descartar su propia reserva.

Para **aprobar**, dentro de transacción serializable en proveedor EF relacional, se verifica otra vez: modo `CON_APROBACION`/`AUTOMATICO`, ausencia de devolución generada, tipo monetario permitido, causal ordinaria CENIT, catálogo activo, última ejecución SOAP realmente funcional I02 (sin ejecución posterior superadora), ID de cola, vigencia de calendario (día hábil/festivo), ciclo siguiente reglamentario, fecha valor, política de ciclo y cutoff. La capa EF conserva `Status` como token de concurrencia y `SaveChangesAsync` alimenta `AuditLogs`. Para **descartar**, no se exige modo activo ni ventana, permitiendo cerrar reservas inseguras. Las pruebas relacionales de competencia simultánea quedan pendientes de ejecutarse.

## Excepciones CENIT deliberadamente no automatizadas

La causa R06 requiere petición acreditada del originador, disponibilidad según el momento y condiciones del Banco. R23 exige reclamación del receptor, posible plazo de 15 días, trámite dentro del siguiente día hábil y monto original sin fraccionamiento. Prenotificaciones requieren su propio flujo, y devoluciones de devoluciones R60–R69 y excepciones de fechas/ciclos necesitan validaciones separadas. Se conserva la gestión manual existente; ninguna de estas excepciones puede aprobarse como una preparación ordinaria en I04. El código tampoco transforma rechazos Dxx de archivos en devoluciones. Fuentes: `CENIT-DSP-152-Anexo-2.md`, `CENIT-Anexo-A-Causales-Devolucion.md`, `CENIT-Anexo-B-Causales-Rechazo.md`, `Manual de Especificaciones Formato NACHA-M CENIT.md` adjuntos al proyecto.

## Modificaciones concretas

| Tipo | Archivos |
|---|---|
| Nuevos | `CenitReturnReviewService.cs`, `ICenitReturnReviewService.cs`, `CenitReturnReviewModels.cs`, `CenitReturnReviewController.cs` |
| Nuevos | Migración `20261103143000_CenitReturnReviewI04.cs` en SQL Server y PostgreSQL |
| Nuevos | `CenitReturnReviewI04Tests.cs` y verificador `automatic_returns_i04_cenit_static_check.py` |
| Modificados | Entidad `CenitReturnPreparation`, configuración EF, DI, dos snapshots y visibilidad del helper de pruebas I03 |
| Sin cambios | Lógica financiera y generadores de ACH Colombia, servicios CENIT manuales, SPA Angular, jobs y transporte |

## Evidencia y límites

- ZIP I03 fuente íntegro (4.903 entradas, `unzip -tq` sin errores) y SHA-256 de origen confirmado.
- **Estáticas I04-CENIT: 71/71**, regresión CENIT **I01 29/29, I02 50/50, I03 57/57**.
- Regresión ACH **I02 18/18, I03 25/25, I05 33/33, I06 28/28, I07 24/24, I08 25/25**; el I04-ACH histórico sigue **32/35** (NO aprobado), sin cambios en su fuente.
- **No ejecutadas**: `dotnet build/test` (falta SDK .NET 10), migraciones sobre SQL Server/PostgreSQL, concurrencia efectiva en dos conexiones, verificación de principal JWT/roles en API, pruebas funcionales Angular, homologación CENIT, pruebas de ventanas bancarias especiales y contingencias.

**I04 incorporado al código, NO-GO para producción.** Siguiente I05-CENIT: generación de artefacto NACHA-M controlado sin transmisión, solo cuando haya pruebas y puerta de seguridad. No iniciar en I04.
