> **Actualización I07-R1 (2026-11-02):** sin pruebas UAT ejecutadas. Se incorporan regresiones de previsualización y descarga/hash; **todos los PRE-01..PRE-14 permanecen pendientes de homologación**. Ver `INFORME_I07R1.md`.

# Matriz de aceptación — prenotificaciones salientes

**Corte:** I07 (2026-11-02). **0/14 pruebas PRE ejecutadas/homologadas** en runtime: `Pendiente` significa que falta evidencia funcional, aun donde ya existen código y pruebas fuente. I07 agregó casos no ejecutados para ciclos, cámara y UI obsoleta. Detalles del contrato y excepciones: `PLAN_IMPLEMENTACION_2026-11-02.md`.

| ID | Escenario | Cámara / cobertura | Incremento principal | Estado | Evidencia de cierre requerida |
|---|---|---|---|---|---|
| PRE-01 | Listar prenotificación originada y pendiente | ACHCOL, CENIT | I02, I06 | Pendiente | API + SPA, traza y deadline |
| PRE-02 | No enviada/finalizada | Ambas | I02 | Pendiente | API 4xx, sin artefacto |
| PRE-03 | Devolución de prenotificación | ACHCOL | I03 | Pendiente | Fixture NACHA-M validado, importe 0, trace |
| PRE-04 | Devolución de prenotificación | CENIT | I03 | Pendiente | Fixture NACHA-M validado y ciclo permitido |
| PRE-05 | Descarga y carga manual | Ambas | I04, I06 | Pendiente | Hash y correlación NachaUpload/estado |
| PRE-06 | Reupload idéntico | Ambas | I04 | Pendiente | Ingesta idempotente, sin doble respuesta |
| PRE-07 | Sin devolución antes del deadline | Ambas | I05 | Pendiente | Job sin transición temprana |
| PRE-08 | Sin devolución después del deadline | Ambas | I05 | Pendiente | Job resuelve estado conforme a regla |
| PRE-09 | Devolución concurrente antes de aceptar tácitamente | Ambas | I04, I05 | Pendiente | Estado consistente e idempotencia |
| PRE-10 | Cámara/banco/ciclo incompatibles | Ambas | I02 | Pendiente | Error controlado en API |
| PRE-11 | JWT/roles faltantes | Ambas | I06 | Pendiente | HTTP 401/403 y accesos |
| PRE-12 | Generación concurrente | Ambas | I03 | Pendiente | Secuencia y evidencia sin colisiones |
| PRE-13 | Cambio cámara/escenario SPA | Ambas | I06 | Pendiente | Limpieza de selección y E2E |
| PRE-14 | Feature flag apagado | Ambas | I01, I07 | Pendiente | 404/denegación controlada, sin efectos |

**Observación:** conservar la diferencia entre prueba estática del código y ejecución real contra backend y base de datos. No usar fixtures históricos como prueba de homologación actual sin reproducirlos.

## Estado I01 (contrato, no certificación PRE)

- Se agregaron pruebas C# `PrenotificationSimulatorContractI01Tests` e `I01_NewPrenotificationReturnMustFailClosedWithoutCreatingFileOrState` **no ejecutadas por ausencia de dotnet**.
- Se superaron 20 comprobaciones estáticas contractuales; **ningún PRE-01..PRE-14 se considera cerrado por ello**.
- PRE-14 / control de exposición: escenario 10 es explícitamente fail-closed en preview/generate; pendiente test runtime y configuración por cámara.
