# SPA 4C.3A.2 — Alias permissions retirement checkpoint

**Estado:** `ALIAS_PERMISSION_RETIREMENT_IMPLEMENTED_VALIDATION_PENDING`

## Baseline

- Baseline obligatorio utilizado: `ACHInterbank-v26-spa-4C3A1-alias-frontend-retirement.zip`.
- Se leyó `AGENTS.md`, `MEMORY.md` y `docs/ai/SPA-4C3A1-ALIAS-FRONTEND-RETIREMENT-CHECKPOINT.md` antes de modificar el lote.
- Alcance único: retirar la deuda backend residual del permiso funcional Alias ya retirado del frontend en 4C.3A.1.
- Fuera de alcance y no modificados: `ach-responses`, 4C.3A.3, engine NACHA-M, aliases técnicos NACHA-M, NACHA Security y migrations/Designer históricos.

## Configuración activa retirada

### PermissionConfiguration

Se retiró de `src/Cfa.ACHInterbank.Persistence/Configuration/PermissionConfiguration.cs`:

- `PermissionConfiguration.ReadAliasesPermissionId`.
- seed `Permission`:
  - `Id = 0ee0a30f-48b6-4b8c-9afd-0dfb3d4770e7`
  - `Name = "CanReadAliases"`
  - `Description = "Consulta de alias"`

GUID histórico de `CanReadAliases`:

```text
0ee0a30f-48b6-4b8c-9afd-0dfb3d4770e7
```

`CanManageAliases` no existe como permiso backend activo y no se creó ningún permiso, seed ni migration sustituta para ese nombre.

### RolePermissionConfiguration

Se retiraron exclusivamente las dos asignaciones del GUID anterior:

- `Admin` — `RoleConfiguration.AdminRoleId = 1f8602da-6415-43f8-b61d-cb396f8577f1`.
- `ACH.Operator` — `RoleConfiguration.OperatorRoleId = a51746c2-0710-4d79-97b1-5b4368326f56`.

No se reasignó otro permiso y no se modificaron los seeds de `CanReadAch`, `CanManageAch`, `CanReadCatalogs`, `CanManageUsers` ni otros permisos.

## MenuItemConfiguration / AliasesId

Se confirmó antes del retiro que `MenuItemConfiguration.AliasesId` era únicamente un constant residual sin consumidores actuales.

Se retiró:

```text
MenuItemConfiguration.AliasesId = 3
```

El modelo actual no contiene un `MenuItem` con ruta `/aliases`. No se agregó `DeleteData` para `MenuItem Id=3` y no se introdujo limpieza manual de filas de navegación históricas no representadas por el modelo actual.

Los filtros defensivos de `/aliases` incorporados en 4C.3A.1 permanecen intactos.

## Migrations forward creadas

### PostgreSQL

- `src/Cfa.ACHInterbank.Persistence/DataBase/Migrations/Postgres/20261001121600_RetireAliasPermissionSeed.cs`
- `src/Cfa.ACHInterbank.Persistence/DataBase/Migrations/Postgres/20261001121600_RetireAliasPermissionSeed.Designer.cs`
- snapshot actual actualizado: `src/Cfa.ACHInterbank.Persistence/DataBase/Migrations/Postgres/AchDbContextModelSnapshot.cs`

### SQL Server

- `src/Cfa.ACHInterbank.Persistence.Migrations.SqlServer/DataBase/Migrations/SqlServer/20261001121700_RetireAliasPermissionSeed.cs`
- `src/Cfa.ACHInterbank.Persistence.Migrations.SqlServer/DataBase/Migrations/SqlServer/20261001121700_RetireAliasPermissionSeed.Designer.cs`
- snapshot actual actualizado: `src/Cfa.ACHInterbank.Persistence.Migrations.SqlServer/DataBase/Migrations/SqlServer/AchDbContextModelSnapshot.cs`

Contenido de ambas migrations:

**UP**

1. elimina `RolePermissions` de `CanReadAliases` para `Admin`;
2. elimina `RolePermissions` de `CanReadAliases` para `ACH.Operator`;
3. elimina el `Permission` `CanReadAliases`.

**DOWN**

1. restaura primero el `Permission` con el mismo GUID, nombre y descripción históricos;
2. restaura después los dos `RolePermissions` originales.

El orden respeta las FK. No hay `CreateTable`, `DropTable`, `AddColumn`, `AlterColumn`, índices, cambios de esquema ni operaciones sobre `MenuItems`.

