# F08 — Plan de ejecución, aceptaciones y bloqueos

**Origen de los 22 escenarios:** `../auditoria-estatica-cenit/MATRIZ_PRUEBAS_POSTERIORES_CENIT.md`, sin modificación de sus condiciones esperadas. Este documento solo agrega responsable propuesto, evidencia que debe recolectarse y agrupación de trabajo; **los 22 PT continúan PENDIENTES / NO EJECUTADOS**.

## Orden de trabajo y puertas de liberación

1. **G0 entorno:** aprovisionar SDK .NET requerido por `global.json` (**10.0.403**, `latestPatch`), restaurar dependencias, configurar SQL Server/PostgreSQL/SQLite según las suites y reloj/calendario controlado. La memoria F07 menciona 10.0.402; prevalece el valor efectivamente contenido en `global.json` del ZIP F07.
2. **G1 ejecución técnica y no regresión:** `bash scripts/test/run_cenit_f07_dotnet_regression.sh`; archivar archivos TRX, log de compilación, versión de SDK, origen de datos y hash de entradas. Ejecutar PT-01/04/05/06/11/14/15/20/21 y confirmar aislamiento ACH Colombia con responsable.
3. **G2 operación con archivos oficiales:** completar PT-02/03/07/08/09/10/12/13/16/17/18 mediante muestras oficiales o ambiente homologador autorizado; conservar NACHA/raw, hashes, correlaciones, respuestas, fecha valor y vigencia del calendario. No simular aprobación del operador.
4. **G3 conciliación y vigencia:** PT-19 conciliación manual firmada por Tesorería, y PT-22 confirmación normativa/operativa con operador. No integrar CUD como atajo.
5. **Dictamen de cierre HC:** auditoría independiente valida resultado positivo y casos negativos, determinación del alcance de las pruebas y acta de aceptación; se actualiza matriz por HC solo después de evidencias trazables.

**Política de bloqueo:** cualquier PT no ejecutada relevante, incidencia no resuelta, falta de firma del responsable o regresión ACH Colombia obliga a **NO APTO PARA HOMOLOGACIÓN / PRODUCCIÓN**. La entrega del código es apta para someterse a QA, no acredita implementación bancaria en producción.

## Registro completo PT-01 a PT-22

| Prueba I10 | Puerta | Responsable propuesto | Evidencia documental exigible | Estado |
|---|---|---|---|---|
| PT-01 | G1 | Desarrollo/QA + DBA | TRX, registro de cola y lote antes/después, logs de Quartz | **PENDIENTE — NO EJECUTADA** |
| PT-02 | G2 | Integración CENIT + QA | archivo C4/C5, respuestas de aceptación/rechazo y trazas | **PENDIENTE — NO EJECUTADA** |
| PT-03 | G2 | Integración CENIT + DBA | ROR bruto 106 posiciones, adenda, evento padre y SQL | **PENDIENTE — NO EJECUTADA** |
| PT-04 | G1 | Desarrollo/QA + DBA | TRX, historial transacción y estado previo/posterior | **PENDIENTE — NO EJECUTADA** |
| PT-05 | G1 | Desarrollo/QA + DBA | respuesta oficial de operador; snapshots simulador | **PENDIENTE — NO EJECUTADA** |
| PT-06 | G1 | Desarrollo/QA | TRX política de prioridades SEC y devoluciones | **PENDIENTE — NO EJECUTADA** |
| PT-07 | G2 | QA + integración CENIT | archivo R23 mismo día, ciclo, aceptación/rechazo operador | **PENDIENTE — NO EJECUTADA** |
| PT-08 | G2 | QA + operación | datos notificación real, fecha valor y calendario de festivos | **PENDIENTE — NO EJECUTADA** |
| PT-09 | G2 | QA + integración CENIT | R06 C5 desde C1-C4, acuses | **PENDIENTE — NO EJECUTADA** |
| PT-10 | G2 | QA + integración CENIT | R74/R32 raw y casos negativos certificados | **PENDIENTE — NO EJECUTADA** |
| PT-11 | G1 | QA + integración CENIT + ACH Colombia | TRX y parser real para archivos en ambas cámaras | **PENDIENTE — NO EJECUTADA** |
| PT-12 | G2 | QA + integración STA | archivos reales de cada servicio, tabla códigos y respuesta | **PENDIENTE — NO EJECUTADA** |
| PT-13 | G2 | Integración CENIT + Tesorería | ADV 280 del operador, cruce por ciclo, fecha y monto | **PENDIENTE — NO EJECUTADA** |
| PT-14 | G1 | QA + DBA | archivo mixto, estado individual y evento auditado por traza | **PENDIENTE — NO EJECUTADA** |
| PT-15 | G1 | QA + DBA | dos workers, bloqueo/concurrencia, una sola actualización | **PENDIENTE — NO EJECUTADA** |
| PT-16 | G2 | Operador CENIT + QA | acta operador C1 y calendario sesión 1 | **PENDIENTE — NO EJECUTADA** |
| PT-17 | G2 | Operador CENIT + QA | acta operador C2/C3, fechas y tipos admitidos | **PENDIENTE — NO EJECUTADA** |
| PT-18 | G2 | Operador CENIT + QA | acta operador C4/C5, ventanas y contingencia | **PENDIENTE — NO EJECUTADA** |
| PT-19 | G3 | Tesorería + Operaciones | conciliación firmada y diferencia con CUD manual | **PENDIENTE — NO EJECUTADA** |
| PT-20 | G1 | QA ACH Colombia | TRX suite completa y comparación de archivos golden | **PENDIENTE — NO EJECUTADA** |
| PT-21 | G1 | Desarrollo/QA + DBA | fallo antes del commit, idempotencia y logs/reprocesos | **PENDIENTE — NO EJECUTADA** |
| PT-22 | G3 | Cumplimiento + Operador CENIT | confirmación de vigencia documental y muestras oficiales | **PENDIENTE — NO EJECUTADA** |

## Ejecución reproducible en entorno adecuado

```bash
cat global.json
dotnet --info
python scripts/test/cenit_f08_closure_audit.py
bash scripts/test/run_cenit_f07_dotnet_regression.sh
```

Para DB, Quartz, gateway y Tesorería se requieren procedimientos adicionales de ambiente y las matrices del audit I10. Guardar evidencia en sistema controlado, con fecha, firmante, entorno, identificadores de transacciones, versión y hash. No editar resultados históricos I01–I10 para convertir pendientes en aprobados.

**Prohibición de confundir evidencias:** script fuente PASS ≠ compilación; caso xUnit escrito ≠ ejecutado; ACK técnico ≠ liquidación; ADV ≠ asiento CUD; ZIP íntegro ≠ homologación.
