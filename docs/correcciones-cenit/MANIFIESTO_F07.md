# Manifiesto F07 — 2026-11-02

- Origen: ZIP F06, SHA-256 `a43539f83b8b6cdc5e36cbadf5d9334dc578bd88ddf2d22cd5acf088ffc8b76d`, 4.651 entradas, comparación CRC y contenidos verificada.
- Originales modificados intencionalmente (7): una prueba xUnit C# y seis documentos de continuidad (ver `F07_COMPARACION_SHA256.tsv`). Cero productivos, cero migraciones, cero WSDL, cero componentes específicos ACH Colombia.
- Nuevos archivos (9): `F07_INFORME_REGRESION.md`, `F07_MEMORY_STATE.md`, `F07_CAMBIOS.patch`, `F07_COMPARACION_SHA256.tsv`, `F07_EVIDENCIA_REGRESION_ESTATICA.txt`, `MANIFIESTO_F07.md`, `CenitF07CrossPhaseRegressionTests.cs`, `cenit_f07_regression_check.py`, `run_cenit_f07_dotnet_regression.sh`.
- Total esperado de entradas F07: **4.660**; sin eliminaciones. ZIP SHA-256 definitivo solo en `.zip.sha256` lateral.
- Ejecutadas: F07 24/24, F01–F06 113/113 verificaciones estáticas; CRC/SHA-256 y parche xUnit aplicable/reversible. xUnit reales: 0 ejecutadas; CI depende de SDK .NET 10 y restauración.
- Hallazgos: 13 implementados pendientes de verificación; 0 formalmente cerrados; no se declara homologación. Siguiente F08.
