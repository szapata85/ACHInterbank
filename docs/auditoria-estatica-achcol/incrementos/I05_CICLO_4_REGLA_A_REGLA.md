# I05 — auditoría estática ACH Colombia, ciclo 4 (siete ciclos)

**Corte:** 2026-11-01 · **I05 TERMINADO como revisión estática; ciclo NO certificado**. Sin cambios en código, tests, frontend ni configuración.

## 1. Regla operativa fuente y ambigüedad puntual

- *Nuevos Ciclos de Proceso y Horarios.pdf*, **página 2**, fila C4: envío hasta **12:30**, planillas **12:30–13:00**, pago **13:00–13:15**, liberación **13:15–13:30** (hora de Bogotá). Se permiten **débito/crédito monetario, prenotificación débito/crédito y devoluciones de P7/C1/C2/C3**.
- La columna de inicio **literalmente dice `11:01pm a 12:30pm`**: inconsistencia interna (PM del día anterior/franja imposible frente a C3 que termina 11:00 y C4 que cierra 12:30). **No se corrige silenciosamente el documento**. El catálogo confirmado `ACHCOL-7C-20261014` fija **11:01am** (`RegulatoryCycleScheduleCatalog.cs:257-267`) y sus tests lo verifican (`RegulatoryCycleScheduleCatalogTests.cs:28-53`). Se considera **CONFLICTO_FUENTE**, vinculado a H02. Requiere ratificación del horario 11:01 am por el administrador/versión operativa; no se presume que el software sea defectuoso por la errata.
- Para C4 la tabla **no enumera PSE, SSS ni DIAN** (a diferencia de C3). La revisión examina si se comprueba esa distinción; la ausencia de un control específico en la ruta revisada no prueba por sí sola que entren operaciones prohibidas.
- *Manual V35 abril 2026* §2.4.1 sigue describiendo **cinco ciclos**, mientras esta auditoría adopta por decisión interna la política siete ciclos efectiva desde 2026-10-14 en código. **La fecha de activación externa y el despliegue no se acreditan** (H02).
- P7 = ciclo 7 del **día hábil operativo anterior**. C1/C2/C3 = ciclos del **mismo día operativo**. Orígenes P6 o C4 **no pertenecen** a la ventana ordinaria de devolución C4.

## 2. Matriz regla por regla — 17 controles

