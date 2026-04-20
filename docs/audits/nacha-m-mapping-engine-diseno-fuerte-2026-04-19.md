# Diseño fuerte del Mapping Engine NACHA-M (ACH Colombia / CENIT)

Fecha: 2026-04-19
Estado: Propuesta ejecutable incremental compatible con builder híbrido actual.

## 1. Resumen ejecutivo del diseño del mapping engine

Este diseño propone un engine **table-driven real** para la cadena:

`SOURCE -> RESOLVE -> TRANSFORM -> VALIDATE -> FALLBACK -> RENDER_TARGET`

sin reescribir de cero el ecosistema existente.

Decisión central:

- Mantener `NachaConfigResolver` para selección contextual de perfil/layout.
- Mantener `NachaFixedWidthRecordRenderer` como renderer final de ancho fijo.
- Introducir un **Field Mapping Engine** desacoplado que resuelva valor por campo con contratos explícitos y trazabilidad auditable.
- Usar migración por fases: iniciar en record 6, luego converger type 7 al engine común, después 1/5, y por último 8/9 parcialmente (preservando cálculo crítico en código).

Resultado esperado:

- El mapping de campos deja de depender de hardcodes y convenciones implícitas.
- Se habilita `ExpressionDsl`, `TransformationPipelineJson`, `CfgFieldRule`, `FallbackPolicyJson` de forma controlada.
- Se mantiene continuidad operativa (modo híbrido y fallback gobernado por política).

## 2. Arquitectura propuesta del pipeline source -> resolve -> transform -> validate -> fallback -> render

### 2.1 Etapas y responsabilidades

1) **Source stage**
- Responsabilidad: obtener valor bruto desde fuente primaria declarada.
- Input: `FieldRuntimePlan`, `RecordRuntimeContext`.
- Output: `FieldValueResult` (valor + metadatos + estado).
- No hace: formateo fixed-width ni validación semántica global.

2) **Resolve stage (canonical/aliases/property path)**
- Responsabilidad: resolver claves canónicas, aliases y rutas de propiedad de forma tipada.
- Input: valor bruto + catálogo canónico + contexto de record.
- Output: valor resuelto + trazas de alias/resolución.
- No hace: reglas de negocio de control 8/9.

3) **Transform stage**
- Responsabilidad: aplicar pipeline declarativo de transformaciones sobre valor lógico.
- Input: valor resuelto + `TransformationPipelineJson` compilado.
- Output: valor transformado.
- No hace: padding final de renderer.

4) **Validate stage**
- Responsabilidad: ejecutar `CfgFieldRule` soportadas por fase y producir issues.
- Input: valor transformado + reglas compiladas + contexto.
- Output: `FieldValidationResult` (OK/Warnings/Errors).
- No hace: cortar longitud o rellenar caracteres.

5) **Fallback stage**
- Responsabilidad: aplicar política por campo cuando source/transform/validate falla.
- Input: estado previo + `FallbackPolicyJson` compilado.
- Output: valor final lógico + reason codes.
- No hace: fallback de arquitectura completa (eso sigue en builder por fase).

6) **Render target stage**
- Responsabilidad: entregar diccionario final de valores por fieldCode para renderer fixed-width.
- Input: mapa `fieldCode -> value` final por record.
- Output: línea NACHA fixed-width.
- No hace: cálculo crítico de control estructural universal.

### 2.2 Capas Clean Architecture

- **Application**: contratos de engine, modelos de request/result, políticas de observabilidad.
- **Domain**: DTOs de plan compilado y value objects de mapping.
- **Persistence/Infrastructure**: implementación concreta de resolvers, DSL executor, transform/rule/fallback handlers, caches.
- **API/SPA**: administración y preview del plan de mapping.

### 2.3 Integración con piezas existentes

- `NachaConfigResolver`: permanece para elegir perfil/layout contextual.
- `NachaFileBuilder`: se convierte en orquestador por record, delegando field mapping al engine.
- `NachaFixedWidthRecordRenderer`: permanece como frontera de salida fixed-width.
- `NachaSemanticValidator`: permanece para invariantes del archivo final.

## 3. Interfaces/componentes principales del engine

### 3.1 Contratos base

- `INachaRecordMappingEngine`
  - `MapRecordAsync(recordCode, sourceObject, plan, context, ct)`
  - Orquesta field-level y devuelve `RecordMappingResult`.

- `INachaFieldMappingEngine`
  - `MapFieldAsync(fieldPlan, sourceObject, context, ct)`
  - Ejecuta pipeline completo por campo.

- `IFieldSourceResolver`
  - `ResolvePrimaryAsync(fieldPlan, sourceObject, context, ct)`.

