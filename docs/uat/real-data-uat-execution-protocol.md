# Protocolo UAT con datos reales — S1

## 1. Propósito
Definir cómo ejecutar UAT con datos reales o reales anonimizados, evidencias y actas humanas para decidir GO UAT formal y mantener o levantar NO-GO productivo según scorecard.

Aclaraciones obligatorias:
- Este protocolo no habilita producción por sí solo.
- Este protocolo no reemplaza aprobación humana.
- Este protocolo no debe contener datos sensibles reales en el repositorio.
- El resultado de la ejecución puede ser: aprobado, aprobado con observaciones o rechazado.

## 2. Alcance
Cobertura mínima obligatoria:
- S1-10 Neteo CENIT E2E.
- S1-11 Liquidez/CUD.
- S1-12 Naming externo ACH/CENIT/STA.
- S1-13 Sobre digital/firma/cifrado.
- S1-20 UAT/runbooks/evidencia firmada.
- Flujos ACH/CENIT relevantes: NACHA, devoluciones, rechazos, ROR, reportes, conciliación.

## 3. Fuera de alcance
- No crear API CUD.
- No crear API contable.
- No contabilizar.
- No declarar liquidación firme sin evidencia.
- No subir información sensible.
- No simular firmas humanas.
- No declarar GO productivo sin comité/scorecard.

## 4. Reglas de protección de datos
Reglas mandatorias para toda ejecución UAT real:
- Usar datos anonimizados o enmascarados cuando el dato sea sensible.
- No subir al repositorio PII, cuentas, saldos reales, llaves privadas, PFX, passwords, certificados privados ni soportes sensibles.
- Usar hash, identificadores trazables y referencias internas en lugar de datos crudos.
- Guardar evidencias externas en repositorio documental seguro o gestor aprobado; no necesariamente en Git.
- Aplicar principio de mínimo privilegio en acceso a evidencias.

## 5. Matriz de casos UAT reales

| ID | Dominio | Cámara | Caso | Datos requeridos | Resultado esperado | Evidencia | Aprobador | Estado |
|---|---|---|---|---|---|---|---|---|
| UAT-REAL-S1-10-001 | S1-10 | CENIT | Neteo CENIT por ciclo | Archivo/ciclo/participantes/posiciones/totales controlados | Neteo por ciclo consistente y trazable | Reporte de neteo + trazabilidad archivo→ciclo→participante→posición | Operaciones + Tesorería | Pendiente |
| UAT-REAL-S1-10-002 | S1-10 | CENIT | Reproceso idempotente de neteo | Mismo lote/ciclo bajo reproceso controlado | No duplicidad de impacto; resultado idempotente | Evidencia de reproceso + comparación de totales | Operaciones + Tecnología | Pendiente |
| UAT-REAL-S1-11-001 | S1-11 | CENIT/CUD | Evidencia CUD registrada | Soporte operacional CUD real o oficial controlado | Evidencia registrada, hash y revisión completa | Soporte CUD + hash + revisión | Tesorería + Riesgo | Pendiente |
| UAT-REAL-S1-11-002 | S1-11 | CENIT/CUD | Liquidez simulada vs evidencia CUD real | Resultado de liquidez + soporte CUD | Consistencia o discrepancia formalmente documentada | Comparativo liquidez vs CUD + acta | Tesorería + Operaciones | Pendiente |
| UAT-REAL-S1-12-001 | S1-12 | ACH Colombia | Naming ACH Colombia | Reglas de naming vigentes + archivo controlado | Nombre generado coincide con esperado normativo | Comparativo esperado vs generado + fuente normativa | Operaciones ACH + Compliance | Pendiente |
| UAT-REAL-S1-12-002 | S1-12 | CENIT | Naming CENIT | Reglas de naming vigentes + archivo controlado | Nombre generado coincide con esperado normativo | Comparativo esperado vs generado + fuente normativa | Operaciones CENIT + Compliance | Pendiente |
| UAT-REAL-S1-13-001 | S1-13 | ACH/CENIT | Sobre firmado/cifrado saliente validado | Archivo saliente firmado/cifrado en ambiente controlado | Validación exitosa por receptor/homologación | Evidencia de firma/cifrado + validación externa | Seguridad + Operaciones | Pendiente |
| UAT-REAL-S1-13-002 | S1-13 | ACH/CENIT | Sobre externo recibido validado | Sobre externo de prueba controlada | Validación fail-close y trazabilidad de certificados | Evidencia de validación + incidentes (si aplica) | Seguridad + Tecnología | Pendiente |
| UAT-REAL-S1-20-001 | S1-20 | Ambas | Runbook ejecutado | Checklists y runbooks vigentes | Ejecución documentada sin brechas críticas abiertas | Checklist ejecutado + bitácora | Operaciones + QA UAT | Pendiente |
| UAT-REAL-S1-20-002 | S1-20 | Ambas | Acta UAT firmada | Resultado consolidado de casos y defectos | Acta formal con decisión y aprobadores | Acta firmada + índice de evidencias | Comité UAT + Dueños de proceso | Pendiente |

## 6. Evidencia mínima por bloqueante

