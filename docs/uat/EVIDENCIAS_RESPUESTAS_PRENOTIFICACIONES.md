# Evidencias UAT - respuestas diferenciales de prenotificaciones CFA

Fecha: 2026-05-23  
Ambiente: UAT/local  
Productivo: NO-GO

## Resultado

Estado: PARCIAL / brecha abierta.

Se verifico que `RegistrarRespuestaTransaccion` permanece clasificado como respuesta diferencial no monetaria:

- IntegrationKey: `WSAXON`
- OperationKey: `RegistrarRespuestaTransaccion`
- MappingPurpose: `DifferentialResponseNotification`
- MappingDirection: `InboundResponse`
- MovesMoney: `false`

Tambien se verifico que el use case de notificacion no inyecta `IWscfaachSoapClient`, no invoca `Proc_Contrapartidas` y no invoca `Proc_Transacciones`.

## Brecha encontrada

No existe todavia un flujo de aplicacion que tome una respuesta diferencial de prenotificacion, cruce contra NACHA-M desagregado y cambie el estado de una `AchTransaction` con `IsPrenotification=true` usando estados reales del dominio.

La prenotificacion interna se modela como `AchTransaction.IsPrenotification=true`; los estados reales disponibles son:

- `Pending`
- `ReturnedByOperator`
- `ReturnedByEpr`
- `AppliedTacitly`
- `Certified`

No se implemento una regla nueva para traducir codigos externos a esos estados porque la homologacion debe venir de catalogos existentes y evidencia funcional. No se simulo aprobacion/rechazo.

## Defecto abierto

`DEF-UAT-SOAP-MAP-004`: falta caso de uso controlado para aplicar respuesta diferencial sobre prenotificacion pendiente originada por CFA, cruzando payload/NACHA-M desagregado/`AchTransaction`, persistiendo trace y state event, sin movimiento monetario.

## Confirmaciones

- No hubo transmision externa.
- No se movio dinero.
- No se afectaron saldos.
- No se expusieron secretos.
- Productivo permanece NO-GO.
