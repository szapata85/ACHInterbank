# SEC-I05 — Runbook de hardening, proxy confiable y pendientes de ambiente

## Alcance implementado (código fuente)

1. `DependencyInjectionService.cs`: Scalar/OpenAPI solo se registran en `Development`; en otros ambientes requieren `Documentation:EnableInNonDevelopment=true` **más** control perimetral (esta opción por sí sola deja los endpoints anónimos, no es autenticación). `/` y `/index.html` redirigen a `/health/live` al estar deshabilitados.
2. `RateLimitClientIdentity.cs`: partición de rate-limit por IP de socket, salvo si el salto inmediato está en `Security:TrustedReverseProxyIps` (direcciones IP literales, sin CIDR) o si el ambiente es Development. Solo entonces se analiza la primera IP válida de `X-Forwarded-For`. La excepción de Development conserva el flujo local existente y **no debe ser usada en producción**.
3. `nginx.conf`: reescribe `X-Forwarded-For` a `$remote_addr` en lugar de concatenar un valor potencialmente proporcionado por cliente. Reduce divulgación de versión, aplica cabeceras en la SPA, bloquea dotfiles y fija timeouts razonables. Mantiene `client_max_body_size 50m`, rutas y tiempos actuales de proxy para no alterar NACHA/MFT.
4. `Program.cs`: oculta encabezado de servidor de Kestrel; `SecurityHeadersMiddleware.cs`: elimina HPKP/Feature-Policy obsoletos y fija HSTS solo cuando la conexión ASP.NET Core realmente es HTTPS fuera de Development. **Si TLS termina antes de Nginx/API, el terminador autorizado debe emitir HSTS** tras comprobación.
5. Pruebas de identidad de cliente xUnit escritas. Verificador Python estático independiente incluido.

## Configuración exigida antes del despliegue

- Identificar el **socket real** que conecta el proxy confiable a la API (dirección IP fija o controlada). Configurar `Security__TrustedReverseProxyIps__0=IP_DEL_PROXY` en el servicio API; solo IP literal, no DNS/CIDR. NO confiar en cualquier 172.16/12 ni usar `0.0.0.0` como comodín. Si hay balanceador adicional delante de Nginx, configurar `real_ip` **solo para sus IPs confiables** antes de reconstruir la IP cliente; de lo contrario Nginx reenviará la IP del balanceador y rate-limit podría agrupar usuarios. Evitar acceso directo público a Kestrel; restringir por segmentación/firewall.
- Confirmar con red/seguridad si el TLS termina en el ingress externo. HSTS se gestiona en el terminador HTTPS, no inferirlo del encabezado recibido. Habilitar TLS y redirección HTTPS allí. No añadir certificados de prueba ni rutas HTTP públicas.
- **No** habilitar `Documentation:EnableInNonDevelopment` sin aprobación de Seguridad y allowlist/mTLS/VPN/permisos del ingress. La aplicación lo ofrece como excepción de operación, no acceso restringido.
- Validar CSP actual (con `unsafe-inline` heredado) con assets Angular; no endurecer CSP a ciegas si rompe UX/Angular. Plantear nonces/hashes y modo report-only con datos UAT antes de imposición.
- `NachaUpload`, bulk ingestion y archivos firmados: mantener límites específicos actuales. Revisar casos `Content-Encoding: br`, límites de bytes descomprimidos y ratio de expansión antes de incorporar decodificación Brotli, **sin** cambiar la estructura NACHA-M. No se agregó un decodificador Brotli.
- Docker: evaluar en UAT usuario no root, filesystem readonly, tmpfs y permisos de `crash.log`/`trace.log`, certificados, volúmenes MFT y healthchecks. **No se activó** este hardening sin comprobar compatibilidad con entrypoint y montajes. Mantener el Compose original como Desarrollo; implementar manifiesto productivo aislado, sin secrets hardcoded, tras diseño de infraestructura.

## Comandos en entorno autorizado

```bash
# Código .NET
dotnet restore ACHInterbank.sln
dotnet build ACHInterbank.sln -c Release
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release

# Nginx REAL de la imagen corregida 1.30.6, no host 1.26
# Primero construir la imagen SPA en host con Docker
# docker compose build --pull achinterbank-spa
# docker run --rm --entrypoint nginx achinterbank-spa:local -t

# Plataforma
# docker compose config
# docker compose build --pull achinterbank-api achinterbank-spa
# Validar 403/404 en rutas ocultas, no acceso a Scalar/OpenAPI en Production,
# headers, health, SPA, login, certificados, subida NACHA sintética de 10MB
# y tasa concurrente detrás del proxy real.
```

## Estado y riesgo residual

- Nginx local 1.26.3: pasó sintaxis **en envoltura de prueba** con sustitución de `resolve` por IP localhost, necesaria por diferencia de versiones. No certifica `nginx -t` real con imagen 1.30.6 ni resolución DNS Docker.
- Dotnet, Docker, DB, UAT, red externa: `NOT_RUN_ENVIRONMENT` sin dar PASS ficticio.
- La dirección del proxy, configuración TLS, timeouts, límites de seguridad de cargas, secretos y permisos de runtime son decisiones de operación a homologar.
- La clave del seed administrativo queda intacta como restricción del usuario. Su riesgo residual requiere aceptación antes de `GO_DEPLOY`.
- `GO_INCREMENT=YES`, `GO_DEPLOY=NO`, `GO_LIVE_MONETARY=NO`, Quartz ACH-CR-001 apagado.
