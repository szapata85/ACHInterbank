# I04 — auditoría estática ACH Colombia, ciclo 3 (siete ciclos)

**Fecha de corte:** 2026-11-01 · **I04 TERMINADO documentalmente, no certificado** · **Código/tests sin modificaciones**.

## 1. Alcance y fuentes

- *Nuevos Ciclos de Proceso y Horarios.pdf*, **pág. 2**, fila ciclo 3: recepción **09:01–11:00**, cierre **11:00**, planilla **11:00–11:30**, pago de compensación **11:30–11:45**, liberación **11:45–12:00**; hora Colombia.
- Tipos señalados: **créditos y débitos monetarios**, **prenotificaciones crédito y débito**, **devoluciones** de ciclos **P6/P7** (día hábil bancario anterior) y **C1/C2** (mismo día operativo). La fuente enumera adicionalmente **PSE, SSS y DIAN** para este ciclo.
- *Manual ACH Colombia V35* (abril 2026), §2.4 y §2.7, como fuente general histórica de cinco ciclos. El proyecto adopta por decisión interna la versión 7C `ACHCOL-7C-20261014`. **No se prueba aquí su activación oficial o despliegue externo** (H02). Las reglas de CENIT no se aplican a ACHCOL.
- Notación: **P** = ciclo del día **hábil operativo anterior**; **C** = ciclo del día **operativo actual**. Casos admisibles C3: P6, P7, C1, C2; casos que deben rechazarse por ventana C3: P5, C3, C4 y demás fuera de matriz (sin perjuicio de causales/reglas especiales de devolución).

## 2. Matriz estática regla por regla — 16 controles

| Regla | Condición verificable C3 | Código y línea de evidencia | Estado estático | Hallazgo / verificación |
|---|---|---|---|---|
| AC-C3-01 | Resolver únicamente política ACHCOL 7C por fecha y cámara | `RegulatoryCycleScheduleCatalog.cs:152-156,245-267`; `ClearingHouseCyclePolicyResolver.cs:35-66` | TRAZADA | H02: no sustituir confirmación externa por constante en código |
| AC-C3-02 | Ventana recepción 09:01–11:00 y corte 11:00 | `RegulatoryCycleScheduleCatalog.cs:202-208,295-315`; `AchColombiaManagedMftHandlers.cs:231-245` | TRAZADA | Límites exactos no ejecutados contra servidor externo |
| AC-C3-03 | Solo días hábiles, fecha operativa e idempotencia al programar | `AchCycleScheduler.cs:40-61,65-99,101-123` | TRAZADA | Debe comprobarse calendario operativo en ambiente autorizado |
| AC-C3-04 | Etapas planilla 11:00–11:30, pago 11:30–11:45 y liberación 11:45–12:00 | `RegulatoryCycleScheduleCatalog.cs:202-208,295-315`; `ClearingHouseCyclePolicyResolver.cs:110-165` | PARCIAL | H03: orden horario modelado, falta prueba de barrera financiera y conciliación |
| AC-C3-05 | Crédito monetario permitido | `RegulatoryCycleScheduleCatalog.cs:28-45,202-208`; `CycleTransactionPolicy.cs:56-71`; `NachaTransactionValidationService.cs:52-73` | TRAZADA | Falta validación con transacciones de cámara |
| AC-C3-06 | Débito monetario permitido | `RegulatoryCycleScheduleCatalog.cs:202-208`; `CycleTransactionPolicy.cs:56-71`; `NachaTransactionValidationService.cs:52-73` | TRAZADA | Falta validación con transacciones de cámara |
| AC-C3-07 | Prenotificación crédito/débito permitida, clase inequívoca y valor cero | `CycleTransactionPolicy.cs:92-105`; `NachaTransactionValidationService.cs:76-78` | TRAZADA | NACHA-M completo queda en I09 |
| AC-C3-08 | Devoluciones C3 solo de **P6/P7/C1/C2** | `RegulatoryCycleScheduleCatalog.cs:178-185`; `RegulatoryCycleScheduleCatalogTests.cs:110-130` | NO_DEMOSTRADA | H01: la matriz se prueba como dato del candidato, no se encontró aplicación a cada devolución productiva |
| AC-C3-09 | Rechazar devoluciones C3 de **P5/C3** y demás orígenes inelegibles | `RegulatoryCycleScheduleCatalogTests.cs:133-150`; `AchReturnOriginalCorrelationComponent.cs:73-114,117-140` | NO_DEMOSTRADA | H01: `ReturnCycleId` puede omitirse y convertirse en ciclo original; no hay guard integral fuente→C3 en ruta general |
| AC-C3-10 | R10 original **C1→C3** no debe rechazarse por ordenar C1 al final del día | `AchReturnEligibilityPolicyComponent.cs:123-174`; `RegulatoryCycleScheduleCatalog.cs:187-208` | INCUMPLIMIENTO_CONFIRMADO | H07 extendido: `OrderBy(StartTime)` ordena C2,C3,C4,C5,C6,C7,C1; `returnIndex(C3)<originalIndex(C1)` produce excepción de orden, si R10 llega a esta ruta |
| AC-C3-11 | R10 de **P6/P7** no debe decidirse como si fueran C6/C7 del día actual | `AchReturnEligibilityPolicyComponent.cs:134-139,143-174` | PARCIAL | H05: ruta R10 interdiaria deriva a reclamaciones; compatibilidad con fuente específica por causal queda pendiente |
| AC-C3-12 | Causal, original, importe y control de duplicidad antes de generar devolución | `AchReturnOriginalCorrelationComponent.cs:57-70,117-156`; `AchReturnsService.cs:87-116,128-202` | PARCIAL | Control general presente, no suple H01/H07 ni prueba todas las causales |
| AC-C3-13 | Emisión MFT C3 en ventana e idempotente | `AchColombiaManagedMftHandlers.cs:149-175,231-245`; `AchColombiaManagedMftOutboundWorkflow.cs:70-121` | PARCIAL | Sin servidor MFT/acuse real |
| AC-C3-14 | Recepción/respuesta C3 en liberación 11:45–12:00 con evidencia | `AchColombiaManagedMftHandlers.cs:261-315`; `AchColombiaManagedMftInboundWorkflow.cs:399-450` | PARCIAL | Ventana explícita y observación implementadas; procesamiento bancario E2E no demostrado |
| AC-C3-15 | Conciliar planilla, pago, liberación y cierre C3 antes de dar estado final | `RegulatoryCycleScheduleCatalog.cs:202-208`; `ClearingHouseCyclePolicyResolver.cs:150-165`; `AchColombiaManagedMftInboundWorkflow.cs:399-450` | NO_DEMOSTRADA | H03: no hay evidencia de confirmado bancario integral en esta revisión |
| AC-C3-16 | Tratamiento específico C3 de operaciones **PSE/SSS/DIAN** conforme a fuente | `CompanyEntryDescriptionCatalogConfiguration.cs:73-78`; `CycleTransactionPolicy.cs:56-71,77-113` | NO_DEMOSTRADA | **H08 nuevo, no incumplimiento demostrado**: catálogo incluye conceptos, pero política genérica de ciclo no distingue estos tres productos. Rastrear validación de producto en I09 |

