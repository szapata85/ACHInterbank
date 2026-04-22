# Deployment Checklist Ejecutable — NACHA Security (Backend + SPA) — 2026-04-22

## 1) Prechecks globales
| CheckId | Control | Responsable | Estado | Resultado real | Evidencia | Observaciones |
|---|---|---|---|---|---|---|
| PRE-01 | Alcance: sin funcionalidades nuevas | Líder Técnico | Pendiente | N/A | N/A | |
| PRE-02 | Sin cambios criptográficos restringidos | Arquitecto Backend | Pendiente | N/A | N/A | |
| PRE-03 | Workflow PostgreSQL manual-only | DevOps | Pendiente | N/A | N/A | |
| PRE-04 | Plan rollback aprobado | Operaciones | Pendiente | N/A | N/A | |
| PRE-05 | On-call asignado (BE/FE/DB/Sec) | PM Técnico | Pendiente | N/A | N/A | |

## 2) Backend checklist
| CheckId | Control | Responsable | Estado | Resultado real | Evidencia | Observaciones |
|---|---|---|---|---|---|---|
| BE-01 | appsettings sin secretos | Seguridad | Pendiente | N/A | N/A | |
| BE-02 | ConnectionStrings correctas | DBA | Pendiente | N/A | N/A | |
| BE-03 | Artifact store fuera de repo | Backend | Pendiente | N/A | N/A | |
| BE-04 | Límite upload validado | Backend | Pendiente | N/A | N/A | |
| BE-05 | Claims/permisos finos disponibles | Seguridad | Pendiente | N/A | N/A | |
| BE-06 | Smoke endpoints operations | QA Backend | Pendiente | N/A | N/A | |
| BE-07 | Smoke endpoints certificados | QA Backend | Pendiente | N/A | N/A | |
| BE-08 | Logs/auditoría sanitizados | Seguridad | Pendiente | N/A | N/A | |
| BE-09 | HTTPS/CORS conforme política | DevOps | Pendiente | N/A | N/A | |

## 3) SPA checklist
| CheckId | Control | Responsable | Estado | Resultado real | Evidencia | Observaciones |
|---|---|---|---|---|---|---|
| FE-01 | API base URL correcta | Frontend | Pendiente | N/A | N/A | |
| FE-02 | Build producción validado | Frontend | Pendiente | N/A | N/A | |
| FE-03 | Rutas por rol validadas | QA Frontend | Pendiente | N/A | N/A | |
| FE-04 | Errores sanitizados | QA Frontend | Pendiente | N/A | N/A | |
| FE-05 | authorizeDownload -> downloadArtifact | QA Frontend | Pendiente | N/A | N/A | |
| FE-06 | sanitizeDownloadFileName activo | QA Frontend | Pendiente | N/A | N/A | |
| FE-07 | Sin secretos en bundle/env | Seguridad | Pendiente | N/A | N/A | |
| FE-08 | Smoke navegación por rol | QA Frontend | Pendiente | N/A | N/A | |

## 4) BD / migraciones
| CheckId | Control | Responsable | Estado | Resultado real | Evidencia | Observaciones |
|---|---|---|---|---|---|---|
| DB-01 | Backup previo | DBA | Pendiente | N/A | N/A | |
| DB-02 | Orden migraciones validado | DBA | Pendiente | N/A | N/A | |
| DB-03 | ExternalFileName* aplicado | DBA | Pendiente | N/A | N/A | |
| DB-04 | DigitalCertificate* aplicado | DBA | Pendiente | N/A | N/A | |
| DB-05 | DigitalEnvelopeOperationLogs aplicado | DBA | Pendiente | N/A | N/A | |
| DB-06 | NachaSecurityOperations aplicado | DBA | Pendiente | N/A | N/A | |
| DB-07 | Índices/constraints/RowVersion OK | DBA | Pendiente | N/A | N/A | |
| DB-08 | Validación post-migración | QA Backend | Pendiente | N/A | N/A | |

