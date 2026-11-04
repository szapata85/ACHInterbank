# APN-I11 — Guía de Sources (proyecto completo)

1. Subir **el ZIP completo APN-I11**. No usar APN-I10 ni el delta como base nueva.
2. Preserva `ACHInterbank_ajustado/`, todo el código recibido y los Memory State anteriores.
3. Leer `MEMORY_STATE_APN_I11.md`, `docs/auditoria-prenotificaciones/STATUS_IMPLEMENTACIONES_I11.md`, `HALLAZGOS_I11.md`, el informe I11 y el contrato de cierre propuesto.
4. ACH Colombia: 7 ciclos. CENIT: 5 sesiones. No equiparar una hora de corte con la recepción final ni una recepción vacía con prueba de completitud.
5. Se ejecutaron **42/42** pruebas Python de fuentes y fechas. Se escribieron **8 pruebas C# nuevas**, **no ejecutadas** sin .NET SDK 10. SQL, Quartz y UAT pendientes.
6. APN-R07, ausencia de prueba externa positiva de fin de entrega, continúa ABIERTO. El contrato de cierre **no está integrado**.
7. Cambios productivos I11: calendario ignora ciclos cancelados, recibe fechas en ventana local/UTC correcta y retiene CENIT negativas ambiguas de sesiones de devolución distintas a la original.
