# MEMORY_STATE — Correcciones incrementales CENIT (F04)

**Fecha:** 2026-11-02. **Línea base histórica:** `ACHInterbank_PROYECTO_COMPLETO_AUDITORIA_CENIT_I10_2026-11-02.zip`. **Base inmediata:** `ACHInterbank_PROYECTO_COMPLETO_CORRECCIONES_CENIT_F03_2026-11-02.zip` SHA-256 `60f8061775ab19a76eac1c3d69061ed036976fe85b1976fedd4e79e5c47abcb1`; CRC OK, 4.628 entradas. **Raíz:** `ACHInterbank_ajustado/`. No se sobrescribió memoria histórica de auditoría.

## Continuidad

- **Fase actual:** F04 — R23, R06, fechas/plazos. **Fases entregadas:** F01, F02, F03, F04; **4/8 = 50 % de hitos** (no cumplimiento/homologación). **Siguiente:** F05, ROR HC-002/008.
- **IMPLEMENTADOS — PENDIENTES DE VERIFICACIÓN:** HC-CENIT-001, 003, 004, 005, 006, 007, 009, 012, 013 (9); **ABIERTO sin implementar:** HC-CENIT-002, 008, 010, 011 (4); **CERRADOS con pruebas ejecutadas:** 0; **bloqueados formalmente:** 0 (pruebas de los implementados pendientes de infraestructura).
- **Código modificado F04 (5 rutas):** `src/Cfa.ACHInterbank.Application/ACH/Services/CenitIncomingReturnPolicy.cs`; `src/Cfa.ACHInterbank.Application/ACH/Models/CenitIncomingReturnPolicyModels.cs`; `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchReturnEligibilityPolicyComponent.cs`; `AchReturnsService.cs`; `IncomingNachaPostParseProcessor.cs` en la misma carpeta. **Pruebas originales modificadas:** `tests/Cfa.ACHInterbank.Tests/CenitIncomingReturnPolicyTests.cs`. **Pruebas nuevas:** `CenitF04ReturnPolicyTests.cs`, `CenitF04CalendarRouteTests.cs`; **script:** `scripts/test/cenit_f04_static_check.py`. **Nuevos escenarios xUnit:** 22, NO ejecutados.
- **Verificaciones ejecutadas:** F04 estático 21/21 PASS, regresión estática F01 12/12 PASS, F02 13/13 PASS, F03 17/17 PASS; CRC/SHA-256 comparación contra ZIP F03, y parche reversible en seco. **.NET, DB, gateway, Quartz, operador:** 0 pruebas ejecutadas. **ACH Colombia:** archivos ajenos a rutas compartidas inalterados por hashes; regresión funcional PENDIENTE.

## Decisiones y límites

1. CUD fuera de alcance; Tesorería valida externamente. No liquidación ni optimización oficial interna. F01/F02/F03 intactas.
2. R23 mismo día se valida exclusivamente como devolución ordinaria en ciclo inmediato siguiente; la excepción R23 de días anteriores se conserva con documentación en §3.2/4.2 (HC-004).
3. R06 en C5 exige entrada original C4 del mismo día ANTES de aceptar evidencia operacional (HC-007). No cambia autorización R06 C2–C4.
4. R23 posterior exige comprobante de recepción de notificación (`ReceiverNotificationReceivedDate`) separado de `OriginalValueDate`, entrada registrada desde `AchCycle.ProcessingDate`, máximo 15 días calendario desde esa recepción y límite calculado con `IOperationalCalendarService.GetNextBusinessDayAsync` desde el siguiente día a la notificación. La fecha aportada `ReceiverRejectionDeadlineDate` no se usa para autorizar (HC-009). Sin evidencia, revisión manual, no respuesta permitida.
5. La fecha `AchCycle.ProcessingDate` prueba ciclo procesado **no necesariamente entrada real al participante**; contrastar en integración con archivo recibido y marca de tiempo de entrada. Notificación aportada exige trazabilidad/soporte manual, no declarada certificada.
6. Restricción F03 interdiaria `CENIT_QUEUE_VALUE_DATE_REVIEW_REQUIRED` queda **abierta como riesgo operativo / revisión externa**, sin transformar automáticamente fechas valor. DN-CEN-007 (prenotificaciones en C5) y DN-CEN-008 (nombres de archivos por canal), sin alterar.

## Evidencias/continuidad

Revisar `F04_INFORME_CORRECCIONES.md`, `F04_CAMBIOS.patch`, `F04_EVIDENCIA_CONTROLES_ESTATICOS.txt`, `REGISTRO_PRUEBAS.md`, `MATRIZ_HALLAZGOS_CORRECCIONES.md`, `MANIFIESTO_F04.md`, `F04_COMPARACION_SHA256.tsv` y `F04_MEMORY_STATE.md`. Documentación F01–F03 y auditorías I01–I10 permanecen en ZIP. **ZIP de salida previsto:** `ACHInterbank_PROYECTO_COMPLETO_CORRECCIONES_CENIT_F04_2026-11-02.zip`; SHA-256 se registra en archivo lateral `.sha256` y verificación externa tras empaquetar para evitar circularidad de hash embebido. **Siguiente fase F05:** correlación ROR C1 y R74 exclusivo R32, conservar idempotencia, pruebas en rutas productivas y sin CUD.
