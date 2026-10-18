# TPA0 baseline inventory — packaging snapshot

Este archivo acompaña `TPA0_BASELINE_INVENTORY.json`. El snapshot fue levantado al empaquetar la planificación; **no equivale a ejecutar TPA0**. TPA0 debe revalidarlo contra el árbol real, agregar characterization tests y producir su propio baseline report.


## Guardrail de diseño R2

Este snapshot describe el código legacy, no el modelo objetivo. La corrección pre-implementación `TPA-DESIGN-R2` establece que `ThirdParty`/`ThirdPartyAccount` son master data agnóstico de cámara. TPA0 debe caracterizar cualquier acoplamiento actual con ACH Colombia/CENIT como deuda a aislar posteriormente; no debe convertirlo en identidad del nuevo maestro.

## Observaciones confirmadas estáticamente

- `CustomerThirdParty` representa una combinación cliente + institución destino + cuenta + identificación receptor y un único status `Pending/Active/Rejected`.
- `AchTransactionService` y `AchBulkTransactionService` realizan onboarding silencioso de `Customer`/`CustomerAccount`.
- `PrenotificationHandler` crea/vincula `CustomerThirdParty` en prenotificaciones cuando resuelve el cliente origen.
- `AchStateTransitionService` sincroniza el resultado automático hacia el status legacy.
- `CustomerThirdPartiesController` es GET-only.
- la SPA de transacciones consulta `status=Active` para listar terceros actualmente elegibles.
- existe autoridad de reglas por cámara en `ClearingHouseTransactionRule`.
- existe un cálculo global de madurez a tres días hábiles en `PrenotificationQueryService` que TPA0 debe caracterizar.
- el job de aceptación tácita usa `SlaDeadlineAtUtc` o fallback por fecha; TPA0 debe confirmar la semántica antes de cambiarla.
- el seeder mantiene referencias textuales ACH Colombia V32 para reglas que deben auditarse contra V35.

## Baseline tree hashes

Consultar el JSON para hashes de `src`, `tests`, SPA y archivos focales. Los hashes sirven para detectar cambios accidentales en el paquete sin `.git`; no reemplazan commit evidence.
