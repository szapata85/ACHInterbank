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
