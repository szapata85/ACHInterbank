## MANUAL DE OPERACIÓN CENIT WEB


- Fecha : 13 de junio de ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA
   - 1. CONTROL DEL DOCUMENTO CONTENIDO
   - 1.1 INFORMACIÓN DE DERECHOS DE AUTOR
   - 1.2 REFERENCIA DEL DOCUMENTO
   - 1.3 COMENTARIOS Y SOLICITUDES DE REVISIÓN
   - 1.4 TÉRMINOS, ABREVIATURAS Y CONVENCIONES
   - 1.5 DISTRIBUCIÓN
   - 1.6 ADMINISTRADOR DEL DOCUMENTO...................................................................
   - 2. INTRODUCCIÓN
   - 3. SESIONES Y FILTROS DE MENSAJES DEL CENIT-WEB
   - 3.1 PRIMERA SESIÓN DE COMPENSACIÓN
   - 3.2 SEGUNDA SESIÓN DE COMPENSACIÓN
   - 3.3 TERCERA SESIÓN DE COMPENSACIÓN
   - 3.4 CUARTA SESIÓN DE COMPENSACIÓN
   - 3.5 QUINTA SESIÓN DE COMPENSACIÓN
   - 3.6 TRANSACCIONES AUTORIZADAS POR SESIONES
   - 4. INGRESO AL SISTEMA CENIT-WEB
   - 5. VISIÓN GENERAL DEL SISTEMA
   - 5.1 MENÚ DE SISTEMA Y PANTALLA PRINCIPAL
   - 5.2 ELEMENTOS DE LAS PANTALLAS
      - 5.2.1 FILTROS
      - 5.2.2 LISTAS
      - 5.2.3 DETALLES DE AUDITORÍA
      - 5.2.4 ERRORES DE USUARIO
   - 6. ROLES Y USUARIOS EN CENIT
   - 6.1 MANTENIMIENTO
      - 6.1.1 LISTAR PARTICIPANTE
      - 6.1.2 MAPEO DE PRODUCTOS
      - 6.1.3 LISTAR CALENDARIO
   - 6.1.4 LISTAR ACTIVIDAD
   - 6.1.5 ALERTAS
   - 6.1.6 LISTAR PRODUCTOS
- 6.2 ENRUTAMIENTO
   - 6.2.1 LISTAR ARCHIVOS............................................................................................
   - 6.2.2 LISTAR LOTES
   - 6.2.3 LISTAR ÍTEMS
   - 6.2.4 LISTAR MENSAJES
   - 6.2.5 GATEWAY
- 6.3 COMPENSACIÓN
   - 6.3.1 ACTIVIDAD DE LA CUENTA
   - 6.3.2 ESTADO DE LA CUENTA
   - 6.3.3 SESIÓN
   - 6.3.4 TRANSACCIÓN
- 6.4 INFORMES
   - 6.4.1 VER
   - 6.4.2 INFORMES INTRADÍA
- 7. ADMINISTRADOR DE USUARIOS CENIT
- 7.1 ASPECTOS GENERALES
- 7.2 MANTENIMIENTO
   - 7.2.1 PERFIL
   - 7.2.2 USUARIO
   - 7.2.3 PARTICIPANTE
- 8. HISTORIAL DE CAMBIOS


##### MANUAL DE OPERACIÓN CENIT WEB

## Fecha : 13 de junio de ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

```
Fecha : 13 de junio de 2025
```
### 1. CONTROL DEL DOCUMENTO CONTENIDO

### 1.1 INFORMACIÓN DE DERECHOS DE AUTOR

```
Este documento contiene información confidencial del Banco de la República. En
consideración del recibo de este documento, el receptor está de acuerdo en no
reproducir o hacer disponible esta información de cualquier manera a personas
fuera de los destinatarios autorizados, directamente responsables de la evaluación
de su contenido.
```
### 1.2 REFERENCIA DEL DOCUMENTO

```
Fecha Título de Documento
Reglamento Circular Externa Operativa y de Servicios DSP-
152
Manual Operativo Circular Externa Operativa y de Servicios DSP-
152 - Anexo 2. Manual operativo
Manual Operativo Anexo A. Causales de devolución servicio de
compensación y liquidación
Formato NACHA-M
CENIT
```
```
Manual de Especificaciones Formato NACHA-
M CENIT
NACHA RULES 2002 NACHA RULES 2003
Manual de
Contingencia
```
```
Manual de Contingencia CENIT
```
```
Guía del Usuario del
Operador
```
##### 2024 - 06 - 13

```
CO_ACH_CENIT Guía de Usuario de Operador
CENIT
```
### 1.3 COMENTARIOS Y SOLICITUDES DE REVISIÓN

```
Todos los comentarios y solicitudes de revisión deben ser dirigidos vía correo
electrónico al administrador de este documento.
```
### 1.4 TÉRMINOS, ABREVIATURAS Y CONVENCIONES

```
No aplica.
```

##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

### 1.5 DISTRIBUCIÓN

```
La distribución de este documento se realiza de conformidad con la siguiente tabla:
```
```
Destinatario
Distribuido a No. de
copias
```
```
Comentario
```
```
Entidades Participantes Encargados Sistema CENIT/
Tecnología/Operaciones
```
##### 43

```
DSP Directora DSP 1
DSP Subdirectora DSP 1
CENIT Jefe Sección CENIT 1
CENIT Administrador CENIT 1
CENIT Profesional CENIT 4
Contratista Gerente de Proyecto 1
Ingeniero DGI Ingeniero Líder y Backup 2
TOTAL 54
```
### 1.6 ADMINISTRADOR DEL DOCUMENTO...................................................................

```
El administrador del presente documento es la Sección CENIT del Departamento de
Sistemas de Pago, Subgerencia de Sistemas de Pago y Operación Bancaria del Banco
de la República. Correo electrónico: cenit@banrep.gov.co
```

##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

### 2. INTRODUCCIÓN

Este documento describe los procedimientos de operación del Sistema de Compensación
Electrónica Nacional Interbancaria del Banco de la República – CENIT, y se constituye en
la guía de navegación del sistema para que el usuario con o sin experiencia, pueda
desplazarse fácilmente al interior de la aplicación y encuentre una rápida respuesta a las
inquietudes que se le puedan presentar con respecto al sistema.

En este documento el usuario no encontrará aspectos reglamentarios o relacionados con
las especificaciones del formato NACHA-M, ya que estos temas se tratan en otros
documentos especializados.

Para la operación del sistema, el usuario debe tener en cuenta las instrucciones que se le
hayan impartido sobre la utilización de los tokens y el nivel de acceso al CENIT-WEB,
elementos que forman parte de la autonomía de cada entidad cuando delega la
responsabilidad sobre la utilización del sistema al interior de la entidad.


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

### 3. SESIONES Y FILTROS DE MENSAJES DEL CENIT-WEB

El día operacional del sistema CENIT-WEB se desarrollará de acuerdo con los horarios y
ciclos descritos en l Circular Externa Operativa y de Servicios DSP- 152 - Anexo 2. Manual
operativo. En cada una de las sesiones se compensarán y liquidarán las transacciones que
hayan sido enviadas al sistema y se rechazarán las que por algún motivo no puedan ser
procesadas por el CENIT-WEB.

Durante el transcurso de cada sesión, así como al final del mismo, las entidades podrán
monitorear sus posiciones multilaterales y los movimientos realizados, como se indica
más adelante.

Para información de los participantes y con el propósito que el monitoreo realizado de los
archivos les permita identificar rápidamente si las transacciones contenidas, serán
procesadas o eventualmente rechazadas, se relacionan a continuación, los horarios y
sesiones autorizados en el sistema, junto con los diferentes tipos de mensajes permitidos
en cada uno de éstos.

### 3.1 PRIMERA SESIÓN DE COMPENSACIÓN

La primera sesión está comprendida entre las 07 : 30 a.m. hasta las 10: 30 a.m. En esta
sesión, el sistema procesará aquellas transacciones contenidas en los archivos y que
correspondan a transacciones monetarias débito y crédito, y prenotificaciones (no
monetarias) débito y crédito.

En esta sesión no serán compensadas las transacciones que correspondan a devoluciones,
con las siguientes excepciones:

- Devoluciones de Devoluciones correspondientes a devoluciones del día hábil
    bancario inmediatamente anterior.
- Devoluciones de días anteriores correspondientes a devoluciones por causal R23.

### 3.2 SEGUNDA SESIÓN DE COMPENSACIÓN

El horario dispuesto para esta sesión está comprendido entre las 11: 00 a.m. y las 1: 00
p.m. En esta sesión es posible el envío de todo tipo de transacciones al Sistema CENIT-
WEB, incluyendo devoluciones de transacciones monetarias y no monetarias, recibidas y


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

