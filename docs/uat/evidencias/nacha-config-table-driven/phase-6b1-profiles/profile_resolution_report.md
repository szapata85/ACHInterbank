# Reporte de resolucion de perfiles

## Resolver

Servicio: `NachaConfigResolver`.

Contexto validado:

| Camara | FlowType | Direction | ServiceClass | Fecha proceso | Resultado |
|---|---|---|---|---|---|
| ACH | ORIGINAL | SALIDA | PPD | 2026-05-24 | Resuelve `OFFICIAL_ACH_SALIDA_ORIGINAL_V1_0` |
| CENIT | ORIGINAL | SALIDA | PPD | 2026-05-24 | Resuelve `OFFICIAL_CENIT_SALIDA_ORIGINAL_V1_0` |

Tests:

- `Profiles_ShouldBeResolvableByNachaConfigResolver_ForAchColombia`
- `Profiles_ShouldBeResolvableByNachaConfigResolver_ForCenit`

Resultado:

- `Success=true`.
- `UsedFallback=false`.
- Layouts resueltos para records 1/5/6/7/8/9.

Productivo: **NO-GO**.

