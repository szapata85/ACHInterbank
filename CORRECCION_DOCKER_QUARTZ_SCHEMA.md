# ACH Interbank — Corrección permanente de Quartz SQL Server 4.7.1

**Actualizado:** 2026-11-05. Corrige el fallo de arranque `Database schema validation failed under table prefix 'QRTZ_'` detectado en `/app/crash.log` tras 172 reinicios de la API.

## Qué se corrigió

- `artifacts/sql/quartz/sqlserver-qrtz-schema.sql` se mantiene como **único script utilizado por el servicio `quartz-schema` de `docker-compose.yml`**.
- El script anterior creaba 11 tablas Quartz 3.x y pasaba sus propias comprobaciones, pero no era compatible con la dependencia `Quartz` **4.7.1** de `src/Cfa.ACHInterbank.Persistence/Cfa.ACHInterbank.Persistence.csproj`.
- Ahora crea/valida **12 tablas básicas** (incluida `QRTZ_PAUSED_JOB_GRPS`), 12 claves primarias, 4 claves foráneas y los 12 índices históricos; aplica además **26 ampliaciones de columnas obligatorias**, con tipos iguales a los de las migraciones oficiales de 4.0, 4.2, 4.3, 4.6 y 4.7.
- Todas las operaciones de creación y ampliación están protegidas por comprobaciones de existencia (`OBJECT_ID` / `COL_LENGTH`). No se ejecutan `DROP TABLE`, `DROP DATABASE` ni limpiezas de volúmenes; los datos existentes no se eliminan.
- Se añadieron comprobaciones explícitas que provocan error no cero en `quartz-schema` si falta cualquiera de esas tablas o columnas.
- No se modificó el código de negocio ni el `Quartz__JobStore__PerformSchemaValidation=true`; la validación real de Quartz sigue activa.

**Fuente técnica:** [migraciones oficiales Quartz.NET v4.7.1](https://github.com/quartznet/quartznet/tree/v4.7.1/database/migrations) y [cambios del esquema](https://www.quartz-scheduler.net/documentation/database/schema-changes.html).
Las tablas opcionales de historial (`QRTZ_EXECUTION_HISTORY`, `QRTZ_MISFIRE_HISTORY`, `QRTZ_JOB_STATUS`) no son necesarias: este proyecto no activa `UseExecutionHistory()`. Si se activa esa función en el futuro, deberán incluirse las migraciones opcionales correspondientes.

## Cómo aplicar en una instalación que YA tiene volúmenes e imágenes

1. Respaldar `src/Cfa.ACHInterbank.Api/appsettings.Development.json` existente, ya que contiene la cadena de conexión local. **No sobrescribir esa contraseña con una de ejemplo.**
2. Extraer el ZIP corregido. Si se copia sobre la carpeta anterior, asegurarse de reemplazar `artifacts/sql/quartz/sqlserver-qrtz-schema.sql` por la versión corregida.
3. Antes de modificar una base con información valiosa, realizar un respaldo SQL Server verificado.
4. Abrir CMD en la raíz del proyecto y ejecutar:

```cmd
docker-ach.cmd down
docker-ach.cmd config
docker-ach.cmd infra
docker-ach.cmd up --no-seed --no-auto-migrations --no-build
docker-ach.cmd status
curl.exe -i http://127.0.0.1:843/health/live
curl.exe -i http://127.0.0.1:843/health/ready
```

`down` **no** lleva `-v`: conserva el volumen y sus datos. `infra` ejecuta el script `quartz-schema` **antes** de la API, con las credenciales existentes y sin descargar SQL en tiempo de ejecución. `--no-build` reutiliza las imágenes compiladas; solo es apropiado si existen.

Si las imágenes no existen, usar `docker-ach.cmd up --no-seed --no-auto-migrations` (sin `--no-build`). Si es una instalación totalmente nueva, aplicar primero las migraciones EF con `docker-ach.cmd migrate`, y decidir si se requiere el seed inicial **una sola vez**.

El archivo `src/Scripts/tables_sqlServer.sql` es un script histórico que contiene capacidad destructiva; **no** ejecutarlo para reparar esta incidencia.

## Comprobación de errores

```cmd
docker logs --tail 100 achinterbank-quartz-schema-1
docker logs --tail 100 achinterbank-achinterbank-api-1
docker exec achinterbank-achinterbank-api-1 sh -c "tail -n 35 /app/crash.log"
```

No usar `docker compose down -v` ni `docker-ach.cmd reset --confirm-data-loss` para esta reparación.

## Evidencia y límites

Para repetir las comprobaciones: `python scripts/test/verify_quartz_4_7_1_sqlserver.py`. Se comprobó estáticamente la estructura SQL, la concordancia de nombres y tipos de las 26 columnas con la especificación de Quartz.NET 4.7.1, la ausencia de sentencias destructivas, las referencias de Docker Compose y la integridad del ZIP. **No se pudo ejecutar SQL Server/Docker ni validar `healthy` desde este entorno.** Es necesario verificar el arranque real en el equipo de destino.
