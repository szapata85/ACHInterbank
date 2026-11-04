# APN-I12 — Resultados de ejecución (corte 2026-11-03)

Se ejecutaron seis suites Python **contra los fuentes de APN-I12**. Resultados guardados en `docs/auditoria-prenotificaciones/evidencias/APN_I12_*.log`.

| Verificación | Resultado | Qué mide |
|---|---|---|
| Python APN-I08 | 6/6 PASS | Contratos estáticos de cálculo de plazo |
| Python APN-I09 | 8/8 PASS | Contratos estáticos de barreras de aceptación |
| Python APN-I10 | 10/10 PASS | Contratos estáticos multicanal |
| Python APN-I11 | 12/12 PASS | 9 contratos estáticos + 3 fechas independientes |
| Python **APN-I12** | **12/12 PASS** | 9 contratos estáticos + 3 ejemplos matemáticos independientes del cursor |
| Matrices normativas | 6/6 PASS | Casos documentales ACH Colombia / CENIT |
| **Total Python** | **54/54 PASS** | No ejecuta C#, EF, SQL, Quartz ni canales |
| `run_apn_i12_dotnet.sh` | **NO_EJECUTADO / exit 127** | SDK dotnet 10.0.403 no disponible |
| xUnit C# I12 (métodos nuevos) | **4 métodos, 0 ejecutados** | No hay resultado TRX certificado |
| SQL Server / PostgreSQL | **NO_EJECUTADO** | No hay ambiente/credenciales de integración |
| Rechazos reales / UAT / Quartz | **NO_EJECUTADO** | No se accedió a cámaras ni reloj de scheduler de prueba |

No se debe transformar esta evidencia en un estatus `HOMOLOGADO`. Las consultas EF de APN-I12 **aún deben probarse** en SQL Server y PostgreSQL antes de habilitar su uso productivo.

## Reproducción

```bash
cd ACHInterbank_ajustado
python scripts/audit/prenotificaciones/test_apn_i12_candidate_scan.py
bash scripts/audit/prenotificaciones/run_apn_i12_dotnet.sh
```
