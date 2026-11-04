# F01 — Informe de correcciones: simulación, neteo y estados definitivos CENIT

**Fecha:** 2026-11-02. **Fase:** F01/08. **Estado de entrega:** IMPLEMENTADA Y REVISADA ESTÁTICAMENTE — VERIFICACIÓN .NET/DB/QUARTZ PENDIENTE. **Hallazgos atendidos:** HC-CENIT-003, HC-CENIT-005, HC-CENIT-006. Ninguno CERRADO.

## Inspección productiva

`AchCycleSchedulerHandler.ExecuteAsync` identifica `CENIT` y llama `CenitCycleExecutionService.StartExecutionAsync`. Antes, este consumía `CenitCycleQueues` y ejecutaba `CenitNettingService.CalculateAsync` + `LiquidityOptimizationService.OptimizeCycleAsync`; el neteo declaraba todos los detalles liquidados y la optimización simulada movía operaciones o las marcaba `ReturnedByOperator` (C4/C5). La ruta independiente oficial `CenitChamberResponseService` se conserva SIN MODIFICACIONES. Ver diff.

## Cambio mínimo aplicado

1. Fin de ventana CENIT → `CenitCycleExecution.Status=AwaitingOfficialResult`, sin afirmar liquidación, rechazo ni fecha de resultado oficial. No ejecutar simulación en Quartz. La cola no se marca `Consumed` hasta la incorporación efectiva que se implementará en F03.
2. `CenitNettingService`: estimación identificable; detalle `IncludedInSettlement=false` y razón explícita; retorno idempotente por ejecución. Datos históricos no migrados ni recalificados.
3. `LiquidityOptimizationService`: no modifica `AchTransaction`, `AchBatch`, `ContrapartidaDispatchItem`, colas ni eventos; registra exclusivamente una proyección `Simulated*` si se invoca expresamente; utiliza liquidez interna simulada (no CUD). No se utiliza para decisiones financieras definitivas.
4. `TransactionPriorityPolicy`: consulta SEC existente y tipo/código; incorpora los diez órdenes de Anexo 2 §5.1 **solo para proyección**. Si no puede determinarse la categoría, valor 0 explícito; sin invención de prioridades reales.
5. `AchPendingCycleEligibilityService`: la evidencia simulada creada ahora no se confunde con procesamiento operativo, sin desproteger evidencias legadas.

**Sin cambios:** código de ACH Colombia, migraciones, WSDL, catálogos STA, respuestas oficiales del gateway y configuración CUD. Se preserva revisión manual externa Tesorería.

## Pruebas y seguridad

Se actualizaron pruebas xUnit existentes y añadieron casos C4/C5, excepción de DB, duplicados, prioridad y aislamiento de cámara; compilación y ejecución imposibles aquí sin SDK .NET. Comprobaciones automáticas estáticas 12/12 PASS (`F01_EVIDENCIA_CONTROLES_ESTATICOS.txt`). Integridad SHA-256/CRC del proyecto documentada en `MANIFIESTO_F01.md`.

## Restricciones y pendientes

- HC-003/005/006: IMPLEMENTADO — PENDIENTE DE VERIFICACIÓN. **0 cierres definitivos** por ausencia de pruebas ejecutadas xUnit y de ruta DB/Quartz externa.
- F03 debe implementar la incorporación atómica de entradas `Queued` al ciclo destino antes de consumirlas; la nueva política conservadora evita la pérdida prematura en F01.
- F02 debe procesar NACK/ADV reales sin confundirse con aceptación técnica o ejecución temporal del ciclo.
- Aclarar con operador si hace falta SEC/dirección de devolución original para casos no determinables; no afectar decisión productiva por esta proyección.
- Confirmar preexistencia y aplicación de respuesta oficial en integración antes de habilitar pagos reales.

**Porcentaje:** 1 de 8 incrementos entregados (12,5 % de hitos; NO cumplimiento normativo). **Próxima fase:** F02 — respuesta oficial NACK individual y ADV. **Reversión:** aplicar `F01_CAMBIOS.patch` en reversa sobre árbol limpio, eliminar archivos nuevos `docs/correcciones-cenit/` y `scripts/test/cenit_f01_static_check.py`, o restaurar el ZIP I10 original (ver manifest).
