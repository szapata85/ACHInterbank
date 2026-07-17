# GO/NO-GO — respuestas SOAP

## Decisión

**NO-GO para liberación productiva.**

El catálogo, seed, resolutor, persistencia, API y SPA quedan técnicamente implementados y con suites offline en verde. R96 está correctamente separado por método y categoría. Sin embargo, los criterios de aceptación exigen Playwright LIVE aprobado y evidencia inequívoca del log WCF; ambos quedaron pendientes sin autorización para repetir el débito ya intentado.

## Condiciones cerradas

- Catálogo único reutilizado y discriminado por fuente/método/código.
- Seed idempotente en SQL Server y PostgreSQL.
- R96 débito/crédito resuelto table-driven.
- Estados técnico y funcional separados.
- Código desconocido fail-closed.
- `CatalogId`, código y descripción persistidos con historial.
- API y Angular no exponen payload.
- Endpoint UAT bloqueado en Production y protegido por permiso/opt-in.
- Un único intento LIVE persistido; sin doble movimiento.

## Bloqueantes restantes

1. Ejecutar en una nueva autorización una prueba Playwright desde cero que complete la verificación visual sin repetir la transacción ya procesada.
2. Obtener evidencia nueva y correlacionable del log WCF local.
3. Cerrar el riesgo heredado de almacenar payload SOAP completo mediante el mecanismo corporativo de cifrado/retención o sustituirlo por evidencia mínima segura.
4. Aprobación humana de seguridad, operación y negocio.

`Proc_Transacciones` sólo fue validado offline. No se ejecutó LIVE. CENIT y productivo externo permanecen bloqueados.

