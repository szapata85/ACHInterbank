# Revision seguridad pre-go-live - ACH Interbank

Fecha de generacion: 2026-05-18
Version: 0.8 preliminar
Rama analizada: `fix/uat-operator-role-seed`
Estado: borrador tecnico para validacion humana
Clasificacion: no incluir secretos, datos personales, certificados privados ni evidencias sensibles en Git.

> Este documento consolida controles de seguridad pre-go-live con base en el inventario del repositorio. Donde no existe evidencia suficiente se marca como BRECHA, TODO, NO ENCONTRADO o PENDIENTE VALIDAR. No reemplaza una revision formal de seguridad, pentest, hardening de infraestructura ni validacion de proveedor/camara.

## 1. Resumen ejecutivo

- El proyecto puede avanzar hacia UAT controlado con datos anonimizados representativos, pero se mantiene NO-GO productivo hasta cerrar evidencias de seguridad, secretos, autorizacion y configuracion por ambiente.
- Se corrigio `AchResponsesController` con `[Authorize]` general explicito; queda pendiente validar politica granular endpoint-rol.
- Se corrigio configuracion productiva SPA para no apuntar a `localhost`; queda pendiente validar build/deploy UAT.
- Se identifico archivo `.env` versionado en la raiz del repositorio. BRECHA: confirmar si contiene secretos reales o valores reutilizables.
- Se reemplazaron defaults sensibles por placeholders locales/de demo en `docker-compose.yml`; queda pendiente validacion de seguridad/operaciones.
- Backend CI `dotnet-ci` y Angular CI `angular-ci` se reportan OK en GitHub Actions; adjuntar evidencia al paquete de release candidate.
- Docker runtime paso para API, PostgreSQL, health checks, Scalar/OpenAPI, SPA estatica y proxy SPA->API/Auth/Navigation por Nginx.
- NU1903 de `System.Security.Cryptography.Xml` 10.0.0 queda corregida en esta estabilizacion: la dependencia transitiva por `System.ServiceModel.*` se fija explicitamente en `10.0.8` en Application y `dotnet list ... --vulnerable --include-transitive` no reporta paquetes vulnerables.
- Rol `ACH.Operator`: cerrado para UAT controlado; `admin` queda asignado a `Admin` y `ACH.Operator` por seed/migracion controlada, y login/JWT sanitizados evidencian ambos roles.
- `docker-compose.yml` no requiere un servicio externo de secretos para iniciar API, PostgreSQL y SPA.
- Existen componentes de certificados, sobre digital, NACHA security, ROR y ACH responses que requieren validacion de autorizacion, auditoria, trazabilidad y pruebas E2E.
- Productivo no debe aprobarse sin acta UAT, evidencias firmables, aceptacion de riesgos y validacion de seguridad/operacion.

## 2. Evidencia tecnica revisada

| Evidencia | Ruta | Observacion |
|---|---|---|
| Controlador ACH responses | `src/Cfa.ACHInterbank.Api/Controllers/AchResponsesController.cs` | Corregido: `[Authorize]` general explicito; pendiente validar policy granular. |
| Configuracion SPA productiva | `web/ach-interbank-ui/src/environments/environment.prod.ts` | Corregido: `apiBaseUrl` relativo; pendiente validar reverse proxy/build UAT. |
| Compose principal | `docker-compose.yml` | Defaults sensibles reemplazados por placeholders; sin dependencia de un proveedor retirado. |
| Archivo de entorno versionado | `.env` | BRECHA: existe en repositorio; revisar contenido sin exponer valores. |
| API csproj | `src/Cfa.ACHInterbank.Api/Cfa.ACHInterbank.Api.csproj` | Corregido: `DocumentationFile` relativo MSBuild; pendiente build. |
| Controladores API | `src/Cfa.ACHInterbank.Api/Controllers` | Existen controladores funcionales, operativos y de interoperabilidad; requieren matriz de autorizacion. |
| Health checks | `/health/live`, `/health/ready` | Docker runtime 2026-05-18: ambos HTTP 200; ready confirma DB healthy. |
| Custodia de secretos | Configuración vigente | Requiere aprobación corporativa; no forma parte del stack Docker local. |
| Tests backend | `tests/Cfa.ACHInterbank.Tests` | Existen pruebas; no se ejecutaron en esta fase documental. |
| Tests Angular | `web/ach-interbank-ui/src/**/*.spec.ts`, `.github/workflows/angular-ci.yml` | Angular CI remoto reportado OK. |
| Docker runtime | `docs/go-live-readiness/DOCKER_RUNTIME_READINESS.md` | API/DB/SPA static OK; SPA->API/Auth/Navigation por puerto 743 validado tecnicamente. |
| PostgreSQL publicado al host | `docker-compose.yml` | `127.0.0.1:${POSTGRES_HOST_PORT:-5432}:5432` habilitado solo para UAT tecnico/local; no usar como patron productivo. |
| Dependencia criptografica vulnerable | `src/Cfa.ACHInterbank.Application/Cfa.ACHInterbank.Application.csproj` | Corregido: `System.Security.Cryptography.Xml` 10.0.8; sin paquetes vulnerables reportados por NuGet. |
| Rol demo `ACH.Operator` | `RoleConfiguration`, `UserRoleConfiguration`, migracion `AddAdminOperatorRoleSeed`, login sanitizado | Cerrado para UAT controlado: `admin` evidencia `Admin` y `ACH.Operator` en respuesta/JWT. |

