# F02 — Respuestas oficiales CENIT, NACK individuales y ADV

**Fecha:** 2026-11-02. **Fase:** F02 (2/8 = 25 % de hitos). **Base:** ZIP F01, SHA-256 `8beee25554db99142677d6c084cb08cb9cf864967e864cbc61dd6c72459084e1`. **Raíz:** `ACHInterbank_ajustado/`.

## Resultado y alcance técnico

HC-CENIT-013: el XML FileNack con `TraceNo` único ahora cambia `AchTransaction.State` a `ReturnedByOperator` y crea `AchTransactionStateEvent` con fuente operador y causal, en el mismo `SaveChangesAsync` que la evidencia de cámara. El estado `AchFileExport.ChamberResponseState` no se muta con un NACK individual. Si la traza falta/ambigua/no coincide, no se cambia la transacción; un ACK técnico de archivo puede coexistir con NACK individual. Idempotencia por respuesta y por estado, sin duplicar eventos en reimportación secuencial. El rechazo técnico **global** de archivo conserva su ruta anterior sin aplicarse indiscriminadamente a una transacción específica.

HC-CENIT-012: la ruta existente Gateway→`CenitChamberResponseService.ImportAsync` ahora admite `ADV` y `NACHAMADV`, así como metadatos anteriores `Reconciliation` cuyo contenido sea 280/ADV. Analiza registros de 106 caracteres, lote SEC `ADV` y clase `280`, transacciones aviso 81–88, fecha valor, identificación del aviso, referencia, importes y totales de control; persiste evidencia por renglón en `CenitChamberResponses` existente correlacionada a un ciclo CENIT identificado. No crea migraciones ni asientos, y no consulta cuentas CUD; la cuenta física permanece en el archivo del operador, mientras que la descripción de conciliación conserva importes/código/ruta/fecha/identificador. La asociación a ciclo se exige explícitamente para evitar inferencia de liquidación.

**Ruta productiva revisada:** `CenitLocalGatewayFolderTransportAdapter` lee metadatos y contenido → `CenitLocalGatewayPickupHostedService` llama a `ICenitChamberResponseService` → `CenitChamberResponseService.Parse/ParseAdv` y `ImportItemAsync` → EF Core `AchTransaction`, `AchTransactionStateEvents`, `CenitChamberResponses` y `AchFileExport`. Los dos componentes gateway no requieren cambios: transportan `MessageType` existente. ACH Colombia sin modificación de código.

## Evidencia y limitaciones

**Código:** un archivo de servicio modificado, una suite xUnit ampliada, un script estático nuevo, documentos/patch/manifiesto. **Pruebas C# creadas:** 9 casos (7 Fact + 2 InlineData), 3 tests existentes extendidos, total F02-T01–T10 según registro (F02-T05 es test existente). **Ejecutadas:** 13/13 controles estáticos F02 + 12/12 F01 PASS; .NET/xUnit 0 ejecutadas (SDK no disponible), sin pruebas fallidas ejecutadas. **Regresión ACH Colombia:** comparación SHA-256 del ZIP base; no prueba funcional. **Estado:** HC-012 y 013 implementados, pendientes de verificación; 0 cerrados. Base F01 permanece implementada pendiente de verificación. 

**Pendientes:** compilación, ejecución xUnit, pruebas de base SQL bajo concurrencia/rollback, terceros prenotificación, validación de importación ADV real y su conciliación con Tesorería, controles físicos NACHA-M completos F06, gateway real externo, PT-13/PT-19/PT-20. No atribuir carácter definitivo a ACK ni a ADV ni declarar certificación CENIT. **CUD fuera de alcance:** revisión de Tesorería manual externa.

**Reversión:** `F02_CAMBIOS.patch` invertido en las dos rutas y ZIP F01 para revertir nuevos documentos/scripts. **Siguiente fase:** F03 HC-CENIT-001, cola entre ventanas y ciclo correcto, con incorporación confirmada y reintentos idempotentes.
