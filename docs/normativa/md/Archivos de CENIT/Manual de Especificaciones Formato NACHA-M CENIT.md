## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

## TABLA DE CONTENIDO

1. INTRODUCCIÓN ........................................................................................................ 2
2. TERMINOLOGÍA ......................................................................................................... 2
3. ESTRUCTURA DE LOS ARCHIVOS ............................................................................... 3
3.1. SERVICIOS PPD - DEPÓSITO DIRECTO Y PAGO PREACORDADO Y CCD -
    CONCENTRACIÓN Y DISPERSIÓN DE FONDOS .......................................................... 4
3.2. SERVICIO CTX - INTERCAMBIO DE INFORMACIÓN CORPORATIVA .............................. 5
4. ESPECIFICACIONES DEL FORMATO NACHA-M ........................................................... 6
4.1. TIPOS DE DATOS ....................................................................................................... 6
4.2. DESCRIPCIóN DE CAMPOS ........................................................................................ 7
4.3. VALIDACIONES DE CAMPOS ...................................................................................... 7
4.4. VALIDACIONES POR PARTE DEL PARTICIPANTE RECEPTOR ........................................ 8
4.4.1. Validación de la identificación del receptor ................................................................ 8
4.4.2. Validaciones para las transacciones crédito (prEnotificación y monetarias) para los
    servicios PPD y CCD .................................................................................................. 9
4.4.3. Validación de las transacciones monetarias debito Prenotificaciones debito para los
    servicios PPD y CCD ................................................................................................ 10
4.4.4. Validación de las transacciones monetarias debito versus las prenotificaciones debito
    para los servicios PPD y CCD ................................................................................... 11
5. MANEJO DEL NÚMERO DE SECUENCIA .................................................................... 12
5.1. NÚMERO DE SECUENCIA PARA EL REGISTRO DE DETALLE DE TRANSACCIONES ...... 12
5.2. NÚMERO DE SECUENCIA PARA EL REGISTRO DE ADENDA EN TRANSACCIONES CON
    MÚLTIPLES ADENDAS – SERVICIO CTX ..................................................................... 12
6. RECOMENDACIONES PARA CONFORMACIÓN DE ARCHIVOS .................................. 13
6.1. PARA EL NOMBRE DEL ARCHIVO ............................................................................. 13
6.2. PARA EL CONTENIDO DEL FORMATO ....................................................................... 13
6.3. CÁLCULO DEL DÍGITO DE CHEQUEO ....................................................................... 15
7. FLUJOGRAMA DE TRANSACCIONES ......................................................................... 16
7.1. TRANSACCIONES GENERADAS POR UN PARTICIPANTE ORIGINADOR ...................... 17
7.1.1. Generación de transacciones de pagos (PRENOTIFICACIONES Y transacciones
    monetarias débito y crédito) .................................................................................... 18
7.1.2. Transacciones de devolución de una devolución ...................................................... 29
7.2. TRANSACCIONES GENERADAS POR UN PARTICIPANTE EN SU CARÁCTER DE
    PARTICIPANTE RECEPTOR ....................................................................................... 31
7.2.1. Generación de transacciones de devolución de prenotificaciones débito y crédito,
    monetarias débito y crédito ..................................................................................... 33
7.3. ARCHIVOS GENERADOS POR EL OPERADOR ACH ................................................... 38
7.3.1. Transacciones para aplicar: Prenotificación Débito y Crédito, Monetarias Débito y
    Crédito, Devoluciones y Devoluciones de Devoluciones ........................................... 39
7.3.2. Rechazos por el operador ACH ................................................................................. 39


## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

- Fecha: 7 de mayo de ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA
   - 7.3.3. Transacciones de Aviso ADV (Resumen Financiero y Contable)
   - 8. HISTORIAL DE CAMBIOS
- ANEXO 1 - DESCRIPCIÓN DE CAMPOS SERVICIOS PPD, CCD y CTX
      - 1.1 Registro de Encabezado de Archivo para todos los servicios
      - 1.2 Registro de Encabezado de Lote para todos los servicios
      - 1.3 Registro de Detalle de Transacciones – Servicios PPD y CCD
      - 1.4 Registro de Detalle de Transacciones – Servicio CTX
      - 1.5 Registro Adenda – Información Adicional para todos los Servicios
      - 1.6 Registro Adenda – Devolución para todos los servicios
      - 1.7 Registro Adenda – Devolución de una Devolución – Servicio PPD
      - 1.8 Registro de Control de Lote para todos los Servicios
      - 1.9 Registro de Control de Archivo para todos los Servicios
- ANEXO 2 – TABLAS ACLARATORIAS
         - CCD Y CTX TABLA 1 - CAUSAL DE DEVOLUCIÓN POR OPERADOR ACH PARA TODOS LOS SERVICIOS PPD,
   - TABLA 2 - CAUSALES DE RECHAZO DE ARCHIVOS POR OPERADOR ACH
   - TABLA 3 - OTRAS CAUSALES DE RECHAZO DE ARCHIVOS POR OPERADOR ACH
   - TABLA 4 - DESCRIPCIÓN DE LOTE
   - TABLA 5 - TIPOS DE SERVICIO
   - TABLA 6 - CÓDIGOS DE TRANSACCIÓN
   - TABLA 7 - CÓDIGOS DE AVISOS DE CONTABILIDAD (ADV)
         - TABLA 8 - CÓDIGOS DE CLASES DE TRANSACCIONES POR LOTE PARA TODOS LOS SERVICIOS
   - TABLA 9 - CÓDIGOS DE TIPO DE REGISTRO ADENDA PARA LOS SERVICIOS PPD, CCD Y CTX
   - TABLA 10 - CÓDIGO DE INDICADOR DE REGISTRO ADENDA PARA TODOS LOS SERVICIOS
   - TABLA 11 - CANALES DE PAGO


## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## Fecha: 7 de mayo de ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

### 1. INTRODUCCIÓN

## El presente documento describe las especificaciones técnicas para efectuar el intercambio electrónico de

## transacciones en el formato NACHA^1 , adaptado a normas y necesidades el sistema financiero colombiano,

## razón por la cual el formato de intercambio se denomina NACHA-M.

## Las definiciones o interpretaciones de este documento se deben seguir estrictamente por parte de los

## Participantes, de acuerdo con lo contemplado en la CEOS-DSP-152 “Reglamento del Sistema de

## Compensación Electrónica Nacional Interbancaria”.

## Las definiciones y terminología utilizada en este documento se encuentran contenidas en el mencionado

## Reglamento y se adicionan las contenidas en el numeral 2 del presente documento.

### 2. TERMINOLOGÍA

**Término Definición**

**Prenotificación**

```
Transacción no monetaria mediante la cual el Originador ordena a su Participante
Originador enviar una transacción a través del sistema ACH, a el Participante
Receptor para obtener una validación acerca de la existencia y condiciones de la
Cuenta Receptora.
```
**Rechazo de Archivo**

```
Archivo que no pudo ser procesado por el sistema ACH, al detectar errores de
formato. Es conocido también como Error Fatal.
```
```
Rechazo por Operador
ACH
```
```
Transacción que no fue aceptada por el sistema ACH, por no cumplir con las
condiciones establecidas. Es conocido también como Error Formal.
Devolución por Operador
ACH
```
```
Transacción que es procesada por el Sistema ACH, pero es devuelta por el
operador por una de las causales establecidas.
```
(^1) Libro Operating NACHA Rules 2000


## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

### 3. ESTRUCTURA DE LOS ARCHIVOS

## Toda la información de transferencias interbancarias manejada en el sistema ACH debe ser en formato

## estándar NACHA-M. Un archivo NACHA-M tiene seis (6) diferentes tipos de registros, cada uno con 106

## caracteres de longitud sin carácter de fin de línea y las siguientes características:

**Función Principal de los Registros NACHA-M**

```
Tipo de Registro Descripción
Registro de Encabezado
de Archivo
(Registro Tipo 1)
```
```
Identifica los participantes origen y destino inmediatos de las transacciones contenidas
en el archivo. Incluye además la fecha, la hora y el identificador del archivo, que
determinan al archivo de manera única.
Registro de Encabezado
de Lote
(Registro Tipo 5)
```
```
Identifica el Originador y describe brevemente el contenido del lote, de acuerdo con la
“Tabla No. 4 Descripciones de Lote”. La información de este registro aplica uniformemente
a los registros detallados incluidos en ese lote. La fecha efectiva para las transacciones en
este lote también está en este registro.
Registro Detallado de
Transacciones
(Registro Tipo 6)
```
```
Contiene la información para aplicar los débitos o créditos, tal como: Código del
Participante Receptor, número de la cuenta, nombre, Tipo de transacción, valor, entre
otros. La información del Registro de Encabezado de Lote incorporada con la información
de este registro describe completamente la transacción. Cada registro de detalle debe
llevar un número de control o de secuencia. Ver numeral 5. Manejo del Número de
Secuencia en archivos.
Registro Adenda
(Registro Tipo 7)
```
```
Este registro es utilizado para describir con mayor información un Registro de Detalle de
Transacciones. Sirve para enviar información relacionada con la transacción.
Para el servicio PPD - Depósito Directo y Pago Preacordado se maneja un solo Registro
Adenda por cada Entrada y es de uso obligatorio para las transacciones monetarias débito
y crédito, las prenotificaciones débito y las transacciones de devolución y de devolución
de una devolución.
Para el Servicio CCD - Concentración y Dispersión de Fondos se maneja un solo Registro
de Adenda por cada Entrada y es de uso obligatorio para todo tipo de transacciones:
crédito, débito, devolución y de devolución de una devolución.
Para el servicio CTX – Intercambio de Información Corporativa se manejan de 1 a 9999
Registros de Adenda por cada Entrada débito y crédito y es de uso obligatorio para todo
tipo de transacciones: crédito, débito, devolución y de devolución de una devolución.
Registro de Control de
Lote
(Registro Tipo 8)
```
```
Este registro está compuesto de los contadores y los totales de control de las
transacciones contenidas en un lote.
```
```
Registro de Control de
Archivo
(Registro Tipo 9)
```
```
Contiene los contadores y totales de control de las transacciones incluidas en el archivo.
Así mismo, contiene el número de lotes y el número de bloques en un archivo. Se deben
utilizar los registros de relleno que sean necesarios para completar bloques en múltiplos
de diez (10) al final del archivo.
```

## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

## 3.1. SERVICIOS PPD - DEPÓSITO DIRECTO Y PAGO PREACORDADO Y CCD - CONCENTRACIÓN Y

## DISPERSIÓN DE FONDOS

## La secuencia de los registros de los servicios PPD y CCD para las transacciones de pagos, de prenotificación,

## devoluciones, devoluciones de devoluciones, rechazos por operador y avisos de contabilidad ADV, se muestra

## a continuación:

## Existe un solo Registro de Encabezado y Control

## de Archivo por cada archivo y un solo Registro de

## Encabezado y Control de Lote por cada lote.

## • Existen tantos Registros de Encabezado y

## Control de Lote, como lotes existan en el

## archivo.

## • Para el servicio PPD aplica un tope máximo de

## 10.000 Registros de Detalle de Transacciones

## por archivo, en tantos lotes como se requieran.

## • Para el servicio CCD aplica un único Registro de

## Detalle de Transacciones por lote y un límite

## máximo de 10.000 lotes por archivo.

## • El Registro Adenda asociado a cada Registro de

## Detalle de Transacciones de pagos que se

## origine, puede ser opcional u obligatorio según

## el tipo de servicio.

## • El Registro Adenda es de uso obligatorio para

## el servicio PPD cuando se envían transacciones

## monetarias débito y crédito, prenotificaciones

## débito, transacciones de devolución y de

## devolución de una devolución. Para el servicio

## CCD es obligatorio para todo tipo de

## transacción.


## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

## 3.2. SERVICIO CTX - INTERCAMBIO DE INFORMACIÓN CORPORATIVA

## La secuencia de los registros para el servicio CTX, para transacciones de pagos, devoluciones, rechazos por

## operador y avisos de contabilidad ADV, se muestra a continuación:

## • Existe un solo Registro de Encabezado y

## Control de Archivo por cada archivo y un solo

## Registro de Encabezado y Control de Lote por

## cada lote.

## • Existen tantos Registros de Encabezado y

## Control de Lote, como lotes existan en el

## archivo.

## • Existen múltiples Registros de Detalle de

## Transacciones dentro de un lote (no hay límite

## de registros dentro de un lote).

## • Manejo de más de una adenda. Permite hasta

## 9.999 Registros de Adenda asociados a cada

## Registro de Detalle de Transacciones de pagos

## que se origine.

## Los Registros de Adenda son de uso obligatorio cuando se envían transacciones crédito o débito CTX y pueden

## contener de 1 a 9.999 registros.

## Para las transacciones de devolución y devolución de una devolución es obligatorio el uso de la adenda

## respectiva, al igual que para las transacciones no monetarias de prenotificación (una sola adenda por

## transacción).

## Para la conformación de los archivos de transacciones y de devoluciones, se debe tener en cuenta que las

## transacciones con servicio CTX no pueden enviarse con otros tipos de servicio en el mismo archivo, por lo

## cual deberán conformarse archivos independientes para este servicio.


## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

### 4. ESPECIFICACIONES DEL FORMATO NACHA-M

## 4.1. TIPOS DE DATOS

## Los caracteres usados para la elaboración de archivos en formato NACHA-M están restringidos a 0-9, A-Z y

## espacios. Los valores EBCDIC entre "00" - "3F" y ASCII entre "00" - "1F" no son válidos.

## Los caracteres aceptados en el formato NACHAM son:

## 0 1 2 3 4 5 6 7 8 9. , ; : - * / & # $ % =

## A B C D E F G H I J K L M N O P Q R S T U V W X Y Z

## a b c d e f g h i j k l m n o p q r s t u v w x y z

## Todos los campos alfanuméricos del archivo pueden contener caracteres de los anteriores con excepción del

## Registro Tipo 6 - Campo 5 - Número de Cuenta Receptora , el cual solo debe contener números.

## Los campos establecidos en el formato NACHA-M tienen las siguientes características:

## Alfanuméricos: Deben ser justificados a la izquierda, y completados con espacios a la derecha.

## Numéricos: Deben ser justificados a la derecha, sin signo y completados con ceros a la izquierda.

## Para cada campo se indica el tipo de inclusión dentro del formato, la cual puede ser:

## Mandatoria (M): Indica uso obligatorio. Campo requerido y validado por el sistema ACH para enrutar

## y procesar las transacciones correctamente.

## Requerida (R): Campo requerido y validado por el Participante Receptor para procesar y aplicar

## con éxito la transacción. El sistema ACH no verifica el contenido del campo, pero

## verifica que el campo esté contenido en el registro que se procesa.

## Opcional (O): Indica uso opcional a discreción del Originador o del Participante que origina la

## transacción; puede brindar complemento a la información de la transacción.

## No Disponible (N/D): Existen campos “Reservados” cuya inclusión es “No Disponible” siempre indica que

## su uso está supeditado a lo que establezca el Operador ACH.


## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

## 4.2. DESCRIPCIÓN DE CAMPOS

## El formato NACHA-M que se observa en el Anexo 1 detalla el contenido de los formatos de los registros y

## define los valores requeridos y los elementos de datos. Los requerimientos y especificaciones como

## contenido y longitud de los elementos se ilustran en estos formatos.

## El formato NACHA-M hace referencia a transacciones, causales y códigos que aplican para los servicios PPD

## (Pagos de cuentas previamente autorizadas o Depósito Directo), CCD (Concentración y Dispersión de Fondos)

## y CTX (Intercambio de Información Corporativa); para las transacciones de pagos, transacciones de

## devolución y devoluciones a devoluciones originadas por un Participante. De igual manera para las

## transacciones a aplicar, los rechazos y los avisos contables entregados por el Operador ACH.

## 4.3. VALIDACIONES DE CAMPOS

## El sistema ACH realiza validaciones sobre el nombre del archivo enviado, y sobre cada registro contenido en

## el archivo en formato estándar NACHA-M, teniendo en cuenta el tipo de inclusión, y el contenido especificado

## para cada campo. Los tipos de errores que se presentan por invalidez en los campos son:

## Errores

## Fatales

## Rechazo total de un archivo que no pudo ser procesado por el sistema ACH, al detectar

## errores de nombre, de formato o invalidez en algunos de sus campos. En este caso, el

## sistema ACH no procesará el archivo y el Participante debe corregir el error y re-enviar el

## archivo.

## Errores

## Formales

## Rechazo generado por el Operador ACH de una o varias transacciones no aceptadas por

## el sistema ACH, por no cumplir con las condiciones establecidas. Únicamente algunas

## validaciones producirán la generación de una transacción de rechazo por parte del

## sistema ACH, con causales específicas, según la Tabla No. 2 Causales de Rechazo por

## Operador ACH. En este caso, el sistema ACH procesará las transacciones correctas y

## generará los rechazos correspondientes, indicando para cada transacción la causal del

## error; el Participante puede corregir las transacciones con error y enviarlas nuevamente.

## Existe un número máximo de errores formales en un mismo archivo, que define el

## Operador de ACH. Si este número es superado, el error formal se convierte en error fatal

## del archivo.

## En el Anexo 1 se observan las causales de devolución o rechazo y el tipo de error que se puede presentar

## durante el envío de archivos, para cada uno de los campos que componen los registros del formato NACHA-

## M.

## El Sistema CENIT realiza validaciones de tipo formato y estructura NACHA-M, así como, validaciones de

## negocio. En cuanto a las validaciones de formato y estructura NACHA-M, el sistema se encuentra en


## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

## capacidad de validar el cumplimiento de la reglamentación en cada uno de los tipos de registros y campos de

## los archivos. Específicamente, inconsistencias en aspectos como la alineación de campos (alfanumérico o

## numérico) y el tipo de inclusión, son revisados y detectados por el sistema, generando rechazos totales o

## parciales de archivos.

## 4.4. VALIDACIONES POR PARTE DEL PARTICIPANTE RECEPTOR

## En este numeral se relacionan las diferentes validaciones que debe llevar a cabo un Participante en su

## carácter de Participante Receptor de transacciones a través del sistema CENIT, de acuerdo con los tipos de

## transacción que reciba y de los procesos a nivel de sus aplicaciones internas.

## Los Participantes que no efectúen las validaciones contenidas en este documento deberán ajustar sus

## sistemas internos para efectuarlas y realizar de manera procedente las devoluciones de transacciones a que

## haya lugar evitando así, posibles incumplimientos que den lugar a la aplicación de sanciones por parte del

## CENIT.

## 4.4.1. VALIDACIÓN DE LA IDENTIFICACIÓN DEL RECEPTOR

## El formato NACHA-M permite solicitar validaciones del campo Identificación del Receptor de forma específica

## para cada transacción, utilizando el campo Datos Discrecionales del Registro de Detalle de Transacciones.

## Según el tipo de transacción, la validación de la Identificación Receptor se hace opcional u obligatoria, así:

```
Tipo de
Transacción Descripción^
Crédito Es opcional para el Participante Originador y/o el Originador solicitar la validación de la identificación
del Receptor para las transacciones de prenotificación crédito y/o para las transacciones monetarias
crédito. Si requiere que el Participante Receptor realice la validación, debe colocar la letra V o v alineado
a la izquierda en el campo 9 – Datos Discrecionales del Registro de Detalle de Transacciones cuando
origine la transacción.
```
```
Esta letra indica al Participante Receptor que debe efectuar la validación de la identificación del Receptor
en la transacción que se envía. El Participante Receptor debe verificar si el campo 9 – Datos
Discrecionales del Registro de Detalle de Transacciones contiene la letra V o v alineado a la izquierda. Si
es así, el Participante Receptor debe efectuar la validación en su sistema interno. Si el campo 9 – Datos
Discrecionales del Registro de Detalle de Transacciones contiene una letra o símbolo diferente a V o v,
como por ejemplo espacios o cualquier otro, el Participante Receptor NO está en la obligación de
efectuar validación alguna, pero sí de aplicar la transacción.
```
```
Para las transacciones crédito, dependiendo de las políticas internas del operador ACH, puede
establecerse como obligatoria la prenotificación y la validación de la identificación del Receptor.
```

## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

```
Débito En el caso de las transacciones de prenotificación débito o de transacciones monetarias débito, no se
exige ninguna letra o símbolo especial para el campo 9 – Datos Discrecionales del Registro de Detalle de
Transacciones. El Participante Originador y/o Originador podrán diligenciar este campo a su total
discreción. Sin embargo, se debe enviar la identificación completa y correcta, ya que el Participante
Receptor debe validar SIEMPRE, la identificación del Receptor contenida en la transacción de
prenotificación débito y en la transacción monetaria débito, y generar la devolución correspondiente si
la identificación no coincide. El Participante Receptor NO debe verificar el contenido del campo 9 – Datos
Discrecionales del Registro de Detalle de Transacciones, sino realizar directamente y para todas las
transacciones débito, la validación en su sistema interno.
```
## En todas las transacciones de prenotificación débito y transacciones monetarias débito, así como en los casos

## crédito que se solicite la validación o se establezca como obligatoria, El Participante Originador y/o Originador

## deben diligenciar el campo 7 – Número de Identificación del Receptor del Registro de Detalle de

## Transacciones. La Identificación del Receptor debe diligenciarse de forma completa, con los ceros a la

## izquierda si los tiene. La identificación por validar no debe contener caracteres diferentes a números, debe

## ser alineada a la izquierda y rellenada con espacios a la derecha.

## La validación en el Participante Receptor consiste en confrontar el contenido del campo 7 – Número de

## Identificación del Receptor del Registro de Detalle de Transacciones contra la información registrada en sus

## bases de datos para el número de cuenta especificado en la transacción, independientemente de que se trate

## de un NIT, C.C. u otro tipo de documento y de que haya sido relacionado con o sin dígito de chequeo. Si

## existe más de una identificación asociada a esa cuenta, el Participante Receptor deberá validar contra todas

## las identificaciones asociadas. Si el número de identificación del Receptor coincide con la información

## registrada en sus bases de datos, el Participante Receptor deberá aplicar la transacción de prenotificación o

## la transacción monetaria, según sea el caso.

## En caso de que la información no coincida, el Participante Receptor debe generar la transacción de devolución

## correspondiente usando la causal R17 (la Identificación no coincide con cuenta del Receptor) de acuerdo con

## los lineamientos operativos y técnicos dados para generar transacciones de devolución.

## 4.4.2. VALIDACIONES PARA LAS TRANSACCIONES CRÉDITO (PRENOTIFICACIÓN Y MONETARIAS) PARA

## LOS SERVICIOS PPD Y CCD

## La validación que debe efectuar un Participante en su carácter de Participante Receptor para las

## transacciones crédito (prenotificaciones y monetarias), independientemente de que la transacción exija o no

## la validación de la cuenta y el número de identificación, es la siguiente:

## Tipo de cuenta y número de cuenta (en una sola validación) +

## • Número de identificación versus número de cuenta (cuando se solicita en datos discrecionales “v” o “V”)


## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

## • Estados de la cuenta (aplicando solo los estados definidos para este tipo de transacciones): Inactividad,

## Cuenta no habilitada para recibir fondos, Cuenta marcada como de la lista Clinton, Cuenta usada en

## medios políticos, etc.

## • Para las transacciones monetarias que lleven Adenda, se debe validar que el formato y campos de la

## adenda estén de acuerdo con el formato NACHA-M, es decir que contenga los campos establecidos y que

## sean del tipo indicado.

## • Para las transacciones tipo PPD Crédito Monetarias, la causal de devolución R32 aplica únicamente para

## el caso en donde NINGUNO DE los subcampos del campo # 3 “Información Relacionada con el Pago ( 3.1.

## Identificación del Originador, 3.4.1. Número de Factura o Cuenta y 3.4.3. Información Libre del Originador )”, contenga

## información; es decir que no haya información para reportar al receptor. Si alguno de los mencionados

## campos tiene información para entregar al receptor, no aplica la devolución.

## 4.4.3. VALIDACIÓN DE LAS TRANSACCIONES MONETARIAS DEBITO PRENOTIFICACIONES DEBITO PARA

## LOS SERVICIOS PPD Y CCD

## Para efectos de contar con un procedimiento estándar, garantizar una adecuada funcionalidad y dar un mayor

## grado de control y verificación de las transacciones tipo débito, se establece que siempre que un Participante

## actúe en su carácter de Participante Receptor de este tipo de transacciones, ya sea que se trate de

## prenotificaciones o transacciones monetarias, deberá efectuar la verificación y validación de la información

## reportada en la transacción que recibe, así:

## • Tipo de cuenta y número de cuenta (en una sola validación)

## • Número de identificación versus número de cuenta ( para débitos es obligatorio hacer esta validación)

## • Estados de la cuenta (aplicando solo los estados definidos para este tipo de operaciones): Inactividad,

## Embargo, Bloqueo por muerte del titular y demás bloqueos débito.

