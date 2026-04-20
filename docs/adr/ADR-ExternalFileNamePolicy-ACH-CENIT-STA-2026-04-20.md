# ADR: External File Name Policy ACH/CENIT/STA

- **Fecha:** 2026-04-20
- **Estado:** Propuesto (diseño, no implementación)
- **Autores:** Auditoría técnica ACHInterbank (Codex)
- **Ámbito:** Backend ACHInterbank (Application/Domain/Persistence/API) + proyección SPA
- **Referencia base obligatoria:** `docs/audits/estado-actual-achinterbank-vs-achv32-cenit-2026-04-20.md`

---

## 1) Contexto

El rebaseline previo confirmó que:

- El núcleo NACHA-M (records/mapping/batch) está avanzado.
- Existe naming técnico parcial para exportación.
- Falta política integral de nombre externo regulatorio ACH/CENIT/STA.
- No existe validación integral de nombre externo ni correlación completa nombre↔contenido.
- Duplicidad de ingesta actual se basa en hash+tamaño, no en nombre externo.

Este ADR propone la arquitectura de diseño para cerrar ese gap sin implementar en esta fase.

---

## 2) Problema

El sistema carece de un marco único, auditable y normativo para:

1. Generar nombre externo por cámara/flujo/tipo.
2. Validar nombre externo de archivos entrantes/salientes.
3. Correlacionar nombre externo con:
   - Registro 1 (incluyendo FileIdModifier / identificador de archivo),
   - conteos reales del archivo,
   - ciclo/cámara/dirección/flujo.
4. Detectar duplicados por nombre externo con reglas operativas (no solo hash).
5. Dejar trazabilidad y evidencia auditable de cada decisión.

---

## 3) Decisión propuesta

Adoptar una **arquitectura de política externa de nombre** compuesta por:

- Política por cámara (`ACH`, `CENIT`, `STA` si aplica),
- Builder/Parser/Validator desacoplados,
- Servicio de secuencias externas,
- Guard de duplicidad por nombre,
- Servicio de correlación nombre↔contenido↔R1,
- Registro persistente y logs de validación/evidencia.

### Principios de diseño

1. **Separar nombre técnico interno vs nombre externo regulatorio.**
2. **No asumir reglas comunes entre ACH y CENIT.**
3. **Toda validación debe producir evidencia auditable.**
4. **Fallar con clasificación explícita** (regulatorio, técnico, ambiguo, requiere revisión manual).
5. **No bloquear operación por ambigüedad normativa**: clasificar como “requiere confirmación normativa ACH/CENIT/STA”.

---

## 4) Alternativas consideradas

### A1. Reusar naming actual en controllers + parches puntuales
- **Pros:** rápido.
- **Contras:** fragmentado, difícil de auditar, alto riesgo regulatorio.
- **Decisión:** descartada.

### A2. Política central por cámara con objetos de dominio y registro auditable
- **Pros:** coherente con Clean Architecture, testeable, trazable, extensible.
- **Contras:** mayor diseño inicial.
- **Decisión:** **aceptada**.

### A3. Configuración 100% editable en BD sin núcleo de reglas
- **Pros:** flexibilidad operativa.
- **Contras:** riesgo de alterar controles críticos sin guardas.
- **Decisión:** descartada para reglas críticas; usar enfoque híbrido (núcleo + parametrización controlada).

---

## 5) Conceptos de dominio (definiciones canónicas)

