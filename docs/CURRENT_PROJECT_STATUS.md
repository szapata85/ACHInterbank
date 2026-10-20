# Estado consolidado del proyecto ACHInterbank

**Corte de revisión:** 2026-10-10  
**Alcance:** código fuente, solución .NET, SPA, pruebas, migraciones, documentación interna y fuentes normativas entregadas en Sources.

## 1. Resultado ejecutivo

Para el frente de integración ACH Colombia trabajado en FASE 1–8, el estado del paquete r908 es **IMPLEMENTACIÓN COMPLETADA / VALIDACIÓN EJECUTABLE FINAL PENDIENTE**. Se implementaron el contrato CFA MFT Boundary, programación operativa persistente por ciclo, Quartz outbound dinámico, ManagedFolder/SFTP CFA, administración y prueba de conexión, seguimiento de transferencias, recepción automática OUT/RET, recuperación/idempotencia y un harness E2E controlado con dos nodos.

Este corte **no** declara producción lista, homologación con ACH Colombia, UAT aprobada, aceptación de usuario, release readiness ni “implementación interna validada”. En el host de preparación no estuvieron disponibles `.NET` ni Docker, y la validación frontend completa tampoco pudo ejecutarse por dependencias no disponibles. El harness de FASE 8 quedó preparado pero **no ejecutado**.

El problema histórico de compilación/restore `NU1105` para `Cfa.ACHInterbank.Persistence.Migrations.SqlServer.csproj` permanece corregido en la solución y el validador del grafo sigue disponible. El resumen operativo para continuar está en `docs/ACH_COLOMBIA_INTERNAL_CLOSURE.md`.

**Actualización focal 2026-10-11 — NACHA Config UX:** `NACHA-CONFIG-UX-001` cerró UX6 y queda `CLOSED`, `NEXT_ALLOWED_PHASE=NONE`. El cierre no agrega funcionalidad ni altera backend, motor NACHA-M, goldens o migraciones; endurece la SPA de parametrización en accesibilidad, Material, branding dinámico, rutas, estados, mensajes de error, responsividad y guardrails de `PUBLICADO`/publicación fail-closed/aislamiento ACH-CENIT. Los validadores estáticos disponibles UX0/UX2/UX3/UX4/UX5/UX6 pasan; Angular/Jasmine/Playwright quedan `NOT_RUN_ENVIRONMENT` por ausencia de `node_modules` y .NET queda `NOT_RUN_ENVIRONMENT` por ausencia de `dotnet`. Esto no equivale a UAT ni release readiness. Evidencia canónica: `docs/NACHA_CONFIG_UX_IMPLEMENTATION_PACKAGE_2026-10-11/NACHA_CONFIG_UX_STATE.md`.

## 2. Evidencia inspeccionada

- 7 proyectos `.csproj`, todos `net10.0`.
- 1 solución `ACHInterbank.sln`.
- 1.730 archivos C# bajo `src/` + `tests/`.
- 697 archivos TS/HTML/SCSS bajo la SPA.
- Migraciones PostgreSQL y proyecto separado de migraciones SQL Server.
- Copias normativas comparadas por SHA-256 contra los archivos entregados; ver `docs/normativa/SOURCE_MANIFEST.md`.
- Estado durable previo consolidado en `MEMORY.md`.
- Auditoría funcional de SPA en `docs/ux/SPA_ROUTE_FUNCTIONAL_AUDIT.md`.

La revisión actual fue estática porque el entorno de preparación no dispone de `dotnet`. No se declara build/test exitoso del paquete actual hasta ejecutarlo en un host con .NET 10.

## 3. Corrección del NU1105

### Antes

`src/Cfa.ACHInterbank.Api/Cfa.ACHInterbank.Api.csproj` contiene:

```xml
<ProjectReference Include="..\Cfa.ACHInterbank.Persistence.Migrations.SqlServer\Cfa.ACHInterbank.Persistence.Migrations.SqlServer.csproj" />
```

El proyecto físico existía y contenía las migraciones SQL Server, pero no estaba incluido en la solución. El proyecto de pruebas referencia la API, por lo que el error aparecía también en `Cfa.ACHInterbank.Tests` de forma transitiva.

