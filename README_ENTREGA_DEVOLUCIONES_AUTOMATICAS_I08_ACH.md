# ACH Interbank — Entrega acumulativa I08-ACH

**Base:** I07 completo. **I08:** verificación previa al commit del archivo NACHA-M generado autónomamente por ACH Colombia, comparando byte a byte contra reconstrucción del registro original. En caso de discrepancia, bloqueo controlado, con reversión mediante transacción de BD relacional existente (pendiente de verificar con BD real). No cambia CENIT, SPA, generación manual, transporte ni liquidación. Flag técnico `false`. **Estado: NO-GO**.

Documentación: `docs/implementacion-devoluciones-automaticas/{MEMORY_STATE.md,INFORME_I08_ACH.md,EVIDENCIA_I08_ACH.md,MATRIZ_UAT_I08_ACH.md,PLAN_AJUSTE_I08_ACH.md,MANIFIESTO_I08_ACH.json}`. Pruebas xUnit I08 y script `scripts/test/automatic_returns_i08_ach_static_check.py` incluidos; xUnit no ejecutadas sin SDK .NET.

**Punto de parada:** I08-ACH. **Siguiente:** I09-ACH, pruebas relacionales y de archivos golden, validación de reportes y homologación externa antes de cualquier GO.
