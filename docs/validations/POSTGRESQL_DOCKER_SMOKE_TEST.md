# PostgreSQL Docker Smoke Test

## Resumen

- Plataforma validada: PostgreSQL en Docker.
- Stack levantado correctamente con:
  - `docker compose -f docker-compose.yml -f docker-compose.postgres.yml down --remove-orphans`
  - `docker compose -f docker-compose.yml -f docker-compose.postgres.yml up -d --build`
- `health/live` y `health/ready` respondieron `200 Healthy` en API y SPA.
- `GET /navigation/menu` sin token respondió `401 Unauthorized`, lo esperado.
- Validación autenticada: login OK con usuario demo `admin`.
- Observación operativa: durante `docker compose up` apareció el warning de red ya existente `achinterbank-onprem_ach_onprem`, pero el stack quedó funcional.

## Login

- Endpoint validado: `POST http://localhost:843/auth/login`
- Credenciales locales demo:
  - Usuario: `admin`
  - Contraseña: derivada del seed `UserConfiguration` (`Admin123!`)
- Resultado:
  - HTTP `200 OK`
  - Token JWT recibido
  - Token enmascarado: `eyJhbGciOiJIUzI1NiIs...zYwKdbUNHM`
- Respuesta autenticada:
  - Roles: `Admin,ACH.Operator`
  - Permisos: `CanReadAliases,CanReadAch,CanManageAch,CanManageUsers,CanReadCatalogs`

## Rutas protegidas probadas

| Ruta | Sin token | Con token | Resultado resumido |
|---|---:|---:|---|
| `/navigation/menu` | 401 | 200 | Menú JSON cargado correctamente. |
| `/financial-institutions` | 401 | 200 | Respuesta `[]`. |
| `/clearing-houses` | 401 | 200 | Respuesta paginada vacía. |
| `/company-entry-descriptions` | 401 | 200 | Catálogo seed cargado correctamente. |
| `/transactions` | 401 | 200 | Respuesta `[]`. |
| `/transactions/cycle-configs` | 404 | 404 | Ruta no existente en el backend actual. |
| `/clearing-house-cycle-configs?clearingHouseId=1` | 401 | 200 | Ruta real equivalente; devuelve `200` con cuerpo vacío para `clearingHouseId=1`. |

## Validación de PostgreSQL interna

- Base de datos confirmada: `ACHInterbank`
- Usuario activo: `example_user`
- Migrations aplicadas: `10`
- Migraciones aplicadas:
  1. `20260516220011_InitialPostgresSchemaBaseline`
  2. `20260516221927_AddIncomingCanonicalFileIdempotencyIndex`
  3. `20260519153106_AddAdminOperatorRoleSeed`
  4. `20260520025954_AddClearingHouseTransactionRules`
  5. `20260520034935_AddClearingHouseRulesMenuAndRuntimeSeeds`
  6. `20260520152600_AddNachaFileNamingRules`
  7. `20260520154333_AddUatMaturedDebitSeeds`
  8. `20260520154741_AddUatMaturedDebitAddendaFields`
  9. `20260520200509_AddNachaInboundSimulator`
  10. `20260521225311_AddIntegrationMappingTrace`

## Validación de seeds

- `Users`: `1`
- `Roles`: `2`
- `UserRoles`: `2`
- `MenuItems`: `33`
- `CatClearingHouse`: `2`
- `CompanyEntryDescription`: `38`
- `FinancialInstitutions`: `0`
- `ClearingHouses`: `0`
- `ClearingHouseConfigs`: `0`
- `ClearingHouseCycleConfigs`: `0`
- `CfgProfile`: `0`

### Usuario demo

- `Username`: `admin`
- `Email`: `admin@achinterbank.local`
- `IsActive`: `true`

## Observación del warning de red

- Durante el `docker compose up` apareció:
  - `Network achinterbank-onprem_ach_onprem Error ... network with name achinterbank-onprem_ach_onprem already exists`
- Aun así, el stack PostgreSQL quedó levantado y funcional.

## Estado del worktree

- Worktree limpio antes de crear este reporte.
- Tras crear este archivo, el worktree quedó con este cambio nuevo únicamente.

## Conclusión

POSTGRESQL OK CON OBSERVACIONES

- Runtime y health checks: OK.
- Login demo: OK.
- Token JWT: OK.
- Rutas protegidas principales: OK.
- PostgreSQL interno: OK con migraciones aplicadas y seeds mínimos presentes.
- Observación pendiente: algunas tablas funcionales de catálogo/operación siguen vacías, lo que es consistente con un entorno local de smoke test y no bloquea la validación base.
