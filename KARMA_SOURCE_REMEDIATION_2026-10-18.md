# ACHInterbank NAV6 — revisión de Karma exclusivamente desde source

## Fuentes y alcance

La base de esta entrega es `ACHInterbank-NAV6-completo-SPA-Karma-ajustes-20261018.zip`, ya entregada al usuario, y el registro local `npm test -- --watch=false` del 18-10-2026. Sin GitHub, sin checkout de otra rama ni actualizaciones de dependencias.

El último resultado recibido fue **777 de 1096 pruebas ejecutadas, 17 fallidas**, seguido de un error `afterAll` en `NachaConfigProfilesPageComponent.cargarCatalogos`. **No** representa el resultado final de la suite completa.

## Correcciones en source de producto

- `shared/components/ui/ui-tarjeta.component.ts`: se incorpora proyección `<ng-content>` dentro de la tarjeta, preservando la alternativa de `TemplateRef` y habilitando formularios que se aportan entre etiquetas `<ui-tarjeta>`. Este defecto funcional explica el DOM vacío de `AchReturnOfReturnManagementComponent`.
- `features/transactions/components/transaction-create/transaction-create.component.ts`: el resumen de validación identifica específicamente una entidad financiera de destino no seleccionada, sin permitir guardar si falta una selección válida.

## Actualizaciones de pruebas

- Se elimina el retorno `undefined` del mock de catálogos del test de recuperación NACHA-M, permitiendo futuras invocaciones del observable.
- Se utilizan harnesses Angular Material para inspeccionar valores/opciones en los selectores, ya que sus opciones se representan en el CDK overlay y no como texto del DOM principal.
- En componentes `OnPush`, se actualizan entradas mediante `ComponentRef.setInput`, se accionan botones reales o se marca el detector propio cuando se cambia un estado manualmente.
- Se inyecta el `MatDialog` simulado correcto para probar confirmación de publicación. Los diálogos operativos continúan siendo obligatorios en el producto.
- La prueba de descarte marca el formulario como `dirty` antes de solicitar confirmación, en lugar de confiar en que `setValue()` lo marque.
- Las pruebas de carga NACHA-M verifican los selectores realmente presentes (`app-clearing-house-select`, input file), sin exigir campos Material ausentes del diseño.
- La prueba de navegación por teclado de importación contractual completa el análisis antes de avanzar por el stepper lineal.

## Validación y límites

- Los **517** archivos TypeScript de `src/app` se analizaron mediante `typescript.createSourceFile` sin errores de sintaxis.
- ZIP completo ensamblado a partir de la última entrega y validado con CRC; los archivos no mencionados permanecen idénticos a la entrega anterior.
- No se alteran `package.json`, `package-lock.json`, scripts de seguridad, backend .NET ni reglas ACH/CENIT/NACHA-M.
- **Pendiente**: ejecutar `npm run build`, `npm test -- --watch=false` y `npm audit --omit=dev` en Windows con dependencias instaladas. Este entorno no dispone de `node_modules` para validar la suite Karma. No afirmar que haya cero pruebas fallidas.

## Instrucciones

Extraer el ZIP completo en carpeta nueva y entrar en `web\ach-interbank-ui`:

```cmd
npm run security:refresh-lock
npm ci
npm run build
npm test -- --watch=false
npm audit --omit=dev
```

Si ya dispone del lockfile regenerado e instalación exitosa, puede preservar `package-lock.json` y `node_modules` en su instalación actual, y reemplazar solo los archivos `src/app` incluidos en este cambio. Nunca copiar `node_modules` dentro de esta distribución.
