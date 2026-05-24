# Evidencias SOAP Proc_Transacciones

Fecha: 2026-05-21

## Clasificacion funcional

- IntegrationKey: `WSCFAACH`
- OperationKey: `Proc_Transacciones`
- Cliente tecnico: `WscfaachSoapClient`
- Naturaleza: credito monetario.
- Originador funcional: entidad financiera externa.
- CFA: entidad receptora/procesadora.
- Mueve dinero: si.
- Proposito mapping: `MonetaryCreditRequest`.

## Hallazgos

- El request se resuelve en `ProcTransaccionesRequestMapper`.
- Requiere `IntegrationMappingSet` publicado.
- El endpoint y SOAP Action se resuelven desde `SoapIntegrationSettingsService`.
- `IncomingNachaPostProcessingOrchestrator` valida `TransactionIntegrationOperation` y `IntegrationMappingReadiness` antes del payload.
- `ProcTransacciones` queda protegido por `ProcTransacciones:Mode`.
- Modo UAT/local por defecto: `DryRun`, sin transmision externa.
- Modo `Disabled`: bloquea controladamente y no invoca `IWscfaachSoapClient`.
- Modo `Live`: no se habilita por defecto y requiere configuracion formal.

## Estado

`DEF-UAT-SOAP-MAP-002` queda **cerrado tecnicamente** para UAT/local. La evidencia automatizada cubre:

- readiness antes de payload;
- missing mapping bloqueado;
- fallback requerido bloqueado;
- DryRun sin transmision;
- Disabled sin invocacion SOAP;
- trace generado cuando readiness permite payload.

Productivo: **NO-GO**.

## Actualizacion 2026-05-23 - fuente NACHA-M desagregada

Se cerro la brecha de fuente de datos para creditos entrantes originados por entidad financiera externa.

Cambios validados:

- El catalogo controlado de mappings publica `NachaHeaders`, `BatchHeaders`, `EntryDetails`, `AddendaRecords`, `BatchControls` y `FileControls`.
- `ProcTransaccionesRequestMapper` carga el contexto NACHA-M desde `IncomingNachaEntryClassification.EntryDetailId` y `NachaID`.
- Los campos SOAP pueden resolverse desde `EntryDetails`, `BatchHeaders`, `NachaHeaders`, `AddendaRecords`, `BatchControls` y `FileControls`.
- `IntegrationMappingTraceWriter` conserva valores fuente sanitizados mediante `SourceValues`.
- No se habilito SQL libre ni seleccion arbitraria de tablas fisicas.
- No hubo transmision externa.

Pruebas agregadas:

- `MappingSourceCatalog_ShouldExpose_NachaDesagregadoSources`.
- `ProcTransacciones_ShouldMapFields_FromNachaDesagregado`.
- `ProcTransacciones_ShouldPersistTrace_WithNachaSourceValues`.

Estado: **OK tecnico UAT para fuente NACHA-M desagregada en Proc_Transacciones**.

## Actualizacion 2026-05-23 - SOAP Envelope DryRun formal

Se completo la evidencia formal de SOAP Envelope para `Proc_Transacciones` en DryRun:

- `docs/uat/evidencias/soap-integrations/mapping-trace/proc_transacciones/proc_transacciones_envelope_sanitizado.xml`
- `docs/uat/evidencias/soap-integrations/mapping-trace/proc_transacciones/dryrun_result.json`
- `docs/uat/evidencias/soap-integrations/mapping-trace/proc_transacciones/no_external_transmission_report.md`
- `docs/uat/evidencias/soap-integrations/mapping-trace/proc_transacciones/validation_report.md`

Validaciones:

- XML no vacio.
- Contiene operacion `Proc_Transacciones`.
- Se mantiene modo `DryRun/Disabled/Mock` para UAT/local.
- `IWscfaachSoapClient` no se invoca en DryRun.
- No hay transmision externa.
- No contiene secretos ni certificados privados.

Estado: **OK tecnico UAT para evidencia formal de envelope DryRun**.
