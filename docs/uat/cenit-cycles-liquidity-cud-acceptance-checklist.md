# Checklist UAT — Ciclos CENIT, neteo, liquidez y evidencia CUD

## 1. Propósito
Validar en UAT:
- ciclos CENIT;
- calendario hábil/festivo;
- cutoff/ventanas;
- neteo multilateral;
- liquidez;
- evidencia CUD sin API;
- contabilidad;
- conciliación;
- criterios de salida de NO-GO.

## 2. Estado actual
- GO técnico: limitado/controlado.
- GO UAT controlado: sí, parcial.
- NO-GO productivo: sí.
- El checklist no habilita producción.
- CUD runtime/API no existe.
- La evidencia CUD puede ser manual, archivo/reporte o simulación UAT.

## 3. Fuentes
- `docs/audits/cenit-cycles-netting-liquidity-cud-matrix-current.md`
- `tests/Cfa.ACHInterbank.Tests/CenitCycleCalendarCharacterizationTests.cs`
- `docs/audits/go-nogo-scorecard-funcional-normativo-2026-04-26.md`
- `docs/audits/s1-matriz-maestra-trazable-funcional-normativa-2026-04-26.md`
- `docs/uat/nacha-records-acceptance-checklist.md`
- `docs/uat/outbound-return-state-traceability-acceptance-checklist.md`
- `docs/uat/incoming-return-orphan-acceptance-checklist.md`
- `docs/uat/rejection-total-partial-acceptance-checklist.md`

## 4. Alcance UAT
- ciclos CENIT 1..5;
- vigencia de configuración;
- día hábil;
- festivo/no hábil;
- cutoff;
- ventana cruzando medianoche;
- neteo;
- liquidez;
- DXX-LIQ;
- ManualEvidenceMode;
- FileOrReportEvidenceMode;
- SimulatedUatMode;
- contabilidad;
- conciliación;
- trazabilidad por ciclo.

## 5. Checklist ciclos CENIT

| ID | Control | Resultado esperado | Evidencia | Estado | Observación |
|---|---|---|---|---|---|
| CEN-CYC-01 | CENIT tiene 5 ciclos | Existen 5 ciclos del día operativo | Consulta config/reportes | Pendiente | |
| CEN-CYC-02 | Ciclos 1..5 activos | IsActive según vigencia | Captura de configuración | Pendiente | |
| CEN-CYC-03 | EffectiveFrom/EffectiveTo presentes | Vigencias trazables por versión | Historial de configuración | Pendiente | |
| CEN-CYC-04 | Ciclo 1 cruza medianoche | Ventana 19:01-08:30 vigente | Evidencia de ventana | Pendiente | |
| CEN-CYC-05 | CutoffTime definido | Cada ciclo tiene cutoff no vacío | Config + reporte | Pendiente | |
| CEN-CYC-06 | Horarios actuales coinciden con matriz | 1: 19:01-08:30, 2: 08:31-11:00, 3: 11:01-14:00, 4: 14:01-16:00, 5: 16:01-18:00 | Matriz + configuración | Pendiente | |
| CEN-CYC-07 | Cambio de vigencia no rompe históricos | Versionado conserva ciclos anteriores | Evidencia de versiones | Pendiente | |
| CEN-CYC-08 | No asumir horarios perpetuos | Se documenta parametrización por vigencia | Acta UAT | Pendiente | |
| CEN-CYC-09 | Ciclos parametrizables | Ajustes controlados por vigencia | Trazabilidad de cambio | Pendiente | |

## 6. Checklist calendario/festivos
- Día hábil permite operación.
- Festivo/no hábil no agenda ciclo.
- `NextBusinessDay` retorna fecha esperada.
- Calendario aplica a scheduler.
- Validar si aplica a returns/incoming/ROR.
- Identificar brechas de uso parcial.
- Timezone `America/Bogota` documentado.
- Riesgo `DateTime.Now/UtcNow` registrado.

## 7. Checklist cutoff/ventanas
- Transacción dentro de ventana.
- Transacción fuera de ventana.
- Cutoff antes/dentro de ventana.
- Ciclo cruzando medianoche usa regla circular.
- Generación fuera de cutoff tiene regla explícita o brecha registrada.
- Incoming fuera de ciclo queda trazado.
- Reproceso de ciclo anterior queda trazado.
- Timezone local vs UTC registrado.

