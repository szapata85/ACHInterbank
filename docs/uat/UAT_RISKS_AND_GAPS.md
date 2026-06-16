# Riesgos y brechas UAT ACH Colombia/CENIT - Fase 6C.10

Decision vigente: Productivo permanece NO-GO.

| ID | Riesgo / brecha | Impacto | Mitigacion | Estado |
| --- | --- | --- | --- | --- |
| RSK-001 | Golden files son semirreales y anonimizados | No prueban certificacion oficial | Usarlos solo como regresion automatizada; ejecutar UAT formal con ACH/CENIT | Abierto |
| RSK-002 | Falta certificacion oficial ACH Colombia/CENIT | Bloquea salida productiva | Agendar certificacion con artefactos UAT y matriz 6C.9 | Abierto |
| RSK-003 | SOAP real pendiente y bloqueado | No valida integracion externa real | Mantener consola read-only; planificar fase SOAP real controlada | Abierto |
| RSK-004 | Certificados reales no usados | No valida handshake/productivo | Usar certificados mock/controlados; no introducir secretos | Abierto |
| RSK-005 | Datos reales no usados | Escenarios pueden diferir de produccion | Definir dataset sintetico representativo y aprobado | Abierto |
| RSK-006 | Dependencia de datos sinteticos | Cobertura funcional limitada | Mapear dataset contra escenarios UAT y causales | Abierto |
| RSK-007 | Warnings Browserslist/Node DEP0205 | Ruido CI/compatibilidad futura | Registrar como deuda tecnica no bloqueante; actualizar toolchain en fase separada | Mitigado |
| RSK-008 | Crash paralelo CLR/EF observado previamente | Inestabilidad de suite en paralelo | Usar `RunConfiguration.MaxCpuCount=1` si ocurre | Mitigado |
| RSK-009 | Secciones parciales si faltan fuentes persistidas | Evidencia incompleta en dashboard/conciliacion | Marcar warnings parciales y completar datos UAT | Abierto |
| RSK-010 | Riesgos operativos de integracion ACH/CENIT | Diferencias de horarios, ciclos, causales | Validar contra DSP-152/Anexos y operadores de negocio | Abierto |
| RSK-011 | Legacy layouts/definitions usado por error | Desalineacion con modelo oficial | Mantener legacy deprecated/read-only y validar Playwright | Mitigado |
| RSK-012 | Uso accidental de `/NachaExport/{hash}` | Error funcional y trazabilidad incorrecta | Guardas Angular/Playwright; export solo por `cycleId` | Mitigado |
| RSK-013 | Datos sensibles en evidencia | Riesgo auditoria/privacidad | Sanitizacion obligatoria; no adjuntar XML SOAP completo ni cuentas/documentos | Abierto |
| RSK-014 | Movimiento monetario accidental | Riesgo financiero critico | Productivo NO-GO, SOAP real bloqueado, sin botones criticos | Mitigado |
| RSK-015 | Drift entre comandos locales y CI | Resultados UAT no reproducibles | Usar `docs/uat/PRE_UAT_AUTOMATED_CHECKLIST.md` y workflows revisados | Mitigado |
| RSK-016 | Duplicidad de dependencias E2E | Version efectiva ambigua en `npm ci` | `@playwright/test` duplicado removido de `package.json` | Mitigado |

## Actualizacion 6D.2

| ID | Actualizacion | Estado |
| --- | --- | --- |
| RSK-001 | Se mantiene como brecha de certificacion; la ronda ampliada agrega dataset mas representativo pero no reemplaza certificacion oficial | Abierto |
| RSK-002 | Se mantiene pendiente de tercero/comite ACH Colombia/CENIT | Abierto |
| RSK-003 | SOAP real sigue bloqueado; UAT ampliado conserva consola read-only | Abierto |
| RSK-005 | Dataset sintetico ampliado definido en `UAT_EXPANDED_SYNTHETIC_DATASET.md` | Mitigacion ampliada |
| RSK-006 | Escenarios nuevos manual review/inconsistencia/ciclo CENIT agregados | Mitigacion ampliada |
| RSK-007 | Warnings Browserslist/Node siguen diferidos; no se cambian dependencias mayores pre-UAT | Mitigado |
| RSK-009 | Fuentes parciales se validaran con datos ampliados cargados en UAT | Abierto |

## Actualizacion 6D.3

| ID | Actualizacion | Estado |
| --- | --- | --- |
| RSK-001 | Ronda ampliada ejecutada con dataset sintetico; golden/evidencia siguen sin reemplazar certificacion oficial | Abierto |
| RSK-002 | Certificacion oficial ACH Colombia/CENIT sigue pendiente | Abierto |
| RSK-003 | SOAP real continua bloqueado; guardas CI/read-only confirmadas | Abierto |
| RSK-005 | Dataset sintetico ampliado cargado/documentado para UAT interno | Mitigacion ampliada |
| RSK-006 | Manual review e inconsistencias cubiertas documentalmente en UAT-EXP-001/002 | Mitigacion ampliada |
| RSK-009 | Ciclo/cola/neteo CENIT queda observado por falta de ejecucion externa | Abierto |
| RSK-010 | Requiere validacion con operadores ACH/CENIT y anexos oficiales | Abierto |

