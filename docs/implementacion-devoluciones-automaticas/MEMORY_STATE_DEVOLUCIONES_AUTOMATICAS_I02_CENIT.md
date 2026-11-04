# MEMORY STATE — DEVOLUCIONES AUTOMÁTICAS I02-CENIT

**Corte documental:** 2026-11-02. **Incremento exclusivo:** I02-CENIT — detección pasiva y a demanda de indicios verificables de fallas de aplicación. **Origen único:** `ACHInterbank_COMPLETO_DEVOLUCIONES_AUTOMATICAS_I01_CENIT_2026-11-02.zip`. **Salida:** `ACHInterbank_COMPLETO_DEVOLUCIONES_AUTOMATICAS_I02_CENIT_2026-11-02.zip`. **No reutilizar ZIP I08-ACH ni proyectos antiguos como base.** Estado productivo: **NO-GO**.

## Continuidad histórica consultada

`MEMORY.md`, `MEMORY_STATE.md`, `docs/implementacion-devoluciones-automaticas/MEMORY_STATE.md`, `MEMORY_STATE_DEVOLUCIONES_AUTOMATICAS_I01_CENIT.md` y las memorias históricas ACH I01–I08. No se altera el contenido anterior de ninguna memoria. I01-CENIT continúa con su API de modos `DESACTIVADO | CON_APROBACION | AUTOMATICO`; la selección no conecta procesamiento. ACH I01–I08 conserva código, I09-A condicional e I09-B pendiente. **La versión I04-ACH de controles estáticos sigue 32/35, no aprobada.**

## Objetivo y cambios reales

I02 expone un **GET de lectura a demanda** que examina las últimas 500 ejecuciones de aplicación de la cámara CENIT (limitando la muestra a 1–100 transacciones distintas). Elige la última ejecución por transacción **antes** de interpretar su resultado, para no promover un rechazo anterior cuando el último intento fue exitoso. Solo muestra fallas funcionales con evidencia real del core, correlación final inequívoca, clasificación funcional elegible, cola en fallo definitivo `IFUNC` y transacción entrante clasificada, cámara/ciclo CENIT y contexto operacional en vivo.

- **`INDICIO_CAUSAL_CENIT`:** resultado funcional confirmado y causal Rxx corroborada en el catálogo específico y vigente de CENIT y en las aplicabilidades del Anexo A. **No afirma que la devolución sea ejecutable.**
- **`REVISION_CAUSAL`:** rechazo funcional confirmado, sin causal suficientemente verificable/aplicable (incluye códigos Dxx, Rxx no reconocidos, catálogos de otra cámara o tipo incompatible).
- **`REVISION_SOLICITUD`:** R06/R23 cuando aplicables, o R34; requieren acreditación de solicitud o decisión humana.
- **`REVISION_PRENOTIFICACION`:** prenotificación con causal en catálogo, pero dirección/adenda/condiciones pendientes.

En todos los casos `ProcessingConnected=false`. Sin tabla nueva, migración nueva, evento persistido, tarea, cola, preparación, creación NACHA-M, remesa, transmisión ni conciliación. No se modifica flujo de ACH Colombia ni servicios manuales de CENIT. Una consulta no registra cambios en entidad; el acceso queda protegido con `P1.ConfigRead`. No se exponen cuentas ni datos personales en DTO.

## Archivos incorporados/modificados

- `src/Cfa.ACHInterbank.Application/ACH/Models/CenitPassiveReturnDetection.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Interfaces/ICenitPassiveReturnDetector.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitPassiveReturnDetector.cs`
- `src/Cfa.ACHInterbank.Api/Controllers/CenitPassiveReturnDetectionController.cs`
- `src/Cfa.ACHInterbank.Persistence/DependencyInjectionService.cs`
- `tests/Cfa.ACHInterbank.Tests/CenitPassiveReturnDetectionI02Tests.cs`
- `scripts/test/automatic_returns_i02_cenit_static_check.py`

Documentos nuevos dentro de `docs/implementacion-devoluciones-automaticas/`: `MEMORY_STATE_DEVOLUCIONES_AUTOMATICAS_I02_CENIT.md`, `INFORME_DEVOLUCIONES_AUTOMATICAS_I02_CENIT.md`, `EVIDENCIA_DEVOLUCIONES_AUTOMATICAS_I02_CENIT.md`, `PLAN_DEVOLUCIONES_AUTOMATICAS_CENIT_I02.md` y manifiesto de cambios/inventario. **Único archivo heredado de código modificado:** registro DI en `DependencyInjectionService.cs`. Sin modificación a SPA Angular.

