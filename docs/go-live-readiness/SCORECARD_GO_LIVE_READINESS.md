# Scorecard Go-Live Readiness - ACH Interbank

Fecha de generacion/revalidacion: 2026-05-18 / 2026-05-19
Version: 0.8 preliminar
Rama analizada: `fix/uat-operator-role-seed`
Estado inicial: Candidato UAT controlado / NO-GO productivo.  
Uso: instrumento preliminar para comite; requiere evidencias y firmas.

## Criterios De Semaforo

| Puntaje | Clasificacion |
|---:|---|
| >= 90 | Candidato GO |
| 75-89 | GO condicionado |
| 60-74 | NO-GO con brechas altas |
| < 60 | NO-GO |

## Ponderacion

| Categoria | Peso | Estado inicial | Puntaje preliminar | Puntaje ponderado | Evidencia | Observacion |
|---|---:|---|---:|---:|---|---|
| Funcionalidad core | 20% | PARCIAL | 82 | 16.4 | Backend CI/local OK; Angular local OK; API/DB health OK; proxy SPA->API/Auth/Navigation/funcional/NACHA OK; transacciones sinteticas, evento inicial nuevo e idempotencia documental OK | Core API funcional sintetico y proxy Docker mejoran; UAT bancario formal sigue pendiente |
| UAT y evidencias | 20% | PARCIAL | 64 | 12.8 | Docs UAT tecnico/funcional actualizados; evidencia de transaccion, persistencia, idempotencia, proxy funcional/NACHA, logs, build, pruebas y acta preliminar final | Actas firmadas, evidencia visual SPA y homologaciones siguen pendientes |
| Seguridad | 15% | PARCIAL | 73 | 10.95 | `[Authorize]`, permisos, `dotnet list --vulnerable` sin hallazgos tras `System.Security.Cryptography.Xml` 10.0.8; `admin` evidencia `Admin` + `ACH.Operator` tras seed/migracion controlada | `.env` trackeado, OpenBao y certificados requieren decision; falta matriz endpoint-rol formal |
| Interoperabilidad externa | 15% | CRITICO | 35 | 5.25 | NACHA layouts tecnicos identificados; transacciones UAT ACH Colombia/CENIT creadas; envelopes SOAP dry-run generados | NACHA-M real UAT quedo bloqueado por archivo 0 bytes/prenotificacion previa; validacion externa/homologacion sigue pendiente |
| Operacion y soporte | 10% | PARCIAL | 68 | 6.8 | Docker compose config/build/runtime, proxy SPA->API/Auth/Navigation/funcional/NACHA OK y PostgreSQL loopback 5432 para UAT local; runbooks/docs | Backup/restore/rollback pendientes |
| Observabilidad | 10% | PARCIAL | 68 | 6.8 | `/health/live` y `/health/ready` OK en Docker | Health cubre API/DB; faltan Quartz/OpenBao/externos y monitoreo |
| Documentacion y trazabilidad | 10% | PARCIAL | 83 | 8.3 | README, docs readiness, evidencia runtime, UAT funcional sintetico, matriz NACHA layouts, contrato idempotencia y acta preliminar final actualizados | Firmas/evidencias UAT formales pendientes; contrato idempotencia evolutivo queda pendiente si se exige 409/key/replay |
| **Total** | **100%** | **NO-GO con brechas altas** |  | **67.4** |  | **NO-GO productivo** |

## Estado Inicial

Resultado preliminar tras backend CI/local OK, Angular CI de rama y pruebas locales OK, validacion Docker runtime, proxy SPA->API/Auth/Navigation/funcional/NACHA OK, UAT tecnico autenticado basico OK con observaciones, UAT funcional sintetico parcial, cierre funcional de DEF-UAT-017, cierre documental de DEF-UAT-018, cierre tecnico de DEF-UAT-019, correccion NU1903, cierre DEF-UAT-015 para usuario demo multirol y UAT integrado NACHA/SOAP con bloqueo de archivo NACHA-M real: **67.4 / 100 - NO-GO productivo con brechas altas**.

Clasificacion operacional: **Candidato UAT controlado**, no apto para productivo.

