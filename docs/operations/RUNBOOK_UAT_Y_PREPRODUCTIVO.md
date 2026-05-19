# Runbook UAT y preproductivo - ACH Interbank

Fecha de generacion: 2026-05-18  
Version: 0.1 preliminar  
Rama analizada: ACH-Interbank-Postgresql  
Estado: borrador operativo para validacion humana  
Clasificacion: no incluir secretos, datos personales, certificados privados ni evidencias sensibles en Git.

> Este runbook documenta procedimientos sugeridos para preparar, validar y operar ambiente UAT/preproductivo. No ejecuta comandos ni reemplaza los runbooks oficiales de infraestructura, seguridad, proveedor/camara o base de datos. Cualquier valor sensible debe suministrarse por canal seguro, vault o mecanismo autorizado.

## 1. Alcance

Este documento cubre:

- Preparacion del ambiente UAT/preproductivo.
- Verificacion de PostgreSQL, API, SPA y health checks.
- Verificacion documental de migraciones, seeds, OpenBao y certificados.
- Procedimientos operativos UAT para NACHA-M, devoluciones, ROR, CENIT, conciliacion, evidencias, defectos, rollback y cierre.

Fuera de alcance:

- Ejecucion real de migraciones.
- Ejecucion de comandos destructivos.
- Provisionamiento de secretos reales.
- Custodia de certificados privados.
- Homologacion oficial con camara/proveedor.

## 2. Rutas reales relevantes

| Elemento | Ruta | Uso operativo |
|---|---|---|
| Solucion .NET | `ACHInterbank.sln` | Restore, build y test backend. |
| API | `src/Cfa.ACHInterbank.Api` | Servicio HTTP principal. |
| Application | `src/Cfa.ACHInterbank.Application` | Casos de uso, DTOs, validaciones. |
| Domain | `src/Cfa.ACHInterbank.Domain` | Entidades, reglas y contratos de dominio. |
| Persistence | `src/Cfa.ACHInterbank.Persistence` | DbContext, EF Core, migraciones y seeds. |
| External | `src/Cfa.ACHInterbank.External` | Integraciones/servicios externos. |
| Tests backend | `tests/Cfa.ACHInterbank.Tests` | Pruebas automatizadas .NET. |
| SPA Angular | `web/ach-interbank-ui` | Frontend UAT. |
| Compose principal | `docker-compose.yml` | Stack local/base; revisar secretos antes de usar en UAT. |
| Compose test | `docker-compose.test.yml` | PostgreSQL de test segun `AGENTS.md`. |
| Setup Codex | `scripts/codex/setup-codex-env.sh` | Preparacion reproducible local. |
| OpenBao scripts | `scripts/openbao` | Soporte OpenBao; validar alcance UAT. |
| Docs UAT | `docs/uat` | Plan, escenarios, datos, acta, evidencias y defectos. |
| Docs go-live | `docs/go-live-readiness` | Checklist, scorecard, brechas, matriz y paquete comite. |
| Docs seguridad | `docs/security` | Revision pre-go-live. |

## 3. Variables requeridas

No registrar valores reales en Git. Documentar solo nombres, origen autorizado y responsable.

| Variable/configuracion | Uso | Origen esperado | Estado |
|---|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | Ambiente API | Pipeline/host UAT | PENDIENTE VALIDAR |
| `ASPNETCORE_URLS` | URLs de exposicion API | Pipeline/host UAT | PENDIENTE VALIDAR |
| `Database__Provider` | Provider de base de datos | Configuracion ambiente | PENDIENTE VALIDAR |
| `ConnectionStrings__PostgresConnection` | Conexion PostgreSQL | Secret manager/vault | PENDIENTE VALIDAR |
| `ConnectionStrings__DefaultConnection` | Conexion alternativa si aplica | Secret manager/vault | PENDIENTE VALIDAR |
| `Cors__AllowedOrigins` | Origenes permitidos SPA | Configuracion ambiente | PENDIENTE VALIDAR |
| `Jwt__Issuer` | Emisor JWT | Secret/config UAT | PENDIENTE VALIDAR |
| `Jwt__Audience` | Audiencia JWT | Secret/config UAT | PENDIENTE VALIDAR |
| `Jwt__SigningKey` o referencia equivalente | Firma token | Secret manager/vault | PENDIENTE VALIDAR |
| `DigitalEnvelope__*` | Sobre digital/firma/certificados | Secret manager/vault | PENDIENTE VALIDAR |
| `OpenBao__*` o equivalente | Secrets provider | OpenBao/vault | PENDIENTE VALIDAR |
| `apiBaseUrl` SPA | URL API consumida por SPA | Build/runtime config | CORREGIDO TECNICAMENTE: base relativa; PENDIENTE VALIDAR reverse proxy/deploy |

