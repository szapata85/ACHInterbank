# ACH Interbank — Centralización de logs técnicos (2026-11-05)

## Procedencia

Aplicado a `ACHInterbank_MENU_CAPACIDADES_OCULTAS_CORRECCION_V2_2026-11-05.zip` como fuente, sin sustituir los cambios de navegación o capacidades ocultas. Se limita a logging, pruebas y documentación. Los 10 cambios contra el ZIP original están inventariados en el reporte de validación entregado.

## Alcance

- NLog 6.2.2 (ya fijado en el proyecto), único archivo técnico activo: `logs/ACHInterbank.log` relativo a `AppContext.BaseDirectory` (`${basedir}`).
- `Program.cs` y `GlobalExceptionMiddleware` ya no escriben `crash.log` ni `trace.log`. Los archivos históricos que hayan existido previamente **no se eliminan automáticamente**; revisarlos y respaldarlos según la política de incidentes antes de borrarlos.
- Los eventos de dominio / auditoría financiera persistidos en base de datos NO se modifican ni quedan sujetos a esta limpieza.

## Política

| Propiedad NLog | Valor | Efecto |
|---|---|---|
| `archiveEvery` | `Day` | Iniciar un histórico cada día, cuando haya actividad que lo requiera |
| `archiveAboveSize` | `10485760` | Rotación al llegar a aproximadamente 10 MiB |
| `maxArchiveDays` | `60` | Limpiar archivos históricos con más de 60 días (aproximación técnica a 2 meses; no equivale a 2 meses calendario exactos) |
| `maxArchiveFiles` | `30` | Conservar como máximo 30 archivos históricos; bajo carga pueden durar menos de 60 días |
| `AsyncWrapper.queueLimit` | `10000` | Máximo de eventos en memoria pendientes de escritura |
| `AsyncWrapper.overflowAction` | `Block` | No descartar errores al saturarse; puede bloquear brevemente si el disco no da abasto |

El archivo activo no está sujeto a eliminación por edad: se rota por fecha/tamaño. La capacidad máxima aproximada por instancia es del orden de 310 MiB (30 archivos de 10 MiB más el activo), excluyendo eventos individuales mayores al umbral y otros archivos ajenos a NLog. En entornos con réplicas, cada instancia debe tener almacenamiento separado; no compartir un archivo físico entre instancias.

## Ejecución y contenedores

- Docker Compose principal: montaje `./.runtime/logs:/app/logs` para preservar los logs técnicos tras recrear el contenedor. La carpeta `.runtime/` se ignora por Git.
- Docker conserva sus propios registros de infraestructura/arranque; para el contenedor API se añade conductor `local` con límites de tamaño y cantidad de archivos. No sustituye la auditoría de negocio.
- Al ejecutar fuera de Docker, ubicar el registro bajo `bin/<Configuration>/net10.0/logs/ACHInterbank.log` o el directorio base de publicación de la API.
- Si no se puede escribir en `logs`, NLog reportará advertencias internas en consola; **no** generará un archivo secundario. Asegurar permisos de escritura y supervisar espacio disponible.
- `NLog.config` se copia al directorio de salida mediante el proyecto API.

## Comportamiento del middleware

- Excepción real: un solo evento estructurado `Error` con excepción/stack trace, `IncidentId`, `TraceId`, `RuleId` y método HTTP. El middleware no hace I/O de archivos por solicitud ni registra cuerpos, query strings ni rutas con identificadores.
- Se conserva el contrato JSON/HTTP 500, incluido el mensaje de `NachaGenerationException` existente. Verificar que los mensajes originados por terceros nunca contengan secretos.
- Cancelación originada en la desconexión del cliente: no produce un falso error 500. Respuesta ya iniciada: se registra y se vuelve a lanzar la excepción para que el servidor cierre la conexión en lugar de intentar cambiar los encabezados.
- Advertencia: las excepciones completas pueden contener datos en sus mensajes; restringir acceso a los archivos y aplicar saneamiento en los puntos de origen si fuera necesario.

## Verificación antes del despliegue

1. `python scripts/validate-solution-projects.py`
2. `dotnet build ACHInterbank.sln -c Release`
3. `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~GlobalExceptionMiddlewareTests|FullyQualifiedName~NLogConfigurationTests"`
4. Ejecutar la API en ambiente local con permisos de escritura y simular un fallo de prueba aislado; comprobar una sola entrada con stack trace en `ACHInterbank.log` y ausencia de escritura en `crash.log`/`trace.log`.
5. Simular rotación con configuración temporal **solo en pruebas**, forzando tamaño y antigüedad reducidos; comprobar limpieza de históricos y restaurar los valores productivos.
6. Verificar parada normal, reinicio, límite de disco y saturación de cola antes de producción.

**Limitación de este cambio**: no reescribe ficheros Docker Compose de clúster con múltiples réplicas ni modifica librerías no relacionadas. Se requiere configurar almacenamiento/logging por instancia para esos despliegues.