## 3. Controllers sensibles

| Controller / area | Ruta | Sensibilidad | Estado seguridad | Observacion |
|---|---|---:|---|---|
| Auth | `src/Cfa.ACHInterbank.Api/Controllers/AuthController.cs` | Alta | PENDIENTE VALIDAR | Login, refresh, reset y flujos de identidad requieren rate limit, auditoria y bloqueo por intentos. |
| Users | `src/Cfa.ACHInterbank.Api/Controllers/UsersController.cs` | Alta | PENDIENTE VALIDAR | Administracion de usuarios/roles debe exigir autorizacion granular. |
| Transactions | `src/Cfa.ACHInterbank.Api/Controllers/TransactionsController.cs` | Alta | PENDIENTE VALIDAR | Operacion monetaria; validar autorizacion, idempotencia y auditoria. |
| Bulk ingestion | `src/Cfa.ACHInterbank.Api/Controllers/BulkIngestionController.cs` | Alta | PENDIENTE VALIDAR | Carga masiva requiere anti doble procesamiento, limites y evidencia de origen. |
| NACHA upload/export | `src/Cfa.ACHInterbank.Api/Controllers/NachaUploadController.cs`, `src/Cfa.ACHInterbank.Api/Controllers/NachaExportController.cs` | Alta | PENDIENTE VALIDAR | Archivos NACHA-M pueden contener datos sensibles; validar mascaramiento/logs. |
| ACH returns | `src/Cfa.ACHInterbank.Api/Controllers/AchReturnsController.cs` | Alta | PENDIENTE VALIDAR | Devoluciones afectan saldos/estado; requiere trazabilidad y autorizacion. |
| ROR | `src/Cfa.ACHInterbank.Api/Controllers/AchReturnOfReturnController.cs` | Alta | PENDIENTE VALIDAR | Return of Return requiere reglas normativas y evidencia E2E. |
| ACH responses | `src/Cfa.ACHInterbank.Api/Controllers/AchResponsesController.cs` | Alta | PARCIAL | `[Authorize]` general aplicado; falta validar si requiere policy granular por accion. |
| CENIT operations | `src/Cfa.ACHInterbank.Api/Controllers/CenitOperationsController.cs` | Alta | PENDIENTE VALIDAR | Ciclos, neteo, liquidez y CUD requieren control operativo y evidencias. |
| Certificates | `src/Cfa.ACHInterbank.Api/Controllers/CertificateManagementController.cs`, `src/Cfa.ACHInterbank.Api/Controllers/DigitalEnvelopeCertificatesController.cs` | Critica | PENDIENTE VALIDAR | No versionar certificados privados; validar almacenamiento seguro y rotacion. |
| NACHA security | `src/Cfa.ACHInterbank.Api/Controllers/NachaSecurityOperationsController.cs` | Alta | PENDIENTE VALIDAR | Firma/sobre digital requieren interoperabilidad y validacion externa. |
| Sobre digital | `src/Cfa.ACHInterbank.Api/Controllers/SobreDigitalController.cs` | Alta | PENDIENTE VALIDAR | Validar proveedor/camara, trazabilidad y no exposicion de material privado. |
| Maintenance | `src/Cfa.ACHInterbank.Api/Controllers/MaintenanceController.cs` | Media/Alta | PENDIENTE VALIDAR | Endpoints operativos pueden ser sensibles por reproceso o mantenimiento. |
| Reports | `src/Cfa.ACHInterbank.Api/Controllers/ReportsController.cs` | Media/Alta | PENDIENTE VALIDAR | Reportes pueden exponer datos personales o financieros; validar mascaramiento. |
| Traceability | `src/Cfa.ACHInterbank.Api/Controllers/AchTraceabilityController.cs` | Media/Alta | PENDIENTE VALIDAR | La trazabilidad no debe filtrar datos sensibles en claro. |

