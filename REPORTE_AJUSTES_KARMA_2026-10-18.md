# Ajustes de pruebas Karma — source ACHInterbank-NAV6.zip

Fuente exclusiva: `ACHInterbank-NAV6.zip` suministrado en el proyecto, sin Git ni GitHub.
SHA-256 del ZIP de entrada: `4093b5986f5085540cabd4ce6555102231a9eddbc4bebc2d962a15d4df920da1`.

## Evidencia recibida

La ejecución `npm test -- --watch=false` en Windows compiló el bundle de pruebas, alcanzó 732/1096 tests con 3 fallos, y luego fue interrumpida por un timeout de Jasmine, `NG0911` y desconexión de Chrome Headless. No hay evidencia de terminación de la suite.

## Correcciones puntuales

1. `web/ach-interbank-ui/src/app/features/ach-claims/ach-claims-list.component.spec.ts`: reset del permiso simulado (`allowCreate`) al inicio de cada caso para evitar contaminación entre pruebas cuando Jasmine altera el orden.
2. `web/ach-interbank-ui/src/app/features/integrations/pages/contract-import-stepper.component.spec.ts`: se espera la estabilización de Angular Material tras pasar al resumen y se verifica que el stepper seleccionó el índice 2, antes de comprobar que el elemento `summary` acepta foco de teclado.
3. `web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-profile-workspace-page.component.spec.ts`: comprobación sin esperas asíncronas de las selecciones deshabilitadas. Se validan la consulta contextual, los catálogos, los valores efectivamente ligados a `MatSelect` y el estado de solo lectura de los cuatro controles.

## Alcance y validación

- Solo cambiaron los tres archivos de pruebas anteriores. Ningún archivo del producto ni manifiesto npm fue modificado.
- Comprobación sintáctica de TypeScript para los tres archivos: satisfactoria mediante transpileModule.
- Comparación con el source de entrada: 4.333 entradas originales preservadas; ninguna omitida.
- Comprobación de integridad del ZIP entregado: satisfactoria.
- No se ejecutó `npm ci`, `npm run build` ni Karma en este entorno por falta de las dependencias del SPA. Las 1.096 pruebas deben confirmarse en Windows; no se afirma que pasen todas.

## Validación en Windows CMD

Desde `web\ach-interbank-ui` de una extracción nueva:

```cmd
npm run security:refresh-lock
npm ci
npm run build
npm test -- --watch=false
npm audit --omit=dev
```

Si el fallo de foco o la suite persiste, conservar el log completo de Karma para validar en el navegador; no aumentar indiscriminadamente el timeout ni silenciar aserciones.
