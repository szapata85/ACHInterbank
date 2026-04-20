# Estado actual ACHInterbank vs ACH Colombia V32 / CENIT (rebaseline)

**Fecha de auditoría:** 2026-04-20 (UTC)  
**Fase:** Auditoría / diagnóstico (sin implementación)  
**Rama inspeccionada local:** `ACH-Interbank-Postgresql` (working tree local)  

---

## 1) Resumen ejecutivo

1. El repositorio sí evidencia madurez en el **núcleo NACHA-M** (records 1/5/6/7/8/9, mapping y batch number), con cobertura fuerte y artefactos de cierre técnico previos en `docs/audits`.  
2. Se encontró cobertura de **naming técnico básico** para exportación (`NACHA_<cycleId>_<timestamp>.txt` y `originCode.cycle.1` para CENIT), incluyendo una normalización del identificador de header para CENIT desde nombre de archivo.  
3. **No se encontró** una política integral de **nombre externo regulatorio** (entrada/salida/devolución/rechazo/reverso/prenotificación), ni validador dedicado de nombre externo, ni correlación completa nombre externo ↔ contenido ↔ conteos regulatorios de detalle.  
4. La detección de duplicados en ingesta existe, pero es por **hash+tamaño**, no por nombre externo.  
5. El baseline de pruebas documentado muestra filtros NACHA verdes, pero suite completa con deuda legacy; el entorno local documentado sigue bloqueado para PostgreSQL real por ausencia de Docker y para Karma por ChromeHeadless.

**Conclusión ejecutiva:**
- Núcleo NACHA-M: **alto avance**.
- Cumplimiento integral filename/nombre externo ACH V32 + CENIT/STA: **brecha crítica**.

---

## 2) Estado actual validado del repositorio inspeccionado

### Hallazgos de estructura y baseline
- `AGENTS.md` define stack y reglas de arquitectura (Clean Architecture, EF Code First, etc.).
- Existe setup Codex (`scripts/codex/setup-codex-env.sh`) y docker compose de pruebas (`docker-compose.test.yml`) con `.env.test.example`.
- Existe documento de estado de ambiente y pruebas (`docs/dev/codex-test-environment.md`) con trazabilidad de resultados y bloqueos.

### Hallazgos funcionales relevantes para auditoría filename
- Hay estrategias por cámara con `BuildFileName`:
  - ACH: `ACH_yyyyMMdd_CC_HHmmss.txt`.
  - CENIT: `OriginCode.Ciclo.1`.
- En `NachaExportController` hay naming y normalización CENIT del identificador de header (posición 36) según secuencia derivada del nombre.
- En ingesta, el nombre se persiste, pero la duplicidad usa hash/tamaño.
- En parser y resolver de ciclo, el nombre se usa principalmente para inferir ciclo/cámara, no para validar política regulatoria de nombre externo.

---

## 3) Cruce ACH Colombia V32 (estado observable en código)

> Nota de alcance: no se encontraron en el repositorio los PDFs normativos completos de ACH Colombia V32 enero 2025; los puntos abajo se clasifican por evidencia de código/tests y por ausencia de documento primario en repo.

| Punto ACH V32 | Estado | Evidencia |
|---|---|---|
| Formato NACHA-M y records 1/5/6/7/8/9 | Implementado y probado parcialmente (según evidencias de tests + cierres internos) | `NachaFileBuilder`, tests de mapping/builder y cierres en `docs/audits` |
| Longitud fija 106, block count, file/batch controls | Implementado y probado parcialmente | Builder y parser incluyen validaciones/control records |
| Registro 1 / FileIdModifier | Implementado | `FileIdModifier` en builder/parser + validaciones CENIT |
| Correlación nombre externo ↔ Registro 1 | Parcial (solo caso CENIT puntual en export) | Normalización posición 36 en export CENIT |
| Política completa de nombre externo ACH por tipo de archivo | **No encontrada** | No hay servicio/política dedicada de filename regulatorio ACH V32 |
| Secuencia externa diaria (tabla alfanumérica completa, límites diarios) | **No encontrada** | Solo mapping secuencia->identificador para CENIT en export |
| Nombre externo para entrada/salida/devolución/rechazo/reverso/prenotificación | **No encontrado** (solo convenciones parciales de export y returns) | No hay matriz por flujo/cámara/tipo |
| Reglas PSE específicas en naming externo | Requiere documento normativo adicional + no evidencia en código | Sin regla explícita en código/test para naming PSE |
| Correlación nombre externo ↔ conteos reales declarados | **No encontrada** | No existe validador filename con conteos regulatorios |

---

## 4) Cruce CENIT (incluyendo STA) — estado observable

