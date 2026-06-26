# Decisiones funcionales de mappings SOAP

Fecha: 2026-06-26  
Estado: Fase 3 documentada, sin implementacion  
Productivo: NO-GO

## 1. Resumen ejecutivo

Esta fase convierte las brechas detectadas en la matriz integral WSDL y en el endurecimiento de readiness en una hoja de decisiones funcionales para aprobacion del equipo funcional, proveedor/core y arquitectura.

No se cambian mappings, seeds, readiness, WSDL, Angular, Docker ni backend. El objetivo es dejar claro que campos requieren definicion antes de una Fase 4 de implementacion.

Estado general:

- `RegistrarRespuestaTransaccion` esta tecnicamente resuelto: conserva 7 parametros WSDL, usa fuentes `DifferentialResponse`, no mueve dinero y no tiene `ANS*` vigentes.
- `Proc_Contrapartidas` conserva `ANS*` donde corresponden por WSDL; esos campos son opcionales/reservados y no deben bloquear readiness.
- `Proc_Contrapartidas` tiene decisiones monetarias criticas pendientes, especialmente `OFMONDEB`, `OFMONCRE` y `OFDD`.
- `Proc_Transacciones` tiene decisiones de parametrizacion y direccion pendientes, especialmente `TREG`, `CONV`, `PROD`, `IDCAMCOMPE`, `RTAACH` y `RTALOC`.
- Fase 2 ya evita falso `OK` cuando campos funcionalmente criticos quedan cubiertos por placeholders como `SEED`, `TEST`, `0`, `1`, `REF-1`, `ACH`, `000010070`, `900123456`, `constant.value` o defaults sin politica funcional.

## 2. Estado actual despues de Fase 1 y Fase 2

### Fase 1 - UI de mappings

`/integraciones/mappings` reconoce fuentes tecnicas validas como:

- `Transaction`
- `Cycle`
- `ClearingHouse`
- `Batch`
- `Constant`
- `DifferentialResponse`
- fuentes NACHA desagregadas

Esto evita que reglas activas con fuentes tecnicas soportadas se vean falsamente como `Sin mapear`.

### Fase 2 - Readiness funcional

El readiness backend ahora puede reportar:

- `OK`
- `READY_WITH_WARNINGS`
- `FUNCTIONAL_MAPPING_PLACEHOLDER`
- `REGISTRAR_WSDL_CONTRACT_INVALID`

Evidencia interna encontrada:

- `IntegrationMappingReadinessService` contiene una lista de placeholders funcionalmente peligrosos.
- `IntegrationMappingReadinessService` valida que `RegistrarRespuestaTransaccion` exponga exactamente 7 parametros WSDL y no tenga `ANS*` activos.
- `TransactionIntegrationReadinessGuaranteeTests` cubre que placeholders criticos no retornen `OK`, que `ANS*` de `Proc_Contrapartidas` no bloqueen y que `PLValidarUsuarioBV` no se catalogue.

## 3. Alcance

Servicios dentro del alcance:

- `WSCFAACH.Proc_Transacciones`
- `WSCFAACH.Proc_Contrapartidas`
- `WSAXON.RegistrarRespuestaTransaccion`

Pantallas relacionadas:

- `/integraciones/mappings`: matriz funcional campo a campo.
- `/integraciones/soap-settings`: solo configuracion tecnica de endpoint, SOAP Action y enabled. No se mezcla con mappings.

## 4. Exclusiones

- `PLValidarUsuarioBV` no se usa. No debe catalogarse, sembrarse, aparecer en readiness, UI ni pruebas funcionales.
- No ejecutar SOAP real.
- No cambiar contratos WSDL.
- No modificar seeds/bootstrap.
- No cambiar readiness en esta fase.
- No tocar Angular.
- No tocar Docker.
- No tocar NACHA-M.
- No tocar OpenBao.
- No tocar logica monetaria SOAP.
- No cambiar endpoints publicos.
- No convertir placeholders en constantes oficiales sin aprobacion funcional.

