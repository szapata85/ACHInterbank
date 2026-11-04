# F3.2 — Paridad SQL Server de cola `AchColFileBased`

**Programa:** `ACHCOL-NACHA-CFA-EXTCYCLE-001`  
**Fecha del incremento:** 2026-10-24  
**Estado:** `IMPLEMENTED_UNVERIFIED`  
**Base:** `ACHInterbank_ACHCOL_CFA_F3_1_UNIFICADO.zip`  
**Entrega:** `ACHInterbank_ACHCOL_CFA_F3_2_UNIFICADO.zip`  
**Siguiente:** `F3.3` — planificación/idempotencia desde detalle NACHA-M.

## Objetivo y límite

Se replica en **SQL Server** la ampliación de la tabla `IncomingNachaDispatchQueue` de PostgreSQL F3.1. Este incremento **no altera** el cálculo de la clave de idempotencia, parser, cifrado, ingresas F1/F2, planificador, worker de ACHCOL ni mapping/SOAP CFA. El feature flag de ACH Colombia se mantiene desactivado por defecto; **no se autoriza Live**.

## Cambios incorporados

1. `src/Cfa.ACHInterbank.Persistence.Migrations.SqlServer/DataBase/Migrations/SqlServer/20261024224500_AchColFileBasedDispatchQueueF32.cs` — migración EF Core registrada con atributos `[DbContext]` y `[Migration]` (sin archivo Designer, siguiendo las migraciones manuales F2.3/F3.1).
2. `src/Cfa.ACHInterbank.Persistence.Migrations.SqlServer/DataBase/Migrations/SqlServer/AchDbContextModelSnapshot.cs` — modelo y relaciones de cola sincronizados con los campos opcionales y el tipo específico SQL Server.
3. `tests/Cfa.ACHInterbank.Tests/IncomingNachaDispatchFileBasedSqlServerF32Tests.cs` — prueba de paridad de operaciones EF Core contra PostgreSQL F3.1, estructura de FK, check, restricciones y `Down` seguro; código **no ejecutado**.
4. `PLAN.md`, `MEMORY_STATE.md`, `CHATGPT_MICRO_START.md`, `README_PAQUETE_UNIFICADO.md` — estado y decisiones actualizados.

## Paridad lógica entre proveedores

| Regla | PostgreSQL F3.1 | SQL Server F3.2 |
| --- | --- | --- |
| `SourceKind` | `varchar(30) NOT NULL DEFAULT 'CycleBound'` | `nvarchar(30) NOT NULL DEFAULT N'CycleBound'` (EF representa el default como string) |
| `EntryDetailId` | `integer NULL` | `int NULL` |
| `AchTransactionId` | `integer NULL` | `int NULL` |
| `AchCycleId` | `text NULL` | `nvarchar(max) NULL` |
| Índice `EntryDetailId` | `IX_IncomingNachaDispatchQueue_EntryDetailId` | Mismo nombre y columna |
| FK | `EntryDetails.EntryDetailID`, `Restrict` | Mismo contrato; `Restrict` equivale a no-cascade en SQL Server |
| Check por origen | `CK_IncomingNachaDispatchQueue_SourceShape` | Mismo contrato lógico con sintaxis de identificadores `[ ]` |
| Índice de idempotencia | Se conserva | Se conserva sin reconstruir |

**Shape de origen:** `CycleBound` exige `AchTransactionId`, `AchCycleId` y `EntryDetailId = null`; `AchColFileBased` exige `EntryDetailId` y permite transacción/ciclo nulos. Otros orígenes no pasan el check. La ruta nueva **no** crea ciclos ficticios.

**Compatibilidad histórica:** la columna no nula `SourceKind` recibe `CycleBound` por default también para filas existentes; la relajación de nulabilidad no invalida entradas actuales que ya tenían transacción y ciclo. La FK nueva permite nulos. No se elimina ni altera el índice único existente de `IdempotencyDispatchKey`.

**Despliegue:** aplicar F2.3 antes de F3.2 en SQL Server (orden cronológico), verificar que no exista divergencia/manual patch de la tabla, respaldar antes de migrar y revisar todas las migraciones EF pendientes. La presencia de la migración no demuestra que se haya aplicado en una base real.

**Rollback:** `Down` se rehúsa mediante `THROW 51032` si hay filas de origen `AchColFileBased`, referencias NACHA o cualquier fila incompatible con el modelo legacy; la comprobación se ejecuta **antes** de quitar columnas/FK/índices. Ejecutar solo dentro de una ventana controlada y conciliar registros antes de cualquier retroceso. La prueba de esta protección queda pendiente de DB SQL Server.

## Decisiones adoptadas

- **DEC-036:** esquema SQL Server equivalente al lógico PostgreSQL, preservando tipos SQL Server (`nvarchar`, `int`), índices, FK Restrict y check por origen.
- **DEC-037:** default persistido `CycleBound` y nuevas referencias nulas, sin backfills inventados de `AchCycleId` y sin tocar el índice de idempotencia.
- **DEC-038:** rollback fail-closed con guardia T-SQL **antes** de operaciones destructivas y excepción 51032. No truncar registros monetarios por restaurar esquema.
- **DEC-039:** registro EF Core y snapshot de SQL Server consistentes con configuración del modelo; constraint T-SQL usa `[identificador]` mientras la expresión en el modelo compartido usa comillas dobles (misma semántica para los dos orígenes). Reconfirmar al ejecutar `has-pending-model-changes` en F6.
- **DEC-040:** F3.2 no habilita nuevos workers ni envíos; la planificación ACHCOL y nueva identidad idempotente son F3.3/F4, certificación final F6.

## Evidencia y límites

- **Revisión estática:** comparados `Up/Down` PostgreSQL y SQL Server, estructura de snapshot, identidad de FK/índice/check, nuevos tests, orden de migraciones y conservación del ZIP. Las comprobaciones estáticas no sustituyen ejecución.
- **Tests agregados:** 3 métodos xUnit de contrato, rollback e identificación de migración (no ejecutados).
- **No ejecutado:** `dotnet build`, `dotnet test`, `dotnet ef migrations list`, scripts `idempotent`, migración SQL Server/PostgreSQL en BD real, pruebas de concurrencia, CFA Live/UAT. Causa: SDK .NET 10 y ambientes no disponibles en la sesión.
- **Estado técnico:** `IMPLEMENTED_UNVERIFIED`. La deuda de pruebas se mantiene hasta F6 sin detener F3.3–F5.

## Continuidad F3.3

Desarrollar planificación de cola para entradas NACHA-M identificadas de forma canónica, con idempotencia y concurrencia, **sin** extrapolar `ExternalCycleNumber` a `AchCycleId`. No enviar SOAP sin política de elegibilidad ni contrato mapeado/homologado. Partir del ZIP íntegro F3.2, no de F3.1 ni de archivos sueltos.
