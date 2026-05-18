# Mitigación temporal de seguridad para `webpack-dev-server` (SPA Angular)

Fecha: 2026-05-18

## Contexto
El árbol actual de dependencias del SPA (`@angular-devkit/build-angular@21.2.7`) instala `webpack-dev-server@5.2.3`, reportado por `npm audit` con el advisory:
- GHSA-79cf-xcqc-c78w (exposición cruzada de código fuente en orígenes no HTTPS)

`npm audit` reporta actualmente **No fix available** en esta cadena transitiva.

## Decisión
No se aplica `npm audit fix --force` ni actualización mayor de Angular/Devkit sin análisis de compatibilidad y validación UAT.

## Mitigaciones operacionales obligatorias
1. **No usar `ng serve` para UAT o producción**.
2. **No exponer `ng serve` por HTTP a redes públicas**.
3. Para UAT, usar `npm run build` y servir `dist/` en infraestructura controlada (IIS/Nginx).
4. Si se requiere dev server, preferir `localhost` o HTTPS controlado.
5. No usar datos reales sensibles en sesiones con `ng serve` expuesto.

## Estado
- Riesgo clasificado como **dev-only** mientras no exista parche compatible en la cadena Angular Devkit.
- Se mantiene postura **NO-GO productivo**; este documento no cambia criterios de salida a producción.