## 5. Decisiones criticas pendientes

| Decision | Servicio | Parametros | Motivo | Bloquea Fase 4 |
|---|---|---|---|---|
| Semantica de monto real en debito/credito | `Proc_Contrapartidas` | `OFMONDEB`, `OFMONCRE` | Riesgo monetario directo si el monto queda en el campo equivocado o en ambos sin regla. | Si |
| Indicador debito/credito | `Proc_Contrapartidas` | `OFDD` | El catalogo sugiere `D/C`; el mapping actual usa `C` sin politica funcional confirmada. | Si |
| Fuente correcta de cuenta origen | `Proc_Contrapartidas` | `OFCTA` | Mapping actual usa `transaction.originatingdfi` con default `000010070`; puede ser DFI, no cuenta. | Si |
| Estado origen y reverso | `Proc_Contrapartidas` | `OFST`, `OFIDREVER` | Placeholders o defaults pueden afectar estado operacional/reversos. | Si |
| Direccion request/response | `Proc_Transacciones` | `RTAACH`, `RTALOC` | Parsers los tratan como respuesta; catalogo actual los tiene requeridos como input. | Si |

## 6. Decisiones altas pendientes

| Decision | Servicio | Parametros | Motivo | Bloquea Fase 4 |
|---|---|---|---|---|
| Constante oficial tipo registro | `Proc_Transacciones` | `TREG` | `SEED` no es valor funcional. El catalogo muestra ejemplo `6`, pero se requiere confirmacion oficial. | Si |
| Parametrizacion de convenio | `Proc_Transacciones` | `CONV` | Debe definirse si viene por convenio, camara, producto, transaccion o tabla parametrica. | Si |
| Parametrizacion de producto | `Proc_Transacciones` | `PROD` | `ACH` no debe asumirse oficial sin aprobacion. | Si |
| Camara compensadora | `Proc_Transacciones`, `Proc_Contrapartidas` | `IDCAMCOMPE`, `OFIDCAMCOMPE` | Debe soportar ACH/CENIT/multicamara sin constante generica `1`. | Si |
| Campos libres | Ambos servicios WSCFAACH | `DISCRE`, `LIBRE`, `LIBRE1`, `OFLIBRE`, `OFLIBRE1` | No se debe inventar semantica para campos libres. | Depende de decision funcional |

## 7. Decisiones de parametrizacion

| Decision | Parametros | Opciones candidatas | Evidencia requerida |
|---|---|---|---|
| Convenio | `CONV` | Parametro por camara; parametro por producto; tabla de convenio; valor por transaccion. | Manual operativo o respuesta del proveedor/core. |
| Producto | `PROD` | Producto interno; producto ACH/CENIT; codigo fijo homologado; derivado del tipo de transaccion. | Tabla oficial de productos o contrato funcional. |
| Camara | `IDCAMCOMPE`, `OFIDCAMCOMPE` | `clearinghouse.id`; `cycle.clearingHouseId`; constante homologada por ambiente. | Catalogo de camaras ACH/CENIT y regla multicamara. |
| IP | `DIRECCIONIP`, `OFDIRECCIONIP` | IP canal cliente; IP API; IP servidor; IP estacion; constante tecnica controlada. | Politica de auditoria/seguridad y requerimiento del proveedor. |
| Tipo registro | `TREG` | Constante oficial `6`; fuente NACHA; tabla parametrica. | Especificacion WSCFAACH o confirmacion proveedor. |

## 8. Decisiones de trazabilidad/auditoria

