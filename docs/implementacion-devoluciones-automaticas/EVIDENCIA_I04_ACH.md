# EVIDENCIA I04-ACH — 2026-11-02

## Línea base e integridad

Base exacta: `ACHInterbank_COMPLETO_DEVOLUCIONES_AUTOMATICAS_I03_ACHCOLOMBIA_2026-11-02.zip`. Se reconstruye un ZIP acumulativo completo preservando todo archivo de I03 que no haya sido cambiado intencionadamente. Manifiesto `MANIFIESTO_I04_ACH.json` documenta rutas, SHA-256 y conteos; el ZIP se comprueba con `ZipFile.testzip()`.

## Comprobaciones realizadas

1. `python scripts/test/automatic_returns_i02_ach_static_check.py` → **18/18 PASS_STATIC**.
2. `python scripts/test/automatic_returns_i03_ach_static_check.py` → **25/25 PASS_STATIC**.
3. `python scripts/test/automatic_returns_i04_ach_static_check.py` → **35/35 PASS_STATIC**.
4. Parseo de AST TypeScript de los archivos nuevos y existentes afectados → **6/6 sin errores de sintaxis**.
5. Inspección `ACHInterbank.sln`: **7/7 referencias de proyectos existentes**.
6. `dotnet` no encontrado en `PATH`; sin evidencia de compilación ni xUnit.
7. `node` y `tsc` global disponibles, pero no existen `node_modules` para resolver Angular; el parser no equivale a `ng build` ni pruebas Karma.

## Casos incorporados, no ejecutados

Backend (`AchColombiaReturnReviewI04Tests.cs`): aprobación sin movimiento financiero; duplicidad y rechazo de segunda aprobación; conflicto con devolución manual y descarte auditable; configuración desactivada; crédito tardío; aislamiento CENIT; R10 pendiente; propiedad de concurrencia EF.

Angular: consulta y contrato de decisión; pantalla con causal/estado; confirmación requerida; ausencia de permiso bloquea; HTTP 409 no se comunica como éxito. Se ajustó el stub del componente padre para preservar su prueba histórica.

## Límites pendientes de certificación

Compilar, ejecutar pruebas unitarias, validar migraciones PostgreSQL de I01/I03, verificar migración SQL Server efectiva, probar concurrencia multiinstancia, browser responsive y tiempo real de ciclos. La aprobación interna es **NO-GO** para generación/transmisión automática.
