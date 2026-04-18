# Diseño objetivo enterprise del modelo de parametrización NACHA-M (ACH Colombia / CENIT)

Fecha: 2026-04-17  
Autor: Arquitectura de dominio/persistencia/front

## 1. Resumen ejecutivo del diseño objetivo

Se propone un **modelo de configuración NACHA-M centrado en perfiles versionados y publicables por contexto**, donde la selección de layouts deja de depender de `RecordCode` aislado y pasa a resolverse por un **motor contextual**: cámara + flujo + dirección + servicio + vigencia + prioridad.

El diseño separa claramente:

1. **Contexto normativo y operativo** (qué aplica y cuándo aplica).  
2. **Definición de composición del archivo** (secuencia de registros 1/5/6/7/8/9).  
3. **Layout por registro y variante** (campos, posiciones, formato, fuente, reglas).  
4. **Ciclo de vida de configuración** (borrador/publicado/inactivo/archivado, versión, historial, auditoría).  

Resultado: base sólida para refactorizar `NachaFileBuilder`, renderer, semantic validator y SPA sin hardcodes normativos dispersos.

---

## 2. Problemas estructurales del modelo actual que corrige este diseño

1. `RecordCode` como eje casi único, sin semántica contextual suficiente.  
2. Sin dimensión explícita de cámara (ACH/CENIT).  
3. Sin dimensión explícita de flujo (original, prenotificación, devolución, retorno, reverso, rechazo).  
4. Sin gestión formal de vigencias ni publicación normativa.  
5. Sin estrategia determinística robusta para resolver múltiples variantes del mismo registro (ej. tipo 7).  
6. Reglas críticas mezcladas entre tablas y código duro, especialmente tipo 7 y validaciones semánticas.  
7. CRUD actuales sin flujo enterprise de borrador/publicación/historial/rollback funcional.

---

## 3. Modelo objetivo completo de tablas/entidades

> Convención: `Cfg*` = configuración publicada/operativa; `Cat*` = catálogo maestro; `Hist*` = histórico inmutable.

### 3.1 Catálogos base (dimensiones)

1. `CatClearingHouse`
   - `Id` (PK)
   - `Code` (UNQ, ACH, CENIT)
   - `Name`
   - `IsActive`

2. `CatFlowType`
   - `Id` (PK)
   - `Code` (UNQ: ORIGINAL, PRENOTIFICACION, DEVOLUCION, RETORNO, REVERSO, RECHAZO, OTRO)
   - `NameEs`
   - `DirectionDefaultId` (FK opcional a `CatDirection`)
   - `IsActive`

3. `CatDirection`
   - `Id` (PK)
   - `Code` (UNQ: ENTRADA, SALIDA)
   - `NameEs`
   - `IsActive`

4. `CatServiceClass`
   - `Id` (PK)
   - `Code` (UNQ, ej. PPD, CCD, etc.)
   - `NameEs`
   - `ClearingHouseId` (FK nullable para servicio global o específico)
   - `IsActive`

5. `CatRecordCode`
   - `Id` (PK)
   - `Code` (UNQ: 1,5,6,7,8,9)
   - `NameEs`
   - `IsMandatoryBase` (bool)

6. `CatConfigStatus`
   - `Id` (PK)
   - `Code` (UNQ: BORRADOR, PUBLICADO, INACTIVO, ARCHIVADO)
   - `IsEditable`
   - `IsPublishable`

7. `CatDataSourceType`
   - `Id` (PK)
   - `Code` (UNQ: CONSTANTE, ENTIDAD, SQL_VIEW, SQL_PROCEDURE, EXPRESION)
   - `NameEs`

8. `CatRuleType`
   - `Id` (PK)
   - `Code` (UNQ: REQUIRED, REGEX, RANGE, ENUM, DATE_FORMAT, CHECKSUM, CONDITIONAL, CROSS_FIELD)
   - `NameEs`

### 3.2 Núcleo de publicación/versionado

9. `CfgProfile`
   - `Id` (PK)
   - `ProfileCode` (UNQ lógico, ej. `ACH_OUT_ORIGINAL_V1`)
   - `NameEs`
   - `Description`
   - `ClearingHouseId` (FK)
   - `FlowTypeId` (FK)
   - `DirectionId` (FK)
   - `ServiceClassId` (FK nullable)
   - `ContextPriority` (int)
   - `EffectiveFrom` (datetime2)
   - `EffectiveTo` (datetime2 nullable)
   - `StatusId` (FK a `CatConfigStatus`)
   - `VersionMajor` (int)
   - `VersionMinor` (int)
   - `PublishedAt` (datetime2 nullable)
   - `PublishedBy` (nvarchar)
   - `SupersedesProfileId` (FK nullable self)
   - `RowVersion` (timestamp/rowversion)

