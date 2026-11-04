# CONTINUIDAD VIGENTE APN-I13 (2026-11-03)

La última base acumulativa es `ACHInterbank_COMPLETO_CORRECCION_PRENOTIFICACIONES_APN_I13_2026-11-03.zip`. Lea `MEMORY_STATE_APN_I13.md` para estado actualizado. **I13 es estabilización sin cambios productivos, NO homologación.** El texto histórico I09 se conserva a continuación.

---

# MEMORY STATE — APN-I09 (continuidad del proyecto)

**Fecha:** 2026-11-03. **Base:** ZIP completo APN-I08. **Siguiente base acumulativa:** `ACHInterbank_COMPLETO_CORRECCION_PRENOTIFICACIONES_APN_I09_2026-11-03.zip`.

## Reglas invariables

- **ACH Colombia trabaja con 7 ciclos**, política `ACHCOL-7C-20261014`. No reemplazar por cinco ciclos del manual V35; se emplea `Nuevos Ciclos de Proceso y Horarios.pdf`.
- **CENIT trabaja con 5 sesiones**; fin de entrega de salida de la quinta sesión: **19:15**.
- `MEMORY.md` y `MEMORY_STATE.md` históricos del proyecto **no se sobreescriben**.
- Los ZIP son acumulativos y contienen el proyecto completo. Nunca confundir pruebas estáticas con certificación runtime.

## Estado de la implementación

- APN-I01..I07: auditoría estática/documental, **sin código productivo corregido en esos incrementos**; homologación `NO_EJECUTADA`.
- APN-I08: fuente corregida de ventanas y liberación, homologación .NET/BD/UAT pendiente.
- **APN-I09:** parche en `AchTacitAcceptanceJobHandler`, guardia en `PrenotificationTacitAcceptanceDecision` y completitud de cinco sesiones CENIT en `ClearingHouseValidationPolicyService`. Sin migraciones, sin nuevos estados, sin cambio UI.
- **Pruebas:** scripts Python APN-I08 (6/6) e I09 (8/8) `PASS_ESTÁTICO`; pruebas xUnit añadidas/ajustadas pero **NO_EJECUTADAS** (no hay `dotnet`); SQL/BD/UAT **NO_EJECUTADOS**.

## Riesgos residuales / prioridad siguiente

- R01: mitigado parcialmente; falta certeza de recepción de todos los archivos del último ciclo.
- R02: corregido en fuente, no homologado.
- R03: mitigado parcialmente en I09; falta un **watermark real de cierre de recepción por cámara/ciclo** y la cobertura de archivos todavía sin cámara conocida.
- R04: abierto; estado de transacción, validación e intento terminal ante devoluciones tardías.
- R05: abierto; ejecución .NET/SQL/UAT.
- R06: abierto; scheduler cada 30 minutos y demoras de SLA.
- R07: nuevo riesgo de falta de prueba concluyente de recepción de todos los archivos; no confundir archivo registrado con cierre del ciclo.

**Siguiente trabajo recomendado (APN-I10):** certificar una barrera explícita de ingestión/cierre por cámara/ciclo y prueba de concurrencia con respuesta tardía; primero verificar disponibilidad de la señal real de recepción y no inventarla.
