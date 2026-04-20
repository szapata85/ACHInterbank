# Auditoría técnica — Motor de mapping NACHA-M (ACH Colombia / CENIT)

Fecha: 2026-04-19

## 1) Resumen ejecutivo de la auditoría

El proyecto **sí avanzó en gobierno de layouts por configuración**, pero el **mapping engine (source -> resolution -> transformation -> render target)** está en estado **parcial**.

- Hay resolución contextual de perfil/variante por catálogos y vigencia.
- Hay asignación de `PropertyPath` y `CONSTANTE` por campo en `CfgLayoutField` + `CfgFieldSourceDefinition`.
- Hay trazabilidad operativa (histórico de cambios de configuración y traza de generación).
- Sin embargo, gran parte del motor declarado en modelo (`ExpressionDsl`, `SqlObjectName`, `TransformationPipelineJson`, `FallbackPolicyJson`, reglas avanzadas) **no se ejecuta en runtime**.
- El comportamiento real depende de convenciones de nombres y del renderer heredado de reflexión.
- RecordCode 7 tiene más madurez (alias/canonical), pero de manera específica, no transversal.

## 2) Qué partes del mapping engine existen hoy realmente

Sí existen y están operativas:

- **Modelo de configuración**: `CfgProfile`, `CfgLayoutVariant`, `CfgLayoutField`, `CfgFieldSourceDefinition`, `CfgFieldRule`.
- **Resolución contextual** de perfil/layout por cámara/flujo/dirección/servicio/vigencia/prioridad.
- **Adapter runtime** de `CfgLayoutVariant -> NachaRecordLayout` en `NachaFileBuilder.RenderWithResolvedLayoutAsync`.
- **Rendering fixed-width** con truncado, padding, justificación y formato básico.
- **Type 7 table-driven híbrido** con alias/canonical y shadow compare.

No existe (ejecución real) o está incompleto:

- Ejecutor de `ExpressionDsl`.
- Ejecutor de `TransformationPipelineJson`.
- Ejecutor de `SqlObjectName` por campo.
- Ejecutor de `FallbackPolicyJson` por campo.
- Motor uniforme de aliases/canonical para todos los record codes.
- Motor de reglas de campo (`CfgFieldRule`) aplicado al valor durante generación.

## 3) Qué fuentes de datos están soportadas hoy

### Soportadas realmente en runtime de mapping por campo

1. **Constante**
   - Vía `DataSourceType = CONSTANTE` y conversión a `DbColumn = CONST:<valor>`.
2. **PropertyPath**
   - Vía `SourceDefinition.PropertyPath`, resuelto por nombre de propiedad/candidatos normalizados.
3. **Alias de clave** (solo type 7)
   - Vía `NachaType7AliasMap` y expansión/canonical en `NachaType7FieldValueResolver` + alineación del builder.

### Declaradas en modelo pero no ejecutadas en motor de campo

4. **EXPRESION (`ExpressionDsl`)** — no hay evaluador.
5. **SQL_VIEW / SQL_PROCEDURE por campo (`SqlObjectName`)** — no hay uso en render por campo.
6. **Fallback por campo (`FallbackPolicyJson`)** — no hay uso.

Nota: sí existe carga SQL por **record definition legacy** (`NachaRecordDataProvider`), pero eso es sourcing de registros completos, no mapping field-level con `CfgFieldSourceDefinition`.

## 4) Qué transformaciones están soportadas hoy

### Soportadas

- Conversión básica por tipo:
  - `DateTime` -> `ToString(format || "yyyyMMdd")`.
  - `decimal` -> entero en centavos.
  - `bool` -> `1`/`0`.
- Truncado por longitud.
- Padding (`PadLeft/PadRight`) por `Justification` y `PadChar`.
- Regla especial hardcode para tipo 5 `SettlementDate` (normalización Juliana).

### No soportadas (aunque el modelo las sugiera)

