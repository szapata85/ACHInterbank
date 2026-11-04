# Entrega acumulativa I06-ACH — ACH Interbank

**ZIP completo:** `ACHInterbank_COMPLETO_DEVOLUCIONES_AUTOMATICAS_I06_ACHCOLOMBIA_2026-11-02.zip`.

Esta entrega conserva íntegramente los incrementos I00–I05 y agrega únicamente I06 de ACH Colombia. La generación automática de archivos queda **conectada al procesador**, pero requiere modo persistido `AUTOMATICO` y una puerta técnica **desactivada por defecto** (`AchCol:EnableAutomaticReturnFileGeneration=false`); ningún envío automático está habilitado. La SPA existente permite **descargar un archivo previamente generado** sin generar otra devolución.

**Ubicación de continuidad:**
- `docs/implementacion-devoluciones-automaticas/MEMORY_STATE.md` — estado vigente de iniciativa.
- `docs/implementacion-devoluciones-automaticas/MEMORY_STATE_I06_ACH.md` — copia congelada de este incremento.
- `docs/implementacion-devoluciones-automaticas/INFORME_I06_ACH.md` — cambios y límites.
- `docs/implementacion-devoluciones-automaticas/EVIDENCIA_I06_ACH.md` — validaciones estáticas y pendientes.
- `docs/implementacion-devoluciones-automaticas/PLAN_AJUSTE_I06_ACH.md` — siguiente alcance.
- `docs/implementacion-devoluciones-automaticas/MANIFIESTO_I06_ACH.json` — comparación e integridad.
- `scripts/test/automatic_returns_i06_ach_static_check.py` — verificador estático.

**NO-GO**: pendiente compilación .NET, pruebas xUnit, compilación Angular, BD real, semántica `ReturnedByEpr`, homologación ACH Colombia. CENIT no se modifica. **Próximo punto: I07-ACH**.
