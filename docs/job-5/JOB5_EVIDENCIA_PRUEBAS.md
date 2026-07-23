# JOB 5 — Evidencia de pruebas

Fecha: 2026-07-23

Veredicto: **NO-GO NORMATIVO**

## Línea base

| Verificación | Resultado |
|---|---|
| Rama | `ACH-Interbank-Postgresql` |
| Commit inicial | `5abd1e91aefc346adbd2dde09632a4e48d7daabb` |
| Commit base | `5abd1e91aefc346adbd2dde09632a4e48d7daabb` |
| Base ancestro de HEAD | Sí, exit code 0 |
| Estado inicial | Limpio |

## Evidencia normativa y hashes

Los hashes se calcularon con `Get-FileHash -Algorithm SHA256`. No se modificó ningún original.

| Archivo | Clasificación | SHA-256 |
|---|---|---|
| `docs/normativa/pdf/ACH-Colombia-V32.pdf` | Official | `D83585B53B31A3A70E4861412F48FF8306ED0D2F439A7443C05E540F6B5736EE` |
| `docs/normativa/pdf/CENIT-DSP-152-Anexo-2.pdf` | Official | `AD6BB2FC48CCF78CE0BDB980BBFFFCAF9D42E52882CC16559A9336F41CFC902D` |
| `docs/normativa/pdf/CENIT-Anexo-A-Causales-Devolucion.pdf` | Official | `D3A8F12EC49876CBFF516DAA3A1651693C5DAE0D75DC2DF8FFE733C1A8A00EFE` |
| `docs/normativa/pdf/CENIT-Anexo-B-Causales-Rechazo.pdf` | Official | `ADF05ED85BF8EF136C61A1560D073EB6B375D50571530DC679164AF78C2A530A` |
| `ACH_COL_RET_001.RET` | SyntheticFixture | `FDE736A96C1C24BE0392E1E56BDD71E4910B63B287D5E8C47479A93AFD7B96EE` |
| `CENIT_RET_001.RET` | SyntheticFixture | `047FAB8F6A35A4E063974C0DABE9F736CD90FD0840E75AB672C831BFDC40CA95` |

## Pruebas focalizadas ejecutadas

Comando:

```powershell
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~WsAxonRespuestaTransaccionesSoapClientCharacterizationTests|FullyQualifiedName~NachaConfigResolverTests|FullyQualifiedName~IncomingNachaIngestionAppServiceTests|FullyQualifiedName~NachaInboundSimulatorTests|FullyQualifiedName~NotificarRespuestaAchUseCaseTests|FullyQualifiedName~AchReconciliationReadModelTests|FullyQualifiedName~NachaUploadControllerJob5Tests" --no-restore
```

Resultado: **72 passed, 0 failed, 0 skipped**.

Cobertura focal:

- seis resultados de selección y ambigüedad de layout;
- requisito de homologación;
- bloqueo diferencial antes de parser/cola/SOAP;
- nombres `.OUT` y `.RET`;
- request persistido con siete parámetros;
- response funcional/técnica persistida;
- conciliación de compuerta y duplicidad;
- allowlist local WSAXON y bloqueo de host/ruta/esquema no permitidos.

Pruebas complementarias:

| Alcance | Resultado |
|---|---|
| Correlación, validación funcional, gateway y duplicidad/orfandad | 92 passed, 0 failed |
| Bytes/offsets ACHCOL, terminadores y CENIT fail-closed | 3 passed, 0 failed |
| Scheduler/orquestador, persistencia de ejecución y retry | 4 passed, 0 failed |
| Traducción específica de perfil inexistente vs. registro faltante | 2 passed, 0 failed |
| ClearingHouse multimotor real, bases temporales | 2 passed, 0 failed |

## Doble carga

| Escenario | Resultado | Evidencia |
|---|---|---|
| A: mismos bytes, mismo nombre | PASS | Una ingesta, un intento duplicado auditado, parser una vez como máximo y cero cola/ejecución en el candidato diferencial bloqueado. |
| B: mismo nombre, bytes diferentes | PASS | Dos ingestas independientes, `FileNameContentConflict`, sin sobrescritura. |
| C: nombre diferente, mismos bytes | PASS | Duplicidad por SHA-256+tamaño, una ingesta canónica y `DuplicateUploadAttempt`. |