## 5) Seguridad y secretos
| CheckId | Control | Responsable | Estado | Resultado real | Evidencia | Observaciones |
|---|---|---|---|---|---|---|
| SEC-01 | No secretos en repo/config | Seguridad | Pendiente | N/A | N/A | |
| SEC-02 | Descarga autorizada+expirable | QA Seguridad | Pendiente | N/A | N/A | |
| SEC-03 | No plano con firma inválida | QA Seguridad | Pendiente | N/A | N/A | |
| SEC-04 | Logs sin contenido sensible | Seguridad | Pendiente | N/A | N/A | |
| SEC-05 | PFX/password fuera de repo | Seguridad | Pendiente | N/A | N/A | |
| SEC-06 | SecretRef no expuesto completo | Seguridad | Pendiente | N/A | N/A | |
| SEC-07 | Identifier/IV bloqueado (sin hardening) | Arquitecto | Pendiente | N/A | N/A | |

## 6) Smoke test post-despliegue
| SmokeId | Paso | Responsable | Estado | Resultado real | Evidencia | Observaciones |
|---|---|---|---|---|---|---|
| SMK-01 | Listar certificados | QA | Pendiente | N/A | N/A | |
| SMK-02 | Generar NACHA plano | QA | Pendiente | N/A | N/A | |
| SMK-03 | Generar NACHA cifrado `.ENV` | QA | Pendiente | N/A | N/A | |
| SMK-04 | Manual encrypt/decrypt | QA | Pendiente | N/A | N/A | |
| SMK-05 | Validar `SIGNATURE_VALIDATION_FAILED` | QA | Pendiente | N/A | N/A | |
| SMK-06 | Auditoría por operationId | QA | Pendiente | N/A | N/A | |
| SMK-07 | Denegaciones por permisos | QA Seguridad | Pendiente | N/A | N/A | |

## 7) Monitoreo postdespliegue (primeras 48h)
| MonitorId | Métrica | Umbral | Responsable | Estado | Evidencia |
|---|---|---|---|---|---|
| MON-01 | Error rate endpoints NACHA security | <= 2% | SoporteTecnico | Pendiente | N/A |
| MON-02 | Fallos autorización descarga | tendencia estable | SoporteTecnico | Pendiente | N/A |
| MON-03 | Eventos fail-close | monitoreo activo | Seguridad | Pendiente | N/A |
| MON-04 | Latencia operaciones críticas | dentro SLO | Backend | Pendiente | N/A |
| MON-05 | Alertas de seguridad | 0 críticas sin atender | Seguridad | Pendiente | N/A |

## 8) Rollback drill
| DrillId | Ejercicio | Responsable | Estado | Resultado real | Evidencia | Observaciones |
|---|---|---|---|---|---|---|
| RB-01 | Rollback backend a versión previa | DevOps | Pendiente | N/A | N/A | |
| RB-02 | Rollback SPA + limpieza cache/CDN | Frontend/DevOps | Pendiente | N/A | N/A | |
| RB-03 | Validación post-rollback | QA | Pendiente | N/A | N/A | |
| RB-04 | Restauración backup DB (simulado) | DBA | Pendiente | N/A | N/A | |
| RB-05 | Acta de drill y tiempos | PM Técnico | Pendiente | N/A | N/A | |

## 9) Criterios formales de cierre despliegue

### Cierre aprobado si:
- 100% de prechecks críticos (`PRE-*`, `SEC-*`) en Aprobado.
- 100% de smoke test críticos (`SMK-02`, `SMK-03`, `SMK-05`, `SMK-07`) aprobados.
- 0 incidentes críticos abiertos tras ventana inicial de monitoreo.
- Rollback drill ejecutado/documentado o evidencia de ejecución previa vigente.

### Cierre rechazado si:
- Falla de seguridad crítica.
- Descarga sin autorización.
- Exposición de secretos.
- Retorno de plano con firma inválida.
- Fallo de migraciones críticas sin plan de recuperación.