## 4. Preparacion de ambiente

### 4.1 Checklist previo

| ID | Control | Estado esperado | Evidencia |
|---|---|---|---|
| RUN-PRE-001 | Rama correcta desplegada | `ACH-Interbank-Postgresql` o release aprobado | Commit/tag registrado en acta UAT. |
| RUN-PRE-002 | Build backend generado | Artefacto versionado | Resultado de build en indice de evidencias. |
| RUN-PRE-003 | Build SPA generado | Artefacto versionado | Resultado de build en indice de evidencias. |
| RUN-PRE-004 | Variables UAT provisionadas | Sin secretos en Git | Registro seguro externo. |
| RUN-PRE-005 | PostgreSQL disponible | Instancia UAT/preprod | Health/connection check. |
| RUN-PRE-006 | Migraciones revisadas | EF Core Code First | Resultado de aplicacion controlada por responsable DB. |
| RUN-PRE-007 | Seeds/datos maestros cargados | Camaras, ciclos, causales, roles | Evidencia de tablas/catalogos. |
| RUN-PRE-008 | Certificados disponibles | Solo referencias seguras | Evidencia de custodia. |
| RUN-PRE-009 | OpenBao definido | Aplica/no aplica formal | Evidencia de decision. |
| RUN-PRE-010 | UAT plan aprobado para iniciar | Criterios de inicio OK | Aprobacion de Tecnologia/Negocio. |

### 4.2 Comandos sugeridos no destructivos

Estos comandos son sugeridos para ejecucion controlada por responsables del ambiente. No se ejecutaron en esta fase documental.

```powershell
dotnet restore ACHInterbank.sln
dotnet build ACHInterbank.sln -c Release
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release
```

```powershell
cd web/ach-interbank-ui
npm ci
npm run build
npm test -- --watch=false --browsers=ChromeHeadless
```

```powershell
docker compose -f docker-compose.test.yml --env-file .env.test.example up -d
docker compose ps
docker compose logs --tail=200
```

### 4.3 Evidencia runtime Docker 2026-05-18

| Control | Resultado | Evidencia |
|---|---|---|
| Docker compose config | OK | `docker compose config --quiet`. |
| Docker build | OK | `docker compose build`. |
| Docker runtime | OK tecnico | `docker compose up -d`; `postgres`, `achinterbank-api` y `achinterbank-spa` quedaron `Up`. |
| PostgreSQL | OK | Contenedor `achinterbank-postgres` `healthy`; esquema con 130 tablas. |
| API live | OK | `http://localhost:843/health/live` HTTP 200. |
| API ready | OK | `http://localhost:843/health/ready` HTTP 200 con DB healthy. |
| SPA estatica | OK | `http://localhost:743` HTTP 200. |
| SPA hacia API | OK tecnico | `http://localhost:743/api/ach/responses` devolvio 401 desde API, no `index.html`. |
| Scalar | OK | `http://localhost:843/scalar` HTTP 200. |
| OpenAPI | OK con observacion | `http://localhost:843/openapi/v1.json` HTTP 200 con timeout ampliado; aprox. 79s directo y 96s via proxy. |
| OpenBao | PENDIENTE / NO APLICA compose actual | No existe servicio OpenBao en `docker-compose.yml`. |

