# Matriz operacional y casos de aceptación

**Fuentes adjuntas del usuario:** ACH Colombia V35 (abril 2026); *Nuevos Ciclos de Proceso y Horarios* (documento independiente); DSP-152 Anexo 2 CENIT (febrero 2025); Anexo A (causales); Manual NACHA-M CENIT (mayo 2026). Confrontar el texto específico antes de implementar cada escenario.

| ID | Caso | ACH Colombia | CENIT | Expectativa para el motor |
|---|---|---|---|---|
| DA-01 | Cuenta cerrada en crédito recibido | Causal compatible según catálogo V35 | R02 si aplica | Detectar fallo de aplicación, correlacionar original, bloquear si no hay causal confiable |
| DA-02 | Fondos insuficientes en débito | Validar causal y ventana propia | R01 para débito monetario | Devolución monetaria conforme a norma; no contabilizar doble |
| DA-03 | Prenotificación inválida | Control por cámara y plazo | Devolución misma fecha valor, máximo último ciclo permitido | Monto 0, sin movimiento de fondos |
| DA-04 | Crédito tardío | Máximo ordinario cuatro ciclos; régimen de calidad para tardías | Regla normal ciclo siguiente y misma fecha valor | No compartir reloj/plazos entre cámaras |
| DA-05 | Débito tardío | Rechazar flujo ordinario fuera de plazo | Regla normal ciclo siguiente y misma fecha valor | Bloquear u orientar gestión según política, sin forzar retorno extemporáneo |
| DA-06 | R23 por solicitud posterior del beneficiario | Procedimiento expreso con solicitud | Excepción hasta 15 días calendario y trámite en siguiente día hábil bancario | **No crear automáticamente por simple fallo de aplicación**; necesita instrucción/soporte |
| DA-07 | R06 por solicitud del originador | Aplicar reglas de cámara correspondiente | R06 con disponibilidad posterior al abono, importe original, confirmación | No simular una devolución unilateral sin autorización/condiciones |
| DA-08 | Devolución de devolución | Separar según operador | Causales R60-R74 del flujo aplicable | Nunca mezclar con ReturnOut ordinario |
| DA-09 | Error de formato o rechazo por operador | No es devolución receptora | No es devolución receptora | No generar nueva devolución de participante |
| DA-10 | Mismo original recibido dos veces / concurrencia | Control estricto | Control estricto | Una sola devolución válida y auditoría, sin doble archivo |
| DA-11 | Modo desactivado | Mantener manual/ReturnIn | Mantener manual/ReturnIn | Ninguna nueva generación automática |
| DA-12 | Modo con aprobación | Sí, si elegible | Solo si flujo homologado | Pendiente de revisión, sin autoenvío |
| DA-13 | Modo automático sin transporte homologado | No transmitir | No transmitir | Archivo preparado/generado según alcance validado, marcado pendiente; sin declarar compensación |
| DA-14 | Cambio de modo con evento pendiente | No reinterpretar históricamente | Igual | Comportamiento determinista y auditable |
| DA-15 | CENIT sin formato/naming aprobado | No aplica | Retener bloqueo | No habilitar modo operativo por parámetro |

**Precaución documental:** el Manual ACH V35 visible en raíz describe cinco ciclos, mientras el PDF adicional de nuevos horarios describe siete; resolver vigencia/aplicabilidad con configuración y evidencia, nunca codificar cinco/siete de forma rígida. La documentación histórica de CENIT ReturnOut registra NO-GO: revisar contra el código vigente y evidencia de homologación antes de levantarlo.

**Criterios comunes por caso:** prueba positiva y negativa, causal/número de secuencia exactos, original inmutable salvo transición autorizada, bitácora, idempotencia, resultado trazable y ruta de remediación si falla.
