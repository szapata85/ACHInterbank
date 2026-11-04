# Verificación de integridad de entrega APN

**Archivo base:** `ACHInterbank_COMPLETO_DEVOLUCIONES_AUTOMATICAS_I06_CENIT_2026-11-03.zip`  
**SHA-256 del ZIP original:** `9235eaff8469c5f4a487b4df9c5fda238845e19e1d57a0548d35cc2828f2e2f7`  
**Archivos originales:** 4.949 (incluye archivos de código, pruebas, frontend y documentación histórica).

La comparación de nombre + CRC32 + tamaño de cada archivo original frente a cada ZIP incremental confirma que ningún archivo de la línea base se alteró ni fue omitido. Las incorporaciones APN son archivos nuevos en `docs/auditoria-prenotificaciones/`, `scripts/audit/prenotificaciones/` y `MEMORY_STATE_APN.md`.

**Alcance de verificación:** integridad del empaquetado, no ejecución del código productivo ni cumplimiento de las cámaras. Los SHA-256 de los ZIP incrementales entregados se registran fuera de los propios ZIP en `APN_MANIFIESTO_ENTREGAS_SHA256.csv`.
