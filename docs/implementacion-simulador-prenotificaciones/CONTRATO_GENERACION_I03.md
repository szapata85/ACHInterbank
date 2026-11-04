# Contrato I03 — devolución de prenotificación

`POST /api/uat/nacha-inbound-simulator/prenotifications/generate-return`

Permisos: `TestTools.Access` + `NachaSimulatorGenerateDifferential`. Solo UAT/local y con banderas de generación seguras. Se mantiene `GET /prenotifications/originated` y `POST /prenotifications/eligibility-preview` del incremento I02.

Ejemplo de cuerpo (IDs/ciclos/banco **ilustrativos**, no datos reales del proyecto):

```json
{
  "outcome": "Returned",
  "clearingHouseCode": "CENIT",
  "originalPrenotificationIds": [101],
  "originalCycleId": "CENIT-20261102-1",
  "responseCycleId": "CENIT-20261102-2",
  "businessDate": "2026-11-02",
  "respondingFinancialInstitutionId": 2,
  "reasonCode": "R02",
  "idempotencyKey": "uat-prenote-101-r02-001"
}
```

El cuerpo para ACH Colombia utiliza `clearingHouseCode: ACHCOL` y ciclos de ACH Colombia. La causal debe estar habilitada por la cámara, tipo de transacción, fecha y catálogo oficial; el ejemplo no garantiza elegibilidad real.

Comportamiento: crea **solo archivo** de devolución NACHA-M a valor cero y evidencia, o retorna el mismo resultado de una clave idempotente previamente generada. No autoimporta ni cambia estado. 400 estructura inválida; 422 reglas o guardas no satisfechas; 401/403 permisos; el resultado 201 incluye URLs relativas autenticadas de descarga y evidencia. La SPA todavía no usa este contrato.

`NoReturnByDeadline` no es admitido aquí; no crea archivo. La aceptación tácita queda para I05.

Idempotencia: la clave se normaliza por trim y se acota por cámara+banco. Una misma clave con otro ciclo, ID, fecha o causal falla con error funcional; otra clave para prenotificación ya simulada se rechaza. En concurrencia real puede surgir una excepción transitoria de BD que todavía requiere prueba de reintento y tratamiento específico antes de homologar.
