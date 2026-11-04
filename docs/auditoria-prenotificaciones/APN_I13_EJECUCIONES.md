# APN-I13 — Resultados reproducibles

**Entorno:** 2026-11-03. No se dispone de SDK dotnet 10.

| Componente | Resultado | Naturaleza |
|---|---|---|
| Python I08 | 6/6 PASS | Contratos estáticos |
| Python I09 | 8/8 PASS | Contratos estáticos |
| Python I10 | 10/10 PASS | Contratos estáticos |
| Python I11 | 12/12 PASS | Contratos y fechas |
| Python I12 | 12/12 PASS | Contratos y modelo matemático independiente |
| Matriz de siete ciclos ACH Colombia y cinco de CENIT | 6/6 PASS | Fixture documental |
| Python I13 | 6/6 PASS | Contratos + SQLite SQL **independiente** |
| **TOTAL Python** | **60/60 PASS** | **No compila ni ejecuta C#** |
| xUnit I09–I13 | NO_EJECUTADO (exit 127) | SDK .NET ausente |
| SQLite EF Core tests I13 | NO_EJECUTADO | Requiere .NET |
| SQL Server/PostgreSQL | NO_EJECUTADO | Sin instancias homologadas |
| Quartz/concurrencia/archivos cámaras/UAT | NO_EJECUTADO | Sin ambiente e integraciones |

Logs individuales: `docs/auditoria-prenotificaciones/evidencias/APN_I13_*.log`.

**Nota de alcance:** el ejemplo `sqlite3` en Python únicamente verifica el comportamiento de una tabla de dos columnas y restricción única; no verifica el mapeo EF Core ni la consulta `UNION` real. La suite SQLite C# nueva es una prueba relacional más representativa, pero no se ha ejecutado.

**Comando de homologación:** `bash scripts/audit/prenotificaciones/run_apn_i13_dotnet.sh` en un host con SDK .NET 10.0.403 y restauración de dependencias. Requiere después pruebas SQL Server y PostgreSQL si se pretende certificar traducción y concurrencia productivas.
