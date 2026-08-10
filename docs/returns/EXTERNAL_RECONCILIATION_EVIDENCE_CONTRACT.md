# Contrato de evidencia externa para Cuadre Operativo

## Versión

| Campo | Valor |
| --- | --- |
| Versión de decisión | 1.0 |
| Fecha | 2026-08-10 |
| JOB | `RET.RECONCILIATION.EXTERNAL.EVIDENCE.CONTRACT.1` |
| Alcance | ACH Colombia y CENIT |
| Estado | Versionado; contratos físicos pendientes |

## Alcance

Este documento define qué información externa necesita consumir el Cuadre Operativo por cámara, fecha operacional y ciclo. Separa el contrato conceptual del formato físico y no autoriza parser, ingesta, transporte, contabilidad, SOAP ni homologación.

La evidencia sólo puede marcar un snapshot como `Balanced` cuando procede de un artefacto atribuible a la cámara, conserva identidad y versión verificables y suministra todas las métricas requeridas. Un cálculo CFA, fixture, simulación, ACK/NACK o ausencia de diferencias internas no sustituye esa evidencia.

## Clasificación de fuentes

| Categoría | Fuente encontrada | Uso permitido |
| --- | --- | --- |
| A — Normativa oficial | ACH Colombia V35, secciones 2.4.5, 2.5.4, 2.6 y Anexo 5 | Define la Planilla de Compensación, su alcance por ciclo y sus métricas |
| A — Normativa oficial | Reglamento CENIT DSP-152 y Anexo 2 Manual Operativo | Define compensación, liquidación, ciclos, archivos de salida y funciones de CENIT PO |
| B — Artefacto oficial de operación | Ninguno disponible en el repositorio | No existe ejemplar operativo verificable para implementar ingesta |
| C — Contrato técnico oficial | Ninguno disponible para la planilla ACH Colombia ni para evidencia de posición CENIT | No existe layout físico versionado aplicable al Cuadre Operativo |
| D — Evidencia interna CFA | `AchOperationalReconciliationSnapshot`, `AchOperationalReconciliationExternalEvidence`, read models y runbooks | Modelo interno; no acredita información de la cámara |
| D — Evidencia interna CFA | `CenitNetPosition` y servicios de neteo | Cálculo operacional interno; puede usar liquidez simulada |
| E — Simulación / fixture / UAT | Golden NACHA-M, pruebas, reportes UAT y evidencias sanitizadas | Regresión y preparación UAT; no prueban formato productivo oficial |
| F — Hipótesis / legacy / no verificable | Matrices que proponen evidencia CUD futura o estados objetivo | Identificación de brechas; no define el contrato externo |

## Conceptos

- **Evidencia externa:** artefacto producido o publicado por la cámara que demuestra resultados de procesamiento, compensación o liquidación.
- **Referencia de evidencia:** identificador verificable del artefacto; no debe fabricarse a partir del snapshot interno.
- **Fecha operacional:** fecha de procesamiento o fecha valor aplicable según la cámara.
- **Ciclo:** sesión de operación cerrada a la que pertenecen los resultados.
- **Revisión:** corrección o reemisión identificada por la fuente externa, no una revisión interna inferida.
- **Contrato físico:** layout, formato, canal y reglas de identificación realmente publicados por la cámara.

## ACH Colombia

### Evidencia identificada

La fuente normativa identifica la **Planilla de Compensación Definitiva** generada por ACH Colombia para cada entidad participante y cada ciclo. ACH Colombia la deja disponible en Integra ACH al cambio/cierre de ciclo. La entidad debe confrontarla con sus archivos, logs y registros internos.

La Planilla de Compensación en Línea es información provisional de posición. No reemplaza la Planilla de Compensación Definitiva para cerrar el snapshot del ciclo.

### Fuente normativa

- V35 2.4.5: disponibilidad en Integra ACH y verificación al cambio de ciclo.
- V35 2.5.4: una planilla definitiva por participante y ciclo; posición neta, fecha/hora del ciclo y valores enviados/recibidos.
- V35 2.6.2: conteos y valores enviados/recibidos por archivo y ciclo, posición neta, validación contra planillas definitivas y regla `recibidas = aplicadas + devueltas`.
- V35 Anexo 5: detalle visible por tipo de transacción, cantidades y valores a favor/en contra, totales y valor neto.

### Identidad y granularidad

| Elemento | Resultado |
| --- | --- |
| Productor | ACH Colombia |
| Participante | Una planilla por entidad participante |
| Fecha | Fecha de generación demostrada |
| Ciclo | Ciclo indicado en la planilla definitiva |
| Granularidad | Participante + fecha operacional + ciclo, con detalle agregado por tipo de operación |
| Referencia inmutable | NO DEMOSTRADA |

