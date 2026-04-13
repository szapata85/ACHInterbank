# Diseño arquitectónico definitivo — Integraciones funcionales Proc_Contrapartidas (2026-04-13)

## 0) Mandato de diseño (no negociable)
Diseñar una solución donde usuarios **no técnicos** configuren de forma autónoma qué información del sistema se envía a `Proc_Contrapartidas`, con UX guiada en español, trazabilidad bancaria y preparación limpia para `Proc_Transacciones`, sin duplicar arquitectura.

---

## 1) Diseño arquitectónico final completo

### 1.1 Principios rectores
1. **Dualidad de capas obligatoria**:
   - Capa técnica interna (exactitud SOAP contractual).
   - Capa funcional visible (lenguaje de negocio).
2. **Single engine, multi-method**: un único motor de mapping versionado para `Proc_Contrapartidas` y `Proc_Transacciones`.
3. **No-code controlado**: sin expresiones libres ni scripting arbitrario; solo reglas y transformaciones permitidas.
4. **Publicación gobernada**: no se publica si no hay cobertura obligatoria + validación verde.
5. **Trazabilidad end-to-end**: cada publicación, preview y ejecución deja huella auditable.

### 1.2 Vista por capas

#### A) Capa técnica interna (backend)
- `SoapMethodDefinition` (método SOAP técnico exacto).
- `SoapMethodParameterDefinition` (parámetro técnico exacto: nombre, tipo, dirección input/output, requerido).
- `SoapTransportSettings` (endpoint, action, timeout, credenciales/token strategy).
- `IntegrationMappingSet` + `IntegrationMappingRule` + `History` (motor versionado reutilizable).
- `IntegrationMappingValidationService` (reglas de publicabilidad).
- `IntegrationMappingPreviewService` (simulación explicable).
- `IntegrationPayloadResolver` (resolución dinámica de valores por parámetro).
- `TypedContractBridge` (conversión del payload resuelto al contrato tipado por método).
- `WscfaachSoapClient` (invocación SOAP + observabilidad).

#### B) Capa funcional visible (frontend+backend)
- `FunctionalFieldCatalog` (fichas en español para cada parámetro técnico).
- `BusinessSourceCatalog` (orígenes en lenguaje negocio).
- `MappingWizardState` (progreso guiado: cobertura, pendientes, bloqueos).
- `UserGuidanceRules` (mensajes accionables no técnicos).

### 1.3 Patrón canonical interno
Para cada método SOAP:
1. Definición técnica exacta (contrato real).
2. Definición funcional amigable (etiqueta, descripción, ejemplo, categoría, ayudas).
3. Reglas de mapping (fuente + transformación + fallback + prioridad controlada).
4. Resolución y validación.
5. Publicación y ejecución.

---

## 2) Flujo funcional para usuario no técnico (UX objetivo)

### 2.1 Flujo principal (wizard en español)
**Paso 1 — Seleccionar integración funcional**
- “Enviar contrapartidas a ACH”.
- Mostrar versión activa y fecha de publicación.

**Paso 2 — Cobertura de campos obligatorios**
- Lista por categorías funcionales (identificación, montos, control, respuesta esperada).
- Cada campo muestra:
  - nombre amigable
  - descripción
  - ejemplo
  - estado: Pendiente / Completo / Con alerta

**Paso 3 — Configurar origen de cada campo**
- Opciones guiadas:
  - “Dato de la transacción”
  - “Dato del lote”
  - “Dato del ciclo”
  - “Valor fijo”
  - “Valor por defecto si vacío”
- Nunca mostrar `SourceKind`, `SourceFieldPath`, `ConditionExpression` como eje principal.

**Paso 4 — Reglas simples**
- Transformación seleccionable (catálogo cerrado).
- Sin expresiones libres.
- Prioridad solo cuando existan múltiples reglas justificadas.

