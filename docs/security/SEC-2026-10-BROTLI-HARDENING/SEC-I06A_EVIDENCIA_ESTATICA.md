# SEC-I06A — Evidencia técnica de preparación de pruebas

Fecha del incremento: 2026-10-27. Padre: `ACHInterbank_ACH_CR_I06_FIX01_SEC_I05_HARDENING_COMPLETO.zip`, SHA-256 `b3e43b46eb24efbf3523f915481dd6ac2115a81dffff9c8b1df76f9af978424a`.

## Alcance implementado

- `tests/Cfa.ACHInterbank.Tests/Security/SecI06AHeadersRegressionTests.cs` (regresión sintética HSTS/headers, con TestServer).
- `tests/Cfa.ACHInterbank.Tests/Security/SecI06ACredentialBoundaryTests.cs` (compatibilidad hash legado y PBKDF2, sin credenciales reales).
- `SEC-I06A_MATRIZ_PRUEBAS.json`: **23 escenarios** de seguridad y de regresión ACH/CENIT enlazados con métodos xUnit fuente; los financieros proceden de suites ya existentes, no se afirma cobertura ejecutada.
- `scripts/security/verify_sec_i06a.py`: validación offline y verificación de preservación del seed; `SEC-I06A_RUNBOOK_EJECUCION.md` recoge comandos y gates externos.
- `MEMORY.md`, `PLAN_IMPLEMENTACION.md` y `MEMORY_STATE.md` actualizados para SEC-I06A / SEC-I06B / SEC-I07.

## Evidencias ejecutadas

| Verificación | Resultado |
|---|---|
| Manifest JSON, 23 escenarios y IDs únicos | PASS_STATIC |
| Referencias a métodos xUnit existentes en fuente | PASS_STATIC |
| Presencia de clases nuevas de prueba | PASS_STATIC |
| Estado no ejecutado de todos los escenarios | PASS_STATIC |
| Igualdad de bytes SHA-256 del seed administrativo | PASS_STATIC (`5aa78003c215fb9f3315cbf097a593018f65d2b3580e657a306bacbec6ce41e7`) |
| Flags GO_INCREMENT / GO_DEPLOY / GO_LIVE_MONETARY, Quartz | PASS_STATIC |
| Gates externos no ejecutados registrados honestamente | PASS_STATIC |
| Total verificador offline | **9/9 PASS_STATIC** |

## No ejecutado — NO afirmar PASS

- `dotnet restore/build/test` y tests xUnit: **WRITTEN_NOT_EXECUTED** / `NOT_RUN_ENVIRONMENT` (SDK .NET 10 ausente).
- Docker, Nginx final 1.30.6, SQL Server, PostgreSQL, ingress, CFA/MFT, UAT y homologación: `NOT_RUN_ENVIRONMENT`.
- No se ha probado la regresión funcional monetaria ni la ausencia de defectos de compilación. Ningún tráfico ACH/CENIT ni dinero real.

## Restricciones respetadas

- Usuario/contraseña/hash del seed administrativo **sin cambios** (`PRESERVE_ADMIN_SEED_CREDENTIAL=TRUE`).
- No se modifican fuentes productivas C#, contratos NACHA-M, tareas Quartz, migraciones ni reglas financieras.
- `GO_INCREMENT=YES`; `GO_DEPLOY=NO`; `GO_LIVE_MONETARY=NO`.
- SEC-I06B permanece en backlog sin bloquear preparación SEC-I07.

**Limitación:** verificación estática no sustituye compilación, tests, SCA en línea ni aprobación de despliegue.
