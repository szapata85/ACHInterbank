# I03 — auditoría estática ACH Colombia, ciclo 2 (siete ciclos)

**Corte:** 2026-11-01 · **Incremento: TERMINADO con hallazgos abiertos** · Auditoría estática y documental, no certificación ni prueba de integración. **Código productivo y tests: sin cambios.**

## 1. Fuente y alcance

- Fuente de horarios y clases: *Nuevos Ciclos de Proceso y Horarios.pdf*, **página 1** (ciclo 2). Contraste con Manual ACH Colombia V35 (abril 2026), §2.4 y §2.7; el esquema de cinco ciclos del manual no reemplaza silenciosamente el anexo de siete, y **H02 sigue abierto**.
- Documento del propio proyecto `docs/operations/ACHCOL_CICLOS_7_VIGENCIA_2026-10-14.md`: decisión interna de activar `ACHCOL-7C-20261014` desde 2026-10-14; no se ha demostrado vigencia/despliegue externo.
- C2: **recepción 07:01–09:00**, cierre **09:00**, planilla **09:00–09:30**, pago **09:30–09:45**, liberación **09:45–10:00** (America/Bogota). Se admiten créditos, débitos, prenotificaciones crédito y débito, y devoluciones de **C5/C6/C7 del día hábil anterior** y **C1 del día operativo actual**. No inferir la aceptación de otros productos en C2 sin evidencia: PSE/SSS/DIAN no se enumeran en esa fila del anexo.
- Para devoluciones, **P** significa día hábil operativo anterior y **C** el actual. Deben aceptarse P5/P6/P7/C1 y rechazarse, entre otros, P4/C2; la tabla de cuatro fuentes se aplica por cada transacción, además de validar causal, importe y duplicados.

## 2. Auditoría regla a regla — 15 controles del ciclo 2

| ID | Regla / criterio verificable | Evidencia exacta en fuente de código | Estado estático | Hallazgo / pendiente |
|---|---|---|---|---|
| AC-C2-01 | Usar versión 7C vigente por fecha, sin mezclar CENIT ni histórico | `RegulatoryCycleScheduleCatalog.cs:152-156,245-267`; `ClearingHouseCycleConfigSeeder.cs:25-45`; `ClearingHouseCyclePolicyResolver.cs:35-66` | TRAZADA | H02: activación externa/despliegue sin evidencia |
| AC-C2-02 | Abrir a las 07:01 y cerrar/cortar a las 09:00 | `RegulatoryCycleScheduleCatalog.cs:194-200,295-315`; `OperationalCycleWindowResolver.cs:8-42`; `AchColombiaManagedMftHandlers.cs:231-245` | TRAZADA | Semántica exacta en minuto límite y despliegue no ejecutados |
| AC-C2-03 | Generar C2 solo en día hábil y preservar fecha operativa | `AchCycleScheduler.cs:40-90,95-123`; `ClearingHouseCyclePolicyResolver.cs:15-66` | TRAZADA | Falta ejecución con calendario real en entorno autorizado |
| AC-C2-04 | Orden horario de planilla 09:00–09:30, pago 09:30–09:45, liberación 09:45–10:00 | `RegulatoryCycleScheduleCatalog.cs:194-200,295-315`; `ClearingHouseCyclePolicyResolver.cs:110-167`; `AchCycleScheduler.cs:105-123` | PARCIAL | H03: la secuencia configurada no demuestra pago confirmado antes de liberar |
| AC-C2-05 | Crédito monetario permitido | `RegulatoryCycleScheduleCatalog.cs:3-17,194-200`; `CycleTransactionPolicy.cs:45-74`; `NachaTransactionValidationService.cs:37-73` | TRAZADA | Falta prueba operativa con archivo real |
| AC-C2-06 | Débito monetario permitido | Mismas rutas de `AC-C2-05`; `CycleTransactionPolicy.cs:57-65` | TRAZADA | Falta prueba operativa con archivo real |
| AC-C2-07 | Prenotificación crédito/débito permitida, clase inequívoca y valor 0 | `CycleTransactionPolicy.cs:92-105`; `NachaTransactionValidationService.cs:56-78` | TRAZADA | NACHA-M de prenotificaciones se revisará en I09 |
| AC-C2-08 | Devolución en C2 solo de P5, P6, P7 o C1 | `RegulatoryCycleScheduleCatalog.cs:174-185`; `RegulatoryCycleScheduleCatalogTests.cs:110-130` | NO_DEMOSTRADA | **H01**: existe tabla en candidato, pero no se observa consumo en la ruta productiva de devoluciones |
| AC-C2-09 | Rechazar devoluciones C2 procedentes de P4, C2 y otras fuentes no incluidas | `RegulatoryCycleScheduleCatalogTests.cs:133-150`; `AchReturnOriginalCorrelationComponent.cs:73-114,117-140`; `AchReturnsService.cs:79-107` | NO_DEMOSTRADA | **H01**: no hay comprobación fuente→destino en `AchReturnsService`; `ReturnCycleId` incluso puede omitirse y por defecto ser el ciclo original |
| AC-C2-10 | R10 C1→C2, si procede la causal, mantener orden operativo C1 antes de C2 | `AchReturnEligibilityPolicyComponent.cs:123-174`; `RegulatoryCycleScheduleCatalog.cs:188-200`; `AchReturnsFileByClearingHouseTests.cs:455-627` | INCUMPLIMIENTO_CONFIRMADO | **H07 ALTA**: `OrderBy(StartTime)` da orden `C2,C3,C4,C5,C6,C7,C1`; al elegir original C1 y retorno C2 `returnIndex < originalIndex`, se lanza `ACHCOL_R10_WINDOW_POLICY_UNRESOLVED`. Defecto estático de la ruta; impacto en despliegue no medido |
| AC-C2-11 | R10 de originales P5/P6/P7: tratar expresamente la diferencia de día | `AchReturnEligibilityPolicyComponent.cs:134-139`; `AchReturnsFileByClearingHouseTests.cs:577-592` | PARCIAL | **H05**: se redirige a reclamaciones todo R10 de fecha operativa anterior; confirmar regla especial ante cámara, sin generalizar a otras causales |
| AC-C2-12 | Causal válida, monto, fecha y duplicados antes de crear archivo | `AchReturnOriginalCorrelationComponent.cs:57-70,117-157`; `AchReturnEligibilityService.cs:13-94`; `AchReturnsService.cs:87-115,128-202` | PARCIAL | Reglas generales trazadas; no sustituyen H01 ni H07 |
| AC-C2-13 | MFT saliente C2 dentro de ventana e idempotente | `AchColombiaManagedMftHandlers.cs:23-64,67-175`; `AchColombiaManagedMftOutboundWorkflow.cs:75-121,311-329` | PARCIAL | Contexto automático localizado; sin validación de servidor, archivo real ni ACK |
| AC-C2-14 | Archivo entrante/acuse se procese únicamente en ventana de liberación C2 y se registre | `AchColombiaManagedMftHandlers.cs:251-315`; `AchColombiaManagedMftInboundWorkflow.cs:399-450` | PARCIAL | Se vigila horario; no hay prueba E2E de respuesta real y aplicación contable |
| AC-C2-15 | Cerrar el ciclo con planilla, pago, liberación y conciliación consistentes | `RegulatoryCycleScheduleCatalog.cs:194-200`; `ClearingHouseCyclePolicyResolver.cs:150-165`; `AchColombiaManagedMftInboundWorkflow.cs:399-450` | NO_DEMOSTRADA | H03: la secuencia normativa está modelada, pero no hay evidencia de barrera financiera y cuadre confirmado |