## • En cuanto a la validación de la adenda, se debe validar que el formato y campos de esta estén de acuerdo

## con el formato NACHA-M, es decir que contenga los campos establecidos y que sean del tipo indicado.

## • Para las transacciones tipo PPD Débito no Monetarias, la causal de devolución R31 (Ver Anexo No. 2 del

## Manual Operativo Sistema de Compensación Electrónica Nacional Interbancaria CENIT), aplica

## únicamente para el caso en donde el Campo 3 – “Información relacionada con el pago” de la Adenda, no

## contenga ninguna información (campo vacío o en blancos).

## Para las prenotificaciones débitos se establece en cuanto a su conservación y validez que éstas se conserven

## hasta trece (13) meses después de haberse recibido la última transacción monetaria. En caso de que se

## cumpla la condición anterior, si se desea reanudar el servicio de pago por débito directo, deberá efectuarse

## de nuevo el proceso de autorización y prenotificación previa.


## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

## 4.4.4. VALIDACIÓN DE LAS TRANSACCIONES MONETARIAS DEBITO VERSUS LAS PRENOTIFICACIONES

## DEBITO PARA LOS SERVICIOS PPD Y CCD

## Los Participantes se pueden catalogar en dos grupos y dependiendo la política aplicada deberán efectuar las

## validaciones establecidas para cada caso en particular, de la siguiente manera:

## a) Participantes que administran base de datos de prenotificados, validan la existencia de la

## prenotificación y comparan la información de la prenotificación versus la transacción monetaria:

## Para estos Participantes aplicaría el siguiente proceso de validación:

## • Validar la existencia de la prenotificación comparando los campos como se indica para las

## prenotificaciones en el numeral 4.4.3 frente a la transacción monetaria recibida.

## • Para la adenda se deben validar únicamente los campos de NIT o código EAN^1 y código de servicio.

## • Una vez barrido el proceso de verificación, si la prenotificación no es válida (algún campo no es

## coincidente), se devolverá por la Causal R10); si pasa la validación, entonces aplicaría la

## transacción.

## b) Participantes que no validan la existencia de la prenotificación ni validan la información de ésta

## versus la transacción monetaria: Para este grupo de Participantes se deberá efectuar la validación de

## la transacción monetaria de la misma forma como se indica para las prenotificaciones en el numeral

## 4.4.3.

(^1) Código asociado al número de identificación de las Administradoras del Sistema de Protección Social


## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

### 5. MANEJO DEL NÚMERO DE SECUENCIA

## 5.1. NÚMERO DE SECUENCIA PARA EL REGISTRO DE DETALLE DE TRANSACCIONES

## Los Participantes Originadores al enviar transacciones deben siempre preparar archivos de manera que los

## NÚMEROS DE SECUENCIA en los registros de detalle dentro de los lotes estén en orden ascendente

## consecutivo por archivo, de acuerdo con el Número de Secuencia asignado a cada transacción. Este número

## puede ser reiniciado diariamente o puede ser reiniciado únicamente cuando se termine la secuencia máxima

## de 9’999.999 transacciones permitidas por los siete (7) caracteres del formato NACHA-M. En cualquier caso,

## el Participante Originador no podrá asignar secuencias repetidas, secuencias no consecutivas en un mismo

## archivo o secuencias no ascendentes en un mismo día, ya que esto será causal de rechazo por parte del

## Operador ACH.

## Ya sea que la reiniciación de la secuencia se haga diariamente o únicamente cuando se agote la secuencia

## máxima, es importante que el Participante Originador determine el mecanismo para identificar de manera

## única las transacciones originadas y las transacciones de devolución que reciba, ya que esto le permitirá

## administrar adecuadamente las transacciones originadas, clasificar, devolver las respuestas a sus

## Originadores y aplicar las operaciones a los Originadores por concepto de devoluciones.

## Las transacciones de devolución generadas por los Participantes Receptores mantienen el número de la

## secuencia de la transacción original, y otros campos como la fecha de transmisión o la fecha efectiva, lo que

## permitirá al Participante Originador localizar la transacción original y los datos relativos al Originador de la

## transacción. El Participante podrá seleccionar el mecanismo más adecuado para identificar las transacciones

## que procesa.

## 5.2. NÚMERO DE SECUENCIA PARA EL REGISTRO DE ADENDA EN TRANSACCIONES CON MÚLTIPLES

## ADENDAS – SERVICIO CTX

## El número de secuencia asignado a cada una de las adendas para el servicio CTX debe asignarse en orden

## ascendente consecutivo por cada registro de detalle de transacción que contenga el archivo. El número de

## adendas por cada transacción crédito o débito CTX puede ir desde 0001 hasta 9.999.

## De igual manera, todos los registros de adenda que se relacionen en una transacción CTX deben estar

## asociados a un solo número de secuencia de transacción del registro de detalle de transacciones.


## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

### 6. RECOMENDACIONES PARA CONFORMACIÓN DE ARCHIVOS

## El Participante Originador o el Participante Receptor deben tener en cuenta los siguientes aspectos al generar

## o recibir archivos de transacciones ACH en formato NACHA-M:

## 6.1. PARA EL NOMBRE DEL ARCHIVO

## • El nombre del archivo debe tener la siguiente nomenclatura: RRRRTTT.ZZZ.1, donde RRRR es el código de

## Ruta, TTT es el código de Transito del Participante que genera el archivo, y ZZZ consecutivo diario para

## cada archivo enviado, iniciando en 1 y hasta 999.

## • El sistema ACH verifica que el número consecutivo ZZZ corresponda con el campo 7 - Identificador del

## Archivo, del Registro de Encabezado de Archivo. Se debe tener en cuenta para la correspondencia entre

## el campo 7 - Identificador del Archivo, del Registro de Encabezado de Archivo y el consecutivo ZZZ; que

## este último se reiniciará al valor ‘A’ cada 36 archivos hasta completar 999, así:

## 6.2. PARA EL CONTENIDO DEL FORMATO

## El Participante debe tener en cuenta las siguientes recomendaciones al generar o recibir archivos en formato

## NACHA-M:

## Archivos:

## • El Participante puede enviar máximo 999 archivos en el día con transacciones PPD y CCD en un mismo

## archivo o transacciones CTX en archivo independiente y recibir más de un archivo, por cada ciclo de

## operación que se ejecute en el sistema ACH, de acuerdo con los archivos que hayan sido procesados,

## así un archivo con transacciones PPD y CCD y un archivo solo con transacciones CTX (cuando le envíen

## en un ciclo transacciones de este tipo de servicio).

## • Al cierre de cada día operacional se publica el archivo ADV (resumen financiero y contable).

## • Los rechazos por operador ACH se reciben en uno o varios archivos independientes, en formato XML

## que se publican de forma inmediata a través del Gateway de la ACH.


## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

## • El Participante debe estar en capacidad de leer los archivos XML para determinar el tipo de error

## obtenido y su corrección, si aplica.

## • Todos los registros que se presentan en este formato son consistentes para todos los tipos de

## transacciones: débito, crédito, prenotificaciones, devoluciones y devoluciones de devoluciones.

## • Se deben diligenciar los campos según el tipo de inclusión y el contenido especificado en el formato.

## • Un mismo archivo no puede contener transacciones de servicios diferentes cuando se envían

## transacciones CTX, es decir, que debe conformarse, según corresponda un archivo independiente con

## transacciones CTX y de ser el caso otro archivo para las transacciones PPD y/o CCD.

## • Un mismo archivo puede contener diferentes tipos de lotes a la vez, como, por ejemplo:

## prenotificaciones crédito, prenotificaciones débito, transacciones monetarias crédito, transacciones

## monetarias débito, devoluciones o devoluciones de devoluciones.

## • Se debe respetar la secuencia de los registros establecida.

## • Un archivo que contenga transacciones de devolución puede conformarse con devoluciones de

## transacciones de diferentes tipos de servicio, siempre que se trate de PPD y CCD.

## • Un mismo archivo con transacciones de devolución de transacciones CTX no puede contener

## transacciones de devolución de servicios diferentes a éste, es decir, que deben conformarse archivos

## independientes con las transacciones de devolución para dicho servicio.

## Lotes:

## • Cada lote debe agrupar información relacionada entre sí, de un mismo Originador (compañía o tipo

## de canal de originación), identificado de acuerdo con lo recomendado en la Tabla No.^4 Descripciones

## de Lote.

## • Puede existir más de un lote de un mismo Originador.

## • Se incluirán lotes de devolución por operador ACH en los archivos que se publican al término de cada

## ciclo operacional, únicamente cuando las transacciones sean devueltas por fondos insuficientes en la

## cuenta de depósito del Participante Originador (causal R01) o cuando, las transacciones son

## canceladas manualmente por el Participante Originador (causal R34).

## • Un lote puede contener como máximo 100.000 transacciones.

## Transacciones:

## • Los registros de detalle deben estar relacionados con la descripción del lote.

## • Dentro de los lotes, los registros de detalle no obedecen a ningún tipo de ordenamiento específico.

## Deben conformarse de acuerdo con los parámetros y campos establecidos.

## Es recomendable que cada lote se conforme con un solo tipo de transacción: devoluciones, devoluciones de

## devoluciones, etc. Por lo tanto, podrá haber tantos lotes dentro de un archivo como tipos de transacciones

## se estén enviando.

## Adendas:

## • El número de Registros de Adenda que contenga una transacción debe estar acorde con el establecido en

## el formato, para cada uno de los tipos de servicio.


## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

## • Los Registros de Adenda deben incluir la información que se defina para cada tipo de servicio (PPD, CCD

## o CTX).

## • Para los servicios que permiten transacciones con múltiples Registros de Adenda se debe respetar la

## secuencia de los registros.

## 6.3. CÁLCULO DEL DÍGITO DE CHEQUEO

## El dígito de chequeo debe ser calculado de forma automático como sigue:

## 1. Multiplique cada dígito en el número de ruta y tránsito por un factor de peso.

## 2. Los blancos deben ser convertidos en ceros. Los factores de peso por cada dígito son:

```
TABLA CÁLCULO DÍGITO DE CHEQUEO
1 2 3 4 5 6 7 8 POSICIÓN
3 7 1 3 7 1 3 7 PESOS
0 R R R R T T T
0 0 0 0 7 8 0 63^78
PESOS: Es siempre una constante
0RRRR: Corresponde al código de ruta (Ciudad)
TTT: Corresponde al código de ruta y tránsito (Banco)
```
## 3. Sumar los resultados de los ocho cálculos.

## 4. Restar la suma del próximo número más alto correspondiente a múltiplo de 10.

## 5. El resultado obtenido es el dígito de chequeo.

## Ejemplo #

```
TABLA CÁLCULO DÍGITO DE CHEQUEO
1 2 3 4 5 6 7 8
3 7 1 3 7 1 3 7
0 0 0 0 1 8 0 9
0 0 0 0 7 8 0 63 78
```
## El dígito de chequeo será: 80 - 78 = 2

## Ejemplo #

```
TABLA CÁLCULO DÍGITO DE CHEQUEO
1 2 3 4 5 6 7 8
3 7 1 3 7 1 3 7
0 0 0 0 1 0 8 8
0 0 0 0 7 0 24 56 87
```
## El dígito de chequeo será: 90 - 87 = 3

## VALIDACIÓN:


## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

## Se validará el algoritmo anterior sobre el campo 4 - Digito de chequeo, del registro Tipo 6 - Detalle de

## Transacciones.

### 7. FLUJOGRAMA DE TRANSACCIONES

```
Entidad Financiera Originadora Operador ACH Entidad Financiera Receptora
```
```
1.1.
```
```
Generación de
Transacciones de
Pagos
```
```
1.2.
```
```
Generación de
Transacciones de
Prenotificación
```
```
3.4.Devoluciones a
Aplicar
```
```
1.4.
```
```
Generación de
Devoluciones de
Devoluciones
```
```
Formato
correcto?
```
```
Aplicación
Exitosa?
```
```
Formato correcto?
Existe Transacción
Original?
```
```
NO
```
```
SI
```
```
3.1.
```
```
Transacciones
de Pagos a
Aplicar
```
```
Archivos ADV
```
```
3.3 Archivos ACH / Avisos Contables
```
```
3.2Rechazos ACH
Aplicación
Exitosa?
```
```
NO
```
```
SI
```
```
1.3.
```
```
Generación de
Devoluciones de
Transacciones
```
```
Archivos de Salida ACH
SI
```
```
NO
3.2Rechazos ACH
```
```
SI
```
```
NO
```
```
3.5.
```
```
Devoluciones de
Devoluciones a
Aplicar
```
```
SI
```
```
Rechazos ACH
```
```
NO
3.
```
```
Formato correcto?
Existe Devolución
Original?
```

## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

## 7.1. TRANSACCIONES GENERADAS POR UN PARTICIPANTE ORIGINADOR

```
Un Participante Originador puede iniciar archivos de transacciones de prenotificación crédito y débito y de
transacciones de pagos crédito y débito hacia un Participante Receptor, de acuerdo con el siguiente formato:
```
```
Registro de Encabezado de Archivo para todos los Servicios
Para transacciones de: Prenotificación Débito y Crédito, Monetarias Débito y Crédito y Devoluciones de Devoluciones
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción
1 Tipo de registro M “1” 1 1 Valor válido para este campo "1".
2 Código de prioridad R N 2 2 - 3 Valor válido “01”.
```
```
3 Código Participante destino
inmediato
```
```
M b0RRRRTTTC 10 4 - 13 Código de la ACH.
```
###### 4

```
Código Participante origen
inmediato M^ b0RRRRTTTC^10 14 -^23
```
```
Código del Participante Originador que envía
el archivo.
5 Fecha de creación del archivo M AAAAMMDD 8 24 - 31 Fecha de creación del archivo.
```
```
6 Hora de creación del archivo O HHMM 4 32 - 35
```
```
Hora en la cual es transmitido o creado el
archivo.
```
```
7 Identificador del archivo M A-Z / 0- 9 1 36 - 36 Identificación de archivos creados en la
misma fecha (máximo 999 /día)
```
```
8 Tamaño del registro M ‘106’ 3 37 - 39
```
```
Indica el número de caracteres contenidos en
cada registro.
```
```
9 Factor de ablocamiento M ‘10’ 2 40 - 41 Define el número de registros dentro de un
bloque.
10 Código de formato M ‘1’ 1 42 - 42 Permite futuras variaciones de formato.
```
```
11 Nombre entidad destino inmediato O AN 23 43 - 65 Nombre de la ACH, en nuestro caso CENIT
```
###### 12

```
Nombre entidad origen
inmediato O^ AN^23 66 -^88 Nombre del^ Participante^ Originador^
13 Código de referencia M AN 8 89 - 96 Identifica el código del sistema.
```
```
14 Reservado N/D Blancos 10 97 - 106 Campo reservado. Este campo debe ir en
blancos.
```
**Registro de Control de Archivo para todos los Servicios
Para transacciones de: Prenotificación Débito y Crédito, Monetarias Débito y Crédito y Devoluciones de Devoluciones
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción**
1 Tipo de registro M “9” 1 1 Valor válido para este campo "9".
2 Cantidad de lotes M N 6 2 - 7 Número de lotes incluidos en el archivo.

3 Numero de bloques M N 6 8 - 13 Número de bloques físicos en el archivo de 10 registros cada uno.

4 Número de transacciones
detalladas y de registros adenda

```
M N 8 14 - 21 Número total de registros de detalle^ y de
adenda en el archivo.
```
5 Totales de control M N 10 22 - 31

```
Sumatoria de los códigos de los
Participantes Receptores de los Registros de
Detalle de Transacciones.
```
6 Valor total de débitos M $$$$$$$$$$$$$$$$cc 18 32 - 49 Suma de los valores de las transacciones
tipo débito del archivo.

7 Valor total de créditos M $$$$$$$$$$$$$$$$cc 18 50 - 67

Suma de los valores de las transacciones
tipo crédito del archivo.
8 Reservado N/D Blancos 39 68 - 106 Campo reservado no disponible.


## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

## 7.1.1. GENERACIÓN DE TRANSACCIONES DE PAGOS (PRENOTIFICACIONES Y TRANSACCIONES

## MONETARIAS DÉBITO Y CRÉDITO)

## Un Participante Originador puede iniciar archivos de transacciones de prenotificación crédito y débito y de

## transacciones de pagos crédito y débito hacia un Participante Receptora, de acuerdo con el siguiente formato:

```
Registro de Encabezado de Lote - Servicios PPD y CTX
Para transacciones de: Prenotificación Débito y Crédito y Monetarias Débito y Crédito
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción
1 Tipo de registro M “5” 1 1 Valor válido para este campo "5”.
```
```
2 Código clase de transacciones por lote M N 3 2 - 4 Código de acuerdo con la Tabla^ N
```
```
o.^
8.
3 Nombre del Originador
M AN 16 5 - 20
```
```
Nombre del Originador para
propósitos descriptivos (1).
4 Datos discrecionales del Originador
O AN 20 21 - 40
```
```
Datos del Originador o del
Participante Originador.
5 Identificación del Originador R AN 10 41 - 50 Número de identificación del
Originador (1).
6 Tipo de servicio M AN 3 51 - 53 De acuerdo con la Tabla No.^5 (PPD o
CTX)
7
M AN 10 54 - 63 Descripción del lote según la Tabla
No. 4
8 Fecha descriptiva O AN 8 64 - 71 Fecha de carácter informativo
asignada por el Originador.
```
9 Fecha efectiva de la transacción (^) M AAAAMMD
D
8 72 - 79 Fecha en la cual se deben aplicar las
transacciones del lote.
10 Fecha de compensación juliana
M N 3 80 - 82
Fecha de compensación o
liquidación de las transacciones.
11 Código estado del Originador (^) M AN 1 83 - 83 El valor válido es “1” e indica el
estado del Originador.
12 Código Participante Originador M 0RRRRTTT 8 84 - 91 Código del Participante Originador
(^13) Numero de lote M N 7 92 - 98 Secuencial ascendente único para
cada lote en el archivo.
14 Reservado N/D Blancos 8 99 - 106 Campo reservado.
(1) Campo de obligatorio diligenciamiento para los adquirentes, en los cuales deben indicar el nombre y el número de identificación completos
del cliente originador.


## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

```
Registro de Encabezado de Lote - Servicio CCD Sistema Seguridad Social SSS
Para transacciones de: Prenotificación Débito y Monetarias Débito
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción
1 Tipo de registro M “5” 1 1 Valor válido para este campo “ 5 ”
```
```
2
```
```
Código clase de transacciones por
lote M^ N^3 2 -^4 Código de acuerdo con la Tabla N
```
```
o.^8
```
```
3 Nombre del Originador M AN 16 5 - 20 Nombre del Originador para propósitos
descriptivos.
```
```
4 Datos discrecionales del Originador O AN 20 21 - 40
```
```
Datos del Originador y/o del Participante
Originador
5 Identificación del Originador R AN 10 41 - 50 Número de identificación del Originador.
```
## 6 Tipo de servicio M AN 3 51 - 53 De acuerdo con la Tabla No. 5 (CCD)

## 7 Descripción de lote M AN 10 54 - 63 Descripción del lote según la Tabla No.^4.

```
8 Fecha descriptiva O AN 8 64 - 71 Fecha de carácter informativo asignada
por el Originador.
```
```
9 Fecha efectiva de la transacción M AAAAMMDD 8 72 - 79
```
```
Fecha en la cual se deben aplicar las
transacciones del lote.
```
```
10 Fecha de compensación juliana M N 3 80 - 82 Fecha de compensación o liquidación de
las transacciones
11 Código estado del Originador M AN 1 83 - 83 El valor válido es “1”.
12 Código Participante Originador M 0RRRRTTT 8 84 - 91 Código del Participante Originador.
```
```
13 Numero de lote M N 7 92 - 98
```
```
Secuencial ascendente único para cada
lote en el archivo.
14 Reservado N/D Blancos 8 99 - 106 Campo reservado.
```

## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

```
Registro de Encabezado de Lote - Servicio CCD Sistema Seguridad Social SSS
Para transacciones de: Prenotificación Crédito y Monetarias Crédito
```
```
# Nombre de Campo Inclusión Contenido Longitud Posición
```
```
Descripción para pagos
Seguridad Social SSS
```
```
Descripción para pagos
Seguridad Social Régimen
Subsidiado PRS
```
```
1 Tipo de registro M “5” 1 1
```
```
Valor válido para este
campo 5.
```
```
Valor válido para este
campo 5
```
```
2 Código clase de
transacciones por lote
```
```
M N 3 2 - 4 Valor válido 220 - créditos Valor válido 220 - créditos
```
(^3) Nombre del aportante M AN 16 5 - 20
Nombre del Aportante
que realiza el pago, para
propósitos descriptivos.
Nombre del municipio,
ente territorial, EPS u otra
entidad que realiza el
pago.

###### 4

```
Datos discrecionales del
Originador O^ AN^20 21 -^40
```
```
Datos del Originador y/o
del Participante
Originador
```
```
Datos del Originador y/o
del Participante Originador
```
```
5 Identificación del aportante R AN 10 41 - 50
```
```
Número de identificación
del Aportante.
```
```
NIT del municipio o ente
territorial cuando el giro es
realizado por el ente y NIT
de la EPS cuando el giro lo
realiza una EPS a una IPS o
beneficiario
6 Tipo de servicio M AN 3 51 - 53 Valor válido CCD* Valor válido CCD*
```
## 7 Descripción de lote M AN 10 54 - 63 Descripción del lote según la Tabla No^ 4.

```
8 Fecha descriptiva O AN 8 64 - 71 Fecha de carácter
informativo
```
```
Fecha de carácter
informativo
```
```
9 Fecha efectiva de la
transacción
```
###### M AAAAMMD

###### D

###### 8 72 - 79

```
Fecha en la cual se deben
aplicar las transacciones
del lote.
```
```
Fecha en la cual se deben
aplicar las transacciones
del lote.
```
###### 10

```
Fecha de compensación
juliana M^ N^3 80 -^82
```
```
Fecha de compensación o
liquidación de las
transacciones.
```
```
Fecha de compensación o
liquidación de las
transacciones.
```
```
11
```
```
Código estado del
Originador M^ AN^1 83 -^83 Valor válido “1”^ Valor válido “1”^
```
```
12 Código Participante
Originador
```
```
M 0RRRRTTT 8 84 - 91 Código del^ Participante
Originador
```
```
Código del Participante
Originador
```
```
13 Numero de lote M N 7 92 - 98
```
```
Secuencial ascendente
único para cada lote en el
archivo
```
```
Secuencial ascendente
único para cada lote en el
archivo.
14 Reservado N/D Blancos 8 99 - 106 Campo reservado. Campo reservado.
* De acuerdo con el formato, para las transacciones CCD tipo crédito, se aclara que por cada pago realizado por un aportante,
municipio, ente territorial u otra entidad se deberá conformar un lote de transacciones crédito con destino a las Administradoras.
```

## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

```
Registro de Control de Lote para todos los Servicios
Para transacciones de: Prenotificación Débito y Crédito, Monetarias Débito y Crédito
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción
1 Tipo de registro M “8” 1 1 Valor válido para este campo "8".
```
```
2 Código clase de
transacciones por lote
```
M N 3 2 - (^4) Código de acuerdo con la Tabla No. 8

###### 3

```
Número de
transacciones detalladas
y de registros adenda
```
```
M N 6 5 - 10 Número de registros de detalle y de adenda en el
lote.
```
```
4 Totales de control M N 10 11 - 20
```
```
Sumatoria de los Códigos de los Participantes
Receptores de los Registros de Detalle de
Transacciones.
```
```
5 Valor total de débitos M $$$$$$$$$$$$$$$$cc 18 21 - 38
```
```
Suma de los valores de las transacciones débito
del lote.
```
```
6 Valor total de créditos M $$$$$$$$$$$$$$$$cc 18 39 - 56
```
```
Suma de los valores de las transacciones crédito
del lote.
7 Identificación del
Originador
```
```
R AN 10 57 - 66 Número de identificación del Originador.
```
###### 8

```
Código de autenticación
de mensajes O^ AN^19 67 -^85
```
```
Campo reservado para un algoritmo de
seguridad.
9 Reservado N/D Blancos 6 86 - 91 Campo reservado no disponible.
```
```
10
```
```
Identificación del
Participante Originador M^ 0RRRRTTT^8 92 -^99
```
```
Código del Participante Originador que inicia la
transacción.
11 Número del lote M N 7 100 - 106 Número del Lote.
```

## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

