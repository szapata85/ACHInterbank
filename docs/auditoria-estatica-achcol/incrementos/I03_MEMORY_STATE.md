# MEMORY_STATE — auditoría ACH Colombia

**Fuente viva y portátil de continuidad.** Leer este archivo antes del próximo incremento; la fotografía de este mismo corte está en `incrementos/I03_MEMORY_STATE.md`.

- Fecha de corte: `2026-11-01`.
- Cámara auditada: `ACHCOL`. **No mezclar reglas de CENIT**.
- Último ZIP completo entregable: `ACHInterbank_PROYECTO_COMPLETO_AUDITORIA_ACHCOL_I03_2026-11-01.zip`.
- ZIP base I03: `ACHInterbank_PROYECTO_COMPLETO_AUDITORIA_ACHCOL_I02_2026-11-01.zip`. Preservar los ZIP previos como hitos, pero continuar siempre desde el último.
- Política en código: `ACHCOL-7C-20261014` (7 ciclos), con histórico V35 de 5 ciclos separado. Fecha adoptada internamente; constancia externa de vigencia pendiente (**H02**).
- Incrementos terminados: `I01`, `I02`, **`I03`**. Progreso **3/10 = 30 % de hitos**, **7** pendientes (`I04`–`I10`). No significa 30 % de cumplimiento normativo.
- Último incremento: **I03 TERMINADO — auditoría estática ciclo 2**, 15 reglas; resultado **6 trazadas, 5 parciales, 3 no demostradas, 1 incumplimiento estático confirmado**. No certificado.
- Próximo: **I04 ciclo 3**: horario 09:01–11:00, P6/P7 día anterior + C1/C2 del actual; confirmar extensión H07 R10 C1→C3.
- **H01 ALTA**, abierto: matriz `ReturnWindows` en catálogo candidato sin consumo productivo localizado; falta validar fuente original y ciclo de devolución en `AchReturnsService`/eligibilidad.
- **H02 MEDIA**, abierto: discrepancia V35 de 5 ciclos frente a anexo de 7 y decisión interna `2026-10-14`, sin constancia externa y despliegue.
- **H03 ALTA por comprobar**, abierto: control de pago real y conciliación anterior a liberación no demostrado; I09.
- **H04 pendiente I09**: ROR ACHCOL y causales transversales, no se presume conforme.
- **H05 MEDIA**, abierto: R10 interdiario se deriva a reclamaciones (`AchReturnEligibilityPolicyComponent.cs:134-139`); contrastar con política documental.
- **H06 MEDIA condicional**, abierto: trigger nocturno C1 busca fecha local en lugar de fecha de proceso proyectada; I02.
- **H07 ALTA NUEVO I03**, defecto estático en ruta R10 C1→C2: `AchReturnEligibilityPolicyComponent.cs:148-160` ordena por `StartTime` y ubica C1 (19:01 del día anterior) tras C2–C7, rechazando por índice una ventana C1→C2 permitida en la tabla. Sin prueba runtime y sin corrección aún; requiere ajuste mínimo y test de regresión focal cuando se intervenga código.
- Rutas clave I03: `RegulatoryCycleScheduleCatalog`, `ClearingHouseCyclePolicyResolver`, `AchCycleScheduler`, `CycleTransactionPolicy`, `NachaTransactionValidationService`, `AchReturnsService`, `AchReturnOriginalCorrelationComponent`, `AchReturnEligibilityPolicyComponent`, MFT handlers/workflows.
- Código productivo/tests/frontend: **sin cambios I01–I03**. Solo Markdown incrementales.
- Pruebas .NET/DB/Quartz/MFT no ejecutadas (comando `dotnet` ausente). **Bloqueadores para seguir auditoría estática: ninguno.** No certificar sin evidencia real.
- Entrega I03: `incrementos/I03_CICLO_2_REGLA_A_REGLA.md`, `incrementos/I03_MEMORY_STATE.md`, `MANIFIESTO_I03.md`; actualización `PLAN_INCREMENTAL.md`, `ESTADO_INCREMENTOS.md`, `README.md`, `MEMORY_STATE.md`.
- Reglas de trabajo: sin sobreingeniería; inspeccionar regla por regla de un ciclo; documentar solo hallazgos y referencias concretas; no corregir por conjeturas; ZIP completo al cerrar cada intervención; informar terminados, pendientes, hallazgos y siguiente paso.
