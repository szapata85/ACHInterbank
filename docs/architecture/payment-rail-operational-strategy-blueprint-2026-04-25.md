# ACHInterbank — Blueprint de transición `ClearingHouse` ➜ `PaymentRailOperationalStrategy` (Prompt 1)

Fecha: 2026-04-25  
Alcance: auditoría arquitectónica y diseño objetivo (sin implementación funcional).

---

## 0) Resumen ejecutivo

El estado actual de ACHInterbank ya contiene piezas críticas para operar ACH Colombia/CENIT (ciclos, ingestión NACHA-M inbound, command center, state machine, neteo/liquidez CENIT, trazabilidad y resiliencia), pero el eje de decisión operacional sigue acoplado a `ClearingHouseId`, `ClearingHouse.Code` y condicionales por cámara en servicios de dominio/aplicación.  

La recomendación es introducir una capa explícita de riel de pago (`PaymentRail`) y una estrategia operacional por riel (`IPaymentRailOperationalStrategy`) que encapsule decisiones de:

- resolución de ciclo,
- elegibilidad/dispatch,
- retornos,
- neteo,
- liquidez,
- observabilidad,
- y capacidades habilitadas.

Esto permite mantener ACH/CENIT como primer riel operativo (fase inmediata) y sumar rieles futuros (SWIFT/BRE-B/tarjetas/intrabancario) sin modificar la lógica existente de ACH/CENIT.

---

## 1) Inventario del diseño actual ACH/CENIT

### 1.1 Entidades y modelo base

El modelo operativo está centrado en cámara de compensación:

- `AchCycle` referencia `ClearingHouseId` + navegación `ClearingHouse`, y concentra ventana operativa/cutoff.  
- `ClearingHouse` define identidad operativa (`Code`, `OriginCode`) y relaciones con ciclos e instituciones.  
- `AchDbContext` expone objetos ACH/CENIT de ciclo, ingestión inbound, cola de dispatch, neteo, liquidez, retorno de devolución y auditoría asociada.  
- El catálogo NACHA config ya semilla `ACH` y `CENIT` como cámaras activas.

### 1.2 Mecanismos actuales de estrategia

Ya existe un patrón Strategy orientado a cámara:

- `IClearingHouseStrategy` (validación transacción + nombre/archivo).
- `ClearingHouseStrategyFactory` con `switch` explícito (`1 => ACH`, `2 => CENIT`).
- Implementaciones `AchClearingHouseStrategy` y `CenitClearingHouseStrategy`.

**Hallazgo:** el patrón existe, pero con dos límites:

1. está acoplado a ID de cámara;
2. cubre solo generación/validación puntual, no operación integral del riel (dispatch/returns/netting/liquidez/capabilities).

### 1.3 Flujo inbound NACHA-M relevante

La ingestión inbound ya está segmentada en subservicios:

- resolución de ciclo/cámara (`IncomingNachaCycleResolver`),
- política de elegibilidad (`IncomingNachaDispatchEligibilityPolicy`),
- planeador de cola (`IncomingNachaDispatchPlanner`),
- máquina de estados manual (`IncomingNachaStateMachineService`),
- command center para acciones manuales y timeline.

Esto es una base excelente para mover reglas a estrategias de riel sin rehacer todo el pipeline.

### 1.4 Operación CENIT especializada ya existente

La ejecución CENIT separa neteo y optimización de liquidez:

- `CenitCycleExecutionService` valida que la cámara sea CENIT y luego ejecuta neteo + liquidez.
- `LiquidityOptimizationService` aplica reglas de diferimiento/rechazo por índice de ciclo y liquidez disponible.
- Configuraciones EF dedicadas (`CenitNettingExecutions`, `LiquidityOptimizationDecisions`) muestran persistencia auditable de decisiones.

---

## 2) Mapa de dependencias por cámara (estado actual)

## 2.1 Dependencias transversales en `ClearingHouseId/Code`

- Ciclo, routing, dispatch e integración usan `ClearingHouseId` como eje de filtro/decisión.
- Varias reglas de negocio dependen de comparaciones directas (`ClearingHouseId == 2`, `Code == "CENIT"`, inferencia por nombre/archivo).

## 2.2 Mapa sintético por dominio operacional

| Dominio operacional | Componente actual | Dependencia por cámara |
|---|---|---|
| Selección de estrategia | `ClearingHouseStrategyFactory` | `switch` por ID (`1/2`) |
| Generación outbound | `NachaFileBuilder` | detección de cámara por nombre/código y políticas de settlement/type7 |
| Resolución inbound ciclo | `IncomingNachaCycleResolver` | OriginCode + inferencia por nombre + estatus especial CENIT |
| Elegibilidad dispatch inbound | `IncomingNachaDispatchEligibilityPolicy` | prioridad distinta por `ClearingHouseId` |
| Encolamiento dispatch | `IncomingNachaDispatchPlanner` | persiste `ClearingHouseId` de ciclo |
| Routing inicial de transacción | `RoutingStrategyService` | selección por preferencias de cámara |
| Ejecución CENIT | `CenitCycleExecutionService` | guard clause exclusiva CENIT |
| Neteo/Liquidez | `LiquidityOptimizationService` | enlazado al ciclo/cámara y reglas por secuencia de ciclo |
| Retorno de devolución | `ReturnOfReturnOrchestrator` | amarre a `CenitCycleExecutionId` cuando aplica |

