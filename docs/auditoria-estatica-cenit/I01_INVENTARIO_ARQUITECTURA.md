# I01 — inventario de código y arquitectura existente

## Línea base comprobada

Archivo `ACHInterbank_PROYECTO_COMPLETO_AUDITORIA_ACHCOL_I10_2026-11-01.zip`; raíz `ACHInterbank_ajustado/`. **4.541 archivos originales** y 110,1 MB descomprimidos antes de adiciones. Su inventario SHA-256 por ruta figura en `SHA256_LINEA_BASE_ACHCOL_I10.csv`. La auditoría ACH Colombia I01–I10 queda preservada íntegramente en `docs/auditoria-estatica-achcol/` sin intervenir archivos previos. Tecnología identificada: solución .NET/C# (Domain, Application, Persistence, API, External), EF Core con migraciones PostgreSQL y SQL Server, frontend Angular/TypeScript, Quartz y pruebas .NET; configuración de transporte local (no equivalencia demostrada con transporte oficial).

## Mapa de ejecución prioritario

| Capacidad | Evidencia de origen → destino (rutas relativas a raíz) | Alcance de I01 |
|---|---|---|
| Cámara y estrategia | `src/Cfa.ACHInterbank.Application/ACH/Implementation/ClearingHouseStrategyFactory.cs:8-14` → `CenitClearingHouseStrategy.cs:9-53` | Estrategia específica; `GenerateCycleFile` produce XML **pero no se encontró llamada de producción a ese método en `src/`**; no inferir que el flujo NACHA-M se genera en XML |
| Calendario y encolamiento | `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitOperatingCalendarPolicy.cs:31-126` → `CenitCycleQueueService.cs:21-47`; `AchTransactionService.cs`, `AchBulkTransactionService.cs` consumen cola | Cinco ciclos, resolución TZ/día bancario y cola están codificados; probar elegibilidad exacta en I02–I06/I08 |
| Quartz/cierre ciclos | `src/Cfa.ACHInterbank.Persistence/ACH/Quartz/Jobs/Implementation/AchCycleSchedulerHandler.cs:78-103` → `CenitCycleExecutionService.cs:45-90` → `CenitNettingService.cs` y `ILiquidityOptimizationService` | Ruta observable de scheduler a compensación/optimización; no consta en I01 resultado externo CUD |
| Devoluciones y R23 | `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchReturnsService.cs:90-148`; `src/Cfa.ACHInterbank.Application/ACH/Services/CenitIncomingReturnPolicy.cs:12-88,234-290`; `IncomingNachaPostParseProcessor.cs:444-580` | Hay validación CENIT consumida desde procesador entrante y servicio de devoluciones; no confundir catálogo con cobertura total |
| ROR | `src/Cfa.ACHInterbank.Api/Controllers/AchReturnOfReturnController.cs:17-43` → `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitReturnOfReturnService.cs:26-272` → `CenitReturnOfReturn2026Layout.cs:8-94` | Causales R60–R74, ciclos de origen/fecha y adenda; revisión individual I07 |
| NACHA-M entrada/salida | `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaFileBuilder.cs`, `NachaOfficialAssemblyPipeline.cs`, `NachaParserService.cs`, `NachaPhysicalParser.cs`, `CenitOrdinaryInbound2026Layout.cs`, `CenitOrdinaryOutbound2026Layout.cs`, `CenitCtxOutbound2026Layout.cs` | Perfiles específicos existen; no implica cobertura de todos los campos ni despliegue |
| Respuestas de cámara | `src/Cfa.ACHInterbank.Api/Controllers/CenitOperationsController.cs:48-120` → `CenitChamberResponseService.cs:24-95,296-428`; `CenitLocalGatewayPickupHostedService.cs:20-45` y `CenitLocalGatewayFolderTransportAdapter.cs` | Recepción/estado y seguimiento local; validar conciliación e idempotencia I09 |
| Persistencia | `src/Cfa.ACHInterbank.Domain/Models/ACH/Cenit*.cs`, configuraciones EF `src/Cfa.ACHInterbank.Persistence/Configuration/Cenit*.cs`, migraciones PostgreSQL/SQL Server `*Cenit*.cs` | No verificar aplicación real de migraciones sin DB |
| SOAP y archivo STA | `src/Cfa.ACHInterbank.External/Connections/WscfaachSoapClient.cs`, `WsAxonRespuestaTransaccionesSoapClient.cs`, contratos WSDL adjuntos; `NachaChamberFlowPolicy.cs` | Son contratos CFA/WSAxon, no se han probado como contrato oficial STA/CENIT/CUD; investigar fronteras I08–I09 |
| DI y separación | `src/Cfa.ACHInterbank.Persistence/DependencyInjectionService.cs:133-135,179-184`, `[Scoped]` en servicios CENIT | Registro para varias interfaces, comportamiento y despliegue no comprobados |

