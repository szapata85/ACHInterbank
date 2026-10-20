# ACHInterbank NAV6 — corrección de Quartz y Docker CMD

## Cambios incluidos

- Corrección de `tools/AchInterbank.DockerCli/Program.cs`: Quartz se consulta con `docker compose ps -a -q quartz-schema` para incluir el contenedor de inicialización aunque ya haya terminado (exit 0).
- `docker-ach.cmd` y `docker-ach.bat` completos: lanzadores de CMD con comprobación de `dotnet`, sin PowerShell. Toda la lógica de Docker, configuración y EF Core permanece en el ejecutor `tools/AchInterbank.DockerCli` (.NET 10).
- Conexión SQL Server en `appsettings.Development.json` con contraseña de ejemplo a reemplazar; no se incluye una contraseña real.
- Dockerfile Angular: sincroniza el lock de npm en el paso de construcción antes de `npm ci` para evitar el error de lock incompleto en la versión empaquetada. **Recomendado:** una vez tengas el lock sincronizado en tu equipo, conserva y versiona el `package-lock.json` correcto y elimina el paso de autocorrección para compilaciones reproducibles.

## Configuración previa

Abre `src/Cfa.ACHInterbank.Api/appsettings.Development.json` y cambia **solo la contraseña de ejemplo** en `ConnectionStrings.SqlConnection` por tu clave local de `sa` (no la publiques en Git):

```
Server=127.0.0.1,1433;Database=ACHInterbank;User Id=sa;Password=REEMPLAZAR_POR_CLAVE_SEGURA;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=true
```

Si estás reemplazando una carpeta NAV6 que ya habías configurado, **respalda y restaura tu propio `appsettings.Development.json`**, para no perder la contraseña. Al cambiar `appsettings` no se cambia la contraseña del volumen SQL ya existente.

## Comandos desde CMD (en la raíz del proyecto)

```bat
docker-ach.cmd config
docker-ach.cmd migrate
docker-ach.cmd up --seed --no-auto-migrations --no-build
docker-ach.cmd status
docker-ach.cmd logs
```

Usa el comando `up` **sin** `--no-build` si todavía no has construido las imágenes para esta copia del proyecto:

```bat
docker-ach.cmd up --seed --no-auto-migrations
```

También puedes usar `docker-ach.bat` en lugar de `.cmd` con los mismos argumentos. Necesitas Docker Desktop ejecutándose y .NET SDK 10; la migración manual necesita además `dotnet-ef` disponible. No se usa PowerShell en Windows.

Para ver los logs del contenedor directamente (sin interpolar Compose):

```bat
docker logs --tail 200 achinterbank-achinterbank-api-1
```

`docker-ach.cmd migrate` primero espera la finalización exitosa del inicializador de Quartz y después aplica las migraciones EF existentes. No uses `reset` a menos que quieras borrar **todos los datos del volumen de este stack**.

## Alcance de validación

El ZIP se ha comprobado estructuralmente (integridad de ZIP, presencia de archivos y corrección `ps -a`). No se han ejecutado Docker, .NET ni npm en el equipo del usuario; el Dockerfile realiza la sincronización de npm cuando se construye en Docker con acceso al registro npm.
