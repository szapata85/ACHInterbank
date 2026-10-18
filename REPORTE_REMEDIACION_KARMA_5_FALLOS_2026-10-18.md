# Informe de remediación Karma — 2026-10-18

## Fuente

`ACHInterbank-NAV6.zip` proporcionado en los sources del proyecto. No se emplearon Git ni GitHub.

## Último diagnóstico comprobable

El registro del usuario muestra `1096` pruebas ejecutadas, `1091 SUCCESS` y `5 FAILED`.

## Cambios

1. `contract-import-stepper.component.spec.ts`: se simula explícitamente análisis listo, paso intermedio Operación y avance a Resumen. Se conserva la aserción del índice seleccionado y del foco accesible del detalle técnico.
2. `clearing-house-preferences.component.spec.ts`: los estados de carga/error/vacío ahora proceden de respuestas reales de los servicios simulados mediante `Subject` y `of([])`, en vez de escrituras directas en un componente `OnPush`; se comprueba el DOM de los estados compartidos y la existencia de la grilla.
3. `nacha-config-test-panel.component.spec.ts`: se comprueba el origen del primer campo, se selecciona el segundo por el evento del componente y se refleja el cambio de `@Input` que en producción haría el padre; después se valida el origen del segundo campo.
4. `operational-restrictions.component.spec.ts`: el control de concurrencia 409 usa una simulación determinista del snackbar del propio componente. Se comprueba que se intenta actualizar con el RowVersion antiguo, que se ofrece Recargar, y que al ejecutar esta acción se consulta y aplica el nuevo RowVersion.

No se modifican los contratos financieros, la implementación del frontend/backend, las dependencias ni los escenarios de seguridad.

## Validación efectuada

- Revisión de sintaxis de los `522` archivos `.ts`/`.tsx` del frontend mediante TypeScript 5.8.3 global: cero errores sintácticos (esto no sustituye el chequeo de tipos de Angular).
- Comparación archivo por archivo con el ZIP base: exactamente cuatro archivos `.spec.ts` modificados, más este informe.
- Integridad CRC del ZIP y SHA-256 verificados.

## Límite explícito

En este entorno `npm ping` a `registry.npmjs.org` falla con `EAI_AGAIN`; no hay `node_modules` de Angular instalado. Por tanto no se pudo ejecutar `npm run build` ni Karma. Estos cambios atienden las cinco causas observadas, pero **no está verificado** el resultado `1096/1096`.

## Validación reproducible en Windows CMD

Desde `web\ach-interbank-ui` con dependencias instaladas:

```cmd
npm run build
npm test -- --watch=false
npm audit --omit=dev
```

Esperado para cerrar la incidencia: `TOTAL: 1096 SUCCESS` y salida exitosa de Karma. Ante cualquier fallo nuevo, revisar el reporte completo sin alterar aserciones para ocultar defectos reales.