**Resultado I04:** 16 controles: **6 TRAZADA, 5 PARCIAL, 4 NO_DEMOSTRADA, 1 INCUMPLIMIENTO_CONFIRMADO** (extensión H07, **no** nueva falla de ejecución). La conformidad bancaria sigue **sin certificar**.

## 3. Análisis dirigido de hallazgos

**H01 (alta, se mantiene).** `RegulatoryCycleScheduleCatalog.cs:178-185` declara explícitamente la tabla P6/P7/C1/C2. `RegulatoryCycleScheduleCatalogTests.cs:113,122-130,136` confirma datos y rechaza P5 como quinto origen. En el snapshot activo solo se persisten ciclos y permisos (`RegulatoryCyclePolicySnapshot.cs` definido en `RegulatoryCycleScheduleCatalog.cs:22-26`; conversión `:124-138`), sin propagar `ReturnWindows`. `CycleTransactionPolicy.cs:56-71` permite clase Return según `AllowsReturn`, no valida ciclo de origen. La ruta `AchReturnsService.cs:79-116` tampoco realiza esa comparación. Se mantiene **brecha de aplicación**, no se infiere el comportamiento de servicios no revisados ni de la cámara externa.

**H07 (alta, extensión C1→C3).** Para `ProcessingDate` común, C1 se configura con `StartTime=19:01`, C3 con `09:01`. `AchReturnEligibilityPolicyComponent.cs:148-160` usa `OrderBy(StartTime)` y luego exige `returnIndex >= originalIndex`. La ruta obtiene `[C2,C3,C4,C5,C6,C7,C1]`; C1→C3 produce `1<6` y error `ACHCOL_R10_WINDOW_POLICY_UNRESOLVED`. **Defecto lógico estático condicionado a R10 y a esta ruta**, no conclusión de que todas las devoluciones C1→C3 fallen en producción. Corrección futura mínima: ordenar por secuencia operativa/instante real, con regresión C1→C2 y C1→C3.

**H08 (media, nuevo, evidencia insuficiente).** La fuente lista PSE/SSS/DIAN para C3. Hay descripciones `PAGOS PSE`, `COBROS PSE`, `PAGOS DIAN`, `COBROS SSS` en configuración (`CompanyEntryDescriptionCatalogConfiguration.cs:73-78`), pero las clases de `CycleTransactionPolicy` son solo crédito, débito, prenotificación, devolución y ROR. Revisar en I09 la validación de tipo/servicio entre archivo, lote, código y ciclo, y negativa para ciclo sin autorización. **No afirmar que PSE/SSS/DIAN están ausentes**, ni que se procesan correctamente sin rastreo adicional.

## 4. Pruebas de regresión propuestas, NO ejecutadas

- `C3 positivo`: devoluciones originadas P6/P7/C1/C2; verificar vinculación de original, fecha operativa y ciclo de devolución.
- `C3 negativo`: devolver P5 o C3 al propio C3; bloquear antes de generar NACHA-M, sin cambiar estado de original.
- `R10 borde`: con 7 ciclos efectivos, R10 C1→C3 debería superar la comprobación de orden y pasar luego las validaciones propias de causal y máximo de oportunidades; R10 fuera de ventana debe fallar por razón correcta.
- `C3 producto`: PSE, SSS, DIAN en C3 y producto no permitido en ciclo diferente; confirmar cómo se identifica el producto en NACHA/lote.
- `C3 infraestructura`: fecha hábil, corte 11:00, planilla, pago 11:30–11:45, liberación 11:45–12:00, idempotencia, ACK y conciliación.

**Ambiente:** esta intervención usó lectura y búsqueda estática. No corrió `.NET`, bases de datos, Quartz, MFT ni homologación; queda `NO_EJECUTADA_EN_ESTE_ENTORNO` y **no bloquea I05**.

## 5. Traspaso

**I05 — ciclo 4:** confirmar hora de apertura `11:01`, cierre `12:30`, planilla hasta `13:00`, pago hasta `13:15`, liberación hasta `13:30`; ventana **P7/C1/C2/C3**. No repetir H01/H07 como nuevos hallazgos; verificar nuevos bordes específicos C4. Mantener abierto H08 hasta I09.