1. **Nombre técnico interno:** nombre operativo local (download/storage), no necesariamente regulatorio.
2. **Nombre externo regulatorio:** identificador de archivo exigido por cámara/protocolo.
3. **Cámara:** ACH Colombia / CENIT.
4. **Flujo:** originación/salida, recepción/entrada, devolución, rechazo, reverso, prenotificación.
5. **Dirección:** outbound / inbound.
6. **Tipo de archivo externo (`ExternalFileType`):** NACHA_OUT, NACHA_IN, RETURN_OUT, RETURN_IN, REJECT_IN, REVERSAL_OUT, PRENOTE_OUT, PRENOTE_IN, STA_IN, STA_OUT.
7. **Identificador de archivo:** valor del header regulatorio correlacionable con el nombre (ej. FileIdModifier cuando aplique).
8. **Secuencia externa:** consecutivo externo de nombre (semántica depende de cámara/flujo).
9. **Conteo declarado:** cantidad indicada en nombre externo (si aplica por norma).
10. **Conteo validado:** cantidad real calculada del contenido según regla vigente.
11. **Duplicado por nombre:** colisión regulatoria/operativa del nombre externo en misma ventana de unicidad.
12. **Evidencia de validación:** conjunto trazable de inputs, reglas aplicadas y resultados.
13. **Correlación nombre↔contenido:** prueba de consistencia entre componentes del nombre y los datos del archivo.

---

## 6) Arquitectura propuesta por capa

### 6.1 Application

Interfaces propuestas:

- `IExternalFileNamePolicy`
- `IExternalFileNameBuilder`
- `IExternalFileNameValidator`
- `IExternalFileNameSequenceService`
- `IExternalFileDuplicateGuard`
- `IExternalFileNameAuditService`
- `IExternalFileNameCorrelationService`

Casos de uso de aplicación:

1. `GenerateExternalNameAsync(context)`
2. `ValidateExternalNameAsync(context)`
3. `CorrelateExternalNameAsync(context)`
4. `RegisterExternalNameAsync(context, result)`
5. `CheckDuplicateAsync(context)`
6. `PreviewExternalNameAsync(context)`

### 6.2 Domain

Objetos/DTOs de dominio propuestos:

- `ExternalFileNameContext`
- `ExternalFileNameComponents`
- `ExternalFileNamePolicyResult`
- `ExternalFileNameValidationResult`
- `ExternalFileNameValidationIssue`
- `ExternalFileNameCorrelationEvidence`
- enums:
  - `ExternalFileType`
  - `ExternalFileFlow`
  - `ExternalFileDirection`

### 6.3 Persistence

Entidades propuestas:

- `ExternalFileSequence`
- `ExternalFileNameRegistry`
- `ExternalFileNameValidationLog`
- `ExternalFileNameCorrelationEvidence`

Concurrencia:

- `RowVersion` para secuencia y registry (si aplica en el modelo).

### 6.4 Infrastructure

Implementaciones propuestas:

- `AchExternalFileNamePolicy`
- `CenitExternalFileNamePolicy`
- `StaExternalFileNamePolicy` (si aplica)
- `AchExternalFileNameParser`
- `CenitExternalFileNameParser`
- `AchExternalFileNameValidator`
- `CenitExternalFileNameValidator`

### 6.5 API

Endpoints sugeridos (futuros):

- `POST /external-file-names/preview`
- `POST /external-file-names/validate`
- `GET /external-file-names/registry`
- `GET /external-file-names/registry/{id}`
- `GET /external-file-names/audit/{correlationId}`

### 6.6 SPA (futuro, no implementación)

- Consulta de registry y estado de validación.
- Filtros por cámara/flujo/fecha/estado.
- Vista de evidencia de correlación y motivos de rechazo.

---

## 7) Matriz cámara / flujo / tipo de archivo (conceptual)

> Estado normativo:
> - **Confirmado por evidencia**: existe comportamiento parcial en repo.
> - **Parcial**: existe algo, no integral.
> - **Requiere confirmación normativa ACH/CENIT/STA**: faltan reglas exactas en fuentes primarias disponibles.