**Paso 5 — Validar**
- Resultado de negocio:
  - “Listo para publicar” o
  - “Faltan 3 campos obligatorios”.
- Acciones sugeridas por campo.

**Paso 6 — Preview entendible**
- Tabla: “Campo funcional → valor resultante → de dónde salió”.
- Panel técnico colapsable opcional para auditor.

**Paso 7 — Publicar versión**
- Nota de publicación obligatoria.
- Confirmación explícita de impacto.

**Paso 8 — Historial y comparación**
- Comparar “qué cambió” en lenguaje de negocio.
- Ver hash/snapshot para auditoría.

### 2.2 UX anti-patrones prohibidos
- Pantalla inicial con endpoint/SOAP Action.
- Formulario crudo por `FieldPath` técnico.
- Tablas densas sin guía de cobertura.

---

## 3) Modelo de backend definitivo

### 3.1 Entidades núcleo (reusar y refinar)
1. `IntegrationMethod` (reusar)
   - ampliar con `ContractCode`, `BusinessUseCase`, `IsFunctionalConfigEnabled`.
2. `IntegrationMethodParameter` (reusar)
   - pasar a contrato **exacto** por método (`OFNIT`, `OFEMP`, etc.).
   - nuevos campos: `Direction` (Input/Output), `TechnicalType`, `FunctionalCategory`, `IsUserConfigurable`.
3. `IntegrationSourceCatalogField` (reusar)
   - normalizar naming y origen de datos de negocio.
4. `IntegrationMappingSet`, `IntegrationMappingRule`, `IntegrationMappingSetHistory` (reusar)
   - mantener lifecycle + compare + snapshot hash.

### 3.2 Entidades nuevas (agregar)
1. `IntegrationFunctionalFieldMetadata`
   - `MethodId`, `ParameterId`, `FriendlyNameEs`, `DescriptionEs`, `ExampleValue`, `HelpTextEs`, `UiGroup`, `UiOrder`, `SensitivityLevel`.
2. `IntegrationBusinessSourceMetadata`
   - `SourceCatalogFieldId`, `FriendlyNameEs`, `BusinessDescriptionEs`, `Example`, `UiGroup`, `UiOrder`, `AllowedForMethods`.
3. `IntegrationPublishAudit`
   - `MappingSetId`, `PublishedBy`, `PublishNote`, `ValidationSnapshotJson`, `PreviewSnapshotJson`, `PublishedAtUtc`.
4. `IntegrationExecutionAudit`
   - `MethodId`, `MappingSetVersion`, `ResolvedPayloadHash`, `RequestXmlHash`, `ResponseXmlHash`, `ExecutionStatus`, `ErrorCode`, `CorrelationId`.

### 3.3 Contratos tipados exactos

#### Proc_Contrapartidas
- Input exacto: `OFNIT`, `OFEMP`, `OFCTA`, `OFDD`, `OFFECHEFEC`, `OFMONDEB`, `OFMONCRE`, `OFIDARCH`, `OFIDLOT`, `OFST`, `OFIDTX`, `OFIDREVER`, `OFIDEBAPLI`, `OFIDCAMCOMPE`, `OFDIRECCIONIP`, `OFLIBRE`, `OFLIBRE1`, `ANSIDLOTE`, `ANSST`, `ANCLC`, `ANSIDTX`, `ANSIDREVER`.
- Output exacto: `ANSIDLOTE`, `ANSST`, `ANCLC`, `ANSIDTX`, `ANSIDREVER`.

#### Proc_Transacciones (preparación)
- Input/Output definidos en `SoapMethodParameterDefinition` desde ahora.
- `IsFunctionalConfigEnabled = false` inicialmente (sin exponer wizard hasta fase correspondiente).

### 3.4 Validación (reglas mínimas obligatorias)
1. Cada campo input requerido debe tener cobertura activa.
2. Fuente válida para tipo destino.
3. Transformación permitida y compatible.
4. Sin prioridades duplicadas activas por parámetro.
5. Sin reglas huérfanas fuera del catálogo técnico.
6. Bloqueo de publicación si falla cualquier regla de severidad Error.

