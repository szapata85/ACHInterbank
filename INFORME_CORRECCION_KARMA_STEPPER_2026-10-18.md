# Revisión dirigida de Karma — accesibilidad del importador de contratos

Fecha: 2026-10-18.

## Autoridad de código y evidencia

- Código fuente de origen: ZIP completo `ACHInterbank-NAV6-source-ajustes-5-Karma-20261018.zip`, acumulado únicamente a partir de los ZIP suministrados en Sources. No se consultó ni utilizó Git/GitHub.
- SHA-256 del ZIP de entrada: `84970dacb093554bcd8ee3432294ed1bd97f1a6148a8166650eb1c172a7aea40`.
- Resultado de Windows recibido: 1.095/1.096 pruebas aprobadas, 1 fallida (`ContractImportStepperComponent mantiene controles de archivo y detalle técnico accesibles por teclado`).
- Única aserción fallida: Stepper Material reporta `selectedIndex` 0 cuando la prueba lo esperaba en 1. Los demás tests se ejecutaron hasta el final de la suite.

## Corrección

En `web/ach-interbank-ui/src/app/features/integrations/pages/contract-import-stepper.component.spec.ts` se establece primero el resultado `ready` del backend y se realiza un ciclo de detección de cambios para actualizar el `[completed]` del primer `mat-step`. Después, se selecciona **mediante el harness de Angular Material** el paso `Operación` como lo hace un usuario, y se comprueban:

1. Signal `currentStep()` = 1 y `MatStepper.selectedIndex` = 1, incluyendo `MatStepHarness.isSelected()`.
2. El botón `Continuar` lleva a `currentStep()` = 2 y `MatStepper.selectedIndex` = 2.
3. El `summary` del detalle técnico es enfocables por teclado; el input de archivo conserva su `aria-describedby` y foco.

Se mantiene `linear=true` y los controles de bloqueo del componente. No se elimina la verificación de foco ni se usan `skip`, `pending`, valores de retorno falsos o ajustes generales de tiempo. El avance automático después del análisis es probado por un caso separado del mismo spec.

## Integridad y límites de la validación

Solo se modificó el spec mencionado y se agregó este informe. Se conservaron todos los otros archivos y su contenido original del ZIP base. El TypeScript modificado fue analizado sintácticamente con TypeScript instalado localmente.

**No hay verificación ejecutada de Angular/Karma en este contenedor.** npm no resuelve `registry.npmjs.org` (`EAI_AGAIN`) y no existe `node_modules`, por lo que no se puede certificar que 1.096/1.096 pasen hasta una ejecución real en Windows. No afirmar PASSED / RELEASE_READY.

Windows CMD, carpeta `web\ach-interbank-ui`, con las dependencias locales instaladas:

```cmd
npm test -- --watch=false
```
