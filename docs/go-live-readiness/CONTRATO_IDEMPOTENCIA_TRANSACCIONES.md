# Contrato De Idempotencia Y Deduplicacion Transaccional - ACH Interbank

Fecha de formalizacion: 2026-05-19 America/Bogota
Version: 0.1 contrato observado UAT
Alcance: transacciones ACH individuales creadas por `POST /transactions`
Estado: contrato actual documentado; evolucion funcional pendiente de decision.

## Resumen Ejecutivo

El comportamiento actual no implementa idempotencia HTTP estricta por `Idempotency-Key` ni replay de respuesta previa. La proteccion existente es una deduplicacion funcional previa a persistencia, ejecutada por `TransactionPolicyService.PreviewAsync` antes de crear la transaccion.

Para UAT tecnico/funcional sintetico, el contrato observado queda documentado y acotado: un reintento equivalente se rechaza con `HTTP 400` y JSON controlado, no persiste una segunda transaccion y no genera un segundo evento inicial.

Productivo permanece **NO-GO**.

## Contrato Actual Observado

| Dimension | Contrato actual |
|---|---|
| Endpoint | `POST /transactions` |
| Momento de validacion | Antes de persistir la transaccion, en `AchTransactionService.RegisterTransactionAsync` mediante `ITransactionPolicyService.PreviewAsync`. |
| Servicio responsable | `TransactionPolicyService`. |
| Persistencia ante duplicado | No persiste nueva transaccion. |
| Eventos ante duplicado | No genera evento inicial adicional en `AchTransactionStateEvents`. |
| HTTP ante duplicado | `400 Bad Request`. |
| Cuerpo ante duplicado | JSON con propiedad `message`. |
| Mensaje actual | `Ya existe una transacción equivalente para el mismo ciclo.` |
| Codigo de error funcional | No existe codigo funcional estable dedicado. |
| Header `Idempotency-Key` | No soportado para este endpoint. |
| Replay de respuesta previa | No soportado. |
| Hash de payload | No usado para transacciones individuales. |
| Constraint unico DB | No existe indice unico especifico para `TransactionExternalId`/`Reference` de transaccion individual. |
| Correlation/request id | No hay contrato estable expuesto en la respuesta de duplicado para `POST /transactions`. |

## Criterios De Deduplicacion

Una solicitud se considera duplicada si existe una transaccion en el ciclo resuelto que coincide con:

| Campo | Regla |
|---|---|
| Ciclo ACH | Debe coincidir `AchCycleId` resuelto para la solicitud. |
| Tipo | Debe coincidir `TransactionTypeEnum`. |
| Monto | Debe coincidir `Amount`. |
| Cuenta origen | Debe coincidir `SourceAccountNumber` normalizada con `Trim()`. |
| Cuenta destino | Debe coincidir `DestinationAccountNumber` normalizada con `Trim()`. |
| Identificador operativo | Usa `TransactionExternalId` si viene informado; si no viene, usa `Reference`. |
| Compatibilidad legacy | Si la transaccion persistida no tiene `TransactionExternalId`, se compara `Reference` contra el identificador operativo resuelto. |

El `IdempotencyKey` devuelto por preview es sintetico/informativo y se forma como:

```text
{cycleId}:{type}:{sourceAccountNumber}:{destinationAccountNumber}:{amount}:{operationalId}
```

Este valor no se recibe como header, no se guarda como llave de idempotencia de API y no habilita replay.

## Ejemplo Sintetico

Solicitud original:

```json
{
  "amount": 1000,
  "transactionExternalId": "UAT-SINT-TRACE-001",
  "reference": "UAT-SINT-TRACE-001",
  "type": 1,
  "accountType": 1,
  "isPrenotification": false,
  "destinationInstitutionId": 93,
  "sourceAccountNumber": "0000000001",
  "destinationAccountNumber": "0000000002",
  "recipientIdNumber": "999999999",
  "recipientName": "CLIENTE UAT",
  "requiresIdentityValidation": false,
  "companyName": "CLIENTE UAT",
  "companyIdentification": "999999999",
  "companyEntryDescriptionId": 1,
  "sourcePersonType": "PJ",
  "recipientPersonType": "PN",
  "addendas": []
}
```

Resultado observado:

| Intento | Resultado |
|---|---|
| Primer `POST /transactions` | `201 Created`, transaccion ID `2`, estado `Pending`, evento inicial creado. |
| Segundo `POST /transactions` con mismo payload | `400 Bad Request`, JSON controlado, sin segunda transaccion, sin segundo evento inicial. |

