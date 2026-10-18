# SPA 4C.3C1b — Angular Validation Environment Blocker Checkpoint

## Tipo de checkpoint

Checkpoint **READ-ONLY** cuyo único objetivo es documentar con precisión el requisito de entorno que bloquea la validación Angular del baseline estáticamente cerrado.

No se instalaron/restauraron dependencias y no se modificó ningún source, HTML/TS/SCSS/CSS/spec, `package.json`, `package-lock.json`, `angular.json`, `tsconfig`, backend, routing, branding, permisos ni configuración. El único artefacto agregado por este micro-lote es este reporte.

## Baseline e integridad

- Baseline: `ACHInterbank-v123-spa-4C3C1a-angular-build-validation-gate.zip`
- SHA-256 esperado: `85de642718c5880cfdc4edd74b56119eeafb01f972494951cc5820d419f24a67`
- SHA-256 observado: `85de642718c5880cfdc4edd74b56119eeafb01f972494951cc5820d419f24a67`
- Integridad: **coincide**.

El baseline contiene el reporte 4C.3C1a que deja la compuerta de build bloqueada por ausencia del Angular CLI local y conserva la precondición estática:

`4C3C0_STATIC_CLOSED_VALIDATION_PENDING`

## 1. Contrato Angular declarado — `package.json`

Archivo revisado exclusivamente desde el baseline existente:

`web/ach-interbank-ui/package.json`

Contrato observado:

```text
scripts.build = "ng build"
@angular/cli = ^21.2.19
@angular-devkit/build-angular = ^21.2.19
@angular/compiler-cli = ^21.2.19
```

Los tres paquetes Angular requeridos por la compuerta están declarados en `devDependencies`.

No se modificó `package.json`.

## 2. Contrato resuelto — `package-lock.json`

Archivo revisado exclusivamente desde el baseline existente:

`web/ach-interbank-ui/package-lock.json`

Valores observados:

```text
lockfileVersion = 3
node_modules/@angular/cli = 21.2.19
node_modules/@angular-devkit/build-angular = 21.2.19
node_modules/@angular/compiler-cli = 21.2.19
```

Las tres entradas resueltas están presentes en el lockfile con versión `21.2.19`.

No se modificó `package-lock.json`.

## 3. Ejecutable Angular local materializado

Se comprobó exclusivamente la ruta solicitada en el baseline extraído:

`web/ach-interbank-ui/node_modules/.bin/ng`

Resultado exacto:

`ng: not found`

No se ejecutó ningún mecanismo alternativo de resolución del CLI.

## 4. Conclusión del blocker de entorno

El baseline **sí contiene el contrato de dependencias Angular**: el script `build` invoca `ng build`, los paquetes `@angular/cli`, `@angular-devkit/build-angular` y `@angular/compiler-cli` están declarados, y el lockfile contiene resoluciones concretas `21.2.19` para los tres.

Sin embargo, el entorno actual **no dispone del ejecutable local materializado** en `web/ach-interbank-ui/node_modules/.bin/ng`.

Por tanto, la validación real de `build`/`test` requiere que las dependencias sean **provisionadas externamente** antes de reintentar la compuerta 4C.3C1a. Este checkpoint no instala, restaura ni corrige dependencias y no interpreta la ausencia del ejecutable como fallo de compilación.

## 5. Comandos expresamente no ejecutados

No se ejecutó:

- `npm ping`;
- `npm install`;
- `npm ci`;
- `npm update`;
- ningún retry contra registry;
- `npx`;
- `ng build`;
- `ng test`;
- Playwright;
- ningún script npm.

No existe exit code de build o test porque ninguno fue invocado.

## 6. Validaciones estáticas de continuidad

Se mantuvo el checkpoint estrictamente READ-ONLY y se efectuó únicamente un scan estático de continuidad sobre los archivos existentes.

```text
4C3C0_STATIC_CLOSED_VALIDATION_PENDING permanece intacto = sí
feature -> feature = 0
core -> features = 0
shared -> features = 0
backend diff = 0
routing diff = 0
branding runtime bypasses = 0
UI source diff = 0
```

### Dependencias entre capas/features

El scan de imports TypeScript relativos/resolubles bajo `web/ach-interbank-ui/src/app` mantiene:

```text
feature -> feature = 0
core -> features = 0
shared -> features = 0
```

### Branding runtime

El scan de los patrones de bypass runtime ya cerrados en 4C.3C0 no encontró reintroducciones en estilos productivos fuera de `_tokens.scss`:

```text
#2563eb = 0
#1d4ed8 = 0
rgba(37, 99, 235, ...) = 0
rgba(226, 232, 240, .28) = 0
rgba(226, 232, 240, .48) = 0
```

Resultado consolidado:

`branding runtime bypasses = 0`

### Inmutabilidad de salida

El ZIP de salida conserva todas las entradas preexistentes del baseline sin cambio de contenido y agrega únicamente este reporte. En consecuencia:

- UI source: `diff = 0`;
- backend `src/`: `diff = 0`;
- routing frontend: `diff = 0`.

No se reabre ningún frente estático ni se modifica el estado de cierre 4C.3C0.

## Estado final

`4C3C1B_ANGULAR_VALIDATION_ENVIRONMENT_BLOCKER_DOCUMENTED`

No se declara `VERIFIED`.

No se instalaron dependencias, no se volvió a ejecutar el build, no se ejecutaron tests y no se avanzó al siguiente lote.
