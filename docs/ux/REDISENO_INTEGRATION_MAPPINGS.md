# Rediseno UX/UI - Mapeos de Integracion

Fecha: 2026-05-21  
Ruta SPA: `/integraciones/mappings`  
Productivo: NO-GO

## Diagnostico

El campo `Integracion` se alimenta de `GET api/integrations/methods`. El catalogo backend solo sembraba metodos `WSCFAACH.*`, por lo que `WsAxonRespuestaTransaccionesSoapClient` no podia aparecer en el dropdown. La pantalla tambien mezclaba creacion de borradores y grilla principal en una vista densa, con poca claridad operativa cuando una integracion no tenia mappings.

## Ajuste aplicado

- Se agrego al catalogo activo el metodo `WSAXON.RegistrarRespuestaTransaccion` con `soapClientCode = WsAxonRespuestaTransaccionesSoapClient`.
- El dropdown muestra integraciones activas aunque no tengan mappings.
- Si la integracion seleccionada no tiene mappings, la pantalla muestra un estado vacio claro.
- La vista principal usa filtros, resumen y cards compactas.
- El formulario de nuevo borrador se movio a modal.
- `Detalle` abre modal read-only.
- `Editar` abre un modal de confirmacion y conserva el flujo editor funcional existente.
- Se agregaron atributos `data-testid` y `data-ux-critical` para validacion DOM con Playwright.

## Restricciones conservadas

- No se eliminaron mappings existentes.
- No se modificaron contratos SOAP.
- No se cambio la semantica de edicion/publicacion.
- No se exponen secretos, credenciales ni certificados privados.
- Productivo permanece **NO-GO**.

## Evidencia esperada

- Screenshot: `docs/ux/evidencias/integration-mappings-after.png`.
- Reporte DOM: `docs/ux/evidencias/ux-validation-integrations.json`.
