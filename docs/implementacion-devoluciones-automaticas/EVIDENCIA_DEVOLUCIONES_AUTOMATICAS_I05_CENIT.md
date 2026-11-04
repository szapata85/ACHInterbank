# EVIDENCIAS I05-CENIT — FUENTE, NO HOMOLOGACIÓN

**Fecha:** 2026-11-03. **ZIP base:** `ACHInterbank_COMPLETO_DEVOLUCIONES_AUTOMATICAS_I04_CENIT_2026-11-03.zip` (SHA-256 `6b6bfeb57ede872f8c0dcde8aa2cd02fd038331123c5653c17fe9fbeb2b6e23b`).

## Verificaciones efectivamente ejecutadas

| Control | Resultado | Alcance real |
|---|---|---|
| `zipfile.testzip()` sobre base I04 | **PASS** | CRC; 4.918 archivos |
| I05-CENIT fuente | **84/84** | `python scripts/test/automatic_returns_i05_cenit_static_check.py` |
| I04-CENIT fuente | **71/71** | Regresión estática |
| I03-CENIT fuente | **57/57** | Regresión estática |
| I02-CENIT fuente | **50/50** | Regresión estática |
| I01-CENIT fuente | **29/29** | Regresión estática |
| ACH Colombia I02, I03, I05, I06, I07, I08 | **18/18, 25/25, 33/33, 28/28, 24/24, 25/25** | Solo fuente, sin cambios funcionales |
| ACH Colombia I04 | **32/35** | Pendiente histórico. NO aprobado |
| Solución / referencias `.csproj` | **7/7** | Solo integridad de grafo, no compilación |
| ZIP I05 CRC/SHA-256 y comparación con I04 | Ver `VERIFICACION_INTEGRIDAD_DEVOLUCIONES_AUTOMATICAS_I05_CENIT.json` | Sin archivos removidos, hashes comparados |

**Pruebas escritas, pero NO ejecutadas:** `tests/Cfa.ACHInterbank.Tests/CenitReturnArtifactI05Tests.cs` verifica exigencia de aprobación humana, rechazo sin registro original válido, generación única/no-financiera, SHA/cifrado, descarga, bloqueo de modo DESACTIVADO, aislamiento ACH/CENIT, permisos HTTP y alteración del perfil/contenido NACHA-M. Además permanece `CenitReturnOut2026Tests.cs` para layout físico. Escribir pruebas no prueba que pasen en .NET.

## Validaciones pendientes de ambiente (NO EJECUTADAS)

1. `dotnet restore`, `dotnet build` y `dotnet test` para solución completa, `CenitReturnArtifactI05Tests`, pruebas anteriores I01–I04 y `CenitReturnOut2026Tests`.
2. Ejecutar migraciones `20261103190000_CenitReturnArtifact2026` en SQL Server y PostgreSQL desechables; confirmar precisión de tipos, encriptación, eliminación/rollback, FK y auditoría.
3. Concurrencia real: dos operadores intentando generar la misma preparación; reserva de secuencias, error/rollback del builder, caída tras commit, pérdida de respuesta HTTP, modo desactivado mientras se genera, interacción con flujo manual de CENIT existente.
4. E2E autenticación/autorización JWT (ConfigRead y ConfigManage), DataProtection key ring/rotación/recuperación, aislamiento de múltiples instancias, descarga sin cache y revisión de logs para datos sensibles.
5. Prueba física y golden files NACHA-M CENIT 2026 PPD/CCD contra datos autorizados: bloques de 106, adenda 99, cuenta, importe, identificaciones, control de lote/archivo y fecha valor. No dar por probado el motor compartido sólo por su código.
6. Contrato de nombre externo, transmisión/acuse/reconciliación y homologación bancaria **sujetos a I06**, separados de esta entrega.

## Instrucción reproducible cuando exista SDK .NET 10

```bash
dotnet restore ACHInterbank.sln
# ajustar la ruta a .sln si el nombre de solución difiere; primero localizar la solución real
find . -maxdepth 2 -name '*.sln' -o -name '*.slnx'
dotnet build --no-restore
# ejecutar pruebas de todo el proyecto, sin confundir compilación con homologación
dotnet test --no-build --filter 'FullyQualifiedName~CenitReturnArtifactI05Tests|FullyQualifiedName~CenitReturnOut2026Tests'
dotnet test --no-build
```

Las instrucciones anteriores **no fueron ejecutadas**. `dotnet` no está instalado en el ambiente actual. No se probó navegador Angular, proveedor bancario, SQL Server/PostgreSQL ni homologación.

## Garantía y limitaciones

En código no hay ruta de transporte I05, ni llamadas al flujo financiero ACH Colombia, ni inserción de retornos `AchReturnsGenerated`. Esta verificación es estática y **no demuestra ausencia de toda condición de carrera ni seguridad productiva**. `Transmittable=false` es un aviso semántico, no un control externo si alguien extrae manualmente el archivo. Se exige revisión operativa y bloqueo externo de canales de salida mientras NO-GO.
