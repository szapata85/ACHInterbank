> **Nota actual (2026-11-19):** Este documento describe un corte anterior. El archivo `appsettings.Development.json` ya no se distribuye. Configure el unico `src/Cfa.ACHInterbank.Api/appsettings.json` y consulte `docs/deployment/CONFIGURACION_UNIFICADA_AUDITORIA.md`. No copie credenciales de paquetes historicos.

# Documentación unificada — ACHInterbank

**Corte de depuración:** 2026-11-16. **Perfil:** fuentes y documentos Markdown, sin PDF/Word ni capturas.

Esta guía consolida el **punto de entrada y la ubicación canónica** de documentos útiles. No reemplaza los manuales oficiales: una compilación de la normativa en texto puede perder diagramas, tablas y diseño original.

## Lectura recomendada, en este orden

1. `README.md` — requisitos, estructura y arranque del proyecto.
2. `LEER_PRIMERO.md` — última secuencia técnica de hotfix y limitaciones del corte NPT.
3. `docs/CURRENT_PROJECT_STATUS.md` y `docs/PENDING_WORK_CLASSIFICATION.md` — estado y pendientes; distinguir cambios estáticos de validación ejecutada.
4. `MEMORY.md` y `AGENTS.md` — decisiones e invariantes que debe respetar el desarrollo.
5. `docs/BUILD_AND_TROUBLESHOOTING.md` — comandos y problemas de compilación.
6. `docs/npt-map-01/MATRIZ_TRAZABILIDAD_66_CAMPOS.md` y `docs/npt-map-01/entrega/CIERRE_NPT_MAP_01_1.md` — mapeos SOAP y riesgos RA-01/RA-02.
7. `docs/DOCUMENTATION_INDEX.md` — índice temático extenso del repositorio.

## Fuentes normativas (mantener separadas por cámara)

| Tema | Fuente en Markdown preservada | Fuente original PDF |
|---|---|---|
| ACH Colombia, versión 35 (abril de 2026) | `docs/normativa/md/ACH-Colombia-V35.md` | Excluida del ZIP liviano; disponible en el ZIP original y en Sources |
| CENIT, Reglamento / operación | `docs/normativa/md/CENIT-DSP-152-Anexo-2.md` | No se aportó PDF del Anexo 2 dentro de este ZIP |
| CENIT, devoluciones y devoluciones de devolución | `docs/normativa/md/CENIT-Anexo-A-Causales-Devolucion.md` | No se aportó PDF separado dentro de este ZIP |
| CENIT, rechazo de archivos | `docs/normativa/md/CENIT-Anexo-B-Causales-Rechazo.md` | No se aportó PDF separado dentro de este ZIP |
| CENIT, formato NACHA-M (7 mayo 2026) | `docs/normativa/md/Archivos de CENIT/Manual de Especificaciones Formato NACHA-M CENIT.md` | Excluida del ZIP liviano |
| CENIT, manual de operación (13 junio 2025) | `docs/normativa/md/Archivos de CENIT/Manual de Usuario CENIT.md` | Excluida del ZIP liviano |
| ACH Colombia, referencia de ciclos | Resumen y matriz en documentación del proyecto | Excluida del ZIP liviano |

Usar `docs/normativa/SOURCE_MANIFEST.md` para verificar los SHA-256 de las fuentes textuales. `ACH-Colombia-V32.md` y `ceos_dsp_152_MAY_27_2022.md` se preservan solo como historia normativa; no asumir vigencia actual. **ACH Colombia y CENIT son cámaras con reglas distintas: no mezclar horarios, causales ni formatos sin verificar el texto específico.**

## Código y dependencias conservados íntegramente

| Ruta | Función |
|---|---|
| `ACHInterbank.sln`, `global.json`, `Directory.Build.props` | Grafo de solución y versión .NET |
| `src/` | API, dominio, aplicación, persistencia EF Core y transporte externo |
| `src/Cfa.ACHInterbank.Persistence.Migrations.SqlServer/` | Migraciones SQL Server separadas; **no son duplicados descartables** |
| `tests/` | Tests .NET, fixtures y contratos |
| `web/ach-interbank-ui/src`, `web/ach-interbank-ui/e2e` | Frontend Angular y pruebas E2E |
| `web/ach-interbank-ui/package.json`, `package-lock.json` | Dependencias reproducibles de Angular |
| `database/`, `scripts/`, `tools/`, `docker/`, `docker-compose*.yml` | SQL, diagnóstico, automatización y despliegue local |
| `.github/` | Gates de CI del proyecto |

## Documentación de arquitectura y operación retenida

- `docs/architecture/`, `docs/adr/`, `docs/api/` — arquitectura y contratos de integración.
- `docs/operations/`, `docs/returns/`, `docs/security/` — operación, devoluciones y controles.
- `docs/uat/`, `docs/go-live-readiness/`, `docs/audits/` — preparación de UAT, riesgos y auditorías (evidencias voluminosas/temporales retiradas según perfil).
- `docs/npt-map-01/entrega/` — informes y plan del paquete NPT remapeados desde la raíz del ZIP original.
- `docs/uat/fixtures-historicas/` — seis fixtures pequeños NACHA/XML útiles de corte I12/I13, separados de los logs y baselines descartados.
- `entrega_pruebas_funcionales_usuarios/paquete_final/` — matrices finales y notas operativas, sin manual Word ni capturas .

## Riesgos y qué **no** se valida con esta depuración

- Estado del proyecto de origen: **NO-GO SOAP Live**, pendiente homologación de fuentes y comprobación de pruebas finales.
- `OFEMP`, `OFIDARCH`, `OFDIRECCIONIP` no se deben sustituir por valores inventados. `NCTAORIG` requiere cuenta originadora real, diferente de `NCTARECEP`.
- Para el corte recibido, hay documentación que explícitamente indica ausencia de ejecución de `dotnet build` y de xUnit. Una comprobación estática no sustituye build, pruebas .NET, UAT o certificación bancaria.
- Se conserva el contenido byte a byte de todos los archivos originales de código/configuración/pruebas, salvo archivos de respaldo `.orig` y diagnósticos regenerables.
- Hay una cadena SQL local **no vacía** en `appsettings.Development.json`, con parámetro de contraseña; auditar y cambiar credenciales antes de compartir el paquete. No se expone aquí su valor.
- Las evidencias y sus manifiestos SHA-256 históricos hacen referencia a otro conjunto de archivos: **no reinterpretarlos como un hash del nuevo ZIP**. El original debe guardarse como expediente para auditoría.

## Qué se eliminó o centralizó

- PDFs fuente, copias literales y guías Word de versiones anteriores: centralización de una copia canónica por manual en el perfil con normativa.
- Baselines TSV/SHA, logs, parches de auditoría, pruebas y exportaciones regenerables.
- Capturas duplicadas y versiones preliminares de manuales, matrices y formatos.
- Copias idénticas de Markdown verificadas por SHA-256, con preferencia por `docs/normativa/md/`.

El inventario completo de exclusiones y reubicaciones está en `docs/depuracion_2026-11-16/`. **No se borró ningún archivo del ZIP original.**
