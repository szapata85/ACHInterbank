# SPA 4C.3A.3b1 — ACH Response Detail + Reproceso Gobernado Checkpoint

## Baseline

Baseline obligatorio aplicado:

`ACHInterbank-v28-spa-4C3A3a-ach-responses-query-surfaces.zip`

El lote se limita exclusivamente a `/ach-responses/:id`. No se avanzó a 4C.3A.3b2/4C.3A.3b3 ni a 4C.3B. No se modificaron backend, routing, `AchResponsesApiService`, modelos/contracts, permisos, Alias ni NACHA Security.

## Archivos modificados

Únicamente se modificaron los cuatro archivos de la superficie detail:

- `web/ach-interbank-ui/src/app/features/ach-responses/pages/ach-response-detail-page.component.ts`
- `web/ach-interbank-ui/src/app/features/ach-responses/pages/ach-response-detail-page.component.html`
- `web/ach-interbank-ui/src/app/features/ach-responses/pages/ach-response-detail-page.component.scss`
- `web/ach-interbank-ui/src/app/features/ach-responses/pages/ach-response-detail-page.component.spec.ts`

Se agregó este checkpoint:

- `docs/ai/SPA-4C3A3B1-ACH-RESPONSE-DETAIL-REPROCESS-CHECKPOINT.md`

## Comportamiento de detalle preservado

La inicialización mantiene el flujo previo:

1. obtiene `id` desde `ActivatedRoute.snapshot.paramMap`;
2. si falta el id, no llama `getDetail` ni `getReprocessAttempts`, marca error y notifica;
3. si existe, ejecuta `loadDetail(id)` y `loadReprocessAttempts(id)`.

Navegación preservada:

- `Volver` -> `/ach-responses`;
- `Ver intentos` -> `/ach-responses/:id/notification-attempts`.

Se conservaron los datos visibles de datos principales, estado/homologación, idempotencia/trazabilidad, intentos incluidos, historial de reprocesos y auditoría. La auditoría continúa mostrando `action`, `occurredAtUtc`, `previousState`, `newState`, `actor`, `reason` y `sanitizedMetadata`.

## Reproceso gobernado preservado

`canReprocess()` conserva exactamente los cinco estados habilitados:

- `ErrorTecnico`
- `PendienteReintento`
- `NoHomologada`
- `ErrorFuncional`
- `Resuelta`

La justificación continúa siendo obligatoria, con mínimo 5 caracteres después de `trim()`, y el valor enviado permanece trimmeado.

Se preservó la generación de `commandId`:

```text
globalThis.crypto?.randomUUID?.()
```

con el mismo fallback basado en `Date.now()` cuando `randomUUID` no está disponible.

Se preservó optimistic concurrency mediante `detail.version`. La invocación continúa enviando:

```text
requestReprocess(detail.id, commandId, detail.version, reason.trim())
```

Éxito preservado:

- notificación de éxito;
- limpieza de `reprocessReason`;
- recarga de detail;
- recarga del historial de reprocesos.

HTTP 409 preservado explícitamente:

- warning de concurrencia;
- recarga de detail;
- el conflicto no se oculta.

Otros errores continúan mostrando `error.error.detail` cuando es string y usan el fallback previo en caso contrario. No se agregaron reintentos automáticos ni un segundo flujo de reproceso.

## Auditoría preservada

`toggleAudit()` mantiene el comportamiento existente:

- solo consulta al abrir;
- al cerrar no llama API;
- si ya existen `auditEntries`, reabrir no vuelve a consultar;
- usa únicamente `getResponseAudit(detail.id)`.

La auditoría permanece read-only y no se agregaron llamadas backend nuevas.

## Migración visual Foundation 4C.1

Se conservaron:

- `app-page-header`;
- `UiEstadoCargaComponent` vía `ui-estado-carga`;
- `UiEstadoErrorComponent` vía `ui-estado-error`.

Se retiró completamente el consumo de `<ui-boton>` en detail y se adoptó `MatButtonModule` para:

- Volver;
- Ver intentos;
- Auditoría;
- Solicitar reproceso;
- Actualizar historial.

La justificación usa ahora `MatFormFieldModule` + `MatInputModule` con `textarea matInput`. Se mantuvo el binding local y no se reescribió el flujo del formulario.

## Estados visuales

Detail dejó de depender del `@use '../styles/ach-response-status'` y de `estado-pill` para los tres grupos de estado objetivo.

Se adoptó `UiEtiquetaEstadoComponent` localmente con los textos existentes y el siguiente mapeo canónico:

