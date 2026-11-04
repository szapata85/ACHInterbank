# SEC-I03 — Runbook de validación externa y criterios GO_DEPLOY

**Estado:** `WRITTEN_NOT_EXECUTED`. Ejecutar exclusivamente en CI/UAT autorizado con red, SDK .NET y daemon Docker habilitado. Cada ejecución requiere fecha, operador, hash de artefacto, imagen/digest y reporte no alterable.

## A. Regenerar inventario fuente (no requiere red)

```bash
python scripts/security/generate_sec_i03_inventory.py --root . --out docs/security/SEC-2026-10-BROTLI-HARDENING
python scripts/security/test_sec_i03_inventory.py
```

Comparar hashes SHA-256 contra `SEC-I03_VALIDACION_ESTATICA.json`. Los archivos fuente son inventarios, NO escaneos CVE ni resultados del runtime.

## B. npm / Angular (requiere Node24/npm12 y registry aprobado)

```bash
cd web/ach-interbank-ui
node --version
npm --version
npm run security:verify-lock
npm ci --include=optional --no-fund
npm ls --all --json > npm-tree-runtime.json
npm audit --json > npm-audit.json
npm run build -- --configuration production
```

Confirmar tarballs y sus integrities contra el registro aprobado. Investigar las 36 entradas sin `integrity`: evaluar causa e incorporar hashes **únicamente** mediante resolución confiable/revisada; no rellenarlos manualmente ni usar `npm audit fix --force`. Revisar overrides y licencias; documentar falsos positivos y riesgos aceptados con responsable y vencimiento. `npm audit` puede retornar exit code distinto de cero por hallazgos: conservar siempre stdout/stderr, código de salida y versión del CLI.

Escaneo alternativo usando OSV-Scanner 2 en entorno autorizado:

```bash
osv-scanner scan source --lockfile=web/ach-interbank-ui/package-lock.json
```

## C. NuGet (requiere .NET 10.0.403 y fuente de paquetes confiable)

```bash
dotnet --info
dotnet restore ACHInterbank.sln
dotnet build ACHInterbank.sln -c Release --no-restore
dotnet test ACHInterbank.sln -c Release --no-build
dotnet list ACHInterbank.sln package --include-transitive
dotnet list ACHInterbank.sln package --vulnerable --include-transitive
```

La solución real debe verificarse por nombre/ruta antes de ejecutar. Generar y revisar `packages.lock.json` para todos los proyectos y aplicar `--locked-mode` **solo después** de validarlos y versionarlos. Regenerar un SBOM completo del grafo restaurado mediante herramienta institucional compatible con CycloneDX/SPDX, no publicar el SBOM **directo** como si fuera transitorio. Evaluar seguridad, licencias y compatibilidad de proveedores EF/ADO.NET.

## D. Imágenes (requiere Docker, acceso a registries y scanner institucional)

```bash
# Tras construir y etiquetar las imágenes finales del ambiente de prueba:
docker image inspect <IMAGE_TAG> --format '{{json .RepoDigests}}'
trivy image --format json --output api-image-scan.json <API_IMAGE_TAG>
trivy image --format json --output spa-image-scan.json <SPA_IMAGE_TAG>
trivy image --format cyclonedx --output api-image.cdx.json <API_IMAGE_TAG>
trivy image --format cyclonedx --output spa-image.cdx.json <SPA_IMAGE_TAG>
```

Sustituir `<...>` por nombres verificados, no comandos literales listos para ejecutar. Auditar además build stages y servicios auxiliares (`debian:bookworm-slim`, `mssql/server:2025-latest`) según el alcance desplegado. Registrar digest **por plataforma**, origen, firma/attestation cuando esté disponible, CVE/OS packages y excepciones. Fijar imágenes por digest validado en una microtarea posterior con revisión de compatibilidad, no asumir SHA sin pull real.

## E. Matriz de resolución de hallazgos

| Campo mínimo | Requisito |
|---|---|
| Identificación | CVE/advisory, componente, versión/digest, ruta/origen y evidencia |
| Exposición | Control de datos no confiables, servicio afectado, alcance de runtime |
| Severidad/prioridad | CVSS + criticidad ACH/CENIT, explotabilidad confirmada versus potencial |
| Tratamiento | parche, mitigación, excepción firmada con caducidad y compensación |
| Retest | artefacto/ambiente, fecha, ejecutor y resultado reproducible |

**Gate final:** los SBOM de este incremento son material de preparación. `GO_DEPLOY=NO` hasta completar las comprobaciones ejecutables pertinentes, escaneo SCA aprobado, controles de identidad, regresión de pagos, autorización de Seguridad/QA/Operaciones y rollback. `GO_LIVE_MONETARY` requiere además homologación correspondiente. **No bloquear SEC-I04 por indisponibilidad de ambiente.**

Referencias: https://cyclonedx.org/specification/overview/ · https://google.github.io/osv-scanner/usage/scan-source · https://www.trivy.dev/docs/latest/guide/configuration/reporting/
