# CENIT - Cambio aislado por camara

Prueba automatizada: `ChangingCenitField_ShouldAffectOnlyCenitFile`.

Resultado:

- Se genera archivo ACH Colombia baseline usando `OFFICIAL_ACH_SALIDA_ORIGINAL_V1_0`.
- Se genera archivo CENIT baseline usando `OFFICIAL_CENIT_SALIDA_ORIGINAL_V1_0`.
- Se modifica un campo del perfil CENIT.
- El archivo CENIT cambia.
- El archivo ACH Colombia permanece igual.

Conclusion: los perfiles son independientes y el cambio de CENIT no afecta ACH Colombia.

Productivo: **NO-GO**.