procesadas al término de la primera sesión y las de devolución de devolución,
correspondientes al día bancario inmediatamente anterior.

### 3.3 TERCERA SESIÓN DE COMPENSACIÓN

El horario dispuesto para esta sesión está comprendido entre las 1: 30 p.m. y las 3: 00 p.m.
En esta sesión es posible el envío de todo tipo de transacciones al Sistema CENIT-WEB,
incluyendo devoluciones de transacciones monetarias y no monetarias, recibidas y
procesadas al término de la segunda sesión.

### 3.4 CUARTA SESIÓN DE COMPENSACIÓN

El horario dispuesto para esta sesión está comprendido entre las 3: 30 p.m. y las 5: 15 p.m.
En esta sesión es posible el envío de todo tipo de transacciones al Sistema CENIT-WEB,
incluyendo devoluciones de transacciones monetarias y no monetarias, recibidas y
procesadas al término de la tercera sesión.

Este es la última sesión en la cual se reciben, compensan y procesan transacciones
monetarias débito o crédito.

### 3.5 QUINTA SESIÓN DE COMPENSACIÓN

Es la última sesión autorizada en el Sistema CENIT-WEB, su horario está comprendido
entre las 5: 45 p.m. y las 6: 45 p.m. En esta sesión, el sistema únicamente procesará
devoluciones monetarias y no monetarias, recibidas y procesadas al término de la cuarta
sesión, y devolución de una devolución de las devoluciones recibidas en los ciclos 2, 3 y 4,
rechazando de forma definitiva las de naturaleza distinta.

### 3.6 TRANSACCIONES AUTORIZADAS POR SESIONES

En la operación interna del CENIT-WEB, de acuerdo con la sesión que se esté procesando,
se habilitan o deshabilitan, según sea el caso, los diferentes códigos de transacción
creados en el sistema, de tal forma que se impida el ingreso de transacciones no
permitidas en una sesión específica; por lo tanto, las entidades autorizadas participantes
deben tener especial cuidado en la revisión de los archivos que envían en cada uno de los
ciclos de operación del sistema.


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

Así mismo, deben estar atentos a recoger oportunamente, todos los archivos generados,
tanto aquellos que notifican la aceptación o rechazo de transacciones para control de sus
operaciones, como los archivos correspondientes al cierre de cada sesión y aplicarlos en
sus sistemas.

### 4. INGRESO AL SISTEMA CENIT-WEB

Para ejecutar el Sistema de Compensación Electrónica Nacional Interbancaria **CENIT-
WEB** , las Entidades Autorizadas deben ingresar al Portal de Servicios del Banco de la
República, denominado **WSEBRA**.

Al ingresar al Portal WSEBRA, aparecerá la siguiente ventana:

Se debe escribir el usuario y la contraseña de la persona que va a ingresar al portal, como
aparece a continuación:

En esta ventana se debe ingresar el login del usuario en el campo “Usuario”, la contraseña
de acceso (ésta se compone de los cuatro dígitos secretos (PIN) del usuario y los seis
dígitos generados por el token OTP) y hacer clic en el botón “Ingresar”.

El token OTP cambia automáticamente cada minuto, por esta razón cuando la clave se
digita muy cerca al cambio de dicho término, suele suceder que el sistema la desconozca
y requiera que se ingrese de nuevo.


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

En el evento que la contraseña sea inválida, el sistema mostrará una ventana con el
mensaje de error. En este caso, reintente la acción anterior, pero si persiste el error
refiérase para mayor información al Manual de Usuarios de WSEBRA o bien, comuníquese
con el Centro de Soporte Informático del Banco de la República al call center 601 3431000.

Si la contraseña es válida, el sistema cargará la página principal que mostrará las
aplicaciones y/o servicios a los cuales el usuario tiene acceso autorizado, así:

De acuerdo con la imagen anterior, el usuario conectado tiene acceso entre otras, a los
siguientes sistemas:

- CENIT – WEB
- CENIT – PO

Para ingresar a cada una de los servicios, se debe dar clic sobre el link correspondiente.


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

### 5. VISIÓN GENERAL DEL SISTEMA

### 5.1 MENÚ DE SISTEMA Y PANTALLA PRINCIPAL

Después de que el usuario ha iniciado sesión exitosamente, se visualiza la pantalla
principal de la ACH CENIT-WEB

La información de la cabecera de la pantalla corresponde a:

1. Información relacionada con el proceso actual del sistema, al momento de
    ingresar. Informa el día de compensación abierto, los procesos anterior y
    siguiente que están programados en el sistema.
2. Barra de Menú
3. Información del usuario que se loguea en el sistema con login y nombre abreviado
    de la entidad. Adicionalmente, tiene dos banderas para cambiar de idioma entre
    inglés y español.
4. Alertas registradas por el sistema que el usuario tiene pendientes por revisar.
5. Fecha y hora actual del sistema.
6. Información del último ingreso exitoso del usuario al sistema.

### 5.2 ELEMENTOS DE LAS PANTALLAS

#### 5.2.1 FILTROS

La mayoría de las funciones a lo largo del sistema, utilizan pantallas de filtro con el fin de
obtener la información deseada de una lista de opciones. Éste es un ejemplo de una
pantalla de filtro más compleja:


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

```
Ítems de la página Significado
```
```
Filtros
```
```
Permite al usuario seleccionar una de las
opciones desplegables
Cajas de texto El usuario ingresa un parámetro específico
Campos “desde” - “a”
Permite al usuario recuperar los datos que
corresponden a un rango.
```
_NOTA: En los filtros de rangos es posible dejar en blanco los rangos, inicial, final o ambos._

_TIP: Mientras más filtros se utilicen, el tiempo de respuesta del sistema será más eficiente_.

#### 5.2.2 LISTAS

La mayoría de las funciones a través del sistema, utilizan pantallas de listas para visualizar
la información deseada. Todas las pantallas tienen controles comunes que permiten al
usuario navegar fácilmente a través de la lista, buscar un dato en la lista, ordenar y
exportar la lista.

Este es un ejemplo de una pantalla de lista:


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

```
El número de ítems desplegados en la página
```
```
Ir a la primera página
```
```
Ir a la página anterior
```
```
Información página actual y total de páginas
```
```
Ir a la siguiente página
```
```
Ir a la última página
```
```
Botones de Exportación: La lista se puede
exportar a archivos CVS o PDF
```
Una vez que un usuario selecciona un ítem de la lista, los detalles del ítem se visualizan
en otra pantalla:


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

Es importante tener en cuenta que si el resultado de la búsqueda, supera los 1.
registros, el sistema muestra la lista completa pero las opciones de ordenamiento por
columnas y los botones de exportación a PDF o CSV, se deshabitilan automáticamente.
En ese caso, se recomienda especificar o filtrar más la búsqueda.

#### 5.2.3 DETALLES DE AUDITORÍA

Los detalles de auditoría se muestran siempre en la parte baja de la pantalla a lo largo del
sistema, registrando el usuario que realizó una determinada operación y el registro de
fecha/hora (timestamp) de la misma.

Esta pantalla muestra por ejemplo, los detalles de una tasa con el registro de auditoría:


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

Después de hacer clic en el botón de **Detalles de Auditoría** , será visualizada en una lista
con las entradas de auditoría correspondientes:


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

Después de seleccionar la operación **Modificar** de la lista, se muestra la siguiente pantalla
de detalle de la entrada de auditoría:

#### 5.2.4 ERRORES DE USUARIO

Cuando ocurre un error, este se visualiza en la parte superior de la pantalla en color rojo.

Este error del usuario ocurrió porque el usuario no ingresó el Nombre del Perfil, el cual es
un campo requerido:

Los mensajes informativos se presentan en color azul y se utilizan para indicar que una
acción o evento fueron ejecutados exitosamente.


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

Por ejemplo, el siguiente mensaje se muestra cuando la operación Modificar se ha
ejecutado satisfactoriamente.

Los mensajes de advertencia tienen el propósito de alertar sobre un funcionamiento que
no cumpla todas las expectativas o que un error controlado puede ser un posible
problema que el usuario deberá tomar en cuenta.

Cuando no existe información ya sea en las operaciones listar, aprobar, rechazar,
modificar el sistema presentará el siguiente mensaje:


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

### 6. ROLES Y USUARIOS EN CENIT

El Sistema CENIT-WEB manejará dos roles de usuario externo: **Administrador de Usuarios**
y **Operador CENIT.**

El **Administrador de Usuarios** es el encargado de habilitar y/o deshabilitar los usuarios
autorizados de la entidad para operar en el sistema y asignar los permisos a las opciones
de menú que considere, de acuerdo con el perfil que establezca la entidad para cada
operador.

**_NOTA: Para conocer las funciones, procedimientos, opciones de menú y acciones correspondientes al
Administrador de Usuarios, ver el numeral 7 de este documento._**

