# Matriz normativa congelada — External Filename ACH/CENIT/STA

**Fecha:** 2026-04-20 (UTC)  
**Fase:** Auditoría normativa y congelación de matriz (sin implementación)  
**Insumos base:**
- `docs/adr/ADR-ExternalFileNamePolicy-ACH-CENIT-STA-2026-04-20.md`
- `docs/audits/estado-actual-achinterbank-vs-achv32-cenit-2026-04-20.md`

---

## 1) Resumen ejecutivo

1. **CENIT/STA sí tiene evidencia normativa concreta** para:
   - existencia de plazos y tipos de archivos STA,
   - obligación de rechazo por causales del Anexo B,
   - regla explícita de **campo 6 “Número de Registros de Detalle” en el nombre del archivo de rechazos**,
   - causales D01..D06, incluyendo **D04 Archivo Duplicado** y **D05 mismatch entre conteo del nombre externo y conteo real**.
2. **ACH Colombia V32 (Manual Entidad Participante enero 2025) no está disponible** en el repo y no se obtuvo copia primaria verificable en esta fase.
3. Por lo anterior, la matriz queda:
   - **parcialmente confirmada para CENIT/STA**,
   - **pendiente/requiere confirmación normativa** para múltiples reglas ACH y subflujos no cubiertos en fuentes primarias disponibles.
4. Recomendación: implementar fase 1 en modo **seguro y auditable**, con reglas hard solo donde haya evidencia normativa directa y reglas pendientes en modo `WARNING`/`RequiresNormativeConfirmation`.

---

## 2) Fuentes revisadas

### 2.1 Fuentes del repositorio ACHInterbank

| Fuente | Disponibilidad | Uso en este documento |
|---|---|---|
| `docs/adr/ADR-ExternalFileNamePolicy-ACH-CENIT-STA-2026-04-20.md` | Disponible | Extracción de 8 preguntas normativas abiertas |
| `docs/audits/estado-actual-achinterbank-vs-achv32-cenit-2026-04-20.md` | Disponible | Baseline técnico previo |
| Documentos ACH V32 / DSP-152 / STA primarios dentro de repo | **No encontrados** | Gap documental interno |

### 2.2 Fuentes externas verificadas (primarias)

1. **Banco de la República — CEOS DSP-152 (27-feb-2025)**  
   URL: `https://www.banrep.gov.co/sites/default/files/reglamentacion/archivos/ceos_dsp-152_Asunto_1_feb_27_2025.pdf`

2. **Banco de la República — Manual CENIT (Anexo 2) versión consolidada**  
   URL: `https://www.banrep.gov.co/sites/default/files/reglamentacion/archivos/manual_dsp_cenit.pdf`

3. **Banco de la República — Anexo B Causales de Rechazo STA (28-nov-2023)**  
   URL: `https://www.banrep.gov.co/sites/default/files/reglamentacion/archivos/ceos-dsp-152-asunto-1-anexo-2-anexo-b-2023-11-28.pdf`

4. **Banco de la República — Anexo A Causales de Devolución CENIT (28-nov-2023)**  
   URL: `https://www.banrep.gov.co/sites/default/files/reglamentacion/archivos/ceos-dsp-152-asunto-1-anexo-2-anexo-a-2023-11-28.pdf`

### 2.3 Fuentes externas objetivo no obtenidas en esta fase

- Manual ACH Colombia “Transferencias Interbancarias para Entidad Participante”, **Versión 32, enero 2025** (no localizado en repo ni recuperado con URL primaria verificable en esta fase).
- Manual de Especificaciones del Formato STA (referenciado por DSP-152), no recuperado en esta fase.

---

## 3) Preguntas normativas del ADR: estado de cierre

Preguntas extraídas del ADR (sección 15):

| # | Pregunta ADR | Estado | Cierre actual |
|---|---|---|---|
| 1 | Estructura oficial nombre externo por cámara/flujo/tipo | Parcial | Confirmada parcialmente para CENIT-STA (rechazos/campo 6); ACH pendiente |
| 2 | Secuencia externa (tipo/reset/límites) | Pendiente | No evidencia explícita suficiente en fuentes consultadas |
| 3 | Correlación nombre externo ↔ FileIdModifier/identificador | Pendiente/Parcial | CENIT confirma correlación por campo 6 para rechazos; FileIdModifier como tal no explícito en norma consultada |
| 4 | Definición oficial “número de registros de detalle” por flujo | Parcial | CENIT-STA define uso del campo 6 en nombre externo para rechazos; definición operativa completa por todos los flujos pendiente |
| 5 | Causal exacta mismatch nombre ↔ conteo | **Resuelta para CENIT-STA** | Anexo B define **D05** |
| 6 | Política de duplicado por nombre y retransmisión válida | Parcial | Anexo B define **D04 Archivo Duplicado**; política completa de retransmisión válida pendiente |
| 7 | Reglas STA inbound/outbound | Parcial | Cap. 2 STA define ciclos, plazos, tipos de archivos y reglas de rechazo |
| 8 | Reglas PSE aplicables al nombre externo | Pendiente | Sin evidencia normativa primaria recuperada en esta fase |

