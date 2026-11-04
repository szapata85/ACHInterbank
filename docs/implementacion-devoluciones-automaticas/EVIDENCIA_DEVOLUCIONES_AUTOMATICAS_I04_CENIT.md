# EVIDENCIA — I04-CENIT

**Fecha:** 2026-11-03. **Tipo de certificación:** NINGUNA. **Estado:** cambios incorporados a fuente; producción NO-GO.

| Comprobación | Resultado real |
|---|---|
| Integridad del ZIP I03 (4.903 entradas) | PASS_ARCHIVE (`unzip -tq`) |
| Hash SHA-256 de entrada vs `.sha256` | PASS_HASH (`31bf10972c6b7e2a5dd087e44fcde15d298ac0ec00ae0c1cfa1dac3a215224e2`) |
| Verificador estático nuevo I04-CENIT | PASS_STATIC 71/71 |
| Regresión I01, I02, I03-CENIT | PASS_STATIC 29/29, 50/50, 57/57 |
| Regresión ACH I02, I03, I05–I08 | PASS_STATIC 18/18, 25/25, 33/33, 28/28, 24/24, 25/25 |
| Regresión ACH I04 histórica | **FAIL_STATIC 32/35**, arrastrado; sin intervención en código ACH |
| Código de xUnit I04 | INCLUIDO, NO EJECUTADO |
| Compilación .NET 10 | NO EJECUTADA: SDK ausente |
| Migración PostgreSQL y SQL Server con BD real | NO EJECUTADA |
| Concurrencia real/aislamiento serializable/índices | NO EJECUTADA |
| Contrato JWT, roles, segregación de personas end-to-end | NO EJECUTADA |
| Angular/SPA y prueba UAT | NO EJECUTADA; la SPA no se modificó |
| Homologación CENIT / transmisión / conciliación | NO EJECUTADA; transmisión fuera del alcance |

Pruebas xUnit incorporadas: segunda persona, mismo preparador bloqueado, identidad sin auditoría, descarte terminal pese a desactivación, modo/ventana cambiantes, evidencia SOAP más reciente, causal especial R23, decisiones inválidas, ausencia de autenticación, endpoint de excepciones sin efectos secundarios, modelo de concurrencia, conservación de ACH Colombia y ausencia de retornos generados.

**Importante:** los controles 71/71 buscan patrones de código y consistencia estática, no ejecutan C# ni demuestran que el código compile. `AuditLogs` depende de la identidad que entregue JWT; identidades alias, revocación de roles y auditoría transaccional deben probarse en el ambiente final. La validación de soporte documental solo comprueba un identificador de referencia, no su autenticidad.

Procedimiento sugerido cuando exista ambiente: `dotnet restore ACHInterbank.sln`; `dotnet build ACHInterbank.sln --no-restore`; `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj --filter FullyQualifiedName~CenitReturnReviewI04Tests`; aplicar/verificar migración I04 en BD descartable de cada proveedor; ejecutar dos POST concurrentes con dos usuarios y confirmar exactamente una transición y una auditoría de modificación; comprobar que ninguna tabla de retorno generado, despacho o movimiento cambió. No ejecutar automáticamente esos procedimientos en producción.
