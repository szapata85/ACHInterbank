# Manifiesto F08 — 2026-11-02

- **Origen:** ZIP F07 SHA-256 `86e4ea5ccf30b1155fbc3a9477889bc611b499d326210b2f042fe5bd99b2c5f0`, **4.660 entradas**, CRC OK y comparación 4.660/4.660 con árbol fuente antes de cambios.
- **Modificados intencionalmente (6):** MEMORY_STATE.md, PLAN_INCREMENTAL_CORRECCIONES.md, ESTADO_FASES.md, MATRIZ_HALLAZGOS_CORRECCIONES.md, REGISTRO_PRUEBAS.md, REGISTRO_CORRECCIONES.md — exclusivamente en `docs/correcciones-cenit/`.
- **Nuevos (11):** 10 documentos F08_DICTAMEN_FINAL_CONDICIONADO.md, F08_MATRIZ_CIERRE_HALLAZGOS.md, F08_PLAN_PRUEBAS_Y_APROBACIONES.md, F08_CHECKLIST_GO_NO_GO.md, F08_INFORME_ENTREGA.md, F08_MEMORY_STATE.md, F08_EVIDENCIA_CONSOLIDADA.txt, F08_CAMBIOS.patch, F08_COMPARACION_SHA256.tsv, MANIFIESTO_F08.md; 1 script `scripts/test/cenit_f08_closure_audit.py`.
- **Entradas finales esperadas F08:** **4.671**; **4.660 originales conservadas**, sin eliminaciones, sin renombrados, sin entradas duplicadas. Se comparan SHA-256 de todos los archivos, no solo el ZIP.
- **Cambios productivos:** 0; **C# tests modificados:** 0; DB/migraciones/WSDL/CUD/ACH Colombia exclusivos: 0. Auditorías históricas I01–I10 y correcciones F01–F07 intactas.
- **Pruebas ejecutadas:** F01–F07 137/137 estáticas/fixtures PASS, F08 17/17 documentales PASS; **154/154** de estas comprobaciones. xUnit **0 ejecutadas**, SDK ausente, **PT-01–PT-22 pendientes**.
- **Dictamen:** 13 HC implementados **ABIERTOS / NO VERIFICADOS DINÁMICAMENTE**; 0 cerrados. **NO-GO** producción/homologación. 8/8 entregables del programa completo, NO 100 % de certificación.
- **Parche reversible:** `F08_CAMBIOS.patch` revierte solo los seis documentos existentes; restauración completa mediante ZIP F07. Los 11 nuevos se retiran si se revierte incrementalmente.
- **Hash del ZIP F08:** se escribe únicamente en archivo lateral `.zip.sha256` una vez generado, evitando autorreferencia. Integridad final documentada externamente en `F08_VERIFICACION_INTEGRIDAD.txt`.