Documentos de evidencia:

- `docs/uat/EVIDENCIA_TECNICA_UAT_RUNTIME.md`
- `docs/go-live-readiness/DOCKER_RUNTIME_READINESS.md`

Decision operativa: el stack es valido para pruebas tecnicas directas de API, BD, health, SPA estatica y proxy SPA->API. Puede iniciar UAT tecnico E2E basico desde SPA con datos anonimizados, usuarios/roles y registro formal de evidencias.

## 5. Verificacion PostgreSQL

| Paso | Accion | Resultado esperado | Evidencia |
|---|---|---|---|
| PG-001 | Confirmar servicio PostgreSQL accesible desde API. | Conexion exitosa con credenciales UAT. | Log de arranque sin secretos. |
| PG-002 | Confirmar version y parametros basicos. | Version aprobada por arquitectura/DBA. | Captura o salida sanitizada. |
| PG-003 | Confirmar base de datos y esquema. | Esquema creado por EF Core. | Evidencia DBA. |
| PG-004 | Confirmar migraciones aplicadas. | Ultima migracion aprobada. | Tabla de historial EF o reporte DBA. |
| PG-005 | Confirmar datos maestros minimos. | Camaras, ciclos, causales, roles y parametros. | Captura/consulta sanitizada. |
| PG-006 | Confirmar backup pre-UAT. | Backup disponible. | ID de backup en repositorio seguro externo. |

Comando EF recomendado solo para ejecucion controlada:

```powershell
dotnet ef database update `
  --project src/Cfa.ACHInterbank.Persistence/Cfa.ACHInterbank.Persistence.csproj `
  --startup-project src/Cfa.ACHInterbank.Api/Cfa.ACHInterbank.Api.csproj `
  --context AchDbContext
