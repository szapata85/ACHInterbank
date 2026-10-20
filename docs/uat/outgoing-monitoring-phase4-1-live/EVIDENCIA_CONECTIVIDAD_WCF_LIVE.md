# Evidencia — conectividad WCF LIVE

## Aislamiento

- Ambiente declarado por el WCF: `DESARROLLO`.
- Hospedaje: IIS Express local, sitio `WSCFAACH`.
- Dependencias productivas: descartadas en la ruta ejecutada; las llamadas descendentes están deshabilitadas en la implementación local.
- Datos: transacciones sintéticas `LIVE-F4.1-PC-TX-*`.
- Producción: no utilizada.

## Contrato y transporte

- WSDL Windows: disponible con HTTP 200 en `localhost:7083`.
- WSDL desde la red Docker: disponible con HTTP 200 mediante `host.docker.internal:7083` y `Host: localhost:7083`.
- Binding: SOAP 1.1.
- Operación: `Proc_Contrapartidas`.
- SOAPAction: `http://tempuri.org/IWSCFAACH/Proc_Contrapartidas`.
- Modo efectivo de la API: `Live`.
- Timeout configurado: 15 segundos.
- `METODO`: usado como metadato de trazabilidad, no enviado como parámetro SOAP.
- `PLValidarUsuarioBV`: no participa en el flujo.

Los logs locales registraron marcadores de recepción de `Proc_Contrapartidas` y respuestas R96 durante la ventana UAT. No se copiaron envelopes ni datos sensibles.
