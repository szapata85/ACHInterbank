# I02 — revisión estática ACH Colombia: ciclo 1 (7 ciclos)

**Corte:** 2026-11-01 · **Estado del incremento:** TERMINADO con hallazgos abiertos · **Naturaleza:** auditoría estática focalizada, sin cambios en código ni certificación de funcionamiento bancario.

## 1. Alcance y criterio

Ciclo 1 de la versión de siete ciclos `ACHCOL-7C-20261014` según decisión documentada en `docs/operations/ACHCOL_CICLOS_7_VIGENCIA_2026-10-14.md` (vigencia interna 2026-10-14). Fuentes: *Nuevos Ciclos de Proceso y Horarios.pdf*, página 1, y *Manual de servicio ACH transferencias interbancarias V35*, abril de 2026, numerales 2.4.1, 2.4.8 y 2.5. El manual V35 incluye cinco ciclos, mientras que el anexo adicional especifica siete; **el contraste de vigencia externa permanece abierto (H02)**. No se inventa una reconciliación normativa.

**C1 esperado en el anexo siete ciclos:** envío desde 19:01 hasta 07:00, cierre 07:00, planilla 07:00–07:30, pago 07:30–07:45 y liberación 07:45–08:00 (hora Colombia). Permite entradas crédito/débito, prenotificaciones crédito/débito, PSE, SSS y DIAN cuando aplique; devoluciones de entradas de ciclos 4, 5, 6 y 7 del día operativo anterior. Se distingue *recepción de archivos*, *generación de devoluciones*, *transferencia MFT*, *compensación* y *respuesta*.

**Estados:** `TRAZADA` = evidencia estática concreta de la regla o tramo; `PARCIAL` = implementación localizable sin demostrar toda la cadena; `NO_DEMOSTRADA` = falta control localizado en ruta auditada; `CONFLICTO_FUENTE` = pendiente de aclaración normativa. No equivalen a pruebas ejecutadas.

## 2. Matriz regla a regla — ciclo 1

