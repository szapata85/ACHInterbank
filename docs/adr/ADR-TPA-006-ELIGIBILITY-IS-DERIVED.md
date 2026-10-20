# ADR-TPA-006 — La elegibilidad de una cuenta es derivada

**Estado:** Accepted for implementation planning

## Decisión

No almacenar un booleano global `Eligible/Active` como autoridad de uso de `ThirdPartyAccount`.

`EligibilityDecision` se obtiene de:

- estado administrativo del maestro;
- política efectiva de la cámara;
- naturaleza/servicio/fecha de la operación;
- validación vigente e intentos;
- restricciones internas aplicables.

Puede existir una proyección `ThirdPartyAccountOperationalState` para consultas de alto volumen, siempre reconstruible desde las autoridades anteriores.

## Razón

Una cuenta puede ser utilizable en un contexto y requerir validación en otro. La elegibilidad no es una propiedad intrínseca del maestro.