## Actualizacion 6D.4

| ID | Riesgo / brecha | Impacto | Mitigacion | Estado |
| --- | --- | --- | --- | --- |
| RSK-017 | Dependencia de terceros ACH Colombia/CENIT | Puede retrasar UAT externo y certificacion | RACI, ventanas y solicitudes de evidencia externas | Abierto |
| RSK-018 | Homologacion oficial de causales pendiente | Puede mantener manual review o inconsistencias | Validar Anexos A/B con terceros y registrar evidencia | Abierto |
| RSK-019 | Ventana CENIT no confirmada | Bloquea ciclo/cola/neteo externo | Plan de ventanas W-005/W-006 | Abierto |
| RSK-020 | Certificados/endpoints externos pendientes | Bloquea pruebas integradas controladas | Controles de seguridad y canales seguros | Abierto |
| RSK-021 | SOAP real controlado futuro | Riesgo operativo si se habilita sin autorizacion | Mantener bloqueo hasta acta formal | Abierto |
| RSK-022 | Datos externos no autorizados | Riesgo privacidad/compliance | Usar dataset sintetico salvo autorizacion expresa | Abierto |
| RSK-023 | Evidencia oficial incompleta | No permite cierre de certificacion | Solicitudes `EXTERNAL_UAT_EVIDENCE_REQUESTS.md` | Abierto |
| RSK-024 | Aprobacion Seguridad/Compliance pendiente | Bloquea intercambio tecnico | RACI y controles de seguridad | Abierto |

## Actualizacion 6D.5

| ID | Riesgo / brecha | Impacto | Mitigacion | Estado |
| --- | --- | --- | --- | --- |
| RSK-025 | Secretos mal custodiados | Exposicion de credenciales/certificados | Modelo de custodia y uso de mecanismo corporativo de secretos o mecanismo aprobado | Abierto |
| RSK-026 | Endpoints no aprobados | Conexion a destino incorrecto o productivo | Registro placeholder y aprobacion Seguridad/Tecnologia | Abierto |
| RSK-027 | Certificados vencidos/incorrectos | Falla handshake o exposicion operativa | Validacion Seguridad sin exponer material real | Abierto |
| RSK-028 | Logging sensible | Exposicion de datos, secretos o payloads | Logging sanitizado y evidencia revisada | Abierto |
| RSK-029 | Exposicion accidental de datos | Riesgo privacidad/compliance | Dataset sintetico y revision de evidencia | Abierto |
| RSK-030 | Autorizacion incompleta | UAT externo bloqueado o no auditable | Checklist aprobacion Seguridad/Compliance/Tecnologia | Abierto |
| RSK-031 | Uso indebido de SOAP real | Riesgo operacional/monetario | Bloqueo SOAP hasta autorizacion formal | Abierto |

## Actualizacion 6D.6

| ID | Riesgo / brecha | Impacto | Mitigacion | Estado |
| --- | --- | --- | --- | --- |
| RSK-032 | Simulacion confundida con aprobacion real | Carga indebida de secretos/endpoints | Marcar aprobacion formal NO otorgada | Abierto |
| RSK-033 | Evidencia faltante ambiente aislado | Bloquea pre-habilitacion externa | Gap analysis y checklist pre-habilitacion | Abierto |
| RSK-034 | Canal seguro no aprobado | Bloquea intercambio con terceros | Solicitud formal Seguridad/Compliance | Abierto |
| RSK-035 | Acta Productivo NO-GO pendiente | Riesgo de interpretacion de alcance | Evidencia NO-GO solicitada a Compliance | Abierto |
| RSK-036 | Evidencia terceros pendiente | Bloquea UAT externo/certificacion | Mantener dependencias ACH/CENIT visibles | Abierto |

## Actualizacion 6D.7

| ID | Riesgo / brecha | Impacto | Mitigacion | Estado |
| --- | --- | --- | --- | --- |
| RSK-037 | Aprobacion formal no otorgada | Bloquea recepcion/carga de parametros reales | Matriz decision y solicitud formal | Abierto |
| RSK-038 | Evidencia incompleta para envio | Puede retrasar revision | Checklist de envio y gap analysis | Abierto |
| RSK-039 | Canal seguro no definido formalmente | Bloquea intercambio con terceros | Decision DEC-002 pendiente | Abierto |
| RSK-040 | Custodia pendiente de aprobacion | Bloquea secretos/certificados | Decision DEC-003 pendiente | Abierto |
| RSK-041 | Terceros pendientes | Bloquea UAT externo/certificacion | Paquete externo y RACI para aceptacion | Abierto |
| RSK-042 | Riesgo interpretacion NO-GO | Puede confundirse con autorizacion productiva | `PRODUCTIVE_NO_GO_ATTESTATION.md` | Abierto |

