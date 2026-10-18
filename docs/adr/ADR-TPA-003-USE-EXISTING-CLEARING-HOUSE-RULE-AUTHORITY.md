# ADR-TPA-003 — Reutilizar autoridad efectiva `ClearingHouseTransactionRule`

**Estado:** Accepted for implementation planning

## Decisión

Las reglas de obligatoriedad de prenotificación, lead time e identificación deben resolverse desde `ClearingHouseTransactionRule` y sus servicios de política existentes. No crear una segunda fuente paralela en el módulo de terceros.

Si hace falta representar deadline de respuesta, ciclos mínimos, retención/expiración o aceptación tácita, extender la autoridad efectiva con campos genéricos y trazabilidad normativa, después de characterization en TPA0.

## Guardrail

Las referencias históricas V32 del seeder deben auditarse en TPA0 contra ACH Colombia V35. No cambiar comportamiento sólo para actualizar texto; separar corrección de metadata de cambio de regla.
