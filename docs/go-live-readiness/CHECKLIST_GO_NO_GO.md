# Checklist GO / NO-GO - ACH Interbank

Fecha de generacion/revalidacion: 2026-05-18 / 2026-05-19
Version: 0.8 preliminar
Rama analizada: `fix/uat-operator-role-seed`
Estado inicial: Candidato UAT controlado / NO-GO productivo.  
Uso: checklist para comite; requiere evidencia y aprobacion humana.

## Checklist

| ID | Categoria | Pregunta de control | Estado | Evidencia | Responsable | Bloquea go-live | Observacion |
|---|---|---|---|---|---|---|---|
| GNG-001 | Funcional | Transacciones ACH individuales funcionan con datos anonimizados? | PENDIENTE VALIDAR | UAT-REAL-007 | Operaciones | Si | |
| GNG-002 | Funcional | Bulk ingestion procesa errores parciales y retry? | PENDIENTE VALIDAR | UAT-REAL-008 | Operaciones/Tecnologia | Si | |
| GNG-003 | Normativa | Existe trazabilidad norma-codigo-prueba-evidencia por camara? | PARCIAL | `docs/audits/s1-requirement-norm-code-test-evidence-closure-matrix-current.md` | Compliance | Si | Requiere firma |
| GNG-004 | Backend | Build y tests backend actuales pasan? | OK | GitHub Actions `dotnet-ci` OK para `49b810f9`; `dotnet build` OK; suite backend local 1091 OK, 1 omitida, 0 fallas | Tecnologia | Si | Adjuntar evidencia al paquete RC |
| GNG-005 | Frontend | Build SPA actual pasa? | OK | `angular-ci` de rama OK; `npm run build` OK; `npm test` 147 specs OK | Tecnologia | Si | Adjuntar evidencia al paquete RC; warnings no bloqueantes |
| GNG-006 | Seguridad | Todos los controllers sensibles tienen autorizacion explicita? | PARCIAL | `AchResponsesController` ahora tiene `[Authorize]`; falta matriz endpoint-rol completa | Seguridad | Si | |
| GNG-007 | OpenBao/secretos | OpenBao UAT esta disponible o existe excepcion aprobada? | PENDIENTE VALIDAR | `scripts/openbao`, compose principal sin OpenBao | Seguridad | Si si aplica | |
| GNG-008 | Certificados/firma/sobre digital | Existe validacion externa oficial? | CRITICO | Docs UAT marcan pendiente | Seguridad | Si | |
| GNG-009 | NACHA-M | Registros 1/5/6/7/8/9 validados por campo? | BLOQUEADO | `docs/uat/UAT_NACHA_M_CAMPO_A_CAMPO.md`; evidencias `docs/uat/evidencias/nacha-m-uat/` | Operaciones/QA/Compliance | Si | Transacciones por camara creadas, pero no hay archivo NACHA-M valido: 422 controlado por prenotificacion previa ausente |
| GNG-010 | ACH Colombia | Flujos ACH tienen aceptacion funcional? | PENDIENTE VALIDAR | Acta UAT pendiente | Negocio | Si | |
| GNG-011 | CENIT | Ciclos CENIT tienen evidencia homologada? | PARCIAL | Checklist CENIT | Operaciones | Si | |
| GNG-012 | STA | STA aplica al alcance y esta validado? | NO CLARO | NO ENCONTRADO | Compliance | PENDIENTE VALIDAR | |
| GNG-013 | ROR | ROR tiene UAT E2E y aprobacion normativa? | PARCIAL | API/SPA existe | Operaciones/Compliance | Si | |
| GNG-014 | Devoluciones | Devolucion salida/entrada estan cerradas? | PARCIAL | Checklists UAT | Operaciones | Si | |
| GNG-015 | Rechazos | Rechazo total/parcial tiene semantica firmada? | PARCIAL | Docs rejection | Compliance | Si | |
| GNG-016 | Conciliacion | Conciliacion operativa esta validada? | PENDIENTE VALIDAR | Reportes | Auditoria | Si | |
| GNG-017 | Contabilidad | Frontera no-contable esta aceptada? | PENDIENTE VALIDAR | Accounting-review docs | Negocio/Auditoria | Si | No hay ledger encontrado |
| GNG-018 | UAT | Acta UAT firmada existe? | CRITICO | Plantilla creada | Auditoria | Si | |
| GNG-019 | Evidencias | Indice de evidencias completo? | CRITICO | `docs/uat/INDICE_EVIDENCIAS_UAT.md` | Auditoria | Si | |
| GNG-020 | Operacion | Runbook UAT/preproductivo aprobado? | PARCIAL | `docs/operations/RUNBOOK_UAT_Y_PREPRODUCTIVO.md` | Operaciones | Si | |
| GNG-021 | Monitoreo | Health checks y monitoreo cubren componentes criticos? | PARCIAL | Docker runtime: `/health/live` OK y `/health/ready` OK con DB healthy | Tecnologia | Si | Falta Quartz/OpenBao/externos y monitoreo real |
| GNG-022 | Backup/restore | Backup y restore ensayados? | NO ENCONTRADO | NO ENCONTRADO | Operaciones | Si | |
| GNG-023 | Rollback | Rollback documentado y ensayado? | PARCIAL | Runbook documental | Operaciones/Tecnologia | Si | |
| GNG-024 | Soporte | Equipo soporte y escalamiento definidos? | PENDIENTE VALIDAR | Acta/runbook | Operaciones | Si | |
| GNG-025 | Mesa de ayuda | Canal de defectos/incidentes UAT definido? | PENDIENTE VALIDAR | Matriz defectos | Operaciones | No | |
| GNG-026 | Aprobaciones | Negocio, Operaciones, Seguridad y Auditoria firmaron? | CRITICO | NO ENCONTRADO | Comite | Si | |
| GNG-027 | Docker/ambiente | Compose UAT no expone secretos y esta parametrizado? | PARCIAL | `docker compose config/build/up` OK; API/Postgres/SPA Up; SPA proxya API/Auth/Navigation via Nginx; PostgreSQL publicado solo en loopback 5432 para UAT local | Operaciones | Si | Falta UAT tecnico con datos anonimizados y revision de secretos; exposicion DB no aplica a productivo |
| GNG-028 | PostgreSQL/migraciones | Migraciones aplicadas sin drift? | PARCIAL | API aplico migraciones automaticas en Docker; DB ready OK; 130 tablas public | Tecnologia | Si | Validar politica DBA para UAT/preproductivo |
| GNG-029 | Seguridad configuracion | `.env`, compose y prod config estan saneados? | PARCIAL | `.gitignore`, compose placeholders, `environment.prod.ts` relativo; `.env` sigue trackeado | Seguridad | Si | Requiere revision humana de `.env` |
| GNG-030 | README/runbook | README operativo no tiene drift? | OK | README raiz saneado y referencia docs UAT/go-live | Tecnologia | No para UAT, si para release formal | |
| GNG-031 | Datos sensibles | No hay datos sensibles versionados? | PENDIENTE VALIDAR | Pre-check `.env` versionado | Seguridad | Si | Requiere revision |
| GNG-032 | SPA/API runtime | La SPA servida por Docker consume API correctamente por la misma URL o proxy aprobado? | OK TECNICO | `http://localhost:743/health/live` OK, `:743/api/ach/responses` 401, `:743/auth/login` 401 JSON, `:743/navigation/menu` 401 sin token desde API | DevOps/Tecnologia | Si | Auth intacta; con token valido navigation debe devolver JSON; ejecutar UAT tecnico funcional |
| GNG-033 | UAT tecnico autenticado | Login real con usuario demo, token y menu fueron validados por automatizacion segura? | OK CON OBSERVACIONES | Login demo `admin` HTTP 200, token recibido/enmascarado, roles respuesta/JWT `Admin,ACH.Operator`, `/navigation/menu`, `/api/roles`, `/api/users`, `/api/ach/responses` HTTP 200 JSON con Bearer | QA/DevOps | Si | No documentar password/token; evidencia visual sigue pendiente si el acta formal la exige |
| GNG-034 | Evidencia visual UAT tecnico | Existe evidencia visual automatizada o manual de navegacion SPA autenticada? | PARCIAL | Logs SPA muestran dashboard, usuarios, transacciones, reportes y ACH responses; browser integrado sin herramienta ejecutable en esta sesion | QA/DevOps | No para UAT tecnico HTTP, si si acta exige captura | Adjuntar capturas sanitizadas en cierre formal |
| GNG-035 | UAT funcional sintetico | Existe flujo sintetico controlado de transaccion con datos anonimizados? | PARCIALMENTE OK | `docs/uat/UAT_FUNCIONAL_SINTETICO.md`: `UAT-SINT-001` historica creada; `UAT-SINT-TRACE-001` revalida evento inicial; duplicado controlado por `:743`; contrato idempotencia documentado | QA/Operaciones | Si | Core API, reintento HTTP SPA Docker, DEF-UAT-017 y DEF-UAT-018 documental OK; evidencia visual y actas pendientes |
| GNG-036 | SPA funcional Docker | Las pantallas funcionales consumen JSON desde `http://localhost:743` y no reciben `index.html`? | OK TECNICO | DEF-UAT-016 cerrado; rutas funcionales sin token devuelven 401 no HTML y con token devuelven 200 JSON por `:743` | Tecnologia/DevOps | No para esta brecha | Mantener cobertura cuando se agreguen nuevos endpoints raiz |
| GNG-037 | Trazabilidad transaccional | La transaccion sintetica tiene estado, timestamps, auditoria y eventos de ciclo de vida? | OK FUNCIONAL NUEVAS TRANSACCIONES | `UAT-SINT-TRACE-001` ID `2` tiene evento inicial `Pending -> Pending`, `Source=System`, `ReasonCode=CREATED`; duplicado deja `transaction_count=1`, `event_count=1` | Tecnologia/QA/Auditoria | Si | `UAT-SINT-001` historica conserva `0` eventos por decision de no backfill |
| GNG-038 | Idempotencia | Reintento identico tiene comportamiento controlado y contrato claro? | OK DOCUMENTAL ACTUAL | Reintento del mismo payload devuelve 400 con mensaje de duplicado equivalente; contrato actual formalizado por ciclo/tipo/monto/cuentas/`TransactionExternalId` o `Reference` en `CONTRATO_IDEMPOTENCIA_TRANSACCIONES.md` | Arquitectura/Tecnologia | Si | 409 Conflict, `Idempotency-Key` y replay quedan como decision evolutiva, no implementada en esta fase |
| GNG-039 | Conciliacion sintetica | Existe lectura basica de conciliacion para ciclo/fecha sinteticos? | OK TECNICO | `GET /api/reports/reconciliation` responde 200 por API directa | Auditoria/Operaciones | Si | No reemplaza conciliacion bancaria real |
| GNG-040 | Seguridad dependencias | No hay paquetes NuGet vulnerables conocidos en la solucion? | OK TECNICO | `System.Security.Cryptography.Xml` fijado en 10.0.8; `dotnet list ... --vulnerable --include-transitive` sin hallazgos | Seguridad/Tecnologia | Si | Mantener monitoreo de advisories |
| GNG-041 | Rol ACH.Operator | Usuario demo evidencia roles esperados `Admin` y `ACH.Operator`? | OK TECNICO | `UserRoleConfiguration` y migracion `AddAdminOperatorRoleSeed` asignan `admin` a `Admin` y `ACH.Operator`; login/JWT sanitizados muestran ambos roles; menu y endpoints read-only responden 200 con Bearer | Seguridad/Tecnologia/QA | No para UAT controlado; si para matriz rol-permiso productiva formal | `admin` queda como usuario demo multirol para UAT controlado; evaluar usuario operador separado antes de preproductivo si seguridad lo exige |
| GNG-042 | NACHA Export | El generador NACHA-M produce archivo UAT no vacio por camara? | BLOQUEADO | `docs/uat/EVIDENCIAS_NACHA_M_UAT.md` | Tecnologia/QA/Operaciones | Si | DEF-UAT-021 cerrado: `/NachaExport` devuelve 422 controlado si faltan prerequisitos; archivo no vacio sigue pendiente tras prenotificacion valida |
| GNG-043 | SOAP Proc_Contrapartidas | Existe dry-run/mock autorizado sin transmision externa? | OK UAT/LOCAL | `docs/uat/UAT_SOAP_PROC_CONTRAPARTIDAS.md`; `docs/uat/evidencias/soap-proc-contrapartidas/runtime_dry_run_validation.md` | Integracion/DevOps/Seguridad | Si | Guardrail `DryRun` por defecto validado con `PROC_DRY_RUN`; endpoint UAT/mock real sigue pendiente para homologacion |

