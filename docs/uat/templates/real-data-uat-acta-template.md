# Plantilla — Acta UAT con datos reales o anonimizados (S1)

> Uso: completar en ejecución real por responsables humanos. Esta plantilla no constituye aprobación automática.

## 1. Encabezado del acta
- ID de acta:
- Fecha de ejecución:
- Fecha de cierre:
- Dominio S1:
- Cámara (ACH/CENIT/ambas):
- Ambiente:
- Responsable de ejecución:
- Responsable de consolidación:

## 2. Casos ejecutados

| ID caso | Descripción | Resultado (Aprobado / Aprobado con observaciones / Rechazado) | Evidencia asociada | Observaciones |
|---|---|---|---|---|
| UAT-REAL-S1-10-001 | Neteo CENIT por ciclo |  |  |  |
| UAT-REAL-S1-10-002 | Reproceso idempotente de neteo |  |  |  |
| UAT-REAL-S1-11-001 | Evidencia CUD registrada |  |  |  |
| UAT-REAL-S1-11-002 | Liquidez simulada vs evidencia CUD real |  |  |  |
| UAT-REAL-S1-12-001 | Naming ACH Colombia |  |  |  |
| UAT-REAL-S1-12-002 | Naming CENIT |  |  |  |
| UAT-REAL-S1-13-001 | Sobre firmado/cifrado saliente validado |  |  |  |
| UAT-REAL-S1-13-002 | Sobre externo recibido validado |  |  |  |
| UAT-REAL-S1-20-001 | Runbook ejecutado |  |  |  |
| UAT-REAL-S1-20-002 | Acta UAT firmada |  |  |  |

## 3. Defectos y riesgos

| ID defecto | Severidad (P0/P1/P2/P3) | Descripción | Responsable | Fecha objetivo | Estado | Evidencia |
|---|---|---|---|---|---|---|
|  |  |  |  |  |  |  |

Reglas:
- P0 bloquea GO UAT formal y GO productivo.
- P1 requiere workaround aprobado.
- P2/P3 pueden quedar pendientes aceptados con riesgo explícito.

## 4. Decisión del acta
- Decisión final UAT: (Aprobado / Aprobado con observaciones / Rechazado)
- GO UAT formal: (Sí / No / Pendiente)
- GO productivo: NO (hasta scorecard actualizado y aprobación de comité)
- NO-GO productivo: Vigente hasta cierre formal de bloqueantes.

## 5. Aprobadores mínimos y firmas/trazabilidad

| Rol | Nombre | Área | Decisión | Fecha | Firma o trazabilidad de aprobación |
|---|---|---|---|---|---|
| QA UAT |  |  |  |  |  |
| Operaciones |  |  |  |  |  |
| Tesorería (S1-10/S1-11) |  |  |  |  |  |
| Seguridad (S1-13) |  |  |  |  |  |
| Compliance |  |  |  |  |  |
| Riesgo Operacional |  |  |  |  |  |
| Dueño de Proceso |  |  |  |  |  |

## 6. Declaraciones de control
- Este documento no autoriza producción por sí solo.
- No se registran datos sensibles reales en el repositorio Git.
- La evidencia sensible se conserva en repositorio documental seguro o gestor aprobado.
