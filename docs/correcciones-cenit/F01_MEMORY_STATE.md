# MEMORY_STATE — Correcciones incrementales CENIT

**Corte:** 2026-11-02. **ZIP de auditoría base:** `ACHInterbank_PROYECTO_COMPLETO_AUDITORIA_CENIT_I10_2026-11-02.zip` (SHA-256 `9c298cf0ae8f8215b4ab00a186c14b1bcc18be78c139b3d8584edec5fa54aeef`). **Raíz:** `ACHInterbank_ajustado/`. Auditoría histórica `docs/auditoria-estatica-cenit/MEMORY_STATE.md` conservada sin modificar.

## Estado

- **Fase actual:** F01 — separación simulación, neteo y estados definitivos. **Fases implementadas:** F01 (1/8 = 12,5 % del plan de entregas); F02–F08 pendientes. El 12,5 % NO significa conformidad/homologación.
- **HC implementados estáticamente pendientes de verificación:** HC-CENIT-003, HC-CENIT-005, HC-CENIT-006.
- **HC cerrados con evidencia ejecutada:** ninguno (0).
- **HC abiertos sin intervención aún:** HC-CENIT-001, 002, 004, 007, 008, 009, 010, 011, 012, 013 (10).
- **HC bloqueados:** ninguno para cambio de código; **verificación externa pendiente** para 003, 005, 006.
- **Hallazgos no cerrados acumulados:** 13. Identificadores originales inmutables.

## Cambios efectivos y pruebas

- **Archivos productivos modificados (5):** `CenitCycleExecutionService.cs`, `CenitNettingService.cs`, `LiquidityOptimizationService.cs`, `TransactionPriorityPolicy.cs`, `AchPendingCycleEligibilityService.cs` (todos en `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/`).
- **Prueba xUnit modificada (1):** `tests/Cfa.ACHInterbank.Tests/CenitOperationalGovernanceTests.cs`, casos de falla simulada, C4/C5, SEC, idempotencia, cámara y cola no consumida. **Script estático nuevo (1):** `scripts/test/cenit_f01_static_check.py`.
- **Pruebas realmente ejecutadas:** script estático 12/12 PASS; pruebas xUnit .NET **0 ejecutadas**, aprobadas **0**, fallidas **0**; compilación no ejecutada (SDK ausente). Ver `REGISTRO_PRUEBAS.md`.
- **Regresión ACH Colombia:** código original preservado SHA-256, sin regresión funcional ejecutada.

## Decisiones arquitectónicas persistentes

1. Cuenta CUD, saldos y liquidación permanecen externos; procedimiento manual Tesorería. Sin APIs, motor oficial sustituto, asientos ni pantallas CUD.
2. Quartz CENIT **no** ejecuta neteo/liquidez simulados. Fin de ventana ≠ resultado financiero; estado interno `AwaitingOfficialResult`.
3. Neteo informativo `IncludedInSettlement=false`. Liquidez informativa sin cambio de estado/lote/cola/evento; `Simulated*`. Efectos oficiales solo desde mensajes del operador.
4. Cola permanece `Queued` durante F01; F03 implementará incorporación efectiva e idempotente (hallazgo 001 no resuelto).
5. SEC y signo de la devolución determinan prioridad informativa, sin inventar clasificación cuando falta campo confiable.
6. Conservar rutas productivas de ACK/NACK/ADV para intervención F02, y preservar ACH Colombia v35 por cámara.
7. No reescribir registros históricos de simulaciones anteriores sin evidencia confiable.

## Riesgos / verificaciones pendientes

.NET SDK no disponible; SQL Server/PostgreSQL/Quartz/gateway oficial y fixtures CENIT externos no disponibles. Debe ejecutarse xUnit + integración DB/Quartz (PT-04/05/06), regresión ACH Colombia (PT-20), análisis de resultados históricos erróneamente etiquetados, y validación operativa de los flujos de F02/F03. Discrepancias normativas DN-CEN-007/008 reservadas; no inventar cierres.

## Integridad, continuidad y entrega

**Último ZIP completo generado:** `ACHInterbank_PROYECTO_COMPLETO_CORRECCIONES_CENIT_F01_2026-11-02.zip`.
**SHA-256 del ZIP de entrega:** se entrega en archivo acompañante `.sha256` externo: un archivo no puede contener su propio hash SHA-256 exacto sin autorreferencia. SHA-256 base arriba, contenido actualizado en `MANIFIESTO_F01.md`, verificación posterior de CRC/comparación binaria y hash del ZIP externo.
**Siguiente fase:** F02 (HC-CENIT-012, 013): ruta gateway→parser→respuesta oficial→estado de transacción/evento y procesamiento ADV para conciliación; conservar idempotencia y no generar asientos CUD. Para continuar, abrir primero este MEMORY_STATE dentro del ZIP F01.

**Verificación de reversibilidad ejecutada:** dry-run del parche unificado en las seis rutas originales: 6/6 PASS.
