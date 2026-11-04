# F08 — Matriz de decisión de cierre de los trece hallazgos

**Corte:** 2026-11-02. **Fuente primaria de IDs y prioridades:** `../auditoria-estatica-cenit/HALLAZGOS_CONSOLIDADOS_CENIT.md` (I10). **Fuente de implementación:** `REGISTRO_CORRECCIONES.md`, informes F01–F06 y regresión F07. La prioridad P1/P2 proviene de I10 y no equivale a una clasificación regulatoria.

**Semántica de estados:** `IMPLEMENTADO — SIN VERIFICACIÓN DINÁMICA` acredita un cambio revisado estáticamente, NO su eficacia. `CERRADO` exigiría xUnit ejecutado, pruebas de integración relevantes y evidencia de operador/ACH Colombia según cada caso. Ningún hallazgo cambia de identidad ni se cierra en F08.

| Hallazgo | Prioridad | Fase | Implementación registrada | Evidencia local de cambio | Prueba posterior | Estado de cierre |
|---|---|---|---|---|---|---|
| HC-CENIT-001 | P1 | F03 | Ciclo encolado incorporado antes de exportación | [F03_INFORME_CORRECCIONES.md](F03_INFORME_CORRECCIONES.md) | PT-01, PT-02, PT-20, PT-21 | **ABIERTO — implementado pendiente de verificación** |
| HC-CENIT-002 | P1 | F05 | ROR C1 correlacionada con devolución origen y evidencia NACHA | [F05_INFORME_CORRECCIONES.md](F05_INFORME_CORRECCIONES.md) | PT-03 | **ABIERTO — implementado pendiente de verificación** |
| HC-CENIT-003 | P1 | F01 | Neteo interno no acredita compensación oficial | [F01_INFORME_CORRECCIONES.md](F01_INFORME_CORRECCIONES.md) | PT-04, PT-05 | **ABIERTO — implementado pendiente de verificación** |
| HC-CENIT-004 | P1 | F04 | R23 mismo día limitado al ciclo siguiente | [F04_INFORME_CORRECCIONES.md](F04_INFORME_CORRECCIONES.md) | PT-07, PT-17 | **ABIERTO — implementado pendiente de verificación** |
| HC-CENIT-005 | P2 | F01 | Prioridad de simulación distingue SEC y tipos de retorno | [F01_INFORME_CORRECCIONES.md](F01_INFORME_CORRECCIONES.md) | PT-06 | **ABIERTO — implementado pendiente de verificación** |
| HC-CENIT-006 | P1 | F01 | Liquidez simulada no provoca rechazo definitivo | [F01_INFORME_CORRECCIONES.md](F01_INFORME_CORRECCIONES.md) | PT-04, PT-05 | **ABIERTO — implementado pendiente de verificación** |
| HC-CENIT-007 | P1 | F04 | R06 en C5 exige origen C4 del mismo día | [F04_INFORME_CORRECCIONES.md](F04_INFORME_CORRECCIONES.md) | PT-09 | **ABIERTO — implementado pendiente de verificación** |
| HC-CENIT-008 | P1 | F05 | ROR R74 condicionada a R32 crédito monetario con adenda | [F05_INFORME_CORRECCIONES.md](F05_INFORME_CORRECCIONES.md) | PT-10 | **ABIERTO — implementado pendiente de verificación** |
| HC-CENIT-009 | P1 | F04 | R23 posterior usa calendario y notificación verificable | [F04_INFORME_CORRECCIONES.md](F04_INFORME_CORRECCIONES.md) | PT-08 | **ABIERTO — implementado pendiente de verificación** |
| HC-CENIT-010 | P2 | F06 | Parser NACHA-M valida alfabeto CENIT por perfil | [F06_INFORME_CORRECCIONES.md](F06_INFORME_CORRECCIONES.md) | PT-11, PT-20 | **ABIERTO — implementado pendiente de verificación** |
| HC-CENIT-011 | P1 | F06 | Errores NACHA-M segregados de códigos STA D01-D06 | [F06_INFORME_CORRECCIONES.md](F06_INFORME_CORRECCIONES.md) | PT-12 | **ABIERTO — implementado pendiente de verificación** |
| HC-CENIT-012 | P1 | F02 | ADV 280/81–88 interpretado para conciliación | [F02_INFORME_CORRECCIONES.md](F02_INFORME_CORRECCIONES.md) | PT-13, PT-19 | **ABIERTO — implementado pendiente de verificación** |
| HC-CENIT-013 | P1 | F02 | NACK individual actualiza transacción, no archivo completo | [F02_INFORME_CORRECCIONES.md](F02_INFORME_CORRECCIONES.md) | PT-14, PT-15 | **ABIERTO — implementado pendiente de verificación** |

## Evidencia necesaria para cambiar a CERRADO

| Hallazgo | Evidencia positiva y negativa que falta ejecutar |
|---|---|
| HC-CENIT-001 | SQL/Quartz: una incorporación, transacción/lote coherentes, fechas valor, reintento y aislamiento ACH |
| HC-CENIT-002 | DB + archivo real ROR: evento padre, traza, importe, ciclo e idempotencia |
| HC-CENIT-003 | Quartz/SQL: sin IncludedInSettlement definitivo por simulación |
| HC-CENIT-004 | Archivo y calendario reales: aceptación C2-C5 solo cuando corresponde |
| HC-CENIT-005 | Pruebas xUnit: orden 1-10 o inexistencia de efectos operativos de simulación |
| HC-CENIT-006 | Quartz/SQL: sin ReturnedByOperator sin mensaje oficial |
| HC-CENIT-007 | Pruebas xUnit/archivo del operador para C1/C2/C3 versus C4 |
| HC-CENIT-008 | DB/raw oficial: bloquear padre distinto R32 y adenda sin campo 3 |
| HC-CENIT-009 | Calendario Bogotá y evidencias: 15 días, siguiente día hábil, festivos |
| HC-CENIT-010 | Parser real: Ñ/ñ CENIT rechazado y ACH Colombia conservado |
| HC-CENIT-011 | Archivos reales STA y NACHA, causa/código sin contaminación cruzada |
| HC-CENIT-012 | Gateway/DB + Tesorería: posiciones/avisos sin asiento de CUD |
| HC-CENIT-013 | SQL/transport: archivo mixto ACK/NACK, orden, idempotencia y concurrencia |

**Resultado del recuento:** 13/13 hallazgos con implementación documentada; **0/13 cerrados**, **0/13 sin implementación registrada**. El estado no es una declaración de conformidad normativa. Ver `F08_PLAN_PRUEBAS_Y_APROBACIONES.md` y el historial original de I10.