| ID | Regla / fuente | Evidencia precisa en código y pruebas | Estado estático | Hallazgo / cierre |
|---|---|---|---|---|
| AC-C1-01 | 7 ciclos, versión efectiva 14-10-2026 (decisión interna) | `RegulatoryCycleScheduleCatalog.cs:154-156,245-267`; `ClearingHouseCycleConfigSeeder.cs:25-45`; `ClearingHouseCyclePolicyResolver.cs:38-66`; `RegulatoryCycleScheduleCatalogTests.cs:28-57` | TRAZADA | Semillas e historial separados; vigencia oficial externa H02, despliegue no probado |
| AC-C1-02 | Corte de recepción 19:01 (D-1)–07:00 (D) | `RegulatoryCycleScheduleCatalog.cs:188-194,295-315`; `OperationalCycleWindowResolver.cs:8-42`; `AchColombiaManagedMftHandlers.cs:23-64,231-245` | PARCIAL | Ventana interdiaria calculada; comportamiento de viernes/festivos y disparador específico requiere revisión H06 |
| AC-C1-03 | Día hábil bancario y fecha de proceso | `AchCycleScheduler.cs:40-61,65-90`; `ProjectedOperationalCycleTests.cs:17-38,102-125` | TRAZADA | Calendario decide próximo hábil e idempotencia de alta; pruebas .NET no ejecutadas aquí |
| AC-C1-04 | Evitar mezclas/duplicados de políticas y conservar historial | `ClearingHouseCyclePolicyResolver.cs:45-66,84-107`; `AchCycleScheduler.cs:95-157`; `ProjectedOperationalCycleTests.cs:40-99` | TRAZADA | No actualizar ciclo vinculado a transacciones, lotes o NACHA sin conciliación |
| AC-C1-05 | Créditos y débitos monetarios ordinarios permitidos | `RegulatoryCycleScheduleCatalog.cs:3-17,187-194,295-315`; `CycleTransactionPolicy.cs:45-74,107-112` | PARCIAL | Permisos por clase funcional; verificar invocaciones en todas las vías de entrada en I09 |
| AC-C1-06 | Prenotificaciones crédito y débito permitidas | `RegulatoryCycleScheduleCatalog.cs:3-17,187-194`; `CycleTransactionPolicy.cs:57-65,92-105` | PARCIAL | Clasifica dirección y falla si es ambigua; recorrer archivos/códigos NACHA-M en I09 |
| AC-C1-07 | PSE, SSS, DIAN donde corresponda (anexo siete ciclos, p. 1) | `RegulatoryCycleScheduleCatalog.cs` modela flags de clases monetarias y prenotificaciones, no hay flags separados para PSE/SSS/DIAN | NO_DEMOSTRADA | Revisar enrutamiento propio de estos servicios en I09; no inferir que los rechaza |
| AC-C1-08 | Devoluciones C1: originales de C4/C5/C6/C7 del día operativo anterior | `RegulatoryCycleScheduleCatalog.cs:96-116,178-185,245-267`; `RegulatoryCycleScheduleCatalogTests.cs:118-150` | NO_DEMOSTRADA | **H01**: matriz solo en candidato; `RequireActivatable()` devuelve `RegulatoryCyclePolicySnapshot` sin ventanas; no hay referencias productivas a `ReturnWindows` fuera del catálogo |
| AC-C1-09 | Excluir devoluciones de C1/C2/C3 n-1 y C1 actual como fuentes C1 | `RegulatoryCycleScheduleCatalogTests.cs:133-150`; `AchReturnOriginalCorrelationComponent.cs:73-114,117-140`; `AchReturnsService.cs:79-107` | NO_DEMOSTRADA | **H01**: la ruta de generación comprueba cámara/selección/causal, pero no elegibilidad específica `ciclo original → ciclo devolución`; las pruebas de `Allows` son de catálogo |
| AC-C1-10 | Excepción causal R10 en devolución débito | `AchReturnEligibilityPolicyComponent.cs:123-174`; `AchReturnsFileByClearingHouseTests.cs` contiene prueba de bloqueo interdiario | PARCIAL | **H05**: R10 rechaza cualquier `ProcessingDate` distinta, incluso origen n-1; contrastar excepción R10 con tabla C1, sin afirmar que todas las causales deban ser iguales |
| AC-C1-11 | Revisión causal, fecha valor, duplicados y generación NACHA-M | `AchReturnsService.cs:83-107,128-202`; `AchReturnEligibilityService.cs:13-94`; `AchReturnOriginalCorrelationComponent.cs:149-157` | PARCIAL | Hay control de duplicados por transacción y validación normativa de causal/fecha; no sustituye matriz de 4 oportunidades |
| AC-C1-12 | Planilla → pago → liberación 07:00–08:00 | `RegulatoryCycleScheduleCatalog.cs:188-194,295-315`; `ClearingHouseCyclePolicyResolver.cs:110-167`; `AchCycleScheduler.cs:105-123` | PARCIAL | **H03**: configuración y orden validados, sin comprobar condición operativa de pago confirmado antes de liberar; I09 |
| AC-C1-13 | Liberación/recepción de respuestas y medición de horarios | `AchColombiaManagedMftHandlers.cs:251-315`; `AchColombiaManagedMftInboundWorkflow.cs:404-450` | PARCIAL | Polling automático usa ventana; si llega fuera de ventana registra observación; no demuestra que cámara pagó y conciliación cerró |
| AC-C1-14 | Despacho automático de archivos sin reenvío duplicado | `AchColombiaManagedMftHandlers.cs:23-64,67-175`; `AchColombiaManagedMftOperationalTriggerTests.cs:20-44,90-110`; `AchColombiaManagedMftInboundWorkflow.cs:40-85` | PARCIAL | **H06**: la ruta de trigger contextual usa `localDate` para buscar `ProcessingDate` y no proyecta D+1 cuando se active C1 la noche anterior; ruta alternativa sin trigger sí examina D/D+1. Comprobar si hay trigger nocturno real |
| AC-C1-15 | Salida del ciclo y cierre financiero/conciliación | Manual V35 §2.5; `AchColombiaManagedMftHandlers.cs:149-175`, `AchColombiaManagedMftInboundWorkflow.cs:399-450` | NO_DEMOSTRADA | **H03**: no hay evidencia end-to-end en esta revisión de planilla, pago, liberación, respuesta y cuadre; I09 y homologación |

