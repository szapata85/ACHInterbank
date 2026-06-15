# Ampliacion funcional V3 - Manual de pruebas locales ACH Interbank

## 1. Actores funcionales del manual

### Usuario funcional de pruebas
- Actor principal: Usuario funcional de pruebas.
- Actores secundarios: Soporte tecnico funcional.
- Para que sirve: Ejecutar escenarios de prueba y reportar incidencias.
- Cuando se usa: Durante la validacion funcional, la preparacion de evidencias y el cierre de pruebas.
- Pasos funcionales: Ingresar al aplicativo, ejecutar el flujo asignado, revisar el resultado y registrar observaciones.
- Que debe validar el usuario: Que la pantalla responda al caso de prueba y que la informacion visible coincida con los datos de prueba.
- Que errores debe reportar: Bloqueos, mensajes inesperados, datos inconsistentes y falta de acceso a la funcion esperada.
- Captura asociada, si existe: No aplica como seccion de referencia general.

### Operador ACH
- Actor principal: Operador ACH.
- Actores secundarios: Revisor / validador operativo y Soporte tecnico funcional.
- Para que sirve: Crear, consultar y validar transacciones, prenotificaciones, ciclos, devoluciones y archivos operativos.
- Cuando se usa: En la operacion diaria y en el seguimiento de resultados de ACH.
- Pasos funcionales: Consultar pantallas operativas, revisar estados, validar detalles y confirmar resultados.
- Que debe validar el usuario: Que los registros visibles correspondan al ciclo, archivo o transaccion revisada.
- Que errores debe reportar: Rechazos, diferencias de conciliacion, faltantes y resultados no esperados.
- Captura asociada, si existe: Varias capturas operativas de la Fase 2A.

### Administrador funcional ACH
- Actor principal: Administrador funcional ACH.
- Actores secundarios: Administrador de catalogos / parametrizacion.
- Para que sirve: Administrar reglas, prioridades, perfiles y configuraciones funcionales de la operacion ACH.
- Cuando se usa: Cuando se ajusta parametrizacion o se revisa la coherencia entre camara, ciclo y reglas.
- Pasos funcionales: Abrir la pantalla de configuracion, revisar el valor vigente, validar la relacion con la camara y confirmar los cambios visibles.
- Que debe validar el usuario: Que la configuracion refleje la politica esperada y no deje ambiguedades.
- Que errores debe reportar: Parametrizacion incompleta, duplicada, contradictoria o fuera de vigencia.
- Captura asociada, si existe: Capturas de preferencias, reglas, ciclos y perfiles.

### Administrador de catalogos / parametrizacion
- Actor principal: Administrador de catalogos / parametrizacion.
- Actores secundarios: Operador ACH.
- Para que sirve: Mantener catalogos maestros y valores funcionales basicos.
- Cuando se usa: Al crear o revisar datos maestros que alimentan otros procesos.
- Pasos funcionales: Entrar al catalogo, revisar los registros visibles, verificar estado y confirmar que el maestro sea consistente.
- Que debe validar el usuario: Que el catalogo tenga valores correctos, sin duplicados ni vacios.
- Que errores debe reportar: Valores faltantes, duplicados, estados incorrectos o campos obligatorios sin completar.
- Captura asociada, si existe: Capturas de catalogos y entidades financieras.

### Administrador de certificados digitales
- Actor principal: Administrador de certificados digitales.
- Actores secundarios: Soporte tecnico funcional y Revisor / validador operativo.
- Para que sirve: Registrar, consultar y gobernar certificados usados en la generacion y el cifrado.
- Cuando se usa: Cuando se valida vigencia, versionado, carga y uso operativo de certificados.
- Pasos funcionales: Abrir la pantalla de certificados, revisar metadatos visibles, verificar vigencia y continuar al flujo de trabajo requerido.
- Que debe validar el usuario: Que el certificado visible corresponda al entorno de prueba y a la operacion esperada.
- Que errores debe reportar: Certificado ausente, vencido, no visible, o con datos no coincidentes.
- Captura asociada, si existe: Capturas de gobierno de certificados y sobre digital.

