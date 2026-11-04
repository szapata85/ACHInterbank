# Entrega APN-I08 — 2026-11-03

Paquete completo de ACH Interbank, acumulativo sobre APN-I07. Corrección focal de vencimientos de prenotificación en ACH Colombia (7 ciclos) y CENIT (5 ciclos).

- Producto modificado: `ClearingHouseValidationPolicyService.cs`.
- Tests C#: `ThirdPartyAccountValidationLifecycleTests.cs` (4 nuevos, 1 actualizado; NO_EJECUTADOS por falta de SDK).
- Evidencia: `docs/auditoria-prenotificaciones/incrementos/APN-I08_CORRECCION_VENCIMIENTOS.md`.
- Verificación estática separada: `scripts/audit/prenotificaciones/test_apn_i08_deadline_guard.py`.
- Riesgo residual: aceptaciones con `ResponseDeadlineAtUtc` persistido antes del cambio y ausencia de barrera de ingestión; planificar APN-I09.