## 4. Endpoints sin autorizacion explicita o con excepcion

| Area | Evidencia | Estado | Riesgo | Accion |
|---|---|---|---|---|
| ACH responses | `src/Cfa.ACHInterbank.Api/Controllers/AchResponsesController.cs` | PARCIAL | Riesgo residual si no se define policy granular por endpoint/rol. | Validar politica endpoint-rol y endurecer con policies existentes si negocio/seguridad lo exige. |
| Branding | `src/Cfa.ACHInterbank.Api/Controllers/BrandingController.cs` | PENDIENTE VALIDAR | Puede ser publico si solo entrega marca; confirmar que no expone datos internos. | Documentar excepcion o restringir si no es publico. |
| Auth public endpoints | `src/Cfa.ACHInterbank.Api/Controllers/AuthController.cs` | PENDIENTE VALIDAR | Login/refresh/reset requieren protecciones anti abuso. | Validar rate limit, bloqueo, auditoria y logs sin secretos. |
| OpenAPI/Scalar | Configuracion API | PENDIENTE VALIDAR | Swagger/Scalar expuesto en ambiente no controlado puede facilitar enumeracion. | Restringir por ambiente o red; documentar excepcion UAT. |
| Health checks | `/health/live`, `/health/ready` | PENDIENTE VALIDAR | Health con exceso de detalle puede filtrar infraestructura. | Mantener respuesta minima y validar autenticacion/red para detalles. |
| JWKS/OAuth/Test controllers | Archivos bajo `src/Cfa.ACHInterbank.Api/Controllers` y/o exclusiones | PENDIENTE VALIDAR | Endpoints de prueba o llaves pueden ser sensibles si compilan en productivo. | Confirmar inclusion/exclusion efectiva por csproj y ambiente. |

## 5. Riesgos especificos

### 5.1 `.env` versionado

Estado: BRECHA.
Evidencia: existe `.env` en la raiz del repositorio.
Riesgo: exposicion accidental de credenciales, cadenas de conexion, secretos de proveedor, configuracion de ambiente o datos sensibles.
Accion recomendada: revisar contenido en canal seguro, rotar valores si hubo exposicion, dejar solo `.env.example` sin secretos y documentar proceso de provisionamiento.

### 5.2 Credenciales por defecto o de ejemplo

Estado: PARCIAL.
Evidencia: `docker-compose.yml` contiene placeholders locales/de demo para servicios de infraestructura.
Riesgo: reutilizacion accidental en UAT/preproductivo/productivo.
Accion recomendada: parametrizar por variables externas, usar vault/secrets provider y prohibir valores productivos en Git.

### 5.3 SPA productiva apuntando a localhost

Estado: CORREGIDO TECNICAMENTE / PENDIENTE VALIDAR UAT.
Evidencia: `web/ach-interbank-ui/src/environments/environment.prod.ts`.
Riesgo residual: el reverse proxy o pipeline UAT debe mantener las rutas relativas correctamente. En Docker runtime 2026-05-18, `http://localhost:743/api/ach/responses`, `POST http://localhost:743/auth/login` y `http://localhost:743/navigation/menu` respondieron desde API, no `index.html`, confirmando proxy y autorizacion intacta.
Accion recomendada: ejecutar UAT tecnico funcional con usuarios/roles y datos anonimizados; no deshabilitar autorizacion para obtener 200 en endpoints protegidos.

### 5.4 Certificados privados

Estado: PENDIENTE VALIDAR.
Evidencia: existen componentes y docs relacionados con certificados/sobre digital.
Riesgo: filtracion de llaves privadas, uso de certificados vencidos o no homologados.
Accion recomendada: almacenar certificados privados fuera de Git, usar referencias seguras, validar rotacion, vencimiento y custodia.

### 5.5 Custodia de secretos

Estado: PENDIENTE DE APROBACION CORPORATIVA.
Evidencia: el stack Docker local no depende de un servicio externo de secretos.
Riesgo: custodia no aprobada o diferencias entre ambiente local y preproductivo.
Accion recomendada: documentar el mecanismo corporativo vigente y exigir fail-closed para flujos sensibles.

### 5.6 TLS/HTTPS

Estado: PENDIENTE VALIDAR.
Evidencia: no se reviso infraestructura de reverse proxy/certificados TLS en esta fase.
Riesgo: transporte no cifrado o certificados no confiables.
Accion recomendada: documentar terminacion TLS, dominios, certificados, renovacion, cipher suites y prueba de conectividad.

