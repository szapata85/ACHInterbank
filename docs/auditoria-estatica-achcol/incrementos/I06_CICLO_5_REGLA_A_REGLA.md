# I06 — auditoría estática ACH Colombia, ciclo 5 (siete ciclos)

**Corte documental:** 2026-11-01 · **I06 TERMINADO como auditoría estática; C5 NO certificado.** Alcance: fuente operativa C5, política efectiva en código, principales rutas de devolución y MFT. No se modificó código, tests ni infraestructura.

## 1. Fuente, fecha y condiciones del ciclo

- *Nuevos Ciclos de Proceso y Horarios.pdf*, **página 2, fila ciclo 5**: recepción de archivos **12:31–14:30**, cierre **14:30**, planillas **14:30–15:00**, pago de compensación **15:00–15:15**, liberación **15:15–15:30**; hora local Bogotá.
- Entradas declaradas: **crédito y débito monetarios, prenotificaciones débito/crédito, devoluciones débito y crédito de transacciones monetarias y prenotificaciones, y transacciones PSE, SSS y DIAN**. Orígenes de devoluciones **C1/C2/C3/C4 del mismo día operativo**; **no** P7 anterior, ni C5 actual, ni C6 futuro.
- La versión histórica del *Manual ACH Colombia V35* (abril 2026) contiene cinco ciclos. El código conserva versión histórica y una política posterior `ACHCOL-7C-20261014` efectiva desde 2026-10-14, pero **el despliegue ni su ratificación externa están verificados** (H02).
- `C1` se refiere al **primer ciclo del mismo día operativo**, aunque su inicio físico sea a las **19:01 del calendario anterior**. La fecha operativa no debe confundirse con la fecha de apertura del ciclo.

## 2. Matriz regla por regla — 18 controles

Las rutas son relativas a `ACHInterbank_ajustado/`. `TRAZADA` significa evidencia de definición o control estático específico; **no** certificación E2E.

