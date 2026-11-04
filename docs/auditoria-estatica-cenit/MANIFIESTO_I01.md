# MANIFIESTO_I01 — control de integridad y alcance

- **Incremento:** I01 inventario y plan, **COMPLETADO**. Fecha documental 2026-11-01.
- **ZIP línea base:** `ACHInterbank_PROYECTO_COMPLETO_AUDITORIA_ACHCOL_I10_2026-11-01.zip`.
- **SHA256 ZIP línea base:** `0898403542681a478c636c4b5744991c7ef7c875568ed0a1ce47fd096b388858`.
- **Raíz interna mantenida:** `ACHInterbank_ajustado/`.
- **Archivos originales en I10:** 4541; **sin modificación verificada:** 4541; **alterados:** 0; **eliminados:** 0; archivos originales duplicados en ZIP: 0.
- **Nueva carpeta:** `docs/auditoria-estatica-cenit/` exclusivamente; **archivos nuevos:** 18 (incluye este manifiesto).
- **Fuentes binarias externas añadidas sin alterar:** 7 (PDF, WSDL, documento de diseño técnico). Transcripciones Markdown normativas ya existentes en `docs/normativa/md/` verificadas idénticas a las adjuntas.
- **Inventario HASH de línea base:** `SHA256_LINEA_BASE_ACHCOL_I10.csv` (4.541 filas, además de encabezado). **Adiciones:** `SHA256_ADICIONES_I01.csv`, todas excepto estos índices y el manifiesto para evitar auto-referencia.
- **Conservación:** todo `src/`, `tests/`, `web/`, `database/`, `docs/auditoria-estatica-achcol/`, configuración, contrato original y demás rutas originales quedan inalterados.
- **Pruebas no ejecutadas:** .NET, DB, Quartz, CUD, CENIT, MFT/SFTP, certificación externa. Tampoco se ejecutaron suites existentes.
- **Reglas preliminares:** 12, 8 parciales + 4 no demostrables; **hallazgos estáticos demostrados:** 0.
- **Verificación de archivo de salida:** generar ZIP completo, hacer `ZipFile.testzip()` sin errores, comparar hash de **cada** archivo original contra el ZIP y confirmar nuevas rutas. El SHA del ZIP emitido se reportará fuera del ZIP (auto-referencia no posible).

**Alcance jurídico:** consulta oficial localizada en 2025, no prueba exhaustiva de todas las comunicaciones posibles. La edición NACHA-M 2026 proviene del documento aportado. No afirmar despliegue/homologación.
