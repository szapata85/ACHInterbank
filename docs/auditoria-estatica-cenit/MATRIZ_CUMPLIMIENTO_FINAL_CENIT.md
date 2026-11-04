# Matriz consolidada — cumplimiento estático CENIT

## Recuento trazable de las evaluaciones

No se usa porcentaje de cumplimiento: las filas reiteran algunas reglas en varios ciclos o incrementos.

| Incremento | Alcance | Filas evaluadas | Cumple delimitado | Parcial | Incumple estáticamente | No demostrable | Hallazgos nuevos |
|---|---|---|---|---|---|---|---|
| I01 | Inventario, fuentes, arquitectura | 12 | 0 | 8 | 0 | 4 | 0 |
| I02 | Ciclo 1 | 24 | 2 | 14 | 3 | 5 | 3 |
| I03 | Ciclo 2 | 28 | 3 | 18 | 3 | 4 | 1 |
| I04 | Ciclo 3 | 30 | 5 | 18 | 4 | 3 | 1 |
| I05 | Ciclo 4 | 34 | 7 | 18 | 5 | 4 | 1 |
| I06 | Ciclo 5 | 36 | 10 | 17 | 6 | 3 | 1 |
| I07 | Devoluciones/ROR/STA | 64 | 7 | 48 | 8 | 1 | 2 |
| I08 | NACHA-M/ADV/archivos | 55 | 16 | 29 | 7 | 3 | 3 |
| I09 | Compensación, frontera CUD, conciliación | 50 | 16 | 23 | 8 | 3 | 1 |
| I10 | Cierre transversal y dictamen | 20 | 3 | 3 | 13 | 1 | 0 |


**I01–I09:** 333 filas, **66** cumplen delimitadamente, **193** parciales, **44** incumplidas, **30** no demostrables; **13** causas raíz. **I10:** 20 comprobaciones de cierre, 3/3/13/1 respectivamente. **Total de anotaciones 353**, con solapamientos; **13 hallazgos acumulados, todos abiertos**. No equivalen a 353 requisitos independientes.

## Matriz funcional de cierre

| Dominio | Ciclos/ámbito | Evaluación final | Sustento y limitación | Hallazgos relacionados |
|---|---|---|---|---|
| Horario y elegibilidad | C1–C5 | Parcial | Aperturas y restricciones por ciclo presentes; cola y excepciones R23/R06 dejan rutas incumplidas. | 001,004,007 |
| Entradas ordinarias y prenotificaciones | C1–C4; salidas C5 | Parcial | Políticas específicas por cámara, pero no todos los escenarios de aplicación/retorno están acreditados. | 001 |
| Devolución ordinaria y R23 | C1–C5 | Incumplimiento delimitado | R23 mismo día y plazo externo autodeclarado; validar recepción/notificación real. | 004,009 |
| ROR R60–R74 | C1/C2/C5 y ventanas | Incumplimiento delimitado | Existen reglas de unicidad/ventana, pero falla correlación C1 y autorización R74. | 002,008 |
| Quinta sesión | C5 | Incumplimiento delimitado | Se prohíben entradas ordinarias; la ruta R06 permite una excepción no restringida por origen. | 007 |
| NACHA-M físico y semántico | C1–C5 | Incumplimiento delimitado | 106 posiciones, lotes/bloques presentes; caracteres Ñ/ñ y errores Dxx con significado erróneo. | 010,011 |
| STA frente a compensación | Servicios diferentes | Incumplimiento delimitado | Catálogo STA D01–D06 existe y se reutiliza como validación NACHA. | 011 |
| Fuera de horario/cola | C1–C5 | Incumplimiento delimitado | Cola temporal sin incorporación efectiva al conjunto de la sesión destino. | 001 |
| Compensación, optimización y CUD | C1–C5 | Incumplimiento delimitado de código interno | Neteo/liquidez simulado ejecutado por job con efectos operativos. CUD bancario sigue fuera de alcance. | 003,005,006 |
| Avisos ADV, ACK/NACK y conciliación | C1–C5 | Incumplimiento delimitado | Sin ruta ADV específica observada; NACK por ítem se aplica al estado del archivo. | 012,013 |
| Pruebas de integración/certificación | Ambiente externo | No demostrable | Sin operador CENIT real, ciclo bancario, validación de archivos, DB/Quartz/colaboración Tesorería. | — |


## Excepción de alcance CUD (decisión formal)

La **inexistencia de API CUD, conexión de saldos o motor propio de liquidación NO es un incumplimiento**. DSP-152, artículos 18 y 19, y Anexo 2 §5.2 regulan la liquidación/optimización del sistema del Banco de la República. La entidad verifica CUD manualmente por Tesorería. ACH Interbank debe procesar **respuestas y resultados oficiales disponibles** sin sustituirlos con simulación. Una eventual aprobación manual por sesión es control institucional externo, no requisito automático de software inferido.

## Límites de inferencia

El inventario y las rutas documentadas son evidencia estática. **No se declara cumplimiento global ni listo para homologación**. Revisar `FUENTES_NORMATIVAS_Y_DIFERENCIAS.md` para DN-CEN-001…008, `HALLAZGOS_CONSOLIDADOS_CENIT.md` para 13 causas y `MATRIZ_PRUEBAS_POSTERIORES_CENIT.md` para verificación operativa futura.
