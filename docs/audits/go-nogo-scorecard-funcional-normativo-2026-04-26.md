# Go/No-Go Scorecard — Auditoría funcional-normativa ACH/CENIT/NACHA-M

**Fecha:** 2026-04-26 (UTC)  
**Ámbito:** Formalización de readiness funcional-normativo para salida productiva.  
**Restricciones aplicadas:** sin cambios de código/servicios/tests/Angular/cripto/migraciones.

---

## 1) Estado base validado

### Cierre técnico P0 (backend)
- Build release en verde.
- Suite backend en verde (394/394).
- Revalidación de 18 fallos históricos P0 en verde (18/18).

Evidencia:
- `docs/audits/evidence/dotnet-build-release-2026-04-26-p0-close.txt`
- `docs/audits/evidence/dotnet-test-release-2026-04-26-p0-close.txt`
- `docs/audits/evidence/p0-18-tests-revalidation-after-cr4-2026-04-26.txt`
- `docs/operations/p0-cr4-cierre-validacion-2026-04-26.md`

---

## 2) Scorecard Go/No-Go funcional-normativo

> Escala de severidad: **Crítica / Alta / Media / Baja**.

| Dominio | Estado | Severidad residual | Evidencia principal | Decisión |
|---|---|---|---|---|
| Estabilidad técnica backend P0 | Listo | Baja | Evidencias build/tests P0 cierre | **GO técnico** |
| Catálogo regulatorio devoluciones (Rxx/DEV14) | Listo (técnico) | Media | Seeder + tests regulatorios/reportes | **GO con control documental** |
| Reportería (returns/reconciliation/auditoría) | Listo (técnico) / Parcial (normativo) | Media | Tests de servicios + endpoints + evidencia CR-4 | **GO para UAT ampliado** |
| Ciclos CENIT + gobernanza operativa | Parcialmente listo | Alta | pruebas unitarias de política/ciclos | **NO-GO productivo sin E2E operativo** |
| Neteo/liquidez CENIT | Parcialmente listo | **Crítica** | cobertura unitaria + shadow compare pasivo | **NO-GO productivo** |
| Naming externo ACH/CENIT/STA | Parcialmente listo | **Crítica** | matriz normativa con reglas pendientes de confirmación | **NO-GO productivo** |
| Sobre digital NACHA-M | Parcialmente listo | Alta | fail-close y harness técnico; falta cierre externo | **NO-GO productivo sin validación externa** |
| UAT NACHA Security | Parcialmente listo | Alta | plan UAT con ítems pendientes | **GO solo para UAT ampliado** |
| Trazabilidad normativa maestra (requisito→norma→código→prueba→evidencia) | No listo | **Crítica** | brecha explícita en auditorías actuales | **NO-GO productivo** |
| Consistencia documental de readiness | No listo | Alta | drift entre docs históricos y cierre P0 | **NO-GO de certificación formal** |

---

## 3) Qué está listo

1. **Cierre técnico P0 backend**: build release + 394/394 + 18/18 de matriz P0.
2. **Cobertura técnica de reglas clave** en devoluciones, reportes, prenotificación, return-of-return y seguridad fail-close.
3. **Arquitectura de evidencias** en `docs/audits/evidence` y runbooks de operaciones por fases.

## 4) Qué está parcialmente listo

1. **Reportes**: técnicamente validados, pero sin matriz formal “report-to-regulation” firmable.
2. **Sobre digital**: controles técnicos implementados; falta vector/evidencia externa oficial de interoperabilidad cerrada.
3. **CENIT netting/liquidez**: lógica operativa implementada y probada unitariamente, sin evidencia E2E operativa real/CUD.
4. **UAT NACHA Security**: plan y estructura disponibles, con pendientes por ejecutar/cerrar.

## 5) Qué bloquea producción (NO-GO)

