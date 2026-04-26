# Validación técnica — Prompt 9 SPA read-only Capability Registry (2026-04-26)

## Alcance

Se implementa pantalla Angular de solo lectura para consultar Capability Registry por riel, consumiendo la API Prompt 8:

- `GET /api/payment-rails/capability-registry/rails`
- `GET /api/payment-rails/capability-registry/rails/{railCode}/capabilities`
- `GET /api/payment-rails/capability-registry/rails/{railCode}/capabilities/{capabilityCode}`

Sin endpoints de edición en frontend y sin cambios en lógica operativa ACH/CENIT.

## Implementación SPA

- Nuevo feature module lazy: `payment-rail-capability-registry`.
- Nueva ruta protegida por permisos read-only:
  - `payment-rail-capability-registry`
  - permisos permitidos por guard: `CanViewPaymentRailCapabilityRegistry | CanManageAch | CanReadAch`.
- Pantalla con:
  - filtros reactivos (`railCode`, `capabilityCode`, `asOfUtc`);
  - listado usando `ui-grilla-empresarial` (AG-GRID);
  - indicadores de estado de consulta;
  - mensajes explícitos de modo read-only (sin edición).

## Validación ejecutada

```bash
cd web/ach-interbank-ui
npm ci
npm run build
```

- Build Angular OK.

```bash
npm test -- --watch=false --browsers=ChromeHeadless --include src/app/features/payment-rail-capability-registry/services/payment-rail-capability-registry-api.service.spec.ts
```

- No ejecutable en este entorno por limitación de browser runtime (`CHROME_BIN` no disponible) y error de Karma/rimraf en entorno actual.

## Conclusión

Prompt 9 SPA implementado en modo read-only:

- consulta por riel/capability operativa en UI;
- sin acciones de edición;
- sin impacto en decisiones legacy;
- sin cutover.
