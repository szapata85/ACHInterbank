# Rediseno UX/UI - Configuracion SOAP

Fecha: 2026-05-20  
Ruta SPA: `/integraciones/soap-settings`  
Productivo: NO-GO

## Diagnostico

La pantalla tenia una composicion densa con campos tecnicos, mapeos y acciones dentro del mismo flujo visual. En resoluciones medias, las filas de mapeo y botones podian quedar comprimidas o montadas. La operacion no distinguia claramente configuracion, seguridad, acciones y estado.

## Ajuste aplicado

Se reorganizo la pantalla sin cambiar endpoints ni semantica funcional:

- Encabezado con contexto UAT/local y advertencia de secretos.
- Seccion `Servicio` para endpoint, SOAP Action, estado y mapeos.
- Seccion `Seguridad y certificados` para aclarar que no se exponen secretos completos ni certificados privados.
- Seccion `Operacion` con acciones separadas:
  - Guardar cambios.
  - Probar conexion.
  - Recargar configuracion.
  - Cancelar.
- Seccion `Estado / ultima prueba` con resultado sanitizado.
- Seccion `Ayuda operativa` con aclaracion de DryRun/UAT-local.

## Restricciones conservadas

- No se cambiaron endpoints SOAP.
- No se cambio la semantica de guardado.
- No se invoca SOAP productivo desde la prueba de la pantalla.
- No se muestran secretos completos.
- No se cambia modo a Live por defecto.

## Validacion

- `npm run build`: OK.
- `npm test -- --watch=false --browsers=ChromeHeadless`: OK.

La pantalla queda preparada para validacion visual/runtime en SPA Docker.
