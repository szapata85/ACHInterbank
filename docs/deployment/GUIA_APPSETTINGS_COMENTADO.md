# Uso del appsettings comentado en ACHInterbank

**Fuente única:** `src/Cfa.ACHInterbank.Api/appsettings.json`. Cada propiedad tiene un comentario inmediatamente anterior. Estos comentarios son documentación para operadores; **no modifican la semántica** de ASP.NET Core ni sustituyen validación de los valores.

## Formato aceptado

La API y EF Core (en tiempo de ejecución y diseño) usan proveedores/lectores que toleran comentarios. Por compatibilidad con los publicadores y validadores, mantenga los comentarios exclusivamente en **líneas completas** que comienzan por `//`. No use `//` al final del valor ni `/* ... */` en este archivo. Evite pasar el JSON a formateadores de JSON estricto: podrían fallar o borrar los comentarios.

```jsonc
"Database": {
  // Proveedor de base de datos usado por la API, EF Core y Quartz persistente.
  "Provider": "SqlServer",
  // Aplica migraciones al arrancar la aplicación (solo cuando esté aprobado).
  "ApplyMigrations": true,
  // Inserta datos semilla al iniciar; se mantiene apagado para producción.
  "ApplySeed": false
}
```

## Publicación IIS, Visual Studio y Docker

- **IIS:** `scripts/iis/publish-iis.ps1` habilita `SpaHosting:Enabled` en el JSON **publicado**, conservando los comentarios. No agrega variables de entorno de configuración.
- **Visual Studio:** `scripts/configure-visualstudio-development.ps1` actualiza solamente `Database` y la cadena elegida en `ConnectionStrings` sin reserializar el archivo.
- **Docker:** el ejecutor `tools/AchInterbank.DockerCli` reconoce los comentarios `//`. Docker Compose sigue teniendo sus propios sobrescritos de infraestructura para contenedores.
- **EF Core:** `AchDbContextFactory` ya permite comentarios al leer las cadenas de conexión en tiempo de diseño.

## Antes de operar con una contraparte real

1. Configurar SQL Server y certificados/cadenas/secretos mediante mecanismos protegidos; restringir acceso NTFS al `appsettings.json` publicado. Evitar distribuir claves en Git y ZIP.
2. Revisar `DigitalEnvelope`, `SoapEndpointSecurity`, `SoapTransport`, endpoints WCF y `SoapIntegrationBootstrap`, especialmente dominios `example.com` y destinos HTTP privados heredados.
3. **Atención:** `SoapEnvelopeLogging:Enabled=true` registra el XML SOAP completo sin anonimizar según `SoapEnvelopeJournal`; evaluar ponerlo en `false` en producción. Verificar políticas de firma, cadena del firmante y retrocompatibilidad de certificados; revisar rutas MFT y permisos. Configurar `appSettings:prueba` con un entero positivo (timeout WCF en minutos) antes de invocar el servicio heredado.
4. Aplicar migraciones de modo controlado y revisar `Quartz:JobStore`, política de misfires y jobs recuperados antes de habilitar operación con terceros.
5. Validar que el simulador NACHA permanezca apagado y que no haya retransmisiones no idempotentes.

El documento `CONFIGURACION_UNIFICADA_AUDITORIA.md` reúne decisiones, riesgos y resultados de la revisión. `INVENTARIO_CLAVES_APPSETTINGS.csv` conserva la clasificación estática inicial de cada ruta.
