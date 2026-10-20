# SPA 4C.3B3a — NACHA Upload Checkpoint

## Estado

`NACHA_UPLOAD_4C3B3A_IMPLEMENTED_VALIDATION_PENDING`

La migración frontend de `NachaUploadComponent` quedó aplicada sobre la foundation 4C.1 y la validación estática pasó. Angular test/build no pudieron ejecutarse porque `node_modules/.bin/ng` no existe y el registry npm continuó sin responder dentro del timeout corto permitido.

## Baseline exacto

- `ACHInterbank-v40-spa-4C3B2b3b-bulk-ingestion-detail.zip`
- Superficie objetivo única: `/transactions/nacha-upload`
- Componente: `NachaUploadComponent`
- Ruta/permiso preservados: `nacha-upload`, `CanManageAch`

No se avanzó a Returns/ROR y no se modificaron bulk-ingestion, clearing-houses, ach-cycles, backend, routing, permisos, contracts ni `NachaUploadService`.

## Implementación real inspeccionada

La superficie actual consume únicamente `NachaUploadService` y mantiene dos operaciones:

- `upload(file, clearingHouseId, { forceReprocess, parentIngestionId })`
  - endpoint preservado: `NachaUpload/upload`;
  - payload preservado: `FormData` con `file`, `clearingHouseId` y, solo en reproceso, `forceReprocess=true` + `parentIngestionId`.
- `listRecords(filters)`
  - endpoint preservado: `NachaUpload/records`;
  - filtros actuales: `immediateOrigin`, `immediateDestination`, `referenceCode`, `achCycleId`, `fileCreationDate`.

Formulario de upload preservado:

- `clearingHouseId`: obligatorio;
- `file`: obligatorio.

La carga inicial sigue ejecutando `loadRecords()` desde `ngOnInit()`.

## Archivos modificados

Implementación funcional:

- `web/ach-interbank-ui/src/app/features/transactions/components/nacha-upload/nacha-upload.component.ts`
- `web/ach-interbank-ui/src/app/features/transactions/components/nacha-upload/nacha-upload.component.html`
- `web/ach-interbank-ui/src/app/features/transactions/components/nacha-upload/nacha-upload.component.scss`
- `web/ach-interbank-ui/src/app/features/transactions/components/nacha-upload/nacha-upload.component.spec.ts`

Evidencia:

- `docs/ai/SPA-4C3B3A-NACHA-UPLOAD-CHECKPOINT.md`

La comparación completa de `features/transactions` contra v40, antes de agregar este checkpoint, mostró exactamente cuatro archivos funcionales modificados: los cuatro archivos de `NachaUploadComponent` indicados arriba.

## Lógica TypeScript preservada

El diff de `nacha-upload.component.ts` contra v40 contiene únicamente:

- imports de `RouterModule`, `MatButtonModule`, `MatFormFieldModule` y `MatInputModule`;
- incorporación de esos módulos al array `imports` del componente standalone.

No se modificó ninguna función, condición, regex, request, manejo de respuesta, mensaje funcional ni estado del componente.

Adicionalmente se verificó que el bloque desde `classifyNachaUploadFile(...)` hasta el final del archivo es byte-a-byte equivalente en contenido textual al baseline.

## Validación de archivos preservada

`classifyNachaUploadFile()` permanece exactamente igual.

Continúa reconociendo/aceptando:

- referencia productiva ACH Colombia `RRRRTTT.ZZZ.YYYYMMDD.N.OUT`;
- referencia productiva CENIT `RRRRTTT.ZZZ.YYYYMMDD.N`, sin extensión adicional;
- archivo operativo ACH Colombia `RRRRTTT.ZZZ.N`;
- devolución ACH Colombia `RRRRTTT.ZZZ.RET`;
- sobre digital ACH Colombia `.ENV`;
- fixture UAT/golden interno `.ach`.

Continúa rechazando explícitamente:

- `.txt`;
- `.nacha`;
- extensiones/nombres desconocidos.

No se cambiaron regex, labels funcionales, detalle ni mensajes de rechazo.

## Upload y reproceso preservados

La llamada continúa siendo conceptualmente:

```text
upload(file, clearingHouseId, {
  forceReprocess,
  parentIngestionId
})
```

No se modificó `NachaUploadService`, `FormData` ni endpoint.

La elegibilidad de reproceso continúa exactamente con la regla existente:

1. `ingestionId` presente;
2. `ingestionStatus` en `Fallido` o `Duplicado`;
3. `parsingStatus` en `FallidoReprocesable` o `EnProceso`.

Se preservan:

- cero reproceso automático;
- mismo archivo seleccionado para el reproceso;
- `forceReprocess=true` y `parentIngestionId` del resultado previo;
- notificaciones success/warning/error actuales;
- `loadRecords()` después de respuesta exitosa o error controlado;
- reset del archivo únicamente cuando `!canReprocess`.

No se reinterpretaron códigos HTTP ni estados del backend.

## Resultado preservado

Se mantiene visible la misma información:

- `message`;
- `traceId`;
- `ingestionId`;
- `ingestionStatus`;
- `cycleResolutionStatus`;
- `parsingStatus`;
- cámara seleccionada;
- cámara detectada/resuelta;
- `totalBatches`;
- `totalEntries`;
- `totalAddendas`;
- `errors`;
- `statusLabel`.

