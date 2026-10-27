> **ESTADO VIGENTE F6.3 (2026-10-25):** fuente de continuidad `ACHInterbank_ACHCOL_CFA_F6_3_UNIFICADO.zip`; `last_completed_microtask=F6.3`, `next_microtask=null`. El expediente `F6_3_VERIFICATION_REPORT.md` documenta verificaciones offline y **certificación externa PENDIENTE**. NO se ejecutaron compilación .NET, xUnit, migraciones reales, XSD ni UAT CFA. R5C falla en ratchet histórico y R7 quedó inconcluso. **NO activar Live, no desbloquear ACHCOL ni reintentar sin conciliación.** El siguiente trabajo es cerrar `CERTIFICATION_GATE_PENDING` con evidencias en ambiente autorizado, no iniciar una nueva fase de implementación.

> **ESTADO VIGENTE F6.2 (2026-10-25):** base y entrega `ACHInterbank_ACHCOL_CFA_F6_2_UNIFICADO.zip`; `next_microtask=F6.3`. El Command Center existente expone 6 KPIs ACHCOL agregados; runbook F6.2 define observacion, GO/NO-GO y reversión por flag OFF. Sin SDK .NET 10/BD/XSD homologados: **NO** se ejecutó compilación, xUnit ni UAT y **NO** se autoriza Live. La deuda F6.1 (R5C/R7, migraciones, pruebas) sigue abierta. Consultar `F6_2_IMPLEMENTATION.md`, `F6_2_OPERATIONS_RUNBOOK.md` y `MEMORY_STATE.md` 1.19.0.

> **ESTADO VIGENTE F6.1 (2026-10-25):** base y nueva entrega `ACHInterbank_ACHCOL_CFA_F6_1_UNIFICADO.zip`; `next_microtask=F6.2`. Auditoría reproducible `scripts/validate-achcol-f61-offline.py` **12/12**, grafo 7/7 PASS, R5D PASS, NAV3.4J 145 PASS; ratchet R5C FAIL por SHA antigua, R7 inconcluso. Guard DryRun endurecido y test nuevo escrito. .NET SDK 10.0.402/DB no disponibles: **NO** hubo compilación, xUnit ni migraciones reales; conservar deuda abierta para F6.3. F6.2: observabilidad y rollout/rollback, **NO habilitar Live**. Leer `F6_1_IMPLEMENTATION.md` y `MEMORY_STATE.md` 1.18.0.

> **ESTADO VIGENTE F5.3 (2026-10-25):** proyecto único `ACHInterbank_ACHCOL_CFA_F5_3_UNIFICADO.zip`; `next_microtask=F6.1`. F5.3 incorporó harness local/inspección del XML DryRun F5.2, con allowlist de parámetros, rechazo XML inseguro y evidencia solo con nombres de campos. Se siguen bloqueando `Live` y el cliente CFA. WSDL importa `xsd0` y `xsd1` externos no entregados; homologación pendiente. Tests escritos **sin ejecución** (.NET 10 ausente). Leer `F5_3_IMPLEMENTATION.md`, `MEMORY_STATE.md` v1.17.0 y preparar F6.1 como fase de verificación, no envíos monetarios sin autorización.

> **ESTADO VIGENTE F5.2 (2026-10-25):** fuente `ACHInterbank_ACHCOL_CFA_F5_2_UNIFICADO.zip`; `next_microtask=F5.3`. Proyección DryRun desde EntryDetail incorporada, muestra XML SOAP parcial y campos no homologados, devuelve CanTransmit=false y no llama CFA. Mantiene Bloqueado F4.2/F4.3. No hay compilación .NET ni XSD externos. Ver `F5_2_IMPLEMENTATION.md` y `MEMORY_STATE.md` 1.16.0.

> **ESTADO VIGENTE F5.1 (2026-10-25):** proyecto de continuidad `ACHInterbank_ACHCOL_CFA_F5_1_UNIFICADO.zip`; `next_microtask=F5.2`. F5.1 documentó 25 parámetros de entrada de `Proc_Transacciones` en inventario código/Markdown, con estados de fuente candidata/condicional/no resuelta. El WSDL disponible importa XSD externos no proporcionados, por lo que la homologación externa sigue abierta. Se protege el mapper heredado contra `AchColFileBased` con transacción/ciclo ficticios, sin modificar `CycleBound`, servicio SOAP ni DB. El envío CFA sigue bloqueado. Leer `F5_1_IMPLEMENTATION.md` y `MEMORY_STATE.md` 1.15.0. F5.2: adaptar origen `EntryDetail` y DryRun protegido, cero Live.

# CHATGPT_MICRO_START — ACHCOL-NACHA-CFA-EXTCYCLE-001

