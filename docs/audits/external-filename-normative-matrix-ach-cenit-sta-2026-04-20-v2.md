# Matriz normativa v2 — Nombre externo ACH / CENIT / STA (auditoría normativa)

**Fecha de recalculo:** 2026-04-20 (UTC)  
**Tipo:** Auditoría normativa + arquitectura (sin implementación)  
**Fuente de verdad regulatoria usada:** `docs/normativa/md` con verificación puntual en `docs/normativa/pdf` cuando hubo dudas de consistencia.

---

## 1) Resumen ejecutivo

1. **La evidencia normativa ahora sí permite cerrar reglas críticas de nombre externo para ACH y CENIT/STA, pero no toda la matriz end-to-end.**
2. **ACH V32 (documento en repo) confirma** nomenclatura `RRRRTTT.ZZZ.1`, correlación obligatoria `ZZZ ↔ Registro 1 campo 7 Identificador del Archivo`, tabla de mapeo `A-Z/0-9 ↔ 001-036`, y límite de **máximo 36 archivos diarios** (incluyendo PSE en nombre del participante).
3. **CENIT/STA confirma** que en rechazos se usa el **campo 6 del nombre** con el número de registros de detalle y soporta causales **D04 (duplicado)** y **D05 (mismatch nombre externo vs contenido)**.
4. Persisten vacíos para una implementación 100% dura en todos los flujos (ej. estructura completa del nombre STA por tipo, política completa de reset/secuencia CENIT/STA fuera del caso de rechazo, y reglas PSE detalladas delegadas al Manual de Operaciones PSE).
5. **Veredicto:** matriz **lista para fase 1 segura** con alcance acotado (hard blocks solo donde hay texto explícito).

---

## 2) Fuentes normativas revisadas

### 2.1 ACH
- `docs/normativa/md/ACH-Colombia-V32.md`  
  - Sección 6.1.10.1 (Nombre del Archivo).  
  - Sección 6.1.10.2 (Nombre de Archivos PSE).  
  - Sección 6.1.10.3 (máximo 36 archivos y reglas de contenido).  
  - Tabla de campo 7 del Registro 1 (Identificador del Archivo, A-Z/0-9).  
  - Tabla/validación de rechazo por identificador de archivo incorrecto (causal 14, extracción de consecutivo desde nombre y comparación contra tabla).
- `docs/normativa/pdf/ACH-Colombia-V32.pdf` (verificación puntual):
  - Página 135: 6.1.10.1 y nomenclatura `RRRRTTT.ZZZ.1`.
  - Página 136: máximo 36 archivos.
  - Página 145+ (según ficha): campo 7 Registro 1, A-Z/0-9, posición 36.

### 2.2 CENIT / STA
- `docs/normativa/md/CENIT-DSP-152-Anexo-2.md`
  - Capítulo 2 numeral 4 (Causales de rechazo STA; obligación campo 6 en nombre de archivo de rechazo y regla para rechazo parcial).
- `docs/normativa/md/CENIT-Anexo-B-Causales-Rechazo.md`
  - D04 Archivo Duplicado.
  - D05 número de registros reportado en nombre externo diferente al contenido.
- `docs/normativa/pdf/CENIT-DSP-152-Anexo-2.pdf` (verificación puntual)
  - Página 18: campo 6 número de registros de detalle en archivo de rechazo.
- `docs/normativa/pdf/CENIT-Anexo-B-Causales-Rechazo.pdf` (verificación puntual)
  - D04 y D05 en hoja única.

### 2.3 Fuente auxiliar (no definitoria para naming)
- `docs/normativa/md/CENIT-Anexo-A-Causales-Devolucion.md` (catálogo de devoluciones Rxx; útil para contexto de flujo, no para regla principal de nombre externo).

---

## 3) Diferencias frente a matriz anterior (v1)

1. **Antes:** ACH estaba mayormente en “Requiere confirmación normativa” por ausencia de V32 en repo.  
   **Ahora:** ACH queda con varios puntos en **Confirmado** (estructura de nombre, correlación con R1, mapeo ZZZ, límite 36).
2. **Antes:** relación `ZZZ ↔ campo 7 Registro 1` tratada como parcial.  
   **Ahora:** queda **Confirmado** por texto explícito y además por regla de rechazo (identificador incorrecto).
3. **Antes:** PSE de naming estaba abierto casi total.  
   **Ahora:** se confirma obligación de reservar rango para PSE y correspondencia con campo 7; pero detalles operativos siguen remitidos al Manual de Operaciones PSE (**Parcial**).
4. **CENIT/STA:** se mantiene confirmación de D04/D05 y campo 6 en rechazo, pero se aclara explícitamente el límite del alcance (rechazos STA, no toda la taxonomía de nombres STA).

---