### 5.7 Logs con datos sensibles

Estado: PENDIENTE VALIDAR.
Evidencia: existen trazabilidad, auditoria y reportes.
Riesgo: logs con cuentas, documentos, tokens, llaves o payloads completos.
Accion recomendada: aplicar mascaramiento, clasificacion de campos sensibles y retencion autorizada.

### 5.8 Auditoria e idempotencia

Estado: PARCIAL.
Evidencia: existen componentes de trazabilidad/idempotencia; falta evidencia UAT firmable.
Riesgo: reprocesos, doble afectacion o imposibilidad de explicar eventos.
Accion recomendada: ejecutar escenarios UAT de idempotencia, reproceso, eventos y conciliacion con evidencia.

### 5.9 NU1903 `System.Security.Cryptography.Xml`

Estado: CORREGIDO TECNICAMENTE.
Evidencia: `dotnet list ACHInterbank.sln package --vulnerable --include-transitive` reporto inicialmente `System.Security.Cryptography.Xml` 10.0.0 como transitiva de `System.ServiceModel.*`; se agrego referencia explicita a `System.Security.Cryptography.Xml` 10.0.8 en `Cfa.ACHInterbank.Application.csproj`.
Validacion: `dotnet restore`, `dotnet list ... --vulnerable`, build y tests backend quedan OK.
Riesgo residual: mantener monitoreo de advisories y evitar introducir previews o upgrades mayores no controlados.

### 5.10 Rol `ACH.Operator`

Estado: CERRADO PARA UAT CONTROLADO.
Evidencia: `RoleConfiguration` define `ACH.Operator`; `UserRoleConfiguration` asigna al usuario seed `admin` los roles `Admin` y `ACH.Operator`; la migracion `AddAdminOperatorRoleSeed` inserta la relacion faltante en bases existentes; login sanitizado confirma roles respuesta/JWT = `Admin,ACH.Operator`.
Validacion: `dotnet build ACHInterbank.sln -c Release` OK; suite backend completa OK; `UserRoleSeedTests` OK; runtime Docker API reconstruido y `/navigation/menu`, `/api/roles`, `/api/users`, `/api/ach/responses` responden 200 con Bearer.
Riesgo residual: `admin` queda como usuario demo multirol para UAT controlado. Antes de preproductivo/productivo, seguridad puede exigir usuario operador separado y matriz endpoint-rol firmada.

## 6. Tabla de controles pre-go-live

