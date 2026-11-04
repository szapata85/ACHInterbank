# APN-I09 — Blindaje de aceptación tácita de prenotificaciones

**Proyecto:** ACH Interbank  
**Corte:** 2026-11-03  
**Base de trabajo:** `ACHInterbank_COMPLETO_CORRECCION_PRENOTIFICACIONES_APN_I08_2026-11-03.zip`  
**Estado:** `IMPLEMENTADO_EN_FUENTE / PRUEBAS_CSHARP_NO_EJECUTADAS / HOMOLOGACION_PENDIENTE`.

## Alcance implementado

1. **Revalidación justo antes de aplicar** `AppliedTacitly`: el job ya no se basa exclusivamente en `ResponseDeadlineAtUtc` persistido. Recalcula el vencimiento con `ClearingHouseValidationPolicyService` y no anticipa un vencimiento anterior almacenado (se usa la fecha más tardía).
2. **Contexto canónico:** verifica que el intento activo sea el actual, en espera de respuesta, sin resolución y vinculado a la misma cámara y ciclo de origen. Registros ambiguos o incompletos permanecen pendientes.
3. **Transmisión demostrable:** exige pertenencia a archivo NACHA exportado con `TransmittedAtUtc`, estado `Transmitted`/`Acknowledged`/`Accepted`, cámara, ciclo y trazabilidad concordantes. Crear una transacción o un intento no prueba transmisión.
4. **Barreras frente a respuestas:** no aplica aceptación tácita si existe `AchResponse` correlacionada, un vínculo entrante `IncomingNachaTransactionLink`, o una devolución simulada activa.
5. **Barreras frente a archivos entrantes:** detiene la decisión cuando detecta una ingestión de la cámara y del período relevante que aún está recibida, validándose, resolviéndose, lista para parsear, parseada o con resultado de procesamiento en curso.
6. **Concurrencia e idempotencia:** comprueba las condiciones y realiza la transición dentro de una transacción `Serializable` cuando el proveedor es relacional, con clave funcional `apn-tacit:{transactionId}`. Un fallo retiene el elemento para una ejecución posterior.
7. **Calendario CENIT incompleto:** para la política `OperationalDayEnd` de CENIT, requiere exactamente cinco sesiones no canceladas de la fecha operativa. No toma una cuarta sesión como fin del día si falta la quinta.
8. **Sin migraciones ni estados nuevos:** se reutilizan las entidades y mecanismos de trazabilidad existentes.

## Reglas de cámaras

- **ACH Colombia:** 7 ciclos operativos (`ACHCOL-7C-20261014`), ventana de cuatro ciclos posteriores; no utilizar la tabla histórica de cinco ciclos del manual V35. El vencimiento se calcula con la liberación final cuando existe metadata válida (APN-I08).
- **CENIT:** 5 sesiones operativas; último ciclo de devoluciones, liberación de salida hasta 19:15. Se requiere calendario completo antes de autorizar una aceptación tácita.

## Archivos productivos modificados

- `src/Cfa.ACHInterbank.Persistence/ACH/Quartz/Jobs/Implementation/AchTacitAcceptanceJobHandler.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ClearingHouseValidationPolicyService.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Services/PrenotificationTacitAcceptanceDecision.cs`

## Pruebas agregadas o actualizadas

- `tests/Cfa.ACHInterbank.Tests/PrenotificationTacitAcceptanceI09SafetyTests.cs`: escenarios de vencimiento temprano/tardío, respuesta presente, transmisión ausente, ingestión pendiente, legado y CENIT.
- `tests/Cfa.ACHInterbank.Tests/ThirdPartyAccountValidationLifecycleTests.cs`: día CENIT completo de cinco sesiones, y calendario incompleto que retorna `null`.
- `scripts/audit/prenotificaciones/test_apn_i09_safety.py`: comprobaciones de presencia de guardias y fuentes (no sustituye ejecutar .NET).

## Límites explícitos / riesgos no cerrados

- **No existe todavía un cierre operativo positivo (`watermark`) que demuestre haber recogido TODOS los archivos de salida de una sesión.** La nueva barrera solo ve archivos ya registrados en la base de datos. Por eso **APN-R03 se mitiga parcialmente, pero no queda cerrado**.
- Un archivo con cámara o fecha aún no identificada puede no correlacionarse al filtro de ingresiones; requiere una política de recepción final certificada por cámara/ciclo.
- El filtro conservador de transmisión y de respuestas puede dejar en `Pending` transacciones históricas sin evidencia suficiente. Es deliberado y exige revisión del origen de esa evidencia antes de relajar la condición.
- Las reglas no prueban que un archivo realmente haya sido recibido por el banco receptor; `TransmittedAtUtc` es evidencia de envío, no de entrega confirmada.
- Las rutas de ingestión, la transición de devoluciones tardías y la consistencia de intentos ya terminales se deben certificar con .NET, bases de datos e integración. **APN-R04 sigue abierto**.
- No se reescriben en masa los deadlines persistidos; se revalidan al evaluar cada candidato.
- El muestreo de candidatos es limitado (`BatchSize × 4`); muchos casos bloqueados antiguos podrían producir inanición de elementos posteriores, asunto que deberá medirse en volumen.

## Criterio de aprobación para producción

No aprobado todavía. Requiere compilación, ejecución xUnit, pruebas SQL Server y PostgreSQL, concurrencia real (devolución vs Quartz), simulación NACHA-M, prueba con transmisión confirmada y verificación de un cierre de entrega de archivos por ciclo.
