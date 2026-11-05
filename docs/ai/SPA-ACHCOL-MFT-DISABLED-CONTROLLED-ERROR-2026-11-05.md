# ACH Colombia — Error de integración MFT deshabilitada (API + SPA)

**Fecha:** 2026-11-05  
**Base:** `ACHInterbank_FILE_EXCHANGE_UX_MATERIAL_BRANDING_2026-11-05.zip`  
**Alcance:** `POST /api/ach-colombia/file-exchange/inbound/execute` y dos rutas relacionadas que exponen el mismo estado (`outbound/execute`, `transfers/{id}/retry`).  
**Estado:** código implementado; verificaciones estáticas satisfactorias; compilación .NET, pruebas xUnit, Angular y Docker/UAT pendientes.

## Evidencia que origina la corrección

Durante las solicitudes observadas el 5 de noviembre de 2026 a las 20:44:56Z, 20:48:32Z y 20:48:55Z, `/app/crash.log` registró `System.InvalidOperationException: ACHCOL_MFT_DISABLED`. La pila apunta a `AchColombiaManagedMftInboundWorkflow.ExecuteAsync`, antes de seleccionar el adaptador de entrada y de recoger archivos. La excepción generaba HTTP 500. Los logs también indicaron que el origen CORS `http://127.0.0.1:743` no estaba permitido; ese aviso es **independiente** y no explica esta excepción.

La captura del administrador muestra la casilla «Integración habilitada» seleccionada; no demuestra por sí sola que la configuración se haya guardado y esté vigente para la API. No se volvió a ejecutar el intercambio ni se hicieron modificaciones en la base de datos.

## Comportamiento implementado

La condición de negocio conocida **y solo esa** se captura en `AchColombiaFileExchangeController` para recepción manual, envío manual y reintento. La respuesta HTTP es `409 Conflict` con `ProblemDetails`:

```json
{
  "title": "Intercambio de archivos no disponible",
  "status": 409,
  "detail": "La integración de archivos de ACH Colombia está deshabilitada. Solicita a un administrador revisar su configuración.",
  "code": "ACHCOL_MFT_DISABLED"
}
```

El cuerpo no muestra stack traces, rutas de almacenamiento, nombres de clases, conexiones ni valores secretos. El código estable permite al SPA traducir con seguridad la situación sin usar el mensaje interno de .NET. Cualquier otra `InvalidOperationException` **no** se captura en el controlador y sigue siendo fallo inesperado para diagnóstico; no se camufla como error operativo.

En `/ach-colombia/file-exchange`, el componente diferencia *únicamente* `(HTTP 409, code = ACHCOL_MFT_DISABLED)`, presenta una tarjeta Angular Material (`role=alert`) en español, con colores semánticos del branding dinámico, y ofrece acceso a `/administracion/mft-ach` cuando el usuario dispone de permisos. Los demás conflictos/500 conservan un aviso genérico seguro que no expone datos técnicos.

## Integridad del procesamiento

Se preservaron los contratos de `IAchColombiaManagedMftExecutionService`, `IAchColombiaManagedMftTransferOperationsService`, las rutas, cuerpos, permisos, clave de idempotencia, deduplicación, nombres de archivo, calendario y código de ingesta. No se modificaron servicios de persistencia, adaptadores ni motores de recepción o envío. No se habilitaron automáticamente operaciones de intercambio, servicios reales ni conexiones financieras.

## Pruebas incorporadas

- `AchColombiaManagedMftCompositionTests`: respuesta 409 y código de negocio para recepción, envío y reintento; excepción inesperada continúa propagándose.
- `AchColombiaFileExchangeComponent.spec.ts`: alerta humanizada, enlace a configuración y tratamiento genérico de otros 409/500 sin mostrar mensajes técnicos.
- `OpenApiDocumentGenerationTests`: respuestas 409 documentadas con `ProblemDetails` en los tres endpoints.
- `scripts/validate-mft-disabled-response-static.py`: 22 controles estáticos (22/22 PASS).
- TypeScript de componente y pruebas analizado sin errores sintácticos mediante el compilador TypeScript global. **Esto no equivale a build Angular.**

## Validación pendiente en equipo con Docker

1. Reconstruir API **y** SPA (no pasar `--no-build`):
   ```cmd
   docker-ach.cmd down
   docker-ach.cmd config
   docker-ach.cmd infra
   docker-ach.cmd up --no-seed --no-auto-migrations
   docker-ach.cmd status
   ```
2. Con un perfil de pruebas **sin habilitar intercambio real**, comprobar el `409` y la alerta contextual; evitar activar envíos o recepciones reales en esta prueba.
3. Confirmar los permisos para el acceso a la configuración y que la API no produce un nuevo incidente `UNHANDLED` para esa condición.
4. En la configuración ya activada del usuario, comprobar que **se guardó** y ejecutar «Probar conexión» en administración; no iniciar recepción reiteradamente sin revisar su estado.
5. Verificar que otros errores no se enmascaran (fallas reales permanecen 500) y que la navegación y branding se comportan correctamente.
6. Validar con `dotnet build ACHInterbank.sln -c Release`, `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release`, `npm ci` y build/pruebas focales Angular en entorno con SDK y dependencias disponibles.

**Limitaciones:** no se dispone en este entorno de `dotnet` ni de dependencias Angular instaladas. No se ejecutaron compilaciones .NET, pruebas xUnit/Jasmine, UAT del navegador ni Docker. El código está modificado y validado estáticamente, no desplegado.
