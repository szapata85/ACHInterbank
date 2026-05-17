# Matriz vigente — Ciclos CENIT, neteo, liquidez y evidencia CUD

## 1. Propósito
Definir estado actual y objetivo de control para ciclos CENIT, calendario, cutoff, neteo multilateral, liquidez, cuenta CUD/cuenta de depósito BanRep, evidencia de liquidación CUD, estados operativos, contabilidad y conciliación.
Aclaración obligatoria: CUD se trata como boundary externo operacional; no se asume API.

## 2. Estado actual
- GO técnico: limitado/controlado.
- GO UAT controlado: sí, parcial.
- NO-GO productivo: sí.
- Ciclos CENIT caracterizados (5 ciclos actuales).
- Neteo/liquidez existen técnicamente en el repo.
- CUD runtime/API no encontrado.
- No existe cierre E2E homologado CENIT → neteo → liquidez real/evidenciada → evidencia CUD → contabilidad → conciliación.
- Esta matriz no habilita producción.

## 3. Fuentes
- `tests/Cfa.ACHInterbank.Tests/CenitCycleCalendarCharacterizationTests.cs`
- `AchCycle`
- `ClearingHouse`
- `ClearingHouseCycleConfig`
- `AchCycleSeeder`
- `CenitOperatingCalendarPolicy`
- `BankHolidayModel`
- `IBankHoliday`
- `AchCycleScheduler`
- `TaskDefinition`
- `CenitNettingExecution`
- `CenitNetPosition`
- `CenitNettingDetail`
- `LiquidityOptimizationService`
- `DXX-LIQ`
- Banco de la República — CENIT
- Banco de la República — Sistema de Cuentas de Depósito CUD

## 4. Contexto operativo CENIT
CENIT opera como riel de bajo valor con cinco ciclos en días hábiles bancarios. Al corte de cada ciclo, se realiza compensación multilateral neta y las posiciones netas se liquidan contra cuentas de depósito en BanRep; adicionalmente se generan archivos de salida para participantes.

Para control operativo se distinguen explícitamente: archivo enviado, archivo recibido, posición neta, liquidez, evidencia CUD, liquidación, contabilidad y conciliación. Esta separación evita asumir que “archivo generado” implique “liquidación firme”.

## 5. Contexto operativo CUD sin API
CUD es un sistema de cuentas de depósito/pagos de alto valor con liquidación bruta en tiempo real, sujeto a saldo suficiente, y con efecto de débito ordenante/crédito beneficiario con firmeza/finalidad cuando la operación queda cumplida.

En el proyecto actual no hay API CUD ni cliente CUD runtime; por tanto no se asume integración técnica directa. La evidencia operacional puede provenir de operación manual, reportes, extractos, soportes, archivos autorizados, SEBRA/canales institucionales o mecanismos homologados de la entidad.

## 6. Principios de diseño CUD sin API
1. No depender de API CUD.
2. No representar CUD solo como saldo booleano.
3. Registrar evidencia: cuenta, participante, ciclo, fecha valor, monto, referencia, estado, soporte, actor, timestamps, hash del soporte, correlación con neteo/contabilidad/conciliación.
4. Separar simulación UAT de evidencia real.
5. Contabilidad solo con evidencia CUD confirmada/aprobada.
6. Firmeza/finalidad por evidencia, no por presunción.
7. Operación manual auditable e idempotente.
8. Correcciones manuales con histórico.

## 7. Matriz ciclos CENIT actuales

| Ciclo | StartTime | EndTime | CutoffTime | Cruza medianoche | IsActive | Vigencia | Fuente | Riesgo |
|---|---:|---:|---:|---|---|---|---|---|
| Ciclo 1 | 19:01 | 08:30 | 08:30 | Sí | Sí | EffectiveFrom/EffectiveTo | Seed/config actual | Medio |
| Ciclo 2 | 08:31 | 11:00 | 11:00 | No | Sí | EffectiveFrom/EffectiveTo | Seed/config actual | Medio |
| Ciclo 3 | 11:01 | 14:00 | 14:00 | No | Sí | EffectiveFrom/EffectiveTo | Seed/config actual | Medio |
| Ciclo 4 | 14:01 | 16:00 | 16:00 | No | Sí | EffectiveFrom/EffectiveTo | Seed/config actual | Medio |
| Ciclo 5 | 16:01 | 18:00 | 18:00 | No | Sí | EffectiveFrom/EffectiveTo | Seed/config actual | Medio |

Nota: horarios y ventana reflejan seed/config vigente, parametrizables por vigencia; no se declaran como definitivos normativos perennes.

## 8. Matriz calendario/festivos

