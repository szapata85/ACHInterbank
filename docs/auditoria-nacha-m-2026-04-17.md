# Auditoría integral del motor NACHA-M (ACH Colombia / CENIT)

Fecha de auditoría: 2026-04-17

## 1) Resumen ejecutivo

El estado actual del motor es **avanzado pero incompleto** para un objetivo enterprise 100% table-driven y cámara-aware.

Hay una base de parametrización real (tablas de definición y layout), pero la construcción efectiva de registros críticos (especialmente tipo 7 y parte de 1/5/8/9) sigue con lógica dura y validaciones normativas en código. Además, el modelo de datos de parametrización no incorpora explícitamente dimensión de cámara/flujo/servicio/versionado, por lo que no puede representar variantes reales ACH/CENIT sin cambios de código o sin sobrecargar semánticamente campos actuales.

## 2) Qué sí está parametrizado

### 2.1 Layout físico de campos (posiciones, longitudes, padding, justificación, formato)
- Existe modelo `NachaRecordLayout` + `NachaRecordField` con:
  - `RecordCode`, `TotalLength`, `Description`.
  - `FieldName`, `StartPosition`, `Length`, `PadChar`, `Justification`, `DbColumn`, `Format`.
- El renderizador usa esos campos para construir registros fixed-width, incluyendo formateo por tipo (`DateTime`, `decimal`, etc.).
- Soporta constantes embebidas vía `DbColumn = "CONST:..."`.

### 2.2 Orden y habilitación de registros
- `NachaRecordDefinitions` define `RecordCode`, `Sequence`, `SourceType`, `SourceName`, `FilterKey`, `IsEnabled`.
- Se cargan y ordenan por `Sequence`; se pueden desactivar registros.

### 2.3 Administración vía API + SPA
- Hay endpoints CRUD para `nacha-layouts` y `nacha-record-definitions`.
- En Angular existe UI para listar/crear/editar/eliminar definiciones y layouts, con formularios reactivos.

### 2.4 Catálogo de concepto/SEC
- `CompanyEntryDescription` parametriza mapeo de concepto (`Term`) a `StandardEntryClassCode`.

## 3) Qué está híbrido

### 3.1 Builder mixto configuración + fallback duro
- El builder recorre definiciones, pero para registros 1/5/8/9 aplica `forceFallback` y usa objetos calculados internamente aun cuando definición no sea `Custom`.
- Para tipo 6 se usa construcción interna (`BuildEntryDetailRecordsAsync`) aunque luego renderiza con layout.
- El tipo 7 no usa layout configurable: se construye con métodos dedicados y posiciones hardcodeadas por tipo de negocio.

### 3.2 Motor de sourcing parcial
- `NachaRecordDataProvider` permite `Entity/View/Procedure`, pero con limitaciones:
  - Entidades soportadas: solo `AchBatch`, `AchTransaction`, `AchTransactionAddenda`.
  - `FilterKey` efectivo: solo `CycleId` y `BatchId`.
  - Sin contrato tipado por cámara/flujo/servicio.

### 3.3 Validación semántica dura sobre salida
- Existe validación semántica robusta post-generación (estructura, secuencia, reglas de addenda), pero codificada con offsets fijos y reglas estáticas.

## 4) Qué está hardcodeado

### 4.1 Reglas críticas del builder
- Switch explícito por códigos `1/5/6/7/8/9`.
- Cálculos de totales, hash, block count, padding con `new string('9', ...)`.
- Asignación de `MULTICREDIT` cuando hay más de un crédito/prenotificación crédito.
- Restricción de SEC a `PPD/CCD` y error si no aplica.
- Longitud exigida de `ReceivingDFI` en tipo 6 (8) para cálculo de dígito de chequeo.

### 4.2 Registro tipo 7 completamente codificado
- `BuildCreditType7Record`, `BuildDebitType7Record`, `BuildReturnType7Record` con:
  - Longitud de registro fija a 106.
  - Posiciones fijas (ej. start 2/4/21/31/82/84/88/100).
  - Reglas de negocio rígidas para addenda 05/99.

### 4.3 Semántica normativa en `NachaSemanticValidator`
- Offsets y longitudes constantes (Batch description, causal devolución, traces, secuencia).
- Reglas estrictas `Rxx/DEV14`, tipo 99 solo para `Return`, etc.

### 4.4 Seeds por defecto
- Definiciones por defecto y seeds iniciales de layouts/fields en código.
- Ajustes de seeder atados a `NachaRecordLayoutId == 1`.

## 5) Qué no soporta ACH/CENIT reales (brechas funcionales)

1. **No hay dimensión explícita de cámara** en `NachaRecordDefinitions`/`NachaRecordLayouts`/`NachaRecordFields`.
2. **No hay dimensión explícita de flujo** (original, prenotificación, devolución, reverso, etc.) a nivel de layout/definición.
3. **No hay versionado normativo** (vigencia por fecha, versión normativa, estado publicado/borrador) en layouts/definiciones.
4. **No hay prioridad/fallback formal por contexto** (cámara+flujo+servicio).
5. **Tipo 7 no es table-driven**; cualquier cambio de formato exige tocar código.
6. **SEC catalogado, pero validación limitada** a `PPD/CCD` en builder.
7. **Sin DSL/reglas parametrizables** para validaciones semánticas: están en C#.
8. **Sin soporte explícito para múltiples layouts por mismo record code según contexto**.

