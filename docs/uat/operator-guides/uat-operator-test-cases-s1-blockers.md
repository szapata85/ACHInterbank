# Set de pruebas UAT operativas — bloqueantes S1

> Referencia operativa complementaria (rutas SPA y ejecución híbrida 12D/12E): `docs/uat/operator-guides/uat-operator-execution-guide.md` (sección **10. Ejecución UAT con apoyo del SPA**).

## S1-10 Neteo CENIT E2E

### UAT-OP-S1-10-001 Validar neteo por ciclo CENIT
- **Objetivo:** Confirmar que el neteo del ciclo presenta totales consistentes y trazables.
- **Prerrequisitos:** Ciclo CENIT asignado y reporte disponible.
- **Datos requeridos:** Archivo/ciclo/participantes/totales de control autorizados.
- **Pasos para usuario operativo:**
  1. Identifique el ciclo CENIT asignado.
  2. Consulte el reporte de neteo del ciclo.
  3. Revise totales débito, crédito y neto.
  4. Compare contra la fuente o control operativo autorizado.
  5. Registre evidencia.
  6. Marque aprobado si coincide.
- **Resultado esperado:** El neteo del ciclo es consistente y puede relacionarse con ciclo, participantes y posiciones.
- **Evidencia requerida:** Captura o reporte del ciclo + referencia de control + registro de comparación.
- **Criterio de aprobación:** Totales y trazabilidad coinciden.
- **Criterio de rechazo:** Diferencias sin justificación o trazabilidad incompleta.
- **Severidad si falla:** P0/P1 según impacto.
- **Aprobador sugerido:** Operaciones + Tesorería.

### UAT-OP-S1-10-002 Validar posiciones por participante
- **Objetivo:** Verificar que las posiciones por participante sean coherentes.
- **Prerrequisitos:** Reporte por participante disponible.
- **Datos requeridos:** Participante, posición esperada, ciclo y cámara.
- **Pasos para usuario operativo:**
  1. Abra el reporte del ciclo.
  2. Revise posición por participante.
  3. Compare con control autorizado.
  4. Registre diferencias si existen.
- **Resultado esperado:** Posiciones por participante consistentes.
- **Evidencia requerida:** Reporte por participante + captura enmascarada.
- **Criterio de aprobación:** Coincidencia de posiciones y cámara correcta.
- **Criterio de rechazo:** Participante sin posición o posición inconsistente.
- **Severidad si falla:** P1.
- **Aprobador sugerido:** Operaciones + Tesorería.

### UAT-OP-S1-10-003 Validar reproceso sin duplicidad
- **Objetivo:** Confirmar que reproceso no genere duplicidad.
- **Prerrequisitos:** Caso de reproceso autorizado.
- **Datos requeridos:** Mismo ciclo y control previo.
- **Pasos para usuario operativo:**
  1. Revise el resultado del primer proceso.
  2. Revise resultado del reproceso.
  3. Compare totales y estado.
  4. Documente si hubo duplicidad.
- **Resultado esperado:** Sin duplicidad de impacto.
- **Evidencia requerida:** Comparativo antes/después + registro de estado.
- **Criterio de aprobación:** Reproceso consistente sin duplicados.
- **Criterio de rechazo:** Evidencia de duplicidad.
- **Severidad si falla:** P0.
- **Aprobador sugerido:** Operaciones + Tecnología + Tesorería.

## S1-11 Liquidez/CUD

### UAT-OP-S1-11-001 Validar que liquidez simulada no sea tratada como saldo real CUD
- **Objetivo:** Confirmar separación entre liquidez simulada y saldo real CUD.
- **Prerrequisitos:** Reporte de liquidez y soporte CUD disponibles.
- **Datos requeridos:** Resultado de liquidez, soporte CUD autorizado.
- **Pasos para usuario operativo:**
  1. Revise resultado de liquidez.
  2. Revise soporte CUD.
  3. Verifique que se diferencien en el registro del caso.
