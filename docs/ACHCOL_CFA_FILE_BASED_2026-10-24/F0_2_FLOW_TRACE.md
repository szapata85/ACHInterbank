# F0.2 — Trazabilidad estática de entrada NACHA-M hasta SOAP CFA

**Programa:** `ACHCOL-NACHA-CFA-EXTCYCLE-001`  
**Sesión:** SES-002, 2026-10-24  
**Tipo:** investigación técnica e identificación de acoplamientos; **no se cambió código funcional**.  
**Fuente:** proyecto extraído del ZIP F0.1, `ACH-Interbank`, `0b4ce1c809ca5acfdafaf455cae4d0eaa5292f07`; archivos C# y modelos existentes.  
**Alcance de evidencia:** lectura estática, no compilación ni conectividad CFA.

## 1. Flujo actual y puntos de corte

```text
Nombre ACHCOL RRRRTTT.ZZZ.N + encabezado NACHA-M (FileIdModifier)
  |-- ExternalFileNameSupport.Parse   [parser existente limitado a flujo NachaOut Outbound]
  |-- IncomingNachaCycleResolver.ResolveAsync
  |       (usa CenitOfficialFileNameParser, busca AchCycles por cámara/fecha;
  |        sin candidato => NoResuelto)
  |-- IncomingNachaIngestionAppService.IngestAsync
  |       (si !resolution.IsResolved => parsing NoEjecutado y retorno)
  |-- IncomingNachaAdmissionValidator.ValidateAsync
  |       (sin AchCycleId => CYCLE_NOT_RESOLVED; fila ausente => CYCLE_NOT_CONFIGURED)
  |-- NachaParserService.ParseAndSaveDetailedAsync
  |       (conserva FileIdModifier; asigna AchCycleId desde resolución)
  |-- IncomingNachaDispatchPlanner.PlanForIngestionAsync
  |       (link a AchTransaction obligatorio; AchTransaction.AchCycle requerido)
  |-- IncomingNachaDispatchEligibilityPolicy.EvaluateAsync
  |       (exige ResolvedAchCycleId y consulta ventana de AchCycle)
  |-- IncomingNachaPostProcessingPipeline
  |       (carga AchTransaction.AchCycle, calendario y ventana; prepara request)
  |-- ProcTransaccionesRequestMapper.ResolveAsync
  |       (requiere AchTransaction y AchCycle, fuentes cycle.id/cycle.processingdate)
  |-- WscfaachSoapClient.ProcTransaccionesAsync
  |       (solo transporte Live; DryRun genera respuesta sintética sin transmisión)
```

### Tabla de evidencia de código

| Componente (bajo `src/`) | Referencia estática verificada | Consecuencia para archivos H/I con sufijo .8/.9 sin ciclo interno |
|---|---|---|
| `Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ExternalFileNames/ExternalFileNameSupport.cs` | `AchRegex` línea 12; `Parse` ~40-63; `IsAch` ~100; `BuildAchName` ~129 | Existe regex que admite `.8`, `.9`, `.10` y sufijos enteros positivos, pero `IsAch` se circunscribe a `NachaOut` + `Outbound`; **no demuestra parseo inbound**. |
| `.../IncomingNachaCycleResolver.cs` | `ResolveAsync` ~40; obtención `fileCycleNumber` ~54; búsqueda por `AchCycles` ~116; retornos `NombreSinCandidato` ~142 y `SinCandidatos` ~266 | El ciclo externo puede obligar a encontrar `AchCycle`, y la ausencia de candidatos resulta en `NoResuelto`. |
| `.../IncomingNachaIngestionAppService.cs` | `IngestAsync` ~58; `if (!resolution.IsResolved)` ~421-446 | El flujo **retorna antes del parser** cuando falta resolución. |
| `.../IncomingNachaAdmissionValidator.cs` | `ValidateAsync` ~29; `CYCLE_NOT_RESOLVED` ~76; `CYCLE_NOT_CONFIGURED` ~102 | Incluso habilitando ingesta, admisión exige `AchCycleId` y fila asociada. |
| `.../NachaParserService.cs` | `ParseAndSaveDetailedAsync`; `cycleNumber` ~411; `request?.ResolvedAchCycleId` ~433; `FileIdModifier` ~466; `CycleNumber`/`AchCycleId` ~474 | Hay campo para letra y número, pero el número leído proviene de parser CENIT; no es aún un contrato ACHCOL independiente. |
| `.../IncomingNachaDispatchEligibilityPolicy.cs` | `EvaluateAsync` ~13; condición `ResolvedAchCycleId` ~26; accesos `transaction.AchCycle` ~50-110 | Bloquea o difiere en ausencia de ciclo resuelto/ventana. |
| `.../IncomingNachaDispatchPlanner.cs` | `PlanForIngestionAsync` ~44; exige link a `AchTransaction`, `Include(x=>x.AchCycle)` ~65; idempotencia `tx.AchCycleId` ~96; cola `AchCycleId` ~164 | Cola actual acoplada a transacción y ciclo. |
| `Cfa.ACHInterbank.Domain/Models/ACH/IncomingNachaDispatchModels.cs` | `IncomingNachaDispatchQueue` propiedades `AchTransactionId int` y `AchCycleId string` no nulas | Requiere revisión del modelo/migraciones o creación de cola alternativa sin falsa FK. |
| `.../IncomingNachaPostProcessingPipeline.cs` | `ResolveQueueContextAsync` ~414; ventana/calendario ~506/563/580; mapper ~1109; envío ~1133 | Requiere `queue.AchTransaction.AchCycle` en más de un punto; un bypass único no soluciona el despacho. |
| `.../ProcTransaccionesRequestMapper.cs` | `ResolveAsync` ~48 recibe `AchTransaction` y `AchCycle`; `IntegrationExecutionContext` usa `cycle.Id` ~88; mapeos `cycle.*` ~300 | Hay que homologar fuente alterna para atributos SOAP y configuración de mapping; no reemplazar `cycle.id` con 8/9 arbitrariamente. |
| `Cfa.ACHInterbank.External/Connections/WscfaachSoapClient.cs` | `ProcTransaccionesAsync` ~53 | El cliente puede reutilizarse; su existencia **no demuestra** que lleguen H/I sin ciclo ni aceptación remota. |

