# Matriz de pruebas — cierre focal NPT

Clasificación estricta: `Ejecutado` = comandos realmente corridos aquí, `Escrito/pending` = xUnit incorporado pero no ejecutado.

| Escenario | Tipo | Estado | Evidencia |
|---|---|---|---|
| 26 invariantes de fuente y modelos SQLite, incluidos 2 workers SQL en dos conexiones | Python+SQLite (no .NET) | Ejecutado 26/26 | `evidencias/npt_focal_guard.log` y JSON |
| Regresión contadores NACHA T6 | Python estático | Ejecutado 28/28 | `evidencias/achcol_nacha_transaction_counts_static_check.log` |
| E2E-01/E2E-02 | Python estático+SQLite | Ejecutado 35/35 | `evidencias/e2e01_e2e02_source_static_check.log` |
| H01/H02 | Python estático+SQLite | Ejecutado 39/39 | `evidencias/h01_h02_static_sqlite_check.log` |
| RES-H02 | Python estático+SQLite | Ejecutado 27/27 | `evidencias/res_h02_soap_preflight_sqlite_check.log` |
| AUD-E2E | Python estático+SQLite | Ejecutado 31/31 | `evidencias/aud_e2e_final_static_and_sqlite_check.log` |
| Verificador NPT histórico | Python estático+SQLite | Ejecutado 41/42, falla una comprobación obsoleta | `evidencias/npt_static_guard.log` |
| Dos workers CFA, token caducado, finalización concurrente | xUnit + SQLite EF Core | Escrito/pendiente | `NptFocalCfaFencingTests.cs` |
| Dos workers Axon, expiración/watchdog, preflight frente a InFlight, finalización atómica | xUnit + SQLite EF Core | Escrito/pendiente | `NptFocalAxonFencingTests.cs` |
| SOAP operation/namespace/fault/cardinalidad/DTD/campos extra | xUnit | Escrito/pendiente | `NptFocalProcTransaccionesParserTests.cs` |
| Prenotificaciones entre archivos y sin vínculo final | xUnit | Escrito/pendiente | `DifferentialPrenotificationResponseProcessorTests.cs` |
| Cancelación, timeout, SOAP Fault, reinicio Quartz y instrumentación de POST físico | xUnit y SOAP local instrumentado | Pendiente de SDK e infraestructura | Matriz E2E original de 38 casos |
| Race cross-node SQL Server | EF .NET10 + SQL Server | Pendiente de infraestructura | No ejecutar UAT |
| Race cross-node PostgreSQL | EF .NET10 + PostgreSQL | Pendiente de infraestructura | No ejecutar UAT |
| SOAP Live productivo | Bancaria | **Prohibido en este incremento** | NO-GO |
