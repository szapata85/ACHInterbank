# APN-I03 — CENIT: cinco sesiones, misma fecha valor y último archivo

**Estado:** EJECUTADO_ESTATICAMENTE; HOMOLOGACION_RUNTIME_PENDIENTE. **Código productivo modificado:** no.

Regla normativa: DSP-152 Anexo 2, numeral 4.7 prenotificaciones y 4.8 devolución. Débito: prenotificación obligatoria; crédito: opcional. Devoluciones: dentro de la **misma fecha valor**, a más tardar en la **última sesión de devoluciones**. Vencido ese plazo sin devolución, se habilitan las entradas posteriores en los términos del manual.

| Sesión | Operación | Corte | Archivos salida (límite) | Uso relevante |
|---|---|---|---|---|
| C1 | 07:30–10:30 | 10:30 | Antes de 11:00 | Prenotificaciones crédito y débito |
| C2 | 11:00–13:00 | 13:00 | Antes de 13:30 | Primeras devoluciones de C1 |
| C3 | 13:30–15:00 | 15:00 | Antes de 15:30 | Devoluciones de C2 |
| C4 | 15:30–17:15 | 17:15 | Antes de 17:45 | Último ingreso de prenotificaciones nuevas |
| C5 | 17:45–18:45 | 18:45 | **Hasta 19:15** | Devoluciones de C4, sin prenotificaciones nuevas |

Ruta relevante: `RegulatoryCycleScheduleCatalog.CenitDsp152Cycles` y `RegulatoryCatalogSeeder` con `PrenotificationResponseDeadlineMode.OperationalDayEnd` para débito y crédito. `ClearingHouseValidationPolicyService` calcula el último `CutoffTime` de día (`18:45`), mientras que la liberación C5 puede extenderse a `19:15` — APN-R01.

**Caso crítico:** prenotificación C1 del día hábil; primera devolución en C2 (archivo disponible al finalizar C2, antes de 13:30); última oportunidad C5 con salida hasta 19:15. No declarar aceptación tácita a las 18:45 sin resolver el estado del archivo final.

**Pruebas:** P01–P05, P07, P13, P16, P17, P22. **Estado:** NO_EJECUTADO_RUNTIME.
