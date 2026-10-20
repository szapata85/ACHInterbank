# Quartz.NET: proyección de ciclos diarios en fines de semana y festivos — 2026-10-20

## Alcance y fechas

ACH Colombia utiliza siete ciclos efectivos desde el **14/10/2026**, versión `ACHCOL-7C-20261014`. El ciclo 4 inicia a las 11:01 a. m. CENIT conserva cinco ciclos. Se respeta el calendario de Colombia, los cierres especiales de cámara y la zona horaria de la cámara (`America/Bogota` cuando corresponda).

## Comportamiento de generación

- `AchCycleScheduler` es una tarea Quartz diaria a las **02:00 America/Bogota**. Su `CalendarPolicy` debe ser `IgnoreCalendar`: el job **se ejecuta también en festivos, sábados y domingos** para crear los ciclos operativos del **próximo día hábil**, no del día no hábil.
- `MisfirePolicy` debe ser `FireAndProceed`: si el servicio pierde el disparo, ejecuta una recuperación al reanudar, y continúa su programación normal. No sustituye vigilancia operativa de cron/Quartz.
- `OperationalCalendarService.GetNextBusinessDayAsync` calcula la siguiente fecha hábil por cámara con sábados, domingos, festivos nacionales y fechas especiales no operativas.
- `RoutingStrategyService` puede crear/completar los ciclos del día hábil objetivo **durante el registro de una transacción**, aunque Quartz no haya corrido. La proyección es idempotente por `ClearingHouseId + CycleName + ProcessingDate` y mantiene referencia a la política/version (`ClearingHouseCycleConfigId`).
- La conciliación de una fecha parcialmente generada completa ciclos faltantes. Si los horarios previos no coinciden con la política vigente y el ciclo ya tiene movimientos asociados (transacciones, lotes o archivos NACHA), **no se reescriben**: se genera un error y se requiere conciliación manual. La generación no autoriza ejecutar transferencias en días no hábiles.

## Escenarios de regresión agregados

| Origen local (Bogotá) | Fecha operativa objetivo | Número esperado ACHCOL |
| --- | --- | --- |
| Sábado 17/10/2026 | Lunes 19/10/2026 | 7 |
| Domingo 18/10/2026 | Lunes 19/10/2026 | 7 |
| Lunes festivo 16/11/2026 | Martes 17/11/2026 | 7 |
| Viernes festivo 25/12/2026 | Lunes 28/12/2026 | 7 |

Incluye pruebas para crear y reintentar el job, completar una fecha con un solo ciclo, el fallback durante registro de transacción, reparar horarios de instancias no usadas y rechazar reescritura de instancias con archivos asociados.

## Despliegue y validación en ambiente local controlado

1. Respaldar la base de datos y desplegar esta versión del backend.
2. Ejecutar seeding: `Database:ApplySeed=true` durante inicialización o flujo `docker-ach.cmd up --seed --no-auto-migrations` según README del proyecto (con migraciones ya aplicadas). El seed repara la **tarea Quartz persistida existente**, no solo altas nuevas.
3. Confirmar en administración de tareas `AchCycleScheduler`: habilitada, sin pausa, zona `America/Bogota`, **02:00**, política `IgnoreCalendar`, misfires `FireAndProceed`. Confirmar sincronización en Quartz.
4. Verificar en `ClearingHouseCycleConfigs` siete reglas activas para ACHCOL desde 14/10/2026, cinco para CENIT.
5. Disparar manualmente `AchCycleScheduler` en entorno de pruebas en sábado o festivo con reloj controlado y verificar siete instancias `AchCycles` **solo** en la siguiente fecha hábil. Repetirlo para comprobar que no se dupliquen.
6. Registrar una transacción de prueba en fin de semana/festivo con Quartz detenido y comprobar que se crea o completa la fecha operativa de destino, **sin despachar** operaciones monetarias antes de la ventana permitida.
7. Verificar bitácora `TaskExecutionLogs` y estado `SchedulerSynchronizationStatus`: errores parciales de cámaras ya no se informan como éxito total.
8. Ejecutar `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release` y `dotnet build ACHInterbank.sln -c Release`. Finalmente realizar UAT con SQL Server o PostgreSQL y Quartz real.

**Validación del paquete:** el entorno de preparación no tiene SDK `dotnet` ni entorno de BD operativo. Se incluyen las pruebas en el proyecto, pero **no** se presenta el cambio como compilado, probado con Quartz real o desplegado. Se requiere UAT.
