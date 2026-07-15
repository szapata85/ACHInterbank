# Evidencia UAT - certificados NACHA Security con SQL Server

Fecha local: 2026-07-14

## Veredicto

GO para el ambiente local controlado SQL Server.

## Defectos reproducidos

1. La SPA enviaba la consulta y las cargas al endpoint simplificado `/nacha-security/certificates`, que colisionaba con el routing SPA de Nginx y devolvia `text/html`; Angular terminaba mostrando `[object Object]`.
2. El `finalize()` de la carga recargaba el catalogo tambien al fallar y ocultaba el error original.
3. La ruta hija no aplicaba efectivamente el permiso de gestion, y la API permitia material privado mediante una policy publica/heredada.
4. El modelo simplificado admitia `RawData` y `Password`; no existia proteccion verificable para el PFX gestionado.
5. El middleware registraba cuerpos y cabeceras sin omitir multipart ni redactar secretos.
6. El actor de carga quedaba como `api` porque el JWT no poblaba `User.Identity.Name`.

## Correcciones verificadas

- La SPA usa `/api/nacha-security/certificates/management/public` y `/private` segun el material.
- El endpoint simplificado de carga devuelve HTTP 410 y no persiste material.
- Los errores de texto, ProblemDetails, ValidationProblemDetails, arrays, red y estados HTTP se normalizan sin convertir objetos a texto.
- La recarga del catalogo ocurre solo despues de un upload exitoso; error, exito, doble envio, selector y password tienen estados independientes.
- `canActivateChild` exige `CanManageCertificates`; las policies de escritura privada/publica se mantienen separadas y el backend sigue siendo autoridad.
- El CER se valida como X.509 publico; el PFX se valida como PKCS#12 con llave privada, correspondencia de llave, vigencia, algoritmo, tamano y key usage.
- El PFX original y su password se protegen con ASP.NET Core Data Protection autenticado. SQL Server guarda metadatos, certificado publico, blob protegido y referencia `dbenc://`; el key ring persiste fuera de SQL Server y del repositorio en un volumen Docker dedicado.
- El middleware omite multipart/binarios y redacta password, token, authorization, cookie, secret, private key, raw data y PFX.
- El actor se resuelve desde `unique_name`, nombre o `sub`; SQL Server registra `admin`.

## Persistencia y reinicios

- Refresco de navegador: ambos certificados continuaron visibles.
- Cierre e inicio de sesion: ambos certificados continuaron visibles.
- Reinicio de API y SPA: ambos certificados continuaron visibles.
- Reinicio de SQL Server conservando volumen, seguido de API y SPA: ambos certificados continuaron visibles.
- Estado final: SQL Server, API y SPA `healthy`.

## Seguridad

- `DigitalCertificateVersions` no tiene columna `Password`.
- La tabla heredada `DigitalEnvelopeCertificates` tiene 0 filas y 0 passwords no vacios.
- El registro privado tiene `HasPrivateKey=1`, blob protegido no vacio y referencia protegida.
- La contraseña del PFX no aparece en stdout de Docker ni en los archivos de log de la API.
- No se encontro marcador PEM de llave privada en logs.
- Playwright deshabilita trace, video y screenshot automaticos para esta especificacion; la captura final se toma despues de limpiar el campo password.

## Evidencias

- `antes-object-object.png`: reproduccion previa del defecto.
- `despues-certificados.png`: catalogo persistido despues de carga y reinicios.
- `http-sanitizado.txt`: estados HTTP observados sin payloads sensibles.
- `sql-sanitizado.txt`: consulta final de metadatos y proteccion.
- `pruebas.txt`: compilaciones y suites ejecutadas.

## Riesgo residual

- El key ring local esta en un volumen Docker persistente. Para produccion externa debe protegerse con HSM, OpenBao/KMS o un protector de claves administrado y controles de backup/rotacion.
- El certificado de firma de prueba vence en 2026-09; requiere rotacion antes de esa fecha.

No se realizo commit ni push.