- Procesamiento: `Notificada`/`Homologada` -> `exitoso`; `PendienteReintento`/`RequiereRevisionManual`/`NoHomologada` -> `pendiente`; `ErrorFuncional` -> `fallido`; resto -> `inactivo`.
- Notificación: `Exitosa` -> `exitoso`; `Pendiente`/`PendienteReintento`/`RequiereRevisionManual` -> `pendiente`; `ErrorFuncional`/`ErrorTecnico` -> `fallido`; resto -> `inactivo`.
- Reproceso: `Completed` -> `exitoso`; `Pending`/`Running`/`FailedFunctional` -> `pendiente`; `FailedTechnical` -> `fallido`; resto -> `inactivo`.

No se modificó `UiEtiquetaEstadoComponent` global y no se borró `ach-response-status.scss`, porque otras superficies diferidas todavía pueden depender de él.

## Decisión sobre tablas

Se conservaron las tablas HTML existentes para:

- historial de reprocesos;
- intentos de notificación incluidos.

Justificación: son tablas simples ya funcionales, pero este micro-lote contiene un flujo sensible de reproceso con optimistic concurrency. Migrarlas simultáneamente a `MatTable` aumentaba superficie de cambio sin aportar una necesidad funcional. No se introdujeron AG Grid, sorting, filtering, pagination ni abstracciones shared nuevas. Columnas, orden y campos visibles permanecen sin cambios.

## Pruebas focales actualizadas

`ach-response-detail-page.component.spec.ts` se amplió para cubrir:

- init con id y carga de detail + historial;
- init sin id y ausencia de llamadas API;
- navegación a bandeja e intentos;
- lista exacta de estados reprocesables;
- rechazo de estado no permitido;
- rechazo de justificación menor de 5 caracteres;
- request válido con id, commandId, `detail.version` y reason trimmeado;
- éxito con limpieza y recargas;
- HTTP 409 con warning + reload;
- error no-409 con `detail` y fallback;
- textos/mapeos de procesamiento, notificación y reproceso;
- presencia de `UiEtiquetaEstadoComponent` para los tres grupos visibles;
- carga lazy de auditoría, cierre sin API y reapertura con entries sin nueva consulta;
- preservación de metadata técnica.

## Validación ejecutable realmente intentada

### Dependencias / registry

Diagnóstico inicial:

```text
node --version
=> v22.16.0

npm --version
=> 10.9.2

node_modules/.bin/ng
=> no existe / dependency tree no utilizable
```

Se realizó una sola comprobación corta de conectividad, conforme al alcance:

```text
timeout 12s npm ping
=> exit 124
=> npm notice PING https://registry.npmjs.org/
```

El registry no respondió dentro del intervalo. Por ello no se lanzó `npm ci`, evitando repetir el bloqueo de instalación ya observado en 4C.3A.3a.

### Test focal detail

Comando:

```text
npm test -- --watch=false --browsers=ChromeHeadless \
  --include='src/app/features/ach-responses/pages/ach-response-detail-page.component.spec.ts'
```

Resultado real:

```text
exit 127
sh: 1: ng: not found
```

### Tests relacionados ach-responses

Comando:

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

Comando:

```text
npm run build
```

Resultado real:

```text
exit 127
sh: 1: ng: not found
```

No se ejecutó backend.

## Validación estática

Validación estática local confirma:

- `ach-responses-routing.module.ts` byte-a-byte igual al baseline;
- ruta `:id` intacta y protegida por `CanReadAch`;
- `AchResponsesApiService` byte-a-byte igual al baseline;
- `ach-responses.models.ts` byte-a-byte igual al baseline;
- backend completo `src/` byte-a-byte igual al baseline: 1475 archivos verificados por SHA-256;
- `manual-review` byte-a-byte igual al baseline;
- `status-mappings` byte-a-byte igual al baseline;
- cero `<ui-boton>` en detail;
- cero `estado-pill` y cero `@use '../styles/ach-response-status'` en detail;
- tres usos visibles de `ui-etiqueta-estado`: procesamiento, notificación y reproceso;
- `detail.version` sigue enviado a `requestReprocess`;
- `commandId` y fallback siguen presentes;
- HTTP 409 continúa tratado explícitamente;
- cero hardcoded brand hex nuevos en los cuatro archivos detail;
- no se agregaron imports cross-feature;
- no se modificaron otros archivos de código fuera de los cuatro detail.

Como comprobación sintáctica adicional, los dos archivos TypeScript modificados (`component.ts` y `component.spec.ts`) se procesaron con el TypeScript global 5.8.3 mediante `transpileModule` y no reportaron errores sintácticos. Esta comprobación no sustituye Angular build/tests.

## Deuda deliberadamente pendiente

Permanecen fuera de alcance y byte-a-byte iguales al baseline:

- `/ach-responses/manual-review` -> 4C.3A.3b2;
- `/ach-responses/status-mappings` -> 4C.3A.3b3.

No se avanzó a esos lotes.

## Estado final

`ACH_RESPONSE_DETAIL_REPROCESS_IMPLEMENTED_VALIDATION_PENDING`

No se usa `VERIFIED` porque Angular tests/build no pudieron ejecutarse con dependencias instaladas.