> **ESTADO VIGENTE F4.3 (2026-10-25):** fuente única `ACHInterbank_ACHCOL_CFA_F4_3_UNIFICADO.zip`, `next_microtask=F5.1`. F4.3 protege el servicio y las acciones visuales del Command Center contra retry/unblock/requeue `AchColFileBased`; deja auditoría y bloquea cierre con intento monetario. El preflight F4.2 sigue siendo simulación local sin SOAP incluso con configuración `Live`. Sin nueva cola, migración ni motor. Pruebas xUnit escritas **sin ejecutar** (SDK ausente). Consultar `F4_3_IMPLEMENTATION.md` y `MEMORY_STATE.md` 1.14.0. **F5.1:** definir/homologar mapa de campos SOAP contra EntryDetail, no enviar CFA antes de F5/F6.


## Instrucción de continuidad (pegar en una nueva sesión)

Trabaja con la implementación incremental de ACHINTERBANK para archivos NACHA-M ACH Colombia (`H/008`, `I/009`, archivos físicos `RRRRTTT.ZZZ.AAAAMMDD.N.OUT` (en claro) o `...N.OUT.env` (sobre digital cifrado), con `N=8`, `9` o mayores) enviados a servicios SOAP CFA aunque no existan ciclos internos `AchCycle` equivalentes.

1. Lee **primero** `docs/ACHCOL_CFA_FILE_BASED_2026-10-24/MEMORY_STATE.md` y `docs/ACHCOL_CFA_FILE_BASED_2026-10-24/PLAN.md` del checkout actualizado.
2. **No repitas fases finalizadas** ni supongas avances sin evidencia en `MEMORY_STATE.md`. Ejecuta `next_microtask`, actualiza su estado y, si es viable, continúa incrementos. **No detengas desarrollo por ausencia de pruebas de ambiente**. Pruebas finales E2E/UAT después de implementar F1–F5, en F6.
3. Antes de editar, captura Git HEAD y `git status`. Revisa si hay solapamientos con `docs/CFA_SOAP_UX_MAPPING_IMPLEMENTATION_PACKAGE_2026-10-12/MEMORY_STATE.md`; no lo sobrescribas.
4. Conserva la independencia `FileIdModifier` ≠ `FileSequence/ZZZ` ≠ `ExternalCycleNumber/N` ≠ `AchCycleId`; la falta de `AchCycle` no bloquea **por sí sola** la ruta ACHCOL. No mezcles parsers CENIT y ACHCOL.
5. No ejecutes envíos SOAP monetarios `Live` sin entorno/autorización CFA, mapeos aprobados, conciliación e idempotencia. Usa `DryRun` para el desarrollo.
6. Al cerrar **cada microimplementación**, registra archivos modificados, verificaciones disponibles realmente ejecutadas y pruebas diferidas para F6, decisiones, riesgos, hashes y `next_microtask`. **Entrega siempre ZIP unificado nuevo con proyecto completo y modificado + documentación actualizada**; nunca solo parches.

**Estado vigente SES-012 / F3.3:** F1.1–F3.3 están implementadas en código, con verificación .NET/BD pendiente. F3.3 añadió planner por `EntryDetail` ACHCOL sin ciclo, clave SHA-256 de negocio, link opcional, migraciones PostgreSQL/SQL Server y protección de cola `Blocked` (no envío). `next_microtask=F4.1`: política de elegibilidad independiente de ciclo/cámara/clase; no liberar trabajadores ni SOAP hasta F4/F5. Fuente exclusiva de continuidad: `ACHInterbank_ACHCOL_CFA_F3_3_UNIFICADO.zip`. Leer [`F3_3_IMPLEMENTATION.md`](F3_3_IMPLEMENTATION.md). Pruebas finales F6.

## Material de referencia

- `PLAN.md` — alcance F0–F6, criterios AT-01–AT-20, límites normativos y de homologación.
- `MEMORY_STATE.md` — estado incremental de la implementación, deuda de verificación, backlog y bitácora.
- `F0_2_FLOW_TRACE.md`, `F0_3_ADR_ARCHITECTURE.md`, `F0_3_A_NAMING_OUT_ENVELOPE.md`, `F1_1_IMPLEMENTATION.md`, `F1_2_IMPLEMENTATION.md`, `F2_1_IMPLEMENTATION.md` y `F2_2_IMPLEMENTATION.md`, `F2_3_IMPLEMENTATION.md`, `F3_1_IMPLEMENTATION.md`, `F3_2_IMPLEMENTATION.md` y `F3_3_IMPLEMENTATION.md` — trazabilidad, arquitectura y primera implementación de código; lectura previa para F1.2.
- `ACHInterbank_ajustado.zip` — fuente base aportada; los contenidos completos ahora están desplegados en este mismo paquete unificado (carpeta raíz `ACHInterbank_ajustado/`). SHA-256 en `F0_1_BASELINE.md`.
- `ACH-Colombia-V35.md` — correspondencia `A–Z ↔ 001–026` y nombre publicado `RRRRTTT.ZZZ.1` para el caso del manual.
- `WSCFAACH.wsdl (1).xml` — interfaz suministrada; verificar importaciones/contrato externo.

