### Anexo 2 Manual Operativo Sistema de Compensación Electrónica Nacional Interbancaria – CENIT

### Anexo A Causales de devolución servicio de Compensación y Liquidación del Sistema De Compensación Electrónica Nacional Interbancaria

**- CENIT**

### Hoja 1 – A2 – AA – 1

### Fecha: martes, 28 de noviembre de 2023

### Asunto 1: Reglamento del Sistema de Compensación Electrónica Nacional Interbancaria - CENIT

## TABLA 1 - CAUSALES DE DEVOLUCIÓN PARA LOS SERVICIOS PPD, CCD Y CTX

```
Causal
Débitos
```
```
Prenotificación
Débitos
Monetaria
Créditos
```
```
Prenotificación
MonetariaCréditos
Descripción Estándar de la Devolución Detalle adicional de la Devolución
(Opcional Recomendado)
```
```
R01 N/A SI N/A N/A Fondos Insuficientes: El saldo disponible no es suficiente para
cubrir el valor de la transacción débito.
```
```
Esta Causal puede ser utilizada por el operador de la ACH
para todos los servicios PPD CCD y CTX
R02 SI SI SI SI Cuenta Cerrada : Cuenta cerrada por orden del Receptor o por
el Participante Receptor que la abrió.
```
```
 Cuenta Saldada : Cuenta activa que ha sido cerrada por
orden del Receptor.
 Cuenta Cancelada: Cuenta activa que ha sido cerrada
por orden del Participante Receptor.
R03 SI SI SI SI Cuenta No Abierta: El número de Cuenta registrado no
corresponde a una Cuenta asignada o abierta.
Se deberá usar solo en caso de que la entidad haga
distribución de numeración de cuentas entre sus sucursales de
manera anticipada, de tal suerte que la cuenta existe en el
sistema, pero aún no ha sido asignada a ningún Usuario.
No deberá utilizarse esta causal en caso de que se esté
haciendo una verificación del número de Cuenta y esta no se
encuentre en el sistema. De igual forma, no debe utilizarse en
el caso de que una Cuenta esté en proceso de apertura o en
espera de documentación para su apertura definitiva.
```
```
La presente circular se firmó mediante la modalidad de Identidad Electrónica PKI o Certificado Digital. Si requiere validar la autenticidad e integridad de la
misma o consultar el documento firmado, diríjase al Departamento de Gestión Documental del Banco de la República a través de https://
http://www.banrep.gov.co/es/transparencia/atencionciudadano o del buzón de correo electrónico DGD-Correspondencia@banrep.gov.co
```

### Anexo 2 Manual Operativo Sistema de Compensación Electrónica Nacional Interbancaria – CENIT

### Anexo A Causales de devolución servicio de Compensación y Liquidación del Sistema De Compensación Electrónica Nacional Interbancaria

**- CENIT**

### Fecha: martes, 28 de noviembre de 2023

### Asunto 1: Reglamento del Sistema de Compensación Electrónica Nacional Interbancaria - CENIT

```
R04 SI SI SI SI Número de Cuenta Inválido : El número de la Cuenta es
incorrecto.
```
1. La estructura del número de Cuenta no es válida
2. El dígito de chequeo no es válido
3. Número incorrecto de dígitos
4. Número de Cuenta contiene caracteres no numéricos
5. El número de Cuenta no coincide con el tipo de cuenta
6. Cuenta no existe
**R06 N/A SI N/A SI Devolución solicitada por el Participante Originador o por el
Originador** : El Originador ha solicitado al Participante
Receptor devolver una transacción.
1. Por conocer que la transacción fue enviada por error
2. Por conocer que la Cuenta pertenece a la lista OFAC
(Oficina de Control de Activos Extranjeros de Estados
Unidos) o Lista Clinton.

```
De acuerdo con el momento en que sea presentada la
solicitud, se deberá tramitar la Devolución, de acuerdo con
los siguientes aspectos:
```
1. Envío dentro del ciclo de devoluciones más inmediato.
2. Sujeta a disponibilidad del dinero en la Cuenta receptora
    cuando la solicitud se haya efectuado en forma posterior
    al abono para las Entradas Crédito.
3. Aplicación solo por el valor original, no aplican
    devoluciones parciales.
4. Confirmación al Participante Originador de la aplicación
    de la causal.
Cuando no sea posible la aplicación de la Devolución
automática por parte del Participante Receptor, esta deberá
ser acordada entre los dos Participantes y tramitarse por
fuera del Sistema.
**R07 N/A SI N/A N/A Autorización de Recaudo Revocada por el Receptor** : El
Receptor ha revocado o cancelado en forma definitiva la
autorización previamente dada al Originador para debitar su
Cuenta en el futuro.


