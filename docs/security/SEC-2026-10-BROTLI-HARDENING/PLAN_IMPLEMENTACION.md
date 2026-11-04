# PLAN DE IMPLEMENTACIÓN — SEC-2026-10 | ACHInterbank

**Estado:** `SEC-I04_IMPLEMENTED_UNVERIFIED_ENVIRONMENT / SEC-I05_NEXT`  
**Fecha de análisis:** 2026-10-26  
**ZIP fuente:** `ACHInterbank_ACH_CR_I06_FIX01_COMPLETO.zip` (baseline funcional); entrega actual SEC-I01 desde `ACHInterbank_ACH_CR_I06_FIX01_SEC_PLAN_MEMORY_COMPLETO.zip`  
**SHA-256 del ZIP fuente:** `f52c0a95617e239b29c92b35ca7149a348a81cb0dec8eafa760d6c313f0d1199`  
**Owner de continuidad:** `docs/security/SEC-2026-10-BROTLI-HARDENING/MEMORY_STATE.md`  
**Iniciativas:** `CR-SEC-001` (parcheo, dependencias y despliegue) y `CR-SEC-002` (IAM y hardening).

## 0. Decisión explícita del solicitante — NO BLOQUEAR INCREMENTOS POR AMBIENTE

- **La indisponibilidad de ambiente de desarrollo, SDK .NET, Node/npm, Docker, SQL Server, PostgreSQL, UAT, MFT, CFA, ACH Colombia, CENIT o credenciales NO bloquea la implementación del siguiente microincremento que pueda realizarse de forma segura con inspección, edición y validación estática.**
- Registrar la ausencia con el estado exacto `IMPLEMENTED_UNVERIFIED_ENVIRONMENT` y adjuntar comandos, criterios de aceptación y evidencias aún por ejecutar. **No transformar `NOT_RUN_ENVIRONMENT` en PASS.**
- Proceder al incremento siguiente al completar el alcance de código/documentación y controles estáticos posibles, aun cuando build, tests, SCA online, contenedores o integración queden pendientes; acumular estos pendientes en el *backlog de validación final*.
- Una dependencia contractual o técnica imprescindible que impida modificar el código sin especulación se registra como `BLOCKED_TECHNICAL` solo para esa microtarea; continuar otras microtareas independientes. **No inventar contratos, parches inexistentes ni datos monetarios.**
- **Separación de gates:** `GO_INCREMENT` permite continuar implementación; `GO_DEPLOY` exige compilación, pruebas pertinentes y evidencias; `GO_LIVE_MONETARY` exige además homologación externa y aprobación de Riesgo, Seguridad, QA y Operaciones. `GO_INCREMENT` nunca autoriza despliegue ni transacciones reales.
- Esta política aplica a las nuevas tareas de seguridad y **no revoca** el `NO_GO` operativo del proyecto ACH-CR-001, ni habilita Quartz, MFT o integraciones monetarias reales.

## 1. Alcance, evidencia y límites

### 1.1 Línea base leída del ZIP fuente (revisión estática)

| Superficie / archivo real | Valor observado | Cambio a planificar | Evidencia de cierre |
|---|---|---|---|
| `src/Cfa.ACHInterbank.Api/Dockerfile` | `aspnet:10.0.13`, `sdk:10.0.402` | >= `aspnet:10.0.14`, SDK compatible `10.0.403`; fijar digest validado | Dockerfile, imagen ejecutada y versión runtime |
| `global.json` | `10.0.402` | `10.0.403` con `rollForward` acorde a política | `dotnet --info`, build |
| `src/Cfa.ACHInterbank.Api/Cfa.ACHInterbank.Api.csproj` | `RuntimeFrameworkVersion=10.0.13` | `10.0.14` o política de servicing justificada | publish + dependencias + runtime |
| `.csproj` en otros proyectos | referencias Microsoft `10.0.13` | inventario y actualización **solo donde exista release compatible** | `dotnet list package --include-transitive --vulnerable` |
| `web/ach-interbank-ui/Dockerfile` | `node:24.21.0-alpine`, `nginx:1.30.5-alpine` | >= `node:24.22.1-alpine` y >= `nginx:1.30.6-alpine` si tags disponibles | build frontend, `node -v`, `nginx -v`, digest |
| Dockerfile Angular | `npm install --package-lock-only` antes de `npm ci` | reparar/versionar lock en fuente y `npm ci` determinista | `npm ci`, audit, build, diff lock |
| `AuthService.cs`, `UsersService.cs`, `UserConfiguration.cs` | cálculo/validación directa de SHA-256 y seed de administrador con contraseña derivada de literal | migración de contraseñas y aprovisionamiento seguro separado | pruebas de autenticación, migración y rotación |
| `docker-compose.yml` | `ASPNETCORE_ENVIRONMENT=Development` | preservar carácter local; definir configuración productiva segregada, sin secretos embebidos | revisión de configuración por entorno |