### Revisor / validador operativo
- Actor principal: Revisor / validador operativo.
- Actores secundarios: Operador ACH.
- Para que sirve: Revisar resultados, conciliacion, rechazos, trazabilidad y evidencias.
- Cuando se usa: En el cierre de una prueba, en la revision de un caso o ante diferencias operativas.
- Pasos funcionales: Abrir el panel o reporte, revisar el detalle y validar que la evidencia cierre con la operacion.
- Que debe validar el usuario: Que el resultado coincida con el proceso ejecutado.
- Que errores debe reportar: Diferencias, estados inesperados, registros no conciliados y evidencias incompletas.
- Captura asociada, si existe: Dashboard operativo, conciliacion y reportes.

### Usuario de consulta / auditoria
- Actor principal: Usuario de consulta / auditoria.
- Actores secundarios: Revisor / validador operativo.
- Para que sirve: Consultar reportes, logs, auditoria y evidencias sin modificar parametrizacion.
- Cuando se usa: Cuando solo se requiere lectura y trazabilidad.
- Pasos funcionales: Abrir la consulta, aplicar filtros y revisar el detalle mostrado.
- Que debe validar el usuario: Que la consulta permita verificar el dato sin modificarlo.
- Que errores debe reportar: Falta de acceso, datos incompletos o inconsistencia entre consulta y evidencia.
- Captura asociada, si existe: Reportes y pantallas de consulta.

### Soporte tecnico funcional
- Actor principal: Soporte tecnico funcional.
- Actores secundarios: Todos los demas actores.
- Para que sirve: Acompanar pruebas, clasificar incidencias y validar si un problema corresponde a datos, parametrizacion, seguridad, camara o aplicacion.
- Cuando se usa: Durante la ejecucion de pruebas, ante errores o cuando una pantalla no responde como se espera.
- Pasos funcionales: Revisar el caso, verificar la pantalla, confirmar el alcance y orientar el siguiente paso.
- Que debe validar el usuario: Que la falla se clasifique correctamente antes de escalarla.
- Que errores debe reportar: Errores tecnicos, accesos bloqueados y comportamientos que no correspondan al flujo funcional.
- Captura asociada, si existe: Segun la pantalla en revision.

## 2. Preparacion funcional de datos maestros

### 2.1 Entidades financieras
- Actor principal: Administrador de catalogos / parametrizacion.
- Actores secundarios: Operador ACH.
- Para que sirve: Mantener la informacion basica de las entidades que participan en la operacion.
- Cuando se usa: Antes de ejecutar pruebas de terceros, clientes, conciliacion o generacion.
- Pasos funcionales: Abrir el mantenimiento, revisar el listado visible, validar los registros y confirmar que el dato de verificacion sea el mostrado por la aplicacion.
- Que debe validar el usuario: Que la entidad financiera corresponda al registro esperado y que los campos visibles esten completos.
- Que errores debe reportar: Registros duplicados, datos faltantes, inconsistencias de identificacion o campos de verificacion vacios cuando deban mostrarse.
- Captura asociada, si existe: `32_financial_institutions_mantenimiento_digito_verificacion.png`.

### 2.2 Priorizacion por camara
- Actor principal: Administrador funcional ACH.
- Actores secundarios: Administrador de catalogos / parametrizacion.
- Para que sirve: Definir el orden o preferencia visible por camara ACH Colombia o CENIT.
- Cuando se usa: Cuando se ajusta la prioridad funcional que aplicara la operacion o la consulta.
- Pasos funcionales: Abrir la pantalla de preferencias, revisar la prioridad mostrada y confirmar la camara asociada.
- Que debe validar el usuario: Que la prioridad visible sea coherente con la camara y el contexto operativo.
- Que errores debe reportar: Prioridades invertidas, registros duplicados o camara incorrecta.
- Captura asociada, si existe: `33_clearing_house_preferences_prioridades_camara.png`.

### 2.3 Tipos de documento
- Actor principal: Administrador de catalogos / parametrizacion.
- Actores secundarios: Operador ACH.
- Para que sirve: Mantener los valores usados para identificar personas o terceros.
- Cuando se usa: Al crear clientes, terceros o registros maestros.
- Pasos funcionales: Abrir el catalogo, revisar los tipos mostrados y validar que existan los valores requeridos.
- Que debe validar el usuario: Que los tipos de documento coincidan con los permitidos por la operacion.
- Que errores debe reportar: Valores inexistentes, duplicados o marcados incorrectamente.
- Captura asociada, si existe: `34_catalog_document_types.png`.