---

## 4) Evidencia ACH V32 encontrada

> Resultado global ACH V32: **No se obtuvo documento primario verificable (Versión 32 enero 2025)** en repo ni fuente oficial descargable durante esta fase.

| Evidencia | Documento/Sección | Regla extraída | Confianza |
|---|---|---|---|
| No hallazgo de manual V32 en repo | Inventario `docs/` | No hay base documental primaria local para congelar reglas ACH de nombre externo | Alta |
| Resultado web: hallazgos no concluyentes o documentos distintos (reglamentos generales/otros productos) | Búsqueda abierta web | No permite cerrar reglas de naming externo ACH V32 solicitadas | Media |

**Clasificación ACH en esta fase:** `Requiere confirmación normativa`.

---

## 5) Evidencia CENIT/STA encontrada

### 5.1 DSP-152 / Manual CENIT (Anexo 2)

| Evidencia | Documento / capítulo | Regla extraída | Confianza |
|---|---|---|---|
| Capítulo 2 STA existe formalmente | Manual CENIT, Cap. 2 | STA es parte operativa formal del sistema | Alta |
| Horarios y ciclos STA (8:00-21:00; tipos de archivos por ciclo) | Manual CENIT, Cap.2 numeral 2 | Hay régimen operativo explícito para envío/recepción/rechazos/confirmaciones | Alta |
| Plazos para envío de archivos STA por tipo | Manual CENIT, Cap.2 numeral 3 | Existen deadlines normativos por tipo de archivo | Alta |
| Rechazos deben basarse en causales de Anexo B | Manual CENIT, Cap.2 numeral 4 | Rechazo está normado por catálogo cerrado de causales | Alta |
| Campo 6 “Número de Registros de Detalle” del nombre del archivo de rechazos debe reflejar conteo original o exacto rechazado (parcial) | Manual CENIT, Cap.2 numeral 4 | Correlación normativa **nombre externo ↔ conteo** explícita para rechazos STA | Alta |

### 5.2 Anexo B — Causales de rechazo STA

| Causal | Texto clave resumido | Regla extraída | Confianza |
|---|---|---|---|
| D01 | Archivo enviado erradamente | Rechazo por destinatario/operador incorrecto | Alta |
| D03 | Archivo con formato errado / no procesable | Rechazo por formato/estructura | Alta |
| D04 | Archivo Duplicado | Rechazo por duplicidad de archivo/información | Alta |
| D05 | Número de registros reportado en nombre externo ≠ registros contenidos | Rechazo por mismatch de conteo declarado en nombre externo vs contenido real | **Alta** |
| D06 | Error en regla de distribución | Rechazo por regla operativa de distribución | Alta |

### 5.3 Anexo A — Causales de devolución

- Se confirma disponibilidad del catálogo de devoluciones (Rxx), útil para flujo de compensación/liquidación.
- No se encontró evidencia directa en Anexo A que reemplace la regla D05 de STA para rechazo de nombre externo.

---

## 6) Matriz normativa congelada (cámara/flujo/tipo)

> Estados permitidos: Confirmado / Parcial / No encontrado / No aplica / Requiere confirmación normativa.

