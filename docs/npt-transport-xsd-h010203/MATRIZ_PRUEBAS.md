# Matriz de pruebas — H-01/H-02/H-03

| Prueba | Estado | Evidencia |
|---|---|---|
| WSDL/XSD oficiales | PASÓ offline 38/38 | `evidencias/wsdl_xsd_verification.log` |
| Source guards previos | PASÓ estático 33/33 | `evidencias/source_guards.log` |
| Fuente+XSD focal H010203 | PASÓ offline 21/21 | `evidencias/offline_h010203.json` |
| Siete scripts regresión | PASARON 7/7 | `evidencias/{counts,e2e,historical,post_e2e,focal,pre_xsd,late}.log` |
| NPT general heredado | FALLÓ 40/42 | `evidencias/npt_general.log`; dos discrepancias heredadas |
| XML arbitrario CFA/Axon debe generar cero POST físicos | ESCRITO, NO EJECUTADO xUnit | Caracterización de ambos clientes |
| Tipos, duplicados, namespaces, DTD/XXE y SOAP 1.2 | ESCRITO, NO EJECUTADO xUnit | Caracterización de ambos clientes |
| `NCTAORIG` ausente/vacío/nil bloquea Live | ESCRITO, NO EJECUTADO xUnit | `ProcTransaccionesOriginAccountSafetyTests` |
| Readiness contrato no homologado | ESCRITO, NO EJECUTADO xUnit | `NachaDesagregadoIntegrationMappingTests` |
| dotnet info/restore/build/test | INTENTADO / NO DISPONIBLE (127) | `evidencias/dotnet_*.log` |
| SQL Server y PostgreSQL | PENDIENTE infraestructura | Sin ejecución |
| UAT, endpoint bancario Live | NO EJECUTADO | Prohibido en este incremento |
