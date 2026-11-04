# MEMORY STATE — DEVOLUCIONES AUTOMÁTICAS I03-CENIT

**Fecha:** 2026-11-03. **Base obligatoria única:** `ACHInterbank_COMPLETO_DEVOLUCIONES_AUTOMATICAS_I02_CENIT_2026-11-02.zip`; SHA-256 `bbb2862461466d54117a897b7a1b065cf396f8725ea396eabcec6577a93a2b69`. **Salida prevista:** `ACHInterbank_COMPLETO_DEVOLUCIONES_AUTOMATICAS_I03_CENIT_2026-11-03.zip`. No reutilizar versiones antiguas. **Producción: NO-GO.**

## Memorias y código revisados

Se inspeccionaron `MEMORY.md`, `MEMORY_STATE.md`, memoria de iniciativa I08-ACH, I01/I02-CENIT, plan I02 y código de I01/I02-CENIT. Se contrastó la implementación con DSP-152 Anexo 2 §§3.1–3.3, 4.2, 4.5, Anexo A R06/R23/R34, manual NACHA-M CENIT. No sobrescribir ninguna memoria histórica.

## Objetivo implementado

Preparación **explícita individual y únicamente de cámara CENIT**, nunca automática por scheduler. Servicio y controlador separados. POST exige `OriginalTransactionId` y `ExpectedExecutionId` de I02; reconsulta última ejecución válida del core y aplica `CenitPassiveReturnDetector.Evaluate` con catálogo de causales de CENIT y clasificación `INDICIO_CAUSAL_CENIT`, sin aceptación de R06, R23, R34, prenotificaciones ni rechazos técnicos. Solo permite una reserva en los modos `CON_APROBACION`/`AUTOMATICO`; `DESACTIVADO` bloquea. Aun con `AUTOMATICO` el procesamiento está desconectado.

Ciclo ordinario CENIT 1→2, 2→3, 3→4, 4→5: mismo día y Fecha Valor, source cerrado, target abierto/programado, política activa y permisiva, horas comparadas con catálogo CENIT, antes de cutoff, sin sábado/domingo/festivo registrado. No se admiten ventanas extemporáneas ni sesiones especiales sin evidencia/homologación. Manejo idempotente con dos índices `UNIQUE`, transacción `Serializable` en proveedor relacional y SaveChanges con `AuditLogs`; conflicto 409 en caso de duplicado/precondición.

## Código modificado (cuatro archivos compartidos)

- `src/Cfa.ACHInterbank.Persistence/DataBase/AchDbContext.cs`: DbSet CENIT independiente.
- `src/Cfa.ACHInterbank.Persistence/DependencyInjectionService.cs`: solo registro DI.
- `src/Cfa.ACHInterbank.Persistence.Migrations.SqlServer/DataBase/Migrations/SqlServer/AchDbContextModelSnapshot.cs`: modelo CENIT I03.
- `src/Cfa.ACHInterbank.Persistence/DataBase/Migrations/Postgres/AchDbContextModelSnapshot.cs`: modelo CENIT I03.

## Código añadido

- `src/Cfa.ACHInterbank.Domain/Models/ACH/CenitReturnPreparation.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Models/CenitReturnPreparationModels.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Interfaces/ICenitReturnPreparationService.cs`
- `src/Cfa.ACHInterbank.Persistence/Configuration/CenitReturnPreparationConfiguration.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitReturnPreparationService.cs`
- `src/Cfa.ACHInterbank.Api/Controllers/CenitReturnPreparationController.cs`
- `src/Cfa.ACHInterbank.Persistence.Migrations.SqlServer/DataBase/Migrations/SqlServer/20261103081000_CenitReturnPreparationI03.cs`
- `src/Cfa.ACHInterbank.Persistence/DataBase/Migrations/Postgres/20261103081000_CenitReturnPreparationI03.cs`
- `tests/Cfa.ACHInterbank.Tests/CenitReturnPreparationI03Tests.cs`
- `scripts/test/automatic_returns_i03_cenit_static_check.py`

Documentación nueva en el directorio `docs/implementacion-devoluciones-automaticas`: `INFORME_DEVOLUCIONES_AUTOMATICAS_I03_CENIT.md`, `EVIDENCIA_DEVOLUCIONES_AUTOMATICAS_I03_CENIT.md`, `PLAN_DEVOLUCIONES_AUTOMATICAS_CENIT_I03.md`, esta memoria, manifiesto e inventario SHA-256. La SPA **no cambia**, ya que I03 es API de preparación administrativa sin flujo de aprobación visual; abordar UX al definir roles de I04.

## Validaciones y límites exactos

- **Sí ejecutado**: integridad ZIP I02; verificación SHA de entrada; lectura e inspección real; estáticos I03-CENIT 57/57, I02-CENIT 50/50, I01-CENIT 29/29 y regresión ACH I02 18/18, I03 25/25, I05 33/33, I06 28/28, I07 24/24, I08 25/25.
- **Control histórico abierto**: I04-ACH 32/35, no aprobado; no modificar fuera de alcance.
- **No ejecutado**: `dotnet build`, xUnit I03, DB SQL Server/PostgreSQL, serialización concurrente real, auditoría autenticada end-to-end, Angular, CENIT homologación, transporte ni conciliación.
- Limitaciones: no detecta anomalías fuera de ventana I02 ni reservas tardías; no valida disponibilidad de fondos ni prueba aceptación por CENIT; ninguna tarea consume la nueva tabla; necesita pruebas reales de calendario y de precondiciones ante cambio concurrente de modo, evidencia o sesión.

## Estados por cámara y conteo

| Cámara | Incrementos | Estado | Pendientes |
|---|---|---|---|
| CENIT | I01–I03 | **Código incorporado; sin compilación/UAT** | — |
| CENIT | I04–I06 | No iniciados | **3 incrementos** |
| ACH Colombia | I01–I08 | Código previo conservado; producción NO-GO | — |
| ACH Colombia | I09-B | Validación compilada pendiente | **1 fase obligatoria** |
| ACH Colombia | I09-A | Preparación técnica condicional | **0–1 fase opcional** |

**Quedan cuatro fases obligatorias del plan actual** (3 CENIT + 1 ACH) y **una opcional**. No se garantiza número final de correctivos. **Parada exacta:** I03-CENIT en código y ZIP de salida; siguiente I04-CENIT: aprobación/revisión independiente, excepciones con evidencia normativa, sin generación ni transporte. No iniciar sin nueva instrucción. **NO-GO**.
