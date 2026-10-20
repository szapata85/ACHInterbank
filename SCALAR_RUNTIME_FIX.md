# Scalar / OpenAPI runtime fix — BUILD-FIXED-11

## Síntoma

Scalar carga su interfaz en `/scalar/`, pero muestra:

`Document 'v1' could not be loaded`

La interfaz de Scalar no genera el contrato. Consume el documento publicado por ASP.NET Core en `/openapi/v1.json`.

## Hallazgo en el source

El proyecto contiene enriquecedores OpenAPI personalizados. Dos de ellos tenían comportamiento *fail-hard* en tiempo de ejecución:

- `OpenApiExamplesTransformer` podía lanzar `InvalidOperationException` si un provider de ejemplo no podía materializarse o serializarse.
- `OpenApiResponseDescriptionsTransformer` lanzaba `InvalidOperationException` si una anotación `OpenApiResponseDescriptionAttribute` apuntaba a un status que ApiExplorer no había publicado.

Ese diseño convierte una inconsistencia documental localizada en una caída completa de `/openapi/v1.json`, y Scalar solo puede mostrar el mensaje genérico de que `v1` no pudo cargarse.

El tracker del propio repositorio deja registrado que las unidades que introdujeron esos transformers quedaron con validación ejecutable pendiente porque el entorno de implementación no disponía de `dotnet`.

## Cambio aplicado

1. Los dos enriquecedores pasan a ser *fail-soft* en runtime:
   - conservan el contrato real producido por ApiExplorer;
   - registran warning con controller/action/status;
   - omiten únicamente el enrichment inválido;
   - nunca derriban el documento OpenAPI completo por metadata accesoria.
2. `GlobalExceptionMiddleware` registra `exception.ToString()` completo para preservar toda la cadena de inner exceptions.
3. En Development se agregó `/openapi/_diagnostics`, que fuerza generación + serialización del documento `v1` y devuelve el error completo si todavía existe un fallo fuera de los dos transformers.
4. `scripts/check-scalar-openapi.ps1` consulta automáticamente `/openapi/_diagnostics` cuando `/openapi/v1.json` no responde 2xx.
5. Las pruebas de los dos transformers se alinearon con el criterio de disponibilidad: metadata de documentación inválida no puede tumbar el documento publicado.

## Verificación local recomendada

Con la API levantada en el perfil HTTPS de Visual Studio:

```powershell
Invoke-WebRequest https://localhost:7269/openapi/v1.json -SkipCertificateCheck
Invoke-WebRequest https://localhost:7269/openapi/_diagnostics -SkipCertificateCheck
.\scripts\check-scalar-openapi.ps1 -BaseUrl "https://localhost:7269"
```

Si `/openapi/v1.json` devuelve 200 y JSON válido, Scalar debe poder consumirlo en `/scalar/`.

Si `/openapi/_diagnostics` devuelve 500, su body contiene la excepción real de generación/serialización y permite identificar el siguiente componente sin depender del mensaje genérico de Scalar.