El **Operador CENIT** tendrá el perfil que le sea asignado por parte del Administrador de
Usuarios de la Entidad Autorizada, el cual le permitirá realizar las actividades que le sean
autorizadas en las diferentes opciones del menú disponible.

### 6.1 MANTENIMIENTO

#### 6.1.1 LISTAR PARTICIPANTE

- **ACH / Mantenimiento / Participante / Listar**

El menú Participante, en el caso de los usuarios externos, será exclusivamente de consulta
para el Operador CENIT. Dicha información es administrada únicamente por la Sección
CENIT del Banco de la República.

Al diligenciar los campos de búsqueda, el sistema muestra la lista de los participantes
disponibles:


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

Se selecciona el participante que desea consultar y a continuación se presentará el
detalle:


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

#### 6.1.2 MAPEO DE PRODUCTOS

A través de esta función, el usuario podrá consultar la información relacionada con los
tipos de transacciones, también llamados productos, que cada Entidad Autorizada tiene
habilitados en el Sistema CENIT-WEB; cada tipo de transacción está autorizada para
Enviar, Recibir o ambas.

Esta información no es modificable y es administrada únicamente por la Sección CENIT
del Banco de la República

- **ACH / Mantenimiento / Participante / Mapeo de productos / Listar**

Al acceder a este menú, una lista con los diferentes productos creados y en estado Activo
será visualizada:


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

Al seleccionar un producto de la lista, se mostrará en detalle las opciones que tienen las
entidades para enviar y/o recibir el producto:


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

#### 6.1.3 LISTAR CALENDARIO

Esta función, presenta al usuario la información de los días hábiles, el día actual, los fines
de semana y feriados que aplican en el Sistema CENIT-WEB; los calendarios son creados
por meses.


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

Esta información no es modificable y es administrada únicamente por la Sección CENIT
del Banco de la República

- **ACH / Mantenimiento / Calendario / Listar**

Al acceder a este menú, una lista con los calendarios creados y en estado Activo será
visualizada:

De la lista proporcionada, se puede seleccionar el Calendario a consultar, haciendo clic
sobre él para ver el detalle:

El sistema diferencia el tipo de día por colores, así:

- Días de compensación (hábiles): color blanco
- Días feriados (no hábiles): color azul


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

- Día de compensación actual (día de operación abierto al momento de la consulta):
    color verde
- Días de compensación anteriores (días de operación ya cerrados al momento de la
    consulta): color gris.

### 6.1.4 LISTAR ACTIVIDAD

- **ACH / Mantenimiento /Actividad/ Listar**

El registro de actividad (log) consiste en las auditorías del sistema, de todas las acciones
y eventos ejecutados en el sistema. Es usado principalmente por el personal técnico para
localizar errores y por los auditores para investigaciones.

Cualquier operación de usuario en el sistema se registra. El usuario puede comprobar los
registros de actividad después de realizar cualquier operación en el sistema, para
asegurarse que fue registrado.

Después de seleccionar la opción, se presenta al usuario la siguiente pantalla:

Se puede consultar por distintos filtros:

- Desde (fecha - tipo calendario)
- De (hora en formato HH:mm)
- Hasta (fecha - tipo calendario)
- A (hora en formato HH:mm)
- Nombre de usuario
- Función
- Infor. adicional

Se selecciona uno o más filtros de consulta y después clic en **Aceptar**. Aparecerá una lista
filtrada con la información solicitada:


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

La información disponible en la lista consiste de lo siguiente: Nombre de Usuario, Nombre
de Grupo, Aplicación, Nombre del Flujo de Trabajo, Función, Operación, Hora de Inicio y
Tiempo de Ejecución.

Hacer clic un registro para ver sus detalles:

Independientemente de los filtros e información de detalle mostrada en la lista, siempre
se mostrarán los datos de Dirección IP del usuario y de Info. Adicional que consiste en el
número de la pantalla para la operación específica.

### 6.1.5 ALERTAS


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

Las alertas son notificaciones de eventos o actividades presentados en el sistema.

Las alertas genéricas son un tipo de alertas que están disponibles en el Sistema CENIT-
WEB. Cada alerta genérica se vincula a un evento específico y se puede configurar para
notificar a los usuarios del registro de un evento vía correo electrónico.

CENIT-WEB también permite mapear alertas genéricas para usuarios específicos, de
modo que solo esos usuarios reciban la notificación.

Las alertas generadas son aquellas notificaciones que se han creado ante la ocurrencia de
un evento específico, relacionado con una alerta genérica.

Las alertas genéricas se pueden ver usando la siguiente entrada del menú:

- **ACH/ Mantenimiento / Alertas / Listar / Genéricas**

En la siguiente pantalla se muestra la relación de alertas genéricas creadas en el sistema:

Cada alerta tiene una severidad (Importancia) que determina si la notificación se produce
para propósitos informativos ( **Información** ), porque ocurrió un evento que requiere ser
revisado ( **Advertencia** ) o bien, porque ocurrió un evento que requiere atención inmediata
( **Error** ).


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

La descripción de cada alerta se encuentra en la columna **Mensaje**. Si la alerta es
desactivada, aparecerá **‘No’** en la columna **Habilitado** y si está habilitada, aparecerá **‘Sí’**.

Una alerta que tiene estado **Activo** y está habilitada en el sistema, producirá una
notificación cuando se genere el respectivo evento.

Para ver los detalles de una alerta de la lista, se hace clic en el registro correspondiente:

- **ACH / Mantenimiento / Alertas / Listar / Generadas**

El filtro de este menú permite seleccionar el estado de la alerta ( **Activo** , **Nuevo** o
**Expirado** ) y las alertas disponibles. Se ingresa un rango de fechas para ver alertas
históricas y se hace clic en Aceptar.


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

El detalle con las alertas generadas, de acuerdo con el filtro seleccionado, aparecerá a
continuación:

### 6.1.6 LISTAR PRODUCTOS

Permite ver los diferentes tipos de transacciones (productos) creados en el Sistema
CENIT-WEB.


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

Esta información no es modificable y es administrada únicamente por la Sección CENIT
del Banco de la República.

- **ACH / Mantenimiento / Producto / Listar**

Los filtros de esta pantalla permiten seleccionar el producto por Nombre, Tipo de
Transacción, Moneda y Estado ( **Todos** , **Activo** o **Removido** ). Se selecciona el filtro(s) a
consultar y se hace clic en Aceptar.

Se selecciona un producto de la lista para ver los detalles del mismo y a continuación, se
verá la información:


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

## 6.2 ENRUTAMIENTO

El módulo de Enrutamiento del CENIT-WEB, permite consultar la información relacionada
con los **Archivos** , **Lotes** , **Ítems** y **Mensajes** que sean procesados por el sistema.

Cada archivo que ingresa al sistema, es validado de acuerdo con las condiciones técnicas
y de negocio establecidas en el Anexo 2 de la Circular Externa Operativa y de Servicios
DSP-152, marco normativo del CENIT.

Con base en la validación realizada, el CENIT-WEB determinará si el archivo y/o las
transacciones contenidas en éste, pueden ser aceptadas y procesadas en el sistema o si
por el contrario, deben rechazarse por alguna inconsistencia detectada.

El Operador CENIT debe monitorear cada archivo de entrada enviado al sistema,
verificando el posicionamiento del archivo y sus transacciones, mediante este menú.

Para el caso de Archivos, el estado final solo puede ser: **Aceptado** , **Parcial** o **Rechazado**.


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

**Aceptado** : El archivo se recibió en el sistema y las transacciones se encuentran
posicionadas para la compensación de la sesión iniciada.

**Parcial** : E archivo fue validado correctamente pero alguna(s) de las transacciones del
mismo, fueron rechazadas.

**Rechazado** : El sistema efectuó la recepción pero al realizar las validaciones encontró
inconsistencias que impedían el posicionamiento del archivo y es rechazado totalmente
junto con las transacciones contenidas.

### 6.2.1 LISTAR ARCHIVOS............................................................................................

- **ACH / Enrutamiento / Archivos / Listar**

Al seleccionar esta opción de menú, se mostrará la siguiente pantalla de búsqueda que
provee diferentes filtros de consulta. Utilice uno o varios de los filtros proporcionados ver
la información de los archivos; se puede consultar por **Tipo de Archivo, Moneda, Nombre
de Archivo, Remitente, Receptor, Fecha Hábil (rango de fechas de/a), Sesión (ciclo),
Monto Débito (rango de valor de/a), Monto Crédito (rango de valor de/a), Estado,
Dirección**. Haga clic en **Aceptar** para ejecutar la consulta; la siguiente pantalla mostrará
el resultado obtenido:


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

Haga clic en un archivo en particular para ver los detalles del mismo.


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

