# MEMORY_STATE — DEV-AUTO — I02-ACH

**Corte:** 2026-11-02. **Base obligatoria siguiente:** `ACHInterbank_COMPLETO_DEVOLUCIONES_AUTOMATICAS_I02_ACHCOLOMBIA_2026-11-02.zip`. **Incremento cerrado en fuente:** I02-ACH, detección pasiva exclusiva ACH Colombia; .NET y UAT pendientes. **Siguiente propuesto:** I03-ACH. CENIT: NO INICIADO.

## Invariantes de continuidad

1. Usar el ZIP íntegro I02-ACH como única base siguiente; preservar `MEMORY.md`, `MEMORY_STATE.md` de raíz y todas las memorias de otras iniciativas. `docs/implementacion-devoluciones-automaticas/PLAN_POR_CAMARA_I01_ACH.md` conserva prioridad sobre el orden mixto histórico.
2. No cambiar ambas cámaras en el mismo incremento. Un filtro `ACHCOL` explícito, sin activar ningún flujo `CENIT`. No extrapolar reglas de tiempos, causales, formatos o circuitos financieros entre cámaras.
3. I01 agregó `AutomaticReturnMode` por cámara con tres valores y endpoint ACHCOL; `ProcessingConnected=false`. La selección de `AUTOMATICO` sigue sin ejecutar devoluciones.
4. I02 observa exclusivamente rechazo funcional confirmado por respuesta de core en `CycleBound` y modo `Live`, asociado a operación ACHCOL. Registra `IncomingNachaProcessingEvents`; no crea `AchReturnGenerated`, no genera archivo, no envía, no mueve dinero ni cambia estados por cuenta propia.
5. Causal Rxx solo como sugerencia si la respuesta ya fue resuelta para la cámara/tipo y el catálogo existente marca `IsForReturn`; si no, `RevisionCausal`. Nunca deducir causa de texto SOAP.
6. Errores de preflight, parsing, transporte, timeout, desconocidos o de ventana NO son fallas confirmadas de aplicación. `AchColFileBased` no habilitado para SOAP sin homologación.
7. La deduplicación de eventos I02 es de mejor esfuerzo en procesamiento normal, **NO DB-first** ni garantiza concurrencia. I03 debe diseñar unicidad persistente de candidatas/devoluciones, respetar cuatro ciclos, diferenciar crédito/débito tardíos y no autoenviar.
8. No declarar build/SQL/xUnit/uAT aprobados en ChatGPT si no se ejecutan. No bloquear mejoras estáticas seguras por falta de ambiente.
9. SPA sin cambios en I02; si I04 toca SPA, usar Angular Material y español natural, sin sobreingeniería.

## Cambios de I02-ACH

- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchColombiaApplicationFailureDetector.cs`: evaluador y registro pasivo.
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaPostProcessingPipeline.cs`: punto de integración tras respuesta de core; fallas del observador no reemplazan el resultado financiero.
- `tests/Cfa.ACHInterbank.Tests/AchColombiaApplicationFailureDetectorI02Tests.cs`: unitarias de crédito/débito/prenotificación, CENIT, tipos de respuesta, deduplicación, ausencia de efectos.
- `scripts/test/automatic_returns_i02_ach_static_check.py`: 18 invariantes estáticos, PASARON.
- Informe, evidencia, memoria y manifiesto acumulativos en `docs/implementacion-devoluciones-automaticas/`.

## Evidencia y estado

- `SOLUTION GRAPH: OK`: 7/7 proyectos.
- `PASS_STATIC 18/18` detector I02.
- `dotnet build`, `dotnet test`, migración I01 contra SQL, SOAP live y regresión: **NO EJECUTADAS** por entorno; no afirmar certificación.
- I00: completo documental. I01-ACH: en fuente; runtime pendiente. **I02-ACH: en fuente; runtime pendiente.** I03-ACH y toda la ruta CENIT: NO INICIADOS. Estado productivo **NO-GO**.

## Próximo paso y parada

I03-ACH deberá evaluar solo incidencias `ACHCOL` con causa y original verificable, reglamento V35, plazos, vínculo con generador manual y reservar idempotencia sólida en BD antes de preparar devoluciones. Primero ejecutar en ambiente propio los tests I01/I02 y registrar evidencia; si no está disponible, continuar trabajo seguro sin afirmar su aprobación. Parar formalmente al final de I02-ACH y entregar ZIP completo.