| Decision | Parametros | Riesgo | Recomendacion tecnica sin decidir negocio |
|---|---|---|---|
| IP de origen | `DIRECCIONIP`, `OFDIRECCIONIP` | Trazabilidad no confiable si se usa `SEED` o `0.0.0.0` sin politica. | Mantener warning hasta que seguridad/proveedor indiquen fuente oficial. |
| Identificador transaccional | `OFIDTX`, `IDTRAN`, `idTransaccion` | Conciliacion incompleta si se usa campo no oficial. | Documentar fuente aprobada y probar trazabilidad end-to-end. |
| Reversos | `IREVER`, `OFIDREVER` | Reversos falsos o no detectados. | Requerir decision funcional de valor normal, reverso y no aplica. |
| Campos libres | `DISCRE`, `LIBRE`, `LIBRE1`, `OFLIBRE`, `OFLIBRE1` | Datos irrelevantes o erroneos en payload. | Clasificar como reservado/opcional o definir fuente exacta. |

## 9. Parametros ya resueltos

### RegistrarRespuestaTransaccion

Resuelto tecnicamente:

- `idCanal`
- `nombreCanal`
- `idTransaccion`
- `idEstado`
- `idTransaccionAxon`

Resuelto como condicional, con validacion funcional de causal:

- `causal`
- `descripcionCausal`

Condicion: `causal` y `descripcionCausal` deben depender de respuesta diferencial y de la tabla de homologacion de estados/causales cuando aplique. No requieren decision monetaria.

### Proc_Contrapartidas

Resuelto como opcional/reservado:

- `ANSIDLOTE`
- `ANSST`
- `ANCLC`
- `ANSIDTX`
- `ANSIDREVER`

Estos parametros son validos solo para `Proc_Contrapartidas`. No deben aparecer como contrato vigente de `RegistrarRespuestaTransaccion`.

### PLValidarUsuarioBV

Resuelto por exclusion: no se usa.

## 10. Matriz de decisiones por servicio

### Proc_Contrapartidas

