# Scalar-SEC-3A — Revalidación build/tests/OpenAPI de metadata security (2026-05-01)

## 1. Resumen ejecutivo
Se ejecutó una revalidación técnica completa de Scalar-SEC-3 con evidencia real de build, pruebas y OpenAPI runtime. El resultado es **CERRADO** para el alcance SEC-3A: los transformadores compilan, están registrados, el esquema `Bearer` existe en OpenAPI y los endpoints P0 objetivo quedaron con `security` en el documento generado en ejecución real.

## 2. Contexto SEC-3 parcial
SEC-3 había introducido `OpenApiSecurityMetadataTransformer` y `OpenApiBearerSecuritySchemeTransformer`, pero estaba pendiente el cierre formal por falta de evidencia de entorno/build/tests/OpenAPI runtime.

## 3. Causa de cierre pendiente
La causa pendiente era operativa: ausencia previa de validación completa en entorno con `dotnet` disponible y sin evidencia de ejecución end-to-end.

## 4. Resultado dotnet disponible
- `bash scripts/codex/setup-codex-env.sh` instaló SDK `10.0.203`.
- `which dotnet` resolvió `/root/.dotnet/dotnet`.
- `dotnet --info` y `dotnet --list-sdks` confirmaron SDK/runtimes activos.
- `global.json` fijó `10.0.203`.

## 5. Resultado build inicial
`dotnet build ACHInterbank.sln -c Release` ejecutó exitosamente con **0 errores** (warnings preexistentes fuera del alcance SEC-3A).

## 6. Inspección de transformadores
- Clase `OpenApiSecurityMetadataTransformer` presente y evaluando metadata `Authorize`/`AllowAnonymous` por operación.
- Clase `OpenApiBearerSecuritySchemeTransformer` presente y agregando `components.securitySchemes["Bearer"]`.
- Registro confirmado en `AddOpenApi` mediante `AddOperationTransformer` y `AddDocumentTransformer`.

## 7. Correcciones aplicadas, si hubo
No se realizaron correcciones de lógica en transformadores ni controladores. Solo se eliminó un artefacto local generado (`src/Cfa.ACHInterbank.Api/C:/...`) que no forma parte del código fuente.

## 8. Resultado pruebas específicas
- Listado de pruebas relevantes por filtro (`OpenApi|Scalar|Security|Authorize|AllowAnonymous|ExplicitAuthorization`) obtenido correctamente.
- Ejecución filtrada:
  - Total: 20
  - Fallidas: 0
  - Exitosas: 20

## 9. Resultado suite completa
Ejecución completa backend:
- Total: 412
- Fallidas: 0
- Exitosas: 412

## 10. Resultado OpenAPI real
Se levantó la API en runtime (`dotnet run`) y se descargó `http://127.0.0.1:5194/openapi/v1.json` hacia `/tmp/openapi-sec3a.json`.

## 11. Conteos generales security
- `TOTAL_OPERACIONES_OPENAPI=213`
- `OPERACIONES_CON_SECURITY=195`
- `OPERACIONES_SIN_SECURITY=18`
- `SECURITY_SCHEMES=['Bearer']`

Se verificó esquema:
- `Bearer`: `type=http`, `scheme=bearer`, `bearerFormat=JWT`.

## 12. Conteos P0 security
CSV P0 generado con rutas objetivo (`Transactions`, `ach-traceability`, `ach-returns`):
- `TOTAL_ENDPOINTS_P0_OPENAPI=12`
- `P0_CON_SECURITY=12`
- `P0_SIN_SECURITY=0`

## 13. Validación AllowAnonymous
Validación sobre endpoints anónimos detectados en OpenAPI runtime:
- `ENDPOINTS_ANONIMOS_REVISADOS=5`
- `ANONIMOS_CON_SECURITY=0`
- `ANONIMOS_SIN_SECURITY=5`

No se observaron endpoints realmente anónimos con `security` indebido.

## 14. Comparación contra SEC-3 original
Comparación de CSV P0:
- `OLD_P0_TOTAL=22`
- `OLD_P0_SIN_SECURITY=0`
- `NEW_P0_TOTAL=12`
- `NEW_P0_SIN_SECURITY=0`

Observación: el nuevo corte P0 está acotado estrictamente al objetivo SEC-3A definido para `Transactions`, `AchTraceability` y `AchReturns`.

## 15. CSV generados
- `docs/api/scalar-sec3a-openapi-security-operaciones-2026-05-01.csv`
- `docs/api/scalar-sec3a-openapi-security-p0-2026-05-01.csv`
- `docs/api/scalar-sec3a-openapi-security-allowanonymous-2026-05-01.csv`

## 16. Resultado build final
Build final ejecutado con éxito:
- `dotnet build ACHInterbank.sln -c Release`
- Resultado: exitoso, 0 errores.

## 17. Riesgos restantes
- Persisten warnings de nulabilidad en módulos ajenos a SEC-3A.
- El cierre aplica a metadata security OpenAPI para el alcance SEC-3/SEC-3A; no implica cierre integral de todas las superficies de seguridad API.

## 18. Qué no se implementó en Scalar-SEC-3A
- No se cambiaron rutas.
- No se cambiaron contratos.
- No se cambiaron DTOs.
- No se cambió lógica de negocio.
- No se cambiaron permisos.
- No se agregaron endpoints.
- No se agregó AllowAnonymous.
- No se tocó Angular.
- No se tocó criptografía.
- No se tocó OpenBao.
- No se declara producción lista.
- No se declara seguridad API total cerrada si quedan controladores P1/P2 pendientes.

## 19. Veredicto
**Veredicto Scalar-SEC-3A: CERRADO (alcance técnico cumplido).**
Con evidencia real de entorno, build, pruebas, OpenAPI runtime y validaciones de `security`/`AllowAnonymous`, se cierra formalmente la validación pendiente de Scalar-SEC-3 para el alcance definido.

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