### 2.4 Sexo / genero
- Actor principal: Administrador de catalogos / parametrizacion.
- Actores secundarios: Operador ACH.
- Para que sirve: Mantener el catalogo de genero o sexo visible para procesos de identificacion.
- Cuando se usa: Durante la validacion de datos maestros o la creacion de clientes.
- Pasos funcionales: Abrir el catalogo, revisar los valores visibles y confirmar su vigencia.
- Que debe validar el usuario: Que el valor mostrado sea el requerido por la prueba.
- Que errores debe reportar: Valores faltantes, duplicados o inactivos sin justificacion.
- Captura asociada, si existe: `35_catalog_gender_types.png`.

### 2.5 Tipos de persona
- Actor principal: Administrador de catalogos / parametrizacion.
- Actores secundarios: Operador ACH.
- Para que sirve: Mantener la clasificacion funcional de persona natural o juridica, segun aplique.
- Cuando se usa: En altas, validaciones de terceros y clientes.
- Pasos funcionales: Abrir el catalogo y revisar que el valor requerido exista y sea legible.
- Que debe validar el usuario: Que el tipo de persona corresponda al caso de prueba.
- Que errores debe reportar: Valores ausentes o mal clasificados.
- Captura asociada, si existe: `36_catalog_person_types.png`.

### 2.6 Codigos de transaccion ACH
- Actor principal: Administrador de catalogos / parametrizacion.
- Actores secundarios: Operador ACH.
- Para que sirve: Mantener los codigos que se usan en la operacion ACH.
- Cuando se usa: En reglas, conciliacion y validacion funcional de procesos.
- Pasos funcionales: Revisar el catalogo, confirmar los codigos visibles y validar su correspondencia con la operacion.
- Que debe validar el usuario: Que el codigo mostrado sea el esperado.
- Que errores debe reportar: Codigos faltantes, duplicados o inconsistentes.
- Captura asociada, si existe: `37_catalog_transaction_codes.png`.

### 2.7 Conceptos de lote
- Actor principal: Administrador de catalogos / parametrizacion.
- Actores secundarios: Operador ACH.
- Para que sirve: Mantener los conceptos visibles que se asocian a lotes o descripciones de entrada.
- Cuando se usa: Al generar, consultar o revisar archivos y lotes operativos.
- Pasos funcionales: Abrir el catalogo, revisar la descripcion visible y confirmar que coincida con el uso esperado.
- Que debe validar el usuario: Que el concepto corresponda al lote o archivo revisado.
- Que errores debe reportar: Descripciones incorrectas, duplicadas o ausentes.
- Captura asociada, si existe: `38_catalog_company_entry_descriptions.png`.

## 3. Administracion de terceros de prenotificacion

### Listado de terceros de prenotificacion
- Actor principal: Operador ACH.
- Actores secundarios: Soporte tecnico funcional.
- Para que sirve: Consultar y seguir los terceros que participan en prenotificacion.
- Cuando se usa: Antes de una validacion operativa o cuando se revisa un registro existente.
- Pasos funcionales: Abrir el listado, usar los filtros visibles, ubicar el tercero y revisar el detalle en la misma vista si aplica.
- Que debe validar el usuario: Que el registro mostrado corresponda al tercero esperado y que la busqueda devuelva resultados coherentes.
- Que errores debe reportar: No encontrar resultados, filtros que no devuelven lo esperado o datos incongruentes.
- Captura asociada, si existe: `28_customer_third_parties_listado_busqueda.png`.

- Creacion de terceros: pendiente de validacion.
- Estado funcional: No encontrado como accion confirmada.
- Observacion: La evidencia disponible solo confirma listado y busqueda.

## 4. Administracion de clientes

### Listado de clientes
- Actor principal: Operador ACH.
- Actores secundarios: Soporte tecnico funcional.
- Para que sirve: Consultar los clientes cargados en la aplicacion.
- Cuando se usa: Para validar datos existentes o ubicar un cliente antes de una operacion.
- Pasos funcionales: Abrir el listado, revisar los registros visibles y localizar el cliente requerido.
- Que debe validar el usuario: Que el registro corresponda al cliente esperado.
- Que errores debe reportar: Cliente no encontrado, lista vacia sin justificacion o datos inconsistentes.
- Captura asociada, si existe: `30_customers_listado.png`.