---

## 3) Decisiones operativas actualmente dispersas

Decisiones que hoy están distribuidas (y deben converger por estrategia de riel):

1. **Resolución de identidad operativa** (cámara detectada/inferida) en resolver inbound.
2. **Priorización de dispatch** (ej. CENIT prioriza diferente).
3. **Ventana operativa y gating** (eligibilidad/blocked/waiting window).
4. **Neteo y optimización de liquidez** (especialización CENIT dentro de servicio dedicado).
5. **Reglas de archivo/formato** (naming/identifier/settlement/type7).
6. **Fallback/inferencia por convenciones de nombre** (riesgo para rieles futuros si se replica ad hoc).
7. **Observabilidad de decisiones** separada por componente, no por capacidad de riel.

Riesgo principal: al agregar SWIFT/BRE-B, la dispersión crecerá con condicionales (`if rail == ...`) y se erosionará Clean Architecture.

---

## 4) Diseño objetivo multi-riel (target)

## 4.1 Modelo conceptual

Introducir conceptos explícitos:

- **PaymentRail**: identidad de riel (ACH_COLOMBIA, CENIT, SWIFT, BREB, TARJETA, INTRABANK, etc.).
- **RailOperationalStrategy**: contrato de operación integral por riel.
- **RailCapabilityModel**: catálogo auditable de capacidades habilitadas por riel (dispatch, returns, netting, liquidity, messaging format, settlement model, etc.).

`ClearingHouse` no desaparece; pasa a ser **atributo/implementación de rail profile ACH-like**, no el eje único de arquitectura.

## 4.2 Patrón de resolución

Agregar un resolver/factory por riel:

- `IPaymentRailStrategyResolver.Resolve(railCode)`
- fallback explícito controlado (`UnknownRailStrategy` fail-closed para operaciones críticas).

No usar `switch` rígido por IDs de DB para la decisión operativa principal.

## 4.3 Separación por capacidades

La estrategia de riel expone módulos/capacidades (componibles):

- Cycle Capability
- Dispatch Capability
- Returns Capability
- Netting Capability
- Liquidity Capability
- File/Messaging Capability
- Compliance & Audit Capability

Cada capacidad declara si aplica (`Enabled`, `NotSupported`, `Planned`) y su modo operativo.

---

## 5) Propuesta de interfaces (blueprint)

> Nota: contrato propuesto para siguientes prompts; no implementado en este prompt.

```csharp
public interface IPaymentRailOperationalStrategy
{
    string RailCode { get; }              // ACH_COLOMBIA, CENIT, SWIFT...
    string RailFamily { get; }            // ACH, RTGS, CARD, INTERNAL...

    IRailCapabilityDescriptor Capabilities { get; }

    Task<CycleResolutionResult> ResolveCycleAsync(RailCycleResolutionRequest request, CancellationToken ct);
    Task<DispatchEligibilityResult> EvaluateDispatchEligibilityAsync(RailDispatchEligibilityRequest request, CancellationToken ct);
    Task<DispatchPlanResult> PlanDispatchAsync(RailDispatchPlanRequest request, CancellationToken ct);

    Task<ReturnDecisionResult> EvaluateReturnAsync(RailReturnRequest request, CancellationToken ct);

    Task<NettingResult> ExecuteNettingAsync(RailNettingRequest request, CancellationToken ct);
    Task<LiquidityDecisionResult> ExecuteLiquidityAsync(RailLiquidityRequest request, CancellationToken ct);

    Task<FileArtifactResult> BuildOutboundArtifactAsync(RailOutboundArtifactRequest request, CancellationToken ct);
}
```

Factory/resolver:

```csharp
public interface IPaymentRailOperationalStrategyResolver
{
    IPaymentRailOperationalStrategy Resolve(string railCode);
}
```

Compatibilidad temporal:

- `IClearingHouseStrategy` se mantiene durante migración como adaptador interno para ACH/CENIT.
- `AchColombiaOperationalStrategy` y `CenitOperationalStrategy` pueden envolver lógica existente mientras se extrae por capacidad.

---

## 6) Propuesta de capabilities por riel

## 6.1 Capability matrix inicial

| Capability | ACH Colombia | CENIT | SWIFT (futuro) | BRE-B (futuro) |
|---|---|---|---|---|
| Cycle orchestration | Sí | Sí | Parcial/No (según diseño) | Sí |
| NACHA-M parsing/building | Sí | Sí (con reglas propias) | No | No |
| Inbound dispatch to Proc_Transacciones | Sí | Sí | No | No |
| Returns / Return-of-return | Sí | Sí (mayor criticidad) | Diferente estándar | Diferente estándar |
| Netting | Limitado | Sí (activo) | No (normalmente RTGS) | Por definir |
| Liquidity optimization | Limitado | Sí (activo) | No/externo | Por definir |
| Operational state machine manual | Sí | Sí | Sí (con otros eventos) | Sí |
| Observability & audit decision logs | Sí | Sí | Sí | Sí |