### Después

`ACHInterbank.sln` incluye ahora:

```text
Cfa.ACHInterbank.Persistence.Migrations.SqlServer
  src\Cfa.ACHInterbank.Persistence.Migrations.SqlServer\Cfa.ACHInterbank.Persistence.Migrations.SqlServer.csproj
```

Validación estática aplicada:

```text
SOLUTION GRAPH: OK
  solution projects: 7
  disk projects:     7
  project references: all targets exist and are loaded by the solution
```

Controles añadidos:

- `scripts/validate-solution-projects.py`
- `scripts/repair-visualstudio-restore.ps1`
- `docs/BUILD_AND_TROUBLESHOOTING.md`

## 4. Código vs. documentación normativa

| Área | Fuente | Código observado | Estado |
|---|---|---|---|
| ACH Colombia — ciclos V35 | `ACH-Colombia-V35.md`, 2.4.1 | `RegulatoryCycleScheduleCatalog.AchColombiaV35`: 5 ciclos 19:01–08:30, 08:31–11:00, 11:01–14:00, 14:01–16:00, 16:01–18:00; ciclo 5 sin débito monetario | **ALINEADO** |
| ACH Colombia — MFT | V35, 2.4.2/2.4.2.1/2.4.2.2 | `ManagedFolder` y `SftpEndpoint` hacia infraestructura CFA, selección efectiva por configuración, host-key, publicación atómica, administración, prueba de conexión, seguimiento e inbound OUT/RET | **IMPLEMENTADO / VALIDACIÓN EJECUTABLE FINAL PENDIENTE / HOMOLOGACIÓN EXTERNA PENDIENTE** |
| ACH Colombia — candidata 7 ciclos | Fuente candidata `Nuevos Ciclos de Proceso y Horarios` + política versionada interna | Etapas horarias, reglas funcionales C1–C7 conocidas, ventana móvil de cuatro oportunidades y aislamiento de V35 histórica están modelados; `EffectiveFrom = null` y Ciclo 4 sin hora inicial resuelta | **IMPLEMENTADO INTERNAMENTE / INACTIVE / NOT ACTIVATABLE / UAT PENDIENTE** |
| CFA — restricciones operacionales | Modelo interno separado de regulación | `CfaOperationalTransactionRestriction` persistente, resolver global → cámara → ciclo, API administrativa, SPA Angular Material y navegación administrativa; `TransactionPolicies` ya no existe en appsettings | **IMPLEMENTADO INTERNAMENTE — UAT PENDIENTE** |
| ACH Colombia — GoAnywhere/SFTP/S3 | V35, 2.4.2 | ACHInterbank se integra con la frontera MFT/SFTP expuesta por CFA; no embebe GoAnywhere ni modela un SFTP propiedad de ACH Colombia. FASE 8 usa SFTP local y actor GoAnywhere simulado únicamente como harness controlado | **BOUNDARY IMPLEMENTADO; CONECTIVIDAD/HOMOLOGACIÓN EXTERNA PENDIENTE** |
| CENIT — 5 ciclos base | DSP-152 Anexo 2, 3.1 | `RegulatoryCycleScheduleCatalog.CenitDsp152` conserva 07:30–10:30, 11:00–13:00, 13:30–15:00, 15:30–17:15, 17:45–18:45 y 19:15 como límite final del ciclo 5 | **ALINEADO EN VENTANAS BASE** |
| CENIT — excepciones ciclo 1 | Manual CENIT 3.1 | `DEV-CENIT-CYCLE1-R23-ROR` implementa excepciones contextuales sin abrir los flags globales: R23 de días anteriores y ROR de la devolución del Día Hábil Bancario inmediatamente anterior; falta ejecución automatizada en host .NET 10 | **IMPLEMENTADO — VALIDACIÓN AUTOMATIZADA PENDIENTE** |
| CENIT — límite de salida ciclos 1–4 | DSP-152 Anexo 2, 3.1 | La fuente expresa colocación de salida hasta antes del inicio del siguiente ciclo; el modelo actual usa `OutputReleaseTime = EndTime` para ciclos 1–4 | **SEMÁNTICA A ACLARAR / NO CAMBIAR A CIEGAS** |
| CENIT NACHA-M — longitud | Manual NACHA-M 7-may-2026, sec. 3 | Motor/perfiles trabajan sobre registros de 106 caracteres | **ALINEADO** |
| CENIT NACHA-M — cardinalidad PPD/CCD/CTX | Manual NACHA-M, sec. 3.1/3.2 | `BuildCenitCardinalityPolicy`: PPD crédito prenote 0–1, PPD débito 1, CCD 1, CTX original 1–9.999 y prenote 1 | **ALINEADO** |
| CENIT — causales Rxx | Anexo A | `CenitIncomingReturnPolicy.CauseDefinitions` contiene R01...R35 aplicables y seeder añade R60–R74 para ROR | **ALINEADO EN CATÁLOGO DEDICADO** |
| CENIT — rechazos Dxx | Anexo B | Seeder dedicado separa `AchFileRejectionCode` D01–D06 del catálogo transaccional | **ALINEADO EN MODELO** |
| CENIT Gateway/PO | Manual de Usuario CENIT | Existe interfaz `ICenitGatewayTransportAdapter` y simulador local; Production rechaza habilitarlo | **BOUNDARY INTERNO; LIVE EXTERNO PENDIENTE** |
| Perfiles NACHA ordinarios | Fuente vigente por cámara | Runtime usa snapshots `PUBLISH` y perfiles separados por cámara/servicio/flujo | **INTERNAMENTE CERRADO PARA ALCANCE ORDINARIO SOPORTADO** |

