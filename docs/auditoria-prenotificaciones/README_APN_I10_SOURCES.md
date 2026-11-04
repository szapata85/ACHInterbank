# APN-I10 — Guía de incorporación en Sources

1. Utilizar **solo el ZIP completo APN-I10** como nueva base de continuidad. Contiene `ACHInterbank_ajustado/` con todo el proyecto y la documentación acumulada.
2. El delta APN-I10 es opcional para revisar diferencias; **no** es una base autónoma.
3. Consultar `MEMORY_STATE_APN_I10.md`, `docs/auditoria-prenotificaciones/STATUS_IMPLEMENTACIONES_I10.md`, `HALLAZGOS_I10.md` y `incrementos/APN-I10_RECEPCION_Y_DEVOLUCIONES_TARDIAS.md`.
4. No interpretar `10/10` Python estáticas como pruebas C# ejecutadas. `dotnet test`, SQL Server/PostgreSQL y UAT permanecen pendientes.
5. Reglas invariables: ACH Colombia **7** ciclos; CENIT **5**; no se fabricó un cierre de recepción de archivos inexistente en el contrato.
