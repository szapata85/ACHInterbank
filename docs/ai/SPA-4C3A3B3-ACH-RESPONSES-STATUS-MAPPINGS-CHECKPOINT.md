# SPA 4C.3A.3b3 — ACH Responses Status Mappings Checkpoint

## Baseline

Baseline obligatorio aplicado:

`ACHInterbank-v30-spa-4C3A3b2-ach-response-manual-review.zip`

Alcance ejecutado: migración frontend/foundation exclusivamente de `/ach-responses/status-mappings`, seguida por una revisión residual y local de `web/ach-interbank-ui/src/app/features/ach-responses` para cerrar 4C.3A.3.

No se avanzó a 4C.3B. No se modificaron backend, migrations, permisos, routing, contracts, `AchResponsesApiService`, modelos, Alias ni NACHA Security.

## Archivos modificados

Superficie funcional:

- `web/ach-interbank-ui/src/app/features/ach-responses/pages/ach-response-status-mappings-page.component.ts`
- `web/ach-interbank-ui/src/app/features/ach-responses/pages/ach-response-status-mappings-page.component.html`
- `web/ach-interbank-ui/src/app/features/ach-responses/pages/ach-response-status-mappings-page.component.scss`
- `web/ach-interbank-ui/src/app/features/ach-responses/pages/ach-response-status-mappings-page.component.spec.ts`

Evidencia/checkpoint agregado:

- `docs/ai/SPA-4C3A3B3-ACH-RESPONSES-STATUS-MAPPINGS-CHECKPOINT.md`

Antes de agregar este checkpoint, la comparación SHA-256 completa contra el baseline reportó exactamente cuatro archivos modificados: los cuatro de `status-mappings`, sin archivos agregados ni eliminados en la feature.

## Métodos/API reales preservados

Antes de modificar se inspeccionó el componente real. `status-mappings` consume únicamente estos métodos de `AchResponsesApiService`:

- `getStatusMappings(filters)`
- `getStatusMapping(id)` — únicamente durante recuperación de conflicto 409 en edición
- `createStatusMapping(request)`
- `updateStatusMapping(id, request)`

No se introdujeron operaciones nuevas.

Aunque `AchResponsesApiService` expone además `setStatusMappingActive(...)` y `getMappingAudit(...)`, la página baseline no los consumía. Por tanto:

- no se inventó un flujo dedicado activate/deactivate;
- no se agregó auditoría a esta página;
- no se agregaron endpoints ni llamadas redundantes.

`AchResponsesApiService` permanece byte-a-byte igual al baseline.

## Campos reales de homologación

Filtros preservados:

- `codigoCamaraCompensacion`
- `tipoRespuesta`
- `activo`

Formulario create/edit preservado:

- `clearingHouseId`
- `responseType`
- `externalCode`
- `externalCause`
- `internalStatusId`
- `externalServiceStatusId`
- `internalStatusName`
- `normalizedCause`
- `normalizedDescription`
- `requiresCause`
- `allowsNotification`
- `priority`
- `effectiveFrom`
- `effectiveTo`
- `isActive`
- `expectedVersion`
- `reason`

No existe un campo de dirección en la implementación actual de esta página/modelo; no se creó.

La grilla conserva sus columnas y valores funcionales existentes: cámara, tipo, estado/causal externos, prioridad, ids/estado interno, causal/descripción normalizadas, requiere causal, permite notificación, activo, vigencias y acción editar.

## Flujo create/edit preservado

### Alta

`newMapping()` conserva:

- `editingId = null`;
- apertura del editor;
- defaults existentes, incluido `responseType = 'Transaccion'`, `isActive = true`, `priority = 0` y `expectedVersion = ''`.

`saveMapping()` conserva validaciones actuales:

- campos requeridos del Reactive Form;
- `reason` requerido con `Validators.minLength(5)`;
- fecha fin no anterior a fecha inicio.

El request conserva normalización/trim y tipos actuales. Para alta, `expectedVersion` continúa resolviendo a `null` cuando está vacío. Se sigue invocando `createStatusMapping(request)`.

### Edición

`editMapping(row)` conserva:

- `editingId = row.id`;
- carga de todos los valores actuales;
- `row.version -> expectedVersion`;
- fechas convertidas al valor de input local sin cambiar el payload posterior.

`saveMapping()` continúa seleccionando `updateStatusMapping(editingId, request)` cuando `editingId` no es `null`. No se convierte update en create, no se genera una versión en frontend y `expectedVersion` sigue viajando en el request.

### Activación/desactivación real

La implementación baseline no tenía un comando UI separado de activación/desactivación. `isActive` es parte del mismo formulario versionado create/edit y se envía mediante `AchResponseMappingWriteRequest` a `createStatusMapping`/`updateStatusMapping`.

