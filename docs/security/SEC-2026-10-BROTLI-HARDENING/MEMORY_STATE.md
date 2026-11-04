# MEMORY_STATE — SEC-2026-10 | ACHInterbank | Parcheo, Brotli e IAM

> **Estado de continuidad del programa de seguridad**. Leer al iniciar cada chat o incremento. No equivale a memoria persistente de ChatGPT, a prueba de despliegue ni a autoridad normativa. Se conserva dentro del ZIP principal del proyecto. Esta iniciativa no reabre automáticamente `ACH-CR-001`.

## Estado estructurado de reanudación

```yaml
initiative_id: SEC-2026-10
subinitiatives: [CR-SEC-001, CR-SEC-002]
base_zip: ACHInterbank_ACH_CR_I06_FIX01_COMPLETO.zip
base_zip_sha256: f52c0a95617e239b29c92b35ca7149a348a81cb0dec8eafa760d6c313f0d1199
parent_artifact: ACHInterbank_ACH_CR_I06_FIX01_SEC_I05_HARDENING_COMPLETO.zip
parent_artifact_sha256: b3e43b46eb24efbf3523f915481dd6ac2115a81dffff9c8b1df76f9af978424a
current_artifact: ACHInterbank_ACH_CR_I06_FIX01_SEC_I06A_TESTS_PREPARATION_COMPLETO.zip
last_updated: 2026-10-27
current_increment: SEC-I06A
increment_status: SEC_I06A_PREPARED_UNVERIFIED_ENVIRONMENT
implementation_started: true
next_microtask: SEC-I07-A_PREP_HOMOLOGACION_SIN_AMBIENTE
next_increment: SEC-I07
security_runtime_validation: NOT_RUN_ENVIRONMENT
security_uat: NOT_RUN_ENVIRONMENT
PRESERVE_ADMIN_SEED_CREDENTIAL: true
seed_user_and_password_modified: false
legacy_password_hashes_automatically_migrated: false
original_ach_cr_001_quartz: DISABLED_KEEP_DISABLED
GO_INCREMENT: YES
GO_DEPLOY: NO
GO_LIVE_MONETARY: NO
missing_environment_blocks_next_increment: false
owner_of_state: docs/security/SEC-2026-10-BROTLI-HARDENING/MEMORY_STATE.md
plan_document: docs/security/SEC-2026-10-BROTLI-HARDENING/PLAN_IMPLEMENTACION.md
```

## 1. Instrucciones durables obligatorias

1. **NO BLOQUEAR EL SIGUIENTE INCREMENTO POR AMBIENTE INDISPONIBLE:** sin .NET/Node/npm/Docker/SQL Server/PostgreSQL/UAT/MFT/CFA/ACH/CENIT, editar fuentes y tests con evidencia estática y continuar. Usar `IMPLEMENTED_UNVERIFIED_ENVIRONMENT` y acumular pruebas en `validation_backlog`.
2. Separar `GO_INCREMENT` (avance de implementación) de `GO_DEPLOY` (release técnico) y `GO_LIVE_MONETARY` (operación financiera). En este snapshot: `YES / NO / NO` respectivamente.
3. No marcar `PASS` una compilación, test, escaneo, homologación ni smoke test no ejecutados; utilizar `NOT_RUN_ENVIRONMENT` o `WRITTEN_NOT_EXECUTED`. No inventar ambiente ni resultados.
4. Si una microtarea requiere contrato/definición externa indispensable y no disponible, dejarla `BLOCKED_TECHNICAL` con el punto concreto y **avanzar otras tareas independientes**. La ausencia de ambiente, por sí sola, no es bloqueo.
5. No modificar semántica NACHA-M, liquidaciones, saldo, causa de devolución/rechazo, admisión, idempotencia, ciclos ACHCOL 7 / CENIT 5 ni contratos CFA por cambios de seguridad sin alcance firmado.
6. Mantener el estado de Quartz `Disabled` y la ejecución de reasignación apagada; no ejecutar flujos monetarios reales, no enviar archivos a cámaras/contrapartes ni intentar credenciales no autorizadas.
7. Entregar cada incremento como **nuevo ZIP completo con código y documentación**, manteniendo y actualizando el memory state de esta iniciativa, plan, decisiones, evidencias y el índice `MEMORY.md`.
8. La memoria existente de `docs/ach-cycle-reassignment/MEMORY_STATE.md` conserva el estado propio ACH-CR-001 `I06-FIX01` / `NO_GO`; no modificarla implícitamente ni atribuirle homologación.
9. Las versiones del plan son **mínimos verificados a 2026-10-26**. Antes de aplicar cambios comprobar versiones/etiquetas oficiales, dependencias, plataforma y digests sin asumir que todos los paquetes Microsoft comparten la misma numeración.

