# Evidencias de ejecución — APN-I11

**Corte:** 2026-11-03. Las pruebas se realizaron sobre fuentes extraídas íntegramente de APN-I10 y modificadas para I11.

| Suite | Resultado real | Interpretación |
|---|---|---|
| Python APN-I08 | 6/6 PASS | Contratos estáticos de plazo |
| Python APN-I09 | 8/8 PASS | Contratos estáticos de barreras |
| Python APN-I10 | 10/10 PASS | Contratos estáticos multicanal |
| Python APN-I11 | 12/12 PASS | 9 inspecciones estáticas y 3 ejemplos independientes de fechas |
| Calendario normativo Python | 6/6 PASS | Consistencia de matrices documentales de ACH Colombia/CENIT |
| `dotnet restore` / `dotnet test` | NO_EJECUTADO, código 127 | No existe `dotnet` en este entorno. No se certifican pruebas C# |
| SQL Server, PostgreSQL, Quartz / UAT | NO_EJECUTADOS | No hay instancias/credenciales/canales de homologación accesibles |

**Total Python:** 42/42 pruebas ejecutadas y aprobadas; no equivalen a 42 pruebas de código C#.

**C# añadidas:** 8 casos escritos, **0 ejecutados**. Su paso a PASS depende de compilación/ejecución real.

Cada resultado está guardado en `docs/auditoria-prenotificaciones/evidencias/APN_I11_TEST_*.log` y se entrega dentro del ZIP completo.

La ausencia de SDK no detuvo la preparación de fuentes y paquetes, pero se conserva la condición **NO_HOMOLOGADO**.