> Nota de alcance: en repo hay referencias funcionales CENIT y SOAP, pero no se encontró el manual operativo CENIT DSP-152 Anexo 2/STA como fuente primaria completa para validar cada regla textual.

| Punto CENIT | Estado | Evidencia |
|---|---|---|
| Diferenciación de cámara CENIT en lógica de ciclo/operación | Implementado parcialmente | `Cenit...` services y políticas de ciclos |
| Naming salida CENIT (`origin.cycle.1`) | Implementado parcialmente | `CenitClearingHouseStrategy`, `NachaExportController` |
| Correlación nombre CENIT ↔ FileIdModifier (header pos 36) | Implementado parcialmente (solo export) | `NormalizeFileHeaderIdentifierForCenitAsync` |
| Validación nombre entrante CENIT contra regla externa completa | **No encontrada** | Resolver solo infiere ciclo/cámara por nombre |
| D05 por discrepancia conteos declarados en nombre vs archivo real | **No encontrada** | D05 existente se usa para controles/hash, no para nombre externo |
| Duplicado por nombre externo | **No encontrado** | Duplicidad por hash/tamaño |
| STA (transferencia de archivos) con reglas de nombre externas | **Requiere documento adicional STA y no evidencia de implementación específica** | No hay módulo explícito `STANamePolicy` / `IncomingFileNameValidator` |

---

## 5) Auditoría específica de nombres de archivo (obligatoria)

### 5.1 Preguntas de control (respuesta corta)

1. **¿Existe servicio de generación de nombres de archivo?**  
   **Sí, parcial** (estrategias por cámara y helper en controller de export).

2. **¿Existe servicio de validación de nombres de archivo?**  
   **No, integral no.** Solo validación puntual regex para nombre CENIT en export.

3. **¿Existe política diferenciada por cámara ACH/CENIT?**  
   **Sí, parcial** (formato de nombre distinto por cámara), pero no política regulatoria completa por flujo/tipo.

4. **¿Existe correlación nombre externo vs Registro 1 (FileIdModifier/campo 7)?**  
   **Sí, parcial y solo para CENIT en export**, reemplazando posición 36 del primer registro.

5. **¿Existe correlación nombre externo vs número real de registros?**  
   **No encontrada.**

6. **¿Existe control de duplicidad por nombre externo?**  
   **No encontrado.** Duplicidad actual es por hash/tamaño.

7. **¿Existen pruebas unitarias/integración/E2E para nombres?**  
   **Unitarias parciales sí** (naming de estrategia/controller). **Integración/E2E regulatoria integral no encontrada.**

8. **¿Cobertura por tipo de archivo entrada/salida/devolución/rechazo/reverso/prenotificación?**  
   **No encontrada como matriz de naming externo regulatorio.**

9. **¿Está cubierto CENIT STA?**  
   **No evidenciado en alcance filename.**

10. **¿Está cubierto ACH V32 en nombres externos?**  
   **No evidenciado integralmente.**

### 5.2 Evidencias técnicas concretas

- Generación de nombres:
  - `AchClearingHouseStrategy.BuildFileName` y `CenitClearingHouseStrategy.BuildFileName`.
  - `NachaExportController.BuildNachaFileName` con branch ACH/CENIT.
- Validación parcial:
  - `ResolveDailySequenceFromFileName` exige patrón CENIT `^[^.]+\.(\d{3})\.1$` (solo export path).
- Correlación con header:
  - `ReplaceHeaderPosition36` ajusta FileIdModifier en la primera línea para CENIT.
- Persistencia/auditoría de nombre generado:
  - `AchFileExport` + `AchFileExportAuditService.RecordGeneratedFileAsync`.
- Ingesta/duplicados:
  - `IncomingNachaIngestionAppService` marca duplicado por hash+tamaño; no por nombre.
- Uso de nombre en parser/resolución:
  - extracción de ciclo desde nombre en parser y cycle resolver.

### 5.3 Clasificación de brecha filename

- **Brecha crítica:** Falta de política integral de nombre externo regulatorio ACH/CENIT/STA con validación bidireccional (nombre↔contenido) y controles de duplicidad por nombre.
- **Brecha alta:** No hay pruebas dedicadas por flujo (entrada/salida/devolución/rechazo/reverso/prenotificación) para nombre externo.
- **Brecha alta:** No hay evidencia de validación de conteo de registros declarado en nombre externo vs conteo real del archivo.

---

## 6) Estado actual de pruebas (según evidencia de repo/docs)

### 6.1 Núcleo NACHA/Mapping/Batch
- Existe evidencia documental de cierre positivo del filtro amplio NACHA/Mapping/BatchNumber en `docs/dev/codex-test-environment.md`.
- También existen pruebas específicas de estrategia/naming CENIT y export controller.

