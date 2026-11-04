# Hallazgos únicos consolidados — CENIT al cierre I10

**Trece (13) hallazgos únicos; 13 abiertos; 0 cerrados; 0 nuevos I10.** Identificadores inmutables. `P1` = riesgo operativo alto a abordar antes de activar el flujo afectado en producción; `P2` = prioridad inmediata posterior, o condicionada al uso real de simulación. **No equivalen a grado de sanción normativa**. Clasificación basada en efectos potenciales y rutas conocidas, no en incidentes ejecutados.

| ID | Prioridad | Ciclo/ámbito | Riesgo | Causa raíz resumida | Estado | Origen |
|---|---|---|---|---|---|---|
| HC-CENIT-001 | P1 | C1–C5 | Riesgo de transacción omitida y estado de cola falsamente finalizado | Cola fuera de ventana consumida sin incluir la transacción en el ciclo de destino | ABIERTO | I02 |
| HC-CENIT-002 | P1 | C1 (ROR de devolución día hábil anterior) | Rechazo indebido de ROR autorizado | ROR C1 sin correlación íntegra de devolución origen | ABIERTO | I02 |
| HC-CENIT-003 | P1 | C1–C5 | Datos de compensación divergentes de resultados efectivos | Detalle neto marcado como incluido antes del diferimiento/rechazo | ABIERTO | I02 |
| HC-CENIT-004 | P1 | C2–C5 | Devolución de crédito admitida bajo excepción improcedente | R23 de mismo día admitido como excepción de días anteriores | ABIERTO | I03 |
| HC-CENIT-005 | P2 | C1–C5 | Diferimiento/rechazo equivocado si se ejecuta el simulador | Orden de prioridad CENIT no distingue clase PPD/CCD/CTX y tipo de devolución | ABIERTO | I04 |
| HC-CENIT-006 | P1 | C4–C5 | Rechazo financiero sin respaldo de mensaje oficial | Rechazo definitivo por fondos desde liquidez simulada | ABIERTO | I05 |
| HC-CENIT-007 | P1 | C5 | Aceptación de devolución fuera del grupo permitido C5 | Excepción R06 sin restringir origen C4 en quinta sesión | ABIERTO | I06 |
| HC-CENIT-008 | P1 | Ciclos ROR aplicables | ROR improcedente aceptada | ROR R74 habilitada sin exigir devolución R32 de crédito monetario | ABIERTO | I07 |
| HC-CENIT-009 | P1 | Ciclos R23 aplicables | Devolución extemporánea | Plazo R23 evaluado con deadline aportada en vez de calcular día hábil | ABIERTO | I07 |
| HC-CENIT-010 | P2 | C1–C5 / NACHA-M | Aceptación de archivo físico no conforme al perfil CENIT | Parser físico admite Ñ/ñ fuera del repertorio CENIT 2026 | ABIERTO | I08 |
| HC-CENIT-011 | P1 | C1–C5 / servicios separados | Causal comunicada distinta del problema real | Códigos D01–D06 de STA reutilizados como errores NACHA-M | ABIERTO | I08 |
| HC-CENIT-012 | P1 | Salidas C1–C5 | Pérdida de evidencia contable de cámara | ADV 280/ADV sin interpretación específica en rutas oficiales auditadas | ABIERTO | I08 |
| HC-CENIT-013 | P1 | C1–C5 / respuestas | Transacción rechazada que conserva estado incompatible; archivo con rechazo global erróneo | NACK transaccional modifica estado del archivo, no de la operación | ABIERTO | I09 |


## Correcciones mínimas y evidencias principales por identificador

### HC-CENIT-001 — Cola fuera de ventana consumida sin incluir la transacción en el ciclo de destino

- **Fuente:** DSP-152 Anexo 2 2025, §3.3. **Rutas inspeccionadas:** `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitCycleQueueService.cs:21-47; CenitCycleExecutionService.cs:71-85; CenitNettingService.cs:50-53`.
- **Evidencia:** Enqueue conserva el ciclo de la transacción; StartExecution marca Consumed; Calculate filtra solo por AchCycleId del ciclo destino.
- **Corrección acotada:** Incorporar las transacciones en el universo del ciclo destino ANTES de marcar Consumed; transacción e idempotencia de reintento.
- **Evidencia para cierre:** prueba reproducible que active la ruta descrita, muestre el estado correcto y demuestre ausencia de regresión para ACH Colombia. Sin pruebas, el hallazgo permanece abierto.

### HC-CENIT-002 — ROR C1 sin correlación íntegra de devolución origen

- **Fuente:** DSP-152 Anexo 2 2025, §§3.2, 4.3 y 4.6. **Rutas inspeccionadas:** `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitReturnOfReturnService.cs:106-115; AchReturnOfReturnFileGenerationService.cs:444-462`.
- **Evidencia:** El flujo registra evento padre, pero la ruta de generación no demuestra SourceReturnTransactionId requerido por validador de ROR del día anterior.
- **Corrección acotada:** Derivar identificador de devolución origen del evento padre al construir archivo ROR; mantener correlación y unicidad.
- **Evidencia para cierre:** prueba reproducible que active la ruta descrita, muestre el estado correcto y demuestre ausencia de regresión para ACH Colombia. Sin pruebas, el hallazgo permanece abierto.