Ese comportamiento se preservó exactamente. No se conectó `setStatusMappingActive(...)` porque hacerlo habría creado un flujo inexistente en el baseline.

## Versionamiento y concurrencia

Se conserva el tratamiento explícito de HTTP 409:

1. mostrar warning de concurrencia;
2. ejecutar `loadMappings()`;
3. si existe `editingId`, consultar `getStatusMapping(id)`;
4. conservar el mecanismo baseline que refresca la versión vigente en el formulario mediante `editMapping(...)` cuando la fila correspondiente está disponible;
5. no reintentar automáticamente `updateStatusMapping`;
6. no generar ni alterar versiones desde frontend.

La única modificación del warning fue de lenguaje visible local: “mapping” -> “homologación”. No cambió control de flujo ni datos enviados.

## Duplicados

No existe heurística frontend de duplicados en el baseline y no se agregó ninguna.

Los errores backend distintos de 409 continúan mostrando `error.error.detail` cuando es string. El fallback visible fue humanizado localmente a `No fue posible guardar la homologación.`. Una prueba focal verifica que un detalle backend de duplicado se presenta sin autocorrección ni segundo intento.

## Auditoría

`status-mappings` no expone auditoría en el baseline. No llama `getMappingAudit(...)`, no tiene toggle de auditoría y no se agregó uno en este lote.

La auditoría general de ach-responses permanece en su superficie ya migrada `/ach-responses/audit`, sin cambios en este lote.

## Migración Foundation 4C.1

Se mantiene:

- `app-page-header`;
- `UiGrillaEmpresarial` para el listado administrativo existente.

Se retiraron todos los `<ui-boton>` de `status-mappings` y se adoptaron:

- `MatButtonModule`;
- `MatFormFieldModule`;
- `MatInputModule`;
- `MatSelectModule`;
- `MatCheckboxModule`.

Los controles Material se aplican a filtros, create/edit, checkboxes y acciones `Nueva homologación`, `Guardar`, `Cancelar`, `Buscar` y `Limpiar`.

Los valores internos permanecen iguales. Por ejemplo, `Transaccion` y `Prenota` no fueron renombrados en los values; solo se humanizó texto visible como `Homologación de estados ACH`.

`UiGrillaEmpresarial` ya encapsula los primitives canónicos `UiEstadoCargaComponent`, `UiEstadoErrorComponent` y `UiEstadoVacioComponent`, por lo que loading/error/empty del listado continúan usando la foundation compartida sin introducir una segunda abstracción.

## Estados visibles y ach-response-status.scss

La grilla existente usa renderers DOM (`createAchBooleanBadgeElement`) para:

- requiere causal;
- permite notificación;
- activo.

Se conservaron esos renderers porque `UiGrillaEmpresarial`/AG Grid ya es la infraestructura establecida y agregar Angular cell-renderers solo para `UiEtiquetaEstadoComponent` ampliaría innecesariamente el alcance y el riesgo.

Por esa razón `ach-response-status-mappings-page.component.scss` conserva deliberadamente:

`@use '../styles/ach-response-status';`

`styles/ach-response-status.scss` permanece byte-a-byte igual al baseline. Además sigue teniendo consumidores reales en:

- `ach-response-list-page.component.scss`;
- `ach-response-manual-review-page.component.scss`;
- `ach-response-status-mappings-page.component.scss`.

No se eliminaron `estado-*`, `prioridad-*` ni `estado-pill` mientras continúen respaldando renderers DOM reales.

## Pruebas focales actualizadas

`ach-response-status-mappings-page.component.spec.ts` cubre según capacidades reales del baseline:

- carga inicial;
- loading y empty;
- error de carga;
- filtros y normalización;
- clear filters;
- validaciones obligatorias y fechas;
- create con request exacto, trims, vigencias, booleans, prioridad y `expectedVersion=null`;
- éxito de create con cierre/reset del editor y recarga;
- edit conservando `id`, `version -> expectedVersion` e `isActive`;
- cancel edit;
- 409 con warning, reload, refresh de versión y cero retry del update;
- error backend de duplicado presentado sin autocorrección;
- fallback de error write;
- badges booleanos existentes de la grilla;
- uso de Material;
- permanencia de `UiGrillaEmpresarial`;
- ausencia de `ui-boton` en la página;
- humanización visible sin cambiar el value `Transaccion`.

No se agregaron tests para auditoría ni para un endpoint separado activate/deactivate porque esas capacidades no existen en la página baseline.

## Validación ejecutable realmente intentada

### Diagnóstico de dependencias

```text
node --version
=> v22.16.0

npm --version
=> 10.9.2

node_modules/.bin/ng
=> missing / dependency tree no utilizable
```

