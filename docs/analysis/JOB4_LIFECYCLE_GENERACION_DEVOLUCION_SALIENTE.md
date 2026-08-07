# JOB 4 — Lifecycle y generación de devolución saliente

## AS-IS y cambio aplicado

`AchReturnsService.GenerateReturnsFileAsync` ya validaba elegibilidad/causal, construía 1/5/6/7/8/9 con Addenda 99 y persistía `AchReturnGenerated`, pero su evento no modificaba el estado. Ahora, después de construir y validar el archivo, persiste la evidencia y ejecuta la transición canónica `IAchStateTransitionService` a `ReturnedByEpr`, con causal, `OriginalTraceRef`, payload de generación e idempotencia `outbound-return-v1:{transactionId}`. En proveedor relacional las operaciones quedan dentro de una transacción local.

## Estado por cámara

ACH Colombia permanece habilitada: ciclo máximo de cuatro aplicado solo para esa cámara; naming se exige desde `IExternalFileNamePolicy`; Addenda 99 usa traza original y nueva secuencia; DFI se invierte receptor original → originador original; prenotificación conserva importe cero. CENIT conserva lifecycle disponible para sus flujos entrantes, pero su generación física saliente se bloquea explícitamente hasta homologar layout, DFI, correlación y naming; no se extrapola ACH Colombia.

## Evidencia

Prueba focalizada `AchOutboundReturnStateAndIdempotencyCharacterizationTests`: 13 aprobadas. Cubre evidencia, transición auditada, causal/traza, fallo previo a persistencia, repetición y concurrencia del lock local. No se modificaron migraciones ni golden files. Docker no fue requerido: no se modificó EF, esquema, índices ni consultas provider-specific.

## Estado

B3: PARCIAL. El lifecycle ACH Colombia queda coherente y auditable, pero falta evidencia provider-specific/concurrencia multinodo y la generación CENIT sigue bloqueada por homologación técnica. B5 ACH Colombia: PARCIAL; CENIT: ABIERTA.