El host no dispone de `dotnet`, por lo que no fue posible ejecutar `dotnet ef` para regenerar o ejecutar estas migrations. Los archivos de migration/Designer y los snapshots actuales se materializaron siguiendo el patrón EF existente del repositorio y se revisaron estáticamente; su generación/ejecución real con EF CLI queda pendiente de un host con .NET 10.

## Migrations históricas y evidencia histórica

No se editó ninguna migration histórica ni ningún `Designer.cs` histórico. Una comparación binaria contra el ZIP baseline confirmó que los únicos archivos existentes dentro de los directorios de migrations que cambiaron fueron los dos snapshots actuales; los archivos `RetireAliasPermissionSeed` son nuevos.

`docs/validations/POSTGRESQL_DOCKER_SMOKE_TEST.md` permanece byte-a-byte igual al baseline. Su referencia a `CanReadAliases` se clasifica como `EXPECTED_HISTORY / HISTORICAL_EVIDENCE` y no como consumidor activo.

También permanecen referencias históricas en migrations/Designer anteriores y en documentación de auditoría/diagnóstico. No se reescribieron.

## Pruebas focales agregadas

Se agregó:

- `tests/Cfa.ACHInterbank.Tests/AliasPermissionRetirementTests.cs`

La prueba focal está diseñada para demostrar:

- ausencia de `CanReadAliases` en seeds actuales;
- ausencia de `CanManageAliases` en seeds actuales;
- ausencia de RolePermissions al GUID histórico;
- ausencia de `/aliases` en el menú seed actual;
- preservación de `CanReadAch`, `CanManageAch`, `CanReadCatalogs` y `CanManageUsers`;
- ausencia de los constants actuales `ReadAliasesPermissionId` y `AliasesId` mediante reflexión;
- que las migrations PostgreSQL/SQL Server `Up` contienen únicamente tres `DeleteData` esperados;
- que los `Down` contienen únicamente la restauración del Permission y de los dos RolePermissions originales.

Estas pruebas no pudieron ejecutarse porque `dotnet` no está instalado en el host.

## Búsqueda final y clasificación

### ACTIVE_CURRENT_CODE

Escaneo de `src` excluyendo artifacts de migrations/Designer:

```text
CanReadAliases          => 0
ReadAliasesPermissionId => 0
CanManageAliases        => 0
AliasesId               => 0
```

Los snapshots actuales y los nuevos `Designer.cs` tampoco contienen el GUID o seed `CanReadAliases`.

### EXPECTED_HISTORY

Permanecen referencias a `CanReadAliases` en:

- migrations y `Designer.cs` históricos;
- migrations nuevas `RetireAliasPermissionSeed.cs`, exclusivamente para identificar el registro a retirar y restaurarlo en `Down`;
- `docs/validations/POSTGRESQL_DOCKER_SMOKE_TEST.md` como evidencia histórica;
- otros documentos históricos de diagnóstico/auditoría de la SPA.

Estas referencias no son consumidores activos.

### VALIDATION_ONLY_NEGATIVE_ASSERTIONS

`tests/Cfa.ACHInterbank.Tests/AliasPermissionRetirementTests.cs` contiene los nombres retirados únicamente como aserciones negativas de ausencia. No constituye un permiso o consumidor funcional activo.

### EXPECTED_FRONTEND_RETIREMENT_GUARDS

`/aliases` permanece en los controles negativos creados en 4C.3A.1:

- filtro defensivo de navegación runtime;
- filtro defensivo de administración de navegación;
- pruebas negativas de navegación;
- cobertura `not-found` de la ruta retirada.

No se reabrió la feature frontend.

### NACHA_TECHNICAL_ALIAS

Los aliases técnicos NACHA-M permanecen intactos. Se compararon hashes antes/después del lote y todos coinciden, incluyendo:

```text
INachaType7AliasMap.cs                  73bc9cf0c8beb982b20108c4c9789f24b9d98ceb0124f0b24efbb0ac84e6726b
INachaType7FieldValueResolver.cs        8e0eef82be7c7a197bf1a188ed95f09e5171447efdc4f129de2f631187ffb569
NachaType7AliasMap.cs                   89c75e2001f949491096e058f29a993429ee31403334af0a8b11f470a720e68e
NachaType7FieldValueResolver.cs         63599ee61a2f6bfd0272526b1ff468e710a6597b17799342619f5895e8fbc17d
NachaCanonicalMapper.cs                 e05317977c2fb6752ea6be8ca8ec0705ffd1f7711e5ae3917b527bbd85e26f91
```

