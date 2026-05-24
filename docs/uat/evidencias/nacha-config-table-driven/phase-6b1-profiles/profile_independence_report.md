# Reporte de independencia de perfiles

## Resultado

ACH Colombia y CENIT quedan como perfiles separados:

| Camara | ProfileCode | Variants propios | Fields propios |
|---|---|---|---|
| ACH Colombia | `OFFICIAL_ACH_SALIDA_ORIGINAL_V1_0` | Si | Si |
| CENIT | `OFFICIAL_CENIT_SALIDA_ORIGINAL_V1_0` | Si | Si |

Validaciones automatizadas:

- `AchColombiaAndCenitProfiles_ShouldBeIndependent`
- `AchColombiaProfile_ShouldContainRecords_1_5_6_7_8_9`
- `CenitProfile_ShouldContainRecords_1_5_6_7_8_9`

Conclusiones:

- No comparten `profileId`.
- No comparten instancias de `CfgLayoutVariant`.
- No comparten instancias de `CfgLayoutField`.
- Cambios futuros en un perfil no deben mutar la estructura del otro perfil.

Productivo: **NO-GO**.