```
Registro de Detalle de Transacciones – Servicios PPD y CCD
Para transacciones de: Prenotificación Débito y Crédito y Monetarias Débito y Crédito
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción
1 Tipo de registro M “6” 1 1 Valor válido para este campo 6
2 Código de transacción M N 2 2 - 3 Código de transacción de acuerdo a la Tabla No.^6.
```
```
3 Código Participante^
Receptor
```
```
M 0RRRRTTT 8 4 - 11 Número de Ruta y Tránsito del Participante Receptor
```
```
4 Digito de chequeo M N 1 12 - 12
```
```
En este campo debe ir el dígito de chequeo del campo
3
```
```
5 Número de cuenta del
Receptor
```
```
R AN 17 13 - 29 Número de cuenta que del Receptor tiene con el
Participante Receptor
```
```
6 Valor de la transacción M $$$$$$$$$$
$$$$$$cc
```
###### 18 30 - 47

```
Tipo de Transacción Valor
Prenotificación Débito / Crédito Cero ($0 pesos)
Transacción Monetaria Débito /
Transacción Monetaria Crédito
```
```
Valor para recaudar
/ pagar
```
```
7
```
```
Número de identificación
del Receptor O^ AN^15 48 -^62
```
```
Campo (^2 ) utilizado por el Originador para identificar al
Receptor 1
8 Nombre del Receptor R AN 22 63 - 84 Registra el nombre del Receptor (^2 ).
```
```
9 Datos discrecionales O AN 2 85 - 86
```
```
Tipo de Transacción
```
```
Si se requiere que se
valide la
identificación
Receptor, este
campo debe
contener
Prenotificación Débito /
Transacción Monetaria Débito
```
```
No requiere un
valor particular.
Prenotificación Crédito /
Transacción Monetaria Crédito
```
```
“V” o “v”
```
###### 10

```
Indicador de registro
adenda M^ N^1 87 -^87
```
```
Valor “1” si se requiere anexar información adicional
relacionada con el pago. Valor “0” en caso contrario.
```
```
11 Numero de secuencia^ M^ N^15 88 -^102
```
```
En las primeras 8 posiciones se debe registrar el Código
del Participante Originador y en las siguientes 7
posiciones, un consecutivo
12 Reservado N/D Blancos 4 103 - 106 Campo reservado
```
(^1) En las transacciones de prenotificación débito y en las transacciones monetarias débito, este campo SIEMPRE deberá contener la identificación

## del Receptor. En aquellas transacciones de prenotificación crédito y en las transacciones monetarias crédito, en las que el Originador requiera

```
validar la identificación del Receptor, este campo deberá contener la identificación del Receptor.
( 2 ) Campo de obligatorio diligenciamiento para los adquirentes, en los cuales deben indicar el nombre y el número de identificación completos
del cliente receptor.
```

## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

```
Registro de Detalle de Transacciones –Servicio CTX
Para Transacciones Monetarias Crédito
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción
1 Tipo de registro M “6” 1 1 Valor válido para este campo "6".
```
2 Código de transacción M N 2 2 - (^3) Código de transacción de acuerdo a la Tabla No. 6
3 Código^ Participante
Receptor
M 0RRRRTTT 8 4 - 11 Número de ruta y tránsito del Participante Receptor
4 Digito de chequeo M N 1 12 - 12 En este campo debe ir el dígito de chequeo del campo 3.
5 Número^ de cuenta del
Receptor
R AN 17 13 - 29 Número de cuenta que el Receptor tiene con el
Participante Receptor
6 Valor de la transacción M $$$$$$$$$$
$$$$$$cc

###### 18 30 - 47

```
Tipo de Transacción Valor
Transacción
Monetaria Crédito
```
```
Valor por pagar
```
###### 7

```
Número de
identificación del
Receptor
```
```
O AN 15 48 - 62 Campo utilizado por el Originador para identificar al
Receptor^1
```
```
8 Numero de registros de
adenda
```
```
R N 4 63 - 66 Número de registros de adenda del registro de detalle de
transacciones.
9 Nombre del Receptor R AN 16 67 - 82 Registra el nombre del Receptor.
10 Reservado R AN 2 83 - 84 Campo reservado
```
```
11 Datos discrecionales O AN 2 85 - 86
```
```
Tipo de Transacción
```
```
Si se requiere que se valide la
identificación del Receptor, este
campo debe contener...
Transacción
Monetaria Crédito
```
```
“V” o “v”
```
###### 12

```
Indicador de registro
adenda M^ N^1 87 -^87
```
```
Valor “1” si se requiere anexar información adicional
relacionada con el pago. Valor “0” en caso contrario.
```
```
13 Numero de secuencia M N 15 88 - 102
```
```
En las primeras 8 posiciones se debe registrar el Código
del Participante Originador y en las siguientes 7
posiciones, un consecutivo.
14 Reservado N/D Blancos 4 103 - 106 Campo reservado.
```
(^1) En las transacciones de prenotificación débito y en las transacciones monetarias débito, este campo SIEMPRE deberá contener la

## identificación del Receptor. En aquellas transacciones de prenotificación crédito y en las transacciones monetarias crédito, en las que el

```
Originador requiera validar la identificación del Receptor, este campo deberá contener la identificación del Receptor
```
```
Registro Adenda – Información Adicional – Servicio PPD
Para transacciones de: Prenotificación Débito y Crédito y Monetarias Débito y Crédito
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción
1 Tipo de registro M “7” 1 1 Valor válido para este campo "7".
2 Código tipo de registro adenda M 2 2 - 3 NValor válido para este campo “05”.
```
```
3
```
```
Información relacionada con el
pago R^ AN^80  4 -^83
```
```
Campo para colocar información relacionada con
el pago.
```
```
4 Numero de secuencia de registro
adenda
```
```
M N 4 84 - 87 Valor válido para este campo “0001”.
```
###### 5

```
Numero de secuencia de
Transacción del registro de
detalle de transacciones
```
###### M N 7 88 - 94

```
Su valor debe coincidir con las siete últimas
posiciones del campo 11, registro tipo “6”, al cual
hace referencia.
```
```
6 Reservado N/D Blancos 12 95 - 106 Campo reservado. Este campo debe ir en blancos.
```

## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

## El Registro Adenda para las transacciones monetarias tipo crédito del servicio PPD es de uso obligatorio y

## se debe usar para informar el Código Único de Referencia del crédito en el campo 3 “Información

## Relacionada con el Pago”. El contenido de este campo es el siguiente:

```
Información Relacionada con el Pago – Servicio PPD
Para transacciones Monetarias Crédito
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción
```
```
3 Información relacionada con el
pago *
```
```
R * AN 80 4 - 83 Campo para ingresar información^ relacionada con el
pago.
```
```
3.1. Identificación del Originador R N “15” 4 - 18
```
```
Cédula o NIT del Originador que realiza el pago o
traslado de fondos. Este campo no debe estar vacío ni
contener ceros.
3.2. Reservado N/D Blancos “2” 19 - 20 Campo reservado. Este campo debe ir en blancos.
```
```
3.3. Propósito de la transacción R AN “10” 21 - 30
```
```
Preferiblemente incluir en este campo, la misma
información del Campo 7 – Descripción de Lote del
registro tipo 5. Puede estar diligenciado con ceros.
No es causal de devolución.
```
```
3.4. Referencia de Pago ** R** AN “53” 31 - 83
```
```
Campo destinado para que el Originador describa el
concepto de la transferencia que está realizando, de
acuerdo con los campos que se relacionan a
continuación:
```
```
3.4.1. Número de factura o
cuenta
```
###### R AN “24” 31 - 54

```
Número de la factura, cuenta de cobro, recibo de
pago, referencia de pago electrónico, código numérico
o alfanumérico que identifica al Originador de manera
única ante el receptor u otro que identifique el pago
que el originador está realizando.
Si no existe este número o referencia este campo
puede contener ceros o estar vacío.
3.4.2. Reservado N/D Blancos “2” 55 - 56 Campo reservado. Este campo debe ir en blancos.
```
```
3.4.3. Información libre del
Originador
```
###### R AN “24” 57 - 80

Campo diligenciado libremente por el Originador para
referenciar su pago.
Si no existe información libre este campo puede
contener ceros o estar vacío.
3.4.4. Reservado N/D Blancos “3” 81 - 83 Campo reservado. Este campo debe ir en blancos.
***** El contenido de este campo no es validado por la ACH, sin embargo, es obligatorio su inclusión por parte del Participante Originador.

** El uso de las adendas para las transacciones monetarias tipo crédito del servicio PPD será obligatorio y los datos a incluir en éstas dependerán
de la información que sea suministrada por el Originador al Participante Originador al ordenar el pago. Para el efecto se deberá hacer uso del
campo 3 “Información Relacionada con el Pago” (80 posiciones). Para el reporte obligatorio a los Receptores de la información contenida en la
adenda de las transacciones PPD crédito a través del extracto de cuenta o del mecanismo acordado con éstos, el Participante Receptora deberá
aplicar las siguientes reglas de selección:

− Si el campo 3.4.1. Número de factura o cuenta contiene información diferente a ceros, reporta como mínimo esta información al Receptor.
− Si el campo 3.4.1. Número de factura o cuenta contiene ceros o está vacío, procede con la validación para el campo 3.4.3. Información libre
del Originador; si este campo 3.4.3. contiene información diferente a ceros, se reporta como mínimo esta información al receptor.
− Si los campos 3.4.1. Número de factura o cuenta y 3.4.3. Información libre del originador, contienen ceros o están vacíos, se reporta al
Receptor como mínimo la información contenida en el campo 3.1. Identificación del Originador posición 4-18.


## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

## El Registro Adenda para las transacciones tipo débito del servicio PPD es de uso obligatorio y se debe usar

## para la transacción monetaria y para la transacción de prenotificación para informar el Código Único de

## Referencia del débito en el campo 3 “Información Relacionada con el Pago”. El contenido de este campo es

## el siguiente:

```
Información Relacionada con el Pago – Servicio PPD
Para transacciones de Prenotificación y Monetarias Débito
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción
```
```
3
```
```
Información relacionada con el
pago R *^ AN^80  4 -^83
```
```
Campo para ingresar información relacionada con
el pago.
```
- Código EAN 13 o NIT R N “13” 4 - 16

```
Código EAN o NIT del facturador. Campo
recomendado para ser validado por el Participante
Receptor entre la prenotificación y la transacción
débito.
```
- Código de Servicio R AN “30” 17 - 46

```
Llave tomada del archivo de facturación. Campo
recomendado para ser validado por el Participante
Receptor entre la prenotificación y la transacción
débito.
```
- Descripción del servicio R AN “15” 47 - 61 Detalle del servicio
- Reservado N/D Blancos “22” 62 - 83 Campo reservado. Este campo debe ir en blancos.
***** El contenido de este campo no es validado por la ACH, sin embargo, es obligatoria su inclusión por parte del Participante
Originador.

```
Registro Adenda – Información Adicional – Servicio CCD
Para transacciones de: Prenotificación Débito y Crédito y Monetarias Débito y Crédito
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción
1 Tipo de registro M “7” 1 1 Valor válido para este campo "7".
```
```
2 Código tipo de registro
adenda
```
```
M 2 2 - 3 Valor válido para este campo “05”.N
```
###### 3

```
Información relacionada con
el pago R^ AN^80  4 -^83
```
```
Campo para colocar información relacionada con el
pago.
```
```
4 Numero de secuencia de registro adenda M N 4 84 - 87 Valor válido para este campo “0001”.
```
###### 5

```
Numero de secuencia de
transacción del registro de
detalle de transacciones
```
###### M N 7 88 - 94

```
Su valor debe coincidir con las siete últimas
posiciones del campo 11, registro tipo “6”, al cual
hace referencia.
6 Reservado N/D Blancos 12 95 - 106 Campo reservado. Este campo debe ir en blancos.
```
```
El Registro Adenda es de uso obligatorio para las transacciones tipo crédito y débito del servicio CCD y se debe usar para la
transacción monetaria o para la transacción de prenotificación.
```
## Las transacciones crédito monetarias del servicio CCD se utilizarán para efectuar los abonos de los pagos a

## las Administradoras del Sistema de Seguridad Social (SSS) y/o a otros entes relacionados. El contenido del

## campo 3 de la adenda es el siguiente:


## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

```
Información Relacionada con el Pago –Sistema de Seguridad Social SSS
Para transacciones de Prenotificación y Monetarias Crédito – Servicio CCD
```
```
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción para pagos
Seguridad Social SSS
```
```
Descripción para pagos
Seguridad Social Régimen
Subsidiado PRS
```
###### 3

```
Información relacionada
con el pago R *^ AN^80  4 -^83
```
```
Campo para ingresar
información relacionada
con el pago.
```
```
Campo para ingresar
información relacionada con
el pago.
```
-^ Código del Operador
    de Información

###### R N “2” 4 - 5

```
Código asignado por el
Ministerio de la
Protección Social
```
```
Código asignado por el
Ministerio de la Protección
Social
```
- Número de la planilla
    de liquidación R^ AN^ “15”^6 -^20

```
Número de la planilla de
liquidación que está
pagando el aportante.
```
```
Número de declaración
DGAS – Declaración de Giro y
Aceptación de Saldos
```
- Número de registros
    de la planilla R^ N^ “6”^21 -^26

```
Número de registros o
empleados enviados a la
Administradora en la
Planilla de Liquidación.
```
```
Número de registros de la
declaración DGAS –
Declaración de Giro y
Aceptación de Saldos
```
- Código Participante
    Originador R^

###### N

###### 0RRRRTTT “8”^27 -^34

```
Código del Participante a
través del cual el
aportante realizó el
pago, según
corresponda:
```
- Mediante débito a la
    cuenta del aportante.
- Mediante
    consignación en una
    cuenta de recaudo.

```
Código del Participante a
través de la cual el municipio,
ente territorial, EPS u otra
entidad realizo el giro.
```
-^ Código de la
    Administradora

###### R AN “6” 35 - 40

```
Código de la
Administradora
receptora de los pagos,
asignado por el
Ministerio de la
Protección Social
```
```
Código de la Administradora
beneficiaria del pago
asignado por el Ministerio de
la Protección Social. Para las
entidades que no tengan
código utilizar NA (no aplica).
```
- NIT del Aportante R AN “16” 41 - 56 NIT del Aportante

```
NIT del municipio o ente
territorial cuando el giro es
realizado por el ente y NIT de
la EPS cuando el giro lo
realiza una EPS a una IPS o
beneficiario
```
- Período de pago R N^
    AAAAMM

```
“6” 57 - 62 Período de pago de la
planilla.
```
```
Periodo inicial de la
declaración DGAS –
Declaración de Giro y
Aceptación de Saldos
```
```
− Canal de Pago R N “2” 63 - 64
```
```
Canal de pago de la
planilla. Ver Tabla No. 11
```
```
Canal de pago de la
declaración DGAS –
Declaración de Giro y
Aceptación de Pagos
```
- Reservado N/D Blancos “19” 65 - 83

```
Campo reservado. Este
campo debe ir en
blancos.
```
```
Campo reservado. Este
campo debe ir en blancos
```

## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

```
* El contenido de este campo no es validado por la ACH, sin embargo, es obligatoria su inclusión por parte del Participante
Originador.
```
## Las transacciones débito monetarias del servicio CCD se utilizarán para efectuar los débitos a las cuentas

## de los Aportantes y/u otros entes en el esquema de pagos al Sistema de Seguridad Social (SSS). El contenido

## del campo 3 de la adenda es el siguiente:

```
Información Relacionada con el Pago –Sistema de Seguridad Social SSS
Para transacciones de Prenotificación y Monetarias Debito – Servicio CCD
```
```
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción para pagos
Seguridad Social SSS
```
```
Descripción para pagos
Seguridad Social Régimen
Subsidiado PRS
```
###### 3

```
Información
relacionada con el pago R *^ AN^80  4 -^83
```
```
Campo para ingresar
información relacionada con el
pago.
```
```
Campo para ingresar
información relacionada con
el pago.
```
- Código EAN 13 o
    NIT

###### R N “13” 4 - 16

```
Código EAN o NIT del aportante.
Campo recomendado para ser
validado por el Participante
Receptor entre la
prenotificación y la transacción
débito.
```
```
NIT del municipio o ente
territorial cuando el giro es
realizado por el ente y NIT de
la EPS cuando el giro lo realiza
una EPS a una IPS o
beneficiario.
Campo recomendado para
ser validado por el
Participante Receptor entre
la prenotificación y la
transacción débito
```
(^) - Código de Servicio R AN “30” 17 - 46
Corresponde al código (longitud
2) y nombre del operador de
información (longitud 28) que
tramita la transacción débito.
Campo recomendado para ser
validado por el Participante
Receptor entre la
prenotificación y la transacción
débito.
Corresponde al código
(longitud 2) y nombre del
operador de información
(longitud 28) que tramita la
transacción débito. Campo
recomendado para ser
validado por el Participante
Receptor entre la
prenotificación y la
transacción débito.

- Descripción del
    servicio

###### R AN “15” 47 - 61

```
Número de la planilla que se
está pagando cuando es la
transacción débito monetaria.
Cuando se trate de una
prenotificación dado que no se
conoce el número de la planilla
este campo debe ir relleno con
ceros (0).
```
```
Número de declaración DGAS
```
- Declaración de Giro y
Aceptación de Saldos.
Cuando se trate de una
prenotificación dado que no
se conoce el número de la
declaración este campo debe
ir relleno con ceros (0).

(^) - Reservado N/D Blancos “22” 62 - 83
Campo reservado. Este campo
debe ir en blancos.
Campo reservado. Este
campo debe ir en blancos.
***** El contenido de este campo no es validado por la ACH, sin embargo, es obligatoria su inclusión por parte del Participante
Originador.


## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

```
Registro Adenda – Información Adicional –Servicio CTX
Para Transacciones Monetarias Crédito
```
```
# Nombre de Campo Inclusión Contenido
```
```
Longitu
d Posición^ Descripción^
1 Tipo de registro M “7” 1 1 Valor válido para este campo "7".
```
```
2
```
```
Código tipo de registro
adenda M^2 2 -^3 1.1.1.1.1.Valor válido para este campo “05”.^ N^
```
```
3 Información relacionada con
el pago
```
```
R AN 80 4 - 83 Campo para colocar información relacionada
con el pago.
```
```
4 Numero de secuencia de
registro adenda
```
###### M N 4 84 - 87

```
Valor válido para este campo iniciando en
“0001”, con numeración consecutiva
ascendente
```
###### 5

```
Numero de secuencia de
transacción del registro de
detalle de transacciones
```
###### M N 7 88 - 94

```
Su valor debe coincidir con las siete últimas
posiciones del campo 13, registro tipo “6”, al
cual hace referencia.
```
```
6 Reservado N/D Blancos 12 95 - 106
```
```
Campo reservado. Este campo debe ir en
blancos.
El uso de las adendas para las transacciones crédito del servicio CTX es obligatorio y los datos a incluir dependerán de la
información que sea necesario reportar al Participante Receptor y/o Receptor, haciendo uso del campo 3 “Información
Relacionada con el Pago” (80 posiciones). Se podrán utilizar tantas adendas como sea necesario hasta 9.999. Para las
transacciones de prenotificación tipo crédito se debe usar solo una adenda.
```
## En el caso de las transacciones crédito relacionadas con los pagos de la Dirección General de Crédito Público

## y del Tesoro Nacional, el contenido del campo 3 es el siguiente:

```
Información Relacionada con el Pago – Servicio CTX- Transacciones de la DGCPTN
Para transacciones Monetarias Crédito
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción
```
```
3 Información relacionada con el
pago
```
```
R * AN 80 4 - 83 Campo para ingresar información relacionada con
el pago.
```
- Código EAN 13 o NIT R AN “13” 4 - 16 Código EAN o NIT del ente pagador.
- Descripción del servicio R AN “15” 17 - 31 Detalle del servicio
-^ Número de referencia de la
    factura

```
R N “20” 32 - 51 Número asignado por el facturador como referencia
para realizar el pago
```
- Valor factura R N “18” 52 - 69 Valor que se está pagando por la respectiva factura
- Reservado N/D Blancos “14” 70 - 83 Campo reservado. Este campo debe ir en blancos.

***** El contenido de este campo no es validado por la ACH, sin embargo, es obligatoria su inclusión por parte del Participante
Originador.

Se deberán relacionar tantas adendas, como facturas o pagos se estén realizando consolidadamente a un determinado facturador.

Con base en procedimiento definido por los bancos en febrero de 2007, se establece el uso obligatorio del formato estándar
Asobancaria 2001 o el que lo sustituya, para el reporte a los receptores de la información contenida en las adendas de las
transacciones CTX.


## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

## 7.1.2. TRANSACCIONES DE DEVOLUCIÓN DE UNA DEVOLUCIÓN

## Una Devolución de una Devolución puede ser originada por un Participante con destino al Participante

## Receptor para informar que una devolución enviada por el Participante Receptor no fue aceptada por razones

## específicas.

## El Sistema CENIT validará la coincidencia entre las transacciones de Devolución de una Devolución y las

## transacciones de Devolución originadas en el día de compensación abierto o el día inmediatamente anterior.

## Se rechazarán aquellas Devoluciones de Devoluciones que no cumplan con esta condición. La validación se

## realiza sobre los siguientes campos:

## • Participante Originador

## • Participante Receptor

## • Número de Secuencia de la Transacción Original

## • Valor de la Transacción

## • Fecha Efectiva de la Transacción Original

```
Registro de Encabezado de Lote – Servicio PPD Para transacciones de: Devolución de una Devolución
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción
1 Tipo de registro M “5” 1 1 Valor válido para este campo "5”.
```
```
2
```
```
Código clase de transacciones
por lote M^ N^3 2 -^4 Código de acuerdo con la^ Tabla N
```
```
o. 8
```
(^3) Nombre del Originador M AN 16 5 - 20 Nombre dede devolución.l^ Originador de la transacción original
4 Datos discrecionales del Originador O AN 20 21 - 40
Datos del Originador y/o del Participante
Originador de la transacción original de
devolución.
5 Identificación del Originador R AN 10 41 - 50
Número de identificación del Originador de la
transacción original de devolución.
6 Tipo de servicio M AN 3 51 - 53 PPD
7 Descripción de lote M AN 10 54 - 63
Descripción del Lote de la transacción de
devolución original.
8 Fecha descriptiva O AAAAMMDD 8 64 - 71 Fecha de carácter informativo de la transacción
original de devolución.
(^9) Fecha efectiva de la transacción M AAAAMMDD 8 72 - 79
Fecha en la cual se deben aplicar las
transacciones de devolución de devolución
contenidas en el lote.
(^10) Fecha de compensación juliana M N 3 80 - 82 Fecha de compensación o liquidación de las
transacciones.
11 Código estado del Originador M AN 1 83 - 83
El valor válido es “1” e indica el estado del
Originador.
12 Código Participante Originador M 0RRRRTTT 8 84 - 91 Código del^ Participante Originador de la
transacción de devolución.
13 Numero de lote M N 7 92 - 98
Secuencial ascendente único para cada lote en el
archivo.
14 Reservado N/D Blancos 8 99 - 106 Campo reservado.


## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

```
Registro de Control de Lote – Servicio PPD
Para transacciones de: Devolución de una Devolución
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción
1 Tipo de registro M “8” 1 1 Valor válido para este campo "8".
```
```
2 Código clase de transacciones por
lote
```
M N 3 2 - (^4) Código de acuerdo con la Tabla No. 8

###### 3

```
Número de transacciones
detalladas y de registros adenda M^ N^6 5 -^10
```
```
Número de registros de detalle y de adenda en el
lote.
```
```
4 Totales de control M N 10 11 - 20
```
```
Sumatoria de los Códigos de los Participantes
Receptores de los Registros de Detalle de
Transacciones.
```
```
5 Valor total de débitos M
```
###### $$$$$$$$$$$

```
$$$$$cc 18 21 -^38
```
```
Suma de los valores de las transacciones débito del
lote.
```
```
6 Valor total de créditos M $$$$$$$$$$$
$$$$$cc
```
```
18 39 - 56 Suma de los valores de las transacciones crédito del
lote.
7 Identificación del Originador R AN 10 57 - 66 Número de identificación del Originador.
```
```
8 Código de autenticación de mensajes O AN 19 67 - 85 Campo reservado para un algoritmo de seguridad.
```
```
9 Reservado N/D Blancos 6 86 - 91 Campo reservado no disponible.
```
```
10 Identificación del Participante
Originador
```
```
M 0RRRRTTT 8 92 - 99 Código del^ Participante Originador^ que inicia la
transacción.
11 Número del lote M N 7 100 - 106 Número del Lote.
```
```
Registro de Detalle de Transacciones – Servicio PPD
Para transacciones de: Devolución de una Devolución
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción
1 Tipo de registro M “6” 1 1 Valor válido para este campo "6".
```
```
2 Código de transacción M N 2 2 - 3
```
```
Código de transacción contenido en la devolución
original.
```
(^3) Código Participante Receptor M 0RRRRTTT 8 4 - 11 Código del Participante Originador de la devolución.
4 Digito de chequeo M N 1 12 - 12 En este campo debe ir el dígito de chequeo del
campo 3.
5
Número de cuenta del
Receptor R^ AN^17 13 -^29
Número de cuenta contenido en la transacción de
devolución.
6 Valor de la transacción M $$$$$$$$$$$
$$$$$cc
18 30 - 47 Valor contenido en la transacción de devolución.
7 Número^ de identificación del
Receptor
O AN 15 48 - 62 Identificación Receptor^ de la transacción de
devolución.
8 Nombre del Receptor R AN 22 63 - 84 Nombre devolución.del Receptor de la transacción de
9 Datos discrecionales O AN 2 85 - 86 Datos contenidos en la transacción de devolución.
10 Indicador de registro adenda M N 1 87 - 87 Valor “1” para anexar información de la devolución
de la devolución.
11 Numero de secuencia M N 15 88 - 102
En las primeras 8 posiciones se debe registrar el
Código del Participante Originador de la devolución
de la devolución y en las siguientes 7 posiciones, un
consecutivo.


## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

```
Registro de Detalle de Transacciones – Servicio PPD
Para transacciones de: Devolución de una Devolución
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción
12 Reservado N/D Blancos 4 103 - 106 Campo reservado.
```
```
Registro Adenda – Devolución de una Devolución – Servicio PPD
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción
1 Tipo de registro M “7” 1 1 Valor válido para este campo "7".
2 Código tipo de registro adenda M ”99” 2 2 - 3 Valor válido para este campo “99”.
```
```
3 Causal de devolución de una
devolución
```
M AN 3 4 - (^6) Causal de devolución de una devolución (1)

###### 4

```
Numero de secuencia de la
transacción original M^ N^15 7 -^21
```
```
Número de secuencia de la transacción
original
5 Reservado N/D Blancos 8 22 - 29 Campo reservado no disponible.
```
```
6
```
```
Código del Participante Receptor
de la transacción original M^ 0RRRRTTT^8 30 -^37
```
```
Código del Participante Receptor de la
transacción original.
7 Reservado N/D Blancos 3 38 - 40 Campo reservado no disponible.
```
```
8
```
```
Numero de secuencia de la
devolución M^ N^15 41 -^55 Número de Secuencia de la Devolución.^
```
```
9 Fecha de compensación de la
devolución
```
```
M N 3 56 - 58 Fecha de Compensación de la Devolución.
```
```
10 Causal de devolución R AN 2 59 - 60 Registra la causal de devolución original.
11 Reservado N/D Blancos 21 61 - 81 Campo reservado no disponible.
```
```
12 Numero de secuencia M N 15 82 - 96
```
```
Número de secuencia del registro adenda
asociado con el Registro de Detalle de
Transacciones. Asignado por el Participante
que genera la devolución de una devolución.
```
```
13 Reservado N/D Blancos 10 97 - 106 Campo reservado no disponible.
(1) Ver Tabla No. 1 - Causales de Devolución para los Servicios PPD, CCD y CTX del Anexo No. 2 del Manual Operativo Sistema
```
```
de Compensación Electrónica Nacional Interbancaria CENIT.
```
## 7.2. TRANSACCIONES GENERADAS POR UN PARTICIPANTE EN SU CARÁCTER DE PARTICIPANTE

## RECEPTOR

Un Participante puede iniciar archivos de transacciones de devolución de Prenotificaciones Débito y Crédito y de

Monetarias Débito y Crédito hacia un Participante Originador, de acuerdo con el siguiente formato:

```
Registro de Encabezado de Archivo para todos los Servicios
Para transacciones de Devolución de: Prenotificación Débito y Crédito, Monetarias Débito y Crédito
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción
1 Tipo de registro M “1” 1 1 Valor válido para este campo "1".
2 Código de prioridad R N 2 2 - 3 Valor válido “01”.
```
```
3 Código entidad destino
inmediato
```
```
M b0RRRRTTTC 10 4 - 13 Código del ACH.
```
###### 4

```
Código entidad origen
inmediato M^ b0RRRRTTTC^10 14 -^23
```
```
Código del Participante Originador que envía el
archivo.
```

## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

```
Registro de Encabezado de Archivo para todos los Servicios
Para transacciones de Devolución de: Prenotificación Débito y Crédito, Monetarias Débito y Crédito
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción
5 Fecha de creación del archivo M AAAAMMDD 8 24 - 31 Fecha de creación del archivo.
```
```
6 Hora de creación del archivo O HHMM 4 32 - 35
```
```
Hora en la cual es transmitido o creado el
archivo.
```
```
7 Identificador del archivo M A-Z / 0- 9 1 36 - 36 Identificación de archivos creados en la misma
fecha (máximo 999 /día)
```
```
8 Tamaño del registro M ‘106’ 3 37 - 39
```
```
Indica el número de caracteres contenidos en
cada registro.
```
```
9 Factor de ablocamiento M ‘10’ 2 40 - 41 Define el número de registros dentro de un
bloque.
10 Código de formato M ‘1’ 1 42 - 42 Permite futuras variaciones de formato.
```
```
11 Nombre entidad destino
inmediato
```
```
O AN 23 43 - 65 Nombre de la ACH.
```
###### 12

```
Nombre entidad origen
inmediato O^ AN^23 66 -^88 Nombre del Participante Originador.^
13 Código de referencia M AN 8 89 - 96 Identifica el código del sistema.
```
```
14 Reservado N/D Blancos 10 97 - 106
```
```
Campo reservado. Este campo debe ir en
blancos.
```
```
Registro de Control de Archivo para todos los Servicios
Para transacciones de Devolución de: Prenotificación Débito y Crédito, Monetarias Débito y Crédito
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción
1 Tipo de registro M “9” 1 1 Valor válido para este campo "9".
2 Cantidad de lotes M N 6 2 - 7 Número de lotes incluidos en el archivo.
```
```
3 Numero de bloques M N 6 8 - 13
```
```
Número de bloques físicos en el archivo de
10 registros cada uno.
4 Número de transacciones
detalladas y de registros adenda
```
```
M N 8 14 - 21 Número total de registros de detalle y de
adenda en el archivo.
```
```
5 Totales de control M N 10 22 - 31
```
```
Sumatoria de los códigos del Participante
Receptor de los Registros de Detalle de
Transacciones.
```
```
6 Valor total de débitos M $$$$$$$$$$$$$$$
$CC
```
```
18 32 - 49 Suma de los valores de las transacciones
tipo débito del archivo.
```
```
7 Valor total de créditos M
```
###### $$$$$$$$$$$$$$$

###### $CC 18 50 -^67

```
Suma de los valores de las transacciones
tipo crédito del archivo.
8 Reservado N/D Blancos 39 68 - 106 Campo reservado no disponible.
```

## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

## 7.2.1. GENERACIÓN DE TRANSACCIONES DE DEVOLUCIÓN DE PRENOTIFICACIONES DÉBITO Y CRÉDITO,

## MONETARIAS DÉBITO Y CRÉDITO

## Una Devolución es creada por el Participante Receptor para notificar al Participante Originador que no pudo

## aceptar una transacción enviada por alguna razón específica. Solamente se puede iniciar una devolución por

## cada transacción recibida y se debe considerar como una transacción nueva. Los Registros Adenda

## originalmente recibidos, no son devueltos. El Sistema CENIT verifica la coincidencia entre las transacciones

## de devolución y las transacciones monetarias originadas durante el respectivo día de compensación, de forma

## tal que aquellas devoluciones que no correspondan a transacciones del día, enviadas previamente a la

## cámara, serán rechazadas. La validación se realiza sobre los siguientes campos: Participante Originador y

## Participante Receptor, Número de Secuencia de la Transacción Original, Valor de la Transacción y Fecha

## efectiva de la Transacción Original.

```
Registro de Encabezado de Lote para todos los Servicios
Para transacciones de Devolución de: Prenotificación Débito y Crédito, Monetarias Débito y Crédito
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción
1 Tipo de registro M “5” 1 1 Valor válido para este campo "5”.
```
```
2 Código clase de transacciones
por lote
```
```
M N 3 2 - 4 Código de acuerdo con la Tabla No. 8
```
```
3 Nombre del Originador M AN 16 5 - 20
```
```
Nombre del Originador de la transacción
original.
```
```
4 Datos discrecionales del
Originador
```
```
O AN 20 21 - 40 Datos Discrecionales de la transacción original.
```
```
5 Identificación del Originador R AN 10 41 - 50
```
```
Identificación del Originador contenida en la
transacción original
```
```
6 Tipo de servicio M AN 3 51 - 53 De acuerdo con la Tabla N
```
```
o. 5 (PPD, CCD, CTX,
etc.)
7 Descripción de lote M AN 10 54 - 63 Descripción del lote según la Tabla No. 4
8 Fecha descriptiva O AAAAMMDD 8 64 - 71 Fecha informativa de la transacción original.
```
```
9
```
```
Fecha efectiva de la
transacción M^ AAAAMMDD^8 72 -^79
```
```
Fecha en la cual se deben aplicar las
devoluciones del lote.
```
```
10 Fecha de compensación
juliana
```
```
M N 3 80 - 82 Fecha de compensación o liquidación de las
transacciones.
```
```
11 Código estado del Originador M AN 1 83 - 83
```
```
El valor válido es “1” e indica el estado del
Originador.
```
```
12 Código Participante Originador M 0RRRRTTT 8 84 - 91 Código del Participante^ que genera la
devolución,
```
```
13 Numero de lote M N 7 92 - 98
```
```
Secuencial ascendente único para cada lote en
el archivo.
14 Reservado N/D Blancos 8 99 - 106 Campo reservado.
```

## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

```
Registro de Control de Lote para todos los Servicios
Para transacciones de Devolución de: Prenotificación Débito y Crédito, Monetarias Débito y Crédito
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción
1 Tipo de registro M “8” 1 1 Valor válido para este campo "8".
```
```
2 Código clase de transacciones por
lote
```
```
M N 3 2 - 4 Código de acuerdo con la Tabla No. 8
```
###### 3

```
Número de transacciones detalladas
y de registros adenda M^ N^6 5 -^10
```
```
Número de registros de detalle y de adenda
en el lote.
4 Totales de control M N 10 11 - 20 Suma de Códigos del Participante Receptor.
```
```
5 Valor total de débitos M
```
###### $$$$$$$$$

```
$$$$$$$cc 18 21 -^38
```
```
Suma de los valores de las transacciones
débito del lote.
```
```
6 Valor total de créditos M $$$$$$$$$
$$$$$$$cc
```
```
18 39 - 56 Suma de los valores de las transacciones
crédito del lote.
7 Identificación del Originador R AN 10 57 - 66 Número de identificación del Originador.
```
```
8 Código de autenticación de
mensajes
```
```
O AN 19 67 - 85 Campo reservado para un algoritmo de
seguridad.
9 Reservado N/D Blancos 6 86 - 91 Campo reservado no disponible.
```
```
10 Identificación^ del Participante
Originador
```
```
M 0RRRRTTT 8 92 - 99 Código del Participante^ que genera la
devolución.
11 Número del lote M N 7 100 - 106 Nuevo Número del Lote.
```

## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

```
Registro de Detalle de Transacciones para los Servicios PPD y CCD
Para transacciones de Devolución de: Prenotificación Débito y Crédito, Monetarias débito y Crédito
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción
1 Tipo de registro M “6” 1 1 Valor válido para este campo "6".
2 Código de transacción M N 2 2 - 3 Código de acuerdo con la Tabla No. 6
```
```
3 Código Participante
Receptor
```
###### M 0RRRRTTT 8 4 - 11

```
Código del Participante Receptor de la devolución,
es decir el Código del Participante Originador de la
transacción original.
```
```
4 Digito de chequeo M N 1 12 - 12
```
```
En este campo debe ir el dígito de chequeo del
campo 3.
5 Número de cuenta
Receptor
```
```
R AN 17 13 - 29 Número de cuenta de la transacción original.
```
```
6 Valor de la transacción M $$$$$$$$$$$$$$$cc 18 30 - 47
```
```
Tipo de Transacción Valor
Devolución de Prenotificación Débito
Devolución de Prenotificación
Crédito
```
```
Cero pesos
($0)
```
```
Devolución de Transacción Débito
Devolución de Transacción Crédito
```
```
Valor por
recaudar /
pago de la
transacción
original.
```
```
7
```
```
Número de identificación
Receptor O^ AN^15 48 -^62 Identificación Receptor^ de la transacción original.^
8 Nombre Receptor R AN 22 63 - 84 Nombre Receptor de la transacción original.
9 Datos discrecionales O AN 2 85 - 86 Datos discrecionales de la transacción original.
```
```
10 Indicador de registro adenda M N 1 87 - 87 Valor “1” para anexar información de la devolución.
```
```
11 Numero de secuencia M N 15 88 - 102
```
```
En las primeras 8 posiciones se debe registrar el
Código del Participante que genera la devolución y en
las siguientes 7 posiciones, un nuevo número
consecutivo.
12 Reservado N/D Blancos 4 103 - 106 Campo reservado.
```

## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

```
Registro de Detalle de Transacciones para el Servicio CTX
Para transacciones de Devolución de: Prenotificación y Monetarias Crédito
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción
1 Tipo de registro M “6” 1 1 Valor válido para este campo "6".
2 Código de transacción M N 2 2 - 3 Código de acuerdo con la Tabla No. 6
```
(^3) Código Participante Receptor M 0RRRRTTT 8 4 - 11
Código del Participante Receptor de la
devolución, es decir el Código del Participante
Originador de la transacción original.
4 Digito de chequeo M N 1 12 - 12
En este campo debe ir el dígito de chequeo del
campo 3.
5 Número de cuenta Receptor R AN 17 13 - 29 Número de cuenta de la transacción original.
6 Valor de la transacción M $$$$$$$$$$$$
$$$cc

###### 18 30 - 47

```
Tipo de Transacción Valor
Devolución de
Prenotificación Débito
Devolución de
Prenotificación Crédito
```
```
Cero pesos ($0)
```
```
Devolución de
Transacción Débito
Devolución de
Transacción Crédito
```
```
Valor por recaudar /
pago de la
transacción original.
```
```
7 Número de identificación Receptor O AN 15 48 - 62 Identificación Receptor de la transacción original.
```
```
8 Numero de registros de
adenda
```
```
R N 4 63 - 66 Número de registro de adenda del registro de
detalle de transacciones
9 Nombre Receptor R AN 18 67 - 84 Nombre Receptor de la transacción original.
10 Datos discrecionales O AN 2 85 - 86 Datos Discrecionales de la transacción original.
```
```
11 Indicador de registro adenda M N 1 87 - 87
```
```
Valor “1” para anexar información de la
devolución.
```
```
12 Numero de secuencia M N 15 88 - 102
```
```
En las primeras 8 posiciones se debe registrar el
Código del Participante que genera la devolución
y en las siguientes 7 posiciones, un nuevo número
consecutivo.
13 Reservado N/D Blancos 4 103 - 106 Campo reservado.
```

## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

```
Registro Adenda – Para transacciones de Devolución de:
Prenotificación Débito y Crédito, Monetarias Débito y Crédito para todos los Servicios
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción
1 Tipo de registro M “7” 1 1 Valor válido para este campo "7".
2 Código tipo de registro adenda M “99” 2 2 - 3 Valor válido “99”.
```
```
3 Causal de devolución M AN 3 4 - 6
```
```
Causal de devolución según la Tabla de Causales
de Devolución (1).
```
###### 4

```
Numero de secuencia de la
transacción original M^ N^15 7 -^21
```
```
Número de secuencia de la transacción original
que se está devolviendo. Debe coincidir con la
información contenida en el campo 11 del
Registro de Detalle de Transacciones de la
transacción original.
```
```
5 Fecha de muerte O AAAAMMDD 8 22 - 29
```
```
Fecha de fallecimiento del titular o beneficiario
de la cuenta.
```
###### 6

```
Código Participante Receptor de
la transacción original M^ 0RRRRTTT^8 30 -^37
```
```
Registra el Código del Participante Receptor de
la transacción original (campo 3, registro tipo “6”
de la transacción original).
```
```
7 Información adicional O AN 44 38 - 81 Descripción estándar de la causal con el mayor
detalle posible.
```
###### 8

```
Numero de secuencia del
registro adenda M^ N^15 82 -^96
```
```
Número de secuencia del Registro Adenda que
está asociado con el Registro de Detalle de
Transacciones. Asignado por el Participante
Receptor que genera la devolución.
9 Reservado N/D Blancos 10 97 - 106 Campo reservado no disponible.
(1)Ver Tabla No. 1 - Causales de Devolución para los Servicios PPD, CCD y CTX del Anexo No. 2 del Manual Operativo Sistema de
```
## Compensación Electrónica Nacional Interbancaria CENIT.


## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

## 7.3. ARCHIVOS GENERADOS POR EL OPERADOR ACH

## El sistema ACH realiza el procesamiento y clasificación del movimiento y genera al cierre de cada ciclo

## archivos que contienen las transacciones para ser aplicadas en el sistema interno del Participante Receptor,

## según el movimiento presentado: prenotificaciones crédito y débito, transacciones monetarias crédito y

## débito, devoluciones y devoluciones de devoluciones. Al cierre de cada día operacional genera un archivo

## de Aviso ADV (Resumen Financiero y Contable).

## Cuando se procesen en el sistema archivos con transacciones del servicio CTX, el Operador ACH publicará

## archivos independientes a los Participantes Receptores, los cuales contendrán exclusivamente este tipo de

## operaciones.

## Los archivos generados por el Operador ACH tienen el siguiente formato:

```
Registro de Encabezado de Archivo para todos los Servicios
Para aplicar transacciones de: Prenotificación Débito y Crédito, Monetarias Débito y Crédito, Devoluciones, Devoluciones de
Devoluciones/ Rechazos por Operador ACH y Avisos ADV
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción
1 Tipo de registro M “1” 1 1 Valor válido para este campo "1".
2 Código de prioridad R N 2 2 - 3 Valor válido “01”.
```
```
3
```
```
Código entidad destino
inmediato M^ B0rrrrtttc^10 4 -^13
```
```
Código del Participante Receptor de las
Transacciones.
```
```
4 Código entidad origen
inmediato
```
```
M B0rrrrtttc 10 14 - 23 Código del ACH.
```
```
5 Fecha de creación del archivo M AAAAMMDD 8 24 - 31 Fecha de creación del archivo.
```
```
6 Hora de creación del archivo O HHMM 4 32 - 35
```
```
Hora en la cual es transmitido o creado el
archivo.
```
```
7 Identificador del archivo M A-Z / 0- 9 1 36 - 36
```
```
Identificación de archivos creados en la
misma fecha (máximo 999 /día)
```
```
8 Tamaño del registro M ‘106’ 3 37 - 39 Indica el número de caracteres contenidos
en cada registro.
```
```
9 Factor de ablocamiento M ‘10’ 2 40 - 41
```
```
Define el número de registros dentro de un
bloque.
10 Código de formato M ‘1’ 1 42 - 42 Permite futuras variaciones de formato.
```
```
11
```
```
Nombre entidad destino
inmediato O^ AN^23 43 -^65 Nombre del Participante Receptor.^
```
```
12 Nombre entidad origen
inmediato
```
```
O AN 23 66 - 88 Nombre del ACH.
```
```
13 Código de referencia R AN 8 89 - 96 Identifica el código del sistema.
```
```
14 Reservado N/D Blancos 10 97 - 106
```
```
Campo reservado. Este campo debe ir en
blancos.
```

## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

## 7.3.1. TRANSACCIONES PARA APLICAR: PRENOTIFICACIÓN DÉBITO Y CRÉDITO, MONETARIAS DÉBITO Y

## CRÉDITO, DEVOLUCIONES Y DEVOLUCIONES DE DEVOLUCIONES

## Los archivos generados por parte del Operador ACH son organizados de igual manera como son recibidas las

## transacciones de los diferentes Participantes de tal manera que se conforma un lote por cada Participante

## Originador independientemente del tipo(s) de transacciones que haya enviado la misma. Se conserva el

## formato de detalle y de adenda según el tipo de transacción que fue originada, de acuerdo con los formatos

## indicados en los numerales 7.1 y 7.2 del presente documento.

## 7.3.2. RECHAZOS POR EL OPERADOR ACH

## Aquellas transacciones que no cumplan con las condiciones definidas son rechazadas por el Operador de ACH

## e informadas de forma inmediata a los Participantes Originadores, mediante archivos de notificación en

## formato XML, cuyo esquema se describe a continuación. El Operador ACH genera tantos archivos de

## notificación de rechazos de transacciones en XML, como archivos sean enviados al sistema.

<?xml version="1.0" encoding="UTF-8"?>

<xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"

xmlns:filenack=" urn:xs:FileNack "

elementFormDefault="qualified"

targetNamespace=" urn:xs:FileNack ">

<xs:element name="FileNack" type="filenack:FileNackType"/>

<xs:complexType name="FileNackType">

<xs:sequence>

<xs:element name="GroupHeader" type="filenack:PaymentInformationNack"/>

```
Registro de Control de Archivo para todos los Servicios
Para aplicar transacciones de: Prenotificación Débito y Crédito, Monetarias Débito y Crédito, Devoluciones, Devoluciones de
Devoluciones, Rechazos por Operador ACH y Avisos ADV
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción
1 Tipo de registro M “9” 1 1 Valor válido para este campo "9".
2 Cantidad de lotes M N 6 2 - 7 Número de lotes incluidos en el archivo.
```
```
3 Número de bloques M N 6 8 - 13 Número de bloques físicos en el archivo de 10
registros cada uno.
```
```
4
```
```
Número de transacciones
detalladas y de registros
adenda
```
```
M N 8 14 - 21 Número total de registros de detalle y de
adenda en el archivo.
```
```
5 Totales de control M N 10 22 - 31
```
```
Sumatoria de los códigos del Participante
Receptor de los Registros de Detalle de
Transacciones.
```
```
6 Valor total de débitos M
```
```
$$$$$$$$$$$$$$$$c
c 18 32 -^49
```
```
Suma de los valores de las transacciones tipo
débito del archivo.
```
```
7 Valor total de créditos M $$$$$$$$$$$$$$$$c c 18 50 - 67 Suma de los valores de las transacciones tipo
crédito del archivo.
8 Reservado N/D Blancos 39 68 - 106 Campo reservado no disponible.
```

## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

<xs:element name="AdditionalRefs" type="filenack:AdditionalReferencesNack"/>

<xs:element name="FileErrorHandling" type="filenack:FileHandlingNack" minOccurs="1"

maxOccurs="unbounded"/>

</xs:sequence>

</xs:complexType>

<xs:complexType name="AdditionalReferencesNack">

<xs:sequence>

<xs:element name="RelatedRef" type="filenack:Max16Text"/>

<xs:element name="OrigSender" type="filenack:Max8Text"/>

</xs:sequence>

</xs:complexType>

<xs:complexType name="PaymentInformationNack">

<xs:sequence>

<xs:element name="GroupId" type="filenack:Max16Text"/>

<xs:element name="Status" type="filenack:Max35Text" minOccurs="0" maxOccurs="1"/>

<xs:element name="IndvItemsTotalNo" type="filenack:Number"/>

<xs:element name="CreationDate" type="filenack:ISODateTime"/>

</xs:sequence>

</xs:complexType>

<xs:simpleType name="Number">

<xs:restriction base="xs:decimal">

<xs:fractionDigits value="0"/>

<xs:totalDigits value="18"/>

</xs:restriction>

</xs:simpleType>

<xs:complexType name="FileHandlingNack">

<xs:sequence>

<xs:element name="AdditionalDesc" type="filenack:Max140Text" minOccurs="0" maxOccurs="1"/>

<xs:element name="Status" type="filenack:Max35Text" minOccurs="0" maxOccurs="1"/>

<xs:element name="BatchNo" type="filenack:Max7Text" minOccurs="0" maxOccurs="1"/>

<xs:element name="TraceNo" type="filenack:Max15Text" minOccurs="0" maxOccurs="1"/>

<xs:element name="ErrorCode" type="filenack: Max50Text "/>

</xs:sequence>

</xs:complexType>

<xs:simpleType name="ISODateTime">

<xs:restriction base="xs:dateTime"/>

</xs:simpleType>

<xs:simpleType name="Max35Text">

<xs:restriction base="xs:string">

<xs:minLength value="1"/>

<xs:maxLength value="35"/>

</xs:restriction>


## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

</xs:simpleType>

<xs:simpleType name="Max16Text">

<xs:restriction base="xs:string">

<xs:minLength value="1"/>

<xs:maxLength value="16"/>

</xs:restriction>

</xs:simpleType>

<xs:simpleType name="Max8Text">

<xs:restriction base="xs:string">

<xs:minLength value="1"/>

<xs:maxLength value="8"/>

</xs:restriction>

</xs:simpleType><xs:simpleType name="Max7Text">

<xs:restriction base="xs:string">

<xs:minLength value="1"/>

<xs:maxLength value="7"/>

</xs:restriction>

</xs:simpleType>

