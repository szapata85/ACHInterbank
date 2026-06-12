# Evidencias Tecnicas - Comite GO/NO-GO

Fecha de actualizacion: 2026-06-12
Alcance: Evidencia tecnica consolidada sin secretos, tokens, passwords ni datos reales.

| Evidencia | Estado | Fuente/documento | Observacion |
| --- | --- | --- | --- |
| dotnet-ci | OK | Reporte actual del proyecto / scorecard | Validacion backend reportada OK. |
| angular-ci | OK | Reporte actual del proyecto / scorecard | Validacion frontend reportada OK. |
| Backend local | OK | docs/go-live-readiness/SCORECARD_GO_LIVE_READINESS.md | Build/test local reportado OK. |
| Angular local | OK | docs/go-live-readiness/SCORECARD_GO_LIVE_READINESS.md | Build/test local reportado OK. |
| Docker runtime | OK | docs/go-live-readiness/DOCKER_RUNTIME_READINESS.md | Runtime disponible para UAT controlado. |
| PostgreSQL healthy | OK | docs/uat/EVIDENCIA_TECNICA_UAT_RUNTIME.md | Base de datos healthy. |
| API health | OK | docs/uat/EVIDENCIA_TECNICA_UAT_RUNTIME.md | Health checks reportados OK. |
| SPA Docker | OK | docs/go-live-readiness/DOCKER_RUNTIME_READINESS.md | SPA disponible. |
| Proxy Nginx SPA -> API | OK | docs/uat/EVIDENCIAS_UAT_TECNICO_BASICO.md | Rutas funcionales confirmadas sin fallback indebido. |
| Auth/login | OK | docs/uat/EJECUCION_UAT_TECNICO_BASICO.md | Login validado sin documentar secretos. |
| Navigation/menu | OK | docs/uat/EVIDENCIAS_UAT_TECNICO_BASICO.md | Menu autenticado validado. |
| Endpoints protegidos read-only | OK | docs/uat/EVIDENCIAS_UAT_TECNICO_BASICO.md | Endpoints respondieron desde API. |
| NU1903 | Corregido tecnicamente | docs/security/REVISION_SEGURIDAD_PRE_GO_LIVE.md | Vulnerabilidad reportada como corregida. |
| BatchResolver | Corregido | docs/go-live-readiness/SCORECARD_GO_LIVE_READINESS.md | Falla preexistente reportada como corregida. |
| G3.5 naming dinamico | GO tecnico con observaciones | Commit `7c3cbb21` | `RRRRTTT.ZZZ.N`; ciclo desde `CycleName` con un unico entero positivo. |
| G3.5.1 cleanup | GO tecnico con observaciones | Commit `ebf7a8a5` | OpenBao/HashiCorp Vault retirado; KeyVault separado. |
| G3.5.2 pre-G3.6 | Cerrado | Commit `c7a5ad50` | Migraciones false por defecto y escaneo residual. |
| G3.6A | GO tecnico | Commit `e5721150`; spec inbound | PostgreSQL/Quartz reales; 2/2; `Proc_Transacciones` dry-run. |
| G3.6B | GO tecnico con observacion | Commit `e5721150`; spec outbound | 2/2; `Proc_Contrapartidas` dry-run; correlacion por `AchCycleId`. |
| Regresion G3.6 | OK | Commit `e5721150` | Build 0 warnings/errores; backend 1652+1 skip; Angular 347/347. |

No existe run GitHub Actions asociado a `e5721150` en el inventario consultado. No se atribuye un job CI inexistente; Quartz queda evidenciado mediante `TaskExecutionLog`.