Se realizó una única comprobación corta del registry, conforme al límite solicitado:

```text
timeout 12s npm ping
=> exit 124
=> npm notice PING https://registry.npmjs.org/
```

El registry no respondió dentro del timeout. No se repitió `npm ci` ni se dejó una instalación bloqueada.

### Test focal status-mappings

```text
npm test -- --watch=false --browsers=ChromeHeadless \
  --include='src/app/features/ach-responses/pages/ach-response-status-mappings-page.component.spec.ts'
```

Resultado real:

```text
exit 127
sh: 1: ng: not found
```

### Tests ach-responses

```text
npm test -- --watch=false --browsers=ChromeHeadless \
  --include='src/app/features/ach-responses/**/*.spec.ts'
```

Resultado real:

```text
exit 127
sh: 1: ng: not found
```

### Build frontend

```text
npm run build
```

Resultado real:

```text
exit 127
sh: 1: ng: not found
```

No se ejecutó backend.

## Validación sintáctica adicional

TypeScript global 5.8.3 procesó con `transpileModule`:

- `ach-response-status-mappings-page.component.ts`: `0 syntactic diagnostics`;
- `ach-response-status-mappings-page.component.spec.ts`: `0 syntactic diagnostics`.

Esta comprobación es únicamente sintáctica y no sustituye Angular build/tests.

## Validación estática obligatoria

Resultado: PASS.

Comparado contra v30 se confirmó:

- `status-mappings` es la única superficie funcional modificada;
- `ach-responses-routing.module.ts` byte-a-byte igual;
- `ach-responses.module.ts` byte-a-byte igual;
- la ruta `status-mappings` conserva `permissionGuard` + `CanReadAch`;
- `AchResponsesApiService` byte-a-byte igual;
- `ach-responses.models.ts` byte-a-byte igual;
- `PagedResponse<T>` propio de ach-responses permanece intacto;
- backend `src/` byte-a-byte igual: 1475 archivos en baseline y 1475 en resultado;
- list, dashboard, attempts, audit, detail y manual-review byte-a-byte iguales al baseline;
- cero tags `<ui-boton>` en toda `features/ach-responses`;
- las siete superficies de ach-responses conservan `app-page-header`;
- `UiGrillaEmpresarial` permanece en status-mappings;
- `ach-response-status.scss` byte-a-byte igual y conserva consumidores reales;
- no se agregaron hardcoded hex en los cuatro archivos status-mappings modificados;
- no se agregaron imports cross-feature;
- `expectedVersion` continúa en el formulario/request de edición;
- HTTP 409 continúa tratado explícitamente;
- no existe retry automático de create/update;
- no se alteraron endpoints/contracts.

## Deuda deliberadamente pendiente

No imprescindible para cerrar 4C.3A.3 y por tanto diferida a 4C.4 o incremento futuro:

- `ach-response-status.scss` y renderers DOM `estado-pill` continúan siendo deuda de compatibilidad en enterprise grids (`list`, `manual-review`, `status-mappings`);
- migrarlos a Angular cell-renderers solo para usar `UiEtiquetaEstadoComponent` requeriría infraestructura adicional y no corresponde a este micro-lote;
- el bloqueo de dependencias npm impide todavía evidencia ejecutable Angular de los lotes 4C.3A.3.

No queda consumo de `<ui-boton>` en ach-responses.

# CIERRE INTEGRAL 4C.3A.3 — ACH RESPONSES

Superficies migradas a foundation 4C.1 durante 4C.3A.3:

- `/ach-responses`
- `/ach-responses/dashboard`
- `/ach-responses/audit`
- `/ach-responses/:id/notification-attempts`
- `/ach-responses/:id`
- `/ach-responses/manual-review`
- `/ach-responses/status-mappings`

Invariantes de cierre:

- contracts/modelos frontend de API no fueron modificados;
- `AchResponsesApiService` no fue modificado;
- backend no fue modificado;
- routing/permisos no fueron modificados;
- el `PagedResponse<T>` propio de ach-responses no se unificó con el de core;
- detail conserva reproceso gobernado, `commandId`, `expectedVersion` y 409;
- manual-review conserva versiones/concurrencia y reglas de huérfanas;
- status-mappings conserva create/update versionado, validaciones y recuperación 409 existente;
- cero `<ui-boton>` en toda la feature;
- no se creó una segunda foundation, store global, NgRx, endpoint ni shared primitive nuevo.

## Estado final

`ACH_RESPONSES_4C3A_IMPLEMENTED_VALIDATION_PENDING`

No se usa `ACH_RESPONSES_4C3A_VERIFIED` porque Angular tests/build no pudieron ejecutarse con un dependency tree instalado y el registry npm permaneció inaccesible durante la comprobación corta permitida.