## 2. Estado de microincrementos (no confundir plan con parche)

| ID | Trabajo | Estado al empaquetar este ZIP | Siguiente acción |
|---|---|---|---|
| SEC-I00 | Plan, inventario y memoria en ZIP | `PLAN_DOCUMENTED` | Abrir SEC-I01 |
| SEC-I01 | .NET runtime, SDK, csproj e imagen API | `IMPLEMENTED_UNVERIFIED_ENVIRONMENT` | evidencias EV-01/02/04 y build pendientes |
| SEC-I02 | Node/Nginx/Docker Angular/lockfile | `IMPLEMENTED_UNVERIFIED_ENVIRONMENT` | arrastrar EV-03/04 y validar npm12/Node24, Nginx en Docker |
| SEC-I03 | NuGet/npm/SBOM y scanner de imágenes | `IMPLEMENTED_UNVERIFIED_ENVIRONMENT` | fuente y SBOM parciales generados; completar SCA/digests en entorno autorizado |
| SEC-I04 | PBKDF2 versionado, compatibilidad SHA-256, conservación exacta del seed | `IMPLEMENTED_UNVERIFIED_ENVIRONMENT` | probar con .NET/BD; rehash de usuarios heredados pendiente, sin tocar seed |
| SEC-I05 | Hardening API, Nginx, proxy, cabeceras y docs con compatibilidad financiera | `IMPLEMENTED_UNVERIFIED_ENVIRONMENT` | validar .NET, red/proxy, Nginx1.30.6 y regresión en entorno autorizado |
| SEC-I06 | Tests automatizados, ACH/CENIT/regresión | `NOT_STARTED` | siguiente: SEC-I06-A_TESTS_SECURITY_REGRESSION |
| SEC-I07 | UAT, aprobación GO_DEPLOY/GO_LIVE, rollback | `NOT_STARTED` | preparar matriz aun sin UAT; no autorizar producción sin evidencia |

## 2.1 SEC-I01 — alcance efectivamente modificado