### 6.2.2 LISTAR LOTES

Los lotes en CENIT-WEB corresponden a bloques de transacciones ordenados con base en
el archivo de entrada.

- **ACH / Enrutamiento / Lotes / Listar**

Utilice uno o varios de los filtros proporcionados ver la información de los archivos; se
puede consultar por **Tipo de Archivo, Tipo, Moneda, Remitente, Receptor, Fecha Hábil
(rango de fechas de/a), Fecha Efectiva (rango de fechas de/a), Estado, Estado de
Compensación y Dirección** , para ejecutar la consulta. Haga clic en **Aceptar** para ejecutar
la consulta; la siguiente pantalla mostrará el resultado obtenido:

Haga clic en un registro en particular para ver los detalles del lote:


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

### 6.2.3 LISTAR ÍTEMS

Los ítems en CENIT-WEB corresponden a cada una de las transacciones recibidas en el
sistema

- **ACH / Enrutamiento / Ítems / Listar**

Utilice uno o varios de los filtros proporcionados ver la información de los archivos; se
puede consultar por **Tipo de Archivo, Tipo de Pago, Código Estándar de Clase de Entrada
(Tipo de Servicio: PPD, CCD, CTX), Número de Secuencia, Remitente (EAO), Receptor
(EAR), Fecha (rango de fechas de/a), Fecha Efectiva (rango de fechas de/a), Remitente
(Cliente Originador), Cuenta Remitente, Receptor (Cliente Receptor), Cuenta Receptor,
Estado, Estado de Compensación, Dirección y Sesión (ciclo)**. Haga clic en **Aceptar** para
ejecutar la consulta; la siguiente pantalla mostrará el resultado obtenido:


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

Haga clic en un ítem particular, para ver los detalles de este:

A través de esta pantalla, se puede consultar el lote que contiene este ítem, haciendo clic
en el botón **Lote Padre**.

En el caso de transacciones de Devolución y/o Devolución de Devolución, en la columna
“CAUSAL” se mostrará la descripción incluida en el campo 7 – Información Adicional, del
registro tipo 7 – Adenda, en el cual se indica la descripción estándar o ampliada de la
causal de Devolución o Devolución de Devolución.


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

En el caso de transacciones crédito y débito, en la columna “CAUSAL” se mostrará la
descripción incluida en el campo 3 – Información Relacionada con el Pago, del registro
tipo 7 – Adenda (posiciones de la 4 a la 83) si dicho campo fue diligenciado; en caso
contrario, dicho campo en la consulta aparecerá vacío.


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

### 6.2.4 LISTAR MENSAJES

Los mensajes en CENIT-WEB corresponden a la información asociada a todo archivo
entrante o saliente que se registre en el sistema.

Se pueden consultar cuatro tipos de mensajes:

- NACHA: Identificador de un mensaje que lleva un archivo de entrada en formato
    NACHAM, el mismo que ingresa al sistema desde la aplicación GATEWAY o PO.
- Archivo de Reconciliación: Identificador de un mensaje que lleva un archivo de
    salida en formato NACHAM que se generan al fin de sesión con el movimiento de
    la entidad y que se envía a la aplicación GATEWAY.
- Archivo de No Actividad: corresponde al registro de los archivos vacíos generados
    por el CENIT-WEB al cierre de una sesión para una entidad sin movimiento.
- XML: Identificador de un mensaje que lleva un archivo en formato XML que el
    sistema generará como respuesta positiva (ACK) o negativa (NACK), ante un
    archivo de entrada enviado al CENIT-WEB.

Para listar los mensajes, se ingresa a la ruta:


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

- **ACH / Enrutamiento / Mensaje / Listar**

La siguiente pantalla de filtro se presenta al usuario:

Utilice uno o varios de los filtros proporcionados ver la información de los archivos; se
puede consultar por **Referencia, Tipo, Propietario, Remitente, Receptor, Estado, Fecha
(rango de fechas desde/hasta)** ; los campos Desde y Hasta, filtran los mensajes al
momento en que el mensaje fue recibido o enviado.

Haga clic en **Aceptar** para ejecutar la consulta; la siguiente pantalla mostrará el resultado
obtenido:


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

Haga clic en cualquier registro de la lista, para ver la información detallada:
**Ejemplo de mensaje XML - NACK (Respuesta Negativa):**


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

Los rechazos por operador ACH se reciben en uno o varios archivos independientes, en
formato XML que se publican de forma inmediata a través del Gateway de la ACH. La
entidad debe estar en capacidad de leer estos archivos para determinar el tipo de error
obtenido y su corrección, si aplica.

En algunos casos, el mensaje de error informa la posición donde el sistema identificó el
error. Por ejemplo:

En esos casos, la entidad deberá revisar el archivo de entrada usando una herramienta
que le permita identificar la posición que reporta el archivo XML (Por ejemplo Notepad++
u otros)

**Ejemplo de mensaje XML - ACK (Respuesta Positiva):**


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

**Ejemplo de Archivo de No Actividad (archivo vacío):**

En cualquier caso, puede hacer clic en el botón de **Descargar** para bajar el archivo.

### 6.2.5 GATEWAY

El Gateway del CENIT-WEB provee el servicio de envío y recepción de archivos desde las
Entidades Autorizadas hacia el sistema.

Se deben tener en cuenta las siguientes consideraciones:
✓ El ingreso al Gateway se realizará a través de CENIT-WEB.
✓ El Gateway puede ser abierto (o iniciado) por un usuario que tenga los debidos
permisos (perfil) a nivel de aplicación, asignados previamente por el
Administrador de Usuarios de la Entidad.
✓ Solo puede haber un Gateway abierto al mismo tiempo. Sin embargo, varios
usuarios de una misma entidad pueden estar autorizados para abrir el Gateway.

Para información adicional, relacionada con la configuración del Gateway, consultar el
documento “ENVÍO DE ARCHIVOS A TRAVÉS DEL GATEWAY CENIT-WEB”.

Para realizar el envío de archivos al sistema, a través de Gateway, se ingresa a la ruta:

- **ACH / Enrutamiento / Gateway**

El sistema informa al usuario si hay conexiones existentes; si no hay ninguna conexión, el
usuario debe iniciarla haciendo clic en el botón Iniciar:


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

Luego de iniciar el Gateway, aparecerá una pantalla que informa al usuario la
configuración definida para el servicio, la cual dependerá de la opción de conexión
(carpetas de archivos o colas de mensajería) escogida por la Entidad Autorizada (Ver
documento “ENVÍO DE ARCHIVOS A TRAVÉS DEL GATEWAY CENIT-WEB”).

Hacer clic en el botón Aceptar para continuar:

Independientemente del tipo de conexión seleccionado por la entidad, los archivos a
enviar, deberán estar disponibles en la carpeta input o la ruta definida para el manejo de
las colas de mensajería, de tal manera que el Gateway pueda tomar el archivo y enviarlo
al sistema.

Para este documento, se trabajará con la opción de carpetas de entrada (input) y salida
(output).


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

##### 6.2.5.1 MONITOR GATEWAY

La pantalla para monitorear el proceso de envío o recepción de archivos se denomina
Monitor Gateway; en esta pantalla se visualizan tres secciones:

```
✓ Sección 1: Se observan los archivos de salida; son los archivos generados por el
CENIT-WEB con destino a la EA.
✓ Sección 2: Se observan los archivos de entrada; son los archivos enviados por la
EA con destino al CENIT-WEB.
✓ Sección 3: Corresponde al log de todos los procesos ejecutados en el Gateway
(envío y recepción)
```
En cada sección de la pantalla se pueden guardar los logs de proceso. Las secciones 1 y 2
permiten **iniciar** o **parar** el proceso ejecutado (envío o recepción).

**6.2.5.2 ENVÍO DE ARCHIVOS**

En este caso, teniendo el Gateway iniciado o antes de hacerlo, el usuario deberá colocar
en su carpeta de entrada (input), los archivos que serán enviados al CENIT-WEB:

```
1
```
```
2
```
```
3
```

##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

El Gateway busca en la carpeta definida y si tiene archivos disponibles, automáticamente,
los toma y los envía al CENIT-WEB. En el Monitor Gateway, se visualiza el estado del envío
de los archivos:

Sin embargo, el usuario deberá confirmar directamente en el sistema, el resultado del
procesamiento del archivo, mediante las opciones de menú ofrecidas por el módulo de
**Enrutamiento**.

**6.2.5.3 RECEPCIÓN DE ARCHIVOS**

Para recibir archivos, el Gateway deberá ser iniciado por el usuario o estar activo en el
momento de la generación de archivos en el sistema.


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

El CENIT-WEB automáticamente colocará, a través del Gateway, los archivos de salida
(movimiento, vacíos, mensajes de notificación XML) generados por el sistema con destino
a la Entidad; este proceso dejará los archivos disponibles para el usuario, en su carpeta
de salida (output).

