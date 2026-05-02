# Scalar-SEC-5A — Validación técnica final de seguridad API (2026-05-01)

## 1. Resumen ejecutivo
Se completó la validación técnica SEC-5A-2 usando evidencia real de OpenAPI runtime generada en SEC-5A-1, generación de CSV finales, pruebas específicas, suite completa backend y build final.

## 2. Evidencia recibida de SEC-5A-1
- `/tmp/openapi-sec5a1.json`: presente.
- `docs/api/scalar-sec5-openapi-security-operaciones-final-2026-05-01.csv`: presente.
- `docs/api/scalar-sec5-openapi-endpoints-sin-security-final-2026-05-01.csv`: presente.

## 3. Resultado OpenAPI real final
OpenAPI runtime cargado desde `/tmp/openapi-sec5a1.json` (extraído de `http://127.0.0.1:5194/openapi/v1.json`).

## 4. Total de operaciones
- `TOTAL_OPERACIONES_OPENAPI=213`

## 5. Operaciones con security
- `OPERACIONES_CON_SECURITY=207`

## 6. Operaciones sin security
- `OPERACIONES_SIN_SECURITY=6`

## 7. Security schemes detectados
- `SECURITY_SCHEMES=['Bearer']`

## 8. Endpoints sin security y justificación esperada
- `POST /Auth/login` → endpoint público de autenticación.
- `POST /Auth/forgot-password` → endpoint público de recuperación.
- `POST /Auth/reset-password` → endpoint público de recuperación.
- `GET /api/users/branding` → consulta pública de branding para login.
- `POST /Oauths/GenerateToken` → endpoint público/preautenticado de token.
- `POST /Oauths/GenerateTokenAsync` → endpoint público/preautenticado de token.

No se observaron endpoints operativos bancarios adicionales sin security.

## 9. Validación AllowAnonymous
CSV generado: `docs/api/scalar-sec5-openapi-allowanonymous-final-2026-05-01.csv`.

Resultados:
- `ENDPOINTS_ANONIMOS_REVISADOS=7`
- `ANONIMOS_CON_SECURITY=1` (PUT branding, correcto por hardening SEC-4B)
- `ANONIMOS_SIN_SECURITY=6` (Auth/Oauths + GET branding)

## 10. Validación de endpoints de escritura
CSV generado: `docs/api/scalar-sec5-openapi-escritura-security-final-2026-05-01.csv`.

Resultados:
- `TOTAL_ENDPOINTS_ESCRITURA=104`
- `ESCRITURA_CON_SECURITY=99`
- `ESCRITURA_SIN_SECURITY=5`

Las 5 escrituras sin security son únicamente de autenticación/token (`Auth` y `Oauths`), alineadas con el criterio esperado.

## 11. Resultado pruebas específicas
- Listado por filtro `Authorization|Authorize|AllowAnonymous|OpenApi|Scalar|Security|ExplicitAuthorization|Uniformity`: exitoso.
- Ejecución filtrada: `26/26` pruebas exitosas.

## 12. Resultado suite completa
- Suite backend completa: `418/418` pruebas exitosas.

## 13. Resultado build final
- `dotnet build ACHInterbank.sln -c Release`: exitoso, 0 errores.

## 14. CSV generados
- `docs/api/scalar-sec5-openapi-security-operaciones-final-2026-05-01.csv`
- `docs/api/scalar-sec5-openapi-endpoints-sin-security-final-2026-05-01.csv`
- `docs/api/scalar-sec5-openapi-allowanonymous-final-2026-05-01.csv`
- `docs/api/scalar-sec5-openapi-escritura-security-final-2026-05-01.csv`

## 15. Riesgos o bloqueos
Sin bloqueos técnicos en SEC-5A-2. Persisten advertencias de nulabilidad históricas fuera del alcance de seguridad API.

## 16. Veredicto técnico
**SEC-5A CERRADO.**

Puede continuar **SEC-5B**.

Constancias de alcance:
- No se cambió lógica de negocio.
- No se cambiaron rutas.
- No se cambiaron contratos.
- No se cambiaron permisos.
- No se agregaron endpoints.
- No se agregó `AllowAnonymous`.
- No se declara producción lista.
