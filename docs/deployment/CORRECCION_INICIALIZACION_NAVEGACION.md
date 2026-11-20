# Correccion de inicializacion del menu /users/list

## Sintoma

`Navigation route '/users/list' must exist before the canonical information-architecture reconciler runs.`

## Diagnostico

- El inicializador ejecuta `DbInitializer.SeedAllAsync` solamente si `Database:ApplySeed` es `true`.
- `Database:ApplyMigrations` ejecuta migraciones EF Core y es independiente de `ApplySeed`.
- `UsersMenuSeeder` (orden 1) crea la ruta `/users/list`, con autorizacion de administrador y permiso `CanManageUsers`.
- `NavigationInformationArchitectureSeeder` (orden 100) asume que la ruta existe y reordena la jerarquia de navegacion.
- Si el seeding anterior no se completo o se perdio el registro, el reconciliador antes lanzaba excepcion.

## Ajuste aplicado

Solo cuando la ruta `/users/list` no existe, `NavigationInformationArchitectureSeeder.ReconcileAdministrationAsync` invoca `UsersMenuSeeder.SeedAsync` para recuperar la ruta y establecer los permisos correspondientes, **antes** de reorganizarla bajo Administracion. Si ya existe, conserva la ruta, su ID y sus enlaces de autorizacion.

Incluye prueba de regresion SQLite in-memory para ruta ausente, permisos e idempotencia.

## Visual Studio / IIS

Configuracion en `src/Cfa.ACHInterbank.Api/appsettings.json`:

```jsonc
"Database": {
  "Provider": "SqlServer",
  "ApplyMigrations": true,
  "ApplySeed": false
}
```

Con `ApplySeed=false` no se ejecutan los seeders; es suficiente para probar las migraciones y el acceso basico, pero no para aprovisionar roles, menus y catalogos de una base nueva.

Para inicializar los catalogos de una base nueva, configurar temporalmente `ApplySeed=true`, revisar las demas dependencias obligatorias de los seeders (incluidas integraciones SOAP) y hacer la ejecucion en un entorno controlado. Cuando finalice, volver a `false` si no se requiere repetir el seed en cada arranque.

## Verificacion

Se verifico estaticamente la existencia de `UsersMenuSeeder`, su orden de ejecucion, la ruta, las autorizaciones y la idempotencia del cambio. El contenedor de preparacion no dispone del SDK .NET, por lo que deben ejecutarse `dotnet test` y la prueba real contra SQL Server en Visual Studio/Windows.