</xs:simpleType><xs:simpleType name="Max50Text">

<xs:restriction base="xs:string">

<xs:minLength value="1"/>

<xs:maxLength value="50"/>

</xs:restriction>

</xs:simpleType>

<xs:simpleType name="Max140Text">

<xs:restriction base="xs:string">

<xs:minLength value="1"/>

<xs:maxLength value="140"/>

</xs:restriction>

</xs:simpleType>

</xs:schema>


## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

### 7.3.3. Transacciones de Aviso ADV (Resumen Financiero y Contable)

## El archivo de avisos se refiere a información concerniente al extracto de un Participante. Cada uno de los

## registros de este extracto refleja las operaciones netas totales efectuadas sobre la cuenta del Participante

## producto de operaciones realizadas en el sistema ACH en un día de operación. Este archivo es publicado a

## los Participantes al cierre de cada día operacional.

```
Registro de Encabezado de Lote para Transacciones de Aviso ADV
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción
1 Tipo de registro M “5” 1 1 Valor válido para este campo "5.
```
```
2
```
```
Código clase de transacciones por
lote M^ ”280”^3 2 -^4 280 -^ Avisos de Contabilidad Automática^
```
(^3) Nombre del Originador M AN 16 5 - 20
Nombre del Originador para propósitos
descriptivos.
4 Datos discrecionales del
Originador
O AN 20 21 - 40 Datos del Originador.
5 Identificación del Originador M AN 10 41 - 50 Número de identificación del Originador.
6 Tipo de servicio M AN 3 51 - 53 ADV.

## 7 Descripción de lote M AN 10 54 - 63 Descripción del lote según la Tabla No. 4

```
8 Fecha descriptiva O AN 8 64 - 71 Fecha de carácter informativo asignada por el
Originador.
```
(^9) Fecha efectiva de la transacción R AAAAMMDD 8 72 - 79
Fecha en la cual se deben aplicar las
transacciones del lote.
(^10) Fecha de compensación juliana O
Insertado por
el Operador
ACH

###### 3 80 - 82

```
Fecha de compensación o liquidación de las
transacciones.
```
```
11 Código estado del Originador M AN 1 83 - 83
```
```
El valor válido es “1” e indica el estado del
Originador.
12 Código Participante Originador M 0RRRRTTT 8 84 - 91 Código del Participante Originador.
```
```
13 Numero de lote M N 7 92 - 98 Secuencial ascendente único para cada lote del
archivo.
14 Reservado N/D Blancos 8 99 - 106 Campo reservado.
```
```
Registro de Control de Lote para Transacciones de Aviso ADV
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción
1 Tipo de registro M “8” 1 1 Valor válido para este campo "8"
```
```
2
```
```
Código clase de transacciones
por lote M^ ”280”^3 2 -^4 280 -^ Avisos de Contabilidad Automática^
```
```
3 Número^ de transacciones
detalladas y de registros adenda
```
```
M N 6 5 - 10 Número de registros de detalle y de adenda en el
lote.
4 Totales de control M N 10 11 - 20 Suma de los Códigos del Participante Receptor.
```
```
5 Valor total de débitos M $$$$$$$$$$$$$$$$  18 21 - 38 Suma de los valores de las transacciones débito
del lote.
```
```
6 Valor total de créditos M
```
```
$$$$$$$$$$$$$$$$
 18 39 -^56
```
```
Suma de los valores de las transacciones crédito
del lote.
```
```
7 Datos del operador ACH O AN 35 57 - 91 Información pertinente al Operador ACH y
definida por él.
```
```
8
```
```
Identificación del Participante
Originador M^ 0RRRRTTT^8 92 -^99 Registra el Código del Participante Originador.^
```

## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

```
Registro de Control de Lote para Transacciones de Aviso ADV
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción
```
###### 9

###### NUMERO DEL LOTE

```
M N 7 100 - 106 Número del Lote.
```
```
Registro de Detalle de Transacciones
Para transacciones de Aviso ADV
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción
1 Tipo de registro M “6” 1 1 Valor válido para este campo "6".
```
## 2 Código de transacción M N 2 2 - 3 Código de acuerdo con la Tabla No. 6

(^3) Código Participante Receptor M 0RRRRTTT 8 4 - 11
Código del Participante Receptor al cual se
le está enviando los avisos de contabilidad.
4 Digito de chequeo M N 1 12 - 12 En este campo debe ir el dígito de chequeo
del campo 3.
5 Número^ de cuenta de depósito^
del Participante

###### R AN 15 13 - 27

```
Número de la cuenta de depósito del
Participante Receptor en el Banco de la
República.
```
```
6 Valor de la transacción M $$$$$$$$$$$
$$$$$cc
```
```
18 28 - 45 Valor total afectado en la cuenta de
depósito.
```
```
7 Numero de ruta del aviso de
contabilidad
```
###### M N 9 46 - 54

```
Contiene el número de ruta con su dígito
de chequeo del Participante Receptor al
cual se le está enviando los avisos de
contabilidad. Se repite la información del
campo 3.
```
```
8 Identificacion de archivo O AN 5 55 - 59
```
```
Fecha de creación del archivo y el
modificador del archivo del Registro de
Encabezado de Archivo asociado con el
aviso de contabilidad que se está enviando.
9 Datos del operador ACH O AN 1 60 - 60 Datos discrecionales del Operador ACH.
```
```
10 Nombre del Participante
Receptor
```
```
R AN 22 61 - 82 Nombre del Participante Receptor^ de los
avisos de contabilidad.
```
```
11 Datos discrecionales O AN 2 83 - 84
```
```
Datos Discrecionales registrados por el
Operador ACH.
```
```
12 Indicador de registro adenda M N 1 85 - 85
```
```
Valor “1” si se requiere anexar información
adicional relacionada con el pago. Valor
“0” en caso contrario.
```
```
13 Numero de ruta operador ACH M 0RRRRTTT 8 86 - 93
```
```
Contiene el código del Operador ACH que
está enviando los avisos de contabilidad.
```
```
14 Fecha juliana de creación del
aviso de contabilidad
```
```
insertado
por el
Operador
ACH
```
```
N 3 94 - 96 Fecha juliana de creación del aviso de
contabilidad.
```
```
15 Numero de secuencia en el lote M N 4 97 - 100
```
```
Número de secuencia diaria asignada por el
Operador ACH.
16 Reservado N/D Blancos 6 101 - 106 Campo reservado.
```

## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

### 8. HISTORIAL DE CAMBIOS

## Fecha Modificación / Inclusión / Eliminación

## 30 - nov- 2020 Se incluye a la entidad COOFINEP

## 04 - mar- 2021 Se formaliza el cambio de JPMORGAN CORPORACIÓN por BANCO JP MORGAN

## 10 - Ago- 2021 1. Se elimina la entidad FEDECAJAS

## 2. Se formaliza el cambio de razón social de BANCO PROCREDIT a BANCO

## CREDIFINANCIERA

## 3. Se formaliza el cambio de razón social de BANCOMPARTIR a MIBANCO

## 4. Se incluye la nueva entidad FOGAFIN – FONDO DE GARANTÍAS DE

## INSTITUCIONES FINANCIERAS

## 26 - oct- 2021 Se actualiza la Tabla No. 4 “DESCRIPCIÓN DE LOTE” para incluir la nueva descripción

## SUBSIDIOS

## 4 - oct- 2022 - Se cambia el nombre del documento

## - Se eliminan las tablas 4, 5 y 6 las cuales quedan contenidas en el Anexo 2 del

## Manual Operativo del Sistema de Compensación Electrónica Nacional

## Interbancaria – CENIT

## - Se actualizan los términos y definiciones de acuerdo con lo establecido en el

## Reglamento del Sistema de Compensación Electrónica Nacional Interbancaria -

## CENIT

## - Se complementan las hojas 1 7 y 2 1 para incluir obligaciones de los adquirientes

## al generar archivos NACHAM

## 10 - mayo- 2023 − Se elimina la obligatoriedad de devolver el campo 7 – Descripción de Lote del

## Registro Tipo 5 – Encabezado de Lote, tal como se recibió en la transacción

## original, para todo tipo de devolución. Hoja No.47

## − Se incluye nota aclaratoria mencionando la generación de un lote de

## transacciones en el archivo de salida, cuando se presente cancelación de

## transacciones por fondos insuficientes o cancelación manual. Hoja No.65

## − Se actualiza la Tabla No. 4 “DESCRIPCIÓN DE LOTE” para incluir las nuevas

## descripciones obligatorias. Hoja No.76

13 - junio- (^2024) − Se incluye el numeral 6.3 CÁLCULO DEL DÍGITO DE CHEQUEO para dar claridad a

## la forma de calcular automáticamente el dato requerido.

## 7 - mayo- 2026 − Se ajusta el numeral 4.1 TIPOS DE DATOS


## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

## ANEXO 1 - DESCRIPCIÓN DE CAMPOS SERVICIOS PPD, CCD y CTX

## 1. DESCRIPCIÓN DE CAMPOS SERVICIOS PPD, CCD Y CTX

#### 1.1 Registro de Encabezado de Archivo para todos los servicios

```
# Nombre de
Campo
```
```
Inclusión Contenido Longitud Posición Descripción Causa de Invalidez en
ACH (*)
```
```
Tipo de
Rechazo
```
###### 1

```
Código tipo de
registro M^ “1”^1 1
```
```
Valor válido para este campo "1".
Indica que es un Registro de
Encabezado de Archivo.
```
- Valor diferente de
    “1”.
- Secuencia dentro del
    archivo inválida.

```
Fatal
```
###### 2

```
Código de
prioridad R^ N^2 2 -^3
```
```
Usado para manejar un esquema
de prioridades. Valor válido
“01”.
```
###### N/A N/A

###### 3

```
Código entidad
destino
inmediato
```
###### M

```
b0RRRRTTT
C 10 4 -^13
```
```
Código del ACH o del Participante
a donde se envía el archivo,
expresado en formato
b0RRRRTTTC donde:
```
- El código de tránsito es
    diferente a los
    establecidos en la
    Tabla de Códigos de
    Tránsito de la ACH (^1 )
- Dígito de chequeo
    inválido.

```
Formal
```
```
b
0RRRR
```
```
TTT
C
```
```
Espacio en blanco
Código de Ruta
00001
Código de Transito
Dígito de chequeo
módulo base 10
Para ACH-CENIT es:
b01111111
```
###### 4

```
Código entidad
origen
inmediato
```
```
M b00001TTT
C
```
###### 10 14 - 23

```
Código del Participante que envía
el archivo o código del ACH,
expresado en formato
b00001TTTC donde:
```
- El código de tránsito es
    diferente a los
    establecidos en la Tabla
    de Códigos de Tránsito
    del ACH.
-
- Dígito de chequeo
    inválido.

```
b
00001
TTT
C
```
```
Espacio en blanco
Valor fijo
Según Tabla de
Códigos de Tránsito(1)^
dígito de chequeo
módulo base 10
```
```
Formal
```
###### 5

```
Fecha de
creación del
archivo
```
###### M

###### AAAAMMD

###### D 8 24 -^31

```
Fecha de creación del archivo.
Expresada en formato
"AAAAMMDD" donde: AAAA es
el año; MM es el mes y DD es el
día de creación del archivo.
```
```
Año, Mes o Día no
corresponden con la
fecha de proceso en el
ACH o son inválidos.
```
```
Fatal
```
###### 6

```
Hora de
creación del
archivo
```
###### O HHMM 4 32 - 35

```
Hora en la cual es transmitido o
creado el archivo. Expresada en
formato "HHMM” donde: HH es
la hora (24 horas) y MM son los
minutos.
```
###### N/A N/A

###### 7

```
Identificador
del archivo M^ A-Z / 0-^9 1 36 -^36
```
```
Este campo permite identificar
archivos creados en la misma
fecha (máximo 999 archivos en la
misma fecha).
```
```
El identificador del
archivo debe
corresponder con el
número consecutivo
registrado en el nombre
```
```
Fatal
```

## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

```
1.1 Registro de Encabezado de Archivo para todos los servicios
```
```
#
```
```
Nombre de
Campo Inclusión^ Contenido^ Longitud^ Posición^ Descripción^
```
```
Causa de Invalidez en
ACH (*)
```
```
Tipo de
Rechazo
Los valores válidos para este
campo son letras mayúsculas de
la “A” a la “Z” y dígitos del “0” al
”9”.
```
```
del archivo enviado (ver
numeral 6.1)
```
```
8 Tamaño del
registro
```
###### M ‘106’ 3 37 - 39

```
Indica el número de caracteres
contenidos en cada registro. El
valor válido es “106”.
```
```
Valor diferente de ‘106’ Fatal
```
###### 9

```
Factor de
ablocamiento M^ ‘10’^2 40 -^41
```
```
Define el número de registros
dentro de un bloque (un bloque
son 1060 caracteres). El número
de registros en el archivo debe
ser múltiplo de 10; en caso de
que los registros de un archivo no
sean múltiplo de diez, se debe
completar con números nueve.
El valor válido es “10”.
```
```
Valor diferente de “10”. Fatal
```
###### 10

```
Código de
formato M^ “1”^1 42 -^42
```
```
Permite futuras variaciones de
formato. El valor válido es “1”. Valor diferente de “1”^ Fatal^
```
###### 11

```
Nombre
entidad
destino
inmediato
```
###### O AN 23 43 - 65

```
Nombre del ACH o del
Participante Receptor a donde se
envía el archivo. Este nombre
corresponde al código del campo
3 de este registro
```
###### N/A N/A

###### 12

```
Nombre
entidad origen
inmediato
```
###### O AN 23 66 - 88

```
Nombre del Participante
Originador que envía el archivo o
del ACH. Este nombre
corresponde al código del campo
4 de este registro
```
###### N/A N/A

###### 13

```
Código de
referencia M^ AN^8 89 -^96
```
```
Identifica el código del sistema y
es definido por el Operador ACH.
Debe llevar 1 en su primera
posición y blancos a la derecha
(1bbbbbbb).
```
```
Valor diferente a
“1bbbbbbb” Fatal^
```
```
14 Reservado N/D Blancos 10 97 - 106
```
```
Campo reservado. Este campo
debe ir en blancos. N/A^ N/A^
```
```
(*) La Causal de Invalidez en ACH hace referencia al contenido del campo; sin embargo, en cuanto a las validaciones de formato
y estructura NACHA-M, el sistema se encuentra en capacidad de validar cada uno de los tipos de registros y campos de los
archivos, por lo tanto, inconsistencias en aspectos como la alineación de campos (alfanumérico o numérico) y el tipo de
inclusión, son revisados y detectados por el sistema, pudiendo generar rechazos totales o parciales de archivos.
(1) Ver Anexo 1-Participantes, Operadores de Información y Códigos de Compensación del Manual Operativo Sistema de
Compensación Electrónica Nacional Interbancaria CENIT.
```

## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

#### 1.2 Registro de Encabezado de Lote para todos los servicios

###### #

```
Nombre de
Campo
```
**Inclusión Contenido Longitud Posición** (^) **Descripción Causa de Invalidez en ACH ** Tipo de
Rechazo**

###### 1

```
Código tipo de
registro M^ “5”^1 1
```
```
Valor válido para este campo
"5". Indica que es un Registro
de Encabezado de Lote.
```
- Valor diferente de “5”
- Secuencia dentro del archivo
    inválida.

```
Fatal
```
###### 2*

```
Código clase
de
transacciones
por lote
```
###### M N 3 2 - 4

```
Identifica el tipo de
transacciones que contiene el
lote. Los valores válidos se
observan en la Tabla No. 8
Códigos de Clases de
```
## Transacciones por lote.

- Valor diferente a los
    establecidos en la Tabla No. 8
    Códigos de Clases de
    Transacciones por lote.
- Código no acorde con las
    transacciones contenidas en
    el lote.

```
Fatal
```
```
3* Nombre del
Originador
```
###### M AN 16 5 - 20

```
El valor de este campo es
establecido por el Originador
para propósitos de identificar
el origen de la transacción o
para describir la misma al
Receptor.
```
- Campo en blanco o en ceros Fatal

###### 4*

```
Datos
discrecionales
del Originador
```
###### O AN 20 21 - 40

```
Permite al Originador y/o al
Participante Originador incluir
códigos o datos (uno o más) de
significado únicamente para
ellos, referencia especialmente
para manejo de las
transacciones contenidas en el
lote. No hay una
estandarización para la
interpretación del valor de este
campo.
```
###### • N/A N/A

###### 5*

```
Identificacion
del Originador R^ AN^10 41 -^50
```
```
Número de identificación del
Originador. El Originador
puede ser la misma
Participante Originador.
```
###### • N/A N/A

```
6* Tipo de
servicio
```
###### M AN 3 51 - 53

```
Este campo es nemónico,
permite identificar los
diferentes tipos de servicio,
según la Valor diferente a los
establecidos en la Tabla No. 5
de Tipos de Servicio.
```
- Valor diferente a los
    establecidos en la Valor
    diferente a los establecidos
    en la Tabla No. 5 de Tipos de
    Servicio.

```
Fatal
```
###### 7

```
Descripción de
lote M^ AN^10 54 -^63
```
```
El Originador establece el
contenido de este campo para
proveer una descripción de la
transacción contenida en el
lote, al Receptor. Se deben
utilizar las descripciones de la
Tabla No.4 Descripciones de
Lote, alineado a la izquierda.
Las descripciones de la Tabla
No. 4 son de uso obligatorio
```
- Si el campo está en blanco o
    ceros Fatal^


## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

```
1.2 Registro de Encabezado de Lote para todos los servicios
```
```
#
```
```
Nombre de
Campo
```
**Inclusión Contenido Longitud Posición** (^) **Descripción Causa de Invalidez en ACH ** Tipo de
Rechazo**

###### 8*

```
Fecha
descriptiva
para el
Originador
```
###### O AAAAMMD

###### D

###### 8 64 - 71

```
El Originador establece este
campo como la fecha que
desea mostrar al Receptor.
Esta fecha es sólo de carácter
informativo.
Este campo no se valida.
```
###### • N/A N/A

###### 9

```
Fecha efectiva
de la
transacción
```
###### M

###### AAAAMMD

###### D 8 72 -^79

```
Fecha en la cual se deben
aplicar las transacciones
contenidas en el lote. Está
expresada en formato
“AAAAMMDD”, donde: AAAA
es el año; MM es el mes y DD
es el día de aplicación del lote.
Esta fecha debe ser igual o
mayor que la fecha de proceso.
```
- Año, Mes o Día menor a la
    fecha de proceso en el ACH
    o son inválidos.
- Si esta fecha es mayor a la
    fecha de proceso en el ACH,
    se almacena para el futuro.
- Si la fecha efectiva de las
    transacciones corresponde
    a un día no hábil en ACH, las
    transacciones se
    procesarán al día hábil
    siguiente.

```
Formal
```
###### 10

```
Fecha de
compensación
juliana
```
###### M N 3 80 - 82

```
Fecha de compensación o
liquidación de las
transacciones. Es la fecha en
que las cuentas de los
Participantes serán afectadas
en el Banco de la República.
```
- El campo viene vacío.
- El dato no corresponde a
    un valor comprendido
    entre 1 y 366
- El campo no es numérico

```
Fatal
```
###### 11

```
Código de
estado del
Originador
```
###### M AN 1 83 - 83

```
Este código hace referencia al
Originador que inicia la
transacción. El valor válido es
“1”.
```
- Valor diferente de “1”. Fatal

###### 12

```
Código
Participante
Originador
```
###### M 00001TTT 8 84 - 91

```
Registra el número de Ruta y
Tránsito del Participante
Originador , expresado en
00001TTT donde:
00001 Valor Fijo
TTT según Tabla de Códigos
de Tránsito(1)
```
- El código de tránsito es
    diferente a los establecidos
    en la Tabla de Códigos de
    Tránsito del ACH(1).
- Código diferente del
    registrado en el Registro de
    Encabezado de Archivo.

```
Fatal
```
```
13 Número del
lote
```
###### M N 7 92 - 98

```
Secuencial ascendente único,
se incrementa en uno cada vez
que ingrese un nuevo registro
tipo "5". Este mismo valor se
debe registrar en el campo 11
del registro tipo “8”. Este
número indica el orden del lote
dentro de un archivo.
```
- Valor no numérico, en
    ceros o duplicado en otro
    lote del mismo archivo.
- Secuencia de Números de
    Lote incorrecta en el
    archivo.

```
Fatal
```
```
14 Reservado N/D Blancos 8 99 - 106
```
```
Campo reservado. Este campo
debe ir en blancos. •^ N/A^ N/A^
```

## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

```
1.2 Registro de Encabezado de Lote para todos los servicios
```
```
#
```
```
Nombre de
Campo
```
**Inclusión Contenido Longitud Posición** (^) **Descripción Causa de Invalidez en ACH ** Tipo de
Rechazo**
* Campo que debe ser devuelto intacto, sin ninguna modificación en cualquier tipo de transacción de devolución.
** La Causal de Invalidez en ACH hace referencia al contenido del campo; sin embargo, en cuanto a las validaciones de formato y estructura
NACHA-M, el sistema se encuentra en capacidad de validar cada uno de los tipos de registros y campos de los archivos, por lo tanto,
inconsistencias en aspectos como la alineación de campos (alfanumérico o numérico) y el tipo de inclusión, son revisados y detectados por el
sistema, pudiendo generar rechazos totales o parciales de archivos.
(1) (1)^ Ver Anexo 1-Participantes, Operadores de Información y Códigos de Compensación del Manual Operativo Sistema de Compensación
Electrónica Nacional Interbancaria CENIT

#### 1.3 Registro de Detalle de Transacciones – Servicios PPD y CCD

**# Nombre de Campo Inclusión Contenido Longitud Posición Descripción Causa de Invalidez en ACH**** (^) **RechazoTipo de**
1 Código tipo de
registro

###### M “6” 1 1

```
Valor válido para este
campo "6". Indica que es
un Registro de Detalle de
Transacciones.
```
- Valor diferente de “6”.
- Secuencia dentro del archivo
    inválida.

```
Fatal
```
###### 2

```
Código de
transacción
```
###### M N 2 2 - 3

```
Indica el tipo de
transacción y el tipo de
cuenta que se envía, según
la Tabla No. 6 de Códigos
de Transacción.
Si el ACH usa los Avisos de
Contabilidad Automática
(ADV), los valores válidos
para este campo son los
especificados en la Tabla 7
de Códigos de Avisos de
Contabilidad.
```
```
El código de transacción es
diferente a los establecidos en
la Tabla No. 6 de Códigos de
Transacción.
```
```
Fatal
```
###### 3

```
Código
Participante
Receptor
```
###### M 00001TTT 8 4 - 11

```
Registra el número de Ruta
y Tránsito del Participante
Receptor de la
transacción, expresado en
0RRRRTTT donde:
00001 Valor fijo
TTT según Tabla de
Códigos de Tránsito(1)
```
```
El código de tránsito es
diferente a los establecidos en
la Tabla de Códigos de Tránsito
del ACH(1).
```
```
Formal
```
###### 4

```
Digito de
chequeo
```
###### M N 1 12 - 12

```
En este campo debe ir el
dígito de chequeo del
campo 3 de este registro
(base 10, con los factores
de cálculo 7, 3, 1 de
derecha a izquierda).
```
```
Dígito de chequeo inválido. Formal
```

## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

```
1.3 Registro de Detalle de Transacciones – Servicios PPD y CCD
```
**# Nombre de Campo Inclusión Contenido Longitud Posición Descripción Causa de Invalidez en ACH**** (^) **RechazoTipo de**

###### 5*

```
Número de
cuenta
Receptor
```
###### R AN 17 13 - 29

```
Número de cuenta
Receptor en el Participante
Receptor, Justificado a la
izquierda, con espacios a la
derecha. Si la cuenta tiene
ceros a la izquierda, deben
ser escritos; este campo no
debe contener caracteres
diferentes a números.
```
###### N/A N/A

```
6* Valor de la transacción M $$$$$$$$$$$$$$$$cc 18 30 - 47
```
```
Este campo registra el valor
monetario de la
transacción. Valor entero
con dos (2) decimales,
expresado en
$$$$$$$$$$$$$$$$CC.
```
- Campo no numérico
- Valor diferente de “0” en
    una transacción de
    prenotificación.
- Valor igual a “0” en una
    transacción débito o
    crédito.
- Límite por tipo de
    transacción excedido

```
Formal
```
###### 7*

```
Número de
identificación
del Receptor
```
###### O AN 15 48 - 62

```
Campo utilizado por el
Originador para identificar
al Receptor. Se podrán
hacer validaciones
convenidas entre el
Originador y el Participante
Receptor o el Receptor
```
###### N/A N/A

```
8* Nombre
Receptor
```
```
R AN 22 63 - 84 Registra el nombre^ del^
Receptor.
```
###### N/A N/A

###### 9*

