# Scorecard Go-Live Readiness - ACH Interbank

Fecha de generacion/revalidacion: 2026-05-18 / 2026-05-19
Version: 0.6 preliminar
Rama analizada: `fix/spa-functional-root-routes-proxy`
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
| Funcionalidad core | 20% | PARCIAL | 81 | 16.2 | Backend CI OK; Angular CI OK; API/DB health OK; proxy SPA->API/Auth/Navigation OK; login demo/token/menu/endpoints read-only OK; transaccion sintetica `UAT-SINT-001` creada por API directa; rutas funcionales reintentadas por `:743` sin HTML fallback; `UAT-SINT-TRACE-001` revalida evento inicial; contrato idempotencia actual formalizado | Core API funcional sintetico, reintento HTTP SPA Docker, DEF-UAT-017 y DEF-UAT-018 documental OK |
| UAT y evidencias | 20% | PARCIAL | 60 | 12.0 | Docs UAT tecnico y funcional actualizados; evidencia de transaccion sintetica, persistencia, idempotencia, proxy funcional, logs, build, pruebas focalizadas, revalidacion runtime de evento inicial y contrato idempotencia | Actas firmadas, evidencia visual SPA, UAT bancario formal y homologaciones siguen pendientes |
| Seguridad | 15% | PARCIAL | 65 | 9.75 | Politicas/middleware + `[Authorize]` en AchResponses | `.env` trackeado, roles/policies y secretos requieren validacion |
| Interoperabilidad externa | 15% | CRITICO | 35 | 5.25 | Sobre digital/naming/CENIT | Validacion externa pendiente |
| Operacion y soporte | 10% | PARCIAL | 66 | 6.6 | Docker compose config/build/runtime, proxy SPA->API/Auth/Navigation OK y PostgreSQL loopback 5432 para UAT local; runbooks/docs | Backup/restore/rollback pendientes |
| Observabilidad | 10% | PARCIAL | 68 | 6.8 | `/health/live` y `/health/ready` OK en Docker | Health cubre API/DB; faltan Quartz/OpenBao/externos y monitoreo |
| Documentacion y trazabilidad | 10% | PARCIAL | 80 | 8.0 | README, docs readiness, evidencia runtime, UAT funcional sintetico y contrato idempotencia actualizados | Firmas/evidencias UAT formales pendientes; contrato idempotencia evolutivo queda pendiente si se exige 409/key/replay |
| **Total** | **100%** | **NO-GO con brechas altas** |  | **64.6** |  | **NO-GO productivo** |

## Estado Inicial

Resultado preliminar tras backend CI OK, angular CI OK, validacion Docker runtime, proxy SPA->API/Auth/Navigation OK, UAT tecnico autenticado basico OK con observaciones, UAT funcional sintetico parcial por API directa y reintento HTTP de rutas funcionales por SPA Docker, cierre funcional de DEF-UAT-017 y cierre documental de DEF-UAT-018 para contrato actual: **64.6 / 100 - NO-GO productivo con brechas altas**.

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
- UAT tecnico autenticado basico queda OK con observaciones: login demo `admin`, token, menu y endpoints read-only pasan; falta confirmar rol `ACH.Operator` visible/asignado y adjuntar evidencia visual si el acta la exige.
- UAT funcional sintetico queda parcialmente OK: datos maestros suficientes, transaccion `UAT-SINT-001` creada, persistida, conciliacion basica consultada e idempotencia controlada.
- SPA Docker ya no devuelve `index.html` para las rutas funcionales reintentadas; falta evidencia visual/acta formal.
- Trazabilidad transaccional historica parcial: `UAT-SINT-001` no tiene evento inicial por ausencia de backfill; `UAT-SINT-TRACE-001` cierra DEF-UAT-017 para nuevas transacciones.
- Contrato de idempotencia actual cerrado documentalmente: duplicado controlado devuelve HTTP 400; queda decision evolutiva 409/idempotency key/replay si aplica.
- Warning NU1903 de vulnerabilidad alta en `System.Security.Cryptography.Xml` 10.0.0 durante build Docker.
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
