README ENTREGA FUNCIONAL
ACH Interbank / CENIT

Contenido del paquete
- Guía funcional para ejecutar las pruebas locales.
- Matriz de escenarios para registrar el resultado de cada caso.
- Formato de incidencias para reportar hallazgos.
- Capturas de referencia de las pantallas principales.
- Resumen corto del estado de la entrega.

Ingreso al sistema
- Ingresar por `http://localhost:743/login`.
- Esa es la pantalla visual de acceso para los usuarios.
- `/auth/login` no es una ruta para usuarios.

Documentos que debe usar el usuario
- `Guia_funcional_pruebas_locales_ACH_Interbank_CENIT.docx`:
  usarla como documento principal de consulta para el recorrido de pruebas.
- `Matriz_escenarios_prueba_funcional_ACH_Interbank_CENIT.xlsx` o su version `_FINAL`:
  usarla para marcar cada escenario como ejecutado, observado o pendiente.
- `Formato_reporte_incidencias_ACH_Interbank_CENIT.xlsx` o su version `_FINAL`:
  usarla para registrar hallazgos, errores visibles o diferencias frente al resultado esperado.

Uso de la matriz de escenarios
- Revisar un escenario a la vez.
- Ejecutar el paso indicado en la aplicación.
- Registrar el resultado obtenido.
- Si el resultado no coincide con lo esperado, registrar el hallazgo en el formato de incidencias.

Registro de incidencias
- Registrar una incidencia por cada hallazgo relevante.
- Describir de forma simple qué se intentó hacer, qué ocurrió y en qué pantalla sucedió.
- Adjuntar la captura correspondiente cuando ayude a explicar el caso.

Uso de las capturas
- La carpeta `capturas/` contiene imágenes de referencia de las pantallas principales.
- Se pueden consultar antes o durante la prueba para validar que la pantalla visible sea la correcta.
- Las capturas de `Ciclos ACH` y `Transacciones` deben revisarse tomando esas opciones desde el menú del sistema.

Datos permitidos
- Usar solo datos de prueba autorizados.
- No usar datos reales sensibles.

Observaciones para la navegación
- Algunas pantallas deben abrirse desde el menú del sistema después del ingreso.
- Para esta entrega, `Ciclos ACH` y `Transacciones` deben abrirse desde el menú del sistema.

Estado de la entrega
- El paquete contiene los documentos principales y las capturas requeridas para iniciar pruebas locales funcionales.
