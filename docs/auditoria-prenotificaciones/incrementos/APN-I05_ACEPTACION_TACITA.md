# APN-I05 — Auditoría del motor de aceptación tácita

**Estado:** EJECUTADO_ESTATICAMENTE, RIESGOS_LOCALIZADOS; HOMOLOGACION_RUNTIME_PENDIENTE. **Código productivo modificado:** no.

## Ruta real

- `AchTacitAcceptanceJobHandler.ExecuteAsync` recoge intentos cuyo `ResponseDeadlineAtUtc <= utcNow`, transacciones pendientes y política que permita aceptación tácita; tiene ruta de compatibilidad para prenotificaciones sin intento canónico.
- `TaskDefinitionSeeder` crea el job con periodicidad `EveryNMinutes`, `N = 30` si no existe configuración; confirmar la periodicidad efectiva de DB.
- `ClearingHouseValidationPolicyService.ResolvePrenotificationResponseDeadlineAsync` resuelve `CycleCount` para ACH Colombia y `OperationalDayEnd` para CENIT y convierte hora local a instante con `IOperationalCycleWindowResolver`.
- `AchStateTransitionService` aplica `Pending -> AppliedTacitly` y sincroniza estados.

## Riesgos específicos

1. **APN-R01:** Deadline basado en cutoff, previo a liberación/ingesta de la última respuesta.
2. **APN-R02:** con menos de cuatro ciclos futuros disponibles, `.Take(window).LastOrDefault()` podría seleccionar prematuramente el último de los incompletos.
3. **APN-R03:** la selección del job no exhibe comprobación independiente de recepción del último archivo ni confirmación explícita de transmisión efectiva de la prenotificación (verificar garantías de rutas previas).
4. **APN-R06:** ventana de ejecución de 30 minutos puede retrasar el cambio tras el plazo; confirmar si esto es admisible.

## Batería de casos para validar

- T−1/T/T+1, día hábil/festivo, C4–C7 cruzando la fecha operacional; dejar la tabla AchCycle incompleta intencionalmente.
- Simular `AwaitingResponse` sin envío confirmado, `Pending` con devolución en cola, y último archivo retenido por entrega MFT.
- Ejecución concurrente de dos workers y de una devolución; estado transaccional consistente e idempotente.
- Correlacionar cron disparado, fecha/hora aplicada, evento persistido e intento actual.

**NO concluir que el job es seguro o inseguro sin reproducir condiciones de carrera.** Las pruebas actuales del simulador I05 no sustituyen integración de job/BD.
