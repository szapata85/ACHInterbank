> Nota G3.5.2: las referencias a proveedores de secretos retirados son historicas y obsoletas desde el cleanup `ebf7a8a5`; no describen el stack vigente.

# S5 — Cierre UAT NACHA Security, runbooks y acta de conformidad

**Fecha:** 2026-04-26 (UTC)
**Brecha objetivo:** S1-20 (UAT / runbooks / evidencia).
**Carácter del documento:** auditoría documental-operativa (sin cambios funcionales).

---

## 1) Objetivo y criterio de clasificación

Auditar estado real de UAT NACHA Security y formalizar clasificación S1-20 con base en:

1. existencia de plan UAT ejecutable;
2. estado real de escenarios P0/P1;
3. existencia de runbooks/checklists operativos;
4. evidencia ejecutada trazable;
5. existencia de acta formal de conformidad y firmas obligatorias.

---

## 2) Inspección documental ejecutada

Comandos ejecutados:

```bash
git status --short
git log --oneline -20
find docs -type f | sort | rg -i "uat|runbook|operacion|operación|nacha-security|security|sobre|digital|evidence|evidencia|go-nogo|readiness|acta|conformidad|checklist"
rg -n "UAT|runbook|operación|operacion|acta|conformidad|pendiente|P0|P1|escenario|checklist|NACHA Security|sobre digital|evidencia|firmar|firma|aprobación|aprobacion|GO|NO-GO" docs -S
```

Resultado de auditoría documental: sí existe paquete robusto de planes/checklists/evidencia, pero no existe cierre formal de matriz UAT NACHA Security ni acta firmada de conformidad de operación.

---

## 3) Respuesta obligatoria (1 a 9)

### 3.1 Qué planes UAT existen

1. **Plan UAT NACHA Security (37 escenarios)**: `docs/uat/nacha-security-uat-plan-2026-04-22.md`.
2. **Plan UAT operativo controlado inbound NACHA-M (Prompt 8)**: `docs/operations/incoming-nacha-uat-controlled-plan-2026-04-25.md`.
3. **Checklist de despliegue NACHA Security** (incluye comandos técnicos y criterios): `docs/deployment/nacha-security-deployment-checklist-2026-04-22.md`.

### 3.2 Qué escenarios están cerrados

- En la **corrida operativa controlada inbound** están reportados UAT-01..UAT-15 como aprobados técnicamente y UAT-16 bloqueado por entorno browser.
- En el **plan NACHA Security de 37 escenarios**, todos permanecen en estado **Pendiente** al corte documental (sin ejecución registrada por escenario con fecha/ejecutor/evidenceId/aprobación).

### 3.3 Qué escenarios siguen pendientes

- Siguen pendientes los **37/37 escenarios** del plan `nacha-security-uat-plan-2026-04-22.md`.
- Pendientes críticos incluyen múltiples **P0** (autorización de descarga, no exposición de plano ante firma inválida, validación de permisos denegados, logs sanitizados, expiración de autorizaciones, etc.).

### 3.4 Qué runbooks existen

Runbooks y guías operativas relevantes identificadas:

1. `docs/operations/incoming-nacha-observability-runbook-2026-04-24.md`.
2. `docs/operations/incoming-nacha-uat-controlled-plan-2026-04-25.md`.
3. `docs/deployment/nacha-security-deployment-checklist-2026-04-22.md`.
4. `docs/dev/docker-compose-proveedor de secretos retirado-uat-2026-04-22.md` (UAT/laboratorio).

### 3.5 Qué evidencias existen

Evidencias disponibles:

- `docs/operations/incoming-nacha-uat-controlled-evidence-2026-04-25.md` (GO condicionado de UAT controlado inbound, con bloqueo entorno para pruebas browser).
- evidencias técnicas P0 en `docs/audits/evidence/*` (build/tests backend).
- evidencias de scorecard/matriz S1 que sostienen estado NO-GO productivo actual.

Evidencia faltante clave:

- set de evidencia por escenario del plan NACHA Security (37 casos) con campos completos (`EvidenceId`, `operationId`, aprobación, defect tracking y cierre por prioridad).

### 3.6 Qué actas faltan

Faltan, como mínimo:

1. **Acta de ejecución UAT NACHA Security** (resultado por escenario, defectos, riesgos residuales).
2. **Acta de conformidad operativa** de runbooks/checklists (incluye rollback drill y monitoreo inicial).
3. **Acta de decisión de comité Go/No-Go** para salida productiva.

### 3.7 Qué firmas faltan

Firmas mínimas faltantes para cierre formal S1-20:

- Dueño de producto/negocio ACH.
- Operaciones.
- Seguridad operacional.
- Compliance/Normativa.
- Arquitectura/QA.

### 3.8 Si permite UAT ampliado

**Sí, permite UAT ampliado controlado**, condicionado a:

1. no declarar cierre de UAT NACHA Security sin ejecutar/registrar P0/P1;
2. registrar evidencia por escenario con trazabilidad completa;
3. cerrar bloqueo de entorno browser donde aplique (CHROME_BIN/Karma/rimraf) o gestionarlo formalmente como restricción de entorno.

### 3.9 Si permite GO productivo

**No permite GO productivo** al corte 2026-04-26, porque:

1. el plan UAT NACHA Security de 37 escenarios sigue en estado pendiente;
2. no hay acta formal de conformidad operativa firmada;
3. no existe evidencia de checklist UAT sin pendientes P0/P1;
4. scorecard vigente exige cierre de UAT y acta de comité para pasar a GO.

---

## 4) Clasificación formal S1-20 (corte 2026-04-26)

| Requisito | Estado | Justificación | Decisión |
|---|---|---|---|
| S1-20 UAT / runbooks / evidencia | **Parcial** | Hay plan UAT, runbooks y evidencia técnica/operativa parcial; persisten escenarios NACHA Security pendientes y ausencia de acta/firma de conformidad. | **NO-GO productivo / GO solo UAT ampliado** |

> Reclasificación propuesta respecto a “Bloqueado” puro: **Parcial**, por existencia verificable de estructura UAT y evidencia operativa parcial; no alcanza “Cumplido técnico / pendiente firmas” porque aún faltan ejecuciones y cierres P0/P1 de la matriz NACHA Security.

---

## 5) Plan mínimo de cierre para mover S1-20

### 5.1 Para pasar a “Cumplido técnico / pendiente firmas”

- Ejecutar y registrar 100% de escenarios P0 y P1 del plan NACHA Security.
- Cerrar defectos críticos/altos o aprobar workaround formal por comité.
- Completar evidencia por escenario (EvidenceId, ejecutor, fecha UTC, operationId, resultado, aprobación QA).

### 5.2 Para pasar a “Cumplido”

- Todo lo anterior, más:
  - acta UAT NACHA Security firmada;
  - acta de conformidad operativa de runbooks/checklists;
  - acta Go/No-Go firmada por negocio, operaciones, seguridad, compliance, arquitectura/QA.

---

## 6) Plantilla breve de acta de conformidad S1-20

```text
Acta: Cierre UAT NACHA Security y runbooks
Fecha (UTC):
Ambiente:
Alcance:

Resumen escenarios:
- P0 ejecutados/aprobados:
- P1 ejecutados/aprobados:
- Pendientes justificados:

Defectos abiertos:
- Críticos:
- Altos:
- Medios/Bajos:

Riesgo residual:
Decisión:
- [ ] GO productivo
- [ ] GO solo UAT ampliado
- [ ] NO-GO

Firmas:
- Negocio ACH:
- Operaciones:
- Seguridad:
- Compliance:
- Arquitectura/QA:
```

---

## 7) Veredicto ejecutivo S5

- **S1-20:** **Parcial** (no cerrado).
- **UAT ampliado:** **Sí permitido** (controlado).
- **GO productivo:** **No permitido** hasta cierre de pendientes P0/P1 + actas + firmas.