**Atención:** el ZIP declara versiones fuente. No se dispone de prueba de versiones realmente ejecutadas en servidores ni de un SBOM completo. No se ha demostrado una ruta explotable Brotli en la API: verificar descompresión efectiva en request/response y dependencias transitivas antes de asignar *explotabilidad confirmada*.

### 1.2 Contexto externo verificado al documentar

- Node.js, aviso del 21-oct-2026: CVE-2026-111497; mínimo reparado de la rama 24: `24.22.1`. Fuente: https://nodejs.org/en/blog/vulnerability/october-2026-security-releases
- Microsoft, actualización fuera de banda del 25-oct-2026: .NET 10 runtime `10.0.14`, SDK `10.0.403`, CVE-2026-111497 y CVE-2026-114148; comprobar también CVE adicionales del servicing. Fuente: https://devblogs.microsoft.com/dotnet/dotnet-october-2026-oob-security-updates/ y https://dotnet.microsoft.com/en-us/download/dotnet/10.0
- Nginx, publicación 14-oct-2026: versión estable `1.30.6` con correcciones de seguridad. Fuente: https://nginx.org/news.html
- **Versiones mínimas a fecha de redacción, NO mandato de congelarlas indefinidamente.** Antes de modificar, comprobar disponibilidad de etiquetas, plataforma, compatibilidad, firmas/digests y si existen versiones de parche posteriores.
- Node es *build stage* de Angular, no evidencia de Node como servidor público de la SPA. ASP.NET 10 no implica por sí solo descompresión de Brotli no confiable: identificar condiciones concretas de uso.

### 1.3 Fuera del alcance

- Cambio de reglas financieras, formato NACHA-M, conciliación, importes, reversos, devoluciones, secuencias o causalidad de errores.
- Cambio de calendario o compatibilidad: ACH Colombia **7 ciclos** conforme a decisiones internas vigentes y CENIT **5 sesiones**, por separado.
- Habilitación del job `AchPendingCycleReassignment` / Quartz, de la capa CFA/MFT real, o ejecución de pagos monetarios reales.
- Resolución inventada de datos contractuales SOAP/MFT, roles o excepciones de certificados.
- Reemplazar esta memoria por las memorias existentes de `docs/ach-cycle-reassignment` u otros programas.

## 2. Registros de decisión y reglas de seguridad

| ID | Decisión vinculante para esta iniciativa |
|---|---|
| SEC-DEC-001 | El artefacto principal se conserva completo; cada entrega se hace como **nuevo ZIP completo**, nunca como ZIP de diffs; conservar baseline y SHA-256 externo. |
| SEC-DEC-002 | Sin ambiente, avanzar microincrementos con `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`, backlog de verificación y `GO_INCREMENT`; no certificar runtime ficticio. |
| SEC-DEC-003 | Parchear imágenes finales y *build stages*; no suponer que actualizar runtime host parchea contenedores o aplicaciones self-contained. |
| SEC-DEC-004 | No usar `npm audit fix --force`, upgrades *major* ni `dotnet add package` indiscriminados; resolver impactos de API y lockfile de forma explícita. |
| SEC-DEC-005 | Nunca activar job de reasignación, servicios reales ni transmisión monetaria para demostrar parches. |
| SEC-DEC-006 | Las credenciales estáticas son asunto separado crítico: prohibido difundir valor literal en memoria, logs, reportes o ejemplos. |
| SEC-DEC-007 | Los paquetes NuGet se actualizarán según matriz de compatibilidad/proveedor y advisories; **no asumir** que todo paquete `10.0.13` tiene parche `10.0.14`. |
| SEC-DEC-008 | No convertir un resultado estático en `GO_DEPLOY`; pruebas de regresión ACH/CENIT y acceso a entorno real son gates de despliegue, **no de implementación**. |

