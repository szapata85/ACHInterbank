# NACHA-M Mapping Engine — Diseño fuerte, incremental y ejecutable (v2)

Fecha: 2026-04-19  
Ámbito: ACH Colombia + CENIT  
Compatibilidad: Mantiene `NachaConfigResolver` + `NachaFileBuilder` híbrido + `NachaFixedWidthRecordRenderer`.

## 1) Resumen ejecutivo del diseño del mapping engine

Se propone un engine de mapping por campo/registro gobernado por configuración que cierra la brecha actual “modelo > ejecución” sin big-bang rewrite.

Decisiones rectoras:

1. Mantener selección contextual actual (`CfgProfile`/`CfgLayoutVariant`) en `NachaConfigResolver`.
2. Insertar un pipeline explícito por campo:
   `SOURCE -> RESOLUTION -> TRANSFORMATION -> VALIDATION -> FALLBACK -> RENDER TARGET`.
3. Conservar en código las invariantes críticas (8/9, block count, integridad estructural universal).
4. Migrar por fases iniciando en record 6 y convergiendo type 7 al engine común.

Veredicto de diseño: realista, incremental, auditable, implementable en la base actual.

## 2) Arquitectura propuesta del pipeline source -> resolve -> transform -> validate -> fallback -> render

### 2.1 SOURCE
- Responsabilidad: obtener valor primario desde `CfgFieldSourceDefinition`.
- Inputs: `FieldRuntimePlan`, `SourceEnvelope` (entity/context/precomputed).
- Output: `SourceResolutionResult`.
- Errores: `SOURCE_MISSING`, `SOURCE_TYPE_UNSUPPORTED`, `PROPERTY_PATH_NOT_FOUND`.
- Trazabilidad: source type, path/constant/expression id, duración.
- Capa: Infrastructure (implementación), Application (contrato).

### 2.2 RESOLUTION
- Responsabilidad: canonicalizar clave, resolver alias y binding tipado.
- Inputs: `SourceResolutionResult`, `CanonicalCatalogSnapshot`.
- Output: `ResolvedValueResult` (valor + canonical key final).
- Errores: `CANONICAL_COLLISION`, `ALIAS_NOT_MAPPED`.
- Trazabilidad: alias entrada, canonical usado, accessor seleccionado.
- Capa: Application/Infrastructure.

### 2.3 TRANSFORMATION
- Responsabilidad: ejecutar pipeline declarativo sobre valor lógico.
- Inputs: `ResolvedValueResult`, `CompiledTransformPipeline`.
- Output: `TransformedValueResult`.
- Errores: `TRANSFORM_TYPE_ERROR`, `TRANSFORM_INVALID_ARG`, `TRANSFORM_OVERFLOW`.
- Trazabilidad: lista ordenada de transforms con before/after resumido.
- Capa: Application (contrato), Infrastructure (handlers).

### 2.4 VALIDATION
- Responsabilidad: ejecutar reglas field-level soportadas por fase.
- Inputs: valor transformado + `CompiledFieldRules` + contexto.
- Output: `FieldValidationResult` (Issues ERROR/WARN).
- Errores: `RULE_EXECUTION_ERROR` + issues de negocio.
- Trazabilidad: ruleCode, resultado, severidad.
- Capa: Application/Infrastructure.

### 2.5 FALLBACK
- Responsabilidad: aplicar política si hubo ausencia/error/issue bloqueante.
- Inputs: resultados previos + `CompiledFallbackPolicy`.
- Output: `FallbackResult` (valor final lógico + reason chain).
- Errores: `FALLBACK_EXHAUSTED`, `FALLBACK_POLICY_INVALID`.
- Trazabilidad: pasos intentados, paso ganador, motivo.
- Capa: Application/Infrastructure.

### 2.6 RENDER TARGET
- Responsabilidad: entregar `fieldCode -> logicalValue` al renderer fixed-width.
- Inputs: resultados finales por campo del record.
- Output: línea NACHA final.
- Errores: `RENDER_FIELD_LENGTH_MISMATCH`.
- Trazabilidad: valor final pre-render, valor rendered.
- Capa: Persistence existente (`NachaFixedWidthRecordRenderer`).

### 2.7 Relación con componentes actuales

