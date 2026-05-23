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