## 4) Resolución de las 8 preguntas abiertas del ADR

| # | Pregunta ADR | Respuesta | Estado | Evidencia | Confianza |
|---|---|---|---|---|---|
| 1 | Estructura nombre externo por cámara | ACH: `RRRRTTT.ZZZ.1` explícito. CENIT/STA: estructura completa remite a manual STA; para rechazo se exige campo 6 en nombre de rechazo. | Parcial | ACH 6.1.10.1; CENIT Cap.2 num.4 | Alta |
| 2 | Secuencia externa | ACH: ZZZ consecutivo diario iniciando en 1; además tabla 001-036. CENIT/STA: no se encontró secuencia completa para todos los tipos STA. | Parcial | ACH 6.1.10.1 + tabla identificador; CENIT Cap.2 | Alta/Media |
| 3 | Relación nombre ↔ FileIdModifier/identificador | ACH: correlación obligatoria de ZZZ con campo 7 Registro 1 (Identificador del Archivo). | Confirmado | ACH 6.1.10.1 + causal 14 de validación | Alta |
| 4 | Definición número de registros de detalle | CENIT/STA rechazo: campo 6 debe reflejar conteo original o exacto rechazado (parcial). Definición transversal por todos los flujos no explícita en fuentes actuales. | Parcial | CENIT Cap.2 num.4 | Alta |
| 5 | Causal mismatch nombre vs contenido | CENIT/STA D05 explícita. ACH: existe causal de rechazo por identificador incorrecto (correlación nombre↔R1), pero no causal equivalente textual tipo D05 para nombre externo/conteos en el extracto revisado. | Parcial | CENIT Anexo B D05; ACH causal 14 identificador | Alta/Media |
| 6 | Política de duplicados | CENIT/STA D04 explícita (archivo duplicado). ACH: se evidencia D31 lote duplicado en mismo día (lote, no necesariamente archivo externo completo). | Parcial | CENIT Anexo B D04; ACH catálogo causales (D31) | Alta/Media |
| 7 | Reglas STA inbound/outbound | Sí hay horarios/ciclos/plazos/causales de rechazo; naming total por tipo remite al manual STA especializado. | Parcial | CENIT Cap.2 nums.2-4 | Alta |
| 8 | Reglas PSE | ACH define rango reservado y correlación campo 7↔nombre para archivos PSE; pero detalles se delegan al Manual de Operaciones PSE y Anexo 7 no incorporados aquí. | Parcial | ACH 6.1.10.2 y 6.1.10.4 | Media |

---

## 5) Evidencia ACH V32 (reglas extraídas)

> Clasificación obligatoria por regla: Confirmado / Parcial / No encontrado / No aplica / Requiere confirmación normativa.

| Regla ACH | Clasificación | Evidencia (documento + sección/página) | Confianza |
|---|---|---|---|
| Nombre externo general usa `RRRRTTT.ZZZ.1` | Confirmado | ACH V32, 6.1.10.1; PDF p.135 | Alta |
| ZZZ es consecutivo diario iniciando en 1 por archivo enviado | Confirmado | ACH V32, 6.1.10.1; PDF p.135 | Alta |
| Sistema verifica `ZZZ` contra campo 7 Identificador del Archivo (Registro 1) | Confirmado | ACH V32, 6.1.10.1; PDF p.135 | Alta |
| Tabla identificador: A-Z => 001-026 y 0-9 => 027-036 | Confirmado | ACH V32, tabla identificador (6.1.10.1) | Alta |
| Campo 7 Registro 1 acepta A-Z/0-9 (posición 36) | Confirmado | ACH ficha técnica Registro 1 (campo 7) | Alta |
| Validación regulatoria de rechazo por identificador incorrecto exige extraer consecutivo del nombre y comparar contra tabla | Confirmado | ACH causal 14 (sección de validaciones/rechazos; referencia 6.1.10.1) | Alta |
| Máximo 36 archivos diarios incluyendo originados por PSE en nombre del participante | Confirmado | ACH 6.1.10.3; PDF p.136 | Alta |
| PSE: entidad debe definir rango de secuencia de identificación de archivo para nombramiento | Confirmado | ACH 6.1.10.2 | Alta |
| PSE: campo 7 (registro 1) debe reservar rango 4..9 y corresponder con secuencia del nombre externo | Confirmado | ACH 6.1.10.2 | Alta |
| Límite PSE “generar archivos con número de secuencia <=31” | Requiere confirmación normativa | ACH 6.1.10.4 aparece como recomendación y remite a Manual de Operaciones PSE/Anexo 7; falta ancla completa aquí para endurecer bloqueo | Media |
| Política integral de duplicado de archivo externo ACH | Parcial | Se observa D31 “lote duplicado” (no cubre inequívocamente archivo externo completo) | Media |

---

