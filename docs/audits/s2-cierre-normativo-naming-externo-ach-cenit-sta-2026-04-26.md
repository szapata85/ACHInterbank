# S2 — Cierre normativo de naming externo ACH/CENIT/STA

**Fecha:** 2026-04-26 (UTC)  
**Ámbito:** Cierre/Clasificación formal de la brecha S1-12 (naming externo).  
**Restricción aplicada:** sin cambios funcionales en servicios productivos ni pruebas.

---

## 1) Veredicto S2

Tras revisar matriz normativa v2, documentación de implementación y cobertura de pruebas:

- **S1-12 NO puede marcarse “Cumplido” pleno** aún.
- **S1-12 se reclasifica a:** **Cumplido técnico / pendiente validación externa**.

Razón:
- Existe implementación y tests para reglas nucleares confirmadas (ACH patrón/correlación/límite, CENIT-STA D04/D05/campo 6).
- Persisten reglas con fuente incompleta para hard-enforcement total (especialmente estructura STA completa fuera de rechazo y duplicidad ACH por archivo externo completo, más detalle PSE delegado a manual externo).

---

## 2) Reglas de naming implementadas (estado técnico)

| Regla | Implementación declarada | Test asociado | Estado técnico |
|---|---|---|---|
| ACH patrón `RRRRTTT.ZZZ.1` | `ExternalFileNamePolicy` + builder/validator | `ExternalFileNamePolicyPhase1Tests` | Implementada |
| ACH correlación `ZZZ ↔ R1 campo 7` | validator + correlación en política | `ExternalFileNamePolicyPhase1Tests` | Implementada |
| ACH límite diario (36) | validator en fase 1 | `ExternalFileNamePolicyPhase1Tests` | Implementada |
| ACH/PSE rango reservado | validator en fase 1 cuando aplica | `ExternalFileNamePolicyPhase1Tests` | Implementada |
| CENIT/STA rechazo D05 (conteo nombre vs contenido) | validator en fase 1 | `ExternalFileNamePolicyPhase1Tests` | Implementada |
| CENIT/STA rechazo D04 (duplicado) | duplicate guard + validator | `ExternalFileNamePolicyPhase1Tests` | Implementada |
| Enforcements STA fuera de rechazo | warning/audit-only | cobertura parcial por tests de warning | Parcial |
| Duplicidad ACH universal por nombre externo | no hard global | cobertura parcial | Parcial |

---

## 3) Reglas documentadas y fuente normativa

### 3.1 Reglas con fuente normativa fuerte (cerrables)

1. ACH: estructura `RRRRTTT.ZZZ.1`.  
2. ACH: correlación obligatoria `ZZZ ↔ campo 7 registro 1` (tabla identificador).  
3. ACH: máximo 36 archivos diarios por participante (incluyendo PSE en su nombre).  
4. ACH/PSE: reglas de rango reservado y correspondencia campo 7 cuando aplica.  
5. CENIT/STA rechazo: D04 (duplicado) y D05 (mismatch conteo nombre externo vs contenido).  
6. CENIT/STA rechazo: uso de campo 6 del nombre para conteo.

### 3.2 Reglas con fuente incompleta o pendiente de confirmación

1. Estructura completa de naming STA por todos los tipos (no sólo rechazo).  
2. Política de secuencia/reset STA por tipo.  
3. Política de duplicidad ACH por archivo externo completo (más allá de D31 de lote).  
4. Detalle PSE operativo completo (remisiones a manual externo no cerrado en repositorio).

---

## 4) Clasificación formal por regla (solicitada)

| Regla | Clasificación |
|---|---|
| ACH patrón `RRRRTTT.ZZZ.1` | **Cumplido técnico / pendiente validación externa** |
| ACH correlación `ZZZ ↔ R1` | **Cumplido técnico / pendiente validación externa** |
| ACH límite 36 diarios | **Cumplido técnico / pendiente validación externa** |
| ACH/PSE rango reservado (base) | **Parcial** |
| CENIT/STA rechazo D04 | **Cumplido técnico / pendiente validación externa** |
| CENIT/STA rechazo D05 + campo 6 | **Cumplido técnico / pendiente validación externa** |
| Naming STA completo fuera de rechazo | **Parcial** |
| Duplicidad ACH por archivo externo (universal) | **Parcial** |
| Reglas no documentadas en fuente primaria disponible | **Fuera de alcance declarado** |

---

## 5) Cobertura de pruebas por regla

- **`ExternalFileNamePolicyPhase1Tests`** cubre builder/validator y enforcements críticos de fase 1 (ACH patrón/correlación/límite/PSE base, CENIT-STA D04/D05, warning/audit-only).  
- **`NachaExportControllerTests`** cubre integración de política de nombre externo en exportación.

Conclusión de QA:
- Cobertura técnica suficiente para estado **“Cumplido técnico”** en reglas confirmadas.
- No sustituye certificación externa o aprobación normativa final para go-live productivo.

---

## 6) Qué falta para mover S1-12 a cada estado destino

### A) Para “Cumplido” (pleno)
1. Incorporar/validar fuente primaria faltante del manual STA completo (naming por tipo).  
2. Cerrar política explícita de duplicidad ACH por archivo externo completo.  
3. Cerrar detalle PSE operativo completo y su regla de enforcement final.  
4. Obtener validación externa/compliance formal y acta de aceptación.

### B) Para “Cumplido técnico / pendiente validación externa”
- **Ya alcanzado** con la evidencia actual.

### C) Para “Parcial”
- Reglas con warning/audit-only y/o fuente incompleta permanecen parciales.

### D) Para “Fuera de alcance declarado”
- Reglas no sustentadas en fuente primaria disponible y no incluidas explícitamente en alcance fase 1.

---

## 7) Decisión de readiness para S1-12

- **Estado anterior (S1):** Bloqueado (NO-GO).  
- **Estado propuesto S2:** **Cumplido técnico / pendiente validación externa**.

Implicación en Go/No-Go:
- Permite continuar UAT ampliado controlado con enforcements ya normativamente confirmados.
- No habilita go-live productivo final sin cierre externo/compliance de reglas pendientes.

---

## 8) Evidencia utilizada en este cierre

1. `docs/audits/external-filename-normative-matrix-ach-cenit-sta-2026-04-20-v2.md`  
2. `docs/audits/external-filename-phase1-implementation-2026-04-20.md`  
3. `docs/adr/ADR-ExternalFileNamePolicy-ACH-CENIT-STA-2026-04-20.md`  
4. `docs/normativa/md/ACH-Colombia-V32.md`  
5. `docs/normativa/md/CENIT-DSP-152-Anexo-2.md`  
6. `docs/normativa/md/CENIT-Anexo-B-Causales-Rechazo.md`  
7. `docs/audits/s1-matriz-maestra-trazable-funcional-normativa-2026-04-26.md`
