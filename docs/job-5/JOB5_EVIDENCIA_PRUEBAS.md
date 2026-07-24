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

### Línea base JOB 5C

| Verificación | Resultado |
|---|---|
| Rama | `ACH-Interbank-Postgresql` |
| Commit inicial y base exacta | `e152d48f20c8161604ae4d39b9496caa538e5851` |
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

El endpoint persistido y la política de seguridad del ambiente son controles complementarios. `Development` usa `ControlledLocal` con puerto 7083; la configuración base queda sin modo y por ello falla cerrada. En la corrección focal anterior no se realizó una invocación SOAP; la ejecución física de JOB 5C se documenta a continuación.

## Prueba Live E2E con referencias productivas de terceros

La validación se ejecutó exclusivamente en el ambiente local controlado. Los archivos externos se leyeron desde sus rutas originales, no se copiaron al repositorio y no se imprimieron cuentas, nombres, documentos, montos ni contenido completo.

Carpetas efectivas:

- ACHCOL: `C:\Users\CHECHO\Documents\proyectos\Interbank\ACHInterbank_SPA2\docs\referencias-reales\tercero-ACHCOL`
- CENIT: la ruta solicitada `tercero-CEN` no existe; se usó la carpeta hermana comprobada `C:\Users\CHECHO\Documents\proyectos\Interbank\ACHInterbank_SPA2\docs\referencias-reales\tercero-CENIT`.

| Cámara | Archivo enmascarado | Clasificación | SHA-256 | Identificación estructural | Resultado Playwright |
|---|---|---|---|---|---|
| ACHCOL | `0001283.***.OUT` | ProductionReference | `F090B5D4BFAB75FE04CD19313EA1ED467D0205F0FC603DE255CF9688C4753518` | 420 registros físicos; 130 T6; addendas T7 tipo `05` | Ingesta canónica `f910d1bb-…`, cámara 1, `NoResuelto`, parser `NoEjecutado`; segunda carga `Duplicado` |
| CENIT | `0001283.***` | ProductionReference | `3566E425E7786B841482612C6EBC507ECD4E41996A1B8391D1CF5BE7F29468BE` | 20 registros físicos; 4 T6; addendas T7 tipo `05` | Ingesta canónica `ba82dbff-…`, cámara 3, `NoResuelto`, parser `NoEjecutado`; segunda carga `Duplicado` |

Ambos archivos corresponden a transacciones monetarias entrantes, no a respuestas diferenciales. En consecuencia, el resultado correcto fue fail-closed: cero clasificaciones funcionales, cero correlaciones, cero despachos y cero llamadas a `RegistrarRespuestaTransaccion`. Cada segunda carga produjo un único evento `DuplicateUploadAttempt`. No se presentó este resultado como homologación normativa.

La ruta principal atravesó login SPA, navegación por menú, selector real con `setInputFiles`, `NachaUpload`, respuesta JSON y conciliación. En ambas cámaras hubo cero errores de navegador/HTTP, cero `index.html` en lugar de JSON y cero `[object Object]`.

### Validación técnica SOAP Live separada

Como los dos archivos productivos no contienen respuestas diferenciales, la frontera SOAP se validó separadamente con datos totalmente sintéticos. El simulador local generó una fuente de un T6/T7, fue cargada por el selector SPA y el parser persistió 1 lote, 1 entrada válida y 1 addenda. Una prenotificación CFA sintética correlacionada transitó de `Pending` a `Certified`; el evento persistido confirma `monetaryMovementCreated=false` y `balancesAffected=false`.

| Evidencia | Resultado |
|---|---|
| Correlation ID | `JOB5C-LIVE-20260723-006` |
| Fuente técnica enmascarada | `0000100.***.OUT`; SHA-256 `08E8A7A80944AEFC925CAEDF0356B26CAD8F178DAA28C8717D9EDF08AD04FF7A` |
| Cadena real | `ProcesarRespuestaAchUseCase` → intento persistido → `NotificarRespuestaAchUseCase` → mapper/gateway/cliente real |
| Endpoint efectivo Windows | `http://localhost:7083/WSAxonRespuestaTransacciones.svc` |
| Topología del proceso Docker | `http://host.docker.internal:7083/WSAxonRespuestaTransacciones.svc`, con `Host: localhost:7083`, misma instancia WCF local |
| SOAPAction | `http://tempuri.org/IWSAxonRespuestaTransacciones/RegistrarRespuestaTransaccion` |
| Request persistido | JSON de 160 caracteres; exactamente siete claves: `idCanal`, `nombreCanal`, `idTransaccion`, `idEstado`, `causal`, `idTransaccionAxon`, `descripcionCausal` |
| Response persistida | JSON funcional de 60 caracteres; `existeError=false`; sin error técnico |
| Resultado | intento 1 `Exitosa`; respuesta `Notificada`; una invocación física |
| Idempotencia | segundo envío `yaProcesada=true`; el log WCF conservó el mismo tamaño, sin invocación adicional |
| Log WCF | `C:\WebServices\WSCFAACH\Log\Trama_ACH_20260723.log`; `2026-07-23 21:10:18,924`; operación y siete nombres comprobados; identificador `000***051` |

