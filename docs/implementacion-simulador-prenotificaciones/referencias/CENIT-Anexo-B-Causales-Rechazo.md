## Anexo 2 Manual Operativo Sistema de Compensación Electrónica Nacional Interbancaria – CENIT

## Anexo B Causales de Rechazo de los Archivos en el Sistema de Transferencia de Archivos

## Hoja 1 – A2 – B – 1

# CIRCULAR EXTERNA OPERATIVA Y DE SERVICIOS DSP- 152

## Fecha: Martes, 28 noviembre de 2023

## Asunto 1: REGLAMENTO DEL SISTEMA DE COMPENSACIÓN ELECTRÓNICA NACIONAL INTERBANCARIA – CENIT

```
No. DESCRIPCIÓN
```
```
D 01 Archivo enviado erradamente. El archivo recibido no corresponde al Operador de Información Receptor, es decir, que al menos una de las respectivas
Administradoras contenida en el archivo recibido no está(n) vinculada(s) con el Operador de Información Receptor. En caso de que el rechazo no corresponda a
la totalidad del archivo recibido, deberá adicionarse al texto establecido en el Manual de Especificaciones del Formato para el Servicio de Transferencia de
Archivos – STA, lo siguiente: Se están rechazando XXX registros correspondientes a la(s) Administradora(s) (nombre(s) o razón social) YYYY”, en donde YYYY es
el código de la(s) respectiva(s) Administradora(s).
El respectivo rechazo deberá enviarse al Operador de Información Originador por parte del Operador de Información Receptor, en forma independiente, para
que el Operador de Información Originador envíe el archivo o la información al Operador correcto.
D 02 Archivo firmado y encriptado erradamente. Archivo que está encriptado y firmado para un Operador de Información Receptor diferente o para usuarios no
válidos y es imposible efectuar su desencripción.
D 03 Archivo con formato errado / Archivo que no fue posible procesar. Archivo que contiene múltiples errores de formato en la estructura del archivo plano, y/o
no cumple con las características establecidas y, por ende, no es posible su procesamiento. Esta causal aplica, si el archivo recibido presenta errores de estructura
en al menos un archivo plano, varios o todos los archivos contenidos en el mismo. En caso de que el rechazo no corresponda a la totalidad del archivo recibido,
deberá adicionarse al texto establecido en el Manual de Especificaciones del Formato para el Servicio de Transferencia de Archivos – STA lo siguiente: “Se están
rechazando XXX registros correspondientes al(os) archivo(s) “nombre del(os) archivo(s) plano(s)”. Se deberá incluir la relación detallada de los nombres de los
archivos planos que se están rechazando.
D 04 Archivo Duplicado. El archivo recibido por el Operador de Información Receptor ya había sido previamente enviado por el Operador de Información Originador.
Esta causal aplica, si la información contenida en el archivo o parte de este ya había sido recibida, es decir, que el archivo contiene información duplicada en al
menos un archivo plano, varios o en todos los archivos enviados por el Operador Originador. En caso de que el rechazo no corresponda a la totalidad del archivo
recibido, deberá adicionarse al texto establecido en el Manual de Especificaciones del Formato para el Servicio de Transferencia de Archivos – STA lo siguiente:
“Se están rechazando XXX registros correspondientes al(os) archivo(s) nombre del(os) archivo(s) plano(s)”. Se deberá incluir la relación detallada de los nombres
de los archivos planos que se están rechazando.
D 05 Número de registros reportado en el nombre externo del archivo diferente al número de registros contenidos en el mismo. La cantidad de registros reportada
en el nombre externo del archivo recibido por el Operador de Información Receptor no corresponde al número de registros contenidos en el archivo enviado
por el Operador de Información Originador.
D 06
Error en regla de distribución establecida por los Operadores de Información. El archivo recibido por el Operador de Información Receptor pertenece a una
Administradora que también está registrada con otro Operador de Información y/o con el Operador de Información Originador, pero no le corresponde el recibo
del archivo como Operador de Información Receptor, de acuerdo con la regla de distribución establecida.
```

