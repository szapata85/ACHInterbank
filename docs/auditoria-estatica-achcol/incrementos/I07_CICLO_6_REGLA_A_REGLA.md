# I07 — auditoría estática ACH Colombia, ciclo 6 (política siete ciclos)

**Corte:** 2026-11-01 · **Estado:** I07 TERMINADO como inspección estática; **C6 NO certificado**. Alcance: reglas del ciclo 6, devoluciones C2–C5, exclusión C1, operaciones y ventanas de MFT. No hay cambios de código de producción, pruebas, migraciones o arquitectura.

## 1. Fuente y comportamiento esperado

- Fuente primaria de la revisión: *Nuevos Ciclos de Proceso y Horarios.pdf*, **página 3, fila 6**: envío de archivos **14:31 a 16:00**, cierre **16:00**, entrega de planillas **16:00–16:30**, pago de compensación **16:30–16:45**, liberación de archivos **16:45–17:00** (hora de Colombia).
- **Operaciones enumeradas:** devoluciones débito/crédito monetarias y prenotificaciones de los ciclos **2, 3, 4 y 5 del día n**; nuevas operaciones **débito, crédito y prenotificación de débito y crédito**; **PSE, SSS y DIAN**. La fila **no** autoriza en C6 devoluciones de C1 del mismo día, de ciclos del día n-1, ni del propio C6.
- La definición en código `ACHCOL-7C-20261014` tiene vigencia declarada a partir del **14-10-2026**. Su instalación/uso en un ambiente real y ratificación externa no fueron verificados; el *Manual V35* contiene esquema histórico de cinco ciclos (**H02**). Se respeta el texto de cada fuente sin reconciliar silenciosamente discrepancias.
- Las devoluciones R10 pueden tener **condiciones adicionales por causal y `MaxCycles`**; una combinación permitida en la ventana general C6 **no garantiza** que cualquier R10 sea aceptable.

## 2. Revisión regla a regla — 19 controles

Rutas de código relativas a `ACHInterbank_ajustado/`. `TRAZADA` = se encontró un control/definición estático relacionado, no certificación de su ejecución en ambiente ACH Colombia.