```

BRECHA: no ejecutar migraciones sin aprobacion DBA/operaciones, backup previo y ventana autorizada.

## 6. Verificacion API

| Paso | Accion | Resultado esperado | Evidencia |
|---|---|---|---|
| API-001 | Iniciar API en ambiente UAT. | Servicio arriba sin errores criticos. | Log de arranque sanitizado. |
| API-002 | Validar configuracion de ambiente. | No usa valores locales ni defaults sensibles. | Checklist configuracion. |
| API-003 | Validar Swagger/Scalar segun ambiente. | Disponible solo si la politica UAT lo permite. | Captura o decision formal. |
| API-004 | Validar CORS. | Solo origen SPA UAT autorizado. | Configuracion sanitizada. |
| API-005 | Validar autenticacion. | Login funcional y tokens protegidos. | Evidencia UAT-REAL-001. |
| API-006 | Validar autorizacion. | Roles restringen acciones. | Evidencia UAT-REAL-002. |
| API-007 | Revisar `AchResponsesController`. | `[Authorize]` general aplicado y policy granular decidida. | Evidencia de seguridad. |

## 7. Verificacion SPA

| Paso | Accion | Resultado esperado | Evidencia |
|---|---|---|---|
| SPA-001 | Construir SPA para UAT/preprod. | Build exitoso. | Resultado `npm run build`. |
| SPA-002 | Validar URL API. | No apunta a `localhost` en build productivo/UAT. | Captura/config sanitizada. |
| SPA-003 | Validar login. | Usuario UAT accede segun rol. | Captura SPA sin datos sensibles. |
| SPA-004 | Validar pantallas operativas. | Transacciones, NACHA, devoluciones, ROR, CENIT si aplica. | Evidencias por escenario. |
| SPA-005 | Validar errores. | Mensajes claros, sin stack trace ni secretos. | Capturas sanitizadas. |
| SPA-006 | Validar endpoints consumidos. | Cada consumo tiene backend equivalente o brecha registrada. | Matriz SPA-backend. |

Estado: `web/ach-interbank-ui/src/environments/environment.prod.ts` usa base relativa y ya no apunta a `localhost`. Validar que el reverse proxy UAT/preproductivo enrute correctamente API y SPA.

## 8. Verificacion health checks

| Endpoint | Resultado esperado | Estado |
|---|---|---|
| `/health/live` | Servicio vivo con respuesta minima. | PENDIENTE VALIDAR |
| `/health/ready` | Dependencias listas, incluida base de datos si aplica. | PENDIENTE VALIDAR |
| OpenAPI/Scalar | Disponible solo por politica de ambiente. | PENDIENTE VALIDAR |

Comandos sugeridos:

```powershell
curl -k https://<api-uat>/health/live
curl -k https://<api-uat>/health/ready
```

No registrar payloads con secretos o detalles internos excesivos en Git.

## 9. Verificacion migraciones

| Control | Resultado esperado | Evidencia |
|---|---|---|
| Fuente de verdad | EF Core Code First | `src/Cfa.ACHInterbank.Persistence` |
| Contexto | `AchDbContext` | Comando EF validado por DBA |
| Historial | Migraciones aplicadas en orden | Reporte sanitizado |
| Rollback | Procedimiento aprobado antes de UAT | Documento operativo externo |
| Backup previo | Backup tomado antes de cambio | ID backup fuera de Git |

BRECHA: el rollback tecnico debe estar probado o aceptado formalmente antes de productivo.

## 10. Verificacion seeds y datos maestros

| Dato maestro | Validacion | Estado |
|---|---|---|
| Camaras ACH Colombia/CENIT/STA | Catalogo cargado y vigente | PENDIENTE VALIDAR |
| Ciclos | Horarios/ventanas aprobados | PENDIENTE VALIDAR |
| Causales | Por camara y flujo | PENDIENTE VALIDAR |
| Estados | Consistentes SPA/backend | PENDIENTE VALIDAR |
| Roles | Tecnologia, operaciones, negocio, auditoria | PENDIENTE VALIDAR |
| Parametros NACHA-M | Layouts/reglas validadas | PENDIENTE VALIDAR |
| CENIT CUD | Parametros y evidencia | BRECHA |

## 11. Verificacion OpenBao

| Paso | Accion | Resultado esperado | Estado |
|---|---|---|---|
| OB-001 | Confirmar si OpenBao aplica a UAT. | Decision documentada. | PENDIENTE VALIDAR |
| OB-002 | Confirmar servicio disponible. | Endpoint interno accesible. | PENDIENTE VALIDAR |
| OB-003 | Confirmar politicas y paths. | Minimo privilegio. | PENDIENTE VALIDAR |
| OB-004 | Confirmar rotacion/revocacion. | Procedimiento definido. | PENDIENTE VALIDAR |
| OB-005 | Confirmar comportamiento ante caida. | Fail-closed en flujos sensibles si aplica. | PENDIENTE VALIDAR |

Evidencia: `scripts/openbao`, `ops/openbao`, `docs/architecture/openbao-integration-2026-04-22.md` y `docs/dev/docker-compose-openbao-uat-2026-04-22.md` existen; `docker-compose.yml` principal no evidencia servicio OpenBao. No agregar OpenBao al compose principal sin decision operativa.

## 12. Verificacion certificados

| Control | Resultado esperado | Estado |
|---|---|---|
| Certificados privados fuera de Git | Solo referencias o hashes | PENDIENTE VALIDAR |
| Certificados publicos controlados | Validez y cadena aprobadas | PENDIENTE VALIDAR |
| Vencimiento | Fechas registradas fuera de Git si sensibles | PENDIENTE VALIDAR |
| Rotacion | Procedimiento definido | PENDIENTE VALIDAR |
| Firma | Validada E2E | PENDIENTE VALIDAR |
| Sobre digital | Validado con proveedor/camara si aplica | BRECHA |

## 13. Procedimiento carga NACHA-M

| Paso | Accion | Evidencia |
|---|---|---|
| NACHA-001 | Preparar archivo NACHA-M anonimizado representativo. | Hash SHA256 y ubicacion segura externa. |
| NACHA-002 | Validar registros tipo 1, 5, 6, 7, 8 y 9. | Reporte de validacion. |
| NACHA-003 | Cargar/generar archivo por flujo UAT aprobado. | Captura SPA o request/response sanitizado. |
| NACHA-004 | Confirmar lote/transacciones creadas. | Registro BD sanitizado o reporte. |
| NACHA-005 | Confirmar totales, hash, block count y controles. | Evidencia tecnica. |
| NACHA-006 | Registrar resultado en `docs/uat/INDICE_EVIDENCIAS_UAT.md`. | ID evidencia. |

BRECHA: no guardar archivo real con datos sensibles dentro del repositorio.

## 14. Procedimiento devolucion

| Paso | Accion | Evidencia |
|---|---|---|
| DEV-001 | Seleccionar transaccion UAT elegible. | ID anonimizado. |
| DEV-002 | Ejecutar devolucion de salida o entrada segun escenario. | Captura/request/response. |
| DEV-003 | Validar causal por camara y flujo. | Matriz de causales. |
| DEV-004 | Validar estado final. | SPA + BD sanitizada. |
| DEV-005 | Validar auditoria/trazabilidad. | Evento de auditoria. |
| DEV-006 | Validar conciliacion si aplica. | Reporte conciliacion. |

Escenarios vinculados: `UAT-REAL-009`, `UAT-REAL-010`, `UAT-REAL-011`, `UAT-REAL-013`.

## 15. Procedimiento ROR

| Paso | Accion | Evidencia |
|---|---|---|
| ROR-001 | Identificar devolucion base elegible. | ID anonimizado. |
| ROR-002 | Ejecutar Return of Return. | Captura/request/response. |
| ROR-003 | Validar regla de causal y estado. | Evidencia funcional. |
| ROR-004 | Validar trazabilidad e idempotencia. | Logs/eventos sanitizados. |
| ROR-005 | Validar conciliacion. | Reporte conciliacion. |

Escenario vinculado: `UAT-REAL-014`.

## 16. Procedimiento CENIT

| Paso | Accion | Evidencia |
|---|---|---|
| CENIT-001 | Confirmar si CENIT esta dentro del alcance UAT. | Decision formal. |
| CENIT-002 | Validar ciclos configurados. | Catalogo/parametro. |
| CENIT-003 | Ejecutar escenario de neteo. | Reporte neteo. |
| CENIT-004 | Ejecutar escenario de liquidez. | Reporte liquidez. |
| CENIT-005 | Ejecutar escenario CUD. | Evidencia CUD segura. |
| CENIT-006 | Validar conciliacion y cierre. | Reporte final. |

BRECHA: CENIT neteo/liquidez/CUD sin evidencia E2E homologada bloquea productivo si aplica al alcance.

## 17. Procedimiento conciliacion

| Paso | Accion | Evidencia |
|---|---|---|
| CON-001 | Seleccionar rango/ciclo UAT. | Parametros anonimizados. |
| CON-002 | Ejecutar conciliacion. | Reporte generado. |
| CON-003 | Comparar transacciones, devoluciones, rechazos y ROR. | Matriz de diferencias. |
| CON-004 | Resolver diferencias o registrar defecto. | Defecto/evidencia. |
| CON-005 | Aprobar conciliacion del ciclo. | Firma Operaciones/Negocio. |

## 18. Procedimiento de evidencias

| Paso | Accion | Regla |
|---|---|---|
| EVI-001 | Crear ID de evidencia. | Usar `EVI-UAT-###`. |
| EVI-002 | Guardar evidencia sensible fuera de Git. | Repositorio seguro externo. |
| EVI-003 | Calcular hash SHA256 si aplica. | Registrar hash, no contenido sensible. |
| EVI-004 | Registrar referencia en `docs/uat/INDICE_EVIDENCIAS_UAT.md`. | Ruta o referencia segura. |
| EVI-005 | Asociar evidencia a escenario y defecto. | Trazabilidad UAT. |
| EVI-006 | Obtener aprobacion del responsable. | Acta/correo/aprobacion formal. |

