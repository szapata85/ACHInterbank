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
| Funcionalidad core | 20% | PARCIAL | 76 | 15.2 | Backend CI OK; Angular CI OK; API/DB health OK en Docker | Falta UAT E2E formal y resolver SPA->API en compose |
| UAT y evidencias | 20% | CRITICO | 35 | 7.0 | Docs UAT y plantillas | Actas/evidencias pendientes |
| Seguridad | 15% | PARCIAL | 65 | 9.75 | Politicas/middleware + `[Authorize]` en AchResponses | `.env` trackeado, roles/policies y secretos requieren validacion |
| Interoperabilidad externa | 15% | CRITICO | 35 | 5.25 | Sobre digital/naming/CENIT | Validacion externa pendiente |
| Operacion y soporte | 10% | PARCIAL | 62 | 6.2 | Docker compose config/build/runtime directo OK; runbooks/docs | Backup/restore/rollback y proxy SPA->API pendientes |
| Observabilidad | 10% | PARCIAL | 68 | 6.8 | `/health/live` y `/health/ready` OK en Docker | Health cubre API/DB; faltan Quartz/OpenBao/externos y monitoreo |
| Documentacion y trazabilidad | 10% | PARCIAL | 73 | 7.3 | README, docs readiness y evidencia runtime actualizados | Firmas/evidencias UAT pendientes |
| **Total** | **100%** | **NO-GO** |  | **56.5** |  | **NO-GO** |

## Estado Inicial

Resultado preliminar tras backend CI OK, angular CI OK y validacion Docker runtime directa: **56.5 / 100 - NO-GO productivo**.

Clasificacion operacional: **Candidato UAT controlado**.

## Brechas Que Impiden Subir De Nivel

- UAT real/anonimizado sin acta firmada.
- Evidencias UAT incompletas.
- CENIT neteo/liquidez/CUD sin E2E homologado.
- Sobre digital/firma/certificados sin validacion externa oficial.
- Naming externo ACH/CENIT/STA sin cierre formal.
- Falta matriz endpoint-rol y validacion de politica granular para `AchResponsesController` aunque ya tiene `[Authorize]`.
- Endpoint mismatch de interoperabilidad SPA/backend.
- `environment.prod.ts` corregido a base relativa, pero el Nginx del contenedor SPA no proxya `/api`; bloquea UAT tecnico E2E desde compose.
- Angular CI remoto OK segun contexto; adjuntar evidencia al paquete RC.
- Backend CI remoto OK segun contexto; adjuntar evidencia al paquete RC.
- Docker compose config/build/runtime directo OK para API/PostgreSQL/SPA estatica; proxy SPA->API pendiente.
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
- Docker runtime directo OK no implica UAT tecnico E2E si la SPA no enruta API.

## Proyeccion De Mejora

| Cierre requerido | Impacto esperado |
|---|---|
| Acta UAT y evidencias firmadas | Subir UAT/evidencias a 75+ |
| Cierre seguridad/configuracion | Subir seguridad a 75+ |
| Validacion externa sobre/naming | Subir interoperabilidad a 75+ |
| E2E CENIT/CUD | Subir funcionalidad/interoperabilidad |
| Backup/restore/rollback ensayado | Subir operacion |