```
Datos
discrecionales O^ AN^2 85 -^86
```
```
Puede ser utilizado por el
Originador o el Participante
Originador para solicitar a
el Participante Receptor ,
las correspondientes
validaciones del Número
de Identificación del
Receptor de transacciones
crédito, colocando el
código “V” o “v” justificado
a la izquierda.
```
###### N/A N/A


## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

```
1.3 Registro de Detalle de Transacciones – Servicios PPD y CCD
```
**# Nombre de Campo Inclusión Contenido Longitud Posición Descripción Causa de Invalidez en ACH**** (^) **RechazoTipo de**
10 Indicador de
registro adenda

###### M N 1 87 - 87

```
Indica la existencia de
Registro Adenda, (1) se
incluye, (0) si no tiene
registro adicional.
```
- Valor diferente de “1” o
    “0”.
- Valor igual a “0”, pero
    existe un Registro Adenda
    asociado.
- Valor igual a “1”, pero no
    existe un Registro Adenda
    asociado.
- Valor diferente de “1” en
    una transacción de
    Devolución, de Devolución
    de una Devolución.

```
Formal
```
```
11 Numero de
secuencia
```
###### M N 15 88 - 102

```
En las primeras 8
posiciones se debe
registrar la Ruta y Tránsito
del Participante Originador
y en las siguientes 7
posiciones. Ver numeral 5
Manual de
Especificaciones Formato
NACHA-M CENIT.
```
- Primeras ocho (8)
    posiciones contienen un
    valor diferente del Código
    del Participante Originador
    contenido en el Registro de
    Encabezado de Lote.
- Número de Secuencia no
    ascendente en el archivo o
    en el día de proceso.
- Número de Secuencia
    duplicado.

```
Fatal
```
```
12 Reservado N/D Blancos 4 103 - 106
```
```
Campo reservado. Este
campo debe ir en blancos. N/A^ N/A^
```
```
* Campo que debe ser devuelto intacto, sin modificación alguna, en cualquier tipo de transacción de devolución
** La Causal de Invalidez en ACH hace referencia al contenido del campo; sin embargo, en cuanto a las validaciones de formato
y estructura NACHA-M, el sistema se encuentra en capacidad de validar cada uno de los tipos de registros y campos de los
archivos, por lo tanto, inconsistencias en aspectos como la alineación de campos (alfanumérico o numérico) y el tipo de inclusión,
son revisados y detectados por el sistema, pudiendo generar rechazos totales o parciales de archivos.
```
## (1) Ver Anexo 1-Participantes, Operadores de Información y Códigos de Compensación del Manual Operativo Sistema de

```
Compensación Electrónica Nacional Interbancaria CENIT
```

## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

#### 1.4 Registro de Detalle de Transacciones – Servicio CTX

```
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción Causa de Invalidez en ACH** RechazoTipo de
```
```
1 Código tipo
de registro
```
###### M “6” 1 1

```
Valor válido para este campo "6".
Indica que es un Registro de Detalle
de Transacciones.
```
- Valor diferente de
    “6”.
- Secuencia dentro
    del archivo inválida.

```
Fatal
```
```
2 Código de
transacción
```
###### M N 2 2 - 3

```
Indica el tipo de transacción y el tipo
de cuenta que se envía, según la
Tabla No. 6 de Códigos de
Transacción.
Si el ACH usa los Avisos de
Contabilidad Automática (ADV), los
valores válidos para este campo son
los especificados en la Tabla No. 7 de
Códigos de Avisos de Contabilidad.
```
```
El código de transacción
es diferente a los
establecidos en la Tabla
No. 6 Códigos de
Transacción.
```
```
Fatal
```
###### 3

```
Código
Participante
Receptor
```
###### M 00001TTT 8 4 - 11

```
Registra el número de Ruta y Tránsito
del Participante Receptor de la
transacción, expresado en 0RRRRTTT
donde:
00001 Valor fijo
TTT según Tabla de Códigos de
Tránsito(1)
```
- El código de tránsito
    es diferente a los
    establecidos en la
    Tabla de Códigos de
    Tránsito del ACH(1).

```
Formal
```
###### 4

```
Digito de
chequeo M^ N^1 12 -^12
```
```
En este campo debe ir el dígito de
chequeo del campo 3 de este registro
(base 10, con los factores de cálculo
7, 3, 1 de derecha a izquierda).
```
```
Dígito de chequeo
inválido. Formal^
```
###### 5*

```
Número de
cuenta
Receptor
```
###### R AN 17 13 - 29

```
Número de cuenta del Receptor en el
Participante Receptor. Justificado a
la izquierda, con espacios a la
derecha. Si la cuenta tiene ceros a la
izquierda, deben ser escritos; este
campo no debe contener caracteres
diferentes a números.
```
###### N/A N/A

```
6* Valor de la
transacción
```
###### M $$$$$$$$$

```
$$$$$$$cc
```
###### 18 30 - 47

```
Este campo registra el valor
monetario de la transacción. Valor
entero con dos (2) decimales,
expresado en $$$$$$$$$$$$$$$$cc.
```
- Campo no numérico
- Valor diferente de “0”
    en una transacción de
    prenotificación
- Valor igual a “0” en
    una transacción
    débito o crédito.
- Límite por tipo de
    transacción excedido

```
Formal
```
###### 7*

```
Número de
identificación
Receptor
```
###### O AN 15 48 - 62

```
Campo utilizado por el Originador
para identificar al Receptor. Se
podrán hacer validaciones
convenidas entre el Originador y el
Participante Receptor o el Receptor
```
###### N/A N/A


## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

```
1.4 Registro de Detalle de Transacciones – Servicio CTX
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción Causa de Invalidez en ACH** RechazoTipo de
```
###### 8*

```
Numero de
registros de
adenda
```
###### R N 4 63 - 66

```
Número de registros de adenda del
registro de detalle de transacciones
Para transacciones monetarias
crédito este campo toma solo valores
comprendidos entre 0001 y 9999.
Para devoluciones monetarias
crédito siempre este campo tiene el
valor “0001”, dado que únicamente
tiene una adenda.
```
###### N/A N/A

```
9* Nombre
Receptor
```
```
R AN 16 67 - 82 Registra el nombre del Receptor. N/A N/A
```
```
10 Reservado R AN 2 83 - 84 Campo Reservado N/A N/A
```
```
11* Datos
discrecionales
```
###### O AN 2 85 - 86

```
Puede ser utilizado por el Originador
o el Participante Originador para
solicitar a el Participante Receptor ,
las correspondientes validaciones del
Número de Identificación del
Receptor de transacciones crédito,
colocando el código “V” o “v”
justificado a la izquierda.
```
###### N/A N/A

###### 12

```
Indicador de
registro
adenda
```
###### M N 1 87 - 87

```
Indica la existencia de Registro
Adenda, (1) se incluye, (0) si no tiene
registro adicional.
```
- Valor diferente de “1”
    o “0”.
- Valor igual a “0”, pero
    existe un Registro
    Adenda asociado.
- Valor igual a “1”, pero
    no existe un Registro
    Adenda asociado.
- Valor diferente de “1”
    en una transacción de
    Devolución, de
    Devolución de una
    Devolución

```
Formal
```
###### 13

```
Numero de
secuencia M^ N^15 88 -^102
```
```
En las primeras 8 posiciones se debe
registrar la Ruta y Tránsito del
Participante Originador y en las
siguientes 7 posiciones. Ver numeral
5 Manual de Especificaciones
Formato NACHA-M CENIT.
```
- Primeras ocho (8)
    posiciones contienen
    un valor diferente del
    Código del
    Participante
    Originador contenido
    en el Registro de
    Encabezado de Lote.
- Número de Secuencia
    no ascendente en el
    archivo o en el día de
    proceso.
- Número de Secuencia
    duplicado.

```
Fatal
```
```
14 Reservado N/D Blancos 4
```
###### 103 -

###### 106

```
Campo reservado. Este campo debe
ir en blancos. N/A^ N/A^
```

## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

```
1.4 Registro de Detalle de Transacciones – Servicio CTX
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción Causa de Invalidez en ACH** RechazoTipo de
* Campo que debe ser devuelto intacto, sin modificación alguna, en cualquier tipo de transacción de devolución
** La Causal de Invalidez en ACH hace referencia al contenido del campo; sin embargo, en cuanto a las validaciones de formato y estructura
NACHA-M, el sistema se encuentra en capacidad de validar cada uno de los tipos de registros y campos de los archivos, por lo tanto,
inconsistencias en aspectos como la alineación de campos (alfanumérico o numérico) y el tipo de inclusión, son revisados y detectados por el
sistema, pudiendo generar rechazos totales o parciales de archivos.
(1) Ver Anexo 1-Participantes, Operadores de Información y Códigos de Compensación del Manual Operativo Sistema de Compensación
Electrónica Nacional Interbancaria CENIT.
```
#### 1.5 Registro Adenda – Información Adicional para todos los Servicios

```
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción Causa de Invalidez en ACH** RechazoTipo de
```
###### 1

```
Código tipo de
registro M^ “7”^1 1
```
```
Valor válido para este campo "7".
Indica que es un Registro Adenda.
```
- Valor diferente de “7”.
- Secuencia dentro del
    archivo inválida.

```
Fatal
```
###### 2

```
Código tipo de
registro
adenda
```
###### M ”05” 2 2 - 3

```
Define la interpretación específica
y el formato de información
adicional contenida en este mismo
registro, según se define en la
Tabla No. 9 de Códigos de Tipo de
Registro Adenda.
```
```
El código tipo de registro
adenda es diferente al
establecido en la Tabla
No. 9 Códigos de Tipo de
Registro Adenda para
información adicional de
transacciones crédito,
débito y de
prenotificaciones, es
decir “05”.
```
```
Fatal
```
###### 3

```
Información
relacionada
con el pago
```
###### R AN 80 4 - 83

```
Este es un campo para colocar
información relacionada con el
pago.
```
###### N/A N/A

###### 4

```
Numero de
secuencia de
registro
adenda
```
###### M N 4 84 - 87

```
Este número es asignado para cada
registro. El primer registro debe
iniciar en “0001”, con numeración
consecutiva ascendente.
```
```
Valor diferente al
asignado a cada registro
de adenda
```
```
Fatal
```
###### 5

```
Numero de
secuencia del
registro de
detalle de la
transacción
asociada a la
adenda
```
###### M N 7 88 - 94

```
Su valor debe coincidir con las siete
últimas posiciones del campo 11,
registro tipo “6”, al cual hace
referencia.
```
```
Valor diferente de las
siete últimas posiciones
del campo 11, registro
tipo “6”.
```
```
Fatal
```
```
6 Reservado N/D Blancos 12 95 - 106 Campo reservado. Este campo
debe ir en blancos.
```
###### N/A N/A

```
** La Causal de Invalidez en ACH hace referencia al contenido del campo; sin embargo, en cuanto a las validaciones de formato y estructura
NACHA-M, el sistema se encuentra en capacidad de validar cada uno de los tipos de registros y campos de los archivos, por lo tanto,
inconsistencias en aspectos como la alineación de campos (alfanumérico o numérico) y el tipo de inclusión, son revisados y detectados por el
sistema, pudiendo generar rechazos totales o parciales de archivos
```

## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

#### 1.6 Registro Adenda – Devolución para todos los servicios

```
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción Causa de Invalidez en ACH** RechazoTipo de
```
```
1 Código tipo de
registro
```
###### M “7” 1 1

```
Valor válido para este campo
"7". Indica que es un Registro
Adenda.
```
- Valor diferente de “7”.
0. Secuencia dentro del
    archivo inválida.

```
Fatal
```
```
2 Código tipo de
registro adenda
```
###### M “99” 2 2 - 3

```
Define la interpretación
específica y el formato de
información adicional contenida
en este mismo registro, según
se define en la Tabla No. 9
Códigos de Tipo de Registro
Adenda.
```
```
El código tipo de registro
adenda es diferente al
establecido en la Tabla
No. 9 Códigos de Tipo de
Registro Adenda para
información de
devoluciones, es decir
“99”.
```
```
Fatal
```
```
3 Causal de
devolución
```
###### M AN 3 4 - 6

```
Registra la causal de devolución
definida para describir la razón
de la devolución Tabla Causales
de Devolución(1). Si existe más
de una causal de devolución se
debe registrar la de mayor peso.
```
```
Valor diferente a los
establecidos en la Tabla
de Causales de
Devolución(1).
```
```
Formal
```
###### 4

```
Numero de
secuencia de la
transacción
original
```
###### M N 15 7 - 21

```
Número de Secuencia de la
transacción original.
```
```
El número de secuencia
de la transacción original
no está presente en el
Registro Adenda o no
coincide con una
transacción original
tramitada en la fecha de
operación.
```
```
Formal
```
###### 5

```
Fecha de
muerte * O^
```
###### AAAAMMD

###### D 8 22 -^29

```
Esta fecha corresponde a la
fecha de fallecimiento del titular
o beneficiario de la cuenta, este
campo es obligatorio cuando en
el campo 3 de este registro, se
encuentran las causales de
devolución R14 o R15, para otras
causales debe ir en blancos. Esta
fecha es expresada en formato
“AAAAMMDD” y no debe ser
superior a la fecha efectiva.
Solo puede ser diligenciada
cuando se usa la causal R14 o
R15 y debe ser válida y menor a
la fecha de proceso.
```
###### N/A N/A

###### 6

```
Código
Participante
Receptor de la
transacción
original
```
###### M 00001TTT 8 30 - 37

```
Registra el número de Ruta y
Tránsito del Participante
Receptor de la transacción
original (campo 3, registro tipo
“6” de la transacción original),
expresado en 00001TTT donde:
00001 Valor fijo
TTT según Tabla No. 6 de Códigos
de Tránsito
```
```
El código de tránsito es
diferente a los
establecidos en la Tabla
de Códigos de Tránsito
del ACH.
```
```
Fatal
```

## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

```
1.6 Registro Adenda – Devolución para todos los servicios
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción Causa de Invalidez en ACH** RechazoTipo de
```
```
7 Información^
adicional
```
###### O AN 44 38 - 41

```
El Participante Receptor debe
colocar la descripción estándar
definida en la Tabla Causales de
Devolución(1). Sin embargo, en
este campo puede ampliar el
detalle de la causal de la
devolución.
```
###### N/A N/A

###### 8

```
Numero de
secuencia del
registro adenda
```
###### M N 15 82 - 96

```
Número de secuencia del
Registro Adenda que está
asociado con el Registro de
Detalle de Transacciones.
Asignado por el Participante
Receptor que genera la
devolución.
```
- Primeras ocho (8)
    posiciones contienen
    un valor diferente del
    Código del Participante
    Originador de la
    devolución contenido
    en el Registro de
    Encabezado de Lote.
- Número de Secuencia
    no ascendente en el
    archivo o en el día de
    proceso.
- Número de Secuencia
    duplicado

```
Fatal
```
```
9 Reservado N/D Blancos 10 97/106 Campo reservado no disponible.
Este campo debe ir en blancos
```
###### N/A N/A

```
* Se genera rechazo de la transacción si el campo no es diligenciado cuando se usa la causal R14 o R15, o se diligencia pero la fecha no es
válida y menor a la fecha de proceso.
** La Causal de Invalidez en ACH hace referencia al contenido del campo; sin embargo, en cuanto a las validaciones de formato y estructura
NACHA-M, el sistema se encuentra en capacidad de validar cada uno de los tipos de registros y campos de los archivos, por lo tanto,
inconsistencias en aspectos como la alineación de campos (alfanumérico o numérico) y el tipo de inclusión, son revisados y detectados por
el sistema, pudiendo generar rechazos totales o parciales de archivos
(1) Ver Tabla No. 1 - Causales de Devolución para los Servicios PPD, CCD y CTX del Anexo No. 2 del Manual Operativo Sistema de
Compensación Electrónica Nacional Interbancaria CENIT.
```
#### 1.7 Registro Adenda – Devolución de una Devolución – Servicio PPD

**# Nombre de Campo Inclusión Contenido Longitud Posición Descripción Causa de Invalidez en ACH *** (^) **RechazoTipo de**
1 Código tipo de
registro

###### M “ 7 ” 1 1

```
Valor válido para este campo
"7". Indica que es un Registro
Adenda.
```
- Valor diferente de “7”.
- Secuencia dentro del
    archivo inválida.

```
Fatal
```
###### 2

```
Código tipo de
registro
adenda
```
###### M “99” 2 2 - 3

```
Define la interpretación
específica y el formato de
información adicional
contenida en este mismo
registro, según se define en la
Tabla No.^9 Códigos de Tipo de
Registro Adenda.
```
```
El código tipo de registro
adenda es diferente al
establecido en la Tabla No. 9
Códigos de Tipo de Registro
Adenda para información de
devoluciones de
devoluciones, es decir “99”.
```
```
Fatal
```

## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

```
1.7 Registro Adenda – Devolución de una Devolución – Servicio PPD
```
**# Nombre de Campo Inclusión Contenido Longitud Posición Descripción Causa de Invalidez en ACH *** (^) **RechazoTipo de**

###### 3

```
Causal de
devolución de
una
devolución
```
###### M AN 3 4 - 6

```
Registra la causal de
devolución de una devolución
definida para describir la razón
de la devolución de una
devolución Tabla Causales de
Devolución de una
Devolución(1).
```
```
Valor diferente a los
establecidos en la Tabla de
Causales de Devolución de
una Devolución(1).
```
```
Formal
```
###### 4

```
Numero de
secuencia de
la transacción
original
```
###### M N 15 7 - 21

```
Contiene el número de
secuencia de la transacción
original de la devolución que se
está devolviendo.
```
```
El número de secuencia de la
transacción original no está
presente en el Registro
Adenda o no coincide con la
secuencia de la transacción
de devolución original que se
está devolviendo.
```
```
Formal
```
```
5 Reservado N/D Blancos 8 22 - 29
```
```
Campo reservado no
disponible. Este campo debe ir
en blancos.
```
###### N/A N/A

###### 6

```
Código
Participante
Receptor de
la transacción
original
```
###### M 00001TTT 8 30 - 37

```
Registra el número de Ruta y
Tránsito del Participante
Receptor de la transacción
original (campo 3, registro tipo
“6” de la transacción original),
expresado en 00001TTT
donde:
00001 Valor fijo
TTT según Tabla No. 6 de
Códigos de Tránsito
```
```
El código de tránsito es
diferente a los establecidos
en la Tabla de Códigos de
Tránsito del ACH.
```
```
Fatal
```
```
7 Reservado N/D Blancos 3 38 - 40
```
```
Campo reservado no
disponible. Este campo debe ir
en blancos.
```
###### N/A N/A

###### 8

```
Numero de
secuencia de
la devolución
```
###### M N 15 41 - 55

```
Número de Secuencia de la
transacción de devolución
original.
```
```
El número de secuencia de la
transacción de devolución no
está presente en el Registro
Adenda o no coincide con una
transacción de devolución
original tramitada en la fecha
de operación o fecha
inmediatamente anterior.
```
```
Formal
```
###### 9

```
Fecha de
compensación
de la
devolución
```
```
M N 3 56 - 58 Fecha de Compensación de la
Devolución.
```
```
Fecha inválida, es decir, no
corresponde a un valor
comprendido entre 1 y 365, o
un carácter diferente de
espacios.
```
```
Fatal
```
```
10 Causal de
devolución
```
```
R AN 2 59 - 60 Registra la causal de
devolución original.
```
###### N/A N/A

```
11 Reservado N/D Blancos 21 61 - 81
```
```
Campo reservado no
disponible. Este campo debe ir
en blancos.
```
###### N/A N/A


## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

```
1.7 Registro Adenda – Devolución de una Devolución – Servicio PPD
```
**# Nombre de Campo Inclusión Contenido Longitud Posición Descripción Causa de Invalidez en ACH *** (^) **RechazoTipo de**
12 Numero de
secuencia

###### M N 15 82 - 96

```
Número de secuencia del
registro adenda que está
asociado con el Registro de
Detalle de Transacciones.
Asignado por la Entidad
Financiera Originadora que
genera la devolución de una
devolución.
```
- Primeras ocho (8)
    posiciones contienen un
    valor diferente del
    Código de la Entidad
    Financiera Originadora
    de la devolución de una
    devolución contenido
    en el Registro de
    Encabezado de Lote.
- Número de Secuencia
    no ascendente en el
    archivo o en el día de
    proceso.
- Número de Secuencia
    duplicado.

```
Fatal
```
```
13 RESERVADO N/D Blancos 10 97 - 106
```
```
Campo reservado no
disponible. Este campo debe ir
en blancos.
```
###### N/A N/A

```
* La Causal de Invalidez en ACH hace referencia al contenido del campo; sin embargo, en cuanto a las validaciones de formato
y estructura NACHA-M, el sistema se encuentra en capacidad de validar cada uno de los tipos de registros y campos de los
archivos, por lo tanto, inconsistencias en aspectos como la alineación de campos (alfanumérico o numérico) y el tipo de
inclusión, son revisados y detectados por el sistema, pudiendo generar rechazos totales o parciales de archivo
(1) Ver Tabla No. 2 - Causales de Devolución de una Devolución para PPD del Anexo No. 2 del Manual Operativo Sistema de
Compensación Electrónica Nacional Interbancaria CENIT.
```
#### 1.8 Registro de Control de Lote para todos los Servicios

**# Nombre de Campo Inclusión Contenido Longitud Posición Descripción Causa de Invalidez en ACH (*)** (^) **RechazoTipo de**

###### 1

```
Código tipo de
registro M^ “8”^1 1
```
```
Valor válido para este
campo "8". Indica que es
un Registro de Control de
Lote.
```
- Valor diferente de “8”.
- Secuencia dentro del
    archivo inválida.

```
Fatal
```
###### 2*

```
Código clase de
transacciones
por lote
```
###### M N 3 2 - 4

```
Identifica el tipo de
transacciones que
contiene el lote. Los
valores válidos se
observan en la Tabla No.
8 Códigos de Clases de
Transacciones por Lote
```
- Valor diferente a los
    establecidos en la Tabla No.
    8 de Códigos de Clases de
    Transacciones por Lote.
- Código no acorde con las
    transacciones contenidas en
    el lote.
- Valor no corresponde con el
    registrado en el campo
    Código Clase de
    Transacciones por Lote del
    Registro de Encabezado de
    Lote.

```
Fatal
```

## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

```
1.8 Registro de Control de Lote para todos los Servicios
```
**# Nombre de Campo Inclusión Contenido Longitud Posición Descripción Causa de Invalidez en ACH (*)** (^) **RechazoTipo de**

###### 3

```
Número de
transacciones
detalladas y de
registros
adenda
```
###### M N 6 5 - 10

```
Número total de
registros de detalle de
transacciones y registros
adenda contenidos en el
lote.
```
```
Valor en blanco o valor que no
concuerda con el conteo de
registros en el lote.
```
```
Fatal
```
###### 4

```
Totales de
control M^ N^10 11 -^20
```
```
Sumatoria de los códigos
de ruta y tránsito del
Participante Receptor de
los Registros de Detalle
de Transacciones,
ignorando el dígito de
chequeo, si es el caso. Los
Registros Adenda no son
sumados.
```
```
Valor en blanco o valor que no
concuerda con la suma de los
códigos de ruta y tránsito de las
transacciones contenidas en el
lote.
```
```
Fatal
```
```
5 Valor total de
débitos
```
###### M $$$$$$$$$

```
$$$$$$$cc
```
###### 18 21 - 38

```
Suma de los valores de
las transacciones tipo
débito contenidas en el
lote. Valor entero con
dos (2) decimales,
expresado en
$$$$$$$$$$$$$$$$cc.
```
```
Valor en blanco o valor que no
concuerda con la suma de las
transacciones débito
contenidas en el lote.
```
```
Fatal
```
###### 6

```
Valor total de
créditos M^
```
###### $$$$$$$$$

```
$$$$$$$cc 18 39 -^56
```
```
Suma de los valores de
las transacciones tipo
crédito contenidas en el
lote. Valor entero con
dos (2) decimales,
expresado en
$$$$$$$$$$$$$$$$cc.
```
```
Valor en blanco o valor que no
concuerda con la suma de las
transacciones crédito
contenidas en el lote.
```
```
Fatal
```
```
7* Identificacion
del Originador
```
###### M AN 10 57 - 66

```
Número de identificación
del Originador. El
Originador puede ser la
misma Participante
Originador. Es el mismo
asignado en el Registro
de Encabezado de Lote.
```
```
El código de tránsito es
diferente a los establecidos en
la Tabla de Códigos de Tránsito
del ACH(^1 ).
```
```
Fatal
```
###### 8

```
Código de
autenticación
de mensajes
```
###### O AN 19 67 - 85

```
Campo reservado para
un algoritmo de
seguridad.
```
###### N/A N/A

```
9 Reservado N/D Blancos 6 86 - 91
```
```
Campo reservado no
disponible. Debe ir en
blancos.
```
###### N/A N/A


## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