### Anexo 2 Manual Operativo Sistema de Compensación Electrónica Nacional Interbancaria – CENIT

### Anexo A Causales de devolución servicio de Compensación y Liquidación del Sistema De Compensación Electrónica Nacional Interbancaria

**- CENIT**

### Fecha: martes, 28 de noviembre de 2023

### Asunto 1: Reglamento del Sistema de Compensación Electrónica Nacional Interbancaria - CENIT

```
R08 N/A SI N/A N/A Orden de No Pago : El Receptor de una Entrada Débito
periódica ha dado orden de no pago a una Entrada Débito
específica para que no sea aplicada. El Participante Receptor
que mantiene la Cuenta debe verificar el propósito del
Receptor cuando hace una solicitud de orden de no pago, con
el fin de asegurarse de que no se trata de una revocación de
autorización (R07).
R09 N/A SI N/A N/A Fondos no Disponibles : El saldo total es suficiente para cubrir
esta transacción, sin embargo el saldo disponible no es
suficiente para cubrir la Entrada Débito.
R10 N/A SI N/A N/A No existe prenotificación : No fue encontrada la autorización o
acuerdo con el Receptor o no existe prenotificación para el
servicio por parte del Receptor relacionado.
R12 N/A SI N/A N/A Originador no autorizado : El Participante Receptor ha sido
notificado por el Receptor de que el Originador de la
transacción no ha sido autorizado para debitar su Cuenta.
R13 N/A SI N/A N/A Devolución de una Entrada Débito por solicitud del Receptor :
El Receptor no acepta la Entrada Débito a su Cuenta por una
razón específica.
```
```
Algunas razones para aceptar una Devolución solicitada por
el Receptor son:
 Monto no autorizado: El valor de la Entrada Débito no
corresponde al monto autorizado por el Receptor.
 Fecha de transacción errada: La fecha de la Entrada
Débito no corresponde a la fecha autorizada por el
Receptor.
 Débito duplicado: El Receptor notifica el recibo de una
Entrada Débito duplicada en su Cuenta
 Autorización de recaudo cancelada: El Receptor ha
cancelado previamente la autorización de recaudo.
```

### Anexo 2 Manual Operativo Sistema de Compensación Electrónica Nacional Interbancaria – CENIT

### Anexo A Causales de devolución servicio de Compensación y Liquidación del Sistema De Compensación Electrónica Nacional Interbancaria

**- CENIT**

### Fecha: martes, 28 de noviembre de 2023

### Asunto 1: Reglamento del Sistema de Compensación Electrónica Nacional Interbancaria - CENIT

```
R14 SI SI N/A N/A Muerte del Delegado o Representante : El delegado o
Representante (apoderado) del Receptor, sea este una
persona o una institución autorizada para recibir transacciones
en nombre de otras personas, ha muerto o ha perdido esa
facultad.
R15 SI SI N/A N/A Muerte del Beneficiario o Titular de la Cuenta : El beneficiario,
Receptor o Titular de la Cuenta ha muerto.
R16 SI
SI
SI
SI
Cuenta Inactiva o Cuenta Bloqueada :
Cuenta inactiva por no tener movimiento en un periodo de
tiempo.
```
```
Cuenta bloqueada por solicitud del Receptor o por el
Participante Receptor.
```
```
Cuenta Inactiva : Por no tener movimiento en un período
específico de tiempo.
Para las Entradas Crédito la inactividad aplica o no, de
acuerdo con la política interna de cada Participante,
dependiendo de si una transacción ACH produce o no la
activación de una Cuenta.
De igual forma, para las Entradas Crédito efectuadas a una
Cuenta cuyo saldo total ha sido trasladado a la DGCP después
de cumplido el período establecido de inactividad, esta se
reportaría como bloqueada y, para una cuenta inactiva, en
donde solo se ha trasladado parte del saldo, esta seguiría
siendo inactiva.
SI SI SI SI Cuenta Inactiva o Cuenta Bloqueada :
Cuenta inactiva por no tener movimiento en un periodo de
tiempo.
```
```
Cuenta bloqueada por solicitud del Receptor o por el
Participante Receptor.
```
```
Cuenta Bloqueada : Por solicitud del Receptor y/o
Participante Receptor.
Estos bloqueos aplican de acuerdo con el tipo de transacción
y se reportarán según corresponda a una Entrada Débito o
Crédito.
Esta causal de bloqueo aplica también para aquellas Entradas
Débito o Crédito cuando se sobrepase el monto límite
establecido para las mismas, situación que validará el
Participante Receptor.
```

### Anexo 2 Manual Operativo Sistema de Compensación Electrónica Nacional Interbancaria – CENIT