| ID | Regla C5 | Evidencia de código / prueba | Dictamen | Observación / hallazgo |
|---|---|---|---|---|
| AC-C5-01 | Resolver ACHCOL 7C por vigencia sin sobrescribir V35 histórica | `src/Cfa.ACHInterbank.Application/ACH/Models/RegulatoryCycleScheduleCatalog.cs:152-156,239-267`; `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/Seeders/ClearingHouseCycleConfigSeeder.cs:25-45`; `.../ClearingHouseCyclePolicyResolver.cs:35-66` | TRAZADA | H02 mantiene pendiente activación real |
| AC-C5-02 | Horario de recepción C5 12:31–14:30 y cutoff 14:30 | `RegulatoryCycleScheduleCatalog.cs:216-222,295-315`; `tests/Cfa.ACHInterbank.Tests/RegulatoryCycleScheduleCatalogTests.cs:27-54` | TRAZADA | Se coteja regla declarada; no se ejecutó Quartz |
| AC-C5-03 | Configuración cronológica de planilla C5 14:30–15:00 | `RegulatoryCycleScheduleCatalog.cs:216-222,310-315`; `ClearingHouseCyclePolicyResolver.cs:129-165` | TRAZADA | Orden/ventana configurados, no entrega bancaria comprobada |
| AC-C5-04 | Procesar pago de compensación 15:00–15:15 | `RegulatoryCycleScheduleCatalog.cs:216-222,311-314`; `ClearingHouseCyclePolicyResolver.cs:150-165` | PARCIAL | Horario declarado y validado; liquidación bancaria y ACK pendiente H03 |
| AC-C5-05 | Liberación de archivos en 15:15–15:30 conforme a resultados | `RegulatoryCycleScheduleCatalog.cs:216-222,314-315`; `AchColombiaManagedMftHandlers.cs:277-315` | PARCIAL | Polling en ventana, pero barrera de pago y respuesta real no demostradas H03 |
| AC-C5-06 | Permitir crédito monetario ordinario C5 | `RegulatoryCycleScheduleCatalog.cs:28-44,216-222`; `CycleTransactionPolicy.cs:56-71` | TRAZADA | El permiso genérico existe |
| AC-C5-07 | Permitir débito monetario ordinario C5 | `RegulatoryCycleScheduleCatalogTests.cs:87-108`; `CycleTransactionPolicy.cs:56-71` | TRAZADA | Distinto de C7, que veta débito monetario |
| AC-C5-08 | Permitir prenotificación de crédito y débito C5 | `RegulatoryCycleScheduleCatalog.cs:28-44,216-222`; `CycleTransactionPolicy.cs:77-105`; `RegulatoryCycleScheduleCatalogTests.cs:88-98` | TRAZADA | Validación de clase funcional; contenido NACHA se verá en I09 |
| AC-C5-09 | Admitir devoluciones solamente si original es C1, C2, C3 o C4 del **día operativo actual** | `RegulatoryCycleScheduleCatalog.cs:178-185`; `RegulatoryCycleScheduleCatalogTests.cs:110-131`; `CycleTransactionPolicy.cs:56-71`; `AchReturnsService.cs:79-116` | NO_DEMOSTRADA | H01: ventana declarada, no comprobada como regla efectiva en el flujo general |
| AC-C5-10 | Rechazar por origen P7 previo, C5 actual u otros orígenes fuera de ventana | `RegulatoryCycleScheduleCatalogTests.cs:133-150` (P7); `AchReturnOriginalCorrelationComponent.cs:117-140` (correlación) | NO_DEMOSTRADA | H01: prueba estática del catálogo no equivale a rechazo en ruta de generación |
| AC-C5-11 | Distinguir día operativo de fecha calendario física y día hábil anterior | `RegulatoryCycleScheduleCatalog.cs:90-106,178-185`; `AchReturnOriginalCorrelationComponent.cs:73-96,117-140` | NO_DEMOSTRADA | En ruta general no se comprobó comparación de `SourceDay` + ciclo; H01/H05 |
| AC-C5-12 | Validación de R10 C1→C5 sin ordenamiento que falsee índice | `AchReturnEligibilityPolicyComponent.cs:123-175` | INCUMPLIMIENTO_CONFIRMADO | H07: `OrderBy(StartTime)` coloca C1 tras C5 y puede emitir `WINDOW_POLICY_UNRESOLVED` antes de evaluar causal/MaxCycles. **No implica que R10 C1→C5 deba aceptarse** |
| AC-C5-13 | Correlacionar original, cámara y ciclo y evitar duplicación de Return Out | `AchReturnOriginalCorrelationComponent.cs:57-71,73-96,117-156`; `AchReturnsService.cs:87-98,128-202` | PARCIAL | Correlación, lock y unicidad presentes; no reemplazan ventana C5 H01 |
| AC-C5-14 | Validar causal y prevenir devolución de transacción ya retornada/reversada | `AchReturnEligibilityPolicyComponent.cs:16-52`; `AchReturnsService.cs:93-116`; `AchReturnOriginalCorrelationComponent.cs:149-156` | PARCIAL | Validaciones locales trazadas; exhaustividad por causal queda para I09 |
| AC-C5-15 | Identificar y procesar PSE, SSS y DIAN en C5 sin arrastrar permisos a C4 | *Nuevos Ciclos*, pág. 2 C5; `CompanyEntryDescriptionCatalogConfiguration.cs:73-78`; `CycleTransactionPolicy.cs:56-71,77-113` | NO_DEMOSTRADA | H08: códigos de descripción existentes, pero no verificado control por producto/ciclo en ruta funcional |
| AC-C5-16 | Outbound automático solo dentro de recepción C5 e idempotente | `AchColombiaManagedMftOutboundWorkflow.cs:49-50,70-121,311-329`; `AchColombiaManagedMftHandlers.cs:116-175` | PARCIAL | Validación de ventana y clave de idempotencia; sin ejecución externa/ACK |
| AC-C5-17 | Inbound automático solo dentro de liberación C5, más reintentos/duplicados | `AchColombiaManagedMftHandlers.cs:261-315`; `AchColombiaManagedMftInboundWorkflow.cs:49-123` | PARCIAL | Ventana validada en código; confirmación y aplicación real por cámara pendiente |
| AC-C5-18 | Conciliación planilla ↔ liquidación ↔ liberación, con estado final verificable | `RegulatoryCycleScheduleCatalog.cs:216-222`; `ClearingHouseCyclePolicyResolver.cs:129-165`; `AchColombiaManagedMftHandlers.cs:277-315` | NO_DEMOSTRADA | H03: no demostrada dependencia de confirmación financiera externa para dar por cerrado C5 |