### HC-CENIT-003 — Detalle neto marcado como incluido antes del diferimiento/rechazo

- **Fuente:** DSP-152 Anexo 2 2025, §5.2; reglamento arts. 18–19. **Rutas inspeccionadas:** `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitNettingService.cs:50-105; LiquidityOptimizationService.cs:129-199`.
- **Evidencia:** IncludedInSettlement=true se almacena antes de que la optimización excluya operaciones; no se observa recomposición de detalle.
- **Corrección acotada:** No publicar ni usar neteo simulado como posición/liquidación oficial; mantener consistencia interna del detalle provisional.
- **Evidencia para cierre:** prueba reproducible que active la ruta descrita, muestre el estado correcto y demuestre ausencia de regresión para ACH Colombia. Sin pruebas, el hallazgo permanece abierto.

### HC-CENIT-004 — R23 de mismo día admitido como excepción de días anteriores

- **Fuente:** DSP-152 Anexo 2 2025, §3.2; Anexo A 2023, R23. **Rutas inspeccionadas:** `src/Cfa.ACHInterbank.Application/ACH/Services/CenitIncomingReturnPolicy.cs:137-160,241-266`.
- **Evidencia:** Se exige anterioridad para C1 pero elapsedDays==0 produce Allow en C2–C5; norma §3.2 refiere R23 a días anteriores.
- **Corrección acotada:** Aplicar condición temporal de R23 por ciclo; resolver interpretación controvertida solo con fuente normativa autorizada.
- **Evidencia para cierre:** prueba reproducible que active la ruta descrita, muestre el estado correcto y demuestre ausencia de regresión para ACH Colombia. Sin pruebas, el hallazgo permanece abierto.

### HC-CENIT-005 — Orden de prioridad CENIT no distingue clase PPD/CCD/CTX y tipo de devolución

- **Fuente:** DSP-152 Anexo 2 2025, §§5.1–5.2. **Rutas inspeccionadas:** `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/TransactionPriorityPolicy.cs:18-26; LiquidityOptimizationService.cs:111-117`.
- **Evidencia:** Priority se obtiene por TransactionTypeEnum y no por SEC/entrada/devolución, a pesar de prioridades 1–10.
- **Corrección acotada:** Si motor es exclusivamente informativo, no decidir estados reales; si quedase simulación, etiquetarla y ordenar con atributos correctos solo para análisis.
- **Evidencia para cierre:** prueba reproducible que active la ruta descrita, muestre el estado correcto y demuestre ausencia de regresión para ACH Colombia. Sin pruebas, el hallazgo permanece abierto.

### HC-CENIT-006 — Rechazo definitivo por fondos desde liquidez simulada

- **Fuente:** DSP-152 reglamento 2025 art.19; Anexo 2 §5.2(b). **Rutas inspeccionadas:** `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitNettingService.cs:63-80; LiquidityOptimizationService.cs:105-120,173-199; Quartz/Jobs/Implementation/AchCycleSchedulerHandler.cs:81-103`.
- **Evidencia:** AvailableLiquidity proviene de créditos internos y LiquiditySourceType=Simulated; el job cambia AchTransaction.State=ReturnedByOperator.
- **Corrección acotada:** Desactivar efecto definitivo de simulación en ruta operativa; aplicar únicamente rechazo oficial de CENIT; no construir API CUD.
- **Evidencia para cierre:** prueba reproducible que active la ruta descrita, muestre el estado correcto y demuestre ausencia de regresión para ACH Colombia. Sin pruebas, el hallazgo permanece abierto.

### HC-CENIT-007 — Excepción R06 sin restringir origen C4 en quinta sesión

- **Fuente:** DSP-152 Anexo 2 2025, §3.2; Anexo A R06. **Rutas inspeccionadas:** `src/Cfa.ACHInterbank.Application/ACH/Services/CenitIncomingReturnPolicy.cs:70-88,188-231; AchReturnEligibilityPolicyComponent.cs:95-120`.
- **Evidencia:** La política admite R06 para entrada de C1–C3 con evidencia operativa, eludiendo la condición de origen de C4.
- **Corrección acotada:** Hacer control de elegibilidad C5 por origen antes de la excepción R06; preservar evidencia específica R06.
- **Evidencia para cierre:** prueba reproducible que active la ruta descrita, muestre el estado correcto y demuestre ausencia de regresión para ACH Colombia. Sin pruebas, el hallazgo permanece abierto.

### HC-CENIT-008 — ROR R74 habilitada sin exigir devolución R32 de crédito monetario