| Servicio | Parametro | Estado actual | Riesgo | Pregunta funcional | Opciones posibles | Recomendacion tecnica sin decidir negocio | Responsable sugerido | Bloquea Fase 4 | Evidencia requerida |
|---|---|---|---|---|---|---|---|---|---|
| Proc_Contrapartidas | `OFMONDEB` | Mapping seed usa constante `0`; Fase 2 lo trata como placeholder critico. | Riesgo monetario directo. | Para debito originado por CFA, el monto real debe enviarse en `OFMONDEB`, `OFMONCRE` o ambos segun naturaleza? | `OFMONDEB=amount` y `OFMONCRE=0`; `OFMONCRE=amount` y `OFMONDEB=0`; ambos segun tipo; otro modelo proveedor. | No modificar hasta tener regla oficial; mantener `FUNCTIONAL_MAPPING_PLACEHOLDER`. | Negocio ACH, Proveedor/Core, Arquitectura. | Si | Especificacion funcional WSCFAACH o respuesta escrita proveedor/core. |
| Proc_Contrapartidas | `OFMONCRE` | Mapping usa `transaction.amount` con default `0`. | Fallback `0` puede ocultar ausencia de monto; posible inversion debito/credito. | En contrapartidas, `OFMONCRE` aplica para debitos, creditos o solo contrapartida contable? | Igual que `OFMONDEB`; condicional por naturaleza; no aplica. | Si la fuente `transaction.amount` se conserva, el default `0` debe quedar warning o eliminarse en Fase 4 con aprobacion. | Negocio ACH, Proveedor/Core. | Si | Tabla de reglas debito/credito por operacion. |
| Proc_Contrapartidas | `OFDD` | Mapping seed usa constante `C`; catalogo indica indicador `D/C`. | Operacion puede viajar con naturaleza incorrecta. | Para debito originado por CFA, el valor esperado es `D`, `C` u otro codigo? | Constante homologada; derivado de tipo transaccion; derivado de operacion SOAP. | No asumir `C`; mantener bloqueo hasta aprobacion. | Negocio ACH, Proveedor/Core. | Si | Manual/proveedor con valores validos. |
| Proc_Contrapartidas | `OFCTA` | Mapping usa `transaction.originatingdfi` con default `000010070`. | Puede enviar DFI donde se espera cuenta origen. | `OFCTA` debe ser cuenta origen real, DFI originador, cuenta contable o identificador de entidad? | `transaction.sourceAccountNumber`; cuenta parametrica CFA; DFI; otra fuente core. | No cambiar a cuenta por criterio tecnico; pedir definicion. | Negocio, Core Bancario, Arquitectura. | Si | Diccionario de datos WSCFAACH y regla de cuenta origen. |
| Proc_Contrapartidas | `OFST` | Mapping seed cae en constante generica `SEED`. | Estado origen invalido o no trazable. | Que estados origen acepta el core para esta operacion? | Estado interno ACH; estado core; constante homologada; no aplica. | Requiere tabla de estados; si no aplica, reclasificar como reservado con aprobacion. | Negocio, Proveedor/Core. | Si | Tabla oficial de estados. |
| Proc_Contrapartidas | `OFIDREVER` | Mapping seed cae en constante `1`; catalogo sugiere `0` si no aplica reverso. | Puede marcar reverso indebidamente. | Cual es el valor para operacion normal y cual para reverso? | `0` normal; null/no aplica; id reverso real; flag por tipo. | No decidir `0` ni `1`; requerir semantica oficial. | Negocio, Operaciones ACH. | Si | Especificacion de reversos. |
| Proc_Contrapartidas | `OFLIBRE` | Mapping seed cae en `SEED`. | Campo libre puede contener basura funcional. | El campo se usa por core/proveedor o es reservado? | Reservado vacio; observacion; codigo convenio; constante homologada. | Marcar pendiente; no usar `SEED`. | Negocio, Proveedor/Core. | Depende | Confirmacion de uso del campo. |
| Proc_Contrapartidas | `OFLIBRE1` | Mapping seed cae en `1`. | Campo numerico libre puede inducir reglas no previstas. | Que representa el campo libre numerico? | Reservado; secuencia; codigo operacion; no aplica. | No inferir desde nombre; pedir definicion. | Negocio, Proveedor/Core. | Depende | Confirmacion funcional. |
| Proc_Contrapartidas | `OFDIRECCIONIP` | Mapping usa constante tecnica `0.0.0.0`; Fase 2 la trata como warning. | Trazabilidad debil, no necesariamente monetaria. | Que IP exige el proveedor: canal, API, servidor, estacion o constante tecnica? | IP real de canal; IP API; IP servidor; constante controlada. | Mantener `READY_WITH_WARNINGS` hasta politica de seguridad/proveedor. | Seguridad, Operaciones, Proveedor. | No, si el proveedor acepta warning | Politica de auditoria/IP. |
| Proc_Contrapartidas | `OFIDCAMCOMPE` | Mapping usa `clearinghouse.id` con default `1`; Fase 2 lo trata como warning si hay fuente. | Riesgo multicamara si cae en default. | El id de camara debe venir de ciclo, clearing house, parametro ambiente o tabla proveedor? | `clearinghouse.id`; `cycle.clearingHouseId`; constante homologada por ambiente. | Usar fuente funcional solo cuando negocio confirme catalogo ACH/CENIT; default debe ser eliminado o controlado. | Arquitectura, Negocio ACH/CENIT. | Si en multicamara | Catalogo oficial de camaras y equivalencias. |
| Proc_Contrapartidas | `ANSIDLOTE` | Activo en catalogo, `Required=false`, sin regla publicada. | Bajo si se confirma como response/reservado. | Estos campos deben mapearse en request o son solo respuesta/reservados? | Mantener opcional; mapear desde respuesta; no usar. | Mantener opcional/reservado y no bloquear readiness. | Proveedor/Core. | No | Confirmacion WSDL/direccion. |
| Proc_Contrapartidas | `ANSST` | Activo en catalogo, `Required=false`, sin regla publicada. | Bajo si es response/reservado. | Igual que `ANSIDLOTE`. | Mantener opcional; mapear respuesta; no usar. | Mantener opcional/reservado. | Proveedor/Core. | No | Confirmacion WSDL/direccion. |
| Proc_Contrapartidas | `ANCLC` | Activo en catalogo, `Required=false`, sin regla publicada. | Bajo si es response/reservado. | Igual que `ANSIDLOTE`. | Mantener opcional; mapear respuesta; no usar. | Mantener opcional/reservado. | Proveedor/Core. | No | Confirmacion WSDL/direccion. |
| Proc_Contrapartidas | `ANSIDTX` | Activo en catalogo, `Required=false`, sin regla publicada. | Bajo si es response/reservado. | Igual que `ANSIDLOTE`. | Mantener opcional; mapear respuesta; no usar. | Mantener opcional/reservado. | Proveedor/Core. | No | Confirmacion WSDL/direccion. |
| Proc_Contrapartidas | `ANSIDREVER` | Activo en catalogo, `Required=false`, sin regla publicada. | Bajo si es response/reservado. | Igual que `ANSIDLOTE`. | Mantener opcional; mapear respuesta; no usar. | Mantener opcional/reservado. | Proveedor/Core. | No | Confirmacion WSDL/direccion. |

