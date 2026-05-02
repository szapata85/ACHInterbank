# Scalar-SEC-1 — Auditoría de autorización explícita en controladores críticos (2026-05-01)

## Resumen ejecutivo
Se ejecutó auditoría técnica-documental de autorización en controllers críticos del API ACHInterbank sin modificar código de seguridad. Se confirmó coexistencia de: (a) controllers con `Authorize` explícito por controller/acción y policy, (b) controllers con `AllowAnonymous` técnico, y (c) controllers críticos sin `Authorize` explícito que dependen de seguridad global. También se validó OpenAPI runtime y se evidenció que los endpoints críticos analizados aparecen con `security=None` por operación.

## Evidencia de validación inicial
- `dotnet build ACHInterbank.sln -c Release`: exitoso (warnings de nulabilidad, 0 errores).
- Estado git limpio al inicio de la auditoría.

## Inspección global de seguridad (pipeline/configuración)
Hallazgos principales:
1. Existe `AddAuthentication` con `JwtBearer`.  
2. Existe `AddAuthorization` con policies (`CanManageAch`, `CanReadAch`, y policies finas de `FineGrainedPermissions`).  
3. Existe `UseAuthentication`.  
4. Existe `UseAuthorization`.  
5. No se encontró `FallbackPolicy` explícita en la configuración inspeccionada.  
6. No se encontró `DefaultPolicy` personalizada explícita.  
7. No se encontró `MapControllers().RequireAuthorization()` global en la evidencia revisada.  
8. `MapOpenApi` y `MapScalarApiReference` se publican con `AllowAnonymous`.  
9. Existen endpoints explícitos con `AllowAnonymous` (`AuthController`, `OauthsController`, `JwksController`, rutas puntuales de `TestsController`, `BrandingController`, `ServersController`, `NachaController`).
10. Con inspección estática no es posible afirmar cierre total de seguridad global; se requiere hardening controlado posterior.

## Inventario y clasificación de controladores críticos mínimos
| Controller | Estado | [Authorize] en controller | [Authorize] en acciones | [AllowAnonymous] | Policies detectadas | Tipo | Criticidad | Clasificación |
|---|---|---:|---:|---:|---|---|---|---|
| TransactionsController | Encontrado | No | No | No | N/A | Mixto (GET/POST) | CRÍTICA | Depende de seguridad global |
| AchTraceabilityController | Encontrado | No | No | No | N/A | Mixto (GET/POST) | CRÍTICA | Depende de seguridad global |
| IncomingNachaCommandCenterController | Encontrado | Sí (`CanReadAch`) | Sí | No | `CanReadAch`, `CanManageAch` | Mixto | CRÍTICA | Autorización explícita |
| BulkIngestionController | Encontrado | Sí (`CanManageAch`) | Sí (hereda) | No | `CanManageAch` | Mixto | CRÍTICA | Autorización explícita |
| PaymentRailCapabilityRegistryController | Encontrado | Sí | Sí | No | `FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry` | GET | ALTA | Autorización explícita |
| ReportsController | Encontrado | Sí | Sí | No | `CanReadAch` | GET | ALTA | Autorización explícita |
| NachaSecurityOperationsController | Encontrado | Sí | Sí | No | policies finas + `CanReadAch` | Mixto | CRÍTICA | Autorización explícita |
| CertificateManagementController | Encontrado | Sí | Sí | No | policies de certificados | Mixto | CRÍTICA | Autorización explícita |
| DigitalEnvelopeCertificatesController | Encontrado | Sí | Sí | No | (Authorize base) | Mixto | CRÍTICA | Autorización explícita |
| NachaExportController | Encontrado | Sí | Sí | No | `CanReadAch`/`CanManageAch` | Mixto | CRÍTICA | Autorización explícita |
| AchReturnsController | Encontrado | **No** | **No** | No | N/A | Mixto | CRÍTICA | Depende de seguridad global |
| CenitOperationsController | Encontrado | Sí | Sí | No | `CanReadAch` | GET | CRÍTICA | Autorización explícita |
| NachaUploadController | Encontrado | No (en clase) | Sí | No | `CanReadAch`, `CanManageAch` | Mixto | CRÍTICA | Autorización explícita por acción |
| AuthController | Encontrado | No | Sí/No (mixto) | Sí | `Authorize` en refresh; login/reset anónimos | Mixto | CRÍTICA | Mixto técnico válido |
| UsersController | Encontrado | Sí | Sí (hereda) | No | `Authorize` | Mixto | CRÍTICA | Autorización explícita |
| RolesController | Encontrado | Sí | Sí (hereda) | No | `Authorize` | GET | CRÍTICA | Autorización explícita |
| PermissionsController | Encontrado | Sí | Sí (hereda) | No | `Authorize` | GET | CRÍTICA | Autorización explícita |