## Entrega unificada

Este archivo viaja **dentro del código fuente** en `docs/ACHCOL_CFA_FILE_BASED_2026-10-24/`. El paquete único contiene el contenido descomprimido original (incluido `.git`) más este directorio documental, sin incluir un segundo ZIP anidado. Para la próxima sesión utilizar el **último** ZIP unificado como entrada; no regresar al ZIP inicial sin los avances. Cada incremento sucesivo debe volver a generar el ZIP completo, aun si la validación de ambiente queda para F6.

## Entrega F2.1 — resolver de contexto ACHCOL sin ciclo (2026-10-24)

Partir del ZIP completo `ACHInterbank_ACHCOL_CFA_F2_1_UNIFICADO.zip`. Resolver de ACHCOL añade estado `ResueltoSinCiclo`, contexto `AchColFileBased`, separación de `ZZZ/N/AchCycleId` y flag desactivado por defecto. Se agregaron pruebas en `IncomingNachaCycleResolverTests.cs`, **sin ejecutar** por falta de .NET 10. Reporte [`F2_1_IMPLEMENTATION.md`](F2_1_IMPLEMENTATION.md), PLAN y MEMORY_STATE actualizados. **Siguiente microtarea F2.2**, desacoplar admission manteniendo validaciones comunes, no saltar directamente a SOAP. Entregar siempre nuevo ZIP fuente completo al finalizar.

## Estado vigente tras F3.2 (2026-10-24)

Fuente de continuidad: **`ACHInterbank_ACHCOL_CFA_F3_2_UNIFICADO.zip`**. PostgreSQL F3.1 y SQL Server F3.2 contienen migraciones de la cola sin ciclo (ninguna ejecutada), snapshots por proveedor, check/FK/índice y protección rollback. **`next_microtask=F3.3`**: idempotencia y planificación del origen NACHA `AchColFileBased`, sin habilitar worker ni SOAP Live hasta fases posteriores. Validar primero el `MEMORY_STATE.md` incluido. Código y pruebas sin compilar ni ejecutar por falta de .NET 10 en esta sesión; comprobar en F6. Ver `F3_2_IMPLEMENTATION.md` y DEC-036–DEC-040.


## Estado tras F3.3

F3.3 `IMPLEMENTED_UNVERIFIED`; planner NACHA separado, cola segura Bloqueada, idempotencia sin GUID/ciclo ni texto sensible, migraciones dobles F3.3; no se ejecutó build/xUnit/DB. `next_microtask=F4.1`. Entregar próximo ZIP completo, sin usar regresivamente F3.2. Nunca activar `Live` prematuramente.

## Estado tras F4.1 (vigente)

F4.1 `IMPLEMENTED_UNVERIFIED`: regla ACHCOL por cámara, clase, tipo-6 monetario y contexto de archivo, aplicada antes de planificación; sin ciclos ficticios, sin que `IsEligible` autorice despachos. Cola permanece `Blocked`, no cambia SOAP ni BD. Pruebas .NET pendientes F6. **`next_microtask=F4.2`**: orquestación/worker aislado de `CycleBound`, sin transporte prematuro. Partir exclusivamente de `ACHInterbank_ACHCOL_CFA_F4_1_UNIFICADO.zip` y leer `F4_1_IMPLEMENTATION.md`.

## Estado tras F4.2 (vigente — 2026-10-25)

Base única para continuar: `ACHInterbank_ACHCOL_CFA_F4_2_UNIFICADO.zip`. F4.2 incorporó prevalidación de entradas ACHCOL bloqueadas dentro de la orquestación R5D, sin tocar el selector SOAP CycleBound. Si el contexto pasa F4.1 y la identidad coincide, queda `Blocked/ACHCOL_MAPPING_NOT_READY_F42`; si no, `Blocked/ACHCOL_PREFLIGHT_REJECTED_F42`. No existen envíos CFA desde esta ruta. Pruebas xUnit añadidas **sin ejecutar** (falta SDK .NET). Leer `F4_2_IMPLEMENTATION.md`. **`next_microtask=F4.3`**: política de fallos inciertos, reintentos y simulación sin habilitar Live. Entregar proyecto completo actualizado y MEMORY_STATE.