- `NachaConfigResolver`: se mantiene (elige layout/variante).
- `NachaFileBuilder`: orquestador de record sequence; delega mapping field-level.
- `NachaFixedWidthRecordRenderer`: sigue como formateador fixed-width final.
- `NachaSemanticValidator`: sigue para invariantes archivo completo.

## 3) Interfaces/componentes principales del engine

Contratos principales (Application):

- `INachaRecordMappingEngine`
  - `Task<RecordMappingResult> MapRecordAsync(RecordMappingRequest request, CancellationToken ct)`

- `INachaFieldMappingEngine`
  - `Task<FieldMappingResult> MapFieldAsync(FieldMappingRequest request, CancellationToken ct)`

- `IFieldSourceResolver`
  - `Task<SourceResolutionResult> ResolveAsync(FieldRuntimePlan plan, SourceEnvelope src, CancellationToken ct)`

- `INachaCanonicalMapper`
  - `CanonicalResolution Resolve(string recordCode, string keyOrAlias)`

- `IFieldTransformationEngine`
  - `Task<TransformedValueResult> ApplyAsync(object? value, CompiledTransformPipeline pipeline, TransformContext ctx, CancellationToken ct)`

- `IFieldValidationEngine`
  - `Task<FieldValidationResult> ValidateAsync(object? value, CompiledFieldRules rules, ValidationContext ctx, CancellationToken ct)`

- `IFieldFallbackEngine`
  - `Task<FallbackResult> ApplyAsync(FieldPipelineState state, CompiledFallbackPolicy policy, FallbackContext ctx, CancellationToken ct)`

- `IExpressionDslCompiler` / `IExpressionDslExecutor`

- `IFieldMappingPlanCompiler`
  - compila `Cfg*` a runtime plan inmutable/cachable.

Soporte:
- `IMappingAuditWriter`
- `IRecordContextProvider`
- `IMappingFeatureGate`

## 4) Fuentes de datos que se soportarán realmente

### Fase inicial (sí)

1. `CONSTANTE`.
2. `ENTITY/PropertyPath` (con canonical mapper y accessors precompilados).
3. `EXPRESION` (DSL mínimo acotado).
4. `CONTEXT_VALUE` (ciclo, cámara, flujo, secuencia, totales precomputados).
5. `ALIAS/CANONICAL` global + override por record.
6. `DERIVED_FROM_PRECOMPUTED` (valores calculados por builder/context provider; no SQL por campo).

### Fase posterior (tal vez)

7. `SOURCE_COMPOSITE` (coalesce de múltiples fuentes primarias).
8. `FIELD_REFERENCE` (valor de otro campo ya resuelto dentro del mismo record, sin ciclos).

### No entra en engine fase 1

9. `SQL_VIEW` por campo.
10. `SQL_PROCEDURE` por campo.

Justificación de exclusión SQL field-level en fase 1:
- alto costo por registro/campo,
- trazabilidad y seguridad más complejas,
- riesgo de introducir latencia/variabilidad operativa.

## 5) Diseño del `ExpressionDsl`

### 5.1 Sintaxis

JSON AST estricto:

```json
{ "op": "coalesce", "args": [
  { "op": "prop", "path": "transaction.receiverCustomerCode" },
  { "op": "ctx", "key": "DefaultReceiverCode" },
  { "op": "const", "value": "0000000000" }
]}
```

Ops fase 1:
- `const`, `prop`, `ctx`, `fieldRef`
- `coalesce`, `default`, `concat`, `substring`
- `if` (cond simple `eq`, `isNullOrEmpty`)
- `trim`, `upper`, `lower`, `replace`

### 5.2 Validación pre-publicación
- JSON schema versionado (`dslVersion`).
- whitelist de `op`.
- límites: profundidad <= 8, nodos <= 64.
- validación de referencias (`prop`, `ctx`, `fieldRef`).
- detección de ciclos en `fieldRef`.

### 5.3 Ejecución runtime
- compile -> AST tipado inmutable.
- execute -> evaluator sin IO y sin reflexión en caliente (delegates precompilados).

### 5.4 Errores
- compilación: bloquea publicación (`DSL_COMPILE_ERROR`).
- runtime: dispara fallback (`DSL_RUNTIME_ERROR`) o fail-fast según política.

