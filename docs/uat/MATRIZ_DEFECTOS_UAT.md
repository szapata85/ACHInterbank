# Matriz de Defectos UAT - ACH Interbank

Fecha de generacion: 2026-05-18  
Version: 0.1 preliminar  
Rama analizada: `ACH-Interbank-Postgresql`  
Estado: matriz inicial; requiere triage humano.

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
| DEF-UAT-001 | UAT-REAL-033 / UAT-REAL-002 | Alta | `AchResponsesController` no evidenciaba `[Authorize]` explicito ni politicas por accion en el pre-check. | Backend API | `src/Cfa.ACHInterbank.Api/Controllers/AchResponsesController.cs`, `tests/Cfa.ACHInterbank.Tests/AchResponsesControllerTests.cs` | Tecnologia/Seguridad | Corregido | 2026-05-18 | 2026-05-18 | `[Authorize]` general aplicado; validar policy granular |
| DEF-UAT-002 | UAT-REAL-034 | Alta | Configuracion productiva SPA apuntaba a `localhost`, no apta para despliegue productivo. | Angular config | `web/ach-interbank-ui/src/environments/environment.prod.ts`, `tests/Cfa.ACHInterbank.Tests/AngularSpaUatReadinessCharacterizationTests.cs` | Tecnologia/Operaciones | Corregido | 2026-05-18 | 2026-05-18 | Base relativa aplicada; validar build/deploy |
| DEF-UAT-003 | UAT-REAL-035 | Alta | SPA consume endpoints `nacha-security/interoperability/*` y no se encontro controller backend equivalente. | SPA/API | `web/ach-interbank-ui/src/app/features/nacha-security/services/interoperability-api.service.ts` | Tecnologia | En analisis | 2026-05-18 | PENDIENTE | Servicio marcado con TODO; requiere decision backend/feature flag/NO APLICA |
| DEF-UAT-004 | UAT-REAL-031 | Media | README raiz conservaba plantilla generica y drift operativo. | Documentacion | `README.md` | Tecnologia | Corregido | 2026-05-18 | 2026-05-18 | README saneado y enlaza docs UAT/go-live |
| DEF-UAT-005 | UAT-REAL-030 | Alta | Existe `.env` versionado; requiere revision para asegurar que no contenga secretos reales. | Configuracion | `.env` | Seguridad/Operaciones | Abierto | 2026-05-18 | PENDIENTE | Revisar y sanear si aplica |
| DEF-UAT-006 | UAT-REAL-030 | Alta | `docker-compose.yml` contenia defaults sensibles o credenciales de ejemplo parametrizadas. | Docker/config | `docker-compose.yml`, `.env.example`, `.env.test.example` | Seguridad/Operaciones | Corregido | 2026-05-18 | 2026-05-18 | Defaults reemplazados por placeholders; validar UAT/preprod |
| DEF-UAT-007 | UAT-REAL-025 | Media | OpenBao no esta incluido en `docker-compose.yml` principal aunque existen scripts/configuracion. | Secretos/OpenBao | `docker-compose.yml`, `scripts/openbao` | Seguridad/Operaciones | Abierto | 2026-05-18 | PENDIENTE | Definir compose UAT o excepcion formal |
| DEF-UAT-008 | UAT-REAL-028 | Media | `DocumentationFile` del API csproj usaba ruta absoluta. | Build/config | `src/Cfa.ACHInterbank.Api/Cfa.ACHInterbank.Api.csproj`, `tests/Cfa.ACHInterbank.Tests/ProjectConfigurationPortabilityTests.cs` | Tecnologia | Corregido | 2026-05-18 | 2026-05-18 | Ruta relativa MSBuild aplicada; validar build |
| DEF-UAT-009 | UAT-REAL-032 | Media | Health checks actuales cubren live/ready DB, pero no evidencian Quartz/OpenBao/externos. | Observabilidad | `src/Cfa.ACHInterbank.Api/DependencyInjectionService.cs` | Tecnologia/Operaciones | Abierto | 2026-05-18 | PENDIENTE | Ampliar controles o documentar alcance |
| DEF-UAT-010 | UAT-REAL-019 | Bloqueante | Evidencia CUD operacional no encontrada; si CUD aplica al alcance, bloquea productivo. | CENIT/CUD | `docs/uat/cenit-cycles-liquidity-cud-acceptance-checklist.md` | Operaciones/Tesoreria | Abierto | 2026-05-18 | PENDIENTE | Ejecutar E2E homologado y adjuntar evidencia |
| DEF-UAT-011 | UAT-REAL-028 | Alta | Suite backend completa no queda verde: falla `AchPreproductionCertificationTests.BatchResolver_RejectsTransactionsWhenResolvedCycleIsClosed`. | Tests backend | `tests/Cfa.ACHInterbank.Tests/AchPreproductionCertificationTests.cs` | Tecnologia/QA | Abierto | 2026-05-18 | PENDIENTE | Analizar sin cambiar reglas ACH/CENIT; ajustar test o servicio solo con decision tecnica |
| DEF-UAT-012 | UAT-REAL-033 | Alta | Suite Angular completa no queda verde: fallan 3 specs existentes de `TransactionCreateComponent`. | Tests Angular | `web/ach-interbank-ui/src/app/features/transactions/components/transaction-create/transaction-create.component.spec.ts` | Tecnologia/QA | Abierto | 2026-05-18 | PENDIENTE | Corregir specs/componentes en fase de pruebas criticas |
