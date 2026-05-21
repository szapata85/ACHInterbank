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

## Rediseño profundo controlado - 2026-05-20

Motivo: el primer rediseño seguía mostrando demasiada información técnica en una sola vista. En resoluciones operativas la combinación de métodos, endpoints, SOAP Action, mapeos y acciones comprimía columnas y generaba riesgo de controles montados.

Patrón aplicado:

- Pantalla principal de resumen, sin formulario gigante inicial.
- Lista compacta desktop y cards compactas responsive para servicios SOAP.
- Endpoint en lista truncado con ellipsis; valor completo solo en detalle.
- Modal/drawer de detalle read-only.
- Modal/drawer de edición con secciones Servicio, Endpoint, Mapeo de parámetros y Seguridad.
- Modal de prueba operativa con resultado sanitizado.
- Ayuda operativa movida a modal para no saturar la vista.

Restricciones conservadas:

- No se modificó backend.
- No se modificaron endpoints.
- No se cambió la semántica de guardar configuración.
- No se invoca SOAP productivo desde la SPA.
- No se cambió modo a Live por defecto.
- No se exponen secretos completos ni certificados privados.

Validación agregada:

- Spec dedicado `soap-integration-settings.component.spec.ts`.
- Valida lista compacta, ausencia de formulario gigante inicial, detalle, edición, cancelación y prueba local.
- `npm run build`: OK.
- `npm test -- --watch=false --browsers=ChromeHeadless`: OK, 164 SUCCESS.

Estado: OK técnico frontend / Productivo NO-GO.
