# Matriz campo a campo NACHA-M - CENIT

| Requisito | Documento fuente | Registro/Campo | Valor esperado | Valor generado | Resultado | Evidencia |
|---|---|---|---|---|---|---|
| Nombre de archivo salida | Ejemplos CENIT disponibles en el proyecto / pendiente homologacion normativa formal | Archivo | RRRRTTT.ZZZ.1 | 0001283.001.1 | OK | metadata.json |
| Codigo ruta originador | Ejemplos CENIT disponibles en el proyecto / pendiente homologacion normativa formal | RRRR | 0001 | 0001 | OK | Nombre archivo |
| Codigo transito originador | Ejemplos CENIT disponibles en el proyecto / pendiente homologacion normativa formal | TTT | 283 | 283 | OK | Nombre archivo |
| Consecutivo diario | Ejemplos CENIT disponibles en el proyecto / pendiente homologacion normativa formal | ZZZ | 001 | 001 | OK | Nombre archivo |
| Identificador interno | Ejemplos CENIT disponibles en el proyecto / pendiente homologacion normativa formal | Registro 1 campo 7 | A | A | OK | Registro tipo 1 |
| Registro tipo 1 presente | Ejemplos CENIT disponibles en el proyecto / pendiente homologacion normativa formal | 1 | 1 | 1 | OK | Archivo generado |
| Registro tipo 5 presente | Ejemplos CENIT disponibles en el proyecto / pendiente homologacion normativa formal | 5 | 1 | 1 | OK | Archivo generado |
| Registro tipo 6 presente | Ejemplos CENIT disponibles en el proyecto / pendiente homologacion normativa formal | 6 | 1 | 1 | OK | Archivo generado |
| Registro tipo 7 presente | Ejemplos CENIT disponibles en el proyecto / pendiente homologacion normativa formal | 7 | 1 | 1 | OK | Archivo generado |
| Registro tipo 8 presente | Ejemplos CENIT disponibles en el proyecto / pendiente homologacion normativa formal | 8 | 1 | 1 | OK | Archivo generado |
| Registro tipo 9 presente/padding | Ejemplos CENIT disponibles en el proyecto / pendiente homologacion normativa formal | 9 | >=1 | 5 | OK | Archivo generado |
| Entry/addenda count | Ejemplos CENIT disponibles en el proyecto / pendiente homologacion normativa formal | Registro 9 | 00000002 | 00000002 | OK | Registro tipo 9 |
| Entry hash | Ejemplos CENIT disponibles en el proyecto / pendiente homologacion normativa formal | Registro 8/9 | 0099998002 | 0099998002 | OK | Registros 8 y 9 |
| Block count | Ejemplos CENIT disponibles en el proyecto / pendiente homologacion normativa formal | Registro 9 | 000001 | 000001 | OK | Registro tipo 9 |
| Total debito raw | Ejemplos CENIT disponibles en el proyecto / pendiente homologacion normativa formal | Registro 9 | 000000000000100000 | 000000000000100000 | OK | Registro tipo 9 |
| No transmision externa | Restriccion UAT | Operacion | false | false | OK | metadata.json |
