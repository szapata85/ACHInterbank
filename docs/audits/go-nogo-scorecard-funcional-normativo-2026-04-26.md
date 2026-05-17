# Go/No-Go Scorecard — Auditoría funcional-normativa ACH/CENIT/NACHA-M

> Referencia UAT complementaria: `docs/uat/incoming-return-orphan-acceptance-checklist.md`.


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
| Naming externo ACH/CENIT/STA | Parcialmente listo | **Crítica** | matriz vigente `docs/audits/external-filename-normative-matrix-ach-cenit-sta-current.md`; cobertura técnica parcial y hardcodes RET/RORNACHA pendientes | **NO-GO productivo** |
| Sobre digital NACHA-M | Parcialmente listo | Alta | fail-close y harness técnico; falta cierre externo | **NO-GO productivo sin validación externa** |
| UAT NACHA Security | Parcialmente listo | Alta | plan UAT con ítems pendientes | **GO solo para UAT ampliado** |
| Return-of-return (ROR) | Cerrado técnico / UAT GO controlado | Alta | Endpoints evaluate + generate-audit-file + generate-nacha-file; UI `/transactions/returns-ror`; gating 409 en UI | **NO-GO productivo hasta cierre normativo/UAT/firma** |
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

## 12) Estado actualizado ROR

- **Endpoints implementados**
  - `POST /ach-returns/return-of-return/evaluate`
  - `POST /ach-returns/return-of-return/generate-audit-file`
  - `POST /ach-returns/return-of-return/generate-nacha-file`
- **Archivos generados**
  - Audit-mode interno pipe: `ROR|...` y `FLOW|...`.
  - NACHA-M productivo independiente: registros `1,5,6,7,8,9`.
- **UI disponible**
  - Ruta Angular: `/transactions/returns-ror`.
- **Pruebas ejecutadas (estado reportado de cierre técnico)**
  - `dotnet restore` OK, `dotnet build -c Release` OK, `dotnet test` filtrado OK (291 passed), `npm run build` OK.
  - `npm test` pendiente por dependencia de `ChromeHeadless` (`libatk-1.0.so.0`).
- **Pendientes UAT**
  - validación E2E con datos representativos/reales en entorno controlado.
  - acta UAT de operación ROR sin pendientes críticos.
- **Pendientes normativos**
  - CENIT: validación explícita de causales/reglas aplicables a devolución de devolución según documentación vigente.
  - ACH Colombia: confirmación de aplicabilidad del flujo según manual vigente y reglas de operador/devolución correspondientes.
- **Riesgos residuales**
  - rechazo externo por formato/naming sin cierre formal con cámara/operador.
  - desalineación de readiness si se interpreta GO técnico como GO productivo.

## 13) No regresión

- No se modificó devolución normal.
- No se modificó incoming NACHA.
- No se modificó parser.
- No se crearon migraciones.
- No se tocaron reglas regulatorias base.

## 14) Pendientes para GO productivo

1. Validación con datos reales.
2. Validación por cámara.
3. Aceptación de formato NACHA-M.
4. Confirmación de naming externo.
5. Trazabilidad requisito→norma→código→prueba→evidencia.
6. Acta UAT.
7. Firma negocio/compliance/operaciones.


## 15) Referencia vigente para diagnóstico de naming externo

La fuente documental vigente para naming externo ACH/CENIT/STA es `docs/audits/external-filename-normative-matrix-ach-cenit-sta-current.md`.

Las matrices `...-2026-04-20.md` y `...-2026-04-20-v2.md` se conservan como histórico de auditoría.

La actualización documental no cambia la decisión actual: **NO-GO productivo** para naming externo hasta cierre normativo, remediación técnica y firmas de negocio/compliance/operaciones.

- Soporte UAT documental de naming: `docs/uat/naming-returns-ror-acceptance-checklist.md` (checklist funcional/técnico/normativo; no implica GO productivo).

- Evidencia documental NACHA-M campo-a-campo por cámara: `docs/audits/nacha-record-level-normative-matrix-ach-cenit-current.md` (no cambia decisión NO-GO productivo).

- Soporte UAT registros NACHA-M: `docs/uat/nacha-records-acceptance-checklist.md` (GO técnico + GO UAT controlado + NO-GO productivo).

- Evidencia documental vigente de causales por cámara/flujo: `docs/audits/cause-code-normative-matrix-ach-cenit-sta-current.md` (referencia de control; no cambia decisión NO-GO productivo).

- Checklist UAT de causales por cámara/flujo: `docs/uat/cause-code-acceptance-checklist.md` (evidencia complementaria; no cambia decisión NO-GO productivo).

- Evidencia adicional de brechas P0 en devolución saliente: `docs/audits/outbound-return-state-traceability-matrix-current.md` (estado/evento/idempotencia multiinstancia).
- Checklist UAT para estado/evento/trazabilidad de devolución saliente: `docs/uat/outbound-return-state-traceability-acceptance-checklist.md` (evidencia requerida para salida de NO-GO productivo).


## Evidencia complementaria de brechas P0 incoming

Se incorpora como evidencia documental vigente para brechas P0 de devolución entrante:

- `docs/audits/incoming-return-e2e-orphan-matrix-current.md`

La decisión se mantiene sin cambios: **NO-GO productivo**.

## Referencia cruzada total vs partial

Para la frontera semántica canónica entre `RejectedTotal`, `RejectedPartial`, `Accepted`, orphan/unresolved, manual audit-only y la distinción formal frente a devolución parcial por monto, ver:

- `docs/audits/total-vs-partial-rejection-matrix-current.md`

## Referencia cruzada checklist UAT total vs partial

Para la validación UAT paso-a-paso de `Accepted`, `RejectedTotal`, `RejectedPartial`, orphan/unresolved, manual audit-only, separación de códigos y relación con ROR/contabilidad, ver:

- `docs/uat/rejection-total-partial-acceptance-checklist.md`

- Referencia complementaria ciclos/neteo/liquidez/evidencia CUD: `docs/audits/cenit-cycles-netting-liquidity-cud-matrix-current.md` (no cambia decisión NO-GO productivo).

- Referencia UAT ciclos/liquidez/evidencia CUD: `docs/uat/cenit-cycles-liquidity-cud-acceptance-checklist.md` (no cambia decisión NO-GO productivo).

- Referencia matriz vigente de sobre/firma/certificados: `docs/audits/digital-envelope-signature-certificate-matrix-current.md` (no modifica decisión NO-GO productivo).

- Referencia checklist UAT de sobre/firma/certificados: `docs/uat/digital-envelope-certificate-acceptance-checklist.md` (no modifica decisión NO-GO productivo).

- Referencia runbook operativo de certificados: `docs/ops/certificate-operations-runbook.md` (no modifica decisión NO-GO productivo).

- Decisión arquitectura certificados: OpenBao/SecretRef excluido del flujo de certificados; BD solo metadata; llaves privadas fuera de BD.

> Referencia punto 10 (reportería/conciliación revisión contable terceros, no contable): `docs/audits/accounting-review-reconciliation-matrix-current.md`.
