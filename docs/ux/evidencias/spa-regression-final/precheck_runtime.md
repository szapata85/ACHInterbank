# Pre-check runtime SPA regresion final

Fecha: 2026-05-24

## Git

- Commit corto: `d85151e6`
- `git status --short`: sin cambios pendientes al inicio de la fase.

## Docker

- `docker compose config --quiet`: OK.
- `docker compose ps`: API, PostgreSQL y SPA arriba.
- `achinterbank-api`: Up.
- `achinterbank-postgres`: Up, healthy.
- `achinterbank-spa`: Up.

## Health

- `GET http://localhost:743/health/live`: 200.
- `GET http://localhost:743/health/ready`: 200.

## Login demo

- Login demo: OK.
- Roles sanitizados: `Admin`, `ACH.Operator`.
- Password/token no impresos.

## Alcance

Pre-check ejecutado antes de la regresion final SPA. Productivo permanece NO-GO.