### Creacion de clientes
- Actor principal: Operador ACH.
- Actores secundarios: Soporte tecnico funcional.
- Para que sirve: Registrar un cliente nuevo con los datos requeridos por la operacion.
- Cuando se usa: Cuando se necesita un alta funcional para pruebas locales.
- Pasos funcionales: Abrir el formulario de alta, completar los campos visibles, revisar validaciones y guardar solo si la pantalla lo permite.
- Que debe validar el usuario: Que la informacion se acepte sin errores y que el cliente quede disponible para consulta.
- Que errores debe reportar: Campos obligatorios vacios, datos invalidos o rechazo al guardar.
- Captura asociada, si existe: `31_customers_nuevo.png`.

## 5. Administracion de entidades financieras y digito de verificacion

### Mantenimiento de entidades financieras
- Actor principal: Administrador de catalogos / parametrizacion.
- Actores secundarios: Operador ACH.
- Para que sirve: Consultar y mantener la informacion de entidades financieras visible en la aplicacion.
- Cuando se usa: Antes de pruebas que dependan de bancos o entidades relacionadas.
- Pasos funcionales: Abrir la pantalla, revisar el listado, confirmar los datos visibles y validar el campo de verificacion si aparece.
- Que debe validar el usuario: Que los datos visibles correspondan al entorno de pruebas y que el registro sea consistente.
- Que errores debe reportar: Registros duplicados, inconsistencia en nombres o campos faltantes.
- Captura asociada, si existe: `32_financial_institutions_mantenimiento_digito_verificacion.png`.

### Digito de verificacion
- Actor principal: Administrador de catalogos / parametrizacion.
- Actores secundarios: Usuario funcional de pruebas.
- Para que sirve: Validar visualmente el campo de verificacion cuando la aplicacion lo muestre.
- Cuando se usa: En la consulta o mantenimiento de entidades financieras.
- Pasos funcionales: Revisar el campo visible y confirmar que la informacion mostrada sea la esperada por la prueba.
- Que debe validar el usuario: Solo el valor visible en pantalla.
- Que errores debe reportar: Ausencia del campo cuando se espere, valor incorrecto o dato inconsistente.
- Captura asociada, si existe: `32_financial_institutions_mantenimiento_digito_verificacion.png`.

## 6. Priorizacion por camara ACH Colombia / CENIT

### Preferencias por camara
- Actor principal: Administrador funcional ACH.
- Actores secundarios: Administrador de catalogos / parametrizacion.
- Para que sirve: Definir la priorizacion o preferencia funcional por camara.
- Cuando se usa: Cuando se revisa que la pantalla muestre el orden o prioridad requerido.
- Pasos funcionales: Abrir la configuracion, revisar la prioridad visible y validar la relacion con la camara.
- Que debe validar el usuario: Que la priorizacion mostrada sea la que corresponde al caso de prueba.
- Que errores debe reportar: Orden equivocado, camara incorrecta o registro duplicado.
- Captura asociada, si existe: `33_clearing_house_preferences_prioridades_camara.png`.

## 7. Reglas por camara y configuracion de ciclos

### Reglas por camara
- Actor principal: Administrador funcional ACH.
- Actores secundarios: Soporte tecnico funcional.
- Para que sirve: Revisar las reglas visibles aplicables a la camara seleccionada.
- Cuando se usa: Cuando se valida una politica o condicion operativa por camara.
- Pasos funcionales: Abrir la pantalla, revisar las reglas visibles y confirmar que correspondan a la camara activa.
- Que debe validar el usuario: Que la regla, el alcance y la camara coincidan.
- Que errores debe reportar: Regla faltante, regla duplicada o camara equivocada.
- Captura asociada, si existe: `39_transactions_clearing_house_rules.png`.

### Configuracion de ciclos
- Actor principal: Administrador funcional ACH.
- Actores secundarios: Operador ACH.
- Para que sirve: Revisar la configuracion vigente de ciclos.
- Cuando se usa: Antes de ejecutar o validar una operacion de ciclo.
- Pasos funcionales: Abrir la pantalla, revisar el ciclo visible y validar su estado o vigencia segun lo mostrado.
- Que debe validar el usuario: Que la configuracion corresponda al ciclo que se espera usar.
- Que errores debe reportar: Ciclo no vigente, datos incompletos o configuracion inconsistene.
- Captura asociada, si existe: `40_transactions_cycle_configs.png`.

## 8. Causales y politicas CENIT