## 8. Checklist neteo
- Existe ejecución de neteo.
- Posición neta por ciclo.
- Posición neta por participante.
- Total débito.
- Total crédito.
- Net amount.
- Value date.
- Clearing house.
- Relación con archivo.
- Relación con liquidez.
- Relación con evidencia CUD.
- No se considera liquidado solo por netear.
- Evidencia de cálculo preservada.

## 9. Checklist liquidez
- Evaluación `Processed`.
- Evaluación `Deferred`.
- Evaluación `Rejected`.
- `DXX-LIQ` se mantiene interno.
- Liquidez simulada no equivale a saldo real CUD.
- Insuficiencia de liquidez no se expone como causal externa.
- Decisión de liquidez queda auditada.
- Pendientes/reintentos documentados.
- Escalamiento operativo definido.
- Manual override requiere control y evidencia.

## 10. Checklist evidencia CUD sin API
Aclaración obligatoria: no existe API CUD en el proyecto. UAT debe validar evidencia operacional, no integración técnica directa.

- Cuenta CUD / cuenta de depósito registrada.
- Participante ordenante.
- Participante beneficiario.
- Posición neta relacionada.
- Fecha valor.
- Monto.
- Moneda.
- Referencia CUD.
- Estado de evidencia.
- Soporte documental.
- Hash del soporte.
- Actor que registra.
- Timestamp de registro.
- Timestamp de liquidación informada.
- Correlation id.
- Idempotency key.
- Aprobación manual si aplica.
- Vínculo con neteo.
- Vínculo con asiento contable.
- Vínculo con conciliación.

## 11. Checklist ManualEvidenceMode
- Tesorería registra referencia CUD.
- Adjunta soporte.
- Registra cuenta, valor, fecha, estado y observación.
- No consume API.
- Requiere actor.
- Conserva hash.
- Permite revisión/aprobación.
- Permite rechazo.
- Permite corrección con histórico.
- No habilita contabilidad sin aprobación.

## 12. Checklist FileOrReportEvidenceMode
- Importar archivo/reporte autorizado.
- Validar formato.
- Calcular hash.
- Cruzar referencia/cuenta/valor/fecha.
- Identificar no conciliados.
- Identificar duplicados.
- Preservar archivo original.
- No asumir API transaccional.
- Generar evidencia auditable.

## 13. Checklist SimulatedUatMode
- Simulador marcado como UAT.
- Saldo parametrizable.
- Aprobación parametrizable.
- Rechazo parametrizable.
- Tiempos parametrizables.
- Estados simulados.
- No equivale a CUD real.
- No habilita producción.
- Resultados claramente marcados como simulados.

## 14. Checklist estados CUD/evidencia

| Estado | Permite contabilidad | Permite conciliación final | Requiere actor | Evidencia |
|---|---|---|---|---|
| NotRegistered | No | No | Sí | Registro de ausencia |
| PendingEvidence | No | No | Sí | Pendiente documentado |
| EvidenceRegistered | No | No | Sí | Soporte + hash |
| EvidenceUnderReview | No | No | Sí | Flujo de revisión |
| SettlementConfirmedByEvidence | Sí (según política) | Parcial/Final | Sí | Confirmación evidenciada |
| SettlementRejectedByEvidence | No | No | Sí | Rechazo evidenciado |
| LiquidityInsufficient | No | No | Sí | Causal interna + contexto |
| ManuallyAdjusted | Condicional | Condicional | Sí | Ajuste aprobado |
| Reconciled | Sí | Sí | Sí | Conciliación cerrada |
| Disputed | No | No | Sí | Disputa documentada |
| SimulationApproved | No (productivo) | No (productivo) | Sí | Resultado simulación |
| SimulationRejected | No | No | Sí | Resultado simulación |

## 15. Checklist contabilidad
- Neteo no genera contabilización final por sí solo.
- Liquidez simulada no genera contabilización final.
- Evidencia CUD confirmada/aprobada habilita contabilización según política.
- Evidencia pendiente bloquea contabilización final.
- Evidencia rechazada bloquea contabilización.
- Ajustes manuales requieren aprobación.
- Asiento debe referenciar ciclo, posición neta y evidencia CUD.