### Campos demostrados

- Fecha y hora/ciclo de generación.
- Cantidades y valores por categorías de transacción descritas en el Anexo 5.
- Totales a favor y en contra.
- Valor/posición neta del participante.
- Valores de transacciones monetarias enviadas y recibidas.
- Devoluciones de participante dentro de las categorías de compensación.
- Devoluciones por operador fuera de compensación; deben cuadrarse contra archivo original y archivo devuelto.

### Correcciones y versiones

La norma exige reportar diferencias inmediatamente para revisión y ajustes. No demuestra un identificador de corrección, número de revisión, regla de reemisión, reemplazo de una planilla definitiva ni orden entre varias planillas definitivas del mismo ciclo.

Pueden coexistir una planilla en línea provisional y una definitiva. No deben tratarse como revisiones equivalentes. La multiplicidad o reemplazo de planillas definitivas queda bloqueada hasta obtener contrato técnico o muestra operativa.

### Contrato físico

| Aspecto | Estado |
| --- | --- |
| Medio de consulta | DEMOSTRADO: Integra ACH |
| Presentación visual | DEMOSTRADA normativamente en el Anexo 5 |
| Formato descargable | NO DEMOSTRADO |
| Layout máquina-parseable | NO DEMOSTRADO |
| Nombre de archivo/schema/API | NO DEMOSTRADO |
| Firma, hash o referencia única | NO DEMOSTRADO |

El Anexo 5 describe contenido visible, pero no constituye por sí mismo un layout físico para parser.

### Evidencia disponible

Existe respaldo normativo suficiente para identificar la planilla requerida y su semántica general. No existe en el repositorio un artefacto B ni un contrato C que permita validar bytes, celdas, columnas, exportación o revisiones.

### Evidencia faltante

- Ejemplar sanitizado emitido por Integra ACH de una planilla definitiva.
- Manual/layout versionado del formato descargable, si existe.
- Identificador oficial y regla de correlación de participante, fecha y ciclo.
- Semántica de corrección/reemisión y precedencia entre versiones.
- Confirmación de cómo mapear totales a favor/en contra a enviados/recibidos sin inferencias.
- Medio autorizado de obtención y metadatos de integridad.

### Gate de implementación

`EXTERNAL_ARTIFACT_REQUIRED`

No se permite diseñar parser o ingestor hasta obtener al menos un ejemplar sanitizado y su especificación o validación formal de estructura.

## CENIT

### Evidencia identificada

El Banco de la República, como administrador de CENIT, calcula al cierre de cada ciclo la posición multilateral neta, liquida las posiciones contra cuentas de depósito y genera archivos de salida con el detalle de las órdenes para participantes receptores.

Las fuentes locales no nombran ni describen un reporte, planilla, mensaje o archivo que entregue conjuntamente al participante sus totales enviados, recibidos y posición neta por ciclo. Los archivos de salida demuestran transacciones destinadas al receptor; ACK/NACK demuestran aceptación técnica, no el resultado completo de compensación.

### Fuente normativa

- Reglamento DSP-152, definición de Compensación Multilateral Neta y artículos 9, 10 y 17.
- Anexo 2, capítulo 1, numerales 3.1 a 3.3: cinco ciclos, cierre, colocación y conservación de archivos de salida.
- Anexo 2, numeral 10: CENIT PO permite enviar/recibir NACHA-M, descargar ACK/NACK y consultar transacciones procesadas mediante archivos enviados.

### Identidad y granularidad

| Elemento | Resultado |
| --- | --- |
| Productor de compensación/liquidación | Banco de la República / CENIT |
| Fecha | Fecha Valor demostrada para entradas |
| Ciclo | Cinco sesiones de compensación y liquidación demostradas |
| Granularidad de posición | Participante + ciclo, demostrada conceptualmente |
| Artefacto que materializa esa posición | NO DEMOSTRADO |
| Referencia oficial de evidencia | NO DEMOSTRADA |

### Campos demostrados

- El cálculo de posición multilateral neta usa el valor de órdenes enviadas y recibidas.
- La liquidación se realiza por ciclo contra cuentas de depósito.
- Los archivos de salida contienen detalle de órdenes destinadas a participantes receptores.
- Existen archivos NACHA-M de entrada/salida y ACK/NACK accesibles por CENIT PO.

No está demostrado que un único artefacto externo contenga conteos enviados/recibidos, valores enviados/recibidos y posición neta con referencia y revisión correlacionables al snapshot.

