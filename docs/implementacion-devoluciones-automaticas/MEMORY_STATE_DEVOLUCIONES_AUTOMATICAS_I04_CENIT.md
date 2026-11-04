# MEMORY STATE — DEVOLUCIONES AUTOMÁTICAS I04-CENIT

**Corte:** 2026-11-03. **ZIP base único:** `ACHInterbank_COMPLETO_DEVOLUCIONES_AUTOMATICAS_I03_CENIT_2026-11-03.zip`; SHA-256 `31bf10972c6b7e2a5dd087e44fcde15d298ac0ec00ae0c1cfa1dac3a215224e2`. **ZIP final:** `ACHInterbank_COMPLETO_DEVOLUCIONES_AUTOMATICAS_I04_CENIT_2026-11-03.zip`. **Estado:** IMPLEMENTADO EN FUENTE, SIN COMPILACIÓN/UAT. **NO-GO PRODUCTIVO**.

## Continuidad leída

Se inspeccionaron ZIP I03 (4.903 archivos, sin error), `MEMORY.md`, `MEMORY_STATE.md` históricos y documentos CENIT I01, I02, I03, más los servicios reales CENIT I01–I03, patrón existente de revisión ACH sin reutilizar su lógica. Normativa revisada: DSP-152 Anexo 2 y Anexo A R06, R23 y R60–R69, NACHA-M CENIT. No reescribir memorias históricas.

## Objetivo y funcionamiento agregado

I04 agrega **solo** revisión autorizada de preparaciones CENIT I03: listar, `APROBAR`, `DESCARTAR`, listar excepciones de I02 para gestión humana. Exige dos personas distintas con trazabilidad de auditoría de INSERT (falla cerrada con identidad `system` o ausente). El revisor se toma de claims JWT, no del cuerpo. Solo `PREPARADA_SIN_ENVIO` puede resolverse; el `Status` de EF es token de concurrencia. Aprobación revalida modo y evidencia última I02, causal, calendario, fecha valor, sesión siguiente, unicidad de retorno generado y origen CENIT en aislamiento `Serializable` de bases relacionales. Descarte puede ejecutarse aun con modo desactivado. Registra revisor, fecha, justificación y referencia de soporte (para aprobación) en tabla existente, además de `AuditLogs`. No genera ni transmite archivos, ni modifica movimientos.

**Excepciones:** GET `.../revisiones/excepciones` expone solo indicios que requieren revisión, conserva `LimitedWindow`, sin persistir decisiones excepcionales. R06, R23, R34, prenotificaciones, devoluciones de devoluciones y plazos extraordinarios siguen fuera de aprobación ordinaria, requieren operación manual y evidencia adicional. Esto NO implica que el procedimiento excepcional completo esté implementado.

## Cambios por archivo

**Modificados:**
- `src/Cfa.ACHInterbank.Domain/Models/ACH/CenitReturnPreparation.cs` (metadatos de decisión)
- `src/Cfa.ACHInterbank.Persistence/Configuration/CenitReturnPreparationConfiguration.cs` (límites EF)
- `src/Cfa.ACHInterbank.Persistence/DependencyInjectionService.cs` (registro servicio)
- `src/Cfa.ACHInterbank.Persistence.Migrations.SqlServer/DataBase/Migrations/SqlServer/AchDbContextModelSnapshot.cs`
- `src/Cfa.ACHInterbank.Persistence/DataBase/Migrations/Postgres/AchDbContextModelSnapshot.cs`
- `tests/Cfa.ACHInterbank.Tests/CenitReturnPreparationI03Tests.cs` (helper de datos compartido a tests I04; sin cambios de casos I03)

**Añadidos:**
- `src/Cfa.ACHInterbank.Application/ACH/Models/CenitReturnReviewModels.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Interfaces/ICenitReturnReviewService.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitReturnReviewService.cs`
- `src/Cfa.ACHInterbank.Api/Controllers/CenitReturnReviewController.cs`
- Migraciones `20261103143000_CenitReturnReviewI04.cs` SQL Server y PostgreSQL
- `tests/Cfa.ACHInterbank.Tests/CenitReturnReviewI04Tests.cs`
- `scripts/test/automatic_returns_i04_cenit_static_check.py`
- Documentación I04: `MEMORY_STATE`, `INFORME`, `EVIDENCIA`, `PLAN`, manifiesto, inventario SHA-256, archivo de ejecución estática.

**Sin modificar:** generadores, SPA, motores, jobs, transporte, servicios manuales CENIT, fuente funcional de ACH Colombia.

## Evidencias exactas

- Ejecutadas: integridad y SHA-256 original; comprobaciones estáticas I04 71/71; regresiones CENIT I01 29/29, I02 50/50, I03 57/57; ACH I02 18/18, I03 25/25, I05 33/33, I06 28/28, I07 24/24, I08 25/25.
- **Pendiente histórico:** ACH I04 **32/35**, sin modificación, NO aprobado.
- **NO ejecutadas:** SDK .NET 10, build, xUnit, migraciones SQL reales, concurrencia dos conexiones, E2E seguridad/roles, UAT CENIT, homologación, archivo/transmisión/conciliación.
- Riesgos abiertos: ninguna aprobación equivale a orden financiera; soporte referenciado no autenticado externamente; identidades alias y segregación requieren validación IAM; estado puede invalidarse después de aprobar y debe revalidarse de nuevo antes de I05; sólo casos ordinarios dentro de ventana, excepciones manuales; no se puede afirmar garantía de serialización sin pruebas reales; detección I02 limitada a 500 intentos y 100 transacciones visibles.

## Progreso y parada

| Iniciativa | Estado | Pendientes del plan actual |
|---|---|---|
| CENIT I01–I04 | En código, sin validación de ejecución | 0 de estos cuatro |
| CENIT I05–I06 | No iniciados | **2 incrementos** |
| ACH Colombia I01–I08 | Código previo intacto; NO-GO | — |
| ACH Colombia I09-B | Validación compilada pendiente | **1 fase obligatoria** |
| ACH Colombia I09-A | Preparación técnica si necesaria | **0–1 fase opcional** |

**TOTAL restante:** 3 fases obligatorias (2 CENIT, 1 ACH) y 1 opcional. No equivale a homologación ni autoriza producción. **Punto de parada: I04-CENIT.** Siguiente I05-CENIT: generación controlada, *sin transporte*, bajo revisión normativa y pruebas. No iniciar sin nueva solicitud. Usar **exclusivamente ZIP I04** como línea base; preservar todos los históricos.
