# ACH Colombia - Matriz records/fields Fase 6B.1

Perfil: `OFFICIAL_ACH_SALIDA_ORIGINAL_V1_0`

| Record | Variant | Fields | Cobertura | Fuente normativa |
|---|---|---:|---|---|
| 1 | `ACH_R1_BASE_V1` | 14 | 1..106 | MAN-004 V32 |
| 5 | `ACH_R5_BASE_V1` | 14 | 1..106 | MAN-004 V32 |
| 6 | `ACH_R6_BASE_V1` | 12 | 1..106 | MAN-004 V32 |
| 7 | `ACH_R7_BASE_V1` | 6 | 1..106 | MAN-004 V32 |
| 8 | `ACH_R8_BASE_V1` | 12 | 1..106 | MAN-004 V32 |
| 9 | `ACH_R9_BASE_V1` | 8 | 1..106 | MAN-004 V32 |

Tipos de fuente usados:

- `CONSTANTE`: record type, constantes normativas.
- `ENTIDAD`: campos fuente de transaccion/addenda con `PropertyPath` controlado.
- `EXPRESION`: calculos runtime, derivados y filler.

No se cambio `NachaFileBuilder` a modo oficial en esta subfase.