### Checkpoint interno: ACH Colombia 7 ciclos

El frente ACH Colombia 7 ciclos permanece **IMPLEMENTADO / VALIDACIÓN EJECUTABLE FINAL PENDIENTE / UAT PENDIENTE**. La regulación y la programación operativa CFA están separadas: C1 puede usar `06:30` como hora operacional y C7 `17:50` como latest-start operacional sin reemplazar `StartTime`/`CutoffTime` regulatorios; C2-C6 no tienen horas automáticas inventadas.

La candidata no está activada. `ACHCOL-7C-CANDIDATE` debe permanecer **INACTIVE / NOT ACTIVATABLE** hasta resolver conjuntamente:

1. el `EffectiveFrom` oficial de la política de 7 ciclos; y
2. la hora inicial oficial del Ciclo 4 por la inconsistencia `11:01pm`/`11:01am`.

No se declara producción, homologación, go-live, release readiness ni aceptación de usuario para este frente. El harness controlado FASE 8 está preparado, pero no fue ejecutado en el host de preparación.

### Observación importante: CENIT ciclo 1

La fuente dice explícitamente que la primera sesión normalmente no compensa devoluciones, **salvo**:

- devoluciones de devoluciones del día hábil bancario inmediatamente anterior; y
- devoluciones de días anteriores por causal R23.

La configuración binaria por clase funcional (`AllowsReturn`, `AllowsReturnOfReturn`) permanece en `false` para el ciclo 1. `DEV-CENIT-CYCLE1-R23-ROR` agrega únicamente política contextual: Return en ciclo 1 admite R23 cuando corresponde a una entrada de días anteriores y conserva las validaciones R23 existentes; ROR en ciclo 1 exige que la devolución origen sea del Día Hábil Bancario inmediatamente anterior, resuelto con `IOperationalCalendarService`. La lógica falla cerrada ante ausencia de policy, calendario o contexto suficiente y está aislada a CENIT. Las pruebas focales y de regresión CENIT 2–5/ACH Colombia están escritas, pero no pudieron ejecutarse en este host porque no existe `dotnet`; por ello el slice aún no se reclasifica a `IMPLEMENTADO — UAT PENDIENTE`.

## 5. Capacidades preservadas y estado aceptado

Según `MEMORY.md`, se preservan los contratos ya aceptados; cada frente conserva su propio nivel de evidencia y no se promueve de estado sin ejecución verificable:

- inmutabilidad de configuración NACHA publicada;
- selección efectiva y table-driven de perfiles ordinarios;
- autoridad PUBLISH inbound/outbound ordinaria;
- cardinalidad CENIT publicada;
- OPS-GAP-001;
- OPS-GAP-002 / 2A–D: implementación ampliada hasta SFTP CFA, administración, observabilidad e inbound, con gate ejecutable final pendiente;
- OPS-GAP-004 local runtime E2E;
- OPS-GAP-005 trazas;
- OPS-GAP-006 ciclos configurables;
- CENIT-FORMAT-NACHAM a nivel interno;
- RET-GAP-018 y RET-GAP-019.

Una capacidad implementada o previamente cerrada en un slice no equivale a homologación externa, validación del paquete r908 ni release productivo.

## 6. Pendientes vigentes — clasificación operativa

La clasificación canónica está en `docs/PENDING_WORK_CLASSIFICATION.md`. Para el cierre técnico se separan cuatro estados: desarrollo propio, deuda técnica no bloqueante, dependencias externas y UAT de usuario.

### 6.1 Desarrollo propio que sí requiere código antes de entregar el alcance correspondiente

- **NACHA-RULE-METADATA-RESIDUAL:** cerrar solo residuales concretos y reproducibles; no reabrir slices ya aceptados.
- **UAT-INTEROPERABILITY-CONTRACT:** implementar backend autorizado para `nacha-security/interoperability/*` o retirar/deshabilitar coherentemente ese contrato SPA.

### 6.2 Capacidades que ya tienen implementación y deben pasar a verificación/UAT

- **ACHCOL-CLAIMS-DEV14:** implementación completa para el alcance acordado: Domain/Persistence, lifecycle e historial, Application/API, permisos exclusivos `Claims.*`, navegación y SPA “Reclamaciones ACH”. DEV14 permanece separado de ReturnOut, Addenda 99, ROR, `Certified` y SOL02. **Validación ejecutable pendiente** en un host con toolchain disponible; no se declara UAT/release readiness.
- **DEV-CENIT-CYCLE1-R23-ROR:** implementación terminada con pruebas focales/regresión añadidas; queda en **VALIDACIÓN AUTOMATIZADA PENDIENTE** porque el host actual no dispone de .NET 10. Solo después de ejecución satisfactoria se reclasifica a `IMPLEMENTADO — UAT PENDIENTE`.

La relectura del código actual encuentra implementación para manual review de respuestas, administración de status mappings, resolución de excepciones de conciliación, scheduler persistente/clusterizable con historial y administración, vistas regulatorias CENIT y administración NACHA config. Estas capacidades ya no deben figurar automáticamente como “falta desarrollo” por observaciones históricas de la auditoría SPA; quedan como **PRUEBA/UAT PENDIENTE** hasta ejecutar la evidencia sobre el artefacto exacto.

### 6.3 Dependencias externas

- **OPS-GAP-003 — CENIT Gateway/PO:** contrato operativo e intercambio real CFA↔CENIT.
- **ACH Colombia MFT externo:** topología productiva final, rutas INPUT/OUTPUT, usuario/credencial, huella, firewall/lista blanca, conectividad CFA↔GoAnywhere/S3/Integra y homologación. La aplicación soporta ManagedFolder/SFTP CFA; la infraestructura real no está validada en este paquete.
- **Homologaciones oficiales / endpoints externos:** CENIT, SOAP UAT/mock y cualquier vector o ambiente controlado requerido.
- **PSE-SCOPE-001:** diferido hasta autoridad/definición y certificación específica.

No desarrollar a ciegas estos puntos mientras falte el insumo externo.

### 6.4 Pruebas que permanecen pendientes para entrega a usuarios

La UAT funcional, validación de roles/pantallas, recorridos ACH Colombia/CENIT, administración NACHA, manual review, mappings, conciliación, scheduler, evidencia visual y acta de aceptación quedan **pendientes intencionalmente** para ejecución por usuarios sobre el paquete entregado. Para el frente ACH Colombia FASE 1–8 también permanecen pendientes el build/tests .NET del paquete exacto, las pruebas frontend completas y la ejecución Docker del harness FASE 8 con dos nodos; por ello todavía no existe validación ejecutable final.

### 6.5 Deuda técnica no bloqueante

