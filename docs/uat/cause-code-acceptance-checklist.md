# Checklist UAT — Causales por cámara y flujo

> Referencia UAT complementaria: `docs/uat/incoming-return-orphan-acceptance-checklist.md`.


## 1. Propósito
Este checklist valida causales por:
- cámara;
- flujo;
- tipo;
- origen normativo;
- comportamiento técnico;
- evidencia UAT;
- decisión Go/No-Go.

Cobertura:
- devolución saliente;
- devolución entrante;
- devolución de devolución (ROR);
- rechazo total de archivo;
- rechazo parcial de registros;
- mensajes técnicos/informativos;
- códigos internos no externos.

## 2. Estado actual
- **GO técnico:** sí.
- **GO UAT controlado:** sí.
- **NO-GO productivo:** sí.
- La policy rail-flow está implementada técnicamente (`IAchCauseCodePolicy`) y validada por pruebas.
- Las causales están caracterizadas por tests.
- Falta evidencia UAT firmada por cámara/flujo.
- CENIT Anexo A confirma causales Rxx, pero requiere cierre por flujo.
- ACH Colombia requiere ratificación por manual/operador/UAT.
- Dxx/Ixxx no son causales Rxx.
- `DXX-LIQ` es interno/no externo.

## 3. Alcance por dominio

### 3.1 ACH Colombia
Validar `R07`, `R10`, `R29`, `R31`, `DEV14` por flujo:
- devolución saliente;
- devolución entrante;
- ROR si aplica;
- command center si aplica.

### 3.2 CENIT
Validar `R01`, `R02`, `R03`, `R04`, `R06`, `R08`, `R09`, `R12`, `R13`, `R14`, `R15`, `R16`, `R17`, `R20`, `R23` por flujo:
- devolución saliente;
- devolución entrante;
- ROR si aplica.

### 3.3 STA / rechazo
Validar `D01`, `D02`, `D03`, `D04`, `D05`, `D06` como rechazo total/parcial, no como devolución.

### 3.4 Técnicos / integración
Validar `I500`, `I503`, `ITIMEOUT`, `ISOAP`, `IFUNC` como operator response / command center / integración técnica, no como devolución.

### 3.5 Internos
Validar `DXX-LIQ`:
- solo `InternalOnly`;
- no externo;
- no cámara;
- no registro 7.

## 4. Checklist técnico por causal

