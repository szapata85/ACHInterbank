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
- No se observo guardrail DryRun/Disabled especifico equivalente a `Proc_Contrapartidas`.

## Estado

Parcial. Antes de ejecutar UAT externo, se recomienda agregar guardrail de no transmision para `Proc_Transacciones`.

Productivo: **NO-GO**.
