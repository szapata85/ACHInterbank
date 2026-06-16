# Matriz campo a campo NACHA-M - ACH Colombia

| Requisito | Documento fuente | Registro/Campo | Valor esperado | Valor generado | Resultado | Evidencia |
|---|---|---|---|---|---|---|
| Nombre de archivo salida | MAN-004 Servicio ACH Transferencias Interbancarias Para Entidades Participantes V32 | Archivo | RRRRTTT.ZZZ.N | 0001283.002.1 | OK | metadata.json |
| Codigo ruta originador | MAN-004 Servicio ACH Transferencias Interbancarias Para Entidades Participantes V32 | RRRR | 0001 | 0001 | OK | Nombre archivo |
| Codigo transito originador | MAN-004 Servicio ACH Transferencias Interbancarias Para Entidades Participantes V32 | TTT | 283 | 283 | OK | Nombre archivo |
| Consecutivo diario | MAN-004 Servicio ACH Transferencias Interbancarias Para Entidades Participantes V32 | ZZZ | 002 | 002 | OK | Nombre archivo |
| Identificador interno | MAN-004 Servicio ACH Transferencias Interbancarias Para Entidades Participantes V32 | Registro 1 campo 7 | B | B | OK | Registro tipo 1 |
| Registro tipo 1 presente | MAN-004 Servicio ACH Transferencias Interbancarias Para Entidades Participantes V32 | 1 | 1 | 1 | OK | Archivo generado |
| Registro tipo 5 presente | MAN-004 Servicio ACH Transferencias Interbancarias Para Entidades Participantes V32 | 5 | 1 | 1 | OK | Archivo generado |
| Registro tipo 6 presente | MAN-004 Servicio ACH Transferencias Interbancarias Para Entidades Participantes V32 | 6 | 1 | 1 | OK | Archivo generado |
| Registro tipo 7 presente | MAN-004 Servicio ACH Transferencias Interbancarias Para Entidades Participantes V32 | 7 | 1 | 1 | OK | Archivo generado |
| Registro tipo 8 presente | MAN-004 Servicio ACH Transferencias Interbancarias Para Entidades Participantes V32 | 8 | 1 | 1 | OK | Archivo generado |
| Registro tipo 9 presente/padding | MAN-004 Servicio ACH Transferencias Interbancarias Para Entidades Participantes V32 | 9 | >=1 | 5 | OK | Archivo generado |
| Entry/addenda count | MAN-004 Servicio ACH Transferencias Interbancarias Para Entidades Participantes V32 | Registro 9 | 00000002 | 00000002 | OK | Registro tipo 9 |
| Entry hash | MAN-004 Servicio ACH Transferencias Interbancarias Para Entidades Participantes V32 | Registro 8/9 | 0099999002 | 0099999002 | OK | Registros 8 y 9 |
| Block count | MAN-004 Servicio ACH Transferencias Interbancarias Para Entidades Participantes V32 | Registro 9 | 000001 | 000001 | OK | Registro tipo 9 |
| Total debito raw | MAN-004 Servicio ACH Transferencias Interbancarias Para Entidades Participantes V32 | Registro 9 | 000000000000100000 | 000000000000100000 | OK | Registro tipo 9 |
| No transmision externa | Restriccion UAT | Operacion | false | false | OK | metadata.json |