## Controllers sin [Authorize] explícito (riesgo)
En el set crítico obligatorio se identificaron sin `Authorize` explícito de controller/acción:
- `TransactionsController`
- `AchTraceabilityController`
- `AchReturnsController`

Estos quedan dependientes de seguridad global y requieren revisión prioritaria.

## Endpoints críticos en OpenAPI con security por operación
Se generó OpenAPI runtime real (`/tmp/openapi-sec1.json`).

Resumen:
- `TOTAL_CRITICOS_OPENAPI=95`
- `SIN_SECURITY=95`

Incluye rutas críticas de transacciones, trazabilidad, command center, bulk ingestion, certificados, seguridad NACHA, reportes, users/roles/permissions y autenticación. Este resultado indica que la especificación OpenAPI actual no materializa metadato de `security` por operación para estos endpoints, incluso cuando varios controllers sí tienen `Authorize` explícito en código.

## Controladores que deberían reforzarse en fase posterior
Recomendación priorizada de hardening (sin aplicar en esta fase):
1. `TransactionsController` — agregar `Authorize` explícito en controller y/o policies por acción.
2. `AchTraceabilityController` — separar policy de consulta vs acción operativa (`certify`).
3. `AchReturnsController` — definir `Authorize` explícito por perfil operativo/regulatorio.
4. Revisar `NachaUploadController` para uniformidad (`Authorize` de clase + policies por acción).

## Controladores que deberían mantener AllowAnonymous (justificación técnica)
Mantener `AllowAnonymous` donde la función técnica lo exige:
- `AuthController`: login/forgot/reset.
- `JwksController`: publicación de llaves públicas.
- `MapOpenApi` / `MapScalarApiReference`: documentación runtime.
- `ServersController` / health/version endpoints técnicos (según operación de plataforma).

## Controladores que requieren revisión de exposición pública
Por criticidad o sensibilidad de datos/operación:
- `ReportsController`
- `NachaSecurityOperationsController`
- `CertificateManagementController`
- `DigitalEnvelopeCertificatesController`
- `IncomingNachaCommandCenterController`
- `BulkIngestionController`
- `TransactionsController`
- `AchTraceabilityController`
- `AchReturnsController`

## Recomendación de siguiente prompt
**Scalar-SEC-2 — Hardening controlado de autorización explícita en controladores críticos**

Objetivo recomendado:
- Definir y aplicar `Authorize` explícito en controllers críticos hoy dependientes de seguridad global.
- Introducir policies diferenciadas por acción (consulta vs mutación) en transacciones, trazabilidad y devoluciones.
- Revalidar OpenAPI para exponer metadato de seguridad por operación y actualizar matriz de riesgo residual.

## Veredicto
**Scalar-SEC-1 CERRADO (auditoría documental/técnica).**

Alcance cerrado: identificación, clasificación y recomendación.  
Hardening de seguridad API: **NO implementado en este prompt**.


Nota Scalar-SEC-1A:
la evidencia faltante de CSV OpenAPI security y build final fue cerrada en
docs/api/scalar-sec1a-cierre-evidencia-autorizacion-openapi-2026-05-01.md.
Se generaron los CSV:
docs/api/scalar-sec1a-openapi-security-operaciones-2026-05-01.csv
docs/api/scalar-sec1a-openapi-security-operaciones-criticas-2026-05-01.csv
docs/api/scalar-sec1-openapi-security-operaciones-2026-05-01.csv