- `INachaCanonicalMapper`
  - `ResolveCanonicalKey(recordCode, keyOrAlias)`.
  - `TryResolvePropertyAccessor(recordCode, entityType, canonicalKey)`.

- `IFieldTransformationEngine`
  - `ApplyAsync(value, transformPlan, context, ct)`.

- `IFieldValidationEngine`
  - `ValidateAsync(value, rulesPlan, context, ct)`.

- `IFieldFallbackEngine`
  - `ApplyAsync(fallbackPlan, priorStageResult, context, ct)`.

- `IExpressionDslEngine`
  - `Compile(expressionJson)` y `Evaluate(compiledExpr, context)`.

- `IFieldMappingPlanCompiler`
  - Transforma `Cfg*` en `FieldRuntimePlan` compilado/cachable.

### 3.2 Componentes de soporte

- `IRecordContextValueProvider` (totales, secuencia, cámara, flujo, fecha proceso, etc.).
- `IMappingAuditSink` (persistencia de trazas resumidas / detalladas).
- `IMappingEngineFeatureFlags` (gating por recordCode/perfil/cámara).

### 3.3 Diseño SOLID

- SRP: cada etapa aislada.
- ISP: interfaces pequeñas por etapa.
- DIP: builder depende de `INachaRecordMappingEngine`, no de implementaciones concretas.
- OCP: nuevas transforms/rules/sources se agregan por handlers.

## 4. Fuentes de datos que se soportarán realmente

### Fase inicial (soporte real)

1) `CONSTANTE`
2) `ENTIDAD` + `PropertyPath` (con canonical mapper)
3) `EXPRESION` (DSL mínimo)
4) `CONTEXT_VALUE` (nuevo tipo lógico; no requiere tabla nueva inmediata, puede inferirse por prefijo `ctx.` en DSL)
5) `ALIAS/CANONICAL` para todos los recordCodes

### Diferidas explícitamente

6) `SQL_VIEW` por campo (diferido)
7) `SQL_PROCEDURE` por campo (diferido)

Justificación del diferimiento SQL field-level:

- Coste alto por fila/campo y riesgo de throughput.
- Riesgo de acoplar generación a SQL dinámico no auditado en caliente.
- Se privilegia consistencia con fuentes in-memory del contexto de corrida.

### Descartadas por ahora

- Scripting arbitrario y evaluadores externos pesados.

## 5. Diseño del `ExpressionDsl`

### 5.1 Sintaxis propuesta (JSON simple)

```json
{
  "op": "coalesce",
  "args": [
    { "op": "prop", "path": "transaction.recipientIdNumber" },
    { "op": "ctx", "key": "CycleName" },
    { "op": "const", "value": "0000000000" }
  ]
}
```

Operaciones fase 1:

- `const`
- `prop`
- `ctx`
- `coalesce`
- `concat`
- `substring`
- `default` (alias de coalesce con valor final)
- `if` (condición simple: `equals`, `isNullOrEmpty`)
- `trim`
- `upper`
- `lower`
- `replace`

### 5.2 Validación pre-publicación

- Parser JSON + validación de esquema (`op`, `args`, tipos).
- Profundidad máxima de árbol (ej. 8).
- Cantidad máxima de nodos (ej. 64).
- Lista blanca de operaciones.
- Prohibición de referencias cíclicas a campos.

### 5.3 Ejecución runtime

- Compilación a AST inmutable cacheable por `FieldSourceDefinitionId` + hash.
- Evaluación sin reflexión repetitiva (accessors precompilados por canonical key).

### 5.4 Manejo de errores

- Error de compilación => bloquea publicación.
- Error de ejecución => pasa a fallback stage con código `DSL_EVAL_ERROR`.

### 5.5 Límite de complejidad

- Sin loops.
- Sin funciones definidas por usuario.
- Sin acceso IO/DB.

## 6. Diseño del `TransformationPipeline`

### 6.1 Forma JSON

```json
[
  { "type": "trim" },
  { "type": "upper" },
  { "type": "replace", "from": "Á", "to": "A" },
  { "type": "remove_non_digits" },
  { "type": "truncate", "length": 15 }
]
```

### 6.2 Transformaciones fase 1

- `trim`
- `upper` / `lower`
- `replace`
- `truncate`
- `substring`
- `remove_non_digits`
- `coalesce` (simple)
- `decimal_scale` (ej. *100)
- `date_normalize` (patrones controlados)

### 6.3 Orden de ejecución

1. Valor base resuelto.
2. Pipeline transform.
3. Reglas post-transform.
4. Renderer aplica padding/justificación y longitud final.

### 6.4 Frontera con renderer

- Engine transforma valor lógico.
- Renderer conserva responsabilidad fixed-width (pad/alineación/corte final de seguridad).

## 7. Diseño del rule engine field-level