| ID | Cámara | Código | Tipo | Flujo | Validación esperada | Comportamiento técnico | Evidencia requerida | Estado | Observaciones |
|---|---|---|---|---|---|---|---|---|---|
| C-ACH-01 | ACH | R07 | Rxx | Outbound Return | Permitido para ACH | Policy rail-flow permite en ACH | Archivo NACHA + R7 + resultado policy | Pendiente | No extrapolar a CENIT |
| C-ACH-02 | ACH | R10 | Rxx | Outbound Return | Permitido para ACH | Policy rail-flow permite en ACH | Archivo NACHA + R7 + resultado policy | Pendiente | |
| C-ACH-03 | ACH | R29 | Rxx | Outbound Return | Permitido para ACH | Policy rail-flow permite en ACH | Archivo NACHA + R7 + resultado policy | Pendiente | |
| C-ACH-04 | ACH | R31 | Rxx | Outbound Return | Permitido para ACH | Policy rail-flow permite en ACH | Archivo NACHA + R7 + resultado policy | Pendiente | |
| C-ACH-05 | ACH | DEV14 | DEVxx | Outbound Return | Permitido para ACH | Policy rail-flow permite en ACH | Archivo NACHA + R7 + resultado policy | Pendiente | |
| C-ACH-06 | ACH | R07 | Rxx | Incoming Return | Permitido para ACH | Incoming valida rail-flow | Resultado ingestión + issue/evento | Pendiente | |
| C-ACH-07 | ACH | DEV14 | DEVxx | Incoming Return | Permitido para ACH | Incoming valida rail-flow | Resultado ingestión + issue/evento | Pendiente | |
| C-ACH-08 | ACH | R07/R10/R29/R31/DEV14 | Rxx/DEVxx | ROR | Aplicabilidad por policy vigente | ROR valida causal original/nueva/linaje | Evaluate + audit/nacha + acta | Pendiente | Si aplica / pendiente por causal |
| C-CEN-01 | CENIT | R01 | Rxx | Outbound Return | Permitido para CENIT | Policy rail-flow permite en CENIT | Archivo NACHA + R7 + resultado policy | Pendiente | |
| C-CEN-02 | CENIT | R02 | Rxx | Outbound Return | Permitido para CENIT | Policy rail-flow permite en CENIT | Archivo NACHA + R7 + resultado policy | Pendiente | |
| C-CEN-03 | CENIT | R03 | Rxx | Outbound Return | Permitido para CENIT | Policy rail-flow permite en CENIT | Archivo NACHA + R7 + resultado policy | Pendiente | |
| C-CEN-04 | CENIT | R04 | Rxx | Outbound Return | Permitido para CENIT | Policy rail-flow permite en CENIT | Archivo NACHA + R7 + resultado policy | Pendiente | |
| C-CEN-05 | CENIT | R06 | Rxx | Outbound Return | Permitido para CENIT | Policy rail-flow permite en CENIT | Archivo NACHA + R7 + resultado policy | Pendiente | |
| C-CEN-06 | CENIT | R08 | Rxx | Outbound Return | Permitido para CENIT | Policy rail-flow permite en CENIT | Archivo NACHA + R7 + resultado policy | Pendiente | |
| C-CEN-07 | CENIT | R09 | Rxx | Outbound Return | Permitido para CENIT | Policy rail-flow permite en CENIT | Archivo NACHA + R7 + resultado policy | Pendiente | |
| C-CEN-08 | CENIT | R12 | Rxx | Outbound Return | Permitido para CENIT | Policy rail-flow permite en CENIT | Archivo NACHA + R7 + resultado policy | Pendiente | |
| C-CEN-09 | CENIT | R13 | Rxx | Outbound Return | Permitido para CENIT | Policy rail-flow permite en CENIT | Archivo NACHA + R7 + resultado policy | Pendiente | |
| C-CEN-10 | CENIT | R14 | Rxx | Outbound Return | Permitido para CENIT | Policy rail-flow permite en CENIT | Archivo NACHA + R7 + resultado policy | Pendiente | |
| C-CEN-11 | CENIT | R15 | Rxx | Outbound Return | Permitido para CENIT | Policy rail-flow permite en CENIT | Archivo NACHA + R7 + resultado policy | Pendiente | |
| C-CEN-12 | CENIT | R16 | Rxx | Outbound Return | Permitido para CENIT | Policy rail-flow permite en CENIT | Archivo NACHA + R7 + resultado policy | Pendiente | |
| C-CEN-13 | CENIT | R17 | Rxx | Outbound Return | Permitido para CENIT | Policy rail-flow permite en CENIT | Archivo NACHA + R7 + resultado policy | Pendiente | |
| C-CEN-14 | CENIT | R20 | Rxx | Outbound Return | Permitido para CENIT | Policy rail-flow permite en CENIT | Archivo NACHA + R7 + resultado policy | Pendiente | |
| C-CEN-15 | CENIT | R23 | Rxx | Outbound Return | Permitido para CENIT | Policy rail-flow permite en CENIT | Archivo NACHA + R7 + resultado policy | Pendiente | |
| C-CEN-16 | CENIT | R01 | Rxx | Incoming Return | Permitido para CENIT | Incoming valida rail-flow | Resultado ingestión + issue/evento | Pendiente | |
| C-CEN-17 | CENIT | R02 | Rxx | Incoming Return | Permitido para CENIT | Incoming valida rail-flow | Resultado ingestión + issue/evento | Pendiente | |
| C-CEN-18 | CENIT | R01→R02 | Rxx | ROR | Permitido si policy actual lo habilita | ROR valida causal original/nueva/linaje | Evaluate + audit/nacha + acta | Pendiente | Según policy vigente |
| C-REJ-01 | STA/Rechazo | D01 | Dxx | File Reject Total | Permitido como rechazo total | Clasificado `FileRejection` | Evidencia rechazo total | Pendiente | No devolución |
| C-REJ-02 | STA/Rechazo | D02 | Dxx | File Reject Total | Permitido como rechazo total | Clasificado `FileRejection` | Evidencia rechazo total | Pendiente | No devolución |
| C-REJ-03 | STA/Rechazo | D03 | Dxx | File Reject Total | Permitido como rechazo total | Clasificado `FileRejection` | Evidencia rechazo total | Pendiente | No devolución |
| C-REJ-04 | STA/Rechazo | D04 | Dxx | File Reject Partial | Permitido como rechazo parcial | Clasificado `FileRejection` | Evidencia rechazo parcial | Pendiente | No devolución |
| C-REJ-05 | STA/Rechazo | D05 | Dxx | File Reject Partial | Permitido como rechazo parcial | Clasificado `FileRejection` | Evidencia rechazo parcial | Pendiente | No devolución |
| C-REJ-06 | STA/Rechazo | D06 | Dxx | File Reject Partial | Permitido como rechazo parcial | Clasificado `FileRejection` | Evidencia rechazo parcial | Pendiente | No devolución |
| C-TEC-01 | Técnico | I500 | Ixxx | Operator Response | Permitido como técnico | Clasificado `TechnicalIntegration` | Logs + respuesta operador | Pendiente | No devolución |
| C-TEC-02 | Técnico | I503 | Ixxx | Operator Response | Permitido como técnico | Clasificado `TechnicalIntegration` | Logs + respuesta operador | Pendiente | No devolución |
| C-TEC-03 | Técnico | ITIMEOUT | Ixxx | Operator Response | Permitido como técnico | Clasificado `TechnicalIntegration` | Logs + respuesta operador | Pendiente | No devolución |
| C-TEC-04 | Técnico | ISOAP | Ixxx | Operator Response | Permitido como técnico | Clasificado `TechnicalIntegration` | Logs + respuesta operador | Pendiente | No devolución |
| C-TEC-05 | Técnico | IFUNC | Ixxx | Operator Response | Permitido como técnico | Clasificado `TechnicalIntegration` | Logs + respuesta operador | Pendiente | No devolución |
| C-INT-01 | Interno | DXX-LIQ | Interno | InternalOnly | Permitido solo interno | `InternalOnly` permitido, externo rechazado | Log policy + evidencia no exposición | Pendiente | Nunca externo |

