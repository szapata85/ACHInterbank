# Matriz WSDL/XSD → implementación

Se emplearon los **ficheros oficiales recibidos**, embebidos sin modificación. `WSCFAACH` importa `xsd0` (target `http://tempuri.org/`) y `xsd1` (tipos de serialización); `WSAxonRespuestaTransacciones` importa `xsd0`, `xsd1` y `xsd2` (data contract `http://schemas.datacontract.org/2004/07/WSCFAACH`). `?xsd=...` del WSDL aparece como `localhost:7083`: **no se consulta**.

| Regla contractual comprobable | XSD/WSDL | Implementación | Resultado offline |
|---|---|---|---|
| SOAP 1.1 document/literal, namespace `http://tempuri.org/` | Ambos WSDL | `OfficialSoapContracts.VerifyBundledWsdlContracts`, parser CFA/Axon | PASS |
| `SOAPAction` exacto para `Proc_Transacciones` | WSCFAACH WSDL | `WscfaachSoapClient.ResolveConfigurationAsync`, paridad de mapping | PASS WSDL + control fuente |
| `SOAPAction` exacto para `RegistrarRespuestaTransaccion` | WSAxon WSDL | `WsAxonRespuestaTransaccionesSoapClient.ResolveConfigurationAsync` | PASS WSDL + control fuente |
| Solicitud CFA: 27 campos, en orden, tipos/nil | CFA `xsd0` | `ProcTransaccionesRequestMapper.BuildSoapBody`; proyección F5.3 | PASS casos offline |
| `NCTAORIG`: sexto campo, `xs:string`, nullable | CFA `xsd0` | XSD + `EnsureNctaOrigSource` exige `Transaction` y fuente real transaccional | PASS estructura; homologación NO inferible |
| `MONTO` `xs:double`, `FECEFEC` `xs:int`, `IDTRAN` `xs:long` | CFA `xsd0` | XMLSchemaSet pre-transport | PASS positivos/negativos |
| CFA respuesta única `RTAACH`, `RTALOC` en namespace correcto y orden | CFA `xsd0` | `ProcTransaccionesResponseParser` + esquema; código no vacío: guard adicional | PASS offline; .NET pendiente |
| Axon: siete campos request ordenados, ints/nillability | Axon `xsd0` | `RespuestaTransaccionesAchGateway` antes de transporte | PASS offline; .NET pendiente |
| Axon result: tipo data contract, `codigoError`, `descripcionError`, `existeError` `xs:boolean` | Axon `xsd0/xsd2` | `RegistrarRespuestaAchSoapResponseParser` + `XmlConvert.ToBoolean` | PASS offline; .NET pendiente |
| Importaciones locales y bloqueo DTD/externos | cinco XSD | `XmlSchemaSet` con imports enlazados, `XmlResolver=null`, XmlReader DTD prohibit | PASS guard fuente; .NET pendiente |
| Interpretación bancaria del éxito/rechazo, origen homologado y F5 sin ciclo | **No definido en XSD** | Guardas Live y rechazo ante incertidumbre preservados | **PENDIENTE aprobación externa** |

**No se presume que `minOccurs=0` signifique éxito:** los parsers exigen resultado presente/no nulo por seguridad interna. Las comprobaciones offline son independientes de la implementación C# y no acreditan homologación funcional.