### Correcciones y versiones

La normativa contempla encolamiento al ciclo siguiente, rechazo, optimización, ampliación de sesión y rechazo masivo antes del cierre. En rechazo masivo se dejan registros con estado archivado y estado definitivo del procesamiento. No se demuestra una regla de corrección o reemisión de evidencia de conciliación después de cerrar y liquidar el ciclo.

### Contrato físico

| Aspecto | Estado |
| --- | --- |
| NACHA-M y archivos de salida | DEMOSTRADOS conceptualmente |
| ACK/NACK | DEMOSTRADOS conceptualmente; insuficientes para conciliación |
| CENIT PO / CENIT WEB | DEMOSTRADOS como herramientas de acceso |
| Especificación física de salida aplicable | NO DISPONIBLE en el repositorio |
| Reporte de posición neta por ciclo | NO DEMOSTRADO |
| Evidencia CUD correlacionable | NO DEMOSTRADA |
| Referencia, revisión y precedencia | NO DEMOSTRADAS |

### Evidencia disponible

El repositorio conserva normativa de compensación, liquidación, ciclos y archivos de salida, además de fixtures NACHA-M internos. No contiene un artefacto operativo emitido por CENIT ni los manuales de usuario/técnicos que demuestren una evidencia de posición/totales por ciclo.

`CenitNetPosition` no es evidencia externa: lo calcula `CenitNettingService`, admite `SimulatedLiquidity` y su fuente predeterminada es `Simulated`.

### Evidencia faltante

- Manual oficial vigente de CENIT PO/CENIT WEB que identifique resultados de ciclo disponibles al participante.
- Especificación técnica vigente de archivos de salida y sus nombres/metadatos.
- Ejemplar sanitizado de salida de un ciclo y, si existe, reporte de posición/compensación.
- Evidencia o extracto CUD que pueda correlacionarse con fecha, ciclo, participante y posición.
- Regla oficial de corrección/reemisión y referencia única.
- Confirmación de qué combinación de artefactos permite validar enviados, recibidos y posición neta.

### Gate de implementación

`NOT_DETERMINABLE`

No se permite diseñar parser. Primero debe determinarse con documentación o artefactos del Banco de la República cuál es la evidencia oficial de resultado por ciclo; los archivos de salida y ACK/NACK no pueden asumirse suficientes.

## Contrato conceptual común

El dominio necesita, sin imponer formato físico:

| Dato conceptual | Regla |
| --- | --- |
| Cámara | Código parametrizado existente; nunca ID o nombre hardcodeado |
| Participante | Identidad del participante al que se expide la evidencia |
| Fecha operacional | Fecha definida por la cámara |
| Ciclo | Sesión cerrada a la que corresponde la evidencia |
| Tipo de evidencia | Definitiva, provisional, salida, liquidación u otro tipo demostrado |
| Referencia externa | Obligatoria y emitida por la fuente |
| Fecha de emisión/registro | Timestamp externo cuando exista; registro interno por separado |
| Métricas | Sólo las expresamente contenidas en el artefacto |
| Posición neta | Valor y convención de signo demostrados por la cámara |
| Revisión | Identidad y precedencia emitidas por la fuente |
| Procedencia e integridad | Canal, nombre, hash/firma o metadatos disponibles sin inventarlos |

La ausencia de un dato se representa como no disponible. No se completa con cero ni con cálculo interno.

## Matriz contractual