### 6.2 Bloques adicionales (backfill/admin/parser/addendas/returns/golden)
- Existen suites dedicadas en `tests/` para backfill/admin, parser fatal, preproducción, returns, addendas, etc.
- El estado “suite completa no verde” está documentado como deuda legacy en el doc de ambiente.

### 6.3 Filename tests
- **Sí hay tests parciales**:
  - `NachaSemanticAndStrategyTests.CenitStrategy_UsesRegulatoryFileNamingConvention`.
  - `NachaExportControllerTests.ExportEncrypted_UsesCenitNamingAndIdentifierNormalization`.
- **No hay tests integrales regulatorios de nombre externo** (validación de conteos, duplicidad por nombre, matriz por tipo de archivo).

### 6.4 SOAP/PostgreSQL/SPA test status
- SOAP: hay unit tests y orquestador con mocks, no E2E real contra endpoint/bd integrada.
- PostgreSQL: preparado por compose/env, sin evidencia de ejecución real en entorno codex documentado.
- SPA: build OK documentado; Karma/ChromeHeadless con bloqueo de entorno documentado.

---

## 7) Estado PostgreSQL integration harness

| Criterio | Estado |
|---|---|
| `docker-compose.test.yml` presente | Sí |
| `.env.test.example` presente | Sí |
| script setup codex presente | Sí |
| migraciones EF Postgres presentes | Parcial (snapshot presente; no se verificó corrida real en este entorno) |
| migración explícita BatchNumberSequence trazada en docs | Sí (documentada) |
| pruebas con PostgreSQL real en este entorno | No evidenciadas |
| uso dominante en tests actuales | SQLite/InMemory |
| Testcontainers | No encontrado |
| estrategia de DB limpia por corrida | Parcial (SQLite in-memory en suites) |
| seed reproducible | Parcial (sí en varias suites, pero no harness Postgres ejecutado aquí) |

**Madurez clasificada:** **Preparado pero no ejecutado / bloqueado por ambiente** (Docker no disponible según evidencia documental del entorno).

---

## 8) Estado E2E SOAP

| Ítem | Estado |
|---|---|
| Proc_Contrapartidas | Parcial (catálogo/mapping/cliente, sin E2E real demostrado) |
| Proc_Transacciones | Parcial (mapper/parser/orquestador con mocks) |
| Fake/mock SOAP | Sí (mocks unitarios) |
| Servidor SOAP local | No encontrado |
| Request payload tests | Sí (mapper/build soap body) |
| Response success/error tests | Sí (parser y orquestador con mocks) |
| Update BD post-SOAP | Parcial (orquestador persiste cola/ejecución) |
| Idempotencia | Parcial (IdempotencyDispatchKey en cola) |
| Reintentos | Sí, en orquestador |
| Trazabilidad | Parcial (IncomingNachaIntegrationExecution) |
| Correlación archivo/transacción/SOAP E2E completa | No demostrada |

**Clasificación:** **Parcial (no E2E completo)**.

---

## 9) Estado SPA/Admin

| Capacidad | Estado |
|---|---|
| NACHA Config (layouts/definitions) | Parcialmente alineado (módulos presentes) |
| Mapping engine administración | Parcial |
| Batch number policy | No encontrado explícito en UI |
| Shadow compare | No encontrado explícito en UI |
| Trazabilidad de generación | Parcial (reporting y export views) |
| Nombres de archivo (política externa) | No alineado/no encontrado |
| Archivos generados/recibidos con compliance naming | Parcial (export/download), no compliance integral |
| Estados SOAP | Parcial (settings y backend), no tablero E2E completo evidenciado |

---

## 10) Matriz de brechas priorizada