## 19. Procedimiento de defectos

| Paso | Accion | Regla |
|---|---|---|
| DEF-001 | Registrar defecto en `docs/uat/MATRIZ_DEFECTOS_UAT.md`. | No incluir datos sensibles. |
| DEF-002 | Clasificar severidad. | Bloqueante, Alta, Media, Baja. |
| DEF-003 | Asociar escenario y evidencia. | ID escenario + ID evidencia. |
| DEF-004 | Definir responsable. | Tecnologia/Operaciones/Negocio/Seguridad. |
| DEF-005 | Definir decision. | Corregir, diferir, rechazar, aceptar riesgo. |
| DEF-006 | Cerrar con evidencia. | Reejecucion o aceptacion formal. |

## 20. Procedimiento rollback

| Paso | Accion | Estado |
|---|---|---|
| RB-001 | Confirmar backup previo de BD. | PENDIENTE VALIDAR |
| RB-002 | Confirmar version anterior API/SPA disponible. | PENDIENTE VALIDAR |
| RB-003 | Confirmar estrategia de rollback de migraciones. | BRECHA |
| RB-004 | Confirmar responsables y ventana. | PENDIENTE VALIDAR |
| RB-005 | Ejecutar simulacro o aceptar riesgo formal. | PENDIENTE VALIDAR |
| RB-006 | Documentar resultado en evidencias. | PENDIENTE VALIDAR |

