# Matriz de pruebas posteriores — integración, controles operativos y certificación

**Corte I10 (2026-11-02): ninguna de estas 22 pruebas se ejecutó en esta auditoría.** `Pendiente` significa programar en un ambiente apropiado, no prueba fallida. Las pruebas se proponen para verificar la remediación, no para reemplazar hallazgos demostrados estáticamente.

| ID | Hallazgo | Escenario | Preparación/casos | Resultado esperado | Dependencia | Ejecución |
|---|---|---|---|---|---|---|
| PT-01 | 001 | C1->C2 por archivo tardío | Archivo elegible recibido fuera de ventana C1 | C2 procesa una vez y cola cambia a Consumed solo tras incorporación | Integración DB/Quartz y reloj controlado | PENDIENTE — NO EJECUTADA |
| PT-02 | 001 | C4->C5 archivo fuera de sesión | Diferenciar rechazo ordinario vs cola de devolución elegible | Entrada ordinaria no se compensa en C5; devolución permitida sí; sin bloqueo global de recepción | Operador/simulator archivo CENIT | PENDIENTE — NO EJECUTADA |
| PT-03 | 002 | ROR de devolución día anterior | ROR en C1 y C2 con traza padre auténtica | Correlación padre, idempotencia y plazo; padre incorrecto rechazado | Datos DB + archivo de muestra | PENDIENTE — NO EJECUTADA |
| PT-04 | 003,005,006 | Insuficiencia simulada C1–C3 | Ejecutar Quartz con crédito interno inferior a débito | Sin cambio irreversible ni falsa liquidación; resultado oficial pendiente | DB + Quartz in-memory/integ | PENDIENTE — NO EJECUTADA |
| PT-05 | 003,005,006 | Insuficiencia simulada C4/C5 | Ejecutar ciclo con saldo etiquetado Simulated insuficiente | No ReturnedByOperator ni DXX-LIQ definitivo sin rechazo oficial; detalle consistente | DB + Quartz | PENDIENTE — NO EJECUTADA |
| PT-06 | 005 | Orden PPD/CCD/CTX y devoluciones | 10 prioridades del Anexo 2 §5.1 | Si permanece simulación analítica, orden conforme o funcionalidad inerte en producción | Unitarios de política o eliminación de efectos | PENDIENTE — NO EJECUTADA |
| PT-07 | 004 | R23 día actual C2–C5 | Entrada y devolución mismo día | Aplicar interpretación documental validada; no usar excepción días anteriores automáticamente | Unitarios + consulta normativa si aplica | PENDIENTE — NO EJECUTADA |
| PT-08 | 009 | R23 posterior con notificación | Notificación viernes/previo festivo, caso fuera de 15 días | Plazo calculado con calendario bancario real y fecha recepción verificada | Calendario bancario + datos verificables | PENDIENTE — NO EJECUTADA |
| PT-09 | 007 | R06 C5 | Devolución de origen C1/C2/C3 y origen C4 | Orígenes prohibidos rechazados, C4 evaluado con evidencia R06 completa | Unitarios elegibilidad y contrato | PENDIENTE — NO EJECUTADA |
| PT-10 | 008 | ROR R74 | R74 sobre padre R02, R32 crédito y R32 débito | Solo R32 crédito monetario y campo 3 adenda legítimo permitidos | Raw NACHA y política | PENDIENTE — NO EJECUTADA |
| PT-11 | 010 | Alfabeto NACHA | Archivos CENIT con Ñ/ñ frente a ASCII permitido | CENIT rechaza caracteres fuera de manual 2026; ACH Colombia sin regresión | Parser y perfil E2E | PENDIENTE — NO EJECUTADA |
| PT-12 | 011 | Errores NACHA vs STA | Archivo con relleno errado y STA con D02 | Código y motivo de cada servicio sin mezcla | Unidad/NACHA/STA | PENDIENTE — NO EJECUTADA |
| PT-13 | 012 | ADV contable | ADV clase 280/servicio ADV real por ciclo | Archivo interpretado, importes y referencias conciliables; sin asiento propio CUD | Operador CENIT + ejemplos oficiales | PENDIENTE — NO EJECUTADA |
| PT-14 | 013 | NACK parcial por transacción | Un rechazo para una traza entre varias aceptadas | Solo operación correspondiente cambia; estado de archivo agregación correcta | Gateway/DB y respuestas simuladas | PENDIENTE — NO EJECUTADA |
| PT-15 | 013 | Idempotencia y concurrencia | Reenviar mismo ACK/NACK y dos procesos concurrentes | Un resultado/evento por ítem; sin actualización cruzada | Integración DB concurrente | PENDIENTE — NO EJECUTADA |
| PT-16 | — | C1: salida y calendario | Archivo de primer ciclo y devoluciones excepcionales | Solo operaciones permitidas y archivos salida antes de C2; festivos Bogotá | Homologación CENIT | PENDIENTE — NO EJECUTADA |
| PT-17 | — | C2/C3: devolución ciclo anterior | Devolución siguiente, fecha valor y prenotificación | Tipo origen, plazo, causal y aplicación no monetaria correctos | Homologación CENIT | PENDIENTE — NO EJECUTADA |
| PT-18 | — | C4/C5: cierre y contingencia | Archivo tardío, salida C5 hasta 19:15, error técnico | No confundir salida con liquidación; aplicación y rechazo según operador | Operador CENIT | PENDIENTE — NO EJECUTADA |
| PT-19 | — | Conciliación con Tesorería manual | Resultado/ADV oficial y soporte de cuenta CUD validado manualmente | Diferencias expuestas y firmadas; ACK técnico no equivale a liquidación | Proceso de Tesorería, sin API CUD | PENDIENTE — NO EJECUTADA |
| PT-20 | — | Integridad ACH Colombia | Casos de regresión previos ACHCOL I01–I10 | Sin cambio del comportamiento de cámara ACH Colombia | Suite de regresión .NET/DB | PENDIENTE — NO EJECUTADA |
| PT-21 | — | Continuidad/rollback | Fallo de DB tras recibir archivo o antes de commit | Reproceso sin duplicar aplicación; incidencia recuperable | DB/operación y logs | PENDIENTE — NO EJECUTADA |
| PT-22 | — | Versiones y elegibilidad documental | Circular/anexo vigentes y muestras del canal real | Confirmación formal de documentos antes de homologación | Banco/Operador y normatividad | PENDIENTE — NO EJECUTADA |


## Responsabilidades y evidencias

- **Desarrollo/QA:** pruebas unitarias y de integración de reglas y estados, DB/Quartz en ambiente adecuado; conservar logs, hashes de archivos, identificador de transacción y resultado.
- **Integración CENIT:** muestras certificadas de PPD/CCD/CTX, ROR, ADV, ACK/NACK, códigos de error, salida/fuera de hora y calendario autorizado; aceptación del operador/Banco.
- **Tesorería:** validación de CUD manual y soporte de liquidación contra posiciones CENIT, fuera de ACH Interbank; el software solo vincula constancias/avisos de su competencia.
- **Auditoría:** revalida cada HC-CENIT-001…013 contra diff mínimo y evidencia ejecutada, sin cambiar IDs ni cerrar por planificar.

**Condición de certificación:** los resultados de pruebas y actas oficiales deben añadirse posteriormente; esta matriz **no constituye certificación**.