| Cámara | Flujo | Dirección | Tipo archivo | Nombre externo requerido | Componentes del nombre | Secuencia externa | Reset secuencia | Relación con Registro 1 | Relación con conteos | Duplicidad por nombre | Causal mismatch | Evidencia documental | Confianza | Estado |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| ACH | Originación | Outbound | NACHA_OUT | Requiere confirmación | No definido en fuente primaria fase | Requiere confirmación | Requiere confirmación | Requiere confirmación | Requiere confirmación | Requiere confirmación | Requiere confirmación | Manual V32 no disponible | Baja | Requiere confirmación normativa |
| ACH | Recepción | Inbound | NACHA_IN | Requiere confirmación | No definido | Requiere confirmación | Requiere confirmación | Requiere confirmación | Requiere confirmación | Requiere confirmación | Requiere confirmación | Manual V32 no disponible | Baja | Requiere confirmación normativa |
| ACH | Devolución generada | Outbound | RETURN_OUT | Requiere confirmación | No definido | Requiere confirmación | Requiere confirmación | Requiere confirmación | Requiere confirmación | Requiere confirmación | Requiere confirmación | Manual V32 no disponible | Baja | Requiere confirmación normativa |
| ACH | Devolución recibida | Inbound | RETURN_IN | Requiere confirmación | No definido | Requiere confirmación | Requiere confirmación | Requiere confirmación | Requiere confirmación | Requiere confirmación | Requiere confirmación | Manual V32 no disponible | Baja | Requiere confirmación normativa |
| ACH | Rechazo | In/Out | REJECT | Requiere confirmación | No definido | Requiere confirmación | Requiere confirmación | Requiere confirmación | Requiere confirmación | Requiere confirmación | Requiere confirmación | Manual V32 no disponible | Baja | Requiere confirmación normativa |
| ACH | Reverso | Outbound | REVERSAL_OUT | Requiere confirmación | No definido | Requiere confirmación | Requiere confirmación | Requiere confirmación | Requiere confirmación | Requiere confirmación | Requiere confirmación | Manual V32 no disponible | Baja | Requiere confirmación normativa |
| ACH | Prenotificación | In/Out | PRENOTE | Requiere confirmación | No definido | Requiere confirmación | Requiere confirmación | Requiere confirmación | Requiere confirmación | Requiere confirmación | Requiere confirmación | Manual V32 no disponible | Baja | Requiere confirmación normativa |
| ACH (PSE) | Subflujo PSE | In/Out | PSE | Requiere confirmación | No definido | Requiere confirmación | Requiere confirmación | Requiere confirmación | Requiere confirmación | Requiere confirmación | Requiere confirmación | No evidencia primaria en fase | Baja | Requiere confirmación normativa |
| CENIT | Compensación/liquidación originación | Outbound | NACHA_OUT | Parcial | No se obtuvo especificación completa de nombre en fuente revisada | Requiere confirmación | Requiere confirmación | Parcial | No explícito completo | Parcial | No explícito | DSP-152/Manual CENIT (operación general) | Media | Parcial |
| CENIT | Compensación/liquidación recepción | Inbound | NACHA_IN | Parcial | No se obtuvo especificación completa | Requiere confirmación | Requiere confirmación | Parcial | Parcial | Parcial | No explícito | DSP-152/Manual CENIT | Media | Parcial |
| CENIT-STA | Envío información a operadores | Outbound | STA_OUT | **Sí** (referencia a manual de especificaciones STA) | Incluye campo 6 para rechazos | Parcial | No explícito | No explícito | **Sí** (campo 6) | **Sí** (D04) | **D05** | Manual CENIT cap.2 num.4 + Anexo B | Alta | Confirmado (alcance rechazo) |
| CENIT-STA | Recepción/rechazo de archivos | Inbound | STA_IN / REJECT_OUT | **Sí** (en rechazos) | Campo 6 = número de registros detalle original o exacto rechazado | Parcial | No explícito | No explícito | **Sí, obligatorio** | **Sí** | **D05** | Manual CENIT cap.2 num.4 + Anexo B D04/D05 | Alta | Confirmado (alcance rechazo) |
| CENIT | Devoluciones | In/Out | RETURN | Sí (catálogo causales Rxx) | No enfocado a nombre externo en evidencia revisada | No explícito | No explícito | No explícito | No explícito | Parcial (R67 devolución duplicada) | Rxx (no D05) | Anexo A | Media | Parcial |
| CENIT | Prenotificación | In/Out | PRENOTE | Parcial | Evidencia en causales R31/R32 (procesamiento) no en naming completo | No explícito | No explícito | No explícito | No explícito | Parcial | No explícito | Anexo A | Media | Parcial |
| CENIT | Reverso | In/Out | REVERSAL | No encontrado explícito en fuentes revisadas | No definido | No definido | No definido | No definido | No definido | No definido | No definido | No hallado directo en fase | Baja | No encontrado |

---

## 7) Matriz de decisión para implementación

