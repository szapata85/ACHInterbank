# SPA 4C.3C1a — Angular Build Validation Gate

## Tipo de checkpoint

Micro-lote de validación **READ-ONLY** cuyo único objetivo es abrir la primera compuerta de validación Angular real del baseline 4C.3C0 estáticamente cerrado, sin modificar código ni instalar/restaurar dependencias.

No se modificó ningún source, HTML/TS/SCSS/CSS/spec, `package.json`, `package-lock.json`, `angular.json`, `tsconfig`, backend, routing, branding, permisos ni configuración. El único artefacto agregado por este micro-lote es este reporte.

## Baseline e integridad

- Baseline: `ACHInterbank-v122-spa-4C3C0-final-static-closure-checkpoint.zip`
- SHA-256 esperado: `b10459b5810245ca486d24c0a11c6d7a3435fb2c9d3fdaea17105ffee30c5353`
- SHA-256 observado: `b10459b5810245ca486d24c0a11c6d7a3435fb2c9d3fdaea17105ffee30c5353`
- Integridad: **coincide**.

La comparación de manifiesto entre v121 y el baseline v122 confirma que v122 agregó exclusivamente `SPA-4C3C0AV-FINAL-STATIC-CLOSURE-CHECKPOINT.md`: 1 entrada agregada, 0 removidas y 0 entradas preexistentes modificadas. Por tanto, el baseline de este lote conserva byte-a-byte el source/backend/routing de v121 y la precondición de cierre estático documentada por 4C.3C0av.

## Precondición 4C.3C0

Se mantiene:

`4C3C0_STATIC_CLOSED_VALIDATION_PENDING`

No se reabre ningún frente estático de 4C.3C0 en este lote.

## Paso 1 — Angular CLI local

Se comprobó exclusivamente, desde la raíz del repositorio contenido en el baseline:

`web/ach-interbank-ui/node_modules/.bin/ng`

Resultado exacto:

`ng: not found`

Conforme al contrato del micro-lote, la validación Angular se detiene en este punto.

No se ejecutó:

- `npm ping`;
- `npm install`;
- `npm ci`;
- ningún retry contra registry;
- `npx`;
- `ng build`;
- tests.

No existe exit code de build porque el build **no fue invocado**.

## Continuidad estática

El baseline v122 ya declaró el cierre estático de 4C.3C0 y, como este micro-lote no modifica ningún archivo preexistente, sus invariantes permanecen sin cambio:

```text
4C3C0 sigue STATIC_CLOSED = sí
feature -> feature = 0
core -> features = 0
shared -> features = 0
backend diff = 0
routing diff = 0
branding runtime bypasses = 0
```

La validación de manifiesto del ZIP de salida debe conservar todas las entradas preexistentes del baseline sin modificación y agregar únicamente este reporte; por construcción no existe cambio de source, backend ni routing.

## Resultado de la compuerta

La compuerta de build queda **bloqueada por ausencia del Angular CLI local**. No se interpreta este bloqueo como fallo de compilación y no se corrige ni instala nada dentro de este micro-lote.

No se declara `VERIFIED`.

## Estado final

`4C3C1A_ANGULAR_BUILD_VALIDATION_BLOCKED_NG_NOT_FOUND`

No se avanza a tests ni al siguiente lote.
