# SEC-I04 — Diff resumido y sin secretos

Modificados:
- AuthService.cs: reemplazo de SHA-256 directo en verificación y reset por servicio PBKDF2/legado.
- UsersService.cs: hash PBKDF2 en Create y Update; eliminación de SHA-256 local.
- MEMORY.md, PLAN_IMPLEMENTACION.md y MEMORY_STATE.md: continuidad, riesgo y decisión de preservar seed.

Agregados:
- CredentialPasswordHasher.cs
- CredentialPasswordHasherTests.cs
- scripts/security/test_sec_i04_auth_static.py
- SEC-I04_EVIDENCIA_ESTATICA.md
- SEC-I04_DIFF_RESUMEN.md
- SEC-I04_VALIDACION_ESTATICA.json

No modificados: UserConfiguration.cs (credencial, hash y HasData del seed intactos), esquemas y migraciones, ACH-CR-001, NACHA, MFT, ciclos ACH/CENIT.
