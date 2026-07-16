# GO/NO-GO Proc_Contrapartidas LIVE local

Fecha: 2026-07-16

## Decisión

**LIVE-GO para la integración técnica local controlada de Proc_Contrapartidas.**

**Productivo externo continúa NO-GO.**

## Evidencia de decisión

| Criterio | Resultado |
|---|---|
| Request real llegó al WCF local | Cumple |
| Método exclusivo Proc_Contrapartidas | Cumple |
| Envelope outbound sin METODO | Cumple |
| Datos sintéticos | Cumple |
| Respuesta real recibida | Cumple, R96 |
| Respuesta persistida | Cumple |
| Persistencia después de reinicio | Cumple |
| Estado funcional coherente | Cumple |
| Log WCF local | Cumple |
| Playwright opt-in | 1 passed |
| Duplicidad de transporte | No; una invocación real |
| Otros métodos SOAP | 0 invocaciones |
| CENIT LIVE | Bloqueado |

## Controles de idempotencia

- Despacho dirigido a una sola transacción.
- Item exitoso terminal y no retryable.
- `AttemptCount=1` para el transporte exitoso.
- Jobs automáticos deshabilitados durante la ventana.
- Retry HTTP deshabilitado de forma válida.
- El escenario pre-transporte se cerró terminal sin replay y conserva evidencia.

## Riesgos residuales

- El WCF legacy incorpora `METODO` sólo en su trazabilidad interna; el envelope outbound no lo contiene.
- Request/response completos siguen persistidos según el modelo vigente; para datos no sintéticos debe cerrarse la decisión de cifrado o tokenización a nivel de campo.
- Falta certificación externa, observabilidad operativa acordada y aprobación humana.
- El resultado local no prueba disponibilidad, contrato ni comportamiento de infraestructura productiva.

## Condiciones para productivo

Homologación contractual, secreto/certificado administrado, protección de payload persistido, runbook de retry/compensación, monitoreo, aprobación de seguridad y autorización formal del comité GO/NO-GO.

