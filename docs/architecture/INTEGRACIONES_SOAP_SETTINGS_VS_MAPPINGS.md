# Integraciones SOAP: settings vs mappings

Fecha: 2026-05-21

## Responsabilidad de soap-settings

La pantalla `/integraciones/soap-settings` administra configuracion tecnica por cliente y operacion SOAP:

- cliente tecnico.
- operacion/metodo.
- endpoint.
- SOAP Action.
- estado habilitado/deshabilitado.
- modo operativo informado.
- prueba local/DryRun.
- certificado o alias cuando aplique, sin exponer secretos.

No debe ser la fuente primaria de la relacion funcional campo-a-campo. Puede mostrar un resumen tecnico del contrato, pero la matriz de campos SOAP se gestiona en `/integraciones/mappings`.

## Responsabilidad de mappings

La pantalla `/integraciones/mappings` presenta la matriz funcional y auditable de campos sistema contra parametros SOAP:

- servicio SOAP.
- parametro SOAP.
- tabla origen controlada.
- campo origen interno.
- campo destino SOAP.
- tipo de dato.
- transformacion.
- valor por defecto.
- requerido/opcional.
- orden.
- version y estado de la relacion.

La vista principal debe evitar protagonismo de `MappingSet`, historial, JSON, preview y rutas tecnicas. Esos elementos quedan como detalle tecnico o auditoria secundaria.

## Relacion obligatoria

Ambas pantallas se relacionan por `IntegrationKey + OperationKey`.

| IntegrationKey | OperationKey | Cliente tecnico | Proposito |
|---|---|---|---|
| WSCFAACH | Proc_Contrapartidas | WscfaachSoapClient | MonetaryDebitRequest |
| WSCFAACH | Proc_Transacciones | WscfaachSoapClient | MonetaryCreditRequest |
| WSAXON | RegistrarRespuestaTransaccion | WsAxonRespuestaTransaccionesSoapClient | DifferentialResponseNotification |

## Decision de arquitectura

Se agrego clasificacion funcional derivada al catalogo de integraciones sin migracion EF:

- `integrationKey`.
- `operationKey`.
- `mappingDirection`.
- `mappingPurpose`.
- `functionalNature`.
- `functionalOriginator`.
- `movesMoney`.

Esto evita que `RegistrarRespuestaTransaccion` se presente como flujo monetario y permite que la SPA filtre por proposito/direccion.

## Estado UAT

Productivo permanece **NO-GO**. Las operaciones SOAP deben permanecer en DryRun/Disabled/Mock en UAT/local salvo autorizacion formal.