No ejecutar rollback sin aprobacion formal. No borrar volumenes como mecanismo normal de rollback.

## 21. Procedimiento cierre operativo

| Paso | Accion | Criterio |
|---|---|---|
| CIERRE-001 | Confirmar escenarios ejecutados. | 100 % de escenarios del alcance con estado. |
| CIERRE-002 | Confirmar defectos bloqueantes/altos. | Cerrados o aceptados formalmente. |
| CIERRE-003 | Confirmar evidencias. | Indice completo con hashes/referencias. |
| CIERRE-004 | Confirmar conciliacion. | Sin diferencias no explicadas. |
| CIERRE-005 | Confirmar seguridad. | Brechas criticas cerradas o aceptadas. |
| CIERRE-006 | Firmar acta UAT. | Negocio, Operaciones, Tecnologia, Seguridad, Auditoria. |
| CIERRE-007 | Preparar comite GO/NO-GO. | Paquete documental actualizado. |

## 22. Escalamiento

| Tipo de incidente | Responsable primario | Escalamiento | Criterio de suspension |
|---|---|---|---|
| Caida API/BD | Tecnologia / Operaciones | Infraestructura / DBA | Bloquea escenarios core. |
| Error seguridad | Seguridad | Comite de riesgo | Exposicion de datos o credenciales. |
| Diferencia conciliacion | Operaciones | Negocio / Tecnologia | Diferencia no explicada. |
| Rechazo proveedor/camara | Negocio / Proveedor | Direccion proyecto | Impide interoperabilidad. |
| Fuga de datos sensibles | Seguridad | Legal / Riesgo / Direccion | Suspension inmediata. |

## 23. Criterios de salida UAT/preproductivo

| Criterio | Estado inicial | Evidencia requerida |
|---|---|---|
| Acta UAT firmada | NO ENCONTRADO | `docs/uat/ACTA_UAT_DATOS_REALES_TEMPLATE.md` completada y firmada. |
| Defectos bloqueantes cerrados | PENDIENTE VALIDAR | Matriz defectos actualizada. |
| CENIT CUD validado si aplica | BRECHA | Evidencia externa/operativa. |
| Sobre digital/firma validado si aplica | BRECHA | Evidencia proveedor/camara. |
| Seguridad critica cerrada | PARCIAL | `AchResponsesController` protegido, SPA prod saneada y compose con placeholders; `.env`, OpenBao y policy granular siguen pendientes. |
| Rollback probado o aceptado | BRECHA | Evidencia simulacro o aceptacion formal. |
| Configuracion productiva sin localhost | PARCIAL | `environment.prod.ts` corregido; falta build/config UAT/preprod. |
