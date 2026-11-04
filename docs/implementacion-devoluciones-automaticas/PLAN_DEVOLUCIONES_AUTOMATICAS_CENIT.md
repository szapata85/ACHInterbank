# Plan independiente — devoluciones automáticas CENIT

**Fecha:** 2026-11-02. **Base funcional:** ZIP acumulativo I08-ACH. **Ejecución autorizada en esta entrega:** exclusivamente I01-CENIT. **NO-GO productivo**.

## Principios de separación

Cada cámara dispone de reglas de devolución y pruebas independientes. Compartir `ClearingHouseConfigs`, auditoría EF o políticas de permisos no autoriza usar la detección, las ventanas, los códigos ni el ejecutor de ACH Colombia para CENIT. `AUTOMATICO` es una selección futura, no conexión a runtime. Sin generación, transmisión, programación ni conciliación por selección.

## Secuencia prevista (no implementada por anticipado)

| Incremento | Alcance futuro delimitado | Restricción de salida |
|---|---|---|
| **I01-CENIT** | Parámetro CENIT, permisos y auditoría, consulta/PUT, validación y concurrencia, valor inicial `DESACTIVADO` | **Implementación en fuente en esta entrega**, sin procesamiento |
| I02-CENIT | Detectar casos reales en flujo CENIT según evidencias de aplicación, sin emitir devoluciones | Solo detección pasiva, causal CENIT verificable |
| I03-CENIT | Preparación sujeta a plazos, fecha valor, sesión y deduplicación CENIT | Sin transporte autónomo; NACHA-M conforme manual |
| I04-CENIT | Manejo de solicitudes/excepciones R06, R23 y devolución de devolución, controles y aprobación | Determinar reglas particulares antes de autorizar generación |
| I05-CENIT | Eventual generación controlada de artefactos CENIT para escenarios homologados | No reusar runner ACH; no salida no autorizada |
| I06-CENIT | Integración comprobada de transporte, acuse y conciliación individual | No equiparar archivo a liquidación; homologación y UAT |

## Condiciones normativas que I01 registra, sin implementar

1. **Sesiones:** el Anexo 2 DSP-152 de 27/02/2025 describe cinco sesiones y diferencias por tipo de transacción; verificar calendario y días hábiles antes de I02/I03. El manual operativo CENIT Web (13/06/2025) separa expresamente la quinta sesión para devoluciones y devolución de devolución.
2. **Devolución ordinaria y fecha valor:** la devolución de entrada monetaria recibida por ciclo debe realizarse en el ciclo siguiente y normalmente con la misma fecha valor; existe excepción posterior por rechazo del receptor, con condiciones y plazos particulares (Anexo 2, apartado 4.2). No extrapolar a todas las operaciones.
3. **R23:** el Anexo A contempla solicitud del receptor para crédito, hasta 15 días y sin devolución parcial; exige control de notificación y oportunidad, no un evento automático por un simple estado.
4. **R06:** solicitada por originador/participante originador, según Anexo A, con condiciones de oportunidad y fondos, solo valor original y posibilidad de acuerdo por fuera del sistema.
5. **Devolución de devolución:** las causales R60–R69 (y condiciones complementarias posteriores) son distintas de las devoluciones ordinarias; el Anexo 2 establece límites por ciclo/fecha y una sola aplicación.
6. **Prenotificaciones:** débito previamente obligatorio con adenda; crédito opcional; devolución de prenotificación con ventana de fecha valor específica (Anexo 2, 4.7–4.8).
7. **NACHA-M:** Manual de Especificaciones 07/05/2026: registros de longitud 106, características por PPD/CCD/CTX, adendas requeridas y archivos CTX separados. No construir ni homologar archivos en I01.
8. **Rechazos STA frente a devoluciones:** Anexo B describe causales D01–D06 de rechazo de archivos de transferencia; NO tratarlas como causales de devolución de transacciones del Anexo A.
9. **Homologación:** no asumir que servicios/manuales CENIT existentes están certificados por el hecho de estar en fuente. Exigir pruebas unitarias, SQL Server/PostgreSQL, regresión manual, golden files, validación de permisos, bitácora/auditoría y homologación por cámara.

## Fuentes adjuntas y diferencias a verificar

- `CENIT-DSP-152-Anexo-2.md` (edición visible 27/02/2025).
- `CENIT-Anexo-A-Causales-Devolucion.md` y `CENIT-Anexo-B-Causales-Rechazo.md` (fecha documental visible 28/11/2023).
- `Manual de Especificaciones Formato NACHA-M CENIT.md` (07/05/2026).
- `Manual de Usuario CENIT.md` (13/06/2025).
- `ceos_dsp_152_MAY_27_2022.md` (nombre de archivo histórico; el contenido adjunto muestra modificaciones posteriores, incluidas de 2025).

**Discrepancia abierta:** las fechas/versiones no son uniformes entre anexos, circular y especificación técnica. Antes de I02, contrastar vigencia y homologación sin fabricar un conjunto de reglas consolidado ni copiar políticas ACH Colombia.

## Parada

I01-CENIT se cierra **solo como código incorporado, sujeto a ejecución y homologación**. No iniciar I02 hasta una nueva instrucción. Se preserva I09-ACH pendiente.
