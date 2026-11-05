# Informe técnico de depuración del proyecto ACHInterbank

**Fecha:** 4 de noviembre de 2026.  
**Base:** `ACHInterbank_ajustado.zip` (el original se conserva intacto).  
**Resultado:** `ACHInterbank_ajustado_LIVIANO_SEGURO.zip`.  
**Alcance:** inventario estático de las entradas del ZIP, huellas SHA-256 de documentos, revisión de estructura y referencias. No constituye una ejecución funcional, UAT ni certificación de las cámaras.

## 1. Diagnóstico de volumen

- Tamaño del ZIP original: **456.31 MiB** (478,476,129 bytes).
- Contenido sin comprimir: **1,747.24 MiB**.
- Archivos originales (sin entradas de directorio): **7,607**.
- Archivos removidos de la versión liviana: **2,517**.
- Peso descomprimido de los archivos retirados: **1,597.43 MiB**.
- Peso en ZIP original de los archivos retirados (suma de tamaños de entradas comprimidas): **395.04 MiB**.
- Archivos originales conservados sin cambio: **5,090**.
- Tamaño total de esos archivos conservados sin comprimir: **149.82 MiB**.

| Directorio generado omitido | Archivos | Sin comprimir | Comprimido en ZIP original | Regeneración |
|---|---:|---:|---:|---|
| `.angular/` | 1,578 | 902.79 MiB | 151.54 MiB | Caché de Angular (regenerable) |
| `bin/` | 689 | 612.35 MiB | 219.03 MiB | Artefactos compilados .NET (regenerables) |
| `obj/` | 164 | 62.17 MiB | 14.34 MiB | Intermedios de compilación .NET (regenerables) |
| `.vs/` | 15 | 14.01 MiB | 8.30 MiB | Caché y configuración local de Visual Studio |
| `dist/` | 71 | 6.10 MiB | 1.83 MiB | Compilación distribuible Angular (regenerable) |

**Regla:** en el ZIP liviano se retiran solo carpetas autogeneradas comprobables. No se han eliminado archivos de código fuente ni migraciones, configuraciones `.csproj`, SQL, WSDL, pruebas `*.spec.ts`, documentación ni capturas guardadas como evidencia.

## 2. Playwright y artefactos de pruebas

- Pruebas E2E de código detectadas: **73** archivos `*.spec.ts`; conservadas.
- Videos presentes: **0** (`.mp4/.webm/.mkv/.avi/.mov`).
- Entradas en `test-results`, `playwright-report` o `blob-report` presentes en el ZIP original: **0**.
- En `web/ach-interbank-ui/playwright.config.ts` se configura `video: 'retain-on-failure'`, `trace: 'retain-on-failure'` y `screenshot: 'only-on-failure'`. Si se ejecutan pruebas en otro ambiente, esos artefactos se crearán allí, pero el script de empaquetado los excluirá en futuras entregas.
- Las capturas bajo `docs/ux/evidencias`, `docs/uat` y `entrega_pruebas_funcionales_usuarios` se conservan porque pueden formar parte de auditoría/UAT.

## 3. Documentación: inventario, duplicaciones y consolidación recomendada

- Documentos y entregables inventariados en todas las rutas documentales consideradas: **1,709**.
- Archivos directamente bajo `docs/`: **1,552**, equivalentes a **61.57 MiB** sin comprimir y **35.82 MiB** en el ZIP original.
- Grupos de duplicados exactos de documentación y entregables (SHA-256, archivo >= 512 bytes): **74**, con **93** copias adicionales, equivalentes a **19.99 MiB** sin comprimir.
- **Decisión de integridad:** no se eliminan aún esos duplicados: hay manifiestos normativos y de auditoría con rutas y hashes de archivos, por lo que moverlos puede desactualizar evidencia histórica. El ahorro principal se obtiene sin tocar esos expedientes.

### Casos documentales de mayor ahorro potencial

| Documento repetido | Rutas detectadas | Exceso aproximado sin comprimir | Estado |
|---|---:|---:|---|
| Manual ACH Colombia V35 | 2 | 6.68 MiB | Conservar ambas rutas por ahora; evaluar manifiestos y referencias |
| Manual de usuario CENIT | 2 | 6.60 MiB | Conservar ambas rutas por ahora; evaluar manifiestos y referencias |
| Manual técnico NACHA-M CENIT | 2 | 1.61 MiB | Conservar ambas rutas por ahora; evaluar manifiestos y referencias |

### Ruta documental canónica