## 3. Roadmap incremental: microentregas independientes

### SEC-I00 — Baseline y plan de continuidad

- **Alcance:** inventario de archivos/configuración, CVE y observaciones confirmadas; registrar decisiones, dependencias y memoria; adjuntar al ZIP completo.
- **Artefactos:** este `PLAN_IMPLEMENTACION.md`, `MEMORY_STATE.md` y referencia en `MEMORY.md`.
- **Aceptación:** ZIP válido, rutas correctas, fuente sin cambios productivos, SHA externo registrado.
- **Estado de esta entrega:** `PLAN_DOCUMENTED`. **No parches aplicados.**

### SEC-I01 — Runtime .NET y cadena de compilación (CR-SEC-001)

- **Archivos candidatos:** `global.json`, Dockerfile API, propiedad `RuntimeFrameworkVersion`, archivos `.csproj` con dependencias Microsoft explícitas.
- **Implementación:** fijar versiones corregidas y compatibles; auditar `RuntimeIdentifier`, imágenes multi-stage, Debian/Alpine y dependencias nativas; inventariar proveedor EF Core y migraciones; evitar cambios de esquema DB sin motivo.
- **Evidencia estática posible sin ambiente:** diff de `FROM`, propiedades/paquetes y búsqueda de referencias antiguas; coherencia entre SDK/Runtime.
- **Evidencia ejecutable pendiente de entorno:** `dotnet --info`, `dotnet restore`, `dotnet build`, `dotnet test`, `dotnet list package --include-transitive --vulnerable`, `dotnet publish`, `docker compose build --pull` y runtime inspeccionado en contenedor.
- **Salida sin ambiente:** `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`; puede abrirse SEC-I02 sin build real.

#### Resultado SEC-I01 — 2026-10-26

- `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`. Cambio aplicado al código del ZIP completo: imágenes .NET 10.0.14/SDK 10.0.403, global.json 10.0.403, RuntimeFrameworkVersion 10.0.14, 20 PackageReference 10.0.14; sin cambios en lógica monetaria.
- Validación estática JSON/XML/diff y inventario: `PASS_STATIC`. Build, restore, test, Docker y SBOM real: `NOT_RUN_ENVIRONMENT`.
- Evidencias: `SEC-I01_EVIDENCIA_ESTATICA.md` y `SEC-I01_DIFF_PATCH.txt` en este directorio. Binarios y `obj` existentes en baseline no son entregables validados.
- `GO_INCREMENT=YES`, `GO_DEPLOY=NO`, `GO_LIVE_MONETARY=NO`.
- Próximo: `SEC-I02-A_PATCH_NODE_NGINX_DOCKERFILE`, aunque no haya ambiente. Las pruebas SEC-I01 se acumulan en `EV-01`, `EV-02`, `EV-04`, `EV-06`, `EV-07`.

### SEC-I02 — Node, Nginx y lockfile determinista (CR-SEC-001)

- **Archivos candidatos:** `web/ach-interbank-ui/Dockerfile`, `package.json`, `package-lock.json`, `nginx.conf`, `.dockerignore` aplicable.
- **Implementación:** sustituir bases corregidas; revisar compatibilidad de npm con Node y de Nginx con configuración; eliminar reparación del lock en tiempo de build cuando lock coherente; fijar digest después de obtener imagen; analizar módulos Nginx habilitados y cabeceras.
- **Evidencia estática:** lectura de Dockerfile/config, lockfile y control de cambios; comprobar que Node no permanezca en imagen final.
- **Evidencia de ambiente:** `npm ci`, `npm audit`, `npm run build -- --configuration production`, `nginx -t`, escaneo de imágenes, `node --version`, `nginx -v`.
- **Salida sin ambiente:** `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`; continuar.

