# Dependencias no subsanables sin tercero

1. **F5 ACH Colombia file-based (NPT-01):** mapping CFA, origen de cuenta, elegibilidad para ACH Colombia file-based, seguridad, contrato de operación y autorización operativa/homologación por participante; la ruta continúa `Blocked`.
2. **Respuesta diferencial física (NPT-02):** layout, perfil y XSD/golden files aprobados por ACH Colombia/CENIT, reglas de archivo/fecha/id original y vectores representativos PPD/CCD/CTX; no crear parser ficticio.
3. **Cuenta CFA origen `NCTAORIG` (NPT-05):** semántica oficial y fuente de cuenta origen distinta de la receptora; confirmación CFA y test contra XSD. No usar valores de relleno.
4. **WSDL importados (NPT-14):** `http://esparta/WSCFAACH/WSCFAACH.svc?xsd=xsd0`, `...xsd1` y `http://esparta/WSCFAACH/WSAxonRespuestaTransacciones.svc?xsd=xsd0`, `...xsd1`, `...xsd2`. Véase JSON de evidencias con URLs y namespaces completos; el hostname es red privada y no se deducen tipos sin los XSD.
5. **Infraestructura pendiente:** SDK .NET 10; restauración con NuGet; dos instancias Quartz reales; SQL Server y PostgreSQL con migraciones aplicadas a copias y concurrencia transaccional; stub SOAP instrumentado que cuente peticiones y pueda cortar antes/durante/después del envío; test de crash/reinicio y conciliación. UAT expresamente diferida.

La ausencia de estas pruebas **no equivale a un certificado ni a código probado en producción**.
