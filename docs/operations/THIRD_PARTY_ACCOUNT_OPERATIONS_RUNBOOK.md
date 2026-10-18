# Runbook operacional — Terceros, cuentas y validaciones

## 1. Principio

Soporte distingue siempre:

- problema de **master data** (tercero/cuenta);
- problema de **validación contextual** (cámara/naturaleza/servicio);
- problema de **elegibilidad/transacción**.

No “aprobar” una cuenta manualmente para solucionar una prenotificación.

## 2. Métricas

- terceros/cuentas creados por single/bulk/manual/backfill;
- deduplicaciones/concurrent unique conflicts;
- intentos de validación por cámara/naturaleza/estado;
- pendientes vencidos;
- rechazos por causal;
- re-prenotificaciones;
- expiraciones/revalidaciones;
- divergencias legacy/canónico;
- latencia de eligibility/read models.

## 3. Diagnóstico de cuenta rechazada

1. ubicar `ThirdPartyAccount` canónica;
2. confirmar datos maestros;
3. identificar el contexto de validación exacto (cámara/naturaleza/servicio);
4. abrir el intento exacto;
5. revisar transaction/correlation/deadline/causal;
6. corregir maestro sólo si la evidencia lo exige;
7. crear un nuevo intento; no reabrir ni editar el anterior.

## 4. Pendiente vencida

No cambiar estado manualmente. Verificar policy efectiva, deadline, ciclo/calendario, job de aceptación tácita, respuesta inbound no correlacionada y estado de transacción. Si no se puede resolver automáticamente, escalar incidente sin inventar aceptación.

## 5. Cuenta usada por otra cámara

No duplicar ni “migrar” la cuenta. Reutilizar el mismo `ThirdPartyAccount` y diagnosticar/crear el contexto de validación requerido por la nueva operación.

## 6. Rollback

- TPA1: legacy sigue siendo autoridad visible;
- TPA2–TPA4: desactivar dual-read/write por flag/adapter según diseño final; conservar canónico;
- TPA5–TPA7: desactivar comandos/UI nuevos sin destruir histórico;
- TPA8+: volver selector/read a adapter legacy si el esquema permanece intacto.