Respuesta actual ante duplicado:

```json
{
  "message": "Ya existe una transacción equivalente para el mismo ciclo."
}
```

## Evidencia UAT

| Evidencia | Resultado |
|---|---|
| `UAT-SINT-TRACE-001` | Transaccion ID `2` creada con datos sinteticos. |
| Evento inicial | `Pending -> Pending`, `Source=System`, `ReasonCode=CREATED`. |
| Reintento duplicado | `HTTP 400` con mensaje controlado. |
| Conteo transacciones | `transaction_count=1`. |
| Conteo eventos | `event_count=1`. |
| Prueba servicio | `RegisterTransactionAsync_WhenPolicyRejectsDuplicate_DoesNotCreateSecondInitialStateEvent`. |
| Prueba policy | `PreviewAsync_CurrentContractReturnsDuplicateMessageAndSyntheticKey`. |
| Prueba controller | `CreateTransaction_ReturnsBadRequestJson_WhenDuplicatePolicyRejects`. |

## Riesgos

| Riesgo | Impacto | Mitigacion actual |
|---|---|---|
| Clientes API pueden esperar `409 Conflict` para duplicados. | Integracion ambigua y manejo heterogeneo de reintentos. | Documentar `400` actual hasta decision formal. |
| No hay replay idempotente. | Un cliente no puede obtener la respuesta original a partir de una llave. | Consultar transaccion por filtros/ID si el primer intento fue exitoso pero la respuesta se perdio. |
| No hay `Idempotency-Key` persistida. | Reintentos con payload equivalente dependen de criterios funcionales, no de contrato HTTP estandar. | Mantener deduplicacion funcional por ciclo/tipo/monto/cuentas/operationalId. |
| No hay constraint unico DB para transaccion individual. | Riesgo teorico ante concurrencia extrema si dos solicitudes equivalentes pasan preview simultaneamente. | Requiere decision posterior si se necesita garantia fuerte en DB. |
| No hay codigo funcional estable. | Clientes deben parsear mensaje o status. | Recomendar codigo estable en evolucion futura. |

## Decisiones Pendientes

| Opcion | Decision requerida | Impacto |
|---|---|---|
| A. Mantener `400 Bad Request` | Aprobar `400` como contrato funcional de duplicado para UAT formal. | Menor cambio, compatible con comportamiento actual; menos expresivo para clientes. |
| B. Migrar a `409 Conflict` | Aprobar cambio de semantica HTTP para duplicados. | Mas alineado con conflicto de recurso; requiere pruebas API/SPA/clientes. |
| C. Implementar `Idempotency-Key` | Definir header, TTL, persistencia, scope, seguridad y errores. | Contrato API mas fuerte; requiere codigo, migracion y pruebas de concurrencia. |
| D. Implementar replay seguro | Definir almacenamiento de respuesta, reglas de payload equivalente y manejo de diferencias. | Mayor robustez para clientes; mayor complejidad operativa y de seguridad. |

## Recomendacion Tecnica

Para cierre UAT actual, aceptar documentalmente el contrato observado: duplicado equivalente retorna `400` JSON controlado y no persiste efectos secundarios adicionales.

Para preproductivo/productivo, recomendar evolucion controlada:

1. Definir codigo funcional estable, por ejemplo `ACH_TRANSACTION_DUPLICATE`.
2. Cambiar duplicado a `409 Conflict` solo con aprobacion de arquitectura/negocio y pruebas de clientes.
3. Evaluar `Idempotency-Key` si existen clientes externos con reintentos automaticos o riesgo de timeout.
4. Evaluar constraint unico o mecanismo transaccional si se requiere garantia fuerte ante concurrencia.

## Impacto En Clientes

Los clientes actuales deben tratar `400` con mensaje de duplicado como rechazo controlado no reintentable con el mismo payload. No deben asumir replay ni usar `Idempotency-Key` en este endpoint.

## Impacto En Pruebas

Las pruebas UAT y automatizadas deben validar:

- Primer intento crea transaccion.
- Duplicado retorna `400` JSON controlado.
- Duplicado no crea segunda transaccion.
- Duplicado no crea segundo evento inicial.
- Preview expone `WouldDuplicate=true` y `IdempotencyKey` informativo.

## Impacto En UAT

DEF-UAT-018 queda cerrado documentalmente para el contrato actual observado. Queda pendiente decision funcional/arquitectonica si se desea evolucionar el contrato antes de preproductivo/productivo.

Productivo sigue **NO-GO**.
