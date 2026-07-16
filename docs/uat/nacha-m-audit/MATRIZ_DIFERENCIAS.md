# Matriz de diferencias NACHA-M

Los valores se expresan como patrones, presencia/ausencia o categorías. No se reproducen cuentas, identificaciones, nombres, importes, referencias ni Trace Numbers.

| ID | Cámara | Registro / nivel | Campo / posición | Valor encontrado en ACHInterbank | Comparativo tercero | Regla / fuente | Clasificación | Severidad | Impacto | Estado | Corrección propuesta |
|---|---|---|---|---|---|---|---|---|---|---|---|
| DIF-001 | ACHCOL | Físico | Longitud 106 | 670 registros exactos | 420 exactos | MAN-004 V32 6.1.2 | Diferencia de datos válida | Informativa | Ninguno | Cumple | Mantener prueba byte a byte |
| DIF-002 | ACHCOL | Físico | Bloques/padding | 67 bloques; 6 T9 padding | 42; 2 padding | MAN-004 V32 6.1.2 | Diferencia de datos válida | Informativa | Ninguno | Cumple | Mantener recálculo físico |
| DIF-003 | ACHCOL | Físico | Codificación | ASCII puro | Un carácter monobyte permitido | MAN-004 V32 6.1.4 | Interoperabilidad | Alta | Posible reemplazo por `Encoding.ASCII` | No demostrado | Definir encoding monobyte oficial y prueba de repertorio |
| DIF-004 | ACHCOL | T1 | Fecha, 24–31 | Sólo seis dígitos ocupan 24–29 | Ocho dígitos en 24–31 | MAN-004 V32, T1 campo 5 | Normativa crítica | Crítica | Fecha inválida/desplaza campos | No cumple | `yyyyMMdd`, longitud 8 |
| DIF-005 | ACHCOL | T1 | Hora, 32–35 | Empieza dos posiciones antes | Ocupa 32–35 | MAN-004 V32, T1 campo 6 | Normativa crítica | Crítica | Header no interoperable | No cumple | Corregir todos los offsets posteriores |
| DIF-006 | ACHCOL | T1 | FileId, 36 | Descriptor en 34 | Presente en 36 | MAN-004 V32 6.1.10.1/T1 | Normativa crítica | Crítica | Correlación nombre/header inválida | No cumple | Mover a 36 y probar ZZZ↔FileId |
| DIF-007 | ACHCOL | T1 | RecordSize/Block/Format, 37–42 | Valores correctos en offsets anteriores | Valores en offsets oficiales | MAN-004 V32 T1 8–10 | Normativa crítica | Crítica | Rechazo por formato | No cumple | Corregir 37–39, 40–41, 42 |
| DIF-008 | ACHCOL | T1 | Nombres, 43–88 | Dos posiciones desplazados | Offsets oficiales | MAN-004 V32 T1 11–12 | Interoperabilidad | Alta | Contenido semántico cortado/mezclado | No cumple | Corregir descriptor |
| DIF-009 | ACHCOL | T1 | Literal de ciclo, 87–93 | Texto interno presente y cruza dos campos oficiales | No presente; ReferenceCode en blanco | MAN-004 sólo define código del sistema | No demostrada | Crítica | Campo origen y referencia contaminados | No cumple | Desacoplar ciclo; resolver fuente oficial |
| DIF-010 | ACHCOL | T1 | Reservado, 97–106 | Filler configurado 95–106 | Blanco 97–106 | MAN-004 V32 T1 campo 14 | Normativa crítica | Alta | Longitud lógica incorrecta | No cumple | Corregir a 97–106 |
| DIF-011 | ACHCOL | T5 | Fechas, 64–79 | Dos fechas de seis posiciones | Fechas de ocho posiciones | MAN-004 V32 T5 campos 8–9 | Normativa crítica | Crítica | Fecha operativa inválida | No cumple | Usar 8 y obligatoriedad por flujo |
| DIF-012 | ACHCOL | T5 | Status/origen/lote | Posiciones 79/80–87/88–94 | 83/84–91/92–98 | MAN-004 V32 T5 11–13 | Normativa crítica | Crítica | Lote y participante mal ubicados | No cumple | Corregir offsets |
| DIF-013 | ACHCOL | T5/T8 | Lote | 4/4 coinciden en offsets internos; 0/4 en oficiales | 78/78 en oficiales | MAN-004 V32 T5/T8 | Normativa crítica | Crítica | Control cruzado inválido para cámara | No cumple | Mover ambos campos y validar físicamente |
| DIF-014 | ACHCOL | Semántico | Reinicio lote | Contador diario por cámara/fecha/origen; puede continuar entre archivos | Comparativo inicia por archivo | MAN-004: inicia 1 dentro del archivo | Normativa crítica | Crítica | Rechazo/duplicidad de lote | No cumple | Número local por archivo; idempotencia separada |
| DIF-015 | ACHCOL | T6 | Monto, 30–47 | Longitud 10 | Longitud 18 | MAN-004 V32 T6 campo 6 | Normativa crítica | Crítica | Overflow/rechazo/desplazamiento | No cumple | Longitud 18, límites y centavos |
| DIF-016 | ACHCOL | T6 | ID/nombre receptor | Empiezan en 40/55 | 48/63 | MAN-004 V32 T6 campos 7–8 | Normativa crítica | Crítica | Datos en campos equivocados | No cumple | Mover y validar sin truncamiento |
| DIF-017 | ACHCOL | T6 | Indicador adenda, 87 | Está en 79; 0/327 válidos en 87 | 130/130 válidos | MAN-004 V32 T6 campo 10 | Normativa crítica | Crítica | Adenda no reconocida | No cumple | Mover y gate T6↔T7 |
| DIF-018 | ACHCOL | T6 | Trace, 88–102 | Está en 80–94; 0/327 con forma oficial | 130/130 válidos | MAN-004 V32 T6 campo 11 | Normativa crítica | Crítica | Correlación/duplicado inválidos | No cumple | Mover y validar composición/unicidad |
| DIF-019 | ACHCOL | Secuencia | Orden T6/T7 | Se agrupan 327 T6 y luego 327 T7; sólo 4 T6 tienen T7 inmediato | 130/130 pares inmediatos | MAN-004 6.1.3 + relación T7 | Operativa crítica | Crítica | Asociación ambigua/rechazo | No cumple | Renderizar pareja por transacción |
| DIF-020 | ACHCOL | T7 | Sufijo de Trace | 0/327 asociaciones con match oficial | 130/130 | MAN-004 V32 T7 campo 8 | Normativa crítica | Crítica | Adenda asociada a detalle incorrecto | No cumple | Control cruzado fail-closed |
| DIF-021 | ACHCOL | T8 | Totales | 12 posiciones cada uno | 18 posiciones | MAN-004 V32 T8 campos 5–6 | Normativa crítica | Crítica | Totales se desplazan | No cumple | Ampliar/mover campos |
| DIF-022 | ACHCOL | T8 | Conteo/hash | Recalculan 4/4 bajo offsets comunes | 78/78 | MAN-004 V32 T8 campos 3–4 | Diferencia permitida por perfil sólo en datos, no en layout | Baja | Algoritmo actual aprovechable | Cumple | Conservar cálculo y añadir vector oficial |
| DIF-023 | ACHCOL | T9 | Conteos/bloques/hash | Recalculan correctamente | Recalculan correctamente | MAN-004 V32 T9 2–5 | Diferencia de datos válida | Baja | Ninguno | Cumple | Mantener |
| DIF-024 | ACHCOL | T9 | Totales/reservado | Totales de 12; reservado desde 56 | Totales de 18; reservado 68–106 | MAN-004 V32 T9 6–8 | Normativa crítica | Crítica | Control de archivo inválido | No cumple | Corregir longitudes y filler |
| DIF-025 | ACHCOL | Nombre | Patrón externo | Nombre tipo 7.3.8.sufijo sin `.OUT` | Mismo patrón con `.OUT` | MAN-004 outbound `RRRRTTT.ZZZ.1` | Pendiente de definición | Crítica | Puede corresponder a interfaces distintas | No demostrado | Obtener contrato CFA y alcance del archivo comparativo |
| DIF-026 | CENIT | Físico | Layout observado | Archivo ACHInterbank auditado identifica semántica CENIT pero usa perfil desplazado | Tercero coincide físicamente con offsets MAN-004 | Ficha CENIT ausente | No demostrada | Crítica | No se puede aprobar por similitud | No demostrado | Obtener especificación CENIT |
| DIF-027 | CENIT | T1 | ReferenceCode/ciclo | Literal interno presente en 87–93 | No contiene literal; ReferenceCode alfanumérico | Ficha CENIT ausente | No demostrada | Crítica | Ubicación de ciclo desconocida | No demostrado | Resolver oficialmente |
| DIF-028 | CENIT | T5/T8 | Lotes | Perfil compartido/daily reset | Tercero usa forma comparable a MAN-004 | Ficha CENIT ausente | No demostrada | Crítica | Reinicio/unicidad desconocidos | No demostrado | Obtener norma, no heredar ACHCOL |
| DIF-029 | CENIT | T6/T7 | Indicador/Trace/asociación | Perfil desplazado y agrupación | Comparativo intercalado | Ficha CENIT ausente | Interoperabilidad / no demostrada | Crítica | Rechazo probable, no prueba normativa | No demostrado | Homologar variantes CENIT |
| DIF-030 | CENIT | T8/T9 | Controles | Algoritmo actual coincide internamente con layout desplazado | Comparativo reconcilia con forma tipo ACH | Ficha CENIT ausente | No demostrada | Crítica | Algoritmo/longitud no aprobables | No demostrado | Obtener regla oficial |
| DIF-031 | CENIT | Nombre | 7.3.8.sufijo | Implementado y observado | Coincide en forma | Manual STA ausente | No demostrada | Crítica | Similitud no demuestra cumplimiento | No demostrado | Incorporar Manual STA/numeral/RuleId |
| DIF-032 | Ambas | Arquitectura | Perfil por cámara | Perfiles separados | N/A | Decisión Opción C | Calidad | Media | Base arquitectónica válida | Cumple | Mantener separación |
| DIF-033 | Ambas | Arquitectura | Lógica fuera de perfil | Switches, aliases, offsets y reglas duplicadas | N/A | Dirección table-driven interna | Calidad | Alta | Divergencia y mantenimiento riesgoso | No cumple | Llevar diferencias al perfil y conservar core de cálculos |
| DIF-034 | Ambas | Seguridad | Trace/auditoría | Valores y registro reconstruible persistidos | No aplica | Minimización/protección de datos | Seguridad o privacidad | Crítica | Exposición potencial de datos | No cumple | Sensibilidad, máscara/hash, retención y acceso |
| DIF-035 | Ambas | Parser | Lectura física | Una sola lectura por línea | Archivos no contienen EOL | MAN-004 para ACHCOL | Operativa crítica | Crítica | Parser puede ignorar bytes posteriores a EOL o depender del header | No cumple | Parser por bytes/perfil y consumo completo |

## Diferencias no clasificadas como defecto

- Cantidad de lotes, transacciones, adendas, bloques y padding: dependen del contenido de cada archivo.
- Fechas, identificadores, nombres, importes, referencias y Trace Numbers: son datos transaccionales diferentes y no se compararon por igualdad.
- El único carácter monobyte no ASCII del archivo del tercero no se clasificó como inválido porque el MAN-004 incluye esa letra en su repertorio; la codificación exacta sigue pendiente.