### 3.5 Transformaciones permitidas (catálogo cerrado)
- `Trim`
- `Uppercase`
- `Lowercase`
- `PadLeft`
- `PadRight`
- `Substring`
- `Concat`
- `DateFormat`
- `NumericFormat`
- `DefaultIfNull`
- `NullIfEmpty`

**No se permiten** transformaciones dinámicas fuera de catálogo ni scripts.

### 3.6 Resolver dinámico + bridge tipado
1. `IntegrationPayloadResolver` resuelve diccionario `parameterName -> value`.
2. `TypedContractBridge` convierte ese diccionario al DTO tipado exacto de método.
3. `SoapSerializer` genera XML exacto según contrato técnico.

### 3.7 Seed técnico/funcional
- Seed técnico: métodos + parámetros exactos + tipos + dirección.
- Seed funcional: metadatos amigables en español (friendlyName, help, ejemplo).
- Seed de fuentes de negocio por dominio (transacción, lote, ciclo, constantes controladas).

---

## 4) Modelo de frontend Angular definitivo

### 4.1 Estructura de módulo `/integraciones`
- `pages/integration-home` (selección de integración funcional)
- `pages/mapping-wizard` (flujo guiado principal)
- `pages/mapping-coverage` (estado por categorías)
- `pages/mapping-preview` (resultado explicable)
- `pages/mapping-history` (auditoría)
- `pages/mapping-compare` (diferencias de versiones)
- `pages/soap-settings-admin` (solo técnico/admin avanzado, fuera del flujo principal)

### 4.2 Componentes clave
- `FunctionalFieldCardComponent`
- `SourceSelectorBusinessComponent`
- `TransformationSelectorControlledComponent`
- `CoverageProgressPanelComponent`
- `ValidationIssueBusinessComponent`
- `PublishConfirmationDialogComponent`

### 4.3 Contrato UI-backend (view models)
- `FunctionalParameterVm`
  - `friendlyNameEs`, `descriptionEs`, `exampleValue`, `category`, `required`, `coverageStatus`.
- `BusinessSourceVm`
  - `sourceLabelEs`, `sourceDescriptionEs`, `example`.
- `RuleEditorVm`
  - opciones simplificadas, sin campos técnicos crudos por defecto.

### 4.4 Navegación y permisos
- Usuario funcional: acceso a wizard + cobertura + preview + publicar.
- Usuario técnico/admin: acceso adicional a configuración SOAP avanzada.
- Separar `CanManageIntegrations` de `CanManageUsers`.

---

## 5) Qué se reutiliza (máximo)

### Backend (reusar)
- `IntegrationMappingSetService` (draft/update/rules/publish/clone/history/compare).
- `IntegrationMappingValidationService` (estructura base de validación).
- `IntegrationMappingPreviewService` (esqueleto de preview).
- `Integration*` entities + configuraciones EF.
- `WscfaachSoapClient` como gateway de transporte.

### Frontend (reusar)
- `IntegrationMappingAdminService` como base API client.
- páginas de compare/history (ajustando semántica de negocio).
- infraestructura de notificaciones, guards, layout y shared components.

---

## 6) Qué se reemplaza

1. Contratos internos actuales de `Proc_Contrapartidas` por contratos tipados exactos del servicio.
2. Catálogo de parámetros `BuildProcContrapartidasParameterCatalog` por catálogo técnico real (`OF*`, `ANS*`).
3. Resolver/mapper actuales basados en `Transactions/Addendas` por resolver escalar por parámetro técnico.
4. Parser de respuesta actual por parser estricto de output real.
5. Modelo de UI editor técnico por wizard funcional guiado.

---

## 7) Qué se elimina