| Regla | Implementar fase 1 | Implementar después | No implementar aún | Requiere confirmación | Riesgo si se implementa sin confirmar |
|---|---|---|---|---|---|
| `ExternalFileNameRegistry` (auditoría neutra) | ✅ Sí |  |  |  | Bajo |
| `ExternalFileNameValidationLog` / evidencia | ✅ Sí |  |  |  | Bajo |
| `DuplicateExternalFileNameGuard` base (modo warning configurable) | ✅ Sí |  |  | ✅ | Medio |
| `IncomingExternalFileNameValidator` genérico (sintaxis + parsing) | ✅ Sí |  |  | ✅ | Medio |
| `STA Reject Name Count Correlation` (campo 6) | ✅ Sí (hard en STA rechazo) |  |  |  | Bajo |
| `D05 validator` para STA rechazo | ✅ Sí (hard) |  |  |  | Bajo |
| `D04 duplicate` para STA rechazo | ✅ Sí (hard) |  |  |  | Bajo |
| `ExternalFileNameBuilder` outbound CENIT general |  | ✅ Sí |  | ✅ | Medio |
| `FileIdModifierCorrelation` cross-flujo |  | ✅ Sí |  | ✅ | Medio/Alto |
| `ExternalFileNameBuilder` outbound ACH |  |  | ✅ | ✅ | Alto |
| `PSENamePolicy` |  |  | ✅ | ✅ | Alto |
| `STANamePolicy` completa (todos los tipos) |  | ✅ Sí |  | ✅ | Medio |
| `DeclaredDetailCountValidator` para ACH no-STA |  |  | ✅ | ✅ | Alto |

---

## 8) Recomendación de implementación fase 1 (bajo riesgo)

1. **Implementar primero infraestructura neutra de auditoría y trazabilidad**:
   - `ExternalFileNameRegistry`
   - `ExternalFileNameValidationLog`
   - `ExternalFileNameCorrelationEvidence`

2. **Habilitar validación hard solo donde la norma está explícita**:
   - STA rechazos: campo 6 (número de registros de detalle) + D05 + D04.

3. **Para reglas no confirmadas, usar modo WARNING/RequiresNormativeConfirmation**:
   - secuencia externa global,
   - correlación con R1 fuera de casos normativamente cerrados,
   - políticas ACH/PSE.

4. **Evitar bloqueos productivos duros en ACH/PSE** hasta tener documento primario V32 verificable.

5. **Registrar toda decisión con evidencia documental referenciada** (fuente, fecha, versión del documento normativo).

---

## 9) Preguntas pendientes para ACH/CENIT

### Para ACH Colombia (V32)

1. Estructura oficial del nombre externo por flujo/tipo.
2. Semántica de secuencia (A-Z, 0-9, límites diarios/ciclo).
3. Relación formal nombre↔FileIdModifier/campo de R1.
4. Definición de “número de registros de detalle” por flujo.
5. Política de duplicados y retransmisión válida.
6. Reglas PSE específicas de naming externo.

### Para CENIT/STA

1. Confirmar manual de especificaciones STA vigente con estructura exacta de nombre por tipo de archivo (más allá del campo 6 en rechazos).
2. Confirmar si D05 aplica estrictamente a todos los escenarios STA o a subconjuntos de tipo/flujo.
3. Confirmar política de secuencia externa completa y reset para cada tipo STA.
4. Confirmar reglas de correlación explícita con campos de encabezado cuando no son archivos de rechazo STA.

---

## 10) Riesgos

### Críticos
- Implementar reglas ACH/PSE sin fuente primaria V32 puede generar incumplimiento regulatorio.

### Altos
- Bloquear archivos por correlaciones no confirmadas puede afectar operación legítima.
- Inferir secuencias/reset sin norma explícita puede producir falsos duplicados.

### Medios
- Inconsistencia entre versiones de anexos (2023 vs 2025 consolidado) si no se controla versionado documental.

### Bajos
- Riesgo de retrabajo en naming builders si se separa desde ahora hard-rules vs warning-rules (mitigable con feature flags y catálogo de reglas).

---

## 11) Estado de congelación

- **Matriz congelada con evidencia normativa parcial**:
  - Confirmada para reglas STA de rechazo (D04/D05 + campo 6).
  - Pendiente para ACH V32 y parte de CENIT no cubierta por fuentes recuperadas.

- **Decisión operativa recomendada**:
  - avanzar fase 1 con implementación segura (auditoría + validación hard solo en reglas confirmadas),
  - mantener reglas pendientes en modo `WARNING` hasta cierre normativo.
