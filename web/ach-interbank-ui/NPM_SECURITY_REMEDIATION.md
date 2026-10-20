# Seguridad de dependencias del SPA (18 de octubre de 2026)

## Alcance y cambios incluidos en el ZIP

Proyecto: `web/ach-interbank-ui` (Angular 21.2.x). Se partió del `npm audit` proporcionado por el solicitante: 32 hallazgos (2 moderados, 28 altos, 2 críticos).

1. Se eliminó **Puppeteer** de `devDependencies`, porque la única referencia a su API en este SPA estaba en `karma.conf.js` para buscar el ejecutable de Chromium. Karma ahora reutiliza el Chromium de `@playwright/test` (si está instalado) o el navegador Chrome encontrado normalmente por `karma-chrome-launcher`, respetando `CHROME_BIN`.
2. Se regeneró **offline** `package-lock.json` tras esta eliminación: 1191 -> 1131 entradas; desaparecieron `puppeteer`, `puppeteer-core`, `@puppeteer/browsers`, `basic-ftp` y `extract-zip` (y otras dependencias exclusivas de esa cadena).
3. La tarea `build-and-test` del workflow Angular instala Chromium mediante Playwright antes de las pruebas de Karma, para no depender de un navegador incidentalmente preinstalado en GitHub Actions.
4. Se corrigió `npm run security:refresh-lock` para Windows: ejecuta npm a través de `process.execPath` + `npm_execpath` (en lugar de llamar `npm.cmd` directamente). Resuelve **todas las dependencias incluyendo las opcionales**; comprueba el resultado con `npm ci --dry-run` y restaura los archivos si algún paso falla. Se necesita acceso al registro npm y no se usa `--force`.

## Completar la remediación en un equipo con conexión a npm

En el directorio `web/ach-interbank-ui`, con Node 24 (igual que CI), usando una terminal **CMD** con conexión al registro npm:

```cmd
npm run security:refresh-lock
npm ci
npm run build
npm test -- --watch=false --browsers=ChromeHeadless
npm audit --omit=dev --audit-level=low
npm audit --audit-level=low
```

El script crea `overrides` para solucionar dependencias transitivas cuyo rango de origen bloquea la corrección:

| Dependencia | Rango vulnerable observado | Objetivo |
|---|---|---|
| `@modelcontextprotocol/sdk` | `1.12.0 - 1.30.1` | `1.31.0` |
| `proxy-addr` | `1.1.0 - 2.0.7` | `2.0.8` |
| `shell-quote` | `1.8.4 - 1.10.0` | `1.11.0` |

Además ejecuta `npm audit fix --package-lock-only` (SIN `--force`), que debe resolver las actualizaciones compatibles para `brace-expansion` (tanto rama 1.x como 5.x), `compression`, `fast-uri`, `ip-address`, `js-yaml`, `nanoid`, `postcss-selector-parser`, `source-map-js` y otras que procedan. **El script termina con éxito cuando npm ha generado un lockfile consistente**, incluso si `npm audit fix` devuelve código 1 debido a avisos sin parche; las vulnerabilidades pendientes se muestran al ejecutar `npm audit` por separado. Si falla la resolución/consistencia, restaura los archivos anteriores. Nunca se declara la auditoría aprobada sin ejecutar `npm audit`. Se debe inspeccionar el lockfile generado y registrar los resultados reales.

### Pendientes sin corrección publicada al revisar las fuentes

- `braces` <= 3.0.3 (`GHSA-vfj7-8cjw-p6xm`): sin versión corregida. Lo utilizan Karma, `micromatch` y `chokidar` en herramientas de desarrollo.
- `http-cache-semantics` <= 4.2.0 (`GHSA-ch52-4w7c-c8xp`): sin versión corregida; depende de él `make-fetch-happen`.
- `extract-zip` <= 2.0.1 (`GHSA-jmr9-qjv8-65gv`, `GHSA-7pqw-9j4j-h8q3`): sin versión corregida; se eliminó la cadena conocida mediante la sustitución de Puppeteer. Verificar el árbol final tras actualizar.

Estos pendientes **no están aceptados ni exentos de CI**. La validación de vulnerabilidades de desarrollo del workflow sigue siendo estricta y puede fallar hasta que se publique una corrección o se apruebe expresamente una sustitución del componente afectado. Una mitigación de exposición en herramientas de desarrollo no equivale a eliminar el hallazgo.

### Restricción de validación de este paquete

En el entorno en que se preparó el ZIP no hubo resolución DNS de `registry.npmjs.org`, tampoco caché de paquetes ni `node_modules` instalado. Por tanto, se pudo regenerar el lockfile tras **eliminar Puppeteer** y comprobar que el manifiesto coincide con la raíz del lockfile, pero **NO** se pudo instalar Angular, compilar, ejecutar pruebas ni verificar `npm audit` después de actualizar las versiones de terceros. Se deja la regeneración segura del lockfile pendiente del equipo con red; no se falsificaron valores `integrity` ni se manipularon versiones directamente dentro del lock. `npm ls --package-lock-only --all` todavía informa un conflicto transitorio `make-dir`/`less` (`ELSPROBLEMS`), observado también en el ZIP original (aunque con otra ruta). **Además, npm 11 informa entradas opcionales faltantes para `make-dir@5.1.0`, `probe-image-size@7.4.0`, `lodash.merge@4.6.2`, `needle@2.9.1` y dependencias secundarias.** El script corregido debe resolverlas usando el registro antes de dar `npm ci` por validado. No se falsificaron entradas ni integridades del lockfile.

Fuentes de seguridad: [MCP SDK GHSA-6qxp-vccf-f47h](https://github.com/advisories/GHSA-6qxp-vccf-f47h), [proxy-addr GHSA-jqcg-44mw-7w3h](https://github.com/advisories/GHSA-jqcg-44mw-7w3h), [shell-quote GHSA-pqg4-j6r4-53mv](https://github.com/advisories/GHSA-pqg4-j6r4-53mv), [braces GHSA-vfj7-8cjw-p6xm](https://github.com/advisories/GHSA-vfj7-8cjw-p6xm), [http-cache-semantics GHSA-ch52-4w7c-c8xp](https://github.com/advisories/GHSA-ch52-4w7c-c8xp).
