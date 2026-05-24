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

