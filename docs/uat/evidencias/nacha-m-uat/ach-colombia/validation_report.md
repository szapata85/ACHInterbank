# Evidencia NACHA-M UAT - ACH Colombia

Fecha: 2026-05-20T11:02:44-05:00
Rama: feature/parametrizacion-reglas-camara-prenotificacion
Commit base: ffe311cf
Estado productivo: NO-GO

## Archivo generado

- Nombre normativo: 0001283.002.1
- Ruta: docs/uat/evidencias/nacha-m-uat/ach-colombia/0001283.002.1
- Tamano: 1060 bytes
- SHA256: E4DAEEE551596D067357953C552CD521871F635F6703D27700171EBC10A0026E
- Generado por: endpoint real /NachaExport/30efaac22a44ed70267f7da09bb806b084d7f263 via SPA localhost:743
- Envio externo: false

## Nomenclatura

| Campo | Valor | Resultado |
|---|---:|---|
| Patron | RRRRTTT.ZZZ.N | OK |
| RRRR | 0001 | OK |
| TTT | 283 | OK |
| ZZZ | 002 | OK |
| Identificador interno registro 1 campo 7 | B | OK |
| Mapeo ZZZ -> campo 7 | 002 -> B | OK |
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
| Entry hash | 0099999002 |
| Total debito raw | 000000000000100000 |

## Transaccion y prenotificacion

| Tipo | Id | Referencia | Estado | Observacion |
|---|---:|---|---|---|
| Prenotificacion UAT madura | 252 | UAT-ACH-PRE-MATURED-001 | AppliedTacitly | Precondicion UAT controlada, sin envio externo |
| Debito monetario | 254 | UAT-ACH-DEB-MATURED-001 | Pending | Incluido en archivo NACHA-M |

## Fuente normativa

MAN-004 Servicio ACH Transferencias Interbancarias Para Entidades Participantes V32

## Resultado

Archivo NACHA-M UAT no vacio, no HTML, no JSON de error, con registros 1/5/6/7/8/9 y nomenclatura parametrizada por camara. No se transmitio a camaras reales.
