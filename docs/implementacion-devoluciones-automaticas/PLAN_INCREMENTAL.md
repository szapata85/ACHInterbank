# Plan incremental — DEV-AUTO-2026-11-02

## Meta

Generación controlada de devoluciones por errores de **aplicación de una transacción recibida**, parametrizable por cámara (ACH Colombia y CENIT), reutilizando el mecanismo de devolución NACHA-M existente. Mantener manuales, recepción de devoluciones, prenotificaciones y simulador UAT sin regresiones.

La expansión será **gradual y acumulativa en cobertura**, NO exponencial en cantidad de código o componentes. Cada fase hace una cosa y entrega proyecto completo.

## Principios de implementación

- Base única: último ZIP íntegro; sin reconstrucciones desde conversaciones ni árboles parciales.
- Antes de modificar: fuente actual > test existente > memoria histórica para describir comportamiento; norma aplicable gobierna requisitos.
- Cambios mínimos por vertical: un contrato y un flujo reutilizables, no dos motores por cámara.
- **DESACTIVADO por defecto** en ambas cámaras; lectura del parámetro en backend, no solo ocultar opciones en SPA.
- Reutilizar `AchReturnsService`, `IAchReturnEligibilityService`, catálogos, políticas por cámara, ciclo configurado, transición y registro de auditoría existentes. No volver a crear generador NACHA-M.
- Rechazo/archivo no procesable, devolución de participante, reclamación posterior, reversión y devolución de devolución son hechos distintos. No convertir todos los errores en devoluciones.
- Una sola devolución por transacción cuando la norma lo exija; deduplicación robusta, sin reintentos que dupliquen archivos o débitos.
- No tocar SPA antes de tener contrato backend; si se toca, mantener Angular Material y patrones UI existentes, todo en español natural.
- CENIT puede continuar bloqueado para ciertos flujos salientes hasta que sus controles/soportes de homologación sean suficientes; **el interruptor no elimina una prohibición preexistente**.
- Ausencia de SDK, conexión a banco, SQL o navegador no detiene cambios seguros: clasificar `NO_EJECUTADA_POR_AMBIENTE`, adjuntar comandos y continuar dentro del alcance técnico seguro. Nunca simular un PASS.

## Secuencia ordenada

| Incremento | Alcance único y verificable | Evidencia mínima | Condición para pasar |
|---|---|---|---|
| **I00** — Base | Inventario real, plan, memoria propia, entrega acumulativa | Informe, manifiesto, ZIP íntegro | Documentación íntegra y fuentes preservadas |
| **I01** — Parámetro | Persistir modo por cámara: DESACTIVADO / CON_APROBACION / AUTOMATICO; lectura backend, permisos y auditoría de cambios; valor inicial DESACTIVADO | Pruebas de valores predeterminados, permiso, persistencia y cambio de modo | Sin tocar procesamiento ni UI |
| **I02** — Detección | Identificar un fallo real de aplicación después de parseo válido y asignar causal candidata sin generar archivo; evitar confundir errores fatales/operador con devolución receptora | Pruebas por crédito, débito, prenotificación, causal no aplicable y no duplicación | Hallazgo observable; ninguna mutación financiera nueva |
| **I03** — Generación reutilizada | Conectar incidentes elegibles al servicio existente de devoluciones, con correlación, plazo/ciclo, seguridad e idempotencia; modo CON_APROBACION solo prepara pendiente | Tests de integración de servicio y reentradas, preservación flujo manual | No envío automático; ACHCOL/CENIT con políticas individuales |
| **I04** — Aprobación operativa | Exponer consulta/aprobación de pendientes mediante la pantalla **Devoluciones** ya existente, con controles backend y Angular Material sencillo | Tests API de autorización + prueba componente/formulario | No crear módulo duplicado ni flujo alternativo |
| **I05** — Automático ACH Colombia | Activar generación automática solo para ACH Colombia y solo operaciones homologadas en código; cortar en su límite de plazo, respetar causales y calendario | Pruebas de habilitado/deshabilitado, cuatro ciclos, crédito tardío/débito tardío, duplicado, incidentes | El archivo generado no se anuncia como transmitido sin acuse |
| **I06** — Automático CENIT | Reutilizar el motor con validación separada: ciclo siguiente, fecha valor, prenotificación y excepciones R23/R06; preservar barreras preexistentes de ReturnOut | Casos cruzados CENIT/ACHCOL, prohibiciones y casos no homologados | Si CENIT físico no está probado, mantener su modo operativo bloqueado y documentar NO-GO |
| **I07** — Salida y conciliación | Integrar, solo cuando exista canal realmente autorizado/probado, transmisión, acuses, estado, recuperación de fallo y conciliación; nunca equiparar archivo generado con pagado | Tests de idempotencia/reintento, acuse aceptado/rechazado, evidencia externa o bloqueo explícito | Sin canal validado: código/estado pendiente y producción NO-GO |
| **I08** — Regresión y entrega | Recorrido funcional por ambas cámaras, reversión de configuración, revisión de permisos, integridad ZIP, guía de operación y UAT | Informe consolidado, casos reproducibles, hallazgos sin ocultar | Cierre técnico condicionado a la evidencia real; no se hereda GO de un ZIP anterior |

**Regla de separación:** los incrementos son candidatos a trabajo uno por turno, no un compromiso de que todos se ejecutaron en esta entrega. Las fases pendientes se registran como `NO_INICIADA`.

## Contrato de entrega obligatorio (cada incremento)

1. **ZIP completo** con todo `ACHInterbank_ajustado/`, no parche parcial; mismo árbol más diff de fase.
2. `MEMORY_STATE.md` actualizado (sección `Decisiones`, `Verificado`, `Pendiente`, `Próximo incremento`).
3. `ESTADO_INCREMENTAL.md` con fases completadas, bloqueadas, en progreso y próximo punto de parada.
4. `INFORME_Ixx.md` breve: objetivo, archivos modificados, cambios reales, riesgos y regresiones.
5. `EVIDENCIA_Ixx.md` con comandos, resultado literal resumido y pruebas NO EJECUTADAS justificadas.
6. `MANIFIESTO_Ixx.json` con ZIP de entrada, SHA-256, lista de cambios y SHA del ZIP de salida comunicado externamente.
7. En respuesta al usuario: resultado y enlace del ZIP, pruebas ejecutadas/no ejecutadas, qué queda, dónde se detiene.

## Criterios de parada obligatoria

- **Parada de seguridad funcional**: ausencia de traza/importe/ciclo/causal confiable, posibilidad de duplicación monetaria, salto de validación regulatoria o transmisión no autorizada. No activar esa ruta; continuar solo con cambios aislados seguros/documentales.
- **Parada de fase**: al cumplir el alcance definido y sus verificaciones estáticas se entrega ZIP y no se mezcla la fase siguiente sin autorización.
- **Parada de homologación**: si no existen runtime/fixtures/canal, no afirmar certificación; registrar limitación y continuar otras correcciones seguras del incremento.
- **Parada productiva**: permisos, transportes, conciliación y UAT incompletos mantienen producción NO-GO aunque el código esté escrito.

## Regla de avance

No convertir I00 en I01 dentro de una misma entrega. Cada nuevo turno debe iniciar con: base ZIP, memoria, estado, próximo incremento, línea base y únicamente después código.