| Bloqueante | Evidencia mínima | Evidencia no aceptada | Aprobadores mínimos | Criterio de cierre |
|---|---|---|---|---|
| S1-10 | reporte de neteo, ciclo, posiciones, totales, trazabilidad archivo→ciclo→participante→posición | evidencia incompleta sin trazabilidad o sin ciclo | Operaciones + Tesorería | consistencia validada o desviación formalmente aceptada |
| S1-11 | soporte CUD o evidencia operacional, hash, revisión, doble aprobación, conciliación | capturas sin fuente, sin hash o sin doble aprobación | Tesorería + Riesgo (o equivalente) | conciliación cerrada o desviación formalmente aceptada |
| S1-12 | nombre esperado vs generado, fuente normativa, archivo real/controlado, aprobación por cámara | naming sin fuente normativa o sin comparación objetiva | Operaciones por cámara + Compliance | naming conforme por cámara o plan de corrección aprobado |
| S1-13 | archivo firmado/cifrado saliente, validación externa/homologada, archivo externo recibido, validación fail-close, evidencia de certificados | validaciones internas sin evidencia externa y sin control de certificados | Seguridad + Operaciones + Tecnología | controles criptográficos aprobados o remediación formal bloqueante |
| S1-20 | checklists ejecutados, runbooks aprobados, actas firmadas, defectos cerrados o aceptados | checklists sin firma, actas incompletas o defectos sin responsable | Comité UAT + dueños de proceso | compuerta humana cerrada con trazabilidad completa |

## 7. Defectos y decisión
- P0 bloquea GO UAT formal y GO productivo.
- P1 requiere workaround aprobado formalmente.
- P2/P3 pueden quedar pendientes aceptados con plan y fecha.
- Todo defecto debe tener responsable, fecha objetivo y evidencia.

## 8. Acta UAT humana
Plantilla oficial:
- `docs/uat/templates/real-data-uat-acta-template.md`

El acta debe incluir como mínimo:
- dominio S1;
- cámara;
- caso ejecutado;
- evidencia;
- resultado;
- defectos;
- decisión;
- aprobadores;
- fecha;
- firma o trazabilidad de firma.

## 9. Índice de evidencias
Plantilla oficial:
- `docs/uat/templates/real-data-uat-evidence-index-template.md`

Debe incluir como mínimo:
- ID evidencia;
- dominio;
- cámara;
- archivo/hash/referencia;
- ubicación segura;
- sensibilidad;
- enmascaramiento;
- responsable;
- estado.

## 10. Compuerta final de scorecard
La ejecución UAT real debe actualizar, cuando aplique:
- matriz consolidada S1;
- scorecard GO/NO-GO;
- checklists UAT;
- runbooks;
- política current-vs-historical (si se detecta contradicción).

## 11. Veredicto inicial
- Protocolo UAT real: definido.
- Ejecución UAT real: pendiente.
- Actas humanas: pendientes.
- GO UAT formal: pendiente.
- GO productivo: NO.
- NO-GO productivo vigente.

## 12. Referencias base obligatorias
- `docs/audits/s1-requirement-norm-code-test-evidence-closure-matrix-current.md`
- `docs/uat/human-signoff-evidence-classification-gates.md`
- `docs/governance/current-vs-historical-matrix-policy.md`
- `docs/audits/go-nogo-scorecard-funcional-normativo-2026-04-26.md`
- `docs/audits/s1-matriz-maestra-trazable-funcional-normativa-2026-04-26.md`
- `docs/uat/accounting-review-reconciliation-acceptance-checklist.md`
- `docs/uat/cenit-cycles-liquidity-cud-acceptance-checklist.md`
- `docs/uat/digital-envelope-certificate-acceptance-checklist.md`
- `docs/uat/naming-returns-ror-acceptance-checklist.md`
- `docs/uat/nacha-records-acceptance-checklist.md`
- `docs/ops/reconciliation-operations-runbook.md`
- `docs/ops/certificate-operations-runbook.md`

## 13. Paquete 12B para usuarios operativos no técnicos
- Guías operativas no técnicas: `docs/uat/operator-guides/uat-operator-execution-guide.md`.
- Set de casos operativos: `docs/uat/operator-guides/uat-operator-test-cases-s1-blockers.md`.
- Checklist de evidencias operativas: `docs/uat/operator-guides/uat-operator-evidence-checklist.md`.
- Plantilla operativa de defectos: `docs/uat/operator-guides/uat-operator-defect-report-template.md`.
- Guía de aprobación/firma: `docs/uat/operator-guides/uat-operator-signoff-guide.md`.
- Paquete PDF/Excel: `docs/uat/operator-guides/uat-final-user-delivery-pack.md`.
- Este paquete no habilita producción por sí solo y mantiene NO-GO productivo vigente.

## 14. Referencia de brechas SPA para compuerta 12D
- Matriz de brechas SPA↔backend↔normativa↔UAT: `docs/audits/spa-angular-backend-uat-alignment-gap-matrix-current.md`.
- Regla: si SPA no cubre un caso UAT, debe ejecutarse por vía documental/manual controlada y quedar trazado en evidencia/acta.
- Para ejecución asistida por SPA (rutas/pantallas operativas, exportación Accounting Review y fronteras CENIT/CUD), usar: `docs/uat/operator-guides/uat-operator-execution-guide.md` (sección **10. Ejecución UAT con apoyo del SPA**).