- Dockerfile de API actualizado a `aspnet:10.0.14` y `sdk:10.0.403`; `global.json` 10.0.403; API `RuntimeFrameworkVersion` 10.0.14.
- 20 referencias NuGet directas de 6 csproj alineadas a 10.0.14 (EF Core, ASP.NET Core, Microsoft.Extensions y System.Security.Cryptography). `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3 y demás terceros sin cambios; compatibilidad real sin verificar por falta de SDK.
- JSON/XML/diff pasaron pruebas estáticas; `dotnet restore/build/test`, contenedores, DB, UAT y SCA remoto `NOT_RUN_ENVIRONMENT`.
- `docs/security/SEC-2026-10-BROTLI-HARDENING/SEC-I01_EVIDENCIA_ESTATICA.md` registra exactamente archivos, comandos y riesgos; `SEC-I01_DIFF_PATCH.txt` contiene el diff.
- Los `obj/bin` heredados no representan salida compilada después del parche, ni deben desplegarse.
- Histórico de la fase SEC-I01: se continuó hacia SEC-I02-A_PATCH_NODE_NGINX_DOCKERFILE; continuar con `GO_INCREMENT=YES` aunque el ambiente siga ausente.

## 2.2 SEC-I02 — alcance realmente intervenido (2026-10-27)

- El build SPA fija `node:24.22.1-alpine` y el runtime final `nginx:1.30.6-alpine` (etapas separadas). Versiones publicadas verificadas en fuente oficial, sin pull ni digest en este entorno.
- El Dockerfile elimina reparación mutable del lock y conserva solo `npm ci --include=optional`; el `package-lock.json` **no cambió**. El paquete agrega `npm run security:verify-lock`, script que comprueba consistencia sin red ni mutar archivos.
- Control estático local con Node 22.16.0/npm 10.9.2 y npm ci en modo dry-run y offline. **No es prueba de compatibilidad del objetivo Node24/npm12**. Las 36 entradas de registry sin campo `integrity` se registran como riesgo de cadena de suministro pendiente de SEC-I03, sin fabricar sumas de verificación.
- `nginx.conf` sin cambios; proxys /auth /api /incoming... preservados. No se cambió `docker-compose.yml`, no se habilitó Quartz, no se tocaron contratos ACH/CENIT ni fuentes .NET.
- Los detalles reproducibles están en `SEC-I02_EVIDENCIA_ESTATICA.md` y `SEC-I02_DIFF_PATCH.txt`.
- Resultado `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`. `GO_INCREMENT=YES`, `GO_DEPLOY=NO`, `GO_LIVE_MONETARY=NO`. Próxima microtarea `SEC-I03-A_SBOM_INVENTORY`.

## 2.3 SEC-I03 — SBOM/inventario fuente reproducible (2026-10-27)

- `scripts/security/generate_sec_i03_inventory.py` y tests: offline, sin restauración, instalaciones ni tráfico de red. **3/3 pruebas locales `PASS_STATIC`** y dos regeneraciones deterministas.
- `SEC-I03_INVENTARIO_FUENTE.json` + `SEC-I03_NPM_LOCK.cdx.json` + `SEC-I03_NUGET_DECLARADO.cdx.json` + `SEC-I03_VALIDACION_ESTATICA.json`: 1.130 npm, 36 sin integrity, 8 proyectos/56 referencias NuGet, 6 imágenes declaradas sin digest. `compositions.aggregate=incomplete` en ambos SBOM.
- **Precaución:** los `obj/project.assets.json` anteriores al parche de .NET no se consideran grafo NuGet actual. No se ejecutó `dotnet restore`, `npm ci` con Node24/npm12, `npm audit`, scan Docker, verificación de integridad remota ni licencias. Ninguna CVE se declara cerrada solo por SBOM fuente.
- `SEC-I03_EVIDENCIA_ESTATICA.md` y `SEC-I03_RUNBOOK_VERIFICACION_EXTERNA.md` recogen cobertura y pending gates EV-04/10/11.
- `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`; `GO_INCREMENT=YES`, `GO_DEPLOY=NO`, `GO_LIVE_MONETARY=NO`.
- **Siguiente:** `SEC-I04-A_CREDENTIALS_SEED_INVENTORY` (detectar y remediar IAM sin difundir secretos, mantener ACH-CR-001 Quartz deshabilitado).

## 3. Hechos de baseline SEC-I00 (históricos: versiones previas al parche)

- API Dockerfile contiene `mcr.microsoft.com/dotnet/aspnet:10.0.13` y `sdk:10.0.402`; `global.json` solicita `10.0.402` y API `.csproj` `RuntimeFrameworkVersion=10.0.13`.
- Dockerfile Angular contiene `node:24.21.0-alpine` y `nginx:1.30.5-alpine`; modifica el lock antes de `npm ci` durante build.
- `AuthService.cs`, `UsersService.cs` y `UserConfiguration.cs` contienen verificación/generación SHA-256 para contraseñas y configuración de administrador inicial con dato secreto codificado en fuente; **no copiar credenciales ni hashes**.
- `docker-compose.yml` es de desarrollo con `ASPNETCORE_ENVIRONMENT: Development`; no confundirlo con la configuración de despliegue productivo.
- Los avisos oficiales publicados en octubre de 2026 confirman Node >= `24.22.1`, .NET Runtime >= `10.0.14` / SDK `10.0.403`, Nginx estable >= `1.30.6`. Esto no certifica exposición efectiva ni necesidad de cambiar paquetes no relacionados.
- La fuente base conserva `docs/ach-cycle-reassignment/MEMORY_STATE.md`: `I06-FIX01`, build .NET y UAT no ejecutados, producción no lista, Quartz apagado.
- **Histórico SEC-I00:** Ningún archivo productivo ni runtime fue parchado por SEC-I00. **SEC-I01 sí modificó configuración y dependencias de código fuente**, sin ejecutar runtime. Solo se agregaron los documentos de esta iniciativa y una referencia en `MEMORY.md`.

## 4. Banderas y gates

```yaml
flags:
  SEC_PATCH_CODE_IMPLEMENTED: true
  DOTNET_RUNTIME_VERIFIED: false
  NODE_NGINX_DOCKERFILE_UPDATED: true
  SPA_LOCK_MANIFEST_STATIC_VERIFIED: true
  SPA_TARBALL_INTEGRITY_COMPLETE: false
  FRONTEND_BUILD_VERIFIED: false
  SBOM_SCA_VERIFIED: false
  SBOM_SOURCE_INVENTORY_GENERATED: true
  SBOM_NPM_LOCK_PARTIAL: true
  SBOM_NUGET_DIRECT_ONLY: true
  IAM_MIGRATION_VERIFIED: false
  IAM_NEW_PASSWORD_HASH_IMPLEMENTED: true
  API_NGINX_HARDENING_SOURCE_IMPLEMENTED: true
  PRODUCTION_PROXY_ALLOWLIST_CONFIGURED: false
  DOCS_DISABLED_BY_DEFAULT_OUTSIDE_DEVELOPMENT: true
  NGINX_TARGET_RUNTIME_VERIFIED: false
  ADMIN_SEED_PRESERVED: true
  ACH_CENIT_REGRESSION_VERIFIED: false
  EXTERNAL_HOMOLOGATION_APPROVED: false
  ACH_CR_001_QUARTZ_ENABLE_APPROVED: false
  REAL_MONETARY_EXECUTION_ALLOWED: false
