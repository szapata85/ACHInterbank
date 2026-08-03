# Angular 21.2.19 security patch

Fecha: 2026-08-03

## Contexto y causa

El audit productivo reportaba ocho vulnerabilidades altas, incluidas
`GHSA-jhpw-976m-542j` y `GHSA-jj27-h5hq-8x99`, para Angular 21.2.18 y
anteriores. El primer intento de actualización dejó paquetes framework y
`@angular/compiler-cli` en 21.2.18, cuyos peers exactos impedían resolver una
mezcla con 21.2.19.

Se alinearon todos los paquetes framework directos y `compiler-cli` a 21.2.19.
CLI, DevKit y el build transitivo ya estaban en 21.2.19. Material y CDK se
mantienen en 21.2.14, su última publicación Angular 21, compatible por peers.

## Lockfile

Los intentos de actualización conservando el lockfile heredado devolvieron
`ERESOLVE` por peers 21.2.18. Se respaldó temporalmente fuera del repositorio,
se eliminó el lockfile heredado y npm generó uno nuevo desde el `package.json`
alineado. No se usaron `--force`, `--legacy-peer-deps`, overrides nuevos ni
Angular 22.

`npm ci` reproduce el árbol: framework, CLI, compiler-cli y build en 21.2.19;
Material/CDK en 21.2.14; sin Angular 22 ni Angular 21.2.18 afectados.

## Validación

- `npm audit --omit=dev --audit-level=high`: 0 vulnerabilidades.
- Ambos advisories originales ausentes del audit JSON.
- `npm run build`: correcto.
- `npm test -- --watch=false --browsers=ChromeHeadless`: 685 correctas.
- Playwright del monitor de salida: 4 correctas, incluidos escritorio, móvil y
  tableta, sin errores de consola ni respuestas 5xx.
- Imagen Docker `achinterbank-spa` reconstruida con `npm ci`; SPA y
  `/health/live` responden HTTP 200.

No se modificó código backend ni workflow. CI remoto queda pendiente de la
publicación del commit local.
