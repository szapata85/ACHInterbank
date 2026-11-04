# I06 — Cierre técnico, verificaciones posibles y deuda pendiente

**Programa:** `SOAP-MAP-SAFE-001` · **Fecha:** 2026-11-01 · **Alcance:** exclusivamente I06 (sin cambios C# ni UAT).  
**Base de trabajo:** ZIP completo integrado I05; no se aplican parches externos.  
**Resultado:** `IMPLEMENTED / NOT_RUN_ENVIRONMENT` para el cierre documental; **no** `VERIFIED_LOCAL`, `GAUNTLET_PASSED`, `UAT_READY` ni `RELEASE_READY`.

## 1. Gates realmente intentados

| Gate | Comando | Evidencia I06 | Estado |
| --- | --- | --- | --- |
| Grafo de solución | `python scripts/validate-solution-projects.py` | `SOLUTION GRAPH: OK`; 7/7 `.csproj` y referencias completas; exit `0` | `PASS_STATIC` |
| Ratchet histórico P8.1 | `python scripts/validate-cfa-p8-1-static.py` | exit `0`, JSON `PASS_STATIC_ONLY`; hubo que crear carpeta temporal `artifacts/`, ausente del ZIP; no se incorpora al entregable | `PASS_STATIC_ONLY` |
| Ratchet P8.3 | `python scripts/validate-cfa-p8-3-gauntlet.py` | intento interrumpido por límite de ejecución del contenedor, sin resultado final ni JSON íntegro | `INCOMPLETE_TIMEOUT`, **NO PASS** |
| SDK | `dotnet --info` | `dotnet: command not found`, exit `127` | `NOT_RUN_ENVIRONMENT` |
| Compilación | `dotnet build ACHInterbank.sln -c Release` | `dotnet: command not found`, exit `127` | `NOT_RUN_ENVIRONMENT` |
| xUnit | `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build` | `dotnet: command not found`, exit `127` | `NOT_RUN_ENVIRONMENT` |
| Dual-provider SQL/Quartz, endpoints, frontend E2E | No invocados: requieren infraestructura autorizada y están cubiertos por handoff P8.4 | Sin evidencia de ejecución I06 | `NOT_RUN_ENVIRONMENT` |

Ninguna de estas comprobaciones estáticas demuestra que los mappings hayan pasado compilación, integración con banco o UAT. **No** se crearon conexiones, transacciones financieras, accesos externos ni despliegues.

## 2. Resultado de I01–I05, sin reclasificar sus gates

| Incremento | Entregado en código | Deuda real |
| --- | --- | --- |
| I01 | Correlación 1:1 y tratamiento conservador de resultado remoto incierto de contrapartidas | xUnit; reintentos/concurrencia SQL, rendimiento y ausencia de duplicados bajo carga |
| I02 | Reglas de integridad de débito `OFDD`, `OFMONDEB`, `OFMONCRE` | xUnit; `OFEMP`, `OFIDARCH`, `OFIDLOT`, `OFST`, `OFDIRECCIONIP` sin homologación |
| I03 | Correlación NACHA tipo 6, montos/identidad y veto Live a `NCTAORIG` sin fuente fiable | xUnit; cuenta origen e interpretación bancaria de `IDORIG` e `INFPAG` |
| I04 | Validación de siete campos Axon y conciliación de errores posgateway | xUnit; concurrencia, reinicio del proceso y fallo de commit posterior al acuse |
| I05 | Guard de equivalencia de SOAP monetario legacy/contract-driven; Axon conserva política v1 | xUnit; no se permite cutover sin P8.4 |

**Política:** las pruebas escritas no son pruebas pasadas. Los campos identificados como `BLOCKED_FUNCTIONAL` no deben considerarse certificados porque el seed pueda generar un XML válido.

## 3. Hallazgo de configuración (no cambiar silenciosamente)

- `src/Cfa.ACHInterbank.Api/appsettings.json` define `ProcContrapartidas:Mode=DryRun` y `ProcTransacciones:Mode=DryRun`.
- `src/Cfa.ACHInterbank.Api/appsettings.Development.json` define **`Mode=Live` para ambos**, y `SoapIntegrationBootstrap` mantiene conexiones **Live** a servicios `localhost:7083` en ese perfil.
- La configuración efectiva depende del entorno y de overrides. Por ello, **no es correcto afirmar que todas las ejecuciones usan DryRun**. No se modificó configuración ni se invocó ningún servicio; es imprescindible comprobar el entorno efectivo antes de permitir una ejecución futura. En este programa sigue prohibido activar Live monetario o cutover.

## 4. Retoma exacta de gates técnicos (Windows CMD, previo a I07)

Desde la raíz del proyecto completo, con .NET SDK compatible con `global.json` (10.0.403 o parche permitido):

```cmd
python scripts/validate-solution-projects.py
python scripts/validate-cfa-p8-1-static.py
python scripts/validate-cfa-p8-3-gauntlet.py
dotnet build ACHInterbank.sln -c Release
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~ContrapartidaDispatchJobServiceTests|FullyQualifiedName~ProcContrapartidasFallbackClosureTests|FullyQualifiedName~IntegrationMappingEndToEndTests|FullyQualifiedName~NachaDesagregadoIntegrationMappingTests|FullyQualifiedName~ProcTransaccionesOriginAccountSafetyTests|FullyQualifiedName~NotificarRespuestaAchUseCaseTests|FullyQualifiedName~RegistrarRespuestaAchContractsTests|FullyQualifiedName~RespuestaTransaccionesAchGatewayTests|FullyQualifiedName~CfaSoapOperationParityGuardTests"
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build
```

Si el ratchet P8.1 no encuentra `artifacts/`, crear esa carpeta temporal antes del comando y excluir los resultados generados del commit/paquete. **No** adaptar expectativas de tests a defectos ni rebajar fallos.

La matriz P8.4 completa (contrato, dual-provider, migraciones, versiones, rollback y no-code) ya existe en `docs/CFA_SOAP_UX_MAPPING_IMPLEMENTATION_PACKAGE_2026-10-12/P8_4_EXTERNAL_VALIDATION_HANDOFF.md`. **No replicar ni declarar PASS esos gates aquí.**

## 5. Condiciones de salida para la etapa final

1. Compilación y pruebas focales/backend con resultado verificable; resolver cualquier `FAILED_CODE` antes de UAT.
2. Acreditar los cinco campos críticos de contrapartidas y cuenta origen `NCTAORIG` contra definición autorizada del banco, sin asumir equivalencia por nombre.
3. Resolver las verificaciones de concurrencia, idempotencia y persistencia posrespuesta con ambiente controlado.
4. Obtener validación P8.4 pertinente antes de activar `UseContractDrivenRequest`.
5. Confirmar configuración operativa efectiva y autorización de entorno UAT, **sin utilizar producción**.

**I06 cierra solamente la consolidación documental y los gates estáticos ejecutables en este ambiente.** `next_increment=I07`, que está sujeto a prerrequisitos y a la autorización de UAT; si faltan, mantener `UAT_PENDING_PREREQUISITES` y no ejecutar Live.