## 6) Debilidades del modelo de datos actual

1. **Modelo insuficiente para variantes contextuales**:
   - Falta `ClearingHouseId/Code`, `FlowType`, `ServiceType`, `EffectiveFrom/To`, `Version`, `Status`.
2. **Falta trazabilidad de cambio funcional** en registros NACHA:
   - Aunque entidades heredan auditabilidad base, no hay historial de versiones de layout/definición con diff semántico ni flujo de publicación.
3. **Sin llaves de unicidad compuestas por contexto** (hoy se asume prácticamente un layout por `RecordCode`).
4. **`DbColumn` es string libre**; riesgo de errores silenciosos y ambigüedad de mapeo.
5. **`FilterKey` abierto** sin catálogo fuerte; proveedor solo entiende dos claves.
6. **No existe modelo de reglas de formato/validación declarativa** (regex, rangos, obligatoriedad condicional por flujo, catálogos externos).

## 7) Debilidades del frontend actual

1. **No hay administración por cámara/flujo/servicio/versionado** (pantallas editan definición/layout “global”).
2. **Terminología parcialmente en inglés** (ej. “NACHA Layouts”, “Custom”, “Entity”, “View”, “Procedure”), incumpliendo 100% español.
3. **Listados usan `app-table` (wrapper AG-Grid)**, no siempre `ui-grilla-empresarial` de forma explícita en el feature NACHA (aunque internamente lo encapsula).
4. **Acciones críticas incompletas en anti doble click**:
   - `save()` sí deshabilita por `saving`.
   - `remove()` no controla loading/disable ni anti doble click explícito.
5. **Sin UX de auditoría funcional**: no muestra historial, versión vigente, quién publicó, diff, ni rollback.
6. **Validaciones frontend básicas** (required/maxLength), sin validaciones semánticas de dominio NACHA-M por cámara/flujo.

## 8) Lista exacta de archivos/tablas afectadas (auditadas)

### Backend / dominio / persistencia
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaFileBuilder.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaFixedWidthRecordRenderer.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaDataLoader.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaRecordDataProvider.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaSemanticValidator.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaRecordDefinitionAppService.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaRecordLayoutAppService.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/Seeders/NachaLayoutSeeder.cs`
- `src/Cfa.ACHInterbank.Persistence/Configuration/NachaRecordDefinitionConfiguration.cs`
- `src/Cfa.ACHInterbank.Persistence/Configuration/NachaFileIdentifierMapConfiguration.cs`
- `src/Cfa.ACHInterbank.Persistence/Configuration/CompanyEntryDescriptionCatalogConfiguration.cs`
- `src/Cfa.ACHInterbank.Persistence/DataBase/AchDbContext.cs`
- `src/Cfa.ACHInterbank.Domain/Models/ACH/NachaRecordDefinition.cs`
- `src/Cfa.ACHInterbank.Domain/Models/ACH/NachaRecordLayout.cs`
- `src/Cfa.ACHInterbank.Domain/Models/ACH/NachaRecordField.cs`
- `src/Cfa.ACHInterbank.Domain/Models/ACH/AchTransactionAddenda.cs`
- `src/Cfa.ACHInterbank.Domain/Entities/Transactions/Enums/TransactionTypeEnum.cs`
- `src/Cfa.ACHInterbank.Api/Controllers/NachaRecordDefinitionsController.cs`
- `src/Cfa.ACHInterbank.Api/Controllers/NachaRecordLayoutsController.cs`

### Frontend Angular
- `web/ach-interbank-ui/src/app/features/ach-cycles/components/nacha-record-definitions.component.ts`
- `web/ach-interbank-ui/src/app/features/ach-cycles/components/nacha-record-definitions.component.html`
- `web/ach-interbank-ui/src/app/features/ach-cycles/components/nacha-layouts.component.ts`
- `web/ach-interbank-ui/src/app/features/ach-cycles/components/nacha-layouts.component.html`
- `web/ach-interbank-ui/src/app/shared/components/table.component.ts`

### Tablas relacionadas identificadas
- `NachaRecordDefinitions`
- `NachaRecordLayouts`
- `NachaRecordFields`
- `NachaFileIdentifierMap`
- `CompanyEntryDescription`
- (contexto operativo) `AchBatches`, `AchTransactions`, `AchTransactionAddendas`, `NachaHeaders`

## 9) Veredicto

**Veredicto final: avanzado pero incompleto.**

### Justificación breve
- **A favor:** existe base de parametrización, CRUD, renderizado por campos, y validación semántica relevante.
- **En contra (crítico):** faltan dimensiones de cámara/flujo/versionado, y el núcleo normativo de registros críticos (sobre todo tipo 7 y reglas semánticas) sigue hardcodeado.

## Hardcodes residuales declarados explícitamente

Sí, hay hardcode residual crítico en:
- Construcción de registros tipo 7.
- Reglas de negocio/semántica en builder y validador.
- Restricciones SEC/DFI y offsets fijos.
- Fallbacks por default definitions y seeds de layout.