gates:
  GO_INCREMENT: YES
  GO_DEPLOY: NO
  GO_LIVE_MONETARY: NO
```

## 5. Backlog de evidencia de entorno (arrastrar entre incrementos)

| ID | Prueba o constancia | Estado inicial | Gate |
|---|---|---|---|
| EV-01 | `dotnet restore/build/test` en SDK corregido | `NOT_RUN_ENVIRONMENT` | GO_DEPLOY |
| EV-02 | Build .NET imagen real + `dotnet --list-runtimes` | `NOT_RUN_ENVIRONMENT` | GO_DEPLOY |
| EV-03 | `npm ci`, `npm audit`, build Angular, `nginx -t` | `NOT_RUN_ENVIRONMENT` | GO_DEPLOY |
| EV-04 | SBOM de fuente parcial `PASS_STATIC`; SCA NuGet/npm en red, transitorios, OS/image scanning, licencias y digests `NOT_RUN_ENVIRONMENT` | `PARTIAL_SOURCE_EVIDENCE` | GO_DEPLOY |
| EV-05 | Auth/IAM y rotación de credenciales del entorno | `NOT_RUN_ENVIRONMENT` | GO_DEPLOY/GO_LIVE |
| EV-06 | Regresión NACHA-M, idempotencia, ciclos, rechazo/devolución con data sintética | `NOT_RUN_ENVIRONMENT` | GO_DEPLOY |
| EV-07 | SQL Server/PostgreSQL, Quartz apagado, MFT/CFA simulados en entorno controlado | `NOT_RUN_ENVIRONMENT` | GO_DEPLOY |
| EV-08 | Aprobación de Seguridad, QA, Operaciones y homologación externa aplicable | `NOT_STARTED` | GO_LIVE_MONETARY |
| EV-09 | Verificar que imagen productiva y configuración desplegada corresponden a la aprobada | `NOT_RUN_ENVIRONMENT` | GO_LIVE_MONETARY |

| EV-10 | Completar y verificar 36 integrities npm contra registry aprobado, revisar lock | `NOT_RUN_ENVIRONMENT` | GO_DEPLOY |
| EV-11 | Obtener digests de las 6 imágenes y escanear capas/paquetes de SO | `NOT_RUN_ENVIRONMENT` | GO_DEPLOY |

**Sin ambiente:** EV-01 a EV-11 tienen validaciones ejecutables pendientes; no bloquear `GO_INCREMENT`. **Nunca liberar servicios de pago por la sola finalización del código.**

## 6. Próxima microtarea concreta

`SEC-I06-A_TESTS_SECURITY_REGRESSION` — Tests de autenticación, hardening y regresión financiera sintética.

- **DECISIÓN VINCULANTE DEL USUARIO (SEC-DEC-009): `PRESERVE_ADMIN_SEED_CREDENTIAL = TRUE`.** El usuario, contraseña literal, hash calculado y mecanismo `HasData` del seed administrativo permanecen **byte a byte** como en SEC-I03. No quitar ni cambiar en incrementos posteriores sin instrucción expresa.
- SEC-I04 introduce hashes nuevos PBKDF2-HMAC-SHA256 de 600.000 iteraciones, sal aleatoria de 16 bytes, clave derivada de 32 bytes, verificación constante y formato versionado; crea/cambia/restablece con PBKDF2. Login acepta SHA-256 legado **solo para verificación**. No se realiza rehash automático; para cuentas preexistentes ajenas al seed, modernización mediante restablecimiento/cambio controlado con autorización en entorno. Nunca modificar seed implícitamente.
- No se cambió esquema ni migraciones; columna `PasswordHash` de longitud 256 admite formato versionado. Ejecutar pruebas unitarias .NET y flujos de login/reset/CRUD en entorno autorizado. Credencial seed fija sigue siendo riesgo residual explícito: mínimo privilegio, control de acceso, monitorización y aceptación por propietario/riesgos antes de GO_DEPLOY.
- Mantener Quartz apagado, reglas ACH/CENIT intactas, `GO_INCREMENT=YES`, `GO_DEPLOY=NO`, `GO_LIVE_MONETARY=NO`.

| EV-12 | Validar documentación disabled, headers, proxy allowlist y rate limit con ingress real; Nginx 1.30.6 `nginx -t`; no filtrar secretos | `NOT_RUN_ENVIRONMENT` | GO_DEPLOY |

| EV-13 | SEC-I06A pruebas xUnit y matriz: ejecutar dotnet/CI aislado, capturar TRX y resultados de suite financiera | `WRITTEN_NOT_EXECUTED` / `NOT_RUN_ENVIRONMENT` | GO_DEPLOY |

## 7. Historial

| Fecha | Sesión / cambio | Archivos | Pruebas | Resultado |
|---|---|---|---|---|
| 2026-10-26 | `SEC-I00` registro de plan/memoria sobre I06-FIX01 | 2 docs nuevos + índice `MEMORY.md` | Inspección de ZIP y contenido; CRC/integridad de nuevo ZIP a validar al empaquetar. **Ninguna ejecución .NET/Docker/UAT** | `PLAN_DOCUMENTED` y `GO_INCREMENT=YES`, `GO_DEPLOY=NO`, `GO_LIVE_MONETARY=NO` |
| 2026-10-26 | `SEC-I01` parches .NET 10.0.14 / SDK 10.0.403 + 20 NuGet directos | global.json, Dockerfile API, 6 csproj, 2 evidencias, plan/memory/index | JSON/XML/versions/diff `PASS_STATIC`; dotnet/Docker/DB `NOT_RUN_ENVIRONMENT` | `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`, GO_INCREMENT=YES; SEC-I02 siguiente |
| 2026-10-27 | `SEC-I02` Node 24.22.1 / Nginx 1.30.6, build SPA inmutable | Dockerfile SPA, package.json, check script, evidencia/diff, plan/memory/index | JSON/manifest/verificador y npm10 dry-run `PASS_STATIC`; Node24/npm12, nginx runtime y Docker `NOT_RUN_ENVIRONMENT` | `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`, GO_INCREMENT=YES; SEC-I03 siguiente |


| 2026-10-27 | `SEC-I03` SBOM fuente y cadena de suministro | generador + tests + 2 SBOM parciales + inventario JSON + validación + runbook/plan/memory | 3/3 tests offline, 2 regeneraciones byte idénticas `PASS_STATIC`; SCA/OS/digests `NOT_RUN_ENVIRONMENT` | `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`; SEC-I04 siguiente |

**Última indicación del solicitante:** "No bloquee el incremento si no tenemos ambiente disponible" (se interpreta también para ausencia de herramientas de ejecución local; completar lo verificable y arrastrar lo pendiente). No permitir esta excepción para dar por autorizado un despliegue o una operación monetaria.

## 8. SEC-I04 — informe de cierre estático (2026-10-27)

- `src/Cfa.ACHInterbank.Application/Security/PasswordHashing/CredentialPasswordHasher.cs`: nuevo hasher PBKDF2-HMAC-SHA256 versionado + verificador de SHA-256 heredado.
- `AuthService.cs`: login reconoce ambos formatos; reset crea hash moderno. `UsersService.cs`: creación y actualización de contraseña usan hash moderno.
- `UserConfiguration.cs`: **sin modificación byte a byte** (incluido seed y su contraseña).
- Tests xUnit nuevos: `CredentialPasswordHasherTests.cs` (`WRITTEN_NOT_EXECUTED` sin SDK .NET). Sin auto-rehash de usuarios heredados ni cambios de BD.
- Validación estática y comparación de bytes en `SEC-I04_EVIDENCIA_ESTATICA.md`; no se ejecutaron restore/build/test/BD/UAT. Salida `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`.
- `SEC-DEC-009`: instrucción del solicitante prevalece sobre frases históricas de retiro/rotación del seed en SEC-I00. NO eliminar ni alterar contraseña inicial sin aprobación posterior. Riesgo residual pendiente de aceptación.


## 9. SEC-I05 — informe de cierre estático (2026-10-27)

- Documentación Scalar/OpenAPI deshabilitada por defecto fuera de Development; excepción opt-in `Documentation:EnableInNonDevelopment` requiere control perimetral extra (sigue anónima si se activa). Root/index redirige a `health/live` sin docs.
- Rate-limit usa socket de origen fuera de Development, excepto IP de proxy explícitamente confiable en `Security:TrustedReverseProxyIps`. En local Development se conserva comportamiento previo, pero Nginx ahora sanea todas las cabeceras `X-Forwarded-For` a `$remote_addr`. Si no se configura proxy confiable en producción con balanceador, el cupo de tasa se comparte; verificar en UAT.
- Middleware cabeceras sin HPKP obsoleto; HSTS solo para peticiones HTTPS no Development. Terminar TLS/HSTS en ingress seguro cuando API está detrás de HTTP privado. `Program.cs` oculta banner Kestrel. Nginx SPA: server_tokens off, headers, dotfiles no públicos, timeouts moderados, límite 50m preservado.
- Se escribe xUnit `RateLimitClientIdentitySecI05Tests.cs` y verificador offline (12/12 PASS_STATIC); Nginx 1.26.3 del host pasó prueba sintáctica **adaptada**, no versión objetivo. Compilación .NET, Docker, ingress, UAT, TLS y regresión financiera `NOT_RUN_ENVIRONMENT`.
- Se adjuntan `SEC-I05_EVIDENCIA_ESTATICA.md`, `SEC-I05_RUNBOOK_HARDENING.md` y `SEC-I05_VALIDACION_ESTATICA.json`. No se modificaron seed administrativo, Dockerfiles, Compose, Quartz ni reglas ACH/CENIT.
- **SIGUIENTE**: `SEC-I06-A_TESTS_SECURITY_REGRESSION`; `GO_INCREMENT=YES`, `GO_DEPLOY=NO`, `GO_LIVE_MONETARY=NO`. La ausencia de ambiente no bloquea el siguiente incremento.

## 10. SEC-I06A — preparación de pruebas (2026-10-27)

- **Alcance ejecutado:** 2 archivos nuevos de pruebas xUnit (`SecI06AHeadersRegressionTests`, `SecI06ACredentialBoundaryTests`); manifiesto JSON con 23 escenarios vinculados a métodos fuente existentes/nuevos; verificador offline y runbook de SEC-I06B.
- **Ejecución real:** únicamente validación offline Python sobre fuentes y ZIP. **Ningún test xUnit, `dotnet build`, Docker, SQL Server, PostgreSQL, UAT ni homologación ejecutados**: estados `WRITTEN_NOT_EXECUTED` y `NOT_RUN_ENVIRONMENT`.
- **Decisión SEC-DEC-009 respetada:** `UserConfiguration.cs` con seed administrativo idéntico en bytes. No se retiró ni cambió la credencial.
- **Cobertura de regresión FINANCIERA:** se preparó la trazabilidad de pruebas preexistentes de ACH Colombia 7 ciclos, CENIT 5 sesiones, NACHA-M, devoluciones, idempotencia y Quartz; la matriz NO certifica su resultado.
- **Split acordado:** SEC-I06A preparado offline; SEC-I06B pendiente de ambiente autorizado. Puede iniciarse SEC-I07 (plan de homologación) sin SEC-I06B completo, **nunca aprobar despliegue** sin evidencias.
- `GO_INCREMENT=YES`; `GO_DEPLOY=NO`; `GO_LIVE_MONETARY=NO`; Quartz ACH-CR-001 sigue apagado. `next_microtask=SEC-I07-A_PREP_HOMOLOGACION_SIN_AMBIENTE`.

