# F04 — Devoluciones R23/R06, fechas y plazos CENIT

**Fecha:** 2026-11-02. **Base inmediata:** ZIP completo F03 SHA-256 `60f8061775ab19a76eac1c3d69061ed036976fe85b1976fedd4e79e5c47abcb1`; raíz `ACHInterbank_ajustado/`. **Hallazgos:** HC-CENIT-004, HC-CENIT-007, HC-CENIT-009. **Resultado:** 3 IMPLEMENTADOS — PENDIENTES DE VERIFICACIÓN; no cerrados.

## Evidencia y causa raíz reconfirmadas

- **HC-CENIT-004:** `CenitIncomingReturnPolicy.EvaluateR23` líneas base F03 241–275 admitía el R23 de misma Fecha Valor en ciclos 2–5 como si fuera la excepción **de días anteriores** del Anexo 2 §3.2. La corrección permite misma fecha solamente cuando `ReturnCycleNumber = OriginalCycleNumber + 1` con evidencia R23, siguiendo §4.2, sin permitir C1 mismo día. Se mantiene R23 de días anteriores en C1–C5 cuando cumple plazos; no se rechaza por defecto todo R23 mismo día. La interpretación de la excepción versus ordinaria requiere casos certificados PT-07.
- **HC-CENIT-007:** `CenitIncomingReturnPolicy.EvaluateR06` (líneas base F03 188–231) permitía R06 en C5 de originales C1–C3 con `ImmediateReturnCycleConfirmed` informado. Ahora C5 exige **origen C4 y mismo día operacional** antes de aplicar condiciones R06: monto completo, solicitud, ciclo inmediato, confirmación y fondos cuando procede. Fuente: DSP-152 Anexo 2 2025 §3.2 y Anexo A R06.
- **HC-CENIT-009:** R23 posterior comparaba `ReturnValueDate` con `ReceiverRejectionDeadlineDate` aportado (base F03 líneas 268–275). Se rechaza ese mecanismo: los dos caminos productivos llaman a `IOperationalCalendarService.GetNextBusinessDayAsync` desde **el día posterior a la recepción de la notificación**. Exigen `ReceiverNotificationReceivedDate` diferenciada de Fecha Valor y fecha de entrada original procedente del ciclo persistido (`AchCycle.ProcessingDate`); la policy verifica 15 días calendario desde esa entrada y el siguiente día hábil desde notificación. Falta evidencia de notificación → revisión manual, no aceptación por fecha límite externa. Fuente: DSP-152 Anexo 2 §4.2 y Anexo A R23.

## Camino real, reversión y alcance

Salida: `AchReturnsService.GenerateReturnsFileAsync` → `AchReturnEligibilityPolicyComponent.ValidateCenitOperationalPolicyAsync` → `CenitIncomingReturnPolicy.Evaluate` → generación NACHA-M, cambios de estado y eventos que ya existían. Entrada: `IncomingNachaPostParseProcessor.EvaluateCenitReturnPolicyAsync` → misma policy → clasificaciones/transiciones existentes. En ambos casos, calendario usado **solo CENIT**. `ReceiverRejectionDeadlineDate` se mantiene en el DTO por compatibilidad, **NO autoriza** R23. No se añadieron migraciones/tablas, CUD API, código de liquidación, entidades ni dependencias.

**Archivos productivos modificados (5):** `CenitIncomingReturnPolicy.cs`, `CenitIncomingReturnPolicyModels.cs`, `AchReturnEligibilityPolicyComponent.cs`, `AchReturnsService.cs`, `IncomingNachaPostParseProcessor.cs` (rutas exactas en manifiesto/patch). **Test original actualizado (1):** `CenitIncomingReturnPolicyTests.cs`; **nuevos test (2)**: `CenitF04ReturnPolicyTests.cs` y `CenitF04CalendarRouteTests.cs`; **script nuevo:** `cenit_f04_static_check.py`. **Diff/reversión**: `F04_CAMBIOS.patch` prueba de aplicación y reversión en seco contra bytes originales de F03; retirar 3 archivos nuevos y docs F04 para volver a F03, o restaurar íntegramente ZIP F03.

## Verificaciones realmente ejecutadas

- `python scripts/test/cenit_f04_static_check.py`: **21/21 PASS**.
- Repetidas: F01 **12/12**, F02 **13/13**, F03 **17/17** PASS. Son pruebas de estructura de fuente, NO compilación C#.
- Parche `patch` aplicado y reversión seca contra seis archivos originales F03: **6/6** PASS.
- **xUnit .NET: 0 ejecutadas, 0 aprobadas, 0 fallidas** (SDK .NET 10 no disponible). Dos nuevos archivos de pruebas: **22 casos fuente** (8 `[Fact]` + 12 `[InlineData]` de dos `[Theory]` en policy = 20; 2 `[InlineData]` de un `[Theory]` sobre calendario = 2). Se ajustó la batería existente R23 para aportar fecha de recepción, notificación y límite calculado.
- **SQL Server/PostgreSQL, gateway, muestras reales, Quartz, regresión funcional ACH Colombia: NO ejecutados**. Comparación SHA-256 completa confirma ausencia de ediciones originales imprevistas en ACH Colombia; NO equivale a una ejecución funcional.

## Riesgos y evidencias pendientes para cerrar

1. `PT-07` contrastar aceptación R23 mismo día en ciclo inmediato vs R23 de días anteriores en C1–C5 con muestras del operador; la excepción del ciclo 1 nunca aplica al mismo día.
2. `PT-08` acreditar fecha de recepción efectiva de entrada **en Participante Receptor**, fecha y fuente verificable de notificación del Receptor, días hábiles Bogotá (incluido festivo 17-ago-2026) y 15 días calendario. `AchCycle.ProcessingDate` es indicador persistido de ciclo, **no prueba independiente de hora de entrega al banco**; si fechas reales difieren, requiere integración documental/ajuste localizado antes del cierre. `ReceiverNotificationReceivedDate` viene de evidencia operativa y necesita validación de autenticidad procedimental.
3. `PT-09` R06 C5 origen C1–C4, ambos signos, fondos según post-abono, confirmación; verificar en entrada y salida real.
4. `PT-20` suite ACH Colombia completa; `PT-21` rollback, no emisión ante manual y reintentos; `PT-22` interpretación normativa confirmada por Banco/operador.
5. Restricción de F03 `CENIT_QUEUE_VALUE_DATE_REVIEW_REQUIRED` permanece, **sin cambio automático de fecha valor**. Los casos interdiarios no admitidos requieren evidencia de tipo de instrucción y regla oficial; la corrección R23/R06 no autoriza trasladar indiscriminadamente transacciones de pago a otra fecha valor.
6. Discrepancia DN-CEN-007 (prenotificaciones C5) permanece pendiente; no se alteró la regla hasta confirmación. DN-CEN-008 nombres según canal también sin cambios.

**Dictamen parcial:** F04 implementada y revisada estáticamente, pendiente de pruebas C#/infraestructura y validación del operador. Ninguno de los 13 HC formalmente CERRADO.
