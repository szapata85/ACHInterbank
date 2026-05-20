| Regla | Registro/Campo | Valor esperado | Valor generado | Resultado | Evidencia |
|---|---|---:|---:|---|---|
| Nombre normativo | Archivo | RRRRTTT.ZZZ.1 | 0001283.002.1 | OK | metadata.json |
| Originador CFA | Archivo | RRRR=0001 TTT=283 | 0001283 | OK | nombre archivo |
| Secuencia diaria | Archivo ZZZ | 001-036 | 002 | OK | nombre archivo |
| Identificador interno | Registro 1 campo 7 | B | B | OK | archivo NACHA-M |
| Código prenotificación | Registro 6 TransactionCode | 28 | 28 | OK | archivo NACHA-M |
| Registros mínimos | 1/5/6/7/8/9 | Presentes | 1:1, 5:1, 6:1, 7:1, 8:1, 9:5 | OK | archivo NACHA-M |
| Block count | Registro 9 | Calculado por sistema | 000001 | OK técnico | archivo NACHA-M |
| Entry hash | Registro 8/9 | Calculado por sistema | 0099998002 | OK técnico | archivo NACHA-M |
| Transmisión externa | Operación | false | false | OK | metadata.json |