#### Resultado SEC-I02 — 2026-10-27

- `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`: en el Dockerfile SPA se sustituyó `node:24.21.0-alpine` por `node:24.22.1-alpine` y `nginx:1.30.5-alpine` por `nginx:1.30.6-alpine`. La etapa final continúa siendo Nginx, no Node.
- Se **eliminó** del Dockerfile el paso mutable `npm install --package-lock-only`; el build ahora ejecuta `npm ci --include=optional` y falla sin modificar el lock cuando no pueda resolver las dependencias.
- Se añadió `npm run security:verify-lock` (script de solo lectura) para comprobar manifiesto, referencias directas y overrides. `package-lock.json` se conservó sin alteraciones: el manifiesto ya estaba sincronizado. Se identificaron **36 tarballs de registry sin `integrity`** que **no se pueden completar de manera confiable sin consultar el registry**; pendientes SEC-I03 y gate de despliegue.
- En este entorno: `npm 10.9.2` sobre Node 22.16.0 permitió `npm ci --dry-run --offline --ignore-scripts`; ello **no valida** npm 12.1.0 sobre Node 24.22.1, descarga real, bundle Angular, `nginx -t`, contenedores ni escaneo. Sin Docker ni red hacia registry, esas pruebas permanecen `NOT_RUN_ENVIRONMENT`.
- La configuración `nginx.conf` y `.dockerignore` se conservaron sin cambios funcionales; revisar hardening en SEC-I05. Las etiquetas oficiales se contrastaron con Docker Hub, pero **no se verificó digest reproducible de plataforma** y no debe inventarse.
- Evidencias: `SEC-I02_EVIDENCIA_ESTATICA.md`, `SEC-I02_DIFF_PATCH.txt`; memoria actualizada. `GO_INCREMENT=YES`, `GO_DEPLOY=NO`, `GO_LIVE_MONETARY=NO`.
- Próximo incremento: `SEC-I03-A_SBOM_INVENTORY`. No bloquear su preparación por falta de ambiente, arrastrando EV-03/04.

### SEC-I03 — SBOM, análisis de dependencias y cadena de suministro (CR-SEC-001)

- **Implementación:** inventario directo/transitivo NuGet/npm/OS; políticas de lock, reproducibilidad, origen de imágenes y controles de artefactos; construir SBOM CycloneDX/SPDX cuando herramientas estén presentes; documentar vulnerabilidades con severidad, ruta de explotación, remediación y excepción temporal aprobada.
- **Evidencia estática:** manifiesto de referencias y política de excepciones; comprobar que no se empaquetan secretos adicionales.
- **Evidencia de ambiente:** SCA en CI, escaneo de imagen final, SBOM y revisión de licencias; no marcar PASS si servicios de advisories no están disponibles.
- **Salida sin ambiente:** backlog `EXTERNAL_VALIDATION_PENDING`, `GO_INCREMENT` permitido.

#### Resultado SEC-I03 — 2026-10-27

- **Implementado:** inventariador offline reproducible, 2 SBOM CycloneDX 1.6 explícitamente incompletos, manifiesto estructurado de dependencias e imágenes, pruebas offline 3/3, runbook SCA externo y resumen de hashes de salida. Ver `SEC-I03_EVIDENCIA_ESTATICA.md`.
- **Hechos de fuente:** npm lock v3 registra 1.130 nodos (36 sin `integrity`); NuGet presenta 8 csproj con 56 PackageReference y 51 componentes/versiones únicos; Docker declara 6 imágenes externas sin digest fijo en 4 Dockerfiles. Hay 7 archivos legacy `obj/project.assets.json` que NO se consideran prueba del grafo actual después del parche .NET.
- **No demostrado:** CVE actuales, licencias, OS packages, SBOM transitivo NuGet, autenticidad tarball, imagen desplegada, digest real y pruebas de ejecución. SCA y ambiente `NOT_RUN_ENVIRONMENT`.
- **Gates:** `GO_INCREMENT=YES`, `GO_DEPLOY=NO`, `GO_LIVE_MONETARY=NO`. Próximo: `SEC-I04-A_CREDENTIALS_SEED_INVENTORY`. No bloquear por falta de ambiente.