| Cámara | Flujo | Dirección | Tipo archivo | Secuencia externa | Correlación R1 | Correlación conteos | Duplicidad por nombre | Estado normativo |
|---|---|---|---|---|---|---|---|---|
| ACH | Originación | Outbound | NACHA_OUT | Sí (propuesta) | Sí (propuesta) | Sí (propuesta) | Sí (propuesta) | Parcial |
| ACH | Recepción | Inbound | NACHA_IN | Según norma | Sí (propuesta) | Sí (propuesta) | Sí (propuesta) | Requiere confirmación normativa ACH/CENIT/STA |
| ACH | Devolución generada | Outbound | RETURN_OUT | Sí (propuesta) | Sí (si aplica) | Sí (propuesta) | Sí (propuesta) | Parcial |
| ACH | Devolución recibida | Inbound | RETURN_IN | Según norma | Sí (si aplica) | Sí (propuesta) | Sí (propuesta) | Requiere confirmación normativa ACH/CENIT/STA |
| ACH | Rechazo | Inbound | REJECT_IN | Según norma | Según norma | Sí (propuesta) | Sí (propuesta) | Requiere confirmación normativa ACH/CENIT/STA |
| ACH | Reverso | Outbound | REVERSAL_OUT | Sí (propuesta) | Sí (propuesta) | Sí (propuesta) | Sí (propuesta) | Requiere confirmación normativa ACH/CENIT/STA |
| ACH | Prenotificación | Outbound | PRENOTE_OUT | Sí (propuesta) | Sí (propuesta) | Conteo según definición detalle | Sí (propuesta) | Requiere confirmación normativa ACH/CENIT/STA |
| CENIT | Originación | Outbound | NACHA_OUT | Sí (evidencia parcial en naming actual) | Sí (evidencia parcial: FileIdModifier pos 36) | Sí (propuesta) | Sí (propuesta) | Parcial |
| CENIT | Recepción | Inbound | NACHA_IN | Según norma CENIT | Sí (propuesta) | Sí (propuesta, incluye D05 si aplica) | Sí (propuesta) | Requiere confirmación normativa ACH/CENIT/STA |
| CENIT | Devolución/rechazo | Inbound/Outbound | RETURN/REJECT | Según norma CENIT | Según norma | Sí (propuesta) | Sí (propuesta) | Requiere confirmación normativa ACH/CENIT/STA |
| CENIT | Prenotificación/Reverso | Inbound/Outbound | PRENOTE/REVERSAL | Según norma | Sí (propuesta) | Sí (propuesta) | Sí (propuesta) | Requiere confirmación normativa ACH/CENIT/STA |
| CENIT/STA | Transferencia STA | In/Out | STA_IN/STA_OUT | Según STA | Según STA | Según STA | Sí (propuesta) | Requiere confirmación normativa ACH/CENIT/STA |

---

## 8) Diseño de correlación nombre externo ↔ Registro 1

### Objetivo

Garantizar que el nombre externo sea coherente con metadatos regulatorios del archivo (incluyendo FileIdModifier/identificador cuando aplique).

### Propuesta técnica

1. Parsear nombre externo en `ExternalFileNameComponents`.
2. Extraer desde contenido los campos relevantes del Registro 1:
   - identificador de archivo / FileIdModifier (si aplica a la cámara/flujo),
   - fecha operativa,
   - originador/cámara.
3. Ejecutar regla de correlación por cámara (`IExternalFileNameCorrelationService`).
4. Emitir `ExternalFileNameValidationIssue` con severidad y evidencia.

### Reglas por cámara (nivel diseño)

- **ACH:** reglas exactas de correlación requieren confirmación normativa ACH V32 (cuando no estén explícitas en repo).
- **CENIT:** mantener compatibilidad con correlación ya evidenciada entre nombre y FileIdModifier en export; extender a validación bidireccional inbound/outbound.
- **STA:** requiere confirmación normativa ACH/CENIT/STA.

### Resultado

- `CorrelationStatus`: `Matched | Mismatch | NotApplicable | RequiresNormativeConfirmation`.
- `CorrelationEvidenceJson` con fuentes, valores comparados y reglas aplicadas.

---

## 9) Diseño de correlación nombre externo ↔ conteos reales

### Objetivo

Validar que cualquier conteo declarado en nombre externo (si la norma lo define) coincida con el conteo real del archivo.

### Motor de conteos propuesto

`IExternalFileNameCorrelationService.CalculateActualCounts(file, flow, chamber)` retorna:

- `DetailCountRecord6`
- `DetailCountRecord6Plus7` (si aplica)
- `TotalRecordCount`
- `BatchCount`
- `EntryAddendaCount`
- `FileControlConsistency`

