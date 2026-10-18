# ACHInterbank NAV6 — Ajustes a pruebas Karma (18 octubre 2026)

## Evidencia recibida

El último registro del usuario reporta `TOTAL: 71 FAILED, 1025 SUCCESS` sobre 1096 pruebas tras una compilación de producción exitosa. Las advertencias de `braces` son indirectas de herramientas de desarrollo y son una incidencia independiente. En el registro previo, `npm audit --omit=dev` reportó cero vulnerabilidades de dependencias de producción.

## Cambios implementados

- Reconciliación de fixtures Router/NavigationEnd y validaciones de opciones del editor de mapeos.
- Declaraciones Angular Material faltantes en pruebas del catálogo NACHA-M.
- Eliminación de la directiva no soportada `matRowDefTrackBy` en las tablas de reclamaciones, pasando `trackBy` a `mat-table`.
- Ejecución de pruebas de componentes `OnPush` con entradas reales `fixture.componentRef.setInput` o marcación explícita cuando el test muta propiedades sin evento de DOM.
- Pruebas de diálogo observando la instancia inyectada que utiliza efectivamente el componente y sincronización en las transiciones Angular Material.
- Adaptación de asserts a textos funcionales aprobados y de pruebas CSS para analizar CSS y no el source map en base64; el token de fuente monoespaciada sigue permitido.
- En la importación WSDL/XSD, lectura segura del primer archivo de `FileList`, compatible con simuladores tipo array sin desactivar restricciones de importación.
- En prenotificaciones, protección de la cuenta de destino ingresada manualmente frente a la carga asíncrona de cuentas contextuales; para operaciones monetarias se sigue requiriendo la selección elegible.
- Ajustes de pruebas de catálogo, historial, perfiles, importación, reclamaciones, devoluciones y restricciones operativas.

## Verificación real en este entorno

- Todos los archivos `*.ts` de `src/app` analizados con el parser TypeScript, sin errores sintácticos.
- Comparación del ZIP contra la entrega de Karma anterior; todos los archivos originales conservados y solo cambios identificados documentados en este informe.
- Archivo ZIP reabierto y probado para CRC e integridad.

**No verificado:** instalación npm, `ng build`, ejecución de `npm test` ni número residual de fallos. Este entorno no puede acceder al registro de npm y no dispone de `node_modules` del proyecto. No interpretar la revisión estática como suite completada.

## Pasos en Windows

Desde `web\ach-interbank-ui`:

```cmd
npm run security:refresh-lock
npm ci
npm run build
npm test -- --watch=false
npm audit --omit=dev
```

`security:refresh-lock` puede seguir señalando `braces` en herramientas de desarrollo. No ejecutar `npm audit fix --force` sin revisar los cambios incompatibles en Karma.