### Proc_Transacciones

| Servicio | Parametro | Estado actual | Riesgo | Pregunta funcional | Opciones posibles | Recomendacion tecnica sin decidir negocio | Responsable sugerido | Bloquea Fase 4 | Evidencia requerida |
|---|---|---|---|---|---|---|---|---|---|
| Proc_Transacciones | `TREG` | Mapping seed usa `SEED`; catalogo muestra ejemplo `6`; Fase 2 permite `6` solo como constante funcional homologable si se aprueba. | Payload invalido si no se confirma tipo registro. | Existe constante oficial para tipo de registro? Es siempre `6`? | Constante `6`; fuente NACHA; tabla parametrica. | Pedir confirmacion formal antes de usar `6` como oficial. | Proveedor/Core, Negocio ACH. | Si | Manual WSCFAACH o respuesta proveedor. |
| Proc_Transacciones | `CONV` | Mapping seed usa `SEED`. | Convenio invalido o imposible de enrutar. | De donde debe salir el convenio? | Parametro por camara; producto; convenio por cliente; transaccion; core. | No inventar fuente; definir tabla o fuente transaccional en Fase 4. | Negocio, Core, Producto. | Si | Tabla oficial de convenios. |
| Proc_Transacciones | `PROD` | Mapping seed usa `SEED` o default generico `ACH` en contextos. | Producto no homologado. | Que codigo de producto espera el servicio? | Producto core; producto ACH/CENIT; constante homologada; parametro por convenio. | No asumir `ACH`; requiere definicion. | Negocio, Producto, Proveedor. | Si | Catalogo de productos. |
| Proc_Transacciones | `IDCAMCOMPE` | Mapping seed usa constante `1`. | Error en ACH/CENIT o ambiente multicamara. | El id de camara debe venir de ciclo, clearing house o parametro homologado? | `clearinghouse.id`; `cycle.clearingHouseId`; constante por ambiente. | Preferir fuente funcional si existe, pero no cambiar hasta equivalencia oficial. | Arquitectura, Negocio ACH/CENIT. | Si | Equivalencia camaras proveedor. |
| Proc_Transacciones | `DIRECCIONIP` | Mapping seed usa `SEED`. | Trazabilidad invalida o rechazo por proveedor si valida IP. | Que IP debe enviarse? | IP canal; IP API; IP servidor; constante tecnica. | Mantener como decision de auditoria; si proveedor no valida puede ser warning. | Seguridad, Operaciones, Proveedor. | No, salvo proveedor la exija | Politica IP. |
| Proc_Transacciones | `DISCRE` | Mapping seed usa `SEED`. | Campo discrecional con basura funcional. | El campo es obligatorio para core o reservado/opcional? | Vacio; observacion; codigo discrecional; fuente NACHA. | No inventar; clasificar reservado si proveedor lo aprueba. | Negocio, Proveedor. | Depende | Confirmacion de uso del campo. |
| Proc_Transacciones | `IREVER` | Mapping seed usa `1`; catalogo sugiere indicador reverso. | Puede marcar reverso indebidamente. | Cual valor representa operacion normal y cual reverso? | `0` normal; `1` reverso; id reverso real; no aplica. | Requiere definicion; no asumir. | Negocio, Operaciones ACH. | Si | Regla de reversos. |
| Proc_Transacciones | `LIBRE` | Mapping seed usa `SEED`. | Campo libre no homologado. | Se usa o debe viajar vacio/reservado? | Reservado vacio; observacion; codigo negocio; no aplica. | No asignar fuente sin definicion. | Negocio, Proveedor. | Depende | Confirmacion funcional. |
| Proc_Transacciones | `LIBRE1` | Mapping usa `fileControls.blockCount`; Fase 2 lo trata como warning por semantica dudosa. | Puede usar conteo de bloques donde se espera otro dato. | Que representa `LIBRE1` para el servicio? | Reservado; secuencia; conteo; campo core especifico. | Mantener warning; no bloquear si proveedor confirma no uso. | Negocio, Proveedor/Core. | No si se confirma reservado | Confirmacion de semantica. |
| Proc_Transacciones | `RTAACH` | Catalogado requerido input con placeholder; parser lo trata como campo de respuesta. | Direccion incorrecta puede contaminar request/readiness. | Es parametro de entrada o solo respuesta? | Output/response; input requerido; reservado. | No cambiar direccion hasta WSDL/proveedor; mantener warning o bloqueo segun confirmacion. | Proveedor/Core, Arquitectura. | Si | WSDL oficial con direccion o ejemplo SOAP. |
| Proc_Transacciones | `RTALOC` | Catalogado requerido input con placeholder; parser lo trata como campo de respuesta. | Igual que `RTAACH`. | Es parametro de entrada o solo respuesta? | Output/response; input requerido; reservado. | No cambiar direccion hasta WSDL/proveedor. | Proveedor/Core, Arquitectura. | Si | WSDL oficial con direccion o ejemplo SOAP. |
| Proc_Transacciones | `NCTAORIG` | Mapping usa `batchHeaders.companyId`; semantica cuenta origen no confirmada. | Cuenta origen incorrecta. | `NCTAORIG` es cuenta origen real o identificacion de empresa? | Cuenta origen; companyId; cuenta core parametrica. | Requiere validacion funcional antes de go-live. | Negocio, Core. | Si si proveedor valida cuenta | Diccionario de datos. |
| Proc_Transacciones | `REGLOTE` | Mapping usa `batchControls.entryAddendaCount`. | Conteo puede no representar registro lote. | `REGLOTE` es numero secuencial, conteo, id de lote u otro? | Conteo; secuencia; batch number; campo core. | Mantener pendiente hasta definicion. | Negocio, Proveedor. | Depende | Especificacion campo. |