### 5.5 Endurecimiento/versionado
- campo `dslVersion` obligatorio (inicia en `1`).
- backward compatibility por versión durante ventana de rollout.

## 6) Diseño del `TransformationPipeline`

Representación:

```json
{
  "version": 1,
  "steps": [
    {"type":"trim"},
    {"type":"upper"},
    {"type":"replace","from":"Á","to":"A"},
    {"type":"truncate","length":15}
  ]
}
```

Transforms fase 1:
- `trim`, `upper`, `lower`, `replace`
- `truncate`, `substring`
- `remove_non_digits`
- `decimal_scale`
- `date_normalize`
- `null_to_default`

Orden:
1) source/resolve,
2) DSL (si source=expresión o fallback step lo requiere),
3) pipeline transform,
4) validación post-transform,
5) render fixed-width.

Relación con renderer:
- Pipeline NO reemplaza padding/alineación final del renderer.
- Renderer conserva truncado de seguridad final.

## 7) Diseño del rule engine field-level

### Soporte fase 1
- `REQUIRED`
- `REGEX`
- `RANGE`
- `ENUM`
- `DATE_FORMAT`

### Diferido
- `CHECKSUM` (fase 2)
- `CONDITIONAL` complejo (fase 2)
- `CROSS_FIELD` completo (fase 2/3)

### Momento
- pre-transform: `REQUIRED` opcional.
- post-transform: default para las demás.
- pre-render: validación de longitud lógica.

### Resultado de regla
- `ERROR`: bloquea campo/record según policy.
- `WARN`: continúa, queda auditado.
- `NORMALIZE`: no en fase 1 (evitar mezclar rule con transform).

### Integración
- prepublicación: sintaxis/consistencia de reglas.
- runtime: ejecución de reglas compiladas.
- auditoría: log de ruleCode + outcome + severidad.

## 8) Diseño del fallback engine por campo

`FallbackPolicyJson`:

```json
{
  "version": 1,
  "onRuleError": "fail_fast",
  "strategy": "ordered_steps",
  "steps": [
    {"type":"alias"},
    {"type":"secondary_source","source":{"type":"EXPRESION","expression":{"op":"ctx","key":"DefaultTrace"}}},
    {"type":"default","value":"0000000"}
  ]
}
```

Estrategias fase 1:
- `coalesce`
- `default`
- `fail_fast`
- `null_if_missing`
- alias fallback
- secondary source (CONSTANTE/EXPRESION/CONTEXT_VALUE)

No fase 1:
- fallback silencioso ilimitado,
- SQL field-level fallback,
- fallback legado automático sin feature gate.

Control anti-fallback silencioso:
- todo fallback genera evento de traza.
- umbrales por corrida (si excede X% fallback -> warning global).

## 9) Diseño del canonical model global

Modelo recomendado: **canonical global + overrides por recordCode**.

Componentes:
1. `CanonicalField` (global namespace).
2. `CanonicalAlias` (global + record-scoped).
3. `RecordFieldBinding` (recordCode + fieldCode -> canonicalField).

Reglas:
- colisión alias->canonical diferente: bloqueante.
- alias record-scoped tiene precedencia sobre global.

Integración con `PropertyPath`:
- `PropertyPath` se resuelve contra canonical binding, no por heurística suelta.
- accessors precompilados por `(entityType, canonicalField)`.

Beneficio:
- unifica type 7 con 1/5/6/8/9 y reduce fragilidad por nombres.

## 10) Qué se queda en código vs qué pasa a configuración

### Se queda en código
- cálculo de control 8/9,
- block count,
- totalización crítica,
- reglas estructurales universales,
- validaciones regulatorias de muy alto riesgo transversal,
- integridad crítica/safety rails.

### Pasa a configuración
- source mapping field-level,
- alias/canonical mapping gobernado,
- DSL mínimo,
- transform pipeline,
- reglas declarativas soportadas,
- fallback field-level,
- resolución contextual de layout (ya existente, se mantiene).

Hardcoded residual explícito (fase 1):
- SettlementDate especial record 5 puede seguir en código temporalmente.

## 11) Estrategia de migración por recordCode (1/5/6/7/8/9)

### Fase 0: infraestructura
- crear contratos + plan compiler + telemetry + feature gates.
- ejecutar en shadow compare sin sustituir salida.