## 6) Evidencia CENIT / STA (reglas extraídas)

| Regla CENIT/STA | Clasificación | Evidencia (documento + sección/página) | Confianza |
|---|---|---|---|
| STA opera con horarios/ciclos definidos (8:00-21:00, 2 ciclos) | Confirmado | CENIT Anexo 2, Cap.2 num.2 | Alta |
| Tipos de archivos STA por ciclo (incluye archivos con rechazos del día) | Confirmado | CENIT Anexo 2, Cap.2 num.2 | Alta |
| Plazos de envío por tipo de archivo STA | Confirmado | CENIT Anexo 2, Cap.2 num.3 | Alta |
| Rechazo STA solo por causales del Anexo B | Confirmado | CENIT Anexo 2, Cap.2 num.4 | Alta |
| En rechazo STA, campo 6 del nombre del archivo de rechazo debe llevar conteo original o exacto rechazado | Confirmado | CENIT Anexo 2, Cap.2 num.4; PDF p.18 | Alta |
| D04 Archivo Duplicado | Confirmado | CENIT Anexo B D04 | Alta |
| D05 mismatch número de registros del nombre externo vs contenido | Confirmado | CENIT Anexo B D05 | Alta |
| Estructura completa del nombre STA por todos los tipos | Requiere confirmación normativa | CENIT remite al “Manual de Especificaciones del Formato STA” no contenido integralmente en fuentes revisadas | Alta (sobre la existencia del gap) |
| Reglas de devolución (Rxx) para CENIT | Confirmado | Anexo A (catálogo Rxx) | Alta |
| Regla de nombre externo para devoluciones CENIT en Anexo A | No encontrado | Anexo A revisado no explicita estructura de nombre externo | Alta |

---

## 7) Matriz normativa actualizada (consolidada)

| Cámara | Flujo | Dirección | Tipo archivo | Nombre requerido | Componentes | Secuencia | Reset | Relación R1 | Relación conteos | Duplicidad | Causal | Estado | Evidencia | Confianza |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| ACH | Originación | Outbound | NACHA_OUT | Sí | `RRRRTTT.ZZZ.1` | ZZZ diario desde 1, tabla 001-036 | Diario (por texto de consecutivo diario) | Sí, ZZZ ↔ campo 7 R1 | No explícita de conteo en nombre general ACH | Parcial (D31 lote) | Causal 14 para identificador incorrecto | Confirmado/Parcial | ACH 6.1.10.1, causal 14, D31 | Alta/Media |
| ACH | Recepción | Inbound | NACHA_IN | Sí (validable por misma norma de nombre + identificador) | `RRRRTTT.ZZZ.1` | Igual | Igual | Sí (validación identificador) | No explícita | Parcial | Causal 14 | Parcial | ACH 6.1.10.1 + causal 14 | Media |
| ACH | PSE | Outbound | NACHA_OUT_PSE | Sí | Nombre externo con secuencia reservada para PSE | Rango reservado por participante | No explícito completo (remite manual PSE) | Sí, corresponde con campo 7 | No cerrada en estas fuentes | No encontrado | No encontrado específico de nombre externo | Parcial | ACH 6.1.10.2 y 6.1.10.4 | Media |
| ACH | Devoluciones / Reversos / Prenotes | In/Out | RETURN/REV/PRENOTE | Parcial | Se usa NACHA-M; naming específico no totalmente discriminado por tipo en extracto revisado | Parcial | Parcial | Parcial | No encontrado | Parcial | Parcial | Requiere confirmación normativa | ACH V32 (varias fichas + referencias cruzadas) | Media/Baja |
| CENIT-STA | Rechazo | Outbound/Inbound | REJECT_STA | Sí | Incluye campo 6 en nombre de rechazo | No explícita completa | No explícito | No explícito | Sí (campo 6 vs registros enviados/rechazados) | Sí (D04) | D04/D05 | Confirmado | CENIT Cap.2 num.4 + Anexo B | Alta |
| CENIT-STA | Intercambio normal (salida/info/confirmación) | In/Out | STA_OUT/STA_IN/ACK/NACK | Parcial | Existe obligación operativa; estructura exacta remite al Manual STA | Requiere confirmación | Requiere confirmación | Requiere confirmación | Requiere confirmación | Requiere confirmación | Requiere confirmación | Requiere confirmación normativa | CENIT Cap.2 nums.2-4 | Media |
| CENIT Comp/Liq | Devoluciones | In/Out | RETURN | Sí (causales Rxx) | No se documenta estructura nombre externo en Anexo A | N/A en evidencia actual | N/A | N/A | N/A | Parcial (por causal de negocio, no por filename) | Rxx | Parcial | Anexo A | Media |

---

## 8) Matriz de decisión para implementación (fase 1)

