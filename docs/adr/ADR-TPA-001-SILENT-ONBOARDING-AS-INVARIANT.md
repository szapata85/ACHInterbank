# ADR-TPA-001 — El onboarding silencioso genérico es un invariante del producto

**Estado:** Accepted for implementation planning  
**Programa:** TPA-GOV-001

## Contexto

El baseline registra/enriquece maestros desde creación single/bulk y crea/vincula `CustomerThirdParty` durante prenotificaciones. El nuevo módulo no debe imponer preregistro manual ni acoplar la identidad del tercero/cuenta a una cámara.

## Decisión

Centralizar un `IThirdPartyOnboardingService` común a single, bulk y alta manual. El servicio opera sólo sobre master data (`Customer -> ThirdParty -> ThirdPartyAccount`) y es agnóstico de ACH Colombia/CENIT, naturaleza, servicio, ciclos y causales.

La resolución de cámara y los prerequisitos ocurren después, en elegibilidad/validación.

## Consecuencias

- mismo tercero/cuenta se reutiliza entre cámaras;
- se requieren normalización, claves únicas y manejo de concurrencia;
- el alta manual y silenciosa convergen;
- onboarding no crea una autorización interbancaria;
- Phase 0 debe caracterizar comportamiento actual antes de refactorizar.
