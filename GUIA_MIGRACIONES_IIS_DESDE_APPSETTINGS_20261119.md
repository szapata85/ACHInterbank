# ACHInterbank — migraciones EF Core por appsettings.json en IIS

La API usa un unico `src/Cfa.ACHInterbank.Api/appsettings.json`, sin overlays de Development/Production. Incluye:

```json
"Database": {
  "Provider": "SqlServer",
  "ApplyMigrations": true,
  "ApplySeed": false
}
```

El publicador `scripts\iis\publish-iis.cmd` **no** añade `Database__ApplyMigrations` ni otros valores en `web.config`; conserva la configuracion JSON. Para IIS habilita `SpaHosting:Enabled` en el `appsettings.json` **publicado** para servir Angular desde `wwwroot`. 

## Instalacion inicial IIS

1. Provisionar SQL Server, la base, el usuario y el acceso necesario para crear/actualizar el esquema.
2. Publicar la solucion con `scripts\iis\publish-iis.cmd` en el equipo de compilacion.
3. En la carpeta publicada, establecer `ConnectionStrings:SqlConnection` y los demas parametros requeridos en **appsettings.json**; proteger el archivo con ACL NTFS y nunca incluir secretos en el repositorio.
4. Realizar copia de seguridad y detener cualquier otra instancia de ACHInterbank.
5. Arrancar IIS de forma controlada, sin trafico ni operaciones ACH, con `Database:ApplyMigrations=true`; la API intentara aplicar las migraciones EF Core, incluida Quartz.
6. Confirmar los logs, `__EFMigrationsHistory` y las tablas `QRTZ_*`.
7. Detener el sitio y cambiar a `Database:ApplyMigrations=false` **en el appsettings.json publicado**, para evitar que las migraciones se apliquen en arranques posteriores; reiniciar y confirmar que la aplicacion y los jobs funcionan.

**Advertencia:** el codigo fuente vuelve a publicar `ApplyMigrations=true` en futuras versiones, por peticion expresa del despliegue actual. Antes de cada actualizacion en produccion, revisar ese valor. La ejecucion automatica de migraciones no sustituye las pruebas UAT, backups y procedimientos de autorizacion de cambios. Si no se dispone de permisos de modificacion de esquema, aplicar las migraciones previamente con el DBA.
