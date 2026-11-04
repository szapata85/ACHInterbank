# MANIFIESTO I08 — auditoría estática CENIT

- **Incremento:** I08, completado 2026-11-01; base `ACHInterbank_PROYECTO_COMPLETO_AUDITORIA_CENIT_I07_2026-11-01.zip`.
- **Archivos base:** 4584. **ZIP I08 esperado:** 4588. **Archivos base perdidos:** 0.
- **SHA-256 vs I07**: 4579 ficheros originales idénticos byte a byte; **5 archivos modificados**, todos exclusivamente documentación acumulativa CENIT; ningún código, configuración, prueba, contrato ni normativa original alterado.
- **Documentos actualizados:** `docs/auditoria-estatica-cenit/PLAN_INCREMENTAL.md`, `MEMORY_STATE.md`, `ESTADO_INCREMENTOS.md`, `REGISTRO_HALLAZGOS.md`, `FUENTES_NORMATIVAS_Y_DIFERENCIAS.md`.
- **Documentos añadidos:** `I08_REGLA_A_REGLA.md`, `I08_MEMORY_STATE.md`, `MANIFIESTO_I08.md`, `SHA256_ADICIONES_I08.csv` en el mismo directorio.
- **Reglas I08:** 55: 16 cumplimiento estático en ruta delimitada, 29 parcial, 7 incumplimiento estático demostrado en ruta, 3 no demostrable. Siete filas incumplidas afectan cuatro causas raíz: `HC-CENIT-001`, `HC-CENIT-010`, `HC-CENIT-011` y `HC-CENIT-012`.
- **Hallazgos:** 3 nuevos I08; acumulados 12; abiertos 12; cerrados 0.
- **Integridad:** entrada ZIP CRC comprobada; comparación SHA-256 archivo por archivo. ZIP I08 verificado tras compresión con CRC y hashes SHA-256 del contenido reextraído. Sin tests .NET, DB, CUD, Quartz, STA ni certificación.
- **Regla de continuidad:** para I09 usar este ZIP completo con `MEMORY_STATE.md` e `I08_MEMORY_STATE.md`. No reconstruir trabajos previos ni tocar ACH Colombia.
- **Autorreferencia de hashes:** `SHA256_ADICIONES_I08.csv` firma únicamente las dos adiciones documentales principales; no se firma a sí mismo ni al manifiesto para evitar autorreferencia.
