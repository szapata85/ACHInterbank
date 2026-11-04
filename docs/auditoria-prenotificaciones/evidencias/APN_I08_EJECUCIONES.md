# Ejecución y limitaciones APN-I08 — 2026-11-03

- Fuente base: `ACHInterbank_PROYECTO_COMPLETO_APN_PRENOTIFICACIONES_FINAL_2026-11-03.zip`.
- `python3 scripts/audit/prenotificaciones/test_apn_i08_deadline_guard.py`: **6 pruebas de inspección estática y horarios documentales APROBADAS**.
- `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj --filter FullyQualifiedName~ThirdPartyAccountValidationLifecycleTests`: **NO_EJECUTADO** (`bash: dotnet: command not found`; código de salida 127).
- Pruebas C# nuevas incorporadas al repositorio, **NO HOMOLOGADAS**; no se debe interpretar como 4/4 PASS.
- SQL Server/PostgreSQL, banco receptor, recepción real de archivos NACHA-M y concurrencia con Quartz: **NO_EJECUTADO**.
- Sin cambios reales a base de datos de usuario ni a la infraestructura.
- El requisito de cierre de APN-R01 exige una barrera de archivos procesados y revisión de plazos canónicos persistidos; no se presume resuelto con esta medida parcial.