1. Falta de **matriz maestra auditable** requisito normativo → implementación → prueba → evidencia.
2. **Naming externo ACH/CENIT/STA** con reglas aún en estado “requiere confirmación normativa”.
3. **Neteo/liquidez CENIT sin validación E2E operacional real** (incluyendo condiciones de liquidación/CUD según aplique).
4. **Sobre digital sin cierre de validación externa/certificación interoperable oficial**.
5. **Drift documental**: coexistencia de artefactos que reportan “no listo / 18 fallos” con otros de cierre P0.

---

## 6) Qué sí permite UAT ampliado

Se autoriza **UAT ampliado controlado** (no go-live) para:
- regresión funcional de reportes,
- operación inbound/outbound NACHA-M en datos sintéticos,
- validaciones operativas de command center y trazabilidad,
- validación de proceso documental de seguridad (sin afirmar certificación externa final).

Condiciones UAT ampliado:
- sin cambiar reglas críticas en caliente,
- trazabilidad obligatoria de evidencia por caso,
- cierre de pendientes en checklist UAT antes de elevar a comité Go/No-Go.

---

## 7) Qué requiere validación externa

1. **Interoperabilidad oficial de sobre digital** con vector/certificados reconocidos por contraparte regulatoria/operativa.
2. **Confirmación normativa de naming externo** ACH/CENIT/STA (estructura, secuencia, correlación R1/conteos, duplicidad).
3. **Validación E2E operacional de neteo/liquidez** en entorno y procedimiento homologado de operación real.

---

## 8) Qué requiere firma de negocio/compliance/operaciones

### Firmas mínimas obligatorias antes de GO productivo
1. **Dueño de producto/negocio ACH**: aceptación funcional por dominio (returns, reportes, ciclos, liquidación).
2. **Compliance/Normativa**: conformidad documental regulatoria y cierre de brechas normativas pendientes.
3. **Operaciones**: aceptación de runbooks, contingencias, escalamiento y evidencia de simulacros.
4. **Seguridad**: aceptación formal de sobre digital e integración de certificados con evidencia externa.
5. **Arquitectura/QA**: cierre de trazabilidad y consistencia de scorecard.

---

## 9) Criterios de aceptación para pasar de NO-GO a GO

### Criterios críticos (todos obligatorios)
- [ ] Matriz maestra requisito→norma→código→prueba→evidencia publicada y versionada.
- [ ] Cierre documental de naming externo con confirmación normativa explícita.
- [ ] Evidencia E2E netting/liquidez CENIT en circuito operativo homologado.
- [ ] Cierre de validación externa de sobre digital (interoperabilidad oficial).
- [ ] Checklist UAT NACHA Security sin pendientes P0/P1.
- [ ] Limpieza de drift documental (estado único oficial de readiness).
- [ ] Acta de comité Go/No-Go firmada por negocio/compliance/ops/seguridad/arquitectura.

---

## 10) Prompts sugeridos de cierre (secuenciales)

### Prompt S1 — Matriz maestra trazable
Objetivo: consolidar matriz requisito normativo ↔ implementación ↔ pruebas ↔ evidencia, con IDs únicos y ownership.

### Prompt S2 — Cierre normativo naming externo
Objetivo: resolver puntos “requiere confirmación normativa” para ACH/CENIT/STA y congelar reglas firmables.

### Prompt S3 — Validación externa sobre digital
Objetivo: incorporar evidencia oficial de interoperabilidad/certificación y actualizar scorecard de seguridad.

### Prompt S4 — E2E operativo netting/liquidez/CUD
Objetivo: ejecutar y documentar corrida operacional end-to-end con criterios de aceptación cuantificables.

### Prompt S5 — Cierre UAT NACHA Security
Objetivo: cerrar pendientes de plan UAT y producir acta de cumplimiento.

### Prompt S6 — Cierre documental y acta Go/No-Go
Objetivo: unificar estado de readiness, retirar contradicciones y emitir acta final firmable.

---

## 11) Veredicto actual

**Veredicto funcional-normativo actual: `NO-GO PRODUCTIVO`**.  
**Veredicto técnico backend P0: `GO TÉCNICO`**.  
**Veredicto operativo recomendado: `GO PARA UAT AMPLIADO CONTROLADO`** (sin salida productiva).