## 6.3 COMPENSACIÓN

El Módulo de Compensación de la ACH permite al usuario la posibilidad de ver el estado
de las transacciones en cualquier parte del proceso, dentro de cualquier sesión.


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

Las transacciones son los débitos y créditos registrados en el sistema, en lotes ordenados,
generados por el sistema de ACH con base en los archivos de entrada.

### 6.3.1 ACTIVIDAD DE LA CUENTA

- **ACH / Compensación / Cuenta / Actividad**

Esta opción le permite al usuario monitorear la actividad de la cuenta de la entidad en
CENIT-WEB.

Se aclara que al hablar de “cuenta”, no se está refiriendo a la cuenta de depósito de la
entidad en el Sistema CUD; sino a la información de transacciones originadas y recibidas
por la entidad en el sistema CENIT-WEB.

La información corresponde a la actividad de la entidad contra las demás entidades
autorizadas participantes de la compensación. Al ingresar por la opción de menú
mencionada, se presenta la siguiente pantalla:

Por defecto, aparecerá la entidad a la cual está asociado el usuario.


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

El usuario puede consultar la información de la actividad de su cuenta por _todas las
sesiones o por una sesión específica, así como por la fecha hábil abierta_ ; seleccionando
los filtros como requiera. Para ver la información histórica, se debe cambiar la fecha hábil
por una fecha anterior.

Hacer clic en el botón **Limpiar** para borrar las selecciones realizadas.

Hacer clic en el botón **Aceptar** para ver la actividad de la cuenta; se muestra la siguiente
pantalla:

La información de la pantalla de ejemplo muestra los datos de las transacciones que
afectan a la cuenta de la entidad Banco de la República.

Las transacciones completadas, es decir, que ya hayan sido compensadas y liquidadas, se
mostrarán en color azul; mientras que las transacciones pendientes, es decir que aún no
han sido liquidadas, se muestran en color rojo. Esta información corresponde a un vínculo


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

que presentará el detalle de las transacciones cuantificadas con el valor mostrado en la
pantalla.

Hacer clic en cualquiera de estos vínculos para mostrar la lista de transacciones.

### 6.3.2 ESTADO DE LA CUENTA

- **ACH / Compensación / Cuenta / Estado**

La información que el usuario puede consultar a través de esta opción, corresponde a los
datos consolidados de las transacciones que afectan la cuenta de la entidad. Esta opción
únicamente muestra la información de la entidad sin compararla contra las demás
entidades participantes. Al ingresar por la opción de menú mencionada, se presenta la
siguiente pantalla:

Por defecto, aparecerá la entidad a la cual está asociado el usuario y solo puede consultar
el estado de su cuenta por _una sesión específica para el día de compensación luego de
haber sido cerrada_.

Hacer clic en el botón **Limpiar** para borrar las selecciones realizadas.

Hacer clic en el botón **Aceptar** para ver la actividad de la cuenta; se muestra la siguiente
pantalla:


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

La información de la pantalla de ejemplo muestra los datos de del estado de la cuenta de
la entidad Banco de la República para la primera sesión del día 20240723.

Los links en color correspondes a un vínculo que mostrará el detalle de las transacciones.
Hacer clic en cualquiera de estos vínculos para mostrar la lista de transacciones.

### 6.3.3 SESIÓN

##### 6.3.3.1 LISTAR SESIÓN

El usuario puede listar la información relacionada con las sesiones parametrizadas en el
sistema, con el propósito de validar en cuál de ellas se encuentra el sistema y en que parte
del proceso se encuentra dicha sesión.

Esta información no es modificable y es administrada únicamente por la Sección CENIT
del Banco de la República.

Para consultar esta información, se ingresa a la ruta:

- **ACH / Compensación / Sesión / Listar**

La siguiente pantalla se mostrará al usuario:


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

Seleccione los filtros requeridos para la consulta, elija la moneda de la sesión e ingrese
una fecha hábil o rango de fechas y haga clic en el botón **Aceptar**.

El botón de **Limpiar** borrará cualquier selección realizada en el filtro.

Seleccione la sesión que desea consultar; la siguiente pantalla presenta al usuario la
información relacionada con dicha sesión, estrictamente con carácter informativo.


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

##### 6.3.3.2 LISTAR MAPEO DE PRODUCTOS DE LA SESIÓN

En CENIT-WEB, al usar el término “Mapeo de Productos”, se hace referencia a los filtros
de mensajes que determinarán si las transacciones contenidas en un archivo de entrada,
serán procesadas o eventualmente rechazadas, de acuerdo con los tipos de mensajes
permitidos en cada una de las sesiones del sistema.

El usuario podrá saber a través de esta opción, para cuál o cuáles sesiones parametrizadas
del sistema, un código de producto (transacción) específico, se encuentra permitido su
envío y procesamiento.

Esta información no es modificable y es administrada únicamente por la Sección CENIT
del Banco de la República.

Para consultar esta información, se ingresa a la ruta:

- **ACH / Compensación / Sesión / Mapeo de productos / Listar**


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

Para ver el detalle se hace clic en el registro deseado:

De acuerdo con la pantalla anterior, el usuario podrá identificar:

1. **Nombre de Producto:** código asignado al tipo de transacción
2. **Tipo de Mapeo:** Aparecerá el término “Sesión” que indica que la transacción está
    habilitada para unas sesiones específicas.
3. **Mapeo del Producto:** en esta sección se relacionan a mano izquierda, las sesiones
    parametrizadas en el sistema y a mano derecha, se presenta un checkbox para
    cada sesión. El checkbox habilitado, indica que el tipo de transacción consultada


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

```
está permitido para la sesión relacionada; de lo contrario, la transacción no puede
ser enviada para la sesión. En el ejemplo la devolución tipo CCD cuenta corriente
NO está habilitada para la primera sesión y es permitida para las demás sesiones.
```
### 6.3.4 TRANSACCIÓN

##### 6.3.4.1 LISTAR TRANSACCIONES

Las transacciones son los débitos y créditos registrados en el sistema, en lotes ordenados
generados por el sistema de ACH con base en los archivos de entrada.

Por lo tanto, es importante tener claro que un lote ordenado de transacciones puede
estar compuesto por una o más transacciones individuales (ítems).

Las transacciones se pueden listar usando la siguiente entrada del menú:

- **ACH / Compensación / Transacción / Listar**

El sistema mostrará la siguiente pantalla de búsqueda:

Utilice uno o varios de los filtros proporcionados para buscar las transacciones; se puede
consultar por **Tipo (Crédito/Débito), Código de Sesión, Entidad (por defecto al usuario
solo le permitirá ver su propia información), Contraparte, Fecha Efectiva (rango de
fechas de/a), Fecha (rango de fechas de/a), Fecha Valor (rango de fechas de/a),
Cantidad (rango de valores de/a), Moneda (COP), Estado**. Adicionalmente, se pueden
ordenar los registros por alguna columna específica, de forma ascendente/descendente.


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

Para el campo **Estado** , los únicos estados para las transacciones son: Pendiente,
Transferido, Cancelado o Liquidado.

**Pendiente** : La transacción se recibió en el sistema y se encuentra pendiente para realizar
la liquidación en el siguiente cierre de sesión que corresponda.

**Transferido** : La transacción se recibió en el sistema pero se presentó faltante de fondos
al realizar la liquidación en el cierre de sesión para el cual ingresó y pasó al siguiente ciclo
de compensación; se permite hacer transferencia de transacciones por faltante de fondos
hasta la tercera sesión del día hábil.
**Cancelado** : El sistema intentó transferir la transacción a una siguiente sesión pero no
encontró una sesión siguiente que permit la compensar la transacción, es decir, que se
devuelve al partcipante originador.

También es válido cuando la entidad realiza la cancelación de forma manual, a través del
menú _ACH / Compensación / Transacción / Listar_.

**Liquidado** : El sistema recibió la transacción y fue compensada exitosamente.

Haga clic en **Aceptar** para ejecutar la consulta; la siguiente pantalla mostrará el resultado
obtenido:

Seleccione un ítem haciendo clic sobre el registro para ver la información detallada:


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

##### 6.3.4.2 CANCELAR UNA TRANSACCIÓN PENDIENTE

Una transacción solo puede ser cancelada por un usuario autorizado del participante
originador, si por alguna razón, la entidad desea o requiere retirarla de la compensación.

Una transacción SOLAMENTE podrá cancelarse cuando se encuentre en estado
_PENDIENTE_ , es decir, que no haya sido liquidada por el sistema.

El usuario SOLAMENTE podrá cancelar transacciones originadas por la entidad, es decir,
aquellas que envió con destino a las demás entidades. No podrá cancelar transacciones
en las cuales actúe como entidad receptora.

