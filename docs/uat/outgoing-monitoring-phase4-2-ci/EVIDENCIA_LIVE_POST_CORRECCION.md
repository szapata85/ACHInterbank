# Evidencia LIVE posterior a la correccion

## Alcance seguro

- WCF local: `http://localhost:7083/WSCFAACH.svc`.
- Acceso Docker autorizado: `http://host.docker.internal:7083/WSCFAACH.svc`.
- Datos sinteticos, sin saldos ni sistemas externos.
- No se documentan XML, cuentas, identificaciones ni credenciales.

El primer intento de preparacion quedo bloqueado antes de SOAP por la politica de Ciclo 5. Se corrigio la zona horaria del runtime local de forma temporal y se usaron bases nuevas. Una entrada inicial con referencia ausente tambien fue rechazada por readiness antes de construir XML; no fue reenviada.

El fixture LIVE se corrigio para persistir una referencia funcional valida, separada del identificador operativo cuando la validacion de caracteres lo exige.

## Resultado

| Motor | Mapping efectivo | Modo | SOAP | Transporte | Respuesta persistida | Intentos | Duplicados |
| --- | --- | --- | --- | --- | --- | ---: | ---: |
| SQL Server | canonico v1, 17/17, sin fallback | Live | `Proc_Contrapartidas` local | Succeeded | Si | 1 | 0 |
| PostgreSQL | canonico v1, 17/17, sin fallback | Live | `Proc_Contrapartidas` local | Succeeded | Si | 1 | 0 |

En PostgreSQL el scheduler completo la llamada antes del dispatch manual; el intento posterior devolvio `CONTRAPARTIDA_ALREADY_SUCCEEDED` y no produjo reenvio. La evidencia persistida confirmo `BusinessStatus=Success`, `SoapTechnicalStatus=Succeeded` y un unico intento.

Las APIs y SPAs terminaron saludables en puertos 843/743 y 844/744. WCF respondio HTTP 200.
