# MEMORY_STATE — auditoría estática incremental CENIT

**LEER PRIMERO. Corte I07 = 2026-11-01.** Último ZIP íntegro: `ACHInterbank_PROYECTO_COMPLETO_AUDITORIA_CENIT_I07_2026-11-01.zip`. Raíz `ACHInterbank_ajustado/`. **Base del próximo I08 = I07**; no reconstruir I01–I07 ni alterar productivo/ACH Colombia.

## Estado actualizado I07

- **Hitos 7/10 (70 % de auditoría, NO certificación); I01–I07 completos, I08–I10 pendientes.**
- **I07: 64 reglas**: 7 cumplimiento estático en ruta delimitada, 48 parcial, 8 incumplimiento estático demostrado, 1 no demostrable. Filas repetidas por causales y controles no equivalen a hallazgos únicos.
- **Hallazgos: 2 nuevos, 9 acumulados, 9 abiertos, 0 cerrados.** Nuevos `HC-CENIT-008` (ROR R74 acepta devolución padre distinta de R32) y `HC-CENIT-009` (R23 acepta fecha tope aportada sin cálculo siguiente día hábil). `HC-002`, `HC-004`, `HC-007` ampliados, `HC-001/003/005/006` pendientes sin cambios. Detalles y localizaciones en `I07_REGLA_A_REGLA.md` y `REGISTRO_HALLAZGOS.md`.
- **Fuentes:** Anexo A y B 28-11-2023; DSP-152 Anexo 2 27-02-2025; NACHA-M 07-05-2026 aportado, vigencia pública externa sin comprobación nueva. STA D01–D06 **separado de Return/ROR**. DN-CEN-001…007 sin solución normativa inventada.
- **Rutas:** `AchReturnsService.GenerateReturnsFileAsync` → `AchReturnEligibilityPolicyComponent` → `CenitIncomingReturnPolicy`; entrada `IncomingNachaPostParseProcessor`; ROR `CenitReturnOfReturnService` → `AchRegulatoryCatalogService` → `AchReturnOfReturnPolicies` sembrado por `RegulatoryCatalogSeeder`. Contraejemplo estático R74 padre R02 y R23 fecha límite autodeclarada, no se ejecutaron pruebas.
- **Límites:** no se ejecutó .NET ni DB/Quartz/CUD/SEBRA/STA ni certificación. 0 cambios código/config/pruebas. Todos los controles son estáticos, y los motivos fácticos de R61–R73 están parciales por falta de validación de soporte sin inferir automáticamente defecto material.
- **Archivos I07 examinados:** `CenitIncomingReturnPolicy.cs`, `CenitIncomingReturnPolicyModels.cs`, `AchReturnDtos.cs`, `AchReturnsService.cs`, `AchReturnCausePolicyComponent.cs`, `AchReturnEligibilityPolicyComponent.cs`, `AchReturnOriginalCorrelationComponent.cs`, `AchReturnOfReturnEligibilityService.cs`, `AchReturnOfReturnFileGenerationService.cs`, `CenitReturnOfReturnService.cs`, `CenitReturnOfReturn2026Layout.cs`, `CenitReturnIn2026Layout.cs`, `CenitRawReturnContractGate.cs`, `AchCauseCodePolicy.cs`, `AchRegulatoryCatalogService.cs`, `RegulatoryCatalogSeeder.cs`, `IncomingNachaPostParseProcessor.cs`, `IncomingNachaIngestionAppService.cs`, `NachaChamberFlowPolicy.cs`; tests fuente `CenitIncomingReturnPolicyTests.cs`, `CenitReturnOfReturn2026Tests.cs`, `AchReturnIdempotencyTests.cs`, `RegulatoryCatalogSeederReturnOfReturnPoliciesByClearingHouseTests.cs`.
- **Siguiente I08:** NACHA-M PPD/CCD/CTX 106 caracteres, adendas, contadores, ADV, rechazos formal/fatal y STA real (separación), envío/recepción fuera de ciclo y encolamiento; verificar HC-001, trazabilidad de archivo y contrato. Producir `I08_REGLA_A_REGLA.md`, snapshot, manifiesto, hashes, ZIP completo y verificación SHA.

## Memoria histórica de I06 (conservada; cifras previas pertenecen a corte I06)

## Decisiones de continuidad y control

- Mantener HC-CENIT-001…009 con identificadores estables; NO fusionar HC-004 (R23 mismo día) con HC-009 (cálculo de plazo bancario) ni HC-008 con HC-002 (correlación C1).
- Corregir solo si se solicita en un turno posterior; no se tocó código productivo.
- Matriz I07 y `REGISTRO_HALLAZGOS.md` son prueba documental estática, no certificado operativo.
- Integridad I07: comparación exacta de hashes SHA-256 de todo archivo I06 salvo cinco documentos históricos de seguimiento previstos para actualización, cuatro archivos I07 añadidos y cero bajas.