## 6.2 Principios de capability model

1. Capacidad explícita > `if` implícito.
2. Capacidad auditable en DB (quién, cuándo, por qué se habilitó/inhabilitó).
3. Configuración versionada por riel/entorno con trazabilidad.
4. Para cálculos NACHA críticos: capacidad marcada como **non-editable logic core** (solo parámetros permitidos y auditados).

---

## 7) Estrategia de migración por fases

## Fase 1 (inmediata): Bridge `ClearingHouse` ➜ `PaymentRail`

- Introducir `IPaymentRailOperationalStrategy` y resolver.
- Crear mapping explícito `ClearingHouse -> RailCode` para ACH/CENIT.
- Mantener servicios actuales operando; agregar adapter layer.

## Fase 2: Estrategias concretas ACH/CENIT

- Implementar `AchColombiaOperationalStrategy`.
- Implementar `CenitOperationalStrategy`.
- Encapsular llamadas existentes (cycle resolver, dispatch planner, netting/liquidity CENIT, builder).

## Fase 3: Mover decisiones dispersas a estrategia

- Migrar prioridad/ventana/dispatch gating a capability de dispatch.
- Migrar resolución de ciclo e inferencias a capability de cycle resolution.
- Migrar netting/liquidity CENIT a capacidades formales del rail.

## Fase 4: Capability registry auditable

- Persistir catálogo de capacidades por riel/version/entorno.
- Exponer endpoint de consulta de capacidades efectivas.
- Instrumentar eventos estructurados por capability.

## Fase 5: Onboarding de nuevo riel sin tocar ACH/CENIT

- Agregar `SwiftOperationalStrategy` (o `BrebOperationalStrategy`).
- Reusar contratos y pipeline comunes.
- Cero cambios en estrategias ACH/CENIT salvo integración transversal de plataforma.

---

## 8) Riesgos y controles

## 8.1 Riesgos técnicos

1. **Regresión funcional ACH/CENIT** por mover lógica de forma abrupta.
2. **Duplicidad temporal de reglas** (viejas vs nuevas) en transición.
3. **Inconsistencia de observabilidad** entre ruta legacy y ruta strategy.
4. **Acoplamiento oculto** a `ClearingHouseId` en servicios no inventariados.

## 8.2 Controles recomendados

- Shadow compare por capability crítica (dispatch eligibility, cycle resolution, netting/liquidity).
- Feature flags por rail/capability (rollout gradual).
- Golden datasets ACH/CENIT (inbound/outbound) con snapshot expected.
- Pruebas contractuales de estrategia (suite común que todo rail debe pasar).
- KPIs de equivalencia: mismo resultado legacy vs strategy > umbral antes de cutover.

---

## 9) Compatibilidad hacia atrás (backward compatibility)

1. Mantener endpoints y DTOs actuales en esta transición.
2. Mantener semántica de `ClearingHouseId` en tablas existentes.
3. Usar adaptadores para que servicios legacy invoquen estrategia sin romper firma pública.
4. No alterar cálculos NACHA críticos ni fallback/shadow compare vigente.
5. Migrar de forma incremental, reversible por feature toggle.

---

## 10) Plan de prompts siguientes (roadmap sugerido)

### Prompt 2 — Contratos y modelo de dominio multi-riel

- Definir interfaces y modelos (`IPaymentRailOperationalStrategy`, descriptors, requests/results).
- Diseñar mapping `ClearingHouse <-> RailCode`.
- Definir ADR de transición.

### Prompt 3 — Implementación fase 1 (adapter bridge)

- Implementar resolver y strategy base.
- Adaptar pipeline inbound para resolver estrategia por rail.
- Sin mover lógica pesada aún.

### Prompt 4 — Implementación fase 2 (ACH/CENIT strategies)

- Crear `AchColombiaOperationalStrategy` y `CenitOperationalStrategy`.
- Encapsular componentes existentes como wrappers.

### Prompt 5 — Implementación fase 3 (migración de decisiones)

- Mover cycle resolution + dispatch eligibility/planning + returns.
- Activar shadow compare legacy vs strategy.

### Prompt 6 — Capability registry auditable

- Modelo EF + API para capacidades por riel/version.
- Auditoría de cambios y effective-dating.

### Prompt 7 — Hardening QA/regulatorio

- Pruebas de regresión y no-regresión.
- Matriz normativa ACH/CENIT y evidencia de equivalencia.

### Prompt 8 — Onboarding rail futuro (esqueleto SWIFT o BRE-B)

- Implementar estrategia stub con capacidades declaradas.
- Validar que no se modifica ACH/CENIT para integrar nuevo riel.

---

## Anexo A — Evidencia de auditoría (fuentes internas)

Componentes clave revisados para este blueprint:

- Modelo base ACH/ClearingHouse/Cycle y DbContext.
- Factory/Strategy actual por cámara.
- Resolver de ciclo inbound, elegibilidad y planner de dispatch.
- State machine del command center inbound.
- Ejecución CENIT (netting/liquidity) y persistencia auditable asociada.