**Casos merecedores de rastreo (NO son hallazgos regulatorios demostrados en I01):** `CenitRawReturnContractGate.cs:9` devuelve `IsHomologated => true` y `AchIncomingReturnIngestionService.cs:45-48` lo consulta; corroborar con metadatos reales de perfil. `CenitOperatingCalendarPolicy.cs:120-126` puede retornar el último ciclo si falta un siguiente ciclo programado; evaluar horarios nocturnos y cola en I06/I08. `CenitClearingHouseStrategy.cs:42-53` emite XML pero no se halló invocación al método en código productivo; **no** es evidencia de salida NACHA errónea.

## Pruebas existentes relacionadas con CENIT

Se localizaron las siguientes pruebas/fixtures con nombre CENIT; existencia inventariada, **no ejecutadas ni aprobadas**. El conteo no equivale a cobertura normativa.

- `tests/Cfa.ACHInterbank.Tests/CenitChamberResponseLifecycleTests.cs`
- `tests/Cfa.ACHInterbank.Tests/CenitCycleCalendarCharacterizationTests.cs`
- `tests/Cfa.ACHInterbank.Tests/CenitIncomingReturnPolicyTests.cs`
- `tests/Cfa.ACHInterbank.Tests/CenitLocalGatewayFolderTransportAdapterTests.cs`
- `tests/Cfa.ACHInterbank.Tests/CenitOfficialFileNameParserTests.cs`
- `tests/Cfa.ACHInterbank.Tests/CenitOperationalGovernanceTests.cs`
- `tests/Cfa.ACHInterbank.Tests/CenitOperationsClearingHouseIsolationTests.cs`
- `tests/Cfa.ACHInterbank.Tests/CenitOrdinaryInbound2026ParserTests.cs`
- `tests/Cfa.ACHInterbank.Tests/CenitOutboundFilePartitionerTests.cs`
- `tests/Cfa.ACHInterbank.Tests/CenitProcTransaccionesPackageCharacterizationTests.cs`
- `tests/Cfa.ACHInterbank.Tests/CenitPublishedCardinalityPolicyTests.cs`
- `tests/Cfa.ACHInterbank.Tests/CenitReturnIn2026ParserTests.cs`
- `tests/Cfa.ACHInterbank.Tests/CenitReturnOfReturn2026Tests.cs`
- `tests/Cfa.ACHInterbank.Tests/CenitReturnOut2026Tests.cs`
- `tests/Cfa.ACHInterbank.Tests/TestData/Nacha/CENIT/CENIT-OUT-001.metadata.json`
- `tests/Cfa.ACHInterbank.Tests/TestData/Nacha/GoldenFiles/CENIT/Incoming/CENIT_IN_001.ach`
- `tests/Cfa.ACHInterbank.Tests/TestData/Nacha/GoldenFiles/CENIT/Outgoing/CENIT_OUT_001.ach`
- `tests/Cfa.ACHInterbank.Tests/TestData/Nacha/GoldenFiles/CENIT/Returns/CENIT_RET_001.RET`
- `tests/Cfa.ACHInterbank.Tests/TestData/Nacha/NormativeSynthetic/Cenit/Ror/CENIT_ROR_IN_001.ach`

## Archivos fuente específicamente denominados CENIT (no excluye código compartido)

