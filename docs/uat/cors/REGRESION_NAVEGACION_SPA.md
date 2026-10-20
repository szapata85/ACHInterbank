# Regresión de navegación SPA

Fecha: 2026-07-16.

## Cobertura

El smoke Playwright opt-in navegó contra SPA 743 y API 843 reales, sin mocks y sin ejecutar SOAP:

1. carga de login y branding;
2. autenticación;
3. menú principal;
4. usuarios;
5. transacciones;
6. consulta read-only del panel del core;
7. captura de errores de consola y red.

Resultado: **1/1 aprobado**. No se observaron mensajes `blocked by CORS policy`, ausencia de `Access-Control-Allow-Origin`, `net::ERR_FAILED` ni errores CORS equivalentes.

Durante la validación del panel se confirmó una regresión independiente de AG Grid 32: la selección por clic está deshabilitada por defecto. La lista de transacciones ahora declara `enableClickSelection: true`; así la selección dispara la consulta existente sin alterar datos ni ejecutar integración.

## Construcción y pruebas SPA

- `npm run build`: aprobado.
- Karma/ChromeHeadless: 395/395.
- Playwright smoke: aprobado.

No se modificó nginx, `apiBaseUrl`, el catálogo R96 ni el payload SOAP.