Es importante recalcar que un lote ordenado (transacción) puede estar compuesto por
una o más transacciones individuales (ítems).

Para cancelar una transacción, se ingresa al siguiente menú:


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

- **ACH / Compensación / Transacción / Cancelar**

Una lista de transacciones pendientes para la entidad originadora, se presenta al usuario:

Seleccione la transacción que se requiere cancelar. Los detalles de la transacción se
muestran en la siguiente pantalla:

Haga clic en el botón de **Cancelar** para cancelar la operación.

Haga clic en el botón de **Aceptar** para enviar la transacción a la cola de aprobación para
la Cancelación.

El siguiente mensaje de confirmación se visualiza:


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

Esto quiere decir que para cancelar efectivamente la transacción, se requiere doble
intervención.

##### 6.3.4.3 APROBAR CANCELACIÓN DE UNA TRANSACCIÓN

La cancelación de una transacción, debe ser aprobada para que sea efectivo el cambio.
Mientras la operación no sea aprobada, la transacción no será cancelada.

Para aprobar la cancelación se ingresa al menú:

- **ACH / Compensación / Transacción / Aprobar**

Una lista de transacciones en estado de aprobación se presenta al usuario:

En la columna denominada “ACCIÓN”, en la lista, indica que la operación a aprobar es
CANCELAR la transacción pendiente.

Seleccione el ítem de la lista para ver los detalles de la acción que requiere aprobación:


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

Haga clic en **Cancelar** para cancelar la aprobación. Esto implica que se cancela la
aprobación, es decir que la operación sigue en estado POR APROBAR y la transacción en
espera de ser cancelada.

Haga clic en **Rechazar** para remover la acción de la cola de aprobación. Esto implica que
la transacción no se cancela, vuelve a su estado PENDIENTE, quedando lista para
liquidarse.

Haga clic en **Aprobar** , de modo que la operación quede en estado ACTIVO. Esto implica
que la transacción queda cancelada y es retirada de la compensación para la entidad.

El siguiente mensaje de confirmación es desplegado:


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

Al volver a listar las transacciones disponibles para cancelar, usando la opción de menú
**ACH/Compensación/Transacción/Cancelar** , la transacción seleccionada no aparecerá
dentro de la lista:

Adicionalmente, el usuario podrá validar el estado de los ítems contenidos en la
transacción cancelada ingresando a la ruta **ACH/Enrutamiento/Ítems/Listar**
encontrándose en estado de compensación **Cancelado**.

## 6.4 INFORMES

Los informes en el Sistema ACH, se pueden generar por demanda (por solicitud del
usuario) o automáticamente, en momentos específicos o al final de un día de operación.

### 6.4.1 VER

Los informes generados automáticamente pueden ser vistos seleccionando la fecha de
operación abierta o una fecha de operación anterior en CENIT-WEB.

La entrada de menú para ver los informes automáticos es:

- **ACH / Informes / Ver**


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

La siguiente pantalla de búsqueda permite al usuario seleccionar el o los informes de su
interés:

Los informes se pueden filtrar por:

- **Tipo de Informe:** el usuario selecciona el informe que le interesa consultar. Si el
    valor seleccionado es “Todos”, el sistema buscará todos los informes disponibles
    que cumplan con los demás filtros de búsqueda.
- **Participante:** por defecto solo aparecerá la entidad a la que pertenece el usuario.
- **Fecha de negocio (desde/hasta):** corresponde a la fecha de negocio en la que el
    informe fue generado por el sistema. Se deben ingresar las fechas usando el
    formato de AAAAMMDD o usando el calendario proporcionado para cada campo.
    Este campo puede ser la fecha de operación abierta o una fecha de operación
    anterior.
- **Moneda:** por defecto la moneda será COP.

Adicionalmente, los informes pueden ser exportados a diferentes formatos. Esto se hace
seleccionado en el campo **Exportar como** , el tipo de archivo en el cual se desea ver el
informe:


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

```
Valor Descripción
PDF Informe exportado como archivo PDF (sale en pantalla)
CSV Informe exportado como un archivo de CSV - separados por coma (informe
descargable)
XLS Informe exportado como un archivo XLS (informe descargable)
```
Elija las opciones en el filtro anterior y haga clic en **Aceptar**. Una lista de informes
disponibles se mostrará al usuario:

La información disponible en la lista consiste de:

- **Tipo de informe** - el tipo de informe
- **Fecha -** fecha de negocio del informe
- **Moneda** - moneda para la cual se genera el informe
- **Participante** - el participante para el cual se genera el informe
- **Grupo** - el grupo del informe
- **Sesión** – sesión a la que corresponde el informe (si aplica para el tipo de informe)

Haga clic en el registro deseado de la lista para generar el informe.


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

### 6.4.2 INFORMES INTRADÍA

Los informes intradía son informes creados por demanda de usuario. Cada informe tiene
su propio filtro para la generación del mismo, basado en los criterios específicos.

**6.4.2.1 INFORME DE LOTES**

La entrada de menú para ver los informe intradía de lotes es:

- **ACH / Informes / Informes Intradía / Informe de Lotes**

La siguiente pantalla de búsqueda se presentará al usuario:

Los filtros disponibles para generar el informe de lotes son:

- **Referencia del lote:** dato asignado por el sistema, se deja en blanco.
- **Fecha hábil:** fechas de negocio desde y hasta que se desea consultar.
- **Sesión:** corresponde a la sesión en la que el lote o lotes fueron registrados.
- **Moneda:** por defecto la moneda será COP

Adicionalmente, el informe puede ser exportado a diferentes formatos. Esto se hace
seleccionado en el campo **Exportar como** , el tipo de archivo en el cual se desea ver el
informe:

```
Valor Descripción
PDF Informe exportado como archivo PDF (sale en pantalla)
CSV Informe exportado como un archivo de CSV - separados por coma (informe
descargable)
```

##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

```
XLS Informe exportado como un archivo XLS (informe descargable)
```
Se seleccionan los filtros deseados y se hace clic en el botón **Aceptar** para que el informe
se muestre al usuario, quien podrá guardar el informe localmente:

##### 6.4.2.2 INFORME DE ÍTEMS

La entrada de menú para ver los informe intradía de ítems es:

- **ACH / Informes / Informes Intradía / Informe de Ítems**

La siguiente pantalla de filtro se presenta al usuario:

Los filtros disponibles para generar el informe de lotes son:

- **Tipo de ítem:** filtro basado en tipo de ítem: Todos, NACHA.


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

- **Ref. de Ítem:** dato asignado por el sistema, se deja en blanco.
- **Fecha hábil:** fechas de negocio desde y hasta que se desea consultar.
- **Estado:** el estado del ítem (Enviado, Aceptado, Retirado)
- **Sesión:** corresponde a la sesión en la que el ítem o ítems fueron registrados.
- **Moneda:** por defecto la moneda será COP

Adicionalmente, el informe puede ser exportado a diferentes formatos. Esto se hace
seleccionado en el campo **Exportar como** , el tipo de archivo en el cual se desea ver el
informe:

```
Valor Descripción
PDF Informe exportado como archivo PDF (sale en pantalla)
CSV Informe exportado como un archivo de CSV - separados por coma (informe
descargable)
XLS Informe exportado como un archivo XLS (informe descargable)
```
Se seleccionan los filtros deseados y se hace clic en el botón **Aceptar** para que el informe
se muestre al usuario, quien podrá guardar el informe localmente:


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

## 7. ADMINISTRADOR DE USUARIOS CENIT

## 7.1 ASPECTOS GENERALES

El Sistema CENIT-WEB maneja esquema de roles de usuario: Administrador de usuarios
(Administrador EA) y Operador CENIT (Operador EA).

El **Administrador EA** es el encargado de habilitar y/o deshabilitar los usuarios autorizados
de la entidad para operar en el sistema y de asignar los permisos a las opciones de menú
que considere, de acuerdo con el perfil que establezca la entidad para cada operador.

El **Operador EA** tendrá el perfil que le sea asignado por parte del Administrador EA, el cual
le permitirá realizar las actividades que le sean autorizadas en las diferentes opciones del
menú disponible.

El procedimiento de creación de un usuario en WSEBRA, será el mismo,
independientemente del rol que le sea asignado (Administrador/Operador).

Es decir que la solicitud de creación de usuarios deberá hacerse ante el Centro de Soporte
Informático, a través del Gestor de Identidades provisto para tal fin y bajo las condiciones
de seguridad y procedimiento establecidas por dicha dependencia.

Cumplidas dichas condiciones, se crearán los usuarios en WSEBRA con su correspondiente
permiso para CENIT-WEB, en el ambiente que corresponda (Producción o Pruebas).

