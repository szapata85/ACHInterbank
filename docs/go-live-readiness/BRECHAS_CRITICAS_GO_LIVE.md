# Brechas Criticas Go-Live - ACH Interbank

Fecha de generacion: 2026-05-18  
Version: 0.1 preliminar  
Rama analizada: `ACH-Interbank-Postgresql`  
Estado: registro actualizado tras correcciones tecnicas de bajo riesgo; requiere triage y aceptacion humana.

## Matriz

| ID | Severidad | Descripcion | Evidencia | Impacto | Riesgo | Accion correctiva | Responsable | Requiere codigo | Requiere validacion humana | Estado |
|---|---|---|---|---|---|---|---|---|---|---|
| G-01 | CRITICO | UAT real/anonimizado sin acta firmada. | `docs/uat/ACTA_UAT_DATOS_REALES_TEMPLATE.md` | Bloquea UAT formal y productivo. | Alto | Ejecutar UAT, completar evidencias y firmar acta. | Auditoria/Operaciones/Negocio | No | Si | Abierto |
| G-02 | CRITICO | CENIT neteo/liquidez/CUD sin E2E homologado. | `docs/uat/cenit-cycles-liquidity-cud-acceptance-checklist.md` | Riesgo operativo-financiero. | Critico | Ejecutar E2E con evidencia CUD o aceptar brecha formal. | Operaciones/Tesoreria | No inicialmente | Si | Abierto |
| G-03 | CRITICO | Sobre digital sin validacion externa oficial. | `docs/uat/digital-envelope-certificate-acceptance-checklist.md` | Rechazo externo/regulatorio. | Alto | Obtener vector/certificacion externa o waiver formal. | Seguridad/Operaciones | PENDIENTE VALIDAR | Si | Abierto |
| G-04 | ALTO | Naming externo ACH/CENIT/STA pendiente de cierre formal. | `docs/audits/external-filename-normative-matrix-ach-cenit-sta-current.md` | Rechazo de archivos o duplicidad. | Alto | Cerrar reglas por camara/flujo y firmar. | Compliance/Operaciones | PENDIENTE VALIDAR | Si | Abierto |
| G-05 | ALTO | `AchResponsesController` sin `[Authorize]` explicito o no evidenciado. | `src/Cfa.ACHInterbank.Api/Controllers/AchResponsesController.cs`, `tests/Cfa.ACHInterbank.Tests/AchResponsesControllerTests.cs` | Posible exposicion de flujo sensible. | Alto | Se agrego `[Authorize]` general y prueba de reflexion; falta validar policy granular si negocio/seguridad lo exige. | Seguridad/Tecnologia | Si | Si | Corregida - pendiente validar |
| G-06 | ALTO | SPA consume endpoints de interoperabilidad no encontrados en backend. | `web/ach-interbank-ui/src/app/features/nacha-security/services/interoperability-api.service.ts` | Pantalla rota o falsa cobertura UAT. | Alto | No se invento backend; se dejo TODO y prueba de contrato SPA. Requiere decision funcional: backend real, feature flag o retirar flujo. | Tecnologia/Seguridad | Si/PENDIENTE | Si | Parcial |
| G-07 | ALTO | Config productiva SPA apunta a `localhost`. | `web/ach-interbank-ui/src/environments/environment.prod.ts`, `tests/Cfa.ACHInterbank.Tests/AngularSpaUatReadinessCharacterizationTests.cs` | Despliegue productivo no apto. | Alto | Se cambio `apiBaseUrl` productivo a relativo y se agrego prueba anti-localhost; falta validar build/deploy UAT. | Tecnologia/Operaciones | Si | Si | Corregida - pendiente validar |
| G-08 | MEDIO | README raiz conserva plantilla generica o drift documental. | `README.md` | Confusion operativa y de release. | Medio | README reemplazado por contenido operativo real ACH Interbank. | Tecnologia | No/cambio doc | Si | Corregida |
| G-09 | ALTO | `.env` versionado si aplica. | `.env` existe en pre-check; `.gitignore` actualizado | Riesgo de secretos/datos sensibles. | Alto | Se agregaron reglas `.gitignore`; no se borro ni destrackeo `.env`. Revisar contenido y rotar si corresponde. | Seguridad/Operaciones | PENDIENTE | Si | Parcial |
| G-10 | ALTO | Credenciales de ejemplo en README/docker-compose. | `README.md`, `docker-compose.yml`, `.env.example`, `.env.test.example` | Riesgo de uso accidental. | Alto | Se reemplazaron defaults por placeholders locales/de demo y README advierte no usar secretos reales. | Seguridad/Operaciones | Si para compose | Si | Corregida - pendiente validar |
| G-11 | MEDIO | OpenBao no incluido en docker-compose principal si aplica al ambiente. | `docker-compose.yml`, `scripts/openbao`, `docs/architecture/openbao-integration-2026-04-22.md`, `docs/dev/docker-compose-openbao-uat-2026-04-22.md` | Secret provider puede no estar disponible. | Medio/Alto | Se documento que no se modifica compose principal; usar scripts/docs existentes o excepcion formal. | Seguridad/Operaciones | PENDIENTE | Si | Parcial - requiere decision humana |
| G-12 | MEDIO | Ruta absoluta `DocumentationFile` en csproj API. | `src/Cfa.ACHInterbank.Api/Cfa.ACHInterbank.Api.csproj`, `tests/Cfa.ACHInterbank.Tests/ProjectConfigurationPortabilityTests.cs` | Build/CI fragil fuera de host original. | Medio | Se cambio a ruta relativa MSBuild y se agrego prueba de portabilidad. | Tecnologia | Si | No | Corregida - pendiente build |
| G-13 | MEDIO | Falta de runbook operativo UAT/preproductivo consolidado. | Este documento crea `docs/operations/RUNBOOK_UAT_Y_PREPRODUCTIVO.md` | Operacion no repetible. | Medio | Revisar/aprobar runbook. | Operaciones | No | Si | Parcial |
| G-14 | ALTO | Falta de evidencia de backup/restore/rollback. | NO ENCONTRADO | Recuperacion no demostrada. | Alto | Documentar y ensayar backup/restore/rollback. | Operaciones/Tecnologia | No inicialmente | Si | Abierto |
| G-15 | MEDIO | Falta de evidencia de health checks completos. | `/health/live`, `/health/ready` solo DB | Monitoreo incompleto. | Medio | Agregar checks o documentar alcance y monitoreo externo. | Tecnologia/Operaciones | PENDIENTE | Si | Abierto |
| G-16 | MEDIO | Drift documental entre docs historicos y estado actual. | `docs/audits`, `README.md` | Decision de comite inconsistente. | Medio | Usar paquete comite y documentos current como fuente vigente. | Auditoria/Tecnologia | No | Si | Abierto |
| G-17 | ALTO | Validaciones no destructivas ejecutadas con fallos existentes en suites completas. | `dotnet build` OK; `npm run build` OK; `dotnet test` falla 1 prueba preproductiva; `npm test` falla 3 specs de transacciones | Estado tecnico parcialmente confirmado; suites completas no estan verdes. | Alto | Corregir fallos existentes o aceptarlos formalmente antes de release candidate. | Tecnologia/QA | PENDIENTE | Si | Parcial |

## Decision Inicial

Con cualquier brecha CRITICA abierta, el estado permanece **NO-GO productivo**. UAT controlado puede avanzar si el ambiente esta disponible, los datos estan anonimizados y las brechas se comunican como restricciones.
