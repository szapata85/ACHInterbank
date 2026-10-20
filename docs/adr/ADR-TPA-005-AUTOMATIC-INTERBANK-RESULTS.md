# ADR-TPA-005 — El resultado interbancario no es un acto manual

**Estado:** Accepted for implementation planning

## Decisión

No exponer endpoint ni acción UI que permita establecer directamente `Validated`/`Rejected` para una prenotificación. Las acciones humanas permitidas son administrar datos, desactivar/reactivar y solicitar/reintentar una validación.

## Fuentes de estado final

- respuesta/devolución NACHA-M correlacionada;
- certificación/estado técnico soportado por el pipeline vigente;
- aceptación tácita cuando la política efectiva determine que el plazo expiró sin devolución.

## Compatibilidad

Preserva el guardrail actual probado por `CustomerThirdPartyGovernanceTests.Controller_DoesNotExposeManualStatusWriteEndpoint` en cuanto al resultado interbancario; los nuevos endpoints de escritura serán de **maestro**, no de resultado.