### 7.1 Soporte fase 1

- `REQUIRED`
- `REGEX`
- `RANGE`
- `ENUM`
- `DATE_FORMAT`

### 7.2 Diferidos fase 2+

- `CHECKSUM`
- `CONDITIONAL` complejo
- `CROSS_FIELD`

### 7.3 Momento de ejecución

- Pre-transform (opcional): `REQUIRED` básico si configura `when=pre`.
- Post-transform (default): `REGEX/RANGE/ENUM/DATE_FORMAT`.
- Pre-render: validación de longitud esperada lógica (no padding).

### 7.4 Reporte de issues

- `ERROR`: bloquea record (según policy de generación).
- `WARN`: no bloquea, queda auditado.

### 7.5 Integración

- Prepublicación: valida sintaxis/configuración de reglas.
- Runtime: ejecuta reglas compiladas por campo.
- Auditoría: guarda rule code, severidad, resultado.

## 8. Diseño del fallback engine por campo

### 8.1 Política JSON propuesta

```json
{
  "strategy": "coalesce",
  "steps": [
    { "type": "alias" },
    { "type": "source_secondary", "source": { "type": "EXPRESION", "expression": { "op": "ctx", "key": "DefaultReceiver" } } },
    { "type": "default", "value": "0000000" }
  ],
  "onValidationError": "fail_fast"
}
```

### 8.2 Soporte fase 1

- `coalesce`
- `default`
- `fail_fast`
- `null_if_missing`
- fallback por alias/canonical
- source secundaria limitada (CONSTANTE o EXPRESION)

### 8.3 No soportado fase 1

- fallback a SQL por campo.
- fallback automático a renderer legado salvo feature flag explícita en mode híbrido por record.

### 8.4 Auditoría

Registrar:

- etapa donde falló,
- paso de fallback aplicado,
- valor final,
- códigos de razón (`SOURCE_MISS`, `RULE_FAIL`, etc.).

## 9. Diseño del canonical model global

### 9.1 Estrategia

- Modelo mixto: **canonical global + overrides por recordCode**.

### 9.2 Estructura

- `CanonicalFieldCatalog` global (e.g. `TraceNumber`, `CompanyId`, `ReceiverId`).
- `RecordCanonicalBinding` (qué canonical aplica a cada record y campo).
- `AliasCatalog` por recordCode y global.

### 9.3 Colisiones

- Detección en compilación de plan:
  - alias -> múltiples canonical (error bloqueante).

### 9.4 Ubicación

- Runtime: servicio `INachaCanonicalMapper`.
- Config: inicialmente bootstrap en código + semillas controladas; luego administrar por backend.

### 9.5 Integración con PropertyPath

- `PropertyPath` se normaliza a canonical key, luego se usa accessor compilado por tipo.
- Se minimiza reflexión directa por campo en caliente.

## 10. Qué se queda en código vs qué pasa a configuración

### Se queda en código (explícito)

- Cálculos críticos de control record 8/9.
- Block count/padding global.
- Invariantes estructurales universales del archivo NACHA.
- Validaciones regulatorias de alto riesgo transversal.
- Lógica de integridad y seguridad operativa.

### Pasa a configuración/engine

- Source mapping field-level.
- Aliases/canonical mapping (con gobernanza).
- ExpressionDsl mínimo.
- Transformation pipeline field-level.
- Reglas declarativas de campo soportadas.
- Fallback policy por campo.
- Selección contextual de layout (ya existente, se mantiene).

## 11. Estrategia de migración por recordCode (1/5/6/7/8/9)

### Fase 0 (preparación técnica)

- Introducir contratos + plan compiler + auditoría de mapping.
- Ejecutar engine en `SHADOW_COMPARE` sin cortar salida actual.

### Fase 1 (record 6 primero)

- Migrar record 6 al engine común.
- Justificación: mayor volumen, mayor beneficio por eliminar convención frágil.

### Fase 2 (record 7 convergencia)

- Reemplazar ruta específica de type 7 por engine común manteniendo alias/canonical existente.
- Preservar rollout policy y shadow compare.

### Fase 3 (record 1 y 5)

- Mantener construcción de DTOs en código, pero mapping de campos en engine.
- SettlementDate especial puede seguir en código temporalmente con deuda técnica explícita.

### Fase 4 (record 8 y 9 parcial)

- Mantener cálculos de control en código.
- Mapping de campos resultantes via engine donde tenga sentido.

### Fase 5 (hardening)

- Activar rule types diferidos gradualmente.
- Reducir fallback legado por profile/layout con métricas.

## 12. Consideraciones de performance y caching

1. Cachear `FieldRuntimePlan` por `LayoutVariantId + version + hash`.
2. Precompilar:
   - AST de ExpressionDsl.
   - pipelines de transform.
   - rules evaluables.
   - accessors tipados por canonical key.
