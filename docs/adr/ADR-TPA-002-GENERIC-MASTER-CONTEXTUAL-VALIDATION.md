# ADR-TPA-002 — Maestro genérico; validación contextual

**Estado:** Accepted for implementation planning  
**Programa:** TPA-GOV-001

## Contexto

El diseño inicial proponía un `ThirdPartyAccountEnrollment` por cuenta+cámara+naturaleza. Esa formulación podía hacer parecer que el onboarding de la cuenta dependía de la cámara y duplicar conceptualmente una misma cuenta cuando cambiara el rail utilizado.

## Decisión

1. `ThirdParty` y `ThirdPartyAccount` son **master data agnóstico de cámara**.
2. El onboarding silencioso/manual termina al resolver o crear `ThirdPartyAccount`.
3. Cámara, naturaleza, servicio, regla efectiva y resultado de prenotificación pertenecen a `ThirdPartyAccountValidation` y sus intentos, creados sólo cuando el contexto operacional lo requiere.
4. La elegibilidad se calcula contextualizadamente; no existe un `Active` universal de cuenta.
5. Una cuenta descubierta por ACH Colombia puede reutilizarse posteriormente por CENIT sin crear otro maestro.

## Consecuencias

- claves naturales del maestro no incluyen cámara;
- no se crea `ThirdPartyAccountEnrollment`;
- CRUD de tercero/cuenta no pide cámara;
- request/retry de validación sí requiere contexto operacional;
- una proyección de estado por cámara puede existir por rendimiento, pero es derivada y reconstruible;
- TPA2 centraliza onboarding genérico y TPA3 incorpora elegibilidad/validación contextual.
