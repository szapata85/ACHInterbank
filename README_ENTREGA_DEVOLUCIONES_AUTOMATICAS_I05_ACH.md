# Entrega acumulativa I05-ACH — ACH Interbank

**ZIP completo:** `ACHInterbank_COMPLETO_DEVOLUCIONES_AUTOMATICAS_I05_ACHCOLOMBIA_2026-11-02.zip`.

Incluye sin recortes el código y la documentación histórica de I00–I04 y el incremento I05 de ACH Colombia. La funcionalidad nueva permite generar manualmente un `.RET` desde una candidata aprobada, con sus validaciones y su nombre oficial; **no envía el archivo ni activa generación autónoma o CENIT**.

Continuidad:
- `docs/implementacion-devoluciones-automaticas/MEMORY_STATE.md` (estado vigente de la iniciativa)
- `docs/implementacion-devoluciones-automaticas/MEMORY_STATE_I05_ACH.md`
- `docs/implementacion-devoluciones-automaticas/INFORME_I05_ACH.md`
- `docs/implementacion-devoluciones-automaticas/EVIDENCIA_I05_ACH.md`
- `docs/implementacion-devoluciones-automaticas/PLAN_AJUSTE_I05_ACH.md`
- `docs/implementacion-devoluciones-automaticas/MANIFIESTO_I05_ACH.json`
- `scripts/test/automatic_returns_i05_ach_static_check.py`

Las verificaciones estáticas no son certificación funcional. No se ejecutó dotnet, Angular build ni SQL Server. **Estado: NO-GO productivo**. Próximo único paso: I06-ACH, tras conservar este ZIP íntegro y registrar la memoria de estado.
