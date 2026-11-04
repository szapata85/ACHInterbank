# SEC-I05 — Evidencia estática de implementación y límites

- Fecha: 2026-10-27
- Base: `ACHInterbank_ACH_CR_I06_FIX01_SEC_I04_IAM_COMPLETO.zip`
- SHA-256 padre: `79c6904f3490664fa7951988ee763d318d81fbe05a9678e61f9c9118f791138f`
- Alcance: control de acceso a documentación, IP de rate limiter detrás de proxy explícitamente confiable, cabeceras API, Kestrel, Nginx y pruebas xUnit.
- Seed administrativo: **no modificado** (comparación byte a byte del archivo `src/Cfa.ACHInterbank.Persistence/Configuration/UserConfiguration.cs` con la versión padre). SHA-256 del archivo, no de contraseña: `5aa78003c215fb9f3315cbf097a593018f65d2b3580e657a306bacbec6ce41e7`.
- Regla financiera ACH/CENIT, Quartz, certificados, Dockerfiles y `docker-compose.yml`: **sin cambios** en este incremento.
- Limitación importante: documentación OpenAPI/Scalar opt-in en no-Development conserva `.AllowAnonymous()`; requiere restricción perimetral si se habilita. Delante del API se deben autorizar los proxies por IP o todos los clientes detrás de un balanceador pueden compartir el mismo cupo de rate-limit.
- `Content-Encoding: br` y descompresión: sin cambios; ruta explotable no determinada. Límites NACHA/MFT: intactos.

## Resultados locales verificables

- Script `scripts/security/verify_sec_i05_static.py`: **12/12 PASS_STATIC**.
- `nginx -t` del host 1.26.3 sobre *versión sintácticamente adaptada* de `nginx.conf` (upstream `resolve` reemplazado por IP 127.0.0.1 y envuelto en `http`): **PASS_SYNTAX_ONLY**. No es prueba Nginx 1.30.6 en contenedor.
- Dotnet build/test, xUnit, Docker build, `nginx -t` en imagen objetivo, Nginx + API detrás de ingress confiable, integración ACH/CENIT y UAT: **NOT_RUN_ENVIRONMENT**.
- Estado: `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`; continuidad habilitada, despliegue NO autorizado.

## Archivos productivos afectados

- `src/Cfa.ACHInterbank.Api/Program.cs`
- `src/Cfa.ACHInterbank.Api/DependencyInjectionService.cs`
- `src/Cfa.ACHInterbank.Api/Middleware/SecurityHeadersMiddleware.cs`
- `src/Cfa.ACHInterbank.Api/Security/RateLimitClientIdentity.cs` (nuevo)
- `web/ach-interbank-ui/nginx.conf`
- `tests/Cfa.ACHInterbank.Tests/RateLimitClientIdentitySecI05Tests.cs` (nuevo, WRITTEN_NOT_EXECUTED)

## Próximo incremento

`SEC-I06-A_TESTS_SECURITY_REGRESSION`: ampliar pruebas de compatibilidad, regresión financiera ACH/CENIT, límites y mocks; mantener `GO_INCREMENT=YES` aunque no exista ambiente.