- `AuthenticationService.GetTokenAsync()` mantiene `NotImplementedException` sin consumidores productivos identificados: eliminar contrato muerto o implementar cuando exista flujo autorizado.
- `REFACTOR-CA-SOLID-RES-001` completó estructuralmente R1–R9 y ejecutó R10 únicamente como recertificación/evidence consolidation. R10 no modifica `src/**` ni `tests/**`; el árbol productivo exacto de R9 permanece en 1.559 archivos (SHA-256 `517e414875074a9eeca9071a20ca4230986e5d3136cd6e62455369b8b11ea91e`) y el árbol de tests en 373 archivos (SHA-256 `d1a9a58a3dc5b744d19e6034e38577e64a71b0ba429b2b1063e1a74f81847f62`). Los ratchets estáticos preservan R8A Reports 7 controllers/20 actions/`CanReadAch` 20/20, R8B Certificate Management 10 services/máximo 1 por file/10 contracts/10 DI mappings, R9 reloj real behavior-determining 0 y sync-over-async 0 con 9 consumers `TimeProvider`, `Contains("CENIT")=8`, Persistence fallback=57 y `PaymentRailOperationalStrategy=0`. El scanner SOLID continúa en exit 1 con 237 Result-pattern findings y 13 SRP flags, `UNCHANGED`, por lo que es deuda residual no bloqueante de este programa y no un PASS. R0–R10 permanecen `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`: faltan Git metadata del artefacto exacto y los gates .NET/provider/runtime obligatorios. `NEXT_ALLOWED_PHASE=NONE`; el plan no define R11.

## 7. Qué se cambió en este paquete

Sobre el ZIP R9, este paquete ejecuta **solo R10** de `REFACTOR-CA-SOLID-RES-001`. El plan define R10 como recertificación interna y cierre documental; no autoriza otra ronda de refactor. No se modificó código productivo ni tests.

- Se agregó `scripts/validate-r10-final-recertification.py`, que exige la cadena completa R1→R9 y consolida los ratchets finales del programa. El validator fija además el árbol productivo/test exacto de R9 para impedir cambios productivos incidentales durante R10.
- La recertificación estática conserva: Application ASP.NET/WCF/NLog/X509Store=0; `AppSettings.Settings`=0; hidden critical collaborators=0; Persistence fallback=57; `new HttpClient`=0; raw default injection=0; generic retry=0; SOAP/WCF blind retry=0; sensitive logs=0; `INachaFileBuilder` 6/6; R5 parser/validators/persistence/stages preservados; broad MFT facade consumers=0; SFTP construction outside factory=0; service locator=0; Command Center query side effects=0; `IAchReturnsService` 2 métodos/2 callers; Reports 7/20/20; Certificate Management 10/1/10/10; R9 real-clock selected scope=0 y sync-over-async=0; `PaymentRailOperationalStrategy=0`; `Contains("CENIT")=8`.
- El scanner `scan_result_solid.py` se ejecuta y continúa en **FAIL ejecutado** (exit 1): 237 Result-pattern findings y 13 SRP flags. R9→R10 es `UNCHANGED`; no se limpia esta deuda en R10 y no se reporta como PASS.
- Comparación SHA-256 contra el ZIP R9: `src/**` 1.559 archivos/0 cambios y `tests/**` 373/0. Dentro de las protecciones: TestData 15/0, seis goldens NACHA 6/0, fingerprint characterization 1/0, fixture CENIT ROR 1/0, `docs/normativa` 12/0, proyecto/migraciones SQL Server 79/0, `web/**` 772/0 y `docs/spa/**` 4/0.
- `BEHAVIORAL_DIFF=NONE`, `NORMATIVE_DIFF=NONE`, `DB_MIGRATIONS=NONE`.
- El archivo fuente no contiene `.git`; `git status --short` y `git rev-parse HEAD` retornan exit 128 y el SHA de implementación permanece `UNAVAILABLE_SOURCE_ARCHIVE_NO_GIT_METADATA`. `dotnet` y Docker no están instalados en este host. Por tanto build Release, suites de arquitectura/focales, tests provider-specific SQL Server/PostgreSQL, goldens/fingerprint/CENIT-ROR ejecutables, backend broad suite y runtime/E2E quedan `NOT_RUN_ENVIRONMENT`. No se declara ningún runtime PASS.
- El controlled E2E MFT/Quartz con Docker es condicional en R10 (`si el ambiente lo soporta`), de modo que Docker ausente no es por sí solo el blocker de cierre. Sí bloquean cierre la falta de evidencia exacta de commit y los gates .NET/provider/runtime que el plan exige.

