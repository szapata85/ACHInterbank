README ENTREGA FUNCIONAL
ACH Interbank / CENIT

Estado de esta entrega
- La pantalla visual de ingreso para usuarios es `http://localhost:743/login`.
- El endpoint tecnico de autenticacion sigue siendo `POST /auth/login`.
- `GET /auth/login` no debe usarse como pantalla funcional.
- En esta corrida las variables `ACH_UAT_USER` y `ACH_UAT_PASSWORD` existieron en la sesion, pero el login de prueba fue rechazado por el sistema.

Contenido de la carpeta
- Guia funcional de uso y validacion.
- Matriz de escenarios de prueba.
- Formato para registrar incidencias.
- Capturas de evidencia visual.
- Resumen de ejecucion de la corrida hecha con Codex.

Resultado de la corrida autenticada del 2026-06-13
- Se ejecuto `capturar_spa_con_node_playwright.js` sin instalar dependencias nuevas.
- La captura `01_inicio_o_login.png` corresponde a la pantalla visual `/login`.
- El intento de autenticacion recibio `401` en `POST /auth/login`.
- No se genero token de sesion en `sessionStorage`.
- Por lo anterior no fue posible capturar pantallas internas autenticadas en esta sesion.

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
- Las rutas internas quedaron omitidas por sesion invalida o falta de autorizacion despues del login fallido.

Uso de datos
- No usar datos reales sensibles.
- No usar credenciales productivas.
- Ejecutar solo con datos de prueba autorizados.

Archivos clave actualizados en esta corrida
- `capturas/resultado_capturas.json`
- `RESUMEN_EJECUCION_CODEX.txt`
- `paquete_final/`

Proximo intento recomendado
- Validar que las credenciales UAT de prueba sean aceptadas por `POST /auth/login`.
- Reejecutar el script de capturas con la misma ruta visual `/login`.
