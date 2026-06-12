# Evidencias UAT - Comite GO/NO-GO

Fecha de actualizacion: 2026-06-12
Alcance: UAT tecnico y funcional sintetico con datos no reales.

| Escenario | Estado | Evidencia | Defecto asociado | Observacion |
| --- | --- | --- | --- | --- |
| UAT tecnico autenticado basico | OK con observaciones | docs/uat/EJECUCION_UAT_TECNICO_BASICO.md | DEF-UAT-011, DEF-UAT-012 | Login, token, menu y endpoints protegidos validados. |
| Proxy funcional SPA Docker | OK | docs/uat/EVIDENCIAS_UAT_FUNCIONAL.md | DEF-UAT-016 | Rutas funcionales raiz confirmadas hacia API. |
| UAT funcional sintetico | PARCIALMENTE OK | docs/uat/UAT_FUNCIONAL_SINTETICO.md | Varios | Permite continuar pruebas controladas; no cierra UAT formal. |
| Transaccion sintetica UAT-SINT-TRACE-001 | OK | docs/uat/EVIDENCIAS_UAT_FUNCIONAL.md | DEF-UAT-017 | Nueva transaccion genero evento inicial esperado. |
| Idempotencia/deduplicacion actual | Cerrado documentalmente | docs/go-live-readiness/CONTRATO_IDEMPOTENCIA_TRANSACCIONES.md | DEF-UAT-018 | Duplicado devuelve 400 JSON controlado y no duplica evento. |
| NACHA-M endpoints/proxy | OK tecnico | docs/go-live-readiness/MATRIZ_NACHA_M_LAYOUTS.md | DEF-UAT-019 | Endpoint/proxy cerrado tecnicamente. |
| NACHA-M export sin falso exito vacio | OK tecnico | docs/uat/EVIDENCIAS_NACHA_M_UAT.md | DEF-UAT-021 | `/NachaExport` responde 422 JSON controlado si falta prenotificacion; no devuelve 200 con 0 bytes. |
| SOAP Proc_Contrapartidas dry-run | OK tecnico UAT/local | docs/uat/EVIDENCIAS_SOAP_PROC_CONTRAPARTIDAS.md | DEF-UAT-022 | Guardrail `DryRun` por defecto validado con `PROC_DRY_RUN`, sin transmision externa. |
| NACHA-M tecnico registros 1/5/6/7/8/9 | OK TECNICO / PARCIAL NORMATIVO | Commits `7c3cbb21`, `e5721150` | DEF-UAT-020 | Naming/generacion/parseo validados; homologacion externa pendiente. |
| G3.6A inbound | GO tecnico, 2/2 | `uat-nacha-inbound-postgres-dispatch.spec.ts` | G3.6A | NachaUpload, persistencia, Quartz y `Proc_Transacciones` dry-run reales. |
| G3.6B outbound | GO tecnico con observacion, 2/2 | `uat-nacha-export-postgres-contrapartidas.spec.ts` | G3.6B | Export `.6` y dispatch dry-run correlacionados por `AchCycleId`; no causalidad. |
| Evidencia visual/operativa | PENDIENTE | docs/uat/EVIDENCIAS_UAT_FUNCIONAL.md | Pendiente | Requerida para actas y comite final. |
| UAT bancario formal | PENDIENTE | docs/uat/ACTA_TECNICA_PRELIMINAR.md | Pendiente | Requiere aprobaciones y actas formales. |

Los archivos en `test-results` son artefactos locales efimeros. La trazabilidad durable es commit + spec + caso + resultado; no se incorporan payloads sensibles ni binarios.
