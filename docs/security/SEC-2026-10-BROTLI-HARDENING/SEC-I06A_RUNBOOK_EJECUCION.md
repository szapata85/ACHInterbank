# SEC-I06A — Runbook de pruebas de seguridad y regresión (sin operación monetaria)

**Naturaleza:** preparación de código xUnit y trazabilidad de escenarios. Los tests xUnit de esta entrega se marcan `WRITTEN_NOT_EXECUTED` o `NOT_RUN_THIS_INCREMENT` cuando son preexistentes. El verificador Python es exclusivamente de **inspección estática**: no prueba compilación .NET ni reglas financieras ejecutables.

## Pruebas incorporadas

- `Security/SecI06AHeadersRegressionTests.cs`: middleware aislado vía `TestServer`, HSTS según entorno y esquema, CSP, Referrer-Policy, nosniff y ocultación de banners. **No** levanta la API real.
- `Security/SecI06ACredentialBoundaryTests.cs`: aceptación de hash SHA-256 legado en mayúsculas; PBKDF2 antiguo solicita rehash sin cambiar credenciales; formatos inválidos rechazados. Fixture 100 % sintético. **Nunca** usa la contraseña del seed.
- `SEC-I06A_MATRIZ_PRUEBAS.json`: traza escenarios de seguridad, ingreso NACHA, ACHCOL, CENIT, devoluciones, idempotencia y Quartz a métodos ya escritos (sin afirmar éxito de ejecución).

## Fase offline local — disponible en modo Chat

Desde la raíz extraída del proyecto:

```bash
python scripts/security/verify_sec_i06a.py --root .
```

También acepta `--zip` para validar el paquete completo sin extraerlo. Solo verifica fuentes/manifest, bytes del seed y banderas de gate.

## SEC-I06B — cuando exista entorno .NET 10 autorizado

Ejecutar solo sobre un entorno **aislado con datos ficticios, sin claves ni endpoints ACH/CENIT reales**. No activar Quartz ACH-CR-001.

```bash
dotnet --info
dotnet restore ACHInterbank.sln
dotnet build ACHInterbank.sln -c Release --no-restore
# Seguridad: ejecutar suite delimitada, sin servicios externos
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~SecI06AHeadersRegressionTests|FullyQualifiedName~SecI06ACredentialBoundaryTests|FullyQualifiedName~CredentialPasswordHasherTests|FullyQualifiedName~RateLimitClientIdentitySecI05Tests" --logger "trx;LogFileName=SEC-I06B-security.trx"
```

Luego, en ambiente de integración **simulado y controlado**, seleccionar las suites existentes de `SEC-I06A_MATRIZ_PRUEBAS.json`, realizar prueba de regresión completa y registrar resultados por caso. No ejecutar una suite completa en un entorno conectado a cámaras, CFA o MFT reales sin autorización y aislamiento confirmado; algunos tests de integración heredados pueden asumir infraestructura.

## Evidencias pendientes antes de GO_DEPLOY

1. `.NET` SDK/restauración/compilación exitosa y prueba xUnit real con TRX.
2. Test de nginx 1.30.6, gateway, DNS/proxies y configuraciones TLS auténticas.
3. SQL Server y PostgreSQL aislados, fixtures sanitizados, idempotencia y rollback.
4. Simulación de respuestas MFT/CFA desconocidas y reintentos; nunca comunicar transacciones reales.
5. Verificación de admisión NACHA-M, rechazos/devoluciones y calendario independiente ACH Colombia (7 ciclos) / CENIT (5 sesiones).
6. Riesgo residual del seed administrativo fijo documentado y aceptado por propietario de seguridad antes de GO_DEPLOY; no modificar su contraseña sin instrucción expresa del solicitante.

Resultados no ejecutados: `NOT_RUN_ENVIRONMENT`. La ausencia de ambiente no bloquea **SEC-I07 (preparación de homologación)**, pero `GO_DEPLOY=NO` y `GO_LIVE_MONETARY=NO` permanecen invariables.
