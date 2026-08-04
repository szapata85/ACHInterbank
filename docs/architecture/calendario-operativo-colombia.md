# Calendario operativo de Colombia

## Fuente única de verdad

`ColombianHolidayStrategy` genera las reglas legales nacionales sin consumir servicios externos ni mantener calendarios anuales codificados. `OperationalCalendarService` combina esas reglas con sábados, domingos y fechas especiales activas de una cámara. La política `RescheduleOnHoliday` del ciclo decide si esa combinación restringe la operación.

La fecha conmemorativa conserva la fecha de la celebración. La fecha efectiva es el día que bloquea la operación. Para las reglas Emiliani, ambas fechas pueden ser diferentes.

## Reglas legales

- Los festivos fijos conservan su fecha.
- Las celebraciones cubiertas por la Ley 51 de 1983 se trasladan al lunes siguiente cuando no ocurren un lunes.
- Jueves y Viernes Santo se calculan desde el Domingo de Pascua gregoriano.
- Ascensión, Corpus Christi y Sagrado Corazón se calculan desde Pascua y aplican el traslado legal.
- Nuestra Señora del Rosario de Chiquinquirá rige desde 2026, conforme a la Ley 2578 de 2026. En 2026 se conmemora el 9 de julio y su descanso efectivo es el lunes 13 de julio.

El calendario de 2026 contiene 19 reglas y 19 fechas efectivas distintas. En 2025 existen 18 reglas legales, pero Sagrado Corazón y San Pedro y San Pablo coinciden el 30 de junio; por ello hay 17 fechas efectivas distintas. La persistencia conserva las dos identidades legales y el calendario operativo agrupa la fecha coincidente una sola vez.

## Aprovisionamiento e idempotencia

`BankHolidayProvisioningService.EnsureYearsAsync` es la operación compartida por el bootstrap, el endpoint administrativo y `BANK_HOLIDAY_SEED`. Usa `RuleCode + CommemorativeDate` como identidad legal estable, repara reglas faltantes, actualiza únicamente registros generados identificables y conserva datos manuales y fechas especiales.

El bootstrap verifica el año operativo de `America/Bogota` y el siguiente. `BANK_HOLIDAY_SEED` conserva su identidad y planificación persistente, evita concurrencia con Quartz y registra cantidades esperadas, insertadas, actualizadas, existentes y omitidas.

## Fechas especiales por cámara

`ClearingHouseSpecialDates` permanece separado de los festivos nacionales. La unicidad es por cámara y fecha. Solo los registros activos bloquean; una fecha de ACH Colombia no modifica CENIT y viceversa. Crear, modificar o activar una fecha reevalúa los ciclos futuros pendientes de esa cámara. Desactivar no adelanta ciclos ya diferidos ni reescribe historia.

## Ciclos y barrera defensiva

La asignación y programación consultan el calendario por cámara. Antes de iniciar un ciclo, crear un archivo definitivo o invocar `Proc_Contrapartidas` o `Proc_Transacciones`, `CycleCalendarGuard` vuelve a evaluar el día vigente. Si está bloqueado:

1. no inicia ni despacha;
2. conserva el identificador, la cámara, los horarios y las transacciones;
3. guarda la fecha original, el motivo y la cantidad de aplazamientos;
4. mueve la fecha al siguiente día hábil de la misma cámara;
5. usa la fecha de procesamiento como token de concurrencia para que una decisión simultánea no se aplique dos veces.

## Verificación

Las pruebas de `OperationalCalendarTests`, los tests de despacho y los componentes Angular cubren el cálculo legal, la separación por cámara, la reparación idempotente, la protección de registros legales, la reprogramación y la ausencia de SOAP cuando el calendario bloquea. Las migraciones `CentralizedOperationalCalendar` existen para PostgreSQL y SQL Server y preservan las tablas manuales existentes.

Referencias normativas: Ley 51 de 1983 y Ley 2578 de 2026. Esta documentación resume las reglas funcionales; no reproduce el texto de las normas.