## Regla De Decision

- Cualquier item `CRITICO` con `Bloquea go-live = Si` mantiene NO-GO productivo.
- UAT puede avanzar si no hay bloqueantes de ambiente y los riesgos estan comunicados.
- Go productivo requiere estados `OK` o riesgo aceptado formalmente en todos los controles bloqueantes.

Estado tras UAT integrado NACHA/SOAP: **UAT tecnico autenticado basico OK con observaciones** y **UAT funcional sintetico PARCIALMENTE OK**. Se crearon transacciones por ACH Colombia y CENIT; DEF-UAT-021 evita falso exito con archivo 0 bytes y DEF-UAT-022 valida dry-run sin transmision externa. NACHA-M UAT queda bloqueado hasta crear prenotificacion valida y obtener archivo no vacio validado campo-a-campo. Productivo permanece **NO-GO**.

## Actualizacion 2026-05-19

| Control | Estado | Observacion |
|---|---|---|
| Reglas de prenotificacion parametrizadas por camara | OK tecnico | Backend/API/SPA implementados; requiere validacion runtime con migracion aplicada. |
| DEF-UAT-020 NACHA-M campo-a-campo | Parcial | Parametrizacion completada, archivo no vacio sigue pendiente. |
| Productivo | NO-GO | Persisten UAT formal, CENIT/CUD, sobre digital, backup/restore y homologaciones. |
## Actualizacion 2026-05-20

- Menu `Transacciones > Reglas por camara`: OK runtime.
- Reglas por camara/naturaleza: OK runtime.
- NACHA-M UAT no vacio ACH Colombia/CENIT: OK tecnico parcial.
- NACHA-M debito post-prenotificacion madura: pendiente.
- Homologacion campo-a-campo/waiver: pendiente.
- Productivo: **NO-GO**.