## 5. Checklist por flujo

| ID | Flujo | Cámara | Códigos permitidos | Códigos rechazados | Validación técnica | Evidencia UAT | Responsable | Estado |
|---|---|---|---|---|---|---|---|---|
| F-01 | Devolución saliente ACH | ACH | R07,R10,R29,R31,DEV14 | Rxx CENIT, Dxx, Ixxx, DXX-LIQ | Policy rail-flow outbound | Archivo + R7 + API/policy | QA+Operaciones | Pendiente |
| F-02 | Devolución saliente CENIT | CENIT | R01,R02,R03,R04,R06,R08,R09,R12,R13,R14,R15,R16,R17,R20,R23 | Rxx/DEVxx ACH, Dxx, Ixxx, DXX-LIQ | Policy rail-flow outbound | Archivo + R7 + API/policy | QA+Operaciones | Pendiente |
| F-03 | Devolución entrante ACH | ACH | R07,R10,R29,R31,DEV14 | Rxx CENIT, Dxx, Ixxx, DXX-LIQ | Incoming rail-flow | Ingestión + eventos/issues | QA Backend | Pendiente |
| F-04 | Devolución entrante CENIT | CENIT | R01,R02,R03,R04,R06,R08,R09,R12,R13,R14,R15,R16,R17,R20,R23 | Rxx/DEVxx ACH, Dxx, Ixxx, DXX-LIQ | Incoming rail-flow | Ingestión + eventos/issues | QA Backend | Pendiente |
| F-05 | ROR ACH | ACH | Según policy ACH vigente | Dxx, Ixxx, DXX-LIQ y no permitidos por linaje | ROR rail-flow+linaje | Evaluate/audit/nacha | QA+Negocio | Pendiente |
| F-06 | ROR CENIT | CENIT | Según policy CENIT vigente | Dxx, Ixxx, DXX-LIQ y no permitidos por linaje | ROR rail-flow+linaje | Evaluate/audit/nacha | QA+Negocio | Pendiente |
| F-07 | Rechazo total archivo | STA/Rechazo | D01,D02,D03 (y D04-D06 si regla operativa aplica) | Rxx/DEVxx, Ixxx como causal devolución | Policy `FileRejectTotal` | Evidencia rechazo total | Operaciones | Pendiente |
| F-08 | Rechazo parcial registro | STA/Rechazo | D04,D05,D06 (y D01-D03 si regla operativa aplica) | Rxx/DEVxx, Ixxx como causal devolución | Policy `FileRejectPartial` | Evidencia rechazo parcial | Operaciones | Pendiente |
| F-09 | Operator response / técnicos | Técnico | I500,I503,ITIMEOUT,ISOAP,IFUNC | Rxx/DEVxx,Dxx,DXX-LIQ externos | Policy `OperatorResponse` | Logs/respuestas/issues | Operaciones | Pendiente |
| F-10 | Command center | Operación | Códigos permitidos por policy de ese flujo | Códigos no elegibles por rail-flow | Policy + trazabilidad | Screenshot/logs/auditoría | Operaciones | Pendiente |
| F-11 | InternalOnly | Interno | DXX-LIQ | Todo uso externo de DXX-LIQ | Policy `InternalOnly` | Logs/auditoría no exposición | Arquitectura+QA | Pendiente |

