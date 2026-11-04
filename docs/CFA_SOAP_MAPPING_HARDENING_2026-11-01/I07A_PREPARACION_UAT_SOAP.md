# I07-A — Preparación de UAT oficial, sin ejecución externa

**Programa:** `SOAP-MAP-SAFE-001`  
**Fecha:** 2026-11-01 · **Base:** ZIP completo I06 integrado  
**Estado de este incremento:** `PREPARATION_COMPLETE / UAT_NOT_STARTED`  
**Prohibido inferir:** `UAT_APPROVED`, `VERIFIED_LOCAL`, `GAUNTLET_PASSED`, `CUTOVER_APPROVED` o `LIVE_AUTHORIZED`.

## Alcance concreto

1. Conservar **sin cambios productivos** el código integrado I01–I05 y las decisiones/documentos de I06.
2. Preparar matriz de ejecución I07-B para **las tres operaciones** y los defectos realmente abiertos, con resultados inicialmente `PENDING`.
3. Enlazar los gates técnicos existentes —sobre todo `P8_4_EXTERNAL_VALIDATION_HANDOFF.md`— en lugar de reimplementarlos.
4. Preparar una verificación local **solo estática** (`scripts/uat/i07a-soap-safe-preflight.cmd`); no levanta API, contenedores ni envía SOAP.
5. Documentar responsables, condiciones de entrada, evidencia, rechazo, rollback y reglas de no retransmisión.

**No se ejecuta UAT ni integración con el banco en I07-A.** No se crea un motor SOAP, nuevas migraciones, entidades, seed, módulos UI ni un tercer camino de mapping.

## Ruta de ejecución para la siguiente etapa

| Orden | Actividad | Responsable propuesto | Resultado requerido antes de avanzar |
|---|---|---|---|
| 0 | Fijar el ZIP/commit exacto y SHA-256, revisar entorno real, perfil y overrides | Líder técnico/Seguridad | Identidad del candidato y `Live` explícitamente controlado; sin secretos en el repositorio |
| 1 | Ejecutar `STATIC-SOLUTION`, `STATIC-P81`, `STATIC-P83` y compilar solución en Release | Desarrollo/QA técnico | `PASS` real; ningún `NOT_RUN_ENVIRONMENT` tratado como PASS |
| 2 | Ejecutar xUnit focal I01–I05 y regresión backend; pruebas de concurrencia/DB en infraestructura controlada | QA técnico | TRX/logs sanitizados, sin errores ni skips de gates obligatorios |
| 3 | Ejecutar **P8.4** completo según handoff existente (migraciones SQL Server/PostgreSQL, paridad 3 servicios, no-code field, gauntlet, frontend cuando aplique) | QA técnico/Arquitectura | Resultado de cada gate; `UseContractDrivenRequest` permanece apagado hasta decisión formal de cutover |
| 4 | Obtener homologación documental del contrato bancario y origen real de campos | Integración CFA + contraparte bancaria | `OFEMP`, `OFIDARCH`, `OFIDLOT`, `OFST`, `OFDIRECCIONIP`, `NCTAORIG`, `IDORIG`, `INFPAG` resueltos o pendientes bloqueantes por ruta |
| 5 | Autorizar **ambiente oficial UAT aislado**, identidad/certificados/endpoints por canal seguro, dataset sintético aprobado y plan de suspensión | Operaciones, Seguridad y Banco | Permiso explícito para UAT; producción continúa NO-GO |
| 6 | **I07-B:** ejecutar casos de `I07A_MATRIZ_CASOS_SOAP.md` en ventana oficial y recopilar evidencia | QA UAT + Banco | Resultado por escenario con trazabilidad y sin exponer datos sensibles |
| 7 | Conciliar resultados, resolver defectos, emitir acta y decisión de UAT por alcance | Negocio/QA/Arquitectura | Aceptación **explícita** o rechazo; ninguna habilitación productiva implícita |

Si falla un gate técnico, se corrige ese defecto antes de declarar el candidato apto para UAT; los trabajos documentales independientes pueden avanzar. Si el ambiente está bloqueado, registrar `NOT_RUN_ENVIRONMENT` y **no crear un PASS ficticio**.