### SEC-I04 — Credenciales, autenticación e IAM (CR-SEC-002)

- **Implementación por microtareas:** (A) inventario del seed y de las rutas de contraseñas; (B) introducir PBKDF2 versionado y verificación compatible con SHA-256 legado, sin cambiar el seed; (C) MFA, administración/roles y revocación de sesión conforme al diseño IAM, pendientes de autorización independiente. **Instrucción posterior del usuario: conservar usuario y clave del seed sin modificarlos.**
- **Protección de datos:** no copiar hashes ni secretos a fixtures/memorias; no resetear usuarios productivos sin procedimiento aprobado de migración y soporte.
- **Evidencia estática:** conservar archivo del seed byte a byte; no generar SHA-256 para contraseñas nuevas; login compatible con SHA-256 heredado; tests escritos y revisión por pares de compatibilidad.
- **Evidencia de ambiente:** tests contra la BD permitida, reversibilidad de migración, usuarios existentes y roles, confirmación de credenciales rotadas por custodio autorizado.
- **Salida sin ambiente:** avanzar en código y tests; dejar la **rotación efectiva de credenciales desplegadas** y regresión como operaciones pendientes externas, sin fingirlas ejecutadas.


#### Resultado SEC-I04 — 2026-10-27

- Estado `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`: nuevo `CredentialPasswordHasher` PBKDF2-HMAC-SHA256 versionado (600.000 iteraciones, salt criptográfico), validación con comparación constante y compatibilidad de **solo lectura** con hashes SHA-256 históricos.
- `AuthService` y `UsersService` usan el nuevo componente; nuevas contraseñas/actualizaciones/restablecimientos se almacenan en PBKDF2; login acepta ambos formatos. **No se modifica el hash de un usuario legado por iniciar sesión**, ni se hacen cambios de esquema.
- **SEC-DEC-009, instrucción del usuario:** `PRESERVE_ADMIN_SEED_CREDENTIAL=TRUE`; `UserConfiguration.cs`, el usuario seed, su contraseña y `HasData` se conservan byte por byte. Esta decisión prevalece sobre el retiro del seed descrito históricamente en versiones previas del plan.
- 4 métodos de prueba xUnit (con casos parametrizados) de compatibilidad, hash salado y rechazo de formatos malformados quedan `WRITTEN_NOT_EXECUTED` sin .NET; pruebas estáticas y comparación de seed `PASS_STATIC`. No afirmar que el password seed es seguro; documentar control compensatorio y riesgo residual.
- Pendientes: validación `dotnet restore/build/test`, integración de BD, pruebas de login/reset y aceptación del riesgo por mantener un seed conocido; `GO_INCREMENT=YES`, `GO_DEPLOY=NO`, `GO_LIVE_MONETARY=NO`.
- Siguiente incremento: **SEC-I05** (`SEC-I05-A_HARDENING_INVENTORY`).

### SEC-I05 — Hardening Docker / API / manejo de payloads

- **Implementación:** configuración específica para producción, mínimo privilegio y file system de contenedores según compatibilidad; proxy confiable, cabeceras, TLS en terminación autorizada, restricción de OpenAPI/Scalar, límites de tamaño/tasa/tiempo, manejo de `Content-Encoding: br` y límites de descompresión donde aplique; verificación de firma/autenticidad e integridad NACHA/MFT; secretos solo por proveedor seguro.
- **Evidencia estática:** configuración por entorno, políticas de acceso y tests de rutas sensibles; registro de decisiones cuando una medida pueda alterar un flujo ACH.
- **Evidencia de ambiente:** `docker compose config`, smoke de endpoints, SAST/DAST autorizados y simulación de entradas inválidas no monetarias.
- **Salida sin ambiente:** continuar; no modificar la semántica financiera ni habilitar networking real no autorizado.