| Control | Evidencia | Estado | Riesgo | Accion recomendada | Bloquea productivo |
|---|---|---|---|---|---|
| Autorizacion explicita en controllers sensibles | `src/Cfa.ACHInterbank.Api/Controllers` | PARCIAL | Acceso indebido a flujos ACH. | Crear matriz de autorizacion por endpoint y rol; revisar atributos/politicas. | Si |
| `AchResponsesController` protegido | `src/Cfa.ACHInterbank.Api/Controllers/AchResponsesController.cs` | PARCIAL | Exposicion o manipulacion si falta policy granular. | `[Authorize]` aplicado; validar policy endpoint-rol. | Si |
| SPA productiva sin localhost | `web/ach-interbank-ui/src/environments/environment.prod.ts` | PARCIAL | Reverse proxy/pipeline mal configurado. | Base relativa aplicada; evidenciar build UAT/preprod. | Si |
| `.env` sin secretos reales versionados | `.env` | CRITICO | Fuga de secretos. | Revisar contenido, rotar si aplica, reemplazar por `.env.example`. | Si aplica |
| Credenciales por defecto fuera de compose | `docker-compose.yml` | PARCIAL | Uso accidental de placeholders en UAT/preprod. | Validar variables no versionadas o secret manager. | Si aplica |
| Custodia corporativa de secretos aprobada | Configuración vigente | PENDIENTE | Diferencia entre UAT y prod o fallback inseguro. | Aprobar mecanismo y runbook corporativo. | Si aplica |
| Certificados privados fuera de Git | Componentes de certificados/sobre digital | PENDIENTE VALIDAR | Compromiso criptografico. | Usar vault/HSM/repositorio seguro, referencias y hashes. | Si |
| Validacion de firma/sobre digital externa | Controladores de sobre digital/NACHA security | PENDIENTE VALIDAR | Rechazo por camara/proveedor. | Ejecutar homologacion y guardar evidencias fuera de Git. | Si aplica |
| TLS/HTTPS definido | Documentacion de despliegue | NO ENCONTRADO | Transporte inseguro. | Documentar dominios, certificados y reverse proxy. | Si |
| Logs sin datos sensibles | Logging/trazabilidad | PENDIENTE VALIDAR | Exposicion de PII/datos financieros. | Revisar payloads, aplicar masking y retencion. | Si aplica |
| Auditoria operativa | Componentes de auditoria/trazabilidad | PARCIAL | Falta de no repudio operacional. | Ejecutar escenarios UAT y evidenciar eventos. | Si |
| Idempotencia comprobada | Componentes y pruebas | PARCIAL | Doble procesamiento. | Ejecutar pruebas UAT de reintento, duplicado y reproceso. | Si |
| Health checks aptos para UAT/prod | `/health/live`, `/health/ready` | PARCIAL | Monitoreo insuficiente o filtracion de detalle. | Validar respuestas, dependencias y seguridad de detalles. | Si aplica |
| OpenAPI/Scalar restringido por ambiente | Configuracion API | PENDIENTE VALIDAR | Enumeracion de API en productivo. | Restringir por ambiente/red/rol o documentar excepcion. | Si aplica |
| Roles operativos definidos | Auth/Users y SPA | OK UAT / PARCIAL PRODUCTIVO | Usuario demo evidencia `Admin` y `ACH.Operator`; falta matriz endpoint-rol productiva formal. | Mantener evidencia UAT y definir usuario operador separado si seguridad lo exige para preproductivo/productivo. | Si para productivo formal |
| Backup/restore/rollback evidenciado | Documentacion operativa | NO ENCONTRADO | Recuperacion incierta. | Ejecutar y documentar simulacro. | Si |
| CI backend | `.github/workflows/dotnet-ci.yml`, commit `49b810f9` | OK CI / OK LOCAL | Build y suite backend local pasan tras estabilizacion. | Adjuntar evidencia CI/local al paquete release candidate. | Si para release candidate |
| CI Angular | `.github/workflows/angular-ci.yml` | OK CI RAMA / OK LOCAL | Ultimo `angular-ci` de rama OK; localmente `npm run build` y 147 specs pasan. | Adjuntar evidencia y vigilar warnings no bloqueantes. | Si para release candidate |

## 7. Recomendaciones previas a productivo

1. Cerrar validacion residual G-05: definir policy granular para `AchResponsesController` si seguridad lo exige.
2. Cerrar validacion residual G-07: evidenciar build/deploy con reverse proxy o URL UAT aprobada.
3. Revisar `.env` versionado y rotar cualquier secreto si hubo exposicion.
4. Parametrizar credenciales de infraestructura y prohibir defaults sensibles en despliegues compartidos.
5. Aprobar la custodia corporativa para UAT/preproductivo y validar comportamiento fail-closed en flujos sensibles.
6. Crear matriz endpoint -> rol -> politica -> evidencia UAT.
7. Validar que endpoints OpenAPI/Scalar/health no expongan informacion sensible en productivo.
8. Ejecutar pruebas de idempotencia, devoluciones, ROR, CENIT, conciliacion, firma y sobre digital con evidencia.
9. Validar certificados, cadena de confianza, vencimientos, rotacion y custodia fuera de Git.
10. Formalizar aceptacion de riesgos remanentes antes de cualquier decision GO.

## 8. Pendientes de validacion humana

| ID | Pendiente | Responsable sugerido | Resultado esperado |
|---|---|---|---|
| SEC-TODO-001 | Confirmar si `[Authorize]` general en `AchResponsesController` es suficiente o requiere policy granular. | Tecnologia / Seguridad | Evidencia de proteccion o cambio aprobado. |
| SEC-TODO-002 | Revisar contenido de `.env` sin exponer valores en documentos. | Seguridad / Operaciones | Decision de rotacion/saneamiento. |
| SEC-TODO-003 | Confirmar el mecanismo corporativo vigente en UAT/preproductivo. | Operaciones / Seguridad | Runbook validado y evidencia. |
| SEC-TODO-004 | Validar que certificados privados no esten versionados. | Seguridad | Inventario y custodia aprobada. |
| SEC-TODO-005 | Revisar logs por PII/datos financieros. | Auditoria / Seguridad | Matriz de campos sensibles y masking. |
| SEC-TODO-006 | Validar TLS/reverse proxy productivo. | Infraestructura | Evidencia de HTTPS y certificados. |
| SEC-TODO-007 | Revisar endpoints consumidos por SPA sin backend equivalente. | Tecnologia | Cierre de brecha funcional/seguridad. |
