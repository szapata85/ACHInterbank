# Plan de implementación y verificación — NPT_SOAP_XSD_FINAL_01

1. Realizado: inventario de los dos WSDL y cinco XSD locales, importaciones y SHA256, validación offline.
2. Realizado en código: resource loading con importación local segura, XSD en request/response CFA y Axon; `SOAPAction` exacto; protección `NCTAORIG` y F5 fail-closed.
3. Realizado: nuevas pruebas C# y adaptación de fixtures al namespace del XSD; scripts Python locales y regresiones existentes.
4. Pendiente de infraestructura: compilar/ejecutar xUnit .NET 10; comparar SQL Server/PostgreSQL, responder con simuladores SOAP instrumentados y verificar cero POST en rechazos pretransporte.
5. Pendiente externo separado: confirmación operativa ACH Colombia F5, procedencia bancaria homologada NCTAORIG y semántica oficial de códigos CFA/Axon.
6. UAT y Live **fuera de alcance**. No abrir Live automáticamente.

Estado de avance: fuente implementada; validación de entorno pendiente. Este documento no es una certificación.
