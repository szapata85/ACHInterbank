# I01 — Contrato público de resultados de prenotificaciones salientes

**Fecha:** 2026-11-02  
**Versión del contrato:** `I01`  
**Estado operativo:** NO HABILITADO; el contrato **no** genera archivos ni actualiza la base de datos.  
**Cámaras en alcance:** `ACHCOL` y `CENIT`, reglas y causales específicas por cámara a verificar en I02/I03.

## Separación semántica obligatoria

1. `Returned`: devolución expresa del banco receptor, con causal oficial. **Cuando I03/I04 estén implementados**: archivo NACHA-M generado solo para descarga; posterior carga manual en NachaUpload, sin autoimportación.
2. `NoReturnByDeadline`: no hay mensaje de aprobación recibido ni archivo NACHA-M positivo. **Cuando I05 esté implementado**: evaluación temporal aislada del proceso de aceptación tácita; no se escribe una causal ni se define un ciclo de respuesta.
3. El valor legado `NachaInboundSimulationType.IncomingPrenotificationResponse = 3` no se elimina ni se reutiliza. El nuevo `IncomingPrenotificationReturn = 10` **continúa bloqueado** en Preview y Generate con `PRENOTIFICATION_RETURN_NOT_IMPLEMENTED` hasta las validaciones de I02 y el constructor homologado de I03.
4. No confundir el flujo de **devolución de una prenotificación enviada** con `RegistrarRespuestaTransaccion` u otros circuitos de respuesta diferencial SOAP. El diseño posterior debe respetar las rutas existentes de dominio y su autoridad normativa.

## Contratos HTTP añadidos (lectura / sin efectos)

Ambos endpoints conservan `[Authorize(Policy=FineGrainedPermissions.TestTools.Access)]` de clase y exigen además `P1Policies.NachaSimulatorRead`.

| Método | Ruta | Efecto |
|---|---|---|
| GET | `/api/uat/nacha-inbound-simulator/prenotifications/contract` | Devuelve resultado tipado, compatibilidad, cámaras y disponibilidad **falsa** de ejecución. |
| POST | `/api/uat/nacha-inbound-simulator/prenotifications/validate-contract` | Valida la **forma** del cuerpo, NO consulta transacciones, NO verifica elegibilidad, NO genera ni importa archivos; `200` incluye `ShapeValid` y `ExecutionEnabled=false`. |

### Solicitud `Returned` ACH Colombia (ejemplo sintético)

```json
{
  "outcome": "Returned",
  "clearingHouseCode": "ACHCOL",
  "originalPrenotificationIds": [101],
  "originalCycleId": "ACH-CYCLE-ORIGINAL",
  "responseCycleId": "ACH-CYCLE-RETURN",
  "businessDate": "2026-11-02",
  "respondingFinancialInstitutionId": 7,
  "reasonCode": "R04",
  "idempotencyKey": "test-only-achcol-101-01"
}
```

`R04` es **solo un ejemplo de estructura**, no una afirmación de aplicabilidad automática: I02/I03 verifican causal según tipo/dirección, fecha, catálogo y cámara.

### Solicitud `Returned` CENIT (ejemplo sintético)

```json
{
  "outcome": "Returned",
  "clearingHouseCode": "CENIT",
  "originalPrenotificationIds": [202],
  "originalCycleId": "CENIT-ORIGINAL",
  "responseCycleId": "CENIT-DEVOLUCION",
  "businessDate": "2026-11-02",
  "respondingFinancialInstitutionId": 8,
  "reasonCode": "R03",
  "idempotencyKey": "test-only-cenit-202-01"
}
```

El código `R03` es también un **ejemplo de forma**: no indica que esa causal sea válida para toda prenotificación. La validación normativa se implementará con el catálogo y contexto de cada cámara.

### Solicitud `NoReturnByDeadline` (sin archivo)

```json
{
  "outcome": "NoReturnByDeadline",
  "clearingHouseCode": "CENIT",
  "originalPrenotificationIds": [202],
  "originalCycleId": "CENIT-ORIGINAL",
  "businessDate": "2026-11-02",
  "respondingFinancialInstitutionId": 8
}
```

Esta solicitud no contiene `responseCycleId` ni `reasonCode` porque **no hubo respuesta**. El vencimiento se validará sobre la información original y un reloj aislado, no sobre la fecha que ingrese libremente el usuario. I05 decidirá el mecanismo seguro de ejecución/consulta para UAT.

### Respuesta de validación (esquema)

```json
{
  "shapeValid": true,
  "executionEnabled": false,
  "contractVersion": "I01",
  "violations": []
}
```

El caso `shapeValid=true` **NO** representa elegibilidad, recepción, aprobación o envío. Fallas estructurales incluyen `PRENOTIFICATION_IDS_REQUIRED`, `PRENOTIFICATION_IDS_DUPLICATED`, `RESPONSE_CYCLE_REQUIRED`, `RETURN_REASON_REQUIRED`, `IDEMPOTENCY_KEY_REQUIRED`, `NO_RETURN_HAS_NO_REASON`, `NO_RETURN_HAS_NO_RESPONSE_CYCLE` y `PRENOTIFICATION_OUTCOME_INVALID`.

## Campos y autoridad de datos

| Campo | Regla I01 | Validación adicional I02–I05 |
|---|---|---|
| `originalPrenotificationIds` | IDs internos positivos, sin duplicidad | Referencias realmente enviadas, propiedad, banco, cámara, estado, correlación con intento canónico |
| `originalCycleId` | Obligatorio | Coincide con el ciclo de origen de las operaciones seleccionadas |
| `responseCycleId` | Obligatorio únicamente en `Returned` | Ciclo permitido para devolver según cámara y fecha, puede diferir del original |
| `businessDate` | Fecha válida obligatoria | Fecha operacional real, calendario y corte de cámara |
| `clearingHouseCode` | ACHCOL o CENIT | Catálogo efectivo y perfil activo |
| `respondingFinancialInstitutionId` | ID positivo | Coincide con la entidad receptora original y no es el banco propio |
| `reasonCode` | Requerido solo en `Returned` | Causal vigente y aplicable al tipo de prenotificación |
| `idempotencyKey` | Obligatorio en `Returned`, máximo 128 | Reserva única durable + control de concurrencia, con hash del request |

**Límite intencional:** no se exponen rutas `execute` ni `generate` nuevas para este contrato en I01. `GET` informa `ExecutionEnabled=false` y la ruta antigua `generate` niega el nuevo escenario. Es un contrato técnicamente publicado, pero **no funcionalmente homologado**.

## Reglas normativas de referencia (documentos locales del paquete)

- ACH Colombia V35 (2026-04), numeral 2.10.3.1: ausencia de devolución en el plazo equivale a prenotificación exitosa.
- DSP-152 CENIT Anexo 2 (2025-02), numeral 4.8: devolución en misma fecha valor y cierre del día operacional; sin devolución vencida permite iniciar entradas.
- NACHA-M CENIT (2026-05), sección 3: registros de 106 caracteres y adendas regladas.

**No afirmar que un código de causal, fecha, ciclo o importe introducido en estos ejemplos está homologado sin consulta al catálogo y al estado del proyecto.**
