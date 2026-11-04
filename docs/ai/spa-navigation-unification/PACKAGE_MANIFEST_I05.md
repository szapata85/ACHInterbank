# Manifiesto de entrega I05 — ACH Interbank

**Programa:** `NAV-UX-MFT-20261027`  
**Artefacto padre:** `ACHInterbank_NAV_UX_MFT_2026-10-27_I04A_PROYECTO_COMPLETO.zip`  
**SHA-256 padre:** `94e6d0c03576b26a9ff34cd32a070679b6e49261f7facb2efe007aca8d2aabd0`  
**Artefacto de salida:** `ACHInterbank_NAV_UX_MFT_2026-10-27_I05_PROYECTO_COMPLETO.zip`  
**SHA-256 de salida:** archivo lateral `ACHInterbank_NAV_UX_MFT_2026-10-27_I05_SHA256.txt`.

## Alcance técnico del incremento

- Corrección del estado accesible de ramas ocultas cuando el lateral del SPA está compacto.
- Nueva prueba Angular de regresión (no ejecutada por falta de entorno), 3 nuevas validaciones estáticas y script + pruebas offline de integridad de línea base.
- Documentación, memoria y plan actualizados; MFT, seguridad IAM, endpoints y reglas monetarias no modificados.

## Puertas de calidad

| Comprobación | Estado |
| --- | --- |
| Retención de archivos padres | **PASS 4.472/4.472**, 0 faltantes |
| SHA-256 exacto de ZIP padre | **PASS** |
| Invariantes de navegación/MFT | **PASS 19/19** |
| Sintaxis/transpilación TypeScript | **PASS 4/4** |
| Tests offline del verificador de linaje | **PASS 3/3** |
| Angular build/unit/E2E, .NET test, seed/claims reales | **NOT_RUN_ENVIRONMENT / PENDING_DEPLOYMENT** |
| Aprobación para producción | **NO** |

## Evidencia principal

- `docs/ai/spa-navigation-unification/LINEAGE_VERIFICATION_I05.json`
- `docs/ai/spa-navigation-unification/TEST_EVIDENCE_I05_2026-10-27.md`
- `docs/ai/spa-navigation-unification/PROGRESS_REPORT_I05_2026-10-27.md`
- `docs/ai/spa-navigation-unification/MEMORY_STATE.md`
- `docs/ai/spa-navigation-unification/BASELINE_LINEAGE_I05.md`
- `docs/testing/SPA_NAVIGATION_MFT_TEST_PLAN_2026-10-27.md`

El ZIP I05 conserva el proyecto completo extraído del ZIP padre I04A. El hash de salida **no** se incluye dentro del ZIP para evitar autorreferencia.
