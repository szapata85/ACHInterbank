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
- `Partial`: existe fallback transicional; `usesFallback=true`; no es OK pleno.
- `Failed`: faltan mappings requeridos o el metodo no esta configurado.

Codigos principales:

- `OK`.
- `INTEGRATION_MAPPING_REQUIRED`.
- `INTEGRATION_MAPPING_FALLBACK`.
- `INTEGRATION_METHOD_NOT_CONFIGURED`.
- `INTEGRATION_OPERATION_NOT_SUPPORTED`.

## Guardrails aplicados

- `Proc_Contrapartidas`: valida operacion esperada y readiness antes de construir XML; fallback transicional queda trazado como `Partial`.
- `Proc_Transacciones`: valida operacion esperada y readiness antes de construir payload/XML.
- `RegistrarRespuestaTransaccion`: valida readiness de `DifferentialResponseNotification` antes de invocar gateway WSAXON; si faltan mappings falla controladamente y no llama al gateway.

## Garantias negativas

- Readiness no invoca SOAP.
- Readiness no cambia estados.
- Readiness no crea movimientos monetarios.
- `RegistrarRespuestaTransaccion` no depende de `IWscfaachSoapClient`.
- `RegistrarRespuestaTransaccion` no resuelve `MonetaryDebitRequest` ni `MonetaryCreditRequest`.

## Productivo

Productivo permanece **NO-GO**.
