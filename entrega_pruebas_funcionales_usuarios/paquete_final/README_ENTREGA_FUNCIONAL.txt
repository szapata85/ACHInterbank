README ENTREGA FUNCIONAL
ACH Interbank / CENIT

Contenido de la carpeta
Esta carpeta contiene los materiales de apoyo para ejecutar pruebas funcionales en ambiente local:
- Guía funcional de uso y validación.
- Matriz de escenarios de prueba.
- Formato para registrar incidencias.
- Capturas de evidencia visual del ambiente local.
- Resumen de ejecución y observaciones de la generación de evidencias.

Cómo deben usar la guía funcional
La guía funcional debe leerse antes de iniciar la ejecución.
La guía explica el orden sugerido de revisión de pantallas y qué validar en cada paso.
La guía está orientada a usuarios funcionales y debe usarse solo con datos de prueba autorizados.

Cómo deben usar la matriz de escenarios
La matriz de escenarios sirve para planear, ejecutar y registrar el resultado de cada caso.
Cada escenario debe marcarse con su estado correspondiente y con observaciones claras cuando aplique.
Si un escenario no puede ejecutarse por acceso, ambiente o datos faltantes, debe registrarse el motivo.

Cómo deben reportar incidencias
Toda novedad debe registrarse en el formato de incidencias.
Cada incidencia debe incluir:
- pantalla o proceso evaluado
- resultado esperado
- resultado observado
- evidencia asociada
- impacto funcional

Sobre las capturas
Las capturas son evidencia visual del ambiente local disponible durante la ejecución.
Las capturas no reemplazan la validación funcional, pero sí ayudan a soportar hallazgos y resultados.

Ingreso al sistema
Los usuarios deben ingresar por:
http://localhost:743

No debe usarse `/auth/login` como URL de ingreso para usuarios.
Esa ruta no debe considerarse una pantalla funcional de acceso para esta entrega.

Uso de datos
No deben usarse datos reales sensibles.
No deben usarse credenciales productivas.
Toda prueba debe realizarse con datos controlados y autorizados para validación.

Recomendación de uso
Primero revise la guía.
Después ejecute la matriz de escenarios.
Finalmente registre cualquier hallazgo en el formato de incidencias y adjunte la evidencia visual disponible.
