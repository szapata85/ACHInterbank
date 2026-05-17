# Runbook operativo — Reportería, conciliación operativa y revisión contable de terceros

## 1. Propósito
Definir cómo operar generación de reportes, revisión de trazabilidad, conciliación operativa, revisión contra contabilidad de terceros, identificación de diferencias, evidencia CUD, neteo/liquidez, huérfanas/no resueltas, manual audit-only, ROR, rechazo total/parcial, exportaciones, cierre operativo y escalamiento.

Este runbook no habilita producción.

## 2. Decisión arquitectónica no contable
**ACHInterbank NO:** contabiliza; genera asientos definitivos; genera ledger/journal/posting; integra APIs contables en línea; reemplaza core contable; reemplaza software contable de terceros; registra movimientos contables oficiales.

**ACHInterbank SÍ:** reporta; consolida información; genera evidencia; facilita conciliación operativa; permite revisión contra terceros; identifica diferencias; traza archivo/ciclo/cámara/transacción/causal/estado/evidencia; soporta auditoría.

## 3. Estado actual
- Base técnica de reportes: **sí**.
- Base técnica de trazabilidad: **sí**.
- Base conciliatoria: **parcial**.
- Revisión contable terceros: **parcial**.
- GO técnico: **sí, acotado/parcial**.
- GO UAT controlado: **sí, parcial**.
- NO-GO productivo: **sí**.
- Este runbook no habilita producción.

## 4. Fuentes
- `docs/audits/accounting-review-reconciliation-matrix-current.md`
- `docs/uat/accounting-review-reconciliation-acceptance-checklist.md`
- `tests/Cfa.ACHInterbank.Tests/AccountingReviewBoundaryCharacterizationTests.cs`
- `ReportsController`
- `AchTraceabilityController`
- `AchTransactionReportService`
- `AchReturnRejectionReportService`
- `AchNachaCycleReportService`
- `AchAuditHistoryReportService`
- `AchTraceabilityService`
- `docs/audits/go-nogo-scorecard-funcional-normativo-2026-04-26.md`
- `docs/audits/s1-matriz-maestra-trazable-funcional-normativa-2026-04-26.md`

## 5. Roles y responsabilidades
| Rol | Responsabilidad | Evidencia esperada | Frecuencia |
|---|---|---|---|
| Operaciones ACH | Ejecutar conciliación diaria/ciclo | bitácora operativa, reportes | diaria/ciclo |
| Tesorería | Revisar neteo/liquidez/CUD operacional | soporte tesorería | por ciclo/cierre |
| Contabilidad de terceros / área contable externa | Revisar contra reportes/evidencia | acta revisión tercero | diaria/cierre |
| Tecnología | Soporte técnico reportería/trazabilidad | ticket, logs, evidencias | incidente/diaria |
| Riesgo operativo | Evaluar impactos y excepciones | acta riesgo | por incidente/cierre |
| Compliance | Verificar cumplimiento y evidencia | visto bueno compliance | cierre/committee |
| Auditoría | Muestreo y trazabilidad | informe auditoría | periódica |
| Negocio | Aprobar criterios operativos | acta negocio | cierre/UAT |
| Aprobador operativo | Autorizar cierre operativo | aprobación firmada | diaria/ciclo |
| Responsable de conciliación | Consolidar diferencias y seguimiento | matriz diferencias | diaria/ciclo |
| Responsable de incidentes | Coordinar escalamiento y resolución | bitácora incidente | por incidente |

Aclaración: Contabilidad de terceros revisa con base en reportes/evidencia; ACHInterbank no contabiliza.

## 6. Insumos operativos
| Insumo | Fuente | Uso | Obligatorio | Observación |
|---|---|---|---|---|
| reportes transacciones enviadas | Reports | control salidas | Sí | PDF disponible |
| reportes transacciones recibidas | Reports | control entradas | Sí | PDF disponible |
| reportes devoluciones | Reports | conciliación causales | Sí | operacional |
| reportes rechazos | Reports | conciliación rechazo | Sí | total/parcial |
| archivos NACHA | Reports/Export | correlación archivo | Sí | evento operativo |
| reportes ciclos | Reports | control por ciclo | Sí | cámara/ciclo |
| reporte conciliación agregada | Reports | diferencias agregadas | Sí | parcial |
| auditoría/histórico | Reports | trazabilidad auditoría | Sí | operacional |
| trazabilidad transacción | Traceability | análisis puntual | Sí | soporte incidente |
| trazabilidad rango | Traceability | análisis masivo | Sí | soporte cierre |
| evidencia huérfanas/no resueltas | incoming/orphan | excepciones | Sí | manual review |
| evidencia manual audit-only | command center | evidencia de acción manual | Sí | no aplicado contable |
| evidencia ROR | ROR flujo | correlación retorno | Sí | reportable |
| evidencia neteo/liquidez | CENIT operativo | control ciclo | Sí | parcial E2E |
| evidencia CUD operacional | soporte externo/manual | respaldo operativo | Parcial | sin API CUD |
| reportes de terceros | sistema tercero | comparación externa | Parcial | depende tercero |
| extractos/reportes externos | tercero/tesorería | contraste externo | Parcial | si existe |
| actas UAT/cierre | UAT/ops | evidencia de aprobación | Sí | cierre operativo |