10. `CfgProfileTag` (opcional pero recomendado)
   - `Id` (PK)
   - `ProfileId` (FK)
   - `TagKey` (ej. `Canal`, `Producto`, `EntidadOrigen`)
   - `TagValue`

### 3.3 Composición de registros por perfil

11. `CfgProfileRecord`
   - `Id` (PK)
   - `ProfileId` (FK)
   - `RecordCodeId` (FK a `CatRecordCode`)
   - `Sequence` (int)
   - `IsEnabled` (bool)
   - `MinOccurs` (int)
   - `MaxOccurs` (int nullable)
   - `SourceStrategy` (enum/string: CUSTOM_BUILDER, TABLE_DRIVEN, MIXED_CONTROLLED)
   - `LayoutVariantId` (FK a `CfgLayoutVariant`)
   - `SemanticRuleSetId` (FK nullable a `CfgRuleSet`)

### 3.4 Variantes/layouts por contexto y registro

12. `CfgLayoutVariant`
   - `Id` (PK)
   - `ProfileId` (FK)
   - `RecordCodeId` (FK)
   - `VariantCode` (ej. `R7_DEVOLUCION_CENIT_V2`)
   - `NameEs`
   - `Description`
   - `Priority` (int)
   - `EffectiveFrom`
   - `EffectiveTo`
   - `StatusId` (FK)
   - `TotalLength` (int, default 106)
   - `SelectionPredicateJson` (json nullable para condiciones avanzadas)
   - `IsDefaultForRecord` (bool)

13. `CfgLayoutField`
   - `Id` (PK)
   - `LayoutVariantId` (FK)
   - `FieldCode` (código técnico estable)
   - `FieldNameEs` (nombre funcional)
   - `StartPosition`
   - `Length`
   - `PadChar`
   - `Justification` (L/R)
   - `FormatMask` (ej. yyyyMMdd)
   - `SortOrder`
   - `IsVisibleInBackoffice`
   - `IsEnabled`
   - `SourceDefinitionId` (FK)
   - `TransformationPipelineJson` (json nullable)

14. `CfgFieldSourceDefinition`
   - `Id` (PK)
   - `DataSourceTypeId` (FK a `CatDataSourceType`)
   - `ConstantValue` (nullable)
   - `EntityName` (nullable)
   - `PropertyPath` (nullable; soporta alias/nesting)
   - `SqlObjectName` (nullable)
   - `ExpressionDsl` (nullable)
   - `ExternalCatalogCode` (nullable)
   - `FallbackPolicyJson` (nullable)

15. `CfgFieldRule`
   - `Id` (PK)
   - `LayoutFieldId` (FK)
   - `RuleTypeId` (FK)
   - `RuleCode` (estable)
   - `ErrorCode`
   - `ErrorMessageEs`
   - `Severity` (ERROR/WARN)
   - `ConditionDsl` (si aplica condicional)
   - `RuleConfigJson` (regex, min/max, catálogo, checksum, etc.)
   - `Order`
   - `IsEnabled`

16. `CfgRuleSet`
   - `Id` (PK)
   - `RuleSetCode` (UNQ)
   - `NameEs`
   - `Description`
   - `Scope` (FILE/BATCH/ENTRY/ADDENDA)

17. `CfgRuleSetRule`
   - `Id` (PK)
   - `RuleSetId` (FK)
   - `RuleTypeId` (FK)
   - `RuleCode`
   - `ConditionDsl`
   - `RuleConfigJson`
   - `ErrorCode`
   - `ErrorMessageEs`
   - `Order`

### 3.5 Auditoría funcional e historial

18. `HistConfigSnapshot`
   - `Id` (PK)
   - `ProfileId` (FK)
   - `VersionMajor`
   - `VersionMinor`
   - `SnapshotType` (DRAFT_SAVE/PUBLISH/ROLLBACK/CLONE)
   - `SnapshotJson` (json completo normalizado del perfil + registros + layouts + campos + reglas)
   - `CreatedAt`
   - `CreatedBy`