| Regla / Control | Decisión | Justificación normativa |
|---|---|---|
| ACH: validar patrón `RRRRTTT.ZZZ.1` (sintaxis base) | **HARD BLOCK** | Regla explícita 6.1.10.1 |
| ACH: validar `ZZZ ↔ campo 7 R1` con tabla A-Z/0-9↔001-036 | **HARD BLOCK** | Regla explícita + causal 14 de rechazo |
| ACH: bloquear si excede 36 archivos diarios por participante (incluye PSE en su nombre) | **HARD BLOCK** | Regla explícita 6.1.10.3 |
| ACH/PSE: validar rango reservado para identificador campo 7 (4..9) y correspondencia con nombre externo PSE | **HARD BLOCK** (cuando archivo marcado como PSE) | Regla explícita 6.1.10.2 |
| CENIT/STA rechazo: validar campo 6 contra registros declarados/ rechazados | **HARD BLOCK** | Cap.2 num.4 + D05 |
| CENIT/STA rechazo: duplicidad D04 | **HARD BLOCK** | Anexo B D04 |
| CENIT/STA no-rechazo: estructura completa de nombre por tipo STA | **WARNING** | Falta detalle completo del Manual de Especificaciones STA en fuentes actuales |
| ACH duplicidad de archivo externo más allá de D31 lote | **WARNING** | Evidencia parcial (D31 es de lote; no prueba cobertura integral de archivo externo) |
| Reglas no explícitas de reset/secuencia por cada subflujo CENIT/STA | **AUDIT ONLY** | No hay texto completo en normativa revisada |
| Heurísticas no normadas (ej. inferencias de naming por convenio interno) | **NO IMPLEMENTAR** | Riesgo regulatorio por sobre-interpretación |

---

## 9) Recomendación de fase 1 segura

### Bloquear (HARD BLOCK)
1. ACH: formato `RRRRTTT.ZZZ.1`.
2. ACH: consistencia `ZZZ ↔ R1.campo7` según tabla identificador.
3. ACH: umbral diario de 36 archivos por participante (incluye PSE originado en su nombre).
4. PSE (cuando aplique): consistencia de rango reservado en campo 7 (4..9) y secuencia de nombre.
5. CENIT/STA rechazo: validación de campo 6 y causales D04/D05.

### Advertir (WARNING)
1. Toda regla de naming STA fuera del alcance explícito de rechazo/campo 6.
2. Duplicidad ACH por archivo externo completo cuando la evidencia solo asegura D31 de lote.

### Auditar (AUDIT ONLY)
1. Métricas de conflictos nombre↔contenido en reglas no completamente cerradas.
2. Trazabilidad obligatoria por cámara/flujo para facilitar cierre normativo posterior.

### No implementar aún
1. Reglas inferidas no textuales sobre estructura STA completa.
2. Suposiciones de simetría ACH↔CENIT sin evidencia normativa directa.

---

## 10) Preguntas pendientes (requieren cierre documental adicional)

1. Estructura completa de nombre STA por cada tipo de archivo (no solo rechazo): pendiente del manual STA de especificaciones completo.
2. Política completa de secuencia/reset STA por tipo de archivo.
3. Cierre explícito de política de duplicados de archivo externo ACH (más allá de D31 lote duplicado).
4. Reglas PSE completas que están delegadas al Manual de Operaciones PSE/Anexo 7 (detalle de rangos, escenarios excepcionales y enforcement exacto).
5. Si en ACH existen causales explícitas equivalentes a D05 para conteo declarado en nombre externo vs contenido (no encontradas explícitamente en el extracto revisado).

---

## 11) Hallazgos por severidad

### Críticos
- No hay soporte para bloquear de forma dura reglas STA de naming completo (fuera de rechazos) sin incorporar el manual STA detallado.

### Altos
- Riesgo de falsos rechazos si se extrapola D31 (lote duplicado) como política universal de duplicidad de archivo externo ACH.

### Medios
- Dependencia de manual PSE para cerrar detalle operativo de naming PSE más allá de lo confirmado en 6.1.10.2.

### Bajos
- La coherencia documental general entre md y pdf en puntos críticos revisados es suficiente para fase 1 acotada.

---

## 12) Veredicto final

**Sí, la matriz queda lista para implementación fase 1 segura**, con alcance explícitamente acotado a reglas normativamente confirmadas:  
- HARD BLOCK en reglas ACH (patrón, correlación con R1, límite 36, rango PSE en campo 7 cuando aplique) y en CENIT/STA para rechazos (campo 6, D04, D05).  
- WARNING/AUDIT ONLY en el resto de reglas donde la normativa revisada no es suficientemente explícita.

**No se recomienda fase 1 “full hard enforcement” para toda la matriz ACH/CENIT/STA** hasta cerrar pendientes del manual STA y detalle completo PSE.