- **Resultado esperado:** **“Liquidez simulada no equivale a saldo real CUD.”**
- **Evidencia requerida:** Captura comparativa + referencia de soporte CUD.
- **Criterio de aprobación:** Separación explícita y trazable.
- **Criterio de rechazo:** Se usa liquidez simulada como saldo real CUD.
- **Severidad si falla:** P0.
- **Aprobador sugerido:** Tesorería + Riesgo.

### UAT-OP-S1-11-002 Registrar evidencia CUD operacional
- **Objetivo:** Asegurar que evidencia CUD quede registrada.
- **Prerrequisitos:** Soporte CUD autorizado disponible.
- **Datos requeridos:** Soporte CUD, hash o referencia interna.
- **Pasos para usuario operativo:**
  1. Identifique soporte CUD del caso.
  2. Registre hash o referencia.
  3. Guarde ubicación segura.
- **Resultado esperado:** Evidencia CUD registrada con trazabilidad.
- **Evidencia requerida:** Registro de referencia + ubicación segura.
- **Criterio de aprobación:** Evidencia completa y responsable definido.
- **Criterio de rechazo:** Soporte sin referencia o sin custodio.
- **Severidad si falla:** P1.
- **Aprobador sugerido:** Tesorería.

### UAT-OP-S1-11-003 Validar revisión/aprobación de evidencia CUD
- **Objetivo:** Verificar doble revisión/aprobación de evidencia CUD.
- **Prerrequisitos:** Evidencia CUD ya registrada.
- **Datos requeridos:** Registro de revisión y aprobadores.
- **Pasos para usuario operativo:**
  1. Revise que exista primer revisor.
  2. Revise que exista aprobador.
  3. Confirme fechas y decisión.
- **Resultado esperado:** Revisión y aprobación completas.
- **Evidencia requerida:** Acta o traza de aprobación.
- **Criterio de aprobación:** Doble revisión completa.
- **Criterio de rechazo:** Falta uno de los aprobadores.
- **Severidad si falla:** P1.
- **Aprobador sugerido:** Tesorería + Riesgo/Compliance.

### UAT-OP-S1-11-004 Validar conciliación contra soporte CUD
- **Objetivo:** Confirmar conciliación contra soporte CUD.
- **Prerrequisitos:** Resultado operativo y soporte CUD disponibles.
- **Datos requeridos:** Totales y referencias de ambos soportes.
- **Pasos para usuario operativo:**
  1. Compare totales operativos con soporte CUD.
  2. Registre diferencias.
  3. Clasifique aprobación/rechazo.
- **Resultado esperado:** Conciliación consistente o diferencia formalmente documentada.
- **Evidencia requerida:** Comparativo firmado o validado por responsable.
- **Criterio de aprobación:** Conciliación cerrada o diferencia aceptada.
- **Criterio de rechazo:** Diferencia crítica sin plan.
- **Severidad si falla:** P0/P1.
- **Aprobador sugerido:** Tesorería + Operaciones.

## S1-12 Naming externo

### UAT-OP-S1-12-001 Validar nombre archivo ACH Colombia
- **Objetivo:** Confirmar nombre de archivo ACH Colombia.
- **Prerrequisitos:** Archivo de caso disponible.
- **Datos requeridos:** Regla de nombre esperada y archivo generado.
- **Pasos para usuario operativo:** comparar nombre esperado vs nombre obtenido y registrar evidencia.
- **Resultado esperado:** Nombre coincide con regla aplicable.
- **Evidencia requerida:** Captura del nombre + regla de referencia.
- **Criterio de aprobación:** Coincidencia exacta.
- **Criterio de rechazo:** Nombre distinto o incompleto.
- **Severidad si falla:** P0/P1.
- **Aprobador sugerido:** Operaciones ACH + Compliance.

