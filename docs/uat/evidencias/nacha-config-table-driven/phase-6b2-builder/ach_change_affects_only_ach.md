# ACH Colombia - Cambio aislado por camara

Prueba automatizada: `ChangingAchColombiaField_ShouldAffectOnlyAchColombiaFile`.

Resultado:

- Se genera archivo ACH Colombia baseline usando `OFFICIAL_ACH_SALIDA_ORIGINAL_V1_0`.
- Se genera archivo CENIT baseline usando `OFFICIAL_CENIT_SALIDA_ORIGINAL_V1_0`.
- Se modifica un campo del perfil ACH Colombia.
- El archivo ACH cambia.
- El archivo CENIT permanece igual.

Conclusion: los perfiles son independientes y el cambio de ACH Colombia no afecta CENIT.

Productivo: **NO-GO**.