| ID | Regla verificable C4 | Evidencia localizada (rutas relativas a `src/` o `tests/`) | Resultado | Observación |
|---|---|---|---|---|
| AC-C4-01 | Cámara ACHCOL, versión vigente por fecha y siete ciclos | `Application/ACH/Models/RegulatoryCycleScheduleCatalog.cs:152-156,245-267`; `Persistence/ACH/Services/Implementation/ClearingHouseCyclePolicyResolver.cs:35-66` | TRAZADA | H02: despliegue no demostrado |
| AC-C4-02 | Apertura C4 indicada `11:01pm` en documento; código utiliza `11:01am` | `RegulatoryCycleScheduleCatalog.cs:209-215,257-267`; `tests/RegulatoryCycleScheduleCatalogTests.cs:28-53` | CONFLICTO_FUENTE | Hora de inicio requiere ratificación formal; no inventar corrección normativa |
| AC-C4-03 | Corte de entrada 12:30 y límite de envío | `RegulatoryCycleScheduleCatalog.cs:209-215,295-315`; `Persistence/ACH/Services/Implementation/AchColombiaManagedMftOutboundWorkflow.cs:311-329` | TRAZADA | No probado contra servidor ni bordes reales |
| AC-C4-04 | Día hábil, fecha operativa e idempotencia de creación | `Persistence/ACH/Services/Implementation/AchCycleScheduler.cs:40-61,65-99,101-123` | TRAZADA | Calendario efectivo pendiente de ejecución |
| AC-C4-05 | Planilla 12:30–13:00, pago 13:00–13:15, salida 13:15–13:30 | `RegulatoryCycleScheduleCatalog.cs:209-215,295-315`; `Persistence/ACH/Services/Implementation/ClearingHouseCyclePolicyResolver.cs:110-165` | PARCIAL | H03: etapas definidas sin prueba de barrera financiera |
| AC-C4-06 | Crédito monetario permitido en C4 | `RegulatoryCycleScheduleCatalog.cs:209-215`; `Application/ACH/Services/CycleTransactionPolicy.cs:56-71`; `Persistence/ACH/Services/Implementation/NachaTransactionValidationService.cs:52-73` | TRAZADA | Emisión bancaria real no ensayada |
| AC-C4-07 | Débito monetario permitido en C4 | Mismas rutas de AC-C4-06 | TRAZADA | No confundir con veto de débito ordinario en C7 |
| AC-C4-08 | Prenotificaciones crédito/débito permitidas y valor cero | `Application/ACH/Services/CycleTransactionPolicy.cs:92-105`; `Persistence/ACH/Services/Implementation/NachaTransactionValidationService.cs:76-78` | TRAZADA | Especificaciones NACHA-M completas en I09 |
| AC-C4-09 | Aceptar devoluciones C4 de **P7/C1/C2/C3**, con origen y día correcto | `RegulatoryCycleScheduleCatalog.cs:178-185`; `tests/RegulatoryCycleScheduleCatalogTests.cs:110-130` | NO_DEMOSTRADA | H01: catálogo/test no acreditan enforcement en servicio productivo |
| AC-C4-10 | Bloquear devoluciones C4 de **P6/C4** y demás fuentes inelegibles | `tests/RegulatoryCycleScheduleCatalogTests.cs:133-150`; `Persistence/ACH/Services/Implementation/AchReturnOriginalCorrelationComponent.cs:73-140` | NO_DEMOSTRADA | H01: no se localizó comparación origen-día vs `ReturnWindows` en ruta ordinaria |
| AC-C4-11 | R10 C1→C4 debe evaluarse en **orden operativo**, antes de su máximo causal específico | `Persistence/ACH/Services/Implementation/AchReturnEligibilityPolicyComponent.cs:123-174`; `RegulatoryCycleScheduleCatalog.cs:187-215` | INCUMPLIMIENTO_CONFIRMADO | H07: `OrderBy(StartTime)` relega C1 al final y genera `WINDOW_POLICY_UNRESOLVED` **si R10 alcanza la ruta**. Aun con el orden corregido R10 podría expirar por `MaxCycles`: no afirmar aceptación automática |
| AC-C4-12 | R10 de P7 anterior respeta el tratamiento separado de reclamaciones/causales | `Persistence/ACH/Services/Implementation/AchReturnEligibilityPolicyComponent.cs:134-146,213-235` | PARCIAL | H05: R10 interdiario deriva a reclamaciones; no generalizar a todas las devoluciones P7 |
| AC-C4-13 | Validación de causal, original, no duplicidad y generación consistente | `Persistence/ACH/Services/Implementation/AchReturnOriginalCorrelationComponent.cs:57-70,117-156`; `AchReturnsService.cs:79-116,128-202` | PARCIAL | Hay verificaciones genéricas, no sustituyen ventana C4 H01 ni R10 H07 |
| AC-C4-14 | Salida MFT C4 en ventana, con control de reintentos e idempotencia | `Persistence/ACH/Services/Implementation/AchColombiaManagedMftHandlers.cs:149-175,231-245`; `AchColombiaManagedMftOutboundWorkflow.cs:49-50,70-121,311-329` | PARCIAL | Sin SFTP/ACK de cámara |
| AC-C4-15 | Polling y archivo entrante C4 en ventana **13:15–13:30** | `Persistence/ACH/Quartz/Jobs/Implementation/AchColombiaManagedMftHandlers.cs:261-315`; `RegulatoryCycleScheduleCatalog.cs:209-215,295-315` | PARCIAL | Filtro en código; no probada recepción E2E ni exactitud en despliegue |
| AC-C4-16 | Conciliar planilla, pago y liberación antes de estado financiero final | `RegulatoryCycleScheduleCatalog.cs:209-215`; `ClearingHouseCyclePolicyResolver.cs:129-165`; `AchColombiaManagedMftInboundWorkflow.cs` | NO_DEMOSTRADA | H03: no se encontró evidencia suficiente de confirmación bancaria integral en el alcance C4 |
| AC-C4-17 | No admitir clases especiales **PSE/SSS/DIAN** en C4 si la matriz de autorizaciones por ciclo así lo exige | Fuente pág. 2, fila C4; `Application/ACH/Services/CycleTransactionPolicy.cs:56-71,77-113`; `Persistence/Configuration/CompanyEntryDescriptionCatalogConfiguration.cs:73-78` | NO_DEMOSTRADA | H08 extendido: política por clase no distingue estos productos; rastrear otras barreras antes de dictaminar incumplimiento |