### Anexo A Causales de devolución servicio de Compensación y Liquidación del Sistema De Compensación Electrónica Nacional Interbancaria

**- CENIT**

### Fecha: martes, 28 de noviembre de 2023

### Asunto 1: Reglamento del Sistema de Compensación Electrónica Nacional Interbancaria - CENIT

```
SI SI N/A N/A Cuenta Inactiva o Cuenta Bloqueada :
Cuenta inactiva por no tener movimiento en un periodo de
tiempo.
Cuenta bloqueada por solicitud del Receptor o por el
Participante Receptor.
```
```
Bloqueo por cuenta embargada
```
```
R17 SI SI SI SI La Identificación no coincide con Cuenta del Receptor: La
estructura del número de Cuenta y el dígito de chequeo son
válidos, pero el número de Cuenta no corresponde con el
número de identificación registrado del Receptor.
R20 SI SI SI SI Cuenta No Habilitada para recibir transacciones :
Cuenta de naturaleza especial que está limitada para recibir
Entradas Débito o Crédito.
```
```
Cuenta marcada como de la Lista Clinton : La Cuenta
Receptora no está habilitada para recibir transacciones
porque es una cuenta marcada como de la Lista Clinton en el
Participante Receptor.
Cuenta usada en medios políticos: Si la Cuenta que se debe
afectar es usada en medios políticos como campañas o
similar podría usarse esta causal de devolución.
R23 N/A N/A N/A SI Devolución de una Entrada Crédito por solicitud del
Receptor:
La Entrada Crédito no es aceptada por el Receptor por no
cumplir con las condiciones pactadas.
```
```
 El valor mínimo solicitado por el Receptor no ha sido
enviado.
 El valor exacto solicitado por el Receptor no ha sido
enviado.
 La cuenta está en litigio y el Receptor no acepta la
Entrada Monetaria.
 La aceptación de la transacción origina un sobrepago.
 El Originador no es conocido por el Receptor.
 El Receptor no ha autorizado esta Entrada Crédito para
esta Cuenta.
Dado que obedece a una reclamación que puede presentar el
Receptor de una transacción hasta 15 días después de
```

### Anexo 2 Manual Operativo Sistema de Compensación Electrónica Nacional Interbancaria – CENIT

### Anexo A Causales de devolución servicio de Compensación y Liquidación del Sistema De Compensación Electrónica Nacional Interbancaria

**- CENIT**

### Fecha: martes, 28 de noviembre de 2023

### Asunto 1: Reglamento del Sistema de Compensación Electrónica Nacional Interbancaria - CENIT

```
realizada la operación original, se deben contemplar los
siguientes aspectos:
 El Participante Receptor deberá contar con un acuerdo y
un procedimiento con su Receptor para tramitar estas
reclamaciones.
 Oportunidad, de acuerdo con el momento en que sea
presentada la solicitud. Envío dentro del ciclo de
devoluciones más inmediato.
 Condiciones de aplicación de la Entrada Débito al
Receptor
 Aplicación solo por el valor original, no aplican
Devoluciones parciales.
Cuando no sea posible la aplicación de la Devolución
automática por parte del Participante Receptor, esta deberá
ser acordada entre los dos Participantes y tramitarse por
fuera del Sistema.
R29 SI SI N/A N/A Devolución de una Entrada Débito por solicitud del Receptor
(Persona Jurídica): El Participante Receptor ha sido notificado
por su Receptor Corporativo (no consumidor), de que el
Originador de la transacción no ha sido autorizado para
debitar su Cuenta.
R31 SI N/A N/A N/A Prenotificación débito no procesada por parte del
Participante Receptor: No fue encontrada la información
requerida del campo 3 del Registro de Adenda (información
adicional) establecida como de obligatoria inclusión por parte
del Participante Originador.
R32 N/A
N/A N/A SI Entrada Crédito no procesada por parte del Participante
Receptor: No fue encontrada la información requerida del
campo 3 del Registro de Adenda (información adicional)
```

### Anexo 2 Manual Operativo Sistema de Compensación Electrónica Nacional Interbancaria – CENIT

### Anexo A Causales de devolución servicio de Compensación y Liquidación del Sistema De Compensación Electrónica Nacional Interbancaria

**- CENIT**

### Fecha: martes, 28 de noviembre de 2023

### Asunto 1: Reglamento del Sistema de Compensación Electrónica Nacional Interbancaria - CENIT

