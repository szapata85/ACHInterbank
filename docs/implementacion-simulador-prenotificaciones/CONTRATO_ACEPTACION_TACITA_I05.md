# Contrato I05 — aceptación tácita de prenotificaciones salientes

**Fecha:** 2026-11-02. **Base:** I04. **Cámaras:** ACH Colombia (ACHCOL) y CENIT.

## Semántica normativa y arquitectónica

Las prenotificaciones salientes no reciben una aprobación positiva NACHA-M por el solo transcurso del tiempo. La ausencia de devolución, una vez vencido el plazo vigente, permite la aceptación tácita. Referencias suministradas: ACH Colombia V35 §2.10.3.1 y CENIT DSP-152 Anexo 2 §4.8. No son equivalentes los horarios de las dos cámaras: se consulta `PrenotificationValidationAttempt.ResponseDeadlineAtUtc`, resuelto previamente con la política efectiva de cada cámara, y **nunca** el SLA monetario `AchTransaction.SlaDeadlineAtUtc`.

## API — lectura sin efectos

`POST /api/uat/nacha-inbound-simulator/prenotifications/tacit-acceptance/preview`

Permisos: `TestTools.Access` + `NachaSimulatorRead`. Solicitud:

```json
{
  "contract": {
    "outcome": "NoReturnByDeadline",
    "clearingHouseCode": "CENIT",
    "originalPrenotificationIds": [123],
    "originalCycleId": "CENIT-CYCLE-01",
    "businessDate": "2026-11-02",
    "respondingFinancialInstitutionId": 22
  },
  "evaluationTimeUtc": "2026-11-03T00:00:00Z"
}
```

El tiempo virtual opcional se usa **solo para la decisión en la respuesta**, no modifica registros de la base, el `TimeProvider` general ni el job Quartz. Se evalúan a la vez `WouldApplyAtEvaluationTime` y `EligibleAtRealTime`. Devuelve razones de no elegibilidad y no realiza transición ni crea archivos.

## API — aplicación solo al vencimiento real

`POST /api/uat/nacha-inbound-simulator/prenotifications/tacit-acceptance/apply-due`

Permisos: `TestTools.Access` + `NachaSimulatorGenerateDifferential`. El cuerpo es `PrenotificationSimulationContractRequest` con `outcome=NoReturnByDeadline`, sin `responseCycleId` ni `reasonCode` y sin tiempo ficticio. Exige ambiente no productivo, opciones UAT/local, sin autoimportación ni transmisión externa, prenotificaciones realmente transmitidas, pendientes, intento canónico vigente, política con `TacitAcceptanceEnabled`, fecha/cámara/banco/ciclo coincidentes, vencimiento **real** validado contra reloj físico, no inyectable y ausencia de devolución previa o archivo simulado de devolución activo. Se utiliza `IAchStateTransitionService.TransitionAsync` con fuente `System`, estado `AppliedTacitly` e idempotencia por prenotificación e intento dentro de transacción serializable. Un reintento de todas las operaciones ya aplicadas devuelve `AlreadyApplied`; no permite selección mixta de terminales y pendientes.

**Importantísimo:** el `apply-due` es una operación de prueba que **sí altera el estado de la BD de UAT**; no se invoca desde la vista previa. En producción está prohibido. La generación de devoluciones y su carga por NachaUpload permanecen independientes.

## Riesgos y condiciones de cierre

- La simultaneidad con el Quartz normal y el soporte real para transacciones explícitas deben probarse con PostgreSQL y SQL Server; `SERIALIZABLE` y la clave funcional no sustituyen estas pruebas.
- Ejecutar compilación, Xunit, snapshots antes/después, aceptación real por ambas cámaras, feriados, doble ejecución, rechazo importado y devolución simulada pendiente.
- El reloj virtual sirve para inspeccionar la decisión futura; **no anticipa el estado persistente**. Si es obligatorio ensayar el tránsito futuro sin esperar al vencimiento, usar un fixture aislado cuya fecha del negocio ya esté vencida frente a la hora real (y nunca modificar el reloj global).
- `I06` conectará la SPA a ambos endpoints; hasta entonces se exponen por API.
