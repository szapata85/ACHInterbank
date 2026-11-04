# MANIFIESTO_I04 — ACH Colombia ciclo 3

- Archivo entregado: `ACHInterbank_PROYECTO_COMPLETO_AUDITORIA_ACHCOL_I04_2026-11-01.zip`.
- Base: `ACHInterbank_PROYECTO_COMPLETO_AUDITORIA_ACHCOL_I03_2026-11-01.zip`.
- **Proyecto completo**, no parche: se retienen todos los archivos del I03 y sus incrementos I01–I03.
- Archivos nuevos: `incrementos/I04_CICLO_3_REGLA_A_REGLA.md`, `incrementos/I04_MEMORY_STATE.md`, `MANIFIESTO_I04.md`.
- Archivos vivos actualizados: `README.md`, `PLAN_INCREMENTAL.md`, `ESTADO_INCREMENTOS.md`, `MEMORY_STATE.md`.
- `src/`, `tests/`, frontend y archivos de configuración: **sin cambios** en I04; no se implementaron correcciones de producción.
- Estado: **I04 terminado**, **4/10 hitos**, **6** por completar. C3: 16 reglas (6 TRAZADA, 5 PARCIAL, 4 NO_DEMOSTRADA, 1 INCUMPLIMIENTO_CONFIRMADO en la ruta R10, extensión H07). H08 nuevo **no demostrado**.
- Integridad exigida para entregar: `ZipFile.testzip() is None`, mismos bytes para todas las entradas I03 excepto los cuatro docs vivos, mismos nombres y sin duplicados; fotografía I04 coincide con `MEMORY_STATE.md` salvo el encabezado.
- Límite: prueba dinámica .NET, DB, Quartz, MFT y homologación ACH **no ejecutadas**. No bloquea avance estático ni equivale a certificación.
