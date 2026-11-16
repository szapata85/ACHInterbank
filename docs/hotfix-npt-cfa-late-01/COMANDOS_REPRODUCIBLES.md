# Comandos reproducibles

Desde la raíz `ACHInterbank_ajustado/`:

```bash
python scripts/test/npt_cfa_late_static_sqlite_check.py
python scripts/test/npt_focal_sqlite_source_guard.py
python scripts/test/npt_static_guard.py
python scripts/test/aud_e2e_final_static_and_sqlite_check.py
python scripts/test/e2e01_e2e02_source_static_check.py
python scripts/test/h01_h02_static_sqlite_check.py
python scripts/test/res_h02_soap_preflight_sqlite_check.py
python scripts/test/achcol_nacha_transaction_counts_static_check.py

dotnet --info
dotnet restore ACHInterbank.sln
dotnet build ACHInterbank.sln -c Release --no-restore
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build --filter 'FullyQualifiedName~NptCfaLateEvidenceTests'
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build
```

Las órdenes dotnet se intentaron en este ambiente pero fallaron con exit 127 (`dotnet` no encontrado). Las pruebas xUnit y de proveedores no se ejecutaron. Para certificar, usar instancia real aislada de SQL Server y PostgreSQL, instrumentar llamadas CFA y correr cada caso con 2 o más workers; sin Live productivo ni UAT. La simulación Python únicamente valida un modelo lógico con PK/índice transaccional SQLite.