3. Resolver una vez por corrida:
   - profile/layout resolution,
   - catálogos canónicos y alias.
4. Evitar reflexión por campo:
   - usar delegates compilados (Expression<Func<...>>).
5. Minimizar allocs:
   - objetos result reutilizables en pool para trazas cuando no se requiera detalle completo.
6. Evitar roundtrips DB:
   - todo plan de mapping cargado upfront por layout seleccionado.
7. Balance flexibilidad/throughput:
   - límites de complejidad DSL,
   - feature flags por record/profile.

## 13. Impacto requerido en backend admin y SPA

### 13.1 Cambios de modelo/backend

Posibles ajustes:

- `CfgLayoutField.TransformationPipelineJson`: activar validación semántica y compilación.
- `CfgFieldSourceDefinition.ExpressionDsl`: activar validador y compilador.
- `CfgFieldSourceDefinition.FallbackPolicyJson`: activar esquema y ejecución.
- `CfgFieldRule`: usar `RuleTypeId`, `ConditionDsl`, `RuleConfigJson` en runtime (hoy subutilizados).
- Nuevo catálogo opcional para canonical/aliases globales (fase 2).

### 13.2 Nuevas capacidades admin backend

- Endpoint de validación de DSL/pipeline/fallback antes de guardar.
- Preview de mapping por campo (valor resuelto + transform + reglas + fallback).
- Endpoint de diagnóstico por record.

### 13.3 Nuevas capacidades SPA

- Editor guiado (JSON asistido) para DSL/pipeline/fallback.
- Vista de trazabilidad por campo en preview.
- Gestión de canonical/aliases (fase 2).

### 13.4 Qué no exponer aún

- Rule types complejos (`CHECKSUM`, `CROSS_FIELD`) hasta estabilizar fase 1.
- SQL field-level sources.

## 14. Lista exacta de componentes/archivos que luego habría que crear o modificar

### Crear (propuesto)

- `src/Cfa.ACHInterbank.Application/ACH/Interfaces/IMapping/INachaRecordMappingEngine.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Interfaces/IMapping/INachaFieldMappingEngine.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Interfaces/IMapping/IFieldSourceResolver.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Interfaces/IMapping/IFieldTransformationEngine.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Interfaces/IMapping/IFieldValidationEngine.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Interfaces/IMapping/IFieldFallbackEngine.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Interfaces/IMapping/INachaCanonicalMapper.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Interfaces/IMapping/IExpressionDslEngine.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Interfaces/IMapping/IFieldMappingPlanCompiler.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Models/Mapping/FieldRuntimePlan.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Models/Mapping/RecordMappingResult.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Models/Mapping/FieldMappingTrace.cs`

### Modificar (propuesto)

- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaFileBuilder.cs`
  - Integrar `INachaRecordMappingEngine` por record.
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaConfigValidationService.cs`
  - Validación semántica de DSL/pipeline/fallback.
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaConfigProfileQueryService.cs`
  - Exponer metadata nueva útil para admin.
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaConfigProfileCommandService.cs`
  - Guardado/validación de configuraciones nuevas.
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaType7AliasMap.cs`
  - Evolución a canonical global + overrides.
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaConfigPreviewService.cs`
  - Preview enriquecida de mapping por campo.
- `src/Cfa.ACHInterbank.Application/ACH/Models/NachaConfigAdminDtos.cs`
  - DTOs para DSL/pipeline/fallback/canonical preview.

### SPA (propuesto)

- `web/ach-interbank-ui/src/app/features/nacha-config-admin/models/nacha-config-admin.models.ts`
- `web/ach-interbank-ui/src/app/features/nacha-config-admin/services/nacha-config-api.service.ts`
- `web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-profile-workspace-page.component.ts`
- `web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-profile-workspace-page.component.html`

## 15. Hallazgos residuales por severidad

### Crítico

- Riesgo de ambigüedad si se habilita configuración sin compilador/validador fuerte de plan.

### Alto

- Riesgo de performance si se habilita SQL field-level prematuramente.
- Riesgo de drift si type 7 no converge al engine común.

### Medio

- Curva de aprendizaje operativa para DSL/pipeline.
- Necesidad de tooling de preview para evitar errores de configuración.

### Bajo

- Incremento de complejidad en DTOs/admin UI (controlable con fases).

## 16. Veredicto final

**listo para implementar fase 1 del mapping engine**.

Condiciones:

- Iniciar por record 6 en shadow compare.
- No habilitar SQL field-level en fase 1.
- Mantener cálculos críticos 8/9 en código.
- Exigir validación prepublicación de DSL/pipeline/fallback antes de publicar.

