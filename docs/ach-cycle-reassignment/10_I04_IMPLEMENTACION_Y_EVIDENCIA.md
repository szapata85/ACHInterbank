# I04 — Proyección futura, calendario operativo y política de fecha valor

**Fecha:** 2026-10-25. **Origen:** `ACHInterbank_ACH_CR_I03_COMPLETO.zip`. **Salida:** `ACHInterbank_ACH_CR_I04_COMPLETO.zip`.

## Alcance implementado (solo I04)

1. **Originación futura ACH Colombia:** `BatchResolver` acepta un ciclo de fecha posterior a la fecha local actual cuando el enrutador lo eligió y la ventana es `Before`. No permite una apertura anticipada **del mismo día** ni un ciclo vencido; se conservan errores existentes de ventana.
2. **Validación de día hábil:** `BatchResolver` usa `IOperationalCalendarService` ya disponible; rechaza la asignación a sábado/festivo/cierre excepcional si el enrutador entrega un ciclo inválido. El enrutador ya calculaba próximo día hábil; esto agrega defensa en el resolver sin reimplementar calendario.
3. **Estado de sesión:** `RoutingStrategyService` excluye ciclos `Closed` y `Cancelled` de su selección. `BatchResolver` rechaza esos estados en rutas diferentes de CENIT, sin modificar el mecanismo de cola CENIT.
4. **Fecha valor:** se conserva la regla I01/I02 de **mismo día valor** para transacciones creadas y pendientes. Si ya venció el último ciclo elegible, la operación NO se mueve a otro día. Motivo interno existente: `NO_ELIGIBLE_TARGET_SAME_VALUE_DATE`. Se requiere evidencia contractual sobre autorizaciones para alterar fecha valor antes de ampliar esa regla; **no se presume**.
5. **Política por tipo de operación:** `ICycleTransactionPolicy` sigue siendo obligatorio; proyección futura no habilita tipos restringidos (p. ej., débito monetario en un ciclo no permitido).

## Flujo as-built (creación)

`RoutingStrategyService` (calendario + ventana + cámara priorizada) → `BatchResolver.ResolveAsync` (estado + día hábil + ventana `Before` solo si procesamiento es posterior al día local) → `ICycleTransactionPolicy` → código NACHA existente → lote existente/creado con `EffectiveEntryDate = cycle.ProcessingDate.Date`.

No se cambia el contrato `AchTransactionRequestData`, ni el mecanismo NACHA ni el flujo de CENIT. El código de I01/I02 permanece protegido contra desplazamientos de fecha valor sin autorización. La tarea Quartz I03 sigue Disabled y necesita flag separado.

## Archivos fuente afectados

- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/BatchResolver.cs`: calendario inyectable opcional, validaciones de estado y día hábil, ventana futura.
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/RoutingStrategyService.cs`: excluir estados terminales de ciclos en elección.
- `tests/Cfa.ACHInterbank.Tests/AchCycleCalendarProjectionTests.cs`: **8 casos xUnit nuevos, solo escritos**, no ejecutados.
- `tests/Cfa.ACHInterbank.Tests/AchPendingCycleEligibilityServiceTests.cs`: **2 casos adicionales** de último ciclo y día inhábil; ahora 14.
- `scripts/validate-cycle-reassignment-i03-static.py`: permite memoria de un incremento posterior sin invalidar retrospectivamente I03.
- `scripts/validate-cycle-reassignment-i04-static.py`: controles específicos de I04; no compila .NET.

## Escenarios de aceptación tratados

| Escenario | Decisión esperada |
|---|---|
| Creada sábado 24-10-2026, enrutada a lunes 26-10 C1 | Aceptar asignación, valor lunes; **no** procesar en sábado. |
| Originación en festivo y ruta a martes hábil | Fecha valor martes según ciclo y calendario, sin sumar 24 h manualmente. |
| `Before` en fecha del mismo ciclo | Rechazar como antes; recepción futura no habilitada. |
| Ciclo de fecha futura definido como inhábil | Rechazar, sin crear lote. |
| Ciclo cerrado/cancelado | Omitir desde routing y rechazar en resolver (fuera de CENIT). |
| Venció último ciclo el mismo día valor | I01 devuelve `NO_ELIGIBLE_TARGET_SAME_VALUE_DATE`, I02 no escribe ni altera fecha. |
| Fuente en día no hábil, antes de buscar destino | I01 devuelve `NOT_BUSINESS_DAY`. |
| Futuro ciclo con tipo de transacción prohibido | Política rechaza; no eludir reglas para crédito/débito/prenotificación. |

## Pruebas y limitaciones

- Los tests están **escritos, no ejecutados**; `dotnet` no está instalado. Ver `ACH_CR_I04_VALIDATION.json` junto al ZIP para salida de validadores y SHA-256.
- No se ejecutaron DB relacional, EF runtime, Quartz, MFT, despacho, red ni UAT. Es obligatorio mantener `PRODUCTION_STATE: NOT_READY`.
- La regla de mover entre distintas fechas valor sigue **NO AUTORIZADA**. No se abre una excepción por mera existencia de un ciclo del siguiente día hábil.
- Se preservan riesgos I05 de carrera con exportación/ACK, integración CENIT y trazabilidad de archivos. I06 consolidará evidencias y las pruebas posteriores.

## Criterios de arquitectura / no sobreingeniería

Sin migraciones, nuevos servicios de base de datos, opciones de despliegue, jobs, locks, cachés, endpoints, colas, rediseño NACHA ni modificación del modelo Domain/Application. `IOperationalCalendarService`, `IOperationalCycleWindowResolver`, `ICycleTransactionPolicy` y `TimeProvider` ya existían.

**Próximo:** I05 — proteger carrera con exportación y regresiones CENIT. **Restan dos incrementos planeados: I05 e I06**.
