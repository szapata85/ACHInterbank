# Evidencia NACHA-M UAT - CENIT

Fecha: 2026-05-20T11:02:44-05:00
Rama: feature/parametrizacion-reglas-camara-prenotificacion
Commit base: ffe311cf
Estado productivo: NO-GO

## Archivo generado

- Nombre normativo: 0001283.001.1
- Ruta: docs/uat/evidencias/nacha-m-uat/cenit/0001283.001.1
- Tamano: 1060 bytes
- SHA256: FD52F7834ADEC53C720E4A877B1D48A8AC15B149BEB7FAFB91EC57CF1B88FCD4
- Generado por: endpoint real /NachaExport/36e1bc97a46b0b1e7f7d04ed98030bda2c4d9c0e via SPA localhost:743
- Envio externo: false

## Nomenclatura

| Campo | Valor | Resultado |
|---|---:|---|
| Patron | RRRRTTT.ZZZ.1 | OK |
| RRRR | 0001 | OK |
| TTT | 283 | OK |
| ZZZ | 001 | OK |
| Identificador interno registro 1 campo 7 | A | OK |
| Mapeo ZZZ -> campo 7 | 001 -> A | OK |
| Originador | Cooperativa Financiera de Antioquia | OK |

## Registros y controles

| Control | Valor |
|---|---:|
| Registros fisicos | 10 |
| Tipo 1 | 1 |
| Tipo 5 | 1 |
| Tipo 6 | 1 |
| Tipo 7 | 1 |
| Tipo 8 | 1 |
| Tipo 9 | 5 |
| Batch count | 000001 |
| Block count | 000001 |
| Entry/addenda count | 00000002 |
| Entry hash | 0099998002 |
| Total debito raw | 000000000000100000 |

## Transaccion y prenotificacion

| Tipo | Id | Referencia | Estado | Observacion |
|---|---:|---|---|---|
| Prenotificacion UAT madura | 253 | UAT-CEN-PRE-MATURED-001 | AppliedTacitly | Precondicion UAT controlada, sin envio externo |
| Debito monetario | 255 | UAT-CEN-DEB-MATURED-001 | Pending | Incluido en archivo NACHA-M |

## Fuente normativa

Ejemplos CENIT disponibles en el proyecto / pendiente homologacion normativa formal

## Resultado

Archivo NACHA-M UAT no vacio, no HTML, no JSON de error, con registros 1/5/6/7/8/9 y nomenclatura parametrizada por camara. No se transmitio a camaras reales.
