# Bitácora de verificaciones APN

- `python3 scripts/audit/prenotificaciones/apn_static_audit.py`: **PASS_STATIC**. Checks `FOUND_STATIC`: 20/20; faltantes: 0.
- Estas comprobaciones identifican código presente; **no ejecutan la aplicación C#** y no homologan ninguna transacción.
- `dotnet` no está disponible en el entorno de generación; `dotnet test`, SQL Server/Postgres, Angular E2E, carga NACHA y job real: **NO_EJECUTADO**.
- Las seis señales de riesgo estático se describen en `HALLAZGOS.md`; ninguna se presenta como caso runtime observado.
- No se ejecutaron transmisiones a cámaras ni escrituras productivas.

- `python3 scripts/audit/prenotificaciones/test_apn_reference_schedule.py`: **6/6 PASS** (únicamente consistencia del fixture documental; no valida lógica C# ni cámaras).
