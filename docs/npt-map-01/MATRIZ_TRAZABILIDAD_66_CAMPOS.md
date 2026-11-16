# Matriz de trazabilidad NPT-MAP-01 — 66 elementos WSDL/XSD

Base: WSDL/XSD originales anexos a Sources; recursos embebidos comparados por SHA-256. `minOccurs=0` refleja opcionalidad XSD, no decisión de obligatoriedad funcional. Estado de los mappings **del código/seed**, no evidencia de datos productivos ni homologación bancaria.

| N° | Operación | Campo | XSD tipo | minOccurs | Fuente/interpretación preservada | Estado/gate | Evidencia |
|---:|---|---|---|---:|---|---|---|
| 1 | `Proc_Transacciones` | `TREG` | `string` | 0 | Sin fuente dedicada acreditada en selector | Sin fuente específica; revisar seed genérico | WSDL/XSD + seed |
| 2 | `Proc_Transacciones` | `TIPTRAN` | `int` | 0 | `transaction.transactionCode` | Baseline conservado | WSDL/XSD + seed |
| 3 | `Proc_Transacciones` | `BCORECEP` | `int` | 0 | `destinationInstitution.transitCodeNormalized` | Baseline conservado | WSDL/XSD + seed |
| 4 | `Proc_Transacciones` | `BCOORIG` | `int` | 0 | `sourceInstitution.transitCodeNormalized` | Baseline conservado | WSDL/XSD + seed |
| 5 | `Proc_Transacciones` | `NORIG` | `string` | 0 | `batchHeaders.companyName` | Protección NACHA-M | readiness + mapper + XSD |
| 6 | `Proc_Transacciones` | `NCTAORIG` | `string` | 0 | `transaction.sourceAccountNumber` | Protección NACHA-M; Live fail-closed sin origen acreditado | readiness + mapper + XSD |
| 7 | `Proc_Transacciones` | `IDORIG` | `string` | 0 | `batchHeaders.companyId` | Protección NACHA-M | readiness + mapper + XSD |
| 8 | `Proc_Transacciones` | `DESTRAN` | `string` | 0 | `procTransacciones.paymentInformation` | Baseline conservado | WSDL/XSD + seed |
| 9 | `Proc_Transacciones` | `FECEFEC` | `int` | 0 | `transaction.effectiveEntryDate` | Baseline conservado | WSDL/XSD + seed |
| 10 | `Proc_Transacciones` | `NCTARECEP` | `string` | 0 | `entrydetails.accountnumber` | Protección NACHA-M | readiness + mapper + XSD |
| 11 | `Proc_Transacciones` | `MONTO` | `double` | 0 | `transaction.amount` | Baseline conservado | WSDL/XSD + seed |
| 12 | `Proc_Transacciones` | `NRECEP` | `string` | 0 | `entryDetails.recipUserName` | Baseline conservado | WSDL/XSD + seed |
| 13 | `Proc_Transacciones` | `IDRECEP` | `string` | 0 | `entryDetails.recipIdNumber` | Baseline conservado | WSDL/XSD + seed |
| 14 | `Proc_Transacciones` | `DISCRE` | `string` | 0 | Sin fuente dedicada acreditada en selector | Sin fuente específica; revisar seed genérico | WSDL/XSD + seed |
| 15 | `Proc_Transacciones` | `CONV` | `string` | 0 | Sin fuente dedicada acreditada en selector | Sin fuente específica; revisar seed genérico | WSDL/XSD + seed |
| 16 | `Proc_Transacciones` | `PROD` | `string` | 0 | Sin fuente dedicada acreditada en selector | Sin fuente específica; revisar seed genérico | WSDL/XSD + seed |
| 17 | `Proc_Transacciones` | `INFPAG` | `string` | 0 | `procTransacciones.paymentInformation` | Baseline conservado | WSDL/XSD + seed |
| 18 | `Proc_Transacciones` | `IDTRAN` | `long` | 0 | `transaction.traceSequenceNumber` | Baseline conservado | WSDL/XSD + seed |
| 19 | `Proc_Transacciones` | `IDLOTE` | `string` | 0 | `procTransacciones.functionalBatchId` | Baseline conservado | WSDL/XSD + seed |
| 20 | `Proc_Transacciones` | `REGLOTE` | `long` | 0 | Sin fuente dedicada acreditada en selector | Sin fuente específica; revisar seed genérico | WSDL/XSD + seed |
| 21 | `Proc_Transacciones` | `IREVER` | `int` | 0 | Sin fuente dedicada acreditada en selector | Sin fuente específica; revisar seed genérico | WSDL/XSD + seed |
| 22 | `Proc_Transacciones` | `LIBRE` | `string` | 0 | Sin fuente dedicada acreditada en selector | Sin fuente específica; revisar seed genérico | WSDL/XSD + seed |
| 23 | `Proc_Transacciones` | `IDCAMCOMPE` | `int` | 0 | `cycle.clearingHouseId` | Baseline conservado | WSDL/XSD + seed |
| 24 | `Proc_Transacciones` | `DIRECCIONIP` | `string` | 0 | Sin fuente dedicada acreditada en selector | Sin fuente específica; revisar seed genérico | WSDL/XSD + seed |
| 25 | `Proc_Transacciones` | `LIBRE1` | `int` | 0 | Sin fuente dedicada acreditada en selector | Sin fuente específica; revisar seed genérico | WSDL/XSD + seed |
| 26 | `Proc_Transacciones` | `RTAACH` | `string` | 0 | Sin fuente dedicada acreditada en selector | Sin fuente específica; revisar seed genérico | WSDL/XSD + seed |
| 27 | `Proc_Transacciones` | `RTALOC` | `string` | 0 | Sin fuente dedicada acreditada en selector | Sin fuente específica; revisar seed genérico | WSDL/XSD + seed |
| 28 | `Proc_TransaccionesResponse` | `RTAACH` | `string` | 0 | SOAP response definido en WSDL/XSD (no fuente request) | Contrato de respuesta; parser conservado | WSDL/XSD |
| 29 | `Proc_TransaccionesResponse` | `RTALOC` | `string` | 0 | SOAP response definido en WSDL/XSD (no fuente request) | Contrato de respuesta; parser conservado | WSDL/XSD |
| 30 | `Proc_Contrapartidas` | `OFNIT` | `string` | 0 | `transaction.companyidentification` | Mapping existente, no alterado | WSDL/XSD + bootstrapper |
| 31 | `Proc_Contrapartidas` | `OFEMP` | `string` | 0 | Sin fuente homologada (no sustituir) | BLOQUEADO: readiness + resolver + transporte | guard compartido + NO-GO pre-POST |
| 32 | `Proc_Contrapartidas` | `OFCTA` | `string` | 0 | `transaction.sourceAccountNumber` | Inmutable: sin defaults/fixed/expresiones/transformación | readiness + resolver + igualdad exacta |
| 33 | `Proc_Contrapartidas` | `OFDD` | `string` | 0 | `transaction.debitCreditIndicator` | Mapping existente, no alterado | WSDL/XSD + bootstrapper |
| 34 | `Proc_Contrapartidas` | `OFFECHEFEC` | `string` | 0 | `transaction.effectiveEntryDate` | Mapping existente, no alterado | WSDL/XSD + bootstrapper |
| 35 | `Proc_Contrapartidas` | `OFMONDEB` | `decimal` | 0 | `transaction.amount` | Mapping existente, no alterado | WSDL/XSD + bootstrapper |
| 36 | `Proc_Contrapartidas` | `OFMONCRE` | `decimal` | 0 | `constant:0` | Mapping existente, no alterado | WSDL/XSD + bootstrapper |
| 37 | `Proc_Contrapartidas` | `OFIDARCH` | `int` | 0 | Sin fuente homologada (no sustituir) | BLOQUEADO: readiness + resolver + transporte | guard compartido + NO-GO pre-POST |
| 38 | `Proc_Contrapartidas` | `OFIDLOT` | `int` | 0 | `batch.id` | Mapping existente, no alterado | WSDL/XSD + bootstrapper |
| 39 | `Proc_Contrapartidas` | `OFST` | `string` | 0 | `constant:OO` | Mapping existente, no alterado | WSDL/XSD + bootstrapper |
| 40 | `Proc_Contrapartidas` | `OFIDTX` | `string` | 0 | `transaction.reference` | Inmutable: sin defaults/fixed/expresiones/transformación | readiness + resolver + igualdad exacta |
| 41 | `Proc_Contrapartidas` | `OFIDREVER` | `int` | 0 | `constant:0` | Mapping existente, no alterado | WSDL/XSD + bootstrapper |
| 42 | `Proc_Contrapartidas` | `OFIDEBAPLI` | `int` | 0 | `transaction.id` | Mapping existente, no alterado | WSDL/XSD + bootstrapper |
| 43 | `Proc_Contrapartidas` | `OFIDCAMCOMPE` | `int` | 0 | `clearinghouse.id` | Mapping existente, no alterado | WSDL/XSD + bootstrapper |
| 44 | `Proc_Contrapartidas` | `OFDIRECCIONIP` | `string` | 0 | Sin fuente homologada (no sustituir) | BLOQUEADO: readiness + resolver + transporte | guard compartido + NO-GO pre-POST |
| 45 | `Proc_Contrapartidas` | `OFLIBRE` | `string` | 0 | `transaction.reference` | Mapping existente, no alterado | WSDL/XSD + bootstrapper |
| 46 | `Proc_Contrapartidas` | `OFLIBRE1` | `int` | 0 | `transaction.id` | Mapping existente, no alterado | WSDL/XSD + bootstrapper |
| 47 | `Proc_Contrapartidas` | `ANSIDLOTE` | `int` | 0 | Sin fuente acreditada / seed ausente | Sin evidencia adicional: no inferir | WSDL/XSD + bootstrapper |
| 48 | `Proc_Contrapartidas` | `ANSST` | `string` | 0 | Sin fuente acreditada / seed ausente | Sin evidencia adicional: no inferir | WSDL/XSD + bootstrapper |
| 49 | `Proc_Contrapartidas` | `ANCLC` | `string` | 0 | Sin fuente acreditada / seed ausente | Sin evidencia adicional: no inferir | WSDL/XSD + bootstrapper |
| 50 | `Proc_Contrapartidas` | `ANSIDTX` | `string` | 0 | Sin fuente acreditada / seed ausente | Sin evidencia adicional: no inferir | WSDL/XSD + bootstrapper |
| 51 | `Proc_Contrapartidas` | `ANSIDREVER` | `int` | 0 | Sin fuente acreditada / seed ausente | Sin evidencia adicional: no inferir | WSDL/XSD + bootstrapper |
| 52 | `Proc_ContrapartidasResponse` | `ANSIDLOTE` | `int` | 0 | SOAP response definido en WSDL/XSD (no fuente request) | Contrato de respuesta; parser conservado | WSDL/XSD |
| 53 | `Proc_ContrapartidasResponse` | `ANSST` | `string` | 0 | SOAP response definido en WSDL/XSD (no fuente request) | Contrato de respuesta; parser conservado | WSDL/XSD |
| 54 | `Proc_ContrapartidasResponse` | `ANCLC` | `string` | 0 | SOAP response definido en WSDL/XSD (no fuente request) | Contrato de respuesta; parser conservado | WSDL/XSD |
| 55 | `Proc_ContrapartidasResponse` | `ANSIDTX` | `string` | 0 | SOAP response definido en WSDL/XSD (no fuente request) | Contrato de respuesta; parser conservado | WSDL/XSD |
| 56 | `Proc_ContrapartidasResponse` | `ANSIDREVER` | `int` | 0 | SOAP response definido en WSDL/XSD (no fuente request) | Contrato de respuesta; parser conservado | WSDL/XSD |
| 57 | `RegistrarRespuestaTransaccion` | `idCanal` | `int` | 0 | `differentialResponse.idCanal` | Flujo diferencial conservado | WSDL/XSD + seed existente |
| 58 | `RegistrarRespuestaTransaccion` | `nombreCanal` | `string` | 0 | `differentialResponse.nombreCanal` | Flujo diferencial conservado | WSDL/XSD + seed existente |
| 59 | `RegistrarRespuestaTransaccion` | `idTransaccion` | `string` | 0 | `differentialResponse.idTransaccion` | Flujo diferencial conservado | WSDL/XSD + seed existente |
| 60 | `RegistrarRespuestaTransaccion` | `idEstado` | `int` | 0 | `differentialResponse.idEstado` | Flujo diferencial conservado | WSDL/XSD + seed existente |
| 61 | `RegistrarRespuestaTransaccion` | `causal` | `string` | 0 | `differentialResponse.codigoCausalExterna` | Flujo diferencial conservado | WSDL/XSD + seed existente |
| 62 | `RegistrarRespuestaTransaccion` | `idTransaccionAxon` | `int` | 0 | `differentialResponse.idTransaccionServicioExterno` | Flujo diferencial conservado | WSDL/XSD + seed existente |
| 63 | `RegistrarRespuestaTransaccion` | `descripcionCausal` | `string` | 0 | `differentialResponse.descripcionCausalExterna` | Flujo diferencial conservado | WSDL/XSD + seed existente |
| 64 | `RegistrarRespuestaTransaccionResponse` | `codigoError` | `string` | 0 | SOAP response definido en WSDL/XSD (no fuente request) | Contrato de respuesta; parser conservado | WSDL/XSD |
| 65 | `RegistrarRespuestaTransaccionResponse` | `descripcionError` | `string` | 0 | SOAP response definido en WSDL/XSD (no fuente request) | Contrato de respuesta; parser conservado | WSDL/XSD |
| 66 | `RegistrarRespuestaTransaccionResponse` | `existeError` | `boolean` | 0 | SOAP response definido en WSDL/XSD (no fuente request) | Contrato de respuesta; parser conservado | WSDL/XSD |

## Inventario verificado

- Campos: Proc_Transacciones=27, Proc_TransaccionesResponse=2, Proc_Contrapartidas=22, Proc_ContrapartidasResponse=5, RegistrarRespuestaTransaccion=7, RegistrarRespuestaTransaccionResponse=3 = 66.
- Recursos oficiales: 7 de 7 idénticos por SHA-256 a los versionados en el proyecto.
- Las operaciones `Proc_Transacciones` y `Proc_Contrapartidas` son monetarias; Axon es notificación diferencial no monetaria.
- Los tres campos `OFEMP`, `OFIDARCH`, `OFDIRECCIONIP` permanecen no acreditados. No se habilita `Proc_Contrapartidas` ni siquiera mediante sobrecarga XML directa.
- `NCTAORIG` únicamente desde `Transactions.SourceAccountNumber`, nunca `EntryDetails.AccountNumber`; sin origen verificable permanece NO-GO Live.
- El barrido independiente con el nombre exacto `BARRIDO_INTEGRAL_SOAP_66_CAMPOS_ACH_CENIT_2026-11-16.md` no apareció en el ZIP examinado. La presente matriz se reconstruyó del contrato WSDL/XSD original y las definiciones de mapping del ZIP, sin suponer conclusiones no accesibles.
