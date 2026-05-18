# Scorecard Go-Live Readiness - ACH Interbank

Fecha de generacion: 2026-05-18  
Version: 0.1 preliminar  
Rama analizada: `ACH-Interbank-Postgresql`  
Estado inicial: Candidato UAT / NO-GO productivo.  
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
| Funcionalidad core | 20% | PARCIAL | 72 | 14.4 | Backend CI OK; Angular CI creado | Falta UAT E2E formal, Angular CI verde y reconciliar test local backend |
| UAT y evidencias | 20% | CRITICO | 35 | 7.0 | Docs UAT y plantillas | Actas/evidencias pendientes |
| Seguridad | 15% | PARCIAL | 65 | 9.75 | Politicas/middleware + `[Authorize]` en AchResponses | `.env` trackeado, roles/policies y secretos requieren validacion |
| Interoperabilidad externa | 15% | CRITICO | 35 | 5.25 | Sobre digital/naming/CENIT | Validacion externa pendiente |
| Operacion y soporte | 10% | PARCIAL | 58 | 5.8 | Runbooks/docs + dotnet-ci OK | Backup/restore/rollback y Docker/health pendientes |
| Observabilidad | 10% | PARCIAL | 60 | 6.0 | Logs, audit, health | Health parcial; evidencia Docker/health pendiente |
| Documentacion y trazabilidad | 10% | PARCIAL | 70 | 7.0 | README y docs readiness actualizados | Firmas/evidencias pendientes |
| **Total** | **100%** | **NO-GO** |  | **55.2** |  | **NO-GO** |

## Estado Inicial

Resultado preliminar tras backend CI OK y creacion de Angular CI: **55.2 / 100 - NO-GO productivo**.

Clasificacion operacional: **Candidato UAT controlado**.

## Brechas Que Impiden Subir De Nivel

- UAT real/anonimizado sin acta firmada.
- Evidencias UAT incompletas.
- CENIT neteo/liquidez/CUD sin E2E homologado.
- Sobre digital/firma/certificados sin validacion externa oficial.
- Naming externo ACH/CENIT/STA sin cierre formal.
- Falta matriz endpoint-rol y validacion de politica granular para `AchResponsesController` aunque ya tiene `[Authorize]`.
- Endpoint mismatch de interoperabilidad SPA/backend.
- `environment.prod.ts` corregido a base relativa; falta evidencia de build/deploy UAT.
- Angular CI pendiente hasta que el nuevo workflow pase en GitHub Actions; localmente `npm test` falla 3 specs existentes.
- Backend CI remoto OK para `3cbff61`; localmente `dotnet test` muestra divergencia en 1 test preproductivo.
- Docker/health checks pendientes de evidencia en ambiente.
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
- Angular CI pendiente mantiene release candidate tecnico incompleto.

## Proyeccion De Mejora

| Cierre requerido | Impacto esperado |
|---|---|
| Acta UAT y evidencias firmadas | Subir UAT/evidencias a 75+ |
| Cierre seguridad/configuracion | Subir seguridad a 75+ |
| Validacion externa sobre/naming | Subir interoperabilidad a 75+ |
| E2E CENIT/CUD | Subir funcionalidad/interoperabilidad |
| Backup/restore/rollback ensayado | Subir operacion |
