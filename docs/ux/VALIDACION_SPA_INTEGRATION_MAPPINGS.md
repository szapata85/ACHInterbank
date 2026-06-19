# Validacion SPA /integraciones/mappings

Fecha: 2026-06-19

## Resultado esperado

Estado: validable para UAT/local como matriz funcional.

- Titulo visible: `Matriz de campos SOAP`.
- Proc_Transacciones visible: si.
- Proc_Contrapartidas visible: si.
- RegistrarRespuestaTransaccion visible: si.
- Descripcion funcional por servicio visible: si.
- Columnas visibles: Servicio SOAP, Parametro SOAP, Tabla origen, Campo origen, Regla de conversion, Obligatorio, Estado, Ultima actualizacion, Acciones.
- Estados visibles: `Mapeado`, `Sin mapear`, `Inactivo`.
- Fuentes origen visibles en matriz: `NachaHeaders`, `BatchHeaders`, `EntryDetails`, `AddendaRecords`, `BatchControls`, `FileControls`.
- SQL libre: no habilitado.
- Tablas fisicas arbitrarias: no habilitadas.
- Historial: accion secundaria `Ver auditoria`.
- Ruta tecnica del campo: solo en detalle tecnico secundario.
- JSON tecnico: no visible en vista principal.
- Preview/payload: no visible en vista principal.
- Usuario solo consulta: sin acciones de edicion.
- Usuario administrador: acciones de borrador y edicion disponibles.

## Separacion con soap-settings

- `/integraciones/soap-settings` administra endpoint, SOAP Action, estado tecnico y prueba local.
- `/integraciones/mappings` administra la relacion campo-a-campo sistema contra SOAP.
- No se mezcla endpoint dentro de la matriz.
- No se mezcla matriz de campos dentro de soap-settings.

## Validaciones automatizadas

- `npm run build`
- `npm test -- --watch=false --browsers=ChromeHeadless`
- `dotnet build ACHInterbank.sln -c Release`
- `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release`

## Observaciones

La SPA consume endpoints existentes de integraciones, parametros SOAP, catalogo de campos origen, transformaciones y mapping sets. Los GET de catalogo/mapping sets son de lectura; las mutaciones permanecen bajo permiso administrativo.

Productivo: NO-GO.
