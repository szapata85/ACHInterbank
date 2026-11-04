# ACH Interbank — Entrega acumulativa I03-ACH

**Cámara:** ACH Colombia. **Trabajo:** candidatas de devolución basadas en rechazos funcionales I02; reserva única por transacción original; comprobación de plazos históricos y siete ciclos.

**No habilitado:** generación NACHA-M automática, transmisión, compensación, débito/crédito de cuentas, UI de aprobación. CENIT no modificado funcionalmente.

La documentación y el punto de parada están en `docs/implementacion-devoluciones-automaticas/MEMORY_STATE.md`, `INFORME_I03_ACH.md` y `EVIDENCIA_I03_ACH.md`.

**Ejecutado:** grafo 7/7; I02 18/18 PASS_STATIC; I03 25/25 PASS_STATIC. **No ejecutado:** .NET, SQL Server, xUnit y homologación. Próximo incremento I04-ACH. No producción.