## 6. Checklist normativo

| ID | Cámara | Código | Documento fuente | Sección/página | Regla normativa | Flujo autorizado | Evidencia | Estado | Firma requerida |
|---|---|---|---|---|---|---|---|---|---|
| N-01 | CENIT | Rxx (tabla Anexo A) | CENIT Anexo A | Tabla de causales | Causales Rxx definidas por cámara | Pendiente cierre por flujo | Extracto + caso UAT | Pendiente | Operaciones + Compliance |
| N-02 | ACH | Rxx/DEVxx aplicables | ACH Colombia V32 | Sección/página exacta por causal en acta UAT | Causales aplicables por operador/manual | Pendiente ratificación por flujo | Extracto + caso UAT | Pendiente | Negocio + Compliance |
| N-03 | ACH/CENIT/STA | Rxx/DEVxx/Dxx/Ixxx/Interno | Matriz de causales current | Documento completo | Control documental vigente de separación por tipo y flujo | Según matriz | Referencia cruzada + checklist firmado | Pendiente | Arquitectura + QA |
| N-04 | ACH/CENIT/STA | Resultado UAT cámara | Acta UAT por cámara | N/A | Confirmación de comportamiento por flujo | Por flujo | Acta UAT + evidencias | Pendiente | Cámara/Operador |
| N-05 | ACH/CENIT/STA | Aprobación negocio | Acta comité | N/A | Aprobación funcional | Todos | Acta firmada | Pendiente | Negocio |
| N-06 | ACH/CENIT/STA | Aprobación operaciones | Acta comité | N/A | Aprobación operativa | Todos | Acta firmada | Pendiente | Operaciones |
| N-07 | ACH/CENIT/STA | Aprobación compliance/riesgo | Acta comité | N/A | Aprobación normativa/riesgo | Todos | Acta firmada | Pendiente | Compliance/Riesgo |

> Nota: marcar como **pendiente** todo lo que no tenga fuente formal exacta (sección/página o acta firmada).

## 7. Checklist de pruebas técnicas

