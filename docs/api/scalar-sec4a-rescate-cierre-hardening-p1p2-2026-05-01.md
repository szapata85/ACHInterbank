> Nota G3.5.2: las referencias a proveedores de secretos retirados son historicas y obsoletas desde el cleanup `ebf7a8a5`; no describen el stack vigente.

# Scalar-SEC-4A — Rescate y cierre amplio de hardening P1/P2 (2026-05-01)

## Resumen ejecutivo
Se completó el rescate de Scalar-SEC-4A con validación técnica de hardening explícito, auditoría global de controladores, pruebas de autorización, suite completa backend y verificación OpenAPI runtime.

## Estado del hardening parcial heredado (SEC-4 parcial)
Validado y **conservado sin reversión**:
- `MaintenanceController` con `[Authorize]` y `RunDbInitializer` con `CanManageAch`.
- `RegulatoryCatalogsController` con `[Authorize]` y todos los `GET` con `CanReadAch`.
- `SobreDigitalController` con `[Authorize]` y acciones operativas con `CanManageAch`.
- Pruebas `AuthorizationUniformityP1P2ControllersTests` presentes.
- No se agregó `AllowAnonymous` nuevo.

## Inventario global y hallazgos de autorización
- Controladores detectados: 52.
- Resultado de auditoría actual: no se encontraron controladores sin `Authorize`/`AllowAnonymous` explícito.
- Se confirma patrón predominante:
  - lectura: `CanReadAch` o política fina de lectura;
  - mutación/operación: `CanManageAch` o política fina equivalente.

## Políticas existentes detectadas
- Base ACH: `CanReadAch`, `CanManageAch`.
- Fino granular: políticas `FineGrainedPermissions` (por ejemplo capability registry y operaciones de seguridad NACHA).
- Existen políticas específicas para certificados, seguridad NACHA y auditoría; se preservan sin cambios.

## Endpoints anónimos justificados
Se mantuvieron anónimos por diseño operativo:
- autenticación y recuperación (`Auth/login`, `forgot-password`, `reset-password`);
- emisión token OAuth/JWKS (`Oauths`, `Jwks`);
- publicación técnica de `MapOpenApi`/`MapScalarApiReference`.

No se agregó `Authorize` en estos casos y no se agregó `AllowAnonymous` nuevo.

## Resultado de build y pruebas
- Build inicial: exitoso.
- Pruebas específicas de autorización/seguridad/OpenAPI (filtro SEC): **23/23** exitosas.
- Suite completa backend: **415/415** exitosas.

## OpenAPI runtime post-SEC-4A
Documento real generado desde API en ejecución:
- `http://127.0.0.1:5194/openapi/v1.json`

Resultados:
- `TOTAL_OPERACIONES_OPENAPI=213`
- `OPERACIONES_CON_SECURITY=205`
- `OPERACIONES_SIN_SECURITY=8`

## Evidencia CSV generada
- `docs/api/scalar-sec4a-openapi-security-operaciones-2026-05-01.csv`
- `docs/api/scalar-sec4a-openapi-security-p1p2-2026-05-01.csv`

Conteo P1/P2 (por filtros de rutas operativas/configuración):
- `TOTAL_P1P2=178`
- `P1P2_SIN_SECURITY=3`

Detalle de P1/P2 sin security en OpenAPI:
1. `GET /api/users/branding`
2. `PUT /api/users/branding`
3. `POST /Nacha/header`

Clasificación:
- `Branding`: excepción funcional pública ya existente (pendiente de decisión de gobierno SEC-5).
- `Nacha/header`: endpoint con comportamiento histórico especial; requiere decisión de negocio/seguridad antes de endurecer sin riesgo de ruptura.

## Matriz resumida de controladores prioritarios y recomendación
- Controladores críticos/altos ya endurecidos con políticas explícitas: **sin cambio requerido en SEC-4A**.
- Controladores anónimos de autenticación/token/JWKS: **mantener anónimos**.
- Excepciones pendientes para SEC-5:
  - Branding (revisión de exposición pública).
  - Nacha/header (revisión de necesidad operativa y patrón de autenticación esperado).

## Qué no se implementó en SEC-4A
- No se cambiaron rutas.
- No se cambiaron contratos públicos.
- No se cambiaron DTOs.
- No se cambiaron permisos existentes.
- No se crearon permisos nuevos.
- No se cambió lógica de negocio.
- No se agregó `AllowAnonymous`.
- No se tocó Angular, criptografía ni proveedor de secretos retirado.
- No se declara producción lista.
- No se declara seguridad API total cerrada (queda trabajo de gobierno en SEC-5).

## Recomendación para Scalar-SEC-5
1. Definir política objetivo para endpoints `Branding` (público controlado vs autenticado).
2. Definir política objetivo para `POST /Nacha/header`.
3. Cerrar brechas residuales de `P1P2_SIN_SECURITY` a 0 con decisión funcional aprobada.
4. Repetir ciclo build + tests + OpenAPI runtime + CSV de control final.

## Veredicto
**Scalar-SEC-4A: CERRADO CON PENDIENTES CONTROLADOS (SEC-5).**
Se cierra el rescate y validación amplia del hardening P1/P2 con evidencia ejecutada; los pendientes remanentes quedan formalmente clasificados para decisión y cierre en SEC-5.
