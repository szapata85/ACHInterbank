# Evidencias prenotificacion UAT madura

Fecha: 2026-05-20  
Estado productivo: NO-GO

## Alcance

Se crearon precondiciones UAT controladas, versionadas por migracion EF Code First, para validar debitos monetarios posteriores sin bypass informal ni SQL manual no versionado.

## Prenotificaciones maduras

| Camara | TransactionId | Referencia | Estado | Fecha efectiva | Cuenta sintetica | Control |
|---|---:|---|---|---|---|---|
| ACH Colombia | 252 | UAT-ACH-PRE-MATURED-001 | AppliedTacitly | 2026-05-13 | 0000001102 | PRENOTE_UAT_PRECONDITION_CREATED |
| CENIT | 253 | UAT-CEN-PRE-MATURED-001 | AppliedTacitly | 2026-05-13 | 0000001202 | PRENOTE_UAT_PRECONDITION_CREATED |

## Debitos monetarios posteriores

| Camara | TransactionId | Referencia | Estado | Monto | Archivo generado |
|---|---:|---|---|---:|---|
| ACH Colombia | 254 | UAT-ACH-DEB-MATURED-001 | Pending | 1000 | 0001283.002.1 |
| CENIT | 255 | UAT-CEN-DEB-MATURED-001 | Pending | 1000 | 0001283.001.1 |

## Salvaguardas

- Datos sinteticos/anonimizados.
- Sin cuentas reales, clientes reales ni identificaciones reales.
- Sin transmision externa.
- Sin backdating productivo: fecha UAT madura documentada como precondicion controlada.
- Trazabilidad via eventos `UAT_PRENOTE_CREATED` / `UAT_DEBIT_CREATED` y payload con `PRENOTE_UAT_PRECONDITION_CREATED`.
