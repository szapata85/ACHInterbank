# I07-A — Casos preparados para I07-B (sin ejecutar)

**Todos los casos están `PENDING_OFFICIAL_UAT`.** Las pruebas xUnit nombradas son evidencia técnica a ejecutar, **no** evidencia bancaria. No reutilizar `OK` de matrices UAT históricas para el artefacto I07-A.

| Caso | Incremento / servicio | Escenario oficial a demostrar | Resultado esperado y control | Evidencia a registrar | Estado |
|---|---|---|---|---|---|
| SM-01 | I01 `Proc_Contrapartidas` | Dos débitos distintos en el mismo ciclo | Dos solicitudes individuales; `OFIDTX`/`OFCTA` y respuesta asociados a su transacción; sin atribución cruzada | IDs pseudonimizados, request/response metadata, `AchTransactionId`, intento y auditoría | PENDING |
| SM-02 | I01 contrapartidas | Respuestas mixtas (éxito + rechazo) | Estado independiente por débito; fallida no contamina exitosa | códigos SOAP, estados persistidos por ID | PENDING |
| SM-03 | I01 contrapartidas | Timeout/estado remoto incierto y replay manual | `RequiereRevisionManual`, sin retransmisión monetaria ciega; éxito ya registrado nunca se reenvía | conteo de llamadas, intentos, marca de conciliación | PENDING |
| SM-04 | I01/I04 concurrencia | Dos workers/reintento sobre el mismo ítem | Una transmisión efectiva por identidad; ninguna duplicación ante carreras | IDs de leases, concurrencia y número de envíos, DB | PENDING |
| SM-05 | I02 contrapartidas | Débito monetario positivo válido | `OFDD=D`, `OFMONDEB=Amount > 0`, `OFMONCRE=0` | trace de mapping + XML saneado | PENDING |
| SM-06 | I02 contrapartidas | Crédito, prenotificación, monto negativo/cero o mapping inconsistente | Rechazo previo a SOAP; ninguna transmisión | código de validación + contador de llamadas=0 | PENDING |
| SM-07 | I02 contrapartidas | Homologar `OFEMP`, `OFIDARCH`, `OFIDLOT`, `OFST`, `OFDIRECCIONIP` | Fuente y significado aprobados **por banco**; sin sustitución por constante supuesta | matriz aprobada con versión y responsable | BLOCKED_FUNCTIONAL |
| SM-08 | I03 `Proc_Transacciones` | Crédito válido con NACHA tipo 6 | `NCTARECEP`, `MONTO`, `TIPTRAN`, `IDTRAN` coinciden con entrada persistida | trace vs NACHA sintético, metadata SOAP | PENDING |
| SM-09 | I03 transacciones | NACHA/BD inconsistente (cuenta/monto/código/trace) | Bloquear antes de invocar SOAP | validación específica y llamadas=0 | PENDING |
| SM-10 | I03 transacciones | Cuentas de origen y destino **distintas** | `NCTAORIG` obtenido de fuente homologada, nunca del receptor; sin fuente no hay Live | resolución fuente/cuenta enmascarada + conformidad bancaria | BLOCKED_FUNCTIONAL |
| SM-11 | I03 transacciones | `IDORIG` e `INFPAG` | Semántica aceptada por contraparte (origen y texto pago) | especificación/acta bancaria y test de ejemplo saneado | BLOCKED_FUNCTIONAL |
| SM-12 | I04 `RegistrarRespuestaTransaccion` | Respuesta válida Axon | Se envían los 7 campos exactos; se persisten `existeError`, `codigoError`, `descripcionError` | request mapeado enmascarado, attempt/estado | PENDING |
| SM-13 | I04 respuesta Axon | Error funcional y error posgateway incierto | No confundir transporte con éxito; incierto exige conciliación y no se reenvía a ciegas | logs/attempt y decisiones, llamadas=1 | PENDING |
| SM-14 | I05 dos operaciones monetarias | Legacy frente a contract-driven | Mismo método, `SOAPAction`, namespace, orden, valores y tratamiento null/vacío según contrato | TRX de paridad y diffs sanitizados | PENDING |
| SM-15 | I05 Axon | Paridad 7 campos legacy frente a contract-driven | Contrato v1 exacto, no se agregan ni alteran campos | TRX de política/contrato | PENDING |
| SM-16 | I05/P8.4 | Versionado, rollback, nuevo campo no-code y migraciones dual-provider | Gates P8.4 obligatorios PASS en el candidato exacto; sin cutover previo | TRX dual-provider + manifiesto + decisión de corte | PENDING |
| SM-17 | I06/I07-B configuración | Ambiente oficial UAT y overrides | Configuración efectiva autorizada; cero endpoints/credenciales productivos en pruebas | registro de ambiente y aprobación de seguridad | BLOCKED_ENVIRONMENT |
| SM-18 | I07-B seguridad operacional | Cancelación/reinicio después del acuse + rollback | No duplicación; persistencia y reconciliación correctas | logs saneados, estado DB, decisión de rollback | PENDING |

## Pruebas locales de respaldo ya escritas (ejecución pendiente)

| Incremento | Clase(s) de tests en `tests/Cfa.ACHInterbank.Tests/` |
|---|---|
| I01 | `ContrapartidaDispatchJobServiceTests.cs`, `ProcContrapartidasFallbackClosureTests.cs` |
| I02 | `IntegrationMappingEndToEndTests.cs` |
| I03 | `NachaDesagregadoIntegrationMappingTests.cs`, `ProcTransaccionesOriginAccountSafetyTests.cs` |
| I04 | `NotificarRespuestaAchUseCaseTests.cs`, `RegistrarRespuestaAchContractsTests.cs` |
| I05 | `CfaSoapOperationParityGuardTests.cs`, `IntegrationContractDrivenSoapBodyWriterTests.cs` |

**Bloqueos que no pueden cerrarse en ChatGPT:** homologación bancaria de 8 campos/semánticas, resultados .NET/DB reales, certificación P8.4, autorización de entorno UAT y evidencia de recepción del banco. Cada prueba oficial requiere resultado observado, no basta con la expectativa de esta matriz.
