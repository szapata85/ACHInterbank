# Evidencias NACHA Config Table-Driven

## Fase 6B.1

Perfiles oficiales creados/completados:

- `OFFICIAL_ACH_SALIDA_ORIGINAL_V1_0`.
- `OFFICIAL_CENIT_SALIDA_ORIGINAL_V1_0`.

Evidencia: `docs/uat/evidencias/nacha-config-table-driven/phase-6b1-profiles/`.

## Fase 6B.2

Builder oficial table-driven activado:

- ACH Colombia usa perfil ACH publicado.
- CENIT usa perfil CENIT publicado.
- Records 1/5/6/7/8/9 salen desde profile.
- No hay fallback legacy en modo oficial.
- Fail-fast con codigos `NACHA_*`.

Evidencia: `docs/uat/evidencias/nacha-config-table-driven/phase-6b2-builder/`.

Pruebas:

- `dotnet build ACHInterbank.sln -c Release`: OK.
- `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build`: OK, 1198 passed, 1 skipped, 0 failed.

Productivo: **NO-GO**.