| ID | Test / suite | Cobertura | Resultado esperado | Evidencia | Estado |
|---|---|---|---|---|---|
| T-01 | `CauseCodeCharacterizationTests` | Caracterización de códigos por tipo/flujo | Verde | Salida `dotnet test` | Ejecutado |
| T-02 | `AchCauseCodePolicyTests` | Policy rail-flow, allow/reject por causal/flujo | Verde | Salida `dotnet test` | Ejecutado |
| T-03 | `AchReturnsFileByClearingHouseTests` | Validación retornos por cámara/salida | Verde | Salida `dotnet test` | Ejecutado |
| T-04 | `AchIncomingReturnIngestionServiceTests` | Incoming return + rail-flow | Verde | Salida `dotnet test` | Ejecutado |
| T-05 | `AchReturnOfReturnEligibilityServiceTests` | ROR por causal/linaje/flujo | Verde | Salida `dotnet test` | Ejecutado |
| T-06 | Tests rechazo `Dxx/Ixxx` | Separación rechazo/técnico vs devolución | Verde | Salida `dotnet test` | Ejecutado |
| T-07 | Tests no exposición `DXX-LIQ` | Restricción `InternalOnly` | Verde | Salida `dotnet test` | Ejecutado |
| T-08 | Suite completa filtrada (1) | CauseCode/AchCauseCodePolicy/Rejection/IncomingReturn/IncomingNacha/RegulatoryCatalog | Verde | filtro ejecutado recientemente | Ejecutado |
| T-09 | Suite completa filtrada (2) | AchReturns/ReturnOfReturn/Nacha/ExternalFileName | Verde | filtro ejecutado recientemente | Ejecutado |

Filtros ejecutados recientemente:
- `CauseCode|AchCauseCodePolicy|Rejection|IncomingReturn|IncomingNacha|RegulatoryCatalog`
- `AchReturns|ReturnOfReturn|Nacha|ExternalFileName`

## 8. Checklist UAT funcional

| ID | Caso UAT | Cámara | Código | Flujo | Entrada | Resultado esperado | Evidencia | Estado |
|---|---|---|---|---|---|---|---|---|
| UAT-01 | ACH devolución saliente con R07 | ACH | R07 | Outbound | Tx elegible ACH | Archivo generado y causal válida | Archivo + R7 + API | Pendiente |
| UAT-02 | ACH devolución saliente con DEV14 | ACH | DEV14 | Outbound | Tx elegible ACH | Archivo generado y causal válida | Archivo + R7 + API | Pendiente |
| UAT-03 | ACH rechazo por R01 | ACH | R01 | Outbound | Intento usar causal CENIT | Rechazo rail mismatch | Error/failure policy | Pendiente |
| UAT-04 | CENIT devolución saliente con R01 | CENIT | R01 | Outbound | Tx elegible CENIT | Archivo generado y causal válida | Archivo + R7 + API | Pendiente |
| UAT-05 | CENIT devolución entrante con R02 | CENIT | R02 | Incoming | Archivo entrante R02 | Ingestión válida | Resultado ingestión/eventos | Pendiente |
| UAT-06 | CENIT rechazo por DEV14 | CENIT | DEV14 | Outbound | Intento usar causal ACH | Rechazo rail mismatch | Error/failure policy | Pendiente |
| UAT-07 | ROR CENIT R01→R02 permitido | CENIT | R01→R02 | ROR | Caso con linaje válido | Evaluate permite | Evaluate/audit/nacha | Pendiente |
| UAT-08 | ROR con D04 rechazado | ACH/CENIT | D04 | ROR | Intento causal Dxx | Rechazado por policy | Error/failure policy | Pendiente |
| UAT-09 | Incoming con I500 rechazado como return | ACH/CENIT | I500 | Incoming Return | Addenda con I500 como return reason | Failure/issue, no devolución válida | Resultado ingestión/issues | Pendiente |
| UAT-10 | DXX-LIQ rechazado como externo | ACH/CENIT/STA | DXX-LIQ | Externo | Intento uso externo | Rechazo policy | Error/failure policy | Pendiente |
| UAT-11 | D04 permitido como rechazo parcial | STA/Rechazo | D04 | FileRejectPartial | Archivo con error parcial | Rechazo parcial válido | Evidencia rechazo parcial | Pendiente |
| UAT-12 | I500 permitido como técnico/operator response | Técnico | I500 | OperatorResponse | Evento técnico integración | Registro técnico permitido | Logs + respuesta operador | Pendiente |
| UAT-13 | Warning NORMATIVE_PENDING no bloquea | ACH/CENIT | Rxx/DEVxx | Return | Caso válido con warning | Warning visible sin bloqueo por sí mismo | Logs + resultado policy | Pendiente |
| UAT-14 | Rail mismatch bloquea o genera failure según flujo | ACH/CENIT | Código no elegible | Return/Incoming/ROR | Causal cruzada por cámara/flujo | Bloqueo/failure según flujo | Error + evidencia procesamiento | Pendiente |
| UAT-15 | Command center conserva trazabilidad | Operación | Varios permitidos | CommandCenter | Operación manual controlada | Traza completa de decisión causal | Screenshot + logs/auditoría | Pendiente |