### Regla de negocio

1. Si nombre declara conteo y norma aplica:
   - comparar `DeclaredDetailCount` vs `ActualDetailCount` según definición de “detalle” por flujo.
2. Si mismatch:
   - generar issue regulatorio (`COUNT_MISMATCH`) y causal mapeable (ej. D05 si norma CENIT lo confirma para ese caso).

### Nota crítica

La definición exacta de “detalle” por flujo/cámara/STA debe quedar en tabla de reglas y en campos de evidencia; cuando no haya fuente primaria en repo, marcar `RequiresNormativeConfirmation`.

---

## 10) Diseño de duplicidad por nombre externo

### Objetivo

Prevenir duplicados regulatorios sin impedir retransmisiones válidas controladas.

### Clave lógica de duplicidad propuesta

`(ClearingHouseId, FlowType, Direction, ProcessingDate, ExternalFileName, OriginatorId?, ReceiverId?)`

### Clasificaciones

1. **Exact duplicate (hard duplicate):** misma clave, mismo hash, misma ventana.
2. **Name duplicate with different hash:** mismo nombre, contenido distinto (alto riesgo).
3. **Valid retransmission:** permitido solo bajo regla explícita y audit trail.

### Estrategia

- `IExternalFileDuplicateGuard.CheckDuplicateAsync(context)` devuelve:
  - `NoDuplicate`
  - `DuplicateRejected`
  - `DuplicateAllowedByPolicy`
  - `RequiresManualReview`

Hash+tamaño permanecen como evidencia complementaria, no criterio único.

---

## 11) Persistencia y auditoría propuestas

### 11.1 `ExternalFileSequence`

Campos sugeridos:

- `Id`
- `ClearingHouseId`
- `FlowTypeId`
- `DirectionId`
- `ProcessingDate`
- `SequenceKey` (scope)
- `LastAssignedValue`
- `PolicyCode`
- `RowVersion`
- `CreatedAtUtc`
- `CreatedBy`

Índice único sugerido:

- `UX_ExternalFileSequence_Scope` = (`ClearingHouseId`,`FlowTypeId`,`DirectionId`,`ProcessingDate`,`SequenceKey`,`PolicyCode`)

### 11.2 `ExternalFileNameRegistry`

Campos sugeridos:

- `Id`
- `ClearingHouseId`
- `FlowTypeId`
- `DirectionId`
- `ExternalFileName`
- `InternalFileName`
- `ExternalFileType`
- `FileIdModifier`
- `ExternalSequence`
- `DeclaredDetailCount`
- `ActualDetailCount`
- `FileHash`
- `FileSize`
- `ProcessingDate`
- `CycleId`
- `Status`
- `ValidationResult`
- `ValidationIssuesJson`
- `CorrelationEvidenceJson`
- `CreatedAtUtc`
- `CreatedBy`
- `RowVersion`

Índices sugeridos:

- `UX_ExternalFileName_UniquenessWindow` (según scope final normativo)
- `IX_ExternalFileName_ProcessingDate`
- `IX_ExternalFileName_CycleId`
- `IX_ExternalFileName_FileHash`

### 11.3 `ExternalFileNameValidationLog`

Campos:

- `Id`
- `RegistryId`
- `ValidationStage` (Build/PreExport/PostParse/Inbound)
- `RuleCode`
- `Severity`
- `IssueCode`
- `IssueMessage`
- `IssuePayloadJson`
- `CreatedAtUtc`
- `CreatedBy`

### 11.4 `ExternalFileNameCorrelationEvidence`

Campos:

- `Id`
- `RegistryId`
- `EvidenceType` (R1,Counts,Duplicate,Normative)
- `InputSnapshotJson`
- `ComputedSnapshotJson`
- `ComparisonResult`
- `CreatedAtUtc`

---

## 12) Integración con flujos existentes (sin cambios en esta fase)

### Puntos de integración propuestos

1. **Generación NACHA saliente:**
   - Pre-build name preview
   - Build name
   - Validación pre-export
   - Registro en `ExternalFileNameRegistry`