Las pruebas están en `IncomingNachaIngestionAppServiceTests` y `AchReconciliationReadModelTests`.

## Ambiente local

Comprobación de disponibilidad, sin ejecutar métodos SOAP:

| Servicio | Resultado |
|---|---|
| SPA `http://localhost:743/` | HTTP 200; contenedor healthy |
| API `http://localhost:843` | Contenedor healthy; `/health` no está publicado y respondió 404 |
| SQL Server | Contenedor healthy |
| PostgreSQL | Contenedor healthy |
| WSAXON WSDL `http://localhost:7083/WSAxonRespuestaTransacciones.svc?wsdl` | HTTP 200; 3218 bytes; XML no impreso |

La configuración efectiva de SQL Server contiene un endpoint no local de ejemplo. El nuevo cliente lo rechaza antes de red. No se modificó esa configuración ni se realizó una invocación.

## Live

| Paso | Estado | Motivo |
|---|---|---|
| WSDL local disponible | PASS | HTTP 200 |
| Archivo diferencial oficial/verificado | SKIPPED | No existe en el repositorio |
| Perfil seleccionado | SKIPPED | ACHCOL y CENIT están en NO-GO normativo |
| Validación/correlación diferencial | SKIPPED | La compuerta bloquea antes del parser |
| Request/response Live | SKIPPED | No se autoriza inventar un perfil ni despachar con endpoint efectivo no local |
| Segunda carga Live | SKIPPED | No existió primera carga Live elegible |
| Invocaciones reales | 0 | Bloqueo normativo y allowlist técnica |

No se usó un mock como evidencia Live.

## Validación final

| Prueba | Entorno/proveedor | Resultado | Observación |
|---|---|---|---|
| Build Release | .NET 10 | PASS | 0 warnings, 0 errors |
| Suite backend amplia | .NET 10 | PASS compuesto | Corrida amplia: 1930 pass, 3 fail, 5 skipped; los tres fallos se aislaron, corrigieron/habilitaron y luego pasaron 3/3. Resultado final no omitido: 1933 verdes. |
| Golden/regresión | In-memory/archivos | PASS focal | 3 casos físicos críticos; los goldens diferenciales siguen clasificados como sintéticos |
| SQL Server multimotor | SQL Server local | PASS | `ClearingHouseMultiDbTests`, base temporal GUID eliminada por el harness |
| PostgreSQL multimotor | PostgreSQL local | PASS | `ClearingHouseMultiDbTests`, base temporal GUID eliminada por el harness |
| SQL Server diferencial E2E | SQL Server local | SKIPPED | Sin perfil/vector diferencial sustentado; no hubo cambio de esquema |
| PostgreSQL diferencial E2E | PostgreSQL local | SKIPPED | Sin perfil/vector diferencial sustentado; no hubo cambio de esquema |
| Migraciones | SQL Server/PostgreSQL | SKIPPED | No se modificó el modelo EF |
| SPA build/test | Angular | SKIPPED | SPA no modificada |
| Playwright focalizado | Chromium | SKIPPED | SPA no modificada y la compuerta impide demostrar un flujo diferencial válido |
| SOAP Live | WCF local | SKIPPED | NO-GO normativo y configuración efectiva no local |

### SKIPPED de la suite amplia

| Prueba | Causa exacta | Cómo habilitar |
|---|---|---|
| 4 pruebas `FinancialPersistenceMigrationTests` | Opt-in de integridad financiera, fuera del cambio JOB 5 | Definir `FINANCIAL_INTEGRITY_REQUIRE_DATABASES=true` y las dos cadenas `FINANCIAL_INTEGRITY_*_CONNECTION_STRING` |
| `SoapArchitectureDiagnosticTests.ApplicationAndDomain_ShouldReportSoapXmlProviderTerms_ForFutureRefactor` | `[Fact(Skip=...)]` permanente: diagnóstico inicial anterior al refactor | Retirar el `Skip` en una tarea arquitectural explícita |
| Live diferencial | No existe perfil/vector diferencial sustentado y el endpoint efectivo no es local | Aportar evidencia normativa verificable, publicar/homologar el perfil y configurar WSAXON local |
| SPA/Playwright | No se modificó SPA; el backend bloquea antes de un resultado diferencial válido | Resolver la compuerta normativa y ejecutar la ruta operatoria existente |
