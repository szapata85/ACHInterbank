# MANIFIESTO F03 — Integridad y reversibilidad

- Fecha de generación: 2026-11-02
- ZIP base: `ACHInterbank_PROYECTO_COMPLETO_CORRECCIONES_CENIT_F02_2026-11-02.zip`.
- SHA-256 ZIP base: `361d8d4a8e3d14313283a50155cfbc9633c37f5bd1f69e5755f09e68720e31a4`.
- Raíz: `ACHInterbank_ajustado/`.
- Entradas originales ZIP F02: 4620.
- Ficheros originales modificados intencionalmente: 9 (4 código + 5 documentos).
- Ficheros nuevos antes del manifiesto: 7.
- Ficheros eliminados: 0.
- Comparación por archivo: `F03_COMPARACION_SHA256.tsv`; confirma hashes de todos los miembros originales al construir F03.
- Parche original F02→F03 reversible: `F03_CAMBIOS.patch` (comprobación `patch --dry-run -R -p1` contra copia de las cuatro fuentes modificadas en LF).
- Tests ejecutados realmente: 17/17 F03, 13/13 F02, 12/12 F01 scripts fuente; no .NET ni SQL.
- Cámara ACH Colombia: ningún cambio en archivos dedicados ACH Colombia; cambio compartido de generador condicionado por código `CENIT`.
- No integraciones CUD, ni migraciones, ni cambios WSDL ni auditorías históricas.
- SHA-256 del ZIP final fuera del ZIP: archivo `.zip.sha256` entregado conjuntamente; no se autocita dentro del ZIP.

## Archivos productivos modificados
- `src/Cfa.ACHInterbank.Application/ACH/Interfaces/ICenitCycleQueueService.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitCycleQueueService.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaFileBuilder.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaFileGenerationCoordinator.cs`

## Riesgos residuales

F03 interdiaria con fecha valor discordante requiere revisión F04; xUnit/SQL, paralelismo de exportación y prueba externa pendientes. HC-001 no está cerrado.