### Causales de devolucion CENIT
- Actor principal: Administrador funcional ACH.
- Actores secundarios: Revisor / validador operativo.
- Para que sirve: Consultar las causales visibles de devolucion regulatoria.
- Cuando se usa: Cuando se valida una devolucion o una prueba de rechazo.
- Pasos funcionales: Abrir la pantalla, ubicar la causal y revisar su descripcion visible.
- Que debe validar el usuario: Que la causal corresponda al caso revisado.
- Que errores debe reportar: Causal incorrecta, ausente o inconsistente.
- Captura asociada, si existe: `41_cenit_causales_devolucion.png`.

### Causales de rechazo CENIT
- Actor principal: Administrador funcional ACH.
- Actores secundarios: Revisor / validador operativo.
- Para que sirve: Consultar las causales visibles de rechazo regulatorio.
- Cuando se usa: Al revisar una respuesta o resultado operacional.
- Pasos funcionales: Abrir la pantalla, revisar el listado y confirmar la causal mostrada.
- Que debe validar el usuario: Que la causal sea la esperada para la prueba.
- Que errores debe reportar: Causal no encontrada, descripcion incorrecta o dato duplicado.
- Captura asociada, si existe: `42_cenit_causales_rechazo.png`.

### Politicas de transaccion CENIT
- Actor principal: Administrador funcional ACH.
- Actores secundarios: Soporte tecnico funcional.
- Para que sirve: Revisar las politicas operativas de transaccion visibles.
- Cuando se usa: Cuando se valida la regla aplicable a una operacion.
- Pasos funcionales: Abrir la pantalla, revisar la politica y confirmar su alcance visible.
- Que debe validar el usuario: Que la politica corresponda a la operacion esperada.
- Que errores debe reportar: Politica ausente, duplicada o mal asignada.
- Captura asociada, si existe: `43_cenit_politicas_transaccion.png`.

### Politicas de prenotificacion CENIT
- Actor principal: Administrador funcional ACH.
- Actores secundarios: Operador ACH.
- Para que sirve: Revisar las politicas visibles de prenotificacion.
- Cuando se usa: En la validacion previa a la prenotificacion o ante una observacion funcional.
- Pasos funcionales: Abrir la pantalla, revisar el contenido visible y confirmar su vigencia.
- Que debe validar el usuario: Que la politica sea la correcta para el proceso revisado.
- Que errores debe reportar: Politica faltante, texto inconsistente o clasificacion incorrecta.
- Captura asociada, si existe: `44_cenit_politicas_prenotificacion.png`.

## 9. Perfiles NACHA-M

### Listado de perfiles NACHA-M
- Actor principal: Administrador funcional ACH.
- Actores secundarios: Operador ACH.
- Para que sirve: Consultar los perfiles oficiales visibles en la aplicacion.
- Cuando se usa: Al revisar la configuracion funcional de NACHA-M.
- Pasos funcionales: Abrir el listado, revisar los perfiles visibles y ubicar el requerido.
- Que debe validar el usuario: Que el perfil mostrado corresponda a la camara o configuracion esperada.
- Que errores debe reportar: Perfil no visible, nombre inconsistente o dato ausente.
- Captura asociada, si existe: `45_nacha_config_perfiles.png`.

### Detalle de perfil NACHA-M
- Estado funcional: Captura observada, pero debe tratarse como validacion puntual del perfil.
- Actor principal: Administrador funcional ACH.
- Para que sirve: Ver el detalle de un perfil cuando existe registro navegable.
- Cuando se usa: Solo al revisar el detalle de un perfil especifico.
- Pasos funcionales: Abrir el listado, seleccionar un perfil y revisar el detalle visible.
- Que debe validar el usuario: Que el detalle pertenezca al perfil seleccionado.
- Que errores debe reportar: Detalle inaccesible, registro no navegable o informacion incompleta.
- Captura asociada, si existe: `46_nacha_config_perfil_detalle_si_existe.png`.

## 10. Consulta operativa, conciliacion y reportes

### Dashboard operativo NACHA-M
- Actor principal: Revisor / validador operativo.
- Actores secundarios: Operador ACH.
- Para que sirve: Revisar el estado operativo general de NACHA-M.
- Cuando se usa: Durante la supervision de procesos o al verificar resultados de ejecucion.
- Pasos funcionales: Abrir el panel, revisar indicadores y validar la informacion mostrada.
- Que debe validar el usuario: Que el dashboard presente el estado esperado.
- Que errores debe reportar: Datos vacios, totales inconsistentes o panel no cargado.
- Captura asociada, si existe: `47_nacha_operational_dashboard.png`.