La estructura ya define referencias prioritarias en `docs/DOCUMENTATION_INDEX.md`. Mantener **fuentes regulatorias inalteradas** y un solo índice, en vez de fundir los archivos fuente de ACH Colombia, CENIT y los informes históricos.

- **Estado vigente:** `docs/CURRENT_PROJECT_STATUS.md`, `MEMORY.md`, `docs/PENDING_WORK_CLASSIFICATION.md`.
- **Normativa y fuentes verificadas:** `docs/normativa/SOURCE_MANIFEST.md`, `docs/normativa/md/`.
- **Construcción:** `docs/BUILD_AND_TROUBLESHOOTING.md`.
- **Auditoría CENIT:** `docs/auditoria-estatica-cenit/` y `docs/correcciones-cenit/`.
- **Evidencias de prueba:** `docs/uat/`, `docs/ux/evidencias/`, `entrega_pruebas_funcionales_usuarios/`.
- **Histórico técnico y decisiones:** `docs/ai/`, `docs/architecture/`, paquetes incrementales; no usar como versión normativa vigente sin verificar.

**No es prudente** unir en un solo archivo los manuales de ACH Colombia y CENIT, ni borrar reportes incrementales I01–I10 o variantes de guías de usuario sin verificar diferencias semánticas y enlaces. La clasificación se basa en estructura, inventario, hashes y documentos canónicos; no implica validación manual de todas las afirmaciones de cada uno de los ~1.500 archivos documentales.

### Próximo paso documental opcional

Para recuperar otros ~20 MiB *sin comprimir* mediante eliminación de copias exactas, primero elegir una ruta canónica por fuente, reescribir vínculos internos, regenerar los manifiestos afectados con control de versiones y conservar una copia íntegra de los expedientes auditados. Esta etapa requiere una decisión explícita sobre retención histórica y no se aplicó en la versión segura.

## 4. Salvaguardas y reconstrucción

La nueva versión conserva:

- `.sln`, proyectos C#, migraciones PostgreSQL y SQL Server, pruebas .NET y TS/Playwright.
- `package.json`, `package-lock.json`, `angular.json`, configuración de Playwright y frontend `src/`.
- Scripts de infraestructura, Docker y configuración (no se ejecutaron servicios).
- WSDL, normas NACHA-M, manuales, informes de auditoría y evidencia de UAT.

El empaquetador incorporado en `scripts/empaquetar_ACHInterbank_liviano.py` puede generar futuras entregas desde una carpeta de fuentes, evitando carpetas regenerables (`.angular`, `.vs`, `bin`, `obj`, `dist`, `node_modules`, reportes de Playwright y coberturas). No elimina nada del directorio de trabajo.

### Validaciones

- En esta elaboración se compararon los archivos originales retenidos contra sus tamaños y CRC32 antes y después de generar el ZIP (5.089 entradas idénticas; el índice documental recibe una nota adicional).
- Se verifica que el ZIP resultante pueda leerse completo mediante `ZipFile.testzip()`.
- Se verifica la presencia de código, pruebas, solución y documentación canónica.
- Se ejecutó `python scripts/validate-solution-projects.py` sobre el ZIP depurado extraído, con resultado **SOLUTION GRAPH: OK (7/7 proyectos, referencias internas correctas)**.
- El resultado pesa **aproximadamente 60,5 MiB**, reducción de **aproximadamente 86,7 %** respecto de los **456,31 MiB** originales.
- **No se ejecutó** `dotnet build`, `dotnet test`, `npm ci`, `npm run build` ni Playwright E2E: se trata de una limpieza estática. Este ambiente no tiene instalado .NET SDK 10 y la aplicación requiere dependencias y servicios que no se reconstruyeron aquí.

### Archivos de evidencia generados

- `INFORME_DEPURACION_ACHInterbank_2026-11-04.md` — este informe.
- `INVENTARIO_ARCHIVOS_RETIRADOS.csv` — ruta, categoría, tamaños y CRC32.
- `INVENTARIO_DOCUMENTACION.csv` — ruta, tipo, tamaños, SHA-256 y grupo de duplicación.
- `DUPLICADOS_DOCUMENTALES_VERIFICADOS.csv` — desglose de grupos de SHA-256 idénticos.

**Advertencia de integridad histórica:** los manifiestos de auditoría que enumeraban todos los archivos del ZIP original deben entenderse como evidencia de su corte original, no como una declaración de identidad binaria del nuevo ZIP. Los archivos que permanecen se conservaron byte a byte, exceptuando la adición de documentación de esta depuración y la anotación añadida al índice documental.
