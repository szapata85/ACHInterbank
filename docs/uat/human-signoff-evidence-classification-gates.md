# Compuertas de clasificación de evidencia y aprobación humana — S1

## 1. Propósito

Este documento define compuertas mínimas para diferenciar: prueba automatizada, evidencia técnica, evidencia UAT asistida por IA, evidencia UAT humana, evidencia externa/oficial, acta de aprobación y evidencia operacional productiva.

- No habilita producción.
- No reemplaza matrices S1 ni checklists UAT.
- Define cuándo una evidencia soporta GO técnico, GO UAT asistido, GO UAT formal y GO productivo.
- Mantiene NO-GO productivo mientras existan P0.

## 2. Principios de control

- Una prueba automatizada no equivale a aprobación humana.
- Una evidencia UAT asistida por IA no equivale a firma de Operaciones, Negocio, Riesgo, Compliance ni Tecnología.
- Una exportación o reporte no equivale a contabilización.
- Un estado técnico no equivale a validación normativa.
- Un archivo generado no equivale a liquidación firme.
- Una validación local no equivale a interoperabilidad oficial externa.
- Para ACH/CENIT, la evidencia debe diferenciar cámara cuando aplique.
- Si falta evidencia humana requerida, máximo GO UAT parcial/controlado.
- Si falta evidencia externa/oficial requerida, no se permite GO productivo.
- Si existe P0 abierto, se mantiene NO-GO productivo.

## 3. Clasificación de evidencia

| Tipo de evidencia | Descripción | Ejemplo | Permite GO técnico | Permite GO UAT asistido | Permite GO UAT formal | Permite GO productivo | Requiere aprobación humana | Observación |
|---|---|---|---|---|---|---|---|---|
| Prueba automatizada | Verificación ejecutable repetible | Unit/integration tests | Sí | Sí (soporte) | No | No | No | No reemplaza UAT humana |
| Evidencia técnica reproducible | Artefacto técnico verificable | Logs, trazas, reportes | Sí | Sí | No | No | Parcial | No reemplaza acta |
| Evidencia UAT asistida por IA | Ejecución pre-UAT/asistida | `docs/uat/accounting-review-ai-assisted-uat-execution-report.md` | Parcial | Sí | No | No | Sí | No equivale a firma humana |
| Evidencia UAT humana | Ejecución por responsable humano | Checklist UAT firmado | Sí | Sí | Sí | Parcial | Sí | Requiere trazabilidad |
| Evidencia externa/oficial | Validación por tercero/cámara | Confirmación interoperabilidad | Parcial | Parcial | Sí (si aplica) | Sí (si aplica) | Sí | Requerida en dominios externos |
| Acta/aprobación formal | Decisión formal registrada | Acta de comité | No | Parcial | Sí | Sí | Sí | Obligatoria para cierre formal |
| Evidencia operacional productiva | Prueba homologada en operación | Corrida operativa controlada | No | No | Parcial | Sí | Sí | Requerida cuando aplique |
| Evidencia de excepción aceptada | Riesgo residual aceptado | Waiver firmado | Parcial | Sí | Sí | Parcial | Sí | No sustituye cierre definitivo |
| Evidencia documental normativa | Trazabilidad a norma fuente | Matrices ACH/CENIT | Sí | Sí | Sí | Parcial | Sí | Diferenciada por cámara |
| Evidencia de seguridad/interoperabilidad | Validación de seguridad/intercambio | Certificados, sobre digital | Parcial | Parcial | Sí | Sí | Sí | Crítica S1-13/S1-14 |

## 4. Compuertas GO/NO-GO

| Compuerta | Evidencia mínima requerida | Aprobadores mínimos | Defectos permitidos | Estado resultante |
|---|---|---|---|---|
| GO técnico | build verde, tests automatizados, matriz trazable, sin P0 técnico | Tecnología / Arquitectura / QA técnico | P1/P2/P3 controlados | GO técnico controlado |
| GO UAT asistido | evidencia técnica + UAT asistida IA + checklist + datos controlados | Tecnología / QA | P1/P2 con registro | GO UAT asistido (no reemplaza UAT humana) |
| GO UAT formal | ejecución humana + acta UAT + evidencia + aprobaciones + 0 P0 + 0 P1 sin workaround | Operaciones + Negocio + Riesgo/Compliance (si aplica) + Tecnología | P2/P3 o P1 con workaround aprobado | GO UAT formal |
| GO productivo | GO UAT formal + evidencia externa/oficial + cierre P0 + runbook + rollback + seguridad + scorecard + comité | Comité/autoridad interna + dueños dominio | Sin P0/P1 críticos | GO productivo |
| NO-GO | P0 abierto o falta evidencia/aprobación humana/externa o cierre CUD/neteo/liquidez | N/A | N/A | NO-GO vigente |

## 5. Aprobadores mínimos por tipo de dominio

