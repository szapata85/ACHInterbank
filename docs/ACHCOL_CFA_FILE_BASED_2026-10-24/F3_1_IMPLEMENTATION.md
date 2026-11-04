# F3.1 — Cola NACHA de origen por entrada (PostgreSQL)

**Fecha:** 2026-10-24  
**Estado:** `IMPLEMENTED_UNVERIFIED`  
**Línea base:** `ACHInterbank_ACHCOL_CFA_F2_3_UNIFICADO.zip`  
**Siguiente:** `F3.2` (migración equivalente SQL Server).

## 1. Objetivo / exclusiones

Se ha extendido **la única** tabla `IncomingNachaDispatchQueue` para que pueda representar `AchColFileBased` sin `AchTransaction` ni `AchCycle`. Esta microtarea incluye el modelo compartido, migración **PostgreSQL** expand-first, consulta tipada y protección transitoria de workers legacy. **No** implementa el nuevo planificador, cálculo de identidad financiera ni generación de SOAP (F3.3–F5), y **no** habilita transmisión monetaria ni ejecución Live.

## 2. Cambios de esquema

| Campo/relación | F3.1 | Comportamiento |
| --- | --- | --- |
| `SourceKind` | `varchar(30) NOT NULL DEFAULT 'CycleBound'` | Discriminador explícito; filas históricas siguen como `CycleBound`. |
| `EntryDetailId` | `int NULL`, índice y FK Restrict a `EntryDetails.EntryDetailID` | **Obligatorio por check constraint** cuando `SourceKind='AchColFileBased'`. |
| `AchTransactionId` | `int NULL` | Sigue requerido para `CycleBound`, opcional para origen NACHA. FK legacy se preserva. |
| `AchCycleId` | `text NULL` en PostgreSQL | Sigue requerido para `CycleBound`, opcional para origen NACHA. No se asigna `8/9` como ciclo ficticio. |
| `IdempotencyDispatchKey` | sin alteración | Índice único y longitud 200 se conservan. El **cálculo** de huella canónica ACHCOL será F3.3. |

Restricción `CK_IncomingNachaDispatchQueue_SourceShape`:

- `CycleBound`: requiere `AchTransactionId` y `AchCycleId`, y `EntryDetailId IS NULL`.
- `AchColFileBased`: requiere `EntryDetailId`; `AchTransactionId`/`AchCycleId` son opcionales.
- Otros `SourceKind` quedan excluidos, aunque valores nulos tampoco son aceptados.

La migración `20261024213000_AchColFileBasedDispatchQueueF31.cs` es posterior a F2.3 y **no recrea** los campos de ingestión ya migrados. `Down` falla de forma explícita si hay filas NACHA originadas o datos de cola incompatibles con el esquema anterior; no las elimina silenciosamente.

## 3. Cambios en aplicación / compatibilidad

- **Domain:** `IncomingNachaDispatchSourceKinds` con `CycleBound` y `AchColFileBased`; relaciones nulas y navegación `EntryDetail`.
- **EF Core:** FK, índice de `EntryDetailId`, check constraint por origen, default legacy y snapshot PostgreSQL.
- **Command Center DTO/consulta:** `AchTransactionId` y `AchCycleId` ahora **nullable**, se expone `SourceKind`; el detalle NACHA se deriva de `queue.EntryDetailId` si existe.
- **Planificador legado:** asigna explícitamente `CycleBound`; **no se ha habilitado** todavía la creación de filas nuevas `AchColFileBased`.
- **Worker/ventana legado:** consulta exclusivamente `CycleBound` como medida fail-closed. No interpreta un origen sin ciclo como `CycleBound` ni lo envía accidentalmente a `Proc_Transacciones`. F4.2 habilitará la rama nueva con contexto y mapper autorizados.

**Advertencia de despliegue:** el runtime C# compartido contiene ahora un nuevo modelo relacional. El esquema SQL Server no se modifica en F3.1: su migración y snapshot se implementan en F3.2. **No desplegar este cambio en un entorno SQL Server hasta completar F3.2**, ni habilitar el feature flag de despacho ACHCOL hasta F3–F5.

## 4. Evidencia y verificaciones

| Verificación | Resultado |
| --- | --- |
| Inspección de entidades/configuración/consulta/worker y migraciones existentes | Ejecutada. |
| Preservación de índice único de idempotencia | Verificada por lectura del código y migración; **no ejecutada en DB real**. |
| Índice FK opcional y check por origen | Incorporados a configuración EF y migración PostgreSQL. |
| Tests focales del modelo, SQLite relacional, FK y anti duplicado | **Escritos**, ejecución pendiente. |
| `dotnet build`/`dotnet test` | **No ejecutados:** SDK .NET 10 no disponible. |
| `dotnet ef database update` PostgreSQL | **No ejecutado**: BD/SDK no disponibles. |
| SQL Server equivalente | **Pendiente F3.2**. |
| SOAP CFA / UAT final | **Pendiente F6**. |

No afirmar éxito de migración o calidad de tests por inspección estática. No detener F3.2 por ausencia de ambientes.

## 5. Decisiones incorporadas

- **DEC-031**: cola única discriminada por `SourceKind`, default histórico `CycleBound`, sin ciclos ficticios.
- **DEC-032**: referencia `EntryDetailId` del origen ACHCOL protegida con FK `Restrict` y shape check; tx/ciclo legacy opcionales solo para ruta NACHA.
- **DEC-033**: esquema PostgreSQL expand-first y `Down` protegido ante datos que requieren el esquema nuevo.
- **DEC-034**: consumidores legacy únicamente `CycleBound` hasta que F4 implemente ruta nueva; ausencia de ciclo nunca inicia por sí sola un SOAP.
- **DEC-035**: DTO con nulabilidad real, sin sustituir `AchTransactionId` por cero o `AchCycleId` por valor ficticio.

## 6. Archivos intervenidos

1. `src/Cfa.ACHInterbank.Domain/Models/ACH/IncomingNachaDispatchModels.cs`
2. `src/Cfa.ACHInterbank.Persistence/Configuration/IncomingNachaDispatchQueueConfiguration.cs`
3. `src/Cfa.ACHInterbank.Persistence/DataBase/Migrations/Postgres/20261024213000_AchColFileBasedDispatchQueueF31.cs` (**nuevo**)
4. `src/Cfa.ACHInterbank.Persistence/DataBase/Migrations/Postgres/AchDbContextModelSnapshot.cs`
5. `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaDispatchPlanner.cs`
6. `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaPostProcessingPipeline.cs`
7. `src/Cfa.ACHInterbank.Application/ACH/Models/IncomingNachaCommandCenterDtos.cs`
8. `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaCommandCenterQueryService.cs`
9. `tests/Cfa.ACHInterbank.Tests/IncomingNachaDispatchFileBasedSchemaF31Tests.cs` (**nuevo**)
10. Documentos de este programa (`PLAN.md`, `MEMORY_STATE.md`, `CHATGPT_MICRO_START.md`, presente reporte y README de paquete).

## 7. Continuidad F3.2

Crear la migración equivalente **SQL Server** (nullable, default `CycleBound`, FK restrict, índice, constraint equivalente), actualizar el snapshot del contexto SQL Server y contrastar paridad estructural. Dejar F3.3 para identificar entradas, calcular fingerprint canónico e implementar el planificador de origen NACHA; no comenzar SOAP prematuramente.