| Área | Requisito ACH V32/CENIT | Evidencia en código | Evidencia en tests | Estado | Severidad | Recomendación | Próximo prompt sugerido |
|---|---|---|---|---|---|---|---|
| Records NACHA 1/5/6/7/8/9 | Construcción/validación estructural | Sí | Sí | Implementado/probado parcial | Medio | Mantener regresión y UAT formal | "Ejecuta auditoría de regresión NACHA con dataset regulatorio externo" |
| Mapping engine | Mapping común + fallback/shadow | Sí | Sí | Implementado/probado parcial | Medio | Mantener hardening | "Evalúa gaps de mapping dinámico por cámara y flujo" |
| Batch number | Secuencia por scope + concurrencia | Sí | Sí | Implementado/probado parcial | Medio | Validar en Postgres real | "Corre pruebas batch-number sobre PostgreSQL real" |
| File naming / nombre externo | Política integral por cámara+flujo | Parcial | Parcial | **No completo** | **Crítico** | Diseñar `FileNamePolicy` + matriz normativa | "Diseña ADR de política de nombre externo ACH/CENIT/STA" |
| Registro 1 vs nombre externo | Correlación FileIdModifier / identificador | Parcial (CENIT export) | Parcial | Parcial | Alto | Formalizar regla bidireccional | "Define validaciones nombre externo vs R1 por cámara" |
| Conteo en nombre vs archivo | D05 por conteos declarados | No encontrado | No encontrado | Brecha | Crítico | Crear validador de conteo declarado | "Diseña validador de conteos en nombre externo" |
| Parser entrada | Validaciones fatales/estructura | Sí | Sí | Implementado parcial | Medio | Extender a reglas de naming externo | "Extiende parser plan para validar nombre externo" |
| Devoluciones | Generación y reglas | Sí | Sí | Implementado parcial | Medio | Añadir naming externo regulatorio | "Diseña naming returns/rechazos/reversos" |
| Prenotificaciones | Lógica operativa | Sí | Sí | Parcial | Medio | Incluir naming y recepción dedicada | "Matriz prenotificación: contenido + filename" |
| Reversos | Lógica operativa | Sí | Sí | Parcial | Medio | Incluir naming específico | "Reglas de reverso: archivo + nombre + correlación" |
| Rechazos | Manejo funcional | Parcial | Parcial | Parcial | Alto | Formalizar flujo y nombre externo | "Diseña flujo de rechazo con convenciones filename" |
| Duplicidad archivo | Duplicado por nombre externo | No | No | Brecha | Alto | Guard de duplicidad por external name + fecha/cámara | "Diseña DuplicateFileNameGuard y estrategia de persistencia" |
| PostgreSQL harness | Integración real en pruebas | Parcial | Parcial | Preparado no ejecutado | Alto | Ejecutar harness en ambiente con Docker | "Ejecuta harness Postgres e informa resultados" |
| SOAP Proc_Contrapartidas | E2E real | Parcial | Parcial | Parcial | Alto | Definir entorno fake SOAP + pruebas contrato | "Diseña plan E2E Proc_Contrapartidas" |
| SOAP Proc_Transacciones | E2E real | Parcial | Parcial | Parcial | Alto | Igual anterior + idempotencia E2E | "Diseña plan E2E Proc_Transacciones" |
| SPA/Admin | Gestión enterprise integral | Parcial | Parcial | Parcial | Medio | roadmap UI para compliance/ops | "Audita UX de operación para trazabilidad filename/SOAP" |
| Observabilidad/auditoría | Trazabilidad end-to-end | Parcial | Parcial | Parcial | Medio | agregar correlación filename externo y reglas evaluadas | "Diseña esquema de auditoría de decisiones de filename" |

---

## 11) Recomendación de próximos prompts

1. **Prompt de diseño (sin código):** ADR de `ExternalFileNamePolicy` ACH/CENIT/STA con matriz por tipo de archivo y campos normativos.
2. **Prompt de especificación de validación:** `IncomingFileNameValidator` + `OutgoingFileNameBuilder` + correlación con R1/FileIdModifier y conteos de detalle.
3. **Prompt de estrategia de persistencia/auditoría:** almacenar `ExternalFileName`, `ExternalSequence`, `DeclaredDetailCount`, `ValidatedDetailCount`, `ValidationResult`, `ValidationEvidenceJson`.
4. **Prompt de pruebas:** plan de pruebas unitarias/integración para nombre externo (entrada/salida/returns/reject/reversal/prenote + duplicados).
5. **Prompt de infraestructura:** ejecución controlada de harness PostgreSQL en ambiente con Docker disponible.
6. **Prompt E2E SOAP:** plan incremental con servidor SOAP fake + escenarios success/functional reject/technical error/idempotencia.

---

## 12) Limitaciones y transparencia de evidencia

- No se implementaron cambios funcionales.
- No se ejecutó harness PostgreSQL en esta fase.
- No se ejecutó E2E SOAP real en esta fase.
- No se encontraron en repo los documentos normativos primarios completos (ACH V32 enero 2025, CENIT DSP-152 Anexo 2/STA) para validación textual exhaustiva; por tanto, varias reglas quedaron en estado “requiere documento normativo adicional”.

---

## 13) Veredicto de esta auditoría

- **PostgreSQL harness:** listo para ejecutar en ambiente con Docker, con brechas conocidas de baseline.
- **E2E SOAP:** no listo como cobertura completa; existe base parcial.
- **Filename/nombre externo:** no cubierto integralmente; iniciar diseño formal es prioritario y bloqueante para cumplimiento completo ACH V32/CENIT.
