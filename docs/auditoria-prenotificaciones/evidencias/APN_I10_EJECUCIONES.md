# Evidencia de ejecución APN-I10

- `python -m unittest discover -s scripts/audit/prenotificaciones -p test_apn_i10_cross_channel.py -v` -> **10/10 PASS_ESTÁTICO**.
- Regresión `test_apn_i09_safety.py` -> **8/8 PASS_ESTÁTICO**.
- Regresión `test_apn_i08_deadline_guard.py` -> **6/6 PASS_ESTÁTICO**.
- Compilación y `dotnet test`: **NO_EJECUTADO**. `dotnet` no está instalado en este entorno.
- SQL Server/PostgreSQL/Quartz/UAT: **NO_EJECUTADOS**.
- Los scripts Python verifican presencia de guardias/código, no su comportamiento a runtime. Las pruebas xUnit C# quedan incluidas para ejecución en CI.
- El ZIP completo se contrasta contra el ZIP I09 con hashes SHA-256 por entrada; se registra el resultado en `APN_I10_INTEGRIDAD.md` y el manifiesto JSON.