### Conciliacion ACH
- Actor principal: Revisor / validador operativo.
- Actores secundarios: Operador ACH.
- Para que sirve: Comparar resultados y detectar diferencias visibles.
- Cuando se usa: Al cerrar pruebas o validar un resultado operativo.
- Pasos funcionales: Abrir la conciliacion, revisar las diferencias y confirmar los resultados visibles.
- Que debe validar el usuario: Que la conciliacion coincida con la operacion ejecutada.
- Que errores debe reportar: Diferencias no esperadas, registros faltantes o estados incongruentes.
- Captura asociada, si existe: `48_ach_reconciliation.png`.

### Reporte de rechazos
- Actor principal: Revisor / validador operativo.
- Actores secundarios: Operador ACH.
- Para que sirve: Consultar rechazos visibles para seguimiento de incidencias.
- Cuando se usa: Al revisar rechazos o al preparar evidencia de una prueba.
- Pasos funcionales: Abrir el reporte, revisar los registros y confirmar el detalle mostrado.
- Que debe validar el usuario: Que el rechazo corresponda al caso revisado.
- Que errores debe reportar: Rechazo faltante, detalle incompleto o resultado inesperado.
- Captura asociada, si existe: `49_reports_rejections.png`.

## 11. Seguridad NACHA-M, certificados digitales y sobre digital

### Dashboard seguridad NACHA-M
- Actor principal: Administrador de certificados digitales.
- Actores secundarios: Soporte tecnico funcional.
- Para que sirve: Entrar al modulo de seguridad NACHA-M y revisar su estado general.
- Cuando se usa: Al iniciar una tarea de seguridad o validar accesos a este modulo.
- Pasos funcionales: Abrir el dashboard, revisar los accesos visibles y continuar al flujo requerido.
- Que debe validar el usuario: Que el modulo de seguridad cargue correctamente.
- Que errores debe reportar: Acceso bloqueado, carga incompleta o panel vacio por error tecnico.
- Captura asociada, si existe: `51_nacha_security_dashboard.png`.

### Gobierno / consulta de certificados
- Actor principal: Administrador de certificados digitales.
- Actores secundarios: Revisor / validador operativo.
- Para que sirve: Consultar certificados y su informacion visible de gobierno.
- Cuando se usa: Cuando se revisa el estado de un certificado o se valida una carga.
- Pasos funcionales: Abrir la pantalla, revisar el listado y confirmar la informacion mostrada.
- Que debe validar el usuario: Que el certificado mostrado sea el esperado para el entorno de prueba.
- Que errores debe reportar: Certificado no visible, listado vacio sin justificacion o datos inconsistentes.
- Captura asociada, si existe: `52_nacha_security_certificates_gobierno.png`.

### Cifrado manual con sobre digital
- Actor principal: Administrador de certificados digitales.
- Actores secundarios: Soporte tecnico funcional.
- Para que sirve: Ejecutar un cifrado manual con la herramienta de sobre digital.
- Cuando se usa: Al validar la operacion manual de cifrado en pruebas locales.
- Pasos funcionales: Abrir la pantalla, cargar el archivo o insumo visible, ejecutar la accion y revisar el resultado mostrado.
- Que debe validar el usuario: Que el proceso termine y que el resultado sea legible.
- Que errores debe reportar: Archivo no aceptado, resultado vacio o fallo visible de procesamiento.
- Captura asociada, si existe: `59_nacha_manual_encrypt_sobre_digital.png`.

### Descifrado manual con sobre digital
- Actor principal: Administrador de certificados digitales.
- Actores secundarios: Soporte tecnico funcional.
- Para que sirve: Ejecutar un descifrado manual con la herramienta de sobre digital.
- Cuando se usa: Al validar la recuperacion de un archivo en entorno de prueba.
- Pasos funcionales: Abrir la pantalla, seleccionar el insumo disponible, ejecutar la accion y revisar el contenido visible.
- Que debe validar el usuario: Que el contenido recuperado sea el esperado.
- Que errores debe reportar: Insumo invalido, resultado vacio o fallo visible de descifrado.
- Captura asociada, si existe: `60_nacha_manual_decrypt_sobre_digital.png`.

