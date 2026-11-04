# I06 — Inventario consolidado de los campos SOAP

**Origen:** `IntegrationCatalogBootstrapper.cs` y `IntegrationMappingBootstrapper.cs` del ZIP I05; decisiones I01–I05. **NO** es un export de base de datos ni un certificado de banco.

Los valores `Seed` significan *regla vigente de referencia*, no homologación. Los campos reservados o no configurados no deben rellenarse con datos inventados.

## Proc_Contrapartidas — 22 campos catalogados

| Elemento SOAP | Regla / origen actual en código | Estado documental I06 |
|---|---|---|
| `OFNIT` | `transaction.companyidentification` | Seed; revisión ejecutable pendiente |
| `OFEMP` | `clearinghouse.code` | BLOCKED_FUNCTIONAL |
| `OFCTA` | `transaction.sourceAccountNumber` | Seed; validar funcionalmente |
| `OFDD` | `transaction.debitCreditIndicator` | GUARDED_I02 (D) |
| `OFFECHEFEC` | `transaction.effectiveEntryDate` | Seed |
| `OFMONDEB` | `transaction.amount` | GUARDED_I02 (>0, exacto) |
| `OFMONCRE` | `constante 0` | GUARDED_I02 (0) |
| `OFIDARCH` | `batch.id` | BLOCKED_FUNCTIONAL |
| `OFIDLOT` | `batch.id` | BLOCKED_FUNCTIONAL |
| `OFST` | `constante OO` | BLOCKED_FUNCTIONAL |
| `OFIDTX` | `transaction.reference` | Seed; correlación I01 |
| `OFIDREVER` | `constante 0` | Seed |
| `OFIDEBAPLI` | `transaction.id` | Seed |
| `OFIDCAMCOMPE` | `clearinghouse.id` | Seed |
| `OFDIRECCIONIP` | `constante 0.0.0.0` | BLOCKED_FUNCTIONAL |
| `OFLIBRE` | `transaction.reference` | Seed |
| `OFLIBRE1` | `transaction.id` | Seed |
| `ANSIDLOTE` | `sin regla publicada` | Reservado/respuesta legacy |
| `ANSST` | `sin regla publicada` | Reservado/respuesta legacy |
| `ANCLC` | `sin regla publicada` | Reservado/respuesta legacy |
| `ANSIDTX` | `sin regla publicada` | Reservado/respuesta legacy |
| `ANSIDREVER` | `sin regla publicada` | Reservado/respuesta legacy |

## Proc_Transacciones — 27 campos (25 de entrada + 2 de salida)

| Elemento SOAP | Regla / origen actual en código | Estado documental I06 |
|---|---|---|
| `TREG` | `sin regla` | Opcional |
| `TIPTRAN` | `transaction.transactionCode` | GUARDED_I03 |
| `BCORECEP` | `destinationInstitution.transitCodeNormalized` | Seed |
| `BCOORIG` | `sourceInstitution.transitCodeNormalized` | Seed |
| `NORIG` | `sourceInstitution.name` | Seed |
| `NCTAORIG` | `entrydetails.accountnumber` | BLOCKED_FUNCTIONAL; veto Live |
| `IDORIG` | `transaction.companyIdentification` | Semántica pendiente |
| `DESTRAN` | `procTransacciones.paymentInformation` | Seed: texto funcional |
| `FECEFEC` | `transaction.effectiveEntryDate` | Seed |
| `NCTARECEP` | `transaction.destinationAccountNumber` | GUARDED_I03 |
| `MONTO` | `transaction.amount` | GUARDED_I03 |
| `NRECEP` | `entryDetails.recipUserName` | Seed |
| `IDRECEP` | `entryDetails.recipIdNumber` | Seed |
| `DISCRE` | `constante V` | Seed |
| `CONV` | `sin regla` | Opcional |
| `PROD` | `sin regla` | Opcional |
| `INFPAG` | `procTransacciones.paymentInformation` | Semántica pendiente |
| `IDTRAN` | `transaction.traceSequenceNumber` | GUARDED_I03 |
| `IDLOTE` | `procTransacciones.functionalBatchId` | Seed |
| `REGLOTE` | `sin regla` | Opcional |
| `IREVER` | `constante 0` | Seed |
| `LIBRE` | `sin regla` | Opcional |
| `IDCAMCOMPE` | `cycle.clearingHouseId` | Seed |
| `DIRECCIONIP` | `sin regla` | Opcional |
| `LIBRE1` | `sin regla` | Opcional |
| `RTAACH` | `respuesta SOAP` | Salida; parser |
| `RTALOC` | `respuesta SOAP` | Salida; parser |

## RegistrarRespuestaTransaccion — 7 entradas

| Elemento SOAP | Regla / origen actual en código | Estado documental I06 |
|---|---|---|
| `idCanal` | `AchResponseNotificationAttempts.IdCanal` | Validación I04 |
| `nombreCanal` | `AchResponseNotificationAttempts.NombreCanal` | Validación I04 |
| `idTransaccion` | `AchResponseNotificationAttempts.IdTransaccion` | Validación I04 |
| `idEstado` | `AchResponseNotificationAttempts.IdEstado` | Validación I04 |
| `causal` | `AchResponseNotificationAttempts.Causal` | Opcional |
| `idTransaccionAxon` | `AchResponseNotificationAttempts.IdTransaccionServicioExterno` | Validación I04 |
| `descripcionCausal` | `AchResponseNotificationAttempts.DescripcionCausal` | Opcional |

## Respuestas y correlación

- `Proc_Contrapartidas`: `ANS*` figura como campo contractual reservado/response legacy; no presumir que son entradas requeridas publicadas. El parser de respuesta y la persistencia por transacción se revisaron en I01.
- `Proc_Transacciones`: `RTAACH`/`RTALOC` son campos de salida; no fuentes de request.
- `RegistrarRespuestaTransaccion`: respuesta `existeError`, `codigoError`, `descripcionError` se persiste en el intento; revisar estados y reconciliación I04.
- Este inventario está definido para impedir cambios accidentales antes de homologación. El baseline legacy **no es autoridad funcional del banco**. Para incertidumbres consultar `DECISIONES_I02_CONTRAPARTIDAS.md` y `DECISIONES_I03_PROC_TRANSACCIONES.md`.