1. Catch silencioso de fallback en mapeo (`catch {}` sin trazabilidad).
2. Exposición de campos técnicos crudos como flujo principal de negocio.
3. Dependencia de permisos `CanManageUsers` para gobernar integraciones.
4. Cualquier soporte de expresiones arbitrarias fuera del catálogo cerrado.

---

## 8) Proyección limpia hacia Proc_Transacciones (sin duplicación)

### 8.1 Qué se deja listo desde ya
- Método técnico `WSCFAACH.Proc_Transacciones` registrado en mismo metamodelo.
- Parámetros técnicos exactos cargados en catálogo.
- Metadatos funcionales base en español (borrador, no expuesto aún).
- Reuso del mismo pipeline:
  `Catalog -> MappingSet -> Validate -> Preview -> Publish -> Resolve -> TypedContractBridge -> SOAP`

### 8.2 Qué NO se hace ahora
- No habilitar wizard de usuario final para `Proc_Transacciones` en esta fase.
- No crear un segundo motor paralelo.

---

## 9) Lista de archivos/módulos potencialmente afectados

## 9.1 Backend — Dominio/Aplicación/Persistencia/API
- `src/Cfa.ACHInterbank.Application/ACH/Models/ProcContrapartidasRequestModels.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Interfaces/IProcContrapartidasRequestMapper.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Interfaces/IProcContrapartidasResponseParser.cs`
- `src/Cfa.ACHInterbank.Application/Integrations/Dtos/IntegrationCatalogDtos.cs`
- `src/Cfa.ACHInterbank.Application/Integrations/Dtos/IntegrationMappingSetDtos.cs`
- `src/Cfa.ACHInterbank.Domain/Entities/Integrations/IntegrationMappingModels.cs`
- `src/Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationCatalogService.cs`
- `src/Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationMappingSetService.cs`
- `src/Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationMappingValidationService.cs`
- `src/Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationMappingPreviewService.cs`
- `src/Cfa.ACHInterbank.Persistence/Integrations/Services/ProcContrapartidasFunctionalMappingResolver.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ProcContrapartidasRequestMapper.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ProcContrapartidasResponseParser.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/Seeders/IntegrationMappingScenarioSeeder.cs`
- `src/Cfa.ACHInterbank.Persistence/Security/Services/SoapIntegrationSettingsService.cs`
- `src/Cfa.ACHInterbank.Api/Controllers/IntegrationMappingSetsController.cs`
- `src/Cfa.ACHInterbank.Api/Controllers/SoapIntegrationSettingsController.cs`

## 9.2 Frontend Angular
- `web/ach-interbank-ui/src/app/features/integrations/integrations-routing.module.ts`
- `web/ach-interbank-ui/src/app/features/integrations/pages/integration-workspace.component.html`
- `web/ach-interbank-ui/src/app/features/integrations/pages/mapping-sets-page.*`
- `web/ach-interbank-ui/src/app/features/integrations/pages/mapping-editor-page.*`
- `web/ach-interbank-ui/src/app/features/integrations/pages/mapping-compare-page.*`
- `web/ach-interbank-ui/src/app/features/admin/components/soap-integration-settings.component.*`
- `web/ach-interbank-ui/src/app/core/services/integration-mapping-admin.service.ts`
- (nuevos) `web/ach-interbank-ui/src/app/features/integrations/pages/mapping-wizard/*`
- (nuevos) `web/ach-interbank-ui/src/app/features/integrations/components/functional-*`

---

## 10) Decisiones finales de gobierno técnico
1. Se conserva el motor versionado existente y se corrige su semántica contractual.
2. Se separa por diseño la UX funcional de la configuración técnica SOAP.
3. Se bloquea cualquier diseño de UI centrado en nombres SOAP crudos como experiencia principal.
4. Se habilita una única arquitectura extensible por método para evitar duplicaciones futuras.
5. Se restringe estrictamente la lógica de transformación a catálogo permitido.