## 7. Frecuencia operativa
| Actividad | Frecuencia | Responsable | Evidencia | Criterio de cierre |
|---|---|---|---|---|
| generación de reportes base | diaria | Operaciones ACH | PDF/reportes | reportes disponibles |
| revisión por ciclo/cámara | por ciclo/cámara | Responsable conciliación | bitácora ciclo | diferencias registradas |
| control por archivo | por archivo | Operaciones ACH | evidencia archivo/hash | archivo reconciliado/escalado |
| cierre de jornada | cierre diario | Aprobador operativo | acta cierre | pendientes clasificados |
| revisión por reproceso | por reproceso | Tecnología + Operaciones | ticket + evidencia | reproceso validado |
| revisión por incidente | por incidente | Responsable incidentes | bitácora incidente | incidente cerrado/escalado |
| revisión por auditoría | por solicitud auditoría | Auditoría + Operaciones | paquete evidencia | hallazgos documentados |

## 8. Procedimiento diario de conciliación operativa
1. Confirmar archivos/procesos del día.
2. Generar reportes operativos.
3. Revisar transacciones enviadas/recibidas.
4. Revisar devoluciones/rechazos.
5. Revisar NACHA/ciclos.
6. Revisar trazabilidad por muestras o excepciones.
7. Revisar reporte conciliación agregada.
8. Comparar contra reportes de terceros.
9. Identificar diferencias.
10. Clasificar diferencias.
11. Adjuntar evidencia.
12. Escalar pendientes.
13. Registrar cierre operativo.
14. Preparar acta si aplica.

Aclaración: ningún paso genera asiento contable.

## 9. Procedimiento por ciclo/cámara
| Control | Resultado esperado | Evidencia | Acción si falla |
|---|---|---|---|
| seleccionar cámara | cámara correcta | captura filtro | corregir filtro |
| seleccionar ciclo | ciclo correcto | captura filtro | corregir ciclo |
| validar totales/conteos/valores | consistencia operativa | reporte ciclo | abrir diferencia |
| validar estados/causales | semántica correcta | reporte + trazas | escalar a negocio/compliance |
| validar rechazos/devoluciones | separación correcta | reporte rechazo/dev | investigar causas |
| validar neteo/liquidez si aplica | consistencia parcial | evidencia CENIT | escalar tesorería |
| validar evidencia CUD si aplica | soporte operacional trazable | soporte CUD | marcar pendiente y escalar |
| registrar diferencias | diferencias clasificadas | matriz diferencias | escalar nivel 2+ |
| cerrar o escalar | cierre documentado | acta/bitácora | mantener abierto |

## 10. Procedimiento por archivo NACHA
Validar archivo recibido/generado, hash (si aplica), cámara, ciclo, fecha operacional, conteos, valores, estado archivo, rechazos, devoluciones, trazabilidad por transacción, evidencia de procesamiento y exportación/reporte asociado.

Aclaración: archivo aceptado/rechazado por cámara es evento operativo, no asiento.

## 11. Procedimiento de revisión contra terceros
Definir tercero, reporte externo, campos comparables, documentación de diferencias, adjunto de evidencia y aprobación de cierre.

Campos mínimos: fecha, cámara, ciclo, referencia/transacción, valor, estado, causal, entidad origen/destino si aplica, archivo, observación, evidencia.