- **Fuente:** Anexo A 2023, tabla 2, causal R74. **Rutas inspeccionadas:** `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/RegulatoryCatalogSeeder.cs:494-517; AchRegulatoryCatalogService.cs:119-154; CenitReturnOfReturnService.cs:198-223`.
- **Evidencia:** Catálogo R74 admite múltiples causas parentales sin exigir R32, crédito monetario y adenda campo 3.
- **Corrección acotada:** Acotar combinación padre R32/crédito monetario y comprobar adenda requerida en raw.
- **Evidencia para cierre:** prueba reproducible que active la ruta descrita, muestre el estado correcto y demuestre ausencia de regresión para ACH Colombia. Sin pruebas, el hallazgo permanece abierto.

### HC-CENIT-009 — Plazo R23 evaluado con deadline aportada en vez de calcular día hábil

- **Fuente:** DSP-152 Anexo 2 2025, §4.2; Anexo A R23. **Rutas inspeccionadas:** `src/Cfa.ACHInterbank.Application/ACH/Services/CenitIncomingReturnPolicy.cs:241-275; AchReturnsService.cs:79-107`.
- **Evidencia:** Comparación con ReceiverRejectionDeadlineDate sin cálculo independiente a partir de notificación verificada y calendario.
- **Corrección acotada:** Calcular día hábil límite desde notificación respaldada; almacenar fecha real de recepción separada de fecha valor.
- **Evidencia para cierre:** prueba reproducible que active la ruta descrita, muestre el estado correcto y demuestre ausencia de regresión para ACH Colombia. Sin pruebas, el hallazgo permanece abierto.

### HC-CENIT-010 — Parser físico admite Ñ/ñ fuera del repertorio CENIT 2026

- **Fuente:** Manual NACHA-M CENIT 07-may-2026, §4.1. **Rutas inspeccionadas:** `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaPhysicalParser.cs:84-92; NachaParserService.cs:96-102`.
- **Evidencia:** IsAcceptedByte permite 0xD1 y 0xF1 sin discriminar cámara; no figuran en el repertorio del manual CENIT aportado.
- **Corrección acotada:** Validar alfabeto permitido por perfil de cámara y conservar compatibilidad de ACH Colombia.
- **Evidencia para cierre:** prueba reproducible que active la ruta descrita, muestre el estado correcto y demuestre ausencia de regresión para ACH Colombia. Sin pruebas, el hallazgo permanece abierto.

### HC-CENIT-011 — Códigos D01–D06 de STA reutilizados como errores NACHA-M

- **Fuente:** Anexo B 2023 (STA); Manual NACHA-M CENIT 2026, §7.3.2 / Anexo 2. **Rutas inspeccionadas:** `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/RegulatoryCatalogSeeder.cs:425-436; NachaInboundValidation.cs:169,261,272,346,384`.
- **Evidencia:** D02/D04/D06 se usan para relleno/conteos/lotes, con significados incompatibles con rechazo STA.
- **Corrección acotada:** Separar espacios de códigos por servicio; mantener salidas error formal/fatal NACHA-M diferenciadas.
- **Evidencia para cierre:** prueba reproducible que active la ruta descrita, muestre el estado correcto y demuestre ausencia de regresión para ACH Colombia. Sin pruebas, el hallazgo permanece abierto.

### HC-CENIT-012 — ADV 280/ADV sin interpretación específica en rutas oficiales auditadas

- **Fuente:** Manual NACHA-M CENIT 07-may-2026, §7.3.3. **Rutas inspeccionadas:** `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaInboundValidation.cs:765-785; CenitChamberResponseService.cs:346-420`.
- **Evidencia:** Ingreso ordinario admite PPD/CCD/CTX y clase 200/220/225; respuesta del gateway solo XML/no-activity/conciliación genérica, no lector ADV 280.
- **Corrección acotada:** Interpretar y correlacionar ADV recibido para control/conciliación; no contabilizar ni operar CUD.
- **Evidencia para cierre:** prueba reproducible que active la ruta descrita, muestre el estado correcto y demuestre ausencia de regresión para ACH Colombia. Sin pruebas, el hallazgo permanece abierto.

### HC-CENIT-013 — NACK transaccional modifica estado del archivo, no de la operación

- **Fuente:** Manual NACHA-M CENIT 2026 §7.3.2; DSP-152 Anexo 2 2025 §3.3. **Rutas inspeccionadas:** `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitChamberResponseService.cs:188-207,229-285`.
- **Evidencia:** ImportItemAsync correlaciona AchTransactionId, pero para NACK aplica ChamberResponseState al export completo y no actualiza AchTransaction.State.
- **Corrección acotada:** Aplicar respuesta oficial por transacción con evento e idempotencia; separar estado global del archivo.
- **Evidencia para cierre:** prueba reproducible que active la ruta descrita, muestre el estado correcto y demuestre ausencia de regresión para ACH Colombia. Sin pruebas, el hallazgo permanece abierto.

## Agrupación sin duplicados

`001` cola; `002/004/007/008/009` reglas de retornos; `003/005/006` simulación y efectos de estado; `010/011` validaciones NACHA y dominios de códigos; `012/013` evidencia y estado de respuestas oficiales. No crear un hallazgo por cada ciclo afectado. No cerrar por mera presencia de clase/catalogo, sin ruta y prueba de corrección.