No se usó un mock como evidencia Live. La referencia productiva, la prueba SOAP técnica y la homologación normativa se mantienen explícitamente separadas: **GO operativo local de ingestión**, **GO técnico SOAP Live** y **NO-GO normativo**.

### Incidencias locales corregidas durante JOB 5C

- drift local PostgreSQL: la migración constaba aplicada pero faltaba `ClearingHouseConfigs.PaymentRailCode`; se reparó la columna/backfill exacto de la migración, sin generar una migración nueva;
- el simulador entrante emitía T1/T5/T7 y dígito de chequeo incompatibles con el parser productivo;
- el parser fallaba globalmente ante `OriginCode` duplicados ajenos a la cámara ya resuelta;
- una prenotificación diferencial validada se marcaba `Notificada` antes del gateway; ahora crea el intento pendiente y solo `NotificarRespuestaAchUseCase` completa la transición;
- la topología WCF local en Docker requirió un `HostHeader` local validado por la política `ControlledLocal`.

## Validación final

| Prueba | Entorno/proveedor | Resultado | Observación |
|---|---|---|---|
| Build Release JOB 5C | .NET 10 | PASS | 0 warnings, 0 errors |
| Backend focal JOB 5C | .NET 10 | PASS | 157 passed, 0 failed, 0 skipped |
| Suite final CI-equivalente JOB 5C | .NET 10 | PASS | 1943 passed, 0 failed, 5 skipped; filtro `Category!=ClearingHouseMultiDb` |
| Angular unitarias | Chrome Headless 147 | PASS | 461 passed |
| Angular build | Producción | PASS | Build completado |
| Playwright referencias productivas | Chromium / SPA real | PASS | 2 cámaras; primera carga persistida y segunda carga duplicada; cero errores de navegador/HTTP |
| Playwright SOAP Live | Chromium / SPA/API/WCF reales | PASS | intento 1 `Exitosa`; verificación idempotente posterior 1 passed; una invocación física total |
| Corrección focal anterior | .NET 10 | PASS | 45 passed, 0 failed, 0 skipped |
| Suite completa local anterior, sin variables multimotor | .NET 10 | FAIL ambiental histórico | 1939 passed, 2 failed, 5 skipped; fallaron exclusivamente los dos `ClearingHouseMultiDbTests` sin configuración opt-in |
| Suite efectiva anterior de `dotnet-ci` | .NET 10 | PASS | 1939 passed, 0 failed, 5 skipped; filtro `Category!=ClearingHouseMultiDb`, `MaxCpuCount=1` |
| GitHub Actions anterior a la corrección | `dotnet-ci` run `30051435354` | FAIL | 1928 passed, 3 failed, 5 skipped; causa corregida por este cambio |
| Golden/regresión diferencial | In-memory/archivos | PASS fail-closed | Los goldens diferenciales siguen clasificados como sintéticos y no habilitan parser ni SOAP |
| SQL Server multimotor | SQL Server local | SKIPPED | Corrección sin cambios de persistencia; opt-in no configurado en esta ejecución |
| PostgreSQL multimotor | PostgreSQL local | SKIPPED | Corrección sin cambios de persistencia; opt-in no configurado en esta ejecución |
| SQL Server diferencial E2E | SQL Server local | SKIPPED | Sin perfil/vector diferencial sustentado; no hubo cambio de esquema |
| PostgreSQL diferencial E2E | PostgreSQL local | SKIPPED | Sin perfil/vector diferencial sustentado; no hubo cambio de esquema |
| Migraciones | SQL Server/PostgreSQL | SKIPPED | No se modificó el modelo EF |
| SOAP Live | WCF local | PASS técnico | Una invocación física real y un segundo envío omitido por idempotencia; no representa homologación normativa |

### SKIPPED legítimos y exclusiones

| Prueba | Causa exacta | Cómo habilitar |
|---|---|---|
| 4 pruebas `FinancialPersistenceMigrationTests` | Opt-in de integridad financiera, fuera del cambio JOB 5 | Definir `FINANCIAL_INTEGRITY_REQUIRE_DATABASES=true` y las dos cadenas `FINANCIAL_INTEGRITY_*_CONNECTION_STRING` |
| `SoapArchitectureDiagnosticTests.ApplicationAndDomain_ShouldReportSoapXmlProviderTerms_ForFutureRefactor` | `[Fact(Skip=...)]` permanente: diagnóstico inicial anterior al refactor | Retirar el `Skip` en una tarea arquitectural explícita |
| 2 pruebas `ClearingHouseMultiDbTests` | Excluidas por el filtro oficial de `dotnet-ci`; la ejecución local sin variables las reportó como fallos de configuración, no como PASS | Definir `CLEARING_HOUSES_REQUIRE_DATABASES=true` y ambas cadenas `CLEARING_HOUSES_*_CONNECTION_STRING` |
| Live diferencial | No existe perfil/vector diferencial sustentado | Aportar evidencia normativa verificable, publicar/homologar el perfil y configurar WSAXON bajo una política permitida |
| E2E diferencial con referencia productiva | Las referencias disponibles son transacciones entrantes con addenda `05`, no respuestas diferenciales | Aportar un archivo diferencial verificable y su perfil homologado |