2. **Export controller:**
   - reemplazar naming inline por `IExternalFileNameBuilder` (futuro)
   - ejecutar `IExternalFileNameCorrelationService` con R1

3. **Ingesta NACHA entrante:**
   - validar nombre externo en fase temprana
   - pasar resultado al parser y a la resolución de ciclo

4. **Parser NACHA:**
   - producir conteos reales y evidencia de correlación

5. **Returns/rechazos/prenotes/reversos:**
   - usar misma política con variantes por flujo

6. **SOAP (futuro):**
   - no tocar integración SOAP en esta fase
   - incluir referencias de external name para trazabilidad transaccional futura

### Orden recomendado de validación

- **Outbound:** BuildName → ValidateName → BuildFile → Correlate(R1+Counts) → Register → Export.
- **Inbound:** Receive → ParseName → DuplicateGuard → ParseFile → Correlate(R1+Counts) → Register → Continuar flujo.

---

## 13) Estrategia de pruebas futura (diseño)

### Unit tests

1. Parsers de nombre ACH/CENIT/STA.
2. Builders ACH/CENIT/STA.
3. Validadores por cámara/flujo.
4. Correlación R1.
5. Correlación de conteos.
6. Duplicate guard.

### Integration tests

1. Persistencia `ExternalFileNameRegistry`.
2. Secuencias externas y concurrencia.
3. Unicidad por nombre.
4. Integración con `IncomingNachaFileIngestion` y `AchFileExport`.

### E2E (futuro)

1. Saliente con nombre válido.
2. Entrante válido.
3. Duplicado por nombre.
4. Nombre con conteo inválido.
5. Nombre inconsistente con R1.
6. Escenarios devolución/rechazo/prenotificación/reverso.

---

## 14) Riesgos y decisiones

### Riesgos regulatorios

- Implementar reglas incompletas por ausencia de documento primario STA/Anexo específico.
- Interpretar “conteo de detalle” distinto a operador/cámara.

### Riesgos técnicos

- Sobrerrestricción que bloquee operación legítima.
- Subrestricción que permita archivos inválidos.

### Riesgos de compatibilidad

- Colisión con naming técnico actual.
- Migración progresiva de lógica existente en controllers/services.

### Decisiones reversibles

- Forma exacta de endpoints API.
- Estructura de payload de evidencia.
- Niveles de severidad por regla.

### Decisiones potencialmente irreversibles

- Definición de scope de unicidad en índices.
- Semántica persistida de secuencia externa y conteo declarado.

---

## 15) Preguntas normativas abiertas (bloqueantes de implementación)

1. Definición oficial por cámara de estructura del nombre externo por cada flujo/tipo.
2. Definición oficial de secuencia externa (alfanumérica, numérica, reset diario/ciclo, límites).
3. Regla formal de correlación nombre↔FileIdModifier/identificador (ACH vs CENIT).
4. Definición oficial de “número de registros de detalle” por flujo.
5. Causal exacta (ej. D05 u otra) para mismatch nombre↔conteo por cámara.
6. Política oficial de duplicado por nombre externo y retransmisión válida.
7. Reglas STA para inbound/outbound (naming, duplicidad, conteos, rechazo).
8. Reglas PSE específicas aplicables al nombre externo (si aplican).

Todas las anteriores quedan como: **“requiere confirmación normativa ACH/CENIT/STA”** hasta tener documentos primarios verificables.

---

## 16) Próximos pasos propuestos

1. Validar preguntas normativas abiertas con documento fuente oficial.
2. Congelar matriz final de reglas por cámara/flujo/tipo.
3. Aprobar diseño de interfaces/entidades.
4. Planificar implementación fase 1 (sin tocar SOAP/SPA):
   - builder/validator/correlation/duplicate guard + registry.
5. Planificar pruebas fase 1 (unit + integration).

---

## 17) Estado de este ADR

- **Listo para revisión arquitectónica y normativa.**
- **No listo para implementación** hasta cerrar preguntas normativas abiertas.