## 3. Rutas recorridas

1. **Config y horario:** `RegulatoryCycleScheduleCatalog → ClearingHouseCycleConfigSeeder → ClearingHouseCyclePolicyResolver → AchCycleScheduler → OperationalCycleWindowResolver`.
2. **Generación devolución:** `AchReturnsService → AchReturnOriginalCorrelationComponent → AchReturnEligibilityPolicyComponent → AchReturnEligibilityService → AchReturnArtifactGenerationComponent`.
3. **Salida y respuesta:** `AchColombiaManagedMftOutboundHandler (dos sobrecargas) → ExecuteOutboundAsync` y `AchColombiaManagedMftInboundHandler → AchColombiaManagedMftInboundWorkflow`.
4. **Pruebas fuente identificadas:** `RegulatoryCycleScheduleCatalogTests`, `ProjectedOperationalCycleTests`, `AchColombiaManagedMftOperationalTriggerTests`, `AchReturnsFileByClearingHouseTests`. **No ejecutadas**.

## 4. Hallazgos de I02 (con severidad y veredicto prudente)

- **H01 — ALTA, brecha estática confirmada en la ruta auditada.** La matriz `ReturnWindows` queda fuera del snapshot activado; los componentes comunes `CycleTransactionPolicy` y `AchReturnsService` no validan la pareja ciclo original/ciclo devolución. La validación por causal y `MaxCycles` no recibe la matriz n-1/C4–7. Acción futura: aplicar control en el punto de generación autorizado y prueba positiva/negativa en C1, sin crear subsistema nuevo. **No declara fallo de producción medido.**
- **H02 — MEDIA, conflicto documental vigente.** V35 describe 5 ciclos; anexo suministrado 7 y errata `cinco (7)`. Se mantiene decisión interna `2026-10-14` y `11:01 a. m.` C4; vigencia ante cámara no acreditada con constancia externa.
- **H03 — ALTA por comprobar, falta demostración E2E.** Existen campos/validaciones de secuencia y ventanas MFT, pero la auditoría no verificó condición de pago de compensación efectivamente confirmado ni cuadre antes de declarar un ciclo cerrado. Se traspasa a I09.
- **H05 — MEDIA, regla R10 particular.** `AchReturnEligibilityPolicyComponent.cs:136-139` envía al flujo de reclamaciones cualquier R10 con original del día operativo previo. Debe confirmarse compatibilidad con el anexo C1 para R10; ya existe prueba que espera ese bloqueo. No cambiar sin decisión explícita.
- **H06 — MEDIA, posible desalineación de fecha de C1 en trigger contextual.** `AchColombiaManagedMftHandlers.cs:116-161` busca C1 por fecha local del disparador, pero `ProcessingDate` de C1 puede ser la siguiente jornada. El handler sin contexto (líneas 23–64) sí busca dos fechas. El test existente usa C1 a las 06:30 y no cubre las 19:01 previas. Es un riesgo **condicionado a la configuración real** y requiere test focal.

## 5. Qué se consideró suficiente, y qué no

**Suficiente para cerrar la inspección I02:** mapa completo de las quince reglas, referencias reproducibles por archivo/línea, clasificación y traslado de hallazgos. **No suficiente para certificar C1:** `dotnet test`/DB/Quartz/MFT/compensación/homologación no ejecutados en este entorno. Falta de ambiente **no bloquea continuar I03**.

**Sin modificaciones productivas ni nuevos componentes.** Si se autoriza corregir H01/H05/H06, se hará como ajuste mínimo con pruebas focalizadas en una intervención posterior, preservando el resto del plan.

## 6. Traspaso preciso a I03

Ciclo 2: cierre 09:00, planilla 09:00–09:30, pago 09:30–09:45, liberación 09:45–10:00; devoluciones de C5/C6/C7 n-1 y C1 actual. Reusar la ruta documentada y verificar especialmente el cruce interdiario, el calendario y las excepciones de causales sin copiar los riesgos no confirmados como defectos.