| Control | Implementación actual | Cobertura runtime | Evidencia | Brecha | Riesgo |
|---|---|---|---|---|---|
| BankHolidayModel | Existe | Parcial | Modelo+tests | Sin malla E2E completa | Medio |
| IBankHoliday | Existe | Parcial | Servicio+tests | Uso no homogéneo en todos los flujos | Medio |
| IsBusinessDay | Existe | Parcial | Caracterización | Faltan escenarios operativos extensos | Medio |
| NextBusinessDay | Existe | Parcial | Servicio | Dependencia de calendario configurado | Medio |
| Scheduler skip non-business day | Existe | Sí (scheduler) | Tests + scheduler | Falta traza E2E operativa integrada | Medio |
| EffectiveFrom/EffectiveTo | Existe en ciclos | Sí | Config ciclos | Requiere disciplina operacional anual | Medio |
| America/Bogota (TaskDefinition) | Existe default | Parcial | Modelo/task | Mezcla con DateTime.Now/UtcNow | Alto |
| Uso mixto DateTime.Now/UtcNow | Existe | Parcial | Caracterización | Riesgo de desfase horario/cutoff | Alto |
| Validación returns/incoming/ROR con calendario | Parcial | Parcial | Matrices actuales | Falta convergencia E2E | Alto |

## 9. Matriz cutoff/ventanas

| Control | Actual | Riesgo | Criterio esperado | Evidencia requerida |
|---|---|---|---|---|
| StartTime/EndTime | Parametrizados | Medio | Ventanas consistentes por ciclo | Config vigente + UAT |
| CutoffTime | Parametrizado | Medio | Corte consistente con ventana | Prueba por ciclo |
| Ventana cruzando medianoche | Presente en ciclo 1 | Alto | Regla circular explícita | Evidencia funcional por transacción |
| Transacción dentro/fuera de ventana | Parcial | Alto | Determinismo por ciclo/hora | Casos UAT controlados |
| Generación fuera de cutoff | Parcial | Alto | Bloqueo o política explícita | Evidencia operativa |
| Incoming fuera de ciclo | Parcial | Alto | Regla de resolución/reproceso | Evidencia por flujo |
| Reproceso | Parcial | Medio | Política trazable | Runbook UAT |
| Timezone operacional | Mixto Now/UtcNow | Alto | Criterio único por operación | Checklist timezone |
| Impacto naming/NACHA/idempotencia | Parcial | Alto | Alineación por ciclo/corte | Evidencia transversal |

## 10. Matriz neteo

Definición: Neteo CENIT = posición multilateral neta por ciclo/participante/fecha antes de liquidación en cuentas de depósito.

| Elemento | Existe | Estado | Fuente | Brecha | Siguiente control |
|---|---|---|---|---|---|
| CenitNettingExecution | Sí | Implementado técnico | Servicio/modelo | Falta cierre E2E con evidencia CUD | Trazabilidad settlement |
| CenitNetPosition | Sí | Implementado técnico | Servicio/modelo | Falta homologación operativa | Validación por participante |
| CenitNettingDetail | Sí | Implementado técnico | Servicio/modelo | Falta enlace formal a evidencia CUD | Correlación documental |
| Relación con ciclo/cámara/value date | Sí | Parcial E2E | Persistencia | Falta circuito completo | Controles de conciliación |
| Débito/crédito/net amount | Sí | Técnico | Persistencia | Falta validación externa homologada | UAT operativo |
| Estado de ejecución neteo | Parcial | Parcial | Ejecución actual | Falta estado de liquidación CUD | Modelo de estados evidencia |
| Auditoría neteo | Parcial | Parcial | Datos persistidos | Falta runbook y firma operacional | Evidencia firmable |
| Vínculo con archivo | Parcial | Parcial | Referencias existentes | Falta control completo archivo→liquidación | Matriz E2E |
| Vínculo con liquidez | Sí | Parcial | Flujo actual | Liquidez simulada no equivale saldo real CUD | Política fuente homologada |
| Vínculo con contabilidad/conciliación | Parcial | No cerrado | Matrices actuales | Falta gate por evidencia CUD confirmada | Control de posting |

## 11. Matriz liquidez

