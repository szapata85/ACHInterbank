# SEC-I02 — Evidencia estática, Node/Nginx y lock inmutable

**Fecha:** 2026-10-27  
**Estado:** `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`  
**Paquete padre:** `ACHInterbank_ACH_CR_I06_FIX01_SEC_I01_DOTNET_COMPLETO.zip`  
**SHA256 padre:** `849a5e671c10b70c9d25c7d4be6e82a778833798113d285446d839cab745ec48`  
**Nuevo paquete:** `ACHInterbank_ACH_CR_I06_FIX01_SEC_I02_NODE_NGINX_COMPLETO.zip` (hash externo en JSON, evitar autorreferencia circular).

## 1. Archivos de código / configuración modificados

| Ruta | Cambio | Resultado |
|---|---|---|
| `web/ach-interbank-ui/Dockerfile` | Node builder `24.21.0` -> `24.22.1`; Nginx final `1.30.5` -> `1.30.6` | `PASS_STATIC` |
| `web/ach-interbank-ui/Dockerfile` | Eliminación de `npm install --package-lock-only`; usar `npm ci --include=optional` | `PASS_STATIC`, compatibilidad npm12 pendiente |
| `web/ach-interbank-ui/package.json` | Añade `security:verify-lock`, lectura sin red ni escrituras | `PASS_STATIC` |
| `web/ach-interbank-ui/scripts/verify-security-lock.mjs` | Script de verificación de versiones declaradas, root del lock, overrides y advertencia integrity | `PASS_STATIC` |
| `web/ach-interbank-ui/package-lock.json` | **Sin cambios**; no inventar integrities ni actualizar versiones sin registry | `UNCHANGED` |
| `web/ach-interbank-ui/nginx.conf` y `.dockerignore` | Sin cambios para evitar alterar las rutas y controles del proxy | `UNCHANGED` |

## 2. Limitaciones detectadas en el lockfile

- Lock v3; manifest root consistente con las **16 dependencias** y **17 dependencias de desarrollo** declaradas.
- **15** overrides explícitos presentes y coherentes con las entradas instaladas del lock según comprobación estática.
- **1131** entradas en `packages` de lock; **36** paquetes con `resolved: https://registry.npmjs.org/...` carecen de campo criptográfico `integrity`, incluyendo Angular. La ausencia impide afirmar integridad autenticada de todos los tarballs.
- No se ha editado o rellenado `integrity`, no se han generado checksums ficticios y no se ha intentado resolver paquetes remotamente; el entorno **no dispone de DNS hacia registry.npmjs.org**.
- La prueba `npm ci --dry-run --offline --ignore-scripts --no-audit --no-fund` dio salida exitosa en **Node v22.16.0 / npm v10.9.2**, sin descargar ni ejecutar la SPA. Esto **NO** equivale a una instalación bajo Node v24.22.1 / npm v12.1.0.
- El Dockerfile final conserva npm 12.1.0 declarado como `ARG NPM_VERSION`; al ejecutar con red deben verificarse compatibilidad/engines y publicación de dependencias. Si `npm ci` falla, reparar lock **en entorno de desarrollo autorizado**, versionar diff y reejecutar; no volver a mutarlo en Docker.

## 3. Validación estática y límites

- Dockerfile por etapas: Node exclusivo en builder y Nginx en runtime final; `nginx.conf` conserva upstream `achinterbank-api:8080` y rutas proxy. No certifica `nginx -t` contra la imagen real.
- No se modificaron fuentes `.cs`, contratos NACHA-M, reglas de siete ciclos ACHCOL/cinco sesiones CENIT, lógica transaccional ni Quartz.
- Etiquetas verificadas en catálogo oficial Docker Hub:
  - https://hub.docker.com/_/node — `24.22.1-alpine`
  - https://hub.docker.com/_/nginx — `1.30.6-alpine`
- Publicaciones upstream:
  - https://nodejs.org/en/blog/vulnerability/october-2026-security-releases
  - https://nginx.org/news.html
- **No se verificaron digests concretos multiarquitectura**, ni pull, ni build de imagen. Resolver digest de la arquitectura destino antes de despliegue.

## 4. Backlog ejecutable, sin bloquear siguiente incremento

```bash
cd web/ach-interbank-ui
node --version
npm --version
npm run security:verify-lock
# Resolver hashes faltantes con registro npm confiable y versionar package-lock.json.
npm ci --include=optional --no-audit --no-fund
npm run build -- --configuration production
npm audit --omit=dev
# A ejecutar donde Docker esté disponible, con tags/digest verificados
# docker compose build --pull --no-cache achinterbank-spa
# docker exec achinterbank-spa nginx -t
# docker exec achinterbank-spa nginx -v
```

Criterio de cierre EV-03: ejecutar `npm ci` real con Node24/npm12, Angular production build, `nginx -t` de imagen. EV-04: SBOM/SCA/escaneo de imagen, integrities y digests. Ambas quedan `NOT_RUN_ENVIRONMENT` hasta evidencias.

## 5. Gates y continuidad

- `GO_INCREMENT=YES`: preparar SEC-I03 aunque no exista entorno.
- `GO_DEPLOY=NO` / `GO_LIVE_MONETARY=NO` hasta revisión de seguridad, pruebas y autorizaciones.
- `ACH-CR-001`: Quartz deshabilitado, sin nuevas operaciones monetarias reales.
- Próxima microtarea: `SEC-I03-A_SBOM_INVENTORY`.
