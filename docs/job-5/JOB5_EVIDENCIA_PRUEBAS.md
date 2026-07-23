# JOB 5 — Evidencia de pruebas

Fecha: 2026-07-23

Veredicto: **NO-GO NORMATIVO**

## Línea base

| Verificación | Resultado |
|---|---|
| Rama | `ACH-Interbank-Postgresql` |
| Commit inicial de la corrección focal | `b317e5df7813479ec4d76dc721f5f7f7367ebba7` |
| Commit base de la corrección focal | `b317e5df7813479ec4d76dc721f5f7f7367ebba7` |
| HEAD inicial igual a la base | Sí |
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
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~NachaIncomingEndToEndProcessingTests|FullyQualifiedName~WsAxonRespuestaTransaccionesSoapClientCharacterizationTests|FullyQualifiedName~NotificarRespuestaAchUseCaseTests" --no-restore
```

Resultado focal posterior a la corrección: **45 passed, 0 failed, 0 skipped**.

Cobertura focal:

- ACHCOL y CENIT sintéticos bloqueados con `ProfileNotFound`;
- persistencia del archivo, selección y diagnóstico auditable;
- cero parseo funcional, correlación, transición, cola o ejecución SOAP;
- request independiente con exactamente siete parámetros;
- `ControlledLocal` y `ConfiguredAllowlist`;
- restricción positiva de puerto y rechazo de host, esquema, ruta, credenciales, fragmentos, comodines y allowlist vacía antes de red.

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

El endpoint persistido y la política de seguridad del ambiente son controles complementarios. `Development` usa `ControlledLocal` con puerto 7083; la configuración base queda sin modo y por ello falla cerrada. No se realizó una invocación SOAP en esta corrección.

## Live

| Paso | Estado | Motivo |
|---|---|---|
| WSDL local disponible | PASS | HTTP 200 |
| Archivo diferencial oficial/verificado | SKIPPED | No existe en el repositorio |
| Perfil seleccionado | SKIPPED | ACHCOL y CENIT están en NO-GO normativo |
| Validación/correlación diferencial | SKIPPED | La compuerta bloquea antes del parser |
| Request/response Live | SKIPPED | No se autoriza inventar un perfil ni despachar sin superar la compuerta normativa |
| Segunda carga Live | SKIPPED | No existió primera carga Live elegible |
| Invocaciones reales | 0 | Bloqueo normativo anterior al despacho |

No se usó un mock como evidencia Live.

## Validación final

| Prueba | Entorno/proveedor | Resultado | Observación |
|---|---|---|---|
| Build Release | .NET 10 | PASS | 0 warnings, 0 errors |
| Corrección focal | .NET 10 | PASS | 45 passed, 0 failed, 0 skipped |
| Suite completa local, sin variables multimotor | .NET 10 | FAIL ambiental | 1939 passed, 2 failed, 5 skipped; fallaron exclusivamente los dos `ClearingHouseMultiDbTests` porque faltó `CLEARING_HOUSES_REQUIRE_DATABASES=true` y sus cadenas |
| Suite efectiva de `dotnet-ci` | .NET 10 | PASS | 1939 passed, 0 failed, 5 skipped; filtro `Category!=ClearingHouseMultiDb`, `MaxCpuCount=1` |
| GitHub Actions anterior a la corrección | `dotnet-ci` run `30051435354` | FAIL | 1928 passed, 3 failed, 5 skipped; causa corregida por este cambio |
| Golden/regresión diferencial | In-memory/archivos | PASS fail-closed | Los goldens diferenciales siguen clasificados como sintéticos y no habilitan parser ni SOAP |
| SQL Server multimotor | SQL Server local | SKIPPED | Corrección sin cambios de persistencia; opt-in no configurado en esta ejecución |
| PostgreSQL multimotor | PostgreSQL local | SKIPPED | Corrección sin cambios de persistencia; opt-in no configurado en esta ejecución |
| SQL Server diferencial E2E | SQL Server local | SKIPPED | Sin perfil/vector diferencial sustentado; no hubo cambio de esquema |
| PostgreSQL diferencial E2E | PostgreSQL local | SKIPPED | Sin perfil/vector diferencial sustentado; no hubo cambio de esquema |
| Migraciones | SQL Server/PostgreSQL | SKIPPED | No se modificó el modelo EF |
| SPA build/test | Angular | SKIPPED | SPA no modificada |
| Playwright focalizado | Chromium | SKIPPED | SPA no modificada y la compuerta impide demostrar un flujo diferencial válido |
| SOAP Live | WCF local | SKIPPED | NO-GO normativo; el flujo queda bloqueado antes del despacho |

### SKIPPED legítimos y exclusiones

| Prueba | Causa exacta | Cómo habilitar |
|---|---|---|
| 4 pruebas `FinancialPersistenceMigrationTests` | Opt-in de integridad financiera, fuera del cambio JOB 5 | Definir `FINANCIAL_INTEGRITY_REQUIRE_DATABASES=true` y las dos cadenas `FINANCIAL_INTEGRITY_*_CONNECTION_STRING` |
| `SoapArchitectureDiagnosticTests.ApplicationAndDomain_ShouldReportSoapXmlProviderTerms_ForFutureRefactor` | `[Fact(Skip=...)]` permanente: diagnóstico inicial anterior al refactor | Retirar el `Skip` en una tarea arquitectural explícita |
| 2 pruebas `ClearingHouseMultiDbTests` | Excluidas por el filtro oficial de `dotnet-ci`; la ejecución local sin variables las reportó como fallos de configuración, no como PASS | Definir `CLEARING_HOUSES_REQUIRE_DATABASES=true` y ambas cadenas `CLEARING_HOUSES_*_CONNECTION_STRING` |
| Live diferencial | No existe perfil/vector diferencial sustentado | Aportar evidencia normativa verificable, publicar/homologar el perfil y configurar WSAXON bajo una política permitida |
| SPA/Playwright | No se modificó SPA; el backend bloquea antes de un resultado diferencial válido | Resolver la compuerta normativa y ejecutar la ruta operatoria existente |
