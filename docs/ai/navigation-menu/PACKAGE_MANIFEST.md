# Package manifest — NAV-MENU-IA-001 documentation baseline

Baseline source ZIP:

`ACHInterbank-R34-TPA9B4-P8.4-EXTERNAL-HANDOFF-FULL-PROJECT.zip`

SHA-256:

`46f7543cb644e945a78690d650b8ed4effb210d93c296d457db120ece2fb7aa3`

## Added documentation

- `docs/ai/navigation-menu/START_HERE.md`
- `docs/ai/navigation-menu/MASTER_PROMPT_CHATGPT.md`
- `docs/ai/navigation-menu/PHASE_PROMPTS_SHORT.md`
- `docs/ai/navigation-menu/MEMORY_STATE.md`
- `docs/ai/navigation-menu/PACKAGE_MANIFEST.md`
- `docs/ai/navigation-menu/phases/PHASE_01_NAV1_BASELINE.md`
- `docs/ai/navigation-menu/phases/PHASE_02_NAV2_SEED_CANONICO.md`
- `docs/ai/navigation-menu/phases/PHASE_03_NAV3_SEGURIDAD.md`
- `docs/ai/navigation-menu/phases/PHASE_04_NAV4_SPA_COPY.md`
- `docs/ai/navigation-menu/phases/PHASE_05_NAV5_COBERTURA.md`
- `docs/ai/navigation-menu/phases/PHASE_06_NAV6_VALIDACION.md`
- `docs/architecture/NAVIGATION_MENU_INFORMATION_ARCHITECTURE.md`
- `docs/architecture/NAVIGATION_MENU_IMPLEMENTATION_PLAN.md`
- `docs/architecture/NAVIGATION_MENU_SEED_STRATEGY.md`
- `docs/security/NAVIGATION_MENU_SECURITY_MODEL.md`
- `docs/ux/NAVIGATION_MENU_COPY_GUIDE.md`
- `docs/ux/NAVIGATION_MENU_ROUTE_MATRIX.md`
- `docs/testing/NAVIGATION_MENU_TEST_STRATEGY.md`
- `docs/operations/NAVIGATION_MENU_NO_GIT_ZIP_WORKFLOW.md`

This package is documentation-only for NAV-MENU-IA-001. Product code behavior has not been intentionally changed by this baseline.


## NAV6 final handoff — 2026-10-15

Input exacto: `ACHInterbank-R34-NAV-IA-NAV5-FULL-PROJECT.zip`, SHA-256 `c11268bb2e5509abf6bbe39f4d2e0074714b1492674ed7d8b2b40183331ec115`.

Artefactos NAV6 agregados:

- `docs/ai/navigation-menu/phases/NAV6_INPUT_TREE_MANIFEST.json`
- `docs/ai/navigation-menu/phases/NAV6_FINAL_RECERTIFICATION.md`
- `scripts/validate-nav6-final-recertification.py`
- `artifacts/nav6/NAV6_VALIDATION_REPORT.json`
- `artifacts/nav6/NAV6_VALIDATION_LOG.txt`

Documentación de estado actualizada: `MEMORY.md`, `START_HERE.md`, `MEMORY_STATE.md`, `PHASE_06_NAV6_VALIDACION.md` y `NAVIGATION_MENU_IMPLEMENTATION_PLAN.md`. No se modificó ningún archivo productivo; el validador NAV6 compara el árbol final contra el manifest SHA-256 completo del ZIP NAV5. El paquete de salida es `ACHInterbank-R34-NAV-IA-NAV6-FULL-PROJECT.zip`; su SHA-256 se entrega externamente tras compresión y revalidación de una extracción limpia.
