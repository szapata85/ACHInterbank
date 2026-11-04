# F06 — Corrección NACHA-M CENIT (HC-CENIT-010, HC-CENIT-011)

**Fecha:** 2026-11-02. **Base inmediata:** ZIP F05 `df34d0c4c6f2bee8c5099657cdae9272da1f861beaff42bb71879dbd80e84ed0` (4.644 entradas, CRC correcto). **Fase 6/8 = 75 % de hitos entregados**, sin homologación. Los hallazgos 010/011: **IMPLEMENTADOS — PENDIENTES DE VERIFICACIÓN**, no cerrados.

## HC-CENIT-010 — Repertorio de bytes NACHA-M CENIT

- **Causa raíz / ruta anterior:** `NachaPhysicalParser.ParseAsync`, `IsAcceptedByte` (~líneas 41,84–92 F05) aceptaba 0xD1/0xF1 (Ñ/ñ) para todas las cámaras; `NachaParserService.ParseAndSaveDetailedAsync` (~línea 101 F05) llamaba a ese parser sin aportar identidad de cámara.
- **Norma:** Manual NACHA-M CENIT 07-may-2026 §4.1 (lista de caracteres admitidos y prohibición de controles); tabla 3 `ERR_INV_PATTERN`. Referencia I10: `../auditoria-estatica-cenit/HALLAZGOS_CONSOLIDADOS_CENIT.md` §HC-CENIT-010. La expresión introductoria de §4.1 y su tabla presentan matices; conservamos los ASCII expresamente listados, incluidas minúsculas y puntuación, y rechazamos Ñ/ñ, que no aparecen.
- **Cambio:** `NachaPhysicalParser.ParseAsync` ahora recibe un indicador de perfil CENIT derivado de la autoridad publicada o del código de perfil conocido (ordinario, Return In, ROR). Solo en CENIT se rechazan bytes 0xD1/0xF1 y se emite diagnóstico físico CENIT (se menciona `ERR_INV_PATTERN` del manual para caracteres inválidos). Se conservan el formato de 106 bytes y validaciones BOM, CR/LF, bloques y cabecera; ACH Colombia mantiene aceptación LATIN1 previa.
- **Ruta productiva:** Ingesta API/servicio → `IncomingNachaIngestionAppService` → `NachaParserService.ParseAndSaveDetailedAsync` → resolución de snapshot/perfil → `NachaPhysicalParser.ParseAsync` → validaciones y persistencia. La revisión física ocurre **antes** de escribir transacciones.
- **Pruebas:** `NachaPhysicalParserTests.cs`: 12 casos fuente nuevos: Ñ, ñ, tilde/control, cinco caracteres válidos, tres errores de encuadre; prueba ACH Colombia Ñ/ñ original conservada. Sin ejecución .NET, pendiente PT-11 E2E.

## HC-CENIT-011 — STA separado de validación NACHA-M

- **Causa raíz / ruta anterior:** `NachaInboundValidation.cs` (varios `ThrowRegulatory("D02"/"D04"/"D05"/"D06")` para relleno, conteos, hash y lotes) y `NachaParserService` (D01, D02, D06) acudían al diccionario `AchFileRejectionCode`, que define D01–D06 según **Anexo B STA 28-nov-2023**, ajeno a la validación NACHA-M.
- **Norma:** Anexo B DSP-152 2023 = rechazo por servicio de transferencia STA; Manual NACHA-M CENIT 07-may-2026 §4.3, §7.3.2 y Anexo 2 tablas 2–3 = errores fatales/formales del intercambio NACHA-M. No trasladar Dxx al parser NACHA-M.
- **Cambio mínimo:** `NachaInboundValidator.CreateContext` determina `IsCenitNachaProfile` (incluye snapshot publicado, ordinario, Return In y ROR), y `SelectNachaErrorDomain` configura el validador antes de validaciones de secuencia, control y DB. El mensaje de error estructural CENIT ahora usa **`CENIT-NACHA-FATAL` como clasificación técnica interna**, no un nuevo código oficial del operador, sin consultar Dxx STA. Para una falla transaccional formal, continúa `NachaValidationFailure` por entrada, sin convertirla en rechazo global de archivo. Los Dxx y descripciones del seeder STA **se conservan intactos**; el comportamiento genérico ACH Colombia conserva compatibilidad anterior.
- **Ruta productiva:** `NachaParserService.ParseAndSaveDetailedAsync` → `NachaInboundValidator.CreateContext/SelectNachaErrorDomain` → parser físico → `ValidateEntrySequencePolicyAsync`, `ValidateBatchSequenceAndControlConsistency`, `ValidateFileControlConsistency` → fallas registradas/manejo existente; no se sustituye la respuesta oficial de operador por código propio.
- **Pruebas:** `NachaInboundValidationSeparationTests.cs`: 13 escenarios nuevos (5 causales Dxx aisladas, duplicación real, ACH original, 4 perfiles, perfil desconocido y error formal individual). Pendiente PT-12 con archivos NACHA y STA del operador.

## Evidencia ejecutada y pendiente

- **Archivos productivos modificados (3):** `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaPhysicalParser.cs`, `NachaParserService.cs`, `NachaInboundValidation.cs`.
- **Pruebas existentes ajustadas (2):** `tests/Cfa.ACHInterbank.Tests/NachaPhysicalParserTests.cs`, `NachaInboundValidationSeparationTests.cs`. **Nuevos escenarios fuente:** 25 (12+13), 0 archivos xUnit nuevos.
- **Ejecutadas realmente:** 24/24 controles estáticos F06; F01 12/12, F02 13/13, F03 17/17, F04 21/21, F05 26/26 (113/113 en conjunto); parche aplicable/reversible en seco en 5 fuentes; comparación SHA-256 y CRC posterior. **0 xUnit ejecutadas, 0 aprobadas, 0 fallidas ejecutadas** por SDK .NET ausente.
- **Regresión ACH Colombia:** rutas específicas inalteradas y prueba histórica Ñ/ñ preservada; se mantiene deliberadamente la compatibilidad del parser fuera de perfil CENIT. **Pruebas funcionales pendientes**. No se modifica contrato WSDL, migración, CUD, API ni esquema DB.
- **Riesgo:** una identidad de perfil CENIT sin snapshot publicado ni código de perfil conocido debe confirmarse en F07; no interpretar el catálogo local de errores como respuesta certificada CENIT. Confirmar con muestra física oficial si existe particularidad de codificación y verificar rutas STA y `ERR_*` del operador en integración.
- **Reversión:** `F06_CAMBIOS.patch` (`patch -R -p1` contra F06); alternativamente restaurar íntegramente ZIP F05. No se borran fuentes previas.

## Siguiente fase

F07: integración transversal, 13 hallazgos, regresión ACH Colombia, idempotencia, conciliación, pruebas dinámicas que permita el entorno. Luego F08: dictamen y entregable consolidado; no declarar homologación sin pruebas oficiales.