**Nota:** las líneas son aproximadas respecto al snapshot identificado y sirven para navegación; para las decisiones de implementación deben revalidarse tras cualquier modificación.

## 2. Separación requerida

- `FileIdModifier`: `H` o `I` del registro 1.
- `FileSequence`: `008` o `009` (componente `ZZZ`), validable contra la tabla oficial según perfil.
- `ExternalCycleNumber`: `8`, `9` o entero positivo `N` del sufijo; no se presume igual a `ZZZ`.
- `AchCycleId`: vínculo interno **opcional** para ruta ACHCOL `AchColFileBased`; en rama legacy sigue usándose.

El parser NACHA-M ACHCOL de entrada debe interpretar el nombre independientemente de `CenitOfficialFileNameParser`. Es posible que por separación de flujos la operación saliente existente ya admita `.8/.9`, sin que ello pruebe recepción o despacho entrante.

## 3. Propuesta para el ADR F0.3 (no aprobada aún)

1. Crear `AchColExternalFileNameParser` inbound y contexto inmutable con cámara, fecha, letra, ZZZ, `ExternalCycleNumber`, `AchCycleId?`.
2. Para `ACHCOL`, diferenciar resultado de cámara/fecha validado y relación opcional con ciclo; no admitir formato/fecha/cámara inválidos.
3. Resolver estrategia para cola y mapper sin `AchCycle`: variante de origen NACHA o nueva cola, sin fabricar ciclos.
4. Reusar cliente SOAP y respuesta; realizar DryRun por archivo H/I cuando exista un entorno de compilación local.
5. Revisar colisiones con programa `CFA-SOAP-UX-MAP-001` antes de modificar `ProcTransaccionesRequestMapper` o artefactos de mappings. Su `MEMORY_STATE.md` no se modifica aquí.

## 4. Política de implementación incremental aprobada por el solicitante

**La ausencia de ambientes de integración, bases externas, cliente CFA habilitado o UAT NO suspenderá las microimplementaciones.** Se seguirá ejecutando el backlog de desarrollo y actualizando el paquete completo por microincremento. La prueba final de integración/homologación se consolida **al finalizar todas las implementaciones**, en la fase F6. Esto no autoriza transmisiones monetarias `Live` ni permite declarar pruebas no ejecutadas como satisfactorias.

Evidencia recomendada por incremento: lectura estática, compilación o pruebas locales **si están disponibles**, pruebas automatizadas simuladas y verificación de cambios. Si no son ejecutables, se registra `IMPLEMENTED_UNVERIFIED` junto con deuda de verificación en F6 y **se avanza a la siguiente tarea**.

## 5. Resultado y siguientes pasos

- **F0.2:** `VERIFIED` **solo** para el mapa estático anterior y la identificación de dependencias; sin cambio funcional.
- **Ninguna prueba E2E, SOAP real, BD o .NET ejecutada:** se difiere expresamente a F6; ello no es bloqueo al desarrollo.
- **Próxima tarea:** `F0.3`, ADR de arquitectura; luego `F1.1`, primer cambio funcional (parser del nombre ACHCOL). Continuar incrementos hasta F6 sin detenerse por falta de ambiente.