### Herramienta sobre digital
- Actor principal: Administrador de certificados digitales.
- Actores secundarios: Soporte tecnico funcional.
- Para que sirve: Usar la herramienta funcional de sobre digital para tareas de operacion y revision.
- Cuando se usa: Cuando se requiere revisar o probar el flujo de sobre digital.
- Pasos funcionales: Abrir la herramienta, revisar las opciones visibles y ejecutar la accion necesaria.
- Que debe validar el usuario: Que la herramienta muestre las opciones y resultados esperados.
- Que errores debe reportar: Pantalla vacia, opciones no visibles o resultado no utilizable.
- Captura asociada, si existe: `61_sobre_digital_tool.png`.

## 12. Generacion NACHA-M y naming

### Estado funcional de la generacion
- Actor principal: Administrador de certificados digitales.
- Actores secundarios: Operador ACH.
- Para que sirve: Probar la generacion del archivo NACHA-M cuando la pantalla muestre un resultado real.
- Cuando se usa: Solo cuando la interfaz devuelve un nombre o resultado visible y utilizable.
- Pasos funcionales: Abrir la pantalla de generacion, revisar si existe salida visible y confirmar el dato mostrado.
- Que debe validar el usuario: Solo lo que la pantalla muestre de forma expresa.
- Que errores debe reportar: Ausencia de resultado, salida no visible o contenido no utilizable.
- Captura asociada, si existe: Pendiente de validacion para `57_nacha_generate_base.png` y `58_nacha_generate_encrypted.png`.

### Naming reglamentario
- Actor principal: Administrador de certificados digitales.
- Actores secundarios: Revisor / validador operativo.
- Para que sirve: Registrar el nombre visible del archivo solo si la aplicacion lo muestra.
- Cuando se usa: En la revision del archivo base, del cifrado o del archivo final exportable.
- Pasos funcionales: Revisar el resultado mostrado, anotar el nombre visible y validar que coincida con la salida de la aplicacion.
- Que debe validar el usuario: Unicamente el nombre que la pantalla, archivo o exportacion muestre de forma explicita.
- Que errores debe reportar: Nombre ausente, incompleto o no visible.
- Captura asociada, si existe: Pendiente de validacion para `64_naming_archivo_base_por_camara_si_visible.png` y `65_naming_archivo_final_env_si_visible.png`.

## 13. Pendientes funcionales de validacion

| Tema | Estado | Motivo | Accion recomendada |
|---|---|---|---|
| Creacion de terceros | No encontrado | La evidencia solo confirma listado y busqueda. | Validar si existe boton, modal o accion interna en la pantalla de terceros. |
| Onboarding silencioso | No encontrado | No se observo ayuda, prellenado ni flujo automatico visible. | Revisar altas de clientes y entidades para confirmar si aparece como comportamiento visible. |
| Rotacion / reemplazo de certificados | No encontrado | No aparece accion visible de activar, revocar, reemplazar o versionar. | Revisar la pantalla de certificados con un registro real si se habilita. |
| Versiones / historial de certificados | Requiere validacion | No existe un registro navegable confirmado para documentarlo como flujo estable. | Confirmar con un certificado real y revisar su detalle. |
| Estados / vigencia de certificados | Requiere validacion | No hay certificados cargados para mostrar estados o fechas de vigencia. | Validar con un certificado visible en la pantalla. |
| Generacion NACHA-M base | Requiere validacion | No se obtuvo resultado funcional utilizable en el entorno. | Repetir la generacion y confirmar salida visible. |
| Generacion NACHA-M cifrada | Requiere validacion | No se obtuvo resultado funcional utilizable en el entorno. | Repetir la generacion y confirmar salida visible. |
| Naming archivo base por camara | Requiere validacion | No se obtuvo un nombre base visible para documentar. | Confirmar solo si la aplicacion muestra el nombre base. |
| Naming archivo final .env | Requiere validacion | No se obtuvo un nombre final exportable visible para documentar. | Confirmar solo si la salida muestra el nombre final. |

## 14. Pantallas retiradas del aplicativo y del manual funcional

| Pantalla | Ruta anterior | Motivo |
|---|---|---|
| Auditoria de certificados / sobre digital | `/nacha-security/digital-envelope/audit` | Retirada por no aportar evidencia funcional util para usuarios finales en el entorno actual. |
| Interoperabilidad / vector oficial | `/nacha-security/digital-envelope/interoperability` | Retirada por no aportar evidencia funcional util para usuarios finales en el entorno actual. |