### UAT-OP-S1-12-002 Validar nombre archivo CENIT
- **Objetivo:** Confirmar nombre de archivo CENIT.
- **Prerrequisitos:** Archivo de caso disponible.
- **Datos requeridos:** Regla de nombre esperada y archivo generado.
- **Pasos para usuario operativo:** comparar nombre esperado vs nombre obtenido y registrar evidencia.
- **Resultado esperado:** Nombre coincide con regla aplicable.
- **Evidencia requerida:** Captura del nombre + regla de referencia.
- **Criterio de aprobación:** Coincidencia exacta.
- **Criterio de rechazo:** Nombre distinto o incompleto.
- **Severidad si falla:** P0/P1.
- **Aprobador sugerido:** Operaciones CENIT + Compliance.

### UAT-OP-S1-12-003 Validar naming de devoluciones/ROR
- **Objetivo:** Verificar naming para devoluciones y ROR.
- **Prerrequisitos:** Archivos de devolución/ROR disponibles.
- **Datos requeridos:** Regla de naming y archivos obtenidos.
- **Pasos para usuario operativo:** revisar y comparar cada nombre de archivo.
- **Resultado esperado:** Naming conforme para ambos flujos.
- **Evidencia requerida:** Lista de nombres + resultado de validación.
- **Criterio de aprobación:** Sin diferencias de naming.
- **Criterio de rechazo:** Cualquier nombre fuera de regla.
- **Severidad si falla:** P1.
- **Aprobador sugerido:** Operaciones + Compliance.

### UAT-OP-S1-12-004 Validar que no se use cámara equivocada
- **Objetivo:** Confirmar coherencia entre cámara del caso y nombre de archivo.
- **Prerrequisitos:** Casos de ACH y CENIT en mismo ciclo de revisión.
- **Datos requeridos:** Cámara del caso y archivo asociado.
- **Pasos para usuario operativo:** verificar cámara del caso y validarla contra archivo.
- **Resultado esperado:** Cámara correcta en cada caso.
- **Evidencia requerida:** Tabla caso/cámara/archivo.
- **Criterio de aprobación:** Sin mezcla de cámara.
- **Criterio de rechazo:** Uso de cámara equivocada.
- **Severidad si falla:** P0.
- **Aprobador sugerido:** Operaciones + QA UAT.

## S1-13 Sobre digital / firma / cifrado

### UAT-OP-S1-13-001 Validar archivo saliente firmado/cifrado
- **Objetivo:** Confirmar que archivo saliente esté firmado/cifrado como se espera.
- **Prerrequisitos:** Archivo saliente y validación operativa disponibles.
- **Datos requeridos:** Archivo de salida y evidencia de validación.
- **Pasos para usuario operativo:** revisar estado de validación y registrar evidencia.
- **Resultado esperado:** Validación exitosa de firma/cifrado.
- **Evidencia requerida:** Reporte o constancia de validación.
- **Criterio de aprobación:** Validación aprobada.
- **Criterio de rechazo:** Firma/cifrado no válidos.
- **Severidad si falla:** P0.
- **Aprobador sugerido:** Seguridad + Operaciones.

### UAT-OP-S1-13-002 Validar archivo externo recibido
- **Objetivo:** Confirmar recepción y validación de archivo externo.
- **Prerrequisitos:** Archivo externo disponible.
- **Datos requeridos:** Archivo recibido y resultado de validación.
- **Pasos para usuario operativo:** revisar que el archivo recibido pase validación y quede trazado.
- **Resultado esperado:** Archivo recibido validado o rechazado de forma controlada.
- **Evidencia requerida:** Registro de validación + referencia del archivo.
- **Criterio de aprobación:** Proceso de validación completo.
- **Criterio de rechazo:** Falta de validación o resultado ambiguo.
- **Severidad si falla:** P1.
- **Aprobador sugerido:** Operaciones + Seguridad.

### UAT-OP-S1-13-003 Validar rechazo por firma/certificado inválido
- **Objetivo:** Confirmar rechazo controlado ante firma/certificado inválido.
- **Prerrequisitos:** Caso controlado de invalidez disponible.
- **Datos requeridos:** Resultado de rechazo y evidencia asociada.
- **Pasos para usuario operativo:** ejecutar caso controlado y verificar rechazo.
- **Resultado esperado:** Rechazo controlado y trazable.
- **Evidencia requerida:** Registro de rechazo + motivo.
- **Criterio de aprobación:** Rechazo correcto con trazabilidad.
- **Criterio de rechazo:** Aceptación incorrecta o rechazo sin traza.
- **Severidad si falla:** P0.
- **Aprobador sugerido:** Seguridad + Riesgo/Compliance.