**Conteo I06: 18 controles = 6 TRAZADA, 6 PARCIAL, 5 NO_DEMOSTRADA, 1 INCUMPLIMIENTO_CONFIRMADO (extensión H07).** Sin hallazgos nuevos redundantes. Ninguna cifra implica conformidad de banca o UAT.

## 3. Hallazgos y delimitación

- **H01 ALTA, ampliación a C5:** `ReturnWindow(5, Current(1), Current(2), Current(3), Current(4))` está declarado, con prueba positiva y negativa P7; el tipo `RegulatoryCyclePolicySnapshot` (`RegulatoryCycleScheduleCatalog.cs:22-26`) no contiene `ReturnWindows`, y `CycleTransactionPolicy` solo comprueba `AllowsReturn` (`:56-71`). `AchReturnsService` y el componente de correlación no muestran consumo explícito de la matriz para la ruta revisada. Exige confirmar enforcement a lo largo de la ruta de devolución; **no afirmar que toda devolución inválida entra en todos los caminos**.
- **H07 ALTA, ampliación a C5:** `AchReturnEligibilityPolicyComponent.cs:148-160` ordena ciclos por hora de inicio y sitúa `C1` al final `[C2,C3,C4,C5,C6,C7,C1]`. Para una devolución R10 C1→C5 esto dispara una falsa falla de resolución antes del conteo de oportunidades. **Tras corregir orden, habrá que aplicar `MaxCycles` y las reglas específicas R10: no se declara C1→C5 legítimo por el solo hecho de figurar en la ventana general C5.**
- **H08 MEDIA, ampliación a C5:** esta fila fuente **sí** habilita productos PSE/SSS/DIAN; una codificación de descripción de lote no prueba una validación de producto por ciclo. Revisar I09 contra C3/C4/C5 para garantizar tanto aceptación debida como rechazo indebido.
- **H02 MEDIA:** diferencia V35 cinco ciclos vs política interna siete; apertura literal inconsistente de C4 ya registrada en I05. No confundir fuente histórica y documento siete ciclos. Vigencia externa/despliegue pendientes.
- **H03 ALTA:** regla de horario de planilla/pago/liberación es diferente a confirmación financiera y conciliación efectiva. Conservar hasta I09.
- **H04:** ROR transversal y causales en I09. **H05:** R10 interdiario pertenece a reclamaciones; mantener separado de la ventana ordinaria C5. **H06:** posible error condicionado de fecha en disparador nocturno C1, no de C5 diurno.

## 4. Validaciones realizadas y pruebas propuestas

**Inspección estática local ejecutada:** 6 verificaciones de presencia de código/regla/test (`ACHCOL-7C-20261014`, cinco horarios C5, `ReturnWindow(5, C1,C2,C3,C4)`, prueba positiva de C5, prueba negativa P7, permiso de débito C5): **6/6 encontradas**. Son chequeos de código fuente, no una ejecución de tests .NET.

**Regresiones focales sugeridas, NO ejecutadas:** (a) C1/C2/C3/C4 actuales aceptados en C5, contra P7 anterior/C5 actual rechazados; (b) fecha operativa vs calendario físico, festivos y fin de semana; (c) R10 C1→C5 con razón de rechazo coherente, revalidación de `MaxCycles` y causal; (d) bordes 12:31, 14:30, 15:00, 15:15, 15:30; (e) PSE, SSS y DIAN C5 permitido vs C4 no enumerado; (f) comprobación planilla, pago y liberación con ACK/conciliación.

**.NET, DB, Quartz, MFT y homologación ACH Colombia no ejecutados en el entorno de este turno.** Esto no bloquea la revisión estática ni permite afirmar ciclo certificado.

## 5. Traspaso a I07

Auditar **ciclo 6**: recepción **14:31–16:00**, planilla **16:00–16:30**, pago **16:30–16:45**, liberación **16:45–17:00**; devoluciones de **C2/C3/C4/C5 del día actual**, no C1 actual; débitos/créditos monetarios, prenotificaciones, PSE/SSS/DIAN. Enfocar vencimiento C1→C6, H01, H07 R10 según ventana propia y bordes de expiración. Mantener lista H01–H08 sin duplicados.
