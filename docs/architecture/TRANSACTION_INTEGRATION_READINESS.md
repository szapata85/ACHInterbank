# Transaction Integration Readiness

Fecha: 2026-05-21

Estado: garantia tecnica implementada y cubierta por pruebas automatizadas.

## Objetivo

Garantizar que una transaccion ACH pueda demostrar, antes de XML, DryRun o dispatch SOAP:

- que operacion SOAP le corresponde;
- que proposito de mapping requiere;
- si mueve dinero;
- si los mappings activos estan completos;
- si existe fallback transicional y no debe marcarse como OK pleno.

## Flujo garantizado

`AchTransaction -> ITransactionIntegrationOperationResolver -> IIntegrationMappingReadinessService -> XML/Payload/Response Trace`

La consulta read-only disponible es:

`GET /Transactions/{id}/integration-readiness`

La consulta no transmite, no genera XML, no cambia estado y no crea movimientos monetarios.

## Clasificacion funcional

| Caso | IntegrationKey | OperationKey | MappingPurpose | Direction | MovesMoney |
|---|---|---|---|---|---|
| Debito originado por CFA | WSCFAACH | Proc_Contrapartidas | MonetaryDebitRequest | OutboundRequest | true |
| Credito originado por entidad externa | WSCFAACH | Proc_Transacciones | MonetaryCreditRequest | OutboundRequest | true |
| Respuesta diferencial | WSAXON | RegistrarRespuestaTransaccion | DifferentialResponseNotification | InboundResponse | false |

## Readiness

Estados:

- `Ok`: mappings requeridos activos completos; `usesFallback=false`.
- `Partial`: reservado para fallback opcional no requerido; `usesFallback=true`; no es OK pleno.
- `Failed`: faltan mappings requeridos o el metodo no esta configurado.

Codigos principales:

- `OK`.
- `INTEGRATION_MAPPING_REQUIRED`.
- `INTEGRATION_MAPPING_FALLBACK`.
- `INTEGRATION_METHOD_NOT_CONFIGURED`.
- `INTEGRATION_OPERATION_NOT_SUPPORTED`.

## Guardrails aplicados

- `Proc_Contrapartidas`: valida operacion esperada y readiness antes de construir XML; fallback transicional requerido queda bloqueado con `Failed`.
- `Proc_Transacciones`: valida operacion esperada y readiness antes de construir payload/XML; en UAT/local `ProcTransacciones:Mode=DryRun/Disabled` no transmite externamente.
- `RegistrarRespuestaTransaccion`: valida readiness de `DifferentialResponseNotification`, persiste trace campo-a-campo y solo invoca gateway WSAXON si no faltan mappings requeridos.

## Garantias negativas

- Readiness no invoca SOAP.
- Readiness no cambia estados.
- Readiness no crea movimientos monetarios.
- `RegistrarRespuestaTransaccion` no depende de `IWscfaachSoapClient`.
- `RegistrarRespuestaTransaccion` no resuelve `MonetaryDebitRequest` ni `MonetaryCreditRequest`.

## Actualizacion 2026-05-21 - cierre fallback Proc_Contrapartidas

`Proc_Contrapartidas` ya no puede construir contrato/XML con fallback transicional:

- `ProcContrapartidasRequestMapper.ResolveAsync` falla con `INTEGRATION_MAPPING_REQUIRED` si no existe mapping publicado.
- `ContrapartidaDispatchJobService` rechaza cualquier `ProcContrapartidasRequestResolution` con `UsedFallback=true` antes de llamar `BuildSoapBody`.
- Readiness para `Proc_Contrapartidas` sin mapping publicado devuelve `Failed`, `usesFallback=true`, `canBuildPayload=false` y `REQUIRED_MAPPING_USES_FALLBACK`.
- Ningun campo requerido puede salir de fallback y marcar readiness `Ok`.

## Productivo

Productivo permanece **NO-GO**.