- Pipeline de transformaciones configurable (`TransformationPipelineJson`).
- Coalesce configurable.
- Concatenación declarativa.
- Substring declarativo.
- Normalizaciones parametrizables (acentos, unicode, reemplazos) por campo.
- Casts avanzados y máscaras custom distintas del `FormatMask` aplicado en `DateTime`.

## 5) Qué validaciones de mapping existen hoy

### Sí existen

- Validación de configuración prepublicación:
  - Records obligatorios base.
  - Secuencia duplicada.
  - Variante vacía.
  - Solapamiento de campos por posición.
  - Fuente faltante (property/constant/expression no informada).
  - Ambigüedad de layout default.
- Validación semántica del archivo final (estructura y reglas de negocio NACHA en runtime).

### No existen o no se ejecutan en pipeline field-level

- Ejecución de `CfgFieldRule` por valor resuelto (required/regex/range/checksum/cross-field declarativo).
- Validación de `PropertyPath` contra metamodelo tipado previo a publicar.
- Validación de `ExpressionDsl` (parser/compilación).
- Validación de transform pipeline (porque no se ejecuta).

## 6) Qué partes del mapping siguen hardcodeadas

- Selección de flow/direction desde transacciones (`ResolveFlowCode`, `ResolveDirectionCode`).
- Cálculo de registros de control 8/9 y totales en builder.
- Normalización especial de `SettlementDate` type 5.
- Resolución por reflexión y convenciones de nombres en renderer.
- Alias/canonical implementado específicamente para type 7 en código.
- Decisiones de fallback/híbrido por modo (`LEGACY/HYBRID/TABLE_DRIVEN/SHADOW_COMPARE`) en builder.

## 7) Análisis por recordCode (1/5/6/7/8/9)

### Record 1

- Puede usar layout variante resuelta por configuración.
- Datos reales del header siguen saliendo de objeto calculado en código (`FileHeaderRecord.From`).
- Mapping por campo limitado a property/const y renderer básico.

### Record 5

- Similar a 1: layout configurable, datos base construidos en código (`BatchHeaderRecord.From`).
- Tiene regla hardcodeada de normalización para `SettlementDate`.

### Record 6

- Layout configurable + objetos de entrada construidos por builder (`BuildEntryDetailRecordsAsync`).
- Mapping por campo depende de nombres de propiedades y convenciones.
- Sin DSL/transforms declarativos avanzados.

### Record 7 (más avanzado)

- Sí tiene ruta table-driven híbrida con alias/canonical y shadow compare.
- Mejor cobertura para variaciones de nombres de fuente.
- Aun así, transform/validación declarativa de campo no está completa.

### Record 8

- Layout configurable, pero datos de control calculados en código (`BatchControlRecord.From`).
- Alta dependencia de cálculo hardcode de negocio.

### Record 9

- Layout configurable, pero control de archivo/padding y block count se calcula en código.
- Campo por campo sigue limitado al renderer básico.

## 8) Qué tan cierto es hoy que el mapping BD/modelo -> campo NACHA-M ya es parametrizable

**Respuesta graduada:**

- **Sí (alto) para layout estructural**: posiciones, longitud, padding/justificación, selección de variante por contexto y vigencia.
- **Sí (medio) para source simple por campo**: constante + property path (especialmente donde el DTO/objeto ya expone la propiedad exacta).
- **Sí (medio-alto) en type 7** por alias/canonical y política híbrida.
- **No (bajo) para mapping complejo enterprise**: transformaciones declarativas, expresiones, fallback por campo, validaciones avanzadas, canonical global, ejecución de reglas `CfgFieldRule`.

Conclusión: hoy el claim “ya parametriza qué campo del sistema va a qué campo NACHA-M” es **parcial y condicionada**; válida para casos simples y para type 7 más maduro, no para un motor enterprise fully table-driven en todos los record codes.

## 9) Qué parte del mapping puede administrarse hoy desde backend/SPA

### Administrable hoy

- Perfil (metadatos, contexto, vigencia, estado).
- Secuencia de records.
- Variantes (nombre, prioridad, default, vigencia).
- Campos (nombre, posición, longitud, `PropertyPath`, habilitado).
- Reglas (mensaje/código/severidad/habilitado) a nivel administrativo.
- Validación prepublicación, historial y snapshots.