**Resultado I05: 17 controles = 6 TRAZADA, 5 PARCIAL, 4 NO_DEMOSTRADA, 1 INCUMPLIMIENTO_CONFIRMADO (H07, ya conocido) y 1 CONFLICTO_FUENTE (H02, ampliado).** Ninguna cifra es una tasa de cumplimiento bancario.

## 3. Hallazgos relevantes y decisión

**H01 (ALTA, abierta):** `ReturnWindows` sí declara `ReturnWindow(4, Previous(7), Current(1), Current(2), Current(3))` (`RegulatoryCycleScheduleCatalog.cs:182`), pero el snapshot activado (`:22-26,124-138`) no contiene esa colección; `CycleTransactionPolicy` (`:56-71`) decide `AllowsReturn`, sin recibir ciclo/día de origen. En el flujo general `AchReturnsService` (`:79-116`) y su correlación (`AchReturnOriginalCorrelationComponent.cs:73-140`) tampoco se detectó aplicación equivalente. **Brecha estática de enforcement en la ruta revisada**, sin inferir ausencia absoluta de otras barreras.

**H07 (ALTA, ya abierto, extendido a C4):** ante original C1 (StartTime 19:01) y devolución C4 (11:01), `AchReturnEligibilityPolicyComponent.cs:148-160` ordena cronológicamente por `StartTime` plano: `[C2,C3,C4,C5,C6,C7,C1]`. De este modo R10 C1→C4 puede salir con `ACHCOL_R10_WINDOW_POLICY_UNRESOLVED` por **orden inválido**, antes del cálculo de oportunidad. **No afirmar que R10 C1→C4 sea finalmente elegible**: además debe respetar `MaxCycles` y su causal. No crear nuevo hallazgo; ampliar H07.

**H02 (MEDIA, ampliada por conflicto de fuente):** fila C4 contiene un `11:01pm` incompatible con 12:30pm; la interpretación `11:01am` está consolidada en catálogo y tests, pero debe conservarse constancia documental de la ratificación. Sin corrección automática al PDF original.

**H08 (MEDIA, no demostrado):** C4 omite PSE/SSS/DIAN aunque C3 sí los incluye; la política genérica por clase no diferencia productos. Verificar si otras reglas de archivo/lote previenen envío C4. La omisión es una diferencia del documento, **no** prueba automática de prohibición contractual ni un fallo confirmado.

**H03/H05:** mantienen la misma condición de I04; continuidad en I09. H06 aplica al ciclo nocturno C1, no se reabre como nuevo problema de C4. H04 ROR transaccional completo se evalúa en I09.

## 4. Pruebas focales de regresión recomendadas — NO ejecutadas

1. **Ventana C4 positiva:** P7 del hábil anterior y C1/C2/C3 del actual, conservando referencia original, fecha operativa y causal.
2. **Ventana C4 negativa:** P6 anterior, C4 actual y ciclo futuro; devolver rechazo correcto **antes** de generar archivo o cambiar estado de la transacción.
3. **R10 C1→C4:** ejecutar orden operativo primero; aplicar después tope `MaxCycles`, posibilidad de devolución y evidencia de causal. Probar que un rechazo por expiración use la razón específica, no `WINDOW_POLICY_UNRESOLVED` falso.
4. **Bordes C4:** 11:01am (pendiente ratificación documental), 12:30, 13:00, 13:15 y 13:30; sólo en día hábil y con zona `America/Bogota`.
5. **Clases especiales:** PSE, SSS y DIAN contra C3 permitido y C4 no enumerado, validando política por producto y código de lote sin asumir que una descripción equivale a transacción funcional.
6. **Liquidación:** planilla→pago→liberación, ACK y conciliación; verificar que ninguna confirmación interna sustituya la externa.

**Ambiente:** no se ejecutaron .NET, base de datos, Quartz, MFT ni aceptación con ACH Colombia. Esto **no bloquea** el avance estático I06; no permite certificar C4.

## 5. Traspaso I06

**Ciclo 5**: envío **12:31–14:30**, planillas **14:30–15:00**, pago **15:00–15:15**, liberación **15:15–15:30**; devoluciones de **C1/C2/C3/C4** del mismo día, créditos/débitos/prenotificaciones, y PSE/SSS/DIAN expresamente listados. Revisar restricciones interdiarias, C1 original y R10 sin duplicar H01/H07/H08. Mantener H02 documental pendiente.
