# ACH Interbank — Entrega acumulativa I04-ACH

Base: ZIP completo I03-ACH. Este paquete entrega el **proyecto completo**, no solo un parche. Conserva la memoria histórica y añade revisión/aprobación interna en la pantalla de devoluciones exclusivamente para ACH Colombia. La consulta y resolución está autorizada, la causal y la ventana se revalidan antes de aprobar, y no existe transmisión, archivo `.RET` ni asiento financiero a partir de esta acción.

Documentación principal:
- `docs/implementacion-devoluciones-automaticas/MEMORY_STATE.md` (vigente de iniciativa)
- `docs/implementacion-devoluciones-automaticas/MEMORY_STATE_I04_ACH.md` (corte histórico)
- `docs/implementacion-devoluciones-automaticas/INFORME_I04_ACH.md`
- `docs/implementacion-devoluciones-automaticas/EVIDENCIA_I04_ACH.md`
- `docs/implementacion-devoluciones-automaticas/MANIFIESTO_I04_ACH.json`

Pruebas estáticas reproducibles: `python scripts/test/automatic_returns_i04_ach_static_check.py`. Las pruebas xUnit/Angular están incorporadas pero sin ejecutar por ausencia de entorno. **Producción: NO-GO.** Próximo: I05-ACH, sin comenzar CENIT.