### SEC-I06 — Pruebas automatizadas y regresión ACH / CENIT

- **Cobertura a producir:** xUnit autenticación + rehash/migración, tests Angular, validaciones NACHA-M y firma, idempotencia, controles de secuencia, rechazo, devolución, reintentos inciertos CFA, límites de payload, recepción y envío; pruebas de compatibilidad con ACH Colombia 7 ciclos y CENIT 5 sesiones, diferenciados.
- **Evidencia estática:** casos y fixtures sintéticos trazables. **Sin ejecutar un test, el test queda `WRITTEN_NOT_EXECUTED`.**
- **Evidencia de ambiente:** build/test real, integraciones simuladas en contenedores, PostgreSQL y SQL Server donde apliquen, pruebas de concurrencia y rollback.
- **Salida sin ambiente:** `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`; registrar cobertura y avanzar a preparación SEC-I07.

### SEC-I07 — Homologación, evidencia final y liberación (sin detener entregas de código)

- **Preparación sin ambiente:** matriz de ejecución, responsables, riesgos, pruebas UAT, despliegue, rollback y ventanas de cambio; inventario de pasos a validar cuando haya acceso; evidencias requeridas por regulador/operador y política interna.
- **Gate `GO_DEPLOY`:** build y tests ejecutados, escaneo sin críticos no tratados, versiones verificadas en contenedores, aprobación Seguridad + QA, DR/rollback revisado.
- **Gate `GO_LIVE_MONETARY`:** además de `GO_DEPLOY`, operación y homologación ACH/CENIT/CFA/MFT según aplicabilidad, pruebas financieras, monitoreo, autorizaciones y ventana operativa aprobados. Quartz permanece deshabilitado salvo autorización independiente.
- **Si no existe ambiente:** documento y backlog pueden quedar listos, pero `GO_DEPLOY=NO` y `GO_LIVE_MONETARY=NO`; cerrar implementación como `IMPLEMENTED_UNVERIFIED_ENVIRONMENT` sin declarar pruebas.

## 4. Riesgos, mitigación y prioridad

| Riesgo | Prioridad de tratamiento | Control propuesto | Qué queda sin demostrar |
|---|---|---|---|
| Base .NET vulnerable a Brotli | P0 | `.NET 10.0.14` en runtime real e imágenes nuevas | ruta explotable, versión realmente desplegada |
| Node de build previo a security release | P1 | `Node >= 24.22.1` | uso de descompresión y dependencias efectivas |
| Nginx con parches pendientes | P1 | `Nginx >= 1.30.6`, validar configuración | módulos y vectores activos, versión efectiva |
| SHA-256 legado + seed administrativo preservado por decisión del usuario | P0 | PBKDF2 en altas, cambios y reset; verificador legado para seed; controles compensatorios y aceptación de riesgo | eficacia de controles en cada ambiente y usuarios históricos aún SHA-256 |
| Dependencias/transitivas e imágenes sin SBOM validado | P1 | SCA, escaneo, lock y digests | dependencias y vulnerabilidades de artefacto de despliegue |
| Regresión financiera tras patching | P0 previo a `GO_DEPLOY` | pruebas ACH/CENIT, idempotencia, rollback | resultados reales contra entorno |
| Activación involuntaria del job ACH-CR-001 | P0 de despliegue | conservar Quartz y flag apagados, validar DB persistida | estado efectivo de jobs en BD destino |

## 5. Validaciones concretas que quedan en cola (NUNCA asumir ejecutadas)

```bash
# .NET: en host con SDK y fuentes
dotnet --info
dotnet restore ACHInterbank.sln
dotnet build ACHInterbank.sln -c Release
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release
dotnet list ACHInterbank.sln package --vulnerable --include-transitive

# Angular: ejecutar dentro de web/ach-interbank-ui
npm ci
npm audit
npm run build -- --configuration production

# Docker: entorno con daemon y permisos
docker compose build --pull achinterbank-api achinterbank-spa
docker compose config
docker exec achinterbank-api dotnet --list-runtimes
docker exec achinterbank-spa nginx -v
```