| Elemento | Existe | Estado | Fuente | Brecha | Riesgo |
|---|---|---|---|---|---|
| LiquidityOptimizationService | Sí | Implementado técnico | Servicio | Falta homologación con evidencia CUD real | Alto |
| Saldo simulado | Sí | UAT/técnico | Lógica interna | Puede confundirse con saldo real CUD | Alto |
| Processed | Sí | Implementado | Decisión actual | Falta vínculo final de liquidación evidenciada | Alto |
| Deferred | Sí | Implementado | Decisión actual | Falta política operativa integral | Medio |
| Rejected | Sí | Implementado | Decisión actual | Falta traza CUD/evidencia asociada | Medio |
| DXX-LIQ | Sí | Interno | Código interno | No debe extrapolarse como causal externa CUD | Medio |
| Insuficiencia de liquidez | Sí | Parcial | Servicio | Falta fuente homologada externa | Alto |
| Auditoría decisiones | Parcial | Parcial | Persistencia | Falta evidencia documental de liquidación | Alto |
| No exposición externa | Parcial | Parcial | Diseño actual | Requiere control de reporting | Medio |
| Relación con net positions | Sí | Parcial | Flujo actual | Falta cierre E2E operacional | Alto |
| Relación con evidencia CUD | No cerrada | Brecha | N/A | Sin modelo formal evidencia | Crítico |
| Reintentos/pendientes/cutoff/escalamiento | Parcial | Parcial | Operación actual | Falta runbook consolidado | Alto |

Aclaración: la liquidez actual no equivale a saldo real CUD.

## 12. Matriz CUD actual y objetivo sin API

| Capacidad CUD/evidencia | Estado repo actual | Objetivo parametrizable sin API | Evidencia requerida | Riesgo |
|---|---|---|---|---|
| Cuenta CUD/cuenta de depósito | No modelado E2E | Referencia parametrizable | Soporte de cuenta y titularidad | Alto |
| Participante ordenante/beneficiario | Parcial técnico | Referencia operacional trazable | Evidencia de participantes | Alto |
| Posición neta relacionada | Parcial | Correlación explícita | Relación neteo↔evidencia | Alto |
| Fecha valor | Parcial | Campo obligatorio de evidencia | Soporte con fecha valor | Alto |
| Monto/moneda | Parcial | Campos obligatorios | Soporte documental | Alto |
| Referencia CUD | No encontrada runtime | Registro obligatorio | Referencia verificable | Crítico |
| Estado evidencia | No consolidado | Estados parametrizables | Workflow auditable | Crítico |
| Soporte documental | No consolidado | Adjuntos/huellas controladas | Archivo/reporte/extracto | Alto |
| Usuario/actor/timestamps | Parcial | Control completo de auditoría | Log y firma operativa | Alto |
| Pendiente/confirmado/rechazado/revisión/conciliado | No consolidado | Catálogo de estados | Evidencia por estado | Crítico |
| Hash de soporte | No consolidado | Hash obligatorio | Integridad documental | Alto |
| Correlación neteo/asiento/conciliación | Parcial | CorrelationId + idempotency key | Cruce consistente | Crítico |
| Aprobación (doble control) | No consolidado | Workflow aprobación | Evidencia aprobatoria | Alto |

Aclaraciones:
- No existe API CUD en el repo.
- No se asume que CUD exponga API.
- Objetivo actual: trazabilidad y evidencia operacional.
- UAT puede usar simulador, marcado explícitamente como simulación.
- Contabilidad solo con evidencia CUD confirmada/aprobada.

## 13. Modos CUD/evidencia sin API
### ManualEvidenceMode
- Tesorería/operaciones registra referencia CUD.
- Adjunta soporte/extracto.
- Registra cuenta, valor, fecha, estado, observación y actor.
- No consume API.

### FileOrReportEvidenceMode
- Importa archivo/reporte autorizado.
- Cruza referencia, cuenta, valor, fecha y estado.
- Conserva hash y no asume API transaccional.

### SimulatedUatMode
- Simulador interno solo para UAT.
- Parametriza saldo, aprobación/rechazo, tiempos y estados.
- No equivale a liquidación real.

## 14. Modelo objetivo parametrizable sin API
Propuesta documental (sin implementación).

Entidades futuras posibles:
- CudSettlementEvidence
- CudSettlementEvidenceFile
- CudSettlementStatusEvent
- CudAccountReference
- CudParticipantReference
- CenitSettlementBatch
- CenitSettlementPosition
- CudManualEvidenceApproval
- CudEvidenceReconciliation
- CudUatSimulationScenario
- CudUatSimulationResult

Interfaces futuras posibles (sin asumir API):
- ICudSettlementEvidenceProvider
- ICudSettlementConfirmationProvider
- ICudManualEvidenceService
- ICudSettlementStatusPolicy
- ICudSettlementEvidenceValidator
- ICudSettlementSimulator
- ISettlementFinalityPolicy
- ILiquidityDecisionPolicy
- ICenitNettingService
- ICenitSettlementEvidenceOrchestrator

Parámetros:
modo, cuenta CUD, participante ordenante/beneficiario, moneda, ventana liquidación, política liquidez, política evidencia, aprobación manual, conciliación, reintentos, reverso/ajuste si aplica, timezone, corte, UAT/producción, doble control, soporte documental, hash obligatorio.

