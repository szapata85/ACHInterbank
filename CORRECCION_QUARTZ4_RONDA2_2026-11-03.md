# ACH Interbank – Compatibilidad .NET 10 y Quartz 4 (ronda 2)

Base: `ACHInterbank_NET10_Quartz4_Compatibilidad_Corregida.zip`.
Objetivo: solucionar errores de compilación reportados (CS1501, CS1739, CS0246, CS0117 y CS1061), sin alterar el flujo de negocio de ACH Colombia ni CENIT.

## Modificaciones

1. `SchedulerSyncService.cs`
   - Tres llamadas `JobBuilder.Create(jobType)` reemplazadas por `JobBuilder.Create().OfType(jobType)`.
   - Expresiones CRON horarios, diarias, semanales, mensuales, anuales y configurables adaptadas a `CronScheduleBuilder.Create(...)`.
   - Para el cron semanal, el día se escribe con su nombre abreviado (`MON`, `TUE`, etc.) para evitar diferencias de numeración entre `DayOfWeek` y Quartz.
   - Las ventanas de ciclo usan `TimeOnly` y `WithInterval(n, IntervalUnit.Minute)`.
   - Misfires para intervalos diarios usan `DailyTimeIntervalTriggerMisfireInstruction.DoNothing`, sin convertirlos a misfires de tipo cron.
   - Los intervalos simples usan `WithInterval(TimeSpan.FromMinutes(...))`.
2. `DynamicJobExecutor.cs`: usa `new JobExecutionException(unhandled)`, que mantiene la ausencia de relanzamiento inmediato.
3. `SchedulerAdminService.cs`: valida cron con `CronExpression.TryParse(..., out _)` y utiliza el constructor de `CronExpression` con zona horaria (su propiedad `TimeZone` es de solo lectura en Quartz 4).
4. Pruebas: adaptado el trigger legado de una prueba a `WithInterval(TimeSpan...)`. Agregadas aserciones para horarios CRON, intervalos simples y política de misfire en ciclos.

## Comprobaciones realizadas en este entorno

- `python scripts/validate-solution-projects.py`: OK, siete proyectos en solución, todas las referencias presentes.
- Inspección estática del código: no quedan las llamadas a la API retirada que originaron los 15 mensajes reportados, incluidos sus usos en pruebas.
- Revisión de las firmas en la documentación oficial de Quartz.NET 4.x.

**Limitación**: no hay ejecutable `dotnet` en el entorno de preparación, por lo que no se ejecutaron `restore`, `build` ni `test`. La compilación final debe verificarse localmente y podrían aparecer otras incompatibilidades.

## Validación local recomendada

Extraer el ZIP completo en una carpeta nueva (sin `bin`, `obj`, `.vs`). Desde la raíz:

```cmd
python scripts\validate-solution-projects.py
dotnet --info
dotnet restore ACHInterbank.sln --force --no-cache
dotnet build ACHInterbank.sln -c Debug --no-restore
dotnet test tests\Cfa.ACHInterbank.Tests\Cfa.ACHInterbank.Tests.csproj -c Debug --no-build
```

Si se usa Quartz con ADO Job Store persistente, revisar y aplicar las migraciones QRTZ correspondientes a la versión Quartz 4.7.1 antes de iniciar los schedulers en cualquier ambiente compartido. No migrar directamente una base de producción sin plan de despliegue y copia de seguridad.

Fuentes técnicas (4.x):
- https://www.quartz-scheduler.net/apidoc/4.x/api/Quartz.JobBuilder-1.html
- https://www.quartz-scheduler.net/apidoc/4.x/api/Quartz.CronScheduleBuilder.html
- https://www.quartz-scheduler.net/apidoc/4.x/api/Quartz.DailyTimeIntervalScheduleBuilder.html
- https://www.quartz-scheduler.net/apidoc/4.x/api/Quartz.CronExpression.html
- https://www.quartz-scheduler.net/apidoc/4.x/api/Quartz.JobExecutionException.html