**Nota:** nombres de servicio/instancia se validarán con el Compose efectivo; estos comandos no son evidencia de ejecución. Complementar con `npm ls`, SBOM, `docker image inspect`, firma/digest y escáner institucional. La disponibilidad del registro remoto/del motor Docker debe registrarse sin bloquear el siguiente incremento.

## 6. Evidencia mínima por microincremento

Cada incremento debe anotar en `MEMORY_STATE.md`:
1. ID de tarea, versión base/ZIP, fecha, rutas modificadas y motivo.
2. Código realmente implementado frente a propuesta; archivos C# / TS / Docker / SQL / tests / docs cambiados.
3. Gates estáticos ejecutados (comando y salida), y ejecutables (`PASS`, `FAIL`, `NOT_RUN_ENVIRONMENT`).
4. Estado `GO_INCREMENT` independiente de `GO_DEPLOY` y `GO_LIVE_MONETARY`.
5. Riesgos, versiones/etiquetas comprobadas, pendientes acumulados y `next_microtask`.
6. Hash SHA-256 **externo** del ZIP resultante; no autorreferenciar dentro del artefacto binario.

## 7. Reanudación

1. Leer `MEMORY.md`, `docs/ach-cycle-reassignment/MEMORY_STATE.md`, `docs/security/SEC-2026-10-BROTLI-HARDENING/PLAN_IMPLEMENTACION.md` y `MEMORY_STATE.md` de esta iniciativa.
2. Tomar `next_microtask` de la memoria de seguridad. Inventariar versiones reales del nuevo ZIP: **el plan no ejecutó SEC-I01**.
3. Implementar microincremento; ejecutar todo lo que permita el entorno y registrar lo faltante sin frenar incrementos siguientes.
4. Entregar de nuevo el proyecto **completo** en ZIP; actualizar memoria/plan/registro de cambios, no tocar memorias de otros frentes salvo referencia explícita.
5. Mantener `GO_DEPLOY=NO` y `GO_LIVE_MONETARY=NO` hasta satisfacer sus gates operativos.


### Avance SEC-I05 aplicado (2026-10-27)

`IMPLEMENTED_UNVERIFIED_ENVIRONMENT`: cambios de cabeceras, exposición de documentación, normalización de `X-Forwarded-For` en Nginx, identidad de cliente de rate limiter con allowlist, ocultación de versión Kestrel y pruebas xUnit escritas. No se activan medidas de filesystem no root/readonly ni cambios de rechazo de Brotli o límites financieros hasta homologar volúmenes MFT, certificados, archivos de trazas y NACHA. Detalles en `SEC-I05_EVIDENCIA_ESTATICA.md` y `SEC-I05_RUNBOOK_HARDENING.md`. Se respeta **SEC-DEC-009: conservar clave y usuario del seed sin cambios**. Siguiente SEC-I06; `GO_INCREMENT=YES`, `GO_DEPLOY=NO`, `GO_LIVE_MONETARY=NO`.

## Adenda operativa SEC-I06A / SEC-I06B (2026-10-27)

- SEC-I06 se divide en **SEC-I06A (preparación en modo Chat)** y **SEC-I06B (ejecución real y homologación en entorno autorizado)**.
- SEC-I06A agrega tests xUnit aislados, matriz de trazabilidad y verificador Python offline. No equivale a `dotnet test` ni cierra cobertura financiera real.
- SEC-I06B queda `NOT_RUN_ENVIRONMENT` hasta contar con .NET 10, Docker, BD y entornos simulados/UAT autorizados; no bloquear SEC-I07 (preparación documental) por esa ausencia.
- Usuario/clave/hash del seed administrativo continúan intactos por instrucción expresa (`PRESERVE_ADMIN_SEED_CREDENTIAL=TRUE`); documentar y aceptar su riesgo antes de producción.
- Mantener `GO_INCREMENT=YES`, `GO_DEPLOY=NO`, `GO_LIVE_MONETARY=NO`, Quartz ACH-CR-001 deshabilitado.
