# SEC-I03 — Evidencia de implementación estática | 2026-10-27

**Iniciativa:** SEC-2026-10 (CR-SEC-001). **Resultado:** `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`.
**ZIP padre:** `ACHInterbank_ACH_CR_I06_FIX01_SEC_I02_NODE_NGINX_COMPLETO.zip`.
**SHA-256 ZIP padre:** `c88f3116fffc77e330be2866c08f77e9ce206e22564bb55a9d3bef5b3382223c`.

## Alcance realmente implementado

- `scripts/security/generate_sec_i03_inventory.py`: inventario offline, **determinista y de solo lectura** de fuentes (`package.json`, `package-lock.json`, todos los `.csproj`, y todos los Dockerfiles), sin realizar auditoría en línea ni mutar dependencias.
- `scripts/security/test_sec_i03_inventory.py`: 3 pruebas offline de resolución npm, alcance explícitamente incompleto y reproducibilidad.
- `SEC-I03_INVENTARIO_FUENTE.json`: evidencia fuente estructurada con versiones, rutas, overrides, SHA-256 del lock, manifiestos NuGet, imágenes y cobertura.
- `SEC-I03_NPM_LOCK.cdx.json`: **SBOM CycloneDX 1.6 parcial**, creado de `package-lock.json` v3. Conserva versiones exactas de los 1.130 nodos, rutas para versiones duplicadas, las integridades sha512 declaradas y aristas de dependencias resolubles. No comprueba instalación ni autenticidad de los tarballs.
- `SEC-I03_NUGET_DECLARADO.cdx.json`: **SBOM CycloneDX 1.6 parcial**, exclusivamente `PackageReference` directos de los `.csproj`. Sin `dotnet restore`, no existe un grafo transitorio actual fiable y se **excluyeron** expresamente los `obj/project.assets.json` heredados por potencial desactualización tras SEC-I01.
- `SEC-I03_VALIDACION_ESTATICA.json`: conteos y hashes reproducibles de los tres artefactos principales.
- `SEC-I03_RUNBOOK_VERIFICACION_EXTERNA.md`: instrucciones de restauración, SCA, licencias, imágenes y aceptación para entornos autorizados.
- `PLAN_IMPLEMENTACION.md`, `MEMORY_STATE.md`, `MEMORY.md`: continuidad actualizada para SEC-I04.

## Resultados observados (sin red, sin restore ni Docker)

| Control | Evidencia | Estado |
|---|---|---|
| npm lock v3 | 1.130 entradas con nombre y versión | `PASS_STATIC` |
| npm `integrity` | 1.094 con sha512 declarado; **36 sin campo `integrity`** | `PARTIAL_SOURCE_EVIDENCE` |
| npm paquetes directos | 0 referencias directas sin resolver en el lock | `PASS_STATIC` |
| NuGet | 8 `.csproj`, **56** referencias directas, **51** componentes/versiones únicos declarados | `PASS_STATIC` |
| NuGet transitivo | Sin `packages.lock.json`; 7 `project.assets.json` heredados **no usados** | `NOT_RUN_ENVIRONMENT` |
| Imágenes externas `FROM` | **6** declaradas en 4 Dockerfiles; 6 **sin digest fijo** | `PASS_STATIC` (inventario) |
| OS y Docker image layers | No hay pull/daemon/scanner para determinar paquetes de sistema y CVE reales | `NOT_RUN_ENVIRONMENT` |
| Auditoría remota de vulnerabilidades/licencias | No se consultó ninguna base de advisories, ni registro npm ni NuGet en este incremento | `NOT_RUN_ENVIRONMENT` |
| Pruebas unitarias del inventariador | 3/3 tests Python, ejecutadas en entorno offline | `PASS_STATIC` |
| Regeneración determinista | Dos ejecuciones independientes produjeron archivos byte a byte idénticos | `PASS_STATIC` |
| Release/UAT/ACH monetario | No se ejecutaron | `NOT_RUN_ENVIRONMENT` |

**Advertencias de interpretación:** la ausencia de `integrity` no demuestra manipulación ni una CVE; un digest de lock **no prueba** que el tarball descargado cumpla ese digest hasta ejecutar `npm ci` contra origen confiable. Todas las 6 imágenes carecen de fijación por digest; esto debe tratarse antes de producción después de validar la arquitectura del despliegue. `CycloneDX` se entrega como SBOM **parcial**, con `compositions.aggregate=incomplete`, nunca como certificado completo de producción. Las dependencias transitivas NuGet, componentes de SO, licencias, severidades y rutas explotables no han sido comprobadas.

## Comandos realmente ejecutados

```bash
python scripts/security/generate_sec_i03_inventory.py \
  --root . \
  --out docs/security/SEC-2026-10-BROTLI-HARDENING
python scripts/security/test_sec_i03_inventory.py
```

Se ejecutaron sobre una copia de los **manifiestos y los `project.assets.json` heredados** extraídos del ZIP SEC-I02, sin instalar dependencias. Los artefactos versionados dentro del nuevo ZIP proceden de esa ejecución. El escaneo con runtime real queda pendiente.

## Rutas de imágenes registradas (ninguna inspeccionada en runtime)

1. `debian:bookworm-slim` — herramienta SFTP auxiliar.
2. `mcr.microsoft.com/mssql/server:2025-latest` — esquema Quartz auxiliar, tag mutable.
3. `mcr.microsoft.com/dotnet/aspnet:10.0.14` — base runtime API.
4. `mcr.microsoft.com/dotnet/sdk:10.0.403` — builder API.
5. `node:24.22.1-alpine` — builder Angular.
6. `nginx:1.30.6-alpine` — runtime de SPA.

## Gates y backlog

- `GO_INCREMENT=YES`: se inicia SEC-I04 aunque permanezcan pendientes las verificaciones en línea.
- `GO_DEPLOY=NO`; `GO_LIVE_MONETARY=NO`; Quartz ACH-CR-001 deshabilitado.
- `EV-04`: ahora **`PARTIAL_SOURCE_EVIDENCE`**, pero SCA en línea, licencias, NuGet transitivo, SBOM de imágenes, verificación de integridad de tarballs y digests siguen `NOT_RUN_ENVIRONMENT`.
- Pendientes nuevos `EV-10`: resolver y verificar 36 integridades faltantes con registry confiable, registrar cambios revisados al lock, sin hashes fabricados.
- Pendientes nuevos `EV-11`: fijar 6 imágenes por digest verificado de arquitectura y ejecutar escaneo OS/layers antes de desplegar (coordinar con SEC-I05).
- Próxima microtarea: `SEC-I04-A_CREDENTIALS_SEED_INVENTORY`, **sin divulgar contraseñas ni hashes**.

El paquete mantiene íntegros los Dockerfiles actualizados en SEC-I01/02 y no modifica `.cs`, `.ts`, servicios financieros, reglas NACHA-M, ciclos, Quartz ni condiciones de compensación.
