# Política de gobierno documental — matrices vigentes vs históricas

## 1. Propósito

Esta política define cómo identificar, usar y actualizar documentos vigentes (`current`) e históricos para evitar contradicciones en:

- matrices S1;
- matrices normativas ACH/CENIT;
- scorecard GO/NO-GO;
- checklists UAT;
- runbooks;
- documentos dev/auditoría.

Aclaraciones:

- No cambia estados S1.
- No habilita producción.
- No reemplaza la matriz consolidada.
- No reemplaza scorecard.
- Gobierna cómo mantener consistencia documental.

## 2. Principios de gobierno documental

- Solo los documentos marcados como `current` o referenciados como vigentes pueden usarse para decisiones GO/NO-GO.
- Documentos históricos no deben usarse para justificar GO productivo.
- Si un documento histórico contradice uno vigente, prevalece el documento vigente.
- Si dos documentos vigentes se contradicen, prevalece el más específico hasta que se actualice la matriz consolidada/scorecard.
- El scorecard GO/NO-GO es la vista ejecutiva, pero no reemplaza la evidencia técnica/normativa.
- La matriz consolidada S1 es la vista de trazabilidad transversal.
- Las matrices específicas gobiernan detalle por dominio.
- Checklists UAT evidencian ejecución, no cambian por sí solos el estado productivo.
- Runbooks operativos guían operación, no habilitan producción sin scorecard.
- Cualquier cambio de estado debe actualizar referencias cruzadas.
- NO-GO productivo se mantiene si hay P0 abierto.

## 3. Taxonomía documental

| Tipo de documento | Propósito | Autoridad | Puede cambiar GO técnico | Puede cambiar GO UAT | Puede cambiar GO productivo | Ejemplo |
|---|---|---|---|---|---|---|
| Matriz consolidada S1 | Trazabilidad transversal requisito→norma→código→prueba→evidencia | Alta (trazabilidad) | Sí (soporte) | Sí (soporte) | No por sí sola | `docs/audits/s1-requirement-norm-code-test-evidence-closure-matrix-current.md` |
| Matriz normativa específica | Detalle por dominio/cámara | Alta (dominio) | Sí (dominio) | Sí (dominio) | No por sí sola | `docs/audits/cause-code-normative-matrix-ach-cenit-sta-current.md` |
| Scorecard GO/NO-GO | Vista ejecutiva de decisión | Máxima (decisión) | Sí | Sí | Sí, con evidencia/aprobaciones requeridas | `docs/audits/go-nogo-scorecard-funcional-normativo-2026-04-26.md` |
| Checklist UAT | Evidencia de ejecución UAT | Media-alta (aceptación) | Parcial | Sí (con acta humana) | No por sí solo | `docs/uat/nacha-records-acceptance-checklist.md` |
| Reporte UAT asistida por IA | Evidencia técnica/pre-UAT asistida | Media (soporte) | Sí (soporte) | Parcial | No | `docs/uat/accounting-review-ai-assisted-uat-execution-report.md` |
| Guía de compuertas de evidencia | Criterios de clasificación/aprobación | Alta (gobierno UAT) | Sí (gobierno) | Sí (gobierno) | No por sí sola | `docs/uat/human-signoff-evidence-classification-gates.md` |
| Runbook operativo | Procedimiento operacional | Media (operación) | Parcial | Parcial | No por sí solo | `docs/ops/reconciliation-operations-runbook.md` |
| Documento dev/auditoría | Contexto técnico/funcional | Media (contexto) | Parcial | Parcial | No por sí solo | `docs/dev/devoluciones-ach-auditoria-cenit-ach-colombia.md` |
| Documento histórico | Trazabilidad de decisiones pasadas | Baja (histórica) | No | No | No | `docs/audits/external-filename-normative-matrix-ach-cenit-sta-2026-04-20.md` |
| Evidencia externa/oficial | Confirmación interoperabilidad/entidad externa | Crítica (salida) | Sí (si aplica) | Sí (si aplica) | Sí (si aplica, junto con scorecard/aprobaciones) | Soporte oficial por cámara/tercero |

Regla: solo scorecard + evidencia requerida + aprobaciones pueden soportar salida productiva.

## 4. Documentos vigentes oficiales