## 10) Defect tracking despliegue (template)
```text
DefectId:
CheckId/SmokeId:
Severidad:
Estado:
Ambiente:
Fecha/Hora UTC:
Responsable:
Descripción:
Resultado esperado:
Resultado obtenido:
Evidencia:
OperationId (si aplica):
Acción correctiva:
Fecha compromiso:
Resultado retest:
```

## 11) Deuda conocida `npm test` Angular
- Estado: no estable en entorno actual (CHROME_BIN/Karma/rimraf).
- Acción: no bloquea despliegue controlado si backend crítico y build SPA están OK, pero queda issue QA obligatorio con fecha compromiso.

## 12) Regla regulatoria
- `identifier/IV` permanece sin hardening hasta vector oficial ACH/CENIT.
- Este estado no bloquea operación interna controlada.
- Sí bloquea certificación oficial de interoperabilidad.

## 13) Comandos técnicos de validación

> Registrar en acta: comando ejecutado, fecha/hora UTC, ejecutor, resultado real y evidencia.
> No exponer secretos, PFX, passwords ni contenido NACHA sensible en salidas adjuntas.

### 13.1 Backend build y tests críticos
```bash
bash scripts/codex/setup-codex-env.sh
export DOTNET_ROOT=$HOME/.dotnet
export PATH=$HOME/.dotnet:$HOME/.dotnet/tools:$PATH

dotnet --info
dotnet restore ACHInterbank.sln
dotnet build ACHInterbank.sln -c Release
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj \
  -c Release \
  --filter "FullyQualifiedName~NachaSecurityOperation|FullyQualifiedName~ManualEncrypt|FullyQualifiedName~ManualDecrypt|FullyQualifiedName~GenerateEncrypted|FullyQualifiedName~Download|FullyQualifiedName~Permission|FullyQualifiedName~Authorization" \
  -v minimal

dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj \
  -c Release \
  --filter "FullyQualifiedName~Signature|FullyQualifiedName~OpenEnvelope|FullyQualifiedName~DigitalEnvelope|FullyQualifiedName~CertificateResolver|FullyQualifiedName~SecretResolver" \
  -v minimal

dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj \
  -c Release \
  --filter "FullyQualifiedName~Nacha|FullyQualifiedName~Mapping|FullyQualifiedName~BatchNumber" \
  -v minimal
```

### 13.2 EF Core y migraciones (PostgreSQL)
```bash
dotnet ef migrations list \
  --project src/Cfa.ACHInterbank.Persistence/Cfa.ACHInterbank.Persistence.csproj \
  --startup-project src/Cfa.ACHInterbank.Api/Cfa.ACHInterbank.Api.csproj \
  --context AchDbContext

dotnet ef database update \
  --project src/Cfa.ACHInterbank.Persistence/Cfa.ACHInterbank.Persistence.csproj \
  --startup-project src/Cfa.ACHInterbank.Api/Cfa.ACHInterbank.Api.csproj \
  --context AchDbContext
```

### 13.3 SPA build y estado de tests
```bash
cd web/ach-interbank-ui
npm ci
npm run build
npm test -- --watch=false --browsers=ChromeHeadless
```

### 13.4 Verificación de no cambios criptográficos restringidos
```bash
git log --oneline -n 20
git diff --name-only HEAD~1..HEAD
git diff -- src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ACH/CryptoServiceScoped.cs
git diff -- src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ACHSobreDigital/OpenEnvelopeAsyncService.cs
git diff -- src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ACH/RsaKeyProvider.cs
```

### 13.5 Verificación workflow PostgreSQL manual-only
```bash
sed -n '1,220p' .github/workflows/postgres-integration-tests.yml
rg -n "workflow_dispatch|if: github.event_name == 'workflow_dispatch'|pull_request|push|schedule|workflow_run" \
  .github/workflows/postgres-integration-tests.yml
```

### 13.6 Verificación rápida de secretos y artefactos sensibles
```bash
git status --short
rg -n "BEGIN (RSA|PRIVATE|EC) KEY|-----BEGIN|SecretRef|PFX|password" src web docs
rg -n "nacha-security/operations|authorize-download|download" src/Cfa.ACHInterbank.Api web/ach-interbank-ui
```
