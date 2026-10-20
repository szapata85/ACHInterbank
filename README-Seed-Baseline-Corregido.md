# ACHInterbank NAV6 — Corrección del seed de contratos baseline-v1

## Diagnóstico

El log del 19/10/2026 confirmó que el arranque falla dentro de `IntegrationCatalogBootstrapper.EnsureAsync` al insertar parámetros de `IntegrationContractVersions` Active creados por la migración `20261012231100_IntegrationsAddContractVersioningBaseline`.

El mensaje es: `No se pueden agregar parámetros a una versión contractual Active o Retired.`

## Corrección

El bootstrapper puede completar por **única vez** los parámetros de los tres contratos `baseline-v1` históricos cuando aún no tienen registros en `IntegrationMethodParameters`.

El permiso es específico para los tres GUID de baseline definidos en la migración, solo si `SourceType` es `BaselineMigration`, el contrato está `Active`, el seed está ejecutándose, y no hay ningún parámetro persistido en ese contrato. Fuera de ese caso se conservan las validaciones de inmutabilidad para versiones Active/Retired, incluyendo ediciones, eliminaciones y adiciones posteriores.

**Este parche no modifica el esquema ni genera migraciones SQL Server.**

## Aplicación recomendada — CMD, sin PowerShell

1. Haz una copia de seguridad de los tres archivos originales.
2. Extrae `ACHInterbank-NAV6-Parche-Seed-Contratos.zip` sobre la carpeta raíz existente `ACHInterbank-NAV6`, preservando su estructura de rutas.
3. **Conserva la migración local** `20261019032612_SyncSqlServerModel` y el `appsettings.Development.json` donde tienes la contraseña. No reemplaces la carpeta completa por la versión ZIP si deseas conservar los cambios locales.
4. Ejecuta desde CMD:

```bat
docker-ach.cmd up --seed --no-auto-migrations
docker-ach.cmd status
```

**No uses `--no-build` la primera vez** después del parche: la API debe recompilarse para incluir el cambio de C#. No necesitas volver a ejecutar `docker-ach.cmd migrate` porque según tus logs las migraciones ya están aplicadas. Tampoco ejecutes `reset` ni `down -v`.

Si aún queda `unhealthy`, consulta:

```bat
docker logs --since 10m --tail 200 achinterbank-achinterbank-api-1
docker inspect --format "{{json .State.Health}}" achinterbank-achinterbank-api-1
```

## Limitación de verificación

Se validaron estáticamente los cambios y el contenido del ZIP. El entorno de construcción no dispone de Docker ni del SDK .NET; por eso todavía es necesaria la prueba compilada y funcional en tu equipo.

## Archivo completo de referencia

Se entrega además `ACHInterbank-NAV6-Seed-Baseline-Corregido.zip` basado en el ZIP anterior. Contiene el BAT y el Compose, pero **no contiene la migración `SyncSqlServerModel` generada posteriormente en tu equipo ni tu contraseña**, y por eso se recomienda aplicar solamente el parche sobre tu código actual.