## 12. Clasificación de diferencias
| Tipo de diferencia | Descripción | Ejemplo | Responsable | Acción |
|---|---|---|---|---|
| por valor | monto no coincide | valor ACH vs tercero | Responsable conciliación | validar origen y escalar |
| por cantidad | conteos no coinciden | #tx por ciclo | Operaciones | recontar y escalar |
| por estado | estado diferente | Pending vs Returned | Operaciones/Negocio | revisar trazabilidad |
| por causal | causal distinta | R01 vs DEV14 | Compliance/Operaciones | validar catálogos |
| por ciclo | ciclo no corresponde | C1 vs C2 | Operaciones | corregir correlación |
| por cámara | cámara inconsistente | CENIT vs ACH | Operaciones | validar filtro |
| por fecha | fecha operacional distinta | T vs T-1 | Operaciones | ajustar ventana |
| por archivo | archivo no correlaciona | nombre/hash distinto | Tecnología/Ops | investigar ingestión |
| por ROR | inconsistencia retorno de retorno | falta correlación | Operaciones | escalar flujo ROR |
| por huérfana/no resuelta | no conciliada | NotFound persistente | Operaciones | revisión manual |
| por manual audit-only | mal clasificada | audit-only marcado aplicado | Operaciones | corregir clasificación |
| por CUD | evidencia incompleta | sin soporte CUD | Tesorería | marcar pendiente |
| por neteo/liquidez | inconsistencia operativa | neto vs liquidez | Tesorería/Tecnología | escalar ciclo |
| técnica | error sistema/reporte | timeout/falla consulta | Tecnología | incidente técnico |
| de tercero | dato externo inconsistente | tercero incompleto | Tercero + Operaciones | solicitar aclaración |

## 13. Huérfanas/no resueltas/manual audit-only
Procedimiento: revisar NotFound, Ambiguous, `candidateTransactionIds`, `manualReviewRequired`, evidencia JSON, actor/comentario/fecha, confirmar no aplicado, confirmar no asiento, confirmar no duplicidad de evidencia, escalar revisión manual si aplica.

Aclaración: manual audit-only no cambia estado contable y no significa aplicación.

## 14. ROR
Procedimiento: identificar ROR generado, correlacionar con transacción original/causal/archivo/cámara/ciclo, revisar duplicidad, revisar evidencia, revisar brechas de ROR recibido/aplicado si no existe, marcar diferencias y escalar.

Aclaración: ROR es reportable/trazable, no posting contable.

## 15. Rechazo total/parcial
Procedimiento: identificar rechazo total, validar no aplicación, identificar rechazo parcial, separar aceptados/rechazados, validar causales/cantidades/valores, confirmar que `RejectedPartial` no es devolución parcial por monto, generar evidencia y escalar diferencias.

## 16. Neteo, liquidez y CUD
Procedimiento: revisar neteo CENIT, posición neta, liquidez Processed/Deferred/Rejected, DXX-LIQ como interno, adjuntar evidencia operacional, revisar evidencia CUD manual/archivo/reporte si existe, comparar CUD vs neteo/liquidez si aplica, marcar pendiente si no hay cierre E2E, escalar diferencia.

Aclaraciones: no API CUD; no contabilización CUD; CUD es evidencia operacional; cierre E2E neteo/liquidez/CUD sigue NO-GO si no está validado.

## 17. Exportaciones y evidencias
Definir: PDF disponible, filtros aplicados, totales, usuario/actor, fecha/hora, cámara/ciclo, hash/correlationId si aplica, almacenamiento de evidencia, nombre sugerido de archivo y control de repetición.

Aclaración: exportar no cambia estado contable ni genera asiento.

## 18. Idempotencia operativa
Definir controles para reporte por rango/archivo, conciliación por ciclo/cámara, duplicate file, reprocess, incoming duplicado, ROR duplicado, manual audit-only, evidencia CUD duplicada, exportación repetida.

Aclaración: la idempotencia ACH existente no equivale automáticamente a idempotencia de reportería/conciliación.

## 19. Cierre operativo de conciliación
Checklist de cierre: reportes generados, diferencias identificadas/clasificadas, evidencias adjuntas, pendientes escalados, tercero revisado si aplica, responsable validó, aprobador aprobó, acta generada si aplica, scorecard actualizado si aplica.

Aclaración: el cierre operativo no es cierre contable oficial.

## 20. Manejo de incidentes
| Incidente | Acción inmediata | Responsable | Evidencia | Criterio de cierre |
|---|---|---|---|---|
| reporte no genera | regenerar/validar filtros | Operaciones/Tecnología | log + captura | reporte emitido |
| PDF incorrecto | regenerar y verificar campos | Operaciones | PDF corregido | PDF validado |
| diferencia valor/cantidad/estado/causal | clasificar y escalar | Responsable conciliación | matriz diferencias | decisión documentada |
| archivo no encontrado | validar correlación archivo | Tecnología/Ops | bitácora archivo | archivo ubicado/escalado |
| transacción no trazable | consultar traza/rango | Operaciones | evidencia traza | traza encontrada/escalada |
| huérfana sin evidencia | completar evidencia | Operaciones | JSON/evidencia | evidencia adjunta |
| manual audit-only duplicado | revisar idempotencia | Operaciones/Tecnología | registro incidente | duplicidad controlada |
| ROR duplicado | validar deduplicación | Operaciones | evidencia ROR | corregido/escalado |
| evidencia CUD faltante | solicitar soporte tesorería | Tesorería | soporte CUD | evidencia recibida |
| neteo/liquidez inconsistente | revisar ciclo y política | Tesorería/Tecnología | evidencia ciclo | consistencia/pendiente formal |
| tercero reporta valor diferente | comparar y documentar | Operaciones + Tercero | acta comparación | diferencia resuelta/escalada |
| error técnico de consulta | abrir incidente | Tecnología | ticket | incidente cerrado |
| posible dato sensible expuesto | contención y notificación | Tecnología/Compliance | acta incidente | mitigación completada |