```
establecida como de obligatoria inclusión por parte del
Participante Originador.
R33 N/A SI N/A SI Devolución de una transacción de depósito electrónico
cuando excede los límites establecidos.
```
```
Monto no autorizado, el valor de la transacción crédito o
débito con destino a depósito electrónico, excede los topes
definidos
```
**R34 SI SI SI SI** (^) **Cancelación Manual** : La transacción no fue liquidada debido a
que la Entidad Financiera Originadora, la canceló
manualmente por decisión propia
Esta Causal puede ser utilizada por el operador de la ACH para
todos los servicios PPD CCD y CTX
**R35 SI SI SI SI Tipo de cuenta errada**.
La transacción no puede ser aplicada debido a que el tipo de
cuenta está errado


### Anexo 2 Manual Operativo Sistema de Compensación Electrónica Nacional Interbancaria – CENIT

### Anexo A Causales de devolución servicio de Compensación y Liquidación del Sistema De Compensación Electrónica Nacional Interbancaria

**- CENIT**

### Fecha: martes, 28 de noviembre de 2023

### Asunto 1: Reglamento del Sistema de Compensación Electrónica Nacional Interbancaria - CENIT

## TABLA 2 - CAUSALES DE DEVOLUCIÓN DE UNA DEVOLUCIÓN PARA PPD

```
Causal Descripción Estándar
R60 Devolución de una Devolución solicitada por el Participante Receptor : El Participante Receptor ha solicitado al Participante Originador, devolver una
transacción de Devolución enviada.
R61 Devolución enviada al Participante incorrecto : El código de Tránsito del Participante Receptor de la devolución no corresponde al código de Tránsito del
Participante Originador.
R62 Número de Secuencia incorrecto : El Participante Receptor ha modificado el número de secuencia de la transacción original contenida en el Registro
Adenda. El Participante Originador no puede identificar la transacción.
R63 Valor Incorrecto: El Participante Receptor está devolviendo una transacción con un valor que difiere del valor de la transacción original.
R64 Número de Identificación incorrecto: El número de identificación reflejado en la transacción de Devolución difiere de la Identificación del Receptor que
fue enviada en la transacción original. El Participante Originador no puede identificar la transacción original.
R65 Código de Transacción Incorrecto: El código de transacción contenido en la transacción de devolución no corresponde con el código de la transacción
original.
R66 Identificación del Originador-Incorrecta: El Participante Receptor ha modificado la identificación del Originador contenida en el Registro de Encabezado
de Lote donde fue enviada la transacción original.
R67 Devolución Duplicada: El Participante Originador ha recibido más de una transacción de Devolución para la misma transacción enviada.
R68 Devolución Extemporánea: La transacción de Devolución no ha sido enviada dentro del tiempo límite establecido, en consecuencia, no es aceptada por
el Participante Originador.
R69 Múltiples Errores: Existe más de una causal de Devolución de la Devolución con información incorrecta: ruta y tránsito, número de secuencia, valor,
número de identificación del Receptor, código de transacción, número de identificación del Originador, etc.
```

### Anexo 2 Manual Operativo Sistema de Compensación Electrónica Nacional Interbancaria – CENIT

### Anexo A Causales de devolución servicio de Compensación y Liquidación del Sistema De Compensación Electrónica Nacional Interbancaria

**- CENIT**

### Fecha: martes, 28 de noviembre de 2023

### Asunto 1: Reglamento del Sistema de Compensación Electrónica Nacional Interbancaria - CENIT

(^1) Estas causales utilizan la simbología de las reglas NACHA 2000 (RXX), su descripción no corresponde a este estándar, dado que han sido modificadas para efectos de
aplicarlas en el Sistema CENIT.
**Causal Descripción Estándar
R70**^1 **Número de cuenta incorrecto:** El número de Cuenta original del registro de detalle de transacciones fue modificado por el Participante Receptor e impide
procesar la transacción de Devolución.
**R71**^1 **Datos Discrecionales del Originador incorrectos:** El campo Datos Discrecionales del Originador del Registro de Encabezado de Lote fue modificado por el
Participante Receptor e impide procesar la transacción de Devolución.
**R72**^1 **Tipo de Servicio incorrecto:** El tipo de servicio del lote enviado en el Registro de Encabezado de Lote fue modificado por el Participante Receptor e impide
procesar la transacción de Devolución.
**R73**^1 **Descripción de Lote incorrecto: El campo Descripción del Lote del Registro de Encabezado de Lote fue modificado por la Entidad Financiera Receptora
e impide procesar la transacción de devolución.**
Aplica en los casos en que no se utiliza el estándar establecido para la descripción de los tipos de lote de la tabla #4 y solo si el cambio de la descripción
es diferente al concepto “DEVOLUCIÓN”.
**R74**^1 **Devolución errada de una transacción crédito monetaria por la causal R32:** El Participante Receptor ha devuelto una Entrada Crédito por la causal R32,
a pesar de la existencia de información en el campo 3 del Registro de Adenda de la transacción original que debe ser entregada al Receptor.


