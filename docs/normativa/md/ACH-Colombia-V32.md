### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Enero del 202 5
Página 1 de 329
```
Información de carácter Confidencial

```
MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD
PARTICIPANTE
```
VERSIÓN 32

ENERO 2025


### ^

```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 3 2
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Enero del 202 5
Página 2 de 329
```
Información de carácter Confidencial

INFORMACIÓN DEL MANUAL

```
TÍTULO Servicio ACH transferencias Interbancarias para Entidad Participante
CÓDIGO DDS-DIS-MAN- 004
VERSIÓN 32
```
### OBJETIVO

```
El presente documento contiene la descripción y características del
servicio ACH Transferencias Interbancarias disponible para las
Entidades Participantes.
```
```
ALCANCE
```
```
Aplica para el servicio ACH Transferencias Interbancarias y las partes
involucradas e interesadas en este servicio, incluyendo los
funcionarios de ACH COLOMBIA y las Entidades Participantes.
```
### DOCUMENTOS RELACIONADOS

```
Manual de Operaciones PSE
Manual de operaciones SOI
Planilla de compensación
Autorización de recaudo
Factura de venta
Cancelación de autorización de recaudo
Autorización de administradores de usuarios ante Integra ACH
(Entidad Financiera)
Instructivo de cuentas de usuarios y contraseñas GIR-GRINS- 010
PROCESOS RELACIONADOS Diseño y Desarrollo de Servicio
```
```
ÁREAS RELACIONADAS Producto,^ Operaciones,^ Comercial,^ Mercadeo,^ Seguridad^ de^ la^
Información y Tecnología
```
### ELABORÓ REVISÓ APROBÓ

```
NOMBRE (S) Luis Carlos Rico Andrés Felipe Soto Oscar Fernando Benavidez
```
```
CARGO (S)
```
```
Líder Soluciones
Transaccionales ACH
```
```
P.O Soluciones
Transaccionales ACH
Director de Producto
```
```
FECHA Enero de 2025 Enero de 2025 Enero de 2025
```
```
Evite el uso de documentos obsoletos.
Recuerde que la última versión aprobada se encuentra en el módulo documental de BSC.
```
```
CONTROL DE CAMBIOS
VERSIÓN FECHA RAZÓN DE LA ACTUALIZACIÓN
```
```
1 a la 2 Enero de 2009
```
```
Se incluye Capítulo 3. Gestión del Riesgo y Seguridad de la Información.
Se actualizan aspectos relacionados con la operación (Parámetros de
seguridad, Tarifas, entre otros).
Se actualizan las denominaciones de cargos y áreas, de acuerdo con la nueva
estructura organizacional de ACH COLOMBIA.
```

### ^

```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 3 2
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Enero del 202 5
Página 3 de 329
```
Información de carácter Confidencial

### CONTROL DE CAMBIOS

### VERSIÓN FECHA RAZÓN DE LA ACTUALIZACIÓN

```
Se actualizan actividades relacionadas con el uso de la herramienta ACHNET
debido a su nueva versión.
```
```
2 a la 3 Noviembre de 2009
```
```
Cambios de forma para alinear el manual con la nueva imagen corporativa de
ACH COLOMBIA.
Se actualizan aspectos relacionados con la operación (Servicios, Procedimientos
de Atención, Tarifas, Horarios, entre otros).
Ajustes en la sección Protección de datos personales, con el objeto de dar mayor
claridad respecto de la aplicación del tema en el servicio ACH.
En el Capítulo 29. Tarifas y comisiones, se especifica que el esquema de tarifas
se actualizará de acuerdo con las decisiones que la Junta Directiva de ACH
COLOMBIA tome al respecto, y que estas tarifas se oficializarán en el acta de
reunión de Junta respectiva. El objetivo del cambio es que el manual no se
desactualice cada vez que se ajuste el esquema tarifario.
En la sección 10.2 Facturación por concepto de reembolso por adquisición de
tokens, se cambió el concepto de la facturación de “Reembolso por
adquisición de tokens” a “Valor correspondiente a tokens asignados”.
```
```
3 a la 4 Diciembre de 2010
```
```
En el ítem 2.7 Manejo de Novedades, se incluye un numeral con las
especificaciones para el manejo de reversiones por Transacciones No
Consentidas y se elimina el punto de Manejo de Reversiones de Transacciones
Débito, debido a que dicho procedimiento no se está aplicando. Se crean los
Anexos 19 y 20 con los documentos generados por ASOBANCARIA para el
manejo de las reversiones por transacciones no consentidas.
Se actualizó la ficha técnica NACHA-M de la Transacción Crédito, incluyendo la
estructura de la Adenda.
Se actualizó el Anexo 5. Detalle de Planilla de Compensación.
Se cambia la estructura del contenido, teniendo en cuenta la orientación por
procesos y se actualizan los numerales mencionados de acuerdo con los cambios
dados a la tabla de contenido.
Se actualiza la plantilla del Manual de acuerdo con los estándares de
documentación de ACH COLOMBIA.
Se cambia la codificación, la versión anterior de este manual es el documento
ACH-OP-MAN-001.
```
```
4 a la 5 Agosto de 2013
```
```
Separación del contenido para la creación del Manual de Servicio ACH.
Actualización cambio razón social proveedores de Tecnología y actualización
gráfica de comunicaciones.
```
```
5 a la 6 Julio de 2014
```
```
De conformidad con la solicitud realizada por parte de su Entidad Financiera en
relación con la Validación de la Identificación para transacciones débito y crédito
en el participante Receptora, en el cual se deben definir de forma expresa dichas
responsabilidades, se realiza modificación en los numerales.
2.11.5. Validación de transacciones débito.
6.1.11.2 Validación en el participante.
```

### ^

```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 3 2
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Enero del 202 5
Página 4 de 329
```
Información de carácter Confidencial

### CONTROL DE CAMBIOS

### VERSIÓN FECHA RAZÓN DE LA ACTUALIZACIÓN

```
6 a la 7 Julio de 2016
```
- En el numeral 2.7.1 Para transacciones crédito, el control en ACH para
    transacciones que exceden los 66.600millones hasta los 200 MM
- Se realiza actualización de Valor en Transacciones Crédito a 23,615.
- En el numeral 2.7.2 Para transacciones debito se actualiza valores de
    transacciones a 56.700.00 millones

```
7 a la 8 Marzo de 2017
```
- Se actualiza el logo de ACH.
- Se adicionan los nuevos códigos de transacciones del manejo de Cuentas de
    Depósito Electrónico.
- Actualización del proceso de creación de los usuarios “Administrador de
    Usuarios” de las Entidades Financieras por parte de Seguridad de la
    Información de AHC Colombia.
- Inclusión de la nueva Causal de devolución de ACH – Transacción no
    Consentida.
- Inclusión de las nuevas causales de Reintegro de acuerdo con el decreto 587
    Estatuto de Protección al Consumidor.
- Inclusión de las devoluciones de pagos Complementarios.
- Actualización del cobro de Comisiones Interbancarias – Servicios y Sanciones a
    través de la compensación.
- Actualización del proceso de vinculación de los usuarios para originar
    transacciones ACH Débito, formato físico o llamada telefónica o por internet.
- Inclusión de “Actividades Prohibidas de un cliente para originar Transacciones
    Débito”.
- Inclusión de “Actividades Restringidas de un cliente para Originar
    transacciones Débito.
- Aclaración de segregación de funciones, El administrador de Usuarios no
    puede ser Administrador de Operación ACH.
- Actualización de los perfiles de Achante para las Entidades Financieras.
- Actualización del cuadro “Actividades Ciclos de Proceso”.
- Actualización del Anexo 2 “Funciones Asociadas al Perfil ACHnet”
- Actualización del Anexo 5 “Planilla de compensación”.
Actualización del Anexo 7 “Tipo de Novedades”.
- Ajuste en la descripción de la causal de rechazo R

```
8 a la 9 Enero de 2018
```
- Se modifica el capítulo 1 adicionando los numerales 1.3 Listado de requisitos
    para el servicio de ACH Transferencias Interbancarias, 1.4 Del procedimiento
    de vinculación, 1.5del procedimiento de exclusión de clientes de ACH
    Colombia S.A., 1.6 procedimiento de exclusión de ACH Colombia.
- Ajuste del manual a las definiciones de “Cliente” y “Usuario”
- Actualización de los numerales 2.1.2.1 límites para transacciones crédito,
    2.1.2.2 límites para transacciones débito
9 a la 10 Abril de 2018 •^ Actualización^ de^ los^ numerales^ 2.4.1^ actividades^ ciclo^ de^ proceso,^ 2.4.^
transacciones a enviar en cada ciclo, con la finalidad de actualizar los plazos


### ^

```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 3 2
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Enero del 202 5
Página 5 de 329
```
Información de carácter Confidencial

### CONTROL DE CAMBIOS

### VERSIÓN FECHA RAZÓN DE LA ACTUALIZACIÓN

```
de envío de rechazos 2 (dos) ciclos después de haber recibido transacción
original
```
- Actualización de la adenda crédito monetaria tipo PPD

```
10 a la 11 Julio de 2018
```
- Actualización de “Actividades Prohibidas de un cliente para originar
    Transacciones Débito”.
- Actualización de “Actividades Restringidas de un cliente para Originar
    transacciones Débito.
- Inclusión de la causal de rechazo R31 Y R32 en el anexo 9. Causales de
    devolución

```
11 a la 12 Mayo de 2019
```
- Actualización del capítulo 2.12 límites de transacciones para el año 2019.
- Actualización del capítulo Actividades con requisitos adicionales para
    vinculación de un Usuario para Originar transacciones Débito
- Se elimino ítem 2.11.2.1 actividades prohibidas y restringidas.
- Actualización de “ANEXO 7. TIPOS DE NOVEDAD Y CAUSALES”
- Inclusión de la causal de rechazo R30 Depósitos Electrónicos
- Actualización del ítem 2.11.5 Actividades a realizar para el proceso débito.

```
12 a la 13 Julio de 2019
```
- Actualización niveles de autorizaciones 2.2.
- Actualización de los perfiles para Entidades financieras 2.2.1. 2
- Actualización de códigos de depósitos electrónicos 6.4.2 registro de detalle de
    transacciones crédito
- Actualización de códigos de depósitos electrónicos 6.5.2 registro de detalle de
    transacciones debito
- Actualización de códigos de depósitos electrónicos 6. 6 .2 registro de detalle de
    transacciones devoluciones de prenotificación crédito debito
- Actualización de códigos de depósitos electrónicos 6.7.2 registro de detalle de
    transacciones devolución por operador
- Actualización anexo 9 causales de rechazos se incluye la aplicación a depósitos
    Electrónicos
- Actualización ANEXO 2. FUNCIONES ASOCIADAS AL PERFIL ACHNET
- Actualización del anexo 8 Factura de venta
- Actualización del anexo 1 Funcionalidades Sistema ACHnet inclusión
    numerales 2.4 Registro Cuentas y 2.5 Autoriza cuentas.
- Ajuste al numeral 2.2.2.4 Políticas de seguridad referenciando el documento
    Instructivo de cuentas de usuarios y contraseñas GIR-GRINS- 010

```
13 a la 14 Enero de 2020
```
- Se actualiza el numeral 4.4 Esquema de Contingencias incluyendo Matriz de
    escalonamiento por contingencia, eliminación del ítem: contingencia en la
    aplicación de archivos y contingencia por el no pago de la compensación.
    (riesgos y continuidad)
- Actualización de definiciones de los participantes en el sistema de ACH
    Colombia: Sistema SEBRA.
- Actualización de terminología: Tarifa o Comisión


### ^

```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 3 2
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Enero del 202 5
Página 6 de 329
```
Información de carácter Confidencial

### CONTROL DE CAMBIOS

### VERSIÓN FECHA RAZÓN DE LA ACTUALIZACIÓN

- Actualización del numeral 2.4.7 incluyendo las características del cobro del uso
    del ciclo 5.
- Actualización del numeral 2.5.6 Contingencia en el sistema SEBRA
- Modificación del numeral 2.9.1 facturación servicio ACH Colombia: como ítem
    de la información solo se deja “servicio proveedor tecnológico, se ajusta el
    concepto de factura de venta,
- Se elimina el numeral 2.9.2.2 factura por concepto de reembolso por
    adquisición de tokens y numeral 2.9.2.3 pago de reembolso a ACH Colombia
- Eliminación del ítem 2.9.3.4 Actualización de Oficinas
- Eliminación del numeral 2.4.8 Horarios y fechas especiales
- Eliminación de la causal de sanción 33 reintegro de intereses por fallas en SOI
    Del anexo 6 eventos sancionables
- Inclusión del numeral 2.8.4 responsable del comité de análisis de reclamos
- Inclusión del numeral 2.12.2 administración de cuentas para montos
    superiores
- Eliminación del numeral 2.4.8 horarios y fechas especiales y del anexo 4 del
    mismo concepto
- Actualización del numeral 2.7 Manejo de novedades
- Actualización del numeral 2.7.3.5 liquidación y pago de reclamos
- Actualización del numeral 2.12 Límites transacciones crédito y débito para el
    2020
- Actualización del numeral 5. Especificaciones Técnicas- requerimientos
    técnicos equipos y sistema operativo.
- Modificación del numeral 2.2.2.2 Autorización de Administradores de
    Usuarios en el Sistema ACHnet
- Modificación del numeral 2.2.2.3 Generación y activación de códigos y claves
- Eliminación del numeral 2.2.2.4 políticas de seguridad uso de claves
- Actualización de la carta circular del numeral 4.2 Seguridad de la información
- Eliminación del numeral 3.2 administración de contraseñas y control de acceso
- Actualización del anexo 1 descripción de las funcionalidades de ACHnet
- Inclusión de la causal de rechazo 37 en el anexo 6 Eventos sancionables del
    esquema de calidad
- Actualización del numeral 2.11.5 adición - Actividades en el participante
    originador

```
14 a la 15 Julio 2020
```
- Eliminación de las causales de sanción: 1, 12, 14, 15, 21 , 23, 25, 29, 34, 35, 36
    del anexo 6 Eventos Sancionables del Esquema de Calidad”
- Inclusión de nuevos términos en el capítulo de “Terminología ACH Colombia”
- Actualización del numeral 2.1.1 Esquema General de Operación
- Inclusión del numeral 2.10.1 esquema general de operación transacción
    crédito
- Inclusión del numeral 2.11.1 esquema general de operación transacción
    débito


### ^

```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 3 2
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Enero del 202 5
Página 7 de 329
```
Información de carácter Confidencial

### CONTROL DE CAMBIOS

### VERSIÓN FECHA RAZÓN DE LA ACTUALIZACIÓN

- Actualización numeral 2.4.1 actividades ciclos de proceso

```
15 a la 16 Enero de 2021
```
- Adicionar el numeral 5.1.5 Certificados Digitales
- Actualización del numeral de Límites 2.12.1 “Para transacciones crédito”
- Actualización del numeral de Límites 2.12.2.1 “Registrar cuenta”
- Actualización del numeral de Límites 2.12.3 “Para transacciones débito”

```
16 a la 17 Mayo de 2021
```
- Actualización numeral 1.3 Listado de requisitos para el SERVICIO ACH
    Transferencias Interbancarias.
- Actualización numeral 1.3.3 Requisitos técnicos.
- Actualización del numeral de Límites 2.12.1 “Para transacciones crédito”
- Actualización numeral 4.4.1.2 Cargue de Archivos Nacha-m (servicio afectado:
    ACH)
- Actualización numeral 4.4.2.2 Contingencia básica.

17 a la (^18) Noviembre de 2021

- Actualización numeral 5.1 requerimientos técnicos.
- Actualización numeral 5.1.1 Equipos y sistema operativo.
- Actualización numeral 5.1.2 Programas.
- Actualización numeral 5.1.3 Configuración.
- Actualización manual de servicio termino Entidades Financieras por Entidad
    Participante.
- Inclusión de numerales 2.5.7 Condiciones de Prefondeo.
- Actualización numeral 1.9 terminología definición Participante/Clientes.
    - Actualización numeral 2.1.1 Esquema general de operación
- Actualización numeral 2.5.7 Condiciones del Sistema de Prefondeo
- Actualización numeral 2.10.1 Esquema general de operación transacción
crédito
- Actualización numeral 2.11.1 Esquema general de operación transacción
débito.
- Inclusión numeral 4.2 Esquema de calidad y control de riesgo
- Actualización numeral 4.3 Seguridad de la información
- Actualización numeral 4.4 protección de datos personales
- Actualización numeral 4.5 esquema de contingencias
- Actualización numeral 6.2 Flujograma de Transacciones
- Actualización numeral 7. Tarifas
- Inclusión Anexo 20 Formulario de Seguridad para Vinculación de Entidades
Participantes
- Inclusión nota aclaratoria numeral 2.12.1 Para transacciones crédito
- Inclusión nota aclaratoria numeral 2.12.3 Para transacciones débito

17 a la (^18) Diciembre 2021

- Actualización numeral 5.1 requerimientos técnicos.
- Actualización numeral 5 .1.5Certificados Digitales
- Inclusión Anexo 21 Guía Generación Mensaje Encriptado.


### ^

```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 3 2
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Enero del 202 5
Página 8 de 329
```
Información de carácter Confidencial

### CONTROL DE CAMBIOS

### VERSIÓN FECHA RAZÓN DE LA ACTUALIZACIÓN

```
18 a la 19 Diciembre 2021 •^ Actualización^ numeral^ 2.12.1^ para^ transacciones^ crédito^
```
- Actualización numeral 2.12.2.1 Registrar cuenta
19 a la 20 Enero de 2022
- Actualización numeral 2.4.2.1 Envío de archivos hacia ACH COLOMBIA
- Actualización numeral 2.5.1 Objetivo de la compensación y liquidación

```
20 a la 21 Febrero de 2022
```
- Actualización numeral 2.4. 7 Transacciones para enviar en cada ciclo
- Inclusión código 29 en Tabla 1. Tabla de Canales de Pago
- Actualización numeral 4.1 Administración del riesgo de lavado de activos y
    financiación del terrorismo.

```
21 a la 22 Mayo de 2022
```
- Actualización numeral 2.1.2.1. para transacciones crédito
- Actualización numeral 2.1.2.3. para transacciones crédito

```
22 a la 23 Junio de 2022
```
- Se actualiza documento con información de Integra ACH
- Se actualiza numeral 1.3.1. Requisitos Legales
- Se reemplazan los Tokens por OTP en todo el documento
- Se reemplaza ACHNET por INTEGRA ACH
- Se actualiza numeral 2.2.1. Niveles de autorizaciones
- Se incluye formato GIR-GRI-FOR- 023 en el numeral 2.2.1.1.
- Se actualiza numeral 2.2.1.2. Administrador de Usuarios ante ACH Colombia
- Se actualiza numeral 2.2.1.3. Perfil Administrador en Integra ACH
- Se actualiza numeral 2.2.1.4. Perfil Operador en Integra ACH
- Se actualiza numeral 2.2.1.5. Perfil Tesorería en Integra ACH
- Se actualiza numeral 2.2.1.7. Perfil Auditor
- Se elimina el perfil registro de cuentas
- Se elimina perfil autoriza cuentas
- Se actualiza el numeral 2.2.2.1. Manejo de novedades
- Se actualiza numeral 2.2.2.3. Generación de código OTP
- Se actualiza numeral 2.4.7. Transacciones para enviar en cada ciclo
- Se elimina numeral 2.9.2. Facturación por concepto de tokens
- Se actualiza numeral 2.12. Límites
- Actualización numeral 2.12.1 Para transacciones crédito
- Actualización numeral 2.12.3 Para transacciones débito
- Se actualiza numeral 4.1. Administración de riesgos de lavado de activos y
    financiación de terrorismo
- Se actualiza numeral 4.3. Seguridad de la Información
- Se actualiza numeral 4.5.1.4. Demora en los cierres de ciclo operacionales de
    ACH
- Se actualiza numeral 4.5.2.3. contingencia externa
- Se actualiza numeral 4.5.3. Contingencia en el proceso de ACH Colombia
- Se actualiza numeral 5.1. Requerimientos técnicos
- Se actualiza numeral 5.2.4. Configuración general
- Se actualiza numeral 5.3. Normas de Seguridad
- Se elimina numeral 7.1. Tarifas por concepto de tokens


### ^

```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 3 2
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Enero del 202 5
Página 9 de 329
```
Información de carácter Confidencial

### CONTROL DE CAMBIOS

### VERSIÓN FECHA RAZÓN DE LA ACTUALIZACIÓN

- Se actualiza Anexo 1. Se referencia Manual funcional Integra ACH para
    entidades financieras
- Se actualiza Anexo 2. Con Matriz de Roles y responsabilidades de Integra ACH
- Se actualiza Anexo 5. Detalles de planilla de compensación
- Se actualiza Anexo 7. Tipo de novedades y causales
- Se actualiza Anexo 9. Causales de devolución
- Se actualiza Anexo 12. Cancelación de autorizaciones de recaudo
- Se actualiza Anexo 14. Vinculación de Entidades Participantes al servicio de
    ACH Transferencias Interbancarias
- Se elimina Anexo 15. Novedad de Tokens de Usuarios ante ACHNET
- Se actualiza Anexo 16. Descripción de Lote
- Se actualiza Anexo 17. Avisos y Mensajes de Error
23 a la 24 Diciembre de 2022
- Se actualiza numeral 2.12.2.1 Registrar Cuentas
- Se actualiza numeral 2.12.2.2 Aprobar Cuentas

24 a la (^25) Enero de 2023

- Actualización numeral 2.12.1 Límites para transacciones crédito
- Actualización numeral 2.12.2.1 Registrar Cuenta
- Actualización del numeral 2.12.2.2 Aprobar Cuenta
- Actualización del numeral 2.12.2.3 Cargue de archivo Nacha-M
- Actualización del numeral 2.12.3 Para transacciones débito

```
25 a la 26 Febrero de 2023
```
- Se elimina del numeral 2.4.7 Transacciones para enviar en cada ciclo; lo
    relacionado con el uso y cobro del ciclo 5
- Se elimina del numeral 2.9.21 Conceptos Involucrados; lo relacionado con
    servicios del cobro del ciclo 5
- Inclusión en el numeral 1.9 Terminología, el concepto de “Sistema externo”
- Inclusión en el numeral 2.4.5 “Pago de compensación”, la opción de pago de
    compensación automático
- Ajuste al Numeral 2.4.1 “Actividades ciclos de proceso” actualizando plazos
    para envió de rechazos débito

26 a la (^27) Abril de 2023

- Inclusión numeral 2.12.1 aprobación de transacciones tipo crédito que
    superan los límites
- Inclusión en el numeral 2.12.2.1. formato Aprobación Cuentas Montos
    Superiores

27 a la (^28) Agosto de 2023

- Ajuste de Límites para transacciones Crédito.
- Se actualiza el numeral 1.3.1. Requisitos Legales
- ANEXO 23. Guía Plazos de solución a Revisiones y Devoluciones

```
28 a la 29 Febrero de 2024
```
- Ajuste numeral 1.3.3.2. Creación nuevas entidades participantes en
    infraestructura tecnológica.
- Ajuste numeral 2.12.3. Montos para transacciones débito
- Ajuste numeral 2.5.7. Condiciones del Sistema de Prefondeo
- Inclusión del numeral 4.5.1.6 Procedimiento para generar excepción de OTP
- Ajuste Anexo 6 para esquema de calidad en sanción 31


### ^

```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 3 2
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Enero del 202 5
Página 10 de 329
```
Información de carácter Confidencial

### CONTROL DE CAMBIOS

### VERSIÓN FECHA RAZÓN DE LA ACTUALIZACIÓN

29 a la (^30) Abril de 2024

- Cambio de horarios de compensación Ciclo 1 y Ciclo 2
- Cambio de horarios de Liberación
- Ajuste en valores monedero-electrónicos
- Inclusión esquema de calidad transacción tipo débito.

```
30 a la 31 Agosto de 2024
```
- Se incluye numeral 5.3.2. Conexión a VPN Internet
- Se ajusta el 2.7.2.2. Tiempo de avances Devolución
- Actualización del numeral 2.12.1. Para transacciones crédito

```
31 a la 32 Enero de 202 5
```
- Se modifica en el registro 5 “Encabezado de Lote”, campo 8 “fecha
    descriptiva” el criterio de inclusión, el cual pasa de ser opcional (O) a
    Mandatorio (M). Para transacciones de: Prenotificación Crédito y Monetarias
    Crédito.
- Ajuste numeral 6.8 Ficha técnica transacción crédito generada por PSE –
    Inclusión del concepto “MULTICREDIT” en el campo descripción de lote en el
    registro tipo 5.
- Ajuste numeral 2.4.7 transacciones para enviar en cada ciclo.
- Ajuste numeral 2.12.3 para transacciones débito.


## ^

### CONFIDENCIAL

Información de carácter Confidencial

TABLA DE CONTENIDO


### ^

### CONFIDENCIAL


### ^

### CONFIDENCIAL


### ^

### CONFIDENCIAL


### ^

### CONFIDENCIAL


### ^

### CONFIDENCIAL


### ^

### CONFIDENCIAL

Administrador de Usuarios: Su labor principal es administrar los requerimientos de creación, modificación,

DOMINIO DE CORREO: _Escribir los dominios de correos de usuarios de la entidad y externos que requieran_

         - VERSIÓN SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
      - Enero del Copyright © ACH COLOMBIA S.A
   - Página 11 de
- ESQUEMA GENERAL TABLA DE CONTENIDO Numeral Página
- MANUAL DE SERVICIO ACH COLOMBIA - ENTIDADES Participantes
- 1. CONCEPTOS GENERALES
- 1.1. Definición general ACH
- 1.2. Participantes del sistema
- 1.3. Listado de requisitos para el SERVICIO ACH Transferencias Interbancarias
- 1.3.1. Requisitos Legales
- 1.3.2. Requisitos Financieros
- 1.3.3. Requisitos Técnicos
- 1.4. Del ProceDIMIENTO de vinculacióN
- 1.5. Del ProceDIMIENTO de exclusión de clientes de ACH Colombia S.A.
- 1.6. ProceDIMIENTO de Exclusión de Clientes de ACH COLOMBIA S.A.
- 1.7. Servicios y aplicaciones
- 1.7.1. Transacciones Tipo Crédito
- 1.7.2. Transacciones Tipo Débito
- 1.7.3. Transferencias de Fondos
- 1.8. Beneficios del sistema
- 1.9. Terminología
- 2. PROCESOS Y OPERACIÓN DEL SERVICIO
- 2.1. Características del Servicio
- 2.1.1. Esquema general de operación
- 2.1.1.1. Relación Usuarios Originadores y Usuarios Receptores
- 2.1.1.2. Relación entre el participante originador y Usuarios Originadores
- 2.1.1.3. Relación entre el participante originador y ACH COLOMBIA
- 2.1.1.4. Relación entre ACH COLOMBIA y Entidades Participantes Receptoras
- 2.1.1.5. Relación entre Entidades Participantes Receptor y los Usuarios Receptores
- 2.1.1.6. Relación entre el Sistema PSE y el sistema ACH
- 2.1.2. Sistema de información de ACH COLOMBIA
- 2.1.3. Manejo de información recibida por ACH COLOMBIA
- 2.1.3.1. Fecha Efectiva
- 2.1.3.2. Modificación de Información
- 2.1.4. Procesamiento en ACH COLOMBIA
- 2.1.4.1. Transacciones Para Procesar
- 2.1.4.1.1. Proceso Principal
- 2.1.4.2. Contingencias
- 2.1.5. Distribución de información desde ACH COLOMBIA
- 2.2. Autorizaciones en ACH COLOMBIA
- 2.2.1. Niveles de autorizaciones
- 2.2.1.1. Representante legal del participante
- 2.2.1.2. Administrador de Usuarios ante ACH COLOMBIA
- 2.2.2. Inclusión de novedades en el sistema INTEGRA ACH
         - VERSIÓN SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
      - Enero del Copyright © ACH COLOMBIA S.A
   - Página 12 de
- 2.2.2.1. Manejo de Novedades Información de carácter Confidencial
- 2.2.2.2. Autorización de Administradores de Usuarios en el Sistema Integra ACH
- 2.2.2.3. Generación de Códigos OTP
- 2.3. Información a los Usuarios
- 2.3.1. En el usuario originador y/o en la EPO
- 2.3.2. En el participante Receptor
- 2.4. Ciclos de Proceso y Horarios
- 2.4.1. Actividades ciclos de proceso
- 2.4.2. Envío de archivos hacia ACH COLOMBIA
- 2.4.3. Cierre de ciclo
- 2.4.4. Entrega de planillas de compensación
- 2.4.5. Pago de compensación
- 2.4.6. Liberación de archivos
- 2.4.7. Transacciones para enviar en cada ciclo
- 2.5. Compensación y Liquidación
- 2.5.1. Objetivo de la compensación y liquidación
- 2.5.2. Manejo cuenta de depósito de ACH COLOMBIA
- 2.5.2.1. Cuenta de Depósito
- 2.5.2.2. Saldo Cero
- 2.5.3. Transacciones para compensar
- 2.5.4. Posición neta del participante
- 2.5.5. Garantía de procesamiento
- 2.5.6. Contingencias en el sistema SEBRA (CUD)
- 2.6. Cuadre Operativo en el participante
- 2.6.1. Objetivo y definición
- 2.6.2. Transacciones y valores
- 2.6.3. Otros conceptos
- 2.7. Manejo de Novedades
- 2.7.1. Concepto y tipos de novedades
- 2.7.1.1. Reclamo
- 2.7.1.2. Solicitud de Certificación
- 2.7.1.3. Reversión
- 2.7.1.4. Devoluciones
- 2.7.1.5. Reintegros
- 2.7.1.6. Devoluciones de Pagos complementarios de seguridad Social
- 2.7.1.7. Procesos Especiales
- 2.7.2. Procedimiento para administrar novedades
- 2.7.2.1. Generación de Novedades y Solución de Casos
- 2.7.2.2. Plazos de Solución
- 2.7.3. Manejo de reclamos
- 2.7.3.1. Responsabilidades de los Participantes
- 2.7.3.2. Condiciones para Solicitar Reclamos
- 2.7.3.3. Novedad de Reclamo – Entidad Participante Originador del caso
- 2.7.3.4. Solución Parcial o Total a Novedad de Reclamos–EPR del Caso
- 2.7.3.5. Liquidación y Pago de Reclamos
         - VERSIÓN SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
      - Enero del Copyright © ACH COLOMBIA S.A
   - Página 13 de
- 2.7.4. Manejo de solicitudes de certificación Información de carácter Confidencial
- 2.7.4.1. Condiciones para Solicitar Certificaciones
- 2.7.4.2. Novedad de Solicitud de Certificación – Entidad Participante Originadora del caso
- 2.7.4.3. Solución Parcial o Total a Novedad de Certificación – EPR del Caso
- 2.7.5. Manejo de reversiones transacciones crédito
- 2.7.5.1. Responsabilidades de los Participantes
- 2.7.5.2. Condiciones para Solicitar Reversiones Crédito
- 2.7.5.3. Novedad de Reversión – Entidad Participante Originadora del caso
- 2.7.5.4. Solución Parcial o Total a Novedad de Reversión – EPR del Caso
- 2.7.5.5. Compensación de Fondos Recuperados por Reversiones
- 2.7.5.6. Cargos a la Cuenta del Cliente Receptor
- 2.7.5.7. Excepciones al Procedimiento de Reversión
- 2.7.6. Manejo de Devoluciones por Transacciones ACH no Consentidas
- 2.7.7. Manejo de Reintegros por Transacciones PSE
- 2.7.8. Manejo de Devoluciones Pagos Complementarios Seguridad Social
- 2.8. Comité de Análisis de Reclamos
- 2.8.1. Definición
- 2.8.2. Objetivo
- 2.8.3. Partícipes
- 2.8.4. Responsable del Comité De Análisis de Reclamos en ACH Colombia
- 2.8.5. Elección de los partícipes
- 2.8.6. Periodicidad de las reuniones
- 2.8.7. Citación a las reuniones
- 2.8.8. Reuniones
- 2.8.9. Metodología de trabajo
- 2.8.10. Actas
- 2.8.11. Informe a las Entidades
- 2.9. Esquema de Facturación
- 2.9.1. Facturación servicio ACH COLOMBIA
- 2.9.1.1. Conceptos Involucrados
- 2.9.1.2. Factura de Venta
- 2.9.1.3. Pago del Servicio ACH COLOMBIA
- 2.9.2. Cobro interbancario
- 2.9.2.1. Conceptos Involucrados
- 2.9.2.2. Soportes por Cobro Interbancario
- 2.9.2.3. Pago de los Cobros Interbancarios
- 2.10. Transacción Crédito
- 2.10.1. Esquema general de operación transacción crédito
- 2.10.2. Flujo de la transacción crédito
- 2.10.3. Uso de la prenotificación crédito
- 2.10.3.1. Control de Prenotificaciones
- 2.10.3.2. Tiempos para iniciar la Prenotificación
- 2.10.4. Transacciones de devolución
- 2.10.4.1. Causales de Devolución
- 2.10.4.2. Plazos para Iniciar una Transacción de Devolución Crédito
         - VERSIÓN SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
      - Enero del Copyright © ACH COLOMBIA S.A
   - Página 14 de
- 2.10.5. Actividades para realizar para el proceso crédito Información de carácter Confidencial
- 2.10.6. Validación de transacciones crédito
- 2.10.7. Ficha técnica para transacciones crédito
- 2.10.8. Consideraciones transacciones crédito recibidas de PSE
- 2.11. Transacción Débito
- 2.11.1. Esquema general de operación transacción débito
- 2.11.2. Proceso de la transacción débito
- 2.11.3. REQUISITOS PARA LA VINCULACIÓN DE USUARIOS ORIGINADORES
- 2.11.4. Uso de la prenotificación débito
- 2.11.4.1. Control de Prenotificaciones
- 2.11.4.2. Plazos para iniciar la Prenotificación
- 2.11.5. Transacciones de devolución
- 2.11.5.1. Devolución de Prenotificaciones Débito y de Transacciones Débito
- 2.11.6. Actividades para realizar para el proceso débito
- 2.11.7. Validación de transacciones débito
- 2.11.8. Novedades
- 2.11.8.1. Orden de no pago
- 2.11.8.2. Cancelación de Autorización de Recaudo
- 2.11.8.3. Modificaciones
- 2.11.9. Reintentos automáticos
- 2.11.10. Pagos parciales
- 2.11.11. Límites
- 2.11.12. Reclamos
- 2.11.13. Ficha técnica para transacciones débito
- 2.12. Límites
- 2.12.1. Para transacciones crédito
- 2.12.2. Administración de Cuentas Para Montos Superiores
- 2.12.2.1. Registrar Cuenta
- 2.12.2.2. Aprobar Cuenta
- 2.12.2.3. Cargue de archivo Nacha-M
- 2.12.3. Para transacciones débito
- 3. ESQUEMA DE CALIDAD
- 3.1. Introducción
- 3.2. Alcance del esquema de calidad
- 3.3. Solución de conflictos
- 3.4. Procedimiento
- 3.4.1. Consolidación de Información de Reclamos
- 3.4.2. Producción de Reportes de Liquidación de Sanciones
- 3.5. Oportunidad y requisitos para oponerse a la sanción
- 3.6. Otros motivos de inconformidad con los reportes de liquidación
- 3.7. Excepciones
- 3.7.1. Situaciones Previsibles
- 3.7.2. Situaciones No Previsibles
- 3.8. Eventos sancionables
- 4. GESTIÓN DEL RIESGO Y SEGURIDAD DE LA INFORMACIÓN
         - VERSIÓN SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
      - Enero del Copyright © ACH COLOMBIA S.A
   - Página 15 de
- 4.1. Administración del riesgo de lavado de activos y financiación del terrorismo (SARLAFT) Información de carácter Confidencial
- 4.2. ESQUEMA DE CALIDAD Y CONTROL DEL RIESGO
- 4.3. Seguridad de la información
- 4.4. Protección de datos personales
- 4.5. Esquema de Contingencias
- 4.5.1. Matriz de Escalonamiento por Contingencia
- 4.5.1.1. Sebra – Inconveniente Para el Acceso al Portal
- 4.5.1.2. Cargue de Archivos Nacha-m (servicio afectado: ACH)
- 4.5.1.3. Descargue de Archivos Nacha-m (servicio afectado: ACH)
- 4.5.1.4. Demora en los cierres de Ciclo Operacionales de ACH
- 4.5.1.5. Problemas de Encripción o Certificados de Archivos Nacha - m
- 4.5.2. Contingencias en el proceso de transmisión
- 4.5.2.1. Lista de Chequeo
- 4.5.2.2. Contingencia Básica
- 4.5.2.3. Contingencia Extrema
- 4.5.3. Contingencias en el proceso de ACH COLOMBIA
- 5. ESPECIFICACIONES TÉCNICAS
- 5.1. Requerimientos Técnicos
- 5.1.1. Equipos y sistema operativo
- 5.1.2. Programas
- 5.1.3. Configuración
- 5.1.4. Contingencia
- 5.1.5. Certificados Digitales
- 5.2. Esquema de Comunicaciones
- 5.2.1. Antecedentes
- 5.2.2. Servicio contratado
- 5.2.3. Beneficios
- 5.2.4. Configuración general
- 5.2.5. Requerimientos de el participante
- 5.2.5.1. Programas
- 5.2.5.2. Equipos y Dispositivos Físicos
- 5.3. MODELOS DE CONECTIVIDAD
- 5.3.1. Conexión Canales Dedicados
- 5.3.2. Conexión VPN
- 5.4. Normas de Seguridad
- 5.4.1. Estación de trabajo, hardware y software
- 5.4.2. Red de comunicaciones
- 5.4.3. Recomendaciones con usuarios originadores y receptores
- 5.4.4. Recomendaciones en el participante
- 5.4.5. Uso de contingencias de comunicaciones
- 6. FORMATO NACHA-M
- 6.1. Generalidades del Formato
- 6.1.1. Antecedentes
- 6.1.2. Estructura de los archivos
- 6.1.3. Secuencia de los registros
         - VERSIÓN SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
      - Enero del Copyright © ACH COLOMBIA S.A
   - Página 16 de
- 6.1.4. Tipos de datos del formato NACHA-M Información de carácter Confidencial
- 6.1.5. Tipos de inclusión de datos
- 6.1.6. Descripción de campos del formato NACHA-M
- 6.1.7. Validaciones de campos del formato NACHA-M
- 6.1.7.1. Rechazo Total de Archivo
- 6.1.7.2. Devolución por Operador
- 6.1.8. Devoluciones por operador
- 6.1.9. Manejo del número de secuencia de transacciones
- 6.1.9.1. Reserva de Rangos de Secuencia de Transacciones PSE
- 6.1.10. Recomendaciones para conformación de archivos NACHA-M
- 6.1.10.1. Para el Nombre del Archivo
- 6.1.10.2. Para el Nombre de Archivos PSE
- 6.1.10.3. Para el Contenido del Formato
- 6.1.10.4. Para el Contenido del Formato NACHA-M PSE
- 6.1.11. Validación de la identificación del cliente receptor
- 6.1.11.1. Envío de la Identificación del Usuario Receptor
- 6.1.11.2. Validación en el participante Receptor
- 6.1.11.3. Validación en Transacciones Crédito
- 6.1.11.4. Validación en Transacciones Débito
- 6.1.12. Cálculo del dígito de chequeo
- 6.2. Flujograma de Transacciones
- 6.3. Ficha Técnica Archivos ACH
- 6.3.1. Consideraciones generales
- 6.4. Ficha Técnica Transacción Crédito
- 6.4.1. Consideraciones generales
- 6.4.2. Requerimientos de formato
- 6.5. Ficha Técnica Transacción Débito
- 6.5.1. Consideraciones generales
- 6.5.2. Requerimientos de formato
- 6.6. Ficha Técnica Transacción Devolución
- 6.6.1. Consideraciones generales
- 6.6.2. Requerimientos de formato
- 6.7. Ficha Técnica Transacción Devolución por Operador
- 6.7.1. Consideraciones generales
- 6.7.2. Requerimientos de formato
- 6.8. Ficha Técnica Transacción Crédito generada por PSE
- 6.8.1. Consideraciones generales
- 6.8.2. Requerimientos de formato
- Para transacciones monetarias crédito generadas por PSE en nombre de las EF
- Para transacciones monetarias crédito generadas por PSE recibidas por EF
- 6.9. Archivo de pagos NACHA-M Seguridad Social
- 6.9.1. Requerimientos de formato
- 6.10. Ficha Técnica NACHA-M (DIAN)
- 7. TARIFAS
- 8. ANEXOS
         - VERSIÓN SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
      - Enero del Copyright © ACH COLOMBIA S.A
   - Página 17 de
- ANEXO 1. FUNCIONALIDADES SISTEMA INTEGRA ACH Información de carácter Confidencial
- ANEXO 2. FUNCIONES ASOCIADAS AL PERFIL INTEGRA ACH
- ANEXO 3. CAUSALES DE DEVOLUCIÓN POR OPERADOR
- ANEXO 5. DETALLE DE PLANILLA DE COMPENSACIÓN
- ANEXO 6. EVENTOS SANCIONABLES DEL ESQUEMA DE CALIDAD
- ANEXO 7. TIPOS DE NOVEDAD Y CAUSALES
- ANEXO 8. FACTURA DE VENTA
- ANEXO 9. CAUSALES DE DEVOLUCIONES - RECHAZOS
- ANEXO 10. DEVOLUCIONES POR SOLICITUD DEL USUARIO RECEPTOR
- ANEXO 11. AUTORIZACIÓN DE RECAUDO
- ANEXO 12. CANCELACIÓN DE AUTORIZACIÓN DE RECAUDO
- ANEXO 13. CONTRATO DÉBITO CLIENTE ORIGINADOR – ENTIDAD FINANCIERA ORIGINADORA
- ANEXO 14. Vinculación Entidades Participantes al servicio de ACH Transferencias Interbancarias (EF)
- FORMATO DE USUARIOS SERVICIO INTEGRA ACH PARA EF
- DATOS DE LA AUTORIZACIÓN
- Entidad Participante. eliminación, bloqueo y desbloqueo de usuarios del sistema Transferencias Interbancarias al interior de su
- DATOS BÁSICOS DEL ADMINISTRADOR DE USUARIOS PRINCIPAL
- DATOS BÁSICOS DEL ADMINISTRADOR DE USUARIOS SUPLENTE
- DATOS BÁSICOS DEL USUARIO FACTURACIÓN – RECLAMOS
- INFORMACIÓN DEL REPRESENTANTE LEGAL
- INFORMACION DE DOMINIO DE CORREO Y SUFIJO DE USUARIOS EN INTEGRA ACH PARA EF
- técnico. acceder al servicio de transferencias interbancarias. Ejm: Funcionarios de la Entidad, contact center, proveedor
- DOMINIO DE CORREO:
- transferencias interbancarias. Ejm: Banco Rojo Sufijo:bancorojo usuario/bancorojo SUFIJO DE USUARIOS: Escribir el sufijo que el usuarios de la entidad requiere para acceder al servicio de
- SUFIJO DE USUARIOS:
- ANEXO 16. DESCRIPCIÓN DE LOTE
- ANEXO 17. AVISOS Y MENSAJES DE ERROR
- ANEXO 18. ACUERDO DE PREVENCIÓN DEL RIESGO OPERATIVO
- ANEXO 19. REGLAMENTO OPERATIVO
- Plazo de Solución de REV - DEV
- ANEXO 20. Formulario de seguridad para vinculación de entidades participantes
- ANEXO 21. Guía Generación Mensaje Encriptado.
- ANEXO 22 FOMATO DE APROBACIÓN CUENTAS MONTOS SUPERIORES
- ANEXO 23 Guía Plazos de solución a Revisiones y Devoluciones


```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 18 de 329
```
## ESQUEMA GENERAL TABLA DE CONTENIDO Numeral Página

```
1.1 Definición general ACH
1.2 Participantes del sistema
1.7 Servicios y aplicaciones
1.8 Beneficios del sistema
1.9 Terminología
```
```
3.1 Introducción
3.2 Alcance del esquema de calidad
3.3 Solución de conflictos
3.4 Procedimiento
```
3.53.6 OportunidadOtros motivos y requisitosde inconformidad para oponerse con los a lareportes sanción de (^)
liquidación
3.7 Excepciones
3.8 Eventos sancionables
2.7 Manejo de Novedades
2.8 Comité de Análisis de Reclamos
2.9 Esquema de Facturación
2.10 Transacción Crédito
2.11 Transacción Débito
2.12 Límites
4.1 Administración del riesgo de lavado de activos y
financiación del terrorismo
4.3 Seguridad de la información
4.4 Protección de datos personales
4.5 Esquema de Contingencias
5.1 Requerimientos Técnicos
5.2 Esquema de Comunicaciones
5.4 Normas de Seguridad
6.1 Generalidades del Formato
6.2 Flujograma de Transacciones
6.3 Ficha Técnica Archivos ACH
6.46.5 FichaFicha TécnicaTécnica TransacciónTransacción CréditoDébito
2.1 Características del Servicio
2.2 Autorizaciones en ACH COLOMBIA
2.3 Información a los
2.4 Ciclos de Proceso y Horarios
2.5 Compensación y Liquidación
2.6 Cuadre Operativo en el participante
6.6 Ficha Técnica Transacción Devolución
6.7 Ficha Técnica Transacción Devolución por Operador
6.8 Ficha Técnica Transacción Crédito generada por PSE
6.96.10 Archivo Ficha Técnicade pagos NACHA NACHA-M-M (DIAN) Seguridad Social

### 3. ESQUEMA

### DE CALIDA.

### ESQUEMA DE

### CALIDAD

### 2. PROCESOS

### Y OPERACIÓN

### DEL SERVICIO

### 1. CONCEPTOS

```
GENERALES
```
### 8. ANEXOS

### 7. TARIFAS

### 6. FORMATO

```
NACHA-M
```
### 5.

```
ESPECIFICACIONES
TÉCNICAS
```
### 4. GESTIÓN DEL

```
RIESGO Y
SEGURIDAD DE LA
INFORMACIÓN
```
ANEXO 1. FUNCIONALIDADES SISTEMA INTEGRA ACH
ANEXO 2. FUNCIONES ASOCIADAS AL PERFIL INTEGRA ACH
ANEXOANEXO 3.4. CAUSALESHORARIOS DEY FECHAS DEVOLUCIÓN ESPECIALES POR OPERADOR
ANEXO 5. DETALLE DE PLANILLA DE COMPENSACIÓN
ANEXO 6. EVENTOS SANCIONABLES DEL ESQUEMA DE CALIDAD
ANEXO 7. TIPOS DE NOVEDAD Y CAUSALES

ANEXOANEXO 8.10. FACTURA DEVOLUCIONES DE VENTA POR (^) SOLICITUD DEL USUARIO RECEPTOR
ANEXO 11. AUTORIZACIÓN DE RECAUDO
ANEXO 12. CANCELACIÓN DE AUTORIZACIÓN DE RECAUDO
ANEXO 13. CONTRATO DÉBITO CLIENTE ORIGINADOR – ENTIDAD
PARTICIPANTE ANEXO 14. AUTORIZACIÓNORIGINADORA DE (^) ADMINISTRADORES DE USUARIOS ANTE
INTEGRA ACH (EF)
ANEXO 16. DESCRIPCIÓN DE LOTE
ANEXO 17. AVISOS Y MENSAJES DE ERROR
ANEXO 18. ACUERDO DE PREVENCIÓN DEL RIESGO OPERATIVO
ANEXOANEXO 19. 20 REGLAMENTOFORMULARIO OPERATIVO DE SEGURIDAD (^) PARA VINCULACIÓN DE
ENTIDADES PARTICIPANTES
ANEXO 21. GUÍA GENERACIÓN MENSAJE ENCRIPTADO.
ANEXO 22. FORMATO APROBACIÓN CUENTAS MONTOS SUPERIORES
ANEXO 23 PLAZOS DE SOLUCIÓN DE REVERSIONES Y DEVOLUCIONES


### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 19 de 329
```
## MANUAL DE SERVICIO ACH COLOMBIA - ENTIDADES Participantes

Este documento establece de manera detallada la forma de operar entre el participante y ACH COLOMBIA, y
las condiciones y términos bajo los cuales se van a prestar los servicios de “Transferencia Interbancaria”.

El Manual de Servicio ACH COLOMBIA – Entidades Participantes hace parte integral del “Contrato Uniforme
para la Transferencia Electrónica de Fondos y Otras Operaciones del Sistema ACH COLOMBIA” como Anexo
No.1, incluyendo sus modificaciones y enmiendas.

La información contenida en este documento es confidencial y propiedad de ACH COLOMBIA. El presente
documento no puede ser reproducido total o parcialmente, sin la debida autorización de ACH COLOMBIA.

Cualquier inquietud sobre el contenido de este manual puede ser consultada a ACH COLOMBIA – Dirección de
Servicio al Cliente, en los teléfonos 5938300 – 7438300 en Bogotá, D.C, Colombia.

### DERECHOS DE AUTOR

Este documento es confidencial de ACH COLOMBIA y está prohibida su reproducción por parte de los
receptores de este documento. Esta información es de tipo confidencial, por lo cual, las entidades que tengan
acceso a este documento están autorizados para distribuirlo exclusivamente al personal autorizado y los
responsables de evaluar el contenido de este documento.


### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 20 de 329
```
## 1. CONCEPTOS GENERALES

## 1.1. Definición general ACH

El sistema ACH (“Automated Clearing House”) es una cámara de compensación automatizada que permite
intercambiar transacciones electrónicas débito y/o crédito, entre las Entidades participantes a través de un
sistema de red centralizado. La Entidad participa en su posible doble condición de Entidad Originadora y/o
Entidad Receptora, lo que le permite la realización de operaciones crédito y/o débito desde o hacia los usuarios
de ACH.

ACH COLOMBIA como cámara de compensación, presta servicios de recepción, validación, clasificación,
proceso, distribución, compensación y liquidación de transacciones electrónicas que comprometen fondos de
terceros, las cuales son procesadas por autorización y bajo la responsabilidad de el participante vinculado.

## 1.2. Participantes del sistema

Se podrán vincular al Sistema de Trasferencias de ACH Colombia todas las Entidades, que ofrezcan en su
portafolio cuentas de depósito y que cuenten con la capacidad tecnológica y operativa de enviar y recibir
transacciones electrónicas a través del sistema ACH COLOMBIA, de acuerdo con las siguientes definiciones:

### PARTICIPANTES EN EL SISTEMA ACH COLOMBIA

### NOMBRE DEFINICIÓN

### ACH COLOMBIA

```
Operador de la cámara de compensación automatizada que se encarga de
recibir, validar, clasificar, distribuir, compensar y liquidar transacciones enviadas
por las Entidades Participantes.
```
```
Usuario Originador (UO)
```
```
Persona natural o jurídica, cliente de una Entidad Participante Originadora que
ha llegado a un acuerdo con ésta para ordenar transacciones hacia o desde su(s)
cuenta(s) a través del sistema ACH COLOMBIA, hacia una o varias cuentas
ubicadas en una o varias Entidades Participantes Receptor.
```
```
Usuario Receptor (UR)
```
```
Persona natural o jurídica, que posee una o varias cuentas en una Entidad
Participante Receptor, que puede recibir transacciones a través del sistema ACH
COLOMBIA.
```
```
Entidad Participante (EP)
Entidad participante autorizada para enviar y/o recibir transacciones
electrónicas a través del sistema ACH COLOMBIA.
Entidad Participante
Originadora (EPO)
```
```
Entidad Participante que envía transacciones electrónicas por mandato de un
Cliente Originador de su Entidad a través del sistema ACH COLOMBIA.
Entidad Participante
Receptor (EPR)
```
```
Entidad Participante que recibe transacciones electrónicas a través del sistema
ACH COLOMBIA, para aplicar a cuentas de sus Clientes Receptores.
```
```
Sistema SEBRA
```
```
Sistema electrónico operado por el Banco de la República, el cual permite a
través del servicio CUD (“Cuenta Única de Depósito”), transferir fondos entre las
Entidades Participantes y ACH COLOMBIA.
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 21 de 329
```
### PARTICIPANTES EN EL SISTEMA ACH COLOMBIA

### NOMBRE DEFINICIÓN

```
Sistema PSE
```
```
El PSE o Proveedor de Servicios Electrónicos es un sistema centralizado y
estandarizado que permite a las Empresas ofrecer a los Usuarios la posibilidad
de realizar pagos en línea, acezando sus recursos desde el participante donde
tienen su dinero y a las Empresas les permite recaudar los fondos en las cuentas
que requieran.
El PSE actúa como Entidad Participante Originadora para el Sistema ACH
COLOMBIA.
```
## 1.3. Listado de requisitos para el SERVICIO ACH Transferencias Interbancarias

Para poder acceder a este servicio los Clientes deberán acreditar los siguientes requisitos:

## 1.3.1. Requisitos Legales

```
− Presentar los documentos que acreditan su existencia y representación legal
− Suscribir la documentación contractual que ACH COLOMBIA defina para la prestación del
servicio
− Suscribir el Acuerdo de Confidencialidad establecido por ACH COLOMBIA.
− Suscribir el Anexo de seguridad y ciberseguridad establecido por ACH COLOMBIA.
− Certificar el cumplimiento de las políticas de los Sistemas de Administración de Riesgo
Operativo (SARO) y sistema de Administración de Lavado de Activos y Financiación del
Terrorismo (SARLAFT) y sistema de control interno (SCI) exigidas por las
Superintendencia Financiera de Colombia, en caso de que ello resulte aplicable. En caso
contrario cumplir con las políticas sobre Seguridad de la Información establecidas por
ACH Colombia y contar con reglas y elevados estándares operativos, técnicos y de
seguridad que permitan el desarrollo de sus operaciones y su participación dentro del
sistema de pago de bajo valor en condiciones de seguridad, transparencia y eficiencia, y
el mantenimiento de sistemas adecuados de administración de los riesgos inherentes a
su actividad y aquellos asociados con su participación dentro del sistema de pago de bajo
valor.
− Las sociedades especializadas en depósitos y pagos electrónicos SEDPES que tengan la
intensión de vincularse al sistema ACH, están habilitadas por el Banco de la Republica a
solicitar la cuenta de depósito CUD.
− Las entidades participantes que no posean cuenta CUD podrán vincularse al sistema
cumpliendo a cabalidad las condiciones previstas para el sistema de Prefondeo. La
información se encuentra expuesta en el apartado 2.5.7 Condiciones del Sistema de
Prefondeo de este manual de servicio.
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 22 de 329
```
## 1.3.2. Requisitos Financieros

```
− Suministrar los estados financieros básicos correspondientes a los dos (2) últimos años
anteriores a la fecha de la solicitud de acceso, debidamente certificados y dictaminados:
Balance General, Estado de Resultados, Estado de Cambios en el Patrimonio, Estado de
cambios en la situación financiera y el Estado de Flujos de Efectivo, para el caso de entidades
con 2 o más años de operación
− En el caso de entidades que tengan menos de dos (2) años de operación, será necesario que
presenten la resolución que impartió su creación y aquella que le dio licencia de operación,
proferidas por la Superintendencia Financiera
− Acreditar que el CLIENTE y sus representantes legales no están reportados ante ninguna
central de riesgos por incumplimiento en el pago de sus obligaciones. Este requisito será
exigible para aquellos CLIENTES que no son objeto de inspección y vigilancia por parte de la
Superintendencia Financiera de Colombia.
− Aportar la documentación que acredite que el Cliente se encuentra al día en sus obligaciones
tributarias
```
## 1.3.3. Requisitos Técnicos

Cumplir con las especificaciones técnicas definidas y requeridas por ACH COLOMBIA estipuladas en el presente
Manual del Servicio ACH Transferencias Interbancarias

```
1.3.3.1 Asignación de códigos para entidades participantes del sistema ACH
```
A continuación, se encuentran las definiciones para la asignación de códigos de las entidades participantes en
la compensación al sistema ACH, Alianzas y SEDPES.

```
− Entidades Participantes al sistema ACH
```
Los códigos asignados para estas entidades son definidos por el Banco de la República, la publicación se
encuentra expuesta en la siguiente URL: https://www.banrep.gov.co/es/sistemas-pago/cenit/codigos-
compensacion

```
− Entidades que operan como Alianzas de Entidades Participantes al sistema ACH
```
Los códigos asignados para estas entidades se definen como se muestra a continuación el cual deben contener
4 dígitos con la siguiente estructura:

Digito 1: Corresponde al número 1 el cual indica que es la ruta para entidades Participantes vinculadas al
sistema ACH.

Digito 2: Corresponde al número 5 el cual indica que es una entidad con convenio y que cuenta con el servicio
de ACH.

Digito 3 y 4: Corresponde a los 2 dígitos asignados a la entidad definidos por el Banco de la República.


### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 23 de 329
```
1.3.3.2 Creación nuevas entidades participantes en infraestructura tecnológica.

ACH COLOMBIA informara con un mes de anticipación al ecosistema el ingreso al servicio de transferencias
interbancarias de nuevas entidades que se encuentran en proceso de vinculación. Es responsabilidad de las
entidades participantes habilitadas en ambientes productivos para el servicio de transferencias interbancarias,
asegurar la creación y configuración en su infraestructura tecnológica de la(s) entidad(s) participante(s)
próximas a ingresar en producción, en todos los canales transaccionales donde dispongan a sus usuarios el
servicio de transferencias interbancarias, con el objetivo de garantizar la prestación del servicio con la totalidad
de entidades vinculadas.

```
− Entidades que no cuentan con código de participante
```
Los códigos asignados para estas entidades se definen con relación al serial definido por el sistema ACH, para
el caso aplica desde el 1801 hasta 1899.

## 1.4. Del ProceDIMIENTO de vinculacióN

Para adquirir la calidad de Cliente de ACH COLOMBIA, cada postulante deberá acreditar el cumplimiento de los
requisitos legales, técnicos y financieros antes mencionados, y allegar los documentos pertinentes, en la forma,
modo y fechas indicados por ACH COLOMBIA.

El Comité de Acceso de la Junta Directiva de ACH COLOMBIA analizará la información de cada postulante y
adoptará una decisión motivada, aprobando o denegando la petición de acceso, la cual deberá adoptarse por
mayoría de los miembros presentes en la respectiva reunión, procurando qué en la deliberación y toma de
decisiones, esté presente el miembro independiente con derecho a voto de la Junta Directiva de ACH
COLOMBIA. La decisión respectiva será comunicada de forma escrita al interesado.

Una vez notificada la decisión de acceso favorable al Cliente, este tendrá un término de dos (2) meses para
realizar su vinculación a los servicios de ACH COLOMBIA con el cumplimiento de los requisitos establecidos
para el servicio requerido. Una vez transcurrido este periodo de tiempo sin que se haya logrado realizar la
vinculación del Cliente, y este continúa requiriendo interés en acceder a los servicios, ACH COLOMBIA podrá
solicitarle adelantar un nuevo trámite de vinculación.

## 1.5. Del ProceDIMIENTO de exclusión de clientes de ACH Colombia S.A.

El Comité de Acceso de la Junta Directiva de ACH COLOMBIA será el encargado de decidir sobre la exclusión de
un Cliente de los Servicios.

La exclusión de un Cliente de los Servicios prestados por ACH COLOMBIA deberá estar motivada en causales
objetivas, transparentes y no discriminatorias. En consecuencia, serán causales de exclusión de Cliente de ACH
COLOMBIA y, por ende, causales de terminación de los correspondientes contratos suscritos, entre otras, las
siguientes:


### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 24 de 329
```
- La inclusión del CLIENTE o sus socios con participación accionaria superior al 5%, en listados de autoridades
internacionales o locales como sospechoso o partícipe de actividades de lavado de activos y/o de financiación
de actividades terroristas, de acuerdo con las disposiciones relativas a la administración del riesgo de lavado
de activos y financiación del terrorismo (SARLAFT).
--La condena a pena privativa de la libertad de cualquiera de sus representantes legales, así como de cualquiera
de los accionistas con participación accionaria superior al 5%, por delitos en los que se haya utilizado a la
sociedad como vehículo para cometer la conducta punible. Se exceptúa la condena por delitos políticos.
- Debido al riesgo sistémico y de contagio que puede propiciar, se considerará de especial gravedad, la
participación en los delitos contra el patrimonio económico reglados en el Título VII del Libro Segundo del
Código Penal, los delitos contra el orden económico y social previstos en el Título X del Libro Segundo del
Código Penal, y los delitos contra la protección de la información y los datos regulados en el Título VII (bis) del
Libro Segundo del Código Penal, sin perjuicio de que pueda aplicarse la exclusión de un Cliente cuyos
administradores y/o accionistas controlantes lo hayan empleado como vehículo para cometer otra clase de
conductas punibles.
- La pérdida de la capacidad del Cliente para hacer frente a sus compromisos contractuales a un costo
razonable, que ponga en entredicho la viabilidad de su negocio (riesgo de liquidez).
- La liquidación o toma de posesión con fines administrativos o liquidatarios del Cliente
- La ejecución de prácticas contrarias a la Constitución, la ley, los reglamentos y las regulaciones aplicables a la
actividad de los Clientes, especialmente en – más no limitándose a– lo que respecta al ordenamiento financiero
y de protección a la libre competencia
- El incumplimiento de los requisitos técnicos, operativos, de seguridad, de ciberseguridad, de control de fraude
u otros que puedan poner en riesgo el sistema o a alguno de sus miembros habiendo otorgado los mecanismos,
y tiempos requeridos para su cumplimiento.
- La compañía realizará un monitoreo anual de sus clientes, en procura de identificar si se encuentran inmersos
en las causales de exclusión consagradas en los numerales anteriores, dando aviso de estas al Comité de Acceso
para que decida acerca de la exclusión o no del cliente.

## 1.6. ProceDIMIENTO de Exclusión de Clientes de ACH COLOMBIA S.A.

El Representante Legal informará por escrito al Cliente respectivo sobre los hechos que indican la posible
configuración de una de las causales de exclusión señaladas en el presente Reglamento y en los contratos. El
Cliente tendrá veinte (20) días hábiles para controvertir los hechos invocados y presentar las pruebas que
considere pertinentes.

Si el Comité de Acceso llegare a desestimar los argumentos y pruebas presentados por el Cliente, y en caso de
que este último insista en su petición, aquél convocará al Comité de Solución de Conflictos de ACH COLOMBIA
para que decida sobre el particular. En caso de que la controversia no pueda ser resuelta a través de los
anteriores mecanismos, se aplicará el proceso de solución de controversias previsto en el respectivo contrato
de servicio.

## 1.7. Servicios y aplicaciones


### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 25 de 329
```
ACH COLOMBIA ofrece un moderno servicio de enlace entre Entidades vinculadas que les permite a los clientes
de éstas, agilizar los pagos, cobros y/o transferencias. Este servicio brinda exactitud, seguridad, comodidad y
rapidez, ya que hay una reducción sustancial en los procesos administrativos y operativos que implican las
labores tradicionales.

Las operaciones financieras se simplifican ya que sólo se requiere darle una instrucción a el participante, con
la cual el usuario, persona natural o jurídica, tiene la cuenta y/o depósitos electrónicos, para que ésta, a través
de ACH COLOMBIA, se encargue de realizar dichas operaciones de forma ágil, oportuna y segura.

ACH COLOMBIA pone en funcionamiento el más revolucionario sistema de enlace electrónico entre las
empresas y los participantes, ofreciendo seguridad, ciberseguridad, eficiencia, productividad y oportunidad.

ACH COLOMBIA es el mecanismo que permite a las Entidades Participantes ofrecer a sus clientes el servicio de
transacciones de pago, recaudo y transferencias de fondos.

## 1.7.1. Transacciones Tipo Crédito

También conocidas como TRANSACCIONES DISPERSIÓN DE FONDOS, que consisten en dispersar fondos desde
una cuenta o deposito electrónico de un cliente de una Entidad Participante hacia una o múltiples cuentas o
depósitos electrónicos de clientes en otras Entidades Participantes. Entre las aplicaciones más comunes para
las transacciones tipo crédito se encuentran las siguientes:

### APLICACIONES CRÉDITO

```
Pago a Proveedores Pago de Intereses
Pago de Cesantías Pago de Nómina
Pago de Comisiones Pago de Pensiones
Pago de Contratistas Pagos PSE
Pago de Dividendos - Acciones Pago de Rendimientos
Pago de Honorarios Pago de Riesgos Profesionales
```
## 1.7.2. Transacciones Tipo Débito

También conocidas como TRANSACCIONES CONCENTRACIÓN DE FONDOS, que consisten en concentrar fondos
provenientes de múltiples cuentas o depósitos electrónicos de los clientes de varias Entidades Participantes en
la cuenta de un cliente de otra Entidad Participante. Las aplicaciones frecuentes para este tipo de transacción
se resumen a continuación:

### APLICACIONES DÉBITO

```
Ahorros Cuotas de Seguros
Cuotas Club Cuotas de Servicios Públicos
Cuotas de Administración Cuotas de TV por Cable
Cuotas de Aportes Cuotas de TV Satelital
Cuotas de Arrendamientos Cuotas Pensiones Universitarias
Cuotas de Cédulas de Capitalización Cuotas Tarjeta de Crédito
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 26 de 329
```
### APLICACIONES DÉBITO

```
Cuotas de Celulares Donaciones
Cuotas de Medicina Prepagada Pago de Impuestos
Cuotas de Pensiones Escolares Recaudos de Cartera
Cuotas de Préstamos Suscripciones
```
Las transacciones tipo crédito y débito pueden ser originadas por personas naturales o jurídicas hacia personas
naturales o jurídicas (transacciones de usuarios y transacciones corporativas) a través de diversos canales que
las Entidades Participantes ofrecen a sus clientes tales como: sistemas de “homebanking”, Internet, oficinas,
terminales directas, cajeros automáticos, sistemas de audio respuesta, cuentas de depósitos electrónicos y
“centro de atención telefónica “, entre otros.

Las transacciones tipo crédito y débito son normalmente pre-acordadas y enviadas periódicamente.

## 1.7.3. Transferencias de Fondos

Las personas naturales o jurídicas pueden trasladar sus recursos desde una entidad participante vinculada,
hacia otra. Esto, no necesariamente es un pago o cobro, sino la realización de movimientos entre cuentas o
depósitos electrónicos previamente establecidas para distribución de recursos y transferencias, entre otros.

## 1.8. Beneficios del sistema

A continuación, se mencionan los principales beneficios que ofrece ACH COLOMBIA a los diferentes
participantes.

### BENEFICIOS DEL SISTEMA

### PARA DESCRIPCIÓN

```
Entidad
Participante
Originadora
```
```
− Ofrecer nuevos servicios a sus clientes personas naturales y jurídicas.
− Lograr la vinculación de nuevos clientes.
− Descongestionar sus oficinas al no tener que efectuar o recibir pagos.
− Obtener mayor competitividad e imagen, tanto en el sector financiero como en el
mercado.
− Percibir mayores ingresos, por las tarifas que les cobre a sus clientes y por el mayor
volumen de negocios que realice con éstos.
− Lograr mayor eficiencia, racionalizar recursos y disminuir tiempos de operación al
reducir trámites, procedimientos y papelería.
− Disminuir costos operativos y administrativos.
− Conservar y ampliar los convenios actuales con sus clientes.
− Fortalecer las relaciones con sus clientes, al ampliar la gama de servicios y su
cobertura
− Facilitar las operaciones de sus clientes, al evitarles incomodidades y desplazamientos
innecesarios.
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 27 de 329
```
### BENEFICIOS DEL SISTEMA

### PARA DESCRIPCIÓN

```
− Obtener mayor rendimiento y productividad en sus sucursales y por consiguiente no
tener que abrir nuevas oficinas.
```
Entidad
Participante
Receptor

```
− Ofrecer nuevos servicios a sus clientes personas naturales y jurídicas.
− Vincular nuevos clientes.
− Descongestionar sus oficinas, al no tener que realizar o recibir pagos.
− Obtener mayor competitividad e imagen, tanto en el mercado como en el sector
financiero.
− Percibir ingresos adicionales, por las tarifas de acceso a la red que recibe de otras
entidades participantes.
− Lograr mayor eficiencia, racionalizar recursos, y disminuir tiempos de operación al
reducir los trámites, procedimientos y papelería.
− Disminuir costos operativos y administrativos.
− Fortalecer las relaciones con sus clientes, al ampliar la gama de servicios y su
cobertura.
− Facilitar las operaciones de sus clientes, al evitarles incomodidades y desplazamientos
innecesarios.
− Obtener mayor rendimiento y productividad en sus sucursales y por consiguiente no
tener que abrir nuevas oficinas.
```
Empresas

```
− Eliminación de múltiples convenios.
− Disminución de procedimientos.
− Racionalización de recursos.
− Facilidad y oportunidad en el manejo de cobros/pagos.
− Descongestión de oficinas.
− Mejor proyección del flujo de efectivo.
− Certeza en el procesamiento de transacciones.
− Reducción de costos de administración y operación.
```
Personas
Naturales

```
Para las transacciones tipo débito:
− Facilidad en los pagos, ya que solo es necesario la primera autorización.
− Puntualidad en los pagos, reduciendo la posibilidad de quedar en mora.
− Mayor seguridad y comodidad en los pagos, al evitar desplazamientos innecesarios,
así como el porte de efectivo.
− Reducción de costos, al disminuir la elaboración de cheques.
Para las transacciones tipo crédito:
− Disminución de tiempo y de costos relacionado con el recibo y depósito de cheques.
− Disponibilidad inmediata de fondos.
− Mayor conveniencia por evitar desplazamientos.
− Seguridad al no tener que llevar cheques o dinero en efectivo.
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 28 de 329
```
## 1.9. Terminología

Para la adecuada interpretación de este documento, cuando sean utilizados los siguientes términos, ya sea en
singular o en plural, deben entenderse de acuerdo con las definiciones que se presentan a continuación:

### TÉRMINO DEFINICIÓN

```
Ciclo de Operación
```
```
Período de tiempo requerido para realizar los procesos de recepción, validación,
clasificación y distribución de transacciones. Puede haber más de un Ciclo de Operación
durante un día hábil y como mínimo uno.
```
```
Cierre de Ciclo
Proceso que realiza el sistema ACH COLOMBIA para los archivos de transacciones
recibidos de las Entidades Participantes dentro del horario establecido.
```
```
Cierre de Día
Proceso final, que el sistema ACH COLOMBIA realiza para terminar las tareas del día y
preparar el sistema para iniciar un nuevo día de proceso.
```
```
Compensación
Conjunto de actividades que realiza ACH COLOMBIA, que le permiten obtener la Posición
Neta de cada Entidad Participante al final de un Ciclo de Operación.
```
```
Participante/Cliente
```
```
Es quien haya sido autorizado por la entidad administradora del sistema de pago de bajo
valor para tramitar órdenes de pago o transferencia de fondos en su sistema. Los
participantes podrán ser entidades vigiladas y no vigiladas por la Superintendencia
Financiera de Colombia.
```
```
Usuario
```
```
Persona natural o jurídica, de naturaleza pública o privada, que tiene un vínculo
contractual con alguno o varios de los CLIENTES, y que, en virtud de dicho vínculo, el
CLIENTE le permite acceder a los Servicios de ACH COLOMBIA, a través de sus diferentes
canales.
```
```
Contrato con Cliente
```
```
Acuerdo suscrito entre el Usuario Originador y la Entidad Participante Originadora, en el
cual se definen las características y condiciones para el envío de transacciones
ordenadas por el Usuario Originador para ser enviadas a través del sistema ACH
COLOMBIA.
```
```
Cuenta Originadora Cuenta^ desde^ la^ cual,^ el^ Usuario^ Originador^ ha^ ordenado^ a^ su^ Entidad^ Participante^
Originadora generar transacciones electrónicas a través del sistema ACH COLOMBIA.
```
```
Cuenta Receptora
Cuenta del Usuario Receptor que recibe transacciones electrónicas a través del sistema
ACH COLOMBIA, originadas desde la Entidad Participante Originadora.
```
```
CUD Cuenta^ única^ de^ depósito,^ es^ el^ sistema^ de^ pagos^ de^ alto^ valor^ del^ país^ administrado^ y^
operado por el Banco de la República
```
### OTP

```
OTP (One Time Password) Código temporal que llega al correo registrado en la creación
del usuario de la Entidad financiera, para que de manera segura pueda ingresar al
sistema.
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 29 de 329
```
### TÉRMINO DEFINICIÓN

Depósitos Electrónicos
Corresponde al depósito trasferible de captación a la vista desde el cual se puede recibir
y/o enviar transferencias interbancarias

Devolución

```
Transacción mediante la cual, la Entidad Participante Receptor informa a la Entidad
Participante Originadora a través del sistema ACH COLOMBIA, que la transacción no
fue aceptada por no cumplir con las condiciones establecidas o porque no fue
aceptada por el Usuario Receptor.
```
Devolución por
Operador

```
Transacción que no fue aceptada por el sistema ACH COLOMBIA, por no cumplir con
las condiciones establecidas.
```
Día Hábil Bancario

```
Día hábil de atención bancaria al público en Bogotá D.C., de acuerdo con lo establecido
por la Superintendencia Financiera. Se excluyen los sábados, domingos, festivos y
horarios extendidos.
```
Fecha de
Compensación

```
Fecha en la cual, las Entidades Participantes deben trasladar o recibir los fondos
correspondientes a las transacciones cursadas a través del sistema ACH COLOMBIA.
```
Fecha Efectiva

```
Fecha en la cual la Entidad Participante Receptora debe aplicar y contabilizar la
transacción en la Cuenta Receptora. Por las condiciones del proceso actual, la Fecha
Efectiva coincide con la Fecha de Proceso, por lo que ambos términos se denominan
Fecha Efectiva.
```
Formato NACHA-M

```
Información de pagos y cobros organizada de forma estándar en un archivo de datos,
que es intercambiado entre ACH COLOMBIA y las Entidades Participantes. ACH
COLOMBIA utiliza el formato estándar NACHA, emitido por la Asociación Nacional de
ACH ́s en Estados Unidos, con algunas variaciones realizadas para nuestro medio, por lo
que se denomina NACHA-M.
```
Horario de Recibo de
Archivos

```
Período de tiempo desde y hasta el cual el sistema ACH COLOMBIA recibe archivos de
las Entidades Participantes antes del Cierre de Ciclo.
```
Liquidación

```
Conjunto de actividades que, conociendo la Posición Neta de cada Entidad participante
como consecuencia de las Transacciones enviadas y/o recibidas por ella, le permiten a
ACH COLOMBIA efectuar el pago y cobro correspondientes.
```
Posición Neta

```
Balance financiero que genera ACH COLOMBIA al final de un Ciclo de Operación para
cada Entidad Participante como consecuencia del intercambio de transacciones entre
las Entidades Participantes.
```
Pre-notificación

```
Transacción no monetaria mediante la cual el Usuario Originador ordena a su Entidad
Participante Originadora a través del sistema ACH COLOMBIA, obtener una validación
acerca de la existencia y condiciones de la Cuenta Receptora en la Entidad Participante
Receptor.
```
Rechazo de Archivo Archivo^ que^ no^ pudo^ ser^ procesado^ por^ el^ sistema^ ACH^ COLOMBIA,^ al^ detectar^ errores^
de formato.


### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 30 de 329
```
### TÉRMINO DEFINICIÓN

Reversión

```
Transacción mediante la cual, la Entidad Participante Originadora, el Usuario
Originador o ACH COLOMBIA, envía a la Entidad Participante Receptor, a través de la
página del facturador de ACH COLOMBIA, la solicitud de corrección a una transacción
débito o crédito, con el propósito de deshacer una transacción que fue realizada
previamente por error de la Entidad Participante Originadora, el Cliente Originador o
ACH COLOMBIA.
```
Tarifa o Comisión

```
Suma de dinero que la Entidad Participante está obligada a pagar a ACH COLOMBIA o a
las demás Entidades participantes como contraprestación por los servicios prestados
bajo este contrato.
```
Transacción Crédito

```
Transacción monetaria realizada a través de ACH COLOMBIA en la cual el Usuario
Originador ordena a su Entidad Participante Originadora debitar la Cuenta Originadora
para acreditar la Cuenta Receptora.
```
Transacción Débito

```
Transacción monetaria realizada a través del sistema ACH COLOMBIA mediante la cual
el Usuario Originador ordena a su Entidad Participante Originadora generar una
transacción hacia la Entidad Participante Receptor con el objeto de que ésta debite
una suma determinada de la Cuenta Receptora, para acreditarla a la Cuenta
Originadora. La transacción débito permite al titular de la Cuenta Receptora realizar
pagos al Usuario Originador.
```
Sistema Externo

```
Es cualquier sistema de compensación y liquidación de operaciones sobre valores,
sistemas de compensación y liquidación de divisas, sistema de compensación y
liquidación de futuros, opciones y otros activos financieros, cámara de riesgo central
de contraparte o Sistema de Pagos diferente del CUD, debidamente autorizado por la
autoridad competente para operar en Colombia
```
### VPN

```
Una VPN (Red Privada Virtual) es una herramienta que permite conectarse a internet de
manera segura y privada. Funciona creando un “túnel” protegido que oculta lo que el
usuario ejecuta en línea, manteniendo los datos y actividades salvaguardados de riesgos
de seguridad
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 31 de 329
```
## 2. PROCESOS Y OPERACIÓN DEL SERVICIO

## 2.1. Características del Servicio

## 2.1.1. Esquema general de operación

El presente Manual de Servicio norma la relación entre las Entidades participantes actuando como el
participante originador y/o como Entidades Participantes Receptoras. Sin embargo, los participantes en el
Esquema General de Operación de ACH COLOMBIA y sus interrelaciones se describen a continuación.

## 2.1.1.1. Relación Usuarios Originadores y Usuarios Receptores

```
Las empresas o personas naturales (Usuarios Originadores) que requieren efectuar transferencias,
pagos o cobros a sus proveedores, clientes o usuarios (Usuarios Receptores) con los cuales tienen
una relación comercial o similar, y quienes tienen cuentas corrientes, de ahorros o depósitos
electrónicos en diversas Entidades Participantes, utilizan el sistema de ACH COLOMBIA.
Para ello, obtienen la información financiera de sus usuarios tal como número y tipo de cuenta
entidad Participante donde tiene la cuenta y la identificación, entre otros.
Dicha información es convertida a órdenes de pago o cobro (transacciones) por los Usuarios
Originadores al medio y formato indicado por el participante donde tienen sus cuentas (Entidad
Participante Originadora). Si alguna de las transacciones no se hace efectiva, el Usuario Originador
contacta al Usuario Receptor para corregir y reenviar la información en una nueva transacción, si es
del caso.
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A

```
Agosto de 2024
Página 32 de 329
```
## 2.1.1.2. Relación entre el participante originador y Usuarios Originadores

```
La Entidad Participante Originadora realiza acuerdos con sus Usuarios Originadores y establece las
condiciones legales, técnicas, operativas y comerciales que éstos deben seguir para procesar las
órdenes de pago o cobro requeridas.
```
```
el participante originador recibe, valida y almacena la información enviada por sus clientes, y separa
aquellas transacciones cuyo destino es la misma Entidad Participante Originadora (transacciones
propias) de las que están dirigidas a otras Entidades Participante (transacciones no propias).
```
```
Posteriormente, el participante originador convierte aquellas transacciones cuyo destino son otras
Entidades Participante (Entidades Participantes Receptoras) vinculadas al sistema ACH COLOMBIA y
prepara estas órdenes de pago o cobro en el formato indicado por ACH COLOMBIA. Si culminado el
ciclo, alguna de las transacciones no se hace efectiva, el participante originador contacta al Usuario
Originador para que éste revise la causa y reenvíe la información, si es del caso, informándole las
razones o causales que se dieron en el proceso.
```
## 2.1.1.3. Relación entre el participante originador y ACH COLOMBIA

```
Una vez el participante originador ha preparado la información, envía las transacciones a ACH
COLOMBIA en los tiempos y condiciones establecidas en el presente Manual de Servicio, el cual hace
parte integral del acuerdo que suscribe ACH COLOMBIA con todos los participantes vinculados. ACH
COLOMBIA recibe y valida las transacciones enviadas por el participante originador por Ciclos de
Operación en horarios específicos, devolviendo parcial o totalmente el archivo de transacciones si se
encuentra error, o procesando las transacciones si están correctas de acuerdo con la estructura del
archivo.
```
```
Los archivos que tengan las transacciones con la estructura correcta serán procesados en el sistema
por ACH Colombia, de acuerdo con la fecha requerida por la Entidad Participante Originadora,
preparando en el formato estándar, las transacciones que correspondan a cada una de las Entidades
Participantes Receptoras, para el posterior envío.
```
```
Así mismo, ACH COLOMBIA calcula los valores de compensación para cada Entidad Participante,
generando una Posición Neta que implica que el participante quede a favor o en contra del sistema,
dependiendo de las transacciones enviadas y/o recibidas durante los diferentes Ciclos de Operación.
La liquidación de dichos valores se paga y cobra a través del sistema SEBRA del Banco de la República.
```
```
ACH COLOMBIA efectúa la liquidación correspondiente a las tarifas de acceso a la red, servicios y
sanciones para cada Entidad Participante Originadora.
```
## 2.1.1.4. Relación entre ACH COLOMBIA y Entidades Participantes Receptoras

```
Una vez ACH COLOMBIA ha recibido los valores de compensación de las Entidades Participantes cuya
Posición Neta es en contra, procede a distribuir los archivos de transacciones en el formato, medio y
tiempos definidos y a efectuar el pago a las Entidades Participantes con saldo a favor.
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A

```
Agosto de 2024
Página 33 de 329
```
```
Cada Entidad Participante Receptor debe aplicar a su sistema de cuentas, los archivos de
transacciones recibidos en los horarios indicados.
```
```
Si al aplicar alguna de las transacciones recibidas, encuentra alguna condición que lo impida, el
participante Receptor genera las respuestas correspondientes en los plazos definidos, (devoluciones)
indicando la causal. Estas devoluciones son procesadas por ACH COLOMBIA y entregadas al
participante originador en los horarios definidos para que ésta a su vez le informe a sus Clientes
Originadores.
```
```
ACH COLOMBIA efectúa la liquidación correspondiente a las tarifas de acceso a la red, servicios y
sanciones para cada Entidad Participante Receptor.
```
## 2.1.1.5. Relación entre Entidades Participantes Receptor y los Usuarios Receptores

```
La Entidad Participante Receptor debe establecer contacto con los Usuarios Receptores para notificar
e informar el detalle de los movimientos sobre sus cuentas o para atender sus inquietudes o
reclamaciones.
```
## 2.1.1.6. Relación entre el Sistema PSE y el sistema ACH

```
El sistema PSE (Proveedor de Servicios Electrónicos) actúa como Entidad Originadora dentro del
sistema ACH. Provee las transacciones tipo crédito con destino a las cuentas de las Empresas que
ofrecen el servicio PSE, en las Entidades Participantes Recaudadoras. Estas transacciones son el
resultado de las compras o pagos realizados a través del sistema PSE, distribuidos de acuerdo con la
necesidad de las Empresas.
```
```
➢ Responsabilidad de los Usuarios
```
```
La responsabilidad de los usuarios del sistema consiste en utilizar las claves que el participante
le habilita, con responsabilidad, confidencialidad, preservando su carácter privado. Es
responsabilidad del usuario el control de las claves de acceso, el ingreso a las páginas del banco
desde computadores seguros y en general de seguir las instrucciones de seguridad de la
información que le indique el participante que le permite el uso del servicio de PSE. Así mismo
los usuarios del servicio PSE, deben contar con conexiones seguras entre el participante y la
página web de su propiedad, en caso de tratarse de recaudo. Igualmente deben advertir a sus
clientes la necesidad de cumplir con normas de seguridad en el uso del servicio PSE, de
conformidad con lo indicado en la definición anterior. El propietario de la información en el
presente caso es el usuario del servicio, quien a través del participante con quien tenga
habilitado el servicio de PSE, suministra la información necesaria que le permita realizar las
transacciones. En consecuencia, el uso de la información y la entrega de esta es responsabilidad
del usuario. Así mismo, el participante se responsabiliza por la información y su manejo una vez
el usuario la entrega para el desarrollo del servicio PSE.
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 34 de 329
```
## 2.1.2. Sistema de información de ACH COLOMBIA

ACH COLOMBIA provee a las Entidades Participantes el sistema de información denominado Integra ACH que
es el único canal de contacto entre ACH COLOMBIA y las Entidades Participantes vinculadas al servicio ACH,
para el intercambio de transferencias electrónicas.

Las Entidades Participantes son responsables por el uso de la información que hagan a través de Integra ACH.
Se deja constancia que el participante es el dueño de dicha información y por lo tanto cada participante podrá
consultar la información referente a sus clientes. No existirá en este sentido reserva bancaria que se pueda
oponer a los bancos respecto de la información de cada banco. De igual manera, ACH COLOMBIA proporciona
las claves de accesos a la página administrativa, pero es responsabilidad del participante el uso de las claves
de acceso.

El sistema Integra ACH aprovecha la tecnología basada en redes de comunicación, lo que facilita el manejo por
parte de los usuarios, en lo que se ha denominado “extranet”. Las principales funciones del sistema Integra
ACH, incluyendo Operación Diaria, Consultas, Información General y administración entre otras, se encuentran
en el Anexo 1: Funcionalidades Sistema Integra ACH.

Para que los usuarios puedan llevar a cabo estas operaciones deben contar con un perfil que determinará su
acceso a cada una de estas funciones (Ver Anexo 2: Funciones Asociadas al Perfil Integra ACH).

Todo envío de información de transacciones hacia y desde ACH COLOMBIA, se hace mediante un canal
dedicado o conexión VPN de comunicaciones utilizando el formato estándar NACHA-M, a través del sistema
Integra ACH. En caso de fallas en el proceso de envío, se debe seguir el procedimiento de contingencias
definido en el numeral 4.5. Esquema de Contingencias de este manual.

Durante el proceso de envío de archivos desde las Entidades Participantes hacia ACH COLOMBIA a través del
sistema Integra ACH, se realizan validaciones específicas de forma y contenido del formato NACHA-M.

```
CONTROLES DEL SISTEMA
TIPO DESCRIPCIÓN
Usuarios y
Perfiles
```
```
Verifica que el usuario esté habilitado en el sistema y les brinda acceso a las funciones
asociadas a su perfil.
```
```
Formato
```
```
Valida el nombre y la estructura del archivo, así como el tipo de campo y los
requerimientos de inclusión de este (opcional, mandatorio, requerido). En caso de error
en el formato, el archivo es devuelto totalmente a la entidad.
```
```
Condiciones
Especiales
```
```
Se revisa el contenido de algunos campos específicos, para que se ajusten a las condiciones
establecidas por ACH COLOMBIA, tales como límites por transacción, lotes duplicados en
el mismo día, tipo de transacciones habilitadas, Fechas Efectivas y códigos de Entidades
Participantes usuarias. En caso de no cumplirse alguna condición, el sistema genera
Devoluciones por Operador (Ver Anexo 3: Causales de Devoluciones por Operador), de
acuerdo con lo especificado en el capítulo 6 FORMATO NACHA-M de este manual o
solicita la autorización del Administrador de Integra ACH del participante, según sea el
caso.
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 35 de 329
```
## 2.1.3. Manejo de información recibida por ACH COLOMBIA

## 2.1.3.1. Fecha Efectiva

```
ACH COLOMBIA recibe archivos de transacciones agrupadas en lotes, cada uno de los cuales
determina la fecha en que debe ser aplicado, es decir la Fecha Efectiva de las transacciones
contenidas en él. ACH COLOMBIA no recibe lotes de transacciones cuya Fecha Efectiva sea menor a
la fecha del día en que son recibidas para proceso. Solo se procesan lotes de transacciones cuya
Fecha Efectiva sea la del día de proceso.
```
```
Si la Fecha Efectiva corresponde a un día no hábil, o a una fecha menor a la fecha de proceso, el
sistema Integra ACH genera una Devolución por Operador (Ver Anexo 3: Causales de Devoluciones
por Operador), de acuerdo con lo establecido en el capítulo 6 FORMATO NACHA-M, numeral 6.1 y
en el numeral 6.7 Ficha Técnica Transacción Devolución por Operador de este manual.
```
## 2.1.3.2. Modificación de Información

```
ACH COLOMBIA no puede cambiar la información original de una transacción y es responsabilidad
de las Entidades Participantes enviar de forma correcta y a tiempo las transacciones y lotes para ser
procesados. En consecuencia, los archivos enviados por el participante originador no pueden ser
modificados por ACH COLOMBIA.
```
## 2.1.4. Procesamiento en ACH COLOMBIA

## 2.1.4.1. Transacciones Para Procesar

```
ACH COLOMBIA procesa únicamente las transacciones que hayan cumplido con el formato NACHA-
M, y con las validaciones y controles establecidos por ACH COLOMBIA. Las Devoluciones por
Operador no son procesadas, ya que corresponden a transacciones que no fueron aceptadas por el
sistema ACH por no cumplir con las condiciones establecidas, y, por lo tanto, el participante Receptor
no recibe las transacciones objeto de la devolución.
```
```
La Entidad participante Originadora debe verificar la razón de la Devoluciones por Operador y hacer
las correcciones necesarias antes de enviar nuevamente la transacción.
```
## 2.1.4.1.1. Proceso Principal

```
Una vez recibida la información correctamente desde las Entidades Participantes y de PSE, el sistema
de ACH realiza el procesamiento de archivos, el cual consiste en clasificar las transacciones según el
código de el participante y preparar un archivo en el formato NACHA-M para cada Entidad
Participante Receptor con las transacciones que le correspondan.
```
```
De igual manera, el sistema de ACH calcula la Posición Neta de cada Entidad Participante frente al
sistema, es decir realiza el proceso de Compensación. El proceso de Liquidación o pago de las
Posiciones Netas es realizado en los horarios estipulados en el numeral 2.4. Ciclos de Proceso y
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 36 de 329
```
```
Horarios, según lo establecido en el numeral 2.5. Compensación y Liquidación.
```
```
El procesamiento de transacciones es realizado por ACH COLOMBIA utilizando el sistema central de
proceso que genera los archivos en el formato NACHA-M, clasificados por Entidad Participante. Estos
archivos son dejados a disposición de cada Entidad Participante por el sistema Integra ACH.
```
```
Este proceso es realizado por ACH COLOMBIA, tantas veces como Ciclos de Proceso se definan en el
numeral 2.4. Ciclos de Proceso y Horarios, para un día.
```
## 2.1.4.2. Contingencias

```
En caso de fallas en el sistema de procesamiento en ACH COLOMBIA, se debe aplicar el Esquema de
Contingencias definido en el numeral 4.5 Esquema de Contingencias.
```
## 2.1.5. Distribución de información desde ACH COLOMBIA

A través del sistema Integra ACH, ACH COLOMBIA deja disponible en un buzón privado para cada Entidad
Participante, los archivos de transacciones Nacha-m y los archivos generados en el proceso diario tales como
los archivos de Devoluciones por Operador.

Cada buzón es de uso exclusivo del participante, y solamente podrá ser consultado por los usuarios autorizados
y registrados en el Sistema Integra ACH para dicha entidad, de acuerdo con los perfiles y autorizaciones
definidos en el numeral 2.2. Autorizaciones en ACH COLOMBIA. La Entidad Participante debe recoger del
buzón, los archivos generados por ACH COLOMBIA por lo menos al finalizar cada Ciclo de Operación.

## 2.2. Autorizaciones en ACH COLOMBIA

## 2.2.1. Niveles de autorizaciones

A continuación, se presenta el esquema de manejo de autorizaciones en ACH, que ilustra los diferentes niveles
de perfiles de el participante en Integra ACH. El Administrador de Usuarios debe ser autorizado ante ACH
COLOMBIA por parte del Representante Legal de el participante, y tiene la función de crear y administrar los
perfiles Administrador, Operador, Tesorería, Consultas y Auditor al interior del participante.

Los siguientes son los roles dispuestos para los usuarios de Entidades Financieras que tiene el servicio de ACH
Transferencias Interbancarias y PSE, para ser asignados por el administrador de Usuarios de la Entidad


### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 37 de 329
```
Los siguientes son los roles dispuestos para los usuarios de Entidades Financieras que tiene el servicio PSE,
para ser asignados por el administrador de Usuarios de la Entidad

## 2.2.1.1. Representante legal del participante

```
Su función principal es autorizar a los funcionarios designados por el participante como
Administradores de Usuarios en ACH COLOMBIA principal y suplente, quienes administraran los
perfiles de los usuarios de su Entidad Participante que manejan los procesos relacionados con Integra
ACH. Las firmas de estos funcionarios son las únicas que tendrán validez para ACH COLOMBIA.
```
```
En caso de existir algún cambio en cualquier de los niveles principales, en el participante, como en el
Representante Legal o en el Administrador de Usuarios, el participante debe usar el formato ilustrado
en el Anexo 14: Vinculación Entidades Financieras al servicio de ACH Transferencias Interbancarias
(Entidad Financiera) GCL-VEV-FOR- 095 y el formato GIR-GRI - FOR- 023 cuando se requiera realizar el
cambio de administrador.
```
## 2.2.1.2. Administrador de Usuarios ante ACH COLOMBIA

```
Autorizado por el Representante Legal, su labor principal es crear y administrar los usuarios en el
sistema Integra ACH. Dado que es este sistema el contacto principal y permanente del participante
con ACH COLOMBIA.
```
```
− Su función más importante consiste en realizar la inclusión de novedades de usuarios para el
Sistema de Información Integra ACH, tales como creación, eliminación, modificación, bloqueo,
activación de usuarios y consulta de usuarios, los cuales pueden tener perfil de Administrador
Integra ACH, Operador, Tesorería, Consultas y Auditoría.
− El administrador de usuarios por políticas de segregación de funciones no deberá ser al mismo
tiempo administrador; control que debe ser ejecutado desde el Administrador de usuarios.
− El Administrador de Usuarios principal y/o suplente pueden consultar periódicamente la
```
```
Representante legal de
la entidad
```
```
Administrador
(AdminApprover)
```
```
Operador
(AdminOperator)
```
```
Tesorería
(TreasuryOperator)
```
```
Auditor
(AditorOperator)
```
```
Consultas
(QueriesOperator)
```
```
Administrador de Usuarios
(UserAdminoperator)
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 38 de 329
```
```
información de los usuarios activos en el sistema de información Integra ACH para su control.
```
```
Los perfiles en el sistema Integra ACH y las correspondientes novedades que se generen para la
asignación de usuarios, deben ser gestionadas por el Administrador de Usuarios, de acuerdo con las
funciones asociadas a cada perfil (Para ampliar información Ver Anexo 2: Funciones Asociadas al Perfil
Integra ACH).
```
```
2.2.1.3. Perfil Administrador en Integra ACH
```
```
− Como su nombre lo indica, su función consiste en administrar la operación diaria de ACH
COLOMBIA. El Administrador de Integra ACH de el participante tiene la responsabilidad de
autorizar las transacciones crédito que superen el límite definido y los lotes duplicados en el
mismo día, detectados por el sistema en el momento que el operador intenta enviar un archivo
con estas condiciones.
− A este perfil se encuentran asociada la funcionalidad de aprobar y consultar las cuentas
receptoras hacia las cuales se envía transacciones que superan los límites transaccionales por
cuenta, las cuales fueron registradas por el perfil de Registro de cuentas y log de transacciones
autorizadas EF
− Este perfil puede realizar eliminación de archivos y transacciones antes de cierre de ciclo y
realizar diferentes consultas de la operación.
−
```
```
2.2.1.4. Perfil Operador en Integra ACH
```
```
Responsable de realizar los procesos diarios de cargue de archivos de información para realizar la
compensación con ACH COLOMBIA y realizar consultas de planillas de compensación, pago de
comisiones y reversiones.
```
A este perfil se encuentran asociada la funcionalidad de crear y consultar las cuentas receptoras hacia las cuales
se envía transacciones que superan los límites transaccionales por día por cuenta desde un mismo originador
y log de transacciones autorizadas EF y consultar el log de transacciones autorizadas EF

```
2.2.1.5. Perfil Tesorería en Integra ACH
```
```
Responsable de realizar los procesos de consulta de las planillas de compensación en cada ciclo y de la
posición de compensación en línea.
```
```
− El funcionario con perfil de tesorería es responsable de validar la información contenida en las
Planillas de Compensación Definitiva y de Posición en Línea.
```
```
2.2.1.6. Perfil Consultas
```
```
Este perfil tiene la posibilidad de realizar consultas de transacciones ACH y SOI (las generadas por el
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 39 de 329
```
```
pago de aportes al sistema de seguridad social).
```
```
2.2.1.7. Perfil Auditor
```
Este perfil puede consultar las Planillas de compensación, transacciones ACH y SOI (las generadas por el pago
de aportes al sistema de seguridad social) y eventos de la operación diaria.

## 2.2.2. Inclusión de novedades en el sistema INTEGRA ACH

A continuación, se describe el procedimiento que deben seguir los Representantes Legales y Administradores
de Usuarios de las Entidades Participantes para incluir novedades de perfiles de uso, en el sistema Integra ACH.

## 2.2.2.1. Manejo de Novedades Información de carácter Confidencial

```
Las Entidades Participantes que requieran solicitar la creación, modificación o eliminación de
Administradores de Usuarios del sistema de información Integra ACH para alguno de sus funcionarios
deben enviar la solicitud firmada por el Representante Legal, y de acuerdo con el formato descrito
en el Anexo 14: Vinculación Entidades Financieras al servicio de ACH Transferencias Interbancarias
(Entidad Participante) GCL-VEV-FOR- 095 y el Formato GIR-GRI-FOR- 023 cuando requiera realizar
cambio de Administrador en la entidad ya vinculada.
```
```
Cualquier novedad de autorización de Administrador de Usuarios en el sistema de Integra ACH debe
ser informada con mínimo tres (3) días hábiles de anticipación a la fecha de inclusión de la novedad;
Esta solicitud está sujeta a la validación de datos y confirmación de firma por parte de ACH
COLOMBIA.
```
## 2.2.2.2. Autorización de Administradores de Usuarios en el Sistema Integra ACH

```
Una vez recibida y verificada la solicitud del usuario, la Gerencia de Seguridad de la Información de
ACH COLOMBIA procede a crear, modificar o eliminar el usuario correspondiente en el sistema
Integra ACH.
```
```
Para la creación de usuarios nuevos o si el participante desea incluir una novedad, el Representante
Legal, debe diligenciar el formato Autorización Administradores de Usuarios ante Integra ACH
(Entidad Participante); Si es un usuario temporal con vigencia limitada de tiempo, se debe indicar en
esta comunicación.
```
## 2.2.2.3. Generación de Códigos OTP

```
El Administrador de Usuarios puede informar el usuario para el ingreso al sistema Integra ACH a los
funcionarios autorizados, de acuerdo con el correo de notificación generado automáticamente por
el sistema en el momento de su creación.
```
```
Como el participante maneja la autenticación por el mecanismo OTP, debe tenerse en cuenta lo
siguiente:
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 40 de 329
```
```
− Para ingresar a Integra ACH se debe digitar el usuario asignado y una contraseña que debe
seguir las recomendaciones de seguridad al momento de su creación; como segundo factor
de autenticación será enviado un OTP a la cuenta de correo registrada en la creación del
usuario para completar el proceso de autenticación.
```
```
− Para las acciones de creación de contraseña, recuperación y logue será enviado un OTP a la
cuenta de correo registrada.
− Las entidades financieras deben asignar cuentas de correo corporativas para los usuarios que
harán uso del servicio, se recomienda que los usuarios tengan acceso a estas cuentas de
correo desde las mismas estaciones de trabajo de donde accederán al sistema Integra ACH.
− El acceso al sistema solo podrá realizarse con un único usuario y rol.
```
## 2.3. Información a los Usuarios

Con el fin de ofrecer un buen servicio a los Usuarios Originadores y/o a los Usuarios Receptores que envían y/o
reciben transacciones a través de ACH COLOMBIA, se presentan las recomendaciones básicas para poder
entregar una información efectiva:

## 2.3.1. En el usuario originador y/o en la EPO

```
− La Entidad Participante Originadora debe disponer de sistemas y mecanismos para que sus Clientes
Originadores tanto personas naturales como jurídicas, o la misma Entidad Participante entreguen la
información completa acerca del originador de la transacción, y la información adicional relacionada
con ésta, independientemente del canal que use para enviarla (“homebanking”, sistemas de audio
respuesta, Internet, “Call Center”, Cajeros Automáticos, planillas, etc).
− La Entidad Participante Originadora debe capacitar a los usuarios Originadores en el uso y beneficios
de brindar información completa, precisa y clara en el registro adenda designado para ello y en los
campos disponibles. El éxito en la entrega de la información a los usuarios Receptores depende en
gran medida de la información que envíen los usuarios Originadores.
− La Entidad Participante Originadora y/o sus Clientes Originadores deben enviar de forma obligatoria,
el registro adenda de información adicional, así como utilizar los campos definidos en el formato
NACHA-M en forma adecuada y de acuerdo con el contenido especificado.
− La Entidad Participante Originadora debe incluir la información detallada del originador de la
transacción (nombre e identificación), sea éste persona natural o jurídica. Para ello deberá utilizar el
Registro de Encabezado de lote en los campos previstos.
− Las transacciones enviadas deben ser agrupadas por lotes de información, de acuerdo con el Usuario
Originador de la transacción.
− La Entidad Participante debe utilizar el campo de Descripción de Lote contenido en el Registro de
Encabezado de Lote (campo 7 de 10 posiciones), de forma estándar y ajustándose a las descripciones
que se describen en el Anexo 16: Tabla de Descripción de Lote.
− La información que se debe entregar, al usuario Receptor para transacciones crédito en el campo 7,
“descripción del Lote”, del Registro de Encabezado de Lote, es obligatorio escribir, una de las siguientes
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 41 de 329
```
```
palabras: “NOMINA”, “PROVEEDOR” o “TRASLADOS”, dependiendo del concepto del pago que genere
la entidad Participante Originadora. Para los pagos realizados por personas naturales aplica siempre la
descripción TRASLADOS. Sin embargo, para estandarizar y hacer más eficiente el sistema, se
recomienda utilizar las descripciones presentadas en el Anexo 16: Tabla de Descripción de Lote.
− La Entidad Participante Originadora debe entregar a sus Clientes Originadores, la información
detallada de las transacciones originadas y la forma como fue afectada su cuenta. Así
mismo, el participante originador debe entregar a sus Clientes Originadores el
resultado de las transacciones enviadas por él, ya sea que hayan sido exitosas o
devueltas, así como el detalle de la causal de la devolución. Esta causal debe ser
clarificada o convertida al lenguaje o sistema utilizado por ellos.
− La Entidad Participante Originadora debe describir a sus Clientes Originadores las devoluciones de
transacciones por ellos enviadas, como mínimo con el mensaje: PAGO NO APLICADO ACH.
− De igual manera el participante originador deberá usar el mensaje REVERSION ACH para aquellos casos
en que se ha reversado una transacción, ya aplicada a sus cuentas.
```
## 2.3.2. En el participante Receptor

```
− La Entidad Participante Receptor debe disponer de sistemas y mecanismos para que sus Clientes
Receptores tanto personas naturales como jurídicas, reciban o puedan acceder la mayor información
de las transacciones recibidas a sus cuentas y/o depósitos electrónicos, independientemente del canal
que use para ello y de acuerdo con sus procedimientos internos (extracto, notas débito, notas crédito,
e-mail, “homebanking”, sistemas de audio respuesta, Internet, “Call Center”, etc.).
− La Entidad Participante Receptor debe indicar con la mayor claridad posible a sus Clientes Receptores,
el(los) medio(s) disponible(s), frecuencia, condiciones y costos asociados de recibir dicha información.
− La Entidad Participante Receptor debe entregar la información a sus Clientes Receptores
oportunamente, con claridad suficiente, completa, precisa e igual a la información enviada por los
Clientes Originadores.
− La Entidad Participante Receptor debe entregar a sus Clientes Receptores la siguiente información,
dependiendo del tipo de canal de que disponga, teniendo en cuenta:
− La Entidad Participante Receptor que utilice medios o canales que restringen considerablemente la
cantidad de información que se puede entregar, debe además de mostrar la información definida en
la tabla Información Básica a Entregar, describir la transacción como mínimo con el mensaje contenido
en el campo estándar DESCRIPCIÓN DE LOTE del Registro de Encabezado de Lote del archivo recibido
más (+) la palabra ACH, según la tabla Información Mínima a Entregar.
− La información presentada en las tablas puede ser extraída de los registros del formato NACHA-M, tal
como se indica en cada una de ellas.
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 42 de 329
```
```
− La Entidad Participante Receptor que utilice medios o canales que permiten entregar la información
total y completa enviada por el participante originador, debe además de mostrar la información
definida en la tabla Información Básica a Entregar, describir la transacción como mínimo con la
información contenida en los campos del formato NACHA-M, según lo describe la tabla Información
Completa a Entregar.
```
## 2.4. Ciclos de Proceso y Horarios

```
Déb it o Créd it o
```
```
SI SI
Número de la
cuenta
```
### 5 13-29 17

```
Registro de Detalle de
Transacciones (tipo 6)
```
```
SI SI
Tipo de
Transacción
```
### 2 2-3 2

```
Registro de Detalle de
Transacciones (tipo 6)
```
```
SI SI
Fecha efectiva
transacción
```
### 9 72-79 8

```
Registro de Encabezado de
Lote (tipo 5)
```
```
SI SI
Valor de la
transacción
```
### 6 30-47 18

```
Registro de Detalle de
Transacciones (tipo 6)
```
```
SI SI
Descripción del
Lote
```
### 7 54-63 10

```
Registro de Encabezado de
Lote (tipo 5)
```
```
SI SI
Nombre del Cliente
Originador
```
### 3 5-20 16

```
Registro de Encabezado de
Lote (tipo 5)
```
```
SI SI
Identificación del
Cliente Originador
```
### 5 41-50 10

```
Registro de Encabezado de
Lote (tipo 5)
```
```
SI SI
Descripción del
Lote
```
### 7 54-63 10

```
Registro de Encabezado de
Lote (tipo 5)
```
### I N F O RM ACI Ó N BÁS I CA A EN TREGAR

```
Tip o d e t ran s acció n N o m b re d e
cam p o
Cam p o P o s ició n Lo n git u d Regis t ro
```
### INFORMACIÓN BÁSICA PARA ENTREGAR

```
Tipo de
```
transacción (^) Nombre de campo Campo Posición Longitud Registro
Débito Crédito
SI NO
Código Cliente
Originador Por Servicio 3 4 -^16  13 Registros^ Adenda^ (tipo^ 7)^
SI NO Referencia 4 17 - 46 30 Registro Adenda (tipo 7)
SI NO Descripción del servicio 6 47 - 61 15 Registro Adenda (tipo 7)

### NO SI

```
Información
Relacionada con el
Pago
```
```
3 4 - 83 80 Registro Adenda (tipo 7)
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 43 de 329
```
## 2.4.1. Actividades ciclos de proceso

ACH COLOMBIA realiza el procesamiento de transacciones para las Entidades Participantes vinculadas, en Días
Hábiles, y de acuerdo con las actividades determinadas para cada Ciclo de Proceso. Actualmente ACH
COLOMBIA realiza cinco (5) Ciclos de Proceso y en cada ciclo realiza las siguientes actividades en los horarios
descritos a continuación:

```
ACTIVIDADES CICLOS DE PROCESO
```
```
Ciclo de
Proceso
```
```
Envío de
Archivos
hacia ACH
COLOMBIA
```
```
Cierre
de Ciclo
```
```
Entrega de
Planillas de
Compensación
```
```
Pago de
Compensación
```
```
Liberación
de
Archivos
```
```
Transacciones Para
Enviar en cada Ciclo
```
```
1 7 :0 1 pm a
8 : 3 0am
```
```
8 : 3 0am 8 : 3 0am a
9: 0 0am
```
```
9: 0 0am a
09 : 15 am
```
```
09 : 15 am
a
09 : 3 0am
```
```
− Devoluciones Débito
y Crédito monetarias
y prenotificaciones
corte 2,3,4 y 5 del día
n- 1
− Débito
− Crédito
− Prenotificación
Débito y Crédito
− Transacciones PSE
− Transacciones SSS
− Transacciones DIAN
2 8 : 3 1am a
11: 0 0am
```
```
11: 0 0a
m
```
```
11: 0 0am a
11 : 3 0m
```
```
11 : 30 am a
11 : 45 pm
```
```
11 : 45 pm
a
12 : 0 0pm
```
```
− Devoluciones Débito
y Crédito monetarias
y prenotificaciones
corte 3,4 y 5 del día n-
1 y corte 1 de día n
− Débito
− Crédito
− Prenotificación
Débito y Crédito
− Transacciones PSE
− Transacciones SSS
− Transacciones DIAN
3 11: 01 am a
2:00pm
```
```
2:00pm 2:00pm a
2:30pm
```
```
2:30pm a
2 : 45 pm
```
```
2 : 45 pm a
3: 00 pm
```
```
− Devoluciones Débito
Y Crédito monetarias
y prenotificaciones
corte 4 y 5 día n- 1 y
corte 1 y 2 del día n
Débito
− Débito
− Crédito
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 44 de 329
```
### ACTIVIDADES CICLOS DE PROCESO

```
Ciclo de
Proceso
```
```
Envío de
Archivos
hacia ACH
COLOMBIA
```
```
Cierre
de Ciclo
```
```
Entrega de
Planillas de
Compensación
```
```
Pago de
Compensación
```
```
Liberación
de
Archivos
```
```
Transacciones Para
Enviar en cada Ciclo
```
```
− Prenotificación
Débito y Crédito
− Transacciones PSE
− Transacciones SSS
− Transacciones DIAN
4 2:01pm a
4:00pm
```
```
4:00pm 4:00pm a
4:30pm
```
```
4:30pm a
4 : 45 pm
```
```
4 : 45 pm a
5: 0 0pm
```
```
− Devoluciones Débito
y Crédito monetarias
y prenotificaciones
corte 5 del día n- 1 y 1,
2 y 3 día n
− Débito
− Crédito
− Prenotificación
Débito y Crédito
− Transacciones PSE
− Transacciones SSS
− Transacciones DIAN
5 4:01pm a
6:00pm
```
```
6:00pm 6:00pm a
6:30pm
```
```
6:30pm a
6:45pm
```
```
6.45pm a
7:00pm
```
```
− Devoluciones Débito
y Crédito monetarias
y prenotificaciones
1,2,3 y 4 día n
− Crédito
− Prenotificación
Débito y Crédito
− Transacciones PSE
− Transacciones SSS
− Transacciones DIAN
```
## 2.4.2. Envío de archivos hacia ACH COLOMBIA

El proceso de envío de archivos hacia ACH COLOMBIA en el participante incluye los siguientes pasos:

1. Preparar los archivos a ser enviados hacia ACH COLOMBIA.
2. Verificar la disponibilidad de conexión con ACH COLOMBIA.
3. Enviar y validar el(los) archivo(s) en el sistema Integra ACH.
4. Corregir los errores que producen Rechazos de Archivo, si existen.
5. Autorizar en Integra ACH las Transacciones que cumplen condiciones especiales, si las hay.
6. Confirmar el envío de cada archivo y verificar el “Log” del sistema.
7. Descargar el(los) archivo(s) de Devoluciones por Operador, si existe(n).
8. Verificar en Integra ACH, la lista de archivos enviados a ser procesados en el ciclo siguiente.


### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 45 de 329
```
9. Verificar la Planilla de Posición Neta al momento del envío.
10. Realizar el Cuadre Operativo.

Estas actividades deben ser realizadas en el horario establecido en la columna “Envío de Archivos hacia ACH
COLOMBIA” del cuadro Actividades Ciclos de Proceso, en Días Hábiles

2.4.2.1 Excepción en el horario de cierre de ciclo
El cambio en el horario de cierre de ciclo se puede dar principalmente por tres motivos descritos a
continuación:

Eventualidad técnica o propia de la entidad participante, con el siguiente procedimiento:
o La entidad debe solicitar mediante la funcionalidad dispuesta para la gestión del servicio el
tiempo adicional requerido, 5, 10, o 15 minutos para el cierre de ciclo actual.
o En caso tal de requerir tiempo adicional al mencionado anteriormente, se debe contactar con
el área de operaciones con el fin de recibir esta autorización.

Eventualidad técnica o propia ACH Colombia
o Se notificará a las entidades participantes mediante la funcionalidad dispuesta para la gestión
del servicio la novedad presentada y si se cuenta con tiempo estimado.
o Una vez solventada la novedad, se notificará solución para continuar con los procesos
correspondientes.

Eventualidad técnica o propia Banco República
o En caso de existir fallas en el sistema SEBRA del Banco de la República, ACH COLOMBIA deberá
verificar la información directamente con esta entidad.
o Se notificará a las entidades participantes mediante la funcionalidad dispuesta para la gestión
del servicio la novedad presentada.
o Una vez solventada la novedad Banco República notifica a ACH COLOMBIA y a las demás
entidades participantes.
o Se verifican los pagos recibidos por parte de las entidades participantes que se encontraban a
favor de ACH COLOMBIA, aquellas que no hayan realizado el pago deberán realizarlo en el
menor tiempo posible para poder continuar con el proceso

Teniendo en cuenta la afectación que se pudo dar por la solicitud de excepción en el horario de cierre del ciclo
afectado, es necesario prever por parte de la operación tiempo adicional en el siguiente ciclo.

## 2.4.3. Cierre de ciclo

El Cierre de Ciclo es iniciado por ACH COLOMBIA a la hora establecida en la columna “Cierre de Ciclo” del
cuadro Actividades Ciclos de Proceso. ACH COLOMBIA no permite extensiones de tiempo a ninguno de los
horarios descritos, debido a que el proceso total y la calidad del sistema se ven afectados. Únicamente en los
casos de solicitud expresa de una Entidad Participante y de acuerdo con el esquema de calidad establecido, se
permitirán envíos tardíos.

## 2.4.4. Entrega de planillas de compensación


### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 46 de 329
```
A través del sistema Integra ACH, ACH COLOMBIA deja disponible la Planilla de Compensación dentro del
horario indicado en la columna “Entrega de Planillas de compensación” del cuadro Actividades Ciclos de
Proceso. Este proceso se hace al inicio del Cierre de Ciclo.

La Planilla de Compensación presenta la Posición Neta calculada para el participante, según el valor de las
Transacciones enviadas y/o recibidas por el sistema Integra ACH, por el sistema PSE, y por las reversiones
pagadas y/o recaudadas.

La Entidad Participante debe consultar su Planilla de Compensación al Cambio de Ciclo, una vez ACH COLOMBIA
lo deje disponible y verificar su contenido contra sus registros realizando el Cuadre Operativo, según los
lineamientos descritos en el 2.6. Cuadre Operativo en el participante.

## 2.4.5. Pago de compensación

Las Entidades Participantes cuya Posición Neta en la Planilla definitiva de Compensación indica saldo en contra,
deben realizar el Pago de Compensación a ACH COLOMBIA a través del sistema SEBRA del Banco de la República
en los horarios establecidos en la columna “Pago de Compensación” del cuadro Actividades Ciclos de Proceso.
En caso de fallas en el sistema SEBRA al momento de efectuar el pago, se debe aplicar el procedimiento descrito
en el capítulo 4.4 Esquema de Contingencia.

Adicionalmente ACH Colombia brinda a las Entidades Participantes la alternativa de pago de compensación por
débito automático, para lo cual las Entidades deben realizar la respectiva autorización ante el Banco de la
República, mediante “Carta de autorización Dirigida al Banco de la Republica” anexo 4 del sistema de cuentas
de depósito; ACH Colombia procederá a descontar de la cuenta CUD de la Entidad Participante autorizada, el
valor de compensación cuando dicha Entidad tenga que pagar a ACH Colombia la compensación de un ciclo
operacional, este débito automático lo realiza ACH Colombia durante el horario establecido como “pago de
compensación” en el cuadro de “Actividades ciclos de proceso” relacionado en el numeral 2.4.1 Actividades
ciclos de proceso, empleando el sistema SEBRA..

En el momento que la Entidad evidencie una diferencia en el valor debitado automáticamente por ACH
Colombia, deberá establecer contacto inmediato mediante el correo de entrega@achcolombia.com.co,
mencionando la novedad presentada y un celular a fin de ser contactado y atendida su petición de forma
prioritaria.

Este proceso lo realiza ACH Colombia únicamente con las Entidades Participantes que lo autoricen y se realizar
por cada ciclo operacional que la Entidad Participante deba pagar a ACH Colombia, para lo cual la Entidad
Participante debe garantizar los recursos necesarios, para que el débito sea exitoso, en el evento de no ser así,
la Entidad será contactada inmediatamente, explicando la causal de rechazo, para que esta ejecute el proceso
de “Pago de Compensación” manualmente por cuenta propia, prestando especial cuidado al cumplimiento de
los horarios establecidos.

El incumplimiento a los horarios determinados en esta actividad causará la aplicación del Esquema de Calidad,
de acuerdo con lo descrito en el capítulo 3. Esquema de Calidad


### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 47 de 329
```
## 2.4.6. Liberación de archivos

Sólo si la totalidad de Entidades Participantes han realizado el Pago de Compensación exitosamente, ACH
COLOMBIA distribuye o entrega los archivos de transacciones a todas las Entidades Participantes Receptoras,
dejando en los buzones correspondientes de cada una, los archivos de aplicación dentro de los horarios
indicados en la columna “Liberación de Archivos” del cuadro Actividades Ciclos de Proceso. Las Entidades
Participantes deben recoger sus archivos para aplicar las transacciones a los sistemas de cuenta internos.

La entrega de archivos de ACH COLOMBIA a las Entidades Participantes depende principalmente de que el
proceso de Pago de Compensación de las transacciones se realice exitosamente en los tiempos establecidos.

El proceso de recepción o descarga de archivos en el participante incluye los siguientes pasos:

1. Verificar el buzón de archivos a recibir en el sistema Integra ACH.
2. Descargar los archivos desde el buzón habilitado por ACH COLOMBIA.
3. Verificar los archivos recibidos desde ACH COLOMBIA.
4. Realizar el Cuadre Operativo.
5. Aplicar las transacciones recibidas en el sistema de cuentas interno.

Paralelamente a la Liberación de Archivos, y una vez disponga de los fondos de compensación, ACH COLOMBIA
realiza el proceso de Distribución de Pagos que consiste en pagar los valores de compensación a las Entidades
Participantes cuya Posición Neta sea a favor. Para ello ACH COLOMBIA utiliza el sistema SEBRA del Banco de la
República.

## 2.4.7. Transacciones para enviar en cada ciclo

La Entidad Participante puede enviar todo tipo de transacciones en cada Ciclo de Proceso, exceptuando el ciclo
5 donde no se aceptarán transacciones débito. Para las Transacciones de Devolución, ACH COLOMBIA revisará
y aplicará el Esquema de Calidad sobre aquellas Transacciones de Devolución que han sido enviadas fuera de
los tiempos máximos establecidos; los tiempos de devolución de transacciones crédito es máximo cuatro ( 4 )
ciclos y débito tanto monetarias como prenotificación es máximo cuatro ( 4 ) ciclos después de haber recibido
la transacción original, para el caso de las devoluciones crédito el sistema de Integra ACH dejara enviar y
compensar las devoluciones que se encuentren fuera de los plazos establecidos aplicando el esquema de
calidad, para el caso de las devoluciones debito el sistema no permitirá enviar y compensar devoluciones fuera
de los plazos establecidos.

## 2.5. Compensación y Liquidación

A continuación, se describe el procedimiento para la Compensación y Liquidación que se realiza entre las
Entidades Participantes y ACH COLOMBIA, que resulta del envío y procesamiento de transacciones en ACH
COLOMBIA.

## 2.5.1. Objetivo de la compensación y liquidación

El objetivo de la Compensación y de la Liquidación es completar el ciclo de envío y recepción de transacciones
entre las Entidades Participantes mediante el pago del valor de estas.
Las transacciones compensadas y liquidadas a través del sistema ACH son:


### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 48 de 329
```
```
− Transacción ACH crédito y débito.
− Transacciones ACH de reversión - (Estas transacciones se incluyen en el ciclo 4).
− Transacciones PSE.
− Transacciones SSS.
− Transacciones DIAN.
```
Estas transacciones arrojan una Posición Neta que indica el valor que una Entidad Financiera debe pagar o
tiene derecho a recibir del sistema.

El proceso de Liquidación consiste en calcular las Posiciones Netas para cada Entidad Financiera en cada Ciclo
de Operación que se realiza.

La Compensación es el proceso siguiente a la Liquidación y consiste en autorizar y hacer efectivos dichos pagos
y cobros, es decir saldar las posiciones netas obtenidas en cada Ciclo.

Se entiende que una orden de transferencia ha sido aceptada cuando ha cumplido los requisitos y controles
de riesgo establecidos en los reglamentos provistos por ACH COLOMBIA, se da por aceptado el archivo de cada
una de las entidades participantes, y se realiza el pago de la compensación, finalizando así el proceso de
liquidación del servicio.

La Compensación de transacciones se efectúa a través del sistema SEBRA del Banco de la República, en las
cuentas de depósito que las Entidades Financieras Participantes tienen en el Banco.

Con respecto a los participantes no vigilados por la Superintendencia Financiera, estos deberán pedir una
cuenta CUD en el sistema SEBRA en los términos que establezca el Banco de la República, y cumplir con los
requisitos operativos del esquema de prefondeo descrito en este manual en el capítulo “Condiciones del
Sistema de prefondeo”

Estos procedimientos se realizan varias veces al día, al final de cada Ciclo de operación y cuentan con diferentes
niveles de verificación y autorización con el fin de garantizar eficiencia y disminuir el riesgo operativo.

Si una Entidad Participante no cuenta con los recursos en el Banco de la República para realizar el pago de la
compensación, se debe realizar el siguiente proceso:

- Contactar a las Áreas Operativas de ACH COLOMBIA encargadas del pago para establecer una hora
    máxima
- En dado caso que la Entidad Participante informe que no cuenta con los recursos para el pago de la
    compensación, este deberá informar por escrito la situación presentada a ACH COLOMBIA con el fin
    de realizar el reproceso del Ciclo.
- El área Operativa de ACH COLOMBIA debe informar a la Vicepresidencia VOT y Presidencia la situación
    presentada.
- Informar a los participantes del sistema que una Entidad Participante no cuenta con los Recursos para
    el Pago de la Compensación.
- Identificar los archivos cargados en el ciclo por la Entidad Participante
- Reprocesar el Ciclo sin la información cargada por el Participante que informó el no pago de la


### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 49 de 329
```
```
compensación
```
- Generar nuevamente la compensación por parte de ACH COLOMBIA
- Gestionar con los entes de control la situación presentada con el participante.
- ACH COLOMBIA deberá validar las condiciones del Participante con el fin de revisar la continuidad o
    retiro del Sistema.
- Realizar acuerdos operativos con el fin de asegurar el pago a través de prefondeo para el pago de la
    compensación.

## 2.5.2. Manejo cuenta de depósito de ACH COLOMBIA

## 2.5.2.1. Cuenta de Depósito

```
Para el manejo de la Compensación con las Entidades Participantes, ACH COLOMBIA tiene una cuenta
de depósito en el Banco de la República; esa cuenta es afectada vía SEBRA, por los cobros y pagos
que efectúen las Entidades Participantes a ACH COLOMBIA.
```
```
La cuenta de depósito de ACH COLOMBIA en el Banco de la República está exenta del Gravamen al
Movimiento Financiero, sin embargo, es responsabilidad del participante pagar los impuestos que se
deriven de las transacciones realizadas, antes de ser enviadas a ACH COLOMBIA.
```
```
El número de cuenta de depósito de ACH COLOMBIA en el Banco de la República y el código de
identificación de las transacciones que se liquidan se describe a continuación.
```
```
Datos SEBRA (CUD) de ACH COLOMBIA
Sistema que utiliza: SEBRA (CUD) – Banco de la República
Nombre cuenta: ACH COLOMBIA S.A.
Número de cuenta: 65810103
Código de compensación: 1511
```
(^1) código asignado por el Banco de la República que debe ser usado únicamente para pagos o cobros desde o
hacia ACH COLOMBIA, el cual es exento del GMF.

## 2.5.2.2. Saldo Cero

```
En la realización del proceso de Compensación en el(los) Ciclo(s) de Operación(es), algunas Entidades
Participantes presentan una Posición Neta a favor de ACH y otras presentan una Posición Neta en
contra de ACH; es decir, algunas Entidades Participantes deben pagar a ACH COLOMBIA y otras
reciben el pago desde ACH COLOMBIA. El resultado neto de esta operación es igual a cero ($0), es
decir que el valor pagado a ACH COLOMBIA es igual al valor pagado por ésta a las Entidades
Participantes.
```
## 2.5.3. Transacciones para compensar


### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 50 de 329
```
Las transacciones que son motivo de compensación son únicamente aquellas que impliquen movimiento de
fondos entre las Entidades Participantes, es decir transacciones monetarias tales como: débitos, créditos
transacciones autorizadas por PSE, créditos transacciones autorizadas por PSE, créditos PSE, reversiones de
transacciones ACH, devoluciones a débitos ACH, transacciones SSS, transacciones DIAN y devoluciones a
créditos ACH, entre otras. Las Devoluciones por Operador no son objeto de compensación en ACH COLOMBIA.

## 2.5.4. Posición neta del participante

ACH COLOMBIA, una vez efectúa el cierre de archivos en cada Ciclo de Operación, realiza el proceso de
Compensación, obteniendo la situación o Posición Neta de cada una de las Entidades Participantes, la cual
puede ser a favor o en contra:

### OPERACIONES EN EL PROCESO DE COMPENSACIÓN EN LA ENTIDAD FINANCIERA

```
Tipos de Operación Realizadas por la EF Acción Por Tomar Resultado Posición Neta
Enviar débitos Debe cobrar Recibe dinero + (A favor)
Enviar créditos Debe pagar Envía dinero - (En contra)
Enviar devoluciones a créditos recibidos Debe pagar Envía dinero - (En contra)
Enviar devoluciones a débitos recibidos Debe cobrar Recibe dinero + (A favor)
Recibir débitos Debe pagar Envía dinero - (En contra)
Recibir créditos Debe cobrar Recibe dinero + (A favor)
Recibir devoluciones a débitos enviados Debe pagar Envía dinero - (En contra)
Recibir devoluciones a créditos enviados Debe cobrar Recibe dinero + (A favor)
Autorizar débitos PSE Debe pagar Envía dinero - (En contra)
Recibir créditos PSE Debe cobrar Recibe dinero + (A favor)
Autorizar débitos SSS Debe pagar Envía dinero - (En contra)
Recibir créditos SSS Debe cobrar Recibe dinero + (A favor)
Autorizar débitos DIAN Debe pagar Envía dinero - (En contra)
Recibir créditos DIAN Debe cobrar Recibe dinero + (A favor)
Autorizar reversiones crédito Debe pagar Envía dinero - (En contra)
Recibir reversiones crédito Debe cobrar Recibe dinero + (A favor)
```
El resultado de este cálculo para todas las transacciones procesadas por el sistema ACH COLOMBIA y el sistema
PSE, determina la Posición Neta de una Entidad Participante en un Ciclo de Operación, lo que a su vez define
si el participante debe cobrar o pagar en el proceso de Liquidación, así:

```
− En Contra:
Cuando la suma total de las operaciones es negativa, el participante debe pagar a ACH COLOMBIA, vía
SEBRA, el valor neto de las transacciones del(los) Ciclo(s) de Operación(es) que corresponda. En este caso
el participante es deudora ante ACH COLOMBIA.
```
```
− A Favor:
Cuando la suma total de las operaciones es positiva, el participante debe cobrar, para lo cual ACH
COLOMBIA, traslada vía SEBRA, el valor neto de las transacciones del(los) Ciclo(s) de Operación(es) que
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 51 de 329
```
```
corresponda. En este caso el participante es acreedora.
```
```
De cada Ciclo de Operación se genera una Planilla de Compensación definitiva para cada Entidad
Participante y con base en ésta, se realiza el cobro y pago entre Entidades Participantes, de las
transacciones procesadas a través del sistema ACH COLOMBIA y del sistema PSE, dependiendo de la
Posición Neta. Una descripción detallada del contenido de la Planilla de Compensación se encuentra en el
Anexo 5: Detalle de Planilla de Compensación.
```
```
Además de la Posición Neta, la Planilla de Compensación detalla la fecha y hora (ciclo) de generación, así
como el valor total de las Transacciones monetarias enviadas y/o recibidas por el participante en su calidad
de Originadora y/o Receptora.
```
```
Las Entidades Participantes a través de SEBRA trasladan los fondos a la cuenta de depósito de ACH
COLOMBIA en el Banco de la República, cuando la Posición Neta es a cargo. ACH COLOMBIA traslada a
través de SEBRA los fondos que corresponden a cada Entidad Participante, que resulte con Posición Neta
a favor.
```
## 2.5.5. Garantía de procesamiento

Para brindar garantía en la Compensación y Liquidación de transacciones, una vez ACH COLOMBIA disponga
de los fondos de las Entidades Participantes deudoras en su cuenta de depósito en el Banco de la República,
efectúa los traslados a las Entidades Participantes acreedoras.

Así mismo, ACH COLOMBIA distribuye archivos a las Entidades Participantes Receptoras hasta cuando se
confirme vía SEBRA la disponibilidad de los fondos.

Con la compensación de transacciones vía SEBRA se brinda garantía a las Entidades Participantes e implica que
las Entidades Participantes deudoras dispongan de los fondos y paguen en forma oportuna a ACH COLOMBIA.
De igual forma, las Entidades Participantes acreedoras tienen la seguridad que las transacciones enviadas por
ACH COLOMBIA van a ser pagadas por ésta.

## 2.5.6. Contingencias en el sistema SEBRA (CUD)

En caso de que existan fallas en el sistema SEBRA del Banco de la República, el participante debe notificar a
ACH COLOMBIA la falla, que es verificada por ACH COLOMBIA directamente en el Banco de la República. De
acuerdo con la hora de reporte de la falla, el participante es eximida o sancionada según lo establecido en el
Anexo 6: Eventos Sancionables del Esquema de Calidad.

2.5.7 Condiciones del Sistema de Prefondeo

Las entidades participantes no vigiladas por la Superintendencia Financiera que deseen vincularse al servicio
de transferencias ACH, deberán contar con una cuenta CUD en los términos establecidos por el Banco de la
República, deberán contar con una cuenta CUD en los términos establecidos por el Banco de la República, o
con una cuenta corriente/ahorros en un establecimiento de crédito (Banco Sponsor) desde donde dará la


### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 52 de 329
```
instrucción de enviar y/o recibir los pagos, y cumplir con los siguientes requisitos operativos: y cumplir con los
siguientes requisitos operativos:

```
− La entidad participante dentro del servicio de transferencias interbancarias deberá asignar
recursos económicos a la cuenta CUD de ACH Colombia. Este monto inicial tendrá un valor
mínimo de 300 millones de pesos, y podría variar según los movimientos históricos durante el
primer mes.
− Posterior a cada ciclo operativo, la entidad participante deberá monitorear los valores
restantes al monto asignado de su disponible, de manera que, si la entidad cuenta con el 30%
de los recursos económicos asignados, deberá reintegrar el 50% del valor inicial con el objetivo
de respaldar los próximos ciclos operativos.
− Al final del día, el sistema ACH depositará el saldo remanente a la cuenta de la entidad
participante.
```
## 2.6. Cuadre Operativo en el participante

A continuación, se describe el procedimiento para realizar el Cuadre Operativo sobre los diferentes procesos
que realiza el participante, relacionados con ACH COLOMBIA.

## 2.6.1. Objetivo y definición

El Cuadre Operativo busca controlar la operación y disminuir los riesgos que se presentan en los procesos
operativos o de sistemas en el participante y en ACH COLOMBIA mediante el control y seguimiento detallado
de las actividades que se realizan.

El Cuadre Operativo en los diferentes procesos incluye la revisión detallada de archivos, reportes, “logs” y
rastros de los sistemas y procedimientos utilizados y/o los soportes que se produzcan en cada uno. La Entidad
Participante debe revisar en forma detallada, los archivos y reportes que se generan a diario y mensualmente,
contra sus registros internos.

Si al efectuar el Cuadre Operativo el participante encuentra diferencias debe reportarlas inmediatamente a
ACH COLOMBIA para hacer una revisión y realizar los ajustes a que haya lugar. Si las diferencias no son
reportadas de inmediato, ACH COLOMBIA asume que el Cuadre Operativo realizado por el participante fue
exitoso.

## 2.6.2. Transacciones y valores

La Entidad Participante debe realizar un Cuadre Operativo por cada Ciclo de Operación, que incluya por lo
menos las siguientes verificaciones: número de transacciones ACH y valor de transacciones ACH enviadas y
recibidas por archivo en cada Ciclo de Operación y la Posición Neta; número de transacciones PSE y valor de
transacciones PSE aprobadas y recibidas en cada Ciclo de Operación, número de transacciones SSS y valor de
transacciones SSS aprobadas y recibidas en cada Ciclo de Operación, número de transacciones DIAN y valor de
transacciones DIAN aprobadas y recibidas en cada Ciclo de Operación.

Algunos de los principales procesos relacionados con ACH COLOMBIA, que deben ser verificados son:


### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 53 de 329
```
- Transmisión de Archivos desde y hacia ACH COLOMBIA
- Transacciones aprobadas a través de PSE
- Transacciones aprobadas de SSS
- Transacciones aprobadas por concepto de pagos a la DIAN
- Reversiones del módulo de reclamos autorizadas y recibidas
- Verificación y clasificación de Transacciones recibidas en el participante
- Aplicación de Transacciones ACH, PSE, SSS y por concepto de pagos a la DIAN en el participante.

En el proceso de transmisión de archivos ACH desde y hacia ACH COLOMBIA, el participante debe verificar que
la suma del total de transacciones enviadas, coincidan con los “logs” del sistema Integra ACH y con los “logs”
del sistema interno de procesamiento. Este proceso puede estar clasificado por tipo de transacción o cualquier
otro criterio.

Las advertencias y mensajes del sistema Integra ACH resultado de la validación de los archivos enviados, deben
ser tenidas en cuenta, así como los controles de duplicidad o límites. La Entidad Participante debe consultar la
“Planilla de Compensación en Línea” para verificar la posición en línea de compensación y las “Planillas de
Compensación Definitivas” que genere el sistema Integra ACH y validarlas contra sus registros internos y los
valores de las transacciones.
Adicionalmente, el participante debe verificar que no existan operaciones inusuales por parte de sus Clientes
Originadores o de los sistemas internos, tales como un número alto de devoluciones, o transacciones de
valores elevados. Se recomienda que el participante establezca los controles que considere pertinentes.

En general, el participante debe verificar que no existan inconsistencias en valores de transacciones, en valores
de compensación, y que el número de Transacciones de devolución generadas sumado al número de
Transacciones aplicadas sea igual al número de Transacciones recibidas para aplicación.

El Cuadre Operativo de transacciones y valores, debe tener en cuenta los siguientes criterios:

Archivo de Transacciones Enviadas:

```
− Transacciones Crédito y Débito, Relotificaciones Crédito y Débito recibidas de los Usuarios
Originadores.
− Transacciones de Devolución Crédito y Débito, Devolución de relotificaciones Crédito y Débito
enviadas como respuesta a otras Entidades Participantes.
− Transacciones de Devolución Crédito y Débito originadas por Solicitud del Usuario Receptor, para ser
enviadas como respuesta a otras Entidades Participantes.
```
Archivo de Conciliación PSE:

```
− Transacciones autorizadas a través de PSE.
− Transacciones recaudadas por PSE.
```
Archivo de Transacciones SSS:


### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 54 de 329
```
```
− Transacciones autorizadas de SSS.
− Transacciones recaudadas por SSS
```
Archivo de Transacciones Recibidas:

```
− Transacciones Crédito y Débito, Prenotificaciones Crédito y Débito recibidas de otras Entidades
Participantes.
− Transacción Crédito recibidas de PSE.
− Transacciones de Devolución Crédito y Débito, Devolución de Prenotificaciones Crédito y Débito
recibidas como respuesta de otras Entidades Participantes.
− Transacciones de Devolución Crédito y Débito recibidas como respuesta de otras Entidades
Participantes, que las originan por Solicitud del Usuario Receptor.
```
Las Transacciones de Devolución por Operador generadas por ACH COLOMBIA se retornan a el participante y
no se compensan por lo que el participante debe verificar el archivo original enviado y el archivo de
Devoluciones por Operador recibido en número de transacciones y valores.

## 2.6.3. Otros conceptos

Los procesos complementarios relacionados con el Cuadre Operativo que debe realizar el participante por
proceso son:

```
− Cobro mensual de comisión por servicio de ACH COLOMBIA.
− Liquidación diaria de facturación de tarifa de acceso a la red.
− Cobro mensual de facturación de tarifa de acceso a la red.
− Liquidación mensual de sanciones y servicios.
```
## 2.7. Manejo de Novedades

Este ítem describe el mecanismo de manejo de novedades que se presentan sobre las transacciones
procesadas a través del sistema de ACH COLOMBIA, de los servicios; de ACH, PSE Y PCSS, es de aclarar que este
proceso es administrado a través del módulo de reclamos al cual tienen acceso las Entidades Participantes y
ACH Colombia por medio de la url https://172.30.19.21/consola/faces/login.jsp.

## 2.7.1. Concepto y tipos de novedades

De las situaciones que se presentan en el flujo de transacciones en ACH, PSE Y PCSS se mencionan las
siguientes:

## 2.7.1.1. Reclamo

```
Una Entidad Participante puede reclamar directamente a ACH COLOMBIA, cuando es ésta la
directamente involucrada en el evento y cuando supone un error u omisión en el proceso diario de
ACH COLOMBIA; de igual manera puede radicar reclamos a otra (S) Entidad (es) Participante (s)
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A

```
Agosto de 2024
Página 55 de 329
```
```
cuando supone un error u omisión en el proceso del participante receptor de una transacción
compensada por el sistema de ACH; La solicitud de información y consultas generales no son
consideradas como reclamos.
Algunas de las respuestas obtenidas a las solicitudes de reclamos pueden confirmar errores u
omisiones en los procesos del participante o de ACH COLOMBIA, casos en los cuales se aplicarán las
sanciones establecidas en el Esquema de Calidad definido en el Capítulo 3 , de este manual.
```
## 2.7.1.2. Solicitud de Certificación

```
Requerimiento de una Entidad Participante a ACH COLOMBIA o a otra Entidad Participante de
generar una certificación de un proceso exitoso o errado específico efectuado. La certificación
pretende obtener una constancia de una Entidad Participante o de ACH COLOMBIA de un proceso
exitoso o errado que ha sido efectuado.
```
## 2.7.1.3. Reversión

```
Solicitud de una Entidad Participante Originadora a una Entidad Participante Receptor, del reintegro
del valor de una transacción crédito realizada previamente, por error de el participante originador o
sus clientes, cuyo propósito es deshacer dicha transacción monetaria.
```
```
Las solicitudes de reversión no son consideradas como reclamos ya que no supone un error de el
participante Receptor, sino de el participante originador o sus clientes.
```
## 2.7.1.4. Devoluciones

```
Solicitud de una Entidad Participante Originadora a una Entidad Participantes Receptora, de la
devolución del valor de una transacción crédito o débito realizada previamente y compensada por el
sistema de ACH Colombia, y la cual es considerada como una transacción no consentida, de acuerdo
con el estatuto de protección al consumidor.
```
## 2.7.1.5. Reintegros

```
Solicitud de una Entidad Participante Autorizadora a una Entidad Participante Recaudadora, del
reintegro del valor de una transacción PSE realizada previamente y compensada por el sistema de
ACH Colombia, de acuerdo con la petición del consumidor teniendo como soporte el estatuto de
protección al consumidor.
```
## 2.7.1.6. Devoluciones de Pagos complementarios de seguridad Social

```
Devolución que realiza una Entidad Participante Recaudadora de pagos complementarios (Cuentas
AFC y/o Libranzas) al participante Autorizador, del valor de los aportes de aquellos usuarios que no
fue posible hacer su abono, y los cuales fueron compensados por el sistema de ACH Colombia.
```
## 2.7.1.7. Procesos Especiales


### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 56 de 329
```
```
Solicitud de una Entidad Participante a ACH COLOMBIA o a otra Entidad Participante de realizar un
procedimiento especial que no está contemplado como un reclamo, como una solicitud, como un
reintegro, como una devolución o como una reversión, por ejemplo, aceptar un valor de devolución
a través del sistema SEBRA.
```
## 2.7.2. Procedimiento para administrar novedades

Para administrar y controlar las novedades (Ver Anexo 7. Tipos de Novedad y Causales) que se presentan, el
participante que establece el requerimiento debe determinar con exactitud y oportunamente la causa o razón
de la novedad, y el participante que recibe la solicitud, debe darle trámite.

ACH COLOMBIA cuenta con el módulo de “Reclamos”, una herramienta automatizada de fácil manejo que les
permite a las Entidades Participantes y a sus funcionarios administrar las novedades de transacciones
procesadas a través del sistema Integra ACH, disminuyendo los procesos manuales y agilizando la solución y
control de los reclamos.

## 2.7.2.1. Generación de Novedades y Solución de Casos

```
Para acceder al módulo de “Reclamos”, el usuario debe ingresar a la siguiente página que es dispuesta
por ACH Colombia a las Entidades Participantes, con la finalidad de administra las novedades
radicadas por los servicios de ACH Colombia,
https://fact.achcolombia.com.co/consola/faces/login.jsp tener el perfil de “Reclamos” y seleccionar
la funcionalidad que requiera, desde el menú principal “Reclamos” (Ver documento relacionado
Instructivo de manejo Módulo de Reclamos).
```
```
En los casos donde no sea satisfactoria la solución dada por el participante Receptor de la novedad
al participante originador de la misma, ACH COLOMBIA debe mediar ante las Entidades Participantes
involucradas con el propósito de encontrar una solución efectiva.
```
```
Las novedades (Ver Anexo 7. Tipos de Novedad y Causales) que se presentan en la operación son
creadas a través del Módulo de Reclamos en la medida de su ocurrencia por el participante afectado.
La Entidad Participante que realiza la solicitud, puede asociar una transacción, un grupo de
transacciones y lote (s) a un mismo caso.
```
```
Así mismo, el participante que recibe la solicitud, reclamo, devolución o reversión puede a través del
módulo de “Reclamos”, controlar los tiempos, detallar el avance del caso o dar solución total a los
mismos.
```
## 2.7.2.2. Plazos de Solución

```
Plazo máximo que se le da al participante Receptor del caso o a ACH COLOMBIA para dar una solución
o respuesta al requerimiento presentado. Este plazo se determina de acuerdo con el tipo de novedad,
causal y fecha de envío del caso por parte del participante originador del mismo.
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 57 de 329
```
```
Los requerimientos de reclamaciones que sean solicitados por un ente regulador tendrán un plazo
de solución máximo de dos (2) días hábiles posteriores a la recepción de la solicitud en el participante,
y deben ser claramente identificados y soportados al momento del envío.
```
```
Para el tipo de novedades REV (Reversiones) y DEV (Devoluciones) el plazo máximo para dar
respuesta definitiva o cierre son 60 días hábiles, sin embargo y de acuerdo con el ANEXO 23 , se debe
realizar avances o cierres en los días establecidos para tal fin. Si no se realiza cierre en el día 60 el
sistema liquidara sanción; se realiza por cada una de las transacciones asociadas a cada
requerimiento y por cada día hábil tardío. De igual manera se aplicará el esquema de calidad en el
caso de no realizar avances o cierres durante los días establecidos en cada uno de los periodos
relacionados en el calendario del ANEXO 23 relacionado en el presente documento.
```
```
Para las novedades REC y SOL se debe dar solución total al segundo día hábil, de lo contrario se
liquidará sanción; se realiza por cada una de las transacciones asociadas a cada requerimiento y por
cada día hábil tardío.
```
```
El reclamo es enviado al funcionario principal y a un funcionario de nivel superior del participante
registrados en el módulo de facturación, que estén autorizados a hacer seguimiento y dar prioridad
a estos requerimientos.
```
```
La Entidad Participante debe contar con los soportes y documentos necesarios que respalden dichos
requerimientos.
```
```
En caso de incumplimiento de los plazos establecidos por parte del participante o ACH COLOMBIA,
se aplicará el Esquema de Calidad definido en el numeral 2.7. Manejo de Novedades, de este manual.
```
```
Adicionalmente, se escala la situación de incumplimiento a un funcionario de nivel superior del
participante o de ACH COLOMBIA, según sea el caso.
```
## 2.7.3. Manejo de reclamos

Se entiende por reclamo la solicitud de verificación de una Transacción enviada de una Entidad Participante
Originadora hacia una Entidad Participante Receptor o hacia ACH COLOMBIA, sobre la cual se presume que el
participante Receptor o ACH COLOMBIA no realizó una operación correctamente o hubo omisión en algún
proceso o norma establecida por una causa específica. Algunos de los errores que puede cometer cualquiera
de los participantes del sistema ACH se resumen a continuación:

```
Tipo de Novedad Plazo de Solución (días hábiles)
REC 2
SOL 2
REV 60
DEV 60
REI 5
DPC 3
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A

```
Agosto de 2024
Página 58 de 329
```
```
− Afectar más de una vez o por valor diferente la cuenta de un usuario como consecuencia de haber
iniciado más de una solicitud de reversión para una misma transacción a través de ACH COLOMBIA o
de otro canal.
− Aplicar o contabilizar una transacción en una fecha diferente a la Fecha Efectiva indicada por ACH
COLOMBIA o dejar disponibles los fondos en la cuenta del Usuario Receptor después del ciclo máximo
definido.
− Aplicar la transacción a un Número de Cuenta o a un Tipo de Cuenta diferente al solicitado.
− Afectar la cuenta de un usuario más de una vez o por un valor diferente al solicitado en la transacción.
− Enviar una transacción débito monetaria sin haber pre-notificado la misma o sin contar con las
autorizaciones respectivas.
− Aplicar una transacción débito sin validar la identificación del Usuario Receptor o transacciones crédito
cuando sea solicitada por el Usuario.
− Devolver transacciones no monetarias de forma tardía.
− Devolver transacciones monetarias de forma tardía.
− Devolver una transacción monetaria por no haber validado correctamente la pre-notificación previa
(cuenta y/o identificación).
− Devolver una transacción modificando la información de la transacción original.
− Enviar transacciones erradas y/o duplicadas que fueron recibidas correctamente desde el participante
hacia ACH COLOMBIA, que impliquen su reversión manual posterior en otra Entidad Participante.
− Cobrar más o pagar menos de lo calculado a una Entidad Participante en el proceso de compensación
por error de ACH COLOMBIA, y que se vea afectada por cambiar su “posición a favor” por “posición en
contra” en la nueva liquidación de compensación.
− Proceso de transacción en diferente fecha a la fecha efectiva
− Contestar reclamos después de los plazos definidos.
```
## 2.7.3.1. Responsabilidades de los Participantes

```
Una Entidad Participante puede reclamar directamente a ACH COLOMBIA, cuando es ésta la
directamente involucrada en el evento y cuando supone un error u omisión en el proceso diario de
ACH COLOMBIA. La solicitud de información y consultas generales no son consideradas como
reclamos.
```
```
Algunas de las respuestas obtenidas a las solicitudes de reclamos pueden confirmar errores u
omisiones en los procesos de el participante o de ACH COLOMBIA, casos en los cuales se aplicarán
las sanciones establecidas en el Esquema de Calidad definido en el numeral 2.7. Manejo de
Novedades, de este manual.
```
## 2.7.3.2. Condiciones para Solicitar Reclamos

```
Las entidades participantes en el sistema ACH COLOMBIA, deben tener en cuenta las siguientes
recomendaciones antes de solicitar la reclamación de transacciones crédito o débito:
```
```
− Que el participante originador del caso, validen que las transacciones que dan origen a la
reclamación hayan sido procesadas por ACH COLOMBIA (si es el caso enviar solicitud de
verificación de transacciones a ACH COLOMBIA, lo anterior aplica para transacciones que no
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A

```
Agosto de 2024
Página 59 de 329
```
```
se encuentren disponibles en la base de datos).
```
```
− Que ACH COLOMBIA actúa como mediador del proceso de reclamos y, por lo tanto, si el
participante crea el caso a través del módulo de “Reclamos”, deberá suspender cualquier otro
intento de reclamación por otro medio.
```
```
− Que las Entidades Participantes ajusten el reglamento de los contratos de cuenta corriente
y/o de ahorros y/o depósitos electrónicos, de manera que se garantice mediante una cláusula,
que el participante Receptor puede efectuar en cualquier momento, débitos o créditos a la
cuenta del Cliente Receptor, por concepto de error u omisión en el proceso de las
transacciones.
```
## 2.7.3.3. Novedad de Reclamo – Entidad Participante Originador del caso

```
− El usuario del participante que origina el reclamo, con el perfil y permisos adecuados, debe
ingresar al módulo de reclamos y registrar la información del nuevo caso principal “REC” en
el formulario que despliega el sistema.
```
```
− Posteriormente debe escoger los lotes y/o transacciones del sistema de reclamos para que
sean adicionados al caso principal “REC”. Los lotes y transacciones pueden estar destinados
a Entidades Participantes diferentes.
```
```
− Una vez el caso principal “REC” contiene al menos un lote o una transacción, está listo para
el siguiente paso que es su envío a las Entidades Participantes destino.
```
## 2.7.3.4. Solución Parcial o Total a Novedad de Reclamos–EPR del Caso

```
− El usuario del participante Receptor del caso de reclamo, con el perfil y permisos adecuados,
debe ingresar al módulo de reclamos para recibir e iniciar la gestión de dar solución parcial o
total a la solicitud de reclamo.
```
```
− La Entidad Participante Receptor del caso de reclamo, de acuerdo con sus procedimientos
internos, valida la información que recibe a través del módulo de reclamos de ACH
COLOMBIA, verificando los datos o condiciones adicionales de la transacción o de la cuenta
para dar solución al caso.
```
```
− La Entidad Participante Receptor del caso de reclamo procede a tramitar el reclamo,
aplicando el procedimiento estipulado para cada caso.
```
```
− La Entidad Participante Receptor del caso debe dar solución al caso de reclamo, de acuerdo
con los tiempos establecidos por las Entidades Participantes y ACH COLOMBIA.
```
```
− La Entidad Participante Receptor del caso de reclamos mantiene la confidencialidad y datos
de ubicación de sus clientes, así como la reserva bancaria sobre su información, de acuerdo
con sus políticas internas.
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 60 de 329
```
## 2.7.3.5. Liquidación y Pago de Reclamos

```
La forma de compensación y pago de las reversiones, devoluciones de ACH, reintegros y devoluciones
de pagos complementarios, que lo requieran se realiza a través de la planilla de compensación del
ciclo 4 de ACH Colombia de cada día operacional, para lo cual se debe tener presente que todas las
novedades anteriormente relacionadas y que impliquen reintegro de dinero y que se autoricen por
parte de la Entidad Participante receptor en el módulo de reclamos antes de las 3: 00 pm, serán
compensadas el mismo día hábil; las autorizaciones de reversiones que se realicen después de este
tiempo serán procesadas en la planilla de compensación del ciclo 4 del día siguiente hábil.
```
```
La compensación de las reversiones, devoluciones de ACH, reintegros y devoluciones de pagos
complementarios, se reflejarán en la planilla de compensación de las Entidades Participantes en el
campo “Reversiones” a “favor” del participante que radico la novedad y en contra de la Entidad
Receptora de la novedad. El detalle de los valores compensados por efectos de reintegro de dineros
por las novedades debe ser descargados por el aplicativo de reclamos.
```
## 2.7.4. Manejo de solicitudes de certificación Información de carácter Confidencial

Es un servicio solicitado por una Entidad Participante a ACH COLOMBIA o a otra Entidad Participante de generar
una constancia o certificación de un proceso exitoso o errado específico efectuado.

```
− Solicitud de Certificación a ACH COLOMBIA; uno de los requerimientos es certificar que una
transacción específica, ha sido procesada en una fecha determinada por ACH COLOMBIA y enviada al
participante Receptor para su posterior proceso.
```
```
− Solicitud de Certificación a una Entidad Participante: Pueden existir diversas solicitudes de
certificaciones. Destacamos las siguientes:
```
```
− Que una transacción haya sido aplicada en la cuenta o deposito electrónico del Usuario
Receptor en una fecha específica, por solicitud de una Entidad Participante Originadora o de
un Usuario Originador
```
```
− Que una transacción haya sido devuelta por una razón y en una fecha específica
```
```
− Que un proceso de aplicación de devoluciones o prenotificaciones no haya sido exitoso
```
## 2.7.4.1. Condiciones para Solicitar Certificaciones

```
Si el participante requiere certificar que una transacción específica, ha sido procesada en una fecha
determinada por ACH COLOMBIA y enviada al participante Receptor para su posterior proceso,
puede hacerlo creando la solicitud correspondiente a través del módulo de reclamos. Una vez creada
la solicitud de certificación a ACH COLOMBIA, el módulo despliega los datos del solicitante, el texto
de certificación y las transacciones certificadas con su correspondiente firma autorizada en ACH
COLOMBIA. Si para el participante le basta con esta certificación en ventana y no necesita una en
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 61 de 329
```
```
papel membretado puede anular el caso sin enviarlo para que no se siga ningún proceso dentro de
ACH COLOMBIA; si requiere la certificación en papelería especial se envía el caso con el fin de que
ACH COLOMBIA genere el listado de transacciones en la papelería correspondiente.
```
## 2.7.4.2. Novedad de Solicitud de Certificación – Entidad Participante Originadora del caso

```
El procedimiento para solicitar una certificación a ACH COLOMBIA o certificación a una Entidad
Participante a través del módulo de reclamos es el siguiente:
```
```
− La Entidad Participante debe crear el caso de solicitud de certificación a ACH COLOMBIA o
solicitud de certificación al participante, según sea el caso. Si el usuario ha seleccionado
transacciones para incluir al caso principal quiere decir que esas transacciones efectivamente
son certificadas por el participante o por ACH COLOMBIA y por tanto se cumple el objetivo.
− El usuario debe seleccionar si requiere la certificación con logo o sin logo del participante o
ACH COLOMBIA, luego hacer clic en el botón “Aceptar” con lo cual aparecerá una ventana
con la prevista de la certificación con logo o sin logo, de acuerdo con la selección.
− En esta ventana aparecen los datos del solicitante, el texto de certificación y las transacciones
certificadas con su correspondiente firma autorizada en ACH COLOMBIA.
− El módulo de reclamos muestra la solicitud en ventana y le permite al usuario ordenar la
impresión de esta en su PC.
```
## 2.7.4.3. Solución Parcial o Total a Novedad de Certificación – EPR del Caso

```
− El usuario de el participante Receptor del caso de solicitud de certificación, con el perfil y
permisos adecuados, debe ingresar al módulo de reclamos para recibir e iniciar la gestión de
certificar.
− La Entidad Participante Receptor del caso de solicitud de certificación, de acuerdo con sus
procedimientos internos, valida la información que recibe a través del módulo de reclamos
de ACH COLOMBIA, verificando la información a certificar.
− La Entidad Participante Receptor del caso de solicitud de certificación procede a tramitar la
certificación a través del módulo de reclamos donde el usuario digita las observaciones o
respuesta de certificación, la cual puede ser vista e impresa por el participante originador de
la solicitud de certificación como respuesta a su caso.
```
## 2.7.5. Manejo de reversiones transacciones crédito

Se entiende por reversión de transacciones crédito, la solicitud de una Entidad Participante a otra Entidad
Participante para intentar recuperar dineros abonados por error de la entidad o del usuario originador. Algunos
de los errores que puede cometer cualquiera de los participantes del sistema ACH COLOMBIA, se resumen a
continuación:

```
− Envío de transacciones, lotes o archivos más de una vez.
− Envío de transacciones por valores diferentes a los pactados o a los facturados (valores mayores o
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A

```
Agosto de 2024
Página 62 de 329
```
```
menores).
− Envío de transacciones en fechas que no corresponden.
− Envío de transacciones a otro tipo o número de cuenta.
```
## 2.7.5.1. Responsabilidades de los Participantes

```
Dependiendo de cuál de los participantes del sistema ACH COLOMBIA comete el error, así se
determinan las responsabilidades y las acciones a seguir.
```
### ACCIONES POR SEGUIR EN CASO DE ERROR

```
Error generado por... Conoce al Cliente Receptor No conoce al Cliente Receptor
Usuario Originador El Usuario Originador hace la
gestión ante el Usuario
Receptor
```
```
La Entidad Participante Receptor
hace la gestión para recuperar el
dinero.
Entidad Participante
Originadora
```
```
N/A La Entidad Participante Receptor
hace la gestión para recuperar el
dinero.
Entidad Participante
Receptor
```
```
La Entidad Participante
Receptor hace la gestión para
recuperar el dinero.
```
### N/A

```
Cuando el Usuario Originador envía transacciones por error, el participante originador asume la
responsabilidad ante el sistema ACH COLOMBIA.
```
```
La Entidad Participante Originadora decide si traslada al Usuario Originador o no los costos que el
proceso de Reversión origen.
```
## 2.7.5.2. Condiciones para Solicitar Reversiones Crédito

```
Las entidades participantes en el sistema ACH COLOMBIA, deben tener en cuenta las siguientes
recomendaciones antes de solicitar la reversión de transacciones crédito enviadas por error:
```
```
− Que la entidad que comete el error verifique detalladamente que las solicitudes de reversión,
específicamente aquellas requeridas por los Usuarios Originadores, correspondan a errores
cometidos y no a posibles fraudes.
− Que ACH COLOMBIA actúa como mediador del proceso de reversiones y, por lo tanto, si el
participante solicita la reversión a través del módulo de “Reclamos”, deberá suspender
cualquier otro intento de reversión por otro medio, para así evitar aplicaciones dobles a la
cuenta del Usuario Receptor.
− Que las Entidades Participantes ajusten el reglamento de los contratos de cuenta corriente
y/o de ahorros, de manera que se garantice mediante una cláusula, que el participante
Receptor puede efectuar en cualquier momento, débitos o créditos a la cuenta del Usuario
Receptor, por concepto de transacciones aplicadas por error, es decir por reversiones.
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A

```
Agosto de 2024
Página 63 de 329
```
## 2.7.5.3. Novedad de Reversión – Entidad Participante Originadora del caso

```
La reversión de transacciones consiste en realizar la solicitud de reversión ante el participante a
través del módulo de reclamos.
```
```
El procedimiento para solicitar a través de ACH COLOMBIA, reversiones de transacciones crédito
enviadas por error de un Usuario Originador de una Entidad Participante Originadora es el siguiente:
```
```
− El usuario del participante que origina la reversión, con el perfil y permisos adecuados, debe
ingresar al módulo de reclamos y registrar la información del nuevo caso principal “REV” en
el formulario que despliega el sistema.
− Posteriormente debe escoger los lotes y/o transacciones del sistema de reclamos para que
sean adicionados al caso principal “REV”. Los lotes y transacciones pueden estar destinados
a Entidades Participantes diferentes.
− Una vez el caso principal “REV” contiene al menos un lote o una transacción, está listo para
el siguiente paso que es su envío a las Entidades Participantes destino.
− El módulo de reclamos valida que las transacciones a reversar mayores o iguales a
$100.000.000.00 sean autorizadas por el Administrador de Integra ACH para su envío. Esta
función le permite a la Entidad Participante Receptor del caso, es decir a la entidad que paga,
ver solamente las transacciones aprobadas para reversión por el participante originador del
caso.
```
## 2.7.5.4. Solución Parcial o Total a Novedad de Reversión – EPR del Caso

```
− El usuario del participante Receptor del caso de reversión, con el perfil y permisos adecuados,
debe ingresar al módulo de reclamos para recibir e iniciar la gestión de dar solución parcial o
total a la solicitud de reversión.
− La Entidad Participante Receptor del caso de reversión, de acuerdo con sus procedimientos
internos, valida la información que recibe a través del módulo de reclamos de ACH
COLOMBIA, verificando que se haya aplicado una transacción crédito a esa cuenta o deposito
electrónico con anterioridad, que el cliente no haya cancelado o cerrado la cuenta o deposito
electrónico, que no esté en proceso jurídico, y otros datos o condiciones adicionales de la
transacción o de la cuenta.
− La Entidad Participante Receptor del caso de reversión procede a tramitar la reversión,
aplicando el procedimiento estipulado para cada caso de acuerdo con lo siguiente:
```
```
REV01:
Reversión que se solicita cuando el participante originador es quien comete el error. En este caso la
devolución del dinero debe ser inmediata sin solicitar autorización al usuario, ya que es el
participante originador la que asume toda responsabilidad por la solicitud de reversión. No aplica el
cobro del 4*1000.
```
```
Excepción:
Para el caso en que la cuenta receptora pertenezca a una entidad oficial o una cuenta embargada no
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A

```
Agosto de 2024
Página 64 de 329
```
```
podrá ser objeto del débito hasta obtener autorización de la entidad competente.
```
```
REV02:
Devolución inmediata por parte del participante Receptor, cuando se confirme que, por error de
ésta, la transacción quedó aplicada a una cuenta o deposito electrónico errado.
```
```
REV03 y REV04:
Se debe obtener autorización por parte del Usuario Receptor para realizar el débito, por tratarse de
un error originado en el Usuario Originador.
```
```
− El módulo de reclamos controla el pago de las reversiones que se lograron recuperar, ya sea
monto parcial o total, exigiendo al Administrador la “autorización de pago” para todas las
reversiones sin importar su monto.
− Posteriormente debe entregar las pruebas y documentación pertinentes al Usuario Receptor
por el débito realizado, indicando que el débito fue aplicado por concepto de reversión.
− La reversión de la transacción en cuentas corrientes podrá hacerse contra el cupo de
sobregiro autorizado, a discreción del participante Receptor del caso de reversión. En el caso
de las cuentas de ahorro, o de las cuentas corrientes sin fondos disponibles o sin cupo de
sobregiro autorizado, el participante Receptor del caso de reversión debe hacer el número
de reintentos posible, por un periodo máximo de 60 días hábiles para recuperar total o
parcialmente los dineros abonados por error. Si pasados los 60 días hábiles de reintentos, no
se logra recuperar el dinero abonado por error, el caso se dará como cerrado o expirado por
el participante Receptor al momento de generar el reporte de resultados. La Entidad
Participante Originadora del caso de reversión debe utilizar otros medios para buscar la
recuperación del dinero o solicitar la reapertura del caso a través del módulo de reclamos.
− La Entidad Participante Receptor del caso de reversión debe indicar a sus Clientes Receptores
que los débitos fueron aplicados por concepto de reversiones. La Entidad Participante
Receptor del caso de reversión puede entregar al Cliente Receptor la documentación
probatoria del error.
− La Entidad Participante Receptor del caso de reversión debe hacer su mejor esfuerzo por
conseguir los dineros abonados a sus Clientes Receptores por error, sin embargo, no garantiza
que éstos se puedan recuperar.
− Para que la labor de recuperación sea exitosa, se recomienda tener en cuenta lo siguiente:
− Realizar intento de débito inmediato a la cuenta del Cliente Receptor, una vez recibida
la solicitud de reversión.
− En caso de no ser exitosa la labor inicial de recuperación, se deberá hacer un reintento
adicional como máximo una semana después del primer intento.
− Si este intento es nuevamente fallido, el participante Receptor podrá contactar al
Cliente Receptor y llegar a un acuerdo con él.
− Dependiendo del acuerdo a que hayan llegado el participante Receptor y el Cliente
Receptor, el participante Receptor podrá realizar nuevamente otro intento de débito.
− Se recomienda revisar los movimientos históricos en la cuenta del Cliente Receptor,
que permita presumir las fechas en que le serán realizados abonos a la cuenta del
Cliente Receptor, y tratar de hacer los intentos en las fechas más oportunas.
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A

```
Agosto de 2024
Página 65 de 329
```
```
− La Entidad Participante Receptor del caso de reversión mantiene la confidencialidad y datos
de ubicación de sus clientes, así como la reserva bancaria sobre su información, de acuerdo
con sus políticas internas.
```
## 2.7.5.5. Compensación de Fondos Recuperados por Reversiones

```
La Entidad Participante Receptor del caso de reversión puede dar un avance o solución total a través
del módulo de reclamos, de acuerdo con la gestión de recuperación de fondos efectuada, registrando
la fecha del abono parcial o total e ingresando el valor que se recupera y se abona a la reclamación
original, para que estos valores sean pagados por compensación en el ciclo 4.
```
## 2.7.5.6. Cargos a la Cuenta del Cliente Receptor

```
El Cliente Originador no debe pagar cargos adicionales por transacciones débito producto de
reversiones; sin embargo, el participante originador de la reversión debe pagar las sanciones
definidas por ACH COLOMBIA para este tipo de transacción.
```
```
La Entidad Participante Receptor de la novedad no debe aplicar cargos a la cuenta o deposito
electrónico del Cliente Receptor ni a el participante originador por motivo de impuestos, comisiones
o cualquier otro cargo diferente a la propia reversión, por ejemplo: impuesto a las operaciones
financieras.
```
```
Es potestad de el participante originador del caso de reversión, trasladar la sanción cobrada al Cliente
por motivo de transacciones generadas por éste en forma errada.
```
## 2.7.5.7. Excepciones al Procedimiento de Reversión

```
− En caso de afectar más de una vez la cuenta o deposito electrónico del Cliente Receptor como
consecuencia de haber iniciado más de un proceso de reversión crédito, la entidad que
comete el error, debe pagar a el participante Receptor de la reversión una sanción, según lo
definido en el capítulo 3. y en el Anexo 6: Eventos Sancionables del Esquema de Calidad.
```
```
− La Entidad Participante Receptor que no valide la Identificación del Cliente Receptor cuando
le sea solicitado, y que por esa causa abone el dinero en una cuenta errada o por valor errado,
deberá reintegrar el dinero al participante originador, a más tardar al día siguiente hábil de la
solicitud de reversión, previa comprobación del error.
− La Entidad Participante Receptor no puede utilizar los dineros abonados por error de el
participante originador, o de ella misma, para efectuar cargos a la cuenta del Cliente Receptor
por conceptos y compromisos que éste tenga pendiente de pago a favor del participante
Receptor, siempre y cuando el error se notifique al participante Receptor antes de efectuar
el cierre de los procesos del día establecidos por ACH COLOMBIA.
− Cuando una autoridad competente con fundamento en normas legales solicite la devolución
del dinero a el cliente del participante Receptor, el participante originador, directamente o a
nombre del Cliente Originador, debe restituir a el participante Receptor los valores que ésta
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 66 de 329
```
```
le haya reintegrado por medio de este procedimiento. Este reintegro se realiza mediante el
mecanismo de compensación, una vez se haya comprobado la existencia del mandato legal.
```
## 2.7.6. Manejo de Devoluciones por Transacciones ACH no Consentidas

Con base en el Acuerdo Interbancario para Gestionar el Riesgo Operativo Originado en Transacciones No
Consentidas por los Clientes, Representadas en Anotaciones en Cuenta, aprobado por la Junta Directiva de
Asobancaria, se definió el manejo de unas causales de devolución especiales originadas en el hecho de haberse
presentado una o varias transacciones crédito y/o débito ACH, los cuales no son reconocidos por el usuario
Originador para el caso de las transacciones crédito y no reconocidas por el usuario Receptor para el caso de
transacciones débito. El acuerdo mencionado se encuentra en el Anexo 19 de presente Manual.

Las causales establecidas para este trámite son las siguientes:

DEV07:
Solicitud de Devolución del valor de una Transacción crédito ACH no Consentida. Aplica en el evento en que
una Entidad Participante Originadora solicite a una Entidad Participante Receptor la devolución de los fondos
de operaciones ACH crédito que se constituyen como no consentidas por los usuarios originadores.

DEV14:
Solicitud de Devolución del valor de una Transacción débito ACH no Consentida. Aplica en el evento en que
una Entidad Participante Receptor solicite a una Entidad Participante Originadora la devolución de los fondos
de operaciones ACH débito que se constituyen como no consentidas por los usuarios receptores.

## 2.7.7. Manejo de Reintegros por Transacciones PSE

Con base en el Acuerdo Interbancario para Gestionar el Riesgo Operativo Originado en Transacciones No
Consentidas por los usuarios, Representadas en Anotaciones en Cuenta, aprobado por la Junta Directiva de
Asobancaria, y de acuerdo con el decreto 587 de abril de 2016, se establece los eventos en que procede el
reintegro de transacciones realizadas a través del botón de PSE.

Las causales establecidas para este trámite son las siguientes:

REI08: Transacción No Consentida PSE

Solicitud de reintegro por transacción PSE no Consentida. Aplica en el evento en que una Entidad Participante
Autorizadora solicite a una Entidad Participante Recaudadora la devolución de los fondos de operaciones PSE
que se constituyen como no consentidas por los usuarios originadores. Este procedimiento aplica para las
transacciones que en PSE estén en estado “Aprobado” y que hayan sido compensadas en alguno de los ciclos
de operación.

Para aquellas transacciones de PSE en estado “Aprobado” y que no hayan sido compensadas se estableció un
proceso en el cual el participante Autorizador que identifique que se trata transacciones no consentidas por
su cliente, realiza un cambio de estado a “Rechazado” con el fin que dichas operaciones no se compensen y
poder así reintegrarle el dinero a su cliente.


### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 67 de 329
```
### REI09:

Solicitud de reintegro por transacción PSE objeto de fraude; aplica en el evento en que una Entidad Participante
Autorizadora solicite a una Entidad Participante Recaudadora el reintegro de una transacción PSE que se
encuentra en estado aprobado y compensada a través del sistema de ACH Colombia, y que de acuerdo con
soportes suministrados por el usuario autorizador corresponde a una transacción objeto de un fraude.

REI10:
Solicitud de reintegro por Transacción PSE Operación No solicitada; aplica en el evento en que una Entidad
Participante Autorizadora solicite a una Entidad Participante Recaudadora el reintegro de una transacción PSE
que se encuentra en estado aprobado y compensada a través del sistema de ACH Colombia, y que de acuerdo
con soportes suministrados por el usuario no fue realizada por él.

REI11:
Solicitud de reintegro por Transacción PSE producto adquirido no recibido; aplica en el evento en que una
Entidad Participante Autorizadora solicite a una Entidad Participante Recaudadora el reintegro de una
transacción PSE que se encuentra en estado aprobado y compensada a través del sistema de ACH Colombia, y
que de acuerdo con soportes suministrados por el usuario el producto comprado no fue recibido por el usuario.

REI12:
Solicitud de reintegro por Transacción PSE producto entregado no corresponde a lo solicitado; aplica en el
evento en que una Entidad Participante Autorizadora solicite a una Entidad Participante Recaudadora el
reintegro de una transacción PSE que se encuentra en estado aprobado y compensada a través del sistema de
ACH Colombia, y que de acuerdo con soportes suministrados por el usuario el producto comprado no
corresponde a lo que solicito a través de la página web del comercio.

REI13:
Solicitud de reintegro por Transacción PSE producto entregado defectuoso; aplica en el evento en que una
Entidad Participante Autorizadora solicite a una Entidad Participante Recaudadora el reintegro de una
transacción PSE que se encuentra en estado aprobado y compensada a través del sistema de ACH Colombia, y
que de acuerdo con soportes suministrados por el usuario el producto comprado y recibido por el usuario
presenta defectos.

El procedimiento específico en cada uno de los casos de reversión por transacciones no consentidas ACH o PSE
fue definido por el Grupo de Trabajo Cuentas Receptoras – Comité Operativo ACH y la ASOBANCARIA y se
encuentra en el Anexo 19 Reglamento Operativo – ASOBANCARIA.

## 2.7.8. Manejo de Devoluciones Pagos Complementarios Seguridad Social

Se entiende por devolución de pagos complementarios de seguridad social, el reintegro de una transacción
que hace una Entidad Participante Receptor de un pago de cuentas AFC o Libranzas a una Entidad Participante
Originadora de pagos AFC o Libranzas, dicho reintegro obedece a un valor que no fue aplicado exitosamente a
la cuenta o libranza receptora de acuerdo con las causales establecidas para dichos servicios. (Ver documento
relacionado, instructivo de manejo de devoluciones de PCSS- cuentas AFC y Libranzas por el módulo de
reclamos.doc).


### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 68 de 329
```
Las causales establecidas para este trámite son las siguientes:

DPC001:
Devolución del valor de una Transacción de cuentas AFC, pagos complementarios; aplica en el evento en que
una Entidad Participante Receptor de una transacción de cuentas AFC, realiza la devolución del dinero de
acuerdo con las causales de rechazo establecidas para dicho servicio.

DPC002:
Devolución del valor de una Transacción de Libranzas, pagos complementarios; aplica en el evento en que una
Entidad Participante Receptor de una transacción de Libranzas, realiza la devolución del dinero de acuerdo con
las causales de rechazo establecidas para dicho servicio.

## 2.8. Comité de Análisis de Reclamos

## 2.8.1. Definición

Es la primera instancia de resolución de conflictos, usada en el evento que surjan reclamaciones entre alguna
Entidad Participante y ACH COLOMBIA o entre Entidades Participantes.

En caso de que tales reclamaciones consecuencia de operaciones efectuadas a través del sistema ACH o del
sistema PSE no puedan ser resueltas por las partes en conflicto en forma directa, conforme al anterior
mecanismo, se intentarán resolver en segunda medida de conformidad con el Comité de Solución de
Conflictos.

## 2.8.2. Objetivo

El objetivo del Comité de Análisis de Reclamos es analizar, evaluar y decidir sobre las conductas cuestionadas
por las Entidades Participantes, frente a las posibles sanciones por el incumplimiento del Esquema de Calidad
y/o soluciones a las novedades radicas en el módulo de reclamos del facturador de ACH Colombia.

## 2.8.3. Partícipes

En el Comité de Análisis de Reclamos participan siete (7) representantes de las Entidades vinculadas al sistema
(5 entidades principales y 2 entidades delegadas) y un representante de ACH COLOMBIA que participa
únicamente como asistente, es decir con voz, pero sin voto en las decisiones de este comité. Este comité es
nombrado para un período fijo de un (1) año; el primer año se conforma por el tiempo que falte para terminar
el año. Cada entidad debe nombrar un representante principal y un suplente.

Si alguna de las Entidades Participantes que participa en el Comité de Análisis de Reclamos se encuentra
incluida dentro de las conductas cuestionadas, debe marginarse de las discusiones y decisiones
correspondientes a ese caso particular. Sin embargo, previendo la situación, ACH COLOMBIA cita a la(s)
Entidad(es) Participante(s) delegadas con anticipación, de tal manera que se asegure el quórum requerido para
la toma de decisiones.

La participación de ACH COLOMBIA en el Comité de Análisis de Reclamos es la de facilitador y de proveedor de


### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 69 de 329
```
la información necesaria para el análisis de las situaciones que se presenten.

## 2.8.4. Responsable del Comité De Análisis de Reclamos en ACH Colombia

El responsable del Comité de Análisis de Reclamos en ACH Colombia es el área de operaciones en cabeza del
Gerente de Servicio al cliente quien tendrá dentro de sus actividades:

```
− Elección de los participantes de las Entidades en el Comité de Análisis de Reclamos.
− Participar en el comité de Análisis de Reclamos como representante de ACH Colombia.
− Realizar la citación al Comité de Análisis de Reclamos cuando se requiera
− Liderar la reunión del Comité de Análisis de Reclamos
− Realizar el Acta del Comité de Análisis de Reclamos
− Enviar el reporte de las decisiones y recomendaciones del Comité de Análisis de Reclamos a
las Entidades afectadas
− Hacer seguimientos a las Entidades Participantes afectadas en el caso.
```
## 2.8.5. Elección de los partícipes

El Comité de Análisis de Reclamos se elige entre las Entidades partícipes del sistema ACH y del sistema PSE,
teniendo en cuenta el total de las transacciones acumuladas en el último trimestre, tanto originadas como
recibidas por cada una de ellas, en el momento de la elección.

Una vez sumadas las transacciones originadas y recibidas, se organizan las Entidades Participantes de mayor a
menor y luego se clasifican en tres (3) grupos:

```
− El primer grupo corresponde al primer 50% de las Entidades Participantes organizadas por el
criterio anteriormente descrito, del cual se eligen las primeras cuatro (4) Entidades
Participantes con más transacciones originadas y recibidas.
− El segundo grupo corresponde al siguiente 33% de las Entidades Participantes, del cual se
eligen las dos (2) primeras Entidades Participantes con más transacciones originadas y
recibidas.
− El tercer grupo corresponde al último 17% de las Entidades Participantes, del cual se elige la
primera, con más transacciones originadas y recibidas.
```
Si alguna(s) de la(s) Entidad(es) Participante(s) no desea(n) o se ve(n) imposibilitada(s) para participar en las
reuniones del Comité de Análisis de Reclamos, se escoge la siguiente Entidad dentro del grupo
correspondiente.

El mecanismo de desempate en aquellos casos en que los criterios de número de transacciones acumuladas,
tanto originadas como recibidas en cada grupo, resulten iguales en dos o más Entidades Participantes, es
seleccionar la Entidad que haya originado y recibido el mayor número de transacciones en el mes
inmediatamente anterior al mes en que se realiza la elección.


### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 70 de 329
```
## 2.8.6. Periodicidad de las reuniones

El comité se reúne cuando las circunstancias así lo requieran.

## 2.8.7. Citación a las reuniones

El comité es citado por ACH COLOMBIA, indicando la agenda de casos a revisar. ACH COLOMBIA cita a la(s)
Entidad(es) Participante(s) delegadas con anticipación, en los casos en que alguna(s) Entidad(es) Participante(s)
se viera(n) obligada(s) a marginarse de la discusión y decisión del caso en cuestión. ACH COLOMBIA debe
notificar a todos los partícipes del comité la realización o cancelación de las reuniones programadas o
extraordinarias.

## 2.8.8. Reuniones

Las reuniones se efectúan en las instalaciones de ACH COLOMBIA a las 8:30 a.m. del día respectivo. Hay quórum
cuando estén presentes al menos tres (3) miembros del comité, entidades principales o delegadas. Las
decisiones deben ser tomadas por mayoría. En caso de que el número de participantes sea cuatro (4) en el
momento de decidir, y haya empate en la decisión, se aplaza dicha decisión hasta que se complete la mayoría.

A cada reunión asiste un representante por cada una de las Entidades Participantes elegidas. En cada reunión,
se elige un presidente entre las Entidades Participante asistentes, quien firma conjuntamente el acta de la
reunión con el representante de ACH COLOMBIA.

## 2.8.9. Metodología de trabajo

En las reuniones se sigue el siguiente orden del día:

1. Verificación de quórum y elección de presidente.
2. Aprobación del acta anterior.
3. Presentación de ACH COLOMBIA de las conductas cuestionadas enviadas por las Entidades
    Participantes.
4. Análisis de las conductas cuestionadas.
5. Decisión sobre las conductas cuestionadas.
6. Varios y propuestas.

## 2.8.10. Actas

De cada una de las reuniones se deja constancia en el acta correspondiente de las decisiones y propuestas
tomadas por el comité.

## 2.8.11. Informe a las Entidades

Las decisiones y recomendaciones que adopte el Comité de Análisis de Reclamos son informadas por ACH
COLOMBIA a las Entidades Participantes que se vieron involucradas en la decisión.


### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 71 de 329
```
## 2.9. Esquema de Facturación

Este numeral se enuncia la forma en que se realiza el proceso de facturación de los servicios prestados por
ACH COLOMBIA, y los conceptos involucrados, según las Tarifas y Comisiones establecidas en Tarifario Oficial
que se envía a las Entidades Participantes anualmente.

## 2.9.1. Facturación servicio ACH COLOMBIA

ACH COLOMBIA factura el valor correspondiente a las transacciones procesadas por el sistema de forma
mensual.

## 2.9.1.1. Conceptos Involucrados

### INFORMACIÓN FACTURA

### ÍTEM DESCRIPCIÓN

```
Servicio Proveedor Tecnológico
```
```
Servicio prestado por ACH Colombia como Proveedor
Tecnológico de las Entidades Participantes para los diferentes
servicios.
El detalle de las Tarifas con los Esquemas de Facturación
correspondientes a cada servicio se encuentra en el Tarifario
Oficial que se envía a las Entidades Participantes anualmente.
```
## 2.9.1.2. Factura de Venta

```
ACH COLOMBIA emite mensualmente una única Factura de Venta (Ver Anexo 8: Factura de Venta),
correspondiente al cobro de los servicios anteriormente descritos. En esta factura se discrimina
adicionalmente el valor de retención por el impuesto de timbre, retención en la fuente, retención de
ICA y retención sobre el IVA facturado.
```
```
ACH COLOMBIA envía la factura, el quinto día hábil del mes siguiente al mes facturado. Este
documento es enviado vía factura electrónica a los correos autorizados en cada una de las Entidades
Participantes vinculadas.
```
## 2.9.1.3. Pago del Servicio ACH COLOMBIA

```
El valor de la Factura de Venta debe ser cancelado a través del participante designado por ACH
COLOMBIA, a través del sistema SEBRA (CUD), teniendo en cuenta los datos de pago descritos en la
siguiente tabla:
```
### DATOS PARA PAGAR FACTURACIÓN A ACH COLOMBIA

```
Nombre ACH COLOMBIA S.A.
NIT 830078512 - 6
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 72 de 329
```
### DATOS PARA PAGAR FACTURACIÓN A ACH COLOMBIA

```
Entidad Financiera BANCO DE OCCIDENTE
Cuenta SEBRA 62012307
Código de Operación 117 pago de Comisiones, Servicios y Sanciones (Gravado)
Valor de la Transacción Crédito TOTAL (Valor total a pagar en la Factura de Venta recibida).
```
## 2.9.2. Cobro interbancario

ACH COLOMBIA a través del módulo de facturación permite la generación de los soportes de cobro
correspondientes a los valores netos por los diferentes conceptos de Tarifas y Servicios que se causen a cargo
y/o a favor de las Entidades Participantes.

## 2.9.2.1. Conceptos Involucrados

```
En el reporte de “Distribución de Pagos” que corresponde al cobro interbancario se involucran
diferentes conceptos:
− Tarifas de acceso a la red: Valor a cobrar o a pagar por el participante, por concepto del
proceso de las transacciones recibidas o enviadas a través del sistema ACH Colombia como
contraprestación por el procesamiento de cada transacción recibida, de acuerdo con las
tarifas señaladas en el Capítulo 7. TARIFAS, de este manual.
− Sanciones: Valor a pagar o cobrar por el participante a otras Entidades Participantes o a ACH
COLOMBIA por eventos sancionables, según se especifica en el Capítulo 3. Esquema de
calidad, de este manual.
```
## 2.9.2.2. Soportes por Cobro Interbancario

```
ACH COLOMBIA, en nombre de las Entidades Participantes, genera los soportes de cobro
correspondientes a los valores netos por los diferentes conceptos de Tarifas de acceso a la red,
Sanciones y Servicios que se causen a cargo y/o a favor de las Entidades Participantes.
```
### SOPORTES POR COBRO INTERBANCARIO

```
Reporte Descripción
```
```
Comisión Tarifas de
acceso a la red
```
```
El Módulo de facturación de ACH Colombia, permite a las Entidades
Participantes generar reporte para el pago/cobro de comisiones entre
entidades de un período determinado.
Detalle de
Comisiones Para
Pagar y/o Cobrar
```
```
El Módulo de facturación de ACH Colombia, permite a las Entidades
Participantes generar reporte con el detalle de comisiones a pagar y/o
cobrar de una Entidad a las demás Entidades Participantes de un período
determinado.
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 73 de 329
```
```
Distribución de
pagos
```
```
El Módulo de facturación de ACH Colombia, permite a las Entidades
Participantes generar reporte del consolidado de sanciones, servicios y
comisiones de acceso a la red de un período determinado.
```
## 2.9.2.3. Pago de los Cobros Interbancarios

```
El valor neto (a favor o en contra) del reporte de “Distribución de Pagos para cada Entidad
Participante” correspondiente al período específico, será compensado en el ciclo 2 del sistema ACH
Colombia, en la fecha entregada por la Gerencia de facturación mediante entrega de los respectivos
soportes, dicho valor será registrado en la planilla de compensación definitiva del ciclo 2 en el bloque
“Comisiones” “A Favor” o “En Contra” dependiendo de cada Entidad Participante.
```
## 2.10. Transacción Crédito

A continuación, se describe el proceso y detalle operativo de la transacción crédito, involucrando las
transacciones de prenotificaciÃ³n crédito.

## 2.10.1. Esquema general de operación transacción crédito

## 2.10.2. Flujo de la transacción crédito

Transacción Crédito consiste en dispersar fondos desde una cuenta o deposito electrónico de un usuario
(Usuario Originador) de una Entidad Participante (Entidad Participante Originadora) hacia una o varias cuentas
o depósitos electrónicos de usuarios (Usuario Receptor) en otras Entidades Participantes (Entidad Participante
Receptor).

```
− Para realizar una transacción crédito, el Usuario Originador debe dar la orden de dispersión (débito a
su cuenta o deposito electrónico) de fondos desde la cuenta o deposito electrónico que mantiene en
su Entidad Participante (Entidad Participante Originadora), utilizando el medio y bajo las condiciones
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 74 de 329
```
```
operativas, técnicas y legales que su Entidad Participante le indique. En esta orden, debe indicar el
detalle de las cuentas o depósitos electrónicos, valores y clientes que van a ser afectados en otras
Entidades Participantes (Entidades Participantes Receptoras).
− El Usuario Originador puede, de forma opcional, previo al envío de la transacción monetaria crédito,
enviar una transacción de prenotificación para validar los datos y condiciones de la cuenta o depósitos
electrónicos del Usuario Receptor, así como también puede de forma opcional, solicitar a el
participante Receptor, validar la identificación del Usuario Receptor.
− Si la transacción monetaria crédito o de prenotificación no resultan exitosas, el participante Receptor
genera una transacción de devolución en los plazos definidos, indicando la causa específica, de
acuerdo con las causales del anexo 9 “Causales de devolución.
− La Entidad Participante Originadora o el Usuario Originador pueden enviar la transacción crédito a
través del sistema ACH COLOMBIA con destino a el participante Receptor, en cualquier momento, o
después de conocer que la prenotificación crédito fue exitosa, si fue su decisión enviarla antes de la
transacción crédito inicial.
− La Entidad Participante Receptor debe aplicar en el sistema interno de cuentas, las transacciones
crédito en la fecha efectiva enviada por el Usuario Originador. La Entidad Participante Receptor puede
hacer validaciones específicas de la información recibida contra la información de sus cuentas o
depósitos electrónicos.
− El abono a la cuenta o depósito electrónico del Usuario Receptor, lo debe realizar el participante
Receptor, el mismo día en que recibe las transacciones crédito desde ACH COLOMBIA, y en los horarios
indicados en el numeral 2.4. Ciclos de Proceso y Horarios.
− Si la transacción de Devolución del crédito no es generada por el participante Receptor, en los horarios
definidos, la transacción crédito se entenderá como aplicada exitosamente.
− ACH COLOMBIA procesa las Transacciones Monetarias Crédito, de Prenotificación Crédito y de
Devolución Crédito que cumplan con el formato NACHA-M.
```
## 2.10.3. Uso de la prenotificación crédito

La Entidad Participante Originadora o el Usuario Originador a su discreción, pueden generar una
prenotificación crédito para cada Usuario Receptor, como transacción previa a la transacción crédito, por una
vez.

La transacción de prenotificación ayuda a que el proceso crédito se dé correctamente y que las transacciones
subsiguientes sean aplicadas apropiadamente a la Cuenta del Usuario Receptor por el participante Receptor.

## 2.10.3.1. Control de Prenotificaciones

```
El control de las transacciones de prenotificación que se ha enviado, es responsabilidad del
participante originador o del Usuario Originador, según el acuerdo que exista entre las partes.
```
```
Si la transacción de devolución de prenotificación no es generada por el participante Receptor en los
horarios y plazos definidos, se entenderá que la prenotificación ha sido exitosa.
```
## 2.10.3.2. Tiempos para iniciar la Prenotificación


### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 75 de 329
```
```
La Entidad Participante Originadora debe realizar la prenotificación mínimo tres (3) días hábiles antes
de la Fecha Efectiva de la primera transacción crédito.
```
## 2.10.4. Transacciones de devolución

Si el proceso de verificación derivado de la transacción de prenotificación o de la transacción crédito no es
exitoso, el participante Receptor deberá generar una transacción de devolución con destino a el participante
originador.

La Entidad Participante Receptor, a través de la Transacción de Devolución, informa a el participante originador
a través del sistema ACH, que la transacción no fue aceptada por no cumplir con las condiciones establecidas
o porque no fue aceptada por el Usuario Receptor.

La Entidad Participante Originadora debe verificar la razón de la devolución e informar al Usuario Originador
para iniciar nuevamente la transacción de prenotificación o la transacción crédito, si es del caso.

## 2.10.4.1. Causales de Devolución

```
La Entidad Participante Receptor debe informar la descripción y código estándar de la devolución, y
en lo posible, informar el detalle de la devolución.
```
```
En el Anexo 9: Causales de Devolución, se presentan los códigos y las causales específicas que deben
ser usadas para las transacciones de prenotificación crédito y para las transacciones monetarias
crédito.
```
## 2.10.4.2. Plazos para Iniciar una Transacción de Devolución Crédito

```
− Entidad Participante Receptor
La Entidad Participante Receptor debe generar la transacción de devolución de prenotificación y
de la transacción de devolución crédito, como máximo en los horarios establecidos en el numeral
2.4. Ciclos de Proceso y Horarios.
```
```
− Cliente Receptor
```
```
Si el participante Receptor recibe una solicitud de no aceptación (reclamo) de la transacción
crédito por parte del Usuario Receptor, puede generar una transacción de devolución crédito
indicando la causal específica, a más tardar en el primer ciclo del día hábil siguiente de recibida
la solicitud de devolución o reclamo, después de efectuado el crédito.
```
```
La Entidad Participante Receptor debe debitar la cuenta del Usuario Receptor como respuesta a
su solicitud de devolución, teniendo en cuenta la información del Anexo 10: Devolución por
Solicitud del Usuario Receptor.
```
## 2.10.5. Actividades para realizar para el proceso crédito Información de carácter Confidencial


### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 76 de 329
```
Las actividades que deben ser realizadas en cada participante del sistema ACH, para completar una transacción
crédito, incluida la transacción de prenotificación crédito, son:

### EN EL USUARIO ORIGINADOR

```
− Explicar la forma y condiciones de operación de la transacción crédito al Usuario Receptor.
− Enviar las transacciones dentro de los horarios, formatos y especificaciones establecidos por el
participante originador.
− Realizar a su discreción, una transacción de prenotificación de carácter técnico a través de su Entidad
Participante Originadora previo al envío de transacciones crédito.
− Indicar al Usuario Receptor que el tiempo máximo para obtener respuesta a una transacción de
prenotificación, si es solicitada, es de un (1) día hábil.
− Aceptar las transacciones de devolución crédito generadas por el participante Receptor o por
solicitud del Usuario Receptor, dentro de los plazos establecidos.
− Aceptar los cargos por mandato legal asignados como consecuencia de los perjuicios que pueda
causar a cualquiera de los participantes (Entidad Participante Originadora, Entidad Participante
Receptor, Usuario Receptor) derivadas de deficiencias o errores operativos.
− Disponer de los procedimientos para el manejo de Reversiones de transacciones crédito procesadas
o enviadas por error.
EN LA ENTIDAD PARTICIPANTE ORIGINADORA
− Capacitar a los Usuario Originadores en la operación de la transacción crédito y entregar un
instructivo diseñado de acuerdo con sus procedimientos internos.
− Celebrar con sus Clientes Originadores un acuerdo en virtud del cual éstos la autorizan para enviar
en su nombre transacciones ordenadas por ellos mismos a través de ACH COLOMBIA.
− Explicar al Usuario Originador, que puede a su discreción, realizar la prenotificación por una sola vez,
para cada Usuario Receptor, explicándole que el proceso de prenotificación ayuda a que la
transacción crédito se dé correctamente y que las transacciones subsiguientes sean aplicadas
apropiadamente a la cuenta del Usuario Receptor por el participante Receptor.
− Indicar al Usuario Originador que el tiempo máximo para obtener respuesta a una transacción de
prenotificación solicitada, es de un (1) día hábil, después de enviada a el participante Receptor.
− Recibir las transacciones enviadas por el Usuario Originador en los horarios y formatos definidos.
− Preparar las transacciones a ser enviadas a ACH COLOMBIA en el formato estándar NACHA-M y
enviarlas en los horarios establecidos.
− Definir los procedimientos para manejo de Reversiones de transacciones crédito procesadas por
error. Estos procedimientos los debe dar a conocer a sus Clientes Originadores.
− Recibir las transacciones de devolución crédito que no pudieron ser aplicadas en el participante
Receptor.
```
## 2.10.6. Validación de transacciones crédito

La validación de la identificación del Usuario Receptor es solicitada a discreción del Usuario Originador o de el


### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 77 de 329
```
participante originador; el participante Receptor está obligada a validar las condiciones y/o el estado de la
cuenta y, si se solicita, debe también validar la identificación del Usuario Receptor, entendida como la
confrontación del número de cuenta y la identificación de su(s) respectivo(s) titular(es) (cuentas individuales
o conjuntas), datos enviados por el participante originador en una transacción de prenotificación o en una
transacción crédito.

En el caso de cuentas conjuntas, el participante Receptor está obligada a validar el único número de
identificación del Cliente Receptor registrado en la transacción de prenotificación o en la transacción crédito
contra todas las identificaciones registradas para ese número de cuenta.

Si al efectuar el proceso de validación de la información de la transacción de prenotificación o de la transacción
crédito, éste no resulta exitoso, el participante Receptor debe generar una transacción de devolución crédito,
en los tiempos y horarios definidos en el numeral 2.4. Ciclos de Proceso y Horarios de acuerdo con las causales
del anexo 9 “Causales de devolución.

La identificación del Usuario Receptor se debe enviar con la información básica de la transacción de
prenotificación o de la transacción crédito, si es que se requiere su validación, según lo indica el formato.

## 2.10.7. Ficha técnica para transacciones crédito

Las especificaciones técnicas para el envío de una Transacción monetaria o de prenotificación Crédito se
encuentran en el numeral 6.4. Ficha Técnica Transacción Crédito, y las especificaciones técnicas para la
generación de Transacciones de Devolución se encuentran en el numeral 6.6. Ficha Técnica Transacción
Devolución.

## 2.10.8. Consideraciones transacciones crédito recibidas de PSE

Además de las condiciones normales, el proceso de las transacciones recibidas del Sistema PSE debe tener en
cuenta lo siguiente:

```
− Prenotificaciones: PSE podrá enviar transacciones de prenotificación crédito; sin embargo,
actualmente no se generan.
− Límites: La Entidad Participante Recaudadora de las transacciones ACH originadas por PSE (tipo CCD),
debe omitir las validaciones actuales de límites para transacciones tipo crédito. Por lo tanto, deberá
abonar las transacciones por los valores indicados por PSE, siempre y cuando se cumplan las
condiciones de verificación de cuenta e identificación correspondientes.
− Devoluciones: La Entidad Participante no debe generar devoluciones a este tipo de transacciones. El
sistema no las procesará. La Entidad Participante debe seguir los procedimientos establecidos en el
Manual de Operaciones de PSE, en caso de no poder abonar las cuentas.
− Ficha Técnica: Las especificaciones técnicas para transacciones crédito originadas por PSE se
encuentran en el numeral 6.8. Ficha Técnica Transacción Crédito generada por PSE.
```
## 2.11. Transacción Débito

A continuación, se describe el proceso y detalle operativo de la transacción débito, involucrando las


### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 78 de 329
```
transacciones de prenotificación débito.

## 2.11.1. Esquema general de operación transacción débito

## 2.11.2. Proceso de la transacción débito

La Transacción Débito consiste en concentrar fondos provenientes de múltiples cuentas o depósitos
electrónicos de los Usuarios (Usuarios Receptores) de varias Entidades Participantes en la cuenta de un Usuario
de otra Entidad Participante (Usuario Originador).

```
− Para realizar una transacción débito, el Usuario Receptor autoriza a el participante Receptor a debitar
su cuenta o depósito electrónico periódicamente, para acreditar la cuenta de un Usuario Originador
en el participante originador.
− El Usuario Receptor entrega al Usuario Originador la Autorización de Recaudo descrita en el Anexo 11:
Autorización de Recaudo. El Cliente Usuario, una vez verifica la información del Usuario Receptor envía
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A

```
Agosto de 2024
Página 79 de 329
```
```
de forma obligatoria, a través del participante donde tiene su cuenta (Entidad Participante
Originadora), una Transacción de Prenotificación Débito a el participante Receptor para verificar las
condiciones de la cuenta, especificando el servicio a debitar.
```
- La autorización de recaudo del Usuario receptor, descrita en el Anexo 11: Autorización de Recaudo,
    puede ser entregada al Usuario originador a través de:
       - Formato físico: el Usuario receptor diligencia la autorización de recaudo mediante
          formato anexo 11: “Autorización de Recaudo” con su respectiva firma para garantizar
          su conocimiento sobre la acción, dicho formato será entregado al Usuario recaudador,
          para iniciar el proceso de inscripción.
       - Llamada Telefónica: el Usuario originador (recaudador) de la transacción débito,
          dispondrá a sus Usuarios receptores los medios para que a partir de una llamada
          telefónica pueda realizar la autorización de recaudo, de acuerdo al anexo 11:
          “Autorización de Recaudo”; la empresa recaudadora debe garantizar opciones de
          autenticación e identificación de sus usuarios, con el fin de ejercer los respectivos
          controles y conocimiento de Usuarios, la llamada debe quedar grabada como soporte
          de la inscripción de la cuenta al servicio.
       - Internet: el Usuario originador (recaudador) de la transacción débito, dispondrá a sus
          Usuario receptores los medios para ingresar a un portal web, el cual garantice el
          conocimiento de cliente y almacene en sus bases de datos la autorización de acuerdo
          con el anexo 11: “Autorización de Recaudo”
− Una vez verificada la información del Usuario Receptor que autoriza el débito, el Usuario Originador
    debe dar la orden de recaudo de fondos a su Entidad Participante (Entidad Participante Originadora)
    para debitar la cuenta o depósito electrónico que el Usuario Receptor mantiene en el participante
    (Entidad Participante Receptor). El Usuario Originador utiliza el medio y las condiciones operativas,
    técnicas y legales que su Entidad Participante le indique. En esta orden, debe indicar el detalle de las
    cuentas o depósitos electrónicos, valores, tipo de servicio, y clientes que van a ser afectados en las
    Entidades Participante Receptoras.
− Por ningún motivo el participante originador (o el Usuario Originador) puede enviar transacciones
    débito hacia cuentas o depósitos electrónicos que no haya prenotificado o cuyas transacciones de
    prenotificación hayan sido devueltas oportunamente por parte del participante Receptor.
− La Entidad Participante Receptor, debe de forma obligatoria, validar la identificación del Usuario
    Receptor cuando el participante originador le envíe una Transacción monetaria débito, o cuando le
    envíe una Transacción de Prenotificación débito. En este caso, no es requerido que el participante
    originador o el Usuario Originador soliciten la validación.
− Si la transacción monetaria débito o de prenotificación débito no resultan exitosas, el participante
    Receptor genera una transacción de devolución en los plazos definidos, indicando la causa específica.
− La Entidad Participante Receptor debe aplicar al sistema de cuentas o depósitos electrónicos, las
    transacciones débito, de acuerdo con la fecha efectiva enviada por el Usuario Originador. La Entidad
    Participante Receptor puede hacer validaciones específicas de la información recibida contra la
    información de sus cuentas o contra la base de datos de " Usuarios Receptores Prenotificados" si la
    maneja, como por ejemplo de límites. La creación de esta base de datos en el participante Receptor
    es opcional.


### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 80 de 329
```
```
− El débito a la cuenta o depósito electrónico del Usuario Receptor, lo debe realizar el participante
Receptor, el mismo día en que se reciben las transacciones desde ACH COLOMBIA, y en los horarios
indicados en el numeral 2.4. Ciclos de Proceso y Horarios.
− El abono a la cuenta del Usuario Originador, resultado de los recaudos realizados, lo debe realizar el
participante originador, el mismo día en que se enviaron las transacciones débito a el participante
Receptor. El manejo de la disponibilidad de los fondos para el Usuario Originador es decisión de el
participante originador de acuerdo con los convenios establecidos entre ellos.
− Si la transacción de Devolución del débito no es generada por el participante Receptor, en los horarios
definidos, la transacción débito se entenderá como aplicada exitosamente.
− ACH COLOMBIA procesa las Transacciones Monetarias Débito, de Prenotificación Débito y de
Devolución Débito, que cumplan con el formato NACHA-M.
```
## 2.11.3. REQUISITOS PARA LA VINCULACIÓN DE USUARIOS ORIGINADORES

Actividades con requisitos adicionales para vinculación de un Usuario para Originar transacciones Débito.

No podrán vincularse comercios relacionados con actividades delictivas, delitos fuente o subyacentes del
lavado de activos determinados en el artículo 323 del Código Penal Colombiano como son: tráfico de
migrantes, trata de personas, extorsión, enriquecimiento ilícito, delitos contra el sistema financiero, tráfico de
drogas tóxicas, tráfico de armas, rebelión, etc.:

```
Actividad Requisitos
```
```
Desarrolladoras de
Software
```
```
Carta de compromiso generada por la empresa en donde se compromete a utilizar el
botón para recaudos propios y no para convertirse en proveedor de servicio como
agregador de pagos.
```
```
Pasarelas de Pago
```
```
Realizar el proceso de vinculación y firmar contrato con ACH Colombia como Agregador
o Pasarela de Pago, cumplir los requisitos definidos en el Manual de Servicio PSE para
Pasarelas y Agregadores de Pago. Cualquier empresa que ejerza la actividad de Pasarela
o agregador a través de PSE sin autorización de ACH Colombia será deshabilitada sin
previo aviso.
NIT asociado para
empresas con
unidades de
negocio
```
```
Carta firmada por el representante legal de la empresa donde conste que la empresa
maneja varias unidades de negocio y por tanto necesita asociar el NIT y la respectiva
cuenta bancaria en el sistema PSE y que el botón será usado únicamente para el recaudo
de las unidades de negocio de las empresas vinculadas. Se debe relacionar el NIT principal
y los NIT asociados. Cámara de comercio del grupo empresarial, donde se demuestre la
relación comercial de las unidades de negocio con el NIT principal.
```
```
Para grupos
económicos
```
```
Carta firmada por el representante legal de la sociedad donde conste que la empresa
maneja diferentes unidades de negocio y por tanto necesitan asociar el NIT y la respectiva
cuenta bancaria en el sistema PSE, y que el botón será usado únicamente para el recaudo
de las unidades de negocio de las empresas vinculadas. Se debe relacionar el NIT principal
y los NIT asociados, carta firmada por el representante legal de la sociedad beneficiaria
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 81 de 329
```
```
Actividad Requisitos
del NIT asociado en la que se autorice la vinculación al botón de la sociedad con el que
hace grupo económico, certificación de composición accionaria de la sociedad
beneficiaria del NIT asociado, firmada por representante legal o revisor fiscal no menor
a 30 días, en donde se acredite la participación accionaria mayor al 50% de la sociedad
con el NIT principal, certificado de existencia y representación legal tanto de la sociedad
con el NIT principal, como de las sociedades beneficiarias del NIT asociado.
```
Billeteras,
monederos o
bolsillos virtuales

```
Deben ser entidades vigiladas por la Superintendencia Financiera de Colombia.
```
Empresas que
presten servicios
de captación,
ahorro,
inversiones.

```
Deben ser entidades vigiladas por la Superintendencia Financiera de Colombia o
Superintendencia de Economía Solidaria.
```
Empresas que
presten servicios
de préstamos,
recuperación de
cartera y
colocación.

```
Indicar en su objeto social la actividad de colocación. De ser una Fintech, debe estar
afiliada a la asociación Fintech. Debe ser presentada por una Entidad Participante.
```
Recaudos a
nombre de
terceros con
fiducia (NIT
Asociado)

```
Contrato de fiducia o documentos que soporten el acuerdo con la fiduciaria o el tercero
que va a recaudar, carta de la empresa que autorice el abono de los recursos al tercero,
certificado de existencia y representación legal del tercero. Se permitirá todo tipo de
empresas con la restricción que las cuentas estén en fiduciarias y anexen la
documentación solicitada.
```
Carteras
Colectivas

```
RUT del fondo de inversión, carta de solicitud de vinculación a PSE de la Sociedad
Administradora mencionando el Fondo de inversión recaudador, cámara de comercio de
la Sociedad Administradora. Cada cartera colectiva debe tener un botón independiente.
```
Donaciones

```
Personería Jurídica, Acta de Constitución o de fundación o certificado de cámara de
comercio donde se especifique que es una entidad sin ánimo de lucro, el objeto de esta
y mínimo 2 años de constitución, certificado del banco donde incluya número de cuenta
a la cual se hará el recaudo, el cuentahabiente debe corresponder con el beneficiario de
la donación. Restricción en topes de transacciones: Rango 1 PJ: 2 SMLV. Rango 2 PN: 2
SMLV.
```
Venta de billetes /
números de lotería

```
Documento donde conste que la beneficencia debe aceptar la reversión de los recursos
en el momento que la participante originadora de los recursos determine que la
transacción fue un fraude (transacción no consentida). Los recursos deben estar en la
cuenta de la beneficencia respectiva. Solo se aceptan compras de persona natural, los
límites de compra se rigen por Rango 1 de persona natural y no se permiten montos
superiores. Servicio ofrecido para las Beneficencias Departamentales que venden sus
loterías a través de internet.
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 82 de 329
```
## 2.11.4. Uso de la prenotificación débito

La Entidad Participante Originadora debe exigir la transacción de prenotificación al Usuario Originador, para

```
Actividad Requisitos
Empresas
farmacéuticas
dedicadas a la
fabricación con
fines medicinales y
científicos de
cannabis
```
```
Licencia emitida por el Ministerio de Salud y protección social para la producción y
fabricación de derivados de cannabis, registro del Instituto Nacional de Vigilancia de
Medicamentos y Alimentos INVIMA, certificación de la existencia de políticas de
prevención de Lavado de activos y financiación del terrorismo aplicadas en toda la cadena
de suministro (semillas, cultivo, fabricación y comercialización).
```
```
Empresas en
liquidación
```
```
Certificación firmada por el Liquidador, indicando la finalidad del uso del botón, el cual
debe estar encaminado únicamente a los actos propios de la liquidación.
```
```
Persona natural
```
```
A la solicitud de vinculación se debe adjuntar copia de la cámara de comercio, RUT y
cedula de ciudadanía del representante legal. Se debe cumplir con todo el proceso de
vinculación y validación exigido por ACH Colombia.
```
```
Servicios prepago
y recargas
```
```
Certificado de existencia y representación legal que especifique mínimo 2 años de
constitución. Debe tener funcionalidad en la página de la empresa, que garantice el
registro de los usuarios que realizan la recarga.
```
```
Giros Nacionales
```
```
Licencia de Servicio Postal de Pago emitida por el Ministerio de las tecnologías de la
información y las telecomunicaciones. Debe tener funcionalidad en la página de la
empresa que garantice el registro de los usuarios que realizan los giros y contar con
controles para prevenir el fraude.
Consorcios y
Uniones
temporales
```
```
La vinculación debe ser realizada por alguno de los integrantes del consorcio o unión
temporal, que sea el titular de la cuenta recaudadora.
```
```
Fabricación y
comercialización
de armas.
```
```
El fabricante de armas, explosivos o munición debe ser proveedor de las Fuerzas Militares
y de Policía en Colombia (INDUMIL).
```
```
Compra y Venta de
divisas
```
```
Certificación suscrita por representante legal de que se cuenta con mecanismos para la
prevención y control del lavado de activos y financiación del terrorismo y se realiza un
conocimiento del cliente enfocado en conocer el origen de los fondos con los que se
realizan las transacciones.
```
```
Juegos de suerte y
azar
```
```
Autorización para operar por parte de Coljuegos. Debe tener un acuerdo de recaudo con
una participante, deberá habilitar controles tales como: auditorías, reversión de pagos,
límites, número máximo de operaciones, bloqueos preventivos, e investigaciones
Certificación de que cuenta con sistema de prevención y control de lavado de activos y
financiación del terrorismo, así como para la prevención de fraude y la seguridad de la
información. El servicio debe estar dirigido a personas naturales habilitadas para el
efecto, inscripción previa del comprador, bancarizado y con tope máximo de
operaciones.
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 83 de 329
```
cada Usuario Receptor, como transacción previa a la transacción Débito, por una vez y garantizar la existencia
del respectivo soporte de autorización que tendrá vigencia limitada o ilimitada, según se requiera.

El procedimiento de prenotificación aplica cuando las vinculaciones se produzcan en la empresa recaudadora
o Usuario Originador.

La transacción de prenotificación ayuda a que el proceso débito se dé correctamente y que las transacciones
subsiguientes sean aplicadas apropiadamente a la Cuenta del Usuario Receptor por el participante Receptor.

## 2.11.4.1. Control de Prenotificaciones

```
El control de las transacciones de prenotificación que se han enviado es responsabilidad del
participante originador o del Usuario
Originador, según el acuerdo que exista entre las partes.
Si la transacción de devolución de prenotificación no es generada por el participante Receptor, en
los horarios y ciclo definidos en el en el numeral 2.4. Ciclos de Proceso y Horarios, se entenderá que
la prenotificación ha sido exitosa.
```
## 2.11.4.2. Plazos para iniciar la Prenotificación

```
La Entidad Participante Originadora debe realizar la prenotificación mínimo tres (3) días hábiles como
tiempo máximo antes de la Fecha Efectiva de la primera transacción débito.
```
## 2.11.5. Transacciones de devolución

En el proceso débito se pueden presentar devoluciones durante la vinculación cuando se generan
transacciones de prenotificación; también se pueden presentar devoluciones durante el proceso de recaudo,
es decir, cuando se genera la transacción monetaria débito.

## 2.11.5.1. Devolución de Prenotificaciones Débito y de Transacciones Débito

```
Si el proceso de verificación derivado de la transacción de prenotificación o de la transacción débito
no es exitoso, el participante Receptor deberá generar una transacción de Devolución con destino a
el participante originador.
```
```
La Entidad Participante Receptor, a través de la Transacción de Devolución, informa a el participante
originador a través del sistema ACH COLOMBIA, que la transacción no fue aceptada por no cumplir
con las condiciones establecidas o porque no fue aceptada por el Usuario Receptor.
```
```
La Entidad Participante Originadora debe verificar la razón de la devolución e informar al Usuario
Originador para iniciar nuevamente la transacción de prenotificación o transacción débito según sea
el caso.
```
```
− Causales de Devolución
La Entidad Participante Receptor debe informar la descripción y código estándar de la
devolución, y en lo posible, informar el detalle de la devolución. En el Anexo 9. Causales de
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 84 de 329
```
```
Devolución, se presentan las causales específicas para transacciones de prenotificación débito y
para transacciones monetarias débito.
− Plazos para Iniciar una Transacción de Devolución Débito
```
```
− Entidad Participante Receptor
```
```
La Entidad Participante Receptor debe generar la transacción de devolución de prenotificación
y la transacción de devolución débito, en los plazos máximos definidos en el en el numeral 2.4.
Ciclos de Proceso y Horarios.
```
```
− Usuario Receptor
```
```
Si el participante Receptor recibe una solicitud de no aceptación (reclamo) de la transacción
débito por parte del Usuario Receptor, debe generar una transacción de devolución débito
indicando la causal específica, a más tardar en el primer ciclo del día hábil siguiente de recibida
la solicitud de devolución o reclamo.
```
```
El Usuario Receptor debe ser consciente que presentar devoluciones débito reiteradas, puede
ser causal de cancelación del servicio por parte del Usuario Originador o de el participante
Receptor.
```
```
El control del número máximo de devoluciones débito por solicitud del Usuario Receptor, debe
ser efectuado por el Usuario Originador o por el participante Receptor a su discreción. Se
recomienda que no exceda de tres (3) solicitudes de devolución por cada servicio prestado a
cada Cliente Receptor.
```
## 2.11.6. Actividades para realizar para el proceso débito

Las actividades que deben ser realizadas por cada participante del sistema ACH, para completar la vinculación
de los Usuarios al servicio débito y la transacción débito, son las siguientes:


### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A

```
Agosto de 2024
Página 85 de 329
```
### ACTIVIDADES EN EL USUARIO ORIGINADOR

```
Prenotificación
```
```
− Explicar la forma y condiciones de operación de la transacción débito al Usuario
Receptor, y solicitarle la autorización dirigida a el participante Receptor para aplicar
transacciones débito ordenadas por el Usuario Originador bajo las condiciones
indicadas en el Anexo 11: Autorización de Recaudo.
− Conservar la Autorización de Recaudo a título de custodia gratuita y en beneficio de
las Entidades Participantes, en un medio adecuado y seguro; el tiempo de
conservación será establecido en el contrato celebrado entre el Usuario Originador
y su Entidad Participante Originadora. El Usuario Originador debe entregar las
autorizaciones a el participante que así lo requiera y al personal que esté
debidamente autorizado para ello. Todo lo anterior, si pacta con su Entidad
Participante tener la custodia de la Autorización de Recaudo.
− Realizar en forma obligatoria una transacción de prenotificación de carácter técnico
a través de su Entidad Participante Originadora previo al envío de transacciones
débito.
− Indicar al Usuario Receptor que el tiempo máximo para obtener respuesta a una
transacción de prenotificación es de un (1) día hábil tiempo a partir del cual se podrá
iniciar la primera transacción débito, si la prenotificación fue exitosa.
− Será responsabilidad del cliente originador validar la autenticidad y validez de la
autorización (formato físico, llamada telefónica, internet) que reciba el usuario
originador en los términos de la ley de comercio electrónico.
```
```
− Aceptar o efectuar la devolución de las transacciones ordenadas por el participante Receptor o por el
Usuario Receptor, independientemente de la causal válida expuesta, dentro de los plazos establecidos.
```
```
− Aceptar los cargos por mandato legal asignados como consecuencia de los perjuicios que pueda causar
a cualquiera de los participantes (Entidad Participante Originadora, Entidad Participante Receptor,
Usuario Receptor) derivadas de deficiencias o errores operativos.
− Suspender el envío de transacciones débito ante la notificación de Cancelación de Autorización (Ver
Anexo 12: Cancelación de Autorización de Recaudo) dada a el participante Receptor o al Usuario
Originador por el Usuario Receptor.
− Disponer de los procedimientos para el manejo de Reversiones de transacciones débito enviadas por el
Usuario Originador por error.
```
### ACTIVIDADES EN LA ENTIDAD PARTICIPANTE ORIGINADORA

```
− Capacitar a los Usuarios Originadores en la operación de la transacción débito y entregar un instructivo
diseñado de acuerdo con sus procedimientos internos incluyendo el de la vinculación del Usuario
Receptor.
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A

```
Agosto de 2024
Página 86 de 329
```
### ACTIVIDADES EN LA ENTIDAD PARTICIPANTE ORIGINADORA

```
− Celebrar con sus Clientes Originadores un acuerdo en virtud del cual éstos la autorizan para enviar en su
nombre transacciones ordenadas por ellos mismos a través de ACH COLOMBIA, de acuerdo con las
condiciones mínimas establecidas en el Anexo 13: Contrato Débito Usuario Originador – Entidad
Participante Originadora.
− Permitir a través de sus canales (físicos, telefónicos y/o virtuales) la inscripción, programación,
modificación y/o cancelación de autorizaciones para la realización de débitos automáticos. Es
responsabilidad de la EF la custodia y conservación de las autorizaciones dadas por los usuarios para la
realización de los débitos, realizadas a través de cualquiera de los canales dispuestos por la EF. La EF será
responsable ante el sistema por las reclamaciones, investigaciones y/o requerimientos relacionados con
las inscripciones de los débitos realizadas a través de dichos canales. En caso de requerirlo, la EF deberá
acreditar la existencia de la autorización dada por el usuario, y así como los medios que utilizo para llevar
a cabo el conocimiento del cliente que otorgó dicha autorización.
```
```
Prenotificación
```
```
− Exigir la prenotificación técnica por una sola vez a los Usuarios Originadores, para
cada Usuario Receptor, como inicio de un proceso de transacciones débito.
− Indicar a los Usuario Originadores que el tiempo máximo para obtener respuesta a
una transacción de prenotificación es de un (1) día hábil, tiempo estimado para
iniciar el proceso de transacciones débito.
− Acordar con los Usuarios Originadores el mecanismo de custodia de la Autorización
de Recaudo y los requerimientos sobre dichas autorizaciones.
− Definir los procedimientos para manejo de Reversiones de transacciones débito procesadas por error.
Estos procedimientos los debe dar a conocer a sus Usuarios Originadores.
− Disponer de un proceso de evaluación de riesgo permanente de los Usuarios Originadores para control
de Límites, entre otros.
− Acordar con sus Usuarios Originadores, el manejo de disponibilidad de fondos de las operaciones de
recaudo realizadas.
```
```
− Definir los procedimientos de manejo de las devoluciones por causa de Cancelaciones de Autorizaciones
dadas por el Usuario Receptor a el participante Receptor e indicarlos al Usuario Originador.
```
```
− Aceptar los cargos por mandato legal asignados como consecuencia de los perjuicios que pueda causar
a cualquiera de los participantes (Entidad Participante Receptor, Usuario Originador, Usuario Receptor)
derivadas de deficiencias o errores operativos.
```
### EN LA ENTIDAD PARTICIPANTE RECEPTOR

```
Prenotificación
```
```
− Hacer una verificación técnica en el momento de recibir una transacción de
prenotificación que consiste en validar las condiciones y/o estado de la cuenta y la
identificación del Usuario Receptor asociado a la misma.
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 87 de 329
```
### EN LA ENTIDAD PARTICIPANTE RECEPTOR

```
− Definir el procedimiento de operación en el momento de notificar a su Cliente
Receptor la validez de una transacción de prenotificación.
− Si el proceso de verificación derivado de la transacción de prenotificación no es
exitoso, el participante Receptor debe enviar una transacción de devolución.
− Si el proceso de verificación derivado de la transacción de prenotificación es exitoso,
el participante Receptor deberá actualizar su base de datos de prenotificaciones, si
la maneja, y aplicar la transacción de acuerdo con la información registrada en la
transacción de prenotificación.
− Hacer una verificación técnica que consiste en validar las condiciones y/o estado de la cuenta y la
identificación del Usuario Receptor asociado a la misma en el momento de recibir una transacción débito;
si el participante Receptor no puede aplicar la transacción débito al sistema de cuentas por una razón
específica, debe generar una transacción de devolución débito.
− Si el proceso de verificación efectuado a la transacción débito es exitoso, el participante Receptor debe
aceptar y aplicar la transacción débito a su sistema de cuentas.
− Aceptar los cargos por mandato legal asignados como consecuencia de los perjuicios que pueda causar
a cualquiera de los participantes (Entidad Participante Originadora, Usuario Receptor) derivadas de
deficiencias o errores operativos.
− La Entidad Participante Receptor debe disponer de los procedimientos para manejo de órdenes de no
pago, reclamos o devoluciones solicitadas por el Usuario Receptor y para el manejo de Cancelaciones de
Autorizaciones de Recaudo y de Reversiones de transacciones débito procesadas por error. Estos
procedimientos los debe dar a conocer a sus Clientes Receptores.
− La Entidad Participante Receptor tiene la opción de crear una base de datos de "Clientes Receptores
Prenotificados o Enrolados” o crear una base de datos de "Excepciones o Negativos" que contengan la
Cancelación de Autorizaciones de Recaudo y órdenes de no pago dadas por los Usuario Receptores, o
crear ambas bases de datos para controlar los servicios de recaudo ofrecidos a sus clientes.
```
## 2.11.7. Validación de transacciones débito

− La Entidad Participante Receptor está obligada a validar las condiciones y/o el estado de la cuenta y la
identificación del Usuario Receptor, entendida como la confrontación del número de cuenta y la
identificación de su(s) respectivo(s) titular(es) (cuentas individuales o conjuntas), datos enviados por el
participante originador en una transacción de prenotificación o en una transacción débito. Cada Entidad
Participante Receptor deberá desarrollar su propio control que le permita comparar la información
recibida con o sin digito de chequeo, dado que dentro del registro no existe un campo adicional que
informe si tal dígito está o no presente.
− En el caso de cuentas conjuntas, el participante Receptor está obligada a validar la Identificación del
Usuario Receptor registrado en la transacción de prenotificación o en la transacción débito contra todas
las identificaciones registradas para ese número de cuenta. La Identificación del Usuario Receptor se debe
enviar con la información básica de la transacción de prenotificación o de la transacción débito.
− Para las diversas operaciones y procesos internos del Usuario Originador y el participante Receptor es
importante tener en cuenta que los datos del(los) titular(es) de la cuenta (Usuario Receptor(es)) en el


### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 88 de 329
```
participante Receptor pueden ser diferentes del (los) usuario(s) directo(s) del servicio registrado en el
Usuario Originador. Por ejemplo, en el caso de una empresa de servicios públicos se puede presentar que
el arrendatario de un predio es el titular de la cuenta en el participante Receptor y quien paga la factura,
mientras que el propietario de dicho predio es el usuario registrado ante la empresa de servicios públicos.
− Si al efectuar el proceso de validación de la información de la transacción de prenotificación, éste no
resulta exitoso el participante Receptor debe generar una transacción de devolución de prenotificación.
− Si al efectuar el proceso de validación de la información de la transacción débito, este no resulta exitoso,
el participante Receptor debe generar una transacción de devolución débito.
− La referencia de pago que envía el participante originador o el Usuario Originador en cada transacción de
prenotificación débito y en cada transacción monetaria débito, puede ser usada por el participante
Receptor para notificar el detalle del débito realizado al Cliente Receptor o para almacenarlo en la base
de datos de prenotificaciones (si la utiliza) para futuras referencias y validaciones. La referencia de pago
debe enviarse como información adicional a la transacción, de acuerdo con el formato establecido.
− Cuando el Usuario Receptor se vincula directamente en el participante Receptor, el participante
originador o el Usuario Originador no requieren enviar prenotificaciones.
− La Entidad Participante Receptor puede efectuar validaciones de las transacciones débito recibidas de los
Usuarios Originadores, contra la base de datos de “Clientes, si la maneja.

## 2.11.8. Novedades

A continuación, se presentan las novedades que pueden solicitar el Usuario Originador a su Entidad
Participante Originadora y el Usuario Receptor a su Entidad Participante Receptor:

## 2.11.8.1. Orden de no pago

```
Es el documento firmado por el Usuario Receptor, donde solicita a su Entidad Participante Receptor
no efectuar una transacción de pago específica. El Usuario Receptor tiene la posibilidad y la opción
de evitar que temporalmente le apliquen débitos a su cuenta por un servicio, siempre y cuando se
cumpla lo siguiente:
```
```
− El Usuario Receptor informe a el participante Receptor su orden de no pago, con una
antelación no inferior a cinco (5) días hábiles antes de la fecha de aplicación del débito. Para
ello es importante que las Entidades Participantes Receptoras dispongan de canales ágiles y
sencillos.
− El Usuario Receptor suministre los siguientes datos para el procesamiento de la orden de no
pago.
− Número de Identificación del Usuario Originador.
− Nombre del Usuario Originador.
− Código Único de Referencia del servicio.
− El Usuario Receptor debe ser consciente que presentar órdenes de no pago reiteradas,
puede ser causal de cancelación del servicio por parte del Usuario Originador o de el
participante Receptor. El control del número máximo de órdenes de no pago, debe ser
realizado por el Usuario Originador o por el participante Receptor a su discreción. Se
recomienda que no exceda de tres (3) órdenes de no pago.
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A

```
Agosto de 2024
Página 89 de 329
```
```
Si el participante Receptor recibe una transacción débito, y el Usuario Receptor ha dado orden de no
pago, el participante Receptor debe generar una transacción de devolución débito usando la causal
R08 - Orden de No Pago. Esta orden se hace efectiva al momento de generar la transacción de
devolución; pagos posteriores serán aplicados normalmente.
```
```
Es responsabilidad del Usuario Receptor resolver y aclarar su situación con el Usuario Originador,
cuando presente una orden de no pago.
```
```
Todos los participantes (Usuario Originador, Entidad Participante Originadora y Entidad Participante
Receptor) deben aceptar y habilitar sus sistemas y políticas para el procesamiento de estas órdenes
de no pago.
```
## 2.11.8.2. Cancelación de Autorización de Recaudo

```
Es el documento firmado por el Usuario Receptor cancelando en forma definitiva la Autorización de
Recaudo (Ver Anexo 12: Cancelación de Autorización de Recaudo) para debitar su cuenta, el cual
debe ser entregado a el participante Receptor o al Usuario Originador, con una anticipación no
inferior a diez (10) días hábiles a la fecha efectiva del próximo envío de transacciones débito. En la
medida en que las circunstancias tecnológicas y legales lo permitan, el documento físico puede ser
reemplazado por uno de tipo electrónico. Si la Cancelación de Autorización de Recaudo es
presentada al Usuario Originador, éste no está obligado a notificar a el participante Receptor; sin
embargo, se recomienda a el participante Receptor establecer los mecanismos de administración de
la base de datos de prenotificaciones no utilizadas.
El documento de Cancelación de Autorización de Recaudo que el Usuario Receptor firma, debe incluir
la información mínima requerida por el Usuario Originador y/o por el participante Receptor para
actualizar sus bases de datos, deshabilitando al Usuario Receptor para recibir transacciones débito
en el futuro, según lo establecido en el Anexo 12.
La conservación y custodia del documento de Cancelación de Autorización de Recaudo la debe hacer
el participante Receptor o el Usuario Originador, según donde se presente.
Es importante anotar que el aviso de Cancelación de Autorización puede darse por una persona
diferente al Usuario Receptor que demuestre un interés legítimo en ello o por una autoridad, en caso
de que se presente una circunstancia que manifiestamente implique la imposibilidad de continuar
con la operación en concreto, tales como la liquidación de la sociedad, la muerte del Usuario
Receptor, la terminación del contrato que otorga causa al pago, o eventos similares.
Si el participante Receptor recibe una transacción débito, y el Cliente Usuario ha cancelado
previamente la autorización de recaudo, el participante Receptor debe generar una transacción de
devolución débito usando la causal R07 - Autorización de Recaudo Revocada por el Usuario Receptor.
```
## 2.11.8.3. Modificaciones

```
El Usuario Receptor que requiera cambiar información financiera (p.e: número o tipo de cuenta, o
Entidad Participante) o información relativa al servicio (p.e: referencia de pago) que fue entregada y
aceptada durante el proceso de vinculación (prenotificación), debe reiniciar el proceso de vinculación
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 90 de 329
```
```
y solicitar la eliminación de la información dada con anterioridad. Los límites pueden ser modificados,
teniendo en cuenta lo mencionado en el numeral 2.11.13. Ficha técnica para transacciones débito.
```
## 2.11.9. Reintentos automáticos

La Entidad Participante Originadora está en capacidad de reenviar una transacción débito a través del sistema
ACH COLOMBIA, cuando haya sido devuelta previamente por el participante Receptor por fondos insuficientes
en la cuenta del Usuario Receptor (Causal de Devolución R01- Fondos Insuficientes) o fondos no disponibles
para cubrir el valor de la transacción débito (Causal de Devolución R09 – Fondos No Disponibles). Lo anterior,
si ha pactado con su Cliente Originador efectuar reintentos automáticos para transacciones débito.

## 2.11.10. Pagos parciales

Para el procesamiento de transacciones débito no se aceptan pagos parciales. La transacción débito monetaria
en la cuenta del Usuario Receptor se realiza por el valor total de la transacción débito generada por el Cliente
Originador.

Cuando no hay fondos suficientes o cuando la disponibilidad de fondos no cubre el valor total de la transacción
débito, el participante Receptor debe generar una transacción de devolución por causal R01-Fondos
Insuficientes o R09-Fondos No Disponibles, según corresponda.

## 2.11.11. Límites

Los límites entre el participante originador y su Cliente Originador, y los límites que el Usuario Originador
acuerde con su Cliente Receptor se establecen a discreción de cada uno de ellos y de acuerdo con el convenio
que establezcan.

Los límites entre el Usuario Originador y el Usuario Receptor pueden ser establecidos en la "Autorización de
Recaudo", si el Usuario Originador o el participante Receptor así lo consideran conveniente, según la
transacción de prenotificación.

Por otra parte, el participante Receptor puede, a su discreción, definir, controlar y establecer mecanismos de
asignación y actualización de límites de mutuo acuerdo con su Usuario Receptor, y generar devoluciones con
la causal correspondiente (R13– Monto no autorizado: El valor de la transacción débito no corresponde al
monto autorizado por el Usuario Receptor) al Cliente Originador, si así lo considera.

Adicionalmente, es importante tener en cuenta que la “Autorización de Recaudo” presenta de forma opcional
la inclusión de los límites, por lo tanto, si se va a realizar una modificación a la autorización referente a los
límites, se debe indicar al Usuario Receptor que se puede dirigir al Usuario Originador (si se han acordado
límites entre ellos) o a el participante Receptor (si el participante ofrece el servicio de control de límites).

## 2.11.12. Reclamos

A continuación, se presentan algunas de las razones de reclamación, que un Usuario Receptor puede presentar
en el momento de solicitar una devolución débito a su Entidad Participante Receptor, para obtener el abono a
su cuenta:


### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 91 de 329
```
```
− Usuario Originador no autorizado: La Entidad Participante Receptor ha sido notificada por su Cliente
Receptor, que el Usuario Originador de la transacción no ha sido autorizado para debitar su cuenta.
− No existe autorización o prenotificación: No fue encontrada la autorización o acuerdo con el Usuario
Receptor o no existe prenotificación.
− Monto no autorizado: El valor de la transacción débito no corresponde con el monto autorizado por
el Usuario Receptor.
− Fecha de transacción errada: La fecha de la transacción débito no corresponde con la fecha autorizada
por el Usuario Receptor.
− Autorización Cancelada: El Usuario Receptor ha cancelado previamente la autorización de recaudo.
− Débito Duplicado: El Usuario Receptor notifica el recibo de una transacción débito duplicada.
− La Devolución por Solicitud del Usuario Receptor (Ver Anexo 10) debe hacerse por escrito e incluir
toda la información relacionada con el reclamo generado por el Usuario Receptor.
```
Cuando el Usuario Receptor solicite una Devolución a una transacción Débito aplicada con anterioridad, como
consecuencia de una reclamación, el participante Receptor debe efectuar el abono a la Cuenta Receptora del
Usuario Receptor, el mismo día de recibida la reclamación, y preferiblemente de forma inmediata.

La Entidad Participante Receptor debe generar una transacción de devolución débito, máximo en el primer
corte del siguiente día hábil de recibida la Solicitud de Devolución por parte del Usuario Receptor, informando
la causal de la devolución (Causal R10 - Devolución débito por solicitud del Usuario Receptor) y dando el mayor
detalle posible del motivo del reclamo.

Si por algún inconveniente no es posible efectuar el abono a la cuenta del Usuario Receptor o generar la
devolución con destino a el participante originador, el mismo día de recibido el reclamo, el participante
Receptor puede hacerlo como máximo al día hábil siguiente de recibido el reclamo; sin embargo, se
recomienda que, para generar confianza en el servicio, el abono se efectúe en forma inmediata.

La Entidad Participante Originadora debe verificar la razón de la devolución, aceptarla e informar al Usuario
Originador.

Cuando el Usuario Receptor no acepta el débito, el participante Receptor de la transacción deberá crear, en el
módulo de reclamos, una solicitud de reversión con causal DEV14 (Solicitud de Devolución de una transacción
ACH débito no consentida).

El Usuario Receptor debe ser consciente que presentar devoluciones débito reiteradas, puede ser causal de
cancelación del servicio por parte del Usuario Originador o de el participante Receptor. El control del número
máximo de devoluciones débito, debe ser efectuado por el Usuario Originador o por el participante Receptor
a su discreción. Se recomienda que no exceda de tres (3) solicitudes de devolución por cada servicio del Usuario
Receptor.

Es importante aclarar que el participante Receptor generará transacciones de devolución por el valor exacto
de la transacción débito original.

La Entidad Participante Receptor debe reintegrar los cargos aplicados a la cuenta del Usuario Receptor por
motivo de impuestos, comisiones o cualquier otro cargo, resultado de la transacción débito aplicada, cuando


### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 92 de 329
```
éste realice la reclamación.

En el caso de presentarse intereses de mora o cargos que sean producto de las devoluciones débito, o reclamos
por valores diferentes a las transacciones débito original, el Usuario Receptor debe contactarse directamente
con su Cliente Originador.
Los inconvenientes que se presenten como resultado de reclamaciones simultáneas del Usuario Receptor ante
el participante Receptor y ante el Usuario Originador, deben ser solucionados directamente entre el Usuario
Originador y el Usuario Receptor.

## 2.11.13. Ficha técnica para transacciones débito

Las especificaciones técnicas para el envío de una Transacción monetaria Débito y de una Transacción de
Prenotificación Débito se encuentran en el numeral 2.11; las especificaciones técnicas para la generación de
Transacciones de Devolución se encuentran en el numeral 2.10.

## 2.12. Límites

ACH COLOMBIA ha establecido algunos valores máximos que pueden ser enviados a través del sistema ACH
COLOMBIA en la realización de las Transacciones, que no deben ser superados por valor de transacción.

## 2.12.1. Para transacciones crédito

Los límites por monto actualmente definidos en ACH COLOMBIA para cada Transacción Crédito se describen a
continuación:

### LÍMITES PARA TRANSACCIONES CRÉDITO

### VALOR TRANSACCIÓN CONTROL EN ACH COLOMBIA

```
Transacciones crédito
cuyo valor oscile entre
$ 1 y $ 200. 000. 000.
000.oo por transacción
hacia cuenta de ahorro,
corriente y monedero
electrónico.
```
```
El sistema Integra ACH no exigirá la autorización de un funcionario del
participante con perfil de AdminAprover para poder procesar cada una de las
transacciones cuyo monto se encuentre en este rango. Es responsabilidad de cada
Entidad Participante configurar los montos mínimos de autorización. El sistema
genera un archivo de devolución por operador con destino a el participante
originador de las transacciones que no se pueden procesar por no cumplir con la
autorización del AdminAprover, indicando la causal de devolución por operador y
descripción de la misma, según el ítem 6.1 1 Generalidades del Formato, numeral
6.1.8 (Ver Anexo 3: Causales de Devolución por Operador).
```
```
Nota: El control de Límites transaccionales a depósitos electrónicos de manera mensual establecido en el
Decreto 222 de 2020 del Departamento Administrativo de la Función Pública, no es responsabilidad de ACH
Colombia, toda vez que los participantes del servicio son quienes deben implementar el mecanismo de control
en su infraestructura tecnológica.
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A

```
Agosto de 2024
Página 93 de 329
```
### LÍMITES PARA TRANSACCIONES CRÉDITO

### VALOR TRANSACCIÓN CONTROL EN ACH COLOMBIA

```
Transacciones crédito
que superen el valor de
los $ 200. 000. 000.
000.oo
```
```
ACH COLOMBIA no procesa aquellas transacciones que superen el límite
establecido, el aplicativo de Integra genera una Devolución por Operador con las
transacciones que supere este límite. La Devolución por Operador es generada
por ACH COLOMBIA utilizando la causal que corresponda según el ítem 6.11
Generalidades del Formato, numeral 6.1.8 (Ver Anexo 3 Causales de Devolución
por Operador).
```
```
Cuando se requiera realizar transacciones que superen esté límite, la Entidad
participante originadora debe:
```
1. Ingresar a Integra ACH e inscribir y aprobar la cuenta recaudadora para
    la cual va este valor (Ver Instructivo creación y autorización de cuentas en
    Integra ACH para montos superiores GOP-PRC-INS 020).
2. Adicionalmente La Entidad participante Originadora debe enviar correo
    electrónico al área de procesamiento de ACH Colombia
    procesamiento@achcolombia.com.co, solicitando la liquidación,
    validación y compensación de esta transacción, anexando el formato de
    ACH “Aprobación Cuentas Montos Superiores ACH” código GOP-PRC-
    FOR- 007 , el cual debe venir firmado por el representante legal de la
    Entidad.
3. ACH Colombia debe aprobar en Integra la cuenta receptora inscrita por la
    Entidad Participante; la cual debe estar aprobada con anterioridad por la
    Entidad Participante originadora con el rol de “AdminAprover”
La autorización del proceso se realiza tal y como se detalla en el procedimiento
interno “MANUAL FUNCIONAL INTEGRA-ACH COLABORADORES INTERNOS V5”


### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 94 de 329
```
## 2.12.2. Administración de Cuentas Para Montos Superiores

A continuación, se presenta el esquema de manejo de la administración de cuentas para transferencias de
transacciones crédito por montos superiores autorizadas en Integra ACH por parte del participante originador

## 2.12.2.1. Registrar Cuenta

El usuario de la Entidad participante Originadora con perfil de AdminOperator debe inscribir en Integra ACH
los datos de la cuenta receptora hacia la cual se van a realizar transferencias crédito por montos superiores de
acuerdo con los límites establecidos en el numeral 2.12.1, adicionalmente debe seleccionar uno de los Límites
disponibles para ser asignado a la cuenta que se está registrando; actualmente existen dos ( 2 ) tipos de límites
para transacciones tipo crédito.

```
− Límite A: Transacciones crédito hasta $200.000.000.000, o y no requiere inscripción de la cuenta
receptora.
− Límite B: Transacciones crédito que superen los $200.000.000.00 0 ,oo se requiere inscribir la cuenta
receptora, una vez autorizada la cuenta por parte de la Entidad Participante, esta debe enviar correo
electrónico a ACH Colombia procesamiento@achcolombia.com.co anexando el formato Aprobación
Cuentas Montos Superiores firmada por el representante legal, ACH aprueba la cuenta receptora en
el aplicativo de Integra y el presidente de ACH o Vicepresidente de Operaciones y Tecnología emitirá
la aprobación de dicho proceso, dicha cuenta aprobada quedará disponible en el sistema mientras se
carga el archivo. 8 (Ver Anexo 22 Aprobación Cuentas Montos Superiores).
```
## 2.12.2.2. Aprobar Cuenta

El usuario del participante con perfil de AdminAprover de ACH debe ingresar al sistema de Integra ACH y
autorizar la cuenta que fue inscrita por el usuario del participante con perfil de AdminOperator, lo anterior con
la finalidad de dejar activa la cuenta receptora para recibir transacciones por montos superiores de acuerdo
con el numeral 2.12 Límites

## 2.12.2.3. Cargue de archivo Nacha-M

El usuario AdminOperator de el participante debe cargar en Integra ACH el archivo Nacha-m que contiene la
(s) transacción (es) crédito enviado (s) de acuerdo con el límite de la transacción, la aplicación solicita la
autorización del AdminAprover de Integra ACH de el participante, el cual debe autenticarse con usuario y OTP
y de esta manera aprobar la (s) transacción (es) de las cuentas que se encuentra ya registradas y aprobadas en
Integra ACH.


### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 95 de 329
```
## 2.12.3. Para transacciones débito

Los límites actualmente definidos en ACH COLOMBIA para cada Transacción Débito se describen a
continuación:

### LÍMITES PARA TRANSACCIONES DÉBITO

### VALOR TRANSACCIÓN CONTROL EN ACH COLOMBIA

```
Transacciones débito
que superen los
$117.028.854
```
```
ACH COLOMBIA no procesa aquellas transacciones que superen el límite
establecido. Integra ACH genera una Devolución por Operador por cada
transacción que supere el límite establecido. La condición en ACH COLOMBIA
se presenta si se supera el límite definido, La Devolución por Operador es
generada por Integra ACH utilizando la causal que corresponda según el ítem
6.1 1 Generalidades del Formato, numeral 6.1.8 (Ver Anexo 3: Causales de
Devolución por Operador).
```
```
Para transacciones de
depósitos Electrónicos
como originadores
débitos y receptores
débito tendrán un
monto hasta 8 SMMLV
que es lo que debe tener
como saldo máximo.
Para el año 2025 es de $
11. 388 .000
```
```
Las transferencias interbancarias débito a Depósitos Electrónicos tiene un
límite por transacción a cliente receptor.
```
```
Nota: El control de Límites transaccionales a depósitos electrónicos de manera mensual establecido en el
Decreto 222 de 2020 del Departamento Administrativo de la Función Pública, no es responsabilidad de
ACH Colombia, toda vez que los participantes del servicio son quienes deben implementar el mecanismo
de control en su infraestructura tecnológica.
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 96 de 329
```
## 3. ESQUEMA DE CALIDAD

## 3.1. Introducción

El presente esquema tiene como finalidad primordial, mantener el servicio de ACH COLOMBIA dentro de los
más altos estándares de calidad y corrección. Busca facilitar el alcance de los siguientes objetivos básicos:

La “Mejora en el Servicio” entendida como la vocación de los partícipes del sistema, consistente en prestar un
servicio a los clientes y usuarios de este que se caracterice por su calidad, transparencia, moralidad, celeridad,
eficiencia y demás elementos y valores conexos con una actividad de servicio.

La “Protección del Sistema”, concebida como el compromiso entre los partícipes de ACH COLOMBIA en
mantener el sistema permanentemente dentro de altos estándares de operatividad, en la medida en que el
servicio de ACH COLOMBIA supone un proceso en cadena, caracterizado por la mutua dependencia entre las
Entidades participantes. Las reglas aquí acordadas tienden a garantizar en particular la fluidez del sistema,
entendida como el flujo continuo e ininterrumpido de información dentro de altos estándares de calidad y, la
confianza e integridad de este, concebida como la minimización de los riesgos inherentes a actuaciones
incorrectas o defectuosas de los partícipes.

## 3.2. Alcance del esquema de calidad

El presente esquema hace parte del estatuto profesional de los partícipes del sistema de ACH COLOMBIA, tiene
una naturaleza gremial, contractual y privada, se fundamenta en la buena fe de los partícipes, el carácter
profesional de su actividad, en la protección al sistema de pagos y en la defensa de los derechos de los usuarios
del servicio. No obstante, en la búsqueda de los fines que le anima y en el desarrollo de los principios que le
fundamenta, no agota la materia.

Las sanciones previstas en este Esquema de Calidad no son plenamente indemnizatorias del daño que los
errores y conductas descritas puedan causar, y se refieren únicamente a los daños sistémicos, esto es, a los
que se relacionan con el servicio y operación de un sistema de pagos y cobros en red sana. En consecuencia,
atienden con exclusividad la calidad de la red en términos estrictamente operativos, por lo que no inhibe a los
afectados a emprender las acciones que tengan plenos efectos resarcitorios frente al daño sufrido.

El Esquema de Calidad, en los términos y con las limitaciones aquí señaladas, es de carácter universal en la
medida en que se aplica a todos los partícipes del sistema ACH COLOMBIA, lo que incluye los fallos de la misma
entidad compensadora ACH COLOMBIA, quien responde en igualdad de condiciones que los otros partícipes,
por los errores que puedan entorpecer la operación normal del sistema.

El Esquema de Calidad tiene por principio, tanto en su definición, cobro y pago, el trámite automático y
administrativo propio de una obligación común. En atención a dicho principio, los partícipes deben procurar
resolver las diferencias que puedan surgir sobre las mismas, ante la administración de ACH COLOMBIA, antes
que acudir o recargar las instancias estatutarias de resolución de conflictos.

Como directriz en este mismo punto, se recomienda que, en aras de la mejor atención a la clientela, se


### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 97 de 329
```
institucionalice como regla de conducta, en caso de conflicto entre las Entidades participantes frente a un
reclamo de un cliente, que en primer término se dé solución al impase del cliente por parte de su Entidad
Participante, y posteriormente se entre a discernir la responsabilidad de una u otra Entidad.

Las conductas descritas previenen a las Entidades participantes partícipes, de incurrir en las faltas más
frecuentes que de modo cotidiano afectan el sistema y desmejoran el servicio, pero en términos positivos
constituyen ante todo un mínimo para alcanzar los fines descritos. Se recomienda, por tanto, a las Entidades,
desarrollar mecanismos internos que promuevan la actividad de sus operadores dentro de una cultura de
máxima probidad moral, óptima preparación de los funcionarios y utilización eficiente de los recursos técnicos,
así como dentro del concepto de calidad total en el servicio.

La experiencia demuestra que establecer y mantener dentro del participante como regla operativa sin
excepciones, los estándares de verificación que den certeza a la entidad sobre todos los datos pertinentes del
destinatario final previenen con eficacia la ocurrencia de errores y trabas en la red.

El Esquema de Calidad no libera de la responsabilidad a ninguna Entidad Participante vinculada al sistema de
ACH COLOMBIA, de las obligaciones incluidas en el Contrato de Prestación de Servicios suscrito entre ACH
COLOMBIA y las Entidades participantes.

## 3.3. Solución de conflictos

En caso de presentarse conflictos frente a las posibles sanciones por incumplimiento del Esquema de Calidad,
o de presentarse cuestionamientos sobre las conductas de las Entidades participantes o de ACH COLOMBIA,
como consecuencia del proceso de operaciones efectuadas a través de ACH COLOMBIA, se deben utilizar los
mecanismos de solución existentes, tales como el Comité de Análisis de Reclamos o el Comité de Solución de
Conflictos (Ver numeral 2.8. Comité de Análisis de Reclamos de este manual).

## 3.4. Procedimiento

El régimen sancionatorio aquí previsto se basa en los mismos presupuestos de la responsabilidad objetiva, de
modo que comprobado el hecho e identificado el actor, se sigue por principio la definición de la consiguiente
responsabilidad, la cual se determina a través del siguiente procedimiento mínimo.

## 3.4.1. Consolidación de Información de Reclamos

Con base en los casos de reclamos, solicitudes y reversiones presentados por los partícipes a través del módulo
de reclamos y/o de la verificación de la información conocida y procesada por ACH COLOMBIA, se agrupan y
liquidan los eventos sancionables ocurridos del día 21 del primer mes al día 20 del siguiente mes, o el día
anterior (si el día 20 no es hábil), Ver Documento Relacionado: Instructivo Cálculo de Sanciones e Instructivo
Módulo de Reclamos.

Lo anterior permite a ACH COLOMBIA consolidar la información y a las Entidades Participantes afectadas tener
la posibilidad de hacer los descargos correspondientes. El cobro y pago de los valores de las sanciones, se hace
al mismo tiempo con los cobros de facturación mensual y en un solo pago consolidado con las comisiones de
acceso a la red y servicios.


### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 98 de 329
```
## 3.4.2. Producción de Reportes de Liquidación de Sanciones

Con base en la información consolidada, ACH COLOMBIA genera dos (2) reportes de liquidación por cada
Entidad Participante partícipe del sistema ACH COLOMBIA; en el primero se anotan las sumas que la misma
saldría a deber a ACH COLOMBIA y/o a los otros partícipes del sistema y, en el segundo, se registran, también
en forma discriminada, las sumas que eventualmente se consoliden en su favor, por conductas de los otros
partícipes, incluido ACH COLOMBIA, en las que haya resultado agraviada.

Los reportes de preliquidación quedan disponibles en el módulo de sanciones el día 25 o durante el día hábil
siguiente (sí el día 25 no es hábil), y hasta el día 29 o el siguiente día hábil (si el 29 no es hábil), en el horario de
8:30 a.m. a 3:00 p.m. para su consulta y validación por parte del participante.

De igual forma y después de recibir los comentarios pertinentes sobre la preliquidación, el primer día hábil del
mes siguiente, ACH COLOMBIA procederá a liquidar de manera definitiva las sanciones y servicios y avisará a
todas las Entidades participantes de la disponibilidad de los reportes definitivos en el módulo de facturación.

## 3.5. Oportunidad y requisitos para oponerse a la sanción

Una vez recibidos los reportes, el participante tiene hasta el día 30 o el siguiente día hábil (si el día 30 no es
hábil), la oportunidad de informar por escrito, su desacuerdo con los ítems atinentes a las sumas que sale a
deber y de aportar las pruebas que pretenda hacer valer. Si no lo hace, las cifras señaladas se liquidan y cobran
en su contra, el quinto día hábil del mes siguiente. El cobro y pago de los valores de las sanciones, se hace al
mismo tiempo con los cobros de facturación mensual de Tarifas de acceso a la red y en un valor neto único,
según se describe en el numeral 6.9. Archivo de pagos NACHA-M Seguridad Social.

Si cuestiona parcialmente el reporte, se liquidan y cobran las conductas no cuestionadas; en cuanto a las
conductas cuestionadas, se reúne el Comité de Análisis de Reclamos, numeral 2.8. Comité de Análisis de
Reclamos, previa confirmación de ACH COLOMBIA, el tercer día hábil de cada mes, con el fin de emitir un
concepto frente a las conductas cuestionadas.

En la medida en que los descargos a juicio del Comité de Análisis de Reclamos no resulten suficientes para
desvirtuar la responsabilidad objetiva aquí establecida, se procede a liquidar y cobrar la sanción; el cobro se
hace junto con la facturación de las comisiones por las transacciones procesadas en el período anterior. La
sanción debe ser pagada sin desmedro de la posibilidad de la participante respectiva de recurrir ante el Comité
de Solución de Conflictos.

Si los descargos resultaren suficientes para desvirtuar la responsabilidad objetiva aquí establecida, la sanción
no se incluye.

Cuando el agraviado sea una de las cinco (5) Entidades Participantes principales pertenecientes al Comité de
Análisis de Reclamos o ACH COLOMBIA, ésta se debe marginar en el momento de la toma de la decisión
respectiva, donde ella esté involucrada, y se da paso a una Entidad Participante delegada.

Esta situación debe ser informada por ACH COLOMBIA con anterioridad a los partícipes del Comité, con el fin


### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 99 de 329
```
de citar a la entidad delegada.

## 3.6. Otros motivos de inconformidad con los reportes de liquidación

La Entidad Participante que estime que ACH COLOMBIA no ha tenido en consideración faltas al Esquema de
Calidad, reportadas por ella, por ACH COLOMBIA, o por otro participe en su contra, puede dentro de la misma
oportunidad señalada en el numeral 7.5 presentar las razones de inconformidad y aportar las pruebas que
pretenda hacer valer.

Cuando ACH COLOMBIA omita una sanción en los reportes de liquidación, ésta es incluida en el siguiente
período, siguiendo el proceso normal establecido en el Esquema de Calidad.

Si se encuentra que una sanción fue liquidada como consecuencia de inconsistencias en el Esquema de Calidad,
el Comité de Reclamos o ACH COLOMBIA reconsidera el cobro de dichas sanciones para el período
inmediatamente anterior, para todas las Entidades participantes sancionadas, en tanto se precisa o ajusta el
Esquema de Calidad.

## 3.7. Excepciones

Teniendo en cuenta que se pueden presentar casos de fuerza mayor y de gran impacto en las entidades
participantes, que imposibiliten la prestación del servicio de acuerdo con lo establecido, se tiene previsto el
manejo de excepciones que justifiquen la no aplicación de sanciones.

## 3.7.1. Situaciones Previsibles

Los eventos programados como cambio de equipos, cambio de programas, traslados, mantenimientos,
pruebas, etc., se deben reportar a ACH COLOMBIA mínimo tres (3) días hábiles antes a la ocurrencia del evento
programado. ACH COLOMBIA reporta a las demás Entidades participantes los eventos programados de las
Entidades participantes y los propios de ACH COLOMBIA, a más tardar al siguiente día hábil de conocer la
información. La Entidad Participante debe reportar los cambios de última hora que se presenten a la fecha de
salida de los eventos programados, si es el caso.

En los casos en que el participante informe las situaciones previsibles con la debida anticipación a ACH
COLOMBIA, todos los eventos sancionables quedan eximidos de sanción, por un periodo máximo de quince
(15) días calendario a partir de la fecha de ocurrencia del evento programado, excepto los relacionados con el
pago de la compensación o de la contestación o solución de reclamos, solicitudes de reversión, o solicitudes
de certificación. Por lo tanto, no se exime de pago de sanción los eventos 16, 17, 18, 19, 20 y 21 según se
describen en el Anexo 6: Eventos Sancionables del Esquema de Calidad.

## 3.7.2. Situaciones No Previsibles

Para situaciones de fuerza mayor o caso fortuito, la participante afectada debe informar a ACH COLOMBIA
dentro de las siguientes ocho (8) horas hábiles, quien a su vez informa a las demás Entidades Participantes
dentro de las siguientes ocho (8) horas hábiles, después de recibida la notificación u ocurrido el evento (en el


### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 100 de 329
```
caso de ACH COLOMBIA).

## 3.8. Eventos sancionables

El Esquema de Calidad está dividido en sanciones para el participante y para ACH COLOMBIA, evaluando
transacciones no monetarias, transacciones monetarias y devoluciones. El detalle de cada uno de los eventos
sancionables para los diferentes tipos de transacciones, se describen en el Anexo 6: Eventos Sancionables del
Esquema de Calidad; Ver Documento Relacionado: Instructivo de Cálculo de Sanciones. En este anexo se utiliza
la siguiente nomenclatura para identificar la entidad a sancionar:

```
EP: Entidad Participante.
ACH: ACH COLOMBIA.
```
## 4. GESTIÓN DEL RIESGO Y SEGURIDAD DE LA INFORMACIÓN

## 4.1. Administración del riesgo de lavado de activos y financiación del terrorismo (SARLAFT) Información de carácter Confidencial

En materia de Administración del Riesgo de Lavado de Activos y Financiación del Terrorismo las Entidades
Participantes deben:

```
− Cumplir con la normatividad vigente en materia de Administración del Riesgo de Lavado de Activos y
la Financiación del Terrorismo, Siendo responsables por el conocimiento de su cliente. Cuando uno de
sus clientes utilice los servicios de ACH COLOMBIA se entiende que el participante ha aplicado los
controles requeridos por las normas y su sistema de administración y prevención para que no se
presente el riesgo de lavado de activos y financiación del terrorismo.
```
```
− Suministrar, en caso de requerirlo, la información necesaria que le permita a ACH COLOMBIA una
gestión adecuada del riesgo de contagio. En consecuencia, el participante deberá remitir la
información que le permita a la Entidad Participante Receptora de los recursos determinar de manera
precisa quien es el originador de los mismos. Se entiende por riesgo de contagio: aquella posibilidad
de pérdida que ACH COLOMBIA pueda sufrir, directa o indirectamente, por una acción u omisión de
las Entidades participantes vinculadas al sistema ACH COLOMBIA en materia de Administración del
Riesgo de Lavado de Activos y Financiación del Terrorismo.
```
```
− Mantener la información completa y actualizada de los usuarios y de los sujetos receptores o
recaudadores.
```
```
− Verificar el tipo, monto y periodicidad de las transacciones de los usuarios que se hagan por medio de
ACH COLOMBIA y que éstas correspondan al perfil definido por el participante. Se entiende que el
participante tiene conocimiento de su cliente y por lo tanto que el mismo está autorizado a realizar la
operación por esta.
```
```
− ACH Colombia, a través de la Gerencia de Cumplimiento, remite a los participantes mensualmente un
listado de señales de alerta correspondientes a sus clientes (usuarios de ACH Colombia). Es
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 101 de 329
```
```
responsabilidad de cada participante la gestión, análisis de la información y Reporte de Operación
Sospechosa a la UIAF (si así lo considera) de acuerdo con los criterios objetivos definidos por cada
Entidad. Estas alertas no omiten la obligatoriedad de los participantes a implementar su propio
SARLAFT, ni de ejecutar las actividades de identificación de operaciones inusuales bajo su propio
esquema de operación. Asimismo, es deber del participante mantener actualizado el correo de
recepción de las señales de alerta, comunicando los cambios que se presenten al buzón
sarlaft@achcolombia.com.co.
```
```
− Realizar el rechazo de las transacciones relacionadas con lavado de activos / financiación del
terrorismo impidiendo su ejecución.
```
## 4.2. ESQUEMA DE CALIDAD Y CONTROL DEL RIESGO

ACH Colombia como entidad regulada por la Superintendencia Financiera de Colombia, tiene un Sistema de
Administración de Riesgo integral, el cual dentro de su alcance gestiona riesgos operacionales, de seguridad
de la información y ciberseguridad, continuidad de negocio, fraude interno y transaccional, de lavado de activos
y financiación del terrorismo, así como también riesgos de tipo sistémico que puedan llegar a impactar a los
diferentes actores del ecosistema financiero. Estos sistemas de gestión cuentan con las políticas, herramientas
tecnológicas, procedimientos, metodologías y demás requerimientos, que sirven de marco de trabajo para la
adecuada administración de los riesgos; permitiendo asegurar un ambiente de control dando cumplimiento a
la normatividad y con metodologías basadas en buenas prácticas de industria.

El Sistema de gestión de Riesgo integral se encuentra documentando, actualizado en todas sus etapas donde
se identifica, mide, controla y monitorea los perfiles de riesgo de los procesos, servicios, canales, terceros,
proyectos y controles de forma integral con un equipo humano calificado y competente para el desarrollo de
esta gestión.

Teniendo en cuenta que, de acuerdo con la circular básica jurídica de la Superintendencia Financiera de
Colombia, por tener ACH Colombia las facultades de EASPBV (Entidades Administradoras de Sistemas de Pago
de Bajo Valor), deben establecer su modelo de gestión de riesgos con base en la identificación de los riesgos
propios de su actividad y a su perfil y apetito de riesgos, incorporando por lo menos aquellos definidos en la
norma y a los cuales puede verse expuesto en el ejercicio de su objeto social.

Por lo anterior, ACH Colombia certifica que se encuentra habilitado para cumplir los servicios ofrecidos, toda
vez que cuenta con metodologías y buenas prácticas de administración y gestión de riesgos.

```
4.2.1 Terminología
```
Riesgo: Es el efecto de la incertidumbre sobre los objetivos.

Riesgo Operacional (RO): Es la posibilidad de que la entidad incurra en pérdidas por las deficiencias, fallas o
inadecuado funcionamiento de los procesos, la tecnología, la infraestructura o el recurso humano, así como
por la ocurrencia de acontecimientos externos asociados a éstos. Incluye el riesgo legal.


### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 102 de 329
```
Seguridad de la Información/Ciberseguridad: Es el conjunto de políticas, estrategias, Metodologías, recursos,
soluciones informáticas, prácticas y competencias para proteger, asegurar y preservar La Confidencialidad,
integridad y disponibilidad de la información que se almacene, reproduzca o procese, así como el desarrollo
de capacidades empresariales para defender y anticipar las amenazas cibernéticas en los sistemas informáticos
y de la operación de la entidad.

Plan de Continuidad de Negocio: Es un plan logístico para la práctica de cómo una organización debe recuperar
y restaurar sus funciones críticas parcial o totalmente interrumpidas dentro de un tiempo predeterminado
después de una interrupción no deseada o desastre.

Riesgo de Lavado de activos y financiación al terrorismo LA/FT: Es la posibilidad de pérdida al introducir o
instrumentar dentro del sistema financiero; recursos provenientes de actividades relacionadas con el lavado
de activos y/o de la financiación del terrorismo.

Riesgo Legal: Es la posibilidad de pérdida en que incurre una entidad al ser sancionada u obligada a indemnizar
daños como resultado del incumplimiento de normas o regulaciones y obligaciones contractuales. El riesgo
legal surge también como consecuencia de fallas en los contratos y transacciones, derivadas de actuaciones
malintencionadas, negligencia o actos involuntarios que afectan la formalización o ejecución de contratos o
transacciones. Aplica a todas las actividades e incluye a terceros que actúen en representación de la entidad
respecto de los procesos y/o actividades tercerizadas.

Riesgo Reputacional: Es la posibilidad de pérdida en que incurre una entidad por desprestigio, mala imagen,
publicidad negativa, cierta o no, respecto de la institución y sus prácticas de negocios, que cause pérdida de
clientes, disminución de ingresos o procesos judiciales.

Riesgo de crédito: Es la posible pérdida que asume un agente económico como consecuencia del
incumplimiento de las obligaciones contractuales que incumben a las contrapartes con las que se relaciona.

Riesgo de liquidez: Dificultad de una empresa para poder hacer frente a sus obligaciones de pago a corto plazo
debido a la incapacidad de convertir sus activos en liquidez sin incurrir en pérdidas.

Riesgo de Contagio: Es la posibilidad de pérdida que una entidad puede sufrir, directa o indirectamente, por
una acción o experiencia de un vinculado. El vinculado es el relacionado o asociado e incluye personas naturales
o jurídicas que tienen posibilidad de ejercer influencia sobre la entidad.

Perfil de Riesgo: Resultado consolidado de la medición permanente de los riesgos a los que se ve expuesta la
entidad.

Riesgo Inherente: Nivel de riesgo propio de la actividad, sin tener en cuenta el efecto de los controles.

Riesgo Residual: Nivel resultante del riesgo después de aplicar los controles.

Riesgos sistémicos: Como entidad centralizadora y eje central del esquema de compensación a nivel nacional,
se han identificado riesgos que pueden generar afectaciones a los diferentes actores del ecosistema financiero,
los cuales son denominados sistémicos, estos provienen de diferentes tipos de riesgo como lo son:
Operacionales, Seguridad de la información, ciberseguridad, continuidad de negocio, fraude interno y
transaccional, lavado de activos y financiación del terrorismo, incumplimiento normativo y legal y de
Continuidad de Negocio.


### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 103 de 329
```
Además de lo anteriormente mencionado, ACH COLOMBIA cuenta con un Esquema de Calidad y Control de
Riesgos que hace parte de las exigencias que la participación en el Sistema ACH COLOMBIA impone a las
Entidades Financieras. La participación en el Sistema ACH COLOMBIA tiene un carácter profesional,
contractual y privado, se fundamenta en la buena fe de los partícipes, y su objetivo es garantizar la “mejora
en el servicio” y la “protección del sistema”, dentro de altos estándares operativos y técnicos.

El esquema de riesgos se basa en una metodología compuesta principalmente de cuatro fases, que son:
Identificación, medición, control y monitoreo; donde en su primera fase se identifican los riesgos a los que se
ve expuesto ACH Colombia de acuerdo con los factores de riesgo establecidos a nivel interno desde los
procesos, activos de información, tecnológicos, así como factores externos, con el fin de establecer el nivel de
exposición que tiene la compañía ante su materialización. Posteriormente se realiza la medición de los riesgos
identificados calificando su impacto y probabilidad de acuerdo con las escalas definidas y así poder determinar
el perfil de riesgo inherente. Luego se realiza la identificación de los controles que se han implementado para
la mitigación de cada uno de los riesgos, los cuales llevan una calificación a nivel de efectividad, la cual
determinará el nivel de mitigación de cada riesgo y así poder obtener el perfil de riesgo residual. Como última
fase se encuentra el monitoreo que se realiza de forma permanente a los controles, en cuanto a su diseño,
aplicabilidad y nivel de efectividad.

Este esquema de riesgos está enmarcado en un modelo de aseguramiento denominado “Modelo de las tres
líneas”, el cual permite desarrollar una estructura de niveles para garantizar un adecuado aseguramiento del
cumplimiento de los controles iniciando directamente desde la operación y a través de un monitoreo de áreas
especiales, finalizando con la revisión de la auditoría interna como área independiente.

## 4.3. Seguridad de la información

ACH COLOMBIA requiere la prestación de los servicios con altos estándares de seguridad y calidad,
especialmente aquellos que permitan asegurar la información que se utiliza en el desarrollo de estos. Así las
cosas, en su carácter entidad vigilada por la Superintendencia Financiera de Colombia, está obligada a cumplir
con las regulaciones que le apliquen, especialmente las concernientes a los requerimientos mínimos de
seguridad y calidad en el manejo de información a través de medios y canales de distribución de productos y
servicios establecidos en la Circular Básica Jurídica Externa 029.

Como consecuencia de lo anterior, en el desarrollo del servicio ACH los participantes deben y se obligan
recíprocamente, en su nombre y en el de sus miembros, empleados, contratistas o consultores, a mantener
un estricto deber de confidencialidad, frente a la información de dicho carácter que reciban o procesen, sin
desmedro de la información suministrada a las autoridades en desarrollo de los deberes de colaboración o
como producto de una exigencia o requerimiento de estas. Se deberá entonces limitar el acceso y uso de la
información exclusivamente a aquellos empleados suyos, proveedores, auditores o consultores que requieran
de dicha información para el cumplimiento del objeto del presente contrato, y se prohíbe a sus empleados,
proveedores o consultores a los que se le otorgue acceso a la información a divulgarla, utilizarla, copiarla o
reproducirla para cualquier finalidad distinta a la propia de este manual. Para el caso de las entidades
participantes del sistema y que tengan el carácter de vigiladas por la Superintendencia Financiera de Colombia
deben cumplir con sus obligaciones legales referentes a la reserva o secreto bancario.

Igualmente, las partes deberán dar cumplimiento a las directrices y requerimientos mínimos de seguridad y
calidad en el manejo de información a través de medios y canales de distribución de productos y servicios, en


### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 104 de 329
```
consonancia con lo establecido en la Circular Básica Jurídica Externa 029 de la Superintendencia Financiera de
Colombia.

Así mismo los Participantes vigilados y no vigilados deberán dar cumplimiento a los requisitos de seguridad y
calidad establecidos por ACH Colombia con el fin de asegurar la confidencialidad, disponibilidad e integridad
de la información en el Servicio de transferencias interbancarias, como se describe en este capítulo. Se debe
tener en cuenta que como parte del proceso de vinculación se debe entregar diligenciado el _Formulario de
Seguridad para Vinculación de Entidades Participantes_ , con el objetivo de confirmar que los participantes
cuentas con los controles y procedimientos necesarios para poder garantizar una adecuada gestión de
seguridad de la información, ciberseguridad, continuidad y SARLAFT para los procesos o servicios que estarán
en prestados por ACH COLOMBIA. Ver anexo _Formulario de Seguridad para Vinculación de Entidades
Participantes GIR-GRI-FOR- 031_

Las entidades que estén avaladas para utilizar el servicio ACH, deben trabajar proactivamente en la búsqueda
e implementación de esquemas que permitan la prevención y el control de fraudes a través del servicio ACH,
en este sentido, las entidades deben atender y colaborar en la aplicación de nuevas medidas que sean emitidas
por los entes reguladores o por los sistemas de control de riego de ACH. De igual forma los usuarios que están
autorizados para usar el servicio deben suministrar a través de su Entidad participante información veraz y
oportuna ante la ocurrencia de fraudes y es responsabilidad de la Entidad verificar dicha información.

ACH COLOMBIA cuenta con un Plan de continuidad, en los cuales se contempla la recuperación con esquemas
contingentes de la plataforma tecnológica y la operación que soporta el servicio ACH. Dicho plan se encuentra
definido, implementado, probado y se mantendrá durante la vigencia de la prestación del servicio.

De la misma manera, las Entidades vigiladas y no vigiladas deben tener una contingencia que les permita
recuperarse frente a cualquier evento.
Igualmente, las partes deberán dar cumplimiento a las directrices y requerimientos mínimos de seguridad y
calidad en el manejo de información a través de medios y canales de distribución de productos y servicios, en
consonancia con lo establecido en la Circular Básica Jurídica Externa 029.de la Superintendencia Financiera de
Colombia

Los usuarios que estén autorizados para utilizar el servicio ACH deben trabajar proactivamente en la búsqueda
e implementación de esquemas que permitan la prevención y el control de fraudes a través del servicio ACH,
en este sentido, los usuarios deben atender y colaborar en la aplicación de nuevas medidas que sean emitidas
por las Entidades participantes o por el sistema ACH. De igual forma los usuarios que están autorizados para
usar el servicio deben suministrar a través de su Entidad Participante información veraz y oportuna ante la
ocurrencia de fraudes. Será responsabilidad de el participante verificar dicha información.
.

ACH COLOMBIA declara que ha contratado pólizas de seguros que amparan estos riesgos con la suficiente
cobertura, comprometiéndose a mantenerlos vigentes. En caso de requerirse, ACH COLOMBIA presentará de
manera detallada con solo el requerimiento escrito en tal sentido, remitido por el funcionario autorizado por
ello, todos los documentos, soportes y planes que permitan validar la declaración contenida en la presente
cláusula. Igualmente, ACH COLOMBIA autoriza expresa e irrevocablemente a los participantes que contraten
el servicio de ACH a efectuar las visitas que consideren necesarias con el fin de verificar el cumplimiento de las


### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 105 de 329
```
obligaciones que asume en la presente cláusula. En caso de que ACH COLOMBIA no acredite el cumplimiento
de esta declaración, el participante podrá dar por terminado unilateralmente el presente contrato por esta
razón.


### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 106 de 329
```
## 4.4. Protección de datos personales

De conformidad con lo establecido en la ley estatutaria 1581 de 2012 y sus decretos reglamentarios,
compilados en el Decreto 1074 de 2015, se entiende que los datos personales son de titularidad exclusiva de
cada persona, los cuales son entregados por las Entidades Participantes en su calidad de responsables del
tratamiento para que ACH COLOMBIA en su calidad de Encargado realice su tratamiento para fines exclusivos
de los servicios ACH descritos en este manual.

Para el tratamiento de estos datos personales se aplicarán los principios indicados en la mencionada ley, que
a saber son:

a) Principio de veracidad o calidad de los registros o datos. Este principio establece que la información sujeta
a tratamiento debe ser veraz, completa, exacta, actualizada, comprobable y comprensible, prohibiendo el
tratamiento de datos parciales, incompletos, fraccionados o que induzcan a error.

Teniendo en cuenta que ACH COLOMBIA realiza el tratamiento de datos personales que son enviados por las
Entidades Participantes en su calidad de Encargado del Tratamiento, y que corresponden a los obtenidos de
los titulares clientes de estas entidades, el principio de veracidad corresponde cumplirlo a la(s) Entidad(es)
Participantes Responsables del Tratamiento. ACH COLOMBIA, mantendrá los datos que se reciban en las
mismas condiciones en las que los recibió.

b) Principio de finalidad. Este principio establece que el tratamiento de datos personales debe obedecer a una
finalidad legítima de acuerdo con la Constitución y la ley, la cual debe informársele al titular de la información
de forma previa o concomitantemente con el otorgamiento de la autorización, cuando ella sea necesaria.

Teniendo en cuenta que ACH COLOMBIA realiza el tratamiento de datos personales en su calidad de Encargado
del Tratamiento, la autorización para el uso de los datos personales será obtenida por la Entidad Participante
en su calidad de responsable del Tratamiento.

En caso de ser necesario, ACH COLOMBIA solicitará copia de dicha autorización a la Entidad Participante,
encontrándose esta última obligada a atender la solicitud dentro de los cinco (5) días hábiles siguientes.

c) Principio de circulación restringida. Este principio establece que el tratamiento de datos personales se sujeta
a los límites que se derivan de la naturaleza de los datos personales, de las disposiciones de la ley y la
Constitución.

Así mismo, establece que los datos personales, salvo la información pública, no podrán ser accesibles por
Internet o por otros medios de divulgación o comunicación masiva, salvo que el acceso sea técnicamente
controlable para brindar un conocimiento restringido solo a los titulares o los usuarios autorizados conforme
a la ley.

ACH COLOMBIA, en su calidad de Encargado del Tratamiento, prestará sus servicios permitiendo el acceso a
los datos solamente al titular de la información y a las Entidades Financieras o no financieras autorizadas por
el titular para ello. Se presume que con el envío de transacciones por el sistema ACH, las Entidades


### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 107 de 329
```
Participantes respectivas tienen la autorización para compartir y acceder a los datos de sus clientes. La(s)
Entidad(es) participantes deberán permitir en cualquier momento realizar la verificación de las autorizaciones
respectivas.

d) Principio de temporalidad de la información. Este principio establece que la información del titular no podrá
ser tratada cuando la finalidad para la cual fue recopilada termine, o la obligación legal o contractual que
requiere del tratamiento no se mantenga.

Al efecto, la información que se entrega a ACH COLOMBIA será utilizada únicamente con el fin de prestar los
servicios de transferencias electrónicas de fondos. Cualquier uso diferente a este por parte de las entidades
participantes autorizadas, será responsabilidad de estas.

e) Principio de seguridad. Este principio establece que la información sujeta a tratamiento ya sea por el
responsable o por el Encargado del Tratamiento, se deberá manejar con las medidas técnicas que sean
necesarias para garantizar la seguridad de los registros evitando su adulteración, pérdida, consulta o uso no
autorizado.

ACH COLOMBIA, como Encargado del Tratamiento, mantiene la información recibida en virtud de la prestación
de sus servicios bajo especiales medidas de seguridad, de acuerdo con lo establecido en el numeral 4.2. de
este mismo Manual.;

f) Principio de confidencialidad. Este principio establece que todas las personas que intervengan en el
tratamiento de datos personales que no tengan la naturaleza de públicos están obligadas en todo tiempo a
garantizar la reserva de la información, inclusive después de finalizada su relación con alguna de las labores
que comprende el tratamiento, pudiendo solo realizar suministro o comunicación de datos personales cuando
ello corresponda al desarrollo de las actividades autorizadas en la ley y en los términos de esta.

ACH COLOMBIA, garantiza la confidencialidad y reserva de la información personal que le es compartida en
virtud de la prestación de sus servicios, de acuerdo con lo establecido en el presente Manual.

PARAGRAFO PRIMERO: La obtención de autorización para el tratamiento de los datos personales es
responsabilidad única y exclusivamente de la(s) Entidad(es) Participantes al igual que el uso que le dé a la
misma, razón por la cual la (s) Entidad(es) Participantes, en su calidad de responsable(s) del tratamiento
garantizan que los datos personales que envían comparten y que solicitan, cuentan con la autorización
correspondiente otorgada por parte del titular de la información.

En caso de así requerirse, ACH COLOMBIA solicitará copia de dicha autorización a la(s) Entidad(es) Financiera(s)
o no financiera(s), encontrándose esta(s) última(s) obligada(s) a atender la solicitud dentro de los cinco (5) días
hábiles siguientes a la solicitud.

PARÁGRAFO SEGUNDO: En virtud de lo establecido en el Decreto 1692 de 2020, la(s) Entidad(es) Participantes
garantiza(n) que cuentan con una política de tratamiento y protección de datos personales y que ACH
COLOMBIA se reserva la facultad de poder solicitar en cualquier tiempo evidencia de esta declaración,
quedando dichas entidades en la obligación de entregarla en los términos establecidos por ACH COLOMBIA.
PARÁGRAFO TERCERO: En virtud de lo establecido anteriormente, ACH COLOMBIA, en su calidad de Encargado


### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 108 de 329
```
```
del Tratamiento, cumplirá con las siguientes obligaciones:
```
- Realizar el Tratamiento de los datos de acuerdo con lo señalado en el presente Manual. Garantizar que el
    Tratamiento de los Datos Personales se realiza cumpliendo con los protocolos de seguridad y
    confidencialidad establecidos en el presente Manual
- Dar Tratamiento, por cuenta del responsable del Tratamiento, a los Datos Personales recibidos en virtud de
    la prestación de los servicios, conforme a los principios que los tutelan.
- Guardar confidencialidad respecto del Tratamiento de los Datos Personales.
- Cumplir las instrucciones y requerimientos que imparta la Superintendencia de Industria y Comercio.
- Adoptar un procedimiento para la atención de consultas y reclamos en materia de tratamiento y protección
    de datos personales.
- Mantener la información bajo los parámetros de seguridad establecidos para impedir su adulteración,
    pérdida, consulta, uso o acceso no autorizado o fraudulento.
- Permitir el acceso a la información únicamente a las personas que pueden tener acceso a ella.
- Garantizar que sus empleados, contratistas, personal y en general cualquier persona que pudiera intervenir
    en cualquier fase contractual conozca y cumpla con las obligaciones aquí pactadas.

```
Así mismo, la(s) Entidad(es) Financiera(s) o no financiera(s), en adición a las demás obligaciones establecidas
en el presente manual, frente a su calidad de responsable(s) del Tratamiento cumplirá(n) con las siguientes
obligaciones:
```
- Suministrar a ACH COLOMBIA, como Encargado del Tratamiento, únicamente datos cuyo Tratamiento esté
    previamente autorizado por el titular de la información.
- Suministrar a ACH COLOMBIA, como Encargado del Tratamiento, únicamente los datos cuyo Tratamiento
    sea necesario para el desarrollo y prestación de los servicios de transferencias electrónicas de fondos
    prestados por ACH.
- Comunicar de forma oportuna al ACH COLOMBIA todas las novedades respecto de los datos que
    previamente le haya suministrado.
- Conservar copia de la Autorización otorgada por el Titular, la cual deberá ser entregada a ACH COLOMBIA
    en caso de llegarse a requerir.
- Comunicar a ACH COLOMBIA cuando la información por el entregada sea incorrecta.

## 4.5. Esquema de Contingencias

```
Se describe a continuación el esquema previsto para ser utilizado en casos de Contingencias en el participante
por fallas en los diferentes mecanismos existentes para que las Entidades Participantes puedan resolver
situaciones que no les permite generar o transmitir los archivos asociados al servicio de transferencias
Interbancarias por ACH Colombia.
A continuación, se encuentra el procedimiento de contingencia para el evento en que se presenten incidentes
que les desvíen su operación normal de trabajo.
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 109 de 329
```
## 4.5.1. Matriz de Escalonamiento por Contingencia

Durante la operación normal de generación, envío y recepción de archivos de transferencias Interbancarias de
ACH COLOMBIA, se pueden presentar diferentes tipos de fallas técnicas que impidan a las Entidades
Participantes el envío y recepción de estos; las fallas se pueden presentar bajo los siguientes escenarios a los
cuales previendo la ocurrencia de cualquiera de estas situaciones, ACH COLOMBIA ha dispuesto una serie de
actividades de manera que las Entidades Participantes recurran a ellas como estrategias de contingencia, con
el fin de asegurar la disponibilidad del Servicios. A continuación, se especifica, para cada escenario, las
diferentes estrategias que podrán aplicar dependiendo de las condiciones técnicas, físicas y logísticas que tenga
en el momento de su ocurrencia

## 4.5.1.1. Sebra – Inconveniente Para el Acceso al Portal

− Actividades descritas en la circular reglamentaria externa DGT- 273 (Contingencias Banco de la Republica)
convenio para acceder a oficinas de una entidad amiga para tener acceso a un terminal de SEBRA. La
Entidad Participante en problemas podrá solicitar a otra Entidad Participante que le permita operar desde
sus instalaciones, sin que se requiera informar la activación de esta contingencia al Banco de la República,
en razón a la disponibilidad del portal de servicios W-SEBRA. Bajo este esquema, dicha estrategia se
constituye en un servicio prestado entre las Entidades Autorizadas, en el cual la Entidad vecina acepta
prestar su canal. Se debe tener en cuenta que es necesario reconfigurar las carpetas de entrada y salida
del Gateway antes y después de la contingencia en la estación prestada por la Entidad vecina. Los demás
procedimientos serán los mismos que se ejecutan cuando se trabaja desde la estación propia de la entidad.
− Comunicarse a la línea de atención de ACH Colombia (57 1) 7438300 informando la situación presentada.

## 4.5.1.2. Cargue de Archivos Nacha-m (servicio afectado: ACH)

```
En el evento de identificar incidencias asociadas en el servicio de ACH que afecten la transmisión de
archivos, el participante deberá radicar la solicitud a través del Service Now notificando la incidencia y a su
vez adjuntando el archivo NACHA – M a cargar en el aplicativo INTEGRA ACH cifrado, por otra parte el
participante deberá manifestar en el caso hacerse acreedora del conocimiento de riesgos, aceptación de
responsabilidad y así mismo debe remitir la siguiente información para procesar su solicitud:
```
- Nombre del archivo NACHA - M
- Valor transacciones débito.
- Valor transacciones crédito.
- Valor total del archivo.
- Conocimiento por parte del banco frente al riesgo y aceptación de la responsabilidad.
- Autorización por parte del banco a ACH para aprobar las transacciones de montos superiores
    contenidas en el archivo.
- La solicitud de el participante la debe dirigir el funcionario responsable de la operación ante ACH.

```
Posteriormente deberá comunicar el incidente al área de operaciones.
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 110 de 329
```
```
Como segunda alternativa el participante puede enviar al correo electrónico
procesamiento@achcolombia.com.co , el archivo NACHA - M cifrado.
```
```
La información que debe contener el correo electrónico es la siguiente:
```
- Nombre del archivo NACHA - M
- Valor transacciones débito.
- Valor transacciones crédito.
- Valor total del archivo.
- Conocimiento por parte del banco frente al riesgo y aceptación de la responsabilidad.
- Autorización por parte del banco a ACH para aprobar las transacciones de montos superiores
    contenidas en el archivo.
- La solicitud de el participante la debe dirigir el funcionario responsable de la operación ante ACH.

− Si los usuarios para el ingreso INTEGRA ACH y el administrador son bloqueados simultáneamente, desde
ACH COLOMBIA, se realiza el desbloqueo del usuario del Administrador de Usuarios primario a través del
ServiceNow.
− Comunicarse a la línea de atención de ACH Colombia (57 1) 7438300 o al celular 3134596869 o enviar un
correo electrónico a cuenta911@achcolombia.com.co.

## 4.5.1.3. Descargue de Archivos Nacha-m (servicio afectado: ACH)

− Comunicarse directamente con el área de Operaciones de ACH COLOMBIA: Gerencia de Entrega o
remitiendo un correo al buzón entrega@achcolombia.com.co
− Comunicarse a la línea de atención de ACH Colombia (57 1) 7438300
− Si se presentan problemas de acceso en la aplicación de Integra ACH, y el archivo se encuentra cifrado,
desde la Gerencia de Entrega se enviará por correo electrónico y este correo se cifrará usando la palabra
confidencial en el asunto y cuerpo del mensaje.
− Si los usuarios para el ingreso INTEGRA ACH y el administrador son bloqueados simultáneamente, desde
ACH COLOMBIA, se realiza el desbloqueo del usuario primario a través del Service Now.

## 4.5.1.4. Demora en los cierres de Ciclo Operacionales de ACH

− Comunicarse directamente con el área de Operaciones de ACH COLOMBIA: Gerencia de Procesamiento.
− Comunicarse a la línea de atención (57 1) 7438300
ACH Colombia notificará desde Integra ACH cuando se presenten demoras en los cierres de ciclo.

## 4.5.1.5. Problemas de Encripción o Certificados de Archivos Nacha - m

− Radicar solicitud a través Centro Integral de Servicios vía, Service Now y comunicar el incidente al área de
operaciones y al centro de experiencia de cliente ACH COLOMBIA.


### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 111 de 329
```
− Comunicarse a la línea de atención (57 1) 7438300; a los celulares: 3155949415 – 3138929583, y se dará
el paso a seguir dependiendo de caso que se esté presentando.
− En caso de tener problemas en el envío archivos por vencimiento de certificados digitales de Certicamara
comunicarse a la línea de atención (57 1) 7438300 o a los celulares: 3155949415 – 3138929583, y se dará
el paso a seguir dependiendo de caso que se esté presentando.

```
4.5.1.6 Procedimiento para generar excepción de OTP
```
− Radicar solicitud en Service Now (Inicio - > Todos los catálogos - > Externo - > Administración de usuarios -
> Desbloqueo de usuarios)
➢ Servicio General: ACH
➢ Servicio/módulo: INTEGRA ACH
− Si el usuario de la entidad participante afectada no cuenta con acceso al Service Now, debe enviar vía
correo electrónico (centrodeexperiencia@achcolombia.com.co ) la solicitud de excepción de OTP.
− El usuario debe incluir la siguiente información independientemente del canal que lo realice
(Correo/Services Now):
− Informar si la solicitud de excepción es por usuario o por entidad.
− Tiempo requerido para mantener la excepción (cantidad de minutos). No se puede solicitar más de un
(1) día la excepción.
− El usuario se puede comunicar al centro de experiencia de cliente ACH COLOMBIA, en el horario de
atención dispuesto al número ( 601 7438300 ext. 2226 ), siempre y cuando tenga como soporte el número
del caso radicado o el correo enviado, para solicitar el respectivo seguimiento de la solicitud.

## 4.5.2. Contingencias en el proceso de transmisión

La Entidad Participante cuenta con un canal dedicado o conexión VPN para la transmisión de datos hacia y
desde ACH COLOMBIA. En caso de no poder establecer comunicación con ACH COLOMBIA utilizando su canal
principal, o su sistema alterno automático, el participante debe ejecutar la lista de chequeo.
Si no se logra resolver el inconveniente, el participante debe seguir el procedimiento definido para el tipo de
contingencia previo seleccionado (contingencia básica), o utilizar alguna de las opciones disponibles definidas
como contingencia extrema. En cualquier caso, los archivos de transacciones deben ser remitidos por
cualquiera de los medios posibles, dentro de los horarios máximos definidos en el Capítulo 3, Ciclos de Proceso
y Horarios.

## 4.5.2.1. Lista de Chequeo

```
La Entidad Participante debe verificar uno a uno los siguientes elementos de control de la lista de
chequeo para lograr establecer comunicación efectiva con ACH COLOMBIA:
```
```
− Verificación Inicial Estación de Trabajo: Validar si la estación de trabajo se encuentra
correctamente conectada tanto a las fuentes de poder como red de comunicaciones. En el
caso de encontrarse problemas en la estación de trabajo, el participante debe corregirlo, La
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A

```
Agosto de 2024
Página 112 de 329
```
```
Entidad Participante debe contar con una estación de trabajo de “backup”, que permita
realizar las mismas actividades que se realizan en la estación de trabajo principal.
− Detección puntual de fallas en la red: Detectar los lugares donde posiblemente pueda
presentarse un mal funcionamiento con las comunicaciones en particular. En este caso debe
contactar a la persona responsable de las comunicaciones en el participante
− Si las pruebas dan como resultado inconveniente en las comunicaciones, se debe
documentar el problema y reportarlo a ACH COLOMBIA con el fin de hacer las pruebas de
conexión e informar la situación al proveedor de servicios de telecomunicaciones y
monitorear que la solución se dé dentro de los tiempos máximos establecidos.
```
## 4.5.2.2. Contingencia Básica

```
Si seguidos los procedimientos anteriores, se encuentra que el problema no puede ser solucionado
inmediatamente, el participante debe utilizar la contingencia básica seleccionada y probada con
anterioridad por el participante y por ACH COLOMBIA.
```
```
La Entidad Participante debe notificar a través de los funcionarios autorizados a ACH COLOMBIA, la
situación de contingencia con anticipación suficiente para hacer el envío y recepción de
transacciones, de forma exitosa y dentro de los horarios máximos establecidos, teniendo en cuenta
los volúmenes de transacciones y la capacidad de los canales de contingencia básica.
```
```
ACH COLOMBIA prepara el ambiente de contingencia necesario (equipos, programas y usuarios),
para que el participante pueda establecer la comunicación exitosa por este medio.
```
```
El mecanismo alterno o contingencia básica disponible es la transmisión utilizando Internet mediante
archivo encriptado. A continuación, se describe en forma detallada cada uno de ellos.
```
### CONTINGENCIAS BÁSICAS PRESELECCIONADAS

```
Mecanismo Alterno Descripción
```
```
Conexión Por enlace dedicado
de contingencia
```
```
Si el problema es originado por fallas en el enlace o en los equipos
de comunicaciones del enlace principal, el participante puede
conectarse a través del enlace alterno. Esto se logra acudiendo a
la URL entregada por el proveedor para tal fin.
```
```
Envío archivos de datos
```
```
Como los archivos de datos se encuentran encriptados, el
participante puede enviar los archivos para proceso a través del
correo electrónico procesamiento@achcolombia.com.co
solicitando formalmente el cargue de archivo NACHA-M.
```
```
El formato del correo debe contener la siguiente información:
```
- Nombre del archivo NACHA - M
- Valor transacciones débito.
- Valor transacciones crédito.


### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A

```
Agosto de 2024
Página 113 de 329
```
### CONTINGENCIAS BÁSICAS PRESELECCIONADAS

```
Mecanismo Alterno Descripción
```
- Valor total del archivo.
- Conocimiento por parte del banco frente al riesgo y
    aceptación de la responsabilidad.
- Autorización por parte del banco a ACH para
    aprobar las transacciones de montos superiores
    contenidas en el archivo.
- La solicitud de el participante la debe dirigir el
    funcionario responsable de la operación ante ACH.

## 4.5.2.3. Contingencia Extrema

```
Si el uso de la contingencia básica presenta inconvenientes o si la transmisión de archivos no resulta
exitosa, el participante puede utilizar alguna de las contingencias extremas disponibles. Los
funcionarios autorizados de el participante deben notificar a ACH COLOMBIA la situación, con
anticipación suficiente para realizar el envío de archivos dentro de los horarios máximos establecidos
en el Capítulo 3 Ciclos de Proceso y Horarios.
```
```
A continuación, se presenta una descripción de cada una de las contingencias extremas disponibles
para las Entidad Participante:
```
### CONTINGENCIAS EXTREMAS DISPONIBLES

```
Mecanismo Descripción
Estación en otra
Entidad
Participante
Vinculada
```
```
Si la falla en las comunicaciones se presenta por daño en el canal de
comunicación, el participante puede, previo acuerdo directo entre las partes,
utilizar la estación de trabajo e infraestructura de alguna de las Entidades
Participantes vinculadas, cumpliendo los horarios previstos de envío y recepción.
ACH COLOMBIA debe ser notificado por los funcionarios autorizados de el
participante que presta el servicio y ayuda a la entidad que presenta la situación
de contingencia. ACH COLOMBIA no interviene en ningún caso en los acuerdos
establecidos por las Entidades Participantes en cuestión
Estación en
ACH COLOMBIA
```
```
Si la falla en las comunicaciones se presenta por daño en el canal de
comunicación, ACH COLOMBIA asignará dentro de sus instalaciones una estación
de trabajo a el participante que lo solicite con por lo menos 30 minutos de
anticipación. El operador autorizado por el Administrador de Procesos Diarios de
el participante podrá desplazarse a ACH COLOMBIA y desde allí enviar sus
archivos de transacciones, previa autorización de ACH COLOMBIA. El operador
deberá identificarse ante el director de Operaciones o el Gerente de
Procesamiento y Vinculación, presentando una autorización firmada por el
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 114 de 329
```
### CONTINGENCIAS EXTREMAS DISPONIBLES

```
Mecanismo Descripción
Administrador de Procesos Diarios autorizado en ACH COLOMBIA. Para la entrega
de archivos de ACH COLOMBIA a el participante, el mismo funcionario deberá
esperar que ACH COLOMBIA culmine el procesamiento para que él mismo
descargue desde la Estación de Trabajo el(los) archivo(s) de salida y ser enviados
por correo electrónico a el participante.
```
```
En caso de no tener correo electrónico se debe generar la excepción al interior
de ACH COLOMBIA para habilitar la unidad extraíble.
```
```
La Entidad Financiera será responsable por la información de cargue y descargue
de archivos en el sistema, así mismo la Entidad deberá garantizar que la unidad
extraíble no contenga código malicioso o malware, haciéndose responsable por
los daños y perjuicios que esto pueda llegar a ocasionar al interior de ACH
COLOMBIA.
```
## 4.5.3. Contingencias en el proceso de ACH COLOMBIA

ACH COLOMBIA cuenta con un sistema de proceso alterno en la nube que provee las herramientas necesarias
para dar continuidad al servicio en caso de presentarse una situación de Contingencia en las instalaciones de
ACH COLOMBIA. Este esquema está en capacidad de atender el proceso diario normal de envío de
transacciones, validación de archivos, clasificación, compensación y distribución de archivos para cada uno de
los Ciclos de Operación.

## 5. ESPECIFICACIONES TÉCNICAS

## 5.1. Requerimientos Técnicos

Las siguientes son las condiciones técnicas mínimas que deben cumplir todas las Entidades Participantes para
su conexión y operación con ACH COLOMBIA.

## 5.1.1. Equipos y sistema operativo

La Entidad Participante debe tener como mínimo una estación de trabajo principal y una de contingencia, y
discrecionalmente estaciones adicionales para las áreas involucradas en el proceso diario. Para cada estación
debe contar con los equipos de comunicaciones y de contingencia según lo descrito en el numeral 5.2 esquema
de comunicaciones y adicionalmente contar como mínimo con las siguientes herramientas:

```
− Sistema Operativo Windows 10, Superior o Windows server con soporte de Microsoft.
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 115 de 329
```
```
− Navegadores de internet actualizados (sugerido Google Chrome, Mozilla FirEPOx, Microsoft Edge). Se
sugiere inhabilitar el traductor del navegador para mejorar la experiencia de navegación en Integra
ACH.
− Aplicativos de apoyo para la verificación de reportes del servicio. (Microsoft EXCEL, PDF, visores de
archivos plano) esto para visualizar archivos en extensiones (.txt, .PDF, .xlsx, .xls etc.)
```
```
− Comunicaciones:
```
```
− Conexión con el enrutador al cual llega el enlace de ACH COLOMBIA.
− Configurar el protocolo TCP/IP con la dirección asignada por ACH COLOMBIA.
```
```
− Seguridad y Controles:
```
```
− Ver numeral 5.4.1. Estación de trabajo, hardware y software.
```
## 5.1.2. Programas

Para tener acceso a los servicios de ACH COLOMBIA, a través del sistema Integra ACH, el participante debe
contar con el siguiente software para conectarse con el aplicativo Integra ACH:

```
− Navegadores de internet actualizados (sugerido Chrome, FirEPOx, Microsoft Edge) esto para
navegación y visualización de manuales de operación.
− Programa compatible para la visualización de documentos PDF, se sugiere Acrobat Reader última
versión.
```
## 5.1.3. Configuración

Las estaciones deberán contar con los permisos necesarios para acceder a las direcciones IP de los Servidores
de producción y de pruebas de ACH COLOMBIA, con los servicios y puertos que se requieren.

Para que TCP/IP funcione en la estación de trabajo, ésta se debe configurar manualmente con direcciones IP
fijas, máscaras de subred y la puerta de enlace o “Gateway” predeterminado para el adaptador de red del
equipo, las direcciones se suministrarán al responsable del área de comunicaciones de la entidad por correo
electrónico, previa solicitud escrita.

Para tener acceso a los servicios de ACH COLOMBIA, a través del sistema Integra ACH, el participante debe
contar con un explorador o “browser” que le permita navegar en la extranet provista por ACH COLOMBIA. Los
manuales de operación pueden ser vistos Por medio de Acrobat Reader

## 5.1.4. Contingencia

Periódicamente el participante debe probar el esquema de contingencia seleccionado.


### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 116 de 329
```
## 5.1.5. Certificados Digitales

Para que las Entidades Participantes entren a participar del servicio de transferencias interbancarias ACH, es
necesario que cuenten con certificados digitales, para lo cual nos permitimos relacionar las siguientes
características que debe tener el certificado:

1. El certificado de firma digital debe cumplir el estándar ITU X.509 V3 y las disposiciones de campos
    mínimos requeridos definidas por la normatividad vigente.
2. El certificado de firma digital para el intercambio de información en INTEGRA ACH con entidades
    Participantes y comercios debe tener como mínimo las siguientes características:

```
a. Algoritmo de firma: SHA2
b. Algoritmo de Has de firma: SHA256
c. Fecha de inicio y Fecha de fin no mayor a 3 años.
d. Uso de la clave: Firma digital, no repudio, cifrado de clave, cifrado de datos, acuerdo de clave.
e. Longitud de la clave RSA 2048
```
3. El certificado de firma digital debe contener los datos mínimos requeridos para su emisión: Documento
    de identidad, Nombre, Dirección, Teléfono y Correo electrónico, además de los definidos en el Artículo
    35 de la Ley 527 de 1999.
4. El certificado deberá ser compatible con los formatos de entrega PKCS#12 y .cer (x509 V3), para la
    emisión en formato .cer la entidad de certificación deberá generar el certificado incluyendo el uso
    determinado por la entidad en el campo OU u otro especifico definido para identificar el propósito de
    la firma.
5. El emisor de los certificados de firma digital deberá estar avalado por la ONAC para emitir los
    certificados de firma digital.
6. El certificado de firma digital deberá posibilitar de manera automática los atributos de seguridad
    jurídica de Autenticidad, Integridad y No repudio, dentro de las comunicaciones electrónicas en las que
    se incorpore el certificado de firma digital.
7. El emisor del certificado de firma digital deberá ofrecer un servicio de consulta CRL y OCSP en línea
    (Lista de Certificados Revocados).
8. El certificado de Firma Digital Persona Jurídica deberá cumplir con la siguiente especificación según el
    marco normativo en Colombia:

```
a. Identificar identifica a una persona jurídica de derecho público o privado, entidad del Estado
o persona jurídica comerciante inscrito en el registro mercantil de las Cámaras de Comercio,
quien tiene el derecho de uso de un determinado sistema de información que será
programado para firmar de manera automatizada o manual a nombre de esa persona jurídica.
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 117 de 329
```
```
Así mismo, tendrá la calidad de suscriptor la persona natural que actúa como representante
legal de la persona jurídica.
Garantizando el cumplimiento de las siguientes condiciones en conjunto y simultáneamente:
(i) que una persona jurídica determinada se ha identificado como tal y ha solicitado el
servicio a través de su representante legal, y
(ii) que esa persona jurídica podrá programar un sistema de información para que firme
digitalmente mensajes de datos, de manera masiva o individual a través de medios
electrónicos, vinculándose jurídicamente.
```
Se recomienda contar con una herramienta que permita cumplir estos estándares de cifrado, ya que
actualmente ACH Colombia utiliza un sistema para cifrado de archivos mediante un proveedor externo, por lo
cual, el desarrollo y soporte asociado al cifrado de archivos por el participante, deben ser validados con sus
propios proveedores o desarrolladores; en el anexo 21 del presente manual de servicio se encuentra la
definición de la construcción de la mensajería encriptada para archivos nacha – M.

## 5.2. Esquema de Comunicaciones

Este numeral describe el Esquema de Comunicaciones existente entre las Entidades Participantes y ACH
COLOMBIA, y los servicios que el participante debe contratar con el proveedor de comunicaciones y los
requerimientos para establecer una conexión segura y eficiente entre las entidades.

## 5.2.1. Antecedentes

ACH COLOMBIA ha conformado una red única de comunicaciones para la transferencia electrónica de datos
entre las Entidades Participantes socias y ACH COLOMBIA; para facilitar el proceso de envío y recepción de
transacciones que se procesan a través del sistema, con el objeto de mejorar el monitoreo de la red y obtener
beneficios económicos por negociación global.

Después de considerar diferentes alternativas de varios proveedores y habiendo evaluado dentro de las
propuestas aspectos técnicos, económicos y administrativos, fue seleccionada y aprobada por la Junta Directiva
un proveedor único para la prestación del servicio de comunicación entre las Entidades Participantes y ACH
COLOMBIA, y por ende no podrá ser utilizado un esquema diferente al establecido donde cada entidad debe
contratar para el servicio con el proveedor Claro y quien entregara en unión temporal un enlace de
contingencia con el proveedor Lumen. Lumen

## 5.2.2. Servicio contratado

El servicio contratado con el proveedor de comunicaciones es CLARO y quien realiza una unión temporal con
el proveedor LUMEN para poder entregar un enlace de contingencia. Estos cuentan con características de
conexión similares y permiten disponer de un enlace digital entre las Entidades Participantes y ACH COLOMBIA
con las siguientes características, entre otras:

```
− Velocidad de mínimo 1Mbps
− Utilización de los Equipos de enrutamiento y direccionamiento IP.
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 118 de 329
```
```
− Utilización de los Equipos/mecanismos de cifrado (IPSec)
− Administración y monitoreo de la red.
− Canal de Contingencia (“disaster recovery”) en el centro alterno de ACH COLOMBIA.
− Mantenimiento preventivo y correctivo del enrutador.
− Contingencia de última milla en ACH COLOMBIA.
```
Los contratos con los proveedores y la negociación de estos enlaces lo realizan directamente de los
participantes con el proveedor Claro, pero únicamente bajo el esquema establecido (Red de Convergencia) por
lo cual el soporte, administración y mantenimiento de estos enlaces es responsabilidad del participante y no
podrán modificarse dichas condiciones sin previa autorización de ACH Colombia.

## 5.2.3. Beneficios

El Esquema de Comunicaciones y los servicios contratados por cada participante bajo este esquema de
comunicaciones, brinda a ACH COLOMBIA y a las Entidades Participantes los siguientes beneficios, entre otros:

```
− Conexión dedicada de el participante con ACH COLOMBIA 7X24
− Seguridad en el proceso de transmisión de datos
− Alta disponibilidad de los canales de comunicación
− Detección y corrección oportuna de problemas de comunicaciones
− Uso de contingencia automática (“disaster recovery”)
− Soporte permanente garantizado
− Contingencia de última milla con diferente medio
− Uso del mismo direccionamiento para conexión de varias terminales desde el participante
− Calidad estándar en el servicio
− Actualización en tecnología sin compra de equipos
− Soporte del proveedor 7X24 (Claro, Lumen)
```
## 5.2.4. Configuración general

A continuación, se muestra la configuración del Esquema de Comunicaciones de ACH COLOMBIA con las
Entidades Participantes, incluyendo el escenario de contingencia.
Esquema de Comunicaciones


### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 119 de 329
```
## 5.2.5. Requerimientos de el participante

Además de los requerimientos técnicos mencionados en este ítem, el participante debe habilitar los siguientes
programas y equipos:

## 5.2.5.1. Programas

```
Protocolo TCP/IP en la Estación de Trabajo: Para que TCP/IP funcione en el equipo, éste se debe
configurar manualmente con direcciones IP, máscaras de subred y la puerta de enlace o “Gateway”
predeterminado para el adaptador de red del equipo.
```
## 5.2.5.2. Equipos y Dispositivos Físicos

```
La Entidad Participante debe disponer de un área de acceso restringido (Centro de Cómputo) para
instalar los equipos de comunicación relacionados:
```
```
− Un armario o “rack” en el cual se puedan instalar los equipos de comunicaciones y asegurar que
estén claramente identificados de los equipos propios del participante.
− Un concentrador o “hub” o un switch que permite la comunicación entre las estaciones de
trabajo con los servidores de ACH. No requiere de una configuración especial para su
funcionamiento. Esta máquina debe ser suministrada por el participante si desea instalar más de
una estación de trabajo.
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 120 de 329
```
```
La Entidad Participante debe garantizar los lineamientos descritos a continuación, los cuales definen
la seguridad, el monitoreo permanente y el funcionamiento del sistema:
```
```
− El proveedor de comunicaciones le entrega e instala en calidad de arrendamiento un enrutador
con cada canal dedicado o conexión VPN a el participante en el o los datacenter ́s que la entidad
defina para tal fin. El mantenimiento y reparación de estos equipos se debe solicitar al proveedor
de comunicaciones La configuración de estos equipos es realizada totalmente por el proveedor
de comunicaciones juntamente con la entidad Participante y si se requiere con el apoyo de ACH
Colombia.
− Todos los equipos de comunicación disponen de una clave de acceso asignada y administrada
por el proveedor, la cual permite controlar el acceso no autorizado y el cambio en la
configuración de estos.
− Las modificaciones, adiciones, y movimientos de los equipos de comunicaciones deben ser
reportados a la Dirección de Tecnología ACH COLOMBIA para evitar cambios en la configuración
o en la calidad del canal y de los niveles de seguridad, con el fin de coordinar con el proveedor
las labores a llevar a cabo, así como la inclusión de nuevas entidades participantes dentro del
esquema para poder asignar el direccionamiento correspondiente.
− Atender las recomendaciones dadas por ACH COLOMBIA para el mejoramiento de la seguridad
en el sistema.
− Contar con procedimientos de contingencias definidos y documentados y seguir los planes y
pruebas de contingencia y recuperación diseñados por ACH COLOMBIA.
− El puerto que se debe habilitar a nivel de comunicaciones y a nivel de Firewall o enrutadores para
comunicación con ACH COLOMBIA es HTTPS (Puerto TCP 443).
```
## 5.3. MODELOS DE CONECTIVIDAD

Con el fin de aumentar la oferta de valor de ACH hacia las Entidades Participantes, se ha dispuesto de un canal
adicional de conectividad dispuesto a través del servicio de VPN; a continuación, se detallan las características
técnicas de los métodos de conexión dispuestos por ACH.

## 5.3.1. Conexión Canales Dedicados

La primera opción de conectividad de las entidades financieras contra ACH Colombia es a través de canales
dedicados; las principales características de este medio de conectividad son:

- Dos canales dedicados en una red MPLS con los proveedores Claro (Canal Principal) y Cirion (Canal
    Backup),
- La contratación como el soporte del canal principal y el canal Backup solo se hace con Claro Colombia;
    esta compañía tiene una alianza estratégica con Cirion para la implementación, funcionamiento y
    soporte del canal Backup.
- Los canales dedicados funcionan con el protocolo HSRP, esto significa que el funcionamiento de estos
    es activo y pasivo.
- Los canales dedicados son cifrados mediante el protocolo IPSEC; esta caracteriza es configurada por
    Claro Colombia y está incluida en la adquisición del servicio.
- El direccionamiento IP para la conexión entre la entidad financiera y ACH Colombia es un
    direccionamiento privado en el segmento _172.30.XX.0/24_ , donde el tercer octeto identifica la entidad


### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 121 de 329
```
```
financiera y solamente se trabajará con el direccionamiento privado anteriormente mencionado (no
se trabaja o acepta segmentos de red 10.0.0.0/8 o 192.168.0.0/16, además, solo se trabaja con la red
172.30.0.0/16).
```
- Por entidad financiera, solo se asignará una dirección IP en el segmento 172.30.XX.0/24, en la
    implementación y después, solo se trabajará con un solo segmento de red, si después la entidad
    financiera adquiere otros servicios con ACH Colombia, trabajará en el mismo segmento de red
    asignado.

La siguiente ilustración corresponde al diagrama de conectividad de canales dedicados

La tabla se visualiza el direccionamiento IP para conectar los servicios

```
Integra Transferencias Interbancarias
```
**BANCO:** (^) **_Nombre de Banco_** **REDES ASIGNADAS**
CANAL CLARO: Ejemplo: 172.30.XX.0/24
LUMEN Ejemplo: 172.30.XX.0/24
**BANCO >>HACIA >> ACH
ORIGEN AUTORIZADO DESTINO AUTORIZADO**
DIRECCION IP NOMBRE DIRECCION IP APLICACIÓN PUERTO
**Ejemplo:
172.30.XX.11a15** Term_TRANSFERENCIAS_EF_PROD^ **172.30.19.20**^
Integra_Transf_Interbancarias_Producci
ón 443
**Ejemplo:
172.30.XX.11a15** Term_Facturación_TRANSFERENCIAS_EF^ **172.30.19.21**^ Facturación^443
**Ejemplo:
172.30.XX.16**
Term_Pruebas_TRANSFERENCIAS_PRUEB
AS **172.30.19.22**^ Integra_Transf_Interbancarias_Pruebas^443


### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 122 de 329
```
## 5.3.2. Conexión VPN

La segunda opción de conexión entre la entidad financiera y ACH Colombia es a través de VPN. Para
implementar este modelo es necesario.

- La VPN entre la entidad financiera y ACH Colombia obligatoriamente debe ser Site to Site.
- La entidad financiera debe entregar 2 direcciones IP por cada ambiente (pruebas y Producción: 4
    IP’s en total), donde una obligatoriamente debe ser publica (Peer VPN). La otra IP puede ser pública
    o privada; para la IP privada no importa el segmento.
- Los parámetros mínimos para la configuración de la VPN son:

(^) **Propiedades del túnel Parámetros**^ **Mínimos**^ **de**^
**Configuración
Fase**

### 1

```
Método de Autenticación PRE-SHARED KEY
Esquema de Cifrado IKE V2
Grupo Diffie-Hellman [RECOMENDADO GRUPO 14]
Algoritmo de Cifrado [RECOMENDADO AES256]
Algoritmo de Hashing [RECOMENDADO SHA-384]
Modo Main o Agresivo [RECOMENDADO MAIN]
Lifetime [RECOMENDADO 86400 S]
```
```
Fase
```
### 2

```
Encapsulación [RECOMENDADO ESP]
Algoritmo de Cifrado [RECOMENDADO AES256]
Algoritmo de Autenticación [RECOMENDADO SHA-256]
Perfect Forward Secrecy PFS (Opcional)
Lifetime [RECOMENDADO 3600 S]
```
- La Entidad Financiera debe contar con un Sistema Autónomo para la configuración de la VPN.
- Tener una plataforma (preferiblemente firewall) para entablar la VPN contra ACH Colombia. Esta
    plataforma debe soportar configuración de enrutamiento BGP. ACH Colombia, recomienda que la
    plataforma con que se va a entablar la VPN tenga soporte por algún fabricante. Esta configuración
    solo es requerida en el ambiente productivo, en ambiente de pruebas no se requiere esta
    configuración con BGP.

```
Información del Terminador
Terminador VPN de ACH
Colombia Producción
```
```
Terminador VPN del
Cliente en Producción
```
```
Marca del Terminador
Proporcionado por PALO
ALTO Networks
Modelo del Terminador Proporcionado^ por^ PALO^
ALTO Networks
```
**Versión del Software** (^) 11.0.3-h5


### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 123 de 329
```
```
Dirección IP PEER
```
### 3.86.72.72 /

### 54.166.61.32

**SISTEMA AUTONO** (^) 64520 / 64521
**RED BGP** (^) 10.145.26.XX/30

- En caso de que la Entidad Financiera ya tenga una VPN entablada con ACH Colombia (VPN contra
    los equipos en nube ACH), este nuevo servicio se conectará con la VPN existente y se agregará un
    nuevo dominio de cifrado para el consumo del nuevo servicio.
- La Entidad Financiera debe diligenciar el formato VPN de ACH Colombia. Como información
    relevante estarán los datos de IP de Dominio de Cifrado de ACH, IP Peer de ACH (Conexión directa),
    los sistemas autónomos, la red BGP y los parámetros mínimos de configuración VPN. La
    configuración BGP es necesaria para la parte de alta disponibilidad en ambiente de Producción.
- El direccionamiento IP para el servicio es:

```
Integra Transferencias Interbancarias
BANCO: Nombre de Banco IP DOMINIO DE CIFRADO EF.
Segmento de red IP Ejemplo: XX. XX. XX. XX
BANCO >>HACIA >> ACH
ORIGEN AUTORIZADO DESTINO AUTORIZADO
DIRECCION IP NOMBRE DIRECCION IP APLICACIÓN PUERTO
IP Ejemplo:
XX.XX.XX.XX
PROD
```
```
Dirección IP Dominio de Cifrado
Entidad Financiera producción
Integra
```
```
10.145.23.20 DominioColombia^ de Producción^ Cifrado^ ACH
Integra 443
IP Ejemplo:
XX.XX.XX.XX
PRUEBAS
```
```
Dirección IP Dominio de Cifrado
Entidad Financiera pruebas
Integra
```
**10.145.23.130** (^) Dominio de Cifrado ACH
Colombia Pruebas Integra 443
**IP Ejemplo:
XX.XX.XX.XX
PROD**
Dirección IP Dominio de Cifrado
Entidad Financiera producción
Facturación
**10.145.23.21** DominioColombia^ de Producción^ Cifrado^ ACH
Facturación 443
El siguiente es el diagrama de conectividad VPN para Integra Transferencias Interbancarias en ambiente
productivo


### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 124 de 329
```
## 5.4. Normas de Seguridad

Este ítem describe las Normas de Seguridad para tener en cuenta por los participantes del sistema ACH
COLOMBIA para asegurar el buen funcionamiento, la integridad y la confidencialidad de la información.

## 5.4.1. Estación de trabajo, hardware y software

Con el fin de garantizar la seguridad y confidencialidad en el sistema ACH COLOMBIA, el participante debe
cumplir con los requerimientos de Software y Hardware definidos para su Estación de Trabajo en el numeral
2.3 Información a los de este manual. Adicionalmente, el participante debe seguir los lineamientos descritos
a continuación:

```
− La/s Estación/es de trabajo debe/n estar ubicada/s en un área de acceso restringido, dentro del área
de operaciones (encargada del proceso de ACH COLOMBIA). Área de acceso restringido, se refiere a
aquella ubicación física, a la cual únicamente puede ingresar personal autorizado por el participante,
la cual posee algún mecanismo de control de acceso y su respectivo seguimiento, tal como control de
acceso físico, CCTV, bitácoras físicas de registro de acceso, etc.
− Si el participante desea conectar una o más estaciones de trabajo adicionales, el administrador de
usuarios del proceso ACH de el participante, deberá informar mediante solicitud formal (impresa o
electrónica) a la Dirección de Tecnología de ACH COLOMBIA, con el fin de autorizar el ingreso de estas
y asignar los permisos necesarios.
− La Entidad Participante deberá instalar y mantener actualizada en la/s estación/es una solución de
antivirus y Firewall personal.
− La entidad participante deberá contar con soluciones tecnológicas de prevención y detección de
intrusos, sistema antivirus, sistema antispam y sistemas de control de navegación de acuerdo con el
nivel de riesgo al que estén expuestos, para asegurar que no se ejecuten virus o software malicioso en
la/s Estación/es de trabajo.
− Se debe restringe la ejecución de aplicaciones no autorizadas en la estación de la entidad
− Se debe mantener instalado y actualizados los sistemas de Antivirus, los sistemas operativos y
navegadores en las estaciones de trabajo.
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 125 de 329
```
```
− La Entidad Participante deberá mantener actualizada/s la/s estación/es en cuanto a parches al sistema
operativo y software navegador.
− La Entidad Participante debe realizar el mantenimiento preventivo y correctivo que deban recibir la/s
estación/es de trabajo, Software y Hardware instalado.
− No permitir la copia de información en dispositivos microSD, USB, CD-ROM o cualquier otro tipo de
unidad removible.
− Los puertos de transmisión y recepción (Periféricos) deberán estar deshabilitados.
− La Entidad Participante deberá desinstalar los programas de edición de texto.
− Deshabilitar los dispositivos de almacenamiento.
− La conexión de la/s estación/es, se deberá realizar a un segmento de red distinto al segmento LAN y/o
la red de trabajo de los demás usuarios. Si el participante desea conectar la estación de trabajo a su
red interna deberá garantizar y será su responsabilidad, la seguridad e integridad de la información
que allí se maneja.
−.
```
- La Entidad Participante debe aplicar estándares de seguridad que le permitan tener un hardening
    que mitigue posibles brechas de seguridad en la configuración de su infraestructura.
- Las entidades participantes deben asignar cuentas de correo corporativas para los usuarios que
    harán uso del servicio, solo se podrá tener acceso con un único usuario y rol a la nueva plataforma.
- Se recomienda que las entidades participantes dentro de los controles de fuga de información que
    hoy aplican en su entidad aseguren la información de este servicio en sus herramientas de correo
    electrónico y sistemas de información.
− Se recomienda que las Entidades Financieras realicen un nuevo análisis de riesgos enfocado a los
    cambios asociados a la nueva plataforma
− En el sistema operativo y en las aplicaciones, se debe controlar:
    − El ingreso a los recursos del sistema.
    − El ingreso a la estación de Trabajo directamente o a través de conexiones internas de la red de el
       participante. - La modificación de parámetros de seguridad o de funcionamiento del sistema.
    − Instalación de software.
    − Manejo de cuentas y privilegios de usuarios.
    − Protección de archivos y/o carpetas.
    − No instalar editores de texto.
    − Deshabilitar dispositivos de almacenamiento (USB, disquetes, CD-ROM, etc.).
    − Definir y crear un directorio para el envío de archivos ACH COLOMBIA con auditoria habilitada.
    − Definir y crear un directorio para la recepción de archivos desde ACH COLOMBIA con auditoria
       habilitada.
    − Acceso a las aplicaciones.
    − Funciones de acuerdo con el perfil del usuario y sus responsabilidades

## 5.4.2. Red de comunicaciones

Los servicios de comunicación para el participante deben ser coordinados directamente con la Dirección de
Tecnología de ACH COLOMBIA. La Entidad Participante debe tener en cuenta los aspectos mencionados en el
numeral 5.2 Esquema de Comunicaciones, que garantizan la seguridad, monitoreo permanente y
funcionamiento del sistema.


### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 126 de 329
```
Sin embargo, el participante debe adicionalmente seguir los lineamientos descritos a continuación:
− Los equipos de comunicaciones deben estar ubicados en áreas de acceso restringido.
− La Entidad Participante debe disponer de un armario (“rack”) independiente para instalar los equipos
de comunicación suministrados por ACH COLOMBIA y/o el proveedor de comunicaciones.
− Todos los equipos de comunicación disponen de una clave de acceso asignada y administrada por el
proveedor, la cual permite controlar el acceso no autorizado y el cambio en la configuración de estos.
− Las modificaciones, adiciones, y movimientos de los equipos de comunicaciones deben ser reportados
a la Dirección de Tecnología de ACH COLOMBIA para evitar cambios en la configuración o en la calidad
del canal y de los niveles de seguridad.
− Atender las recomendaciones dadas por ACH COLOMBIA para el mejoramiento de la seguridad en el
sistema.
− Contar con procedimientos de contingencias y seguir los planes y pruebas de contingencia y
recuperación diseñados por ACH COLOMBIA.
ACH COLOMBIA ha establecido diversos niveles de seguridad y definirá otros complementarios para garantizar
el cumplimiento de los principios de seguridad de la información (Confidencialidad, Integridad y Autenticidad,
No repudiación y Auditabilidad) y mantener los controles actuales en los que cada Entidad Participante
vinculada solo pueda acceder a su información, y no a información de otras Entidades Participantes o a
información no pertinente de ACH COLOMBIA.

## 5.4.3. Recomendaciones con usuarios originadores y receptores

Se presentan a continuación algunas recomendaciones que las Entidades Participantes deben tener en cuenta
con sus Clientes Originadores y/o Clientes Receptores, las cuales están estrechamente relacionadas a los
procesos de ACH COLOMBIA:

```
− La Entidad Participante debe definir los procedimientos internos para identificar plenamente a un
usuario que solicite transferencias de fondos, una orden de no pago, una cancelación de autorización
u otra novedad al sistema. Adicionalmente, debe definir los controles necesarios para el proceso de
estas (presentación dentro de los tiempos establecidos, autenticidad, procedimiento, justificación y
firmas, entre otros).
− La Entidad Participante también debe controlar las condiciones de almacenamiento de los formatos
de Autorización y la autenticidad de estas; ya que ACH COLOMBIA asume en todo caso que las
Transacciones ordenadas por parte del participante, son correctas y exactamente iguales a las
suministradas a ella por parte de sus clientes.
− La Entidad Participante debe efectuar una auditoría periódica a sus clientes, la cual debe tender a
certificar que el Usuario acepta, entiende y cumple todos los procedimientos, medidas de seguridad y
requerimientos para ordenar a el participante realizar transacciones a través de ACH COLOMBIA.
− La Entidad Participante debe evaluar permanentemente el riesgo del Usuario Originador,
considerando los siguientes puntos: situación financiera del cliente, confidencialidad de información y
control de lavado de activos, entre otros.
− La Entidad Participante debe establecer unos indicadores de eficiencia del Usuario Originador,
teniendo en cuenta el número de reclamos presentados por el Usuario Originador y/o Receptor, el
porcentaje de devoluciones sobre el número total de Transacciones enviadas y el número de
reversiones solicitadas entre otros.
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 127 de 329
```
```
− La Entidad Participante debe controlar y hacer seguimiento a las transacciones que se consideren
inusuales dentro del perfil del cliente, así como a las transacciones rechazadas por el participante
originador por no cumplir con las condiciones establecidas para el proceso de transacciones ACH
COLOMBIA (Ejemplo: límites en el monto).
− Por seguridad el participante debe tomar todas las precauciones necesarias para garantizar la
confidencialidad del material e información que ACH COLOMBIA u otra Entidad Participante le
proporcionen, las cuales en ningún caso, serán menores de aquellas tomadas para mantener sus
propios asuntos y negocios importantes en reserva cuando la naturaleza de éstos así lo exijan,
absteniéndose en lo sucesivo de efectuar para si o para terceros, arreglos, reproducciones,
adaptaciones o cualquier otra clase de mutilación, deformación o modificación del sistema o de los
datos que lleguen a su conocimiento en el desarrollo del servicio ACH COLOMBIA.
```
## 5.4.4. Recomendaciones en el participante

Las Entidades Participantes deben tener en cuenta los siguientes aspectos en lo relacionado a los procesos de
ACH COLOMBIA:
− La Entidad Participante Receptor debe contar con personal idóneo, de confianza y preparado para
asumir la responsabilidad de transferencias de fondos efectivas en todos los niveles y para todos los
perfiles permitidos en el sistema.
− Autorizar y asignar el personal adecuado y capacitado para solicitar procesos especiales, y perfiles en
el sistema Integra ACH.
− La Entidad Participante Receptor debe implementar los procedimientos internos de control que
aseguren el cumplimiento de las normas de ACH COLOMBIA y la ejecución adecuada y segura de los
procesos internos; de igual forma, seguir las recomendaciones que genere ACH COLOMBIA,
específicamente en las siguientes áreas: recepción de información desde los Usuarios,
almacenamiento de la información recibida, conversión de datos, preparación de archivos para envío
a ACH COLOMBIA, transmisión de información, recepción de información desde ACH COLOMBIA y
aplicación a los sistemas internos. Para cada área se debe enfocar el control en los accesos a los sitios
donde se procesan transacciones ACH, en el acceso lógico a archivos, en la elaboración de
procedimientos operativos completos y en el cumplimiento de normas y otros que ACH COLOMBIA
emita de manera formal.
− Como mecanismo de control, el participante debe realizar el Cuadre Operativo, según lo especificado
en el numeral 2.5 Compensación y Liquidación para hacer seguimiento al movimiento de operaciones
generadas y recibidas a través de ACH COLOMBIA.
− La Entidad Participante debe dar las autorizaciones pertinentes para la Administración de la Seguridad
y la Administración de los Procesos Diarios frente a ACH COLOMBIA. Estos funcionarios deben tener
facultades para reportar novedades en el sistema, y tomar decisiones frente al proceso diario y
procesos especiales, respectivamente.
− Segregación de funciones de acuerdo con los perfiles permitidos en el sistema, el usuario con rol
administrador de usuarios no debe tener otros roles asignados
− El administrador de usuarios de la entidad debe asegurar que un usuario solo tenga un rol y un acceso
asignado, en caso de que la entidad no pueda cumplir esta recomendación es importante que esto se
encuentre registrado dentro de sus riesgos. El usuario para ingreso a Integra ACH es personal e
intransferible, la Entidad Participante debe tomar las medidas necesarias para la administración,
cuidado y mantenimiento de estos; La Entidad Participante será responsable por las consecuencias
que pueda acarrear el uso que los usuarios del participante den al acceso al sistema.


### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 128 de 329
```
```
− La Entidad Participante debe garantizar la confidencialidad e integridad de la información enviada y
recibida, implementando sistemas de encriptación de los archivos transmitidos, de acuerdo con los
lineamientos dados por ACH COLOMBIA.
− Se recomienda que la Entidad Participante dentro de los controles de fuga de información que hoy
aplican en su entidad, aseguren la información de este servicio en sus herramientas de correo
electrónico y sistemas de información.
− Se recomienda que la Entidad Participante realice un nuevo análisis de riesgos enfocado a los cambios
asociados a la nueva plataforma.
```
## 5.4.5. Uso de contingencias de comunicaciones

La Entidad Participante debe seleccionar con anterioridad a su utilización alguno de los mecanismos de
contingencia en comunicaciones, igualmente debe tener establecidos los procedimientos internos que
garanticen la autenticidad de la información contenida en los archivos de Transacciones enviados a ACH
COLOMBIA.

Al momento de aplicar la contingencia seleccionada, el Administrador de Procesos Diarios de el participante
ante ACH COLOMBIA, debe solicitar directamente autorización, a la Dirección de Tecnología de ACH
COLOMBIA, para crear la contingencia a través de VPN,

Este procedimiento garantiza seguridad en el manejo de los procesos de contingencia ejecutados por el
participante y ACH COLOMBIA.

## 6. FORMATO NACHA-M

## 6.1. Generalidades del Formato

## 6.1.1. Antecedentes

Como resultado del acuerdo logrado entre las Cámaras de Compensación Automatizadas o sistemas ACH
(Automated Clearing House) que operan en Colombia, ACH COLOMBIA y CENIT, se genera el presente
documento que describe las Especificaciones Técnicas necesarias para efectuar el intercambio electrónico de
transacciones en el formato NACHA1, adaptado al medio colombiano, el cual se denomina NACHA-M.

Las reglas operativas que rigen el contexto en el cual cada ACH utiliza este formato, son establecidas por cada
ACH.

El formato que se describe en este documento se basa en el formato estándar NACHA del libro Operating
NACHA Rules 2000, ajustado a las necesidades y normas colombianas.

Las definiciones o interpretaciones de este documento se deben seguir estrictamente.


### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 129 de 329
```
## 6.1.2. Estructura de los archivos

Toda la información de transferencias interbancarias manejada en el sistema ACH COLOMBIA debe ser en
formato estándar NACHA-M. Un archivo NACHA-M tiene seis (6) diferentes tipos de registros, cada uno con
106 caracteres de longitud sin carácter de fin de línea y las siguientes características:

### ESTRUCTURA DE ARCHIVOS NACHA-M

### TIPO DE REGISTRO DESCRIPCIÓN

Registro de Encabezado de
Archivo
(Registro Tipo 1)

```
Identifica las entidades origen y destino inmediatos de las transacciones
contenidas en el archivo. Incluye además la fecha, la hora y el identificador del
archivo, que determinan al archivo de manera única.
```
Registro de Encabezado de
Lote (Registro Tipo 5)

```
Identifica el Usuario Originador y describe brevemente el contenido del lote.
La información de este registro aplica uniformemente a los registros
detallados incluidos en ese lote. La fecha efectiva para las transacciones en
este lote también está en este registro.
```
Registro Detallado de
Transacciones
(Registro Tipo 6)

```
Contiene la información suficiente para aplicar los débitos o créditos, tal
como: Código de el participante Receptor, Número de la cuenta, Nombre, Tipo
de transacción, Valor, entre otros. La información del Registro de Encabezado
de Lote incorporada con la información de este registro describe
completamente la transacción. Cada registro de detalle debe llevar un
Número de Control o de Secuencia. Ver numeral 19.9 Manejo del Número de
Secuencia en archivos.
```
Registro Adenda
(Registro Tipo 7)

```
Este registro es utilizado para describir con más información un Registro de
Detalle de Transacciones. Sirve para enviar información relacionada con la
transacción. El Registro Adenda debe ser usado cuando se envían
Transacciones Débito y Crédito, Prenotificaciones Débito y Crédito, y
Transacciones de Devolución.
```
Registro de Control de Lote
(Registro Tipo 8)

```
Este registro está compuesto de los contadores y los totales de control de las
transacciones contenidas en un lote.
```
Registro de Control de Archivo
(Registro Tipo 9)

```
Contiene los contadores y totales de control de las transacciones incluidas en
el archivo. Así mismo, contiene el número de lotes y el número de bloques en
un archivo. Se deben utilizar los registros de relleno que sean necesarios para
completar bloques en múltiplos de diez (10) al final del archivo.
```
NACHA: National Automated Clearing House Association

## 6.1.3. Secuencia de los registros

La secuencia de los registros del formato NACHA-M, para Transacciones monetarias Crédito y Débito, de
Prenotificación Débito y Crédito, de Devoluciones y Devoluciones por Operador, se muestra a continuación:


### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 130 de 329
```
Existe un solo Registro de Encabezado y Control de Archivo por cada archivo y un solo Registro de Encabezado
y Control de Lote por cada lote.

```
− Existen tantos Registros de Encabezado y Control de Lote, como lotes existan en el archivo.
− Existen múltiples Registros de Detalle de Transacciones dentro de un lote (no hay límite de
registros dentro de un lote).
− El Registro Adenda asociado a cada Registro de Detalle de Transacciones Monetarias Crédito y
Débito que se origine, es de uso obligatorio.
− Para las transacciones de Devolución, y de Devoluciones por Operador, el Registro Adenda es
obligatorio.
```
## 6.1.4. Tipos de datos del formato NACHA-M Información de carácter Confidencial

Los caracteres usados para la elaboración de archivos en formato NACHA-M están restringidos a 0 - 9, A-Z y
espacios. Los valores EBCDIC entre "00" - "3F" y ASCII "00" - "1F" no son válidos. Los caracteres aceptados en
el formato NACHA-M se muestran en la tabla Caracteres Aceptados por ACH COLOMBIA.

Los campos establecidos en el formato NACHA-M tienen las siguientes características:

```
− Alfanuméricos: Deben ser justificados a la izquierda, y completados con espacios a la derecha.
− Numéricos: Deben ser justificados a la derecha, sin signo y completados con ceros a la izquierda.
```
```
CARACTERES ACEPTADOS POR ACH COLOMBIA
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 131 de 329
```
```
0 H h X x
1 I i Y y
2 J j Z z
3 K k.
4 L l ,
5 M m ;
6 N n :
7 Ñ ñ -
8 O o *
9 P p /
A a Q q &
B b R r #
C c S s $
D d T t %
E e U u =
F f V v
G g W w
```
Todos los campos alfanuméricos del archivo pueden tener los caracteres de la lista a excepción del Registro 6
Campo 5 Número de Cuenta Receptora.

## 6.1.5. Tipos de inclusión de datos

Para cada campo se indica el tipo de inclusión dentro del formato NACHA-M, la cual debe ser seguida
estrictamente, así:

Mandatorio (M):
Indica uso obligatorio. Campo requerido y validado por el sistema ACH COLOMBIA para enrutar y procesar las
transacciones correctamente.

Requerido (R):
Campo requerido y validado por la Entidad Financiera Receptora para procesar y aplicar con éxito la
transacción. El sistema ACH no verifica el contenido del campo, pero verifica que el campo exista en el registro
que se procesa.

Opcional (O):
Indica uso opcional a discreción participante que origina la transacción; puede brindar complemento a la
información de la transacción.

No Disponible (N/D):
Existen campos “Reservados” cuya inclusión es “No Disponible” siempre indica que su uso está supeditado a
lo que establezca el Operador ACH.

## 6.1.6. Descripción de campos del formato NACHA-M


### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 132 de 329
```
El formato NACHA-M que se observa en cada Ficha Técnica, detalla el contenido de los formatos de los registros
y define los valores requeridos y los elementos de datos. Los requerimientos y especificaciones como contenido
y longitud de los elementos se ilustran en estos formatos.

El formato NACHA-M hace referencia a transacciones, causales y códigos que aplican para servicios PPD (Pagos
de Depósito Directo y Pagos Prea cordados), para las transacciones monetarias y de prenotificación débito y
crédito, y para las transacciones de devolución entregadas por una Entidad Participante.

Este formato también hace referencia a las transacciones crédito, que aplican para el servicio CCD
(Concentración de Fondos), para transacciones de Pago PSE, de acuerdo con el Anexo 7: Ficha Técnica NACHA-
M, que se encuentra en el Manual de Operaciones PSE.

Para cada tipo de transacción se detalla la Ficha Técnica apropiada que se enmarca en el Flujo de Transacciones
posibles en el sistema ACH COLOMBIA, según se describe en el numeral 6.2 Flujograma de Transacciones.

## 6.1.7. Validaciones de campos del formato NACHA-M

El sistema utilizado por ACH COLOMBIA realiza validaciones sobre el nombre del archivo enviado, y sobre cada
registro contenido en el archivo en formato estándar NACHA-M, teniendo en cuenta el tipo de inclusión, y el
contenido especificado para cada campo de acuerdo con el tipo de transacción.

Los tipos de errores que se presentan por invalidez en los campos del formato NACHA-M se resumen en los
siguientes:

```
− Campos obligatorios que son suprimidos
− Registros Obligatorios que son suprimidos
− Contenido errado de los campos
− Caracteres inválidos en los campos
− Nombre de archivo errado
```
Dependiendo del tipo de error que se presente durante el envío del archivo, el sistema de intercambio de
información utilizado por ACH COLOMBIA y por las Entidades Participantes produce los siguientes tipos de
respuestas:

## 6.1.7.1. Rechazo Total de Archivo

```
Conocido como error fatal o Rechazo Total de un archivo. Se produce cuando un archivo no pudo ser
procesado por el sistema ACH COLOMBIA, al detectar errores de nombre, de formato o invalidez en
algunos de sus campos o archivos mal aplicados. Tal es el caso de una transacción monetaria que no
indica el valor de la transacción. En este caso ACH COLOMBIA no puede procesar dicha transacción
porque la información no es suficiente o porque los errores son críticos.
```
```
Cuando se presenta un Rechazo Total, ACH COLOMBIA no procesa el archivo, ni siquiera parcialmente
y el participante debe corregir el error y reenviar el archivo completo. ACH COLOMBIA genera
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 133 de 329
```
```
mensajes de error de forma inmediata al envío del archivo y deja registro de los errores detectados
(Ver Anexo17: Avisos y Mensajes de Error).
```
## 6.1.7.2. Devolución por Operador

```
Conocido también como error formal o Devolución por Operador. Se produce cuando una
transacción no puede ser procesada o aceptada por el sistema ACH COLOMBIA, por no cumplir con
ciertas condiciones de formato estándar o contenido establecidas. Únicamente algunas validaciones
producirán la generación de una transacción de Devolución por Operador por parte del sistema ACH
COLOMBIA, con causales específicas (Ver Anexo 3: Causales de Devolución por Operador), para
aquellos errores que no son altamente críticos o riesgosos para el sistema.
```
```
Cuando se presenta una Devolución por Operador, ACH COLOMBIA procesa las transacciones
correctas y genera las Devoluciones por Operador correspondientes, indicando para cada transacción
la descripción del error; el participante puede corregir las transacciones con error y enviarlas
nuevamente en otro Ciclo.
```
```
En el siguiente numeral se amplía la información relativa a las Devoluciones por Operador como
mecanismo de generación y uso del formato NACHA-M.
```
## 6.1.8. Devoluciones por operador

A continuación, se presentan algunas consideraciones para tener en cuenta para el manejo de las
Transacciones de Devolución por Operador:

```
− El sistema Integra ACH está en capacidad de realizar devoluciones parciales de los archivos de
acuerdo con las condiciones de validación resultado del proceso de envío. El sistema extrae del
archivo original las transacciones que no puede procesar, genera automáticamente un archivo
de “Devolución por Operador”; recalcula los totales y reconstruye el archivo original, con las
transacciones que no presentan inconsistencias; elimina también los valores de compensación
de las transacciones extraídas, de las planillas de compensación normales.
```
```
− El sistema despliega Avisos informativos, donde se presenta la causal de devolución por operador
y su descripción, para que sean tenidos en cuenta por la Entidad Financiera Originadora (Ver
Anexo 3: Causales de Devolución por Operador).
```
```
− El(los) archivo(s) de Devoluciones por Operador se pueden acceder utilizando la opción
Instrucciones Recibidas Integra ACH y se identifican siguiendo el mismo estándar descrito en el
numeral 6.1.10.1, pero utilizando la extensión RET.
```
```
− La estructura y ficha técnica detallada de los archivos de Devoluciones por Operador se muestra
en el numeral 6.7 Ficha Técnica Transacción Devolución por Operador.
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 134 de 329
```
## 6.1.9. Manejo del número de secuencia de transacciones

A continuación, se presentan algunos aspectos que se deben tener en cuenta en el manejo del Número de
Secuencia en el formato NACHA-M:

```
− Las Entidades Participantes Originadoras al enviar transacciones deben siempre preparar
archivos de manera que los registros de detalle dentro de los lotes estén en orden ascendente,
no necesariamente consecutivos, de acuerdo con el Número de Secuencia asignado a cada
transacción.
− Este número puede ser reiniciado diariamente o puede ser reiniciado únicamente cuando se
termine la secuencia máxima de 7’000.000 transacciones permitida. En cualquier caso, la Entidad
Financiera Originadora no podrá asignar secuencias repetidas o secuencias no ascendentes en
un mismo día ya que esto será causal de Devolución por parte del Operador ACH.
− Si el participante originador opta por reiniciar el Número de Secuencia únicamente cuando se
termine la secuencia máxima permitida, debe asignar máximo 6'999.999 números de secuencia
a las transacciones que origine.
− Así mismo, puede asignar un rango de secuencias para cada canal de origen específico. Antes de
la asignación de este número es importante que el participante originador verifique los números
previamente asignados (si el máximo - 6'999.999- se aproxima, es conveniente renumerar,
borrando las asignaciones previas repetidas), dejando un margen de transacciones sin
asignación; por ejemplo, asignar máximo 6'500.000 de transacciones. En este caso, el
participante originador debe conservar el Número de Secuencia del último registro de detalle,
para que el siguiente archivo enviado comience con la secuencia siguiente a la del último proceso.
− Ya sea que la reiniciación de la secuencia se haga diariamente o únicamente cuando se agote la
secuencia máxima, es importante que el participante originador determine el mecanismo para
identificar y ligar de manera única las transacciones originadas con las transacciones de
Devolución que reciba, ya que esto le permitirá administrar adecuadamente las transacciones
originadas, clasificar, devolver las respuestas a sus Clientes Originadores y aplicar las operaciones
a los Usuarios Originadores por concepto de Devoluciones.
− Las transacciones de Devolución generadas por las Entidades Participantes Receptoras
mantienen el Número de la Secuencia de la transacción original, y otros campos como la fecha
de transmisión o la Fecha Efectiva o Fecha de Proceso, que permitirán a el participante originador
localizar la transacción original y los datos relativos al Usuario Originador de la transacción.
− La Entidad Financiera podrá seleccionar el mecanismo más adecuado para identificar las
transacciones que procesa.
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 135 de 329
```
## 6.1.9.1. Reserva de Rangos de Secuencia de Transacciones PSE

```
Cada Entidad Participante Autorizadora vinculada al PSE debe reservar un rango de secuencias de
origen para que el PSE pueda generar las transacciones en su nombre y luego efectuar el proceso de
cobro de las transacciones previamente autorizadas mediante el Pago en Línea.
```
```
El rango de secuencia de transacciones es utilizado por el PSE para crear los archivos en formato
NACHA-M en nombre de cada Entidad Financiera Autorizadora. Se debe tener en cuenta lo siguiente:
```
```
− La Entidad Financiera dejará disponible al PSE el rango de secuencia de transacciones
desde el número 7.000.001 hasta el número 9.999.999, que corresponde al campo 11
“Número de Secuencia”, del registro 6 “Detalle de Transacciones” del formato NACHA-
M.
```
```
Este campo está compuesto de quince (15) posiciones así: ocho (8) posiciones para el código de la
Entidad Financiera Originadora y siete (7) posiciones para el número consecutivo.
```
```
− La Entidad Participante debe verificar el mecanismo actual de asignación de secuencias
de las transacciones enviadas a ACH COLOMBIA, que en algunos casos es inicializado
de forma diaria y en otros se acumula indefinidamente.
− La Entidad Participante no puede utilizar en el futuro el rango reservado al PSE.
```
```
Ver el detalle en el Manual de Operaciones PSE y el Anexo 7: Ficha Técnica NACHA-M, que se
encuentra en el manual en referencia.
```
## 6.1.10. Recomendaciones para conformación de archivos NACHA-M

La Entidad Participante Originadora o Receptora debe tener en cuenta los siguientes aspectos al generar o
recibir archivos de transacciones de ACH COLOMBIA en formato NACHA-M:

## 6.1.10.1. Para el Nombre del Archivo

```
− El nombre del archivo debe tener la siguiente nomenclatura: RRRRTTT.ZZZ.1, donde RRRR es el
código de Ruta, TTT es el código de Transito de la Entidad Financiera que genera el archivo, y ZZZ
consecutivo diario empezando por 1 para cada archivo enviado.
```
```
− El sistema ACH verifica que el número consecutivo ZZZ corresponda con el contenido del campo
7 - Identificador del Archivo, del Registro de Encabezado de Archivo, teniendo en cuenta la tabla
de Identificador de Archivo, que se muestra a continuación:
```
### TABLA DE IDENTIFICADOR DE ARCHIVO

### IDENTIFICADOR DEL ARCHIVO

```
(Campo 7)
```
```
Número Consecutivo (ZZZ)
(Label Externo)
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A

```
Agosto de 2024
Página 136 de 329
```
### ‘A’ – ‘Z’ 001 – 026

### ‘0’ – ‘9’ 027 – 036

## 6.1.10.2. Para el Nombre de Archivos PSE

```
Cada Entidad Participante Autorizadora vinculada al PSE debe definir un rango de números de
secuencia de identificación de archivo que será usado por el PSE, para realizar el nombramiento de
los archivos.
```
```
La Entidad Participante Autorizadora debe dejar disponible el rango desde el número 4 hasta el
número 9 en el campo 7 “Identificador del Archivo” del registro 1 “Encabezado de Archivo”, que
adicionalmente debe corresponder con el identificador de secuencia del nombre externo del archivo.
```
```
Ver el detalle en el Manual de Operaciones PSE y el Anexo 7: Ficha Técnica NACHA-M, que se
encuentra en el manual en referencia.
```
## 6.1.10.3. Para el Contenido del Formato

```
La Entidad Participante debe tener en cuenta las siguientes recomendaciones al generar o recibir
archivos en formato NACHA-M:
Archivos:
```
```
− La Entidad Participante puede enviar máximo 36 archivos (incluidos los originados por PSE en
nombre del participante) y/o recibir más de un archivo en el día, tantos como Ciclos de
Operación se ejecuten en el sistema ACH COLOMBIA.
− Todos los registros que se presentan en este formato son consistentes para todos los tipos de
Transacciones: Débito, Crédito, 4 , Devoluciones y Devoluciones por Operador del sistema ACH
COLOMBIA.
− Se deben diligenciar los campos de acuerdo con el tipo de inclusión y el contenido especificado
en el formato NACHA-M.
− Un mismo archivo puede contener diferentes tipos de lotes a la vez, como, por ejemplo:
Prenotificaciones Crédito, Prenotificaciones Débito, Transacciones monetarias Crédito,
Transacciones monetarias Débito, o Devoluciones.
− Se debe respetar la secuencia de los registros establecida.
− Los archivos de Devoluciones por Operador se generan de forma separada e independiente de
los archivos de proceso entregados por ACH COLOMBIA.
```
```
Lotes:
```
```
− Cada lote debe agrupar información relacionada entre sí, de un mismo Usuario Originador
(persona natural o persona jurídica), identificado de acuerdo con las descripciones
presentadas en el Anexo 16, Tabla de Descripciones de Lote.
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 137 de 329
```
```
− Puede existir más de un lote de un mismo Usuario Originador (p.e: lote de nómina, lote de
proveedores, lote de recaudos, etc.; todos los lotes pueden ser originados por el mismo
Usuario Originador).
```
```
Transacciones:
```
```
− Los registros de detalle deben estar relacionados con la Descripción del Lote.
− Dentro de los lotes, los registros están ordenados por el número de secuencia.
− Es recomendable que cada lote se conforme con un solo tipo de transacción: Devoluciones,
Prenotificaciones, Créditos, etc. Por lo tanto, podrá haber tantos lotes dentro de un archivo
como tipos de transacciones se estén enviando.
```
## 6.1.10.4. Para el Contenido del Formato NACHA-M PSE

```
Entidad Participante Autorizadora
```
```
Cada Entidad Financiera Autorizadora vinculada al PSE debe tener en cuenta las siguientes
recomendaciones (Ver Manual de Operaciones PSE y Anexo 7: Ficha Técnica Formato Nacha-M) sobre
el contenido del formato NACHA-M PSE:
```
```
− Generar archivos con número de secuencia menor o igual a 31.
− Generar archivos con número de secuencia de transacciones menor o igual a 7.000.000.
− El archivo será generado por PSE en nombre del Banco autorizador.
− Las Entidades Participantes autorizadoras deben estar vinculadas al PSE de forma obligatoria.
```
```
Entidad Participante Receptor
```
```
Cada Entidad Participante Receptor o Recaudadora de Pagos PSE debe tener en cuenta las siguientes
recomendaciones (Ver Manual de Operaciones PSE y Anexo 7: Ficha Técnica Formato Nacha-M) sobre
el contenido del formato NACHA-M:
− Las Entidades Participantes Receptoras de transacciones Crédito, deben estar vinculadas al
Sistema ACH.
− No se utilizarán transacciones de prenotificación débito ni crédito.
− Los archivos deben ser nombrados según el estándar y se generarán tantos archivos como
procesos de aplicación de fondos existan, utilizando las secuencias de nombre reservadas.
− Habilitar transacción tipo CCD.
− Controlar que no se generen devoluciones por transacciones originadas por PSE.
− Crear PSE como Entidad Participante Originadora.
```
## 6.1.11. Validación de la identificación del cliente receptor

El formato NACHA-M permite solicitar validaciones del campo Identificación del Usuario Receptor de forma
específica para cada transacción, utilizando el campo Datos Discrecionales del Registro de Detalle de
Transacciones. Según el tipo de transacción, la validación de la Identificación del Usuario Receptor se hace
opcional u obligatoria.


### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A

```
Agosto de 2024
Página 138 de 329
```
## 6.1.11.1. Envío de la Identificación del Usuario Receptor

```
En todas las transacciones de prenotificación débito y en todas las transacciones monetarias débito,
así como en los casos crédito que se solicite la validación, la Entidad Financiera Originadora y/o el
Usuario Originador deben diligenciar el campo 7 – Número de Identificación del Usuario Receptor del
Registro de Detalle de Transacciones. La Identificación del Usuario Receptor debe diligenciarse de
forma completa, con los ceros a la izquierda si los tiene, e incluyendo el dígito de chequeo si lo tiene.
La identificación para validar no debe contener caracteres diferentes a números, debe ser alineada a
la izquierda y completada con espacios a la derecha.
```
## 6.1.11.2. Validación en el participante Receptor

```
La validación en el participante receptor consiste en confrontar el contenido del campo 7 – Número
de identificación del Usuario receptor del registro de detalle de transacciones contra la información
registrada en sus bases de datos para el número de cuenta o depósito electrónico especificado en la
transacción, cada Entidad Participante Receptor deberá desarrollar su propio control que le permita
comparar la información recibida con o sin dígito de chequeo dado que dentro del registro no existe
un campo adicional que informe si tal dígito está o no presente. Si existe más de una identificación
asociada a esa cuenta, el participante receptor deberá validar contra todas las identificaciones
asociadas con los controles que establezca para tal efecto.
```
```
Si el número de identificación del Usuario Receptor coincide con la información registrada en sus
bases de datos, el participante Receptor deberá aplicar la transacción de prenotificación, o la
transacción monetaria según sea el caso.
```
```
En caso de que la información no coincida, la Entidad Participante Receptor debe generar la
transacción de devolución correspondiente usando la causal R17 (la Identificación no coincide con
Cuenta del Usuario Receptor) según el Anexo 9: Causales de Devolución, y de acuerdo con los
lineamientos operativos y técnicos dados para generar transacciones de devolución. Primero debe
verificarse la validez de la cuenta y posteriormente la validez de la identificación del Usuario Receptor.
```
```
En ningún otro tipo de transacción (Devolución, Devolución por Operador ACH) se debe solicitar o
realizar la validación de la Identificación del Usuario Receptor.
```
## 6.1.11.3. Validación en Transacciones Crédito

```
Es opcional para la Entidad Financiera Originadora y/o el Usuario Originador solicitar la validación de
la identificación del Usuario Receptor para las transacciones de prenotificación crédito y/o para las
transacciones monetarias crédito.
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 139 de 329
```
```
Si requiere que el participante Receptor realice la validación, debe colocar el símbolo “V” o “v” solo
o acompañado de cualquier letra, alineado a la izquierda en el campo 9 – Datos Discrecionales del
Registro de Detalle de Transacciones cuando origine la transacción.
```
```
Este símbolo indica a el participante Receptor que debe efectuar la validación de la identificación del
Usuario Receptor en la transacción que se envía.
```
```
La Entidad Participante Receptor debe verificar si el campo 9 - Datos Discrecionales del Registro de
Detalle de Transacciones contiene el símbolo “V” o “v” alineado a la izquierda, o acompañado de
cualquier letra. Si es así, la Entidad Participante Receptor debe efectuar la validación en su sistema
interno. Si el campo 9 – Datos Discrecionales del Registro de Detalle de Transacciones contiene un
símbolo diferente a “V” o “v”, como por ejemplo espacios o cualquier otro, la Entidad Participante
Receptor NO está en obligación de efectuar validación alguna, pero sí de aplicar la transacción de
forma normal.
```
## 6.1.11.4. Validación en Transacciones Débito

```
En el caso de las transacciones de prenotificación débito o de transacciones monetarias débito, no
se exige ningún símbolo especial para el campo 9 – Datos Discrecionales del Registro de Detalle de
Transacciones.
```
```
La Entidad Participante Originadora y/o el Usuario Originador podrán diligenciar este campo a su total
discreción. Sin embargo, se debe enviar la identificación completa y correcta, ya que la Entidad
Participante Receptor debe validar SIEMPRE la identificación del Usuario Receptor contenida en la
transacción de prenotificación débito y en la transacción monetaria débito, y generar la devolución
correspondiente si la identificación no coincide.
```
```
La Entidad Receptora NO debe verificar el contenido del campo 9 – Datos Discrecionales del Registro
de Detalle de Transacciones, sino realizar directamente y para todas las transacciones débito, la
validación en su sistema interno.
```
## 6.1.12. Cálculo del dígito de chequeo

El dígito de chequeo que corresponde al Código de Ruta y Tránsito de una Entidad Participante se calcula
usando módulo 10, de acuerdo con lo siguiente:

1. Multiplique cada dígito en el número de ruta y tránsito por un factor de peso. Los blancos deberán ser
    convertidos a ceros. Los factores de peso por cada dígito son:

### 0 R R R R T T T

```
Posición: 1 2 3 4 5 6 7 8
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A

```
Agosto de 2024
Página 140 de 329
```
```
Pesos: 3 7 1 3 7 1 3 7
```
2. Sume los resultados de los ocho cálculos.
3. Substraiga este resultado del próximo número más alto múltiplo de 10. El resultado es el dígito de
    chequeo.

```
0 R T T
Ejemplo: No. de Ruta: 0 7 6 4 0 1 2 5
Multiplique^ por:^3 7 1 3 7 1 3 7
Suma: 0 49 6 12 0 1 6 35 = 109
```
```
Dígito de chequeo = 1 (110 - 109)
```

### MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDADES PARTICIPANTES

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 141 de 329
```
## 6.2. Flujograma de Transacciones

(^)

### RESUMEN FLUJOGRAMA DE TRANSACCIONES ACH

### TRANSACCIONES

### POSIBLES

### GENERADA POR... CAPITULO COMENTARIOS

### TRANSACCIÓN

### MONETARIA CRÉDITO

```
Entidad
Participante
Originadora/PSE
```
### 2

### 3

- Usada para abonar otras cuentas o depósito electrónico.
- Validación de la Identificación del Usuario Receptor opcional.


### MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDADES PARTICIPANTES

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 142 de 329
```
### RESUMEN FLUJOGRAMA DE TRANSACCIONES ACH

### TRANSACCIONES

### POSIBLES

### GENERADA POR... CAPITULO COMENTARIOS

### TRANSACCIÓN DE

### PRENOTIFICACIÓN

### CRÉDITO

```
Entidad
Participante
Originadora
```
### 2

### 3

- Usada para verificar el estado de las cuentas o depósito electrónico a abonar
- Validación de la Identificación del Usuario Receptor opcional

### TRANSACCIÓN

### MONETARIA DÉBITO

```
Entidad
Participante
Originadora
```
### 2

### 4

- Usada para recaudar de otras cuentas o depósito electrónico que se hayan
    vinculado usando prenotificaciones.
- Información Adicional obligatoria estandarizada con el Código Único de
    Referencia.
- Validación de la Identificación del Usuario Receptor obligatoria para la Entidad
    Participante Receptor.

TRANSACCIÓN DE
PRENOTIFICACIÓN
DÉBITO

```
Entidad
Participante
Originadora
```
### 2

### 4

- Usada para verificar el estado de las cuentas desde las cuales se va a recaudar
- Información Adicional obligatoria estandarizada con el Código Único de
    Referencia
- Validación de la Identificación del Cliente Receptor obligatoria para la Entidad
    Participante Receptor.

### TRANSACCIÓN DE

### DEVOLUCIÓN

```
Entidad
Participante
Receptor
```
### 2

### 5

- Usada para notificar la no aplicación exitosa de una transacción por una razón
    específica
- Aplica para devoluciones de transacciones crédito, de prenotificaciones
    crédito, de transacciones débito y de prenotificaciones débito
- Requiere el uso de Información Adicional para indicar la razón de la devolución
TRANSACCIÓN DE
DEVOLUCIÓN POR
OPERADOR

### ACH COLOMBIA

### 2

### 6

- Usada para notificar la no aceptación de ACH COLOMBIA de una transacción
- Requiere el uso de Información Adicional para indicar la razón de la devolución
- Se generan en archivos independientes

### ARCHIVOS ACH

### ACH COLOMBIA

### 2

### 2

- Usado para que la Entidad Participante Receptor realice la aplicación de la
    totalidad de transacciones al sistema interno del participante.
- Aplica para transacciones crédito y débito, para transacciones de
    prenotificación, y para todo tipo de devoluciones excepto las generadas por
    ACH COLOMBIA.


### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 143 de 329
```
## 6.3. Ficha Técnica Archivos ACH

## 6.3.1. Consideraciones generales

El sistema ACH COLOMBIA realiza el procesamiento y clasificación de movimiento y genera al cierre de cada
ciclo un archivo que contiene las transacciones para ser aplicadas en el sistema interno de la Entidad Financiera,
según el movimiento presentado: prenotificaciones crédito y débito, transacciones monetarias crédito y
débito, devoluciones a prenotificaciones o a transacciones monetarias. Los archivos de transacciones y
movimiento para una Entidad Participante generados por el Operador ACH COLOMBIA tienen en cuenta los
siguientes aspectos:

```
− Un archivo generado por el sistema ACH COLOMBIA puede contener todos los tipos de
transacciones, excepto las Devoluciones por Operador que son organizadas en un archivo
independiente.
− Las Transacciones contenidas en el archivo son generadas por una Entidad Participante hacia otra
Entidad Participante vinculada a ACH COLOMBIA. ACH COLOMBIA únicamente distribuye las
transacciones a quien corresponda, según el código de el participante Receptor de la transacción
enviada por una Entidad Participante.
− Para el archivo de transacciones generado como resultado del procesamiento en ACH
COLOMBIA, se usa la secuencia de registros del formato NACHA-M, así: Registro de Encabezado
de Archivo, Registro de Encabezado de Lote, Registro de Detalle de Transacciones, Registro
Adenda (si es usado), Registro de Control de Lote y Registro de Control de Archivo.
− El archivo de transacciones generado por parte del Operador ACH es organizado de igual manera
como son recibidas las transacciones de las diferentes entidades de tal forma que se conforma
un lote por cada Entidad Financiera Originadora independientemente del tipo(s) de
transacciones que haya enviado la misma. Se conserva el formato del lote, del detalle y de adenda
según el tipo de transacción que fue originada. Se ordena por Entidad Participante Originadora,
Cliente Originador y secuencia.
− Cada transacción contenida en el archivo de salida de la Entidad Financiera mantiene el número
de secuencia de la transacción originalmente enviada.
− El Registro Adenda es entregado o copiado a el participante que corresponda, de forma
completamente exacta a la transacción enviada por el participante.
```
## 6.4. Ficha Técnica Transacción Crédito

## 6.4.1. Consideraciones generales

Las Transacciones Crédito son generadas por el participante originador hacia ACH COLOMBIA para abonar
cuentas de Usuario de otras Entidades Participantes Receptoras vinculadas a ACH COLOMBIA.

A continuación, se presentan algunas consideraciones de la Ficha Técnica de la Transacción Crédito.

```
− Este ítem describe la Transacción de Prenotificación Crédito, la cual es opcional en el proceso;
describe también la Transacción monetaria Crédito.
```

### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A

```
Agosto de 2024
Página 144 de 329
```
```
− Tanto para la Transacción de Prenotificación Crédito como para la Transacción monetaria Crédito
se debe usar la secuencia de registros del formato NACHA-M, así: Registro de Encabezado de
Archivo, Registro de Encabezado de Lote, Registro de Detalle de Transacciones, Registro Adenda,
Registro de Control de Lote y Registro de Control de Archivo.
```
```
− Se recomienda que la Entidad Financiera Originadora agrupe la información de los pagos por
Cliente Originador y por tipo de información que contenga cada lote.
```
```
− A cada transacción que se origine de le debe asignar un número de secuencia.
```
```
− Si se requiere que el participante Receptor valide la Identificación del Usuario Receptor en las
transacciones tipo crédito, se deben seguir los lineamientos del numeral 2.10 y en el capítulo 6
FORMATO NACHA-M.
```
```
− El Registro Adenda es de uso obligatorio para las transacciones tipo crédito y se debe usar para
la transacción monetaria y para la transacción de prenotificación.
```
```
− La información relacionada con el pago debe ser lo más clara y completa posible.
```

```
MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 145 de 329
```
## 6.4.2. Requerimientos de formato

```
Registro de Encabezado de Archivo
Para transacciones de: Prenotificación Crédito y Monetarias Crédito
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción
1 TIPO DE REGISTRO M “1” 1 1 Valor válido para este campo "1".
2 CODIGO DE PRIORIDAD R N 2 2 - 3 Valor válido “01”.
```
```
3 CODIGO ENTIDAD DESTINO INMEDIATO M
bRRRRRTT
TC
```
### 10 4 - 13

```
Código de ACH COLOMBIA ( 00 0101006) y dígito de
chequeo.
```
```
4 CÓDIGO ENTIDAD ORIGEN INMEDIATO M bRRRRRTT
TC
10 14 - 23 Código^ de^ el^ participante^ originador^ que^ envía^ el^
archivo y dígito de chequeo.
```
```
5 FECHA DE CREACION DEL ARCHIVO M
```
### AAAAMM

```
DD 8 24 -^31 Fecha^ de^ creación^ del^ archivo.^
6 HORA DE CREACION DEL ARCHIVO O HHMM 4 32 - 35 Hora en la cual es transmitido o creado el archivo.
```
```
7 IDENTIFICADOR DEL ARCHIVO M A-Z / 0 - 9 1 36 - 36
Identificación de archivos creados en la misma
fecha.
```
```
8 TAMAÑO DEL REGISTRO M ‘106’ 3 37 - 39
Número de caracteres contenidos en cada
registro.
9 FACTOR DE ABLOCAMIENTO M ‘10’ 2 40 - 41 Número de registros dentro de un bloque.
10 CODIGO DE FORMATO M ‘1’ 1 42 - 42 Permite futuras variaciones de formato.
11 NOMBRE ENTIDAD DESTINO INMEDIATO O AN 23 43 - 65 Nombre del ACH (ACH COLOMBIA).
12 NOMBRE ENTIDAD ORIGEN INMEDIATO O AN 23 66 - 88 Nombre del participante originador.
13 CODIGO DE REFERENCIA O AN 8 89 - 96 Código del sistema.
14 RESERVADO N/D Blancos 10 97 - 106 Campo reservado. Este campo debe ir en blancos.
```

```
MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 146 de 329
```
```
Registro de Control de Archivo
Para transacciones de: Prenotificación Crédito y Monetarias Crédito
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción
```
1 TIPO DE REGISTRO M “9” 1 1 Valor válido para este campo "9".

2 CANTIDAD DE LOTES M N 6 2 - 7 Número de lotes incluidos en el archivo.

3 NUMERO DE BLOQUES M N 6 8 - 13
Número de bloques físicos en el archivo de 10
registros cada uno.

4 NUMERO^ DE^ TRANSACCIONES^
DETALLADAS Y DE REGISTROS ADENDA
M N 8 14 - 21 Número^ total^ de^ registros^ de^ detalle^ y^ de^ adenda^
en el archivo.

### 5 TOTALES DE CONTROL M N

### 1

### 0

### 22 - 31

```
Sumatoria de los códigos de las Entidades
Participantes Receptoras de los
Registros de Detalle de Transacciones
```
### 6 VALOR TOTAL DE DEBITOS M

### $$$$$$$$

### $$$$$$$$

### $$

### 1

### 8 32 -^49

```
Suma de valores de las transacciones tipo débito
del archivo.
```
### 7 VALOR TOTAL DE CREDITOS M

### $$$$$$$$

### $$$$$$$$

### $$

### 1

### 8

```
50 - 67 Suma^ de^ valores^ de^ las^ transacciones^ tipo^ crédito^
del archivo.
```
8 RESERVADO N/D Blancos

### 3

### 9

```
68 - 106 Campo reservado no disponible.
```

```
MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 147 de 329
```
```
Registro de Encabezado de Lote
```
Para transacciones de: Prenotificación Crédito y Monetarias Crédito

```
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción
```
1 TIPO DE REGISTRO M “5” 1 1 Valor válido para este campo "5”.

2 CODIGO^ CLASE^ DE^ TRANSACCIONES^ POR^
LOTE
M N 3 2 - 4 200 - Débitos y Créditos, 220 - Créditos

### 3 NOMBRE DEL USUARIO ORIGINADOR M AN 16 5 - 20

```
Nombre del Usuario Originador para propósitos
descriptivos.
```
### 4 DATOS^ DISCRECIONALES^ DEL^ USUARIO^

### ORIGINADOR

### O AN 20 21 - 40

```
Datos del Usuario Originador y/o de la Entidad
Financiera
Originadora.
```
5 IDENTIFICACION^ DEL^ USUARIO^
ORIGINADOR
M AN 10 41 - 50 Número de identificación del Usuario Originador.

6 TIPO DE SERVICIO M AN 3 51 - 53 PPD^ (Pagos^ de^ Depósito^ Directo^ y^ Pagos^ Prea
cordados)

### 7 DESCRIPCION DE LOTE

### M

### AN

### 10

### 54 - 63

```
Descripción del lote según anexo 16. Como
mínimo se deben usar las siguientes palabras:
NOMINA, PROVEEDOR o TRASLADOS,
dependiendo del concepto del pago que genere la
EPO. Para los pagos realizados por personas
naturales aplica siempre la descripción
TRASLADOS.
```
8 FECHA DESCRIPTIVA M AN 8 64 - 71
Fecha informativa asignada por el Usuario
Originador.

9 FECHA EFECTIVA DE LA TRANSACCION R

### AAAAMM

### DD

### 8 72 - 79

```
Fecha en la cual las Entidad Financiera Receptora
deben aplicar las transacciones del lote.
```
10 FECHA DE COMPENSACIÓN JULIANA O N 3 80 - 82 Fecha de liquidación de las transacciones.


```
MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 148 de 329
```
```
Registro de Encabezado de Lote
```
Para transacciones de: Prenotificación Crédito y Monetarias Crédito

```
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción
```
11

### CODIGO ESTADO DEL USUARIO

### ORIGINADOR

### M AN 1 83 - 83

```
Valor válido “1” e indica el estado del Usuario
Originador.
```
12 CODIGO^ ENTIDAD^ PARTICIPANTE^
ORIGINADORA
M RRRRRTTT 8 84 - 91 Código de el participante originador.

13 NUMERO DE LOTE M N 7 92 - 98 Secuencial^ ascendente^ único^ para^ cada^ lote^ en^ del^
archivo iniciando en 1.

14 RESERVADO N/D Blancos 8 99 - 106 Campo reservado.


```
MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 149 de 329
```
```
Registro de Control de Lote
```
Para transacciones de: Prenotificación Crédito y Monetarias Crédito

```
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción
```
1 TIPO DE REGISTRO M “8” 1 1 Valor válido para este campo "8".

2 CODIGO^ CLASE^ DE^ TRANSACCIONES^ POR^
LOTE
M N 3 2 - 4 200 - Débitos y Créditos, 220 - Créditos

### 3

### NUMERO DE TRANSACCIONES

### DETALLADAS Y DE REGISTROS

### ADENDA

```
M N 6 5 - 10 Número^ de^ registros^ de^ detalle^ y^ de^ adenda^ en^ el^
lote.
```
### 4 TOTALES DE CONTROL M N

### 1

### 0

### 11 - 20

```
Sumatoria de códigos de las Entidades Participantes
Receptora de los Registros de Detalle de
Transacciones.
```
### 5 VALOR TOTAL DE DEBITOS M

### $$$$$$$$

### $$$$$$$$

### $$

### 1

### 8

```
21 - 38 Suma^ de^ valores^ de^ las^ transacciones^ débito^ del^
lote.
```
### 6 VALOR TOTAL DE CREDITOS M

### $$$$$$$$

### $$$$$$$$

### $$

### 1

### 8

### 39 - 56

```
Suma de valores de las transacciones crédito del
lote.
```
### 7

### IDENTIFICACION DEL USUARIO

### ORIGINADOR

### R AN

### 1

### 0

```
57 - 66 Número de identificación del Usuario Originador.
```
### 8 CODIGO DE AUTENTICACION DE MENSAJES O AN

### 1

### 9

```
67 - 85 Campo reservado para un algoritmo de seguridad.
```
9 RESERVADO N/D Blancos 6 86 - 91 Campo reservado no disponible.

10 IDENTIFICACION^ DE^ LA^ ENTIDAD^
PARTICIPANTE ORIGINADORA
M RRRRRTTT 8 92 - 99 Código^ de^ la^ Entidad^ Financiera^ Originadora^ que^
inicia la transacción.

11 NUMERO DEL LOTE M N 7 100 - 106 Número del Lote.


```
MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 150 de 329
```
```
Registro de Detalle de Transacciones
```
Para transacciones de: Prenotificación Crédito y Monetarias Crédito

```
# Nombre de Campo Inclusión Contenid
o
Longitud Posició
n
Descripción
```
1 TIPO DE REGISTRO M “6” 1 1 Valor válido para este campo "6".

### 2

### CODIGO DE TRANSACCION M^

### N 2

### 2

### -

### 3

```
Transacción/Ti
po CTA
```
```
Cuenta
Corriente
```
```
Cuenta de
Ahorros
Depósitos
electrónicos
```
```
Prenotificación
Crédito
```
### 23 33 53

```
Transacción
Crédito
```
### 22 32 52

### 3

### CODIGO ENTIDAD PARTICIPANTE

### RECEPTOR M^

### RRRRRTT

### T 8 4 -^11

```
Número de Ruta y Tránsito de la Entidad Participante
Receptor.
```
4 DIGITO DE CHEQUEO M N 1 12 - 12 Dígito de chequeo correspondiente al campo 3.

5

### NUMERO DE CUENTA DEL USUARIO

### RECEPTOR

### R AN 17 13 - 29

```
Número de cuenta del Usuario Receptor en la Entidad
Participante Receptor. Solo caracteres numéricos.
```
### 6

### VALOR DE LA TRANSACCIÓN

### M

### $$$$$$$

### $$$$$$$

### $$$$

### 18 30 - 47

```
Tipo de
Transacción
Valor
```
```
Prenotificación
Crédito
Cero ($0 pesos)
```
```
Transacción
Monetaria Crédito Valor^ por pagar^ o^ a^ abonar^
```
### 7 NUMERO^ DE^ IDENTIFICACION^ DEL^

### USUARIO RECEPTOR O

(^1) AN 15 48 - 62
Campo utilizado por el Usuario Originador para
identificar al
Usuario Receptor.
8 NOMBRE DEL USUARIO RECEPTOR R AN 22 63 - 84 Registra el nombre del Usuario Receptor.


```
MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 151 de 329
```
```
Registro de Detalle de Transacciones
Para transacciones de: Prenotificación Crédito y Monetarias Crédito
```
```
# Nombre de Campo Inclusión Contenid
o
Longitud Posició
n
Descripción
```
### 9 DATOS DISCRECIONALES O AN 2 85 - 86

```
Si se requiere que el participante Receptor valide la
identificación del Usuario Receptor, este campo debe
contener “V” o “v” en la primera posición en la
prenotificación crédito o en la transacción monetaria
crédito.
```
```
10 INDICADOR DE REGISTRO ADENDA M N 1 87 - 87
Valor “ 1 ” para anexar información adicional
relacionada con el pago.
```
### 11 NUMERO DE SECUENCIA M N 15 88 - 102

```
En las primeras 8 posiciones se debe registrar el
Código de el participante originador y en las
siguientes 7 posiciones, un consecutivo.
```
```
12 RESERVADO N/D Blancos 4
```
### 103 -

```
106 Campo^ reservado.^
```
(^1) En aquellas transacciones de prenotificación crédito y en las transacciones monetarias crédito, en las que el Usuario Originador requiera
validar la identificación del Usuario Receptor, este campo deberá contener la identificación del Usuario Receptor.
Registro Adenda – Información Adicional
Para transacciones Monetarias Crédito
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción
1 TIPO DE REGISTRO M “7” 1 1 Valor válido para este campo "7".
2

### CODIGO TIPO DE REGISTRO

### ADENDA

```
M “05” 2 2 - 3 Valor válido para este campo “05”.
```
### 3

### IDENTIFICACIÓN USUARIO

### ORIGINADOR

### R N 15 4 - 18

```
Cédula o NIT del cliente originador que realiza el pago o traslado
de fondos. Este campo no debe estar vacío o contener ceros
```

```
MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 152 de 329
```
```
Registro Adenda – Información Adicional
Para transacciones Monetarias Crédito
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción
```
4 RESERVADO N/D Blancos 2 19 - 20 Campo reservado. Este campo debe ir en blancos.

5

### PROPOSITO DE LA

### TRANSACCIÓN

### R AN 10 21 - 30

```
Debe contener la información del campo 7 - Descripción de Lote
del registro tipo 5
```
### 6

### NUMERO DE FACTURA O

### CUENTA R^ AN^24 31 -^54

```
Número de la factura, cuenta de cobro, recibo de pago, referencia
de pago electrónico, código numérico o alfanumérico que
identifica al cliente de manera única ante el receptor u otro que
identifique el pago que el originador está realizando. Si no existe
número referencia este campo debe contener ceros.
```
7 RESERVADO N/D Blancos 2 55 - 56 Campo reservado. Este campo debe ir en blancos.

### 8

### INFORMACION LIBRE DEL

### ORIGINADOR

### R AN 24 57 - 80

Campo diligenciado libremente por el originador para referenciar
su pago. Si no existe información libre este campo debe contener
ceros.
9 RESERVADO N/D Blancos 3 81 - 83 Campo reservado. Este campo debe ir en blancos.

10

### NUMERO DE SECUENCIA DE

```
REGISTRO DE ADENDA M^ N^4 84 -^87 Valor^ válido^ para^ este^ campo^ “0001”^
```
11 NUMERO DE SECUENCIA DE

```
M N 7 88 - 94
```
```
Su valor debe coincidir con las siete últimas posiciones del campo
TRANSACCION DEL REGISTRO DE
DETALLE DE TRANSACCIONES
11, registro tipo “6”, al cual hace referencia.
```
12 RESERVADO N/D Blancos 12 95 - 106 Campo reservado. Este campo debe ir en blancos.

```
Registro Adenda – Información Adicional
Para transacciones de Prenotificación Crédito o monetaria Crédito
```

```
MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 153 de 329
```
```
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción
```
1 TIPO DE REGISTRO M “7” 1 1 Valor válido para este campo "7".

2 CODIGO TIPO DE REGISTRO ADENDA M “05” 2 2 - 3 Valor válido para este campo “05”.

3 IDENTIFICACIÓN USUARIO ORIGINADOR R AN 15 4 - 18
Debe tener el número de cedula para personas
naturales y el NIT para personas jurídicas.

4 RESERVADO N/D Blancos 2 19 - 20 Campo Reservado.

5 PROPOSITO DE LA TRANSACCIÓN R AN 10 21 - 30
Debe contener la descripción de la transacción
que se encuentra en el registro 5

### 6 REFERENCIA DEL PAGO R AN 53 31 - 83

```
Campo destinado para que el Usuario originador
describa el concepto de la transferencia que está
realizando.
```
7 NUMERO^ DE^ SECUENCIA^ DE^ REGISTRO^ DE^
ADENDA
M N 4 84 - 87 Valor válido para este campo “0001”

### 8

### NUMERO DE SECUENCIA DE

### TRANSACCION DEL REGISTRO DE

### DETALLE DE TRANSACCIONES

### M N 7 88 - 94

```
Su valor debe coincidir con las siete últimas
posiciones del campo
11, registro tipo “6”, al cual hace referencia.
```
9 RESERVADO N/D Blancos 12 95 - 106 Campo reservado. Este campo debe ir en blancos.
*El Registro Adenda es de uso obligatorio para las transacciones tipo crédito y se debe usar para la transacción monetaria y para la
transacción de prenotificación.


```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 154 de 329
```
## 6.5. Ficha Técnica Transacción Débito

## 6.5.1. Consideraciones generales

Las Transacciones Débito son generadas por el participante originador hacia ACH COLOMBIA para recaudar
fondos de cuentas de clientes de otras Entidades Participantes Receptoras vinculadas a ACH COLOMBIA.

A continuación, se presentan algunas consideraciones de la Ficha Técnica de la Transacción Débito.

```
− Este numeral describe la Transacción de Prenotificación Débito, la cual es obligatoria en el
proceso débito, cuando la vinculación del Cliente Receptor se realiza a través del Cliente
Originador; este numeral describe también la Transacción monetaria Débito.
− Tanto para la Transacción de Prenotificación Débito como para la Transacción monetaria Débito
se debe usar la secuencia de registros del formato NACHA-M, así: Registro de Encabezado de
Archivo, Registro de Encabezado de Lote, Registro de Detalle de Transacciones, Registro Adenda,
Registro de Control de Lote y Registro de Control de Archivo.
− Se recomienda que la Entidad Financiera Originadora agrupe la información de los pagos por
Cliente Originador y por tipo de información que contenga cada lote.
− A cada transacción que se origine de le debe asignar un número de secuencia nuevo.
− Se deben seguir los lineamientos del numeral 2.11 Transacción Débito y en el capítulo 6
FORMATO NACHA-M de este manual en lo referente a la validación del Cliente Receptor, la cual
se hace obligatoria para este tipo de transacción.
− El Registro Adenda es de uso obligatorio para las transacciones tipo débito y se debe usar para
la transacción monetaria y para la transacción de prenotificación.
− Se recomienda que como mínimo los campos del Registro Adenda como son el Código Cliente
Originador por Servicio, y la Referencia 1 sean parte de la base de datos de el participante
Receptor, si ésta la maneja.
```

### MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 155 de 329
```
## 6.5.2. Requerimientos de formato

```
Registro de Encabezado de Archivo
Para transacciones de: Prenotificación Débito y Monetarias Débito
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción
1 TIPO DE REGISTRO M “1” 1 1 Valor válido para este campo "1".
2 CODIGO DE PRIORIDAD R N 2 2 - 3 Valor válido “01”.
```
```
3 CODIGO ENTIDAD DESTINO INMEDIATO M
bRRRRRTT
TC
```
### 10 4 - 13

```
Código de ACH COLOMBIA ( 000 101006) y dígito de
chequeo
```
```
4 CODIGO ENTIDAD ORIGEN INMEDIATO M bRRRRRTT
TC
10 14 - 23 Código^ de^ el^ participante^ originador^ que^ envía^ el^
archivo y dígito de chequeo.
```
```
5 FECHA DE CREACION DEL ARCHIVO M AAAAMM
DD
8 24 - 31 Fecha de creación del archivo.
```
```
6 HORA DE CREACION DEL ARCHIVO O HHMM 4 32 - 35 Hora en la cual es transmitido o creado el archivo.
```
```
7 IDENTIFICADOR DEL ARCHIVO M A-Z / 0 - 9 1 36 - 36
Identificación de archivos creados en la misma
fecha.
8 TAMAÑO DEL REGISTRO M ‘106’ 3 37 - 39 Número de caracteres contenidos en cada registro.
9 FACTOR DE ABLOCAMIENTO M ‘10’ 2 40 - 41 Número de registros dentro de un bloque.
10 CODIGO DE FORMATO M ‘1’ 1 42 - 42 Permite futuras variaciones de formato.
11 NOMBRE ENTIDAD DESTINO INMEDIATO O AN 23 43 - 65 Nombre del ACH (ACH COLOMBIA).
```
### 12 NOMBRE ENTIDAD ORIGEN INMEDIATO O AN 23 66 - 88

```
Nombre del participante originador.
```

### MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 156 de 329
```
```
Registro de Encabezado de Archivo
Para transacciones de: Prenotificación Débito y Monetarias Débito
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción
```
### 13 CODIGO DE REFERENCIA O AN 8 89 - 96

```
Código del sistema.
```
```
14 RESERVADO N/D Blancos 10 97 - 106
Campo reservado. Este campo debe ir en blancos.
```
Registro de Control de Archivo

```
Para transacciones de: Prenotificación Débito y Monetarias Débito
```
```
# Nombre de Campo Inclusió
n
Contenido Longitud Posición Descripción
```
```
1 TIPO DE REGISTRO M “9” 1 1 Valor válido para este campo "9".
2 CANTIDAD DE LOTES M N 6 2 - 7 Número de lotes incluidos en el archivo.
```
```
3 NUMERO DE BLOQUES M N 6 8 - 13
Número de bloques físicos en el archivo de 10 registros
cada uno.
```
### 4

### NUMERO DE TRANSACCIONES

### DETALLADAS Y DE REGISTROS

### ADENDA

### M N 8 14 - 21

```
Número total de registros de detalle y de adenda en el
archivo.
```
### 5 TOTALES DE CONTROL M N 10 22 - 31

```
Sumatoria de los códigos de las Entidades Participantes
Receptoras de los
Registros de Detalle de Transacciones.
```

### MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 157 de 329
```
```
Registro de Encabezado de Archivo
Para transacciones de: Prenotificación Débito y Monetarias Débito
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción
```
### 6 VALOR TOTAL DE DEBITOS M

### $$$$$$$$$

### $$$$$$$$$

### 18 32 - 49

```
Suma de valores de las transacciones tipo débito del
archivo.
```
### 7 VALOR TOTAL DE CREDITOS M $$$$$$$$$

### $$$$$$$$$

### 18 50 - 67

```
Suma de valores de las transacciones tipo crédito del
archivo.
```
```
8 RESERVADO N/D Blancos 39 68 - 106 Campo reservado no disponible.
```
Registro de Encabezado de Lote

```
Para transacciones de: Prenotificación Débito y Monetarias Débito
Nombre de Ca# mpo Inclusión Contenido Longitud Posición Descripción
1 TIPO DE REGISTRO M “5” 1 1 Valor válido para este campo "5”.
2 CODIGO CLASE DE TRANSACCIONES POR LOTE M N 3 2 - 4 200 - Débitos y Créditos, 225 - Débitos
```
```
3 NOMBRE DEL CLIENTE ORIGINADOR M AN 16 5 - 20
Nombre del Cliente Originador para propósitos
descriptivos.
```
### 4

### DATOS DISCRECIONALES DEL CLIENTE

### ORIGINADOR O^ AN^20 21 -^40

```
Datos del Cliente Originador y/o de la Entidad
Financiera
Originadora.
```

### MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 158 de 329
```
### 5 IDENTIFICACION DEL CLIENTE ORIGINADOR M AN 10 41 - 50

```
Número de identificación del Cliente
Originador.
```
```
6 TIPO DE SERVICIO M AN 3 51 - 53 PPD^ (Pagos^ de^ Depósito^ Directo^ y^ Pagos^ Prea
cordados)
7 DESCRIPCION DE LOTE M AN 10 54 - 63 Descripción del lote según anexo 16.
```
```
8 FECHA DESCRIPTIVA O AN 8 64 - 71
Fecha informativa asignada por el Cliente
Originador.
```
### 9 FECHA EFECTIVA DE LA TRANSACCION R

### AAAAMM

### DD 8 72 -^79

```
Fecha en la cual las Entidad Participante
Receptor deben aplicar las transacciones del
lote.
10 FECHA DE COMPENSACIÓN JULIANA O N 3 80 - 82 Fecha de liquidación de las transacciones.
```
```
11 CODIGO ESTADO DEL CLIENTE ORIGINADOR M AN 1 83 - 83
Valor válido “1” e indica el estado del Cliente
Originador.
```
```
12
```
### CODIGO ENTIDAD PARTICIPANTE

### ORIGINADORA

```
M RRRRRTTT 8 84 - 91 Código de el participante originador.
```
### 13 NUMERO DE LOTE M N 7 92 - 98

```
Secuencial ascendente único para cada lote en
del archivo iniciando en 1.
14 RESERVADO N/D Blancos 8 99 - 106 Campo reservado.
```
Registro de Control de Lote

```
Para transacciones de: Prenotificación Débito y Monetarias Débito
```
```
Nombre de Ca# mpo
Inclusió
n Contenido^ Longitud^ Posición^ Descripción^
1 TIPO DE REGISTRO M “8” 1 1 Valor válido para este campo "8".
2 CODIGO CLASE DE TRANSACCIONES POR LOTE M N 3 2 - 4 200 - Débitos y Créditos, 225 - Débitos
```
```
3 NUMERO^ DE^ TRANSACCIONES^ DETALLADAS^ Y^
DE REGISTROS ADENDA
M N 6 5 - 10 Número^ de^ registros^ de^ detalle^ y^ de^ adenda^ en^
el lote.
```

### MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 159 de 329
```
### 4 TOTALES DE CONTROL M N 10 11 - 20

```
Sumatoria de códigos de las Entidades
Participantes
Receptora de los Registros de Detalle de
Transacciones.
```
```
5 VALOR TOTAL DE DEBITOS M
```
### $$$$$$$$$

### $$$$$$$$$

### 18 21 - 38

```
Suma de valores de las transacciones débito del
lote.
```
```
6 VALOR TOTAL DE CREDITOS M $$$$$$$$$
$$$$$$$$$
18 39 - 56 Suma^ de^ valores^ de^ las^ transacciones^ crédito^
del lote.
```
```
7 IDENTIFICACION DEL CLIENTE ORIGINADOR R AN 10 57 - 66
Número de identificación del Cliente
Originador.
```
```
8 CODIGO DE AUTENTICACION DE MENSAJES O AN 19 67 - 85
Campo reservado para un algoritmo de
seguridad.
9 RESERVADO N/D Blancos 6 86 - 91 Campo reservado no disponible.
```
```
10
```
### IDENTIFICACION DE LA ENTIDAD

### PARTICIPANTE ORIGINADORA

### M RRRRRTTT 8 92 - 99

```
Código de la Entidad Financiera Originadora
que inicia la transacción.
11 NUMERO DEL LOTE M N 7 100 - 106 Número del Lote.
```
Registro de Detalle de Transacciones

```
Para transacciones de: Prenotificación Débito y Monetarias Débito
Nombre de Ca# mpo Inclusión Contenido Longitud Posición Descripción
1 TIPO DE REGISTRO M “6” 1 1 Valor válido para este campo "6".
```
### 2

### CODIGO DE TRANSACCION

### M

### N

### 2

### 2 - 3

```
Transacción/Ti
po CTA
```
```
Cuenta
Corriente
```
```
Cuenta
de
Ahorros
```
```
Depósitos
electrónicos
```
```
Prenotificación
Débito
```
### 28 38 57


### MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 160 de 329
```
```
Transacción
Débito
```
### 27 37 55

### 3 CODIGO^ ENTIDAD^ PARTICIPANTE^

### RECEPTOR

```
M RRRRRTTT 8 4 - 11 Número^ de^ Ruta^ y^ Tránsito^ de^ la^ Entidad^
Participante Receptor.
4 DIGITO DE CHEQUEO M N 1 12 - 12 Dígito de chequeo correspondiente al campo 3.
```
### 5

### NUMERO DE CUENTA DEL CLIENTE

### RECEPTOR R^ AN^17 13 -^29

```
Número de cuenta del Cliente Receptor en la
Entidad Participante Receptor. Solo caracteres
numéricos.
```
### 6

### VALOR DE LA TRANSACCIÓN

### M

### $$$$$$$$

### $$$$$$$$

### $$

### 18

### 30 - 47

```
Tipo de Transacción Valor
Prenotificación Débito Cero ($0 pesos)
```
```
Transacción Monetaria Débito
Valor para
recaudar
```
### 7

### NUMERO DE IDENTIFICACION DEL CLIENTE

### RECEPTOR R

(^1) AN 15 48 - 62
Campo utilizado por el Cliente Originador para
identificar al
Cliente Receptor.
8 NOMBRE DEL CLIENTE RECEPTOR R AN 22 63 - 84 Registra el nombre del Cliente Receptor.
9 DATOS DISCRECIONALES O AN 2 85 - 86 Este campo no debe contener un valor particular.
10 INDICADOR DE REGISTRO ADENDA M N 1 87 - 87 Valor “1” para anexar la referencia del pago.

### 11

### NUMERO DE SECUENCIA

### M

### N

### 15

### 88 - 102

```
En las primeras 8 posiciones se debe registrar el
Código de la Entidad Financiera Originadora y en las
siguientes 7 posiciones, un consecutivo.
12 RESERVADO N/D Blancos 4 103 - 106 Campo reservado.
```
(^1) En las transacciones de prenotificación débito y en las transacciones monetarias débito, este campo SIEMPRE deberá contener la
identificación del Cliente Receptor.


### MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 161 de 329
```
Registro Adenda – Información Adicional

```
Para transacciones de: Prenotificación Débito y Monetarias Débito
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción
1 TIPO DE REGISTRO M “7” 1 1 Valor válido para este campo "7".
2 CODIGO TIPO DE REGISTRO ADENDA M “05” 2 2 - 3 Valor válido para este campo “05”.
```
```
3 CODIGO CLIENTE ORIGINADOR POR SERVICIO M N 13 4 - 16
Código EAN- 13 que identifica el tipo de servicio o
NIT
```
### 4 REFERENCIA 1 M AN 30 17 - 46

```
Código único asignado por el Cliente Originador
(empresa recaudadora) al Cliente Receptor. Es
constante en el tiempo.
```
### 5

### DESCRIPCION DEL SERVICIO

### M

### AN

### 15

### 47 - 61

```
Descripción específica del recaudo para ser
informada al Cliente
Receptor.
6 RESERVADO N/D AN 22 62 - 83 Campo reservado. Este campo debe ir en blancos.
```
```
7 NUMERO^ DE^ SECUENCIA^ DE^ REGISTRO^
ADENDA
M N 4 84 - 87 Valor válido para este campo “0 0 01”.
```
### 8

### NUMERO DE SECUENCIA DE TRANSACCION

### DEL REGISTRO DE DETALLE DE

### TRANSACCIONES

### M N 7 88 - 94

```
Su valor debe coincidir con las siete últimas
posiciones del campo
11, registro tipo “6”, al cual hace referencia.
9 RESERVADO N/D AN 12 95 - 106 Campo reservado. Este campo debe ir en blancos.
```

```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 162 de 329
```
## 6.6. Ficha Técnica Transacción Devolución

## 6.6.1. Consideraciones generales

Las Transacciones de Devolución son generadas por el participante Receptor hacia ACH COLOMBIA para
informar a el participante originador que una transacción no fue exitosa por una razón específica. A
continuación, se presentan algunas consideraciones de la Ficha Técnica de la Transacción de Devolución:

```
− Este ítem describe la Transacción de Devolución a Prenotificación Crédito y Débito y la de
Devolución a la Transacción monetaria Crédito y Débito.
− Para cualquier tipo de Devolución se debe usar la secuencia de registros del formato NACHA-M,
así: Registro de Encabezado de Archivo, Registro de Encabezado de Lote, Registro de Detalle de
Transacciones, Registro Adenda, Registro de Control de Lote y Registro de Control de Archivo.
− Los valores contenidos en el Registro de Encabezado de Lote, el Registro de Detalle de
Transacciones y el Registro de Control de Lote de la transacción original que reciba el participante
Receptor, se deben conservar al momento de generar la Transacción de Devolución, excepto
aquellos que se especifiquen en esta ficha técnica.
− La Entidad Participante Receptor solamente puede iniciar una Devolución para cada transacción
recibida.
− Las Transacciones de Devolución son transacciones nuevas y por lo tanto deben tener asignado
un nuevo número de secuencia.
− El Registro Adenda recibido originalmente en una transacción, no son retornados al generar una
Devolución.
− La Entidad Participante no debe mezclar transacciones de Devolución con otro tipo de
transacciones.
− La Entidad Participante Receptor debe agrupar la información de los Devoluciones por Usuario
Originador asociado a cada lote y por tipo de información contenida en cada uno, manteniendo
siempre el orden del lote original.
− No aplica ningún requerimiento de validación del Usuario Receptor en una Transacción de
Devolución.
− El Registro Adenda es de uso obligatorio para las Transacciones de Devolución.
```

```
MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 163 de 329
```
## 6.6.2. Requerimientos de formato

```
Registro de Encabezado de Archivo
Para transacciones de: Devolución de Prenotificación Crédito – Débito y para transacciones de Devolución de Monetarias Crédito – Débito
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción
1 TIPO DE REGISTRO M “1” 1 1 Valor válido para este campo "1".
2 CODIGO DE PRIORIDAD R N 2 2 - 3 Valor válido “01”.
```
```
3 CODIGO ENTIDAD DESTINO INMEDIATO M
bRRRRRTT
TC
```
### 10 4 - 13

```
Código de ACH COLOMBIA ( 000 101006) y dígito de
chequeo
```
```
4 CODIGO ENTIDAD ORIGEN INMEDIATO M bRRRRRTT
TC
10 14 - 23 Código^ de^ el^ participante^ que^ envía^ el^ archivo^ y^
dígito de chequeo.
```
```
5 FECHA DE CREACION DEL ARCHIVO M AAAAMM
DD
8 24 - 31 Fecha de creación del archivo.
```
```
6 HORA DE CREACION DEL ARCHIVO O HHMM 4 32 - 35 Hora en la cual es transmitido o creado el archivo.
```
```
7 IDENTIFICADOR DEL ARCHIVO M A-Z / 0 - 9 1 36 - 36
Identificación de archivos creados en la misma
fecha.
```
```
8 TAMAÑO DEL REGISTRO M ‘106’ 3 37 - 39
Número de caracteres contenidos en cada
registro.
9 FACTOR DE ABLOCAMIENTO M ‘10’ 2 40 - 41 Número de registros dentro de un bloque.
10 CODIGO DE FORMATO M ‘1’ 1 42 - 42 Permite futuras variaciones de formato.
11 NOMBRE ENTIDAD DESTINO INMEDIATO O AN 23 43 - 65 Nombre del ACH (ACH COLOMBIA).
12 NOMBRE ENTIDAD ORIGEN INMEDIATO O AN 23 66 - 88 Nombre del participante que envía el archivo.
13 CODIGO DE REFERENCIA O AN 8 89 - 96 Código del sistema.
14 RESERVADO N/D Blancos 10 97 - 106 Campo reservado. Este campo debe ir en blancos.
```

```
MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31
```
### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A

```
Agosto de 2024
Página 164 de 329
```
```
Registro de Control de Archivo
Para transacciones de: Devolución de Prenotificación Crédito – Débito y para transacciones de Devolución de Monetarias Crédito – Débito
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción
1 TIPO DE REGISTRO M “9” 1 1 Valor válido para este campo "9".
2 CANTIDAD DE LOTES M N 6 2 - 7 Número de lotes incluidos en el archivo.
```
```
3 NUMERO DE BLOQUES M N 6 8 - 13
Número de bloques físicos en el archivo de 10
registros cada uno.
```
```
4
```
### NUMERO DE TRANSACCIONES DETALLADAS

### Y DE REGISTROS ADENDA

### M N 8 14 - 21

```
Número total de registros de detalle y de adenda
en el archivo.
```
### 5 TOTALES DE CONTROL M N 10 22 - 31

```
Sumatoria de los códigos de las Entidades
Participantes Receptor de los
Registros de Detalle de Transacciones.
```
### 6 VALOR TOTAL DE DEBITOS M

### $$$$$$$$

### $$$$$$$$

### $$

```
18 32 - 49 Suma^ de^ valores^ de^ las^ transacciones^ tipo^ débito^
del archivo.
```
### 7 VALOR TOTAL DE CREDITOS M

### $$$$$$$$

### $$$$$$$$

### $$

### 18 50 - 67

```
Suma de valores de las transacciones tipo crédito
del archivo.
```
```
8 RESERVADO N/D Blancos 39 68 - 106 Campo reservado no disponible.
```

```
MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 165 de 329
```
Registro de Encabezado de Lote

Para transacciones de: Devolución de Prenotificación Crédito – Débito y para transacciones de Devolución de Monetarias Crédito – Débito

# Nombre de Campo Inclusión Contenido Longitud Posición Descripción

1 TIPO DE REGISTRO M “5” 1 1 Valor válido para este campo "5”.

2 CODIGO CLASE DE TRANSACCIONES POR LOTE M N 3 2 - 4 Código^ Clase^ de^ Transacciones^ por^ lote^ de^ la^
transacción original.

3 NOMBRE DEL USUARIO ORIGINADOR M AN 16 5 - 20
Nombre del Usuario Originador de la transacción
original.

4

### DATOS DISCRECIONALES DEL USUARIO

```
ORIGINADOR O^ AN^20 21 -^40 Datos^ Discrecionales^ de^ la^ transacción^ original.^
```
5 IDENTIFICACION DEL USUARIO ORIGINADOR M AN 10 41 - 50
Identificación del Usuario contenida en la
transacción original.

6 TIPO DE SERVICIO M AN 3 51 - 53
PPD (Pagos de Depósito Directo y Pagos Prea
cordados)

7 DESCRIPCION DE LOTE M AN 10 54 - 63 Descripción del lote de la transacción original.

8 FECHA DESCRIPTIVA O AN 8 64 - 71 Fecha informativa de la transacción original.

9 FECHA EFECTIVA DE LA TRANSACCION R

### AAAAMM

```
DD 8 72 -^79 Fecha^ de^ aplicación^ de^ las^ devoluciones^ del^ lote.^
```
10 FECHA DE COMPENSACIÓN JULIANA O N 3 80 - 82 Fecha de liquidación de las transacciones.

11 CODIGO ESTADO DEL USUARIO ORIGINADOR M AN 1 83 - 83
Valor válido “1” e indica el estado del Usuario
Originador.

12 CODIGO^ ENTIDAD^ PARTICIPANTE^
ORIGINADORA
M RRRRRTTT 8 84 - 91 Código de el participante que genera la devolución,

### 13 NUMERO DE LOTE M N 7 92 - 98

```
Secuencial ascendente único para cada lote en el
archivo iniciando en 1.
```

```
MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 166 de 329
```
```
14 RESERVADO N/D Blancos 8 99 - 106 Campo reservado.
```
Registro de Control de Lote

Para transacciones de: Devolución de Prenotificación Crédito – Débito y para transacciones de Devolución de Monetarias Crédito – Débito

# Nombre de Campo Inclusión Contenido Longitud Posición Descripción

1 TIPO DE REGISTRO M “8” 1 1 Valor válido para este campo "8".

### 2 CODIGO^ CLASE^ DE^ TRANSACCIONES^ POR^ LOTE^

### M

### N

### 3

### 2 - 4

```
Código Clase de Transacciones por lote de la
transacción original.
```
### 3

### NUMERO DE TRANSACCIONES DETALLADAS Y

### DE REGISTROS ADENDA

### M

### N

### 6

### 5 - 10

```
Número de registros de detalle y de adenda en el
lote.
```
### 4

### TOTALES DE CONTROL

### M

### N

### 10

### 11 - 20

```
Suma de códigos de las Entidades Participantes
Receptoras de los Registros de Detalle de
Transacciones.
```
### 5 VALOR TOTAL DE DEBITOS M

### $$$$$$$$

### $$$$$$$$

### $$

### 18 21 - 38

```
Suma de valores de las transacciones débito del
lote.
```
### 6 VALOR TOTAL DE CREDITOS M

### $$$$$$$$

### $$$$$$$$

### $$

```
18 39 - 56 Suma^ de^ valores^ de^ las^ transacciones^ crédito^ del^
lote.
```
7 IDENTIFICACION DEL USUARIO ORIGINADOR R AN 10 57 - 66 Número de identificación del Usuario Originador.

8 CODIGO DE AUTENTICACION DE MENSAJES O AN 19 67 - 85 Campo reservado para un algoritmo de seguridad.

9 RESERVADO N/D Blancos 6 86 - 91 Campo reservado no disponible.

### 10

### IDENTIFICACION DE LA ENTIDAD

### PARTICIPANTE ORIGINADORA

### M

### RRRRRTTT

### 8

### 92 - 99

```
Código de el participante que genera la devolución.
```

```
MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 167 de 329
```
11 NUMERO DEL LOTE M N 7 100 - 106 Nuevo Número del Lote.

Registro de Detalle de Transacciones

Para transacciones de: Devolución de Prenotificación Crédito – Débito y para transacciones de Devolución de Monetarias Crédito – Débito

# Nombre de Campo Inclusión Contenido Longitud Posición Descripción

1 TIPO DE REGISTRO M “6” 1 1 Valor válido para este campo "6".

### 2 CODIGO DE TRANSACCION M N 2 2 - 3

```
Transacción
Original/Tipo Cta.
```
```
Cuenta
Corriente
```
```
Cuenta
de
Ahorros
```
```
Depósitos
electrónicos
```
```
Prenotificación
Crédito
Transacción
Monetaria Crédito
```
### 21 31 51

```
Prenotificación
Débito Transacción
Monetaria Débito
```
### 26 36 56

### 3 CODIGO ENTIDAD PARTICIPANTE RECEPTOR M RRRRRTTT 8 4 - 11

```
Código de el participante Receptor de la devolución, es
decir el Código de la Entidad Financiera Originadora de
la transacción original.
```
4 DIGITO DE CHEQUEO M N 1 12 - 12 Dígito de chequeo correspondiente al campo 3.

5

### NUMERO DE CUENTA DEL USUARIO

### RECEPTOR

```
R AN 17 13 - 29 Número de cuenta contenido en la transacción original.
```
6 VALOR DE LA TRANSACCIÓN M $$$$$$$$ 18 30 - 47 Tipo de Transacción Valor


```
MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 168 de 329
```
### $$$$$$$$

### $$

```
Devolución de Prenotificación
Débito
Devolución de Prenotificación
Crédito
```
```
Cero ($0 pesos)
```
```
Devolución de Transacción
Débito
Devolución de Transacción
Crédito
```
```
Valor transacción
original.
```
### 7

### NUMERO DE IDENTIFICACION DEL USUARIO

### RECEPTOR

### O/R AN 15 48 - 62

```
Identificación del Usuario Receptor de la transacción
original.
```
8 NOMBRE DEL USUARIO RECEPTOR R AN 22 63 - 84 Nombre^ del^ Usuario^ Receptor^ de^ la^ transacción^
original.

9 DATOS DISCRECIONALES O AN 2 85 - 86 Datos Discrecionales de la transacción original.

10 INDICADOR DE REGISTRO ADENDA M N 1 87 - 87 Valor “1” para anexar información de la devolución.

### 11 NUMERO DE SECUENCIA M N 15 88 - 102

```
En las primeras 8 posiciones se debe registrar el Código
de el participante que genera la devolución y en las
siguientes 7 posiciones, un nuevo número consecutivo.
```
12 RESERVADO N/D Blancos 4 103 - 106 Campo reservado.


```
MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 169 de 329
```
Registro Adenda

Para transacciones de: Devolución de Prenotificación Crédito – Débito y para transacciones de Devolución de Monetarias Crédito – Débito

# Nombre de Campo Inclusión Contenido Longitud Posición Descripción

1 TIPO DE REGISTRO M “7” 1 1 Valor válido para este campo "7".

2 CODIGO TIPO DE REGISTRO ADENDA M “99” 2 2 - 3 Valor válido “99”.

3 CAUSAL DE DEVOLUCION M AN 3 4 - 6 Causal de devolución según el anexo 9.

### 4

### NUMERO DE SECUENCIA DE LA TRANSACCION

### ORIGINAL

### M N 15 7 - 21

```
Número de secuencia de la transacción original
que se está devolviendo. Debe coincidir con la
información contenida en el campo 11 del Registro
de Detalle de Transacciones de la transacción
original.
```
5 FECHA DE MUERTE O AAAAMM
DD
8 22 - 29 Fecha^ de^ fallecimiento^ del^ titular^ o^ beneficiario^ de^
la cuenta.

### 6 CODIGO^ ENTIDAD^ PARTICIPANTE^ RECEPTOR^

### DE LA TRANSACCION ORIGINAL

### R RRRRRTTT 8 30 - 37

```
Código de la Entidad Participante Receptor de la
transacción original (campo 3 del Registro de
Detalle de Transacciones de la transacción original).
```
7 INFORMACION ADICIONAL O AN 44 38 - 81
Descripción estándar de la causal con el mayor
detalle posible.

### 8

### NUMERO DE SECUENCIA DEL REGISTRO

### ADENDA M^ N^15 82 -^96

```
Número de secuencia del Registro Adenda que está
asociado con el Registro de Detalle de
Transacciones. Asignado por la Entidad Participante
Receptor que genera la devolución.
```
9 RESERVADO N/D Blancos 10 97 - 106 Campo reservado no disponible.


```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 3 1
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 170 de 329
```
## 6.7. Ficha Técnica Transacción Devolución por Operador

## 6.7.1. Consideraciones generales

Las Transacciones de Devolución por Operador son generadas por el sistema ACH COLOMBIA para
informar a el participante que envía alguna transacción, que el proceso de validación detallado no fue
exitoso en ACH COLOMBIA por una razón específica, y que no podrá ser cargado. Aquellas transacciones
que no cumplan con las condiciones definidas son rechazadas por el Operador de ACH, mediante Errores
Formales utilizando el mismo formato usado para la generación de devoluciones desde el participante
Receptor.

Las validaciones son realizadas por el sistema ACH COLOMBIA sobre transacciones permitidas como:
prenotificación crédito, prenotificación débito, transacción monetaria crédito, monetario débito, o
devolución a transacciones. A continuación, se presentan algunas consideraciones de la Ficha Técnica
de la Transacción de Devolución por Operador:

```
− Para cualquier tipo de Devolución se debe usar la secuencia de registros del formato NACHA-M,
así: Registro de Encabezado de Archivo, Registro de Encabezado de Lote, Registro de Detalle de
Transacciones, Registro Adenda, Registro de Control de Lote y Registro de Control de Archivo.
− Los valores contenidos en el Registro de Encabezado de Lote, el Registro de Detalle de
Transacciones y el Registro de Control de Lote de la transacción original que reciba el
participante originador, se conservan al momento de generar la Transacción de Devolución por
Operador, excepto aquellos que se especifiquen en esta ficha técnica.
− ACH COLOMBIA solamente puede iniciar una Devolución por Operador para cada transacción
recibida.
− Las Transacciones de Devolución por Operador mantienen el número de secuencia enviado por
el participante en la transacción original.
− El Registro Adenda recibido originalmente en una transacción a validar no es retornado al
generar una Devolución por Operador.
− ACH COLOMBIA genera las Devoluciones por Operador en un archivo separado y exclusivo para
este tipo de transacciones, según lo especificado en el numeral 6.1.8 Devoluciones por
operador.
− ACH COLOMBIA agrupa la información de los Devoluciones por Operador por Usuario
Originador, manteniendo la organización del archivo originalmente recibida, ordenando el
archivo por Entidad Participante y secuencia.
− Cada transacción de Devolución por Operador contiene de forma obligatoria el Registro Adenda.
− ACH COLOMBIA indica la causal de Devolución por Operador según el Anexo 3: Causales
Devolución por Operador.
```

### MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 171 de 329
```
## 6.7.2. Requerimientos de formato

Registro de Encabezado de Archivo

```
Para transacciones de: Devolución por Operador
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción
1 TIPO DE REGISTRO M “1” 1 1 Valor válido para este campo "1".
2 CODIGO DE PRIORIDAD R N 2 2 - 3 Valor válido “01”.
```
### 3

### CODIGO ENTIDAD DESTINO INMEDIATO

### M

```
bRRRRRTT
TC
```
### 10

### 4 - 13

```
Código de el participante que recibe el archivo, es
decir de
el participante que originalmente lo envió.
```
```
4 CODIGO ENTIDAD ORIGEN INMEDIATO M
bRRRRRTT
TC
10 14 - 23 Código de ACH COLOMBIA (0001 010 06).
```
### 5 FECHA DE CREACION DEL ARCHIVO M

### AAAAMM

### DD

```
8 24 - 31 Fecha de creación del archivo.
```
```
6 HORA DE CREACION DEL ARCHIVO O HHMM 4 32 - 35 Hora en la cual es creado el archivo.
```
```
7 IDENTIFICADOR DEL ARCHIVO M A-Z / 0 - 9 1 36 - 36 Identificación^ de^ archivos^ creados^ en^ la^ misma^
fecha.
```
```
8 TAMAÑO DEL REGISTRO M ‘1 06 ’ 3 37 - 39
Número de caracteres contenidos en cada
registro.
9 FACTOR DE ABLOCAMIENTO M ‘10’ 2 40 - 41 Número de registros dentro de un bloque.
```
### 10 CODIGO DE FORMATO M ‘1’ 1 42 - 42

```
Permite futuras variaciones de formato.
```

### MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 172 de 329
```
### 11 NOMBRE ENTIDAD DESTINO INMEDIATO O AN 23 43 - 65

```
Nombre del participante.
```
### 12 NOMBRE ENTIDAD ORIGEN INMEDIATO O AN 23 66 - 88

```
Nombre del ACH (ACH COLOMBIA).
```
### 13 CODIGO DE REFERENCIA O AN 8 89 - 96

```
Código del sistema: “1” o blancos.
```
```
14 RESERVADO N/D Blancos 10 97 - 106 Campo reservado. Este campo debe ir en blancos.
```
Registro de Control de Archivo

```
Para transacciones de: Devolución por Operador
Nombre# de Campo Inclusión Contenido Longitud Posición Descripción
1 TIPO DE REGISTRO M “9” 1 1 Valor válido para este campo "9".
2 CANTIDAD DE LOTES M N 6 2 - 7 Número de lotes incluidos en el archivo.
```
```
3 NUMERO DE BLOQUES M N 6 8 - 13 Número^ de^ bloques^ físicos^ en^ el^ archivo^ de^10 registros^
cada uno.
```
### 4

### NUMERO DE TRANSACCIONES

### DETALLADAS Y DE REGISTROS

### ADENDA

### M N 8 14 - 21

```
Número total de registros de detalle y de adenda en el
archivo.
```
### 5

### TOTALES DE CONTROL

### M

### N

### 10

### 22 - 31

```
Sumatoria de los códigos de las Entidades Participantes
Receptor de los Registros de Detalle de Transacciones.
```

### MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 173 de 329
```
### 6 VALOR TOTAL DE DEBITOS M

### $$$$$$$$$

### $$$$$$$$$

### 18 32 - 49

```
Suma de valores de las transacciones tipo débito del
archivo.
```
```
7 VALOR TOTAL DE CREDITOS M $$$$$$$$$
$$$$$$$$$
18 50 - 67 Suma^ de^ valores^ de^ las^ transacciones^ tipo^ crédito^ del^
archivo.
8 RESERVADO N/D Blancos 39 68 - 106 Campo reservado no disponible.
```
Registro de Encabezado de Lote

```
Para transacciones de: Devolución por Operador
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción
1 TIPO DE REGISTRO M “5” 1 1 Valor válido para este campo "5”.
```
```
2
```
### CODIGO CLASE DE TRANSACCIONES POR

### LOTE

### M

### N

### 3

### 2 - 4

```
Código Clase de Transacciones por lote de la
transacción original.
```
```
3 NOMBRE DEL USUARIO ORIGINADOR M AN 16 5 - 20
Nombre del Usuario Originador de la transacción
original.
```
```
4 DATOS^ DISCRECIONALES^ DEL^ USUARIO^
ORIGINADOR
O
AN
20
21 - 40
Datos Discrecionales de la transacción original.
```
```
5 IDENTIFICACION^ DEL^ UISUARIO^
ORIGINADOR
M AN 10 41 - 50 Identificación^ del^ Usuario^ contenida^ en^ la^
transacción original.
```
```
6 TIPO DE SERVICIO M AN 3 51 - 53
Tipo de Servicio contenido en la transacción
original.
7 DESCRIPCION DE LOTE M AN 10 54 - 63 Descripción del lote de la transacción original.
8 FECHA DESCRIPTIVA O AN 8 64 - 71 Fecha informativa de la transacción original.
```
```
9 FECHA EFECTIVA DE LA TRANSACCION R AAAAMM
DD
8 72 - 79 Fecha de aplicación de las devoluciones del lote.
```
```
10 FECHA DE COMPENSACIÓN JULIANA O N 3 80 - 82 Fecha de liquidación de las transacciones.
```
```
11
```
### CODIGO ESTADO DEL USUARIO

### ORIGINADOR

### M AN 1 83 - 83

```
Valor válido “1” e indica el estado del Usuario
Originador.
```

### MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 174 de 329
```
### 12

### CODIGO ENTIDAD PARTICIPANTE

### ORIGINADORA M^ RRRRRTTT^8 84 -^91

```
Código de la entidad que genera la devolución.
Debe ir el código de la entidad financiera
originadora, ya que es ella misma la que genera la
devolución
```
13 NUMERO DE LOTE M N 7 92 - 98
Secuencial ascendente único para cada lote en el
archivo.

14 RESERVADO N/D Blancos 8 99 - 106 Campo reservado.


### MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 175 de 329
```
Registro de Control de Lote

```
Para transacciones de: Devolución por Operador
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción
1 TIPO DE REGISTRO M “8” 1 1 Valor válido para este campo "8".
```
### 2

### CODIGO CLASE DE TRANSACCIONES POR

### LOTE

### M

### N

### 3

### 2 - 4

```
Código Clase de Transacciones por lote de la
transacción original.
```
### 3

### NUMERO DE TRANSACCIONES

### DETALLADAS Y DE REGISTROS ADENDA

### M

### N

### 6

### 5 - 10

```
Número de registros de detalle y de adenda en el
lote.
```
### 4

### TOTALES DE CONTROL

### M

### N

### 10

### 11 - 20

```
Suma de códigos de las Entidades Participantes
Receptoras de los Registros de Detalle de
Transacciones.
```
```
5 VALOR TOTAL DE DEBITOS M
```
### $$$$$$$$$

### $$$$$$$$$ 18 21 -^38

```
Suma de valores de las transacciones débito del
lote.
```
```
6 VALOR TOTAL DE CREDITOS M
```
### $$$$$$$$$

### $$$$$$$$$

### 18 39 - 56

```
Suma de valores de las transacciones crédito del
lote.
```
```
7
```
### IDENTIFICACION DEL USUARIO

### ORIGINADOR

```
R AN 10 57 - 66 Número de identificación del Usuario Originador.
```
### 8

### CODIGO DE AUTENTICACION DE

### MENSAJES

```
O AN 19 67 - 85 Campo reservado para un algoritmo de seguridad.
```
```
9 RESERVADO N/D Blancos 6 86 - 91 Campo reservado no disponible.
```
### 10

### IDENTIFICACION DE LA ENTIDAD

### PARTICIPANTE ORIGINADORA

### M

### RRRRRTTT

### 8

### 92 - 99

```
Código de la entidad que genera la devolución.
11 NUMERO DEL LOTE M N 7 100 - 106 Nuevo Número del Lote.
```

### MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 176 de 329
```
Registro de Detalle de Transacciones

```
Para transacciones de: Devolución por Operador
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción
1 TIPO DE REGISTRO M “6” 1 1 Valor válido para este campo "6".
```
### 2 CODIGO DE TRANSACCION M N 2 2 - 3

```
Transacción
Original/Tipo
Cta.
```
```
Cuenta
Corriente
```
```
Cuenta de
Ahorros
Depósitos
electrónicos
```
```
Prenotificació
n Crédito
Transacción
Monetaria
Crédito
```
### 21

### 31 51

```
Prenotificació
n Débito
Transacción
Monetaria
Débito
```
### 26 36 56

### 3 CODIGO^ ENTIDAD^ PARTICIPANTE^

### RECEPTOR

### M RRRRRTTT 8 4 - 11

```
Código de el participante Receptor de la
devolución. Se debe mantener el código de la
Entidad Participante Receptor de la transacción
original.
4 DIGITO DE CHEQUEO M N 1 12 - 12 Dígito de chequeo correspondiente al campo 3.
```
### 5

### NUMERO DE CUENTA DEL USUARIO

### RECEPTOR

### R AN 17 13 - 29

```
Número de cuenta contenido en la transacción
original.
6 VALOR DE LA TRANSACCIÓN M $$$$$$$$$ 18 30 - 47 Tipo de Transacción Valor
```

### MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 177 de 329
```
Registro de Detalle de Transacciones

```
Para transacciones de: Devolución por Operador
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción
$$$$$$$$$ Devolución de Prenotificación
Débito Devolución de
Prenotificación Crédito
Devolución a Enrolamiento
```
```
Cero ($0 pesos)
```
```
Devolución de Transacción Débito
Devolución de Transacción
Crédito
```
```
Valor
transacción
original.
```
```
7
```
### NUMERO DE IDENTIFICACION DEL

### USUARIO RECEPTOR O/R^ AN^15 48 -^62

```
Identificación del Usuario Receptor de la
transacción original.
```
```
8 NOMBRE DEL USUARIO RECEPTOR R AN 22 63 - 84
Nombre del Usuario Receptor de la transacción
original.
9 DATOS DISCRECIONALES O AN 2 85 - 86 Datos Discrecionales de la transacción original.
10 INDICADOR DE REGISTRO ADENDA M N 1 87 - 87 Valor “1” para anexar información de la devolución.
```
### 11 NUMERO DE SECUENCIA M N 15 88 - 102

```
En las primeras 8 posiciones se debe registrar el
Código de la Entidad Financiera que genera la
devolución y en las siguientes 7 posiciones, un
nuevo número consecutivo. En las primeras 8
posiciones debe ir el código de la Entidad
Participante Originadora, ya que es ella misma la
que genera la devolución.
12 RESERVADO N/D Blancos 4 103 - 106 Campo reservado.
```

### MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 178 de 329
```
Registro de Adenda

```
Para transacciones de: Devolución por Operador
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción
1 TIPO DE REGISTRO M “7” 1 1 Valor válido para este campo "7".
2 CODIGO TIPO DE REGISTRO ADENDA M “99” 2 2 - 3 Valor válido “99”.
```
```
3 CAUSAL DE DEVOLUCION M AN 3 4 - 6
Causal de devolución según lo descrito en el
numeral 6.1.8 Devoluciones por operador.
```
### 4

### NUMERO DE SECUENCIA DE LA

### TRANSACCION ORIGINAL

### M N 15 7 - 21

```
Número de secuencia de la transacción original
que se está devolviendo. Debe coincidir con la
información contenida en el campo 11 del
Registro de Detalle de Transacciones de la
transacción original.
```
```
5 FECHA DE MUERTE O AAAAMM
DD
8 22 - 29 Fecha^ de^ fallecimiento^ del^ titular^ o^ beneficiario^
de la cuenta.
```
### 6

### CODIGO ENTIDAD PARTICIPANTE

### RECEPTOR DE LA TRANSACCION

### ORIGINAL

### R 0RRRRTTT 8 30 - 37

```
Código de la Entidad Participante Receptor de la
transacción original (campo 3 del Registro de
Detalle de Transacciones de la transacción
original).
```
```
7 INFORMACION ADICIONAL O AN 44 38 - 81 Descripción^ estándar^ de^ la^ causal^ con^ el^ mayor^
detalle posible.
```
### 8

### NUMERO DE SECUENCIA DEL REGISTRO

### ADENDA

### M N 15 82 - 96

```
Número de secuencia del Registro Adenda que
está asociado con el Registro de Detalle de
Transacciones. Asignado por la Entidad
Participante Receptor que genera la devolución.
9 RESERVADO N/D Blancos 10 97 - 106 Campo reservado no disponible.
```

```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 3 1
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 179 de 329
```
## 6.8. Ficha Técnica Transacción Crédito generada por PSE

## 6.8.1. Consideraciones generales

El Proveedor de Servicios Electrónicos (PSE) debe generar un (1) archivo de transacciones en formato
NACHA-M, con destino a ACH COLOMBIA, así:

El archivo de transacciones crédito, se genera hacia la cuenta puente o cuenta conciliadora de PSE,
iniciadas en nombre del participante originador que han autorizado los débitos en línea procesados
exitosamente a través del Proveedor de Servicios Electrónicos (PSE). Este proceso afecta la
compensación de el participante originador en contra, y la compensación de la cuenta puente de PSE a
favor.

Se debe tener en cuenta lo siguiente:

```
− Para generar las Transacciones monetarias Crédito se debe usar la secuencia de registros
del formato NACHA-M, así: Registro de Encabezado de Archivo, Registro de Encabezado
de Lote, Registro de Detalle de Transacciones, Registros de adenda o información
adicional, Registro de Control de Lote y Registro de Control de Archivo.
− No se utilizarán transacciones de prenotificación débito ni crédito.
− A cada transacción que se inicie en nombre de cada Entidad Participante Originadora, se
le debe asignar un número de secuencia ascendente durante el día de proceso, de acuerdo
con el rango reservado por cada Entidad Participante definido en los parámetros de PSE.
Dicho rango puede ser reutilizado diariamente, definido y controlado en el PSE.
− Los archivos deben ser nombrados según el estándar y se generarán tantos archivos como
procesos de aplicación de fondos existan, utilizando las secuencias de nombre reservadas.
```

```
MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 180 de 329
```
## 6.8.2. Requerimientos de formato

## Para transacciones monetarias crédito generadas por PSE en nombre de las EF

```
Registro de Encabezado de Archivo
Para transacciones Monetarias Crédito generadas por PSE en nombre de las EF
```
```
# Nombre de Campo Inclusión
Conteni
do
```
```
Longitu
d
Posición Descripción
```
```
1 TIPO DE REGISTRO M “1” 1 1 Valor válido para este campo "1".
2 CODIGO DE PRIORIDAD R N 2 2 - 3 Valor válido “01”.
```
### 3 CODIGO ENTIDAD DESTINO INMEDIATO M

```
bRRRRR
TTTC
```
### 10 4 - 13

```
Código de ACH COLOMBIA (000101006) y dígito
```
```
de cheque.
```
### 4 CODIGO ENTIDAD ORIGEN INMEDIATO M

```
bRRRRR
TTTC
10 14 - 23 Código^ de^ EF^ y^ dígito^ de^ cheque.^
```
### 5 FECHA DE CREACION DEL ARCHIVO M

### AAAAM

### MDD

```
8 24 - 31 Fecha de creación del archivo.
```
```
6 HORA DE CREACION DEL ARCHIVO O HHMM 4 32 - 35 Hora^ en^ la^ cual^ es^ creado^ el^ archivo.^
```
### 7 IDENTIFICADOR DEL ARCHIVO M A-Z / 0 - 9 1 36 - 36

```
Identificación de archivos creados en la misma
```
```
fecha.
```

```
MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 181 de 329
```
Registro de Encabezado de Archivo

Para transacciones Monetarias Crédito generadas por PSE en nombre de las EF

# Nombre de Campo Inclusión
Conteni
do

```
Longitu
d
Posición Descripción
```
### 8 TAMAÑO DEL REGISTRO M ‘106’ 3 37 - 39

```
Número de caracteres contenidos en cada
```
```
registro.
```
### 9 FACTOR DE ABLOCAMIENTO M ‘10’ 2 40 - 41

```
Número de registros dentro de un bloque.
```
10 CODIGO DE FORMATO M ‘1’ 1 42 - 42 Permite futuras variaciones de formato.

11 NOMBRE ENTIDAD DESTINO INMEDIATO O AN 23 43 - 65 Nombre del ACH (ACH COLOMBIA).

12 NOMBRE ENTIDAD ORIGEN INMEDIATO O AN 23 66 - 88 Proveedor de Servicios Electrónicos (PSE).

13 CODIGO DE REFERENCIA O AN 8 89 - 96 Código del sistema.

14 RESERVADO N/D Blancos 10 97 - 106
Campo reservado. Este campo debe ir en
blancos.


```
MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 182 de 329
```
Registro de Control de Archivo

Para transacciones Monetarias Crédito generadas por PSE en nombre de las EF

# Nombre de Campo Inclusión
Contenid
o Longitud^ Posición^ Descripción^

1 TIPO DE REGISTRO M “9” 1 1 Valor válido para este campo "9".

2 CANTIDAD DE LOTES M N 6 2 - 7 Número de lotes incluidos en el archivo.

3 NUMERO DE BLOQUES M N 6 8 - 13
Número de bloques físicos en el archivo de 10
registros cada uno.

4

### NUMERO DE TRANSACCIONES

### DETALLADAS Y DE REGISTROS ADENDA M^ N^8 14 -^21

```
Número total de registros de detalle y de adenda
en el archivo.
```
### 5 TOTALES DE CONTROL M N 10 22 - 31

```
Sumatoria de los códigos de las Entidades
Participantes Receptoras de los Registros de
Detalle de Transacciones.
```
### 6 VALOR TOTAL DE DEBITOS M

### $$$$$$$

### $$$$$$$

### $$

```
18 32 - 49 Este campo contiene ceros.
```
### 7 VALOR TOTAL DE CREDITOS M

### $$$$$$$

### $$$$$$$

### $$

### 18 50 - 67

```
Suma de valores de las transacciones tipo crédito
del archivo.
```
8 RESERVADO N/D Blancos 39 68 - 106 Campo reservado no disponible.

Se debe generar un lote por cada uno de los servicios (PSE-Empresas, Seguridad Social - PSE y Seguridad Social – Planilla Asistida - DIAN), en
total se debe generar máximo tres lotes por cada archivo de cobros.


```
MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 183 de 329
```
Registro de Encabezado de Lote

Para transacciones Monetarias Crédito generadas por PSE en nombre de las EF

# Nombre de Campo Inclusión
Contenid
o
Longitud Posición Descripción

1 TIPO DE REGISTRO M “5” 1 1 Valor válido para este campo "5”.

2

### CODIGO CLASE DE TRANSACCIONES

```
POR LOTE M^ N^3 2 -^4 220 -^ Créditos^
```
3 NOMBRE DEL USUARIO ORIGINADOR M AN 16 5 - 20 PSE o SSS o SSS OC o PSE para pagos de la DIAN

4

### DATOS DISCRECIONALES DEL USUARIO

### ORIGINADOR

```
O AN 20 21 - 40 Campo reservado.
```
### 5 IDENTIFICACION^ DEL^ USUARIO^

### ORIGINADOR

```
M AN 10 41 - 50 Número^ de^ Identificación^ de^ ACH^ COLOMBIA^ -^
PSE.
```
6 TIPO DE SERVICIO M AN 3 51 - 53 PPD

7 DESCRIPCION DE LOTE M AN 10 54 - 63
COBROS PSE o COBROS SSS o COB SSS OC ö PAGOS
DIAN o MULTICREDIT

8 FECHA DESCRIPTIVA N/D AN 8 64 - 71 Campo reservado.

9 FECHA EFECTIVA DE LA TRANSACCION R

### AAAAM

### MDD

### 8 72 - 79

```
Fecha en la cual ACH COLOMBIA procesará las
transacciones.
```
10 FECHA DE COMPENSACIÓN JULIANA O N 3 80 - 82 Fecha de liquidación de las transacciones.

11

### CODIGO ESTADO DEL USUARIO

```
ORIGINADOR M^ AN^1 83 -^83 Valor^ válido^ “1”^ e^ indica^ el^ estado^ del^ PSE.^
```
### 12

### CODIGO ENTIDAD PARTICIPANTE

### ORIGINADORA M^

### RRRRRTT

### T 8 84 -^91

```
Código de el participante originador (que autorizó
y procesó débitos en línea exitosamente a través
del PSE).
```

```
MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 184 de 329
```
```
Registro de Encabezado de Lote
Para transacciones Monetarias Crédito generadas por PSE en nombre de las EF
```
```
# Nombre de Campo Inclusión
Contenid
o Longitud^ Posición^ Descripción^
```
```
13 NUMERO DE LOTE M N 7 92 - 98
Secuencial ascendente único para cada lote en del
archivo iniciando en 1.
14 RESERVADO N/D Blancos 8 99 - 106 Campo reservado.
```
Registro de Control de Lote

Para transacciones Monetarias Crédito generadas por PSE en nombre de las EF

# Nombre de Campo Inclusión
Contenid
o

```
Longitu
d Posición^ Descripción^
```
1 TIPO DE REGISTRO M “8” 1 1 Valor válido para este campo "8".

2 CODIGO^ CLASE^ DE^ TRANSACCIONES^ POR^
LOTE
M N 3 2 - 4 220 - Créditos

### 3 NUMERO^ DE^ TRANSACCIONES^

### DETALLADAS Y DE REGISTROS ADENDA

```
M N 6 5 - 10 Número^ de^ registros^ de^ detalle^ y^ de^ adenda^ en^ el^
lote.
```
### 4 TOTALES DE CONTROL M N 10 11 - 20

```
Sumatoria de códigos de las Entidades Participantes
Receptoras de los Registros de Detalle de
Transacciones.
```
### 5 VALOR TOTAL DE DEBITOS M

### $$$$$$$

### $$$$$$$

### $$

```
18 21 - 38 Este campo contiene ceros.
```
### 6 VALOR TOTAL DE CREDITOS M

### $$$$$$$

### $$$$$$$

### $$

```
18 39 - 56 Suma^ de^ valores^ de^ las^ transacciones^ crédito^ del^
lote.
```

```
MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 185 de 329
```
Registro de Control de Lote

Para transacciones Monetarias Crédito generadas por PSE en nombre de las EF

# Nombre de Campo Inclusión
Contenid
o

```
Longitu
d
Posición Descripción
```
### 7

### IDENTIFICACION DEL USUARIO

### ORIGINADOR

```
R AN 10 57 - 66 Número de Identificación de ACH COLOMBIA - PSE.
```
### 8 CODIGO^ DE^ AUTENTICACION^ DE^

### MENSAJES

```
O AN 19 67 - 85 Campo reservado para un algoritmo de seguridad.
```
9 RESERVADO N/D Blancos 6 86 - 91 Campo reservado no disponible.

10

### IDENTIFICACION DE LA ENTIDAD

### PARTICIPANTE ORIGINADORA

### M RRRRTTT 8 92 - 99

```
Código de el participante originador-indicada en el
registro de Encabezado de Lote.
```
11 NUMERO DEL LOTE M N 7 100 - 106 Número del Lote.

```
Las transacciones de Seguridad Social Planilla Asistida manejan diferentes canales, por lo cual debe manejarse sólo para este tipo de
transacciones un registro de detalle para cada uno de los canales.
```

```
MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31
```
### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A

```
Agosto de 2024
Página 186 de 329
```
```
Registro de Detalle de Transacciones
```
```
Para transacciones Monetarias Crédito generadas por PSE en nombre de las EF
```
```
# Nombre de Campo Inclusión Conteni
do
Longitud Posición Descripción
```
```
1 TIPO DE REGISTRO M “6” 1 1 Valor válido para este campo "6".
```
### 2 CODIGO DE TRANSACCION M N 2 2 - 3

```
Transacción/Tipo CTA
Cuenta
Puente
```
```
Transacción Crédito 42
```
```
3
```
### CODIGO ENTIDAD PARTICIPANTE

### RECEPTOR

### M 000011

### 01

```
8 4 - 11 Número de Ruta y Tránsito de PSE.
```
```
4 DIGITO DE CHEQUEO M 5 1 12 - 12 Dígito de chequeo correspondiente al campo 3.
```
```
5 NUMERO^ DE^ CUENTA^ DEL^ USUARIO^
RECEPTOR
```
### R 111111

### 1101

```
17 13 - 29 Número^ de^ cuenta^ puente^ asignada^ por^ ACH^
COLOMBIA.
```
### 6 VALOR DE LA TRANSACCIÓN M

### $$$$$$

### $$$$$$

### $$$$

### 18 30 - 47

```
Valor total de recaudos exitosos procesados a
través del PSE y autorizados por el participante
originador en un ciclo de proceso.
```
```
7 NUMERO^ DE^ IDENTIFICACION^ DEL^
USUARIO RECEPTOR
```
### O 830078

### 5126

```
15 48 - 62 Identificación de ACH COLOMBIA.
```
```
8 NOMBRE DEL USUARIO RECEPTOR R AN 22 63 - 84 Nombre de ACH COLOMBIA.
```
```
9 DATOS DISCRECIONALES N/D AN 2 85 - 86 Campo reservado.
```
```
10 INDICADOR DE REGISTRO ADENDA M N 1 87 - 87
Valor válido “1”. Para anexar información
adicional relacionada con el pago.
```

```
MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31
```
### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A

```
Agosto de 2024
Página 187 de 329
```
### 11 NUMERO DE SECUENCIA M N 15 88 - 102

```
En las primeras 8 posiciones se debe registrar el
Código de el participante originador y en las
siguientes 7 posiciones, un consecutivo que inicia
en el límite inferior del rango reservado por el
participante originador para el PSE y que va
máximo hasta el límite superior en un día de
proceso.
12 RESERVADO N/D Blancos 4 103 - 106 Campo reservado.
```

```
MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 188 de 329
```
Las transacciones PSE empresas y Seguridad social manejan el canal 15 y las de planilla asistida de acuerdo con una tabla. La tabla de transacciones
fuera de línea maneja los canales por los cuales se han hecho transacciones.

```
Registro Adenda – Información Adicional
Para transacciones Monetarias Crédito generadas por PSE en nombre de las EF
```
```
# Nombre de Campo Inclusión
Conteni
do Longitud^ Posición^ Descripción^
1 TIPO DE REGISTRO M “7” 1 1 Valor válido para este campo "7".
2 CODIGO TIPO DE REGISTRO ADENDA M “05” 2 2 - 3 Valor válido para este campo “05”.
```
```
3 CÓDIGO DEL PSE R N 8 4 - 11 Número^ de^ Identificación^ del^ proveedor^ de^
servicios electrónicos “00001101”
4 NUMERO DE TRANSACCIONES DEBITADAS M N 10 12 - 21 Número de transacciones debitadas.
```
### 5 CANAL DE PAGO R AN 2 22 - 23

```
Canal de pago “15” para PSE empresas y
Seguridad Social y para planilla asistida el canal de
acuerdo con la tabla.
6 RESERVADO N/D Blancos 62 - 60 24 - 83 Campo reservado.
```
```
7
```
### NUMERO DE SECUENCIA DE REGISTRO DE

```
ADENDA M^ N^4 84 -^87 Valor^ válido^ para^ este^ campo^ “0001”^
```
### 8

### NUMERO DE SECUENCIA DE

### TRANSACCION DEL REGISTRO DE DETALLE

### DE TRANSACCIONES

### M N 7 88 - 94

```
Su valor debe coincidir con las siete últimas
posiciones del campo 11, registro tipo “6”, al cual
hace referencia.
9 RESERVADO N/D Blancos 12 95 - 106 Campo reservado. Este campo debe ir en blancos.
```

```
MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 189 de 329
```
## Para transacciones monetarias crédito generadas por PSE recibidas por EF

```
Registro de Encabezado de Archivo
Para transacciones Monetarias Crédito generadas por PSE recibidas por EF
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción
1 TIPO DE REGISTRO M “1” 1 1 Valor válido para este campo "1".
2 CODIGO DE PRIORIDAD R N 2 2 - 3 Valor válido “01”.
```
```
3 CODIGO ENTIDAD DESTINO INMEDIATO M
bRRRRRTT
TC
```
### 10 4 - 13

```
Código de ACH COLOMBIA (000101006) y dígito
de chequeo
```
```
4 CODIGO ENTIDAD ORIGEN INMEDIATO M bRRRRRTT
TC
10 14 - 23 Código de EF y dígito de cheque
```
### 5 FECHA DE CREACION DEL ARCHIVO M AAAAMM

### DD

```
8 24 - 31 Fecha de creación del archivo.
```
```
6 HORA DE CREACION DEL ARCHIVO O HHMM 4 32 - 35 Hora en la cual es creado el archivo.
```
```
7 IDENTIFICADOR DEL ARCHIVO M A-Z / 0 - 9 1 36 - 36
Identificación de archivos creados en la misma
fecha.
```
```
8 TAMAÑO DEL REGISTRO M ‘106’ 3 37 - 39
Número de caracteres contenidos en cada
registro.
9 FACTOR DE ABLOCAMIENTO M ‘10’ 2 40 - 41 Número de registros dentro de un bloque.
10 CODIGO DE FORMATO M ‘1’ 1 42 - 42 Permite futuras variaciones de formato.
```
```
11
```
### NOMBRE ENTIDAD DESTINO

### INMEDIATO

```
O AN 23 43 - 65 Nombre del ACH (ACH COLOMBIA).
```
```
12 NOMBRE ENTIDAD ORIGEN INMEDIATO O AN 23 66 - 88 Proveedor de Servicios Electrónicos (PSE).
13 CODIGO DE REFERENCIA O AN 8 89 - 96 Código del sistema.
```
```
14 RESERVADO N/D Blancos 10 97 - 106
Campo reservado. Este campo debe ir en
blancos.
```

```
MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31
```
### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A

```
Agosto de 2024
Página 190 de 329
```
```
Registro de Control de Archivo
Para transacciones Monetarias Crédito generadas por PSE recibidas por EF
```
```
# Nombre de Campo Inclusi
ón
Contenido Longitud Posición Descripción
```
```
1 TIPO DE REGISTRO M “9” 1 1 Valor válido para este campo "9".
2 CANTIDAD DE LOTES M N 6 2 - 7 Número de lotes incluidos en el archivo.
```
```
3 NUMERO DE BLOQUES M N 6 8 - 13
Número de bloques físicos en el archivo de 10
registros cada uno.
```
```
4 NUMERO^ DE^ TRANSACCIONES^
DETALLADAS Y DE REGISTROS ADENDA
M N 8 14 - 21 Número^ total^ de^ registros^ de^ detalle^ y^ de^
adenda en el archivo.
```
### 5 TOTALES DE CONTROL M N 10 22 - 31

```
Sumatoria de los códigos de las Entidades
Participantes Receptoras de los Registros de
Detalle de Transacciones.
```
### 6 VALOR TOTAL DE DEBITOS M

### $$$$$$$$

### $$$$$$$$

### 

```
18 32 - 49 Este campo contiene ceros.
```
### 7 VALOR TOTAL DE CREDITOS M

### $$$$$$$$

### $$$$$$$$

### 

```
18 50 - 67 Suma^ de^ valores^ de^ las^ transacciones^ tipo^
crédito del archivo.
```
```
8 RESERVADO N/D Blancos 39 68 - 106 Campo reservado no disponible.
```

```
MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31
```
### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A

```
Agosto de 2024
Página 191 de 329
```
Registro de Encabezado de Lote

Para transacciones Monetarias Crédito generadas por PSE recibidas por EF

# Nombre de Campo Inclusión
Conteni
do Longitud^ Posición^ Descripción^

1 TIPO DE REGISTRO M “5” 1 1 Valor válido para este campo "5”.

2 CODIGO^ CLASE^ DE^ TRANSACCIONES^ POR^
LOTE
M N 3 2 - 4 220 - Créditos

3 NOMBRE DEL USUARIO ORIGINADOR M AN 16 5 - 20 PSE para empresas

4

### DATOS DISCRECIONALES DEL USUARIO

```
ORIGINADOR N/D^ AN^20 21 -^40 Campo^ reservado.^
```
5

### IDENTIFICACION DEL USUARIO

### ORIGINADOR

### M AN 10 41 - 50

```
Número de Identificación de ACH COLOMBIA -
PSE.
```
6 TIPO DE SERVICIO M AN 3 51 - 53 CCD (Cash Concentration or Disbursement)

7 DESCRIPCION DE LOTE M AN 10 54 - 63 PAGOS PSE

8 FECHA DESCRIPTIVA N/D AN 8 64 - 71 Campo reservado.

9 FECHA EFECTIVA DE LA TRANSACCION R

### AAAAM

### MDD

### 8 72 - 79

```
Fecha en la cual ACH COLOMBIA procesará las
transacciones.
```
10 FECHA DE COMPENSACIÓN JULIANA O N 3 80 - 82 Fecha de liquidación de las transacciones.

11 CODIGO^ ESTADO^ DEL^ USUARIO^
ORIGINADOR
M AN 1 83 - 83 Valor válido “1” e indica el estado del PSE

### 12 CODIGO^ ENTIDAD^ PARTICIPANTE^

### ORIGINADORA

### M RRRRRT

### TT

### 8 84 - 91

```
Código de el participante originador (que
autorizó y procesó débitos en línea exitosamente
a través del PSE). Código PSE
```
13 NUMERO DE LOTE M N 7 92 - 98
Secuencial ascendente único para cada lote en
del archivo iniciando en 1.

14 RESERVADO N/D Blancos 8 99 - 106 Campo reservado.


```
MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31
```
### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A

```
Agosto de 2024
Página 192 de 329
```
Registro de Encabezado de Lote

Para transacciones Monetarias Crédito generadas por PSE recibidas por EF

# Nombre de Campo Inclusión
Conteni
do Longitud^ Posición^ Descripción^

Registro de Control de Lote

Para transacciones Monetarias Crédito generadas por PSE recibidas por EF

# Nombre de Campo Inclusión
Conteni
do Longitud^ Posición^ Descripción^

1 TIPO DE REGISTRO M “8” 1 1 Valor válido para este campo "8".

2

### CODIGO CLASE DE TRANSACCIONES POR

### LOTE

```
M N 3 2 - 4 220 - Créditos
```
### 3

### NUMERO DE TRANSACCIONES

### DETALLADAS Y DE REGISTROS ADENDA

### M N 6 5 - 10

```
Número de registros de detalle y de adenda en el
lote.
```
### 4 TOTALES DE CONTROL M N 10 11 - 20

```
Sumatoria de códigos de las Entidades
Participantes Receptoras de los Registros de
Detalle de Transacciones.
```
### 5 VALOR TOTAL DE DEBITOS M

### $$$$$$

### $$$$$$

### $$$$

```
18 21 - 38 Este campo contiene ceros.
```
### 6 VALOR TOTAL DE CREDITOS M

### $$$$$$

### $$$$$$

### $$$$

### 18 39 - 56

```
Suma de valores de las transacciones crédito del
lote.
```
### 7 IDENTIFICACION^ DEL^ USUARIO^

### ORIGINADOR

```
R AN 10 57 - 66 Número^ de^ Identificación^ de^ ACH^ COLOMBIA^ -^
PSE.
```
8 CODIGO^ DE^ AUTENTICACION^ DE^
MENSAJES
O AN 19 67 - 85 Campo Autenticación de Mensajes

9 RESERVADO N/D Blancos 6 86 - 91 Campo reservado no disponible.


```
MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31
```
### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A

```
Agosto de 2024
Página 193 de 329
```
Registro de Encabezado de Lote

Para transacciones Monetarias Crédito generadas por PSE recibidas por EF

# Nombre de Campo Inclusión
Conteni
do Longitud^ Posición^ Descripción^

10

### IDENTIFICACION DE LA ENTIDAD

### PARTICIPANTE ORIGINADORA

### M

### RRRRRT

### TT

### 8 92 - 99

```
Código de el participante originador-indicada en
el registro de Encabezado de Lote.
```
11 NUMERO DEL LOTE M N 7 100 - 106 Número del Lote.

Registro de Detalle de Transacciones

Para transacciones Monetarias Crédito generadas por PSE recibidas por EF

# Nombre de Campo Inclusión Contenid
o

```
Longitud Posición Descripción
```
1 TIPO DE REGISTRO M “6” 1 1 Valor válido para este campo "6".

### 2

### CODIGO DE TRANSACCION

### M

### N

### 2

### 2 - 3

```
Transacción/Tipo
Cta.
```
```
Cuenta
Corriente
```
```
Cuenta de
Transacción Crédito 22 Ahorros 32
```
### 3 CODIGO ENTIDAD PARTICIPANTE RECEPTOR M

### 0RRRRTT

### T

### 8 4 - 11

```
Número de Ruta y Tránsito de la Entidad Financiera
Receptor.
```
4 DIGITO DE CHEQUEO M N 1 12 - 12 Dígito de chequeo correspondiente al campo 3.

### 5 NUMERO^ DE^ CUENTA^ DEL^ USUARIO^

### RECEPTOR

### R

### AN

### 17

### 13 - 29

```
Número de cuenta del Usuario Receptor en la
Entidad Participante Receptor que se extrae de las
Reglas de Negocio del PSE. Solo caracteres
numéricos.
```

```
MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31
```
### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A

```
Agosto de 2024
Página 194 de 329
```
### 6 VALOR DE LA TRANSACCIÓN

### M

### $$$$$$$

### $$$$$$$

### $$$$

### 18

### 30 - 47

```
Valor por pagar o a abonar correspondiente a la
suma total de recaudos exitosos realizados por el
PSE para este cliente o Empresa Pública o Privada.
```
### 7 NUMERO^ DE^ IDENTIFICACION^ DEL^ USUARIO^

### RECEPTOR

### 1

### O

```
AN 15 48 - 62 Número^ de^ Identificación^ del^ Cliente^ Receptor^ o^
Empresa Pública o Privada.
```
### 8 NOMBRE DEL USUARIO RECEPTOR R AN 22 63 - 84

```
Registra el nombre del Usuario e Receptor o
Empresa Pública o Privada.
```
9 DATOS DISCRECIONALES O AN 2 85 - 86 Este campo debe contener “V” o “v” en la primera
posición.
10 INDICADOR DE REGISTRO ADENDA M N 1 87 - 87 Valor válido “1”. No se adicionan registros adenda.

### 11 NUMERO DE SECUENCIA

### M

### N

### 15

### 88 - 102

En las primeras 8 posiciones se debe registrar el
Código de PSE y en las siguientes 7 posiciones, un
consecutivo que inicia en 1.
12 RESERVADO N/D Blancos 4 103 - 106 Campo reservado.


```
MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31
```
### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A

```
Agosto de 2024
Página 195 de 329
```
```
Registro Adenda – Información Adicional
Para transacciones Monetarias Crédito generadas por PSE recibidas por EF
```
```
# Nombre de Campo Inclusión
Conteni
do Longitud^ Posición^ Descripción^
1 TIPO DE REGISTRO M “7” 1 1 Valor válido para este campo "7".
2 CODIGO TIPO DE REGISTRO ADENDA M “05” 2 2 - 3 Valor válido para este campo “05”.
```
```
3 CÓDIGO DEL PSE R N 4 4 - 7
Número de Identificación del proveedor de
servicios electrónicos “1101”
```
```
4 NUMERO DE TRANSACCIONES DEBITADAS M AN 10 8 - 17
Número de transacciones debitadas. Aplica
para un código por cuenta.
5 CODIGO DE SERVICIO M N 10 18 - 27 Código del servicio recaudado.
```
```
6
```
### CODIGO ENTIDAD PARTICIPANTE

### RECEPTOR

### M N 8 28 - 35

```
Código de el participante Receptor donde se
abonará el dinero.
7 NIT DEL RECAUDADOR M AN 16 36 - 51 Código de la Empresa receptora de los pagos.
```
```
8 FECHA DE ABONO M
```
### AAAAM

```
MDD 8 52 -^59 Nit^ del^ Recaudador^
9 CICLO DE ABONO M N 2 60 - 61 Ciclo en el que se realiza el abono.
10 CANAL DE PAGO R AN 2 62 - 63 Canal de pago “15” INTERNET
```
### 11 TIPO DE PAGO M N 2 64 - 65

```
Código para identificar el pago como 01
Recurrente ,02 Compras. Aplica para un tipo
por cuenta.
12 RESERVADO N/D Blancos 18 66 - 83 Campo reservado.
```
```
13 NUMERO^ DE^ SECUENCIA^ DE^ REGISTRO^ DE^
ADENDA
M N 4 84 - 87 Valor válido para este campo “0001”
```

```
MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31
```
### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A

```
Agosto de 2024
Página 196 de 329
```
```
Registro Adenda – Información Adicional
Para transacciones Monetarias Crédito generadas por PSE recibidas por EF
```
```
# Nombre de Campo Inclusión
Conteni
do Longitud^ Posición^ Descripción^
```
### 14

### NUMERO DE SECUENCIA DE

### TRANSACCION DEL REGISTRO DE DETALLE

### DE TRANSACCIONES

### M N 7 88 - 94

```
Su valor debe coincidir con las siete últimas
posiciones del campo 11, registro tipo “6”, al
cual hace referencia.
```
```
15 RESERVADO N/D Blancos 12 95 - 106
Campo reservado. Este campo debe ir en
blancos.
```

```
MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 197 de 329
```
## 6.9. Archivo de pagos NACHA-M Seguridad Social

## 6.9.1. Requerimientos de formato

```
Las transacciones de abono a las cuentas de las Administradoras se incluirán en lotes dentro de los archivos de proceso diario en cada ciclo de
proceso. A continuación, se presenta el detalle de los lotes:
```
Registro de Encabezado de Archivo

Para transacciones Monetarias Crédito generadas por PSE en nombre de las EF

# Nombre de Campo Inclusión Contenido Longitud Posición Descripción

1 TIPO DE REGISTRO M “1” 1 1 Valor válido para este campo "1".

2 CODIGO DE PRIORIDAD R N 2 2 - 3 Valor válido “01”.

3 CODIGO ENTIDAD DESTINO INMEDIATO M
bRRRRRTT
TC 10 4 -^13

```
Código de ACH COLOMBIA (000101006) y dígito de
cheque.
```
4 CODIGO ENTIDAD ORIGEN INMEDIATO M
bRRRRRTT
TC
10 14 - 23 Código de EF y dígito de cheque.

### 5 FECHA DE CREACION DEL ARCHIVO M

### AAAAMM

### DD

```
8 24 - 31 Fecha de creación del archivo.
```
6 HORA DE CREACION DEL ARCHIVO O HHMM 4 32 - 35 Hora en la cual es creado el archivo.

7 IDENTIFICADOR DEL ARCHIVO M A-Z / 0 - 9 1 36 - 36 Identificación^ de^ archivos^ creados^ en^ la^ misma^
fecha.

8 TAMAÑO DEL REGISTRO M ‘106’ 3 37 - 39 Número de caracteres contenidos en cada registro.

9 FACTOR DE ABLOCAMIENTO M ‘10’ 2 40 - 41 Número de registros dentro de un bloque.

10 CODIGO DE FORMATO M ‘1’ 1 42 - 42 Permite futuras variaciones de formato.

11 NOMBRE ENTIDAD DESTINO INMEDIATO O AN 23 43 - 65 Nombre del ACH (ACH COLOMBIA).


```
MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 198 de 329
```
```
Registro de Encabezado de Archivo
Para transacciones Monetarias Crédito generadas por PSE en nombre de las EF
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción
12 NOMBRE ENTIDAD ORIGEN INMEDIATO O AN 23 66 - 88 Proveedor de Servicios Electrónicos (PSE).
13 CODIGO DE REFERENCIA O AN 8 89 - 96 Código del sistema.
14 RESERVADO N/D Blancos 10 97 - 106 Campo reservado. Este campo debe ir en blancos.
```
Registro de Control de Archivo

Para transacciones Monetarias Crédito generadas por PSE en nombre de las EF

# Nombre de Campo Inclusión Contenido Longitud Posición Descripción

1 TIPO DE REGISTRO M “9” 1 1 Valor válido para este campo "9".

2 CANTIDAD DE LOTES M N 6 2 - 7 Número de lotes incluidos en el archivo.

3 NUMERO DE BLOQUES M N 6 8 - 13 Número^ de^ bloques^ físicos^ en^ el^ archivo^ de^10
registros cada uno.

4 NUMERO^ DE^ TRANSACCIONES^
DETALLADAS Y DE REGISTROS ADENDA
M N 8 14 - 21 Número^ total^ de^ registros^ de^ detalle^ y^ de^ adenda^ en^
el archivo.

### 5 TOTALES DE CONTROL M N 10 22 - 31

```
Sumatoria de los códigos de las Entidades
Participantes Receptoras de los Registros de Detalle
de Transacciones.
```
### 6 VALOR TOTAL DE DEBITOS M

### $$$$$$$$

### $$$$$$$$

### 

```
18 32 - 49 Este campo contiene ceros.
```

```
MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 199 de 329
```
Registro de Control de Archivo

Para transacciones Monetarias Crédito generadas por PSE en nombre de las EF

# Nombre de Campo Inclusión Contenido Longitud Posición Descripción

### 7 VALOR TOTAL DE CREDITOS M

### $$$$$$$$

### $$$$$$$$

### 

### 18 50 - 67

```
Suma de valores de las transacciones tipo crédito
del archivo.
```
8 RESERVADO N/D Blancos 39 68 - 106 Campo reservado no disponible.


```
MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 200 de 329
```
Registro de Encabezado de Lote

Para transacciones Monetarias – Sistema de Seguridad Social - ACHsss

# Nombre de Campo Inclusión Contenido Longitud Posición Descripción

1 TIPO DE REGISTRO M “5” 1 1 Valor válido para este campo "5”.

2 CODIGO^ CLASE^ DE^ TRANSACCIONES^ POR^
LOTE
M N 3 2 - 4 220 - Créditos

3 NOMBRE DEL USUARIO ORIGINADOR M AN 16 5 - 20 Nombre del Aportante

4

### DATOS DISCRECIONALES DEL USUARIO

### ORIGINADOR

```
O AN 20 21 - 40 Campo reservado.
```
### 5

### IDENTIFICACION DEL USUARIO

### ORIGINADOR

```
M AN 10 41 - 50 Número de Identificación del Aportante.
```
6 TIPO DE SERVICIO M AN 3 51 - 53 CCD

7 DESCRIPCION DE LOTE M AN 10 54 - 63 SSS

8 FECHA DESCRIPTIVA N/D AN 8 64 - 71 Campo reservado.

9 FECHA EFECTIVA DE LA TRANSACCION R

### AAAAMM

### DD

### 8 72 - 79

```
Fecha en la cual ACH COLOMBIA procesará las
transacciones.
```
10 FECHA DE COMPENSACIÓN JULIANA O N 3 80 - 82 Fecha de liquidación de las transacciones.

11

### CODIGO ESTADO DEL USUARIO

```
ORIGINADOR M^ AN^1 83 -^83 Valor^ válido^ “1”^
```
12

### CODIGO ENTIDAD PARTICIPANTE

```
ORIGINADORA M^ N^8 84 -^91 Código^ de^ la^ Entidad^ Originadora.^
```
13 NUMERO DE LOTE M N 7 92 - 98
Secuencial ascendente único para cada lote en del
archivo iniciando en 1.

14 RESERVADO N/D Blancos 8 99 - 106 Campo reservado.


```
MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 201 de 329
```
Registro de Control de Lote

Para transacciones Monetarias Crédito – Sistema de Seguridad Social

# Nombre de Campo Inclusión Contenido Longitud Posición Descripción

1 TIPO DE REGISTRO M “8” 1 1 Valor válido para este campo "8".

2 CODIGO^ CLASE^ DE^ TRANSACCIONES^ POR^
LOTE
M N 3 2 - 4 220 - Créditos

### 3

### NUMERO DE TRANSACCIONES DETALLADAS

### Y DE REGISTROS ADENDA M^ N^6 5 -^10

```
Número de registros de detalle y de adenda en el
lote.
```
### 4 TOTALES DE CONTROL M N 10 11 - 20

```
Sumatoria de códigos de las Entidades
Participantes Receptoras de los Registros de
Detalle de Transacciones.
```
### 5 VALOR TOTAL DE DEBITOS M

### $$$$$$$$

### $$$$$$$$

### 

```
18 21 - 38 Este campo contiene ceros.
```
### 6 VALOR TOTAL DE CREDITOS M

### $$$$$$$$

### $$$$$$$$

### 

### 18 39 - 56

```
Suma de valores de las transacciones crédito del
lote.
```
### 7

### IDENTIFICACION DEL USUARIO

```
ORIGINADOR R^ AN^10 57 -^66 Número^ de^ Identificación^ de^ ACH^ COLOMBIA^ -^ PSE.^
```
8 CODIGO DE AUTENTICACION DE MENSAJES O AN 19 67 - 85 Campo reservado para un algoritmo de seguridad.

9 RESERVADO N/D Blancos 6 86 - 91 Campo reservado no disponible.

10 IDENTIFICACION^ DE^ LA^ ENTIDAD^
PARTICIPANTE ORIGINADORA
M 0RRRRTTT 8 92 - 99 Código^ de^ el^ participante^ originador-indicada en^ el^
registro de Encabezado de Lote.

11 NUMERO DEL LOTE M N 7 100 - 106 Número del Lote.


### MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 202 de 329
```
Registro de Detalle de Transacciones

Para transacciones Monetarias Crédito – Sistema de Seguridad Social - ACHsss

# Nombre de Campo Inclusión Contenido Longitud Posición Descripción

1 TIPO DE REGISTRO M “6” 1 1 Valor válido para este campo "6".

### 2 CODIGO DE TRANSACCION M N 2 2 - 3

```
Cta. Corriente 22 /Cta. Ahorros 32 Corresponde al
tipo de cuenta seleccionado por la Administradora
en el registro.
```
### 3

### CODIGO ENTIDAD PARTICIPANTE

### RECEPTOR

### M N 8 4 - 11

```
Número de Ruta y Tránsito de el participante
Receptor. Corresponde al código de la EF
seleccionado por la Administradora en el proceso de
registro.
```
4 DIGITO DE CHEQUEO M N 1 12 - 12 Dígito de chequeo correspondiente al campo 3.

5

### NUMERO DE CUENTA DEL USUARIO

### RECEPTOR R^ AN^17 13 -^29

```
Corresponde al número de cuenta seleccionado por
la Administradora en el registro.
```
### 6 VALOR DE LA TRANSACCIÓN M

### $$$$$$$$

### $$$$$$$$

### 

### 18 30 - 47

```
Valor por pagar o abonar enviado por el Operador
de Información correspondiente al valor a abonar
por cada aportante a cada Administradora en cada
una de las transacciones de planilla de liquidación
del SSS.
```
7 NUMERO^ DE^ IDENTIFICACION^ DEL^ USUARIO^
RECEPTOR
O AN 15 48 - 62 Número^ de^ identificación^ de^ la^ Administradora^
receptora de los pagos.

8 NOMBRE DEL CLIENTE RECEPTOR R AN 22 63 - 84 Nombre^ de^ la^ Administradora^ Receptora^ de^ los^
pagos.

9 DATOS DISCRECIONALES O AN 2 85 - 86
Este campo debe contener “V” o “v” en la primera
posición.

10 INDICADOR DE REGISTRO ADENDA M N 1 87 - 87
Valor válido “1”. Se debe adicionar un registro de
adenda.


### MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 203 de 329
```
Registro de Detalle de Transacciones

Para transacciones Monetarias Crédito – Sistema de Seguridad Social - ACHsss

# Nombre de Campo Inclusión Contenido Longitud Posición Descripción

### 11 NUMERO DE SECUENCIA M N 15 88 - 102

```
En las primeras 8 posiciones se debe registrar el
Código de la Entidad Originadora y en las siguientes
7 posiciones, un consecutivo que inicia en “1”.
```
12 RESERVADO N/D Blancos 4 103 - 106 Campo reservado.

Registro Adenda – Información Adicional

Para transacciones de: Monetarias Crédito del Sistema de Seguridad Social

# Nombre de Campo Inclusión Contenido Longitud Posición Descripción

1 TIPO DE REGISTRO M “7” 1 1 Valor válido para este campo "7".

2 CODIGO TIPO DE REGISTRO ADENDA M “05” 2 2 - 3 Valor válido para este campo “05”.

3 CÓDIGO DEL OPERADOR DE INFORMACION M N 2 4 - 5 Número^ de^ Identificación^ del^ Operador^ de^
Información o proveedor de tecnología.

4 NUMERO DE PLANILLA DE LIQUIDACIÓN M AN 15 6 - 20
Número de la Planilla de Liquidación utilizada por el
Aportante al realizar el débito en el SSS.

5 NUMERO DE REGISTROS DE LA PLANILLA M N 6 21 - 26
Número de registros o empleados enviados a la
Administradora en la Planilla de Liquidación.

6

### CODIGO ENTIDAD PARTICIPANTE

### ORIGINADORA

### M N 8 27 - 34

```
Código de el participante originador de donde el
Aportante realizó el débito en el SSS.
```
7 CÓDIGO DE LA ADMINISTRADORA M AN 6 35 - 40
Código de la Administradora receptora de los pagos
del Sistema de Seguridad Social.

8 NIT DEL APORTANTE M AN 16 41 - 56 Nit del Aportante

9 PERIODO DE PAGO M AAAAMM 6 57 - 62 Periodo de pago de la planilla.


### MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 204 de 329
```
Registro Adenda – Información Adicional

Para transacciones de: Monetarias Crédito del Sistema de Seguridad Social

# Nombre de Campo Inclusión Contenido Longitud Posición Descripción

10 CANAL DE PAGO M AN 2 63 - 64 Canal de pago de la planilla. Ver Tabla 1.

11 RESERVADO N/D Blancos 19 65 - 83 Campo reservado.

12

### NUMERO DE SECUENCIA DE REGISTRO DE

```
ADENDA M^ N^4 84 -^87 Valor^ válido^ para^ este^ campo^ “0001”^
```
### 13

### NUMERO DE SECUENCIA DE

### TRANSACCION DEL REGISTRO DE DETALLE

### DE TRANSACCIONES

### M N 7 88 - 94

```
Su valor debe coincidir con las siete últimas
posiciones del campo 11, registro tipo “6”, al cual
hace referencia.
```
14 RESERVADO N/D Blancos 12 95 - 106 Campo reservado. Este campo debe ir en blancos.


### MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 205 de 329
```
Tabla 1. Tabla de Canales de Pago

```
CÓDIGO DESCRIPCIÓN
1 POR VENTANILLA EN EFECTIVO
2 POR VENTANILLA EN CHEQUE
3 POR BUZÓN DE AUTOSERVICIO
11 DÉBITO EN CUENTA POR SISTEMA DE AUDIORRESPUESTA
12 DÉBITO EN CUENTA POR CAJERO ELECTRÓNICO
13 DÉBITO EN CUENTA POR DATÁFONO
14 DÉBITO EN CUENTA POR DOMICILIACIÓN
15 DÉBITO EN CUENTA POR INTERNET
21 TARJETA CRÉDITO POR SISTEMA DE AUDIORRESPUESTA
22 TARJETA CRÉDITO POR CAJERO ELECTRÓNICO
23 TARJETA CRÉDITO POR DATÁFONO
24 TARJETA CRÉDITO POR DOMICILIACIÓN
25 TARJETA CRÉDITO POR INTERNET
29 CORRESPONSAL BANCARIO
```

### MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 206 de 329
```
## 6.10. Ficha Técnica NACHA-M (DIAN)

```
Registro de Encabezado de Archivo
Para transacciones Monetarias Crédito generadas por PSE recibidas por EF DIAN
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción
1 TIPO DE REGISTRO M “1” 1 1 Valor válido para este campo "1".
2 CODIGO DE PRIORIDAD R N 2 2 - 3 Valor válido “01”.
```
```
3 CODIGO ENTIDAD DESTINO INMEDIATO M
bRRRRRTT
T C
```
### 10 4 - 13

```
Código de el participante Receptor. Y Dígito de
chequeo
```
```
4 CODIGO ENTIDAD ORIGEN INMEDIATO M
bRRRRRTT
T C
```
### 10 14 - 23

```
Código de ACH COLOMBIA (000101006). y Dígito
de chequeo
```
```
5 FECHA DE CREACION DEL ARCHIVO M AAAAMM
DD
8 24 - 31 Fecha de creación del archivo.
```
```
6 HORA DE CREACION DEL ARCHIVO O HHMM 4 32 - 35 Hora en la cual es transmitido o creado el archivo.
```
```
7 IDENTIFICADOR DEL ARCHIVO M A-Z / 0 - 9 1 36 - 36
Identificación de archivos creados en la misma
fecha.
```
```
8 TAMAÑO DEL REGISTRO M ‘106’ 3 37 - 39
Número de caracteres contenidos en cada
registro.
9 FACTOR DE ABLOCAMIENTO M ‘10’ 2 40 - 41 Número de registros dentro de un bloque.
10 CODIGO DE FORMATO M ‘1’ 1 42 - 42 Permite futuras variaciones de formato.
11 NOMBRE ENTIDAD DESTINO INMEDIATO O AN 23 43 - 65 Nombre del ACH (ACH COLOMBIA).
12 NOMBRE ENTIDAD ORIGEN INMEDIATO O AN 23 66 - 88 Nombre del participante originador.
13 CODIGO DE REFERENCIA O AN 8 89 - 96 Código del sistema.
14 RESERVADO N/D Blancos 10 97 - 106 Campo reservado. Este campo debe ir en blancos.
```

### MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A

```
Agosto de 2024
Página 207 de 329
```
Registro de Control de Archivo

Para transacciones Monetarias Crédito generadas por PSE recibidas por EF DIAN

# Nombre de Campo Inclusión Contenido Longitud Posición Descripción

1 TIPO DE REGISTRO M “9” 1 1 Valor válido para este campo "9".

2 CANTIDAD DE LOTES M N 6 2 - 7 Número de lotes incluidos en el archivo.

3 NUMERO DE BLOQUES M N 6 8 - 13
Número de bloques físicos en el archivo de 10
registros cada uno.

4

### NUMERO DE TRANSACCIONES DETALLADAS

### Y DE REGISTROS ADENDA

### M N 8 14 - 21

```
Número total de registros de detalle y de adenda
en el archivo.
```
### 5 TOTALES DE CONTROL M N 10 22 - 31

```
Sumatoria de los códigos de las Entidades
Participantes Receptoras de los
Registros de Detalle de Transacciones.
```
### 6 VALOR TOTAL DE DEBITOS M

### $$$$$$$$

### $$$$$$$$

### $$

### 18 32 - 49

```
Suma de valores de las transacciones tipo débito
del archivo.
```
### 7 VALOR TOTAL DE CREDITOS M

### $$$$$$$$

### $$$$$$$$

### $$

### 18 50 - 67

```
Suma de valores de las transacciones tipo crédito
del archivo.
```
8 RESERVADO N/D Blancos 39 68 - 106 Campo reservado no disponible.


### MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 208 de 329
```
Se genera un lote por cada uno de los servicios (PSE-Empresas, Seguridad Social - PSE, Seguridad Social y Dian - Planilla Asistida), en total se debe
generar máximo cuatro lotes por cada archivo de pagos.

```
Registro de Encabezado de Lote
Para transacciones Monetarias Crédito generadas por PSE recibidas por EF DIAN
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción
1 TIPO DE REGISTRO M “5” 1 1 Valor válido para este campo "5”.
```
```
2
```
### CODIGO CLASE DE TRANSACCIONES POR

```
LOTE M^ N^3 2 -^4 220 -^ Créditos^
3 NOMBRE^ DEL^ USUARIO^ ORIGINADOR^ M AN 16 5 - 20 PSE para pagos de la DIAN
```
```
4
```
### DATOS DISCRECIONALES DEL USUARIO

### ORIGINADOR

```
N/D AN 20 21 - 40 Campo reservado.
```
### 5 IDENTIFICACION DEL USUARIO ORIGINADOR M AN 10 41 - 50

```
Número de Identificación de ACH COLOMBIA -
PSE.
6 TIPO DE SERVICIO M AN 3 51 - 53 CCD
7 DESCRIPCION DE LOTE M AN 10 54 - 63 PAGOS DIAN
8 FECHA DESCRIPTIVA N/D AN 8 64 - 71 Campo reservado.
```
```
9 FECHA EFECTIVA DE LA TRANSACCION R AAAAM
MDD
8 72 - 79 Fecha^ en^ la^ cual^ las^ Entidad^ Participante^ Receptor^
deben aplicar las transacciones del lote.
10 FECHA DE COMPENSACIÓN JULIANA O N 3 80 - 82 Fecha de liquidación de las transacciones.
```
```
11 CODIGO ESTADO DEL USUARIO ORIGINADOR M
```
### A

### N 1 83 -^83

```
Valor válido “1” e indica el estado del usuario
Originador.
```
```
12
```
### CODIGO ENTIDAD PARTICIPANTE

### ORIGINADORA

### M

### RRRRRTT

### T

```
8 84 - 91 Código de el participante originador.
```
### 13 NUMERO DE LOTE M N 7 92 - 98

```
Secuencial ascendente único para cada lote en del
archivo iniciando en 1.
```

### MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 209 de 329
```
Registro de Encabezado de Lote

Para transacciones Monetarias Crédito generadas por PSE recibidas por EF DIAN

# Nombre de Campo Inclusión Contenido Longitud Posición Descripción

### 14 RESERVADO

### N

### /

### D

```
Blancos 8 99 - 106 Campo reservado.
```

### MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A

```
Agosto de 2024
Página 210 de 329
```
Registro de Control de Lote

Para transacciones Monetarias Crédito generadas por PSE recibidas por EF DIAN

# Nombre de Campo Inclusión Contenido Longitud Posición Descripción

1 TIPO DE REGISTRO M “8” 1 1 Valor válido para este campo "8".

2 CODIGO^ CLASE^ DE^ TRANSACCIONES^ POR^
LOTE
M N 3 2 - 4 220 - Créditos

### 3

### NUMERO DE TRANSACCIONES

### DETALLADAS Y DE REGISTROS ADENDA M^ N^6 5 -^10

```
Número de registros de detalle y de adenda en el
lote.
```
### 4 TOTALES DE CONTROL M N 10 11 - 20

```
Sumatoria de códigos de las Entidades Participantes
Receptoras de los Registros de Detalle de
Transacciones.
```
### 5 VALOR TOTAL DE DEBITOS M

### $$$$$$$$

### $$$$$$$$

### 

```
18 21 - 38 Este campo contiene ceros.
```
### 6 VALOR TOTAL DE CREDITOS M

### $$$$$$$$

### $$$$$$$$

### 

### 18 39 - 56

```
Suma de valores de las transacciones crédito del
lote.
```
### 7

### IDENTIFICACION DEL USUARIO

```
ORIGINADOR R^ AN^10 57 -^66 Número^ de^ Identificación^ de^ ACH^ COLOMBIA^ -^ PSE.^
```
8

### CODIGO DE AUTENTICACION DE

```
MENSAJES O^ AN^19 67 -^85 Campo^ Autenticación^ de^ Mensajes^
```
9 RESERVADO N/D Blancos 6 86 - 91 Campo reservado no disponible.

10

### IDENTIFICACION DE LA ENTIDAD

### PARTICIPANTE ORIGINADORA

### M RRRRRTTT 8 92 - 99

```
Código de el participante originador-indicada en el
registro de Encabezado de Lote.
```
11 NUMERO DEL LOTE M N 7 100 - 106 Número del Lote.


### MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A

```
Agosto de 2024
Página 211 de 329
```
```
Registro de Detalle de Transacciones
Para transacciones Monetarias Crédito generadas por PSE recibidas por EF DIAN
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción
1 TIPO DE REGISTRO M “6” 1 1 Valor válido para este campo "6".
```
### 2 CODIGO DE TRANSACCION M N 2 2 - 3

```
Transacción / Tipo
CTA
CTA Ahorros CTA Corriente
```
```
Transacción
Crédito 32 22
```
```
3
```
### CODIGO ENTIDAD PARTICIPANTE

### RECEPTOR

```
M RRRRRTTT 8 4 - 11 Número^ de^ Ruta^ y^ Tránsito^ de^ participante^
receptor.
4 DIGITO DE CHEQUEO M 5 1 12 - 12 Dígito de chequeo correspondiente al campo 3.
```
```
5 NUMERO^ DE^ CUENTA^ DEL^ USUARIO^
RECEPTOR
R AN 17 13 - 29 Número de cuenta del usuario receptor.
```
### 6 VALOR DE LA TRANSACCIÓN M

### $$$$$$$$

### $$$$$$$$

### 

```
18 30 - 47 Valor total de la transacción
```
### 7 NUMERO^ DE^ IDENTIFICACION^ DEL^

### USUARIO RECEPTOR

```
R AN 15 48 - 62 Identificación del usuario receptor.
```
```
8 NOMBRE DEL USUARIO RECEPTOR R AN 22 63 - 84 Nombre del usuario receptor.
9 DATOS DISCRECIONALES N/D AN 2 85 - 86 Campo reservado.
```
### 10 INDICADOR DE REGISTRO ADENDA M N 1 87 - 87

```
Valor válido “1”. Se debe adicionar un registro de
adenda.
```

### MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A

```
Agosto de 2024
Página 212 de 329
```
### 11 NUMERO DE SECUENCIA M N 15 88 - 102

```
En las primeras 8 posiciones se debe registrar el
Código de el participante originador y en las
siguientes 7 posiciones, un consecutivo que inicia
en el límite inferior del rango reservado por el
participante originador para el PSE y que va
máximo hasta el límite superior en un día de
proceso.
12 RESERVADO N/D Blancos 4 103 - 106 Campo reservado.
```

### MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 213 de 329
```
Registro Adenda – Información Adicional

Para transacciones Monetarias Crédito generadas por PSE recibidas por EF DIAN

# Nombre de Campo Inclusión Contenido Longitud Posición Descripción

1 TIPO DE REGISTRO M “7” 1 1 Valor válido para este campo "7".

2 CODIGO TIPO DE REGISTRO ADENDA M “05” 2 2 - 3 Valor válido para este campo “05”.

3 CÓDIGO DEL PSE R N 4 4 - 7
Número de Identificación del proveedor de
servicios electrónicos “1101”

4 NUMERO DE TRANSACCIONES DEBITADAS M AN 10 8 - 17
Número de transacciones debitadas. Aplica para
un código por cuenta.

5 CODIGO DE SERVICIO M N 10 18 - 27 Código del servicio recaudado.

6 CODIGO ENTIDAD PARTICIPANTE RECEPTOR M N 8 28 - 35 Código^ de^ el^ participante^ Receptor^ donde^ se^
abonará el dinero.

7 NIT DEL RECAUDADOR M AN 16 36 - 51 Nit del Recaudador

8 FECHA DE ABONO M

### AAAAMM

```
DD 8 52 -^59 Fecha^ de^ Abono^
```
9 CICLO DE ABONO M N 2 60 - 61 Ciclo en el que se realiza el abono.

10 CANAL DE PAGO R AN 2 62 - 63 Canal de pago “15” INTERNET

### 11 TIPO DE PAGO M N 2 64 - 65

```
Código para identificar el pago como 01
Recurrente ,02 Compras. Aplica para un tipo por
cuenta.
```
12 RESERVADO N/D Blancos 18 66 - 83 Campo reservado.

### 13

### NUMERO DE SECUENCIA DE REGISTRO DE

### ADENDA

### M N 4 84 - 87

```
Valor válido para este campo “0001”
```

### MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 214 de 329
```
Registro Adenda – Información Adicional

Para transacciones Monetarias Crédito generadas por PSE recibidas por EF DIAN

# Nombre de Campo Inclusión Contenido Longitud Posición Descripción

### 14

### NUMERO DE SECUENCIA DE

### TRANSACCION DEL REGISTRO DE DETALLE

### DE TRANSACCIONES

### M N 7 88 - 94

```
Su valor debe coincidir con las siete últimas
posiciones del campo 11, registro tipo “6”, al cual
hace referencia.
```
15 RESERVADO N/D Blancos 12 95 - 106 Campo reservado. Este campo debe ir en blancos.


### SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 215 de 329
```
## 7. TARIFAS

Las tarifas que cobra ACH COLOMBIA a los participantes por las Transferencias ACH se encuentran estipuladas
en el Tarifario del año correspondiente. El esquema de tarifas se actualizará mínimo una vez al año, de acuerdo
con las definiciones bajo el modelo de gobierno de ACH COLOMBIA, comprendido por el comité de tarifas
evaluador y comité de tarifas aprobador. Así mismo, ACH COLOMBIA comunicará oportunamente a todos los
niveles interesados en las Entidades Participantes el Esquema de Tarifas aprobado.

## 8. ANEXOS

## ANEXO 1. FUNCIONALIDADES SISTEMA INTEGRA ACH Información de carácter Confidencial

Para profundizar en las funcionalidades del sistema Integra ACH para los usuarios de las Entidades
Participantes, diríjase al Manual Funcional de Entidades Financieras Versión


```
MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 3 1
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 216 de 329
```
## ANEXO 2. FUNCIONES ASOCIADAS AL PERFIL INTEGRA ACH

Cuando una Entidad Financiera está vinculada al servicio de transferencias interbancarias y PSE, cuenta con los siguientes roles y permisos:

```
Menú
Principal
Mein menú
```
```
Opción
Desplegada
Deployed
Option
```
```
Administrador de
Usuarios
UserAdminoperator
```
```
Administrador
AdminAprover
```
```
Operador
Adminoperator
```
```
Tesorería
TreasuryOperator
```
```
Consultas
QueriesOperator
```
```
Auditor
AuditorOperator
```
```
Inicio
(Start)
```
```
Perfil de inicio
(Home
ProArchivo)
```
```
Leer Leer Leer Leer Leer Leer
```
```
Mi perfil
(My
ProArchivo)
```
```
Leer Leer Leer Leer Leer Leer
```
```
Módulo de
Transferencias
(Payment
Module)
```
```
Tablero de
Pagos
(Pagos
Dashboard)
```
```
Los permisos para
tener acceso de
visualización y
exportación al
tablero de pagos se
darán de acuerdo
con los permisos
asignados a la
pantalla todos los
pagos.
```
```
Los permisos para
tener acceso de
visualización y
exportación al
tablero de pagos
se darán de
acuerdo con los
permisos
asignados a la
pantalla todos los
pagos.
```
```
Los permisos para
tener acceso de
visualización y
exportación al
tablero de pagos
se darán de
acuerdo con los
permisos
asignados a la
pantalla todos los
pagos.
```
```
Los permisos
para tener
acceso de
visualización y
exportación al
tablero de pagos
se darán de
acuerdo con los
permisos
asignados a la
pantalla todos
los pagos.
```
```
Los permisos para
tener acceso de
visualización y
exportación al
tablero de pagos
se darán de
acuerdo con los
permisos
asignados a la
pantalla todos los
pagos.
```
```
Los permisos para
tener acceso de
visualización y
exportación al
tablero de pagos
se darán de
acuerdo con los
permisos
asignados a la
pantalla todos los
pagos.
```
```
Todos los
Pagos
(All Pagos)
```
### -

```
Leer
```
- Eliminar pago
- Exportar (Incluye
Imprimir tablero
de pagos)

```
Leer
```
- Exportar -^

```
Leer
```
- Exportar (Incluye
    Imprimir tablero
       de pagos)

```
Leer
```
- Exportar (Incluye
    Imprimir tablero
       de pagos)


```
MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 3 1
```
### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A

```
Agosto de 2024
Página 217 de 329
```
```
Todas las
Transacciones
(All
Transactions)
```
### -

```
Leer
```
- Transacciones
    ACH_Leer
- Transacciones
    ACH_ Exportar
- Transacciones
Enviadas_Leer
- Transacciones
Enviadas_Exportar
- Transacciones
Recibidas_Leer
- Transacciones
Recibidas_Exportar
- Transacciones
SOI_Leer
- Transacciones
SOI_Exportar

```
Leer
```
- Transacciones
    ACH_Leer
- Transacciones
    ACH_ Exportar
- Transacciones
Enviadas_Leer
- Transacciones
Enviadas_Exportar
- Transacciones
Recibidas_Leer
- Transacciones
Recibidas_Exportar
- Transacciones
SOI_Leer
- Transacciones
SOI_Exportar

```
Se debe retirar
los permisos de
leer y
Transacciones
recibidas
```
```
Leer
```
- Transacciones
    ACH_Leer
- Transacciones
    ACH_ Exportar
- Transacciones
Enviadas_Leer
- Transacciones
Enviadas_Exportar
- Transacciones
Recibidas_Leer
- Transacciones
Recibidas_Exportar
- Transacciones
SOI_Leer
- Transacciones
SOI_Exportar

```
Leer
```
- Transacciones
    ACH_Leer
- Transacciones
    ACH_ Exportar
- Transacciones
Enviadas_Leer
- Transacciones
Enviadas_Exportar
- Transacciones
Recibidas_Leer
- Transacciones
Recibidas_Exportar
- Transacciones
SOI_Leer
- Transacciones
SOI_Exportar

```
Tablero de
Instrucciones
(Instructions
Dashboard)
```
```
Los permisos para
tener acceso de
visualización y
exportación al
tablero de
Instrucciones se
darán de acuerdo
con los permisos
asignados a la
pantalla
Instrucciones
Recibidas
```
```
Los permisos para
tener acceso de
visualización y
exportación al
tablero de
Instrucciones se
darán de acuerdo
con los permisos
asignados a la
pantalla
Instrucciones
Recibidas
```
```
Los permisos para
tener acceso de
visualización y
exportación al
tablero de
Instrucciones se
darán de acuerdo
con los permisos
asignados a la
pantalla
Instrucciones
Recibidas
```
```
Los permisos
para tener
acceso de
visualización y
exportación al
tablero de
Instrucciones se
darán de acuerdo
con los permisos
asignados a la
pantalla
Instrucciones
Recibidas
```
```
Los permisos para
tener acceso de
visualización y
exportación al
tablero de
Instrucciones se
darán de acuerdo
con los permisos
asignados a la
pantalla
Instrucciones
Recibidas
```
```
Los permisos para
tener acceso de
visualización y
exportación al
tablero de
Instrucciones se
darán de acuerdo
con los permisos
asignados a la
pantalla
Instrucciones
Recibidas
```

```
MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 3 1
```
### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A

```
Agosto de 2024
Página 218 de 329
```
```
Instrucciones
Recibidas
(Received
Instructions)
```
```
Leer
Pantalla principal:
```
- Eliminar Archivo
    - Exportar
- Descargar
Al seleccionar una
transacción:
- Exportar Registro
Exportar Detalle
- Exportar Logs
- Aceptar archivo
- Aprobar Límites

```
Leer
Pantalla principal:
Forzar Rechazo
Exportar
Al seleccionar una
transacción:
```
- Exportar Registro
    Exportar Detalle
       - Exportar Logs
- Aceptar archivo
- Subir Archivo

### -

```
Leer
```
- Exportar logs

```
Subir Archivo
(Archivo
Upload)
```
```
Subir Archivo: Los
permisos para
tener acceso de
visualización y
cargue de archivo
en la pantalla Subir
Archivo se darán
desde los permisos
asignados en la
pantalla
Instrucciones
Recibidas.
Nota: La acción de
subir archivo se
debe realizar
```
```
Subir Archivo: Los
permisos para
tener acceso de
visualización y
cargue de archivo
en la pantalla Subir
Archivo se darán
desde los permisos
asignados en la
pantalla
Instrucciones
Recibidas.
Nota: La acción de
subir archivo se
debe realizar
```
```
Subir Archivo: Los
permisos para
tener acceso de
visualización y
cargue de archivo
en la pantalla Subir
Archivo se darán
desde los permisos
asignados en la
pantalla
Instrucciones
Recibidas.
Nota: La acción de
subir archivo se
debe realizar
```
```
Subir Archivo:
Los permisos
para tener
acceso de
visualización y
cargue de
archivo en la
pantalla Subir
Archivo se darán
desde los
permisos
asignados en la
pantalla
Instrucciones
Recibidas.
```
```
Subir Archivo: Los
permisos para
tener acceso de
visualización y
cargue de archivo
en la pantalla Subir
Archivo se darán
desde los permisos
asignados en la
pantalla
Instrucciones
Recibidas.
Nota: La acción de
subir archivo se
debe realizar
```
```
Subir Archivo: Los
permisos para
tener acceso de
visualización y
cargue de archivo
en la pantalla Subir
Archivo se darán
desde los permisos
asignados en la
pantalla
Instrucciones
Recibidas.
Nota: La acción de
subir archivo se
debe realizar
```

```
MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 3 1
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 219 de 329
```
```
directamente en la
pantalla de Subir
Archivo
```
```
directamente en la
pantalla de Subir
Archivo
```
```
directamente en la
pantalla de Subir
Archivo
```
```
Nota: La acción
de subir archivo
se debe realizar
directamente en
la pantalla de
Subir Archivo
```
```
directamente en la
pantalla de Subir
Archivo
```
```
directamente en la
pantalla de Subir
Archivo
```
```
Instrucciones
Distribuidas
(Distributed
Instructions)
```
### - -

```
Leer
```
- Exportar
- Descargar

### - - -

Reportes de
Compensación
(Clearing
Report)

```
Planilla de
Compensación
(Balance
Screen)
```
### -

```
Leer
Exportar
```
```
Leer
Exportar
```
```
Leer
Exportar
```
### -

```
Leer
Exportar
```
```
Usuarios y
Roles
(Users and
Roles)
```
```
Gestión de
Usuarios
(User
Management)
```
```
Leer
```
- Crear
- Exportar
- Actualizar

### - - - - -

```
Gestión de
Roles
(Role
Management)
```
```
Leer
```
- Exportar

### - - - - -

```
Desbloqueo
de Usuarios
(Unlock users)
```
```
Leer
```
- Actualizar -^ -^ -^ -^ -^


```
MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 3 1
```
### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A

```
Agosto de 2024
Página 220 de 329
```
```
Reportes
Reportes de
estadística
```
### -

```
Leer
Diario
Acumulado
Archivo Salida
Descripción de
Lote
Exportar
```
```
Leer
Diario
Acumulado
Archivo Salida
Descripción de
Lote
Exportar
```
### - - -

```
Catálogos
(Add ons)
```
```
Límites
(Limits)
```
### -

```
Leer
Exportar
```
```
Leer
Exportar
```
### - - -

```
Límite por
Banco
(Limits per
bank)
```
### -

```
Leer
```
- Exportar
- Actualizar

```
Leer - - -
```
```
Aprobación de
cuentas
(Accounts
Aprobar)
```
### -

```
Leer
```
- Actualizar
    - Exportar

### - - - -

```
Inscripción de
Cuentas
(Register
Accounts)
```
- Leer

```
Leer
```
- Crear
- Actualizar
- Eliminar
- Exportar
- Reactivar

### - - -

```
Solicitud
Tiempo Extra
(Cycle
Extension)
```
### -

```
Leer
```
- Crear
- Exportar
- Actualizar
- Eliminar

```
Leer
```
- Crear
- Exportar
- Actualizar
- Eliminar

### - - -


```
MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 3 1
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 221 de 329
```
```
Mensajes
Recibidos
(Inbox)
```
```
Leer
Exportar
```
```
Leer
Exportar
```
```
Leer
Exportar
```
```
Leer
Exportar
```
```
Leer
Exportar
```
```
Leer
Exportar
```
```
Logs de
eventos
```
```
Leer
Exportar
```
### - - - -

```
Leer
Exportar
```
Cuando una Entidad Financiera está vinculada únicamente al servicio PSE, cuenta con los siguientes roles y permisos:

```
Menú
Principal
Main menú
```
```
Opción Desplegada
Deployed Option
```
```
Administrador de
Usuarios
(UserAdminOperator)
```
```
Operador PSE
(Operator)
```
```
Tesorería
(TreasuryOperator)
```
```
Consultas
(QueriesOperator)
```
```
Auditor
(AuditorOperator)
```
```
Inicio
(Start)
```
```
Perfil de inicio
(Home ProArchivo)
Leer Leer Leer Leer Leer
```
```
Mi perfil
(My ProArchivo)
Leer Leer Leer Leer Leer
```

```
MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 3 1
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 222 de 329
```
**Módulo de
Transferencias**

```
Todas las
Transacciones
(All Transactions)
```
### -

```
Leer
```
- Transacciones
    ACH_Leer
- Transacciones
    ACH_ Exportar
- Transacciones
Enviadas_Leer
- Transacciones
Enviadas_Exportar
- Transacciones
Recibidas_Leer
- Transacciones
Recibidas_Exportar
- Transacciones
SOI_Leer
- Transacciones
SOI_Exportar

### -

```
Leer
```
- Transacciones
    ACH_Leer
- Transacciones
    ACH_ Exportar
- Transacciones
Enviadas_Leer
- Transacciones
Enviadas_Exportar
- Transacciones
Recibidas_Leer
- Transacciones
Recibidas_Exportar
- Transacciones
SOI_Leer
- Transacciones
SOI_Exportar

```
Leer
```
- Transacciones
    ACH_Leer
- Transacciones ACH_
Exportar
- Transacciones
Enviadas_Leer
- Transacciones
Enviadas_Exportar
- Transacciones
Recibidas_Leer
- Transacciones
Recibidas_Exportar
- Transacciones
SOI_Leer
- Transacciones
SOI_Exportar

```
Instrucciones
Distribuidas
(Distributed
Instructions)
```
```
Leer
```
- Exportar
- Descargar

```
Usuarios y
Roles
(Users and
Roles)
```
```
Gestión de Usuarios
(User Management)
```
```
Leer
```
- Crear
- Exportar
- Actualizar

### - - - -

```
Desbloqueo de
Usuarios
(Unlock users)
```
```
Leer
```
- Actualizar -^ -^ -^ -^


```
MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 3 1
```
### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A

```
Agosto de 2024
Página 223 de 329
```
```
Roles y Permisos
(Roles & Permissions)
```
```
Leer
```
- Exportar

### - - - -

Reportes de
Compensación
(Clearing
Report)

```
Planilla de
Compensación
(Balance Screen)
```
- Leer^
    - Exportar

```
Leer
```
- Exportar
    - Leer^
       - Exportar

```
Reportes
(Reports)
```
```
Reporte de
Estadísticas
(Statistics Report)
```
### -

```
Leer
Diario
Acumulado
Archivo Salida
Descripción de
Lote
Exportar
```
```
Leer
Diario
Acumulado
Archivo Salida
Descripción de Lote
Exportar
```
```
Leer
Diario
Acumulado
Archivo Salida
Descripción de Lote
Exportar
```
```
Catálogos
(Add ons)
```
```
Mensajes Recibidos
(Inbox)
```
```
Leer
```
- Exportar

```
Leer
```
- Exportar

```
Leer
```
- Exportar

```
Leer
```
- Exportar

```
Leer
```
- Exportar
Logs de Eventos
(Audit Logs)

```
Leer
```
- Exportar
    - - - Leer^
       - Exportar


```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 3 1
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 224 de 329
```
## ANEXO 3. CAUSALES DE DEVOLUCIÓN POR OPERADOR

### TIPO DESCRIPCIÓN

```
D01 La fecha efectiva es menor a la fecha de proceso.
D02 La fecha efectiva no es válida.
D03 Valor de la transacción no es numérico.
D04 El valor de la transacción de prenotificación es diferente de cero.
D05 El valor de la transacción monetaria es igual a cero
```
### D06

```
El valor de la transacción excede el monto diario permitido por usuario, por cuenta, proveniente
del mismo originador.
D07 Transacción que excede el límite. No autorizada.
D08 El número de secuencia del registro adenda es incorrecto.
```
### D09

```
El indicador de registro adenda de la transacción no concuerda con el(los) registro(s)
adenda(s).
D10 La causal de devolución en el registro de adenda es incorrecta.
D11 El número mínimo de registros de adenda para esta transacción es diferente de 1.
D12 El código de tipo de registro adenda es incorrecto.
```
### D13

```
En la información relacionada con el pago en un débito. El campo referencia1, el campo
descripción del servicio y/o código del usuario originador es vacío o contiene ceros.
```
### D14

```
En la información relacionada con el pago en un débito, el campo código de cliente originador por
servicio no es numérico.
D15 El nombre del usuario originador del lote es vacío.
```
### D16

```
La identificación del usuario originador en el registro de control del lote no coincide con la del
encabezado del lote.
D17 El número de cuenta receptora no es válida.
D18 El número de lote en el registro de control del lote no coincide con el del encabezado del lote.
D19 El código de el participante originador del lote es vacío o contiene ceros.
D20 El código clase de transacción en el registro de control del lote no es válido.
```
### D21

```
El código de el participante Receptor de la transacción no es válido o dicha entidad
no ha entrado en producción o está bloqueado
D22 El nombre del cliente receptor de la transacción es vacío.
D23 El número de identificación del usuario receptor de la transacción es vacío.
D24 La descripción del lote es vacía.
D25 La descripción de un lote de Cuentas de Préstamo no es válida.
D26 Campo alfanumérico no alineado a la izquierda.
D27 La línea tiene Caracteres Especiales en el encabezado de lote o en la transacción.
D28 Devolución de una devolución.
D29 Devolución débito tardía.
```

```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 3 1
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 225 de 329
```
D30 Entidad Participante no puede procesar débitos.

D31 Lote duplicado en el mismo día, que no fue debidamente autorizado.

D32 Transacción no permitida en este ciclo.


```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 3 1
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 226 de 329
```
## ANEXO 5. DETALLE DE PLANILLA DE COMPENSACIÓN

```
Transacciones ACH Cantidad Tx a Favor
```
```
Valor a
Favor ($)
```
```
Cantidad
Tx en
Contra Valor en Contra ($)
Débitos 0 $0.00 0 $0.00
Créditos 0 $0.00 0 $0.00
Devoluciones débito 0 $0.00 0 $0.00
Devoluciones crédito 0 $0.00 0 $0.00
Prenotificación Débito 0 $0.00 0 $0.00
Prenotificación Crédito 0 $0.00 0 $0.00
Dev. Prenotifi. Débito 0 $0.00 0 $0.00
Dev. Prenotifi. Crédito 0 $0.00 0 $0.00
Seguridad social OINF 0 $0.00 0 $0.00
SubTotal 0 $0.00 0 $0.00
Total ACH 0 $0.00 0 $0.00
Transacciones PSE No. A Favor No. En Contra
Pagos Empresas $0.00 $0.00
Pagos SSS $0.00 $0.00
Pagos SSS Otros Canales $0.00 $0.00
Pagos DIAN $0.00 $0.00
SubTotal PSE $0.00 $0.00
Total PSE $0.00 $0.00
Reversiones
Valor $0.00 $0.00
Total Rev $0.00 $0.00
Transacciones en Línea No. A Favor No. En Contra
Valor 0 $0.00 0 $0.00
Total TxLA 0 $0.00 0 $0.00
```
```
Pago Comisiones
Valor $0.00 $0.00
Total Com $0.00 $0.00
(ACH + PSE + REV + TxLA + PCOM) $0.00 $0.00
VALOR NETO $0.00 $0.00
```
```
EN CONTRA DE : xxxxx
Cuenta de Depósito : 65810103 - Sebra Código de Transacción : 151
```
```
DETALLE MONTOS DE COMPENSACIÓN
```
```
ACH COLOMBIA S.A
23/03/2022
```
```
PLANILLA DE COMPENSACIÓN DEFINITIVA CICLO 1 xxxx
```
```
A Favor En Contra
```
```
A Favor En Contra
```

```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 3 1
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 227 de 329
```
## ANEXO 6. EVENTOS SANCIONABLES DEL ESQUEMA DE CALIDAD

```
No. Descripción Sanción
Tipo de
Transacción
```
```
Entidad
Sancionada
```
### 2

```
Enviar transacciones por error
propio, que implique su reversión
manual posterior en otra Entidad
Participante.
```
```
1 SMLVD por cada transacción.
Se cobra máximo 2 SMLVM por cada
Entidad Beneficiaria involucrada en
cada Caso.
```
```
Débitos
Créditos
```
### EF

### 3

```
Afectar más de una vez o por valor
diferente la cuenta de un usuario
como consecuencia de haber
iniciado más de una solicitud de
reversión para una misma
transacción a través del módulo de
reclamos o de otro canal.
```
```
½ DTF sobre el valor en exceso o en
defecto de las transacciones adicional o
errada, mínimo ½ SMLVD.
```
```
Débitos
Créditos
EF
```
### 4

```
Aplicar o contabilizar una
transacción en una fecha diferente a
la Fecha Efectiva indicada por ACH
COLOMBIA o dejar disponibles los
fondos en la cuenta del usuario
Receptor después del ciclo máximo
definido.
```
```
½ DTF por el valor de la transacción por
día, mínimo ½ SMLVD por día. Sanción
total, máxima el valor de la transacción.
```
```
Crédito
EF
```
### 5

```
Aplicar la transacción a un Número
de Cuenta o a un Tipo de Cuenta
diferente al solicitado.
```
```
½ DTF por el valor de la transacción por
día, mínimo ½ SMLVD por día. Sanción
total, máxima el valor de la transacción.
```
```
Débitos
Créditos
EF
```
### 6

```
Afectar la cuenta de un usuario más
de una vez o por un valor diferente
al solicitado en la transacción.
```
```
½ DTF sobre el valor en exceso o en
defecto de las transacciones adicional o
errada, mínimo ½ SMLVD.
```
```
Débitos
Créditos
EF
```
### 7

```
Enviar una transacción monetaria sin
haber prenotificado la misma o sin
contar con las autorizaciones
respectivas.
```
```
½ SMLVD por cada transacción.
Débito
EF
```
### 8

```
Aplicar una transacción sin validar la
identificación del usuario Receptor
para transacciones débito o para
transacciones crédito cuando sea
solicitada por el usuario
```
```
½ SMLVD por cada transacción.
```
```
Débitos
Créditos
EF
```
### 9

```
Devolver transacciones no
monetarias de forma tardía.
```
```
½ SMLVD por cada transacción de
devolución no monetarias enviada de
forma tardía, por cada día hábil de
retraso.
```
```
Crédito
EF
```

```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 3 1
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 228 de 329
```
No. Descripción Sanción
Tipo de
Transacción

```
Entidad
Sancionada
```
### 10

```
Devolver transacciones monetarias
de forma tardía.
```
```
½ DTF por el valor de la transacción por
día, mínimo ½ SMLVD por día. Sanción
total, máxima el valor de la transacción.
```
```
Crédito
EF
```
### 11

```
Devolver una transacción monetaria
por no haber validado
correctamente la prenotificación
(cuenta y/o identificación).
```
```
½ SMLVD por cada transacción.
```
```
Débitos
Créditos
EF
```
13 Devolver una transacción sin justa
causa. ½^ SMLVD^ por^ cada^ transacción.^

```
Débitos
Créditos
```
### EF

### 16

```
No solucionar en los plazos definidos
el reclamo interpuesto por una
Entidad Participante, por no aplicar
ni devolver una transacción
monetaria recibida de otra Entidad
Participante.
```
```
Reintegro automático del valor de la
transacción que no fue aplicada ni
devuelta.
```
```
Crédito
EF
```
### 17

```
Contestar reclamos, solicitudes,
reversiones o devoluciones después
de los plazos definidos.
```
```
½ SMLVD por cada día calendario de
retraso.
Débitos
Créditos
```
### EF ACH

### 18

```
Pagar el valor de compensación
dentro de los diez (10) minutos
siguientes al plazo máximo de pago
establecido.
```
```
1 SMLVM por cada pago tardío.
```
```
Débitos
Créditos
EF
```
### 19

```
Pagar el valor de compensación
después de los diez (10) minutos
siguientes al plazo máximo de pago
establecido, pero antes de sesenta
(60) minutos en los ciclos 1, 2, 3 y 4
o antes de treinta (30) minutos en el
ciclo 5, contados a partir de la hora
máxima de pago normal establecida
para cada ciclo.
```
```
2 SMLVM por cada pago tardío.
```
```
Débitos
Créditos
EF
```
### 20

```
Pagar el valor de compensación
después de sesenta (60) minutos en
los ciclos 1, 2, 3 y 4 o después de
treinta (30) minutos en el ciclo 5,
contados a partir de la hora máxima
de pago normal establecida para
cada ciclo,
o no pagar la compensación.
```
```
3 SMLVM por cada pago tardío.
Adicionalmente no se procesan los
archivos enviados por la entidad
acreedora a ACH COLOMBIA y se
adiciona el costo de reprocesar los
archivos en ACH COLOMBIA.
```
```
Débitos
Créditos
```
### EF


```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 3 1
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 229 de 329
```
No. Descripción Sanción
Tipo de
Transacción

```
Entidad
Sancionada
```
### 22

```
Iniciar un proceso de reclamación
por error del usuario Receptor, al no
aceptar que ha dado previamente al
usuario Originador una
“Autorización de Recaudo” para
debitar su cuenta.
```
```
½ SMLVD por cada proceso de
reclamación errado.
```
```
Débitos
EF
```
### 24

```
Enviar transacciones erradas y/o
duplicadas que fueron recibidas
correctamente desde la Entidad
Participante hacia ACH COLOMBIA,
que impliquen su reversión manual
posterior en otra Entidad
Participante.
```
```
½ SMLVD por cada transacción. Se cobra
máximo 2 SMLVM por cada Entidad
Beneficiaria involucrada en cada Caso.
```
```
Débitos
Créditos
ACH
```
### 26

```
Procesar una transacción en
diferente fecha a la Fecha Efectiva
indicada.
```
```
SMLVD por cada transacción.
```
```
Débitos
Créditos
ACH
```
### 27

```
Aplicar la transacción a un Número
de Cuenta o a un Tipo de Cuenta
diferente al solicitado.
```
```
½ DTF por el valor de la transacción por
día, mínimo ½ SMLVD por día. Sanción
total, máxima el valor de la transacción.
```
```
Débitos
Créditos
EF
```
### 28

```
Enviar transacciones por error del
usuario Originador, que implique su
reversión manual posterior en otra
Entidad Participante.
```
```
1 SMLVD por cada transacción.
Se cobra máximo 2 SMLVM por cada
Entidad Beneficiaria involucrada en
cada
Caso.
```
```
Débitos
Créditos
```
### EF

### 29

```
Enviar transacciones con
Información incorrecta al
destinatario, es decir, que la
información al destinatario no
contemple la información mínima
establecida como estándar en ACH
COLOMBIA.
```
```
½ SMLVD por cada transacción con
información errada, con un tope
máximo de
20 SMLVM.
```
```
Débitos
Créditos
EF
```
### 30

```
Enviar transacciones que por error
del usuario Originador queden
doblemente aplicadas en la Entidad
Participante Receptor.
```
```
1 SMLVD por cada transacción.
Se cobra máximo 2 SMLVM por cada
Entidad Beneficiaria involucrada en
cada Caso.
```
```
Débitos
Créditos
```
### EF

31 Solicitar tiempo adicional para
transmisión de archivos.

```
(¼) SMLVM por cada solicitud de tiempo
adicional en cada ciclo sea utilizado o no
por el participante solicitante.
```
```
Débitos
Créditos EF^
```

```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 3 1
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 230 de 329
```
No. Descripción Sanción
Tipo de
Transacción

```
Entidad
Sancionada
```
32 Dejar transacciones PSE pendientes
por un lapso mayor a los tiempos
establecidos en el Manual de
Operaciones PSE.

```
1 SMLVD por cada transacción, con un
tope máximo de 2SMLVM por cada
entidad.
```
### PSE EF

37 No tener los ajustes tecnológicos,
operativos y/o no tener creadas en
portales, apps, web, etc. la Entidad
Participante vinculada a la cámara
de compensación de ACH Colombia
para enviar transferencias
interbancarias; o no tener los
desarrollos para enviar y/o recibir
transferencias a nuevos códigos de
transacciones

```
1 SMLVM durante el primer mes
después de reportada la novedad por
parte de la entidad afectada. A partir del
segundo mes, el valor de la sanción se
incrementará en 1 SMLVM adicional por
cada mes que la Entidad sancionada
demore en tener todos sus desarrollos
listos para transferir a la entidad
participante afectada y/o para
adaptarse a los nuevos códigos de
transacciones.
```
```
Débito
Crédito
```
### EF


### MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 231 de 329
```
## ANEXO 7. TIPOS DE NOVEDAD Y CAUSALES

### ABREVIATURA DESCRIPCIÓN

### REC

```
RECLAMO: Solicitud de una Entidad Participante a ACH COLOMBIA o a otra Entidad Participante, de verificar un proceso
específico en una transacción en la que se pudo haber cometido un error o en la que pudo haber incumplimiento en
las normas establecidas.
```
### REV

```
REVERSION: Solicitud de una Entidad Participante a otra Entidad Participante de intentar recuperar dineros abonados
por error de la entidad, usuario o de ACH COLOMBIA.
```
### SOL

```
SOLICITUD: Requerimientos de una Entidad Participante a ACH COLOMBIA o a otra Entidad Participante de generar una
certificación de un proceso específico efectuado con resultado exitoso o errado.
```
### DEV

```
DEVOLUCION: Solicitud de una Entidad Participante a otra Entidad Participante, de intentar recuperar una transacción
ACH crédito o débito no consentida de acuerdo con el estatuto de protección al consumidor.
```
### REI

```
REINTEGRO: Solicitud de una Entidad Participante a otra Entidad Participante, de intentar recuperar una transacción
que fue realizada por el botón de PSE, y que por características es una transacción no consentida de acuerdo con el
estatuto de protección al consumidor.
```
### DPC

```
DEVOLUCION PAGOS COMPLEMENTARIOS: Devolución que hace una Entidad Participante a otra Entidad Participante
del valor de un pago de pagos complementario de seguridad Social que no fue abonado exitosamente al cliente
recaudador.
```

### MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 232 de 329
```
### ABREVIATURA DESCRIPCIÓN LARGA

### DESCRIPCIÓN

### CORTA

### *TIPO DE

### TRANSACCIÓN

### QUE APLICA

### APLICA

### EF/ACH

### REC01

```
Afectar más de una vez o por valor diferente la cuenta de un
usuario como consecuencia de haber iniciado más de una solicitud
de reversión para una misma transacción a través de ACH
COLOMBIA o de otro canal.
```
```
Rev. Duplicada o
Errada
```
### 32

### 22

### 52

### 37

### 27

### 55

### 31

### 21

### 51

### 36

### 26

### 56

### EF

### REC02

```
Aplicar o contabilizar una transacción en una fecha diferente a la
Fecha Efectiva indicada por ACH COLOMBIA
o dejar disponibles los fondos en la cuenta del usuario
Receptor después del ciclo máximo definido.
```
```
Aplicar Tarde
```
### 22

### 32

### 52

### EF

### REC03

```
Aplicar la transacción a un Número de Cuenta o a un Tipo de
Cuenta diferente al solicitado. No.^ o^ Tipo^ Cta.^ Errado^
```
### 22

### 32

### 52

### 27

### 37

### 55

### EF

```
Afectar la cuenta de un usuario más de una vez o por un valor
diferente al solicitado en la transacción.
```
```
Error al Afectar
Cta.
```
### 22

### 32

### 52

### EF


### MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 233 de 329
```
### ABREVIATURA DESCRIPCIÓN LARGA

### DESCRIPCIÓN

### CORTA

### *TIPO DE

### TRANSACCIÓN

### QUE APLICA

### APLICA

### EF/ACH

### REC04 27

### 37

### 55

### 21

### 31

### 51

### 26

### 36

### 56

### REC05

```
Enviar una transacción monetaria sin haber prenotificado la
misma o sin contar con las autorizaciones respectivas.
```
```
No Prenotificar
Débito
```
### 27

### 37

### 55

### EF

### REC06

```
Aplicar una transacción débito sin validar la identificación del
usuario Receptor o transacciones crédito cuando sea solicitada
por el Cliente.
```
```
No Validar ID CR
```
### 22

### 32

### 52

### 27

### 37

### 55

### EF

### REC07

```
Devolver transacciones no monetarias de forma tardía.
Nota: Aplica sólo para créditos
```
```
Dev. Tardía
Prenot.
```
### 23

### 33

### 53

### EF

### REC08

```
Devolver transacciones monetarias de forma tardía.
Nota: Aplica sólo para créditos
```
```
Dev. Tardía
Monetaria
```
### 21

### 31

### 51

### EF

### REC09

```
Devolver una transacción monetaria por no haber validado
correctamente la prenotificación previa (cuenta y/o
identificación).
```
```
Error Validación
Prenot.
```
### 22

### 32

### 52

### EF


### MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 234 de 329
```
### ABREVIATURA DESCRIPCIÓN LARGA

### DESCRIPCIÓN

### CORTA

### *TIPO DE

### TRANSACCIÓN

### QUE APLICA

### APLICA

### EF/ACH

### 27

### 37

### 55

### REC10

```
Devolver una transacción modificando la información de la
transacción original.
```
```
Dev. Modificando
Inf.
```
### 22

### 32

### 52

### 27

### 37

### 55

### 23

### 33

### 28

### 38

### EF

### REC11

```
Enviar transacciones erradas y/o duplicadas que fueron recibidas
correctamente desde el participante hacia ACH COLOMBIA, que
impliquen su reversión manual posterior en otra Entidad
Participante.
```
```
Proceso Errado
ACH
```
### 22

### 32

### 52

### 27

### 37

### 55

### 21

### 31

### 51

### 26

### 36

### 55

### ACH

### REC12

```
Cobrar más o pagar menos de lo calculado a una Entidad
Participante en el proceso de compensación por error de ACH
COLOMBIA, y que se vea afectada por cambiar su
```
```
Cobro o Pago
Errado ACH
```
### 22

### 32

### 52

### ACH


### MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 235 de 329
```
### ABREVIATURA DESCRIPCIÓN LARGA

### DESCRIPCIÓN

### CORTA

### *TIPO DE

### TRANSACCIÓN

### QUE APLICA

### APLICA

### EF/ACH

```
“posición a favor” por “posición en contra” en la nueva liquidación
de compensación.
```
### 27

### 37

### 55

### 21

### 31

### 51

### 26

### 36

### 57

REC13 Proceso de transacción es diferente fecha a la fecha efectiva

```
Proceso de transacción
en diferente fecha a la
fecha efectiva
```
### 22

### 32

### 52

### 27

### 37

### 55

### 21

### 31

### 51

### 26

### 36

### 56

### 23

### 33

### 28

### 38

### ACH

REC14 Contestar reclamos después de los plazos definidos.
Contestar
Reclamos Tarde

### 22

### 32

### 27

### EF ACH


### MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 236 de 329
```
### ABREVIATURA DESCRIPCIÓN LARGA

### DESCRIPCIÓN

### CORTA

### *TIPO DE

### TRANSACCIÓN

### QUE APLICA

### APLICA

### EF/ACH

### 37

### 21

### 31

### 26

### 36

### 23

### 33

### 28

### 38

### REV01

```
Enviar transacciones por error de la EPO, que implique su
reversión manual posterior en otra Entidad Participante.
```
```
Rev. por Error
EPO
```
### 22

### 32

### 52

### 27

### 37

### 55

### 21

### 31

### 51

### 26

### 36

### 56

### EF

### REV02

```
Aplicar transacciones por error de la EPR o UR, que implique su
reversión manual posterior en otra Entidad Participante.
```
```
Rev. por Error
EPR o CR
```
### 22

### 32

### 52

### 27

### 37

### 55

### 21

### EF


### MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 237 de 329
```
### ABREVIATURA DESCRIPCIÓN LARGA

### DESCRIPCIÓN

### CORTA

### *TIPO DE

### TRANSACCIÓN

### QUE APLICA

### APLICA

### EF/ACH

### 31

### 51

### 26

### 36

### 56

REV03 Reversión solicitada por el usuario Originador debido a un error. Rev. por Error del CO.

### 22

### 32

### 52

### 27

### 37

### 57

### 21

### 31

### 51

### 26

### 36

### 56

### EF

### REV04

```
Reversión solicitada por doble abono por error del usuario
Originador.
Rev. por Error del CO.
```
### 22

### 32

### 52

### 27

### 37

### 55

### 21

### 31

### 51

### 26

### 36

### EF


### MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 238 de 329
```
### ABREVIATURA DESCRIPCIÓN LARGA

### DESCRIPCIÓN

### CORTA

### *TIPO DE

### TRANSACCIÓN

### QUE APLICA

### APLICA

### EF/ACH

### 56

### SOL01

```
Que una transacción específica, ha sido procesada en una fecha
determinada por ACH COLOMBIA y enviada a el participante
Receptor para su posterior proceso.
```
```
Texto Certificación: De acuerdo con la solicitud en referencia, ACH
COLOMBIA S.A., certifica que la(s) transacción(es) descritas a
continuación, fueron procesadas a través de nuestro sistema y
enviadas a la(s) entidad(es) relacionada(s).
```
```
Certificación de
ACH
```
### 22

### 32

### 52

### 27

### 37

### 55

### 21

### 31

### 51

### 26

### 36

### 56

### 23

### 33

### 53

### 28

### 38

### 57

### ACH

### SOL02

```
Que una transacción fue aplicada en la cuenta del UR en una fecha
específica, por solicitud de una EPO o de un UO.
```
```
Texto Certificación: De acuerdo con la solicitud en referencia,
certificamos que la(s) transacción(es) descritas a continuación,
fue (ron) aplicada(s) en la cuenta receptora de acuerdo con los
datos suministrados en su
solicitud.
```
```
Certificación
EPR
```
### 22

### 32

### 52

### 27

### 37

### 55

### 21

### 31

### 51

### EF


### MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 239 de 329
```
### ABREVIATURA DESCRIPCIÓN LARGA

### DESCRIPCIÓN

### CORTA

### *TIPO DE

### TRANSACCIÓN

### QUE APLICA

### APLICA

### EF/ACH

### 26

### 36

### 56

### 23

### 33

### 28

### 38

### 57

### SOL03

```
Que una transacción fue devuelta por una razón y en una fecha
específica.
```
```
Texto Certificación: De acuerdo con la solicitud en referencia,
certificamos que la(s) transacción(es) descritas a continuación,
fue (ron) devuelta(s) por la causal y en la fecha señaladas.
```
```
Certificación de
Dev.
```
### 22

### 32

### 52

### 27

### 37

### 55

### 23

### 33

### 53

### EF

### SOL04

```
Que un proceso de aplicación de devoluciones o prenotificaciones
no fue exitoso.
```
```
Texto Certificación: De acuerdo con la solicitud en referencia,
certificamos que la transacción(es) descritas a continuación, no
fue (ron) procesadas con éxito.
```
```
Certificación Proceso
No Exitoso.
```
### 21

### 31

### 51

### 26

### 36

### 56

### 23

### 33

### 53

### 28

### 38

### EF


### MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 240 de 329
```
### ABREVIATURA DESCRIPCIÓN LARGA

### DESCRIPCIÓN

### CORTA

### *TIPO DE

### TRANSACCIÓN

### QUE APLICA

### APLICA

### EF/ACH

### 57

### SOL05

```
Datos del usuario Originador
```
```
Texto Certificación: De acuerdo con la solicitud en referencia,
certificamos que la(s) transacción(es) descritas a continuación,
fue (ron) originada(s) de acuerdo con los datos suministrados del
usuario Originador.
```
```
Certificar Datos Cliente
Originador
```
### 22

### 32

### 27

### 37

### 23

### 33

### 28

### 38

### EF

### DEV07

```
Solicitud de Devolución de una transacción ACH crédito no
consentida – Estatuto Protección al consumidor
```
```
Devolución transacción
ACH crédito No
consentida
```
### 22

### 32

### 52

### EF

### DEV14

```
Solicitud de Devolución de una transacción ACH débito no
consentida
```
```
Devolución transacción
ACH Debito No
consentida
```
### 27

### 37

### 55

### EF

### REI08

```
Solicitud de Reintegro de una transacción PSE que fue objeto de
fraude
```
```
TX PSE Objeto de
Fraude
```
### 22

### 32

### 52

### EF

### REI09

```
Solicitud de Reintegro de una transacción PSE que fue objeto de
fraude de acuerdo con – Estatuto Protección al consumidor
decreto 587
```
```
Reintegro TX PSE
objeto de Fraude
```
### 22

### 32

### 52

### EF


### MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 241 de 329
```
### ABREVIATURA DESCRIPCIÓN LARGA

### DESCRIPCIÓN

### CORTA

### *TIPO DE

### TRANSACCIÓN

### QUE APLICA

### APLICA

### EF/ACH

### REI10

```
Solicitud de Reintegro de una transacción PSE que no fue
solicitada por el usuario de acuerdo con – Estatuto Protección al
consumidor decreto 587
```
```
Reintegro TX PSE No
Solicitada
```
### 22

### 32

### 52

### EF

### REI11

```
Solicitud de Reintegro de una transacción PSE en la cual el usuario
manifiesta que el Producto adquirido No se recibió de acuerdo
con – Estatuto Protección al consumidor decreto 587
```
```
Reintegro TX PSE
Producto Adquirido No
recibido
```
### 27

### 37

### 55

### EF

### REI12

```
Solicitud de Reintegro de una transacción PSE en la cual el usuario
manifiesta que el Producto Entregado No es el Solicitado de
acuerdo con – Estatuto Protección al consumidor decreto 587
```
```
Reintegro TX PSE
Producto Recibido No
es el Solicitado
```
### 22

### 32

### 52

### EF

### REI13

```
Solicitud de Reintegro de una transacción PSE en la cual el usuario
manifiesta que el Producto Entregado esta Defectuoso de
acuerdo con – Estatuto Protección al consumidor decreto 587
```
```
Reintegro TX PSE
Producto Entregado
Esta Defectuoso
```
### 27

### 37

### 55

### EF

### DPC001

```
Devolución de pagos a Cuentas AFC que no fueron abonadas
exitosamente en el participante Receptor.
```
```
Devolución Abonos
Cuentas AFC
```
```
No. Cuenta AFC
EF
```
### DPC00 2

```
Devolución de pagos de Libranzas que no fueron abonadas
exitosamente en el participante Receptor.
```
```
Devolución Pagos de
Libranzas
No. libranza EF
```

### MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 242 de 329
```
*Códigos de Transacciones Servicio ACH

Código Descripción

21 devolución Crédito a Cuenta. Corriente
22 crédito a Cuenta Corriente
23 Prenotificación Crédito a Cuenta Corriente
26 devolución Débito a Cuenta Corriente
27 débito a Cuenta Corriente
28 Prenotificación Débito a Cuenta Corriente
31 devolución Crédito a Cuenta de Ahorros
32 crédito a Cuenta de Ahorros
33 Prenotificación Crédito a Cuenta de Ahorros
36 devolución Débito a Cuenta de Ahorros
37 débito a Cuenta de Ahorros
38 Prenotificación Débito a Cuenta de Ahorros
51 devolución Crédito a Depósitos Electrónicos
52 crédito a Depósitos Electrónicos
53 Prenotificación Crédito a Depósitos Electrónicos
56 devolución Débitos a Depósitos Electrónicos
55 débitos a Depósitos Electrónicos
57 Prenotificación Débito a Depósitos Electrónicos


### MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 243 de 329
```
## ANEXO 8. FACTURA DE VENTA


### MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 244 de 329
```
## ANEXO 9. CAUSALES DE DEVOLUCIONES - RECHAZOS

```
Causal
```
```
Débitos Créditos
Descripción Estándar Detalle Adicional de la Devolución
Prenotificación Monetaria Prenotificación Monetaria
```
```
R01 N/A SI N/A N/A Fondos^ Insuficientes:^ El^ saldo^ disponible^ no^ es^ suficiente^ para^ cubrir^ el^ valor^ de^ la^
transacción débito.
```
### R02

### SI

### SI

### SI

### SI

```
Cuenta o Depósito Electrónico
Cerrado: Cuenta o Depósito
Electrónico cerrado por orden del
usuario Receptor o por el
participante originador.
```
- Cuenta o Depósito Electrónico Saldado:
cuenta o Depósito Electrónico activo que ha
sido cerrado por orden del usuario Receptor.
- Cuenta o Depósito Electrónico Cancelado:
cuenta o Depósito Electrónico activo que ha
sido cerrada por orden de la Entidad
Participante Receptor.

```
R03 SI SI SI SI
```
```
Cuenta o Depósito Electrónico No Abierto: El Número Cuenta o Depósito
electrónico registrado no corresponde a una cuenta o Depósito Electrónico
asignado o abierto.
```
### R04

### SI

### SI

### SI

### SI

```
Número Cuenta o Depósito
Electrónico Inválido: El número de
la cuenta o Depósito Electrónico es
incorrecto.
```
- La estructura del Número Cuenta o Depósito
Electrónico no es válida.
- El Dígito Chequeo no es válido
- Número incorrecto de dígitos.

### R06

### N/A

### SI

### N/A

### SI

```
Devolución Solicitada por el
participante originador: La Entidad
Participante Originadora ha
solicitado a el participante
Receptor, devolver una
transacción.
```
- Por conocer que la transacción fue enviada
por error.
- Por conocer que la cuenta pertenece a la lista
Clinton.

### R07 N/A SI N/A N/A

```
Autorización de Recaudo Revocada por el usuario Receptor: El usuario Receptor ha
revocado o cancelado en forma definitiva la autorización previamente dada al
usuario Originador para debitar su cuenta o Depósito Electrónico en el futuro.
```
```
R08
N/A
SI
N/A
N/A
```
```
Orden de No Pago: El usuario Receptor de una transacción débito periódica ha dado
orden de no pago a una transacción débito específica para que no sea aplicada. La
```

### MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 245 de 329
```
Causal
Débitos Créditos
Descripción Estándar Detalle Adicional de la Devolución
Prenotificación Monetaria Prenotificación Monetaria
Entidad Participante Receptor debe verificar el propósito del usuario Receptor,
cuando hace una solicitud de orden de no pago, esto con el fin de asegurarse que
no se trata de una revocación de autorización (R07).

R09 N/A SI N/A N/A Fondos^ no^ Disponibles:^ El^ saldo^ total^ es^ suficiente^ para^ cubrir^ esta^ transacción,^ sin^
embargo, el saldo disponible no es suficiente para cubrir la transacción débito.

R10 N/A SI N/A N/A
No existe prenotificación: No fue encontrada la autorización o acuerdo con el
usuario Receptor o no existe prenotificación

### R12 SI SI N/A N/A

```
usuario Originador no autorizado: La Entidad Participante Receptor ha sido
notificada por su usuario Receptor, que el usuario
Originador de la transacción no ha sido autorizado para debitar su cuenta o
Depósito Electrónico.
```
### R13

### N/A

### SI

### N/A

### N/A

```
Devolución por solicitud del
usuario Receptor (Persona
Natural): El Cliente Receptor, no
acepta el débito a su cuenta o
Depósito Electrónico por una
razón específica.
```
```
Fecha de transacción errada: La fecha de la
transacción débito no corresponde a la fecha
autorizada por el usuario Receptor.
Monto no autorizado: El valor de la transacción
débito no corresponde al monto autorizado por
el usuario Receptor.
Débito Duplicado: El usuario Receptor notifica
el recibo de una transacción débito duplicada
en su cuenta o Depósito Electrónico.
```
### R14

### SI

### SI

### N/A

### N/A

```
Muerte del delegado o Representante: El delegado o Representante (apoderado)
del usuario Receptor, sea este una persona o una institución autorizada para recibir
transacciones en nombre de otras personas, ha muerto o ha perdido esa facultad.
El beneficiario o Cliente Receptor no ha muerto.
```
R15 SI SI N/A N/A

```
Muerte del Beneficiario o Titular de la Cuenta o Depósito Electrónico: El
Beneficiario, usuario Receptor o Titular de la cuenta o Depósito Electrónico ha
muerto.
```

### MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 246 de 329
```
Causal
Débitos Créditos
Descripción Estándar Detalle Adicional de la Devolución
Prenotificación Monetaria Prenotificación Monetaria

### R16

### SI

### SI

### SI

### SI

```
Cuenta o Depósito Electrónico
Inactivo o Cuenta o Depósito
Electrónico Bloqueado: Cuenta o
Depósito Electrónico inactivo por
no tener movimiento en un
periodo de tiempo o por solicitud
del titular de esta o por el
participante Receptor.
```
- Cuenta o Depósito Electrónico Inactivo: Por no
tener movimiento en un período específico de
tiempo.
- Cuenta o Depósito Electrónico Bloqueado: Por
solicitud del titular de la cuenta o Depósito
Electrónico o usuario Receptor y/o por el
participante Receptor.

### R17 SI SI SI SI

```
La Identificación no coincide con Cuenta o Depósito Electrónico del usuario
Receptor. La estructura del Número Cuenta o Depósito Electrónico y el Dígito
Chequeo son válidos, pero el Número Cuenta o Depósito Electrónico no
corresponde con el número de identificación del usuario Receptor registrado.
```
### R20 SI SI SI SI

```
Cuenta o Depósito Electrónico No
Habilitado para recibir
transacciones: cuenta o Depósito
Electrónico de naturaleza especial
que está limitada para recibir
transacciones débito o crédito.
```
- Transacción no puede ser aplicada debido a
que el usuario Receptor está asociado a listas
restrictivas: la información asociada al usuario
receptor (nombre, id, adenda) objeto de la
transacción, No permite aplicar transacciones
porque genera coincidencia total o parcial con
una o varias listas restrictivas.
- Transacción no puede ser aplicada debido a
que el usuario Originador está asociado a las
listas restrictivas: la información asociada al
usuario originador (nombre, id, descripción de
lotes, datos discrecionales del usuario
originador) remitente de la transacción, No
permite aplicar transacciones porque genera
coincidencia total o parcial con una o varias
listas restrictivas.


### MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 247 de 329
```
Causal
Débitos Créditos
Descripción Estándar Detalle Adicional de la Devolución
Prenotificación Monetaria Prenotificación Monetaria
Las posibles listas restrictivas que el
participante podrá valida son: OFAC, ONU,
Banco de Inglaterra, Unión Europea o listas
restrictivas de Colombia.

- Cuentas o Depósito Electrónico usados en
medios políticos: si la cuenta que se debe
afectar es usada en medios políticos como
campañas y similar, podría usarse esta causal.

### R23 N/A N/A N/A SI

```
Devolución de una transacción
crédito por solicitud del usuario
Receptor: La transacción crédito
no es aceptada por el usuario
Receptor por no cumplir con las
condiciones pactada.
```
- El valor mínimo solicitado por el usuario
Receptor no ha sido enviado.
- El valor exacto solicitado por el usuario
Receptor no ha sido enviado.
- La cuenta o Depósito Electrónico está en litigio
y el usuario Receptor no acepta la transacción.
- La aceptación de la transacción origina un
sobrepago.
- El usuario Originador no es conocido por el
usuario
Receptor.
- El usuario Receptor no ha autorizado esta
transacción crédito para esta cuenta.
R29 SI SI N/A N/A Devolución de una transacción débito por solicitud del usuario Receptor (Persona
Jurídica): La Entidad Participante Receptor ha sido notificada por su usuario
Receptor Corporativo (no consumidor), que el usuario Originador de la transacción
no ha sido autorizado para debitar su cuenta o Depósito Electrónico.

### R30 SI SI SI SI

```
Cliente Receptor no habilitado
para recibir transacciones a
Depósitos Electrónicos
```
```
La cuenta destino no es una cuenta asociada a
una persona Natural
```

### MANUAL DE SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE

### VERSIÓN 31

### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
```
```
Agosto de 2024
Página 248 de 329
```
Causal
Débitos Créditos
Descripción Estándar Detalle Adicional de la Devolución
Prenotificación Monetaria Prenotificación Monetaria

R31 SI N/A N/A N/A Prenotificación^ no^ procesada^ por^
parte de la Entidad Receptora

```
Para Efectuar la devolución de transacciones de
prenotificación débito cuando no se encuentre
la información total o parcial del campo 3 del
registro de adenda, establecida como de
obligatoria inclusión por parte de las Entidades
Originadoras
```
R32 N/A N/A N/A SI Transacción^ no^ procesada^ por^
parte de la Entidad Receptora

```
Para efectuar la devolución de transacciones
monetarias tipo PPD crédito, cuando no se
encuentre la información total o parcial del
campo 3 del registro de adenda, establecida
como de obligatoria inclusión por parte de las
entidades Originadoras.
```
R33 SI SI SI SI

```
Devolución de una transacción de
depósito electrónico cuando
excede los límites establecidos.
```
```
Monto no autorizado, el valor de la transacción
crédito o débito con destino a depósito
electrónico excede los topes definidos.
```
R35 SI SI SI SI Tipo de Cuenta Errada
La transacción no puede ser aplicada debido a
que el tipo de cuenta está errado.


```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota
```
```
Agosto de 2024
Página 249 de 329
```
## ANEXO 10. DEVOLUCIONES POR SOLICITUD DEL USUARIO RECEPTOR

1. Devolución crédito por solicitud del usuario receptor

### DEVOLUCIÓN CRÉDITO POR SOLICITUD DEL USUARIO RECEPTOR

```
INFORMACIÓN DEL TITULAR DE LA CUENTA (Usuario Receptor)
Nombres y Apellidos
```
```
Número de Identificación Tipo
```
C.C. NIT OTROS

```
Dirección Teléfono Ciudad
```
### INFORMACIÓN PARA SOLICITAR UNA DEVOLUCIÓN CRÉDITO

```
Entidad Financiera Receptora (donde el Titular tiene la Cuenta)^
Sucursal
Ciudad
```
Tipo de Cuenta Número
Corriente Ahorros Otros

```
Por medio de este documento me permito solicitar al (a la) _ ___ __________la
Nombre Entidad Financiera donde tiene la Cuenta
“Devolución” de una transacción crédito originada por __ ______________________;
Nombre de la Empresa que Abona (Cliente Originador)
```
```
Autorizo expresamente debitar de mi cuenta $___ ___ ___ _, valor que
Corresponde a la transacción crédito no aceptada.
La razón de la no aceptación del crédito es: _ ___ ___ __________
```
```
Valor de la Transacción $ ___ Fecha de Aplicación: AAAA/MM/DD
```
```
Firma del Titular _ ___ ___ ________
```
```
Número de Identificación: _____________________
```
```
Fecha de Diligenciamiento
AAAA/MM/DD
```
```
INFORMACIÓN DE LA EMPRESA QUE ABONA (Cliente Originador)
Nit. Cliente Originador: ___ ___ ___ ___ ___ ______
```
```
Nombre Entidad Financiera Originadora: (opcional)__________ ___ ___ ______
```

```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota
```
```
Agosto de 2024
Página 250 de 329
```
### REGLAMENTO PARA SOLICITAR UNA DEVOLUCIÓN CRÉDITO

```
El Cliente Receptor a través de este documento solicita, a su Entidad Participante Receptor generar una
transacción de devolución crédito aplicada con anterioridad.
```
```
Para Devoluciones a Solicitud del Cliente Receptor:
La Entidad Participante debitará el dinero resultado de la devolución solicitada por el Cliente Receptor
como máximo al día hábil siguiente de efectuado el reclamo.
Algunas de las causales posibles de reclamación o de solicitud de devolución por parte del Cliente Receptor
son:
El valor mínimo solicitado por el Cliente Receptor no ha sido enviado. El valor exacto solicitado por el
Cliente Receptor no ha sido enviado. La cuenta está en litigio y el Cliente Receptor no acepta la transacción
La aceptación de la transacción origina un sobrepago.
El Cliente Originador no es conocido por el Cliente Receptor.
El Cliente Receptor no ha autorizado esta transacción crédito para esta cuenta.
El Cliente Receptor autoriza expresamente debitar de su cuenta, el valor que corresponda a una
devolución crédito por reclamación presentada por él mismo a el participante Receptor.
```
```
El Cliente Receptor debe dirigirse a su Cliente Originador para resolver los conflictos relativos a los créditos
enviados a su cuenta o a la relación existente entre ellos.
```
### EL CLIENTE LA ENTIDAD PARTICIPANTE


```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota
```
```
Agosto de 2024
Página 251 de 329
```
2. Devolución débito por solicitud del usuario receptor

### DEVOLUCIÓN DÉBITO POR SOLICITUD DEL USUARIO RECEPTOR

```
INFORMACIÓN DEL TITULAR DE LA CUENTA (Usuario Receptor)
Nombres y Apellidos
```
```
Número de Identificación Tipo
```
```
C.C. NIT OTROS
```
```
Dirección Teléfono Ciudad
```
```
INFORMACIÓN PARA SOLICITAR UNA DEVOLUCIÓN
Entidad Financiera Receptora (donde el Titular tiene la
Cuenta)
```
```
Sucursal Ciudad
```
```
Tipo de Cuenta Número
```
```
Corriente Ahorros Otros
```
```
Por medio de este documento me permito solicitar al (a la) _ ___ _________la
Nombre Entidad Financiera donde tiene la
Cuenta
```
1. “Devolución del Débito” aplicado 2. “Orden de No pago” del Débito a aplicar de la transacción débito
autorizada previamente en el documento de "Autorización de Recaudo" otorgado a la misma Entidad
Financiera mencionada, para recibir dicha transacción débito originada por ___ ___ ___ _______,
acreditando mi cuenta aquí identificada.
    Nombre de la Empresa Recaudadora (Cliente Originador)

```
La razón de la no aceptación del débito es: ______ ___ ___ ___ ______ Valor de la Transacción: ___
```
```
Fecha de Aplicación:
AAAA/M
M/DD
Firma del Titular ___ ___ ___ ________
Número de Identificación: ________ _______
```
```
Fecha de Diligenciamiento
```
```
AAAA/MM/DD
```
```
INFORMACIÓN DE LA EMPRESA BENEFICIARIA (Cliente Originador)
Nit. Cliente Originador: Código Único de Referencia del Servicio:
```
```
Nombre Entidad Financiera Originadora: (opcional)__________ ___ ___ _____
```

```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota
```
```
Agosto de 2024
Página 252 de 329
```
### REGLAMENTO PARA SOLICITAR UNA DEVOLUCIÓN DÉBITO

```
El Cliente Receptor a través de este documento solicita, a su Entidad Participante Receptor generar una
transacción de devolución débito aplicada o una orden de no pago a una transacción específica.
```
```
Para Devoluciones a Solicitud del Cliente Receptor:
```
```
La Entidad Participante abonará el dinero resultado de la devolución solicitada por el Cliente Receptor como
máximo al día hábil siguiente
de efectuado el reclamo.
El Cliente Receptor debe ser consciente que presentar solicitudes de devolución reiteradas, puede ser causal
de cancelación del servicio por parte del Cliente Originador o por parte del participante Receptor.
Algunas de las causales posibles de reclamación o de solicitud de devolución por parte del Cliente Receptor
son:
```
```
Cliente Originador no autorizado: La Entidad Participante Receptor ha sido notificada por su Cliente
Receptor, que el Cliente Originador de la transacción no ha sido autorizado para debitar su cuenta.
No existe autorización o prenotificación: No fue encontrada la autorización o acuerdo con el Cliente Receptor
o no existe prenotificación. Monto no autorizado: El valor de la transacción débito no corresponde al monto
autorizado por el Cliente Receptor.
Fecha de transacción errada: La fecha de la transacción débito no corresponde a la fecha autorizada por el
Cliente Receptor. Transacción débito fraudulenta.
Autorización Cancelada: El Cliente Receptor ha cancelado previamente la autorización de recaudo.
Débito Duplicado: El Cliente Receptor notifica el recibo de una transacción débito duplicada. Para Ordenes de
```
```
no Pago:
```
```
El Cliente Receptor debe solicitar una Orden de No Pago, con una antelación no inferior a cinco (5) días
hábiles antes de la fecha de aplicación del débito.
Una Orden de no Pago se hace efectiva cuando el Cliente Originador envía la primera transacción posterior
a la orden de no pago dada por el Cliente Receptor en la Entidad Participante Receptora, y ésta es devuelta
por la Entidad Financiera. Débitos posteriores serán realizados normalmente.
```
```
El Cliente Receptor debe ser consciente que presentar órdenes de no pago reiteradas, puede ser causal de
cancelación del servicio por parte del Cliente Originador o por parte del participante Receptor.
```
```
El Cliente Receptor debe dirigirse a su Cliente Originador para resolver los conflictos relativos a los débitos
enviados a su cuenta o a la relación existente entre ellos.
```
### EL CLIENTE LA ENTIDAD PARTICIPANTE


```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota
```
```
Agosto de 2024
Página 253 de 329
```
## ANEXO 11. AUTORIZACIÓN DE RECAUDO


```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota

```
Agosto de 2024
Página 254 de 329
```
### REGLAMENTO PARA LA AUTORIZACIÓN DE RECAUDO

El(los) titular(es) de la cuenta señalada (Cliente), autoriza(mos) incondicionalmente y por un término
indefinido, por medio de este documento y a través de la Empresa Recaudadora, a el participante donde
tiene(n) su cuenta, lo siguiente: (1) debitar de la cuenta el valor que corresponde a la transacción débito
indicada y entregar dicho valor a la Empresa Recaudadora; (2) conservar el documento en sede de la Empresa
Recaudadora; (3) enviar el contenido de la información arriba descrita, de manera electrónica, advirtiendo
que el Cliente le da pleno valor y eficacia al documento de autorización aquí dado y que ante cualquier error
de la Empresa Recaudadora en la conversión electrónica de la orden, afecta tan solo la relación del Cliente
con dicha Empresa Recaudadora; ( 4 ) debitar la cuenta en la fecha de aplicación indicada por el Cliente; (5)
debitar la cuenta en una fecha diferente a la indicada, tan sólo en aquellos casos en los que la Empresa
Recaudadora tenga inconvenientes de índole técnico u operativo que no le permitan debitar la cuenta
oportunamente; (6) extender esta misma autorización a modificaciones obligatorias que realice el
participante, tales como: al número de cuenta, sucursal o nombre del participante que por ejemplo, entra en
proceso de fusión, venta o cierre de oficinas.

El(los) titular(es) de la cuenta señalada se compromete(n) a: (1) mantener fondos en la fecha indicada por el
Cliente (2) proveer la autorización de parte de todos los titulares de la cuenta en este documento o las copias
de este que fueren necesarias, o en su defecto a asumir las consecuencias que se deriven de no declarar la
condición de manejo de firmas conjuntas de la cuenta, liberando así a la Empresa Recaudadora y a el
participante de toda responsabilidad.

El(los) titular(es) de la cuenta señalada, declara(n) que conoce(n) y acepta(n) lo siguiente: que (1) la Entidad
Participante donde tiene(n) su cuenta únicamente realizará los débitos en el día de aplicación mencionado;
(2) que el débito autorizado se podrá hacer ordinariamente durante el tiempo y la oportunidad indicados,
siempre que la cuenta tenga fondos disponibles y que, si el día no fuere hábil, el débito se hará el siguiente
día hábil. No obstante, si en esa oportunidad no hay fondos disponibles en la cuenta, el débito podrá hacerse
cuando existan fondos disponibles; (3) que el participante donde tiene su cuenta podrá abstenerse de hacer
el débito si no existen fondos disponibles para ello o si se presenta alguna causal que lo impida; (4) que las
únicas modificaciones a la presente Autorización de Recaudo que el Cliente podrá solicitar a la Empresa
Recaudadora son: novedad de fecha de aplicación y monto autorizado (si aplica); la novedad se debe entregar
con diez (10) días hábiles de anticipación, del envío del próximo débito. Si el Cliente desea autorizar a otra
Entidad Participante, a otro número o tipo de cuenta, deberá cancelar el formato vigente y diligenciar una
nueva Autorización de Recaudo; (5) que la Autorización de Recaudo solamente podrá ser cancelada
mediante comunicación escrita enviada por el Cliente a la Empresa Facturadora o la sucursal de el
participante donde el Cliente tiene su cuenta, con una anticipación no inferior a diez (10) días hábiles a la
fecha a partir de la cual se desee hacer efectiva la cancelación; ( 6 ) que esta autorización se considera como
una adición o modificación al Contrato de Cuenta Corriente o al Reglamento de la Cuenta de Ahorros que el
Cliente tiene con el participante donde tiene la cuenta; (7) que el Cliente puede dirigir sus reclamaciones o
solicitudes de devolución directamente a la Empresa Recaudadora en cualquier momento, o a el participante
donde tiene su cuenta en un plazo máximo de cuarenta y cinco ( 4 5) días calendario a partir de la fecha de
aplicación del débito; (8) que puede dar una orden de no pago a el participante donde tiene su cuenta, para
una transacción débito específica con una antelación no inferior a cinco (5) días hábiles antes de la fecha de


```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota
```
```
Agosto de 2024
Página 255 de 329
```
```
aplicación del débito; (9) que presentar órdenes de no pago o solicitud de devoluciones reiteradas, puede
implicar un costo adicional o ser causal de cancelación del servicio por parte de la Empresa Recaudadora o
por parte del participante donde tiene su cuenta.
Al dar la presente autorización el Cliente es consciente que pueden surgir conflictos que impliquen la necesidad
de revelar la documentación e información aquí contenida, así como otros datos pertinentes de su relación
bancaria y comercial. Renuncia, por tanto, en tales circunstancias y dentro de los precisos límites de tales
```
conflictos,^ a^ la^ reserva^ bancaria^ a^ favor^ de^ las^ entidades^ involucradas.^


```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota
```
```
Agosto de 2024
Página 256 de 329
```
## ANEXO 12. CANCELACIÓN DE AUTORIZACIÓN DE RECAUDO

(^)
CANCELACIÓN DE AUTORIZACIÓN DE RECAUDO
INFORMACIÓN DEL TITULAR DE LA CUENTA (Usuario Receptor)
Nombres y Apellidos
Número de Identificación Tipo
C.C. NIT OTROS
Dirección Teléfono Ciudad

### INFORMACIÓN PARA LA CANCELACIÓN DE AUTORIZACIÓN

```
Entidad Financiera Receptora (donde el Titular tiene la
Cuenta)
```
```
Sucursal Ciudad
```
```
Tipo de Cuenta Número
```
```
Corriente Ahorros Otros
Por medio de este documento me permito ordenar la cancelación de la "Autorización de
Recaudo" otorgada al (a la) _ ___ ___ ___ _ para
Nombre Entidad Financiera Receptora donde tiene la cuenta
recibir transacciones débito originadas por ___ ___ ___ ___.
Nombre de la Empresa Recaudadora (Cliente
Originador)
```
```
A continuación, explico el motivo de la cancelación: _____ ___ ______
```
```
___ ___ ___ ___ ___ ___ _____.
Firma del Titular
___ ___ ___ ____
```
```
Número de Identificación: ________ ___
```
```
Fecha de Diligenciamiento
```
```
AAAA/MM/DD
```
```
INFORMACIÓN DE LA EMPRESA BENEFICIARIA (Cliente Originador)
NIT. Cliente Originador: Código Único de Referencia del Servicio:
```
```
Nombre Entidad Financiera Originadora:
(opcional)_____
```

```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota
```
```
Agosto de 2024
Página 257 de 329
```
## ANEXO 13. CONTRATO DÉBITO CLIENTE ORIGINADOR – ENTIDAD FINANCIERA ORIGINADORA

### ADICIÓN AL CONTRATO DE CUENTA CORRIENTE / AHORROS PARA EL SERVICIO DE TRANSACCIONES DEBITO

```
Entre los suscritos (NOMBRE DE LA PERSONA NATURAL QUE REPRESENTA A LA EMPRESA – CLIENTE
ORIGINADOR), mayor de edad, identificado como aparece al pie de su firma y domiciliado en la ciudad de
(CIUDAD), en su calidad de (POSICIÓN QUE OCUPA DENTRO DE LA EMPRESA – CLIENTE ORIGINADOR),
actuando en nombre y representación de (NOMBRE DE LA EMPRESA - CLIENTE ORIGINADOR), en adelante
denominado EL CLIENTE ORIGINADOR (CO), sociedad legalmente constituida por Escritura Pública No. - ---,
domiciliada en (NOMBRE DE LA CIUDAD), inscrita en el registro mercantil bajo el No. - ---- debidamente
autorizado para suscribir la presente adición como consta en el poder otorgado en documento que se anexa
(O EN EL CERTIFICADO DE EXISTENCIA Y REPRESENTACIÓN LEGAL) de una parte y, de la otra, (NOMBRE DE
LA PERSONA NATURAL QUE REPRESENTA A LA ENTIDAD PARTICIPANTE), mayor de edad, identificado como
aparece al pie de su firma, y domiciliado en la ciudad de (NOMBRE DE LACIUDAD), en su calidad de
(POSICIÓN QUE OCUPA DENTRO DE LA ENTIDAD PARTICIPANTE O CLASE DE APODERADO), actuando en
nombre y representación de (NOMBRE DE LA ENTIDAD PARTICIPANTE ORIGINADORA) entidad legalmente
constituida por Escritura Pública No. ------, domiciliada en la ciudad de (NOMBRE DE LA CIUDAD), inscrita en
el Registro Mercantil bajo el No. -------------, portadora del Nit. tributario - ----------, en adelante denominada
la ENTIDAD PARTICIPANTE ORIGINADORA (EPO), debidamente autorizado para suscribir la presente adición
como consta en el poder otorgado en documento que se anexa (O EN EL CERTIFICADO DE EXISTENCIA Y
REPRESENTACIÓN LEGAL (SUPERBANCARIA), han acordado celebrar la presente adición al contrato de
cuenta corriente o de ahorros, para el servicio de transacciones débito, que se regirá por las siguientes
cláusulas:
```
```
TERMINOLOGÍA
```
```
Para la adecuada interpretación de esta adición, cuando sean utilizados términos cuya inicial sea en
mayúscula, ya sea en singular o en plural, o como sustantivo o verbo, deberán entenderse de acuerdo
con las definiciones que se incluyen en el Anexo 1, a menos que un significado diferente sea atribuido a
los mismos en otra parte de esta adicción. Los términos que no sean expresamente definidos se entenderán
en el sentido dados a ellos por el lenguaje técnico respectivo o por su significado y sentido naturales y
obvios, de acuerdo con su uso general.
```
```
La presente adición al contrato o reglamento de cuenta de ahorros o corriente se considera como accesoria,
por lo tanto, lo que no se contemple en esta adición, se suple con lo estipulado en el contrato o reglamento
de cuenta de ahorros o corriente. CLAUSULA PRIMERA – OBJETO El objeto de la presente adición es la
prestación del servicio de Transacciones Débito por parte de la EPO, utilizando para ello el sistema ACH
ofrecido por ACH COLOMBIA con quien la EPO ha suscrito un Contrato de Prestación de Servicios y de
Afiliación al Sistema ACH.
CLAUSULA SEGUNDA - OBLIGACIONES Y RESPONSABILIDADES DEL CO
```
1. El CO obtendrá la Autorización de Recaudo por escrito del CR, cerciorándose de que haya sido
debidamente otorgada, de la validez de la identidad del CR, que contenga datos ciertos y correctos, y que
se expida conforme al Anexo 2 adjunto, que hace parte integrante de esta adicción. La Autorización de
Recaudo para efectuar la Transacción Débito es propiedad de la EPR y por lo tanto se encuentra a su
entera disposición.


```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota

```
Agosto de 2024
Página 258 de 329
```
2. EL CO mantendrá una base de datos o registro permanentemente actualizado y de fácil manejo de dicha
Autorización de Recaudo, de su cancelación y en general de cualquier modificación que se pueda presentar.
3. El CO mantendrá los documentos originales de la Autorización de Recaudo de las Transacciones Débito
en sus archivos a título de depósito gratuito en beneficio de la EPO y de la EPR cuyo costo será asumido por
el CO en su totalidad. El archivo supone el empleo de dispositivos técnicos y de seguridad que garanticen
la confidencialidad de la información contenida en ellos y permitan la conservación de los documentos
originales de acuerdo con las normas vigentes, su observación física, su preservación, la reconstrucción de
su contenido en caso de contingencia y su entrega física y/o exhibición, dentro de las veinticuatro (24) horas
hábiles siguientes a la solicitud de autoridades arbitrales, judiciales, administrativas o de la EPO o de la EPR.
El CO en su calidad de depositario de las Autorizaciones de Recaudo será responsable ante la EPO, la EPR
y el CR hasta por culpa levísima por el uso indebido, pérdida, sustracción, deterioro, adulteración o por
cualquier otro hecho que comprometa o afecte la integridad física o ideológica de las Autorizaciones de
Recaudo.
El CO podrá contratar con sociedades especializadas en la guarda y custodia de documentos, el depósito
de los documentos originales de la Autorización de Recaudo de la Transacción Débito siempre y cuando se
cumplan con los requisitos enunciados anteriormente.
4. El CO previamente a la realización de Transacciones Débito, en forma obligatoria, debe ordenar a la
EPO la realización del proceso de Prenotificación hacia el CR titular de la Cuenta Receptora de la
Transacción. La Prenotificación deberá realizarse una sola vez por cada autorización que el CO ha obtenido
de un CR y previamente a la realización de la primera Transacción Débito dirigida hacia dicho CR. Para
adelantar el proceso de Prenotificación el CO procederá a la transmisión electrónica de datos o a la entrega
de medios magnéticos a la EPO, según sea el caso, como mínimo, con cuatro (4) Días Hábiles Bancarios de
anticipación a la fecha en que se ordene la primera Transacción Débito. Si el proceso de Prenotificación no
resultare exitoso en la EPR, deberá reiniciarse.
5. En caso de que el proceso de Prenotificación resultare exitoso, el CO procederá a la transmisión
electrónica de datos o a la entrega de medios magnéticos a la EPO, según sea el caso, que permitan a la EPO
contar con al menos dos (2) Días Hábiles Bancarios para efectuar la Transacción Débito. Si la Transacción
Débito no resultare exitosa en la EPR, deberá reiniciarse.
6. El CO asume total y completa responsabilidad por la Transacción Débito que origine, obligándose a
originarla únicamente con base en instrucciones y autorizaciones previas del CR. En caso de que las
Autorizaciones de Recaudo fueren erróneas, fraudulentas, imprecisas, incompletas o inexistentes, el CO será
responsable por el valor total de tales Transacciones, y por lo tanto autoriza irrevocablemente a la EPO
7. para debitar en cualquier momento de cualquiera de sus cuentas tales sumas de dinero para responder
ante la EPO, la EPR y/o el CR y devolver los fondos a la respectiva Cuenta Receptora.
8. Dicha autorización irrevocable se extiende al monto indebidamente afectado, como al daño emergente,
al lucro cesante, incluyendo, pero no limitándose a cualquier suma que la EPO, la EPR o el CR deba pagar
para reembolsar los débitos generados, a los intereses sobre tales sumas liquidados a la máxima tasa legal
permitida, así como a los costos legales, honorarios de abogados y demás expensas. El CO responderá hasta
por culpa levísima, fuerza mayor y caso fortuito, tal como los define la ley, por todos los perjuicios que se
causen a la EPO, a la EPR y al CR como consecuencia de la ocurrencia de cualquiera de estos hechos.


```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota

```
Agosto de 2024
Página 259 de 329
```
9. El CO deberá mantener en su(s) cuenta(s) con la EPO, en función del volumen de transacciones originado,
fondos suficientes para cubrir las posibles Devoluciones de Transacciones originadas por el CO a cargo de
las cuentas del CR, independientemente del tipo de Devolución. Igualmente, el CO será el responsable de
notificar al CR sobre tales Devoluciones e instaurar las acciones de cobranza pertinentes, así como corregir,
preparar y originar nuevamente las Transacciones a que haya lugar. En el supuesto en el cual el CR pida la
Devolución, dentro de los siguientes cuarenta y cinco (45) días calendario después de realizado el débito, el
CO autoriza irrevocablemente a la EPO para debitar las sumas necesarias de cualquiera de sus cuentas y
acreditarlas a la(s) respectiva(s) Cuenta(s) Receptora(s).
10. El CO acepta que los fondos están sujetos a pago definitivo de acuerdo con los procedimientos de la
EPO, por lo cual la EPO podrá demorar la disponibilidad de los fondos hasta el momento que se confirme
cada cobro. Sin embargo, si la EPO acuerda con el CO abonar el total de los fondos, sin esperar las posibles
Devoluciones por parte de la EPR, el CO deberá mantener en su(s) cuenta(s) con la EPO, en función del
volumen de transacciones originado, fondos suficientes para cubrir las posibles Devoluciones de
Transacciones generadas por la EPR.
11. El CO se obliga a atender y resolver los reclamos, que surjan por parte del CR, de la EPO y/o de la EPR,
dentro de los siguientes tres (3) Días Hábiles Bancarios después de recibido el reclamo.
12. El CO se obliga a no exceder los Límites Máximos establecidos por la EPO

```
CLAUSULA TERCERA - OBLIGACIONES Y RESPONSABILIDADES DE LA EPO
```
1. La EPO procesará las instrucciones de la Transacción Débito originadas por el CO, de acuerdo con la
información suministrada y verificada por éste.
2. La EPO acreditará los fondos al CO en la fecha en que la disponibilidad final de los fondos así lo permita;
igualmente debitará las Devoluciones el mismo día que las reciba, suministrando la información pertinente
en cada caso al CO.
3. La EPO dará su debido curso a las instrucciones de la Transacción Débito. Para tal efecto, toda instrucción
enviada por el CO se entiende previamente verificada por éste. No obstante, lo anterior, si la EPO tuviera,
en cualquier momento, duda sobre la procedibilidad, legitimidad o autenticidad de la operación, podrá
rehusarse a ejecutar cualquier instrucción de la Transacción Débito.
4. La EPO no es directa ni indirectamente garante de ninguna cuenta por cobrar a favor del CO y no será
responsable por ninguna demora o falla en la prestación del servicio a su cargo ni por la inexactitud de
cualquier dato(s) o instrucción(es) suministrado(s) por el CO.
5. La responsabilidad de la EPO no excederá en ningún caso por cada Transacción Débito del valor de esta,
suma ésta a la cual se limita la responsabilidad de la EPO.
6. La EPO no será responsable en casos de fuerza mayor o caso fortuito.


```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota

```
Agosto de 2024
Página 260 de 329
```
### CLAUSULA CUARTA - SUSPENSION DEL SERVICIO

Las partes acuerdan expresamente que los servicios objeto de esta adición pueden ser suspendidos
temporalmente por la EPO, por la existencia de fallas técnicas que le impidan prestar adecuadamente el
servicio, o en aquellos eventos en los cuales se presenten situaciones que impliquen riesgo técnico para su
sistema. En estos casos, la EPO informará al CO sobre la suspensión temporal de los servicios en el momento
en que éste solicite realizar una Transacción Débito, y una vez la causa de la suspensión temporal haya sido
subsanada, la EPO informará de este hecho al CO indicando el momento a partir del cual se reiniciará la
prestación del servicio suspendido.

CLAUSULA QUINTA - LAVADO DE ACTIVOS

1. El CO tiene expresamente prohibido utilizar los servicios objeto de esta adición para realizar
operaciones de lavado de activos, de acuerdo con lo establecido por el Artículo 247 A del Código Penal,
adicionado por el Artículo 9º de la Ley 365 de 1.997 y las demás normas que lo complementen, modifiquen
y adicionen.
2. El CO se obliga a cumplir con todas las medidas de seguridad establecidas por las autoridades
competentes y por la EPO para controlar las operaciones de lavado de activos. La EPO podrá llamar en
garantía al CO en los procesos que se inicien en su contra por parte de otro cliente, de otra Entidad
Participante, o de un tercero sobre hechos relacionados con esta adicción.

CLAUSULA SEXTA – DEVOLUCION DE TRANSACCIONES

Las partes acuerdan expresamente que en el evento en que un CR devuelva una Transacción Débito
ordenada por el CO, la EPO debitará de su Cuenta Originadora el valor de la Transacción devuelta. Las
partes acuerdan una penalidad a cargo del CO equivalente al ---% de la Transacción devuelta, para lo
cual el CO autoriza irrevocablemente a la EPO a debitar automáticamente cualquiera de sus cuentas.
Lo anterior, siempre y cuando la Transacción Débito haya sido efectuada erradamente o por culpa o
negligencia del CO. Esta penalidad no extinguirá la obligación principal, ni el pago de los perjuicios que
se causen a la EPO, a la EPR y/o al CR como consecuencia de la Transacción Débito y de su Devolución.

CLAUSULA SEPTIMA - COMISION O TARIFA

1. El CO se obliga a pagar a la EPO por cada Transacción Débito ordenada, la Comisión y/o Tarifa establecida
    en el Anexo 3, aun cuando la Transacción sea devuelta o rechazada. De tiempo en tiempo las Comisiones
    y/o Tarifas podrán ser modificadas unilateralmente por la EPO. Esta modificación entrará en vigor dentro
    de los (NUMERO DE DÍAS) Días Hábiles Bancarios siguientes a la fecha en que la EPO envíe al CO una
    comunicación informándole sobre la modificación de las Comisiones y/o Tarifas, entendiéndose que el
    CO las ha aceptado por el sólo hecho de continuar utilizando los servicios objeto de esta adicción.
2. Las Comisiones o Tarifas causadas con ocasión de la prestación de los servicios objeto de esta adición, serán
    pagadas (PERIODICIDAD).


```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota
```
```
Agosto de 2024
Página 261 de 329
```
### CLAUSULA OCTAVA - NOTIFICACIONES

1. Todas las notificaciones u otras comunicaciones que sean requeridas o permitidas bajo esta adición y sus
Anexos, se harán por escrito y serán suficientes si se entregan personalmente, con constancia del sello
correspondiente o evidencia similar de la parte que recibe, o si son enviadas por correo certificado, o
telegramas dirigidos en la siguiente forma:

A la EPO: Al CO:
[Dirección] [Dirección]
Atención: Atención:
Tel No.: Tel No.:

2. Las notificaciones u otras comunicaciones se entenderán recibidas en la fecha en que efectivamente se
reciben.
3. Cualquiera de las partes puede, mediante notificación dada según lo aquí previsto, cambiar la dirección o
cualquier otra información que se señala para efectuar las notificaciones.


```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota

```
Agosto de 2024
Página 262 de 329
```
### CLAUSULA NOVENA - CONFIDENCIALIDAD

El CO y la EPO se comprometen a guardar absoluta reserva sobre toda la información, políticas,
procedimientos o Transacciones que les sean dadas a conocer con ocasión del desarrollo de la presente
adición (en adelante “Información Confidencial”) y a usarla únicamente con los propósitos relativos a esta
adicción. Particularmente se considera Información Confidencial, cualquier información que reciban las
partes derivadas de las Transacciones Débito, de los procedimientos de seguridad, y de la información sobre
la EPO. La obligación de confidencialidad no se extiende en ningún caso a: (a) Información que fuere del
dominio público previamente a la fecha en la cual hubiere sido entregada a la correspondiente parte; (b)
Información que se haga pública lícitamente durante la vigencia de la presente adición; y, (c) Información que
deba ser entregada por mandato legal a las autoridades de cualquier orden. En caso de terminación de la
adición por cualquier causa, el CO devolverá a la EPO la totalidad de la Información Confidencial. En caso de
incumplimiento de la presente obligación de confidencialidad por parte del CO, deberá responder por todos
los daños y perjuicios que cause.

CLAUSULA DECIMA - DURACIÓN Y TERMINACION DE LA ADICIÓN

1. La presente adición tendrá una duración de un (1) año, a partir de la fecha de su firma y se prorrogará
automáticamente.
2. Cualquiera de las partes podrá terminar en cualquier momento esta adición notificando de este hecho a la
otra parte. Esta terminación será efectiva a partir de los cuarenta y cinco (45) días calendario siguientes a la
fecha en que la parte afectada reciba la notificación correspondiente de la parte que decida dar por terminado
o en la fecha indicada en dicha notificación si es posterior a aquella.
3. La terminación de esta adición no afectará las Transacciones Débito que hayan sido ordenadas por el CO
con anterioridad a la fecha en que se hizo efectiva la terminación. El CO continuará obligado a pagar a la EPO
las sumas que a esta fecha le adeude por concepto de los servicios prestados bajo esta adicción.
4. En caso de terminación de la presente adición, el CO se obliga a constituir una póliza de cumplimiento para
garantizar el pago de un débito incorrecto cuya reclamación y reversión sean hechas después de cuarenta y
cinco (45) días calendario siguientes de haberse terminado esta adición y cuyo valor asegurado será de ___
salarios mínimos legales mensuales.

CLAUSULA DECIMO PRIMERA - CESION DE LA ADICIÓN
Esta adición no podrá ser cedida total ni parcialmente por el CO sin previa autorización de la EPO. CLAUSULA
DECIMO SEGUNDA - ARBITRAMENTO
Las partes acuerdan que cualquier controversia o reclamo que surja entre ellas con ocasión de la ejecución,
terminación o interpretación de la presente adición, que no pueda ser resuelto por las partes, se someterá a
la decisión de un Tribunal de Arbitramento cuyo fallo será en derecho y de acuerdo con las siguientes reglas:

Designación: Los miembros del Tribunal serán designados directamente y de común acuerdo entre las partes
y a falta de acuerdo por el Centro de Arbitraje y Conciliación Mercantiles de la Cámara de Comercio de Bogotá.

Miembros: Los miembros del Tribunal de Arbitramento serán ciudadanos colombianos y se sujetarán a los
dispuestos en la Ley 446 de 1998 y demás normas que lo modifiquen adicionen o complementen.


```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota

```
Agosto de 2024
Página 263 de 329
```
Conformación: El Tribunal estará integrado por un (1) árbitro.

Arbitramento Técnico: Se someterán a él todas las controversias de carácter eminentemente técnico; el
Tribunal estará conformado por un (1) árbitro quien deberá ser experto en el tema objeto de la controversia,
bien sea por formación profesional o por experiencia. La Organización Interna del Tribunal se sujetará a las
reglas previstas para el efecto por el Centro de Arbitraje y Conciliación Mercantiles de la Cámara de Comercio
de Bogotá.

Arbitramento en Derecho: Se someterán a él todas las controversias de carácter eminentemente jurídico; el
Tribunal estará conformado por un (1) árbitro quien deberá ser Abogado Titulado. La Organización Interna
del Tribunal se sujetará a las reglas previstas para efecto por el Centro de Arbitraje y Conciliación Mercantiles
de la Cámara de Comercio de Bogotá.

Naturaleza del Arbitramento: Si dentro de los cinco (5) días hábiles siguientes a la solicitud de Arbitramento
las partes no se ponen de Acuerdo sobre si la controversia es de carácter eminentemente técnico o no, la
correspondiente consulta deberá someterse al criterio de un amigable componedor seleccionado por la
Cámara de Comercio de Bogotá, a quienes las partes deberán presentar sus argumentos por escrito dentro
de los cinco (5) días hábiles siguientes a la aceptación del nombramiento. El amigable componedor deberá
decidir sobre el asunto dentro de los dos (2) días hábiles siguientes a dicha fecha.


```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota

```
Agosto de 2024
Página 264 de 329
```
Designación del árbitro: Si las partes no llegaren a un acuerdo sobre la designación del árbitro dentro de los
cinco (5) días hábiles siguientes a la solicitud de Arbitramento o de la decisión del amigable componedor, éste
será designado por la Cámara de Comercio de Bogotá.

Procedimiento Arbitral: El procedimiento arbitral se sujetará a las normas contenidas en los artículos 121 y
ss. de la Ley 446 de 1.998.

Las partes acuerdan que todas las controversias, procesos y acciones que tengan que ver con cobros,
continúen conociéndose a través de la jurisdicción ordinaria, así como los asuntos que tengan un trámite
abreviado o sumario.

CLAUSULA DECIMO TERCERA - IMPUESTO DE TIMBRE

Por ser este documento una adición al contrato de cuenta corriente o de ahorros no genera impuesto de

timbre. En constancia se suscribe a los ___ días del mes de ______.

La EPO: El CO:

Nombre: Nombre:
Cargo: Cargo:
Identificación: Identificación:


```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota
```
```
Agosto de 2024
Página 265 de 329
```
Anexo 1. DEFINICIONES

1. ACH o ACH COLOMBIA: Operador que presta servicios de recepción, validación, procesamiento, compensación y
    distribución de transacciones electrónicas que comprometen fondos de terceros, las cuales son procesadas por
    autorización y bajo la responsabilidad de los participantes.
2. Autorización de Recaudo: Autorización otorgada por el Cliente Receptor (CR) a su Entidad Participante Receptor
    (EPR) para que acepte las transacciones débito generadas por el Cliente Originador (CO).
3. Cliente Originador (CO): Persona jurídica con quien el participante originador (EPO) celebra esta adición y a nombre
    de quien utiliza los servicios de ACH, ordenando Transacciones Débito desde su(s) cuenta(s) a través del sistema
    ACH hacia una o varias cuentas de otro(s) Cliente(s) Receptor(es) (CR), radicada(s) en el participante Receptor
    (EPR), previa Autorización de Recaudo.
4. Cliente Receptor (CR): Persona natural o jurídica, cliente de el participante Receptor (EPR), quien ha otorgado
    una Autorización de Recaudo al Cliente Originador (CO), para que envíe o inicie una transacción débito y a la
    Entidad Participante Receptora (EPR) para que debite de su cuenta sumas determinables por un plazo
    determinado y que la acrediten a la cuenta del Cliente Originador (CO) a través del sistema ACH COLOMBIA.
5. Contrato de Prestación de Servicios y Afiliación al Sistema ACH: Acuerdo celebrado entre las entidades Participantes
    y ACH COLOMBIA bajo el cual se regulan las relaciones que surjan entre ellas con ocasión de las Transacciones
    Débito originadas y/o recibidas por ellas y que se realizan a través del sistema ACH.
6. Cuenta Originadora: Cuenta corriente, de ahorros, o similar, en la cual se acreditan los valores debitados de la
    Cuenta Receptora correspondientes a la Transacción Débito.
7. Cuenta Receptora: Cuenta corriente, de ahorros, o similar desde la cual se debitan los valores correspondientes a
    la Transacción Débito para ser acreditados a la Cuenta Originadora.
8. Devolución: Transacción mediante la cual el participante Receptor (EPR) informa a el participante originador (EPO)
    a través del sistema ACH, que la transacción no fue aceptada por no cumplir con las condiciones establecidas o
    porque no fue aceptada por el Cliente Receptor (CR) en los términos del Anexo 4.
9. Día Hábil Bancario: Día hábil de atención bancaria al público en la ciudad de Bogotá, de acuerdo con lo estipulado
    por la Superintendencia Bancaria.
10. Entidad Participante Originadora (EPO): Entidad Participante que origina Transacciones electrónicas por mandato
    de un Cliente Originador (CO), a través del Sistema ACH.
11. Entidad Participante Receptora (EPR): Entidad Participante que recibe órdenes para efectuar Transacciones
    electrónicas a través del sistema ACH, con el objeto de aplicarlas a la(s) cuenta(s) de su(s) Cliente(s) Receptor(es)
    (CR).
12. Límite(s) Máximo(s): Valor(es) tope autorizado(s) para ser enviado(s) por el Cliente Originador (CO) a través del
    sistema ACH. El tope máximo diario para cada transacción es de ____________; el tope máximo diario desde
    un Cliente Originador (CO) hacia una misma cuenta del Cliente Receptor es de ____________; el tope máximo
    diario del total de transacciones enviadas por el Cliente Originador (CO) hacia el participante originador (EPO) es
    de _______.
13. Prenotificación: Es una Transacción no monetaria cuyo propósito es obtener una validación acerca de la existencia
    y condiciones de la Cuenta Receptora; esta se debe hacer por una vez. El Cliente Originador (CO)
    adicionalmente entrega los datos de la identificación del Cliente Receptor (CR) a su Entidad Participante
    Originadora (EPO), quien los debe enviar como datos de la transacción de Prenotificación y el participante
    Receptor (EPR) debe validar que la identificación del titular de la cuenta es igual a la que tiene registrada para la
    Cuenta Receptora.
14. Tarifa o Comisión: Suma de dinero que el Cliente Originador (CO) está obligado a pagar a el participante originador
    (EPO) como contraprestación por los servicios prestados bajo esta adicción. Se entenderá por Tarifa cuando la
    suma de dinero es una suma fija; y se entenderá por Comisión, cuando la suma de dinero sea un porcentaje
    del valor de la Transacción Débito que se efectúe a través de ACH.
15. Transacción: Conjunto de datos enviados a través de ACH que especifican las condiciones para realizar una
    operación monetaria o no monetaria.
16. Transacción Débito: Transacción monetaria realizada a través del sistema ACH en la cual el Cliente Originador
    (CO) ordena a su Entidad Participante Originadora (EPO) generar una transacción hacia la Entidad Participante
    Receptora (EPR) con el objeto de que ésta debite una suma determinada de la Cuenta Receptora, para acreditarla
    a la Cuenta Originadora. La transacción débito permite al titular de la Cuenta Receptora realizar pagos al Cliente
    Originador.


```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota
```
```
Agosto de 2024
Página 266 de 329
```
Anexo 2. Autorización de recaudo


```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota
```
```
Agosto de 2024
Página 267 de 329
```
### REGLAMENTO

```
Como titular(es) de la cuenta señalada (Cliente), autorizo(mos) incondicionalmente y por un término
indefinido, por medio de este documento, lo siguiente: (1) a el participante a debitar de la cuenta aquí
indicada el valor que corresponde a la transacción débito y entregar dicho valor a la Empresa Recaudadora;
(2) a la Empresa Recaudadora a conservar el presente documento en su sede; (3) a la Empresa Recaudadora
a enviar la información aquí contenida, de manera electrónica; que ante cualquier error de la Empresa
Recaudadora en la conversión electrónica de la Autorización de Recaudo , efectuaré(mos) los reclamos única
y exclusivamente a la Empresa Recaudadora;(4) a el participante a debitar la cuenta aquí indicada en una
fecha diferente a la inicialmente prevista y determinada entre la Empresa Recaudadora y el Cliente, tan sólo
en aquellos casos en los que la Empresa Recaudadora tenga inconvenientes de índole técnico u operativo
que no le permitan debitar la cuenta oportunamente; (5) extender esta misma autorización a modificaciones
obligatorias que realice el participante.
```
```
Como titular(es) de la cuenta señalada me(nos) obligo(amos) a: ( 1 ) mantener fondos suficientes en la cuenta
indicada para cubrir las operaciones; (2) proveer la autorización de parte de todos los titulares de la cuenta
en este documento o las copias de este que fueren necesarias, o en su defecto a asumir las consecuencias
que se deriven de no declarar la condición de manejo de firmas conjuntas de la cuenta, liberando así a la
Empresa Recaudadora y a el participante de toda responsabilidad.
```
```
Como titular(es) de la cuenta señalada, declaro(amos) que conozco(cemos) y acepto(amos) lo siguiente:
(1) que el débito autorizado se podrá hacer ordinariamente durante el tiempo y la oportunidad indicados,
siempre que la cuenta aquí señalada tenga fondos disponibles y que, si el día no fuere hábil, el débito se
hará el siguiente día hábil. No obstante, si en esa oportunidad no hay fondos disponibles en la cuenta, el
débito podrá hacerse cuando existan fondos disponibles; (2) que el participante donde tengo(tenemos) la
cuenta podrá abstenerse de hacer el débito si no existen fondos disponibles para ello o si se presenta alguna
causal que lo impida; (3) que si deseo(amos) autorizar a otra Entidad Participante, a otro número o tipo de
cuenta, debo(emos) cancelar el formato vigente y diligenciar una nueva Autorización de Recaudo; (4) que la
presente Autorización de Recaudo solamente podrá ser cancelada mediante comunicación escrita enviada a
la Empresa Recaudadora y a la sucursal de el participante donde tengo(nemos) la cuenta, con una
anticipación no inferior a diez (10) días hábiles a la fecha a partir de la cual se desee hacer efectiva la
cancelación; (5) que debo(emos) dirigir las reclamaciones o solicitudes de devolución, en cualquier
momento, a la Empresa Recaudadora con copia a la sucursal de la Entidad Participante donde tengo(enemos)
radicada la cuenta en un plazo máximo de cuarenta y cinco (45) días calendario a partir de la fecha de
aplicación del débito; (6) que puedo(podemos) dar una orden de no pago a la sucursal de la Entidad
Participante donde tengo(tenemos) la cuenta, para una transacción débito especifica con una antelación no
inferior a cinco (5) días hábiles antes de la fecha de aplicación del débito; (7) que presentar órdenes de no
pago o solicitud de devoluciones reiteradas, puede implicar un costo adicional o ser causal de cancelación
del servicio por parte de la Empresa Recaudadora o por parte del participante donde tengo(tenemos) la
cuenta.
Al dar la presente autorización soy(somos) consciente(s) que pueden surgir conflictos que impliquen la
necesidad de revelar la documentación e información aquí contenida.
```

```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota
```
```
Agosto de 2024
Página 268 de 329
```
```
Anexo 4. Causales de Devolución para Transacciones Débito
```
Causal Descripción Estándar de la Devolución
Detalle adicional de la Devolución (Opcional
Recomendado)

### R01

```
Fondos Insuficientes: El saldo disponible no es suficiente para cubrir el valor de la transacción débito. Aplica
para transacciones monetarias débito.
```
### R02

```
Cuenta Cerrada: Cuenta cerrada por
orden del Usuario Receptor o por el
participante originador. Aplica para
transacciones de prenotificación débito
y para transacciones monetarias débito.
```
```
− Cuenta Saldada: cuenta activa que ha sido cerrada por orden del
Usuario Receptor.
− Cuenta Cancelada: cuenta activa que ha sido cerrada por orden del
participante Receptor.
```
```
R03 Cuenta No Abierta: El número de cuenta registrado no corresponde a una cuenta asignada o abierta. Aplica para
transacciones de prenotificación débito y para transacciones monetarias débito.
```
### R04

```
Número de Cuenta Inválido: El número
de la cuenta es incorrecto. Aplica para
transacciones de prenotificación débito
y para transacciones monetarias débito.
```
```
− La estructura del número de cuenta no es válida.
− El dígito de chequeo no es válido.
− Número incorrecto de dígitos.
```
### R06

```
Devolución Solicitada por la Entidad
Participante Originadora: La Entidad
Participante Originadora ha solicitado a
la Entidad Participante Receptora,
devolver una transacción. Aplica para
transacciones monetarias débito.
```
```
− Por conocer que la transacción fue enviada por error.
− Por conocer que la cuenta pertenece a la lista Clinton.
```
### R07

```
Autorización de Recaudo Revocada por el Usuario Receptor: El Usuario Receptor ha revocado o cancelado en
forma definitiva la autorización previamente dada al Usuario Originador para debitar su cuenta en el futuro.
Aplica para transacciones monetarias débito.
```
### R08

```
Orden de No Pago: El Usuario Receptor de una transacción débito periódica ha dado orden de no pago a una transacción débito específica
para que no sea aplicada. La Entidad Participante Receptor debe verificar el propósito del Usuario e Receptor, cuando hace una solicitud de
orden de no pago, esto con el fin de asegurarse que no se trata de una revocación de autorización (R07). Aplica para transacciones monetarias débito.
```
### R09

```
Fondos no Disponibles: El saldo total es suficiente para cubrir esta transacción, sin embargo, el saldo disponible
no
es suficiente para cubrir la transacción débito. Aplica para transacciones monetarias débito.
Algunas razones para aceptar una devolución solicitada por el Usuario
Receptor son:
− Usuario Originador no autorizado: La Entidad
Participante Receptora ha sido notificada por su Cliente
Receptor, que el Cliente Originador de la transacción no ha sido
autorizado para debitar su cuenta.
− No existe autorización o prenotificación: No fue encontrada la
autorización o acuerdo con el Usuario Receptor o no existe
prenotificación.
− Monto no autorizado: El valor de la transacción débito no
corresponde al monto autorizado por el Usuario e Receptor.
```

```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota
```
```
Agosto de 2024
Página 269 de 329
```
```
Anexo 4. Causales de Devolución para Transacciones Débito
```
Causal Descripción Estándar de la Devolución
Detalle adicional de la Devolución (Opcional
Recomendado)

### R10

```
Devolución de una transacción débito
por solicitud del Usuario Receptor
(Persona Natural): El Usuario Receptor,
no acepta la transacción débito a su
cuenta por una razón específica. Aplica
para transacciones monetarias débito.
```
```
− Fecha de transacción errada: La fecha de la transacción débito no
corresponde a la fecha autorizada por el Usuario Receptor.
− Transacción débito fraudulenta.
− Autorización de Recaudo cancelada: El Usuario Receptor ha
cancelado previamente la autorización de recaudo.
− Débito Duplicado: El Usuario Receptor notifica el recibo de una
transacción débito duplicada en su cuenta.
```
### R12

```
Sucursal Vendida a otra Entidad Participante: Una Entidad Participante puede continuar recibiendo
transacciones con destino a una cuenta de una sucursal que fue vendida a otra Entidad Participante. Como la
Entidad Participante Receptora no puede mantener la cuenta más tiempo y no está autorizada para registrar
la transacción, debe hacer la devolución de esta, a el participante originador. Aplica para transacciones de
prenotificación débito y para transacciones monetarias débito.
```
### R14

```
Muerte del delegado o Representante: El delegado o Representante (apoderado) del Usuario Receptor, sea
este una persona o una institución autorizada para recibir transacciones en nombre de otras personas, ha
muerto o ha perdido esa facultad. El beneficiario o Usuario Receptor no ha muerto. Aplica para transacciones
de prenotificación débito y para transacciones monetarias débito.
```
### R15

```
Muerte del Beneficiario o Titular de la Cuenta: El Beneficiario, Usuario Receptor o Titular de la cuenta ha muerto.
Aplica para transacciones de prenotificación débito y para transacciones monetarias débito.
```
### R16

```
Cuenta Inactiva o Cuenta Bloqueada:
Cuenta inactiva por no tener
movimiento en un periodo de tiempo
y/o por solicitud del titular de esta o por
la Entidad Participante Receptora.
Aplica para transacciones de
prenotificación débito y para
transacciones monetarias débito.
```
```
Cuenta Inactiva: Por no tener movimiento en un período específico
de tiempo.
Cuenta Bloqueada: Por solicitud del titular de la cuenta o Usuario
Receptor y/o por la Entidad Participante Receptor.
```
### R17

```
La Identificación no coincide con Cuenta del Usuario Receptor. La estructura del número de cuenta y el dígito
de chequeo son válidos, pero el número de cuenta no corresponde con el número de identificación del Usuario
Receptor registrado. Aplica para transacciones de prenotificación débito y para transacciones monetarias
débito.
```
### R20

```
Cuenta No Habilitada para recibir
transacciones: Cuenta de naturaleza
especial que está limitada para recibir
transacciones débito o crédito. Aplica
para transacciones de Prenotificación
```
```
− Transacción no puede ser aplicada debido a que el Usuario
Receptor está asociado a listas restrictivas: La información
asociada al Usuario Receptor (nombre, id, adenda) objeto de la
transacción, No permite aplicar transacciones porque genera
coincidencia total o parcial con una o varias listas restrictivas.
− Transacción no puede ser aplicada debido a que el usuario
```

```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota
```
```
Agosto de 2024
Página 270 de 329
```
```
Anexo 4. Causales de Devolución para Transacciones Débito
```
Causal Descripción Estándar de la Devolución
Detalle adicional de la Devolución (Opcional
Recomendado)
débito y para transacciones monetarias
débito

```
Originador está asociado a las listas restrictivas: La información
asociada al Usuario Originador (nombre, id, descripción de lotes,
datos discrecionales del Usuario originador) remitente de la
transacción, NO permite aplicar transacciones porque genera
coincidencia total o parcial con una o varias listas restrictivas.
Las posibles listas restrictivas que la Entidad Participante Receptor
podrá validar son: OFAC, ONU, Banco de Inglaterra, Unión Europea o
listas restrictivas para Colombia.
− Cuentas Usadas en Medios Políticos: si la cuenta que se debe
afectar es usada en medios políticos como campañas o similar,
podría utilizarse esta causal.
```
### R29

```
Devolución de una transacción débito por solicitud del Usuario Receptor (Persona Jurídica): La Entidad
Participante Receptora ha sido notificada por su Usuario Receptor Corporativo (no consumidor), que el Usuario
Originador de la transacción no ha sido autorizado para debitar su cuenta. Aplica para transacciones de
prenotificación débito y para transacciones monetarias débito.
```
```
R30 Clientecuenta^ asociadaReceptor ano una^ habilitado persona^ paraNatural^ recibir transacciones^ a^ depósitos^ Electrónicos:^ La^ cuenta^ destino^ no^ es^ una^
```
```
R31 Pre-notificación^ no^ procesada^ por^ parte^ de^ la^ Entidad^ Receptora:^ Para^ Efectuar^ la^ devolución^ de^ transacciones^
de pre-notificación débito cuando no se encuentre la información total o parcial del campo 3 del registro de
adenda, establecida como de obligatoria inclusión por parte de las Entidades Originadoras
```
```
R33 Devolución^ de^ una^ transacción^ de^ depósito^ electrónico^ cuando^ excede^ los^ límites^ establecidos:^ Monto^ no^
autorizado, el valor de la transacción crédito o débito con destino a depósito electrónico excede los topes
definidos.
```
```
R35 Tipo^ de^ Cuenta^ Errada:^ La^ transacción^ no^ puede^ ser^ aplicada^ debido^ a^ que^ el^ tipo^ de^ cuenta^ está^ errado.^
```
## ANEXO 14. Vinculación Entidades Participantes al servicio de ACH Transferencias Interbancarias (EF)

### EXO 15. NOVEDAD TÓKEN DE USUARIO


```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota
```
```
Agosto de 2024
Página 271 de 329
```
### VINCULACIÓN ENTIDADES FINANCIERAS AL SERVICIO DE ACH TRANSFERENCIAS INTERBANCARIAS

### DATOS DE LA AUTORIZACIÓN

FECHA DE LA SOLICITUD (^) Dia Mes Año
TIPO DE NOVEDAD: ☐ Inclusión ☐ Eliminación ☐ Modificación
DATOS BÁSICOS DE LA ENTIDAD FINANCIERA
RAZÓN SOCIAL:
NOMBRE COMERCIAL: (^)
NIT: (^)
DIRECCIÓN:
CIUDAD: (^)
NÚMEROS TELEFÓNICOS:
INFORMACIÓN BÁSICA PARA CONFIGURACIÓN
CÓDIGO RUTA Y TRANSITO:
DEVOLUCIÓN DÉBITO ☐ CUR (Código único de referencia.) “Doble referencia” para solicitudes debito (Afecta Adenda).
VALOR MÍNIMO AUTORIZADO ☐^ ESTÁNDAR^
(Marque si utilizará el valor de $1.500.000. 000.oo)
☐ PERSONALIZADO (Indique el valor)
$
CUENTA SEBRA: PORTAFOLIO:
RESPONSABLE TÉCNICO PARA LAS PRUEBAS
NOMBRE: (^)
CARGO ACTUAL:
ÁREA:
DIRECCIÓN: CIUDAD:
TELÉFONO: CELULAR:
E-MAIL:
INFORMACIÓN DEL REPRESENTANTE LEGAL
NOMBRE: CEDULA: (^)
“Yo, ____________________________, identificado (a) con la cedula de ciudadanía No. _____________, obrando
en nombre y representación de _____________________, identificada con el Nit. _____________, manifiesto de
forma libre, consciente, expresa e informada que autorizo a ACH COLOMBIA S.A., para recolectar, almacenar,
organizar, usar, transmitir o transferir, y en general, tratar de manera directa, o a través de un tercero encargado
del tratamiento, la información personal de acuerdo con las siguientes finalidades: i) Proveer nuestros productos
o servicios; II) Comunicar eficientemente información propia de ACH Colombia S.A. y/o aliados comerciales, sobre
productos, servicios y ofertas; III) Informar sobre nuevos productos o servicios; IV) Dar cumplimiento a
obligaciones contraídas con nuestros clientes y/o proveedores; v) Evaluar la calidad de los servicios; vi) Informar
sobre cambios de nuestros productos o servicios; VII) Participar en programas de lealtad con beneficios; VIII)
Realizar estudios de mercado sobre hábitos de consumo; IX) así como cualquier otra relacionada con nuestros
productos y/o servicios para el cumplimiento de las obligaciones contractuales.


```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota
```
```
Agosto de 2024
Página 272 de 329
```
### VINCULACIÓN ENTIDADES FINANCIERAS AL SERVICIO DE ACH TRANSFERENCIAS INTERBANCARIAS

De igual forma, declaro que he sido informado que ACH COLOMBIA S.A. cuenta con una Política de Tratamiento
de Datos Personales en los términos de la Ley 1581 de 2012, el decreto 1377 de 2013 y demás normas que lo
regulen o complementen, a la cual tengo acceso a través de su página web”

Yo, en mi calidad de Representante Legal de la entidad financiera arriba determinada, por medio de la presente y
bajo mi absoluta responsabilidad, autorizo al responsable técnico designado para las pruebas, quien tendrá la
responsabilidad de garantizar la certificación en nombre de la entidad financiera.
*NOTA: Cualquier cambio tecnológico, de seguridad, de conectividad o de aplicativo que afecte la disponibilidad,

seguridad o funcionamiento del sistema Integra ACH, será informado con por lo menos quince (15) días de

anticipación a ACH COLOMBIA y será probado de acuerdo con los requerimientos de ACH COLOMBIA.

### ________________________________________

Firma del Representante Legal
CC.

## FORMATO DE USUARIOS SERVICIO INTEGRA ACH PARA EF

## DATOS DE LA AUTORIZACIÓN

FECHA AUTORIZACION: (^) TIPO DE NOVEDAD: Creación
Eliminación Modificación
ENTIDAD PARTICIPANTE: (^) NIT:
Administrador de Usuarios: Su labor principal es administrar los requerimientos de creación, modificación,

## Entidad Participante. eliminación, bloqueo y desbloqueo de usuarios del sistema Transferencias Interbancarias al interior de su

Entidad Participante.

## DATOS BÁSICOS DEL ADMINISTRADOR DE USUARIOS PRINCIPAL

NOMBRE TIPO DE DOCUMENTO (^)
APELLIDOS NUMERODOCUMENTO^ DE^


```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota
```
```
Agosto de 2024
Página 273 de 329
```
CORREO ELECTRONICO

TELEPONO EXTENSION CELULAR

CARGO

FIRMA

## DATOS BÁSICOS DEL ADMINISTRADOR DE USUARIOS SUPLENTE

NOMBRE TIPO DE DOCUMENTO (^)
APELLIDOS NUMERODOCUMENTO^ DE^
CORREO ELECTRONICO
TELEPONO EXTENSION TELEPONO:
CARGO
FIRMA:

## DATOS BÁSICOS DEL USUARIO FACTURACIÓN – RECLAMOS

NOMBRE TIPO DE DOCUMENTO

APELLIDOS

```
NUMERO DE
DOCUMENTO
```
CORREO ELECTRONICO EXTENSION TELEPONO:
TELEPONO
CARGO

FIRMA:

## INFORMACIÓN DEL REPRESENTANTE LEGAL

NOMBRE: TIPO DE DOCUMENTO (^)
APELLIDO (^)
NUMERO DE
DOCUMENTO (^)
Yo, en mi calidad de Representante Legal de la Entidad arriba determinada, por medio de la presente y
bajo mi absoluta responsabilidad, autorizo a partir de la fecha, se incluyan las siguientes novedades: (1)
Se incluya el Administrador de Usuarios Principal y Suplente (Su labor principal es administrar los
requerimientos de creación, modificación o inactivación de usuarios, Administración de Roles del sistema
TRANSFERENCIAS INTERBANCARIAS al interior de su Entidad). Igualmente me hago responsable por la
información de la Entidad consignada en este formato, así como los datos de los correos electrónicos de
los usuarios con los cuales se crearán las contraseñas para el acceso al sistema, asegurando que se
cumple con los requerimientos legales y operativos exigidos por ACH COLOMBIA.
De conformidad con lo dispuesto en la Ley 1581 de 2012 y el Decreto 1377 de 2013, autorizo a ACH
COLOMBIA S.A. para recolectar, almacenar, usar, procesar y en general proceder con el tratamiento de
los datos personales contenidos en el presente formato. Doy mi autorización expresa para que ACH
COLOMBIA recolecte y de cualquier otra manera traten los datos personales de forma directa o a través


```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota
```
```
Agosto de 2024
Página 274 de 329
```
de sus empleados, asesores y/o terceros encargados del tratamiento, los cuales son indispensables para
cumplir con los propósitos de los servicios habilitados por ACH COLOMBIA”.

__________________________________
Firma del Representante Legal

```
Nota: Adjuntar un Certificado de Representación Legal con una vigencia no mayor a 60 días
```
## INFORMACION DE DOMINIO DE CORREO Y SUFIJO DE USUARIOS EN INTEGRA ACH PARA EF

DOMINIO DE CORREO: _Escribir los dominios de correos de usuarios de la entidad y externos que requieran acceder_

## técnico. acceder al servicio de transferencias interbancarias. Ejm: Funcionarios de la Entidad, contact center, proveedor

## DOMINIO DE CORREO:

## transferencias interbancarias. Ejm: Banco Rojo Sufijo:bancorojo usuario/bancorojo SUFIJO DE USUARIOS: Escribir el sufijo que el usuarios de la entidad requiere para acceder al servicio de

_transferencias interbancarias. Ejm: Banco Rojo Sufijo: banco rojo usuario/banco rojo_

## SUFIJO DE USUARIOS:


```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota
```
```
Agosto de 2024
Página 275 de 329
```
## ANEXO 16. DESCRIPCIÓN DE LOTE

La Entidad Participante debe utilizar el campo de Descripción de Lote contenido en el Registro de Encabezado
de Lote (campo 7 de 10 posiciones), de forma estándar, para lo que se sugieren las siguientes descripciones:

```
DESCRIPCIÓN DE LOTE
NOMBRE DESCRIPCIÓN
ADMON Pago de administración
AHORROS Ahorros
APORTES Pago de aportes
ARRIENDOS Pago de arriendos
BEEPER Pago de beeper
CEDULA CAP Pago cédulas de capitalización
CELULAR Pago de celulares
CESANTIAS Pago de cesantías
CLUB Pago cuota club
COLEGIO Pago de matrículas escolares
COMISIONES Pago de comisiones
CONTRATIST Pago de contratistas
DIVIDENDOS Pago de dividendos – acciones
DONACION Pago de donaciones
HONORARIO Pago de honorarios
IMPUESTOS Pago de impuestos
INTERESES Pago de intereses
NOMINA Pago de nómina
OTROS Otro tipo de pago
SSS Pagos PSE Seguridad Social
PENSIONES Pago de pensiones
PREPAGADA Pago medicina prepagada
PRESTAMOS Pago de cuota de préstamos
PROVEEDOR Pago a proveedores
RENDIMIENT Pago de rendimientos
RIESGOS P Riesgos Profesionales
SEGURO Pago de seguros
SERV PUBLI Pago de servicios públicos
SUSCRIPCI Pago de suscripciones
TARCREDITO Pago de tarjeta de crédito
TRASLADOS Transferencias de fondos
TV X CABL Pago televisión por cable
```

```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota

```
Agosto de 2024
Página 276 de 329
```
```
TV SATELIT Pago de televisión satelital
UNIVERSIDA Pago matrículas universitarias
ARRIENDO Pago de arriendos
NOMINA Pago de Nominas
SEGUROS Pago de Seguros
CELULARES Pago de Celulares
COMISION Pago de Comisiones
PROVEEDORES Pago de Proveedores
TRASLADO Transferencia de fondos
```

```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota
```
```
Agosto de 2024
Página 277 de 329
```
## ANEXO 17. AVISOS Y MENSAJES DE ERROR

### AVISOS

### ID TIPO DESCRIPCIÓN

```
32 Aviso Sedevol^ hau^ ccaión.mbi ado^ información^ de^ la^ transacción^ original^ al^ generar^ la^
```
83 Aviso^ Este^ lote^ tiene^ transacciones^ para^ almacenar.^

### ERROR FATAL OBSERVACIONES

### ID DESCRIPCIÓN

```
1 El archivo no llegó al servidor. Intente
enviarlo nuevamente.
```
```
El archivo no llego al servidor durante el cargue, por lo
cual no puede ser procesado.
3 La longitud de alguno de los registros es
incorrecta.
```
```
Se debe evaluar que al desblocar en líneas de 106
caracteres la longitud de todas líneas debe ser
también de 106 hasta la última. (Contar todos los
caracteres y el resultado debe ser un múltiplo de 106)
4 La secuencia del tipo de registro es
incorrecta, o el tipo de registro es
inválido.
```
```
Se debe evaluar en el campo "tipo de registro" lo
siguiente
Deben venir en secuencia ascendente en el siguiente
orden (1 5 6 7 8 9)
Únicamente se aceptan los valores (1 5 6 7 8 9) en caso
contrario se muestra mensaje de descripción.
Debe validar que el Archivo (Inicia 1 finaliza 9)
Debe validar que el Lote (Inicia 5 finaliza 8)
Debe validar que la Transacción (Inicia 6 finaliza7)
Secuencia en manual de servicio 6.1.3
5 El número del lote no está en secuencia
ascendente o está incorrecto.
```
```
El campo "numero de lote" está ubicado en las líneas
5 y se debe validar lo siguiente:
Debe ser en orden ascendente
En caso de no cumplir el orden el archivo se debe
rechazar
7 El número de secuencia de la transacción
no está en secuencia ascendente.
```
```
El campo "numero de secuencia" está ubicado la línea
6 y se debe validar lo siguiente:
```
- Debe ser en orden ascendente
En caso de no cumplir el orden el archivo se debe
rechazar.
8 El código de la Entidad Financiera
Originadora de la transacción es
incorrecto.

```
El "Código de entidad financiera originadora" está
ubicado en la línea 5 en el campo Código entidad
financiera originadora y se debe validar lo siguiente:
```

```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota

```
Agosto de 2024
Página 278 de 329
```
- El campo mencionado debe ser validado contra la
tabla de vinculación de bancos, si no está en la tabla
de bancos se debe rechazar.
9 El archivo está abocado
incorrectamente.

```
Se debe evaluar que el archivo venga abocado en una
sola línea. Si el archivo no viene alocado debe
rechazarse.
10 El código de prioridad del archivo es
incorrecto.
```
```
Se "código de prioridad" está ubicado en la Línea 1 y
debe validar lo siguiente:
```
- Que el valor del campo debe ser igual a "01"en caso
de ser diferente el archivo se debe rechazar.
11 El código de la entidad destino inmediato
es incorrecto.

```
El "Código de la entidad destino inmediato" está
ubicado en las líneas 1 en el campo "Código de la
entidad destino inmediato" y se debe validar lo
siguiente:
```
- El campo mencionado anteriormente debe ser
validado contra la tabla de vinculación de bancos
adicionando el digito de chequeo, si no está en la tabla
de bancos se debe rechazar.
12 El código de la entidad origen inmediato
es incorrecto.

```
El "Código de la entidad origen inmediato" está
ubicado en las líneas 1 en el campo "Código de la
entidad origen inmediato" y se debe validar lo
siguiente:
```
- El campo mencionado anteriormente debe ser
validado contra la tabla de vinculación de bancos
adicionando el digito de chequeo, si no está en la tabla
de bancos se debe rechazar.
13 La fecha de creación del archivo es
incorrecta.

```
La "Fecha de creación del archivo" está ubicado en la
línea 1 en el campo "Fecha de creación del archivo" y
se debe validar lo siguiente:
```
- El campo mencionado debe contener la fecha del día
actual en formato YYYYMMDD en caso de ser
diferente el archivo se debe rechazar.
14 El identificador del archivo es incorrecto. El identificador de archivo está ubicado en la línea 1
en el campo "Identificador de archivo" y para validar
este campo se debe realizar lo siguiente:
- Identificar el nombre del archivo recibido.
- Extraer el valor del número consecutivo del nombre
del archivo
- Extraer el valor del campo identificador de archivo
- Buscar el valor del número de consecutivo en la tabla
relacionada (identificadores de archivo)


```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota

```
Agosto de 2024
Página 279 de 329
```
- Comparar de acuerdo con la tabla (identificadores de
archivo) el valor del campo "identificador de archivo"
con el valor de la tabla en la columna "identificador de
archivo", si el valor de los campos es diferente se debe
rechazar el archivo.

```
(Ver capítulo 6.1.10.1 del manual de servicio)
15 El campo: (tamaño de registro) es
incorrecto.
```
```
El "tamaño de registro" está ubicado en la Línea 1 y
debe validar lo siguiente:
```
- Que el valor del campo debe ser igual a "106" en caso
de ser diferente el archivo se debe rechazar.
16 El campo de factor de abocamiento es
incorrecto.

```
El "factor de abocamiento" está ubicado en la Línea 1
y debe validar lo siguiente:
```
- Que el valor del campo debe ser igual a "10" en caso
de ser diferente el archivo se debe rechazar.
17 El código de formato es incorrecto. El "código de formato" está ubicado en la Línea 1 y
debe validar lo siguiente:
- Que el valor del campo debe ser igual a "1" en caso
de ser diferente el archivo se debe rechazar.
19 El código clase de transacciones del lote
es inválido.

```
El "código clase de transacciones" está ubicado en la
Línea 5 y debe validar lo siguiente:
```
- Que el valor del campo únicamente puede tener los
siguientes valores: 200, 220, 225.
En caso de ser diferente el archivo se debe rechazar.
20 El tipo de servicio del lote es inválido. El "tipo de servicio" está ubicado en la Línea 5 y debe
validar lo siguiente:
- Que el valor del campo únicamente puede tener los
siguientes valores: PPD, CCD.
En caso de ser diferente el archivo se debe rechazar.
22 El código estado del usuario originador
del lote es incorrecto.

```
El "código de estado del usuario originador" está
ubicado en la Línea 5 y debe validar lo siguiente:
```
- Que el valor del campo debe ser igual a "1" en caso
de ser diferente el archivo se debe rechazar.
24 El código de la transacción es inválido. El "código de transacción" está ubicado en la Línea 6 y
debe validar lo siguiente:
- Que el valor del campo únicamente puede tener los
siguientes valores: (21, 22, 23, 26, 27, 28, 31, 32, 33,
36, 37, 38, 51, 52, 53, 56, 55, 57).
En caso de ser diferente el archivo se debe rechazar.


```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota

```
Agosto de 2024
Página 280 de 329
```
```
30 El lote contiene varios tipos de
transacciones que no se permiten enviar
en un mismo lote.
```
```
El "código clase de transacción por lote" que está
ubicado en la línea 5 (220 - Créditos) debe aceptar los
"códigos de transacción" con los siguientes valores (21
22 23 31 32 33 51 52 53) que están ubicados en la línea
6.
En caso de ser diferente el archivo se debe rechazar.
El "código clase de transacción por lote" que está
ubicado en la línea 5 (225 - Débitos) debe aceptar los
"códigos de transacción" (26 27 28 36 37 38 55 56 57)
En caso de ser diferente el archivo se debe rechazar.
El "código clase de transacción por lote" que está
ubicado en la línea 5 (200 - Débitos y Créditos) debe
aceptar todos los tipos de transacción (21 22 23 26 27
28 31 32 33 36 37 38 51 52 53 56 55 57)
En caso de ser diferente el archivo se debe rechazar.
35 El dígito de chequeo de la Entidad
Financiera Receptora de la transacción
es incorrecto.
```
```
El campo "digito de chequeo" ubicado en la línea 6
debe corresponder con el registrado en el módulo
"Paty" de volpay, correspondiente al banco del campo
"Código entidad financiera receptora" ubicado en la
línea 6.
En caso de ser diferente el archivo se debe rechazar.
37 La cuenta receptora de la transacción es
inválida.
```
```
El campo "Número de cuenta del usuario receptor"
ubicado en la línea 6, no puede venir vacío (solo
blancos) debe tener solo números alineados a la
izquierda con espacios a la derecha (AN). En caso de
ser diferente el archivo se debe rechazar.
38 El indicador de registro adenda de la
transacción es inválido.
```
```
El "indicador de registro de adenda" está ubicado en
la Línea 6 y debe validar lo siguiente:
```
- Que el valor del campo debe ser igual a "1" en caso
de ser diferente el archivo se debe rechazar.
44 El número de secuencia del registro
adenda debe estar en orden consecutivo
ascendente.

```
El campo "numero de secuencia de registro de
adenda" está ubicado en la Línea 7 y debe validar lo
siguiente:
```
- Que el valor del campo de todos los registros tipo 7,
debe venir en orden ascendente, en caso de ser
diferente el archivo se debe rechazar.
47 La fecha de muerte en el registro adenda
es incorrecta.

```
El campo "fecha de muerte" está ubicada en la línea 7
de la estructura de devolución y debe validar lo
siguiente:
```
- Si el campo no trae información, este no debe ser
validado, en caso contrario se debe validar que el valor
del campo tenga la estructura de fecha YYYYMMDD, si
el formato es diferente el archivo se debe rechazar.


```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota

```
Agosto de 2024
Página 281 de 329
```
```
49 El número de secuencia del registro
adenda es incorrecto.
```
```
El campo "número de secuencia del registro adenda"
de la línea 7 en el "archivo de devoluciones" debe
validar lo siguiente:
```
- Que el valor del campo "numero de secuencia" de la
línea 6 sea igual al valor del campo "número de
secuencia del registro adenda" de la línea 7.
En caso de ser diferente el archivo se debe rechazar.
51 El número de registros de detalle y
adenda en el registro de control del lote
es incorrecto.

```
El campo "número de transacciones detalladas y de
registros adenda" de la línea 8 debe validar lo
siguiente:
```
- Que el valor del campo debe corresponder con el
conteo de las líneas 6 y 7 de cada lote, si es diferente
el archivo se debe rechazar.
52 El total de control en el registro de
control del lote es incorrecto.

```
El campo "totales de control" ubicado en la línea 8,
debe corresponder con la Sumatoria de los campos
"Código Entidad Financiera Receptora" ubicado en la
línea 6 dentro del mismo lote. Si la sumatoria no
corresponde el archivo se debe rechazar.
53 La suma de valores de transacciones
débito en el registro de control del lote
es incorrecta.
```
```
El campo "valor total de débitos" ubicado en la línea 8,
debe corresponder con la Sumatoria de los campos
"Valor de la transacción" ubicados en la línea 6 y que
correspondan a los códigos de transacción debito
marcados en el campo "códigos de transacción" con
estos valores (26 27 28 36 37 38 55 56 57) dentro del
mismo lote. Si la sumatoria no corresponde el archivo
se debe rechazar.
54 La suma de valores de transacciones
crédito en el registro de control del lote
es incorrecta.
```
```
El campo "valor total de créditos" ubicado en la línea
8, debe corresponder con la Sumatoria de los campos
"Valor de la transacción" ubicados en la línea 6 y que
correspondan a los códigos de transacción crédito
marcados en el campo "códigos de transacción" con
estos valores (21 22 23 31 32 33 51 52 53) dentro del
mismo lote. Si la sumatoria no corresponde el archivo
se debe rechazar.
56 El código de la Entidad Financiera
Originadora en el registro de control del
lote es incorrecto.
```
```
El campo "Identificación de la entidad financiera
originadora" ubicado en la línea 8, debe corresponder
con el campo "código entidad origen inmediato"
ubicado en la línea 1 del archivo. Si los códigos son
diferentes el archivo se debe rechazar.
58 La cantidad de lotes en el registro de
control del archivo es incorrecta.
```
```
El campo "Cantidad de lotes" ubicado en la línea 9,
debe corresponder con el número de lotes de todo el
archivo. Si los códigos son diferentes el archivo se
debe rechazar.
```

```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota

```
Agosto de 2024
Página 282 de 329
```
```
59 El número de bloques en el registro de
control del archivo es incorrecto.
```
```
El campo "Numero de bloques" ubicado en la línea 9,
debe corresponder con el número de bloques en el
archivo de 10 líneas cada uno. Si los valores son
diferentes el archivo se debe rechazar.
60 El número de registros de detalle y
adenda en el registro de control del
archivo es incorrecto.
```
```
El campo "número de transacciones detalladas y de
registros adenda" de la línea 9 debe validar lo
siguiente:
```
- Que el valor del campo debe corresponder con el
conteo de las líneas 6 y 7 de todo el archivo, si es
diferente el archivo se debe rechazar.
61 El total de control en el registro de
control del archivo es incorrecto.

```
El campo "totales de control" ubicado en la línea 9,
debe corresponder con la Sumatoria de los campos
"Código Entidad Financiera Receptora" ubicado en la
línea 6 de todo el archivo. Si la sumatoria no
corresponde el archivo se debe rechazar.
62 La suma de valores de transacciones
débito en el registro de control del
archivo es incorrecta.
```
```
El campo "valor total de débitos" ubicado en la línea 9,
debe corresponder con la Sumatoria de los campos
"Valor de la transacción" ubicados en la línea 6 y que
correspondan a los códigos de transacción debito
marcados en el campo "códigos de transacción" con
estos valores (26 27 28 36 37 38 55 56 57) de todo el
archivo. Si la sumatoria no corresponde el archivo se
debe rechazar.
63 La suma de valores de transacciones
crédito en el registro de control del
archivo es incorrecta.
```
```
El campo "valor total de créditos" ubicado en la línea
9, debe corresponder con la Sumatoria de los campos
"Valor de la transacción" ubicados en la línea 6 y que
correspondan a los códigos de transacción crédito
marcados en el campo "códigos de transacción" con
estos valores (21 22 23 31 32 33 51 52 53) de todo el
archivo. Si la sumatoria no corresponde el archivo se
debe rechazar.
64 Las líneas necesarias para completar el
archivo deben tener solo el carácter '9'.
```
```
Se debe validar que solo se utilice el carácter 9
después de la línea nueve 9. en caso contrario se debe
rechazar el archivo.
65 La fecha juliana es incorrecta. El campo "fecha de compensación juliana" ubicado en
la línea 5, se debe validar únicamente
que el campo cuando venga diligenciado solo tenga
números. En caso de ser diferente el archivo se debe
rechazar.
68 El nombre de la entidad destino no
puede ser vacío.
```
```
El campo "Nombre entidad destino inmediato"
ubicado en la línea 1, no puede venir vacío (solo
blancos). En caso de ser diferente el archivo se debe
rechazar.
```

```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota

```
Agosto de 2024
Página 283 de 329
```
```
69 El nombre de la entidad origen no puede
ser vacío.
```
```
El campo "Nombre entidad origen inmediato" ubicado
en la línea 1, no puede venir vacío (solo blancos). En
caso de ser diferente el archivo se debe rechazar.
71 La identificación del usuario originador
del lote no puede ser vacía.
```
```
El campo "Identificación del usuario originador"
ubicado en la línea 5, no puede venir vacío (solo
blancos). En caso de ser diferente el archivo se debe
rechazar.
74 No existe el código de la transacción. El campo "Código de transacción" ubicado en la línea
6, no puede venir vacío (solo blancos). En caso de ser
diferente el archivo se debe rechazar.
80 El código de referencia del archivo es
incorrecto.
```
```
El campo "Código de referencia" ubicado en la línea 1,
debe ser solo "1" o vacío (solo blancos). En caso de ser
diferente el archivo se debe rechazar.
85 El código de la entidad financiera en el
registro adenda es incorrecto.
```
```
El "Código de entidad financiera receptora de la
transacción original" de la estructura de
"devoluciones" está ubicado en la línea 7 y se debe
validar lo siguiente:
```
- El campo mencionado debe ser validado contra la
tabla de vinculación de bancos, si no está en la tabla
de bancos se debe rechazar."
86 La hora de creación del archivo es
incorrecta.

```
El campo "Hora de creación del archivo" ubicado en la
línea 1, debe ser numérico o vacío (solo blancos). En
caso de ser diferente el archivo se debe rechazar.
87 El campo reservado debe contener
blancos únicamente.
```
```
Los campos que aparecen en toda la estructura como
"reservados" deben ser vacíos (solo blancos). En caso
de ser diferente el archivo se debe rechazar.
88 El nombre del archivo no concuerda con
la Entidad Originadora, o esta incorrecto.
```
```
Se debe realizar lo siguiente:
```
- Identificar el nombre del archivo recibido.
- Extraer el valor del código de la Entidad financiera del
nombre del archivo
- Validar contra el campo "Código entidad origen
inmediato" ubicado en la línea 1
En caso de que sean diferentes el archivo se debe
rechazar.
100 El archivo fue movido del servidor. No se
pudo cargar su información.

```
Para mapear errores técnicos de Volpay
```
```
101 El archivo ya había sido enviado. No se
cargó su información.
```
```
Cuando el "nombre del archivo" ya se encuentra en la
base de datos de Volpay marcado como "cargado", no
debe permitir cargar otro archivo con el mismo
nombre en el mismo día.
156 La descripción del lote no es válida. El campo "Descripción de lote" ubicado en la línea 5,
debe corresponder con los contenidos en el anexo 16
```

```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota

```
Agosto de 2024
Página 284 de 329
```
```
(Ver hoja Anexo 16 Descripción de lote). En caso de ser
diferente el archivo se debe rechazar.
```
```
157 Los registros de relleno no están
completos
```
```
Se deben utilizar los registros de relleno (carácter 9)
que sean necesarios para completar bloques en
múltiplos de 10 al final del archivo. en caso contrario
se debe rechazar el archivo.
```

```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota
```
```
Agosto de 2024
Página 285 de 329
```
## ANEXO 18. ACUERDO DE PREVENCIÓN DEL RIESGO OPERATIVO

### ACUERDO INTERBANCARIO

### PARA GESTIONAR EL RIESGO OPERATIVO ORIGINADO EN TRANSACCIONES NO CONSENTIDAS POR LOS

### CLIENTES, REPRESENTADAS EN ANOTACIONES EN CUENTA

### INTRODUCCION

Uno de los propósitos de la Asociación Bancaria y de Entidades Participantes de Colombia, ASOBANCARIA, es
el de promover y mantener la confianza del público en el sector financiero y proteger la imagen de este.

Como es bien sabido, los participantes han venido modernizando y mejorando sus procesos y plataformas
tecnológicas, avance que permite ofrecer servicios a los clientes y usuarios mediante la realización de
transacciones y pagos a través de los canales electrónicos, en los cuales, los establecimientos bancarios ofrecen
su infraestructura para facilitar las relaciones comerciales y el traslado de recursos entre terceros.

Así mismo, en el marco de la administración de riesgos propios de la naturaleza de sus actuaciones, el sector
ha realizado cuantiosas inversiones, tanto monetarias como en recursos humanos, para administrar los riesgos
operativos, dentro de los que se encuentran – según la normatividad expedida por la Superintendencia
Financiera de Colombia- el fraude interno, el fraude externo, las relaciones laborales, clientes, daños a activos
fijos, fallas tecnológicas y ejecución y administración de procesos.

Como complemento a estas acciones los establecimientos bancarios concertaron el presente acuerdo
interbancario que permite operativizar la reversión de anotación en cuenta resultado de un débito o cargo no
consentido por alguno de sus clientes.


```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota
```
```
Agosto de 2024
Página 286 de 329
```
### LA JUNTA DIRECTIVA DE LA ASOCIACIÓN BANCARIA Y DE ENTIDADES PARTICIPANTES - ASOBANCARIA

### CONSIDERANDO

1. Que los participantes han mejorado sus sistemas de administración de riesgo operativo, para lo cual han
    realizado inversiones cuantiosas en la adquisición de aplicaciones y definición de planes y programas de
    seguridad;
2. Que los participantes han implementado la Circular Básica Jurídica Externa 029.expedida por la
    Superintendencia Financiera de Colombia sobre “requerimientos mínimos de seguridad y calidad que
    deben atender para el manejo de la información a través de los diferentes medios y canales utilizados para
    la distribución de los productos y servicios que ofrecen a sus clientes y usuarios”; no obstante lo anterior,
    las entidades pueden estar expuestas a riesgos operacionales derivados de transacciones no consentidas
    de los clientes.
3. Que uno de los principios consagrados en la Constitución es la buena fe y este se aplica en todos los
    órdenes, incluyendo el ámbito contractual;
4. Que a los clientes les asiste el derecho a solicitar la reversión de una transacción no consentida por ellos y
    realizada desde su(s) cuenta(s);
5. Que los participantes a través de los canales electrónicos tienen dispuesta la prestación de un servicio
    transaccional de pagos, en el cual actúan como intermediarios tecnológicos y no como ordenantes de los
    pagos efectuados a través de los mencionados canales;
6. Que los beneficiarios de los pagos objetados y reversados cuentan con los mecanismos contractuales y, en
    consecuencia, con las acciones judiciales que estimen pertinentes para perseguir el pago de las acreencias
    a su favor;
7. Que los participantes quieren participar activamente en la prevención de los riesgos operativos y, en
    consecuencia, se encuentran interesadas en prestar a sus clientes y usuarios el concurso que éstos
    requieran, a fin de evitar que la mala fe del suplantador y/o usurpador cause un perjuicio económico a sus
    clientes y usuarios.

En desarrollo de lo anterior, los participantes:


```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota
```
```
Agosto de 2024
Página 287 de 329
```
### ACUERDAN

PRIMERO. En el evento que un cliente informe a una participante que de su(s) cuenta(s) y/o productos
financieros se han realizado débitos o cargos no consentidos por él – representados en anotaciones en cuenta-
mediante canales electrónicos, los cuales han sido abonados a otras cuentas y/o productos financieros y solicite
la reversión de éstos, el participante realizará las siguientes actividades:

```
(a) Podrá exigir al cliente los documentos que considere necesarios para dejar constancia de que los
débitos y/o cargos no consentidos objeto de reversión no han sido realizados ni autorizados por él;
```
```
(b) Ejecutará las medidas tendientes a hacer efectiva la reversión de la anotación en cuenta, tales como
bloqueo o retención de los débitos o cargos no consentidos, siempre que sea posible y procedente,
hasta por el monto de estos, incluidas las comisiones, impuestos, tasas y contribuciones que se
hubieren causado, de las cuentas beneficiarias y/o productos financieros abiertos en el propio Banco.
```
```
(c) Solicitará a los establecimientos de crédito que tengan abiertas las cuentas beneficiarias y/o
productos financieros de los débitos y/o cargos no consentidos, la reversión de la anotación en
cuenta, siempre que sea posible, hasta por el monto de estos.
```
```
(d) La Entidad Participante donde estén las cuentas beneficiarias y/o productos financieros destinatarios
de los débitos y/o cargos no consentidos, hará su mejor esfuerzo para la reversión de la anotación en
cuenta de estos.
```
PARÁGRAFO PRIMERO. Para efectos del presente Acuerdo se tendrán en cuenta las siguientes definiciones:

- Débitos y/o cargos no consentidos: son aquellas transacciones que no han sido realizadas ni
    autorizadas por el titular de la cuenta o que habiendo sido realizadas por él no corresponden a la
    voluntad del titular.
- Anotación en cuenta: es un registro contable que representa un valor monetario.
- Reversión: es la anulación del registro contable de una transacción y de los efectos que ésta produjo.
- Transacción: es una operación que implica o conlleva movimiento de dinero.
- Canales electrónicos: Se consideran canales electrónicos para efectos de este acuerdo: internet, banca
    móvil, IVR, cajeros electrónicos y cualquier otro que se implemente en el futuro.

PARÁGRAFO SEGUNDO. Se entenderá que la reversión de la anotación en cuenta es posible cuando haya
recursos disponibles en las cuentas y/o productos financieros beneficiarios.

Para el caso de los pagos por PSE que impliquen la venta de mercancías, la reversión de la anotación en cuenta
solo será posible cuando el establecimiento de comercio no haya hecho entrega de estas. Lo dispuesto en este
inciso solo se aplicará cuando sea regulado en el reglamento operativo.

El presente acuerdo no se aplicará cuando la cuenta receptora de los recursos sea de entidades de seguridad
social.


```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota
```
```
Agosto de 2024
Página 288 de 329
```
PARÁGRAFO TERCERO. La entidad que vaya a realizar la reversión de la anotación en cuenta podrá exigir a la
solicitante una comunicación en los términos del Anexo 1.

En caso de que la entidad que realice la reversión de la anotación en cuenta no reciba la comunicación de que
trata este parágrafo, contará con el compromiso de indemnidad, a que se refiere este acuerdo, a cargo de la
entidad solicitante de la misma.

SEGUNDO. Los establecimientos de crédito que en desarrollo del presente Acuerdo soliciten la medida de
reversión de la anotación en cuenta proveniente de los débitos y/o cargos no consentidos, se obligan a asumir
la responsabilidad que se pueda derivar de su ejecución.

Así mismo, los establecimientos de crédito cuando actúen como solicitantes de la medida, con la suscripción
del presente acuerdo, aceptan mantener indemnes a los establecimientos de crédito receptores de la solicitud
y a terceros participantes en el proceso de reversión de cualquier reclamación formulada por los beneficiarios
de los débitos y/o cargos no consentidos.

TERCERO. La anterior solicitud de reversión de la anotación en cuenta solo podrá ser presentada siempre que
medie una razonable inmediatez, entre el descubrimiento de los hechos y la presentación de dicha solicitud.

CUARTO: Este acuerdo empezará a regir a partir del 1 de enero de 2011 para las transacciones originadas a
partir de esa fecha. Los establecimientos de crédito deberán adecuar sus convenios y/o reglamentos de
cuentas y ajustar los reglamentos operativos establecidos para las transacciones que se citan a continuación.

```
Tipo de transacción
Transacciones crédito nacionales aplicadas a
través de:
```
- Sistema ACH de ACH Colombia
- CENIT
Convenios de recaudo para servicios públicos
domiciliarios y privados:
- Transacciones PSE
- Canales electrónicos
o Manejos propios
o Manejo a través de un tercero*
Convenios de recaudo para la compra de
mercancía

```
* P.e.: ATH, Redeban, Servibanca.
```

```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota
```
```
Agosto de 2024
Página 289 de 329
```
### ANEXO 1

Señores
Nombre de la entidad
Ciudad

Asunto: Solicitud de reversión de la anotación en cuenta de los débitos y/o cargos no consentidos por el titular
de la cuenta ordenante y de los que fue beneficiaria la(s) cuenta(s) corriente y/o de Ahorros
No. _____________

Como es sabido, el día ______ de _______ 20__, fueron transferidos desde la cuenta _____________ No.
______________ de la cual es titular nuestro cliente ______________ los siguientes débitos y/o cargos:
________________________________________ de los cuales fue beneficiario el titular de la cuenta
____________ No. _____________.

Nuestro cliente informó al Banco que los débitos y/o cargos antes descritos no fueron efectuados por él, ni con
su autorización, contienen o fueron resultado de un error.

En razón a lo expuesto, solicitamos que los débitos y/o cargos precitados sean bloqueados o retenidos, y luego
se reversen de las cuentas beneficiarias la anotación en cuenta correspondiente. Todo lo anterior, lo
solicitamos bajo la responsabilidad de nuestra entidad y los mantendremos indemnes de cualquier reclamación
que formule el beneficiario del pago o cualquier otra persona por los cargos y reversiones.

Para constancia, se firma en la ciudad de ____________, a los _________ () días del mes de ___________ del
año________

### ____________________

Nombre de la Entidad
Cargo _______________
C.C. ________________
Dirección____________
Teléfono ____________


```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota
```
```
Agosto de 2024
Página 290 de 329
```
## ANEXO 19. REGLAMENTO OPERATIVO

Reglamento Operativo

1. Tipo de Transacción: Transacciones crédito nacionales aplicadas a través del Sistema ACH de ACH
    Colombia

```
a) Procedimiento
```
```
i) La entidad del cliente solicitante de la reversión de la anotación en cuenta (originador) genera la
solicitud de bloqueo inmediato a la entidad receptora, por parte del funcionario encargado vía
teléfono y posterior confirmación vía correo electrónico usando el directorio de contactos (Tabla
1 - Directorio de contactos de EF) y en horarios 7x24. La información mínima que debe entregar la
entidad originadora es: Detalle de la transacción y datos de la cuenta a bloquear.
```
```
ii) La entidad receptora realiza el bloqueo de la cuenta o de la transacción, dependiendo de la
funcionalidad interna de la entidad receptora inmediatamente después de recibida la solicitud de
bloqueo vía Teléfono, y posterior confirmación vía correo electrónico de la entidad originadora.
```
```
iii) La entidad receptora confirma por correo electrónico inmediatamente después de realizado el
bloqueo o como máximo una (1) hora después del bloqueo, vía correo electrónico a la entidad
originadora lo siguiente:
```
```
(1) Si la cuenta fue bloqueada.
(2) Si llegó la transacción origen de la reclamación a la cuenta.
(3) Si existen o no fondos origen de la reclamación en la cuenta.
```
```
iv) Si llegó la transacción y existen fondos origen de la reclamación en la cuenta, la entidad originadora
radica el caso en el Módulo de Reclamos de ACH COLOMBIA utilizando la causal REV07, a más
tardar al día siguiente hábil de haberse cumplido el paso anterior. Si este proceso no es cursado
por la entidad originadora, la entidad receptora podrá desbloquear la cuenta.
```
```
v) La entidad receptora hace un reporte de avance a la entidad originadora notificándole si existen o
no recursos en la cuenta de la transacción reclamada. Si no existen recursos se cierra el caso del
REV07.
```
```
vi) Si los recursos están disponibles, en la entidad originadora la cual formaliza el procedimiento
remitiendo a la entidad receptora el formato de carta de solicitud de reversión (Anexo 1),
diligenciado en su totalidad. Este envío se realizará a través del módulo de reclamos de ACH a más
tardar al tercer día hábil después de haber radicado el caso en el Módulo de Reclamos.
```
```
vii) La reversión por parte del participante receptor se realizará a más tardar al día siguiente hábil al
cumplimiento de lo previsto en el numeral vi (formalización de la operación) a través del
mecanismo que ACH Colombia tiene establecido. Después de surtida la reversión, la entidad
receptora podrá realizar el desbloqueo de la cuenta.
```

```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota

```
Agosto de 2024
Página 291 de 329
```
```
El anterior procedimiento se realizará teniendo en cuenta las instrucciones del “Manual Operativo de ACH
Colombia” y el “Instructivo del Módulo de quejas y reclamos de ACH Colombia”.
```
```
b) Fecha a partir de la cual empieza a regir: 1 de enero del 2011.
```
2. Convenios de recaudo para servicios públicos domiciliarios y privados:

```
1.1. Tipo de Transacción: Convenios de recaudos vía PSE (Transitorio)
```
```
a) Procedimiento
```
```
i) El participante Autorizador (Entidad del usuario afectado) deberá reportar por correo electrónico
a la Dirección de Soporte al Cliente de ACH, la(s) transacción(es) de PSE identificada(s) como no
consentidas, el horario definido es días hábiles de 8:00 a.m. a 5:00 p.m.
```
```
El listado de los contactos autorizados por parte de los participantes para reportar este tipo de
casos, se relacionan en la Tabla 1. Directorio de contactos de EF. La cuenta de correo electrónico
a la cual deberán reportar los casos es serviciopse@achcolombia.com.co, y en el asunto debe decir
“Solicitud REV08 – Transacción No Consentida PSE”
```
```
Para el reporte de los casos se deberá diligenciar el formato Reporte de Transacciones No
Consentidas PSE – EF, que se encuentra en el Anexo 3 del presente documento.
Los datos mínimos requeridos que el participante deberá reportar en el formato son:
```
- CUS.
- Fecha de la Transacción.
- Valor.

```
ii) Para realizar lo descrito en los numerales (II, III y IV) ACH Colombia contará con dos días hábiles.
Un funcionario de la Dirección de Soporte al Cliente en ACH Colombia, con la información
suministrada realiza la verificación de dichas transacciones en el sistema, identificando la siguiente
información:
```
- Que las transacciones hayan sido efectuadas a través de PSE.
- Que se encuentren en estado Aprobado.
- Que correspondan a Empresas que sean susceptibles de generar reversiones (archivo
    consolidado que no sean de recursos públicos, pago de impuestos y seguridad social).
- Que sean transacciones efectuadas con la antigüedad definida para solicitar reversiones,
    el tiempo a manejar para la antigüedad es 30 días.
- Finalmente identifica la(s) Entidad(es) Participante(s) Recaudadora(s) de las transacciones
    involucradas.

```
iii) Si al verificar la información se identifica que las transacciones no cumplen con las validaciones
establecidas, ACH Colombia enviará dicha respuesta a el participante que reportó el caso.
```

```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota

```
Agosto de 2024
Página 292 de 329
```
```
iv) En el caso que las transacciones cumplan con las validaciones establecidas, ACH Colombia enviará
la respuesta por correo electrónico a el participante que reportó el caso con copia a la(s)
Entidad(es) Participante(s) Participantes(s), a los correos que se relacionan en la Tabla 1. Directorio
de contactos de EF.
```
```
ACH COLOMBIA complementará el formato Reporte de Transacciones No Consentidas PSE – EF
enviado por el participante que reportó el caso, con los datos de las transacciones en cuestión.
```
```
Los datos de cada transacción son los siguientes:
```
- CUS.
- Fecha de la Transacción.
- Valor.
- Empresa.
- Código de Servicio.
- Nombre del Servicio.
- Ciclo de la Transacción.
- Entidad Participante Autorizadora.
- Entidad Participante Recaudadora.
- Estado de la Transacción (solo las de estado Aprobada).

```
v) La Entidad Participante que reportó el caso deberá contactar a la(s) Entidad(es) Participantes(s)
Recaudadora(s) involucradas para solicitar la gestión de recuperación de los fondos, a los correos
que se relacionan en la Tabla 1. Directorio de contactos de EF.
```
```
vi) La(s) Entidad(es) Participantes(s) Recaudadora(s) deberá(n) realizar el trámite de las reversiones a
más tardar al día siguiente hábil de recibida la solicitud por parte del participante Autorizador (EF
del usuario afectado) y abonar los valores recuperados vía SEBRA a la Cuenta de Depósito del
participante Autorizador, con código de transacción 151 y con el concepto “Recuperación
Transacciones No Consentidas PSE – Empresa XXX”, y dar respuesta vía correo electrónico a el
participante que reportó el caso con copia a ACH Colombia, adjuntando nuevamente el formato
Reporte de Transacciones No Consentidas PSE – EF relacionando la fecha de abono por SEBRA y
los valores recuperados.
```
```
En el caso que no sea posible recuperar dinero, se debe relacionar esto mismo en el formato
Reporte de Transacciones No Consentidas PSE – EF en el campo destinado para tal fin.
```
```
viii) La reversión por parte del participante receptor se realizará a más tardar al día siguiente hábil al
cumplimiento de lo previsto en el numeral vi (formalización de la operación) a través del
mecanismo que ACH Colombia tiene establecido. Después de surtida la reversión, la entidad
receptora podrá realizar el desbloqueo de la cuenta.
```
```
ix) Si la entidad receptora no da la respuesta dentro de los tiempos establecidos se aplicará la sanción
vigente cuyo valor es de 1SMLVD.
```

```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota

```
Agosto de 2024
Página 293 de 329
```
```
b) Fecha a partir de la cual empieza a regir: 1 de enero del 2011.
```
```
1.2. Tipo de Transacción: Convenios de recaudos vía PSE definitivo
```
```
a) Procedimiento
```
```
a. Procedimiento antes de cerrado el ciclo de PSE:
```
```
i) Si la transacción no ha sido dispersada en el proceso de cierre de PSE, el usuario con privilegios de
el participante Autorizadora debe ejecutar el procedimiento establecido para el cambio de estado
de la transacción de Aprobada a Rechazada. De manera directa a través del módulo de PSE
habilitado para el manejo del cambio de estado de estas transacciones.
(1) El control es dual, un funcionario que entra a modificar el estado de la transacción y otro que
entra a verificar y a aprobar la transacción.
(2) Si se presenta un error en el momento de realizar el cambio de estado de la transacción, es
responsabilidad directa de la entidad que entro a modificar el estado de la transacción.
```
```
b. Procedimiento después de cerrado el ciclo de PSE:
```
```
i) El banco del cliente solicitante de la reversión de la anotación en cuenta (originador) radica el caso
en el Módulo de Reclamos de ACH COLOMBIA utilizando la causal REV08, donde se solicita el
bloqueo inmediato la entidad receptora.
```
```
En el módulo se mostrará como mínimo la siguiente información:
```
- CUS.
- Fecha de la Transacción.
- Valor.
- Empresa.
- Código de Servicio.
- Nombre del Servicio.
- Ciclo de la Transacción.
- Entidad Participante Autorizadora.
- Entidad Participante Recaudadora.
- Estado de la Transacción (solo las de estado Aprobada).

```
ii) La entidad receptora realiza el bloqueo de la cuenta o de la transacción, dependiendo de la
funcionalidad interna de la entidad de manera inmediata o como máximo una (1) hora después de
recibida la solicitud de bloqueo, si existen o no fondos en la cuenta. Si no existen recursos se cierra
el caso del REV08.
```
```
iii) Si existen fondos, el banco originador formaliza el procedimiento remitiendo a el participante
Recaudadora el formato de carta de solicitud de reversión (Anexo 1). Este envío se realizará a
```

```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota

```
Agosto de 2024
Página 294 de 329
```
```
través del módulo de reclamos de ACH a más tardar al segundo día hábil después de haber
radicado el caso en el Módulo de Reclamos.
```
```
x) La reversión por parte del banco receptor se realizará a más tardar al día siguiente hábil al
cumplimiento de lo previsto en el numeral III (formalización de la operación) a través del
mecanismo que ACH Colombia tiene establecido. Después de surtida la reversión, la entidad
receptora podrá realizar el desbloqueo de la cuenta.
```
```
El anterior procedimiento se realizará teniendo en cuenta las instrucciones del “Manual Operativo de
ACH Colombia” y el “Instructivo del Módulo de quejas y reclamos de ACH Colombia”.
```
```
Mecanismos de contingencia:
```
```
Aplica en el momento que el participante no pueda acceso al módulo de PSE a realizar las solicitudes
de forma automática.
```
```
El mecanismo será a través de una solicitud escrita con los siguientes campos mínimos de la
transacción:
```
- CUS.
- Fecha de la Transacción.
- Valor.
- Empresa.
- Código de Servicio.
- Nombre del Servicio.
- Ciclo de la Transacción.
- Entidad Participante Autorizadora.
- Entidad Participante Recaudadora.
- Estado de la Transacción (solo las de estado Aprobada).

```
La solicitud debe estar firmada por los funcionarios autorizados de la entidad, los cuales reposan en la
carpeta que ACH maneja para este proceso.
```

```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota
```
```
Agosto de 2024
Página 295 de 329
```
ANEXO 20 Formulario de Seguridad para Vinculación de Entidades Participantes

(^)

### DIRECCIÓN DE SEGURIDAD Y RIESGOS

### FORMULARIO DE VINCULACIÓN DE ENTIDADES

```
Código:
GIR-GRI-FOR- 031
Versión: 1
```
```
Consulta de contexto del estado de implementación de riesgos en la empresa
```
```
Objetivo:
Validar que la entidad participante que desea vincularse a los servicios ofrecidos por ACH COLOMBIA cuenta con los controles y procedimientos
necesarios para poder garantizar una adecuada gestión de seguridad de la información, ciberseguridad, continuidad y sarlaft para los procesos o
servicios que estarán en prestados por ACH COLOMBIA.
```
```
Instrucciones:
```
1. Indicar la cobertura para cada una de las preguntas, asignando un valor entre 1 y 5, donde 1 es que no existe, 2 existe para pocos eventos, 3
existe para algunos eventos, 4 existe para todos los eventos y 5 cuando existe para todos los eventos, está documentado, existe y se evidencia.
N/A Si la pregunta no se enfoca a la labor desarrollada.
2. Adjuntar evidencias para cada punto. Dentro de la casilla “Observaciones / Evidencia”. Se debe describir brevemente cómo se cumple el requisito
e indicarla ruta exacta donde se encontrará el soporte de lo respondido en la evidencia adjunta, es decir, si la evidencia de un criterio está inmersa
en un manual, se debe decir con precisión en donde se encuentra la información que se soporta (Ejemplo: Hoja tres (3), capítulo cinco (5), numeral
2)

(^)


```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota

```
Agosto de 2024
Página 296 de 329
```
(^)

### CONTROLES DE SEGURIDAD 1 2 3 4 5

```
"Cumple
(SI / NO / N/A)"
```
```
Observaciones /
Evidencia
```
1. Certificaciones

### 1.1

```
Certificación de Cloud Security Alliance
<https://cloudsecurityalliance.org/>
1.2 Certificación de controles de organización de servicios tipo SOC2
1.3 ISO 27001 - Sobre el proceso objeto del contrato.
1.4 PCI^ DSS^ -^ En^ caso^ de que^ se^ requiera^ almacenar,^ procesar^ o^ transmitir^
datos de tarjetahabientes
1.5 Desarrollo Seguro <OWASP, otros. (especifique)>
```
2. Gobierno de Seguridad /Ciberseguridad

### 2.1

```
¿Cuenta con Política de seguridad de la información / Ciberseguridad
aprobada por una estructura de gobierno dentro de la organización?
```
### 2.2

```
¿Las políticas y procedimientos de Seguridad de la información son
revisados al menos una vez al año o cuando se tenga un cambio
significativo en la organización?
```

```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota

```
Agosto de 2024
Página 297 de 329
```
### 2.3

```
¿El personal, terceros, outsourcing y contratistas de la organización
reciben educación y son evaluados sobre concientización en temas
relacionados con Seguridad de la Información al menos 1 vez al año?
```
### 2.4

```
¿Se identifican, comunican y se gestionan controles a los riesgos /
amenazas de Seguridad y ciberseguridad emergentes que puedan
llegar a afectar a la organización.?
```
### 2.5

```
¿Se cuenta con un marco para la gestión de riesgos de Seguridad de la
información y Ciberseguridad?
```
### 2.6

```
¿Cuenta con pólizas de Ciberseguridad y seguridad de la información?
```

```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota

```
Agosto de 2024
Página 298 de 329
```
3. Control de Acceso Lógico

### 3.1

```
¿Cuenta con lineamientos formales relacionados con la asignación de
ID de inicio de sesión de usuario únicos para funcionarios y terceros?
```
### 3.2

```
¿Tiene un procedimiento documentado de gestión de acceso donde se
evidencie el ciclo de vida de acceso a usuarios para los sistemas de
información de la organización (alta, modificación y baja de accesos)?
```
```
3.3
```
```
¿Se cuentan con parámetros de contraseña fuertes teniendo en
cuenta longitud, complejidad e historial?
```
### 3.4

```
¿Se cuentan documentados y activos los lineamientos para el bloqueo
automático después de repetidos intentos fallidos de acceso de
usuarios?
```
```
3.5
```
```
¿Cuenta con autenticación multifactorial para los usuarios
administradores que se conectan remotamente en su organización?
```
### 3.6

```
¿Tienen un procedimiento documentado sobre la revocación de
credenciales cuando un funcionario o tercero finaliza su acuerdo
contractual o contrato laboral?
```
### 3.7

```
¿Tienen matrices de roles y privilegios de funcionarios y terceros que
tienen acceso a los sistemas de información de su organización?
```
### 3.8

```
¿Se gestiona una adecuada segregación de funciones sobre los
usuarios administradores?
```
### 3.9

```
¿Se controla y define las responsabilidades sobre la instalación de
software en su compañía?
```

```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota

```
Agosto de 2024
Página 299 de 329
```

```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota

```
Agosto de 2024
Página 300 de 329
```
4. Gestión de Activos

### 4.1

```
¿Tienen lineamientos y procedimientos documentados relacionados
con la identificación, clasificación y protección de los activos de
información según su nivel de exposición al riesgo y su importancia
para la organización?
```
(^)

5. Seguridad de los datos

### 5.1

```
¿Se realiza eliminación o destrucción segura de información
confidencial de activos próximos a ser retirados o desmantelados o
cuando sea requerido?
```
```
5.2
```
```
¿Cuentan con controles o herramientas para detectar o bloquear la
fuga de información o eliminación potencial no autorizada/no
intencional de información confidencial?
```
### 5.3

```
¿Está prohibido y bloqueado el uso de dispositivos de almacenamiento
externo (USB, unidades CD/DVD, Discos Duros externos) donde se
almacena y procesa información?
```
### 5.4

```
¿Existe separación entre entornos de pruebas, desarrollo y la red de
producción?
```
### 5.5

```
¿Los datos confidenciales en reposo y en tránsito están protegidos
mediante cifrado seguro según lo definido por las mejores prácticas de
la industria?
```
```
5.6
```
```
¿El acceso a los datos confidenciales son supervisados / monitoreados
garantizando que sólo el personal autorizado pueda accederlos?
```

```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota

```
Agosto de 2024
Página 301 de 329
```
### 5.7

```
¿Los archivos que contienen información confidencial de la compañía
que son transmitidos por correo electrónico se encuentran
previamente cifrados?
```
```
5.8
```
```
¿Todas las conexiones que se establecen entre su compañía y ACH
COLOMBIA se encuentran cifradas utilizando algoritmos fuertes?
```
### 5.9

```
¿Se cifran los dispositivos móviles (por ejemplo, computadoras
portátiles, tabletas, medios extraíbles) que se utilizan para almacenar
datos confidenciales?
```
### 5.10

```
¿Cuenta con procedimientos para generar, conservar, revisar
regularmente los registros de eventos acerca de actividades del
usuario, excepcionales, fallas y eventos de seguridad de la información
y ciberseguridad??
```
6. Gestión de Vulnerabilidades

### 6.1

```
¿La empresa es miembro o está suscrita a una organización de
intercambio de información sobre amenazas y vulnerabilidades?
```
### 6.2

```
¿Cuenta con un proceso formal para difundir información sobre
amenazas y vulnerabilidades que sean de interés común internamente
para su compañía y para ACH COLOMBIA?
```
### 6.3

```
¿Existe un proceso documentado de gestión de vulnerabilidades
donde semestralmente (como mínimo) se identifique, detecte y valore
las vulnerabilidades encontradas en los sistemas donde se almacena,
procesa o transmite información?
```

```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota

```
Agosto de 2024
Página 302 de 329
```
### 6.4

```
¿Existen planes de remediación para mitigar las vulnerabilidades
identificadas?
```
### 6.5

```
¿Se ejecutan pruebas de Intrusión (al menos semestralmente ) para los
sistemas donde se almacena, procesa o transmite información de ACH
COLOMBIA?
```
(^)
(^)
(^)

7. Procedimientos de Protección de Información

### 7.1

```
¿Se cuentan con procedimientos documentados de contratación para
determinar si se realizan verificaciones de antecedentes / evaluaciones
para todos los empleados, terceros y outsourcing?
```
```
7.2
```
```
¿Se cuentan con procedimientos de contratación para puestos con
acceso a información confidencial para determinar si son
proporcionales a su nivel de riesgo?
```
### 7.3

```
¿Se firman acuerdos de confidencialidad con los empleados con el fin
de garantizar que no exista revelación de información confidencial?
```
8. Incidentes de Seguridad de la Información y Ciberseguridad

### 8.1

```
¿Cuenta con una metodología estructurada de Gestión de Incidentes
(preparación, detección y análisis, contención, erradicación,
recuperación y actividades post-incidente)?
```
### 8.2

```
¿Cuenta con controles / herramientas que realicen el monitoreo de los
activos de información e identifique, detecte y notifique los incidentes
```

```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota

```
Agosto de 2024
Página 303 de 329
```
```
de Seguridad de la Información y Ciberseguridad en tiempo real (por
ejemplo, un SOC)?
```
### 8.3

```
¿Cuenta con un servicio de Ciber inteligencia y Alertas tempranas con
el fin de detectar amenazas?
```
### 8.4

```
De acuerdo con el objeto del contrato establecido con ACH COLOMBIA
y en caso de establecerse un incidente en su compañía que afecte a
ACH COLOMBIA, ¿Todos los incidentes de seguridad de la información
son notificados a ACH COLOMBIA, el detalle de su investigación, las
acciones tomadas y su resultado es informado?
```
9. Controles de Seguridad

### 9.1

```
¿Cuenta con un servicio de antivirus, se actualiza diariamente y realiza
análisis en tiempo real y periódicos para detectar y contener malware?
```
### 9.2

```
¿Aplica plantillas de Hardening a toda su infraestructura? Detalle el
proceso.
```
### 9.3

```
¿Todos los dispositivos tecnológicos cuentan con las últimas
actualizaciones (parches o fixes) del fabricante?
```
### 9.4

```
¿Cuenta con una herramienta de filtro de contenido, la cual bloquea el
acceso a páginas restringidas, por ejemplo, sin limitarse a estos:
(correo electrónico personal, carga y descarga de archivos, streaming,
sitios de evasión de proxy, redes sociales, herramientas de acceso
remoto y contenido de naturaleza ilegal, entre otros)?
```

```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota

```
Agosto de 2024
Página 304 de 329
```
### 9.5

```
¿Se cuenta con servicios de antispam en la compañía?
```
### 9.6

```
¿Se cuenta en funcionamiento dispositivos perimetrales tales como
IPS, Firewall?
```
(^)

10. Desarrollo

### 10.1

```
¿Realiza Pruebas de Análisis de código Estático/Dinámico antes de salir
en producción? Describa Método
```
### 10.2

```
¿Se cuenta con una metodología para el desarrollo de software
formalmente documentada y alineada con alguna(s) de las mejores
prácticas de la industria?
```
```
10.3
```
```
¿Se incluyen consideraciones de seguridad de la información a lo largo
de todo el ciclo de vida del desarrollo de software?
```
```
10.4
¿Se desarrollan las aplicaciones de software alineadas con las mejores
prácticas de la industria?
```
```
10.5
```
```
¿Todos los desarrollos de software se realizan alineados a la
metodología definida?
```
### 10.6

```
¿Se remueven todas las cuentas de usuario, aplicación y contraseñas
utilizadas en las fases de desarrollo y pruebas antes de su paso a
producción?
```

```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota

```
Agosto de 2024
Página 305 de 329
```
### 10.7

```
¿Se realizan verificaciones al código antes de que cualquier cambio sea
promovido al ambiente de producción?
```
### 10.8

```
¿Se han implementado controles de acceso que garantizan una debida
separación entre los ambientes de desarrollo/pruebas con el ambiente
productivo?
```
### 10.9

```
¿Se ha definido una correcta segmentación de funciones entre los
ambientes de desarrollo/pruebas y el ambiente productivo?
```
### 10.10

```
¿Se ha garantizado que los datos productivos no son utilizados en los
ambientes de desarrollo/pruebas?
```
(^)
(^)
(^)

11. Gestión de riesgos Operativos

### 11.1

```
¿La organización tiene establecido un modelo de gestión de riesgos
documentado e implementado?
```
```
11.2
¿La organización dispone de una estructura de gobierno que apoye la
gestión de riesgos al interior?
```
### 11.3

```
¿Existe algún escenario o comité donde se reporte el estado de riesgos
de la entidad?
```
### 11.4

```
¿Existe un proceso de gestión de riesgos, documentado e
implementado?
11.5 ¿Existe^ una^ política^ de^ gestión^ de^ riesgos^ documentada^ e^
implementada?
```

```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota

```
Agosto de 2024
Página 306 de 329
```
```
11.6 ¿Cuenta^ con^ una^ Matriz^ integral^ de^ riesgo?^
```
### 11.7

```
¿Se ha definido modelo de capacitación y conciencia en términos de la
gestión de riesgos?
```
### 11.8

```
De los riesgos gestionados por la organización, ¿alguno es de
cumplimiento obligatorio por exigencias de ley, normativas o
contractuales?
```
```
11.9
```
```
¿El proceso de gestión de riesgos está en el alcance de las revisiones
de la auditoría interna de su organización?
```
```
11.10 Ha^ identificado^ riesgos^ de^ fraude^ interno^ ¿Cómo^ los^ gestiona?^
```
### 11.11

```
¿Cuenta con alternativas para mitigar o transferir el riesgo: "Otrosí,
póliza de responsabilidad, seguros, ¿etc.”?
```
```
11.12 Cuenta^ con^ infraestructura^ tecnológica^ amplia^ y^ suficiente^ para^
soportar los servicios prestados por ACH Colombia.
```
(^)

12. Gestión de Continuidad del Negocio

### 12.1

```
¿Se ha establecido un Gobierno, Roles y responsabilidades para la
gestión de la continuidad del negocio?
```
### 12.2

```
¿Se ha establecido una política de gestión de continuidad del negocio?
```
### 12.3

```
¿El estado de continuidad es reportado a la dirección?, en caso
afirmativo describa la frecuencia.
```
### 12.4

```
¿Se ha definido e implementado un modelo metodológico de gestión
de continuidad de negocio?
```

```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota

```
Agosto de 2024
Página 307 de 329
```
```
Existe definido y documentada la metodología de continuidad del
negocio?
12.5 ¿Se^ tiene^ una^ gestión^ de^ riesgos^ de^ continuidad^ del^ negocio?^
```
### 12.6

```
¿De los resultados metodológicos han identificado sus RTO Y RPO de
procesos críticos?
```
### 12.7

```
¿Se efectúa validación periódica que los proveedores cuente con
planes de continuidad documentados y probados?
```
```
12.8
```
```
¿La organización tiene un directorio de continuidad de personal critico
documentado y actualizado?
```
```
12.9 ¿Cuentan^ con^ un^ centro^ alterno^ de^ operaciones^ y^ de^ datos?^
```
```
12.10 ¿Cuentan^ con^ un^ Plan^ de^ Emergencias?^ -^ última^ actualización^
12.11 ¿Cuentan^ con^ un^ Plan^ de^ Crisis?^ -^ última^ actualización^
```
```
12.12
```
```
¿Cuentan con un Plan de Recuperación de Procesos? - última
actualización
```
### 12.13

```
¿Cuentan con un Plan de Recuperación de Desastres - TI, incluye la
infraestructura asociada al servicio con ACH COLOMBIA - última
actualización
12.14 ¿Se^ cuenta^ con^ un^ cronograma^ o^ plan^ de^ pruebas,^ aprobado?^
```
```
12.15 ¿Se^ han^ efectuado^ pruebas^ específicas^ al^ servicio,^ fueron^ exitosos?^
```
```
12.16 ¿Cuándo^ los^ resultados^ de^ pruebas^ no^ son^ satisfactorios^ establecen^
planes de mejora?
```
```
12.17
```
```
¿La organización ha establecido un plan de capacitación, en términos
de continuidad?
```
```
12.18
¿Se ejecuta formación específica a personal que participa
directamente en la continuidad del negocio de la organización?
```

```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota

```
Agosto de 2024
Página 308 de 329
```
```
12.19 ¿Se^ efectúa^ capacitación^ especifica^ al^ personal^ involucrado^ en^ la^
gestión de crisis?
12.20
¿Se efectúa capacitación especifica al personal involucrado en la
brigada de emergencia?
12.21 ¿Se^ han^ definido^ actividades^ de^ seguimiento^ al^ SGCN?^
```
```
12.22
¿Se han establecido e implementado indicadores de gestión para el
SGCN?
```
```
12.23
```
```
¿El SGCN está integrado al alcance de los planes de auditoría de la
compañía?
```
(^)

13. ESTRATEGIA DE GESTIÓN DE FRAUDE

```
13.1 Existen^ políticas^ y^ procedimientos^ para^ la^ administración^ de^ los^
recursos humanos
13.2
Efectúan análisis de confiabilidad en el momento de la contratación de
los funcionarios
13.3 Efectúan^ actualización^ periódica^ de^ los^ datos^ del^ personal^
```
### 13.4

```
Existen procedimientos específicos para la contratación de personal
con acceso a áreas claves
```
### 13.5

```
Existe y se ejecuta un procedimiento que establezca un nivel de
exigencia de acuerdo con su nivel de riesgo de fraude
```

```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota

```
Agosto de 2024
Página 309 de 329
```
### 13.6

```
Se establece y se ejecuta un seguimiento y monitoreo especial de
transacciones a los comercios nuevos
```
### 13.7

```
Cuenta con un programa de prevención y control de fraude que
contemple clientes, empleados, proveedores, etc.
```
### 13.8

```
Existen y se prueban los procedimientos para el proceso de
investigación de fraudes con los clientes
```
### 13.9

```
Se establecen metodologías probadas para comunicar a los
interesados y a otros actores los resultados de las investigaciones
```
### 13.10

```
Se consideran evaluaciones periódicas a los comercios con base en sus
niveles de riesgo de fraude
```
### 13.11

```
Se analiza y se replantea la estrategia operativa con base en los
reclamos, quejas, devoluciones, fraude, etc., recibidos por los clientes
```
### 13.12

```
Existe un proceso documentado que permita la verificación
permanente de reglas para la autorización y el monitoreo de
transacciones
```
### 13.13

```
La autorización para el procesamiento de transacciones está sujetas a
verificaciones y validaciones previas, montos, número de
transacciones, devoluciones, etc.
```
### 13.14

```
Se realizan seguimientos a las transacciones que se salen de los niveles
de autorización
```
### 14. SARLAFT


```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota

```
Agosto de 2024
Página 310 de 329
```
### 14.1

```
Tienen implementado un Sistema de administración de Riesgos de
Lavado de activos y Financiación del terrorismo y personal responsable
para su gestión
```
### 14.2

```
Existen procedimientos adecuados que soporten la vinculación de sus
clientes
```
### 14.3

```
Existen procedimientos que soporten la debida diligencia en el
asegurar razonablemente el conocimiento de sus clientes
```
### 14.4

```
Se realiza verificaciones a la idoneidad de los clientes que se vinculan
(Verificación de documental, visita, consultas centrales de riesgo,
consulta fuentes de información pública, etc.)
```
### 14.5

```
Se realizan verificaciones a los clientes en listas restrictivas (OFAC,
ONU, Unión Europea), se incluye representante legal,
socios/accionistas, representante legal
```
```
14.6
```
```
En los contratos suscritos con sus clientes se incluyen programas de
prevención de riesgos LAFT en términos de conocimiento, control y
sensibilización
```
```
14.7
```
```
Se realiza periódicamente monitoreo / actualización de la información
de sus clientes
```
### 14.8

```
Las operaciones de sus clientes son monitoreadas en cuanto a
volumen transaccional y montos de las operaciones
```
### 14.9

```
Se tienen cláusulas contractuales con sus clientes que establezcan los
procedimientos en caso de identificar vínculos de estos con
actividades relacionadas con LAFT
```
### 14.10

```
Se reportan las operaciones sospechosas detectadas
```
(^)


```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota

```
Agosto de 2024
Página 311 de 329
```
### 15. TRATAMIENTO DE DATOS PERSONALES

### 15.1

```
Cuenta con procesos específicos para proteger la información de datos
personales de acuerdo con lo establecido en la ley 1581 y 1266
```
### 15.2

```
En los contratos que se suscriben con los comercios se incluye el
cumplimiento de tratamiento de datos personales acorde a lo exigido
por la ley
15.3 Se^ ha^ publicado^ la^ política^ y^ el^ manual^ operativo^ para^ el^ tratamiento^
de datos personales como aspecto de verificación
```
```
15.4
```
```
¿Conoce y socializa con sus empleados la Política para el Tratamiento
de Datos Personales?
```
### 15.5

```
Se ha designado un responsable para la administración y sanciones
ante el incumplimiento de la política de tratamiento de datos
personales
```
### 15.6

```
Cumple con la entrega de reportes de seguridad de la información y/u
otro tipo de reportes solicitados contractualmente por ACH
COLOMBIA, o aplicables según las normas vigentes
```
### 15.7

```
¿Ha tenido usted alguna investigación o sanción por temas
relacionados con la protección de datos personales? (entendemos que
puede ser información confidencial, por lo que sólo necesitamos un sí
o un no)
```
### 15.8

```
¿Tiene procedimientos de asignación de responsabilidades y
autorizaciones en el tratamiento de la información personal? Si los
tiene, indicar cuáles son y cuál es el proceso de verificación de las
autorizaciones respectivas.
```
### 15.9

```
¿Ha implementado acuerdos de confidencialidad con las personas que
tienen acceso a la información personal? Si tiene un modelo, por favor
adjuntarlo
```

```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota

```
Agosto de 2024
Página 312 de 329
```
### 15.10

```
¿Tiene controles de seguridad en la tercerización de servicios para el
tratamiento de la información personal?
```
### 15.11

```
¿Ha implementado una política específica para el acceso a la
información personal de las bases de datos con información personal
sensible?
```
```
15.12
```
```
¿Tiene una política implementada de copia de respaldo de la
información confidencial?
```
### 15.13

```
¿Cuenta con una política implementada para el correcto tratamiento
de la información personal en las diferentes etapas del ciclo de vida del
dato (recolección, circulación y disposición final)?
```
### 15.14

```
¿Cuenta con un procedimiento implementado para la validación de
datos de entrada y procesamiento de la información personal, para
garantizar que los datos recolectados y procesados sean correctos y
apropiados, como confirmación de tipos, formatos, longitudes,
pertinencia, cantidad, etc.?
```
### 15.15

```
¿Cuenta con principios de veracidad o calidad de los registros o datos,
la información contenida en los Bancos de Datos debe ser veraz,
completa, exacta, actualizada, comprobable y comprensible?
```
```
15.16
```
```
¿Como prohíbe el registro y divulgación de datos parciales,
incompletos, fraccionados o que induzcan a error?
```
### 15.17

```
¿Cuenta con una política implementada para el intercambio físico o
electrónico de datos (ejemplo, comercio electrónico) transporte y/o
almacenamiento de información personal?
```
### 15.18

```
¿Cuenta con una política y procedimientos implementados de gestión
de incidentes de seguridad de la información personal?
```

```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota

```
Agosto de 2024
Página 313 de 329
```
(^)
(^)

16. SENSIBILIZACIÓN Y FORMACIÓN A USUARIOS

### 16.1

```
Se adelantan campañas informativas sobre las medidas de seguridad
que deben adoptar los compradores y vendedores para la realización
de operaciones de comercio electrónico
16.2
Se informa al usuario final sobre la manera como se realiza el
procedimiento de pago
```

```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota
```
```
Agosto de 2024
Página 314 de 329
```
```
Esquema de Seguridad
```
Los Servicios de ACH COLOMBIA están basados en una arquitectura hibrida, con componentes distribuidos, las entidades que se vinculan a los servicios ofrecidos
por ACH COLOMBIA deben garantizar los siguientes niveles de seguridad.

```
Características de seguridad propias de la plataforma de Nube
```
```
REQUERIMIENTO CUMPLE DESCRIPCION DEL CUMPLIMIENTO EVIDENCIA
```
La entidad debe verificar que el proveedor de servicios en la
nube cuente y mantenga vigente, al menos, la certificación ISO
27001, y de observancia a los estándares o buenas prácticas,
tales como ISO 27017 y 27018. El proveedor puede certificarse
con estándares o mejores prácticas que reemplacen,
sustituyan o modifiquen las anteriores y debe disponer de
informes de controles de organización de servicios:
SSAE16/ISAE 3402 tipo II:
SOC 1
SOC 2
Informe de la auditoría pública SOC 3
ISO 27001
ISO 27017
ISO 27018
PCI DSS v3.2.
HIPAA
CSA STAR
Google Cloud Platform y la Directiva de Protección de Datos
de la UE
Protección de información personal y datos del número
individual (Japón)
FISC (Japón)
Prácticas recomendadas de la MPAA

La entidad debe asegurar a ACH COLOMBIA que su proveedor
de servicios en la nube cuente y mantenga vigente, al menos,
la certificación ISO 27001, y de observancia a los estándares o
buenas prácticas, tales como ISO 27017 y 27018. El proveedor
puede certificarse con estándares o mejores prácticas que
reemplacen, sustituyan o modifiquen las anteriores y debe
disponer de informes de controles de organización de
servicios (SOC1, SOC2, SOC3).

La entidad debe entregar a ACH COLOMBIA la certificación
donde se evidencie que su proveedor CSP le garantiza una
disponibilidad de al menos el 99.95% en los servicios
prestados en la nube


```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota
```
```
Agosto de 2024
Página 315 de 329
```
La entidad debe entregar a ACH COLOMBIA una certificación
del perfil de riesgo de la nube, en donde se contempló temas
como autenticación, procesamiento de información,
protección de los datos y aplicaciones, controles de seguridad,
entre otros evaluados o definidos por la entidad.

Socializar a ACH COLOMBIA los mecanismos que la entidad
establece que le permiten contar con respaldo de la
información que se procesa en la nube, la cual debe estar a
disposición de la entidad cuando así lo requiera.

Socializar a ACH COLOMBIA como garantiza la independencia
de su información y de sus copias de respaldo de la
información de las otras entidades que procesen en la nube.
La independencia se puede dar a nivel lógico o físico.

Certificar a ACH COLOMBIA como la entidad garantizar y
mantiene cifrada la información clasificada como confidencial
en tránsito o en reposo, usando estándares y algoritmos
reconocidos internacionalmente que brinden al menos la
seguridad ofrecida por AES, RSA o 3DES.

Certificar a ACH COLOMBIA como la entidad tiene bajo su
control la administración de usuarios y de privilegios para el
acceso a los servicios ofrecidos ante la entidad, así como a las
plataformas, aplicaciones y bases de datos que operen en la
nube, dependiendo del modelo de servicio contratado.

Certificar a ACH COLOMBIA como la entidad Monitorea los
servicios contratados para detectar operaciones o cambios no
deseados y/o adelantar las acciones preventivas o correctivas
cuando se requiera

Socializar a ACH COLOMBIA como la entidad considera dentro
del plan de continuidad del negocio la operación en la nube y
como ejecuta pruebas que resulten necesarias para confirmar
la efectividad de los procedimientos contingentes.

Socializar a ACH COLOMBIA si la entidad cuenta con la
estrategia de migración a otra plataforma en caso de
terminación del contrato por cualquiera de las partes, por la
interrupción o la degradación en la prestación del servicio de
parte del proveedor de servicios en la nube o por cualquier
otro motivo que haya evaluado la entidad.

La entidad deberá de informar a ACH COLOMBIA el nombre
del proveedor que prestará los servicios en la nube y de los
subcontratistas o _partners_ que le prestarán servicios asociados

al objeto del contrato. (^)
La entidad deberá de informar a ACH COLOMBIA la relación de
los procesos que serán manejados en la nube, incluyendo las
aplicaciones, tipo de datos, productos y servicios asociados a
éstos. (^)


```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota
```
```
Agosto de 2024
Página 316 de 329
```
La entidad deberá de informar a ACH COLOMBIA la ubicación
física o región donde se procesarán y almacenarán los datos.

La información solo podrá ser almacenada en países que
tengas las mismas condiciones o normas alienadas exigidas
por la normatividad colombiana

La entidad deberá de informar a ACH COLOMBIA las
certificaciones otorgadas al proveedor del servicio y/o sitio de

procesamiento. (^)
La entidad deberá de informar a ACH COLOMBIA la relación de
auditorías a las que se somete el proveedor de servicios
contratado.
La entidad deberá de informar a ACH COLOMBIA la
información sobre los niveles de servicio establecidos.
La entidad deberá de informar a ACH COLOMBIA el diagrama
con la plataforma tecnológica que soportará los servicios
contratados.
¿Dentro del contrato con su proveedor de servicio de nube,
tiene establecido cláusulas de incumplimiento en caso de
superar niveles de indisponibilidad, de acuerdo con lo
requerido por ACH Colombia? (^)
La entidad debe establecer procedimientos para verificar el
cumplimiento de los acuerdos de niveles de servicio que se
hubiere establecido con el proveedor de servicios en la nube y
sus subcontratistas o partners. (^)
Evidencia de configuración en la plataforma como mínimo
utilizando 2 zonas de disponibilidad diferentes de
procesamiento y almacenamiento de datos separadas
geográficamente y que mantengan una mínima latencia al
origen de las transacciones lo que permita contar con una alta
disponibilidad del servicio. (^)


```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota
```
```
Agosto de 2024
Página 317 de 329
```
```
Controles de Seguridad y Ciberseguridad
```
REQUERIMIENTO (^) CONEXIÓNTIPO^ DE^ CUMPLE DESCRIPCION DEL CUMPLIMIENTO EVIDENCIA
Para que una Entidad Financiera pueda hacer uso
de los servicios primero debe establecer
comunicación al servicio vía internet utilizando el
protocolo de comunicación Https con el protocolo
criptográfico TLS V1.2 (Transport layer security).
TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256
TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384
TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA256
TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA384
TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256
TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384
TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA256
TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA384
TLS_DHE_RSA_WITH_AES_128_GCM_SHA256
TLS_DHE_RSA_WITH_AES_256_GCM_SHA384
TLS_DHE_RSA_WITH_AES_128_CBC_SHA256
TLS_DHE_RSA_WITH_AES_256_CBC_SHA256
Canal
Dedicado
API
La entidad debe implementar el uso de certificados
digitales a usar para la autenticación de servidores
(Certificados de Sitio y/o Mutual TLS) deben
contemplar como mínimo una llave de 2048 bits
con algoritmo de RSA, con algoritmo de integridad
SHA 256, en este certificado debe tener el dominio
de la entidad para ser validado por los
componentes del servicio.
Canal
Dedicado
API
La entidad debe implementar el Cifrado sobre los
metadatos utilizando Cifrado Simétrico AES
128/256 O 3DES desde el origen al destino (cifrado
punto a punto)
Canal
Dedicado
API
Dentro de las funcionales del standard de JSON,
existen las funcionalidades de JWT (JSON Web
token), JWS (JSON web Signature) y JWE (JSON
Web Encryption representa contenido cifrado
mediante estructuras de datos basadas en JSON),
de acuerdo con lo anterior lo que espera ACH
Colombia es que incluyamos estas funcionalidades
dentro de la mensajería JSON, no que se realice el
cifrado del token.
API


```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota
```
```
Agosto de 2024
Página 318 de 329
```
Las peticiones que emiten los clientes (entidades)
sobre el servicio de ACH COLOMBIA y si son
recibidas en la nube por un api Gateway y se
realiza la siguiente validación en cada petición:

1. La entidad debe contar con un API KEY.
2. La entidad debe contar con un client id y client
secret, con esta información pueden solicitar los
tokens de acceso OAtuh 2.0. del servicio.
3. El token de acceso debe ser válido (la duración
de cada token está definida por un tiempo de 1
hora)
4. El token debe tener una politica de revocación
implementada para los eventos de riesgos o
incidentes en los que sea necesario.
5. Se debe validar el redirect_uri en el lado del
servidor para que solo permita las urls habilitadas.
6. Se debe utilizar el parámetro state con un hash
aleatorio para prevenir ataques de CSRF Cross-site
request forgery o falsificación de petición en sitios
cruzados.
7. Se debe definir el ámbito scope en la aplicación
para indicar en las peticiones a qué información se
requiere acceder.

```
API
```
La entidad debe entregar a ACH COLOMBIA el
Certificado de pruebas de Ethical Hacking sobre la
arquitectura donde estará el servicio prestado

```
Aplica
General
```
La entidad debe entregar a ACH COLOMBIA el
Certificado de pruebas de continuidad sobre la
arquitectura donde estará el servicio prestado

```
Aplica
General
```
La entidad debe certificar el desarrollo seguro de
sus apis o webservices para el consumo del servicio
de ACH COLOMBIA.

```
Aplica
General
```
WS – Security con certificado y firmas digitales en
todos los mensajes

```
Canal
```
Dedicado (^)
WS-Security – extensión de seguridad para
mensajes Soap, definida por Microsoft, verisign e
IBM.
Canal
Dedicado (^)
Autenticación basada en IP/Certificado – Para
identificar el autor de la llamada del webservice se
utiliza un mecanismo que verifica el IP que hizo la
llamada y cual el subject del certificado enviado
dentro del mensaje.
Canal
Dedicado


```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota
```
```
Agosto de 2024
Página 319 de 329
```
ANEXO 21 Guía Generación Mensaje Encriptado.

Digital Envelope

El formato Digital Envelope funciona firmando la información a ser protegida con una llave privada RSA, y luego
procediendo a cifrar dicha información con una clave simétrica AES de 256 bits. Finalmente se cifra la clave del
mensaje haciendo uso del certificado digital X.509 del receptor del mensaje (haciendo uso del algoritmo RSA).
A continuación, se explica el contenido del archivo XML por cada tag.
Estructura sobre digital XML
<versión>: La versión del Sobre Digital. Valor esperado “1”.
<identifier>: Identificador del sobre. Generado concatenando el serial del certificado del firmante y un número
aleatorio (o pseudoaleatorio seguro). Este es el vector de inicialización (IV) usado al momento de hacer el
cifrado AES. El algoritmo del IV es
AES/ECB/PKCS5padding. Los bytes del ID se deben sacar de este instanciado como BigInteger y no como String.
Calcular el IV ejecutando 1 bloque AES usando la llave de contenido (la llave producto de descifrar el campo
<encryptedKey>) sobre el valor en el campo
<identifier></identifier>.
<timestamp>: Marca de tiempo del momento en que fue generado el sobre digital.
<recipientInfo>: Información del receptor compuesto por los siguientes tags:
<certificateInfo>: Información del certificado hacia el cual fue cifrado el sobre digital.
Compuesto de los siguientes tags:
<issuer>: Información del DN (Distinguished Name) del certificado emisor
siguiendo el estándar definido en el RFC4519. (cn=, o=, ou=, c=, st=, l=, street=)
<serial>: El serial del certificado.
<keyEncryptionAlgorithm>: Algoritmo con el cual fue cifrada la llave simétrica del
mensaje.
<encryptedKey>: La llave simétrica con la cual fue cifrada la información del tag
<encryptedContent> (información codificada en BASE 64). Usar la llave privada que
corresponde al certificado de destino de este mensaje, con el algoritmo detallado en
<keyEncryptionAlgorithm>.
<encryptedContentInfo>: Información del mensaje cifrado, definida en los siguientes tags:
<contentType>: Tipo de contenido. Este campo es SignedData, que quiere decir
que la información cifrada en este mensaje está firmada.
<contentEncryptionAlgorithm>: Algoritmo con el cual fue cifrado el mensaje
firmado que se encuentra en el tag <encryptedContent>.
<encryptedContent>: La información firmada que fue cifrada con la llave de
mensaje que se encuentra en el tag <encryptedKey> (información codificada en
BASE 64). Al descifrar esta información obtendrá un nuevo sobre digital XML donde
se encuentra la información protegida junto a su firma digital.

Estructura mensaje firmado

Al descifrar el contenido que se encontraba en <encryptedContent> se obtiene un nuevo
XML con la siguiente estructura:


```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota
```
```
Agosto de 2024
Página 320 de 329
```
<versión>: La versión del Sobre Digital. Valor esperado “1”.
<signerInfo>: Información de quién firmó el mensaje, compuesto de los siguientes tags:
<signatureAlgorithm>: El algoritmo de firma del mensaje.
<certificateInfo>: Información del certificado que corresponde a la llave privada
usada para firmar el mensaje, definido en los tags <issuer> y <serial>
<issuer>: Información del DN (Distinguished Name) del certificado emisor siguiendo
el estándar definido en el RFC4519. (cn=, o=, ou=, c=, st=, l=, street=)
<serial>: El serial del certificado.
<certificate>: El certificado que corresponde a la llave privada usada para firmar el mensaje
en formato ASCII (Base64).
<contentInfo>: El contenido original que fue protegido. Viene comprimido en ZIP y
codificado en Base64.
<encryptedDigest>: Es la firma digital de la información en claro que se encuentra en
“contentInfo”. En sí es un hash criptográfico haciendo uso del algoritmo de firma definido en
el tag <signatureAlgorithm>, el cual luego es cifrado con la llave privada que corresponde al
certificado que se encuentra en el tag <certificate>. Se debe usar dicho certificado para
verificar la firma digital. La información se encuentra codificada en Base64.

Ejemplo de mensajería encriptada para NACHA - M

<?xml versión="1.0" encoding="utf-8"?>
<!DOCTYPE envelope SYSTEM "envelope.dtd">
<envelope>
<versión>1</versión>
<identifier>6013207168031250769414451478020475378361636336</identifier>
<timestamp>Tue 09 20:09:51 COT 2021</timestamp>
<recipientInfo>
<certificateInfo>
<issuer>CN=ACH COLOMBIA SA., C=CO, E=ADMINISTRADORESCERTIFICADOS@ACHCOLOMBIA.COM.CO,
L=BOGOTA D.C., O=ACH COLOMBIA SA., OID.1.3.6.1.4.1.23267.2.3=8300785126, SERIALNUMBER=1449986,
OU=NACHAM, S=BOGOTA D.C.</issuer>
<serial>132945358989136383410303738658257264777</serial>
</certificateInfo>
<keyEncryptionAlgorithm>RSA/NONE/PKCS1Padding</keyEncryptionAlgorithm>

<encryptedKey>X1YNnNPCH+vCVC+slAKp4GL4stBtKvFYWKYLXtnaJ17KXP0EJ4yZhEqjYjcpjaK3Wv2q3hcju4QiR
WszW3wvs7oPz7TL4Xn2YGQedLoevcwYz44p6zJPDA9Ux2224FwZ7Fm31emdUlwuJjZDz6QelsBLn/tU4muh+pC
3gOeFPqz60drGwSGQnccDoY3qSNBdChNuSMNqpE76BhhYP2PQx0wmP4dFbhc+HQ+SNaOwdA4XekpB5RpD7
tUhJv9CYImPsgTJVleVPdD9nJdJkH+Obk/RDioJAHFuHoWiwwaMZg0RAmPeTzzk7YcwfQG8ig57BB7+7pxFb7iRQ
VbdzdigrQ==</encryptedKey>
</recipientInfo>
<encryptedContentInfo>
<contentType>signedData</contentType>


```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota
```
```
Agosto de 2024
Página 321 de 329
```
<contentEncryptionAlgorithm>AES/CBC/PKCS5padding</contentEncryptionAlgorithm>

<encryptedContent>VZvwWbHll2YsewqM7ATjqHoksOQJzkgA7IpHKBfHxw9/f+U3GKA61GmW1v7KvXOkwCX1f
du7dLa4hlfaGAfL93ZwLe+0bCEzBXPnREe9Z9VOmnx1qBh5/ZCv568sE3PQxxc4qh24+J6Ua30h7ytjjGy6ATdXcKI
hsm0FuI3fthqVeHAI/clCPHlRubdAxMJjvHz6HcE+jCJQOTHtrkPy39JRXLIF8YHV6NPKTgxxozjX94PdUD3yYL1Kk2G
FlqJZA9+Xtm1HrJ1f67D3P4jqBrkFoV5n8JxxuUFydeetWjGE41lk3ZVxaas3DxUYz4wXYDo6bb5hJB0OOLGWBkqb
ffT3Fa0h4rRKjNu/kQ+wWrUDXi644VzdqHrq+lvSy7PPBU/KeNf19Wk1OfQHweWO9OZzP64193EJNBWTxFFfrrst
C0ATRjw27Az9nzE/wRYvBeti3rLvw41PGKSoAmdvtL8EUw/aGKeBaYpyYTbA0EIPPIov3UZ7t+2O5S5iWYbEumHb
UNUdZiKpI/mY83NF3R31x+2eAXk1HPVGj9gibXUQQCLQEIWRxwZx911mObwcjYK3xcGlGKKJ9p4bzpzrfeOXIayP
aCMvgM0K/bi9UyJqe1X/BMx+zDv2GWj++0WYBZGZ4ODETK1O6bGo7J90GSZpCah7C+d9mGWB+VGJbF61lysa
IPYodkznOHAsx+Ua8uVhRZ+z38sgM7o99wPQ/96jJjBaPSKOWWlcGfC+skrE36ejDfeW64zvk8jeYvziNQPAvVh1q
UaZHzCBeXErHaGu0rq3KhmLMiqwvAivKBQ6dp6meffGtxNl2yCW/ETtykDN6Xd51NVkI4l5OLgIyBuvNXnebcQgH
/dgdnf/JLkAcQFIqfiRzikGsF2oqd9gwrGRWp74CceETIHoeV4que/n+n2nHWPL+svRtxRmBUGfu+tU2ft/wX3WDN
BB/nbCR7NYlA/pO4xdnSORrmHA3P1GvJcvGpxatN932Y3+syTYVLe3c7u/WY/cG5vjZvqXsaiY8Pi7G8+KjT8HlYjvO
4Ys/s25TEQT1liCKo68HJ+DZiUGFAm8Q5g+IjCqWktprAg1iAn7KoqrY0uyJwixHMq0BmbjLnkzaA3eBaftTRe92Xu
BCpL884/hUoNJXX51k0iLXd4n1AUFhIvqP4oGyWtv6pA9iafxjSUqZhEgy8tItODbUAWwSoBaxC7nKoWpYBiD9xW
u0tpJcz3e5+duHwDr1pLyYyWq7KYCDozYLtcBZNmJUHXGpmjnpWsk8//1KtljtV5V/Jgk6oNobdOBoyz2/ULFvSbN
yjHk/5JwErJLt+QVX7otOrSmElHQSr21zlmMiE28zwS5/uwXEvT5zT6ZF6n18NWxL0we7klHx8JiOhHv7k/KQpUpy
Av7VwoGz+G8RLPT5kXg+9Bf6z36OWEVAt9NbsL0mMEPW5Fz7UDI4CHwhLrON8jfPxrADfUaQJwSzETAjTjEs6I+
dU2l+k0e284lD8dmsjHLkYeSC25ixYQ6dNKwdzGMNlRcdnZ3B5IglkDATJipxd945Qn+c6whz1JBuwsKPgcuxob9nI
4gUsjHXkXmjQyqHL7Y0Lxy1aOgW5qWIee+bzw20d7pU08m3bHUPfJasj8ZWNh8XSBXS5m5izg84R5aV4vbUaM
MDikL/PP+jZLeOAPQ9hXHswzzH5I+o2nO5ZJtt6SJXZKHQ3952n7LzZ35wVkpQJiuDkCdusHSADtHbP1RrZVN8dtL
6+ByDcfLw6jdoSrcd1QLdMYB5NK5mqJcwja2q1+FxeAOz8ULGTW9mm7B/mElhz2GkPY9f2JEVEU3MJS5TktuAw
CYohtXa+375dXBg65gCBX/2NiMgPTIq9CXU8KrqO6NfNrxx1LEAmkVC3h3KWM2hQQk1b3fMtgPd5h3yJ+Bz7zL
nStlBOFAlktl02zW2RMV9CAbAEGOHBvZOZm+87x93xEKaSf+DFjXEMEvCYrZ4OjCnoFhmrc/JUQ09P3+YirtfB5d
m4Q8WHYc4o0j5mqDK8MYGAXdW0CHthZ+UPUZBiqxeVr7nGuvDzFyqTOjWMBefnSDtCbcEOj/gFh6Io2eIqxuE
r51VQy47GVm2g8li7TAHiVxkP8StBarBK+5VxrA/W4AjUv1xotEyHvbw+pPxytBZKk5XCrbgueRiWBIA00lX8UNJTc
VQvqdL3zvdDQzu1KTyxFs5p9puxkOVkxXBwFJLZONt09wDE9S3rhqQQgD0YIPX7mx6sadFfDPX99kv6G+dfAvqK
N/qUikVcUrOB8qQ4q0BhgT8kPtX/Us/es7l1wwWJcA3dU69gyr75XlJfoxwg1Pm3l4p+Mo0K/Hqp44iHf+DHkSvBZ
666e5+766tzp23xhf5xSi4CT50gOPE6A+zNJp9Ido96a1ca5I2ZGfOLpaZUos8Y4JsVDQpTRcQRT5ARyiICZM1aeTfD
6wENbV107/7p6Sy8E4RcMoOm2IH2BfbQPKHwUgf/g2OCIILHaiVys2dW4h66GgpNBoI0iP2IwMbMy0vTUzDY2j
QriAgcOAgLXyKsjPFRvY3UP1BJITVLz/2GtB+IRxmvtSYtf+Gk6xTR9MFxNssZDdh17kZaIr9TI5xfMTvp88Hll1ISmTR
5cNSDDsj6FZux8ZZD9+vkLhTl7CuStuFasgyZLMCUsMLLHO6xtU5mf0t+kakYfjQB8y3rf3lOtC6xWA+hD6fCQ+Xnv
p3bb5z3ZLvggyXxU/aRN7VgVjDYxGT6kyna0zedhvn31o37kprhVawvYn31gAZFRKJOtTAs8PtMDNhbq+cRUuZAY
yw2SWTkhIOQ15FmGGml9R0g3TT/EhEPpPwYg/tnh2cVcMfC7ocNiH7VeYA6jfZHYF35M7razkL3tNVX1HPWbup
EV459MN+ej9UKLnxSs5LLyk3IEnzq6Xrz3F9SzexT1SR5hytZwVT3aBkCiv/2gsG3C2xHyrjM54lFd1o2NSy4e8ZHKV
Wqt3u2gs3UD9z1fC3GZolohwZ4/+IYvR9COff0ml47r1zoIdv9Ht3bFUvy8GBRUFcDGnco1vxW62ng9+cbMpaQB
55Rz+O9Vxc8WXXHjGu5Cl8PUQ/XmRspUcyDFLRgm4y0mNExXFl5loJVlMhWQkp1Va8P2RcmMblbdnGiRBNotN
NbKa7e5WUM+Eo3PIUU9zBOyHTDjgWBk3RjXCbcnSUTJqvvb1zAPXZVVOfR2lTUo2sdXPw5ppWk2GBA7MelGs
4gHcWQzS2YiVFMwwxHKw3XJIKo6p9ASbCfPCPOhx6hhFNihBu9JXVoz9JHqYzG5+FcBwcqEEzydZwbur8YMAI4P
5IgRqQXch322Fh3b6zrcCtYzCYTmpVNdRr/eC16HYQPeZZO8KbnJxv/fPgm8W4LxB6ZJA+MH8lyvXgQWtl8mXtiU
muHimY8Abs1wP9QQw4KxyhnmZp2GPpyZDV30LGIPHxoQif6k3fjhhbN6f46SNngxic2llyfjgLa3Ufa062UeDmcEK
iGMDghkAjEXSKsB7Cn9iNNXpbPKf22Ky2xqkCeCx+XM4xnz19T8TOWjdgg/xfRu42qlM+ZZk/pCTxTkzLVMeG9NX
1Jg1s4gY6xFX/3J+QFPM79vy7tUBrJIXvFqUnooL46k7umC3qGuAd96FcLug8E3fzKLKvtTWya/NHvUOqOM24qer


```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota
```
```
Agosto de 2024
Página 322 de 329
```
0oimpoU7yT2x7jY5Y5kNb/mzhzF/gt4lid4jClGV+dnhQTumzXqPNAJXLrxaEbFIPCOmzNBf0jJ9qOZcmYgF5t2bZub
mgq6OLcLB+yRHfHQ89HZpkjBLhdB0zdWGsaLhgQWAq1By88ificnS2/k8KQk4YsO5OEdPUVEknX+5nuKAPEROU
9NVKysKFFzCuke50mBVoC+s0DIR1WgDXM8KTjOVkaX79UDO1A07Q9/uV9oFvJSYv0Agtuxsg22AWvTDSbjPnR9
UiTGsMQgCYvIjUPe892Van9J+WyQ+uxRl/xNtNI1rcbBhPxnKSpnBJU1Qcoe0s5JCagA3+zGhAkma/DUzTjejJbETe
hiF0m6/7aAyDALP1DGArSWL+qys5KwHAXRcP5WMjN97jwRaalAQ5EXEy2VgF/wNJ3r92PIBBajsztUpNy61mwr
wj0T93pfpLEJZBOFkqMUT3x7xWHqseF3+5lUoP3jKyEoMYBCcXWAVv5Ldx2f9P4KKhY79SR9NGqZPDppyLCyy1P
/g50QA4beGAUd/NhQjRZmKwqVgaHR6qZCljpVmSyjgl5XhFzdId0Y25luRwzEv5k4zL3bBvMX42XiX8R2leMG3b9
XeYxf4q76y6Mr/9jdpHk7Rq3QJOHyENWZUoTZGhp0rWmIqo5kKplI9PjKIjUK7eTRdta1uuChvmOlGWA7HO+Ep
HzDjlKczyOeDuM+aIbH92Otj+WMPwHVy+sgYp18kISR3KfUE5FNnLw9yFbXUsJeaGldMZrH9i5F/dzlNoeORu/uC
VK0icbWgiDUy9ETXwTgkQA0IKFNZID7EjaLQQL7I53NGqWNx+VWVYSOtDS57OVdQ+Y2qzH9Yg33aX3s5OYIHTd
N6ZmIZfsZvewZpgDbffhMN33gM7oKVKMu4AFzTjNtPWOLHu8YJVdCueJPF2I8IajCul6sS0NDlPYSHqE1ArrpUHrp
pwLFSeIwWU67OP1ERLi+qN1WgT8aK9cT8x1PLCE1zFqJgnb2nyRdSva0fczSJ+p8BfyXlzodO545+H61xra7wUKO
NXxoGPA1wCDV3DtzpdbY2dhqZBw4rEbv+JPJ51P5KIz/KM5F1pKXr4VH8gJEJgrRDOkqB5dKuRavHRb1sWmBzM
6HtAm/5Tc8teBlu+RH70jNq6tC8g2GuP3p+4TW9XvH7lswBzQqiDyA0LZtgmngyvP9ruvyeTpc/lCtU1cDyWMOW
M0qmZ+uZpmD6Byq5L1jUBWM2pMe6sJe/93rM83j61epwMXaVY0N4tjZCLwrlHUsMX4JeA18ei2bjA6HGDgy4
SmMpobJDGhxjOEpTbUKQXbwxgKD2F+BDO3ciLgp7plmitW/cEISAlmtkBhuIqNKDEvAUuDBssQJ+c0i/fC7R/MM
X5sL9DYf8O7ruQKE2hegJOKS1Ei81l7knKEXevYqxcD2yn+0YFXYuyFl0AfYACcQDxRfjjUZG1IzDM9+6ACLjRDXRd+
AhKTB+ZWIABv2uIdgB1w8plwRka19gKlAPtPVWmFDWBF7l6Coe8yUxwqDmqeo4LTjtg3rHxx9WlebkOTxIJIvGIr
xPfmHmoviUP6kxSWLHhWrW1BJHC6HfpcOUWsdg04XaFZ3fcep/ryDdFed1xBm5v8mEkGJL914W5GGpHmfB4
1F5yuSE7Fvfyb+9xMgAe7CBTR4JleZEyr39yUl0jqN27COkKBfK2DD7/K6i1CXyaBGjyWJtCwtx9ycljBSq3EKtAX46m
C/xi4Xl6GsUfUZB9kyUn65euC15WnMkZA7yiKb+DQcaO0JWXkxpTVryHnDK3cTvdjQHN+R4W/musGFQEzsYBlq
ORnghO5KTcXoP0vNftjpFbCZDvLTLPKRbwLoCb3u4lKISupRVgGjIYtHYy2eUVTI9pv5LzbDSpUzGb5nWvVUWLnd
ZryGpFc2wE+SMZfKRQSNOAkZcGewotO4CnH0ZneNyzUDt9vuwJ7oeKaP6y3WnVqpb/Tc9ghsVrX67fzpKWX5Ck
zoj+VEo8ErwHM/a1n3qpjxY2/OnEAsw5/Na6o9RvYOdlNc/mPWig9cQFf9o7oS174ND1VQnIqLIBqaF1odEL57/T
kr/+8MONax57LWxWDapmhhErNRflHurpDnUyZvXD0mVMQkKsHvu3htiGFPurbInCJw5upb474PvZWANvOMy
PwR6i8Cdi4zr+7Fexih9iIUt45pSzzz/9NSgYvlbeTJ/vNB0ygplBtXarGD//ImGI4mJg8Aiui1WORUxSlFNwtNUXmaBU
BBq5ltoZx5t1c2EyH8AokC9ZK2Be6iZEW+zJM+sti7NB3ugVkb8G/Yo0Xw3OQ4cmY3RT4tcZMwAi3D7iz92wqldO
lhmRlAI+es8ZgzhNBXcefJppB9uxSRIq3mYf0PwHLq4iTjcDjycB2/XXvW2oBsX6out1s+Ym9ndyKx0CZxraAf15YTt+
sqkWdT7RK1Ldjlw31d/NPfz5Tq9JLI9/iyk7/HpsegQMLcUICuWNcw5QgKNdqdI7Xb1Otz/XeoLUlNIunKl6J/u9oIm
mzRAnBwvFnzeSdvl7MUjXwkGC7M9ql7UJZMGvCnkc+HFcdbBlpKHHHaalOzsnfVzgmY/RH7+47XMPf22Hzs6r9Z
iflm3MFfCkKy61M09O2mHdlLRaWgl1jy0i7FD2IFa2TqztF5yTWjY92Lw+2WdCsUfz08bExaCX/5y3UjVIvgU1Wwf
rzGYrVosXDfWAzWSuXXuV+bc+4gPyM/XDuSfnZEYUt36Go0h6qCBlzehAtPKp7S3IksQVNoWEdjxrf5Bun3/dUg0
1NOibVjGSOy3rGJ51TVvJnFV5hxlH047QQkr0VtVa3lmIxEsDfFHFZR3AVRE8HKBCSnxDKd/zcIR20l/fo3h/0GOuSP
TALKa5uNM</encryptedContent>
</encryptedContentInfo>
</envelope>


```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota
```
```
Agosto de 2024
Página 323 de 329
```
### ANEXO 22 FORMATO APROBACIÓN CUENTAS MONTOS SUPERIORES

Bogotá mes de día de año

Señores
ACH COLOMBIA S.A.
Atn. Dirección de Operaciones
Ciudad

Asunto: Solicitud para el procesamiento de transacciones por encima de los topes fijados por ACH COLOMBIA,
originadas por los clientes autorizados por el Banco para el efecto, según relación adjunta.

Respetados señores:

En nombre de NOMBRE DE BANCO presentamos a los clientes que, de acuerdo con el conocimiento que
tenemos de los mismos, autorizamos para que envíen transacciones por montos superiores por valor al límite
D que se encuentra establecido actualmente en ACH Colombia. Por lo anterior en el formato adjunto
relacionamos la información básica de dichos clientes para permitir transar la operación por valor de $MONTO
DE LA TRANSFERENCIA. Con fecha dd-mm-aaaa

Cordialmente,

### _____________________________

Representante Legal
Funcionário autorizado

Nombre: _____________________


```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota
```
```
Agosto de 2024
Página 324 de 329
```
### AUTORIZACIÓN CLIENTES PARA ENVÍO DE TRANSACCIONES ACH

### AUTORIZACION CLIENTES PARA ENVIO DE TRANSACCIONES DE MONTOS SUPERIORES AL LÍMITE

```
ESTABLECIDO EN ACHnet
```
```
Datos de la Autorización
FECHA
AUTORIZACION:
TIPO DE
NOVEDAD: Inclusión
```
```
Datos Básicos de Los Clientes
RAZON SOCIAL
CLIENTE
ORIGINADOR:
```
### NIT CLIENTE

### ORIGINADOR

```
No. CUENTA
RECEPTORA
```
### TIPO DE

### CUENTA

### NIT CLIENTE

### RECEPTOR

### ENTIDAD

### FINANCIERA

### RECEPTORA

```
Información del Representante Legal
```
```
NOMBRE: CEDULA:
Yo, en mi calidad de Representante Legal de NOMBRE DE BANCO, por medio de la presente y bajo mi
absoluta responsabilidad, autorizo a que se tramite la presente novedad con los clientes originadores
arriba mencionados, en la base de datos del sistema Integra ACH, en relación con el envío de operaciones
por montos superiores al límite B establecido actualmente en ACH COLOMBIA *.
```
```
Firma del Representante Legal
Para uso exclusivo de ACH COLOMBIA
```
Director de Operaciones

```
Coordinador de Operaciones
```
(^)


```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota
```
```
Agosto de 2024
Página 325 de 329
```
## ANEXO 23 Guía Plazos de solución a Revisiones y Devoluciones

## Plazo de Solución de REV - DEV

- Se tienen definidos 60 días hábiles para dar respuesta a las Reversiones y Devoluciones.
- Se deben dar respuesta de solución total o avance necesariamente los días ( 1 o 2, 16 o 17, 31 o 32, 46
    o 47 ) de no registrar respuesta en estos días se hará efectivo el esquema de calidad.
- Los demás días son opcionales para dar avances o cerrar el caso definitivamente.
- El día 60 se debe cerrar el caso con solución total caducidad de la reversión, en caso de no cerrar el
    caso se genera sanción por cada día tardío hasta que se realice el cierre a cada transacción asociada.


```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

```
Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota
```
```
Agosto de 2024
Página 326 de 329
```
```
b) Fecha a partir de la cual empieza a regir: 30 de abril 2011
```
Tabla 1
Directorio de contactos de EF

```
Entidad
Nombre persona
de seguridad Cargo^ Teléfonos^ Correo^ electrónico^
Banco Av.
Villas
```
### CARLOS

### ALBERTO

### BOTERO

```
(beltranrd@bancoavvillas.com.co)
(boteroc@bancoavvillas.com.co)
```
```
Banco
Bancamía
```
### VICTOR

### JARAMILLO

```
(victor.jaramillo@bancamia.com.co)
```
```
Banco BBVA
Colombia
```
### RUBÉN DARÍO

### MORERA

```
(ruben.morera@bbva.com.co)
```
```
Hunaldo
Armando Rocha
```
```
Dto. de
Seguridad
```
### 3471600

```
Ext 1034
```
```
hunaldo.rocha@bbva.com.co
```
```
Guillermo
Arismendi
Guevara
```
```
Dto. de
Seguridad
```
### 3471600

```
Ext 1845
```
```
guillermo.arismendi@bbva.com.co
```
```
Jorge Hernando
Cruz
```
```
Gestión y
Desarrollo
```
### 3822600

```
Ext 3046
```
```
jorgeh.cruz@bbva.com.co
```
```
Rafael Antonio
Lopesierra
```
```
Gestión y
Desarrollo
```
### 3822600

```
Ext 3045
```
```
rafael.lopesierra@bbva.com.co
```
```
Fredy Pinzon
González
```
```
Operaciones 3471600
Ext 1122
```
```
fredy.pinzon@bbva.com.co
```
```
Wilman Fabian
Tibambre
```
```
Operaciones 3471600
Ext 1752
```
```
wilmanf.tibambre@bbva.com.co
```
```
Banco BCSC LEONARDO
BOGOTA
```
```
(lbogota@fundacion-social.com.co)
```
```
Banco Bogotá SIXTO VARGAS
MORENO
```
```
Jefe del
Departamento
de Seguridad
```
### 3320032

```
ext. 1250
celular 311 -
2722100
```
```
(svargas@bancodebogota.com.co)
```
```
Raúl Uriel
Laverde Barreto
```
```
Líder
Departamento
de Seguridad
```
### 3320032

```
ext. 3498
celular 310 -
3217839
Banco
Colpatria
```
```
Monitoreo Área de Riesgo 7456300
ext. 3901 -
3902 - 3996 -
3995
```
```
riesoper@colpatria.com
```
```
Carlos Peña Área de Riesgo Ext.3993 penaca@colpatria.com
```

```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota

```
Agosto de 2024
Página 327 de 329
```
```
Entidad
Nombre persona
de seguridad Cargo^ Teléfonos^ Correo^ electrónico^
Mauricio
Vásquez B.
```
```
Director de
Riesgo
```
```
Ext.3991
Cel.320490
3019
```
```
vasquem@colpatria.com
```
```
Diana Duarte Área de
Canales
Electrónicos
```
```
Ext.3433 duarted@colpatria.com
```
```
Banco Citi BERNARDO
SOLÍS CUBIDES
```
```
(Bernardo.soliscubides@citi.com)
(maria.del.rocio.jimenez@citi.com)
Banco
Davivienda
```
### ALEJANDRO

### PATIÑO

### FÉLIX ROZO

### CAGUA

```
(apatino@davivienda.com)
(gpinto@davivienda.com)
(frozo@davivienda.com)
```
```
Banco GNB
Sudameris
```
```
Servicio al
cliente
```
```
3077707 CallCenter@gnb.loc
```
```
Humberto Ávila Director Pagos
Electrónicos
```
### 3368260

### 3387200

```
ext. 136
```
```
havila@gnbsudameris.com.co
```
```
Banco Helm
```
```
Ever Arévalo
Casas
```
```
Operaciones
Financieras e
Internacionale
s
```
### 5818181

```
ext. 2980 ever.arevalo@grupohelm.com
```
```
Miguel Ángel
Gracia Guzmán
```
```
Operaciones
Financieras e
Internacionale
s
```
### 5818181

```
ext. 3021 miguel.gracia@grupohelm.com
```
```
Nohora Torres
Acero
```
```
Operaciones
Financieras e
Internacionale
s
```
### 5818181

```
ext. 3018 nohora.torres@grupohelm.com
```
```
Stella Patricia
Ortiz
```
```
Dirección
control
transaccional 3394630 stella.ortizqgrupohelm.com
```
```
Francisco
Salcedo
```
```
Dirección
control
transaccional 3394642 francisco.salcedo@grupohelm.com
```
```
Luis Javier Rozo
```
```
Gerencia de
Riesgo
Operacional
```
### 5818181

```
Ext. 4396 luis.rozo@grupohelm.com
Banco HSBC DUBERNEY
HOYOS
CALDERÓN
```
```
(duberney.hoyos@hsbc.com.co)
```
```
Banco de
Occidente
```
```
Alexander
Gutiérrez
```
```
Director de
Productos y
```
```
Tel 8861111
Ext. 1810
```
```
fgutierrez@bancodeoccidente.com.co
```

```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota

```
Agosto de 2024
Página 328 de 329
```
```
Entidad
Nombre persona
de seguridad Cargo^ Teléfonos^ Correo^ electrónico^
Canales (Banca
empresarial)
Liliana Cortes Gerente de
Desarrollo
(Tecnología)
```
```
Tel.297200
0 Ext. 6677
```
```
lcortes@bancodeoccidente.com.co
```
```
Néstor Raúl
Cortes
```
```
Gerente
Soluciones
Funcionales
(Tecnología).
```
```
Tel.
2972000
ext. 6532
```
```
ncortes@bancodeoccidente.com.co
```
```
Evelio
Avellaneda
```
```
Director
Compensación
Electrónica
(Operaciones)
```
```
Tel.
2972000
Ext. 7033
```
```
eavellaneda@bancodeoccidente.com.co
```
### FRANKLIN

### TÍJARO

### FERNÁNDEZ

```
Gerente
Seguridad
Bancaria
```
```
tel.
2972000
ext. 6946 -
6941
horarios
habilites ftijaro@bancodeoccidente.com,co
HECTRO
OSWALDO ARIAS
```
```
Líder
seguridad
Bancaria
```
```
tel.
2972000
ext. 6947
horarios
hábiles hariash@bancodeoccidente.com.co
Ricardo Oliveros
Vargas
```
```
Líder
Monitoreo de
Transacciones
```
```
tel.
4000202
ext. 7422 y
7434
Horarios no
habilites roliveros@bancodeoccidente.com,co
Banco Popular JORGE
EDUARDO
PINZÓN
RODRÍGUEZ
```
```
(jorge_pinzon@bancopopular.com.co)
```
```
Banco
Procredit
```
```
Raúl Rivero Gerente de
Proyectos
```
```
Tel;
5954040
Ext. 7099
```
```
r.rivero@procredit.com.co
```
```
Carlos Alberto
López Bedoya
```
```
Gerente
Administrativo
y de
Operaciones
```
```
Tel:
5954040
Ext. 1010
```
```
c.lopez@procredit.com.co
```
```
Banco RBS AMPARO
ALARCÓN
```
```
(amparo.alarcon@rbs.com)
```

```
SERVICIO ACH TRANSFERENCIAS INTERBANCARIAS PARA ENTIDAD PARTICIPANTE
VERSIÓN 31^
```
### CONFIDENCIAL

Copyright © ACH COLOMBIA S.A
Prohibida su reproducción parcial o tota

```
Agosto de 2024
Página 329 de 329
```
```
Entidad
Nombre persona
de seguridad Cargo^ Teléfonos^ Correo^ electrónico^
Banco
Santander
```
### MARLON

### LASCANO

### OSCAR VARGAS

### DUARTE

```
(mlascanovesga@santander.com.co)
(ovargasduarte@santander.com.co)
```
```
Bancolombia Sergio Fernando
Londoño
quintero
```
```
Gerente 4041048 SELONDON@BANCOLOMBIA.COM
```
```
Tatiana Paola
Ochoa Jaramillo
```
```
Gerente 4041274 tochoa@bancolombia.com.co
```
```
Diana Marcela
Posada
```
```
Aux
Administrativo
```
### 4041044 DPOSADA@BANCOLOMBIA.COM.CO

```
Ana María
Gómez Pulgarín
```
```
Aux
Administrativo
```
```
4041393 anamargo@bancolombia.com.co
```
```
Sandra María
Echeverri
Arango
```
```
Jefe de Sección 4041057 SANECHEV@bancolombia.com.co
```