### RegistrarRespuestaTransaccion

| Servicio | Parametro | Estado actual | Riesgo | Pregunta funcional | Opciones posibles | Recomendacion tecnica sin decidir negocio | Responsable sugerido | Bloquea Fase 4 | Evidencia requerida |
|---|---|---|---|---|---|---|---|---|---|
| RegistrarRespuestaTransaccion | `idCanal` | Fuente `DifferentialResponse`; readiness `OK`. | Bajo. | Ninguna bloqueante. | Mantener. | No cambiar. | Arquitectura. | No | Tests existentes. |
| RegistrarRespuestaTransaccion | `nombreCanal` | Fuente `DifferentialResponse`; readiness `OK`. | Bajo. | Ninguna bloqueante. | Mantener. | No cambiar. | Arquitectura. | No | Tests existentes. |
| RegistrarRespuestaTransaccion | `idTransaccion` | Fuente `DifferentialResponse`; readiness `OK`. | Bajo. | Ninguna bloqueante. | Mantener. | No cambiar. | Arquitectura. | No | Tests existentes. |
| RegistrarRespuestaTransaccion | `idEstado` | Fuente `DifferentialResponse`; readiness `OK`. | Bajo. | Ninguna bloqueante. | Mantener. | No cambiar. | Arquitectura. | No | Tests existentes. |
| RegistrarRespuestaTransaccion | `idTransaccionAxon` | Fuente `DifferentialResponse`; readiness `OK`. | Bajo. | Ninguna bloqueante. | Mantener. | No cambiar. | Arquitectura. | No | Tests existentes. |
| RegistrarRespuestaTransaccion | `causal` | Fuente `DifferentialResponse`; condicional. | Medio si causal requerida no se homologa. | Cuando aplica causal obligatoria? | Solo rechazo; siempre si existe causal externa; opcional. | Mantener condicional; validar tabla de homologacion. | Negocio ACH, Proveedor. | No para Fase 4 de WSCFAACH; si para flujo de rechazos | Casuistica de estados/causales. |
| RegistrarRespuestaTransaccion | `descripcionCausal` | Fuente `DifferentialResponse`; condicional. | Medio si descripcion requerida no se homologa. | Cuando debe enviarse descripcion? | Solo rechazo; opcional; descripcion externa; descripcion normalizada. | Mantener condicional; validar tabla de homologacion. | Negocio ACH, Proveedor. | No para Fase 4 de WSCFAACH; si para flujo de rechazos | Casuistica de estados/causales. |