R10 queda `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`; el programa no se promueve a `VERIFIED/CLOSED` y R0–R9 no se promueven retroactivamente.

## 8. Validación pendiente para cierre interno

En un ambiente que pueda ejecutar el artefacto exacto, repetir primero los validators estáticos y después los gates obligatorios del plan:

```powershell
python scripts/validate-solution-projects.py
python scripts/validate-r2-di-configuration.py
python scripts/validate-r3-external-resilience.py
python scripts/validate-r4a-nacha-builder-facade.py
python scripts/validate-r4b-nacha-record-renderers.py
python scripts/validate-r4c-nacha-assembly-pipeline.py
python scripts/validate-r4d-chamber-flow-policies.py
python scripts/validate-r4e-chamber-resolution.py
python scripts/validate-r5a-inbound-architecture.py
python scripts/validate-r5b-inbound-validation.py
python scripts/validate-r5c-inbound-persistence.py
python scripts/validate-r5d-inbound-postprocessing.py
python scripts/validate-r6a-mft-interface-segregation.py
python scripts/validate-r6b-mft-implementation-separation.py
python scripts/validate-r6c-incoming-command-center.py
python scripts/validate-r7-returns-separation.py
python scripts/validate-r8a-reports-controller-modularization.py
python scripts/validate-r8b-certificate-management-service-cohesion.py
python scripts/validate-r9-deterministic-time-async.py
python scripts/validate-r10-final-recertification.py
python scripts/scan_result_solid.py
dotnet build ACHInterbank.sln -c Release
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build
```

Además deben ejecutarse las suites focales de architecture/NACHA/inbound/Returns/MFT/Quartz/SOAP-integrations, validaciones SQL Server + PostgreSQL donde apliquen y los controles golden/fingerprint/CENIT ROR sobre el mismo artefacto. SPA build/test solo es gate de R10 si SPA fue tocada; R10 no la tocó. Controlled MFT/Quartz E2E se ejecuta si el ambiente soporta Docker/Compose.

## 9. Criterio de cierre del programa

`REFACTOR-CA-SOLID-RES-001 = CLOSED` únicamente si todos los gates internos obligatorios de R10 pasan sobre el commit/artefacto exacto y la documentación/memory queda actualizada. Este paquete no cumple ese criterio porque carece de Git metadata y no puede ejecutar los gates .NET/provider/runtime.

La deuda residual conocida no debe confundirse con blockers nuevos: SOLID 237/13, `Contains("CENIT")=8` y Persistence fallback=57 permanecen sin aumento y fuera del alcance R10. El siguiente paso no es R11: `NEXT_ALLOWED_PHASE=NONE`. Solo corresponde **re-ejecutar la recertificación de R10 sobre este mismo artefacto en un ambiente ejecutable**. Esto no implica UAT_READY, USER_ACCEPTED, RELEASE_READY ni homologación externa.

## Programa planificado TPA-GOV-001 — terceros, cuentas y prenotificaciones

El 2026-10-11 se inicializó el programa `TPA-GOV-001`. Antes de iniciar TPA0 se aplicó la corrección de diseño `TPA-DESIGN-R2`: el master canónico es cliente → tercero → cuenta y es **agnóstico de cámara**; ACH Colombia V35 y CENIT aparecen únicamente después, en elegibilidad y validación contextual. El onboarding silencioso existente en `AchTransactionService` y `AchBulkTransactionService` se define como invariante y debe preservarse o mejorarse. El programa está **PLANNED** y `NEXT_ALLOWED_PHASE=TPA0`; todavía no introduce cambios funcionales ni migraciones. Ver `docs/ai/third-party-accounts/START_HERE.md`.
