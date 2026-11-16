# Comandos reproducibles (NO ejecutados aquí en .NET)

Desde `ACHInterbank_ajustado/` con .NET 10 y entorno de pruebas aislado:

```bash
dotnet --info
dotnet restore ACHInterbank.sln
dotnet build ACHInterbank.sln -c Release --no-restore
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build --filter 'FullyQualifiedName~ProcTransaccionesResponseParserTests|FullyQualifiedName~NotificarRespuestaAchUseCaseTests|FullyQualifiedName~WscfaachSoapClientCharacterizationTests' --logger 'trx;LogFileName=npt-focal.trx'
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build --logger 'trx;LogFileName=npt-regression.trx'
python3 scripts/test/npt_static_guard.py
```

Migraciones (SOLO con backup y bases DE PRUEBAS preparadas; revisar ensamblados y `DbContext` antes de aplicar):

```bash
dotnet ef migrations list --project src/Cfa.ACHInterbank.Persistence --startup-project src/Cfa.ACHInterbank.Api --context AchDbContext
dotnet ef migrations list --project src/Cfa.ACHInterbank.Persistence.Migrations.SqlServer --startup-project src/Cfa.ACHInterbank.Api --context AchDbContext
```

**Importante:** rutas de startup reales y la configuración `--connection` deben contrastarse con la solución antes de ejecutar comandos de aplicación; no se asume acceso ni se ejecuta ninguna migración en este paquete. Configurar bases de prueba, crear snapshot y comprobar filas `Dispatching` antes de migrar. No ejecutar UAT.
