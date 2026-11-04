# EVIDENCIA — I02-CENIT

**Fecha documental:** 2026-11-02. **ZIP de entrada:** `ACHInterbank_COMPLETO_DEVOLUCIONES_AUTOMATICAS_I01_CENIT_2026-11-02.zip`. **Salida:** `ACHInterbank_COMPLETO_DEVOLUCIONES_AUTOMATICAS_I02_CENIT_2026-11-02.zip`. **NO-GO**.

## Tabla inequívoca de ejecución

| Validación | Estado | Evidencia |
|---|---|---|
| Verificación del ZIP de entrada | **EJECUTADA** | `unzip -t` sin errores, SHA-256 de entrada cotejado |
| Análisis de código/memorias | **EJECUTADO** | servicios actuales, catálogos, EF, rutas y plan I01 |
| Comprobaciones de fuente I02-CENIT | **EJECUTADAS** | **50/50** en `scripts/test/automatic_returns_i02_cenit_static_check.py` |
| Regresión estática I01-CENIT | **EJECUTADA** | **29/29** |
| Regresión estática ACH I02–I08 | **EJECUTADA** | 18/18, 25/25, **32/35 I04**, 33/33, 28/28, 24/24, 25/25 (I04 NO aprobada) |
| Comparación SHA-256 contra ZIP base | **EJECUTADA** | manifiesto JSON de entrada/salida; ninguna eliminación de archivo histórico |
| Integridad del ZIP de salida | **EJECUTADA** | `unzip -t`, SHA-256 externo; métricas definitivas en manifiesto |
| Compilación `dotnet build` | **NO EJECUTADA** | .NET SDK no disponible en este contenedor |
| Pruebas xUnit I02 e I01 | **NO EJECUTADAS** | Código de pruebas incorporado, sin resultados inventados |
| SQL Server / PostgreSQL | **NO EJECUTADAS** | No hay infraestructura bancaria ni conexiones; sin nuevas migraciones I02 |
| HTTP autenticado y logs reales | **NO EJECUTADAS** | Servidor .NET no arrancado |
| UAT bancaria, homologación, NACHA-M | **PENDIENTES** | Sin entorno CENIT, archivos no generados |

## Reproducción en ambiente .NET 10

```bash
dotnet build ACHInterbank.sln --configuration Release
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj --filter FullyQualifiedName~CenitPassiveReturnDetectionI02Tests --configuration Release
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj --filter FullyQualifiedName~CenitAutomaticReturnSettingsI01Tests --configuration Release
python3 scripts/test/automatic_returns_i02_cenit_static_check.py
```

Si el `.sln` real tiene otro nombre, identificarlo antes de ejecutar `dotnet build`. Ejecutar HTTP GET autenticado con y sin `P1.ConfigRead`, verificar 403 para perfil no autorizado, ausencia de escrituras (auditoría/Devoluciones/NACHA), aislamiento ACH y resultado coherente con `IncomingNachaIntegrationExecution` + `IncomingNachaTransactionLink`. En SQL Server/PostgreSQL, usar base no productiva con transacciones verificables y con reversiones: 1) rechazo CENIT confirmado (indicio), 2) rechazo técnico (no indicio), 3) éxito posterior (no indicio), 4) causa R06/R23 (revisión), 5) devolución ya generada (no indicio), 6) intento ACH (no entra), 7) ninguna preparación ni envío.

## Control de integridad fuera de alcance

Los hashes por ruta de los archivos heredados se comparan por manifiesto; solo cambia `src/Cfa.ACHInterbank.Persistence/DependencyInjectionService.cs` entre archivos heredados (registro del nuevo servicio). Cualquier otro cambio heredado no previsto detiene el cierre. Se conservan íntegros todos los archivos históricos del ZIP anterior.

## Estado total

| Cámara | Incremento(s) | Estado al cierre de I02-CENIT | Pendientes del plan actual |
|---|---|---|---|
| CENIT | **I01** | Incorporado al código; ejecución/homologación pendientes | — |
| CENIT | **I02** | Incorporado al código; validador estático 50/50; xUnit/DB pendientes | — |
| CENIT | **I03–I06** | No iniciados | **4 incrementos** |
| ACH Colombia | **I01–I08** | Incorporados al código histórico; no homologados | — |
| ACH Colombia | **I09-B** | Ejecución en entorno compilado pendiente | **1 fase obligatoria de validación** |
| ACH Colombia | **I09-A** | Preparación técnica condicional | **0–1 fase opcional** |

**Conteo:** 6 incrementos CENIT planificados; **2 incorporados**, **4 por implementar**. Para las dos cámaras restan **4 incrementos CENIT + 1 fase obligatoria I09-B ACH**, más **I09-A solo si el entorno lo necesita**. Esto es el conteo del plan actual, **no** una garantía de puesta en producción ni un número cerrado de iteraciones de corrección.