## 15. Estados CUD/evidencia sugeridos

| Estado | Significado | Permite contabilidad | Permite conciliación final | Requiere actor | Observación |
|---|---|---|---|---|---|
| NotRequired | No aplica evidencia CUD | No | No | No | Objetivo |
| NotRegistered | Sin evidencia registrada | No | No | Sí | Objetivo |
| PendingEvidence | En espera de soporte | No | No | Sí | Objetivo |
| EvidenceRegistered | Soporte cargado | No | No | Sí | Objetivo |
| EvidenceUnderReview | En revisión | No | No | Sí | Objetivo |
| SettlementConfirmedByEvidence | Confirmada por evidencia | Sí | Parcial | Sí | Objetivo |
| SettlementRejectedByEvidence | Rechazada por evidencia | No | No | Sí | Objetivo |
| LiquidityInsufficient | Insuficiencia de liquidez | No | No | Sí | Objetivo |
| ManuallyAdjusted | Ajuste manual auditado | Condicional | Condicional | Sí | Objetivo |
| Reconciled | Conciliada | Sí | Sí | Sí | Objetivo |
| Disputed | En disputa | No | No | Sí | Objetivo |
| SimulationApproved | Aprobada en simulación UAT | No (productivo) | No (productivo) | Sí | UAT |
| SimulationRejected | Rechazada en simulación UAT | No | No | Sí | UAT |

Aclaraciones:
- Son estados objetivo, no necesariamente existentes hoy.
- Simulación no equivale a CUD real.
- Contabilidad final solo con evidencia confirmada/reconciled según política aprobada.

## 16. Relación con contabilidad y conciliación
El neteo produce posición neta operacional; la liquidez define avance/diferido/rechazo; la evidencia CUD confirma o rechaza liquidación. En consecuencia, contabilidad no debe postear como liquidado sin evidencia CUD confirmada/aprobada.

La conciliación debe separar: generado, enviado, aceptado, neteado, pendiente liquidez, insuficiencia, evidencia pendiente, evidencia registrada, confirmado por evidencia, rechazado, contabilizado, conciliado y disputado.

## 17. Brechas P0/P1/P2
### P0
- No hay runtime CUD ni evidencia CUD E2E.
- No hay cierre CENIT→neteo→liquidez→evidencia CUD→contabilidad→conciliación.
- Riesgo de contabilizar sin liquidación firme.
- Riesgo timezone/cutoff.
- Liquidez simulada confundida con saldo real CUD.
- Falta boundary formal de evidencia CUD.

### P1
- Falta checklist UAT específico.
- Falta runbook operativo.
- Falta reporte por ciclo/posición/liquidez/evidencia CUD.
- Falta simulador formal UAT.
- Falta modelo de evidencia manual/archivo/reporte.
- Falta estados de evidencia CUD.

### P2
- Dashboard, métricas, alertas, replay por ciclo, optimización liquidez, automatización futura si existe canal homologado.

## 18. Criterios de salida de NO-GO
1. Ciclos CENIT validados.
2. Calendario hábil/festivo validado.
3. Cutoff/ventanas validados.
4. Timezone definido.
5. Neteo E2E validado.
6. Posiciones netas validadas.
7. Liquidez validada contra fuente homologada o simulador aprobado UAT.
8. Cuenta CUD parametrizada.
9. Modo CUD definido: manual evidence / file-report evidence / simulated UAT.
10. Evidencia CUD modelada.
11. Referencia CUD registrada.
12. Estado evidencia CUD definido.
13. Firmeza/finalidad por evidencia aprobada.
14. Contabilidad depende de evidencia confirmada.
15. Conciliación incluye evidencia CUD.
16. Reintentos/errores/pendientes definidos.
17. Manual override gobernado.
18. Doble control definido.
19. UAT ACH/CENIT ejecutado.
20. Evidencia BanRep/CENIT disponible.
21. Firmas negocio/operaciones/tesorería/riesgo/compliance.
22. Aprobación tecnología.
23. Scorecard actualizado.

## 19. Decisión vigente
- GO técnico: limitado/controlado.
- GO UAT controlado: sí, parcial.
- NO-GO productivo: sí.
- Esta matriz no habilita producción.
- Próximo paso: checklist UAT CENIT ciclos/liquidez/CUD o diseño documental de simulador CUD parametrizable.

- Referencia UAT ciclos/liquidez/evidencia CUD: `docs/uat/cenit-cycles-liquidity-cud-acceptance-checklist.md` (no cambia decisión NO-GO productivo).

- Referencia matriz vigente de sobre/firma/certificados: `docs/audits/digital-envelope-signature-certificate-matrix-current.md` (no modifica decisión NO-GO productivo).