## 21. Escalamiento
| Condición | Nivel | Responsable | Tiempo objetivo | Evidencia |
|---|---|---|---|---|
| diferencia crítica | Nivel 1→2 | Operaciones/Tecnología | < 2h | ticket + acta |
| CUD no conciliado | Nivel 2→3 | Tesorería/Riesgo | < 4h | soporte CUD |
| archivo/ciclo incompleto | Nivel 1→2 | Operaciones/Tecnología | < 2h | bitácora ciclo |
| huérfanas recurrentes | Nivel 2 | Operaciones/Tecnología | < 1 día | reporte excepciones |
| ROR inconsistente | Nivel 2→3 | Operaciones/Compliance | < 1 día | evidencia ROR |
| datos sensibles | Nivel 3→4 | Compliance/Tecnología | inmediato | acta seguridad |
| error de reportería | Nivel 2 | Tecnología | < 4h | ticket |
| bloqueo de cierre | Nivel 4 | Comité UAT/Go-NoGo | mismo día | acta comité |

Niveles: Nivel 1 Operaciones; Nivel 2 Tecnología; Nivel 3 Riesgo/Compliance; Nivel 4 Comité/UAT/Go-NoGo.

## 22. Controles de seguridad y auditoría
- mínimo privilegio;
- acceso controlado a reportes;
- no exponer datos sensibles innecesarios;
- trazabilidad de usuario;
- auditoría de generación/exportación;
- control de evidencias;
- segregación de responsabilidades;
- revisión periódica;
- no manipular datos para “cuadrar”;
- no crear asientos manuales desde ACHInterbank.

## 23. Evidencia mínima por cierre
- reporte transacciones enviadas;
- reporte transacciones recibidas;
- reporte devoluciones;
- reporte rechazos;
- reporte archivos NACHA;
- reporte ciclos;
- reporte conciliación agregada;
- reporte auditoría/histórico;
- trazabilidad transacción/rango;
- evidencia huérfanas;
- evidencia manual audit-only;
- evidencia ROR;
- evidencia rechazo total/parcial;
- evidencia neteo/liquidez;
- evidencia CUD si aplica;
- reporte diferencias;
- exportaciones PDF;
- acta/cierre;
- aprobaciones.

## 24. Criterios de salida NO-GO del punto 10
1. Boundary no-contable documentado.  
2. Tests no-contables pasan.  
3. Matriz reportes/conciliación aprobada.  
4. Checklist UAT creado.  
5. Runbook conciliación creado.  
6. Reporte conciliatorio mínimo definido.  
7. Diferencias contra terceros definidas.  
8. Huérfanas/no resueltas reportables.  
9. Manual audit-only reportable.  
10. ROR reportable.  
11. Rechazo total/parcial reportable.  
12. Neteo/liquidez reportable.  
13. Evidencia CUD reportable.  
14. Estados conciliatorios definidos.  
15. Idempotencia reportería definida.  
16. Idempotencia conciliación definida.  
17. Exportación requerida definida.  
18. Evidencia UAT adjunta.  
19. Aprobación negocio.  
20. Aprobación operaciones.  
21. Aprobación riesgo/compliance.  
22. Aprobación tecnología.  
23. Scorecard actualizado.

## 25. Riesgos residuales
- confusión entre reporte y contabilización;
- confusión evento ACH vs asiento;
- confusión manual audit-only vs aplicado;
- confusión RejectedPartial vs devolución parcial por monto;
- falta reporte formal de terceros;
- falta estados conciliatorios;
- falta idempotencia formal reportería/conciliación;
- falta Excel/CSV si negocio lo exige;
- CUD sin API;
- CUD sin cierre E2E runtime;
- neteo/liquidez sin cierre E2E productivo;
- dependencia de revisión manual;
- NO-GO productivo vigente.

## 26. Decisión vigente
- GO técnico actual: **sí, acotado/parcial**.
- GO UAT controlado: **sí, parcial**.
- NO-GO productivo: **sí**.
- El sistema no contabiliza.
- Este runbook no habilita producción.
- Próximo recomendado: `feat(reporting): add accounting-review report model`, solo si matriz/checklist/runbook son aprobados.