La clasificación `Rechazo controlado` permanece intacta.

## Migración foundation 4C.1

Se conservaron:

- `app-page-header`;
- `UiGrillaEmpresarial` para los records persistidos;
- `app-clearing-house-select` y sus bindings actuales;
- input file nativo, sin drag-and-drop ni parsing frontend nuevo.

Se adoptó localmente:

- `MatButtonModule` para cargar, cancelar, reprocesar, buscar y limpiar;
- `MatFormFieldModule` + `MatInputModule` para los filtros de texto/fecha;
- `UiEstadoCargaComponent` durante el upload;
- `RouterModule` para conservar el `routerLink` existente del botón Cancelar al migrarlo a Material.

El template funcional queda con cero `<ui-boton>`.

### CSS

Se sustituyeron hardcodes locales por tokens foundation:

- shadow local -> `var(--shadow-md)`;
- error de archivo -> `var(--color-error-text)`;
- badges success/warning/error -> `--color-*-surface` / `--color-*-text`;
- bloque de errores -> `var(--color-error)` / `var(--color-error-surface)` / `var(--color-error-text)`.

Resultado estático: cero hardcoded hex/rgb en TS/HTML/SCSS funcionales de `NachaUploadComponent`.

## Pruebas focales actualizadas

Solo se actualizó `nacha-upload.component.spec.ts`.

Cubre:

- clasificación ACH Colombia productiva `.OUT`;
- clasificación CENIT productiva sin extensión adicional;
- patrón operativo `RRRRTTT.ZZZ.N`;
- `RRRRTTT.ZZZ.RET`;
- `.ENV`;
- `.ach`;
- rechazo `.txt` / `.nacha`;
- rechazo de formato desconocido;
- carga inicial de records;
- filtros y `clearFilters()`;
- clearing house + archivo obligatorios;
- archivo inválido no llama API;
- upload válido conserva argumentos exactos y recarga records;
- `canReprocess` y reproceso con `forceReprocess + parentIngestionId`;
- cero reproceso automático de un resultado reprocesable;
- permanencia del mismo archivo mientras `canReprocess=true`;
- campos visibles del resultado, incluido `Rechazo controlado`;
- `app-page-header`, `UiGrillaEmpresarial`, Material y cero `<ui-boton>` renderizado.

No se agregaron capacidades que no existieran en el baseline.

## Validación ejecutable real

Entorno observado:

- Node: `v22.16.0`;
- npm: `10.9.2`;
- `node_modules/.bin/ng`: ausente;
- TypeScript global: `5.8.3`.

Se realizó una única comprobación corta del registry:

- comando: `timeout 12s npm ping`;
- exit code: `124`;
- causa: `registry.npmjs.org` no respondió dentro del timeout;
- salida observable: `npm notice PING https://registry.npmjs.org/`.

Intentos reales:

1. `npm test -- --watch=false --browsers=ChromeHeadless --include=src/app/features/transactions/components/nacha-upload/nacha-upload.component.spec.ts`
   - exit code: `127`;
   - causa: `ng: not found`.
2. `npm run build`
   - exit code: `127`;
   - causa: `ng: not found`.

No se ejecutó backend.

Validación sintáctica complementaria con TypeScript global `5.8.3` y `transpileModule(..., reportDiagnostics=true)`:

- `nacha-upload.component.ts`: 0 diagnostics sintácticos;
- `nacha-upload.component.spec.ts`: 0 diagnostics sintácticos.

Esta comprobación no sustituye Angular test/build.

## Validación estática contra v40

PASS antes de agregar este checkpoint.

Se confirmó byte-a-byte sin cambios:

- `NachaUploadService`;
- `transactions-routing.module.ts` y permiso `CanManageAch` de `nacha-upload`;
- todas las superficies B1/B2 fuera de `NachaUploadComponent`;
- Returns y Return-of-Return;
- clearing-houses completo: 17 archivos;
- ach-cycles completo: 18 archivos;
- backend `src/` completo: 1475 archivos.

Comparación de `features/transactions`:

- 79 archivos antes;
- 79 archivos después;
- únicos 4 archivos modificados: TS/HTML/SCSS/spec de `NachaUploadComponent`.

También se confirmó:

- `NachaUploadService` hash preservado: `2513cef057f6e61ce1632edd9bbe440033d2d719ef6b9310b14b16d13244a766`;
- routing hash preservado: `b46e8b240ded22bc88af2bbc33280ed0845baa674e9a9eeb013f3a7bff687150`;
- cero nuevos imports `feature -> feature` en el TS modificado;
- cero hardcoded hex/rgb en TS/HTML/SCSS funcionales del upload;
- cero `<ui-boton>` en el template.

## Pendiente deliberado

Fuera de este lote permanecen, sin modificación:

- Returns/ROR;
- cycle-configs;
- clearing-houses;
- ach-cycles;
- boundaries cross-feature reservados para B9;
- validación Angular ejecutable cuando el entorno vuelva a disponer de dependencias/npm registry.

No se avanzó a Returns/ROR.