**Conteo estático I03:** 15 controles: 6 `TRAZADA`, 5 `PARCIAL`, 3 `NO_DEMOSTRADA`, 1 `INCUMPLIMIENTO_CONFIRMADO`; estos estados **no** miden cumplimiento real de cámara. 

## 3. Hallazgo prioritario nuevo: H07 — clasificación del orden en R10

**Reproducción estática (sin afirmar ejecución en DB):** la agenda activa configura C1 a las `19:01`, C2 `07:01`, C3 `09:01`, C4 `11:01`, C5 `12:31`, C6 `14:31`, C7 `16:01`. El código `AchReturnEligibilityPolicyComponent.cs:148-160` consulta todos los ciclos de la misma `ProcessingDate` y ordena por `StartTime`. Esto genera `[C2,C3,C4,C5,C6,C7,C1]`. En devolución R10 con original en C1 del mismo día operativo y destino C2, `originalIndex=6`, `returnIndex=0`; el guard `returnIndex < originalIndex` la bloquea. Para C1, la hora de apertura corresponde a la tarde del **día anterior** aunque `ProcessingDate` sea la del cierre.

**Veredicto:** la aritmética de orden de ciclos es incorrecta **en esa ruta y caso bajo política 7C**. No significa que todo retorno C1→C2 falle ni que el fallo se haya observado en un ambiente desplegado; otros códigos de devolución siguen otra ruta. Las pruebas R10 existentes usan escenarios sintéticos y no prueban C1 19:01 → C2 07:01 con la agenda 7C completa.

**Corrección mínima para planificar, no ejecutada:** obtener el orden operativo por `CycleNumber` de la política vigente (o instante de ventana asociado a fecha operativa), no por `StartTime` local sin día; agregar regresión R10 C1→C2 permitida y caso fuera de ventana bloqueado. Evitar cambios transversales en el motor de ciclos.

## 4. Evidencias y límites

- Hallazgo H01 **persiste** en C2: búsqueda `rg 'ReturnWindows|EligibleSources|RegulatoryCycleReturnWindow' src --glob '!**/RegulatoryCycleScheduleCatalog.cs'` no arrojó usos adicionales al catálogo. `RequireActivatable` produce un `RegulatoryCyclePolicySnapshot` sin matriz (`RegulatoryCycleScheduleCatalog.cs:19-27,116-128`).
- `RegulatoryCycleScheduleCatalogTests.cs:110-150` sí verifica P5/P6/P7/C1 y el rechazo de P4 en la tabla candidata; **no prueba su invocación productiva**.
- Ruta real identificada: `AchReturnsService → AchReturnOriginalCorrelationComponent → AchReturnEligibilityPolicyComponent / AchReturnEligibilityService → AchReturnArtifactGenerationComponent`. La política `CycleTransactionPolicy` valida solo `AllowsReturn` por ciclo (`CycleTransactionPolicy.cs:56-71`).
- Revisión con lectura de fuente y búsqueda local; `dotnet` **no está instalado** en este entorno. No se ejecutaron tests .NET/DB/Quartz/MFT, no se inspeccionó sistema bancario real. No bloquear I04 por esto.
- **Sin código nuevo, sin cambios productivos, sin nuevos servicios o scripts en el proyecto.** I03 documenta, no corrige.

## 5. Traspaso a I04

Ciclo 3: 09:01–11:00; planilla 11:00–11:30; pago 11:30–11:45; liberación 11:45–12:00; ventana de devoluciones P6/P7 y C1/C2. En I04, además de H01, verificar si H07 afecta también el retorno R10 de origen C1 a C3. Revisar los productos especiales solo si están incluidos en la fila fuente, sin asumir que aplican en todos los ciclos.
