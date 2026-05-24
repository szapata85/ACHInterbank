# NACHA Config Table-Driven Oficial - Direccion tecnica

## Principio

La generacion NACHA-M oficial debe resolver siempre un `CfgProfile` publicado y vigente para la camara, flujo, direccion y servicio aplicables.

## Reglas objetivo Fase 6B

1. ACH Colombia y CENIT deben tener perfiles separados.
2. Registros 1/5/6/7/8/9 deben salir de `CfgProfileRecord` + `CfgLayoutVariant` + `CfgLayoutField`.
3. Campos requeridos sin source o sin valor deben fallar controladamente.
4. Valores largos no deben truncarse silenciosamente.
5. Calculos criticos NACHA-M permanecen en codigo y se trazan.
6. Legacy queda solo como referencia historica, migracion o shadow compare.
7. La SPA oficial debe ser `/nacha-config-admin/perfiles`.

## Calculos no editables libremente

- EntryHash.
- BlockCount.
- BatchCount.
- Totales debito/credito.
- Check digit.

Estos calculos pueden representarse como fuentes calculadas en trace, pero no deben moverse a configuracion editable sin control normativo.

Productivo: **NO-GO**.

## Actualizacion 2026-05-24 - Fase 6B.1

Se agregan seeds runtime idempotentes de perfiles oficiales UAT/local:

- `OFFICIAL_ACH_SALIDA_ORIGINAL_V1_0`.
- `OFFICIAL_CENIT_SALIDA_ORIGINAL_V1_0`.

Ambos perfiles quedan:

- publicados (`PUBLICADO`);
- vigentes desde `2026-01-01`;
- separados por `CatClearingHouse`;
- con registros 1/5/6/7/8/9;
- con `CfgLayoutVariant` propio por record;
- con `CfgLayoutField` propio por record;
- con fuentes `CONSTANTE`, `ENTIDAD` y `EXPRESION`;
- sin cambiar aun el modo oficial de `NachaFileBuilder`.

Evidencia: `docs/uat/evidencias/nacha-config-table-driven/phase-6b1-profiles/`.

Brechas pendientes para Fase 6B.2:

- builder oficial table-driven;
- fail-fast;
- trace FieldDefinition -> valor generado;
- deprecacion legacy;
- SPA oficial.

Productivo: **NO-GO**.