## Contrato HTTP

`GET /api/cenit/devoluciones-automaticas/deteccion?maxTransactions=100`; exige autenticación y `P1.ConfigRead`; 400 por límite inválido (1–100). Devuelve `mode`, `processingConnected=false`, `examinedTransactions`, `limitedWindow`, `findings`. Cada hallazgo solo incluye identificadores internos/operativos, estado, causal corroborada o `null`, explicación y fecha UTC. El flujo de lectura puede consultar evidencias aunque CENIT permanezca `DESACTIVADO`; **nunca** dispara automatización.

## Validaciones realizadas y no realizadas

- Realizadas: comprobación SHA/integridad del ZIP original, inspección de código y memoria, análisis normativo CENIT, inspección de archivos modificados, control de fuente I02 (50/50) y regreso I01-CENIT (29/29). Regresión ACH: I02 18/18; I03 25/25; I04 32/35 **fallida histórica**; I05 33/33; I06 28/28; I07 24/24; I08 25/25.
- **No ejecutadas** por falta de SDK .NET y entorno de integración: `dotnet build`, `dotnet test` (incluidas xUnit nuevas), SQL Server/PostgreSQL, consultas reales sobre datos bancarios, autenticación real, UAT y homologación CENIT. Los tests C# se añadieron **sin afirmar éxito de ejecución**.
- La huella/comparación del ZIP de salida constará en `MANIFIESTO_DEVOLUCIONES_AUTOMATICAS_I02_CENIT.json`, inventario y `.sha256` externos.

## Riesgos y puntos abiertos

1. La consulta solo inspecciona una ventana reciente (500 intentos); **no es una detección exhaustiva ni una cola persistente**. No garantiza cobertura de todos los casos históricos ni reemplaza comprobación de idempotencia transaccional I03.
2. `INDICIO_CAUSAL_CENIT` no valida sesión siguiente, Fecha Valor, feriados, ventana de devolución, fondos ni aprobación. **I03/I04 deben revalidar la evidencia bajo bloqueo transaccional antes de cualquier preparación.**
3. R06/R23 tienen requisitos de solicitud y oportunidad; devolución de devolución R60–R69, plazos de prenotificaciones y causales Dxx de rechazo STA requieren caminos separados. No se implementan en I02.
4. Mapas/estatus del core y versiones regulatorias siguen sujetos a ejecución/homologación; que un campo Rxx exista no prueba vigencia operativa plena. Ajustes históricos I01 para PostgreSQL y riesgo ABA del modo siguen abiertos.
5. Esta API **no** modifica el pipeline ni crea eventos auditables de detección. El rastro son IDs referenciados de la cola/ejecución persistida; para un historial materializado/auditable se requerirá diseño posterior justificado.
6. El modelo asume clasificación de transacción entrante determinada y ejecución `Live`. Si la integración no deja evidencia suficiente, falla cerrado (cero hallazgos). Requiere prueba sobre datos reales del entorno.

## Estado de implementaciones y cuánto falta

| Cámara | Incremento(s) | Estado al cierre de I02-CENIT | Pendientes del plan actual |
|---|---|---|---|
| CENIT | **I01** | Incorporado al código; ejecución/homologación pendientes | — |
| CENIT | **I02** | Incorporado al código; validador estático 50/50; xUnit/DB pendientes | — |
| CENIT | **I03–I06** | No iniciados | **4 incrementos** |
| ACH Colombia | **I01–I08** | Incorporados al código histórico; no homologados | — |
| ACH Colombia | **I09-B** | Ejecución en entorno compilado pendiente | **1 fase obligatoria de validación** |
| ACH Colombia | **I09-A** | Preparación técnica condicional | **0–1 fase opcional** |

**Conteo:** 6 incrementos CENIT planificados; **2 incorporados**, **4 por implementar**. Para las dos cámaras restan **4 incrementos CENIT + 1 fase obligatoria I09-B ACH**, más **I09-A solo si el entorno lo necesita**. Esto es el conteo del plan actual, **no** una garantía de puesta en producción ni un número cerrado de iteraciones de corrección.


**Parada exacta:** I02-CENIT incorporado en código, verificaciones estáticas ejecutadas, xUnit y UAT pendientes. **No iniciar I03 sin nueva instrucción.** Próximo I03-CENIT: preparación **sin envío**, control de ciclo siguiente, Fecha Valor, deduplicación y revalidación normativa/operativa. **NO-GO**.