## Comandos técnicos preparados (Windows CMD; **no son UAT oficial**)

Desde la raíz descomprimida de `ACHInterbank_ajustado`:

```cmd
python scripts/validate-solution-projects.py
if not exist artifacts mkdir artifacts
python scripts/validate-cfa-p8-1-static.py
python scripts/validate-cfa-p8-3-gauntlet.py
dotnet build ACHInterbank.sln -c Release
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~ContrapartidaDispatchJobServiceTests|FullyQualifiedName~ProcContrapartidasFallbackClosureTests|FullyQualifiedName~IntegrationMappingEndToEndTests|FullyQualifiedName~NachaDesagregadoIntegrationMappingTests|FullyQualifiedName~ProcTransaccionesOriginAccountSafetyTests|FullyQualifiedName~NotificarRespuestaAchUseCaseTests|FullyQualifiedName~RegistrarRespuestaAchContractsTests|FullyQualifiedName~CfaSoapOperationParityGuardTests"
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build
```

**Antes de iniciar**: confirmar que el ejecutor utiliza base de pruebas local, mocks/fixtures y no conecta a endpoints externos. El proyecto contiene `Mode=Live` en `appsettings.Development.json` para operaciones monetarias; el `DryRun` base **no neutraliza automáticamente** ese override. Evitar lanzar la API con perfil Development como paso de este preflight. No copiar variables/secretos a logs. El `.cmd` incluido **solo ejecuta los primeros controles estáticos**, nunca `dotnet`, API, DB, SOAP o UAT.

No adelantar `UseContractDrivenRequest` ni los checks de corte P8.4: sus criterios y comandos están en el handoff canónico.

## Escenarios y controles obligatorios

La matriz I07-A enlaza: I01 cardinalidad / no doble envío; I02 naturaleza y montos débito; I03 cuenta receptora y `NCTAORIG`; I04 siete campos y estado incierto; I05 paridad, XML, null/empty; configuración efectiva; concurrencia; reintento, trazas, rollback. En todos, el resultado empieza en `PENDING`, aunque haya evidencias históricas de UAT local de fases anteriores.

Para el registro oficial reutilizar:

- `docs/uat/MATRIZ_ESCENARIOS_UAT_SOAP_END_TO_END_FINAL.md` (histórico: no certifica este ZIP).
- `docs/uat/ACTA_UAT_SOAP_END_TO_END_TEMPLATE.md` (acta).
- `docs/uat/EXTERNAL_UAT_PREPARATION_ACH_CENIT.md` (coordinación y seguridad).
- `docs/uat/UAT_CERTIFICATE_ENDPOINT_REGISTER.md` (registro de endpoints sin exponer credenciales).
- `docs/uat/MATRIZ_DEFECTOS_UAT.md` (defectos).
- `docs/CFA_SOAP_UX_MAPPING_IMPLEMENTATION_PACKAGE_2026-10-12/P8_4_EXTERNAL_VALIDATION_HANDOFF.md` (validación técnica exhaustiva y cutover).
- `docs/CFA_SOAP_MAPPING_HARDENING_2026-11-01/INVENTARIO_CAMPOS_SOAP_I06.md` (56 campos del seed y riesgos abiertos).

## Evidencia mínima exigida durante I07-B (NO producida aquí)

- Identificador del candidato exacto (SHA-256 y estado de configuración efectiva **sin imprimir secretos**).
- Ambiente oficial, entidad responsable, fecha/hora, autorizaciones y versión de contrato.
- ID de caso y referencia sintética, identidad/correlación de la transacción pseudonimizada, mapping set publicado y versión.
- Resultado esperado y observado, códigos/campos de retorno, estado persistido, intentos y decisión de conciliación.
- TRX, stdout sanitizado, identificadores de logs, capturas sin datos sensibles y decisión firmada.
- Evidencia de rollback que **no** reenvíe transacciones monetarias confirmadas.

**Criterio de salida I07-A:** documentación y matriz completas, preflight estático disponible, bloqueos identificados y ZIP integral generado/verificado. **No** equivale a aptitud UAT ni a homologación del banco.