### No administrable de forma efectiva end-to-end

- Transform pipeline ejecutable por campo (aunque existe el atributo en entidad).
- Configuración útil de source type complejo por campo (`SqlObjectName`, `ExpressionDsl`, `FallbackPolicyJson`) con efecto runtime.
- Alias/canonical global para todos los record codes.
- RuleType/ConditionDsl/RuleConfigJson con ejecución real durante rendering.

## 10) Limitaciones y fragilidades principales

1. **Fragilidad por nombres**: `PropertyPath` depende de coincidencia por reflexión + normalización heurística.
2. **Asimetría type 7 vs resto**: canonical/alias robusto solo en 7.
3. **Modelo > ejecución**: hay capacidad declarada en tablas que no tiene intérprete runtime.
4. **Transformaciones insuficientes** para cambios normativos sin código.
5. **Hardcodes de negocio** en builder para controles/cálculos críticos.
6. **Reglas declarativas no ejecutadas** en pipeline de valor.
7. **Riesgo de híbrido ambiguo** si se expande sin un DSL común y un evaluador único.

## 11) Lista exacta de archivos/componentes críticos auditados

Backend / Dominio / Persistencia:

- `src/Cfa.ACHInterbank.Domain/Models/ACH/Config/NachaConfigCoreEntities.cs`
- `src/Cfa.ACHInterbank.Domain/Models/ACH/Config/NachaConfigCatalogEntities.cs`
- `src/Cfa.ACHInterbank.Persistence/Configuration/ACHConfig/NachaConfigCoreConfiguration.cs`
- `src/Cfa.ACHInterbank.Persistence/Configuration/ACHConfig/NachaConfigCatalogConfiguration.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaConfigResolver.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaFileBuilder.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaFixedWidthRecordRenderer.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaType7FieldValueResolver.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaType7AliasMap.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaConfigValidationService.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaSemanticValidator.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaRecordDataProvider.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaConfigPreviewService.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaConfigProfileCommandService.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaConfigProfileQueryService.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/Seeders/NachaConfigBackfillSeeder.cs`

Frontend SPA NACHA Config Admin:

- `web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-profile-workspace-page.component.ts`
- `web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-profile-workspace-page.component.html`
- `src/Cfa.ACHInterbank.Application/ACH/Models/NachaConfigAdminDtos.cs`

Pruebas revisadas como evidencia:

- `tests/Cfa.ACHInterbank.Tests/NachaConfigResolverTests.cs`
- `tests/Cfa.ACHInterbank.Tests/NachaType7AliasMapTests.cs`
- `tests/Cfa.ACHInterbank.Tests/NachaFileBuilderUnitTests.cs`

## 12) Hallazgos residuales por severidad

### Crítico

- Capacidad declarada de mapping avanzado sin ejecución real (`ExpressionDsl`, `TransformationPipelineJson`, `FallbackPolicyJson`, reglas field-level), lo que puede generar falsa sensación de parametrización total.

### Alto

- Dependencia de convenciones de nombre/reflexión para `PropertyPath`.
- Hardcodes de negocio en record 5/8/9 que limitan “config-first”.
- Canonical/alias no global (concentrado en type 7).

### Medio

- Backend/SPA exponen edición administrativa de reglas/campos, pero no todos los parámetros tienen efecto runtime real.
- Riesgo de divergencia entre catálogo de `CatRuleType` y ejecución efectiva.

### Bajo

- Duplicidad técnica de lógica de render/reflexión entre builder interno y renderer.

## 13) Veredicto final

**Veredicto: PARCIAL**.

Recomendación de siguiente paso:

- No conviene ir “solo incremental cosmético”.
- Sí conviene una fase de **diseño fuerte del mapping engine** (DSL mínimo, transform pipeline, rule engine field-level, canonical global multi-record, adapter único), y luego implementación incremental por record code priorizando 6 y controles 8/9 sin romper operación.