No se tocaron canonical mappings ni el engine NACHA-M.

## Archivos modificados / agregados

Modificados respecto del baseline:

- `src/Cfa.ACHInterbank.Persistence/Configuration/PermissionConfiguration.cs`
- `src/Cfa.ACHInterbank.Persistence/Configuration/RolePermissionConfiguration.cs`
- `src/Cfa.ACHInterbank.Persistence/Configuration/MenuItemConfiguration.cs`
- `src/Cfa.ACHInterbank.Persistence/DataBase/Migrations/Postgres/AchDbContextModelSnapshot.cs`
- `src/Cfa.ACHInterbank.Persistence.Migrations.SqlServer/DataBase/Migrations/SqlServer/AchDbContextModelSnapshot.cs`

Agregados:

- `src/Cfa.ACHInterbank.Persistence/DataBase/Migrations/Postgres/20261001121600_RetireAliasPermissionSeed.cs`
- `src/Cfa.ACHInterbank.Persistence/DataBase/Migrations/Postgres/20261001121600_RetireAliasPermissionSeed.Designer.cs`
- `src/Cfa.ACHInterbank.Persistence.Migrations.SqlServer/DataBase/Migrations/SqlServer/20261001121700_RetireAliasPermissionSeed.cs`
- `src/Cfa.ACHInterbank.Persistence.Migrations.SqlServer/DataBase/Migrations/SqlServer/20261001121700_RetireAliasPermissionSeed.Designer.cs`
- `tests/Cfa.ACHInterbank.Tests/AliasPermissionRetirementTests.cs`
- `docs/ai/SPA-4C3A2-ALIAS-PERMISSIONS-RETIREMENT-CHECKPOINT.md`

No se eliminaron archivos de producción adicionales en este lote.

## Validación realmente ejecutada

### 1. Validación de solución/proyectos

```text
python scripts/validate-solution-projects.py
=> exit 0
=> SOLUTION GRAPH: OK
   solution projects: 7
   disk projects: 7
   project references: all targets exist and are loaded by the solution
```

### 2. Tests backend focales

Comando intentado:

```text
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~AliasPermissionRetirementTests|FullyQualifiedName~NavigationMenuSeedTests"
```

Resultado real:

```text
exit 127
bash: dotnet: command not found
```

### 3. Build Release

Comando intentado:

```text
dotnet build ACHInterbank.sln -c Release
```

Resultado real:

```text
exit 127
bash: dotnet: command not found
```

### 4. Suite backend completa

Comando intentado:

```text
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release
```

Resultado real:

```text
exit 127
bash: dotnet: command not found
```

### Providers / Docker

Se comprobó disponibilidad de Docker:

```text
docker --version
=> exit 127
=> bash: docker: command not found
```

Por tanto no fue posible ejecutar migrations reales ni consultas post-migrate sobre PostgreSQL o SQL Server en este host. No se simuló ningún resultado de provider.

### Validación estática adicional

Se ejecutó una comprobación estática local que confirmó con `exit 0`:

- configuración activa sin los cuatro símbolos/nombres residuales;
- snapshots y nuevos target models sin el seed Alias;
- migrations nuevas con únicamente el retiro/restauración esperado y orden FK correcto;
- ausencia de operaciones sobre `MenuItems` o schema;
- migrations/Designer históricos sin cambios;
- evidencia histórica PostgreSQL sin cambios;
- aliases técnicos NACHA-M con hashes idénticos al baseline;
- `ACTIVE_CURRENT_CODE` sin referencias Alias residuales.

Esta comprobación estática no sustituye build, tests ni migration execution.

## Bloqueos reales del entorno

- `.NET 10 / dotnet CLI`: no disponible en PATH ni en ubicaciones estándar del host.
- Docker CLI: no disponible.
- En consecuencia: sin build, sin tests ejecutables y sin validación real de migrations PostgreSQL/SQL Server.

## Alcance retenido

- No se migró `ach-responses`.
- No se avanzó a 4C.3A.3.
- No se reabrió 4C.3A.1.
- No se crearon permisos sustitutos.
- No se tocaron aliases técnicos NACHA-M.

## Estado final

`ALIAS_PERMISSION_RETIREMENT_IMPLEMENTED_VALIDATION_PENDING`
