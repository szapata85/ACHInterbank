# Scalar-SEC-4B — Cierre puntual de rutas P1/P2 sin security (2026-05-01)

## Resumen
Se cerró la brecha puntual heredada de SEC-4A para rutas P1/P2 sin security. Se aplicó hardening en `PUT /api/users/branding` y `POST /Nacha/header`, manteniendo `GET /api/users/branding` como endpoint público por diseño.

## Evidencia heredada SEC-4A
SEC-4A reportó como pendientes:
- `GET /api/users/branding`
- `PUT /api/users/branding`
- `POST /Nacha/header`

## Clasificación y decisión por ruta
| Ruta | Controller | Acción | Método | Tipo | Pública o interna | Estado previo | Decisión SEC-4B |
|---|---|---|---|---|---|---|---|
| `/api/users/branding` | `BrandingController` | `GetBrandingAsync` | GET | Consulta | Pública (pantalla login/identidad visual) | `AllowAnonymous` | Se mantiene anónima con justificación explícita |
| `/api/users/branding` | `BrandingController` | `SaveBrandingAsync` | PUT | Escritura configuración | Interna administrativa | `AllowAnonymous` | Se endurece con `Authorize(Policy = "CanManageAch")` |
| `/Nacha/header` | `NachaController` | `SaveHeader` | POST | Escritura operativa NACHA | Interna operativa | Controller `AllowAnonymous` | Se endurece con `Authorize` + `Authorize(Policy = "CanManageAch")` |

## Cambios aplicados
1. `BrandingController.SaveBrandingAsync`:
- se removió `AllowAnonymous`;
- se aplicó `Authorize(Policy = "CanManageAch")`.

2. `NachaController`:
- se removió `AllowAnonymous` a nivel controller;
- se aplicó `Authorize` a nivel controller;
- `SaveHeader` quedó con `Authorize(Policy = "CanManageAch")`.

3. Pruebas nuevas:
- `AuthorizationUniformitySec4BPendingRoutesTests` valida:
  - GET branding sigue anónimo;
  - PUT branding exige `CanManageAch` y no es anónimo;
  - POST Nacha/header exige `CanManageAch`, controller con `Authorize` y sin `AllowAnonymous`.

## Resultados de build y pruebas
- Build Release: exitoso.
- Pruebas específicas SEC-4B/seguridad: `26/26` exitosas.
- Suite completa backend: `418/418` exitosas.

## OpenAPI runtime post-SEC-4B
Archivo generado: `/tmp/openapi-sec4b.json`.

Conteos:
- `TOTAL_OPERACIONES_OPENAPI=213`
- `OPERACIONES_CON_SECURITY=207`
- `OPERACIONES_SIN_SECURITY=6`

Rutas sin security remanentes:
- `POST /Auth/login`
- `POST /Auth/forgot-password`
- `POST /Auth/reset-password`
- `GET /api/users/branding`
- `POST /Oauths/GenerateToken`
- `POST /Oauths/GenerateTokenAsync`

Todas son coherentes con endpoints públicos/autenticación y no corresponden a brecha P1/P2 de escritura.

## Validación puntual de las 3 rutas
- `GET /api/users/branding`: Encontrado=Sí, TieneSecurity=No (excepción pública justificada).
- `PUT /api/users/branding`: Encontrado=Sí, TieneSecurity=Sí.
- `POST /Nacha/header`: Encontrado=Sí, TieneSecurity=Sí.

## CSV generados
- `docs/api/scalar-sec4b-openapi-security-operaciones-2026-05-01.csv`
- `docs/api/scalar-sec4b-openapi-security-rutas-pendientes-2026-05-01.csv`
- `docs/api/scalar-sec4b-openapi-security-allowanonymous-2026-05-01.csv`

## Riesgos residuales y alcance
- No se cambió lógica de negocio, rutas, contratos ni DTOs.
- No se crearon permisos nuevos.
- No se agregó `AllowAnonymous` nuevo.
- No se declara seguridad total cerrada para toda la plataforma; se cierra el objetivo puntual SEC-4B.

## Veredicto
**Scalar-SEC-4B: CERRADO.**
Las dos rutas de escritura pendientes quedaron protegidas y la ruta de lectura pública quedó formalmente justificada y trazable en OpenAPI/CSV.

## Nota Scalar-SEC-5

La auditoría final de seguridad API y matriz de aceptación quedó consolidada en:

`docs/api/scalar-sec5-auditoria-final-seguridad-api-matriz-aceptacion-2026-05-01.md`

La evidencia final OpenAPI/CSV quedó en:

- `docs/api/scalar-sec5-openapi-security-operaciones-final-2026-05-01.csv`
- `docs/api/scalar-sec5-openapi-endpoints-sin-security-final-2026-05-01.csv`
- `docs/api/scalar-sec5-openapi-allowanonymous-final-2026-05-01.csv`
- `docs/api/scalar-sec5-openapi-escritura-security-final-2026-05-01.csv`

Veredicto:
se declara cerrado el frente de autorización explícita y metadata OpenAPI/Scalar de seguridad para el alcance evaluado.

No se declara producción lista.
