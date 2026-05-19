# Matriz de Defectos UAT - ACH Interbank

Fecha de generacion: 2026-05-18  
Version: 0.2  
Rama analizada: `fix/spa-docker-runtime-proxy-and-images`  
Estado: matriz actualizada tras reintento UAT tecnico autenticado.

## Severidades

- Bloqueante: impide UAT formal o productivo.
- Alta: afecta flujo critico o seguridad, requiere cierre antes de productivo.
- Media: requiere correccion o aceptacion formal.
- Baja: mejora o ajuste documental.

## Estados

- Abierto.
- En analisis.
- Corregido.
- Rechazado.
- Diferido.
- Aceptado como riesgo.
- Cerrado.

## Matriz

| ID defecto | Escenario | Severidad | Descripcion | Componente | Evidencia | Responsable | Estado | Fecha apertura | Fecha cierre | Decision |
|---|---|---|---|---|---|---|---|---|---|---|
| DEF-UAT-001 | UAT-REAL-033 / UAT-REAL-002 | Alta | `AchResponsesController` no evidenciaba `[Authorize]` explicito ni politicas por accion en el pre-check. | Backend API | `src/Cfa.ACHInterbank.Api/Controllers/AchResponsesController.cs`, `tests/Cfa.ACHInterbank.Tests/AchResponsesControllerTests.cs` | Tecnologia/Seguridad | Corregido | 2026-05-18 | 2026-05-18 | `[Authorize]` general aplicado; validar policy granular. |
| DEF-UAT-002 | UAT-REAL-034 | Alta | Configuracion productiva SPA apuntaba a `localhost`, no apta para despliegue productivo. | Angular config | `web/ach-interbank-ui/src/environments/environment.prod.ts`, `tests/Cfa.ACHInterbank.Tests/AngularSpaUatReadinessCharacterizationTests.cs` | Tecnologia/Operaciones | Corregido | 2026-05-18 | 2026-05-18 | Base relativa aplicada; proxy Docker validado. |
| DEF-UAT-003 | UAT-REAL-035 | Alta | SPA consume endpoints `nacha-security/interoperability/*` y no se encontro controller backend equivalente. | SPA/API | `web/ach-interbank-ui/src/app/features/nacha-security/services/interoperability-api.service.ts` | Tecnologia | En analisis | 2026-05-18 | PENDIENTE | Requiere decision backend/feature flag/NO APLICA. |
| DEF-UAT-004 | UAT-REAL-031 | Media | README raiz conservaba plantilla generica y drift operativo. | Documentacion | `README.md` | Tecnologia | Corregido | 2026-05-18 | 2026-05-18 | README saneado y enlaza docs UAT/go-live. |
| DEF-UAT-005 | UAT-REAL-030 | Alta | Existe `.env` versionado; requiere revision para asegurar que no contenga secretos reales. | Configuracion | `.env` | Seguridad/Operaciones | Abierto | 2026-05-18 | PENDIENTE | Revisar y sanear si aplica. |
| DEF-UAT-006 | UAT-REAL-030 | Alta | `docker-compose.yml` contenia defaults sensibles o credenciales de ejemplo parametrizadas. | Docker/config | `docker-compose.yml`, `.env.example`, `.env.test.example` | Seguridad/Operaciones | Corregido | 2026-05-18 | 2026-05-18 | Defaults reemplazados por placeholders; validar UAT/preprod. |
| DEF-UAT-007 | UAT-REAL-025 | Media | OpenBao no esta incluido en `docker-compose.yml` principal aunque existen scripts/configuracion. | Secretos/OpenBao | `docker-compose.yml`, `scripts/openbao` | Seguridad/Operaciones | Abierto | 2026-05-18 | PENDIENTE | Definir compose UAT o excepcion formal. |
| DEF-UAT-008 | UAT-REAL-028 | Media | `DocumentationFile` del API csproj usaba ruta absoluta. | Build/config | `src/Cfa.ACHInterbank.Api/Cfa.ACHInterbank.Api.csproj`, `tests/Cfa.ACHInterbank.Tests/ProjectConfigurationPortabilityTests.cs` | Tecnologia | Corregido | 2026-05-18 | 2026-05-18 | Ruta relativa MSBuild aplicada; validar build. |
| DEF-UAT-009 | UAT-REAL-032 | Media | Health checks actuales cubren live/ready DB, pero no evidencian Quartz/OpenBao/externos. | Observabilidad | `src/Cfa.ACHInterbank.Api/DependencyInjectionService.cs` | Tecnologia/Operaciones | Abierto | 2026-05-18 | PENDIENTE | Ampliar controles o documentar alcance. |
| DEF-UAT-010 | UAT-REAL-019 | Bloqueante | Evidencia CUD operacional no encontrada; si CUD aplica al alcance, bloquea productivo. | CENIT/CUD | `docs/uat/cenit-cycles-liquidity-cud-acceptance-checklist.md` | Operaciones/Tesoreria | Abierto | 2026-05-18 | PENDIENTE | Ejecutar E2E homologado y adjuntar evidencia. |
| DEF-UAT-011 | UAT-REAL-028 | Alta | Suite backend completa no queda verde: falla `AchPreproductionCertificationTests.BatchResolver_RejectsTransactionsWhenResolvedCycleIsClosed`. | Tests backend | `tests/Cfa.ACHInterbank.Tests/AchPreproductionCertificationTests.cs` | Tecnologia/QA | Abierto | 2026-05-18 | PENDIENTE | Analizar sin cambiar reglas ACH/CENIT. |
| DEF-UAT-012 | UAT-REAL-033 | Alta | Suite Angular completa no queda verde: fallan 3 specs existentes de `TransactionCreateComponent`. | Tests Angular | `web/ach-interbank-ui/src/app/features/transactions/components/transaction-create/transaction-create.component.spec.ts` | Tecnologia/QA | Abierto | 2026-05-18 | PENDIENTE | Corregir specs/componentes en fase de pruebas criticas. |
| DEF-UAT-013 | UAT-TECH-005 | Alta | Variables `ACH_UAT_DEMO_USERNAME` y `ACH_UAT_DEMO_PASSWORD` no estaban disponibles en ejecuciones previas; bloqueaban login real controlado. | Ambiente/UAT tecnico | `docs/uat/EJECUCION_UAT_TECNICO_BASICO.md` | Operaciones/QA/DevOps | Cerrado | 2026-05-18 | 2026-05-18 | Variables presentes en este reintento; login demo 200 ejecutado sin imprimir secretos. |
| DEF-UAT-014 | UAT-TECH-009 | Media | Browser integrado no pudo aportar evidencia visual automatizada confiable en esta sesion. | Browser/SPA | `docs/uat/EVIDENCIAS_UAT_TECNICO_BASICO.md` | QA/DevOps | Abierto | 2026-05-18 | PENDIENTE | Mantener como limitacion de herramienta; validacion HTTP con token y logs pasa. |
| DEF-UAT-015 | UAT-TECH-006 | Media | Rol esperado `ACH.Operator` no aparece en respuesta de login ni JWT; rol visible `Admin` autoriza menu y endpoints read-only. | Identidad/Autorizacion | `docs/uat/EJECUCION_UAT_TECNICO_BASICO.md` | Seguridad/Tecnologia | Abierto | 2026-05-18 | PENDIENTE | Confirmar si `ACH.Operator` debe estar asignado al seed `admin` o si `Admin` cubre permisos operativos. |
| OBS-UAT-001 | UAT-TECH-011 | Baja | Logs PostgreSQL muestran FATAL previos por usuarios inexistentes `root`/`sa`. | PostgreSQL/Operacion | `docker compose logs postgres --tail=120` | Operaciones/DevOps | Abierto | 2026-05-18 | PENDIENTE | Revisar origen de probes/conexiones; no bloqueo del UAT tecnico basico. |