19. `HistConfigChange`
   - `Id` (PK)
   - `ProfileId` (FK)
   - `EntityName`
   - `EntityId`
   - `ChangeType` (INSERT/UPDATE/DELETE/PUBLISH/STATUS)
   - `BeforeJson`
   - `AfterJson`
   - `ChangedAt`
   - `ChangedBy`
   - `CorrelationId`

20. `CfgPublishRequest` (workflow de publicación)
   - `Id` (PK)
   - `ProfileId` (FK)
   - `RequestedBy`
   - `RequestedAt`
   - `ApprovedBy` (nullable)
   - `ApprovedAt` (nullable)
   - `Status` (PENDING/APPROVED/REJECTED/CANCELLED)
   - `ValidationReportJson`

---

## 4. Relaciones y llaves del nuevo modelo

### 4.1 Llaves principales
- Todas las tablas `Cfg*`, `Cat*`, `Hist*`: PK surrogate `Id` (int/bigint) + índices únicos naturales donde aplique.

### 4.2 Llaves únicas críticas

1. `CatClearingHouse(Code)` único.  
2. `CatFlowType(Code)` único.  
3. `CatRecordCode(Code)` único.  
4. `CfgProfile(ProfileCode)` único.  
5. `CfgProfile` índice único por contexto+versión:
   - `(ClearingHouseId, FlowTypeId, DirectionId, ServiceClassId, VersionMajor, VersionMinor)`.
6. `CfgProfileRecord` único por `(ProfileId, RecordCodeId, Sequence)`.  
7. `CfgLayoutVariant` único por `(ProfileId, RecordCodeId, VariantCode, VersionStatusLogical)`; adicional único parcial `IsDefaultForRecord = 1` por `(ProfileId, RecordCodeId, vigencia activa)`.  
8. `CfgLayoutField` único por `(LayoutVariantId, FieldCode)` y por `(LayoutVariantId, StartPosition)` para evitar colisiones.

### 4.3 Relaciones cardinales

- `CfgProfile` 1..N `CfgProfileRecord`.  
- `CfgProfileRecord` 1..1 `CfgLayoutVariant` activo (por versión publicada).  
- `CfgLayoutVariant` 1..N `CfgLayoutField`.  
- `CfgLayoutField` 1..1 `CfgFieldSourceDefinition`.  
- `CfgLayoutField` 1..N `CfgFieldRule`.  
- `CfgProfileRecord` 0..1 `CfgRuleSet`; `CfgRuleSet` 1..N `CfgRuleSetRule`.  
- `CfgProfile` 1..N `HistConfigSnapshot` y 1..N `HistConfigChange`.

---

## 5. Dimensiones contextuales soportadas

El modelo soporta explícitamente:

1. **Cámara:** `CfgProfile.ClearingHouseId`.  
2. **Flujo:** `CfgProfile.FlowTypeId`.  
3. **Dirección:** `CfgProfile.DirectionId`.  
4. **Servicio/SEC:** `CfgProfile.ServiceClassId`.  
5. **RecordCode:** `CfgProfileRecord.RecordCodeId` + `CfgLayoutVariant.RecordCodeId`.  
6. **Variante contextual:** `CfgLayoutVariant` con prioridad + predicado.  
7. **Vigencia:** `EffectiveFrom/EffectiveTo` en `CfgProfile` y `CfgLayoutVariant`.  
8. **Estado:** `StatusId` (borrador/publicado/inactivo/archivado).  
9. **Versión:** `VersionMajor/VersionMinor`.  
10. **Prioridad de resolución:** `ContextPriority` + `Priority` de variante.

---

## 6. Diseño de campos y reglas parametrizables

### 6.1 Estructura de campo

`CfgLayoutField` + `CfgFieldSourceDefinition` + `CfgFieldRule` permiten parametrizar:

- nombre funcional (`FieldNameEs`)  
- posición (`StartPosition`)  
- longitud (`Length`)  
- padding (`PadChar`)  
- justificación (`Justification`)  
- formato (`FormatMask`)  
- fuente de dato (`DataSourceTypeId`, `EntityName`, `PropertyPath`, `SqlObjectName`)  
- constante (`ConstantValue`)  
- expresión/alias (`ExpressionDsl`)  
- obligatoriedad (`CfgFieldRule` tipo REQUIRED)  
- regla condicional (`ConditionDsl`)  
- transformaciones (`TransformationPipelineJson`)  
- catálogo externo (`ExternalCatalogCode`)  
- orden (`SortOrder`)  
- visibilidad/uso (`IsVisibleInBackoffice`, `IsEnabled`)  
- validación declarativa (`RuleConfigJson`)  