## 11. Preguntas para funcional/proveedor

Preguntas directas para `Proc_Contrapartidas`:

1. Para un debito originado por CFA, el monto real va en `OFMONDEB`, `OFMONCRE` o ambos campos con regla por naturaleza?
2. Si un campo de monto no aplica, debe viajar en `0`, vacio, null, omitido o no debe existir?
3. Cual es el valor oficial de `OFDD` para debito originado por CFA?
4. `OFCTA` es cuenta origen real, DFI originador, cuenta contable o identificador de entidad?
5. Que valores oficiales acepta `OFST`?
6. Que representa `OFIDREVER` en operacion normal y en reverso?
7. `OFLIBRE` y `OFLIBRE1` son reservados, obligatorios o deben mapearse a una fuente especifica?
8. `OFDIRECCIONIP` debe ser IP real del canal, IP de API, IP del servidor, IP de estacion o constante tecnica aprobada?
9. `OFIDCAMCOMPE` debe venir de `clearinghouse.id`, ciclo, parametro ambiente o tabla proveedor?
10. Los campos `ANS*` de `Proc_Contrapartidas` son solo respuesta/reservados y pueden quedar sin mapping request?

Preguntas directas para `Proc_Transacciones`:

1. `TREG` es siempre la constante oficial `6`?
2. `CONV` debe venir de convenio por cliente, convenio por producto, camara, transaccion o tabla parametrica?
3. `PROD` debe ser producto core, producto ACH/CENIT, codigo fijo homologado u otra fuente?
4. `IDCAMCOMPE` debe venir del ciclo, clearing house o parametro fijo por ambiente?
5. `DIRECCIONIP` debe representar canal, servidor, API, estacion o constante controlada?
6. `DISCRE`, `LIBRE` y `LIBRE1` son reservados/opcionales o tienen semantica obligatoria?
7. `IREVER` debe viajar como `0` en operacion normal y `1` en reverso, o usa otra semantica?
8. `RTAACH` y `RTALOC` son campos de entrada o exclusivamente de salida/respuesta?
9. `NCTAORIG` debe ser cuenta origen real o puede ser `batchHeaders.companyId`?
10. `REGLOTE` es conteo, secuencia, id de lote o campo diferente?