Sin embargo, la asignación del perfil para un nuevo usuario dependerá del rol que se le
asigne, así:

- El perfil del **Administrador EA** será asignado por la Sección CENIT del Banco de la
    República, en cabeza del administrador del sistema, siempre y cuando sea el
    primer administrador o no haya otro administrador disponible en la entidad.
- El perfil del **Operador EA** , será asignado por el **Administrador EA** Ldesignado en
    cada entidad, una vez que este cuente con su perfil en el sistema.

Vale la pena aclarar los siguientes puntos:

- Los usuarios autorizados para el Sistema CENIT-WEB, con el rol de **OPERADOR EA** ,
    deben contar con certificado digital para firma y encripción de archivos (SUCED o


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

```
el servicio que lo reemplace a futuro), habilitado y activo para este sistema si
dentro de sus funciones estará realizar la firma y encripción de archivos para envío
a través del Gateway de CENIT-WEB o bien, mediante CENIT-PO. Por lo anterior,
en este caso, la entidad deberá tramitar tanto la solicitud de Novedades SEBRA
como la creación del Certificado Digital que aplique.
```
- Los usuarios autorizados para el Sistema CENIT-WEB, con el rol de
    **ADMINISTRADOR EA** , solo requerirán token de acceso a WSEBRA, ya que para la
    función específica a desarrollar en el sistema, no aplica el certificado digital. Sin
    embargo, podrán solicitar dicho certificado digital, tenerlo habilitado y activo para
    este sistema, si dentro de la segregación de funciones internas se define que podrá
    firmar y encriptar los archivos que serán enviados por un **OPERADOR EA** a través
    del Gateway de CENIT-WEB o bien, mediante CENIT-PO. Por lo anterior, en este
    caso, la entidad deberá tramitar tanto la solicitud de Novedades SEBRA como la
    creación del Certificado Digital que aplique.

**El Administrador EA** podrá ejecutar las siguientes funciones:

- **Perfiles** :
    ✓ Crear perfiles para la entidad, con base en el perfil por defecto Operador
       EA.
    ✓ Modificar perfiles creados para la entidad (No podrá modificar el perfil por
       defecto Operador EA).
    ✓ Remover (eliminar) perfiles creados para la entidad (No podrá eliminar el
       perfil por defecto Operador EA).
- **Usuario** :
    ✓ Crear usuarios para la entidad, previa solicitud de novedades de usuarios
       ente el Centro de Soporte y confirmación de la misma.
    ✓ Modificar perfil a usuarios de la entidad (asignar perfil por defecto
       Operador EA o perfil propio para la entidad).
    ✓ Desactivar usuarios por inactividad en el sistema (p.e.: vacaciones).
    ✓ Activar usuarios por inactividad en el sistema (p.e.: vacaciones).

```
NOTA: Para eliminar usuarios de la entidad, se deberá realizar solicitud de
novedades de usuarios ente el Centro de Soporte, a través del Gestor de
Identidades provisto para tal fin y bajo las condiciones de seguridad y
procedimiento establecidas por dicha dependencia y no se deberá ejecutar
ninguna función en el sistema sobre el usuario retirado; por tal razón, no hay una
```

##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

```
opción de eliminar usuarios dentro de las funciones del Administrador.
Solamente, deberá confirmarse la eliminación, listando la relación de usuarios de
la entidad; el usuario eliminado seguirá apereciendo en el listado pero su estado
debe corresponder a “Removido”.
```
**El Administrador EA** tendrá autorizadas por defecto, las siguientes opciones de menú:

## 7.2 MANTENIMIENTO

El módulo de **Mantenimiento** proporciona las siguientes opciones: **Perfil** , **Usuario** y
**Participante**.

### 7.2.1 PERFIL

##### 7.2.1.1 LISTAR PERFIL

Para ver los perfiles creados previamente, utilice la siguiente función:

- **ACH / Mantenimiento / Perfil / Listar**

Hay un filtro disponible para listar perfiles:


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

El campo **Estado** permite filtrar la búsqueda con las siguientes opciones: **Todo** , **Activo** y
**Removido**.

Al seleccionar el filtro deseado y hacer clic en Aceptar, se muestra la información de los
perfiles creados que correspondan:

Posteriormente, al seleccionar uno de los ítems de la lista, el sistema mostrará el detalle
del perfil:


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

##### 7.2.1.2 CREAR PERFIL

El Administrador de Usuarios podrá crear perfiles adaptados a su operatividad, mediante
el uso de las siguientes funcionalidades:

- **ACH / Mantenimiento / Perfil / Crear**

Hay un filtro disponible para la creación de perfiles:


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

El usuario ingresa el nombre del perfil a crear y habilita (checklist) los permisos
requeridos, de acuerdo con sus requerimientos, con base en el perfil por defecto que el
sistema le mostrará y acepta los cambios.

El sistema solicita la confirmación de la creación del perfil:

Al revisar la información y aceptar, el sistema muestra el siguiente mensaje:


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

Esto quiere decir, que el perfil no quedará activo en el sistema, mientras no sea aprobada
la creación del mismo.

**7.2.1.3 MODIFICAR PERFIL**

- **ACH / Mantenimiento / Perfil / Modificar**

El sistema muestra los perfiles activos que se encuentran creados:

El Administrador elige el perfil que desea modificar y el sistema muestra la siguiente
pantalla:


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

El Administrador realiza las modificaciones necesarias para el perfil y acepta los cambios;
el sistema solicita confirmación antes de guardar los cambios.

Esta modificación de perfil, también requiere ser aprobada para dejar en firme los
cambios, por lo que el perfil queda en un estado Por Aprobar.

Luego de la aprobación, el sistema confirma la modificación del perfil.

##### 7.2.1.4 APROBAR PERFIL

- **ACH / Mantenimiento / Perfil / Aprobar**


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

Se muestran los perfiles pendientes de aprobación, el Administrador selecciona el perfil
que desea aprobar, verifica la información antes de proceder y aprueba la creación del
perfil, como se muestra en las siguientes pantallas:

Luego de la aprobación el sistema muestra la confirmación:

Después de crear y aprobar un perfil, este puede ser modificado, de acuerdo con los
requerimientos de la entidad, a través de su Administrador de Usuarios.

##### 7.2.1.5 REMOVER PERFIL

- **ACH / Mantenimiento / Perfil / Remover**


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

Se muestran los perfiles disponibles para remover, el Administrador selecciona el perfil
que desea eliminar, verifica la información antes de proceder y aprueba la eliminación del
perfil, como se muestra en las siguientes pantallas:

Luego de la aceptar el cambio el sistema muestra la confirmación:

### 7.2.2 USUARIO

A través del Módulo **Usuario** , el Administrador de Usuarios podrá realizar su función de
control, asignación de perfiles y activación o desactivación de los usuarios operadores
asociados a su entidad.

**7.2.2.1 LISTAR USUARIO**

- **ACH / Mantenimiento / Usuario / Listar**


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

El campo **Estado** permite filtrar la búsqueda de usuarios por las siguientes opciones: **Todo** ,
**Activo** , **Desactivado** y **Removido**.

Al filtrar y hacer clic en Aceptar, se muestran en pantalla los usuarios de la entidad, que
cumplan con el Estado seleccionado.

Para ver el detalle de un usuario, se elige el registro deseado de la lista:


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

##### 7.2.2.2 CREAR USUARIO

El proceso de creación de usuarios, tanto para el Sistema CENIT-WEB como para el
Sistema de Encripción de Archivos utilizado, se realiza mediante solicitud de novedades
de usuarios ante el Centro de Soporte, a través del Gestor de Identidades provisto para
tal fin y bajo las condiciones de seguridad y procedimiento establecidas por dicha
dependencia.

Por lo tanto, antes de que el Administrador ingrese a crear usuarios, previamente la
entidad deberá haber realizado la solicitud respectiva ante el Centro de Soporte
Informático; luego de haber recibido la confirmación de atención de su solicitud, el
Administrador de la entidad procederá con la creación de sus usuarios y la asignación del
perfil correspondiente.

- **ACH / Mantenimiento / Usuario / Crear**

El sistema muestra un filtro en donde se observa el nombre del grupo en el cual se va a
crear el usuario. Por defecto, aparecerá la entidad a la cual está asociado el
Administrador.


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

Al hacer clic en Aceptar, el sistema mostrará la lista de los usuarios ingresados al sistema
por el Centro de Soporte Informático, de acuerdo con la solicitud remitida previamente y
que se encuentran pendientes por crear en el sistema:

El Administrador selecciona el usuario que desea crear y a continuación se presentará el
detalle del usuario.

En esta pantalla, el Administrador podrá asignar el perfil que corresponderá a éste
usuario; se debe tener en cuenta, que solamente podrá modificar la información que se
encuentra habilitada (Correo Electrónico, Lenguaje Preferido y Nombre del Perfil).

Luego de diligenciar la información requerida, debe hacer clic en Aceptar:

El sistema le solicitará revisar la información antes de confirmar la creación del usuario:


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

Luego de aceptar la información, el usuario es creado quedando en estado **Por Aprobar** :

##### 7.2.2.3 MODIFICAR USUARIO

- **ACH / Mantenimiento / Usuario / Modificar**

El campo **Nombre del Grupo** permite filtrar la búsqueda. Por defecto, aparecerá la
entidad a la cual está asociado el Administrador.

El sistema muestra la lista de usuarios creados y disponibles para modificar:


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

El Administrador selecciona el usuario que desea modificar y a continuación se presentará
el detalle del usuario.

En esta pantalla, el Administrador únicamente podrá modificar la información que se
encuentra habilitada (Correo Electrónico, Lenguaje Preferido y Nombre del Perfil).

Luego de diligenciar la información requerida, debe hacer clic en Aceptar:

Se modifica el campo requerido y el sistema solicita la verificación del cambio:


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

Luego de la modificación el sistema muestra la confirmación:

##### 7.2.2.4 APROBAR USUARIO


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

- **ACH / Mantenimiento / Usuario / Aprobar**

Se muestran los usuarios creados, pendientes de aprobación; el Administrador selecciona
el usuario que desea aprobar, verifica la información antes de proceder y aprueba la
creación y/o modificación del usuario, como se muestra en las siguientes pantallas:


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

Luego de la aprobación el sistema muestra la confirmación:

Después de crear y aprobar un usuario, este puede operar el sistema de acuerdo con los
permisos habilitados según el perfil asignado.

Un usuario creado y activado en el sistema, puede ser modificado, de acuerdo con los
requerimientos de la entidad.

##### 7.2.2.5 DESACTIVAR USUARIO


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

- **ACH / Mantenimiento / Usuario / Desactivar**

El sistema muestra la lista de usuarios creados y disponibles para desactivar:

El Administrador selecciona el usuario que desea desactivar a continuación se presentará
el detalle del usuario.

Se solicita la confirmación antes de aceptar la desactivación del usuario:


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

Luego de la aprobación, el sistema confirma la desactivación del usuario.

##### 7.2.2.6 ACTIVAR USUARIO

- **ACH / Mantenimiento / Usuario / Activar**

El sistema muestra la lista de usuarios desactivados y disponibles para activar:


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

El Administrador selecciona el usuario que desea activar a continuación se presentará el
detalle del usuario.

Se solicita la confirmación antes de aceptar la activación del usuario:

Luego de la aprobación, el sistema confirma la activación del usuario.

### 7.2.3 PARTICIPANTE

El menú de Participante, en el caso de los usuarios externos, será exclusivamente de
consulta para el usuario. Dicha información es administrada únicamente por la Sección
CENIT del Departamento de Sistemas de Pago del Banco de la República.

##### 7.2.3.1 LISTAR PARTICIPANTE

- **ACH / Mantenimiento / Participante / Listar**


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

El sistema muestra la lista de los participantes disponibles en el sistema:

Se selecciona el participante que desea consultar y a continuación se presentará el
detalle:


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

##### 7.2.3.2 DESCRIPCIÓN DE CAMPOS DE UN PARTICIPANTE

A continuación, la información relacionada con los participantes del sistema CENIT:

```
Campo Descripción
Nombre * Nombre completo del participante.
Nombre corto * Un nombre acortado del participante.
Swift * BIC de SWIFT o Pseudo BIC de la entidad participante
Código * Código de compensación del participante
Código Sebra * Código SEBRA del participante
```
```
Idioma
```
```
El idioma preferido por defecto para los usuarios que son asignados al Grupo
del Participante.
Opciones disponibles:
* Inglés
* Español
```

##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

```
Tipo de Bloqueo Campo por defecto “Permitir Todos”
Contacto Principal El nombre del contacto principal del participante. Este campo es solo informativo.
Teléfono de Contacto
Principal
```
```
El número de teléfono del contacto principal del participante. Este campo es
solo informativo
Contacto Alternativo El nombre de un contacto alternativo del participante. Este campo es solo informativo.
Teléfono de Contacto
Alternativo
```
```
El número de teléfono del contacto alterno del participante. Este campo es
informativo solamente.
Dirección La dirección principal del participante.
Correo Electrónico El correo electrónico que será utilizado para enviar alertas y reportes.
Banco Central Indica que el participante es una Oficina Principal y operador del Sistema ACH.
Participante de Compensación Debe activarse para que el participante sea considerado en la compensación y por tanto pueda enviar y recibir pagos electrónicos
```
```
Participante de Conexión STP
```
```
Este campo indica si el participante participante tiene una conexión directa al
Sistema ACH CENIT.
Opciones disponibles:
```
- Ninguno: No se permite conexión entre ACH y el participante. Los mensajes
de entrada, salida y ACKs serán enviados y recibidos a través de CENIT-PO.
- Gateway: Se permite conexión vía Gateway entre el ACH y el participante. Los
mensajes de entrada, salida y ACKs serán enviados y recibidos a través del
Gateway.
**Copia de Archivos a PO** Este botón, cuando sea seleccionado, indica que el participante recibirá una copia de los mensajes de salida en el sistema CENIT-PO.

```
Tipo de liquidación
```
```
Indica si la liquidación de la posición multilateral al final de una sesión afectará
directamente a la cuenta del participante o indirectamente a la cuenta de otro
participante que actúa como agente de liquidación.
Ajuste de líneas NACHA Indica que los archivos NACHA que genera el ACH deben tener el texto ajustado (wrapped) en lugar de una línea continua de texto.
NIT Número de Identificación Tributaria de la entidad.
No. Cuenta depósito Número de la Cuenta de Depósito en CUD usada para la afectación de PMNs en cada ciclo.
```
```
No. Cuenta para Comisión Número de la Cuenta de Depósito en CUD usada para la afectación de procesos de facturación y cobro de la comisión CENIT.
Operador de Información Campo informativo que indica si el participante es un Operador de Información.
```
```
Tipo de Entidad
```
```
Opciones disponibles:
```
- Público
- Privado

```
Tipo de Facturación
```
```
Indica el tipo de tarifa de facturación que debe ser aplicada a la actividad de
compensación del participante. Opciones disponibles:
```
- Ninguno: Ninguna tarifa debe ser aplicada
- Fijo: Solamente se aplicará tarifa fija
- Variable: Solamente se aplicará tarifa variable
- Ambos: Se aplicará la tarifa fija y la tarifa variable


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

```
IVA
```
```
Por defecto el IVA está definido con parámetro general del sistema; este campo
definirá un valor del IVA aplicable solamente para el participante si así lo
requiere. Si no hay información en este campo, se aplica el IVA general.
No Envía Comisión a CUD Establece que el valor calculado por comisión no debe ser liquidado directamente en CUD.
```
```
Certicámara Al activar esta opción se instruye al sistema que los archivos de entrada deben contar con una firma adicional
```
```
Utiliza Canal Dedicado Indica si el participante utiliza un canal dedicado para conectarse al Portal WSEBRA y el sistema CENIT
```
##### 7.2.3.3 MAPEO DE PRODUCTOS

A través de esta función, el usuario podrá consultar la información relacionada con los
tipos de transacciones (productos) que cada Entidad Autorizada tiene habilitados en el
Sistema CENIT-WEB; si cada tipo de transacción está autorizada para Enviar, Recibir o
ambas.

Esta información no es modificable y es administrada únicamente por la Sección CENIT
del Departamento de Sistemas de Pago del Banco de la República.

- **ACH / Mantenimiento / Participante /Mapeo de Productos / Listar**

El sistema muestra la lista de los tipos de transacciones (productos) creados en el Sistema
CENIT-WEB:


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

Al seleccionar uno de los ítems, se presenta la siguiente información:


##### MANUAL DE OPERACIÓN CENIT WEB

##### ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

**Fecha :** 13 de junio de 2025

## 8. HISTORIAL DE CAMBIOS

```
Fecha Modificación / Inclusión / Eliminación
Marzo 2006 Ajuste Reglamentación
Febrero 2008 Actualización
Febrero 2009 Actualización
Febrero 2011 Actualización
Octubre 2013 Actualización
Abril 2014 Actualización
Octubre 2017 Actualización
Septiembre 2024 Se actualiza y ajusta el formato del documento.
Junio 2025 - Se actualiza la información del numeral 7.1.ASPECTOS
GENERALES del capítulo 7. ADMINISTRADOR DE USUARIOS
CENIT
```
- Se elimina el numeral 1.3 CAMBIOS DESDE LA VERSIÓN
ANTERIOR
- Se incluye el numeral 8. HISTORIAL DE CAMBIOS