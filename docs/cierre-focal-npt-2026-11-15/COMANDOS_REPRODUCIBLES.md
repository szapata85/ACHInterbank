# Comandos reproducibles — NPT focal 2026-11-15

## Ejecutados aquí

```bash
cd ACHInterbank_ajustado
python scripts/test/npt_focal_sqlite_source_guard.py
python scripts/test/achcol_nacha_transaction_counts_static_check.py
python scripts/test/e2e01_e2e02_source_static_check.py
python scripts/test/h01_h02_static_sqlite_check.py
python scripts/test/res_h02_soap_preflight_sqlite_check.py
python scripts/test/aud_e2e_final_static_and_sqlite_check.py
python scripts/test/npt_static_guard.py  # 41/42, expectativa histórica obsoleta: ver informe
```

Los 26/26 y cinco suites con exit 0 son controles de fuente y modelos de Python, NO pruebas del código C#.

## Intentados y no ejecutables: SDK .NET ausente, código 127

```bash
cd ACHInterbank_ajustado
dotnet --info
dotnet restore ACHInterbank.sln
dotnet build ACHInterbank.sln -c Release --no-restore
dotnet test ACHInterbank.sln -c Release --no-build
```

## Próximos comandos de G1 (no ejecutados aquí)

```bash
cd ACHInterbank_ajustado
dotnet restore ACHInterbank.sln
dotnet build ACHInterbank.sln -c Release --no-restore
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~NptFocal"
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build
dotnet test ACHInterbank.sln -c Release --no-build
```

## Infraestructura pendiente G2–G3

- Iniciar SQL Server y PostgreSQL de pruebas, ejecutar migraciones de base y confirmar índices, CAS de `ExecuteUpdateAsync` y concurrencia real EF con dos hosts Quartz simultáneos.
- SOAP determinista con contador físico de POST; inyectar timeout/cancelación/caída antes, durante y después de transporte y verificar un POST máximo para operaciones no conciliadas.
- Reiniciar el host Axon y comprobar watchdog independiente. Confirmar que los eventos tardíos se guardan sin alterar `RequiereRevisionManual`.
- Ejecutar los 38 escenarios de matriz E2E original y las regresiones del proyecto, registrar resultados, no iniciar UAT.
- WSDL CFA/AXON XSD importados pendientes: para NPT-10 solo se comprobó forma segura, no homologación de esquema.