| Documento | Ruta | Tipo | Estado | Autoridad | Observación |
|---|---|---|---|---|---|
| Matriz consolidada S1 | `docs/audits/s1-requirement-norm-code-test-evidence-closure-matrix-current.md` | Matriz consolidada | Vigente | Alta | Vista transversal de trazabilidad |
| Compuertas de evidencia | `docs/uat/human-signoff-evidence-classification-gates.md` | Guía | Vigente | Alta | Gobierno de evidencia/aprobación humana |
| Matriz maestra S1 | `docs/audits/s1-matriz-maestra-trazable-funcional-normativa-2026-04-26.md` | Matriz maestra | Vigente | Alta | Resumen maestro funcional-normativo |
| Scorecard GO/NO-GO | `docs/audits/go-nogo-scorecard-funcional-normativo-2026-04-26.md` | Scorecard | Vigente | Máxima | Vista ejecutiva de decisión |
| Matriz NACHA por registro | `docs/audits/nacha-record-level-normative-matrix-ach-cenit-current.md` | Matriz específica | Vigente | Alta | Detalle por cámara |
| Matriz causales ACH/CENIT/STA | `docs/audits/cause-code-normative-matrix-ach-cenit-sta-current.md` | Matriz específica | Vigente | Alta | Causales por flujo/cámara |
| Matriz naming externo | `docs/audits/external-filename-normative-matrix-ach-cenit-sta-current.md` | Matriz específica | Vigente | Alta | Naming por flujo/cámara |
| Matriz estado devolución saliente | `docs/audits/outbound-return-state-traceability-matrix-current.md` | Matriz específica | Vigente | Alta | Estado/evento/idempotencia |
| Matriz huérfanas inbound | `docs/audits/incoming-return-e2e-orphan-matrix-current.md` | Matriz específica | Vigente | Alta | E2E huérfanas/no resueltas |
| Matriz rechazo total/parcial | `docs/audits/total-vs-partial-rejection-matrix-current.md` | Matriz específica | Vigente | Alta | Regla total vs parcial |
| Matriz ciclos/neteo/liquidez/CUD | `docs/audits/cenit-cycles-netting-liquidity-cud-matrix-current.md` | Matriz específica | Vigente | Alta | S1-10/S1-11 |
| Matriz sobre/firma/certificados | `docs/audits/digital-envelope-signature-certificate-matrix-current.md` | Matriz específica | Vigente | Alta | S1-13/S1-14 |
| Matriz conciliación/revisión contable | `docs/audits/accounting-review-reconciliation-matrix-current.md` | Matriz específica | Vigente | Alta | Punto 10 |
| Documento dev/auditoría ACH/CENIT | `docs/dev/devoluciones-ach-auditoria-cenit-ach-colombia.md` | Documento dev/auditoría | Vigente | Media | Contexto técnico-funcional |

Si algún archivo no existe: marcar “No encontrado en el repositorio auditado”.

## 5. Criterio para documento histórico

Un documento se clasifica como histórico si:

- no tiene sufijo `current`;
- fue reemplazado explícitamente;
- contiene estado anterior;
- no está referenciado por scorecard o matriz vigente;
- contradice un documento vigente;
- está en carpeta archive/history si existe;
- tiene fecha antigua y no aparece como fuente vigente.

Regla: un documento histórico puede explicar decisiones pasadas, pero no soporta GO actual.

## 6. Política de precedencia

| Prioridad | Fuente | Uso |
|---|---|---|
| 1 | Evidencia externa/oficial y aprobaciones humanas firmadas | Cierre formal y decisiones críticas |
| 2 | Scorecard GO/NO-GO vigente | Decisión ejecutiva vigente |
| 3 | Matriz consolidada S1 vigente | Trazabilidad transversal |
| 4 | Matrices específicas `current` | Detalle por dominio/cámara |
| 5 | Checklists UAT vigentes | Evidencia de ejecución UAT |
| 6 | Runbooks operativos vigentes | Guía de operación/control |
| 7 | Documentos dev/auditoría vigentes | Contexto técnico/funcional |
| 8 | Documentos históricos | Referencia histórica |

Aclaraciones:

- Para decisiones productivas debe existir scorecard actualizado.
- Para trazabilidad técnica debe existir matriz consolidada.
- Para detalle por dominio manda matriz específica.
- Para UAT manda checklist + acta/evidencia humana.
- IA asistida aplica solo como pre-UAT/evidencia técnica.

## 7. Política de actualización

Cada cambio documental que afecte estado debe:

- actualizar matriz específica correspondiente;
- actualizar matriz consolidada S1 si afecta trazabilidad;
- actualizar scorecard si afecta GO/NO-GO;
- actualizar checklist UAT si afecta aceptación;
- actualizar runbook si afecta operación;
- actualizar referencias cruzadas;
- mantener NO-GO productivo si hay P0;
- registrar brecha si no hay evidencia suficiente.

