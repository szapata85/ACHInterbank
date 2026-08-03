# Evidencia — Docker y Playwright

## Docker

- SQL Server, PostgreSQL, ambas API y ambas SPA: saludables durante la validación.
- Proxy SPA–API: respuestas JSON correctas, sin 502.
- WSDL accesible desde la red Docker del stack PostgreSQL y desde el host.
- Imagen API corregida usada por ambos stacks.

## Playwright

- Suite LIVE por etapas: creación, despacho exitoso, error controlado, reintento y validación.
- Validación LIVE final SQL Server: 1/1.
- Validación LIVE final PostgreSQL: 1/1.
- Monitor funcional Docker: 4/4 (escritorio, móvil, tableta y permisos).
- Sin mocks de navegador ni respuestas interceptadas.
- Sin errores inesperados de consola, 5xx o 502 en las corridas aprobadas.

Capturas sanitizadas:

- [Monitor SQL Server](evidencias/monitor-sqlserver-sanitizado.png)
- [Monitor PostgreSQL](evidencias/monitor-postgresql-sanitizado.png)