### UAT-OP-S1-13-004 Validar evidencia de certificados
- **Objetivo:** Confirmar que la evidencia de certificados sea suficiente y segura.
- **Prerrequisitos:** Evidencias de certificado disponibles en repositorio seguro.
- **Datos requeridos:** Referencias de certificado y custodio.
- **Pasos para usuario operativo:** validar referencia, responsable y vigencia documental.
- **Resultado esperado:** Evidencia completa sin exponer material sensible.
- **Evidencia requerida:** Hash/referencia + ubicación segura + responsable.
- **Criterio de aprobación:** Evidencia trazable y protegida.
- **Criterio de rechazo:** Falta de referencia o exposición sensible.
- **Severidad si falla:** P1.
- **Aprobador sugerido:** Seguridad.

## S1-20 UAT/runbooks/evidencia

### UAT-OP-S1-20-001 Ejecutar runbook operativo
- **Objetivo:** Verificar que el runbook se ejecute y quede registrado.
- **Prerrequisitos:** Runbook vigente disponible.
- **Datos requeridos:** Caso operativo y responsable.
- **Pasos para usuario operativo:** seguir pasos del runbook y registrar cada hito.
- **Resultado esperado:** Runbook ejecutado sin vacíos críticos.
- **Evidencia requerida:** Bitácora de ejecución + resultados.
- **Criterio de aprobación:** Ejecución completa documentada.
- **Criterio de rechazo:** Pasos críticos omitidos.
- **Severidad si falla:** P1.
- **Aprobador sugerido:** Operaciones + QA UAT.

### UAT-OP-S1-20-002 Validar checklist completo
- **Objetivo:** Confirmar checklist UAT completo.
- **Prerrequisitos:** Checklist diligenciable disponible.
- **Datos requeridos:** Lista de controles por caso.
- **Pasos para usuario operativo:** revisar cada control y marcar estado.
- **Resultado esperado:** Checklist completo con estado por caso.
- **Evidencia requerida:** Checklist con fecha y responsable.
- **Criterio de aprobación:** Sin campos críticos vacíos.
- **Criterio de rechazo:** Información incompleta.
- **Severidad si falla:** P1.
- **Aprobador sugerido:** QA UAT + Operaciones.

### UAT-OP-S1-20-003 Validar acta UAT
- **Objetivo:** Confirmar que el acta UAT esté completa.
- **Prerrequisitos:** Acta en plantilla vigente.
- **Datos requeridos:** Casos, defectos, evidencias y decisión.
- **Pasos para usuario operativo:** revisar completitud del acta y aprobadores.
- **Resultado esperado:** Acta completa y trazable.
- **Evidencia requerida:** Acta final + referencias de evidencia.
- **Criterio de aprobación:** Acta sin vacíos críticos.
- **Criterio de rechazo:** Acta incompleta.
- **Severidad si falla:** P0/P1.
- **Aprobador sugerido:** Comité UAT.

### UAT-OP-S1-20-004 Validar defectos cerrados o aceptados
- **Objetivo:** Confirmar que defectos estén cerrados o aceptados formalmente.
- **Prerrequisitos:** Registro de defectos consolidado.
- **Datos requeridos:** Lista de defectos y decisión.
- **Pasos para usuario operativo:** revisar estado, responsable y fecha objetivo.
- **Resultado esperado:** Defectos con decisión formal y trazable.
- **Evidencia requerida:** Registro de defectos + aprobación de riesgo si aplica.
- **Criterio de aprobación:** Sin defectos críticos abiertos.
- **Criterio de rechazo:** P0 abierto o sin responsable.
- **Severidad si falla:** P0.
- **Aprobador sugerido:** QA UAT + Riesgo/Compliance + Dueño de proceso.
