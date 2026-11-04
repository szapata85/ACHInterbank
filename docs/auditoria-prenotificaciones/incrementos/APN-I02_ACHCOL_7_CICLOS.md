# APN-I02 — ACH Colombia: siete ciclos y cuatro ciclos siguientes

**Estado:** EJECUTADO_ESTATICAMENTE; HOMOLOGACION_RUNTIME_PENDIENTE. **Código productivo modificado:** no.

La política del catálogo actual es `RegulatoryCycleScheduleCatalog.AchColombiaSevenCycleConfirmedPolicy` versión `ACHCOL-7C-20261014`, con 7 ciclos (no usar 5 del V35 para horarios actuales). `RegulatoryCatalogSeeder.BuildClearingHouseTransactionRules` define `CycleCount` y `PrenotificationResponseWindowCycles = 4` tanto para débito como crédito.

| Origen | Corte | Primera devolución (ciclo/archivo) | Cuarto ciclo siguiente y última liberación |
|---|---|---|---|
| C1 | 07:00 | C2, 09:45–10:00 | C5, 15:15–15:30 del mismo día |
| C2 | 09:00 | C3, 11:45–12:00 | C6, 16:45–17:00 del mismo día |
| C3 | 11:00 | C4, 13:15–13:30 | C7, 18:45–19:00 del mismo día |
| C4 | 12:30 | C5, 15:15–15:30 | C1, 07:45–08:00 del siguiente día hábil |
| C5 | 14:30 | C6, 16:45–17:00 | C2, 09:45–10:00 del siguiente día hábil |
| C6 | 16:00 | C7, 18:45–19:00 | C3, 11:45–12:00 del siguiente día hábil |
| C7 | 18:00 | C1, 07:45–08:00 del siguiente día hábil | C4, 13:15–13:30 del siguiente día hábil |

**Este cuadro es una matriz de expectativa basada en la tabla de siete ciclos, no la observación del runtime del proyecto.** Confirmar que los cortes no se tomen como confirmación bancaria y que el calendario esté efectivamente poblado para cuatro ciclos posteriores.

### Puntos de auditoría

- `ClearingHouseValidationPolicyService.ResolvePrenotificationResponseDeadlineAsync` selecciona ciclos por `ProcessingDate/CutoffTime` y devuelve `CutoffTime`, anterior a la liberación del archivo de respuesta. APN-R01.
- El mismo método usa `.Take(window).LastOrDefault()` sin exigir que se encuentren exactamente cuatro ciclos; evaluar datos incompletos (APN-R02).
- Comprobar festivos, día hábil siguiente, reprogramación, tipos no admitidos, estados de transmisión y actualizaciones del seed. Revisar `docs/operations/ACHCOL_CICLOS_7_VIGENCIA_2026-10-14.md`.
- Probar las ventanas de devolución reales de la tabla, no cuatro ciclos imaginados exclusivamente por ordinal.

**Pruebas obligatorias:** P01–P05, P07, P13–P15, P21, P22. **Estado:** NO_EJECUTADO_RUNTIME.
