# ACH Interbank — registro SOAP sin enmascaramiento

**Base de este ajuste:** `ACHInterbank_MENU_CAPACIDADES_OCULTAS_V2_LOGS_SOAP_60DIAS_2026-11-05.zip`.

## Propósito y cobertura

`ACHInterbank.SOAP.log` guarda **el XML textual original** entregado a `StringContent` y el texto de la respuesta obtenido con `ReadAsStringAsync` para las operaciones:

- `WSCFAACH.Proc_Contrapartidas`.
- `WSCFAACH.Proc_Transacciones`, incluidas las ejecuciones paralelas.
- `WSAxonRespuestaTransacciones.RegistrarRespuestaTransaccion` (RespuestasAXON), incluidas las ejecuciones paralelas.

Cada solicitud (`REQUEST`) y su respuesta (`RESPONSE`) tienen el mismo `correlationId`; se incluyen operación, estado HTTP, duración, indicador SOAP Fault y longitud original. Ante un timeout, cancelación o error de transporte sin respuesta, se registra `NO_RESPONSE` y no se inventa un XML.

## Importante: datos completos

- **No se enmascara ni elimina ningún dato del XML.** Se conservan los elementos, valores, atributos, encabezados SOAP, comentarios, espacios y saltos de línea presentes en las cadenas originales.
- **No se trunca ni suprime XML por tamaño**: se eliminó `SoapEnvelopeLogging:MaxEnvelopeChars` y el transformador `Sanitize`.
- XML malformado y XML con DTD también se registran exactamente como los recibe el journal. El análisis secundario de la bandera `soapFault` es solo de lectura, prohíbe DTD y no puede reescribir ni suprimir el XML registrado.
- Los valores enviados al banco y recibidos del banco no se cambian. El log contiene **cadenas de XML**, no una captura byte a byte del transporte HTTP (no incluye sus encabezados HTTP).
- El fichero técnico `ACHInterbank.log` continúa separado y no duplica el contenido SOAP por la regla exclusiva de NLog.

## Seguridad operativa obligatoria

Este archivo puede guardar números de cuenta, identificaciones, saldos, tokens, contraseñas incluidas en encabezados SOAP y cualquier otro dato financiero o personal que envíen las integraciones. **Trátalo como información restringida**: limita a administradores autorizados el acceso al volumen `.runtime/logs`, controla copias y respaldos, cifra el almacenamiento cuando corresponda y evita enviar este archivo a soportes o repositorios públicos. La política de 60 días debe ser coherente con la política de datos de la entidad. Se preserva la opción de deshabilitarlo rápidamente ante un incidente.

## Configuración y límites

En `src/Cfa.ACHInterbank.Api/appsettings.json`:

```json
"SoapEnvelopeLogging": {
  "Enabled": true
}
```

Se puede deshabilitar mediante `SoapEnvelopeLogging__Enabled=false`, aplicando la recarga o reinicio que corresponda.

El archivo se mantiene en `${basedir}/logs/ACHInterbank.SOAP.log` (normalmente `/app/logs/ACHInterbank.SOAP.log` en Docker), y NLog rota diariamente o al alcanzar 10 MiB, hasta 30 históricos y como máximo 60 días de antigüedad. La condición de cantidad puede eliminar históricos antes de los 60 días; un envelope mayor de 10 MiB puede hacer que un archivo individual sobrepase ese tamaño antes de rotar.

La cola asíncrona SOAP está limitada a **256 eventos** (`overflowAction=Discard`), con lotes de 50. Al aumentar el volumen de XML sin límite de caracteres, se reduce la cantidad en cola para contener el uso de memoria. **Bajo saturación o terminación abrupta, NLog puede perder registros diagnósticos**. No debe usarse como comprobante único o auditoría financiera persistente. Se mantiene la prioridad de no bloquear operaciones financieras por la escritura de logs.

## Consulta en Docker

```cmd
docker exec achinterbank-achinterbank-api-1 tail -n 100 -f /app/logs/ACHInterbank.SOAP.log
```

Para conservar saltos de línea y revisar XML completos, es preferible consultar el archivo desde un editor con acceso restringido, ya que `grep` de líneas aisladas puede omitir parte del envelope.

## Pruebas en el entorno con .NET 10

```bash
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj --filter FullyQualifiedName~SoapEnvelopeJournalTests
```

Validar con un endpoint aislado la igualdad textual solicitud/respuesta, respuestas HTTP 500, SOAP Fault, XML malformado, mensajes mayores de 131.072 caracteres, concurrencia, saturación de cola y limpieza de históricos. La actualización no modifica reglas financieras, endpoints, timeouts ni reintentos.
