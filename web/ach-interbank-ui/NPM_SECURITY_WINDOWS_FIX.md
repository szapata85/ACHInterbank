# Recuperación de npm audit en Windows (SPA Angular)

Esta revisión corrige el script anterior que iniciaba `npm.cmd` con `spawnSync(..., shell:false)` en Windows y no mostraba la causa del fallo. El **lockfile de la entrega previa es incompleto** y no se debe usar directamente con `npm ci` hasta regenerarlo desde un registro npm accesible.

## CMD — pasos en la carpeta `web\ach-interbank-ui`

```cmd
node -v
npm -v
npm ping
npm run security:refresh-lock
npm ci
npm run build
npm audit
```

- Usa Node 24, como en CI. Comprueba que `npm ping` se conecta con tu registro npm configurado. Si tu empresa usa un proxy o registro privado, configura su acceso autorizado previamente.
- El comando `security:refresh-lock` invoca el CLI de npm a través de Node, ejecuta `npm install --package-lock-only --include=optional` para resolver paquetes que faltan (por ejemplo `make-dir@5.1.0` y `probe-image-size@7.4.0`), aplica los parches semver compatibles sin `--force`, vuelve a conciliar el lock y prueba `npm ci --dry-run` antes de terminar.
- El script solo sale con éxito si npm generó un lockfile y la validación de `npm ci --dry-run` terminó sin error. Los avisos de `npm audit` sin corrección disponible se revisan **después** y no se ocultan.
- Si `npm ping` falla, no se pueden descargar metadatos ni los paquetes nuevos. Verifica conexión, proxy corporativo, certificado o registro configurado; evita inventar hashes/integridades o borrar entradas del lockfile.
- No ejecutes `npm audit fix --force`. Una instalación fallida explica el error `Could not find ... @angular-devkit/build-angular:browser` al compilar.

## Si continúan errores

Ejecuta en CMD y comparte la salida de:

```cmd
npm config get registry
npm ping
node -v
npm -v
npm run security:refresh-lock
```

El script conserva el mensaje concreto de npm, en lugar de devolver únicamente `could not regenerate the lockfile`.

**Estado de la entrega:** Se comprobó la sintaxis del script y su restauración de archivos ante un fallo de npm offline; sin acceso al registro no fue posible completar la instalación, la compilación ni certificar una auditoría sin vulnerabilidades.
