# Checklist GO / NO-GO - ACH Interbank

Fecha de generacion: 2026-05-18  
Version: 0.2 preliminar  
Rama analizada: `fix/spa-docker-runtime-proxy-and-images`  
Estado inicial: Candidato UAT / NO-GO productivo.  
Uso: checklist para comite; requiere evidencia y aprobacion humana.

## Checklist

| ID | Categoria | Pregunta de control | Estado | Evidencia | Responsable | Bloquea go-live | Observacion |
|---|---|---|---|---|---|---|---|
| GNG-001 | Funcional | Transacciones ACH individuales funcionan con datos anonimizados? | PENDIENTE VALIDAR | UAT-REAL-007 | Operaciones | Si | |
| GNG-002 | Funcional | Bulk ingestion procesa errores parciales y retry? | PENDIENTE VALIDAR | UAT-REAL-008 | Operaciones/Tecnologia | Si | |
| GNG-003 | Normativa | Existe trazabilidad norma-codigo-prueba-evidencia por camara? | PARCIAL | `docs/audits/s1-requirement-norm-code-test-evidence-closure-matrix-current.md` | Compliance | Si | Requiere firma |
| GNG-004 | Backend | Build y tests backend actuales pasan? | OK | GitHub Actions `dotnet-ci`: OK segun contexto | Tecnologia | Si | Adjuntar evidencia al paquete RC |
| GNG-005 | Frontend | Build SPA actual pasa? | OK | GitHub Actions `angular-ci`: OK segun contexto; validacion local previa build/test OK | Tecnologia | Si | Adjuntar evidencia al paquete RC |
| GNG-006 | Seguridad | Todos los controllers sensibles tienen autorizacion explicita? | PARCIAL | `AchResponsesController` ahora tiene `[Authorize]`; falta matriz endpoint-rol completa | Seguridad | Si | |
| GNG-007 | OpenBao/secretos | OpenBao UAT esta disponible o existe excepcion aprobada? | PENDIENTE VALIDAR | `scripts/openbao`, compose principal sin OpenBao | Seguridad | Si si aplica | |
| GNG-008 | Certificados/firma/sobre digital | Existe validacion externa oficial? | CRITICO | Docs UAT marcan pendiente | Seguridad | Si | |
| GNG-009 | NACHA-M | Registros 1/5/6/7/8/9 validados por campo? | PARCIAL | Matrices NACHA | Operaciones/QA | Si | Falta evidencia UAT |
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
| GNG-033 | UAT tecnico autenticado | Login real con usuario demo, token y menu fueron validados por automatizacion segura? | OK CON OBSERVACIONES | Login demo `admin` HTTP 200, token recibido/enmascarado, `/navigation/menu`, `/api/roles`, `/api/users`, `/api/ach/responses` HTTP 200 JSON con Bearer | QA/DevOps | Si | No documentar password/token; rol `ACH.Operator` no visible en JWT/respuesta y requiere confirmacion |
| GNG-034 | Evidencia visual UAT tecnico | Existe evidencia visual automatizada o manual de navegacion SPA autenticada? | PARCIAL | Logs SPA muestran dashboard, usuarios, transacciones, reportes y ACH responses; browser integrado sin herramienta ejecutable en esta sesion | QA/DevOps | No para UAT tecnico HTTP, si si acta exige captura | Adjuntar capturas sanitizadas en cierre formal |

## Regla De Decision

- Cualquier item `CRITICO` con `Bloquea go-live = Si` mantiene NO-GO productivo.
- UAT puede avanzar si no hay bloqueantes de ambiente y los riesgos estan comunicados.
- Go productivo requiere estados `OK` o riesgo aceptado formalmente en todos los controles bloqueantes.

Estado tras reintento autenticado: **UAT tecnico autenticado basico OK con observaciones**. UAT funcional con transacciones sinteticas sigue pendiente. Productivo permanece **NO-GO**.