```
1.8 Registro de Control de Lote para todos los Servicios
```
**# Nombre de Campo Inclusión Contenido Longitud Posición Descripción Causa de Invalidez en ACH (*)** (^) **RechazoTipo de**

###### 10

```
Identificacion
del Participante
Originador
```
###### M 00001TTT 8 92 - 99

```
Registra el número de
Ruta y Tránsito del
Participante Originador ,
expresado en 00001TTT,
donde:
00001 Valor fijo
TTT según Tabla
Códigos de Tránsito(1)
```
```
Campo en blanco o en ceros o
valor no coincide con la
identificación registrada en el
Registro de Encabezado de
Lote.
```
```
Fatal
```
```
11 Número^ del
lote
```
###### M N 7 100 - 106

```
Número del Lote. Es el
mismo asignado en el
Registro de Encabezado
de Lote.
```
- Valor no numérico, en
    ceros o duplicado en otro
    lote del mismo archivo.
- Secuencia de Números de
    Lote incorrecta en el
    archivo
- Valor no coincide con el
    número de lote registrado
    en el Registro de
    Encabezado de Lote.

```
Fatal.
```
```
*Campo que debe ser devuelto intacto, sin modificación alguna, en cualquier tipo de transacción de devolución o devolución
de una devolución
** La Causal de Invalidez en ACH hace referencia al contenido del campo; sin embargo, en cuanto a las validaciones de formato
y estructura NACHA-M, el sistema se encuentra en capacidad de validar cada uno de los tipos de registros y campos de los
archivos, por lo tanto, inconsistencias en aspectos como la alineación de campos (alfanumérico o numérico) y el tipo de
inclusión, son revisados y detectados por el sistema, pudiendo generar rechazos totales o parciales de archivos
(1) Ver Anexo 1-Participantes, Operadores de Información y Códigos de Compensación del Manual Operativo Sistema de
Compensación Electrónica Nacional Interbancaria CENIT
```

## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

#### 1.9 Registro de Control de Archivo para todos los Servicios

```
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción Causa de Invalidez en ACH(*) RechazoTipo de
```
```
1 Código tipo de
registro
```
###### M “9” 1 1

```
Valor válido para este campo "9".
Indica que es un Registro de
Control de Archivo.
```
- Valor diferente de “9”.
- Secuencia dentro del
    archivo inválida.

```
Fatal
```
```
2 Cantidad de
lotes
```
###### M N 6 2 - 7

```
Número de lotes incluidos en el
archivo. Debe ser igual al número
de registros de encabezado de
lote en el archivo.
```
```
Valor en blanco o valor
que no concuerda con el
conteo de lotes en el
archivo.
```
```
Fatal
```
```
3 Numero de
bloques
```
###### M N 6 8 - 13

```
Número de bloques físicos en el
archivo de 10 registros cada uno,
contando todos los tipos de
registros usados (de encabezado y
control y de relleno) para
completar bloques. Cada vez que
el contador de registros sea igual a
10 se debe sumar 1 al contador de
bloques y restaurar el contador de
registros hasta completar el
archivo.
```
```
Valor no coincide con el
número de bloques en el
archivo.
```
```
Fatal
```
###### 4

```
Número de
transacciones
detalladas y de
registros
adenda
```
###### M N 8 14 - 21

```
Número total de registros de
detalle de transacciones y de
registros adenda contenidos en el
archivo.
```
```
Valor en blanco o valor
que no concuerda con el
conteo de registros de
detalle y de registros
adenda en el archivo.
```
```
Fatal
```
```
5 Totales de
control
```
###### M N 10 22 - 31

```
Sumatoria de los códigos de ruta y
tránsito del Participante Receptor
de los Registros de Detalle de
Transacciones, ignorando el dígito
de chequeo, si es el caso. Los
Registros Adenda no son
sumados.
```
```
Valor en espacios o valor
que no concuerda con la
suma de los totales de
control de los Registros
de Control de Lote.
```
```
Fatal
```
```
6 Valor total de
débitos
```
###### M $$$$$$$$$

```
$$$$$$$cc
```
###### 18 32 - 49

```
Suma de los valores de las
transacciones tipo débito
contenidas en el archivo. Valor
entero con dos (2) decimales,
expresado en
$$$$$$$$$$$$$$$$cc.
```
- Valor en espacios o
    valor que no
    concuerda con la suma
    de las transacciones
    débito contenidas en el
    archivo.
- Monto excede el
    máximo permitido
    para un archivo.

```
Fatal
```
###### 7

```
Valor total de
créditos M^
```
###### $$$$$$$$$

```
$$$$$$$cc 18 50 -^67
```
```
Suma de los valores de las
transacciones tipo crédito
contenidas en el archivo. Valor
entero con dos (2) decimales,
expresado en
$$$$$$$$$$$$$$$$cc.
```
- Valor en espacios o
    valor que no
    concuerda con la suma
    de las transacciones
    crédito contenidas en
    el archivo.
- Monto excede el
    máximo permitido
    para un archivo.

```
Fatal
```

## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

```
1.9 Registro de Control de Archivo para todos los Servicios
```
```
# Nombre de Campo Inclusión Contenido Longitud Posición Descripción Causa de Invalidez en ACH(*) RechazoTipo de
```
```
8 Reservado N/D Blancos 39 68 - 106
```
```
Campo reservado no disponible.
Debe ir en blancos. N/A^ N/A^
(*) La Causal de Invalidez en ACH hace referencia al contenido del campo; sin embargo, en cuanto a las validaciones de formato y estructura
NACHA-M, el sistema se encuentra en capacidad de validar cada uno de los tipos de registros y campos de los archivos, por lo tanto,
inconsistencias en aspectos como la alineación de campos (alfanumérico o numérico) y el tipo de inclusión, son revisados y detectados por el
sistema, pudiendo generar rechazos totales o parciales de archivos
```

## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

## ANEXO 2 – TABLAS ACLARATORIAS

## TABLA 1 - CAUSAL DE DEVOLUCIÓN POR OPERADOR ACH PARA TODOS LOS SERVICIOS PPD, CCD Y CTX

```
Causal Descripción Estándar
```
**R01**

```
Fondos Insuficientes: La transacción no puede ser liquidada debido a que la cuenta de depósito del
Participante en el Banco de la República no tiene fondos suficientes.
```
**R34**

```
Cancelación Manual: La transacción no fue liquidada debido a que el Participante Originador, la canceló
manualmente por decisión propia.
```
## NOTA: Siempre que se generen devoluciones por Operador ACH debido a causales R01 – Fondos Insuficientes

## y R04 – Cancelación Manual , las transacciones devueltas serán notificadas mediante el archivo de salida de

## la sesión correspondiente, en un primer lote de transacciones originado por el sistema CENIT, con destino a

## la Entidad Participante Originadora.

## La siguiente tabla incluye una descripción estándar de los errores más frecuentes que se les presentan a los

## Participantes en el envío de archivos al sistema CENIT.

### TABLA 2 - CAUSALES DE RECHAZO DE ARCHIVOS POR OPERADOR ACH

```
Código de Error ACH Descripción del Error Solución
```
###### ERR_DUP_FILE

```
El archivo está duplicado: remitente = {0},
identificador de archivo = {1}, fecha de creación
del archivo = {2}
```
```
Revisar la secuencia del último archivo enviado durante el día
de operación del sistema. Y colocar la siguiente secuencia
tanto en el nombre del archivo como en el modificador de
identificador de archivo.
```
###### ERR_FILENAME_SENDER

```
Error en el nombre del archivo - Código de
Remitente erróneo.
```
```
Renombrar el archivo, si el campo de Ruta/Tránsito no
corresponde con el campo 4 del registro de encabezado de
archivo, siendo este campo correcto en su contenido.
```
###### ERR_GW_SENDER

```
El MENSAJE no fue enviado usando el Gateway
del Remitente (GW: {Código Swift del Gateway
que envía el archivo}, SWIFT del Remitente:
{Código Swift del origen inmediato})
```
```
* Modificar el campo 4 del registro de encabezado de archivo y
el de Ruta/Tránsito en el nombre del archivo, si éstos no
corresponden con el código de Ruta y Tránsito del banco
originador de las transacciones.
* Verificar el nombre del archivo de acuerdo al siguiente
formato: RRRRTTT.SSS.N. Donde R corresponde a la Ruta, T al
Tránsito, S a la secuencia del archivo, N al código del sistema.
* Un usuario autorizado en el sistema debe enviar el archivo.
```
###### ERR_FILENAME_MODIFIER

###### _MATCH

```
El número consecutivo del nombre de archivo
no coincide con el Identificador de Archivo
```
```
* Renombrar el archivo, si el campo de Ruta/Tránsito no
corresponde con el campo 4 del registro de encabezado de
archivo, siendo este campo correcto en su contenido.
* Modificar el campo 4 del registro de encabezado de archivo y
el de Ruta/Tránsito en el nombre del archivo, si éstos no
corresponden con el código de Ruta y Tránsito del banco
originador de las transacciones.
```
###### ERR_INV_DATE_TIME

```
La fecha u hora es inválida - {Nombre de
campo}
```
```
Nombre de archivo no es una fecha
Fecha de creación del mensaje > Fecha de creación del archivo
Valor fecha no igual a la fecha de compensación
```

## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

## TABLA 2 - CAUSALES DE RECHAZO DE ARCHIVOS POR OPERADOR ACH

```
Código de Error ACH Descripción del Error Solución
```
```
Fecha de creación en el futuro
La fecha de creación es demasiado antigua
```
```
Cambiar la fecha en el campo 5 del registro de encabezado de
archivo y en los campos 8 y 9 del registro de encabezado de
lote según el formato AAAAMMDD. Donde A se refiere al año,
M al mes y D al día.
```
###### ERR_ITEMS_REF_NOT_CON

###### SECUTIVE

```
Números de secuencia de ítems en el lote no
son consecutivos
```
```
Revisar las secuencias de las transacciones dentro y entre el
archivo a enviar.
```
###### ERR_INV_REC_CODE

```
Código de banco de receptor inválido (code =
?) en Registro de Detalle
```
```
Verificar que el código de Ruta y Tránsito en los campos 3 y 12
del registro de entrada tipo 6 son válidos dentro del sistema.
```
```
ERR_ITEMS_REF_NOT_ASC
ENDING
```
```
Los números de secuencia de los ítems en el
archivo no están en orden ascendente o algún
número de secuencia ya existe en el ACH
```
```
Validar el archivo.
```
```
Revisar las secuencias de las transacciones entre los archivos.
```
```
ERR_RC26 Error en el campo mandatorio {nombre de
campo}: {descripción de error}
```
```
Validar en el archivo, el campo relacionado en el mensaje de
error.
```
###### ERR_ADDENDA_TRACE_MI

###### SSMATCH

```
Núm. secuencia de adenda no concuerda con
los últimos 7 dígitos del núm. secuencia del
registro de detalle
```
```
Revisar las secuencias de registros de transacciones y
secuencias de registros de adendas.
Verificar que el código de adenda y tipo de transacción o el tipo
de devolución son cálidos dentro del sistema y
correspondientes uno con el otro.
```
###### ERR_INV_PATTERN

```
El valor "{valor del campo}" no coincide con el
patrón {numérico, alfanumérico, blancos} en el
campo {nombre del campo} para el tipo de
registro {código de tipo de registro}
```
```
Revisar el archivo manualmente: buscar caracteres no
permitidos como vocales tildadas, tildes, apóstrofes, un
tabulador o un carácter no numérico en un campo para
caracteres numéricos.
```
```
ERR_FILE_MAX_ERR_NO_E
XCEEDED
```
```
El número de errores del archivo exceden el
máximo número de errores permitidos en el
archivo
```
```
Revisar el archivo contra la relación de errores encontrados en
el archivo enviado.
```
```
ERR_MAX_PAY_VAL_EXCEE
DED
```
```
Error en el campo mandatorio Monto: Valor
máximo excedido
```
```
Verificar con el operador del sistema el valor del Monto
Máximo permitido.
```
###### ERR_ITEM_AMOUNT

```
Error en el campo monto - Monto cero no es
permitido
```
```
Validar el archivo – Monto debe ser mayor que cero
Comúnmente los pagos que no sean prenotificaciones deben
tener un monto diferente a cero.
```
```
ERR_MIXED_FILE El archivo contiene ambos CTXs y PPDs/CCDs
```
```
Se deben enviar archivos independientes cuando se originan
transacción CTX, no se puede enviar en un mismo archivo
transacciones CTX con otro tipo de servicio.
```
###### ERR_BATCH_NO_NOT_EQU

###### AL

```
Núm. de lotes en control de archivo (9) no es
igual al núm. de lotes en el archivo (1)
```
```
Validar el archivo.
```
```
El número de lotes es superior o inferior al que se encuentra en
el campo 2 del registro de control de archivo.
```

## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

## TABLA 2 - CAUSALES DE RECHAZO DE ARCHIVOS POR OPERADOR ACH

```
Código de Error ACH Descripción del Error Solución
```
###### ERR_RC28

```
Dígito de control inválido para {código de
participante} - recibido:{dígito recibido},
esperado:{dígito esperado}
```
```
Validar el archivo.
El campo 4 del registro de entrada de una transacción no
corresponde con el campo 5 (identificación del receptor) del
mismo.
```
###### ERR_RC25

```
Valor del indicador del registro de adenda es
incorrecto
```
```
Validar el archivo.
En el registro de entrada de una transacción se especifica la
inclusión
de un registro de adenda (en el campo 11), pero éste no se
encuentra en el archivo.
```
```
ERR_RECORD_MISSING {Nombre de registro} no se ha encontrado
```
```
Validar el archivo.
Cuando se espera un registro consecutivo a otro. Por ejemplo,
si no se coloca el registro de control de archivo al final de este.
```
###### ERR_INV_FLD_VAL_EXPECT

###### ED

```
El valor del campo Número de Bloques es
inválido - recibido: {valor recibido}, esperado:
{valor esperado}
```
```
Validar el archivo.
El número de bloques es superior o inferior al que se encuentra
en el campo 3 del registro de control de archivo.
El valor del campo Número de Registros de
Detalle y Adendas es inválido - recibido: {valor
recibido}, esperado: {valor esperado}
```
```
Validar el archivo.
El número de registros de adenda es superior o inferior al que
se encuentra en el campo 4 del registro de control de archivo.
```
```
El valor del campo Hash Totales de Control es
inválido - recibido: {valor recibido}, esperado:
{valor esperado}
```
```
Validar el archivo.
La suma de los valores del campo de identificación del receptor
es diferente al valor del campo 5 del registro de control de
archivo.
El valor del campo Número de Bloques es
inválido - recibido: {valor recibido}, esperado:
{valor esperado}
```
```
Validar el archivo.
E l número de bloques es superior o inferior al que se encuentra
en el campo 3 del registro de control de archivo.
El valor del campo Número de Registros de
Detalle y Adendas es inválido - recibido: {valor
recibido}, esperado: {valor esperado}
```
```
El número de registros de adenda es superior o inferior al que
se encuentra en el campo 4 del registro de control de archivo.
```
```
El valor del campo Hash Totales de Control es
inválido - recibido: {valor recibido}, esperado:
{valor esperado}
```
```
Validar el archivo
La suma de los valores del campo de identificación del receptor
es diferente al valor del campo 5 del registro de control de
archivo.
```
```
El valor del campo Hash Totales de Control es
inválido - recibido: {valor recibido}, esperado:
{valor esperado}
```
```
Validar el archivo.
La suma de los valores del campo de identificación del receptor
es diferente al valor del campo 4 del registro de control de
archivo.
```
###### ERR_INV_FLD_VAL_EXPECT

###### ED

```
El valor del campo Valor Total de Débitos es
inválido - recibido: {valor recibido}, esperado:
{valor esperado}
```
```
Validar el archivo
La suma de las cantidades débito incluidas en las transacciones
del lote es diferente a la contenida en el campo 5.
El valor del campo Valor Total de Créditos es
inválido - recibido: {valor recibido}, esperado:
{valor esperado}
```
```
La suma de las cantidades crédito incluidas en las transacciones
del lote es diferente a la contenida en el campo 6.
```
```
El valor del campo Valor Total de Débitos es
inválido - recibido: {valor recibido}, esperado:
{valor esperado}
```
```
Validar el archivo
La suma de las cantidades débito incluidas en las transacciones
del lote es diferente a la contenida en el campo 6.
El valor del campo Valor Total de Créditos es
inválido - recibido: {valor recibido}, esperado:
{valor esperado}
```
```
Validar el archivo
La suma de las cantidades crédito incluidas en las transacciones
del lote es diferente a la contenida en el campo 7.
```

## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

## TABLA 2 - CAUSALES DE RECHAZO DE ARCHIVOS POR OPERADOR ACH

```
Código de Error ACH Descripción del Error Solución
```
###### ERR_RC13

```
Código de Banco Remitente inválido (code =
{Código de Remitente}) en Registro de
Cabecera de Lote
```
```
Validar el archivo.
código contenido en el campo de origen inmediato en el
registro de encabezado de archivo no corresponde con el que
se encuentra en el campo 12 del registro de encabezado de
lote. Por ejemplo: Código de Banco Remitente inválido (código
= 00001001) en Cabecera de Archivo.
```
###### ERR_NOT_EQUAL

```
Código de Clase de Transacciones en Registro
de Control de Lote no es igual a Código de Clase
de Transacciones en Registro de Cabecera de
Lote
```
```
Revisar manualmente los códigos de clase de servicio de los
registros de encabezado y de control del/los lotes.
El campo de código de clase de servicio en el registro de
encabezado de lote no corresponde con el campo 2 del registro
de control de lote
```
```
ERR_TRACE_NO_INV Número de secuencia inválido
```
```
Revisar el campo del número de secuencia del registro de
entrada tipo 6 respecto al campo 12 del registro de encabezado
de lote.
Los números entre paréntesis, por ejemplo {1} indican que en su lugar el sistema colocará el valor adecuado de acuerdo al contexto del error
que está lanzando. Por ejemplo: En el error ERR_INV_FLD_VAL_EXPECTED, el error podría mostrarse como " El valor del campo Hash Totales
de Control es inválido - recibido: 0000002005, esperado: 0000002004".
Siempre que el mensaje informe la posición donde el sistema identificó el error, el participante deberá revisar el archivo de entrada usando
una herramienta que le permite identificar la posición que reporta el archivo XML (Por ejemplo Notepad++ u otros)
```

## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

## La siguiente tabla incluye una descripción estándar de otro tipo de errores que se le pueden presentar a los

## Participantes en el envío de archivos al sistema CENIT

### TABLA 3 - OTRAS CAUSALES DE RECHAZO DE ARCHIVOS POR OPERADOR ACH

```
Código de Error ACH Descripción del Error Solución
```
```
ERR_ADDENDA_ORIG_TRACE
_INV
```
```
Número secuencia de registro origen {0} es
inválido para adenda con núm. de secuencia
{1}
```
```
En caso de devoluciones cuando el número de
secuencia del registro original (Campo 4 de
adenda) no existe o no es válido
ERR_ADDENDA_SEQ_NO_IN
V
```
```
Núm. secuencia de adenda {0} es inválido El número de secuencia de adenda es inválido
```
```
ERR_ADDENDA_SEQ_NO_NO
T_ASCENDING
```
```
Los números de secuencia de las adendas no
están en orden ascendente: {0} - {1}
```
```
Los números de secuencia de las adendas no
están en orden ascendente
```
```
ERR_ADDENDA_TRACE_MIS
MATCH
```
```
Número secuencia de adenda no concuerda
con los últimos 7 dígitos del núm. secuencia
del registro de detalle
```
```
Número secuencia de adenda no concuerda
con los últimos 7 dígitos del núm. secuencia del
registro de detalle
```
```
ERR_ALL_ITEMS_IN_BATCH_I
NVALID
```
```
No hay ítems válidos en el lote No hay ítems válidos en el lote
```
```
ERR_ALL_ZEROES El campo {0} tiene sólo ceros
```
```
El campo señalado contiene sólo ceros. Validar
el tipo de campo e información de este.
```
```
ERR_BATCH_FATAL_ERR
```
```
Se ha encontrado un error fatal. El lote será
rechazado
```
```
Se ha encontrado un error fatal. El lote será
rechazado
ERR_BATCH_ID_NOT_ASC Número de lote no está en orden ascendente Número de lote no está en orden ascendente
ERR_BATCH_ORDER Orden de lote inválido Orden de lote inválido
ERR_BATCH_REJ El lote fue rechazado El lote fue rechazado
```
```
ERR_CER_NOT_RETRIEVED
```
```
No se obtuvo el certificado como resultado de
la verificación de firma en el servicio PKI
```
```
El servicio de PKI no respondió
satisfactoriamente a la validación de firma del
archivo por lo que el sistema rechaza dicho
archivo
```
```
ERR_CREATION_TIME_INV Hora de creación es inválida
```
```
Aplica para campo 6, registro tipo 1. Sin
embargo, el campo es de inclusión Opcional, el
sistema valida que esté presente pero no
contenido
```
```
ERR_DECRYPT_VERIFY_SIGN
ATURE
```
```
Error en el proceso de descripción y
verificación de firma
```
```
Se utiliza cuando sucede un problema de
comunicación con la interface de PKI o existe un
problema técnico con la aplicación al momento
de desencriptar y validar la firma de un archivo
```
```
ERR_EMPTY_FLD El campo {0} es nulo o vacío
```
```
El campo señalado es nulo o vacío. Validar el
tipo de campo e información de este.
```
```
ERR_ENTRY_CLASS_CODE Código Estándar de Clase de Entrada {0} {1}
```
```
Cuando el campo 2 del registro tipo 5 no
corresponde a la información de la Tabla No. 8
```
```
ERR_ENTRY_TRAN_CODE Código de Tran. de registro {0} inválido
```
```
Cuando el campo 2 del registro tipo 6 no
corresponde a la información de la Tabla No. 6
```
```
ERR_FILENAME_EXTENSION
```
```
El nombre de archivo no tiene la extensión
requerida
```
```
El nombre de archivo no tiene la extensión
requerida
```

## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

## TABLA 3 - OTRAS CAUSALES DE RECHAZO DE ARCHIVOS POR OPERADOR ACH

```
Código de Error ACH Descripción del Error Solución
```
```
ERR_FILENAME_FORMAT
```
```
El formato del nombre de archivo no coincide
con el formato requerido
```
```
El formato del nombre de archivo no coincide
con el formato requerido
```
```
ERR_FILENAME_MODIFIER
```
```
Formato del número consecutivo del nombre
de archivo es incorrecto
```
```
Formato del número consecutivo del nombre
de archivo es incorrecto
```
```
ERR_FILENAME_SYSTEM
```
```
Error en el nombre del archivo - Id de Sistema
erróneo
```
```
Error en el nombre del archivo - Id de Sistema
erróneo (Para CENIT es 1)
```
```
ERR_GENERIC {0}
```
```
Esta causal se utiliza para errores inesperados
en escenarios excepcionales.
```
```
ERR_INNACTIVE_REC Participante Receptor no está activo
```
```
Participante Receptor no está activa en el
sistema
```
```
ERR_INNACTIVE_SND Participante Remitente no está activo
```
```
Participante Originador no está activa en el
sistema
ERR_INV_ADDENDA_NO Número de registros de adenda inválido Número de registros de adenda inválido
ERR_INV_BUS_DATE La fecha hábil es inválida La fecha hábil es inválida
```
```
ERR_INV_CER
```
```
Certificado no autorizado para firmar: DN
[{0}]; sistema= {1}; NIT = {2}; cédula = {3}
```
```
El certificado usado para firmar el archivo no se
encuentra autorizado/activo
```
```
ERR_INV_DEATH_DATE
```
```
Fecha de muerte inválida en adenda con
número de secuencia {0}
```
```
Fecha de muerte inválida en la adenda señalada
```
```
ERR_INV_FILE_LENGTH
```
```
Longitud del archivo inválida [{0}]: no es un
multiplicador exacto del tamaño de bloque
[{1}]
```
```
Longitud de archivo no corresponde con
cantidad de bloques de este
```
```
ERR_INV_FILE_REF
```
```
El código de referencia en la cabecera del
archivo es nulo o vacío
```
```
El código de referencia en campo 3 del registro
tipo 1, es nulo o vacío
```
```
ERR_INV_FILE_REF_CENIT
```
```
El código de referencia en la cabecera del
archivo debería ser 1bbbbbbb
```
```
El código de referencia en campo 3 del registro
tipo 1, debería ser 1bbbbbbb
```
```
ERR_INV_FLD_LENGTH La longitud del campo {0} es inválida
```
```
La longitud del campo señalado es inválida.
Validar tipo del campo
```
```
ERR_INV_FLD_PARSER
```
```
El valor del campo {0} es inválido en la
posición {1} del archivo. El valor {2} no se
admite en el sistema.
```
```
Validar el dato y tipo de campo señalado.
```
```
ERR_INV_FLD_VAL Valor del campo inválido: {0} Validar el dato y tipo de campo señalado.
```
```
ERR_INV_FLD_VAL_NOT_ALL
OWED
```
```
El valor del campo {0} es inválido. El valor {1}
no se admite en el sistema
```
```
Validar el dato y tipo de campo señalado.
```
```
ERR_INV_JUSTIFICATION (*)
```
```
El campo {0} en el tipo de registro {1} no está
justificado a la {2}
```
```
Aplica para cualquier campo que no cumpla con
la justificación requerida:
•Alfanumérico: Justificado a la izquierda con
espacios a la derecha
•Numérico: Justificado a la derecha, sin signo y
con ceros a la izquierda
```

## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

## TABLA 3 - OTRAS CAUSALES DE RECHAZO DE ARCHIVOS POR OPERADOR ACH

```
Código de Error ACH Descripción del Error Solución
ERR_INV_NUMBER Número inválido en campo {0} Validar el dato y tipo de campo señalado.
```
```
ERR_INV_PADDING Relleno no válido: {0}, posición actual: {1}
```
```
"9" es el único caracter válido como relleno del
archivo para completar un bloque. Si se
encuentra un caracter diferente, éste se
muestra en el parámetro {0} y la posición donde
fue encontrado {1} del mensaje.
```
```
ERR_INV_PAR_NIT
```
```
No se encontró un participante con el NIT {0}
o no está activo (NIT obtenido del certificado
de la firma)
```
```
El NIT que se obtiene del certificado de firma no
corresponde a ningún participante registrado
en el ACH o no se encuentra activo
```
```
ERR_INV_PRIORITY_CODE El código de prioridad debe ser 01
```
```
Aplica para el Campo 2 del registro tipo 1.
Validar el dato y tipo de campo.
```
```
RR_INV_PROD_MAX_DATE
```
```
Fecha mayor a la fecha futuro-máxima del
producto
```
```
Aplica para el campo "Fecha Efectiva de la
Transacción" - campo 9, registro tipo 5
```
```
ERR_INV_PROD_MIN_DATE
```
```
Fecha menor a la fecha futuro-mínima del
producto
```
```
Aplica para el campo "Fecha Efectiva de la
Transacción" - campo 9, registro tipo 5
```
```
ERR_INV_REC_CODE Código de Banco Receptor inválido
```
```
Código del Participante Receptor no es válido
para el sistema.
```
```
ERR_INV_RECORD
```
```
Se ha detectado un tipo de registro inválido
("{0}") en la posición {1} del archivo
```
```
Validar tipo de registro señalado.
```
```
ERR_INV_RECORD_LENGTH
```
```
Longitud de registro inválido {0} para tipo de
registro {1}
```
```
Validar tipo de registro señalado.
```
```
ERR_INV_RECORD_TYPE
```
```
Tipo de registro inválido en {0} - recibido: {1},
esperado: {2}
```
```
Validar tipo de registro señalado.
```
```
ERR_MIXED_ITEMS_IN_BATC
H
```
```
Ítems mixtos en lote: DIRECTOS - {0},
DEVOLUCIÓN - {1}, DEVOLUCIÓN DE
DEVOLUCIÓN - {2}
```
```
El lote de transacciones contiene diferentes
tipos de transacciones.
```
```
ERR_NO_CER
```
```
Los certificados del firmante no pudieron ser
obtenidos
```
```
El certificado usado para firmar el archivo no
pudo ser validado.
```
```
ERR_NO_DESCRIPTOR
```
```
No se encontraron campos descriptivos para
el tipo de registro {0}
```
```
Este error suele presentarse cuando la ACH no
estuvo en capacidad de obtener el contenido
de un registro al momento de realizar el análisis
campo a campo.
El parámetro {0} indica el tipo de campo
analizado.
Revisar que los registros del archivo cumplan
con la estructura planteada por el estándar y el
tipo de dato de cada campo sea el adecuado.
```
```
ERR_NO_ENTRIES No se encontraron registros en el archivo
```
```
No se encontraron registros válidos en el
archivo. Revisar construcción del archivo.
ERR_NO_PAYMENTS_IN_FILE El archivo no contiene pagos El archivo no contiene pagos
```

## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

## TABLA 3 - OTRAS CAUSALES DE RECHAZO DE ARCHIVOS POR OPERADOR ACH

```
Código de Error ACH Descripción del Error Solución
```
```
ERR_NO_PRODUCT No se ha encontrado el producto para {0}
```
```
El tipo de producto (tipo de transacción)
señalado en la transacción, no existe en la ACH
ERR_NO_RECORDS_IN_FILE No hay registros en el archivo No hay registros en el archivo
ERR_NO_SIG El mensaje no fue firmado El mensaje no fue firmado
ERR_NON_VALUE_AMOUNT Monto debe ser cero Monto debe ser cero
```
```
ERR_ON_US_ENTRY
```
```
Ítem con el mismo remitente y receptor no se
permite en el ACH
```
```
Los campos relacionados con Originador y
Receptor no pueden corresponder al mismo
código del Participante
```
```
ERR_INV_SIG Firma inválida
```
```
El sistema no pudo desencriptar y verificar la
firma del archivo para obtener el contenido.
```
```
ERR_INV_SIG_NO
```
```
Número invalido de firmas - esperado = {0};
recibido = {1}
```
```
El archivo no fue firmado con el o los
certificados requeridos.
```
```
ERR_INV_SIGNING_USER_PA
R
```
```
Participante del usuario que firma [{0}] y
participante del usuario que envía [{1}] no son
los mismos
```
```
Se presenta cuando el archivo es firmado por
un usuario cuyo Participante no es el mismo
que el Participante del usuario que ejecuta el
gateway para enviar el archivo en cuestión.
```
```
ERR_INV_SND_CODE Código de Banco Remitente inválido
```
```
Código del Participante Originador no es válido
para el sistema.
```
```
ERR_INV_USER_SSN
```
```
No se encontró un usuario con cédula {0} o no
está activo
```
```
Cuando se realiza la validación del certificado
de la firma se verifica que la cédula del firmante
corresponda a un usuario registrado en ACH
```
```
ERR_JULIAN_DATE_RANGE
```
```
La fecha de Compensación Juliana debe estar
entre [1-366]
```
```
La fecha de Compensación Juliana no
corresponde a un valor entre 1 y 366
```
```
ERR_MISSMATCH_CER
```
```
El mensaje no fue firmado con el certificado
del remitente
```
```
El certificado usado para firmar el archivo no
pertenece al Participante Originador
```
```
ERR_MIXED_ITEMS_IN_BATC
H
```
```
Ítems mixtos en lote: DIRECTOS - {0},
DEVOLUCIÓN - {1}, DEVOLUCIÓN DE
DEVOLUCIÓN - {2}
```
```
El lote de transacciones contiene diferentes
tipos de transacciones.
```
```
ERR_NO_CER
```
```
Los certificados del firmante no pudieron ser
obtenidos
```
```
El certificado usado para firmar el archivo no
pudo ser validado.
```
```
ERR_NO_DESCRIPTOR
```
```
No se encontraron campos descriptivos para
el tipo de registro {0}
```
```
Este error suele presentarse cuando la ACH no
estuvo en capacidad de obtener el contenido
de un registro al momento de realizar el análisis
campo a campo.
El parámetro {0} indica el tipo de campo
analizado.
Revisar que los registros del archivo cumplan
con la estructura planteada por el estándar y el
tipo de dato de cada campo sea el adecuado.
```
```
ERR_NO_ENTRIES No se encontraron registros en el archivo
```
```
No se encontraron registros válidos en el
archivo. Revisar construcción del archivo.
```

## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

## TABLA 3 - OTRAS CAUSALES DE RECHAZO DE ARCHIVOS POR OPERADOR ACH

```
Código de Error ACH Descripción del Error Solución
ERR_NO_PAYMENTS_IN_FILE El archivo no contiene pagos El archivo no contiene pagos
```
```
ERR_NO_PRODUCT No se ha encontrado el producto para {0}
```
```
El tipo de producto (tipo de transacción)
señalado en la transacción, no existe en la ACH
```
```
ERR_NO_RECORDS_IN_FILE No hay registros en el archivo No hay registros en el archivo
```
```
ERR_NO_SIG El mensaje no fue firmado El mensaje no fue firmado
ERR_NON_VALUE_AMOUNT Monto debe ser cero Monto debe ser cero
```
```
ERR_ON_US_ENTRY
```
```
Ítem con el mismo remitente y receptor no se
permite en el ACH
```
```
Los campos relacionados con Originador y
Receptor no pueden corresponder al mismo
código del Participante
```
```
ERR_ORIG_ITEM_MISMATCH
```
```
El ítem de devolución hace referencia a un
ítem original equivocado
```
```
El ítem de devolución hace referencia a un ítem
original equivocado
```
```
ERR_ORIG_ITEM_NOT_FOUN
D
```
```
El ítem referenciado no ha sido encontrado
(ref. = {0})
```
```
Este error se presentará cuando:
Devolución: el sistema no encuentra
coincidencia exacta entre la transacción
original y la devolución en el día de
compensación.
Devolución de Devolución: el sistema no
encuentra coincidencia exacta entre la
devolución original y la devolución de
devolución en el día de compensación abierto y
el día inmediatamente anterior.
La búsqueda se realizará mediante los campos:
```
- Participante Originador de la
transacción/devolución original.
- Participante Receptor de la
transacción/devolución original.
- Número de Secuencia de la
transacción/devolución original.
- Fecha efectiva de la transacción/devolución
original.
- Valor de la transacción/devolución original.
La transacción/devolución original debe haber
sido aceptada, compensada y liquidada en un
ciclo del sistema.
Este error puede asociarse a la causal de
rechazo ERR_RC19 cuando se trata de una
transacción de devolución de prenotificación a
la cual no se le encuentra la prenotificación
original.
ERR_PKI_MANAGER_NOT_F
OUND

```
¡No se pudo obtener el administrador de PKI
para validar la seguridad del certificado!
```
```
Este error ocurre cuando existe un problema
interno de la aplicación.
```

## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

## TABLA 3 - OTRAS CAUSALES DE RECHAZO DE ARCHIVOS POR OPERADOR ACH

```
Código de Error ACH Descripción del Error Solución
```
```
ERR_PROD_NOT_ALLOWED
```
```
Participante {0} no tiene permitido
enviar/recibir pagos para el producto {1}
```
```
El Participante no tiene permitido
enviar/recibir pagos para el producto señalado
```
```
ERR_PROD_NOT_ALLOWED_
RECEIVE
```
```
Participante {0} no tiene permitido recibir
pagos para el producto {1}
```
```
El Participante Receptor no tiene permitido
recibir pagos para el producto señalado
```
```
ERR_PROD_NOT_ALLOWED_
SEND
```
```
Participante {0} no tiene permitido enviar
pagos para el producto {1}
```
```
El Participante Originador no tiene permitido
enviar pagos para el producto señalado
```
```
ERR_RC18 Fecha efectiva de registro incorrecta - {0}
```
```
Fecha efectiva de registro incorrecta - Valor
inválido
```
```
ERR_RC19 Error en el campo monto - {0}
```
```
Se utiliza junto con los errores
ERR_NON_VALUE_AMOUNT o
ERR_ITEM_AMOUNT o
ERR_ORIG_ITEM_NOT_FOUND, y se presenta
si:
```
- Se está enviando una transacción monetaria
con valor cero
- Se está enviando una prenotificación con
valor diferente de cero
- Se está enviando una devolución monetaria o
de prenotificación, cuyo valor no corresponde a
un ítem original.
ERR_RC24 Registro duplicado Registro duplicado

```
ERR_RC26 Error en el campo mandatorio {0}: {1}
```
```
Error en el campo mandatorio señalado {0},
error {1}
```
```
ERR_RC28
```
```
Dígito de control inválido para {0} - recibido:
{1}, esperado: {2}
```
```
Dígito de control inválido para {0} - recibido: {1},
esperado: {2}
```
```
ERR_RECORD_MISSING {0} no se ha encontrado
```
```
No se encontró algún registro esperado, por
ejemplo: Registro de Control de Archivo no se
ha encontrado
```
```
ERR_RET_CODE_TRAN_TYPE
```
```
Se ha recibido una adenda con un código de
razón de devolución para un tipo de pago que
no es devolución
```
```
El código de causal de devolución o devolución
de devolución, de la adenda (campo 3 de
registro 7) y/o el código de la transacción
(campo 2 de registro 6) no corresponden a un
código de devolución o devolución de
devolución.
```
```
ERR_SETTL_DATE_INV
```
```
Fecha de liquidación inválida (juliana) - valor
recibido: {0}
```
```
Fecha de liquidación inválida (juliana) es
inválida
```
```
ERR_SETTL_DATE_NULL Fecha de liquidación (juliana) faltante Fecha de liquidación (juliana) no fue incluida.
```
```
ERR_TRACE_NO_SEQ
```
```
Número de secuencia no está en orden
ascendente
```
```
Los números de secuencia de los registros no
están en orden ascendente
```

## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

## TABLA 3 - OTRAS CAUSALES DE RECHAZO DE ARCHIVOS POR OPERADOR ACH

```
Código de Error ACH Descripción del Error Solución
```
```
ERR_TRAN_CODE_MISMATC
H
```
```
Código de transacción no concuerda - Ítem:
{0}, lote: {1}
```
```
Cuando el código del campo 2 del registro tipo
6 no corresponde el tipo de transacción del lote
```
```
ERR_VALUE_DATE_INV Valor de fecha efectiva inválido Valor de fecha efectiva inválido
```
```
ERR_VALUE_DATE_NULL Falta la fecha efectiva Valor de la fecha efectiva vacío o nulo
```
```
ERR_INV_PAR_NIT
```
```
No se encontró un participante con el NIT {0}
o no está activo (NIT obtenido del certificado
de la firma)
```
```
El NIT que se obtiene del certificado de firma no
corresponde a ningún participante registrado
en el ACH o no se encuentra activo
```
```
ERR_NO_MORE_SESSIONS_F
OR_ITEM
```
```
Pago no aceptado en la sesión actual o en
sesiones posteriores
```
```
El tipo de producto (Tabla No. 6 - Códigos de
Transacción) ) contenido en la transacción
enviada, no puede ser procesado en la sesión
actual ni en sesiones posteriores a la misma
(p.e. envío de transferencias crédito en la
última sesión exclusiva para devoluciones)
```
```
ERR_ADDENDA_PPD_CT_NO
T_PRESENT
```
```
Error en el campo 'Indicador de Registro de
Adenda'. La adenda es obligatoria para
registros PPD de crédito
```
```
No se ha incluido 1 adenda en un ítem PPD de
crédito
```
```
ERR_INV_ADDENDA_NO_PP
D_CT
```
```
Número de registros de adenda inválido para
registros PPD de crédito. 1 adenda es
obligatoria
```
```
No se ha incluido 1 adenda en un ítem PPD de
crédito
```
```
Los números entre paréntesis, por ejemplo {1} indican que en su lugar el sistema colocará el valor adecuado de acuerdo al
contexto del error que está lanzando.
Por ejemplo: En el error ERR_TRACE_NO_LIMIT_MAX, el error podría mostrarse como "El número de secuencia 90002
excedió el número máximo permitido en el sistema 90000"
Siempre que el mensaje informe la posición donde el sistema identificó el error, el Participante deberá revisar el archivo de
entrada usando una herramienta que le permite identificar la posición que reporta el archivo XML (Por ejemplo Notepad++ u
otros)
(*)El sistema se encuentra en capacidad de validar el cumplimiento de la reglamentación en cada uno de los tipos de registros
y campos de los archivos. Específicamente, inconsistencias en aspectos como la alineación de campos (alfanumérico o
numérico) son revisados y detectados por el sistema, generando rechazos totales o parciales de archivo
```

## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 7 de mayo de 2026

### TABLA 4 - DESCRIPCIÓN DE LOTE

**CAMPO 7 - DESCRIPCIÓN DE LOTE DEL REGISTRO TIPO 5**

**VALOR DEL CAMPO DESCRIPCIÓN DEL CAMPO**

**PAGOS**

```
Pagos a Proveedores, Pagos de Nómina, Pagos de Servicios
Públicos, Pagos Domiciliados, Pagos Banca Móvil, Pagos Banca
Virtual, Pagos Tarjeta de Crédito, Pagos Suscripciones, Pagos
de Matrículas, Pagos de Impuestos, Pagos de Viáticos, Pagos
Administración, Pagos de Seguro de Depósito, Desembolsos,
etc.
SSS Pago Sistema Seguridad Social
PRS Pago Sistema Seguridad Social –^ Pagos Régimen Subsidiado^
```
**RECAUDOS** Recaudos -^ Débito Interbancario^

**TRANSFER** Transferencias o traslados^

**DEVOLUCION** Devoluciones -^ Devolución de Devolución^

```
PRENOTIFIC Prenotificaciones -^ Prenota^
SUBSIDIOS Pagos de Subsidios
```

## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 10 de mayo de 2023

### TABLA 5 - TIPOS DE SERVICIO

**Tipo Descripción**

**PPD**

**Depósito Directo y Pago Preacordado**

```
Transacciones débito o crédito (diferentes a transacciones MTE o POS/SHR) iniciadas por un
Originador para efectuar recaudos o transferir fondos desde o hacia la cuenta de un Receptor. Esta
transacción contiene un (1) registro adenda.
```
**POS/SHR***

```
Transacción de Puntos de Venta/Redes Compartidas:
Estos servicios representan aplicaciones débito en puntos de venta en redes no compartidas (POS)
o en redes compartidas (SHR). Estas transacciones son frecuentemente iniciadas por Personas
Naturales por medio de una tarjeta plástica.
```
**MTE***

```
Transacción de Transferencias desde Máquinas:
Este servicio soporta transacciones débito y crédito originadas por Personas Naturales por medio
de cajeros automáticos o por medio de máquinas o equipos usados por los Originadores.
```
**CIE***

```
Transacción Iniciada por una Persona Natural:
Son transacciones crédito iniciadas por un tercero, con autorización previa Receptor (Persona
Natural) o iniciada directamente por el Receptor (Persona Natural). Un ejemplo de este servicio
son las transacciones iniciadas desde Internet. Contiene un registro adenda.
```
**POP***

```
Transacción de Punto de Compra:
Identifica transacciones débito iniciadas por un Originador con base en la autorización escrita y en
la información de la cuenta del Receptor obtenida de un documento fuente (como un cheque) dada
directamente en el Punto de Compra para efectuar la transferencia de fondos desde la Cuenta
Receptora hacia la Cuenta Originadora. Servicio usado únicamente para transacciones no
recurrentes “en persona” en el Punto de Compra, y para las que no existe una autorización de
débito automático permanente.
```
**PBR***

```
Pagos de Personas Naturales Entre Fronteras:
Transacciones crédito de Personas Naturales para transferir fondos hacia otros países o entre
fronteras. Permite a los Participantes identificar de manera ágil los pagos entre países que
requieran un manejo especial con información única referida al servicio como la tasa de
intercambio, moneda usada, códigos de países, etc.
```
**CCD**

```
Concentración y Dispersión de Fondos:
Transacciones débito o crédito iniciadas por Personas Jurídicas para concentrar o dispersar fondos
desde o hacia sus sucursales, franquicias o agencias o de otras organizaciones (Grupos
Económicos). Esta transacción contiene un (1) registro adenda.
```
**CTX**

```
Intercambio de Información Corporativa:
Transacciones débito o crédito iniciadas por Personas Jurídicas para transferir fondos hacia o desde
cuentas de otro Participante, consignando la información adicional sobre las transacciones
efectuadas.
Esta transacción puede contener hasta 9.999 Registros Adenda.
```
**CBR***

```
Pagos Corporativos Entre Fronteras:
Transacciones crédito de Personas Jurídicas para transferir fondos hacia otros países o entre
fronteras. Permite a los Participantes identificar de manera ágil los pagos entre países que
requieran un manejo especial con información única referida al servicio como la tasa de
intercambio, moneda usada, códigos de países, etc.
```

## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 10 de mayo de 2023

```
TABLA 5 - TIPOS DE SERVICIO
Tipo Descripción
```
**COR***

```
Notificación de Cambio
Identifica transacciones de Notificación de Cambio generadas por una Participante Receptor.
Requiere Registro Adenda.
```
**ENR***

```
Transacción de Enrolamiento Automático:
Permite a una Participante Receptor, notificar a los Originadores a través del sistema ACH, la
vinculación o enrolamiento de nuevos Receptores, para futuros créditos o débitos (p.e:
domiciliación). Esta transacción puede contener hasta 9.999 Registros Adenda.
```
**ACK***

```
Confirmación de un pago ACH:
Transacción de confirmación de una transacción Crédito CCD exitosa.
```
**ATX***

```
Confirmación de Intercambio de Información Financiera:
Transacción de confirmación de una transacción Crédito CTX exitosa.
```
**DNE***

```
Transacción de Notificación de Muerte:
Usada por entes del gobierno para notificar a una Participante Receptor que el Receptor de un
pago del gobierno, ha fallecido.
```
**ADV**

```
Aviso de Contabilidad Automática:
Servicio opcional utilizado por el sistema ACH para identificar avisos de contabilidad automáticos
usados para conciliación por los Participantes
```
**RET***

```
Transacción de Rechazo:
Servicio opcional utilizado por el sistema ACH para enviar rechazos generados a partir de medios
no automáticos.
* Este Servicio será desarrollado posteriormente.
```
## ^


## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 10 de mayo de 2023

### TABLA 7 - CÓDIGOS DE AVISOS DE CONTABILIDAD (ADV)

## TRANSACCIÓN

## Aviso Automático ADV

## Descripción

## 81 Operaciones débito originadas

## 82 Operaciones crédito originadas

## 83 Operaciones crédito recibidas

## 84 Operaciones débito recibidas

## 85 Movimiento crédito por mensajes rechazados

## 86 Movimiento débito por mensajes rechazados^

## 87 Neto a Cobrar (total créditos)

## 88 Neto a pagar (total débitos)

### TABLA 6 - CÓDIGOS DE TRANSACCIÓN

```
TIPO DE
SERVICIO
```
**TRANSACCIÓN**

**Crédito a Débito a**

```
CorrienteCuenta
AhorrosCuenta de
ContableCuenta
```
```
ElectrónicosDepósitos
CorrienteCuenta
AhorrosCuenta de
ContableCuenta
```
**ElectrónicosDepósitos**

```
PPD,CCD, CTX CRÉDITO 22 32 42 52
PPD,CCD, CTX PRENOTIFICACIÓN 23 33 43 53 28 38 48 57
PPD,CCD, CTX DEVOLUCIÓN 21 31 41 51 26 36 46 56
```
```
CCD, CTX (por
desarrollar)
```
```
Transacción crédito
de valor $0.00 con
envío de información
```
24 34 44 54 29 39 49 N/A

PPD,CCD, CTX DEBITO 27 37 47 55

##### TABLA 8 - CÓDIGOS DE CLASES DE TRANSACCIONES POR LOTE PARA TODOS LOS SERVICIOS

## Código Descripción^

## 200 Transacciones débito y crédito^

## 220 Transacciones crédito^

## 225 Transacciones débito^

## 280 Avisos de Contabilidad Automáticos ADV^


## MANUAL DE ESPECIFICACIONES FORMATO NACHA-MC

## ADMINISTRADOS POR EL BANCO DE LA REPÚBLICA

## Fecha: 10 de mayo de 2023

### TABLA 9 - CÓDIGOS DE TIPO DE REGISTRO ADENDA PARA LOS SERVICIOS PPD, CCD Y CTX

## DESCRIPCIÓN CÓDIGO

```
Transacciones crédito, débito y prenotificaciones crédito y débito 05
Transacciones de Devolución o de Devolución de una Devolución 99
```
### TABLA 11 - CANALES DE PAGO

## (ADENDA TRANSACCIONES CCD - SEGURIDAD SOCIAL)

## DESCRIPCIÓN CÓDIGO

## Por Ventanilla En Efectivo 01

## Por Ventanilla En Cheque 02

## Por Buzón De Autoservicio 03

## Débito En Cuenta Por Sistema De Audio Respuesta 11

## Débito En Cuenta Por Cajero Electrónico 12

## Débito En Cuenta Por Datáfono 13

## Débito En Cuenta Por Domiciliación 14

## Débito En Cuenta Por Internet 15

## Tarjeta Crédito Por Sistema De Audio Respuesta 21

## Tarjeta Crédito Por Cajero Electrónico 22

## Tarjeta Crédito Por Datáfono 23

## Tarjeta Crédito Por Domiciliación 24

## Tarjeta Crédito Por Internet 25

### TABLA 10 - CÓDIGO DE INDICADOR DE REGISTRO ADENDA PARA TODOS LOS SERVICIOS

## DESCRIPCIÓN Código

La transacción SI contiene Registro Adenda 1

La transacción NO contiene Registro Adenda 0


