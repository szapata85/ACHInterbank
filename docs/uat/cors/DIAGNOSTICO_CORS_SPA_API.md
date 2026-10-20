# Diagnóstico CORS SPA–API

Fecha de ejecución: 2026-07-16. Entorno: Development local controlado.

## Causa raíz

La SPA `local-e3` se sirve desde `http://localhost:743` y consume la API en `http://localhost:843`. El origen 743 no estaba en `Cors:Origins`, por lo que la respuesta HTTP 200 de branding no incluía `Access-Control-Allow-Origin` y el navegador la bloqueaba.

Existía además un middleware manual de `OPTIONS` que reflejaba cualquier `Origin`, agregaba encabezados CORS por fuera de la política oficial y terminaba el preflight. Esto creaba dos motores CORS con criterios distintos.

## Corrección

- `http://localhost:743` se agregó únicamente al archivo Development, junto con el origen 4200 ya usado.
- La configuración base quedó sin orígenes implícitos. Production no habilita localhost salvo configuración explícita del ambiente.
- La política `CorsPolicy` consume sólo `Cors:Origins`, elimina duplicados y aplica métodos, encabezados y credenciales.
- Una lista vacía falla de forma cerrada.
- Se eliminó el middleware manual de `OPTIONS` y el encabezado CORS duplicado del middleware de seguridad.
- `UseCors` permanece antes de autenticación y autorización; no se deshabilitaron JWT, WAF, rate limiting ni encabezados de seguridad.

## Topología validada

| Componente | Topología |
| --- | --- |
| SPA | host local, puerto 743, configuración `local-e3` |
| API | host local, puerto 843, `ASPNETCORE_ENVIRONMENT=Development` |
| Base | SQL Server local desechable/controlado |
| SOAP | WCF local, puerto 7083; no participa en las pruebas CORS |

No se introdujo proxy Angular ni se modificó `apiBaseUrl`. La instancia Docker de la SPA se detuvo para evitar dos procesos compitiendo por IPv4/IPv6 en el puerto 743; la validación final usó una única instancia host.