| Concepto | ACH Colombia | Evidencia | CENIT | Evidencia |
| --- | --- | --- | --- | --- |
| Cámara | DEMOSTRADO | DEMOSTRADO — V35 | DEMOSTRADO | DEMOSTRADO — DSP-152 |
| Fecha operacional | DEMOSTRADO | DEMOSTRADO — fecha de planilla | DEMOSTRADO | BLOQUEADO — Fecha Valor conocida, artefacto de cierre ausente |
| Ciclo | DEMOSTRADO | DEMOSTRADO — planilla definitiva por ciclo | DEMOSTRADO | BLOQUEADO — cinco sesiones, artefacto de resultado ausente |
| Referencia oficial | NO DEMOSTRADO | NO DEMOSTRADO — sin ID inmutable | NO DEMOSTRADO | NO DEMOSTRADO — sin artefacto identificado |
| Conteo enviado | DEMOSTRADO | BLOQUEADO — V35 2.6.2/Anexo 5 sin mapping físico | BLOQUEADO | BLOQUEADO — sin reporte/layout de ciclo |
| Valor enviado | DEMOSTRADO | BLOQUEADO — V35 2.5.4/2.6.2 sin mapping físico | BLOQUEADO | BLOQUEADO — sin reporte/layout de ciclo |
| Conteo recibido | DEMOSTRADO | BLOQUEADO — V35 2.6.2/Anexo 5 sin mapping físico | BLOQUEADO | BLOQUEADO — salida conceptual sin layout |
| Valor recibido | DEMOSTRADO | BLOQUEADO — V35 2.5.4/2.6.2 sin mapping físico | BLOQUEADO | BLOQUEADO — salida conceptual sin layout |
| Devoluciones | DEMOSTRADO | DEMOSTRADO — V35 2.6.2; operador fuera de compensación | DEMOSTRADO | BLOQUEADO — artefacto físico ausente |
| Posición neta | DEMOSTRADO | DEMOSTRADO — planilla definitiva | DEMOSTRADO | NO DEMOSTRADO — CENIT calcula/liquida, artefacto no identificado |
| Fecha emisión | DEMOSTRADO | DEMOSTRADO — fecha/hora de generación | NO DEMOSTRADO | NO DEMOSTRADO — sin evidencia identificada |
| Revisión/corrección | BLOQUEADO | BLOQUEADO — ajuste previsto, versionado no definido | BLOQUEADO | BLOQUEADO — sin regla posterior al cierre |
| Formato físico | BLOQUEADO | BLOQUEADO — sin layout/export oficial | BLOQUEADO | BLOQUEADO — sin layout de evidencia de ciclo |
| Medio de obtención | DEMOSTRADO | BLOQUEADO — Integra ACH sin mecanismo técnico | DEMOSTRADO | BLOQUEADO — CENIT PO/WEB sin artefacto exacto |

## Diferencias por cámara

- ACH Colombia define una Planilla de Compensación Definitiva y detalla normativamente su contenido agregado.
- CENIT define el proceso de compensación/liquidación y archivos transaccionales, pero no demuestra en las fuentes locales un artefacto resumen equivalente a la planilla ACH Colombia.
- La planilla ACH Colombia contiene categorías a favor/en contra; no se debe mapear automáticamente a campos enviados/recibidos sin contrato físico.
- En CENIT, una salida NACHA-M, un ACK/NACK, una posición interna y una evidencia CUD representan hechos distintos.

## Compatibilidad con `AchOperationalReconciliationExternalEvidence`

**Compatible parcialmente.** Los campos `EvidenceReference`, conteos, valores, `NetPosition` y `RecordedAt` expresan el mínimo conceptual esperado. Sin embargo:

- `IsComplete` presupone que un único artefacto aporta todos los conteos, valores y posición; esto no está demostrado para CENIT.
- No representa tipo/provisionalidad, participante, versión externa, procedencia, integridad ni convención de signo.
- En ACH Colombia no está demostrado el mapping físico entre categorías a favor/en contra y `Sent*`/`Received*`.
- `RecordedAt` no distingue emisión externa de registro interno.

No se modifica el modelo en este JOB. Los cambios de persistencia corresponden a un trabajo posterior, después de adquirir contratos físicos.

## Datos no demostrados

- Identificador inmutable de evidencia por cámara.
- Contrato de reemisión/corrección y precedencia.
- Formato físico consumible de la planilla ACH Colombia.
- Artefacto CENIT que contenga o correlacione posición, enviados y recibidos por ciclo.
- Evidencia CUD oficial correlacionable.
- Firma/hash/metadatos de integridad disponibles en cada canal.

## Riesgos

- Conciliar con evidencia provisional o incompleta.
- Confundir ACK/NACK con compensación o liquidación.
- Confundir `CenitNetPosition` con posición emitida por CENIT.
- Interpretar categorías a favor/en contra como dirección sin respaldo técnico.
- Sobrescribir una evidencia definitiva por una supuesta corrección sin precedencia oficial.

## Gates

| Cámara | Gate | Bloqueo |
| --- | --- | --- |
| ACH Colombia | `EXTERNAL_ARTIFACT_REQUIRED` | Falta ejemplar operativo sanitizado y contrato físico/versionado de la planilla definitiva |
| CENIT | `NOT_DETERMINABLE` | Falta identificar y documentar el artefacto oficial que materializa resultados y posición por ciclo |

## Próximo JOB permitido

`RET.RECONCILIATION.EXTERNAL.EVIDENCE.ACQUISITION.ACHCOL.1`

Debe obtener, clasificar y versionar un ejemplar sanitizado de Planilla de Compensación Definitiva de Integra ACH y su especificación o validación formal de estructura. No debe implementar parser.