| ID | Regla C6 | Código / prueba localizado | Resultado | Observación |
|---|---|---|---|---|
| AC-C6-01 | Versión ACHCOL siete ciclos vigente para fecha operativa | `src/Cfa.ACHInterbank.Application/ACH/Models/RegulatoryCycleScheduleCatalog.cs:152-158,239-267`; `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ClearingHouseCyclePolicyResolver.cs:35-66` | TRAZADA | H02: despliegue real no verificado |
| AC-C6-02 | Recepción C6 14:31–16:00; cierre y corte 16:00 | `RegulatoryCycleScheduleCatalog.cs:224-229,295-316`; `tests/Cfa.ACHInterbank.Tests/RegulatoryCycleScheduleCatalogTests.cs:27-54` | TRAZADA | Definición y aserción de tiempo; no ejecución Quartz |
| AC-C6-03 | Planillas C6 16:00–16:30 | `RegulatoryCycleScheduleCatalog.cs:224-229,310-315`; `ClearingHouseCyclePolicyResolver.cs:129-165` | TRAZADA | Se valida orden de etapas, no recepción de planilla bancaria |
| AC-C6-04 | Compensación/pago C6 16:30–16:45 | `RegulatoryCycleScheduleCatalog.cs:224-229`; `ClearingHouseCyclePolicyResolver.cs:150-165` | PARCIAL | Horario configurado; confirmación real de compensación/pago pendiente H03 |
| AC-C6-05 | Liberación de archivos C6 16:45–17:00 | `RegulatoryCycleScheduleCatalog.cs:224-229`; `src/Cfa.ACHInterbank.Persistence/ACH/Quartz/Jobs/Implementation/AchColombiaManagedMftHandlers.cs:261-315` | PARCIAL | Polling validado por ventana; barrera con pago conciliado no demostrada H03 |
| AC-C6-06 | Crédito monetario ordinario permitido en C6 | `RegulatoryCycleScheduleCatalog.cs:28-44,224-229`; `src/Cfa.ACHInterbank.Application/ACH/Services/CycleTransactionPolicy.cs:56-71` | TRAZADA | Permiso genérico por clase funcional |
| AC-C6-07 | Débito monetario ordinario permitido en C6 | `RegulatoryCycleScheduleCatalogTests.cs:87-108`; `CycleTransactionPolicy.cs:56-71` | TRAZADA | Distinto de prohibición en C7 |
| AC-C6-08 | Prenotificación débito/crédito permitida en C6 | `RegulatoryCycleScheduleCatalogTests.cs:87-108`; `CycleTransactionPolicy.cs:77-105` | TRAZADA | Adendas y validaciones NACHA-M son transversales I09 |
| AC-C6-09 | Devoluciones de originales C2/C3/C4/C5 del día actual | `RegulatoryCycleScheduleCatalog.cs:178-185`; `RegulatoryCycleScheduleCatalogTests.cs:110-131`; `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchReturnsService.cs:79-116` | NO_DEMOSTRADA | H01: declaración/test del catálogo no prueban aplicación en generación ReturnOut |
| AC-C6-10 | Bloquear devolución cuyo origen es C1 del día actual | `RegulatoryCycleScheduleCatalogTests.cs:133-150` (negativa C6–C1); `CycleTransactionPolicy.cs:56-71` | NO_DEMOSTRADA | H01: permiso `AllowsReturn` no coteja origen C1 en la ruta general |
| AC-C6-11 | Bloquear originales de día anterior o de C6 y posteriores | `RegulatoryCycleScheduleCatalog.cs:90-106,178-185`; `AchReturnsService.cs:79-116` | NO_DEMOSTRADA | Falta rastrear enforcement preciso de fecha operativa y ciclo de origen H01 |
| AC-C6-12 | Comparar día operativo, no solo hora civil, en devolución | `RegulatoryCycleScheduleCatalog.cs:90-106`; `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchReturnOriginalCorrelationComponent.cs:73-96,117-140` | NO_DEMOSTRADA | Correlación de cámara y selección por lote existe; no demuestra `SourceDay` de matriz C6 |
| AC-C6-13 | Para R10, aplicar condiciones propias sin confundir ventana general con máximo de ciclos | `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchReturnEligibilityPolicyComponent.cs:123-175,213-235` | PARCIAL | Chequea débito, día y `MaxCycles`; H07 de orden general sigue vivo, **sin** afirmar aceptación R10 C2→C6 |
| AC-C6-14 | Correlacionar origen/cámara y evitar doble archivo de retorno | `AchReturnOriginalCorrelationComponent.cs:73-96,117-156`; `AchReturnsService.cs:87-98,128-202` | PARCIAL | Correlación y unicidad; no sustituyen la ventana C6 |
| AC-C6-15 | Validación de causal y elegibilidad/importe del retorno | `AchReturnEligibilityPolicyComponent.cs:16-80`; `AchReturnsService.cs:93-116`; `AchReturnOriginalCorrelationComponent.cs:149-156` | PARCIAL | Comprobaciones generales; auditoría de todas las causales I09 |
| AC-C6-16 | Permitir PSE/SSS/DIAN solo donde el anexo los contempla | `src/Cfa.ACHInterbank.Persistence/Configuration/CompanyEntryDescriptionCatalogConfiguration.cs:73-78`; `CycleTransactionPolicy.cs:56-71,77-113` | NO_DEMOSTRADA | H08: catálogo de descripciones no demuestra autorización por producto/ciclo |
| AC-C6-17 | Outbound C6 solo dentro de envío, con protección de reintentos | `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchColombiaManagedMftOutboundWorkflow.cs:39-50,70-123,311-329`; `AchColombiaManagedMftHandlers.cs:116-175` | PARCIAL | Validación ventana e idempotencia presentes; no envío/acuse externo |
| AC-C6-18 | Inbound C6 en ventana de liberación, con detección de duplicados | `AchColombiaManagedMftHandlers.cs:261-315`; `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchColombiaManagedMftInboundWorkflow.cs:40-75,88-123` | PARCIAL | Polling, hash y recuperación; aplicación bancaria no certificada |
| AC-C6-19 | Cierre C6 conciliado: planilla + pago + archivos + estados | `RegulatoryCycleScheduleCatalog.cs:224-229`; `ClearingHouseCyclePolicyResolver.cs:129-165`; `AchColombiaManagedMftHandlers.cs:261-315` | NO_DEMOSTRADA | H03: falta evidencia de barrera end-to-end y conciliación con cámara |