### Fase 1: record 6 (primero)
- mayor volumen/transversalidad,
- mayor retorno al eliminar resolución frágil por nombres.

### Fase 2: record 7
- converger engine específico type 7 al engine común,
- mantener rollout policy/fallback gate ya existente.

### Fase 3: records 1 y 5
- preservar construcción de datos críticos en código,
- mover mapping field-level al engine.

### Fase 4: records 8 y 9 parcial
- mantener cálculos críticos en builder,
- mapear fields derivados con engine.

### Fase 5: hardening
- activar rule types diferidos,
- tightening de fallback por perfil/cámara,
- retiro gradual de rutas legacy con métricas.

## 12) Consideraciones de performance y caching

1. Cachear plan compilado por `LayoutVariantId + RowVersion + hash`.
2. Precompilar DSL, pipeline y reglas.
3. Resolver contexto/metadata una vez por corrida.
4. Resolver field pipeline por registro.
5. Evitar reflexión en caliente: delegates compilados y diccionario de accessors.
6. Cero queries field-level repetitivas: carga upfront por variante.
7. Minimizar allocs con estructuras ligeras + pooling de trazas detalladas.
8. Balancear flexibilidad/throughput con límites DSL y feature gates por record.

## 13) Impacto requerido en backend admin y SPA

### Backend admin
- validar/compilar `ExpressionDsl`, `TransformationPipelineJson`, `FallbackPolicyJson` antes de publicar.
- exponer endpoint preview field-level con traza completa.
- impedir guardar configuraciones con runtime no soportado por fase.

### SPA
- formularios reactivos para DSL/pipeline/fallback (JSON asistido).
- tabla AG-GRID (`ui-grilla-empresarial`) de trazas por campo.
- acciones críticas con loading/disabled/anti-doble click.
- textos 100% español.

### No exponer todavía
- rule types diferidos,
- SQL field-level sources,
- opciones de fallback no soportadas en runtime.

## 14) Lista exacta de componentes/archivos que luego habría que crear o modificar

### Crear
- `src/Cfa.ACHInterbank.Application/ACH/Interfaces/Mapping/INachaRecordMappingEngine.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Interfaces/Mapping/INachaFieldMappingEngine.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Interfaces/Mapping/IFieldSourceResolver.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Interfaces/Mapping/INachaCanonicalMapper.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Interfaces/Mapping/IFieldTransformationEngine.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Interfaces/Mapping/IFieldValidationEngine.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Interfaces/Mapping/IFieldFallbackEngine.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Interfaces/Mapping/IExpressionDslCompiler.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Interfaces/Mapping/IExpressionDslExecutor.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Interfaces/Mapping/IFieldMappingPlanCompiler.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Models/Mapping/*.cs` (request/result/trace/errors)
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/Mapping/*` (implementaciones)

### Modificar
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaFileBuilder.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaConfigValidationService.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaConfigPreviewService.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaConfigProfileCommandService.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaConfigProfileQueryService.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Models/NachaConfigAdminDtos.cs`
- `web/ach-interbank-ui/src/app/features/nacha-config-admin/models/nacha-config-admin.models.ts`
- `web/ach-interbank-ui/src/app/features/nacha-config-admin/services/nacha-config-api.service.ts`
- `web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-profile-workspace-page.component.ts`
- `web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-profile-workspace-page.component.html`

## 15) Hallazgos residuales por severidad

### Crítico
- habilitar configuración sin plan compiler/validator fuerte reintroduce inconsistencia “modelo > runtime”.

### Alto
- no converger type 7 al engine común perpetúa doble ruta técnica.
- introducir SQL field-level temprano puede degradar throughput y trazabilidad.

### Medio
- curva de adopción del DSL/pipeline sin tooling guiado.
- incremento temporal de complejidad operativa durante fases híbridas.

### Bajo
- sobrecarga moderada de DTOs/admin UI para soportar nuevas capacidades.

## 16) Veredicto final

**Listo para implementar fase 1 del mapping engine** con guardrails:

1. iniciar por record 6,
2. shadow compare obligatorio,
3. feature gate por record/profile/cámara,
4. no habilitar SQL field-level en fase 1,
5. mantener cálculos críticos 8/9 en código,
6. auditoría field-level obligatoria en modo diagnóstico/rollout.