| Dominio S1 | Tipo | Aprobadores mínimos | Evidencia humana requerida | Evidencia externa requerida | Estado actual |
|---|---|---|---|---|---|
| S1-01 Parser inbound | ACH Colombia | Tecnología + QA + Operaciones | Sí | Según flujo | Parcial |
| S1-02 Builder/generación | ACH Colombia | Tecnología + QA + Operaciones | Sí | Según flujo | Parcial |
| S1-06 Devoluciones | Ambas | Operaciones + Negocio + Compliance + Tecnología | Sí | Según cámara | Parcial |
| S1-07 ROR | Ambas | Operaciones + Negocio + Compliance + Tecnología | Sí | Según cámara | Parcial fuerte / NO-GO productivo |
| S1-10 Neteo CENIT | CENIT | Operaciones + Negocio + Riesgo/Compliance + Tecnología | Sí | Sí | Bloqueado / NO-GO |
| S1-11 Liquidez/CUD | CENIT | Operaciones + Negocio + Riesgo/Compliance + Tecnología | Sí | Sí | Bloqueado / NO-GO |
| S1-12 Naming externo | Ambas | Operaciones + Negocio + Compliance + Tecnología | Sí | Sí | Bloqueado / NO-GO |
| S1-13 Sobre/firma/cifrado | Ambas | Seguridad + Operaciones + Compliance + Tecnología | Sí | Sí | Bloqueado / NO-GO |
| S1-14 Certificados | Transversal | Seguridad + Operaciones + Tecnología | Sí | Según integración | Parcial |
| S1-15 Reportes/auditoría | Transversal | Operaciones + Negocio + Tecnología | Sí | No (salvo tercero) | Parcial |
| S1-20 UAT/runbooks/evidencia | Transversal | Operaciones + Negocio + Riesgo/Compliance + Tecnología | Sí | Sí cuando aplique | Bloqueado / NO-GO |

## 6. Reglas especiales por cámara

### ACH Colombia
- Fuente ACH Colombia V3.2 trazada.
- Caso UAT por flujo ACH.
- Evidencia de archivo/naming/causal.
- Aprobación Negocio/Operaciones.
- Evidencia externa/interoperabilidad cuando aplique.

### CENIT
- Fuente CENIT DSP/Anexos trazada.
- Caso UAT por flujo CENIT.
- Evidencia de ciclos/cutoff y causales CENIT.
- Evidencia de neteo/liquidez.
- Evidencia CUD como boundary operacional.
- Aprobación Operaciones/Negocio/Riesgo/Compliance.

Aclaraciones:
- CUD sin API runtime no puede declararse cerrado productivamente.
- Liquidez simulada no equivale a saldo real CUD.
- DXX-LIQ no equivale a rechazo oficial CUD.

## 7. Criterios para aceptar evidencia UAT asistida por IA

Aceptable para pre-UAT, validación de formato/exportación, no-contabilidad, detección de inconsistencias y paquete preliminar de evidencias.

No aceptable para firmar UAT, reemplazar Operaciones, aprobar GO productivo, declarar cumplimiento normativo completo o validar interoperabilidad externa oficial.

Marcación obligatoria: **“Evidencia técnica automatizada; pendiente aprobación humana”.**

## 8. Criterios para acta UAT humana

Campos mínimos: ID S1, Cámara, Requisito, Fuente normativa, Caso ejecutado, Resultado esperado/obtenido, Evidencia adjunta, Defectos, Workaround, Decisión, aprobadores (Operaciones/Negocio/Riesgo-Compliance/Tecnología), Fecha, Observaciones, Firma o trazabilidad de aprobación.

## 9. Defectos y severidad

| Severidad | Descripción | Permite GO técnico | Permite GO UAT | Permite GO productivo |
|---|---|---|---|---|
| P0 | Falla crítica normativa/operativa/integridad | No | No (si afecta dominio probado) | No |
| P1 | Falla alta con impacto controlable | Sí, con mitigación | Sí, con workaround aprobado | No sin aceptación formal |
| P2 | Falla media | Sí | Sí | Parcial, sujeto a aprobación formal |
| P3 | Mejora/documentación | Sí | Sí | Sí, sin bloquear salida |

## 10. Compuertas específicas para bloqueantes actuales

| Bloqueante | Dominio | Evidencia mínima para desbloquear | Aprobación requerida | Estado actual |
|---|---|---|---|---|
| S1-10 Neteo CENIT E2E | S1-10 | Evidencia E2E CENIT trazable por ciclo/posición + cierre documental | Operaciones + Negocio + Compliance + Tecnología | NO-GO / bloqueado |
| S1-11 Liquidez/CUD | S1-11 | Evidencia de liquidez + evidencia CUD operacional + conciliación | Operaciones + Riesgo/Compliance + Tecnología | NO-GO / bloqueado |
| S1-12 Naming externo ACH/CENIT/STA | S1-12 | Validación naming por cámara/flujo sin hardcodes críticos | Operaciones + Compliance + Tecnología | NO-GO / bloqueado |
| S1-13 Sobre digital interoperabilidad externa | S1-13 | Evidencia de interoperabilidad externa + firma/cifrado | Seguridad + Operaciones + Compliance + Tecnología | NO-GO / bloqueado |
| S1-20 UAT/runbooks/evidencia firmada | S1-20 | Checklists completos + actas firmadas + runbooks aprobados | Operaciones + Negocio + Riesgo/Compliance + Tecnología | NO-GO / bloqueado |

## 11. Relación con matriz consolidada punto 11

Referencia: `docs/audits/s1-requirement-norm-code-test-evidence-closure-matrix-current.md`.

Esta guía complementa la matriz, no cambia estados S1, define clasificación de evidencia y aprobaciones, y no declara cerrado ningún dominio.

## 12. Veredicto

- GO técnico evidencia: **Sí, controlado**.
- GO UAT asistido: **Sí**, cuando exista harness o evidencia IA.
- GO UAT formal: **pendiente aprobación humana**.
- GO productivo: **NO**.
- NO-GO productivo vigente.