## Actualizacion 6D.8

| ID | Riesgo / brecha | Impacto | Mitigacion | Estado |
| --- | --- | --- | --- | --- |
| RSK-043 | Decision Seguridad/Compliance no recibida | Bloquea avance externo y carga de parametros | Registro decision 6D.8 con estado `Pendiente` | Abierto |
| RSK-044 | Aprobacion con observaciones | Puede condicionar o limitar pre-habilitacion | Plan respuesta observaciones y matriz acciones | Abierto |
| RSK-045 | Rechazo formal | Detiene avance y exige correcciones | Registrar impacto y condiciones de reenvio | Abierto |
| RSK-046 | Observaciones criticas | Bloquean UAT externo o intercambio tecnico | Severidad critica y bloqueo hasta validacion | Abierto |
| RSK-047 | Retraso por tercero | Mantiene certificados/endpoints/evidencia externa pendientes | Matriz acciones con `Bloqueado por tercero` | Abierto |
| RSK-048 | Evidencia insuficiente | Puede impedir aprobacion Seguridad/Compliance | Log evidencias decision y gap analysis | Abierto |
| RSK-049 | Interpretacion incorrecta de NO-GO | Puede confundirse preparacion con autorizacion productiva | Acta NO-GO y restricciones reiteradas | Abierto |

## Actualizacion 6D.9

| ID | Riesgo / brecha | Impacto | Mitigacion | Estado |
| --- | --- | --- | --- | --- |
| RSK-050 | Decision ejecutiva pendiente | Bloquea continuidad UAT externo condicionada | Paquete ejecutivo y solicitud decision | Abierto |
| RSK-051 | Interpretacion incorrecta de alcance ejecutivo | Puede confundirse con GO productivo | Brief ejecutivo y acta NO-GO | Abierto |
| RSK-052 | Presion por salida productiva | Riesgo operativo/compliance | Productivo NO-GO explicito en anexos | Abierto |
| RSK-053 | Aprobacion parcial con observaciones | Puede condicionar avance y requerir acciones | Plan observaciones y matriz acciones | Abierto |
| RSK-054 | Bloqueo por comite | Detiene UAT externo | Plantilla acta y decision request con impactos | Abierto |
| RSK-055 | Dependencia de terceros persiste | Retrasa evidencia ACH/CENIT | RACI, ventanas y dashboard ejecutivo | Abierto |

## Actualizacion 6D.10

| ID | Riesgo / brecha | Impacto | Mitigacion | Estado |
| --- | --- | --- | --- | --- |
| RSK-056 | Decision de comite no recibida | Bloquea ejecucion posterior | Registro decision comite y evidence log | Abierto |
| RSK-057 | Aprobacion con observaciones | Condiciona avance y exige cierre | Plan observaciones ejecutivas | Abierto |
| RSK-058 | Rechazo formal | Detiene UAT externo | Plan remediacion condicionado | Abierto |
| RSK-059 | Avance indebido antes de decision | Riesgo operativo/compliance | Matriz post-comite bloqueada por decision | Abierto |
| RSK-060 | Confusion entre UAT externo y produccion | Riesgo de interpretacion GO | Productivo NO-GO en todos los documentos | Abierto |
| RSK-061 | Evidencia insuficiente post-comite | Puede impedir ejecucion controlada | Log evidencias decision comite | Abierto |

## Actualizacion 6D.11C

| ID | Riesgo / brecha | Impacto | Mitigacion | Estado |
| --- | --- | --- | --- | --- |
| RSK-062 | Decision externa no recibida | Mantiene paquete en espera | Protocolo de espera decision externa | Abierto |
| RSK-063 | Paquete desactualizado durante espera | Evidencia puede perder vigencia | Congelamiento y checklist revalidacion | Abierto |
| RSK-064 | Cambios no controlados | Riesgo auditoria/trazabilidad | Control de cambios UAT | Abierto |
| RSK-065 | Confusion entre congelado y aprobado | Avance indebido | Acta congelamiento con NO-GO explicito | Abierto |
| RSK-066 | Avance indebido sin decision formal | Riesgo operativo/compliance | Prohibiciones durante congelamiento | Abierto |

## Condiciones bloqueantes

- Cualquier SOAP real no autorizado.
- Movimiento monetario real.
- Dato real de cliente o secreto en evidencia.
- Mutacion critica desde consolas read-only.
- Uso de legacy como fuente oficial.
- Descarga `/NachaExport/{hash}`.
- Aprobacion productiva sin certificacion oficial ACH Colombia/CENIT.

## Cierre

La preparacion UAT puede avanzar solo con datos sinteticos, evidencia sanitizada y decision formal de comite. Productivo permanece NO-GO.
