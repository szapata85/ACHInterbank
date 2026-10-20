# Ajustes de las pruebas Angular · 18 de octubre de 2026

## Alcance

Este es el proyecto **completo**, basado en `ACHInterbank-NAV6-completo-SPA-tests-corregidos.zip` y conservando los 4330 archivos existentes. Se modifican **únicamente dos** archivos de pruebas `*.spec.ts`, y se agrega este informe.

1. `src/app/features/integrations/state/contract-workspace.store.spec.ts`: la simulación actualizaba el parámetro B (`detail.parameters[0]`) pero verificaba el parámetro A al ordenarlo por `sortOrder`. Ahora actualiza y verifica el mismo parámetro A identificado por `parameterPath`.
2. `src/app/features/integrations/pages/soap-integration-settings.component.spec.ts`: inyecta el espía `MatDialog` en el inyector del componente standalone, evitando abrir diálogos reales en Karma; actualiza las expectativas de textos para coincidir con la interfaz vigente; al reconsultar después del PUT simula correctamente la respuesta fresca del backend; invoca `markForCheck()` para el estado modificado manualmente bajo `OnPush`; agrega un caso de rechazo de la previsualización backend que **prohíbe** abrir diálogo o guardar.

## Validación efectuada

- Coincidencia de nombres de 4330 archivos con el ZIP base.
- Verificación CRC: exactamente los dos archivos enumerados difieren del ZIP base.
- Parseo/transpilación sintáctica de ambos `.spec.ts` con TypeScript: sin errores sintácticos.
- Integridad de los elementos del nuevo ZIP comprobada.

**Limitación:** no se han ejecutado `npm ci`, `npm run build` ni los 1095+ casos de Karma en este entorno debido a falta de acceso al registro npm. No afirmar que toda la suite pasa hasta ejecutarla en Windows.

## Validación en CMD (Windows)

Desde `web\ach-interbank-ui`:

```cmd
npm run security:refresh-lock
npm ci
npm run build
npm test -- --watch=false
npm audit --omit=dev
```

Si tienes un `package-lock.json` actualizado por `security:refresh-lock` en tu directorio existente, conserva ese archivo: el ZIP contiene el lockfile de la entrega anterior, que debe sincronizarse de nuevo antes de `npm ci`.

El aviso de las 11 vulnerabilidades `braces` en dependencias de desarrollo es independiente; no usar `npm audit fix --force` a ciegas.