### 6.2 DSL mínimo recomendado

- `ConditionDsl`: expresiones booleanas acotadas (ej. `flow == 'DEVOLUCION' && tx.type == 'RETURN'`).  
- `ExpressionDsl`: expresiones de transformación seguras (sin ejecución arbitraria).  
- Evaluador sandboxed con funciones whitelisted (`upper`, `padLeft`, `digitsOnly`, `substring`, `coalesce`).

### 6.3 Política para tipo 7

Tipo 7 deja de estar codificado por método y pasa a:

- variantes de `CfgLayoutVariant` por flujo/cámara/servicio;
- campos y reglas en tablas;
- sólo permanecen en código validaciones de seguridad estructural no negociables (ver sección 11).

---

## 7. Diseño de selección contextual de layouts

### 7.1 Algoritmo de resolución (determinístico)

Entrada de contexto runtime:

- `clearingHouse`, `flowType`, `direction`, `serviceClass`, `processingDate`, `recordCode`, señales de transacción/lote.

Pasos:

1. Seleccionar perfiles `CfgProfile` con:
   - `Status = PUBLICADO`
   - vigencia activa (`EffectiveFrom <= fecha < EffectiveTo/null`)
   - match exacto de cámara/flujo/dirección
   - `serviceClass` exacto o `NULL` (fallback controlado)
2. Ordenar perfiles por:
   - exactitud de match de servicio (exacto > null)
   - `ContextPriority` desc
   - versión más nueva (`VersionMajor`, `VersionMinor` desc)
3. Tomar el primer perfil ganador.
4. Dentro del perfil, buscar `CfgLayoutVariant` para `recordCode` con estado PUBLICADO y vigencia activa.
5. Evaluar `SelectionPredicateJson` (si existe) contra contexto extendido.
6. Ordenar candidatos por:
   - predicate true > false
   - `Priority` desc
   - `IsDefaultForRecord` desc
   - versión/fecha de publicación desc
7. Seleccionar único layout; si empate -> error de configuración (bloquea publicación).

### 7.2 Garantías

- No depende de `RecordCode` aislado.  
- Resolución reproducible y auditable (guardar `resolverTraceJson` por ejecución).  
- Bloqueos preventivos en publicación ante ambigüedad.

---

## 8. Diseño de versionado/publicación/auditoría

### 8.1 Estados

- `BORRADOR`: editable, no ejecutable.  
- `PUBLICADO`: ejecutable, inmutable (excepto desactivación).  
- `INACTIVO`: no seleccionable para nuevas ejecuciones, conserva historial.  
- `ARCHIVADO`: retirado operativamente, sólo consulta.

### 8.2 Flujo de ciclo de vida

1. Crear/editar borrador (`CfgProfile` + hijos).  
2. Ejecutar validación pre-publicación (integridad, superposición de vigencia, campos, reglas, no ambigüedad).  
3. Generar `CfgPublishRequest`.  
4. Aprobar y publicar:
   - setear `Status=PUBLICADO`, `PublishedAt/By`
   - crear `HistConfigSnapshot` tipo PUBLISH
   - registrar diffs en `HistConfigChange`.
5. Desactivar/archivar o clonar para nueva versión.

### 8.3 Rollback/clone

- Rollback = crear nuevo borrador clonando snapshot publicado anterior (`SupersedesProfileId` al actual).  
- Nunca editar directamente un perfil ya publicado.

### 8.4 Auditoría funcional obligatoria

Cada cambio debe registrar:

- quién (`ChangedBy`)  
- cuándo (`ChangedAt`)  
- qué cambió (`BeforeJson/AfterJson`)  
- correlación de operación (`CorrelationId`)  
- versión afectada (`ProfileId + VersionMajor/Minor`)

---

## 9. Estrategia de migración desde el modelo actual

### 9.1 Principios

1. Migración incremental sin romper exportación vigente.  
2. Doble lectura temporal (`legacy + nuevo`) con feature flag.  
3. Backfill automático de datos reutilizables.

### 9.2 Fases

