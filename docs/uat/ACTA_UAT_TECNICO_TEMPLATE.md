# Acta UAT Tecnico Basico - Template - ACH Interbank

Fecha de generacion: 2026-05-18  
Version: 0.2 preliminar  
Rama objetivo: `fix/spa-docker-runtime-proxy-and-images`  
Validacion humana requerida: si  
Clasificacion: no registrar passwords, tokens completos, datos reales, datos personales ni secretos.

## Datos Del Proyecto

| Campo | Valor |
|---|---|
| Proyecto | ACH Interbank |
| Tipo de UAT | Tecnico autenticado basico |
| Ambiente | Docker Compose local/UAT tecnico |
| URL SPA | `http://localhost:743` |
| URL API | `http://localhost:843` |
| Rama | `fix/spa-docker-runtime-proxy-and-images` |
| Commit | `141484fc78434322ff87f25c8914002719b35264` |
| Usuario demo | `admin` |
| Password | NO DOCUMENTAR |
| Token | NO DOCUMENTAR COMPLETO |
| Token recibido | Si, evidencia enmascarada `eyJ...6_k` |
| Roles esperados | `Admin`, `ACH.Operator` |
| Roles confirmados | Parcial: `Admin` visible; `ACH.Operator` no visible en respuesta/JWT |

## Participantes

| Rol | Nombre | Area | Firma |
|---|---|---|---|
| Tecnologia | PENDIENTE | PENDIENTE | PENDIENTE |
| QA | PENDIENTE | PENDIENTE | PENDIENTE |
| Operaciones | PENDIENTE | PENDIENTE | PENDIENTE |
| Seguridad | PENDIENTE | PENDIENTE | PENDIENTE |
| Auditoria | PENDIENTE | PENDIENTE | PENDIENTE |
| Negocio | PENDIENTE | PENDIENTE | PENDIENTE |

## Escenarios

| ID | Escenario | Resultado | Evidencia | Observacion |
|---|---|---|---|---|
| UAT-TECH-001 | Health live | OK | EV-TECH-001 | HTTP 200 JSON. |
| UAT-TECH-002 | Health ready | OK | EV-TECH-002 | DB healthy. |
| UAT-TECH-003 | Login dummy negativo | OK | EV-TECH-003 | HTTP 401 JSON. |
| UAT-TECH-004 | Menu sin token | OK | EV-TECH-004 | HTTP 401 desde API. |
| UAT-TECH-005 | Login real usuario demo | OK | EV-TECH-006 | No se documento password ni token completo. |
| UAT-TECH-006 | Token/JWT claims | OK con observacion | EV-TECH-007 | `ACH.Operator` no visible. |
| UAT-TECH-007 | Menu con token | OK | EV-TECH-008 | JSON esperado recibido. |
| UAT-TECH-008 | Pantallas protegidas read-only | OK | EV-TECH-009 a EV-TECH-011 | No se crearon transacciones. |
| UAT-TECH-009 | Navegacion dashboard/logs SPA | PARCIAL | EV-TECH-013, EV-TECH-014 | Evidencia visual automatizada pendiente por limitacion de herramienta. |
| UAT-TECH-010 | Logs sin secretos | OK con observacion | EV-TECH-018 | Sin tokens/passwords completos; EF debug muestra nombre de columna `PasswordHash`. |
| UAT-TECH-011 | PostgreSQL healthy y accesible local | OK | EV-TECH-015 a EV-TECH-017 | Solo UAT tecnico/local. |

## Decision Preliminar

- [ ] APROBADO UAT TECNICO BASICO.
- [x] APROBADO CON OBSERVACIONES.
- [ ] RECHAZADO.
- [ ] BLOQUEADO.

Observaciones de aprobacion preliminar:

- Confirmar formalmente si el seed `admin` debe tener claim/rol `ACH.Operator` visible o si `Admin` cubre el alcance operativo.
- Mantener DEF-UAT-014 como limitacion de browser integrado, no como bloqueo funcional mientras HTTP/token/logs sigan OK.
- Productivo permanece NO-GO.

## Riesgos Aceptados

| Riesgo | Decision | Responsable | Firma |
|---|---|---|---|
| `ACH.Operator` no visible en respuesta/JWT | PENDIENTE | Seguridad/Tecnologia | PENDIENTE |
| Evidencia visual automatizada no generada por limitacion de herramienta | PENDIENTE | QA/DevOps | PENDIENTE |
| PostgreSQL logs con FATAL previos por usuarios inexistentes | PENDIENTE | Operaciones/DevOps | PENDIENTE |

## Firmas

| Area | Nombre | Decision | Firma | Fecha |
|---|---|---|---|---|
| Tecnologia | PENDIENTE | PENDIENTE | PENDIENTE | PENDIENTE |
| QA | PENDIENTE | PENDIENTE | PENDIENTE | PENDIENTE |
| Operaciones | PENDIENTE | PENDIENTE | PENDIENTE | PENDIENTE |
| Seguridad | PENDIENTE | PENDIENTE | PENDIENTE | PENDIENTE |
| Auditoria | PENDIENTE | PENDIENTE | PENDIENTE | PENDIENTE |
| Negocio | PENDIENTE | PENDIENTE | PENDIENTE | PENDIENTE |