**Totales I07: 19 reglas = 6 TRAZADA, 7 PARCIAL, 6 NO_DEMOSTRADA, 0 incumplimientos nuevos confirmados.** No se suman hallazgos repetidos ni se interpreta el resultado como certificación.

## 3. Hallazgos y matiz específico del sexto ciclo

- **H01 (ALTA, ampliado a C6):** `ReturnWindow(6, Current(2), Current(3), Current(4), Current(5))` existe en catálogo. La prueba `[InlineData(6,1,CurrentOperationalDay)]` demuestra **exclusión declarada** de C1; no consta contraste de esa matriz en `AchReturnsService` (ruta examinada) ni en la política funcional, que verifica solo `AllowsReturn`. No se afirma que todas las rutas permitan retornos fuera de ventana: falta prueba de extremo a extremo.
- **H03 (ALTA, abierto):** horarios de planilla, compensación y liberación existen, pero no se verificó dependencia de respuesta financiera externa para cierre.
- **H08 (MEDIA, ampliado a C6):** PSE/SSS/DIAN enumerados en fuente C6; hay valores de descripción de lote. No se pudo demostrar validación específica de producto por ciclo.
- **H07 (ALTA, registrado desde I03):** `AchReturnEligibilityPolicyComponent.cs:148-160` ordena los ciclos por `StartTime` (C1 19:01 queda último), un defecto para secuencias que involucran C1 con origen permitido en ciclos previos. **En C6, C1→C6 está expresamente FUERA de la ventana general**; el defecto H07 no prueba por sí mismo una aceptación ni rechazo indebido de una operación permitida en C6. Para orígenes permitidos C2–C5 se debe revisar `MaxCycles` y la base R10 por separado. No se crea un H09 artificial.
- **H02** discrepancia de 5 vs 7 ciclos / vigencia externa; **H04** devolución de devolución; **H05** tratamiento R10 interdiario; **H06** fecha del disparador nocturno C1: continúan con alcance y estado previos, sin ampliar a C6 sin evidencia.

## 4. Comprobaciones y límites

**Chequeos estáticos de fuente ejecutados (7/7 positivos):** versión activa `ACHCOL-7C-20261014`; horas C6 y cuatro fases; `ReturnWindow(6, Current(2)..Current(5))`; prueba positiva de fuentes C6; prueba negativa de C1 en C6; permiso de débito C6; descripciones PSE/DIAN/SSS presentes. Estos chequeos inspeccionan texto de código y pruebas, **no los ejecutan**.

**Regresiones a ejecutar fuera de esta auditoría:** originales C2–C5 actuales aceptados / C1 actual, P7 previo y C6 actual rechazados; diferencias entre devolución ordinaria y R10 según `MaxCycles`; bordes 14:31, 16:00, 16:30, 16:45, 17:00; aceptación de PSE/SSS/DIAN C6; idempotencia; fecha valor/feriados; comprobación planilla-pago-liberación con acuses y conciliación.

**No ejecutado aquí:** suites .NET, DB, Quartz, MFT, ACH ni homologación externa. Son pendientes de validación, **no bloqueos** para completar inspección estática, y **no se marcan como aprobados**.

## 5. Traspaso a I08

Ciclo 7: envío **16:01–18:00**, cierre **18:00**, planillas **18:00–18:30**, pago **18:30–18:45**, liberación **18:45–19:00**; devoluciones **C3/C4/C5/C6 del día actual**; crédito monetario, prenotificaciones y PSE/SSS/DIAN; **NO débito monetario ordinario**. Auditar expresamente el veto al débito, expiración C2, H01 y H08. Mantener H01–H08 sin duplicar.