- `src/Cfa.ACHInterbank.Api/Controllers/CenitOperationsController.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Configuration/CenitLocalGatewayOptions.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Implementation/CenitClearingHouseStrategy.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Implementation/PaymentRails/CenitPaymentRailOperationalStrategy.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Interfaces/ICenitChamberResponseService.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Interfaces/ICenitCycleExecutionService.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Interfaces/ICenitCycleQueueService.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Interfaces/ICenitGatewayTransportAdapter.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Interfaces/ICenitIncomingReturnPolicy.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Interfaces/ICenitNettingService.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Interfaces/ICenitOperatingCalendarPolicy.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Interfaces/ICenitRawReturnContractGate.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Interfaces/ICenitReturnOfReturnService.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Models/CenitChamberResponseModels.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Models/CenitIncomingReturnPolicyModels.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Models/CenitOutboundFilePartitioning.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Models/CenitReturnOfReturnModels.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Services/CenitIncomingReturnPolicy.cs`
- `src/Cfa.ACHInterbank.Domain/Models/ACH/CenitChamberResponse.cs`
- `src/Cfa.ACHInterbank.Domain/Models/ACH/CenitCycleExecution.cs`
- `src/Cfa.ACHInterbank.Domain/Models/ACH/CenitCycleQueue.cs`
- `src/Cfa.ACHInterbank.Domain/Models/ACH/CenitNetPosition.cs`
- `src/Cfa.ACHInterbank.Domain/Models/ACH/CenitNettingDetail.cs`
- `src/Cfa.ACHInterbank.Domain/Models/ACH/CenitNettingExecution.cs`
- `src/Cfa.ACHInterbank.Domain/Models/ACH/Enums/CenitChamberResponseEnums.cs`
- `src/Cfa.ACHInterbank.Persistence.Migrations.SqlServer/DataBase/Migrations/SqlServer/20260816160725_CenitReturnOfReturn2026.cs`
- `src/Cfa.ACHInterbank.Persistence.Migrations.SqlServer/DataBase/Migrations/SqlServer/20260831133017_OpsGap004CenitChamberResponses.cs`
- `src/Cfa.ACHInterbank.Persistence.Migrations.SqlServer/DataBase/Migrations/SqlServer/20260831183444_CenitRuntimeE2EOwnershipAndMultiError.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitChamberResponseService.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitCtxOutbound2026Layout.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitCycleExecutionService.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitCycleQueueService.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitLocalGatewayFolderTransportAdapter.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitLocalGatewayPickupHostedService.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitNettingService.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitOperatingCalendarPolicy.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitOrdinaryInbound2026Layout.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitOrdinaryOutbound2026Layout.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitRawReturnContractGate.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitReturnIn2026Layout.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitReturnOfReturn2026Layout.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitReturnOfReturnService.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitReturnOut2026Layout.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ExternalFileNames/CenitOfficialFileNameParser.cs`
- `src/Cfa.ACHInterbank.Persistence/Configuration/CenitChamberResponseConfiguration.cs`
- `src/Cfa.ACHInterbank.Persistence/Configuration/CenitCycleExecutionConfiguration.cs`
- `src/Cfa.ACHInterbank.Persistence/Configuration/CenitCycleQueueConfiguration.cs`
- `src/Cfa.ACHInterbank.Persistence/Configuration/CenitNetPositionConfiguration.cs`
- `src/Cfa.ACHInterbank.Persistence/Configuration/CenitNettingDetailConfiguration.cs`
- `src/Cfa.ACHInterbank.Persistence/Configuration/CenitNettingExecutionConfiguration.cs`
- `src/Cfa.ACHInterbank.Persistence/DataBase/Migrations/Postgres/20260816160605_CenitReturnOfReturn2026.cs`
- `src/Cfa.ACHInterbank.Persistence/DataBase/Migrations/Postgres/20260831132903_OpsGap004CenitChamberResponses.cs`
- `src/Cfa.ACHInterbank.Persistence/DataBase/Migrations/Postgres/20260831183322_CenitRuntimeE2EOwnershipAndMultiError.cs`

**Criterio de amplitud:** además de esta lista revisar por búsqueda transversal de llamadas toda clase compartida `Nacha*`, `Ach*`, `Liquidity*`, `Cycle*`, calendarios, endpoints, `appsettings`, migraciones, seeding, interfaces y tests. La lista de nombres CENIT **no** constituye por sí sola un mapa completo de dependencias.
