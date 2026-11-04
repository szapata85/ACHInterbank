# Plan priorizado de correcciones mínimas — posterior a I10

**No se implementa código en esta auditoría.** Se agrupan 13 hallazgos en **seis paquetes pequeños**, no se prescribe nueva arquitectura, integración CUD ni motor de liquidación. El orden final de despliegue depende del flujo activo y la ventana de operación.

| Orden | Paquete mínimo | IDs | Cambio requerido | Criterio de aceptación estático y dinámico |
|---|---|---|---|---|
| 1 (P1) | Separar simulación de estado real | 003,005,006 | En la ruta Quartz, impedir que neteo/optimización simulados produzcan ReturnedByOperator, inclusión oficial o liquidación; si se conserva simulador, dejarlo claramente informativo. | C4/C5 insuficiente simulado nunca cambia estado final; solo resultado oficial de CENIT puede hacerlo; detalle interno coherente. |
| 2 (P1) | Respuesta por ítem y avisos | 012,013 | Aplicar FileNack individual a transacción con estado/evento y deduplicación; leer ADV 280/ADV a evidencia de conciliación. | Archivo con un NACK individual mantiene otros registros no rechazados; recepción repetida no duplica; ADV correlacionado sin asiento CUD. |
| 3 (P1) | Incorporación real de cola | 001 | Mover la transacción a conjunto/ejecución de ciclo destino de forma transaccional e idempotente, antes de Consumed. | Archivo tardío C1→C2 y C4→C5 solo encola operación admisible; sin pérdida ni doble procesamiento. |
| 4 (P1) | Elegibilidad y plazo de devoluciones | 002,004,007,008,009 | Preservar vínculo ROR padre, restringir R23/R06 según sesión y notificación, acotar R74, calcular días hábiles reales. | Casos positivos y negativos para C1/C2/C5, R23 día anterior, R06 origen C4, R74 sobre R32, expiración y duplicado. |
| 5 (P1/P2) | Perfil NACHA por cámara | 010,011 | Separar alfabeto y espacio de errores NACHA-CENIT de STA Dxx sin romper parser ACH Colombia. | Ñ/ñ no permitido por CENIT 2026; causal D02 STA no significa relleno; validación ACH Colombia sin regresiones. |
| 6 (control) | Conciliación y documentación operacional | Observación sin HC nuevo | Al conectar reconciliación con ruta CENIT, condicionar invariante ACH Colombia v35; pruebas y evidencias de Tesorería manual. | No atribuir validación CUD a ACH Interbank; registrar fuente/fecha/aprobador externo y conservar evidencias sin modificar libro bancario. |


**Criterios de no alcance:** (a) no API/consulta/transferencia CUD; (b) no implementar liquidación, algoritmo oficial de optimización ni asientos contables; (c) no refactorizar arquitectura; (d) no alterar reglas ACH Colombia por defecto. Las excepciones documentales DN-CEN-007/008 requieren criterio de operador cuando el cambio pueda rechazar operaciones.

**Secuencia posterior:** corrección localizada → pruebas unitarias del cambio → integración con DB/Quartz de prueba → archivos de certificación CENIT y conciliación manual de Tesorería → decisión de habilitación del flujo afectado. No marcar ningún paso ejecutado a la fecha del dictamen.
