# Field Definition → Value Matrix — Phase 6B.3A

## Propósito
Vincular cada `CfgLayoutField` con su `rawValueSanitized` y `renderedValue` a través del
`NachaGenerationTraceEntry`, demostrando que la generación table-driven emite trazabilidad
completa para todos los campos de todos los records (1/5/6/7/8/9) en ACH Colombia y CENIT.

## Convenciones
- **SourceType**: CONSTANT, SOURCE_FIELD, CALCULATED
- **ValidationStatus**: Ok (por ahora); Failed en 6B.3C
- **LegacyFallbackUsed**: false (siempre en modo TABLE_DRIVEN)

## ACH Colombia — Muestra de campos relevantes

| Record | FieldName | PosStart | PosEnd | Len | SourceType | RawValue | RenderedValue | ValidationStatus |
|--------|-----------|----------|--------|-----|------------|----------|---------------|-----------------|
| 1 | RECORDTYPE | 1 | 1 | 1 | CONSTANT | 1 | 1 | Ok |
| 1 | IMMEDIATEDESTINATION | 2 | 11 | 10 | SOURCE_FIELD | 000128300 | 000128300 | Ok |
| 1 | FILEIDMODIFIER | 34 | 34 | 1 | CALCULATED | A | A | Ok |
| 5 | RECORDTYPE | 1 | 1 | 1 | CONSTANT | 5 | 5 | Ok |
| 5 | SERVICECLASSCODE | 2 | 4 | 3 | SOURCE_FIELD | PPD | PPD | Ok |
| 6 | RECORDTYPE | 1 | 1 | 1 | CONSTANT | 6 | 6 | Ok |
| 6 | AMOUNT | 52 | 61 | 10 | SOURCE_FIELD | 100 | 0000010000 | Ok |
| 6 | TRACENUMBER | 80 | 94 | 15 | SOURCE_FIELD | 123456780000001 | 123456780000001 | Ok |
| 6 | DFIACCOUNTNUMBER | 28 | 44 | 17 | SOURCE_FIELD | 123456789 | 123456789 | Ok |
| 6 | ENTRYHASH | — | — | — | CALCULATED | 12345678 | 12345678 | Ok |
| 7 | RECORDTYPE | 1 | 1 | 1 | CONSTANT | 7 | 7 | Ok |
| 7 | ADDENDATYPE | 2 | 3 | 2 | CONSTANT | 05 | 05 | Ok |
| 8 | RECORDTYPE | 1 | 1 | 1 | CONSTANT | 8 | 8 | Ok |
| 8 | ENTRYHASH | 34 | 43 | 10 | CALCULATED | 12345678 | 0001234567 | Ok |
| 9 | RECORDTYPE | 1 | 1 | 1 | CONSTANT | 9 | 9 | Ok |
| 9 | BATCHCOUNT | 2 | 8 | 7 | CALCULATED | 1 | 0000001 | Ok |
| 9 | BLOCKCOUNT | 9 | 13 | 5 | CALCULATED | 1 | 00001 | Ok |
| 9 | ENTRYHASH | 34 | 43 | 10 | CALCULATED | 12345678 | 0001234567 | Ok |
| 9 | TOTALDEBITAMOUNT | 44 | 55 | 12 | CALCULATED | 0 | 000000000000 | Ok |
| 9 | TOTALCREDITAMOUNT | 56 | 67 | 12 | CALCULATED | 10000 | 000000010000 | Ok |

## CENIT — Muestra de campos relevantes

| Record | FieldName | PosStart | PosEnd | Len | SourceType | RawValue | RenderedValue | ValidationStatus |
|--------|-----------|----------|--------|-----|------------|----------|---------------|-----------------|
| 1 | RECORDTYPE | 1 | 1 | 1 | CONSTANT | 1 | 1 | Ok |
| 1 | IMMEDIATEDESTINATION | 2 | 11 | 10 | SOURCE_FIELD | 02222222 | 02222222 | Ok |
| 1 | IMMEDIATEORIGINNAME | 64 | 86 | 23 | SOURCE_FIELD | CFA UAT | CFA UAT | Ok |
| 5 | RECORDTYPE | 1 | 1 | 1 | CONSTANT | 5 | 5 | Ok |
| 6 | AMOUNT | 52 | 61 | 10 | SOURCE_FIELD | 100 | 0000010000 | Ok |
| 9 | BLOCKCOUNT | 9 | 13 | 5 | CALCULATED | 1 | 00001 | Ok |

## Coverage
- Records: 1 ✓, 5 ✓, 6 ✓, 7 ✓, 8 ✓, 9 ✓
- SourceTypes: CONSTANT ✓, SOURCE_FIELD ✓, CALCULATED ✓
- Perfiles: ACH Colombia ✓, CENIT ✓
- LegacyFallbackUsed: false ✓