**Fase 0 – Preparación**
- Crear nuevas tablas `Cat*`, `Cfg*`, `Hist*` sin tocar motor actual.

**Fase 1 – Backfill inicial**
- Convertir `NachaRecordDefinitions`, `NachaRecordLayouts`, `NachaRecordFields` a un primer `CfgProfile` por contexto default (ej. ACH/ORIGINAL/SALIDA).
- Mapear `CompanyEntryDescription` a `CatServiceClass`/reglas según necesidad.

**Fase 2 – Publicación controlada**
- Publicar primer perfil equivalente funcional al legado.
- Ejecutar pruebas de equivalencia archivo-a-archivo.

**Fase 3 – Resolución híbrida controlada**
- Builder consulta nuevo resolver contextual; si no encuentra perfil, fallback explícito a legacy con evento auditado.

**Fase 4 – Corte definitivo**
- Eliminar fallback legacy tras cobertura de perfiles ACH/CENIT.
- Marcar tablas legacy como read-only y luego deprecarlas.

### 9.3 Reutilización/reemplazo

- Reutilizar datos de `NachaRecordLayouts/Fields` como semilla.  
- Reemplazar semántica de `NachaRecordDefinitions` por `CfgProfileRecord`.  
- Mantener `CompanyEntryDescription` como catálogo auxiliar o migrarlo a catálogos normalizados de servicio.

---

## 10. Diseño orientado a administración desde SPA Angular

### 10.1 Capacidades mínimas de backoffice

1. **Listado de perfiles** con filtros: cámara, flujo, dirección, servicio, estado, vigencia, versión.  
2. **Editor de perfil** (reactivo) con tabs:
   - contexto
   - secuencia de registros
   - variantes por recordCode
   - campos/rules
   - validación pre-publicación
   - historial.
3. **Acciones críticas**: guardar, publicar, clonar, inactivar, archivar, rollback con loading + disabled + anti doble click.  
4. **Historial/auditoría**: diff visual de cambios por versión.

### 10.2 DTOs agregados recomendados

- `NachaProfileListDto` (grilla).  
- `NachaProfileDetailDto` (árbol completo).  
- `NachaLayoutVariantEditorDto`.  
- `NachaPublishValidationReportDto`.  
- `NachaConfigHistoryDto`.

### 10.3 Endpoints funcionales (no CRUD plano)

- `GET /nacha-config/profiles` (filtros avanzados).  
- `GET /nacha-config/profiles/{id}` (detalle agregado).  
- `POST /nacha-config/profiles` (crear borrador).  
- `POST /nacha-config/profiles/{id}/clone`.  
- `POST /nacha-config/profiles/{id}/validate`.  
- `POST /nacha-config/profiles/{id}/publish`.  
- `POST /nacha-config/profiles/{id}/inactivate`.  
- `GET /nacha-config/profiles/{id}/history`.  
- `GET /nacha-config/profiles/{id}/resolver-preview`.

### 10.4 Reglas SPA obligatorias

- UI 100% español.  
- Listados sobre `ui-grilla-empresarial`/AG-GRID.  
- Formularios reactivos.  
- Botones críticos con guardas anti doble click + estado visual.

---

## 11. Qué debe quedar en tablas y qué debe quedar en código

### 11.1 En tablas (configurable)

1. Selección de variantes por contexto.  
2. Secuencia de registros por perfil.  
3. Layout de campos y fuentes de dato.  
4. Reglas declarativas de formato y validación.  
5. Vigencia/estado/versionado/publicación.

### 11.2 En código (no negociable)

1. **Reglas estructurales universales de seguridad**:
   - integridad de longitud total de registro (106 mientras normativa vigente lo exija por cámara/perfil),
   - consistencia de ensamblado,
   - protección contra configuración maliciosa/ambigua.
2. **Motor de evaluación seguro** de DSL (sin ejecución arbitraria).  
3. **Controles de compliance de alto riesgo** (checksums críticos, límites de overflow, saneamiento estricto).
4. **Orquestación transaccional** de publicación y auditoría.

### 11.3 Capa híbrida controlada (temporal)

Durante migración:
- fallback legacy explícito y auditado por evento;  
- fecha de retiro definida por hitos de cobertura.

**Declaración explícita de hardcode residual aceptado:**
- Se mantiene hardcode sólo para invariantes técnicos y de seguridad del motor, no para semántica normativa variante por cámara/flujo.

---