## 8. Política de estados

Estados permitidos:

- GO técnico controlado.
- GO UAT asistido.
- GO UAT formal.
- GO UAT parcial/controlado.
- NO-GO productivo.
- Bloqueado.
- Parcial.
- Débil.
- Cerrado trazablemente.

Reglas:

- “Cerrado trazablemente” no implica GO productivo.
- “GO UAT asistido” no implica aprobación humana.
- “GO UAT formal” requiere acta/aprobación humana.
- “GO productivo” requiere scorecard, evidencia externa/oficial cuando aplique, cierre P0 y aprobación formal.
- Si hay P0, el estado productivo debe ser NO-GO.

## 9. Drift documental

Se entiende por drift documental:

- contradicción de estados entre documentos;
- documento histórico usado como vigente;
- falta de referencia cruzada;
- scorecard no actualizado;
- checklist UAT no alineado;
- matriz específica no sincronizada con matriz S1;
- evidencia IA tratada como aprobación humana;
- GO técnico confundido con GO productivo.

Controles:

- pruebas documentales como `S1TraceabilityMatrixTests`;
- revisión de referencias cruzadas;
- búsqueda periódica de “GO productivo: Sí”;
- revisión de documentos sin `current`;
- política de cambio por commit pequeño.

## 10. Reglas específicas para ACH Colombia vs CENIT

- ACH Colombia V3.2 debe sostener reglas ACH.
- CENIT DSP/Anexos deben sostener reglas CENIT.
- No extrapolar CENIT a ACH ni ACH a CENIT sin fuente.
- Si un requisito aplica a ambas cámaras, debe tener fuente/evidencia por cámara.
- Si no se demuestra una cámara, la fila queda parcial.
- CUD sigue boundary externo sin API runtime.
- Liquidez simulada no equivale saldo CUD real.
- DXX-LIQ no equivale rechazo oficial CUD.

## 11. Bloqueantes vigentes que no pueden relajarse por documentación

| Dominio | Bloqueante | Razón | Estado |
|---|---|---|---|
| S1-10 Neteo CENIT E2E | Cierre E2E neteo por ciclo/posición | Falta evidencia homologada E2E | NO-GO / bloqueado |
| S1-11 Liquidez/CUD | Cierre liquidez + evidencia CUD | CUD boundary externo sin cierre completo | NO-GO / bloqueado |
| S1-12 Naming externo ACH/CENIT/STA | Cierre naming por cámara/flujo | Cobertura parcial/hardcodes pendientes críticos | NO-GO / bloqueado |
| S1-13 Sobre digital interoperabilidad externa | Validación externa firma/cifrado | Falta interoperabilidad externa oficial | NO-GO / bloqueado |
| S1-20 UAT/runbooks/evidencia firmada | Cierre UAT formal y actas | Pendientes críticos y firma humana | NO-GO / bloqueado |

Ningún documento de política puede convertirlos a GO productivo sin evidencia requerida.

## 12. Checklist para modificar documentos vigentes

- [ ] ¿Cambia estado GO/NO-GO?
- [ ] ¿Cambia una brecha P0/P1/P2?
- [ ] ¿Afecta ACH Colombia, CENIT o ambas?
- [ ] ¿Tiene fuente normativa?
- [ ] ¿Tiene evidencia técnica?
- [ ] ¿Tiene evidencia UAT humana si aplica?
- [ ] ¿Requiere actualizar scorecard?
- [ ] ¿Requiere actualizar matriz consolidada?
- [ ] ¿Requiere actualizar matriz específica?
- [ ] ¿Requiere actualizar checklist UAT?
- [ ] ¿Requiere actualizar runbook?
- [ ] ¿Mantiene NO-GO si hay P0?

## 13. Veredicto de política

- Política documental vigente: **Sí**.
- GO técnico documental: **Sí, controlado**.
- GO UAT documental: **Parcial/controlado**.
- GO productivo: **NO**.
- NO-GO productivo vigente.

## 14. Referencia cruzada punto 12 (UAT real con datos reales/anonimizados)

- Documento vigente del protocolo: `docs/uat/real-data-uat-execution-protocol.md`.
- Plantillas de cierre humano:
  - `docs/uat/templates/real-data-uat-acta-template.md`
  - `docs/uat/templates/real-data-uat-evidence-index-template.md`
- Regla de gobierno: la existencia del protocolo y plantillas no cambia por sí sola el estado GO/NO-GO productivo.
- Para cambio de estado se requiere ejecución real, evidencia suficiente, aprobaciones humanas y scorecard actualizado.