## 16. Checklist conciliación
- Generado.
- Enviado.
- Aceptado.
- Neteado.
- Pendiente liquidez.
- Liquidez insuficiente.
- Evidencia CUD pendiente.
- Evidencia registrada.
- Confirmada por evidencia.
- Rechazada por evidencia.
- Contabilizado.
- Conciliado.
- Disputado.

Cada categoría debe poder reportarse separadamente.

## 17. Evidencia requerida
- Configuración de ciclos.
- Calendario.
- Cutoff.
- Archivo entrada/salida.
- Ejecución neteo.
- Posiciones netas.
- Decisión liquidez.
- DXX-LIQ si aplica.
- Cuenta CUD.
- Participantes.
- Referencia CUD.
- Soporte/evidencia.
- Hash soporte.
- Actor.
- Timestamps.
- Aprobación.
- Asiento contable.
- Reporte conciliación.
- Acta UAT.
- Firmas negocio/operaciones/tesorería/riesgo/compliance.

## 18. Criterios de salida de NO-GO
1. Ciclos CENIT validados.
2. Calendario validado.
3. Cutoff validado.
4. Timezone definido.
5. Neteo E2E validado.
6. Posiciones netas validadas.
7. Liquidez validada.
8. DXX-LIQ controlado.
9. Modo CUD definido.
10. Evidencia CUD modelada.
11. Referencia CUD registrada.
12. Soporte documental obligatorio.
13. Hash de soporte obligatorio.
14. Aprobación definida.
15. Contabilidad depende de evidencia confirmada.
16. Conciliación incluye evidencia CUD.
17. Reintentos/pendientes definidos.
18. Manual override gobernado.
19. Simulación UAT separada de evidencia real.
20. UAT ACH/CENIT ejecutado.
21. Firmas negocio/operaciones/tesorería/riesgo/compliance.
22. Aprobación tecnología.
23. Scorecard actualizado.

## 19. Riesgos residuales
- CUD sin API runtime.
- Evidencia manual incompleta.
- Liquidez simulada confundida con saldo real.
- Contabilizar sin liquidación firme.
- Desfase timezone/cutoff.
- Reportes sin categoría clara.
- Ausencia de runbook.
- UAT real pendiente.
- NO-GO productivo vigente.

## 20. Decisión vigente
- GO técnico: limitado/controlado.
- GO UAT controlado: sí, parcial.
- NO-GO productivo: sí.
- Este checklist no habilita producción.
- Próximo paso: runbook operativo o diseño documental del simulador CUD parametrizable.

- Referencia matriz vigente de sobre/firma/certificados: `docs/audits/digital-envelope-signature-certificate-matrix-current.md` (no modifica decisión NO-GO productivo).

- Referencia checklist UAT de sobre/firma/certificados: `docs/uat/digital-envelope-certificate-acceptance-checklist.md` (no modifica decisión NO-GO productivo).

> Referencia punto 10 (reportería/conciliación revisión contable terceros, no contable): `docs/audits/accounting-review-reconciliation-matrix-current.md`.

- Referencia checklist UAT punto 10: `docs/uat/accounting-review-reconciliation-acceptance-checklist.md`.

- Referencia runbook operativo conciliación punto 10: `docs/ops/reconciliation-operations-runbook.md`.


> Referencia cruzada punto 10 (reportería/conciliación contra terceros):
> - `docs/audits/accounting-review-reconciliation-matrix-current.md`
> - `docs/uat/accounting-review-reconciliation-acceptance-checklist.md`
> - `docs/ops/reconciliation-operations-runbook.md`
>
> El endpoint backend `POST /api/reports/accounting-review/export` provee exportación PDF/CSV/XLSX para soporte de revisión operativa, sin contabilizar y sin cambiar el NO-GO productivo.

Referencia de trazabilidad consolidada: para trazabilidad requisito→norma→código→prueba→evidencia por cámara, ver `docs/audits/s1-requirement-norm-code-test-evidence-closure-matrix-current.md`. Esta referencia no cambia NO-GO productivo.


Referencia de compuertas de evidencia y aprobación humana: para clasificación de evidencia, GO UAT formal y aprobación humana, ver `docs/uat/human-signoff-evidence-classification-gates.md`. Esta referencia no cambia NO-GO productivo.