## 12. Lista exacta de tablas/archivos que después habrá que crear o modificar

### 12.1 Tablas nuevas a crear

- `CatClearingHouse`
- `CatFlowType`
- `CatDirection`
- `CatServiceClass`
- `CatRecordCode`
- `CatConfigStatus`
- `CatDataSourceType`
- `CatRuleType`
- `CfgProfile`
- `CfgProfileTag`
- `CfgProfileRecord`
- `CfgLayoutVariant`
- `CfgLayoutField`
- `CfgFieldSourceDefinition`
- `CfgFieldRule`
- `CfgRuleSet`
- `CfgRuleSetRule`
- `CfgPublishRequest`
- `HistConfigSnapshot`
- `HistConfigChange`

### 12.2 Tablas existentes a modificar/reutilizar

- `NachaRecordDefinitions` (migración a `CfgProfileRecord`)
- `NachaRecordLayouts` (migración a `CfgLayoutVariant`)
- `NachaRecordFields` (migración a `CfgLayoutField`)
- `CompanyEntryDescription` (integración con `CatServiceClass`/reglas)

### 12.3 Archivos backend a crear/modificar (planeados)

- Crear:
  - `src/Cfa.ACHInterbank.Domain/Models/ACH/Config/*` (nuevas entidades)
  - `src/Cfa.ACHInterbank.Persistence/Configuration/ACHConfig/*` (EF mappings)
  - `src/Cfa.ACHInterbank.Application/ACH/Interfaces/INachaConfigResolver.cs`
  - `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaConfigResolver.cs`
  - `src/Cfa.ACHInterbank.Application/ACH/Services/NachaConfigPublicationService.cs`
  - `src/Cfa.ACHInterbank.Api/Controllers/NachaConfigProfilesController.cs`
- Modificar:
  - `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaFileBuilder.cs`
  - `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaFixedWidthRecordRenderer.cs`
  - `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaSemanticValidator.cs`
  - `src/Cfa.ACHInterbank.Persistence/DataBase/AchDbContext.cs`

### 12.4 Archivos frontend a crear/modificar (planeados)

- Crear:
  - `web/ach-interbank-ui/src/app/features/ach-cycles/pages/nacha-config-profiles-page.*`
  - `web/ach-interbank-ui/src/app/features/ach-cycles/pages/nacha-config-profile-editor-page.*`
  - `web/ach-interbank-ui/src/app/features/ach-cycles/pages/nacha-config-history-page.*`
  - `web/ach-interbank-ui/src/app/features/ach-cycles/services/nacha-config-profiles.service.ts`
- Modificar:
  - `web/ach-interbank-ui/src/app/features/ach-cycles/ach-cycles-routing.module.ts`
  - `web/ach-interbank-ui/src/app/core/services/navigation.service.ts`

---

## 13. Hallazgos residuales por severidad

### Crítico

1. Riesgo de ambigüedad en resolución contextual si se permiten predicados libres sin validador de exclusividad.  
2. Riesgo de publicar configuraciones inconsistentes sin pipeline obligatorio de validación previa.

### Alto

1. Complejidad de migración tipo 7 desde hardcode a reglas declarativas sin suite de regresión exhaustiva.  
2. Dependencia de calidad de datos legados para backfill confiable.

### Medio

1. Curva de aprendizaje del DSL de condiciones/expresiones.  
2. Posible sobrecarga operativa inicial del backoffice de configuración.

### Bajo

1. Aumento de cantidad de tablas y necesidad de documentación técnica continua.  
2. Ajustes menores de UX para mantener simplicidad en edición avanzada.

---

## 14. Veredicto final del diseño

**Veredicto: sólido.**

Justificación:

- Corrige de forma directa y explícita las brechas estructurales detectadas (cámara, flujo, servicio, vigencia, publicación, prioridad contextual, auditoría).  
- Define entidades, relaciones, llaves, ciclo de vida y estrategia de migración sin caer en tabla monolítica ambigua.  
- Queda listo para implementación incremental bajo Clean Architecture.

No obstante, **aún no es “enterprise listo para implementación” en términos de ejecución inmediata** porque falta materializar:

1. contratos DSL definitivos,  
2. reglas de validación pre-publicación automatizadas,  
3. plan de pruebas de equivalencia masiva para corte legacy.

Con esos tres entregables técnicos adicionales, el diseño escala a “enterprise listo para implementación”.
