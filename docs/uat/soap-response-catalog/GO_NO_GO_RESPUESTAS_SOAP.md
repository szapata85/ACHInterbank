# GO/NO-GO — respuestas SOAP

Fecha: 2026-07-16.

## Decisión local controlada

**LIVE-GO LOCAL.** La decisión aplica exclusivamente a la topología localhost autorizada y a `Proc_Contrapartidas` con datos sintéticos.

Se demostraron:

- CORS, login, navegación y smoke Playwright en verde;
- una sola llamada WCF asociada a una nueva transacción sintética;
- R96 resuelto por catálogo y persistido con descripción, `CatalogId` y estados correctos;
- panel Angular visible;
- bloqueo del segundo dispatch antes del transporte;
- persistencia consultable tras reiniciar la API;
- delta WCF correlacionable y ausencia de otros métodos;
- builds y suites backend/Angular en verde.

La corrida monetaria inicial falló en una aserción posterior por la codificación CP850 de `sqlcmd`; la validación se reanudó read-only sobre el mismo TransactionId y terminó verde. No hubo segundo movimiento.

## Límites y riesgos residuales

- Productivo externo permanece **NO-GO**.
- `Proc_Transacciones` LIVE y CENIT LIVE no fueron ejecutados y permanecen bloqueados.
- El logger legacy WCF reconstruye una trama interna que contiene una etiqueta `METODO`; ACHInterbank no la envía en su body outbound. Conviene separar explícitamente log de recepción y transformación en el WCF.
- Persiste el riesgo heredado del almacenamiento de payload SOAP completo; debe cerrarse con la política corporativa de cifrado, retención y acceso.
- Se requiere aprobación humana de seguridad, operación y negocio antes de cualquier uso no local.

No se autoriza producción, repetición del débito ni ampliación del alcance.
