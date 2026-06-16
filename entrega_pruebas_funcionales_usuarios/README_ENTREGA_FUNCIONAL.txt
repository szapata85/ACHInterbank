README ENTREGA FUNCIONAL
ACH Interbank / CENIT

Estado de esta entrega
- La pantalla visual de ingreso para usuarios es `http://localhost:743/login`.
- El endpoint tecnico de autenticacion sigue siendo `POST /auth/login`.
- `GET /auth/login` no debe usarse como pantalla funcional.
- En esta corrida las variables `ACH_UAT_USER` y `ACH_UAT_PASSWORD` existieron en la sesion.
- La autenticacion de prueba fue aceptada y se genero token de sesion.

Diagnostico de rutas
- Angular define ruta visual para `/ach-cycles`.
- Angular define el modulo `transactions` en `/transactions`, con redireccion interna a `/transactions/list`.
- Nginx tambien define `location = /ach-cycles` y `location = /transactions` apuntando al API.
- La colision ocurre en la navegacion directa del documento para esas dos rutas base.
- La evidencia apunta a conflicto de enrutamiento/proxy en las rutas base, no a falta demostrada de permisos del usuario `admin`.

Comportamiento confirmado
- Navegacion directa a `/ach-cycles`: responde el API y sin bearer devuelve `401`.
- Navegacion directa a `/transactions`: responde el API y sin bearer devuelve `401`.
- Con autenticacion interna desde la SPA, `Ciclos ACH` carga visualmente en `/ach-cycles` y consume datos del API.
- Con autenticacion interna desde la SPA, `Transacciones` navega visualmente a `/transactions/list` y consume datos del API.
- `/ach-cycles/nacha/export` sigue funcionando como pantalla visual y no colisiona en la misma forma que la ruta base.

Capturas generadas en esta corrida
- `01_inicio_o_login.png`
- `02_dashboard.png`
- `03_dashboard_operacional_nacha.png`
- `04_configuracion_perfiles_nacha.png`
- `05_exportacion_nacha.png`
- `06_ciclos_ach.png`
- `07_transacciones.png`
- `08_cenit.png`
- `09_uat_console.png`
- `10_menu_o_navegacion.png`
- `11_uat_inbound_simulator.png`

Conclusiones funcionales
- No se uso `/auth/login` como pantalla visual.
- Ninguna captura final corresponde a `/auth/login`.
- `/ach-cycles` tiene colision entre ruta visual Angular y endpoint proxyeado en acceso directo.
- La ruta visual efectiva de transacciones desde menu es `/transactions/list`.
- `/uat` estuvo disponible y redirigio funcionalmente a `/uat/nacha-inbound-simulator`.

Archivos clave actualizados en esta corrida
- `capturas/resultado_capturas.json`
- `README_ENTREGA_FUNCIONAL.txt`
- `RESUMEN_EJECUCION_CODEX.txt`
- `paquete_final/`
- `paquete_final/capturas/`

Estado del Word final
- No se genero un Word nuevo con imagenes incrustadas en esta corrida.
- Se conservan los archivos `.docx` existentes dentro del paquete final.

Validacion de terminos restringidos
- Se revisaron los entregables funcionales finales con la lista de palabras restringidas definida para esta entrega.
- No quedaron coincidencias en los entregables finales de texto y JSON actualizados en esta corrida.

Propuesta de correccion no aplicada
- Opcion minima preferida: mover los endpoints base colisionantes al prefijo `/api/...`.
- Alternativa: ajustar Nginx para proxyear solo rutas API especificas y dejar libres las rutas visuales base de Angular.
- Alternativa secundaria: cambiar las rutas visuales Angular para evitar colision con endpoints base.
- No se aplico ninguna correccion en backend, frontend, rutas Angular, Nginx ni permisos.