Preguntas directas para `RegistrarRespuestaTransaccion`:

1. `causal` y `descripcionCausal` son obligatorias solo en rechazo o tambien en aprobacion?
2. La descripcion debe venir literal del proveedor o de una tabla normalizada interna?

## 12. Criterios para aprobar Fase 4

Fase 4 solo deberia iniciar cuando existan respuestas aprobadas para:

- Ubicacion del monto real en `Proc_Contrapartidas`.
- Valor/regla de `OFDD`.
- Fuente correcta de `OFCTA`.
- Regla de reversos para `OFIDREVER` e `IREVER`.
- Politica para `TREG`, `CONV`, `PROD`, `IDCAMCOMPE`.
- Direccion real de `RTAACH` y `RTALOC`.
- Clasificacion de campos libres como fuente concreta, reservado u opcional.
- Politica de IP para `DIRECCIONIP` y `OFDIRECCIONIP`.

Criterios tecnicos futuros:

- No se introducen constantes sin evidencia funcional.
- Readiness no retorna `OK` con placeholders criticos.
- `READY_WITH_WARNINGS` solo se usa para defaults tecnicos aceptados o campos condicionales.
- `RegistrarRespuestaTransaccion` conserva 7 parametros WSDL y sin `ANS*`.
- `Proc_Contrapartidas` conserva `ANS*` opcionales/reservados.
- `PLValidarUsuarioBV` sigue excluido.
- Tests backend cubren cada decision aprobada.
- `/integraciones/mappings` muestra la clasificacion funcional sin mezclar `/integraciones/soap-settings`.

## 13. Riesgos si se implementa sin respuesta

| Riesgo | Impacto | Servicio |
|---|---|---|
| Enviar monto en campo equivocado | Movimiento monetario incorrecto o rechazo del core. | `Proc_Contrapartidas` |
| Enviar naturaleza `D/C` equivocada | Debito/credito invertido o rechazo. | `Proc_Contrapartidas` |
| Usar DFI como cuenta | Rechazo, conciliacion fallida o afectacion contable. | `Proc_Contrapartidas` |
| Usar placeholders en campos criticos | Falso readiness o payload invalido. | Ambos WSCFAACH |
| Tratar response como request | Contrato SOAP inconsistente. | `Proc_Transacciones` |
| Asumir constantes de convenio/producto | Rechazo por parametrizacion no homologada. | `Proc_Transacciones` |
| Usar IP no trazable | Auditoria insuficiente. | Ambos WSCFAACH |
| Inventar semantica de campos libres | Payload funcionalmente contaminado. | Ambos WSCFAACH |

## 14. Recomendacion de orden de atencion

1. Resolver `OFMONDEB`/`OFMONCRE` y `OFDD`.
2. Resolver `OFCTA`.
3. Confirmar direccion de `RTAACH`/`RTALOC`.
4. Confirmar `TREG`, `CONV`, `PROD` e `IDCAMCOMPE`.
5. Resolver reversos: `OFIDREVER` e `IREVER`.
6. Definir IP: `DIRECCIONIP` y `OFDIRECCIONIP`.
7. Clasificar campos libres/reservados: `DISCRE`, `LIBRE`, `LIBRE1`, `OFLIBRE`, `OFLIBRE1`.
8. Confirmar condicion de `causal` y `descripcionCausal`.

## 15. Veredicto

Fase 3 documentada: las decisiones funcionales bloqueantes quedan listas para validacion de negocio/proveedor antes de cualquier Fase 4 de implementacion.

## 16. No modificar codigo

Este documento no autoriza cambios de codigo. La futura Fase 4 debe partir de respuestas funcionales aprobadas y debe modificar solo los componentes necesarios, con pruebas especificas por decision.