## Brechas Que Impiden Subir De Nivel

- UAT real/anonimizado sin acta firmada.
- Evidencias UAT incompletas.
- CENIT neteo/liquidez/CUD sin E2E homologado.
- Sobre digital/firma/certificados sin validacion externa oficial.
- Naming externo ACH/CENIT/STA sin cierre formal.
- Falta matriz endpoint-rol y validacion de politica granular para `AchResponsesController` aunque ya tiene `[Authorize]`.
- Endpoint mismatch de interoperabilidad SPA/backend.
- `environment.prod.ts` corregido a base relativa y Nginx proxya `/auth`, `/navigation`, `/api`, `/health`, `/openapi` y `/scalar`; falta UAT tecnico funcional con datos anonimizados.
- Angular CI remoto OK segun contexto; adjuntar evidencia al paquete RC.
- Backend CI remoto OK segun contexto; adjuntar evidencia al paquete RC.
- Docker compose config/build/runtime OK para API/PostgreSQL/SPA estatica y proxy SPA->API/Auth/Navigation; PostgreSQL publicado en loopback 5432 solo para UAT local.
- UAT tecnico autenticado basico queda OK con observaciones: login demo `admin`, token, roles `Admin` + `ACH.Operator`, menu y endpoints read-only pasan; falta adjuntar evidencia visual si el acta la exige.
- UAT funcional sintetico queda parcialmente OK: datos maestros suficientes, transaccion `UAT-SINT-001` creada, persistida, conciliacion basica consultada e idempotencia controlada.
- SPA Docker ya no devuelve `index.html` para las rutas funcionales reintentadas; falta evidencia visual/acta formal.
- Trazabilidad transaccional historica parcial: `UAT-SINT-001` no tiene evento inicial por ausencia de backfill; `UAT-SINT-TRACE-001` cierra DEF-UAT-017 para nuevas transacciones.
- Contrato de idempotencia actual cerrado documentalmente: duplicado controlado devuelve HTTP 400; queda decision evolutiva 409/idempotency key/replay si aplica.
- NU1903 de vulnerabilidad alta en `System.Security.Cryptography.Xml` 10.0.0 corregida con referencia explicita 10.0.8; mantener monitoreo de advisories.
- NACHA-M layouts tecnicos 1/5/6/7/8/9 presentes y proxied; intento real UAT ACH Colombia/CENIT quedo bloqueado por archivo 0 bytes y prenotificacion previa ausente.
- SOAP `Proc_Contrapartidas` tiene envelopes XML dry-run sanitizados, pero el job automatico requiere guardrail UAT/mock para no intentar endpoints externos no autorizados.
- Rol `ACH.Operator` visible para `admin` tras seed/migracion controlada; queda pendiente matriz endpoint-rol formal para productivo.
- Riesgo de `.env` versionado y defaults sensibles en compose/documentacion.
- OpenBao no levantado en compose principal si aplica al ambiente.
- Backup/restore/rollback sin evidencia.

## Reglas De Decision

- UAT sin acta firmada = NO-GO productivo.
- CENIT CUD sin evidencia = NO-GO productivo si aplica al alcance.
- Sobre digital sin validacion externa = NO-GO productivo si aplica al flujo externo.
- Secretos/credenciales en Git = NO-GO hasta saneamiento o aceptacion formal de riesgo.
- SPA prod localhost = NO-GO despliegue productivo.
- Defecto bloqueante abierto = NO-GO productivo.
- Evidencia tecnica automatizada no reemplaza aprobacion humana.
- Backend CI OK no implica GO productivo.
- Docker runtime/proxy OK no implica GO productivo ni reemplaza UAT con actas.

## Proyeccion De Mejora

| Cierre requerido | Impacto esperado |
|---|---|
| Acta UAT y evidencias firmadas | Subir UAT/evidencias a 75+ |
| Cierre seguridad/configuracion | Subir seguridad a 75+ |
| Validacion externa sobre/naming | Subir interoperabilidad a 75+ |
| E2E CENIT/CUD | Subir funcionalidad/interoperabilidad |
| Backup/restore/rollback ensayado | Subir operacion |
