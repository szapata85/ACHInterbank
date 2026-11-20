# Entrega ACHInterbank — Quartz EF Core y despliegue IIS sin Docker

Base: `ACHInterbank_Quartz_EFCore_Sin_Contenedor_20261118.zip` (proyecto fuente completo).

## Ajustes incluidos

1. Se conserva la migracion SQL Server EF Core de Quartz.NET 4.7.1; la instancia de SQL Server no necesita un contenedor auxiliar para `QRTZ_*`.
2. Modo opcional `SpaHosting:Enabled` en la API: sirve Angular desde `wwwroot`, permite rutas Angular al refrescar, conserva rutas de API, health checks y previene fallback SPA en llamadas JSON y assets inexistentes.
3. Script `scripts/iis/publish-iis.cmd`/`.ps1` para compilar API+Angular y crear un **unico** sitio IIS sin nginx/ARR. Habilita `SpaHosting` en el `appsettings.json` publicado y no agrega variables de entorno a `web.config`.
4. Perfil heredado de publicacion FolderProfile actualizado de net8.0 a net10.0.
5. Guia de configuracion y recuperacion: `docs/deployment/IIS_WINDOWS_SERVER.md`.
6. Chequeo estatico IIS: `scripts/test/verify_iis_hosting.py`.

## Compatibilidad

- Docker sigue operativo en su arquitectura original; `SpaHosting:Enabled=false` por defecto.
- La migracion Quartz existente, la politica de *misfire*, `RequestsRecovery`, el almacen persistente y las protecciones de idempotencia permanecen sin alteraciones funcionales.
- No se han modificado llamadas SOAP, comportamiento NACHA-M ni el transporte de archivos. **No se afirma** que un envio de resultado externo incierto sea automaticamente recuperable sin duplicados.

## Evidencia de esta entrega

- `python scripts/validate-solution-projects.py`: OK.
- `python scripts/test/verify_quartz_4_7_1_sqlserver.py`: OK.
- `python scripts/test/verify_iis_hosting.py`: OK.
- **No ejecutados**: .NET build/test, Angular build/test, IIS, migracion sobre SQL Server, Docker ni pruebas de caida/UAT. No hay .NET/Docker/IIS disponible en este entorno y el paquete contiene fuentes, no binarios de produccion.

## Primeros pasos del cliente

Leer `docs/deployment/IIS_WINDOWS_SERVER.md` antes de instalar: publicar en una maquina Windows de build, provisionar TLS y SQL Server, configurar las credenciales fuera del repositorio, aplicar migraciones con backup y validar las rutas y el reinicio del scheduler con contrapartes de UAT.
