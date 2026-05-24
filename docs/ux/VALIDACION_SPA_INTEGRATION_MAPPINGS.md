# Validacion SPA /integraciones/mappings

Fecha: 2026-05-23

## Resultado

Estado: validada para UAT/local.

- WSCFAACH visible: si.
- WSAXON visible: si.
- Proc_Transacciones visible: si.
- Proc_Contrapartidas visible: si.
- RegistrarRespuestaTransaccion visible: si.
- MonetaryDebitRequest visible: si.
- MonetaryCreditRequest visible: si.
- DifferentialResponseNotification visible: si.
- OutboundRequest visible: si.
- InboundResponse visible: si.
- Fuentes NACHA-M visibles: si (`NachaHeaders`, `BatchHeaders`, `EntryDetails`, `AddendaRecords`, `BatchControls`, `FileControls`).
- SQL libre: no habilitado.
- Tablas fisicas arbitrarias: no habilitadas.
- Scroll horizontal: no detectado.
- Botones cortados: no detectados.

## Evidencia

- `docs/ux/evidencias/integration-mappings-nacha-sources.png`
- `docs/ux/evidencias/integration-mappings-proc-contrapartidas.png`
- `docs/ux/evidencias/integration-mappings-proc-contrapartidas-validation.json`
- `docs/ux/evidencias/integration-mappings-wsaxon-response.png`
- `docs/ux/evidencias/integration-mappings-ux-validation.json`

## Validacion Proc_Contrapartidas

Estado: validada para UAT/local.

- Operacion: `WSCFAACH / Proc_Contrapartidas`.
- Proposito: `MonetaryDebitRequest`.
- Direccion: `OutboundRequest`.
- Fuentes origen controladas: visibles desde catalogo SPA/API.
- Campos destino SOAP/XML: visibles desde catalogo SPA/API.
- `sourceFieldPath`: derivado desde catalogo controlado en el editor; no editable como SQL libre.
- SQL libre: no habilitado.
- Tablas fisicas arbitrarias: no habilitadas.
- Evidencia visual/DOM: `docs/ux/evidencias/integration-mappings-proc-contrapartidas-validation.json`.

## Observaciones

La SPA consume los endpoints existentes de integraciones, parametros destino y catalogo de fuentes. No se modifico backend ni reglas ACH/NACHA-M/CENIT/ROR.

Productivo: NO-GO.

## Validacion editor Proc_Transacciones - 2026-05-23

Ruta validada:

- `/integraciones/mappings/WSCFAACH.Proc_Transacciones/dc1b034b-4de3-4043-93cc-79072bf8a5e9`

Diagnostico:

- El mapping set existe y el API responde 200.
- `WSCFAACH.Proc_Transacciones` se parsea correctamente como `methodCode`.
- Endpoints criticos validados: mapping set, parametros destino, source catalog, transformations e history.
- El error de navegador `A listener indicated an asynchronous response... message channel closed...` no aparecio en Playwright/Chromium limpio; se considera ruido probable de extension cuando aparezca en Brave/Chrome.
- Causa raiz del bloqueo: el editor completaba `loadAll` pero la vista podia permanecer en estado visual de loading sin forzar deteccion de cambios ni exponer error funcional por endpoint.

Correccion:

- `mapping-editor-page.component.ts` ahora valida mismatch entre ruta y mapping set.
- Cada endpoint critico convierte fallas/timeout en error funcional visible.
- `loading` se cierra con `finalize` y se fuerza `ChangeDetectorRef.detectChanges()` al cambiar a `ready` o `error`.
- Si falla el render posterior a la carga, se muestra error visible con `Reintentar` y `Volver al listado`.

Evidencia:

- `docs/ux/evidencias/mapping-editor-proc-transacciones-loaded.png`
- `docs/ux/evidencias/mapping-editor-proc-transacciones-validation.json`

Resultado Playwright:

- `loadingCleared=true`.
- `formVisible=true`.
- `failedRequests=[]` para requests criticos.
- `consoleErrors=[]`.

Productivo: NO-GO.
