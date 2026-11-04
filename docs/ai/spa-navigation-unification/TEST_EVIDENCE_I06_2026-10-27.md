# Registro de evidencia I06 — Navegación hover/contraste

- `python scripts/validate-spa-navigation-mft-unification.py`: **PASS 23/23**, 4 nuevos checks de estilos / cobertura E2E.
- TypeScript global `transpileModule` en 4 archivos: **PASS 4/4 (solo sintaxis)**; `e2e/navigation-sidebar-hover-contrast.spec.ts` incluye color primario claro, sidebar oscuro/claro y estados hover/foco/activo/expandido.
- `npm run build`, `npm test`, `npx playwright test`: **NOT_RUN_ENVIRONMENT**; el árbol ZIP no incluye `node_modules` completo ni API/DB QA desplegadas. La existencia de Chromium por sí sola no prueba que la SPA pueda arrancar.
- `dotnet build/test`, test de seeder y permisos MFT: **NOT_RUN_ENVIRONMENT** (SDK/servicios no presentes).
- Pruebas Playwright codificadas pero no ejecutadas **no son PASS**. No hay certificado de contraste runtime ni aprobación productiva.
- Integridad ZIP e invariante de linaje: consultar `LINEAGE_VERIFICATION_I06.json` y checksum externo de entrega.
