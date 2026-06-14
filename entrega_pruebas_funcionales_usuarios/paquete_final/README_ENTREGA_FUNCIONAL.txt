README ENTREGA FUNCIONAL
ACH Interbank / CENIT

Estado de esta entrega
- La pantalla visual de ingreso para usuarios es `http://localhost:743/login`.
- El endpoint tecnico de autenticacion sigue siendo `POST /auth/login`.
- `GET /auth/login` no debe usarse como pantalla funcional.
- En esta corrida las variables `ACH_UAT_USER` y `ACH_UAT_PASSWORD` existieron en la sesion.
- La autenticacion de prueba fue aceptada y se genero token de sesion.

Contenido de la carpeta
- Guia funcional de uso y validacion.
- Matriz de escenarios de prueba.
- Formato para registrar incidencias.
- Capturas de evidencia visual.
- Resumen de ejecucion de la corrida hecha con Codex.

Resultado de la corrida autenticada del 2026-06-13
- Se ejecuto `capturar_spa_con_node_playwright.js` sin instalar dependencias nuevas.
- La captura `01_inicio_o_login.png` corresponde a la pantalla visual `/login`.
- El intento de autenticacion recibio `200` en `POST /auth/login`.
- Se confirmo token de sesion en la SPA autenticada.
- No se uso `/auth/login` como pantalla visual.

Capturas generadas en esta corrida
- `01_inicio_o_login.png`
- `02_dashboard.png`
- `03_dashboard_operacional_nacha.png`
- `04_configuracion_perfiles_nacha.png`
- `05_exportacion_nacha.png`
- `08_cenit.png`
- `09_uat_console.png`
- `10_menu_o_navegacion.png`

Rutas internas previstas para la evidencia
- `/dashboard`
- `/ach/nacha/operational-dashboard`
- `/nacha-config-admin/perfiles`
- `/ach-cycles/nacha/export`
- `/ach-cycles`
- `/transactions`
- `/cenit`
- `/ach/nacha/soap-uat-console`
- `/uat` solo si queda funcional tras autenticacion

Resultado por alcance
- La pantalla `/login` quedo validada como acceso visual correcto.
- Ninguna captura final usa `/auth/login`.
- La ruta opcional `/uat` estuvo disponible y cargo funcionalmente como `/uat/nacha-inbound-simulator`.
- Las rutas `/ach-cycles` y `/transactions` quedaron omitidas en evidencia PNG por respuesta `401` al navegar directamente en esta sesion autenticada.

Uso de datos
- No usar datos reales sensibles.
- No usar credenciales productivas.
- Ejecutar solo con datos de prueba autorizados.

Archivos clave actualizados en esta corrida
- `capturas/resultado_capturas.json`
- `README_ENTREGA_FUNCIONAL.txt`
- `RESUMEN_EJECUCION_CODEX.txt`
- `paquete_final/`
- `paquete_final/capturas/`

Estado del Word final
- No se genero un Word nuevo con imagenes incrustadas en esta corrida.
- Se conserva la guia `.docx` existente dentro del paquete final.

Validacion de terminos restringidos
- Se revisaron los entregables funcionales finales con la lista de palabras restringidas definida para esta entrega.
- No quedaron coincidencias en los entregables finales de texto y JSON actualizados en esta corrida.

Proximo intento recomendado
- Revisar autorizaciones de `/ach-cycles` y `/transactions` para el usuario de prueba si tambien deben quedar evidenciadas.
- Si se requiere evidencia visual adicional de `/uat`, agregar una captura opcional dedicada en una corrida posterior.