## 9. Evidencia requerida
- archivo NACHA de muestra;
- registro 7 con causal;
- respuesta de API;
- resultado de policy;
- logs de warning/error;
- auditoría de generación o ingesta;
- evidencia de rechazo total/parcial;
- screenshots command center si aplica;
- hash de archivo si aplica;
- acta UAT;
- firma cámara/operador;
- firma negocio;
- firma operaciones;
- firma riesgo/compliance;
- aprobación técnica.

## 10. Criterios de salida de NO-GO productivo
1. Cada causal tiene cámara definida.
2. Cada causal tiene flujo permitido.
3. Cada causal tiene tipo definido: Rxx/DEVxx/Dxx/Ixxx/interno.
4. Cada causal tiene fuente normativa.
5. Cada causal tiene prueba técnica.
6. Cada causal tiene evidencia UAT.
7. ACH y CENIT no comparten causal sin evidencia.
8. Dxx/Ixxx no se usan como devolución.
9. Rxx/DEVxx no se usan como rechazo técnico.
10. DXX-LIQ nunca sale a cámara.
11. Outbound valida rail-flow.
12. Incoming valida rail-flow.
13. ROR valida causal original/nueva/linaje.
14. Rechazo total/parcial queda separado.
15. Warnings normativos están documentados.
16. Rail mismatch genera bloqueo/failure según flujo.
17. Command center registra trazabilidad.
18. Cámara/operador valida archivos.
19. UAT aprobado.
20. Firma negocio.
21. Firma operaciones.
22. Firma compliance/riesgo.
23. Aprobación técnica.
24. Actualización de scorecard Go/No-Go.

## 11. Acta mínima sugerida
- Fecha.
- Cámara.
- Ambiente.
- Flujo.
- Código causal.
- Archivo / transacción.
- Resultado esperado.
- Resultado observado.
- Evidencia adjunta.
- Hallazgos.
- Decisión.
- Acciones pendientes.
- Firmas.

## 12. Riesgos residuales
- fuentes normativas incompletas;
- CENIT Anexo A limitado a causales, no necesariamente todos los flujos;
- ACH pendiente ratificación operativa;
- riesgo de confundir rechazo Dxx con causal Rxx;
- riesgo de usar Ixxx como devolución;
- riesgo de códigos internos saliendo a cámara;
- riesgo de UI permitiendo causal no elegible;
- riesgo de command center sin trazabilidad suficiente;
- riesgo de tratar warnings como aprobación productiva;
- riesgo de hard-block sin UAT.

## 13. Decisión vigente
- **GO técnico:** sí.
- **GO UAT controlado:** sí.
- **NO-GO productivo:** sí.
- **Próximo hito:** UAT con evidencia por causal/cámara/flujo y firmas.

- Referencia obligatoria para devolución saliente (estado/evento/idempotencia y trazabilidad): `docs/audits/outbound-return-state-traceability-matrix-current.md`.


## Referencia cruzada incoming/orphan

Para validación de causales entrantes en contexto E2E + huérfanas/no resueltas, usar también:

- `docs/audits/incoming-return-e2e-orphan-matrix-current.md`

## Referencia cruzada total vs partial

Para la frontera semántica canónica entre `RejectedTotal`, `RejectedPartial`, `Accepted`, orphan/unresolved, manual audit-only y la distinción formal frente a devolución parcial por monto, ver:

- `docs/audits/total-vs-partial-rejection-matrix-current.md`

## Referencia cruzada checklist UAT total vs partial

Para la validación UAT paso-a-paso de `Accepted`, `RejectedTotal`, `RejectedPartial`, orphan/unresolved, manual audit-only, separación de códigos y relación con ROR/contabilidad, ver:

- `docs/uat/rejection-total-partial-acceptance-checklist.md`

