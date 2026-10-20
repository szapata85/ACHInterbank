# Arquitectura SPA — Centro de integración CFA

## 1. Resultado de experiencia

La navegación principal deja de estar orientada a tecnología SOAP y pasa a estar orientada a tareas.

Entrada objetivo:

```text
Integraciones
  └── CFA
       ├── Créditos recibidos en CFA
       ├── Débitos originados en CFA
       └── Respuestas de transacciones
```

Nombres técnicos se muestran como información secundaria:

- Créditos recibidos en CFA — `Proc_Transacciones`
- Débitos originados en CFA — `Proc_Contrapartidas`
- Respuestas de transacciones — `RegistrarRespuestaTransaccion`

## 2. Arquitectura frontend objetivo

```text
features/integrations/
├── integrations.routes.ts
├── shell/
│   └── integration-center-shell.component.*
├── data-access/
│   ├── integration-workspace.api.ts
│   ├── integration-contract.api.ts
│   ├── integration-mapping.api.ts
│   └── integration-connection.api.ts
├── state/
│   ├── integration-center.store.ts
│   ├── contract-workspace.store.ts
│   └── mapping-workspace.store.ts
├── presentation/
│   ├── integration-copy.ts
│   ├── mapping-status.presenter.ts
│   └── technical-detail.presenter.ts
├── service-overview/
│   ├── service-cards.component.*
│   └── service-overview.component.*
├── contract/
│   ├── contract-versions.component.*
│   ├── contract-diff.component.*
│   ├── contract-import-dialog.component.*
│   └── contract-parameter-details.component.*
├── mapping/
│   ├── mapping-workspace.component.*
│   ├── mapping-field-list.component.*
│   ├── mapping-field-card.component.*
│   ├── mapping-rule-form.component.*
│   ├── source-picker.component.*
│   ├── transformation-editor.component.*
│   ├── mapping-validation-summary.component.*
│   ├── mapping-preview.component.*
│   └── publish-mapping-dialog.component.*
└── connection/
    ├── connection-overview.component.*
    └── connection-advanced-dialog.component.*
```

No es obligatorio crear exactamente cada archivo en una fase. La división define responsabilidades máximas.

## 3. Estado con signals

No introducir un state framework adicional solamente para este programa.

Usar Angular signals:

```text
API observable/promise
      ↓
feature store
  signal(workspace)
  signal(loading)
  signal(error)
  signal(selectedParameterId)
      ↓
computed(filteredParameters)
computed(completionSummary)
computed(canPublish)
      ↓
presentation components
```

Reglas:

- `computed` para estado derivado;
- no recalcular reglas de backend;
- efectos solo para sincronización necesaria;
- `takeUntilDestroyed` para streams;
- `ChangeDetectionStrategy.OnPush` en componentes de presentación;
- formularios reactivos tipados.

## 4. Read model orientado al usuario

La pantalla no debe ensamblar significado a partir de códigos técnicos.

El backend debe entregar por servicio:

```text
functionalTitle
functionalDescription
technicalMethodName
movesMoney
contractVersionLabel
contractStatus
mappingStatus
mappingCoverage
connectionStatus
canEditMapping
canPublish
canManageContract
canManageConnection
```

Para cada dato requerido por CFA:

```text
parameterId
functionalName
functionalDescription
technicalName
required
categoryLabel
status
sourceSummary
transformationSummary
exampleValue
technicalDetails
```

## 5. Workspace de relación de información

### Vista principal

Desktop:

```text
┌──────────────────────────────────────────────────────────────┐
│ Créditos recibidos en CFA            Configuración 25/26     │
├───────────────────────┬──────────────────────────────────────┤
│ Datos requeridos CFA  │ Configuración del dato seleccionado │
│                       │                                      │
│ ✓ Cuenta receptora    │ Cuenta receptora                     │
│ ✓ Monto               │ Descripción funcional                │
│ ! Tipo de cliente     │                                      │
│ ✓ Identificación      │ Tomar de: [Beneficiario > ...]      │
│                       │ Tratamiento: [Sin cambios]            │
│                       │ Vista previa                          │
└───────────────────────┴──────────────────────────────────────┘
```

Tablet:

- lista como navegación principal;
- edición en `MatDrawer` o panel sustituto;
- nunca forzar una tabla horizontal.

Móvil:

```text
Tipo de cliente
3 de 26

[Descripción]
[Origen del dato]
[Tratamiento]
[Vista previa]

Atrás                     Guardar
```

La acción primaria permanece accesible sin scroll horizontal.

## 6. Lenguaje visible

Usar siempre español funcional.

| Término técnico | Texto visible principal |
|---|---|
| Mapping | Relación de información |
| Mapping set | Versión de configuración |
| Parameter | Dato requerido por CFA |
| Source | Origen del dato |
| Transformation | Tratamiento del dato |
| Endpoint | Dirección del servicio |
| SOAP Action | Acción técnica |
| Live | Envío habilitado |
| DryRun | Simulación sin envío |
| Draft | Borrador |
| Published | Publicada |
| Archived | Archivada |
| Preview | Vista previa |
| Contract | Contrato del servicio |
| Contract version | Versión del contrato |
| Diff | Cambios entre versiones |

No traducir códigos técnicos dentro de la sección avanzada; allí deben conservarse exactamente.

## 7. Divulgación progresiva

### Nivel funcional por defecto

Muestra:

- qué dato necesita CFA;
- de dónde se obtiene;
- qué tratamiento se aplica;
- si está completo;
- ejemplo/vista previa.

### Detalle técnico colapsado

Muestra bajo acción “Ver información técnica”:

- `ParameterPath`;
- XML element name;
- tipo XSD/data type;
- namespace;
- orden;
- `SourceKind`;
- `FieldPath`;
- TransformationCode;
- IDs internos solo si el usuario tiene permiso de soporte.

## 8. Flujo para un campo nuevo

Cuando una nueva versión de contrato contiene parámetros agregados:

```text
Nueva versión de contrato detectada
  ↓
"2 datos nuevos requieren configuración"
  ↓
Abrir datos pendientes
  ↓
Seleccionar origen
  ↓
Seleccionar tratamiento compatible
  ↓
Vista previa
  ↓
Guardar borrador
  ↓
Validar versión completa
```

El SPA no permite publicar si el backend reporta campos requeridos incompletos.

## 9. Importación de contrato

Pantalla avanzada para usuarios autorizados:

1. Seleccionar servicio.
2. Cargar archivo WSDL/XSD local.
3. Mostrar hash y nombre.
4. Ejecutar análisis.
5. Presentar diff humanizado:
   - datos agregados;
   - datos retirados;
   - cambios de obligatoriedad;
   - cambios de tipo;
   - cambios de orden.
6. Crear versión borrador.
7. Completar nombres/descripciones funcionales de campos nuevos.

Nunca enviar automáticamente a runtime desde la pantalla de importación.

## 10. Angular Material obligatorio

Usar componentes Material/CDK en vez de overlays artesanales:

- `mat-card`;
- `mat-nav-list` / `mat-list`;
- `mat-form-field`;
- `mat-select`;
- `mat-autocomplete` para fuentes extensas;
- `mat-radio-group` cuando las opciones sean pocas;
- `mat-expansion-panel` para detalle técnico;
- `mat-dialog` para publicar/importar/confirmar;
- `mat-drawer` para detalle adaptable;
- `mat-chip-set` para estados/filtros;
- `mat-progress-bar`/`mat-progress-spinner`;
- `mat-stepper` para importación y activación;
- CDK `FocusMonitor`, `LiveAnnouncer` o equivalentes cuando aplique.

No crear elementos `div/li` clicables sin semántica de control.

## 11. Branding dinámico

El módulo solo consume tokens del sistema:

```scss
color: var(--color-text);
background: var(--color-surface);
border-color: var(--color-border);

.primary-action {
  background: var(--color-primary);
  color: var(--color-on-primary);
}
```

Estados usan tokens semánticos globales; no deben derivarse del color de marca.

Prohibido:

```scss
background: #0b4a8b;
color: #991b1b;
```

salvo un valor base dentro del sistema global de tokens, no dentro de la feature.

El hardening debe alinear la tipografía Material con la tipografía global seleccionada por el proyecto y eliminar la divergencia Material/feature.

## 12. Responsive

Breakpoints funcionales mínimos:

- compacto: `< 768px`;
- mediano: `768px–1199px`;
- amplio: `>= 1200px`.

No depender solamente de media queries de ancho de viewport; usar CSS Grid/Flex y, cuando aporte valor, container queries.

Matriz mínima de prueba:

- 360×800;
- 390×844;
- 768×1024;
- 1024×768;
- 1366×768;
- 1440×900;
- 1920×1080.

Requisitos:

- sin scroll horizontal de la tarea primaria;
- acciones críticas accesibles;
- dialogs no exceden viewport;
- textos largos no rompen layout;
- campos técnicos largos usan wrapping/copy affordance en sección avanzada.

## 13. Accesibilidad

Objetivo: WCAG 2.2 AA para el flujo de administración.

Requisitos mínimos:

- navegación completa con teclado;
- foco visible;
- orden de foco lógico;
- `MatDialog` restaura foco;
- títulos y landmarks;
- errores vinculados al campo;
- `aria-live` para validación/publicación cuando aplique;
- no usar color como único estado;
- controles con nombre accesible;
- contraste AA con branding claro/oscuro;
- zoom 200 % sin pérdida funcional;
- iconos acompañados de texto/label cuando comuniquen estado.

## 14. Confirmaciones de riesgo

### Publicar configuración

Mostrar:

- servicio;
- versión de contrato;
- cantidad de campos configurados;
- cambios respecto a publicada;
- actor;
- advertencia sobre efecto operativo.

### Activar nueva versión de contrato

Mostrar:

- versión anterior;
- versión nueva;
- campos agregados/retirados/modificados;
- mapping que será activado;
- resultado de validación;
- posibilidad de rollback.

### Habilitar envío real

Mostrar:

- servicio funcional;
- endpoint sanitizado;
- modo actual/nuevo;
- estado del mapping;
- confirmación explícita.

## 15. Deuda que se elimina durante el refactor

Al finalizar:

- `mapping-sets-page.component.ts` no debe conservar responsabilidades de editor, dialogs, presentación y data-access en un solo archivo;
- `mapping-editor-page.component.ts` se divide en workspace + componentes de presentación;
- dialogs hechos con backdrop/aside migran a `MatDialog`/CDK;
- catálogos funcionales no se amplían en TypeScript;
- la pantalla técnica SOAP deja de ser entrada primaria del módulo;
- no quedan términos funcionales en inglés evitables.
